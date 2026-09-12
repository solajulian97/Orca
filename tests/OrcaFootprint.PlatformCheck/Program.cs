using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

// Offline semantic check against installed platform metadata, not a NinjaScript F5 replacement.
string root = Path.GetFullPath(args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal)) ?? Directory.GetCurrentDirectory());
bool liveGenerated = args.Contains("--live-generated", StringComparer.Ordinal);
string source = liveGenerated
    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8", "bin", "Custom", "Indicators")
    : Path.Combine(root, "Orca Trades", "Working_Suite", "Indicators");
string framework = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET", "Framework64", "v4.0.30319");
string platform = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NinjaTrader 8", "bin");
string custom = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8", "bin", "Custom", "NinjaTrader.Custom.dll");
var paths = new List<string>();
foreach (string name in new[] { "mscorlib.dll", "System.dll", "System.Core.dll", "System.Xml.dll", "System.ComponentModel.DataAnnotations.dll", "System.Xaml.dll" })
    paths.Add(Path.Combine(framework, name));
foreach (string name in new[] { "WindowsBase.dll", "PresentationCore.dll", "PresentationFramework.dll" })
    paths.Add(Path.Combine(framework, "WPF", name));
foreach (string name in new[] { "NinjaTrader.Core.dll", "NinjaTrader.Gui.dll", "SharpDX.dll", "SharpDX.Direct2D1.dll", "SharpDX.DXGI.dll" })
    paths.Add(Path.Combine(platform, name));
paths.Add(custom);
var trees = new List<SyntaxTree>();
foreach (string name in new[] { "OrcaFootprintCore.cs", "OrcaCandleVolumeProfile.cs", "OrcaCandleVolumeProfile.Rendering.cs" })
{
    string file = Path.Combine(source, name), code = File.ReadAllText(file);
    int generated = code.IndexOf("#region NinjaScript generated code", StringComparison.Ordinal);
    if (generated >= 0 && !liveGenerated) code = code.Substring(0, generated);
    trees.Add(CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.CSharp7_3), file));
}
if (liveGenerated)
{
    // These private fields normally come from other NinjaTrader-generated partials.
    // Supply only that host scaffolding; compile the CVP wrappers exactly as generated.
    const string hostPartials = @"
namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns {
    public partial class MarketAnalyzerColumn {
        private NinjaTrader.NinjaScript.Indicators.Indicator indicator;
    }
}
namespace NinjaTrader.NinjaScript.Strategies {
    public partial class Strategy {
        private NinjaTrader.NinjaScript.Indicators.Indicator indicator;
    }
}";
    trees.Add(CSharpSyntaxTree.ParseText(hostPartials, new CSharpParseOptions(LanguageVersion.CSharp7_3), "GeneratedHostScaffolding.cs"));
}
var compilation = CSharpCompilation.Create("OrcaFootprintOfflineCheck", trees,
    paths.Select(path => MetadataReference.CreateFromFile(path)),
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, warningLevel: 4));
var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
foreach (Diagnostic error in errors) Console.WriteLine(error);
Console.WriteLine($"Offline platform semantic check ({(liveGenerated ? "live including generated code" : "authored source")}): {errors.Length} errors. NinjaTrader F5 and runtime validation still required.");
if (errors.Length != 0) return 1;

var git = new ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
git.ArgumentList.Add("show");
git.ArgumentList.Add("34d7faf:Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs");
using var process = Process.Start(git)!;
string baseline = process.StandardOutput.ReadToEnd(); process.WaitForExit();
if (process.ExitCode != 0) throw new Exception("Cannot verify backup baseline 34d7faf.");
var oldRoot = CSharpSyntaxTree.ParseText(baseline).GetRoot();
var newRoot = trees[1].GetRoot();
string Tokens(SyntaxNode node) => string.Join(" ", node.DescendantTokens().Select(token => token.Text));
MethodDeclarationSyntax Method(SyntaxNode node, string name) => node.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == name);
bool HasStandaloneIndicatorConverter(SyntaxNode node)
{
    var classes = node.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray();
    bool HasBase(ClassDeclarationSyntax c, string name) => c.BaseList?.Types.Any(b => b.Type.ToString().Split('.').Last() == name) == true;
    return classes.Any(c => HasBase(c, "IndicatorBaseConverter")) && !classes.Any(c => HasBase(c, "Indicator"));
}
foreach (var tree in trees)
    if (HasStandaloneIndicatorConverter(tree.GetRoot()))
        throw new Exception("NinjaScript generation risk: IndicatorBaseConverter without the concrete Indicator declaration in " + tree.FilePath);
