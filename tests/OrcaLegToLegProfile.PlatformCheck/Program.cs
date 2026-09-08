using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO.Compression;
using System.Xml.Linq;
using System.Text.Json;
using System.Security.Cryptography;

// Source and installed-metadata checks only; NinjaTrader F5 and template validation are separate.
string root = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--")) ?? Directory.GetCurrentDirectory());
bool liveGenerated = args.Contains("--live-generated");
string relative = "Orca Trades/Working_Suite/Indicators/OrcaLegtoLegProfile.cs";
string source = File.ReadAllText(Path.Combine(root, relative));
using var archive = ZipFile.OpenRead(Path.Combine(root, ".codex-backups/leg-to-leg-pre-settings-2026-09-07_233554/LegToLeg-pre-settings.zip"));
using var reader = new StreamReader(archive.GetEntry("workspace/" + relative)!.Open());
string baseline = reader.ReadToEnd();
var oldRoot = CSharpSyntaxTree.ParseText(baseline).GetRoot();
var newRoot = CSharpSyntaxTree.ParseText(source).GetRoot();
string Tokens(SyntaxNode node) => string.Join(" ", node.DescendantTokens().Select(t => t.Text));
var stripper = new SettingsPresentationStripper();
if (Tokens(stripper.Visit(oldRoot)!) != Tokens(stripper.Visit(newRoot)!))
    throw new Exception("Change outside Display metadata and the exact indicator description.");
var indicator = newRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.Text == "OrcaLegtoLegProfile");
var descriptions = indicator.DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(a => a.Left.ToString() == "Description").ToArray();
if (descriptions.Length != 1 || descriptions[0].Right is not LiteralExpressionSyntax literal || literal.Token.ValueText != SettingsPresentationStripper.Description)
    throw new Exception("Indicator description mismatch.");
Console.WriteLine("PASS: all non-presentation source tokens match the backup, including defaults, property identities, ranges, serialization, enums, calculations, rendering, data paths and generated wrappers.");

Dictionary<string, PropertyDeclarationSyntax> AutoSettings(SyntaxNode node) => node.DescendantNodes().OfType<ClassDeclarationSyntax>()
    .Single(c => c.Identifier.Text == "OrcaLegtoLegProfile").Members.OfType<PropertyDeclarationSyntax>()
    .Where(p => p.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == "Display"))
    .ToDictionary(p => p.Identifier.Text);
var original = AutoSettings(oldRoot);
var settings = AutoSettings(newRoot);
if (settings.Count != 77 || !original.Keys.Order().SequenceEqual(settings.Keys.Order()))
    throw new Exception("Settings inventory changed.");
AttributeSyntax? Display(PropertyDeclarationSyntax p) => p.AttributeLists.SelectMany(a => a.Attributes).SingleOrDefault(a => a.Name.ToString() == "Display");
string Value(AttributeSyntax display, string name) => ((LiteralExpressionSyntax)display.ArgumentList!.Arguments.Single(a => a.NameEquals?.Name.Identifier.Text == name).Expression).Token.ValueText;
var positions = new HashSet<(string, string)>();
var groups = new HashSet<string>();
foreach (var setting in settings)
{
    var display = Display(setting.Value) ?? throw new Exception("Unorganized setting: " + setting.Key);
    var group = Value(display, "GroupName");
    if (!positions.Add((group, Value(display, "Order")))) throw new Exception("Duplicate display order: " + setting.Key);
    groups.Add(group);
}
if (groups.Count != 11) throw new Exception("Expected eleven settings groups.");
var hidden = settings.Values.Where(p => p.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == "Browsable" && a.ArgumentList!.Arguments.Single().Expression.ToString() == "false")).Select(p => p.Identifier.Text).Order().ToArray();
if (!hidden.SequenceEqual(new[] { "LegBoxBrush", "ShowCurrentLegBox" })) throw new Exception("Unexpected hidden settings.");
Console.WriteLine("PASS: 75 visible settings in 11 groups; two leg-box properties retained hidden for compatibility.");

