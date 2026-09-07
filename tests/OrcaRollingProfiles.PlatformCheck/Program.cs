using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO.Compression;

// Source and installed-metadata checks only; NinjaTrader F5 and template validation are separate.
string root = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--")) ?? Directory.GetCurrentDirectory());
bool liveGenerated = args.Contains("--live-generated");
string relative = "Orca Trades/Working_Suite/Indicators/OrcaRollingProfiles.cs";
string source = File.ReadAllText(Path.Combine(root, relative));
using var archive = ZipFile.OpenRead(Path.Combine(root, ".codex-backups/rolling-profiles-pre-settings-2026-09-07_184716/RollingProfiles-pre-settings.zip"));
using var reader = new StreamReader(archive.GetEntry("workspace/" + relative)!.Open());
string baseline = reader.ReadToEnd();
var oldRoot = CSharpSyntaxTree.ParseText(baseline).GetRoot();
var newRoot = CSharpSyntaxTree.ParseText(source).GetRoot();
string Tokens(SyntaxNode node) => string.Join(" ", node.DescendantTokens().Select(t => t.Text));
var stripper = new SettingsPresentationStripper();
if (Tokens(stripper.Visit(oldRoot)!) != Tokens(stripper.Visit(newRoot)!))
    throw new Exception("Change outside Display metadata and the exact indicator description.");
var indicator = newRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.Text == "OrcaRollingProfiles");
var descriptions = indicator.DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(a => a.Left.ToString() == "Description").ToArray();
if (descriptions.Length != 1 || descriptions[0].Right is not LiteralExpressionSyntax literal || literal.Token.ValueText != SettingsPresentationStripper.Description)
    throw new Exception("Indicator description mismatch.");
Console.WriteLine("PASS: all non-presentation source tokens match the backup, including defaults, property identities, ranges, serialization, enums, calculations, rendering, data paths and generated wrappers.");

Dictionary<string, PropertyDeclarationSyntax> AutoSettings(SyntaxNode node) => node.DescendantNodes().OfType<ClassDeclarationSyntax>()
    .Single(c => c.Identifier.Text == "OrcaRollingProfiles").Members.OfType<PropertyDeclarationSyntax>()
    .Where(p => p.AccessorList != null && p.AccessorList.Accessors.Count == 2 && p.AccessorList.Accessors.All(a => a.Body == null && a.ExpressionBody == null))
    .ToDictionary(p => p.Identifier.Text);
var original = AutoSettings(oldRoot);
var settings = AutoSettings(newRoot);
if (settings.Count != 52 || !original.Keys.Order().SequenceEqual(settings.Keys.Order()))
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
if (groups.Count != 8) throw new Exception("Expected eight settings groups.");
if (original.Values.Count(p => Display(p) == null) != 6) throw new Exception("Unexpected original ungrouped setting count.");
Console.WriteLine("PASS: all 52 settings retained in 8 groups with unique ordering; 6 existing Misc settings now have labels/groups.");

var cvp = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root, "Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs"))).GetRoot()
    .DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.Text == "OrcaCandleVolumeProfile")
    .Members.OfType<PropertyDeclarationSyntax>().ToDictionary(p => p.Identifier.Text);
string[] shared = { "VolumeTickCompression", "DeltaTickCompression", "UseDynamicAggregation", "DynamicAggregationMultiplier",
    "DeltaDynamicRowMinPixels", "DynamicDeltaMinCompression", "DynamicDeltaMaxCompression", "ProfileWidthPx", "DeltaWidthPx",
    "ProfileBarSpacingPx", "VolumeBrush", "VolumeOpacity", "MinBrightness", "UseGradient", "GradientSteps", "ShowPOC", "POCBrush",
    "ShowValueArea", "ValueAreaPercent", "ShowVAColor", "VABrush", "ShowVALines", "VALineBrush", "VALineThickness",
    "ShowDeltaText", "DeltaTextMinThreshold", "DeltaTextFontSize" };