Console.WriteLine("PASS: IndicatorBaseConverter stays with the concrete indicator, not a helper partial.");
string[] preserved = { "ClassifySignedVolume", "ResolvePrimaryBarIndex", "IsPriceInsidePrimaryBar", "GetTimeDistanceTicks", "NormalizeTradeVolume",
    "ResolveLegacyProfileDisplayMode", "RegisterSharedProfileSourceForKey" };
foreach (string name in preserved)
    if (Tokens(Method(oldRoot, name)) != Tokens(Method(newRoot, name))) throw new Exception("Legacy contract changed: " + name);
var render = Method(newRoot, "OnRender");
var earlyBranch = render.DescendantNodes().OfType<IfStatementSyntax>().Single(n => n.Condition.ToString() == "IsEnhancedFootprintActive");
var oldVolumeDelta = Method(oldRoot, "OnRender").DescendantNodes().OfType<IfStatementSyntax>()
    .Single(n => n.Condition.ToString() == "showVolumeProfile || showDeltaProfile");
var newVolumeDelta = render.DescendantNodes().OfType<IfStatementSyntax>()
    .Single(n => n.Condition.ToString() == "showVolumeProfile || showDeltaProfile");
if (Tokens(oldVolumeDelta) != Tokens(newVolumeDelta))
    throw new Exception("Volume/Delta render branch changed during Bid x Ask work.");
if (earlyBranch == null) throw new Exception("Enhanced render branch missing.");
var bidAskProfile = Method(newRoot, "DrawBarBidAskProfile");
if (!Tokens(bidAskProfile).Contains("centerGap") || !Tokens(render).Contains("FootprintScaffold"))
    throw new Exception("Bid x Ask candle reservation is missing.");
var coreRoot = trees[0].GetRoot();
var scaffoldEnum = coreRoot.DescendantNodes().OfType<EnumDeclarationSyntax>()
    .Single(e => e.Identifier.Text == "FootprintScaffoldMode");
var scaffoldMembers = scaffoldEnum.Members.Select(m => m.Identifier.Text).ToArray();
if (!scaffoldMembers.SequenceEqual(new[] { "Off", "OhlcSpine", "OhlcSpineAndBody", "HollowBodyDelta" }))
    throw new Exception("Candle Display enum values were reordered or Hollow Body Delta is missing.");
if (!Tokens(render).Contains("if ( ! hollowBodyDelta )")
    || !Tokens(render).Contains("DrawBarBidAskProfile ( chartScale , barIdx , barCenterX , panelTop , panelBottom , bidAskWidth , deltaCompressionTicks , centerGap , bodyDeltaWidth , o , c )"))
    throw new Exception("Normal Bid x Ask hollow body must suppress the filled candle and pass OHLC body bounds to the center column.");
if (!Tokens(bidAskProfile).Contains("FootprintFormatting . IntersectsBody")
	|| !Tokens(bidAskProfile).Contains("DrawHollowBodyDeltaRow"))
	throw new Exception("Normal Bid x Ask body-row selection or full-height center delta drawing is missing.");
var hollowBodyRow = Method(newRoot, "DrawHollowBodyDeltaRow");
var bodyDeltaWidthMethod = Method(newRoot, "ResolveBodyDeltaColumnWidth");
if (!Tokens(hollowBodyRow).Contains("FootprintFormatting . SignedNumber")
	|| !Tokens(hollowBodyRow).Contains("SelectBidAskBrush")
	|| !Tokens(hollowBodyRow).Contains("if ( bodyRow ) RenderTarget . DrawRectangle")
	|| !Tokens(hollowBodyRow).Contains("RenderTarget . DrawLine")
	|| Tokens(hollowBodyRow).Contains("FillRectangle")
	|| Tokens(hollowBodyRow).Contains("BidAskTextMinThreshold"))
	throw new Exception("Normal center delta must draw every row, box body rows, separate wick rows, and never fill the center.");
if (!Tokens(bodyDeltaWidthMethod).Contains("Math . Max ( 24f , BidAskTextFontSize * 3f + 4f )"))
	throw new Exception("Hollow delta automatic width no longer targets a narrow signed four-digit value.");