var cvp = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root, "Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs"))).GetRoot()
    .DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.Text == "OrcaCandleVolumeProfile")
    .Members.OfType<PropertyDeclarationSyntax>().ToDictionary(p => p.Identifier.Text);
string[] shared = { "VolumeTickCompression", "DeltaTickCompression", "UseDynamicAggregation", "DynamicAggregationMultiplier",
    "DeltaDynamicRowMinPixels", "DynamicDeltaMinCompression", "DynamicDeltaMaxCompression", "ProfileBarSpacingPx", "VolumeBrush", "VolumeOpacity", "MinBrightness", "UseGradient", "GradientSteps", "ShowPOC", "POCBrush",
    "ShowValueArea", "ValueAreaPercent", "ShowVAColor", "VABrush", "ShowVALines", "VALineBrush", "VALineThickness" };
var aliases = new Dictionary<string,string> { ["VolumeTickCompression"] = "TickCompression", ["UseDynamicAggregation"] = "UseDynamicDeltaAggregation",
    ["DynamicAggregationMultiplier"] = "DeltaDynamicMultiplier" };
foreach (string name in shared)
{
    string cvpName = aliases.TryGetValue(name, out var alias) ? alias : name;
    if (Value(Display(settings[name])!, "Name") != Value(Display(cvp[cvpName])!, "Name")) throw new Exception("Shared CVP label mismatch: " + name);
}
Console.WriteLine("PASS: " + shared.Length + " shared setting labels match CVP.");