var aliases = new Dictionary<string,string> { ["VolumeTickCompression"] = "TickCompression", ["UseDynamicAggregation"] = "UseDynamicDeltaAggregation",
    ["DynamicAggregationMultiplier"] = "DeltaDynamicMultiplier", ["DeltaWidthPx"] = "DeltaProfileWidthPx" };
foreach (string name in shared)
{
    string cvpName = aliases.TryGetValue(name, out var alias) ? alias : name;
    if (Value(Display(settings[name])!, "Name") != Value(Display(cvp[cvpName])!, "Name")) throw new Exception("Shared CVP label mismatch: " + name);
}
Console.WriteLine("PASS: " + shared.Length + " shared setting labels match CVP.");

string livePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8/bin/Custom/Indicators/OrcaRollingProfiles.cs");
string Authored(string text) => text.Replace("\r\n", "\n").Split("#region NinjaScript generated code")[0].TrimEnd();
string compiledSource = liveGenerated ? File.ReadAllText(livePath) : Authored(source);
if (liveGenerated && Authored(compiledSource) != Authored(source)) throw new Exception("Deployed authored source mismatch.");
var trees = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(compiledSource, new CSharpParseOptions(LanguageVersion.CSharp7_3), "OrcaRollingProfiles.cs") };
if (liveGenerated)
    trees.Add(CSharpSyntaxTree.ParseText(@"
namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns {
 public partial class MarketAnalyzerColumn { private NinjaTrader.NinjaScript.Indicators.Indicator indicator; }
}
namespace NinjaTrader.NinjaScript.Strategies {
 public partial class Strategy { private NinjaTrader.NinjaScript.Indicators.Indicator indicator; }
}", new CSharpParseOptions(LanguageVersion.CSharp7_3), "OfflineGeneratedHostFields.cs"));
string framework = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET/Framework64/v4.0.30319");
string platform = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NinjaTrader 8/bin");
var paths = new List<string>();
foreach (string name in new[] { "mscorlib", "System", "System.Core", "System.Xml", "System.ComponentModel.DataAnnotations", "System.Xaml", "System.Data" }) paths.Add(Path.Combine(framework, name + ".dll"));
foreach (string name in new[] { "WindowsBase", "PresentationCore", "PresentationFramework" }) paths.Add(Path.Combine(framework, "WPF", name + ".dll"));
foreach (string name in new[] { "NinjaTrader.Core", "NinjaTrader.Gui", "SharpDX", "SharpDX.Direct2D1", "SharpDX.DXGI" }) paths.Add(Path.Combine(platform, name + ".dll"));
paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8/bin/Custom/NinjaTrader.Custom.dll"));
var compilation = CSharpCompilation.Create("RollingProfilesOfflineCheck", trees, paths.Select(p => MetadataReference.CreateFromFile(p)), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
foreach (var error in errors) Console.WriteLine(error);
Console.WriteLine($"Offline Rolling Profiles semantic check ({(liveGenerated ? "live including generated wrappers" : "authored source")}): {errors.Length} errors. NinjaTrader F5/runtime remain separate.");
return errors.Length == 0 ? 0 : 1;

sealed class SettingsPresentationStripper : CSharpSyntaxRewriter
{
    public const string Description = "Displays volume and delta across a moving intraday or multi-day trading window. Includes full-session or RTH filtering, point of control, value area, adjustable rows, and customizable colors and delta labels.";
    public override SyntaxNode? VisitAttributeList(AttributeListSyntax node) => node.Attributes.All(a => a.Name.ToString() == "Display") ? null : base.VisitAttributeList(node);
    public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        if (node.Expression is AssignmentExpressionSyntax a && a.Left.ToString() == "Description"
            && a.Right is LiteralExpressionSyntax literal && literal.Token.ValueText == Description) return null;
        return base.VisitExpressionStatement(node);
    }
}