foreach (var oldEnum in oldRoot.DescendantNodes().OfType<EnumDeclarationSyntax>())
{
    var newEnum = newRoot.DescendantNodes().OfType<EnumDeclarationSyntax>().Single(e => e.Identifier.Text == oldEnum.Identifier.Text);
    if (Tokens(oldEnum) != Tokens(newEnum)) throw new Exception("Serialized enum changed: " + oldEnum.Identifier);
}
var oldDefaults = Method(oldRoot, "OnStateChange").DescendantNodes().OfType<IfStatementSyntax>().First().Statement;
var newDefaults = Method(newRoot, "OnStateChange").DescendantNodes().OfType<IfStatementSyntax>().First().Statement;
var assignments = newDefaults.DescendantNodes().OfType<AssignmentExpressionSyntax>().ToDictionary(a => a.Left.ToString(), a => Tokens(a.Right));
foreach (var assignment in oldDefaults.DescendantNodes().OfType<AssignmentExpressionSyntax>())
    if (assignment.Left.ToString() != "Description"
        && (!assignments.TryGetValue(assignment.Left.ToString(), out var actual) || actual != Tokens(assignment.Right)))
        throw new Exception("Legacy default changed: " + assignment.Left);
if (assignments["Description"] != "\"Displays volume and delta at each candle's price levels, with Volume, Delta, combined, and Bid x Ask footprint views. Includes point of control, value area, adjustable row sizing, and customizable colors and text.\"")
    throw new Exception("CVP settings description changed unexpectedly.");
if (assignments["EnhancedFootprint"] != "false") throw new Exception("Enhancement must default off.");
if (assignments["FootprintShowHealth"] != "false") throw new Exception("Technical status must default hidden.");
if (assignments["FootprintEmphasizeWinner"] != "true" || assignments["FootprintWinnerRatio"] != "1.5")
    throw new Exception("Winner emphasis defaults changed.");
if (assignments["ColorVolumeTextByDelta"] != "false" || assignments["ScaleVolumeTextColorByDeltaPercent"] != "true"
    || assignments["VolumeTextDeltaMinAbsolute"] != "150" || assignments["VolumeTextDeltaMinPercent"] != "15.0"
    || assignments["BoldQualifiedVolumeText"] != "true")
    throw new Exception("Volume row delta-emphasis compatibility defaults changed.");
if (assignments["ScaleVolumeTextBrightnessByVolume"] != "false" || assignments["VolumeTextMinBrightness"] != "0.35f")
    throw new Exception("Volume + Delta text-brightness defaults changed.");
foreach (string name in new[] { "ColorVolumeTextByDelta", "ScaleVolumeTextColorByDeltaPercent", "VolumeTextDeltaMinAbsolute", "VolumeTextDeltaMinPercent", "BoldQualifiedVolumeText", "ScaleVolumeTextBrightnessByVolume", "VolumeTextMinBrightness" })
{
    var property = newRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>().Single(p => p.Identifier.Text == name);
    if (property.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString().Contains("NinjaScriptProperty")))
        throw new Exception("Volume text presentation setting must not alter generated factory signatures: " + name);
}
var volumeProfile = Method(newRoot, "DrawBarVolumeProfile");
var volumeTextBrush = Method(newRoot, "SelectVolumeTextBrush");
var rowEmphasisActive = newRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>()
    .Single(p => p.Identifier.Text == "IsDeltaColoredVolumeTextActive");
if (!Tokens(rowEmphasisActive).Contains("ProfileDisplayMode == CandleProfileDisplayMode . Volume")
    || !Tokens(rowEmphasisActive).Contains("ColorVolumeTextByDelta")
    || Tokens(rowEmphasisActive).Contains("ShowVolumeText"))
    throw new Exception("Volume row emphasis must stay Volume-only without depending on volume-text visibility.");
if (!Tokens(volumeProfile).Contains("ShowDelta || emphasizeVolumeRowsByDelta")
    || !Tokens(volumeProfile).Contains("FootprintFormatting . IsDeltaEmphasis ( rowDelta , vol , VolumeTextDeltaMinAbsolute , VolumeTextDeltaMinPercent )")
    || !Tokens(volumeProfile).Contains("brush = rowDelta > 0 ? posDeltaBrushDx : negDeltaBrushDx")
    || !Tokens(volumeProfile).Contains("DrawVolumeTextLabel ( vol , maxVol , profileRootX"))
    throw new Exception("Volume-only row emphasis lost matching aggregated delta qualification or row-color selection.");
