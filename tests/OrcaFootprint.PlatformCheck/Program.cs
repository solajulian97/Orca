using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

// Offline semantic check against installed platform metadata, not a NinjaScript F5 replacement.
string root = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
string source = Path.Combine(root, "Orca Trades", "Working_Suite", "Indicators");
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
    if (generated >= 0) code = code.Substring(0, generated);
    trees.Add(CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.CSharp7_3), file));
}
var compilation = CSharpCompilation.Create("OrcaFootprintOfflineCheck", trees,
    paths.Select(path => MetadataReference.CreateFromFile(path)),
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, warningLevel: 4));
var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
foreach (Diagnostic error in errors) Console.WriteLine(error);
Console.WriteLine($"Offline platform semantic check: {errors.Length} errors. NinjaTrader F5 and runtime validation still required.");
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
string[] preserved = { "ClassifySignedVolume", "ResolvePrimaryBarIndex", "IsPriceInsidePrimaryBar", "GetTimeDistanceTicks", "NormalizeTradeVolume",
    "ResolveLegacyProfileDisplayMode", "DrawBarBidAskProfile", "RegisterSharedProfileSourceForKey" };
foreach (string name in preserved)
    if (Tokens(Method(oldRoot, name)) != Tokens(Method(newRoot, name))) throw new Exception("Legacy contract changed: " + name);
var render = Method(newRoot, "OnRender");
var earlyBranch = render.DescendantNodes().OfType<IfStatementSyntax>().Single(n => n.Condition.ToString() == "IsEnhancedFootprintActive");
if (Tokens(render.RemoveNode(earlyBranch, SyntaxRemoveOptions.KeepNoTrivia)!) != Tokens(Method(oldRoot, "OnRender")))
    throw new Exception("Legacy render body changed beyond the opt-in early branch.");
foreach (var oldEnum in oldRoot.DescendantNodes().OfType<EnumDeclarationSyntax>())
{
    var newEnum = newRoot.DescendantNodes().OfType<EnumDeclarationSyntax>().Single(e => e.Identifier.Text == oldEnum.Identifier.Text);
    if (Tokens(oldEnum) != Tokens(newEnum)) throw new Exception("Serialized enum changed: " + oldEnum.Identifier);
}
var oldDefaults = Method(oldRoot, "OnStateChange").DescendantNodes().OfType<IfStatementSyntax>().First().Statement;
var newDefaults = Method(newRoot, "OnStateChange").DescendantNodes().OfType<IfStatementSyntax>().First().Statement;
var assignments = newDefaults.DescendantNodes().OfType<AssignmentExpressionSyntax>().ToDictionary(a => a.Left.ToString(), a => Tokens(a.Right));
foreach (var assignment in oldDefaults.DescendantNodes().OfType<AssignmentExpressionSyntax>())
    if (!assignments.TryGetValue(assignment.Left.ToString(), out var actual) || actual != Tokens(assignment.Right))
        throw new Exception("Legacy default changed: " + assignment.Left);
if (assignments["EnhancedFootprint"] != "false") throw new Exception("Enhancement must default off.");
Console.WriteLine("PASS: backup classifier, attribution, migration, enums, defaults, shared publication, and legacy render token parity.");
var enhancedRender = Method(trees[2].GetRoot(), "RenderEnhancedFootprint");
var semantics = compilation.GetSemanticModel(trees[2]);
foreach (var allocation in enhancedRender.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
    if (semantics.GetTypeInfo(allocation).Type?.IsValueType != true) throw new Exception("Managed allocation in enhanced render: " + allocation);
foreach (var call in enhancedRender.DescendantNodes().OfType<InvocationExpressionSyntax>())
    if (call.Expression.ToString().Contains("Capture") || call.Expression.ToString().Contains("Register") || call.Expression.ToString().Contains("Prepare"))
        throw new Exception("Preparation/cache access in enhanced render: " + call);
if (enhancedRender.DescendantNodes().OfType<LockStatementSyntax>().Any()) throw new Exception("Lock in enhanced render.");
Console.WriteLine("PASS: enhanced render has no authored managed object allocations, locks, or preparation/cache calls.");

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