// Read actual saved XML without instantiating NinjaTrader or changing templates.
var owned = indicator.Members.OfType<PropertyDeclarationSyntax>().Select(p => p.Identifier.Text).ToHashSet();
using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".codex-backups/leg-to-leg-pre-settings-2026-09-07_233554/manifest.json")));
int templateFiles = 0, instances = 0, ownedValues = 0;
foreach (var file in manifest.RootElement.GetProperty("Files").EnumerateArray())
{
    string entryName = file.GetProperty("Entry").GetString()!;
    if (!entryName.StartsWith("saved-templates/")) continue;
    using var entryStream = archive.GetEntry(entryName)!.Open();
    using var bytes = new MemoryStream();
    entryStream.CopyTo(bytes);
    byte[] archived = bytes.ToArray();
    string expectedHash = file.GetProperty("SHA256").GetString()!;
    if (!Convert.ToHexString(SHA256.HashData(archived)).Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
        throw new Exception("Archived template hash mismatch: " + entryName);
    byte[] current = File.ReadAllBytes(file.GetProperty("Source").GetString()!);
    if (!current.SequenceEqual(archived)) throw new Exception("Saved template changed since backup: " + entryName);
    using var xmlStream = new MemoryStream(archived);
    var xml = XDocument.Load(xmlStream);
    var profiles = xml.Descendants().Where(e => e.Name.LocalName == "OrcaLegtoLegProfile" && e.Elements().Any(c => c.Name.LocalName == "ReversalTicks")).ToArray();
    if (profiles.Length == 0) throw new Exception("Missing profile instance: " + entryName);
    instances += profiles.Length;
    ownedValues += profiles.Sum(e => e.Elements().Count(c => owned.Contains(c.Name.LocalName)));
    templateFiles++;
}
if (templateFiles != 55) throw new Exception("Expected 55 saved template files.");
if (!owned.Contains("UseSeparateResetAggregation")) throw new Exception("Legacy reset aggregation compatibility property missing.");
Console.WriteLine($"PASS: {templateFiles} saved XML files unchanged and hash-verified; {instances} profile instances, {ownedValues} source-owned values backed by unchanged property definitions. Actual NinjaTrader loading remains untested.");

string livePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8/bin/Custom/Indicators/OrcaLegtoLegProfile.cs");
string Authored(string text) => text.Replace("\r\n", "\n").Split("#region NinjaScript generated code")[0].TrimEnd();
string compiledSource = liveGenerated ? File.ReadAllText(livePath) : Authored(source);
if (liveGenerated && Authored(compiledSource) != Authored(source)) throw new Exception("Deployed authored source mismatch.");
var trees = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(compiledSource, new CSharpParseOptions(LanguageVersion.CSharp7_3), "OrcaLegtoLegProfile.cs") };
if (liveGenerated)
    trees.Add(CSharpSyntaxTree.ParseText(@"
namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns {
 public partial class MarketAnalyzerColumn { private NinjaTrader.NinjaScript.Indicators.Indicator indicator; }
}
namespace NinjaTrader.NinjaScript.Strategies {
 public partial class Strategy { private NinjaTrader.NinjaScript.Indicators.Indicator indicator; }
}", new CSharpParseOptions(LanguageVersion.CSharp7_3), "OfflineGeneratedHostFields.cs"));
// The isolated local Indicator partial shadows the installed factory host. Include
// NinjaTrader's existing generated ATR factory, without changing any live source.
if (liveGenerated)
{
    string atrSource = File.ReadAllText(Path.Combine(Path.GetDirectoryName(livePath)!, "@ATR.cs"));
    int factoryStart = atrSource.IndexOf("#region NinjaScript generated code", StringComparison.Ordinal);
    if (factoryStart < 0) throw new Exception("Installed ATR generated factory missing.");
    trees.Add(CSharpSyntaxTree.ParseText(atrSource.Substring(factoryStart), new CSharpParseOptions(LanguageVersion.CSharp7_3), "InstalledATRGeneratedFactory.cs"));
}
string framework = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET/Framework64/v4.0.30319");
string platform = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NinjaTrader 8/bin");
var paths = new List<string>();
foreach (string name in new[] { "mscorlib", "System", "System.Core", "System.Xml", "System.ComponentModel.DataAnnotations", "System.Xaml", "System.Data" }) paths.Add(Path.Combine(framework, name + ".dll"));
foreach (string name in new[] { "WindowsBase", "PresentationCore", "PresentationFramework" }) paths.Add(Path.Combine(framework, "WPF", name + ".dll"));
foreach (string name in new[] { "NinjaTrader.Core", "NinjaTrader.Gui", "SharpDX", "SharpDX.Direct2D1", "SharpDX.DXGI" }) paths.Add(Path.Combine(platform, name + ".dll"));
paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8/bin/Custom/NinjaTrader.Custom.dll"));
var compilation = CSharpCompilation.Create("LegToLegOfflineCheck", trees, paths.Select(p => MetadataReference.CreateFromFile(p)), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
foreach (var error in errors) Console.WriteLine(error);
Console.WriteLine($"Offline Leg-to-Leg Profile semantic check ({(liveGenerated ? "live including generated wrappers" : "authored source")}): {errors.Length} errors. NinjaTrader F5/runtime remain separate.");
return errors.Length == 0 ? 0 : 1;

sealed class SettingsPresentationStripper : CSharpSyntaxRewriter
{
    public const string Description = "Displays volume and delta across price swings, with tick- or ATR-based leg detection. Includes active and historical profiles, point of control, value area, and optional active-leg delta resets with statistics.";
    public const string OldDescription = "Rotation-based leg delta/volume profile with Value Area, POC, gradient support, and an optional live active-leg delta reset.";
    public override SyntaxNode? VisitAttributeList(AttributeListSyntax node)
    {
        if (node.Parent is PropertyDeclarationSyntax p && (p.Identifier.Text == "ShowCurrentLegBox" || p.Identifier.Text == "LegBoxBrush")
            && node.Attributes.Count == 1 && node.Attributes[0].ToString() == "Browsable(false)") return null;
        return node.Attributes.All(a => a.Name.ToString() == "Display") ? null : base.VisitAttributeList(node);
    }
    public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        if (node.Expression is AssignmentExpressionSyntax a && a.Left.ToString() == "Description"
            && a.Right is LiteralExpressionSyntax literal && (literal.Token.ValueText == Description || literal.Token.ValueText == OldDescription)) return null;
        return base.VisitExpressionStatement(node);
    }
}