if (!Tokens(volumeTextBrush).Contains("FootprintFormatting . VolumeTextIntensity")
    || !Tokens(volumeTextBrush).Contains("volumeTextIntensityBrushes")
    || !Tokens(volumeTextBrush).Contains("volumeTextBrushDx")
    || Tokens(volumeTextBrush).Contains("DeltaBrush"))
    throw new Exception("Volume text must remain in its selected color while preserving combined-mode brightness scaling.");
if (Tokens(Method(newRoot, "DrawVolumeTextLabel")).Contains("BoldQualifiedVolumeText"))
    throw new Exception("Qualified volume text must remain at its selected normal formatting.");
var rowEmphasisProperty = newRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>()
    .Single(p => p.Identifier.Text == "ColorVolumeTextByDelta");
if (!rowEmphasisProperty.AttributeLists.ToFullString().Contains("Emphasize Volume Rows by Delta"))
    throw new Exception("Volume row emphasis setting label is missing.");
var legacyBoldProperty = newRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>()
    .Single(p => p.Identifier.Text == "BoldQualifiedVolumeText");
if (!legacyBoldProperty.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString().Contains("Browsable") && a.ArgumentList?.ToString() == "(false)"))
    throw new Exception("Legacy qualified-text weight setting must stay serialized but hidden.");
if (!Tokens(Method(newRoot, "EnsureBarMaps")).Contains("ShouldCollectStrictBidAskEvidence")
    || !Tokens(Method(newRoot, "ProcessTradeIntoPrimaryBar")).Contains("ShouldCollectStrictBidAskEvidence"))
    throw new Exception("Qualified Volume-mode hover no longer retains strict Bid/Ask evidence.");
var qualifiedEvidence = Method(newRoot, "TryGetQualifiedVolumeTextEvidence");
if (!Tokens(qualifiedEvidence).Contains("FootprintFormatting . IsDeltaEmphasis")
    || !Tokens(qualifiedEvidence).Contains("barBidVolumeMaps")
    || !Tokens(qualifiedEvidence).Contains("barAskVolumeMaps")
    || !Tokens(qualifiedEvidence).Contains("barUnclassifiedVolumeMaps"))
    throw new Exception("Qualified Volume-mode hover evidence lost its threshold or strict-side contract.");
var renderingRoot = trees[2].GetRoot();
var volumeHover = Method(renderingRoot, "TryShowQualifiedVolumeTextTooltip");
if (!Tokens(volumeHover).Contains("{0:N0} x {1:N0}\\n{2:+#,0;-#,0;0}")
    || !Tokens(volumeHover).Contains("N/A x N/A\\nN/A")
    || Tokens(volumeHover).Contains("Unclassified"))
    throw new Exception("Qualified Volume-row hover lost its compact two-line value contract.");
var settingsConverter = Method(newRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
    .Single(c => c.Identifier.Text == "OrcaFootprintSettingsConverter"), "GetProperties");
if (!Tokens(settingsConverter).Contains("indicator . ProfileDisplayMode != CandleProfileDisplayMode . Volume")
    || !Tokens(settingsConverter).Contains("! indicator . ColorVolumeTextByDelta"))
    throw new Exception("Volume row delta-emphasis settings are not contextual to Volume mode and the main toggle.");
if (!Tokens(settingsConverter).Contains("indicator . ProfileDisplayMode != CandleProfileDisplayMode . VolumeAndDelta")
    || !Tokens(settingsConverter).Contains("! indicator . ScaleVolumeTextBrightnessByVolume"))
    throw new Exception("Volume text brightness settings are not contextual to Volume + Delta mode and the main toggle.");
foreach (string name in new[] { "DrawBidAskClusterText", "DrawBidAskHistogramText" })
    if (!Tokens(Method(newRoot, name)).Contains("FootprintFormatting . IsWinner"))
        throw new Exception("Normal Bid x Ask winner emphasis missing: " + name);
Console.WriteLine("PASS: backup classifier, attribution, migration, enums, defaults, shared publication, and legacy render token parity.");
var enhancedRender = Method(trees[2].GetRoot(), "RenderEnhancedFootprint");
var enhancedPrepare = Method(trees[2].GetRoot(), "PrepareFootprintFrame");
var centerGutter = Method(trees[2].GetRoot(), "ResolveFootprintCenterGutter");
if (!Tokens(enhancedPrepare).Contains("BodyDeltaRow")
    || !Tokens(enhancedPrepare).Contains("FootprintFormatting . IntersectsBody")
    || !Tokens(enhancedPrepare).Contains("FootprintFormatting . SignedNumber")
    || !Tokens(centerGutter).Contains("FootprintScaffoldMode . HollowBodyDelta"))
    throw new Exception("Enhanced hollow body delta preparation or automatic center reservation is missing.");
var bodyDeltaLayout = enhancedPrepare.DescendantNodes().OfType<AssignmentExpressionSyntax>()
	.Single(a => a.Left.ToString() == "paint.CenterDelta");
if (bodyDeltaLayout.Ancestors().OfType<IfStatementSyntax>().Any(i => i.Condition.ToString().Contains("ShowBidAskText"))
	|| bodyDeltaLayout.Ancestors().OfType<IfStatementSyntax>().Any(i => i.Condition.ToString().Contains("BodyDeltaRow") || i.Condition.ToString().Contains("BidAskTextMinThreshold"))
	|| Tokens(hollowBodyRow).Contains("ShowBidAskText"))
	throw new Exception("Hollow body delta must remain visible when side Bid x Ask text is hidden.");
if (!Tokens(enhancedRender).Contains("FootprintFormatting . Magnitude ( row . Delta )")
	|| !Tokens(enhancedRender).Contains("RenderTarget . DrawRectangle")
	|| !Tokens(enhancedRender).Contains("RenderTarget . DrawLine")
	|| !Tokens(enhancedRender).Contains("&& ! hollowBodyDelta"))
	throw new Exception("Enhanced hollow body delta opacity, body outlines, wick separators, or wick suppression is missing.");
var scaffoldConverter = trees[2].GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
    .Single(c => c.Identifier.Text == "FootprintScaffoldModeConverter");
if (!Tokens(scaffoldConverter).Contains("\"Hollow Body Delta\""))
    throw new Exception("Hollow Body Delta is missing from the Candle Display dropdown.");
var semantics = compilation.GetSemanticModel(trees[2]);
foreach (var allocation in enhancedRender.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
    if (semantics.GetTypeInfo(allocation).Type?.IsValueType != true) throw new Exception("Managed allocation in enhanced render: " + allocation);
foreach (var call in enhancedRender.DescendantNodes().OfType<InvocationExpressionSyntax>())
    if (call.Expression.ToString().Contains("Capture") || call.Expression.ToString().Contains("Register") || call.Expression.ToString().Contains("Prepare"))
        throw new Exception("Preparation/cache access in enhanced render: " + call);
if (enhancedRender.DescendantNodes().OfType<LockStatementSyntax>().Any()) throw new Exception("Lock in enhanced render.");
Console.WriteLine("PASS: enhanced render has no authored managed object allocations, locks, or preparation/cache calls.");

// Match the invalid helper-as-indicator wrapper observed in the user's F5 output.
string badWrapper = File.ReadAllText(Path.Combine(root, "tests", "OrcaFootprint.PlatformCheck", "InvalidConverterWrapper.txt"));
var regression = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(badWrapper, new CSharpParseOptions(LanguageVersion.CSharp7_3)));
var regressionCodes = regression.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Id).ToHashSet();
if (!regressionCodes.Contains("CS1061") || !regressionCodes.Contains("CS0311"))
    throw new Exception("Generated-converter regression failed to reproduce the reported errors.");
Console.WriteLine("PASS: invalid generated converter wrapper reproduces CS1061 and CS0311.");

string fixture = File.ReadAllText(Path.Combine(root, "tests", "OrcaFootprint.PlatformCheck", "BaselineFixture.txt"));
fixture = fixture.Replace("/* BASELINE_METHODS */", string.Join(Environment.NewLine,
    new[] { "ClassifySignedVolume", "ResolvePrimaryBarIndex", "IsPriceInsidePrimaryBar", "GetTimeDistanceTicks" }.Select(name => Method(newRoot, name).ToFullString())));
var frameworkRefs = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p));
var fixtureCompilation = CSharpCompilation.Create("FootprintBaselineFixture", new[] { CSharpSyntaxTree.ParseText(fixture) }, frameworkRefs,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
using var output = new MemoryStream();
var emit = fixtureCompilation.Emit(output);
if (!emit.Success) throw new Exception(string.Join(Environment.NewLine, emit.Diagnostics));
var assembly = System.Reflection.Assembly.Load(output.ToArray());
Console.WriteLine("PASS: " + assembly.GetType("BaselineFixture")!.GetMethod("Run")!.Invoke(null, null) + " extracted baseline behavior fixtures.");
return 0;
