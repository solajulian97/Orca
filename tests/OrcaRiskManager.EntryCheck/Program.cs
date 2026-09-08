using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

string root = Path.GetFullPath(args.FirstOrDefault() ?? Directory.GetCurrentDirectory());
string addons = Path.Combine(root, "Orca Trades/Working_Suite/AddOns");
var parse = new CSharpParseOptions(LanguageVersion.CSharp7_3);
var risk = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(addons, "OrcaRiskManagerAddOn.cs")), parse);
var router = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(addons, "OrcaExecutionRouterAddOn.cs")), parse);
string Method(SyntaxTree tree, string name) {
    var matches = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == name).ToArray();
    return (matches.Length == 1 ? matches[0] : matches.Single(m => (m.Parent as ClassDeclarationSyntax)?.Identifier.Text == "OrcaRiskPanel")).ToString();
}
string Type(SyntaxTree tree, string name) => tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(m => m.Identifier.Text == name).ToString();
string framework = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET/Framework64/v4.0.30319");
string platform = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NinjaTrader 8/bin");
var references = new List<MetadataReference>();
foreach (string name in new[] { "mscorlib", "System", "System.Core", "System.Xml", "System.ComponentModel.DataAnnotations", "System.Xaml", "System.Data" })
    references.Add(MetadataReference.CreateFromFile(Path.Combine(framework, name + ".dll")));
foreach (string name in new[] { "WindowsBase", "PresentationCore", "PresentationFramework" })
    references.Add(MetadataReference.CreateFromFile(Path.Combine(framework, "WPF", name + ".dll")));
var platformReferences = new List<MetadataReference>(references);
foreach (string name in new[] { "NinjaTrader.Core", "NinjaTrader.Gui", "SharpDX", "SharpDX.Direct2D1", "SharpDX.DXGI" })
    platformReferences.Add(MetadataReference.CreateFromFile(Path.Combine(platform, name + ".dll")));
platformReferences.Add(MetadataReference.CreateFromFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8/bin/Custom/NinjaTrader.Custom.dll")));
var sizing = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(addons, "OrcaRiskSizing.cs")), parse);
var platformCheck = CSharpCompilation.Create("OrcaRiskPlatformCheck", new[] { risk, router, sizing }, platformReferences,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
var errors = platformCheck.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
foreach (var error in errors) Console.WriteLine(error);
if (errors.Length != 0) return 1;
Console.WriteLine("PASS: complete Risk Manager, Router and sizing helper compile against installed platform references (offline; not NinjaTrader F5).");

if (args.Length > 1)
{
    string baseline = Path.GetFullPath(args[1]);
    foreach (var pair in new[] { ("OrcaRiskManagerAddOn.cs", risk), ("OrcaExecutionRouterAddOn.cs", router) })
    {
        var previous = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(baseline, pair.Item1)), parse);
        string Key(MethodDeclarationSyntax method) => string.Join(".", method.Ancestors().OfType<ClassDeclarationSyntax>().Reverse().Select(c => c.Identifier.Text)) + "." + method.Identifier.Text + method.ParameterList.NormalizeWhitespace().ToFullString();
        var before = previous.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ToDictionary(Key);
        var after = pair.Item2.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ToDictionary(Key);
        var allowed = new HashSet<string> { "AttachChartWindowBinding", "QueueChartWindowRefresh", "GetSelectedChartTab", "GetActiveChartTab", "AttachToTab", "ExecuteTrade", "ExecuteFastCommand", "PlaceDragOrderAt", "SubmitStagedBracket", "SubmitAltSpaceQuickEntryAt" };
        int preserved = 0;
        foreach (var old in before)
        {
            if (!after.TryGetValue(old.Key, out var updated)) throw new Exception("Existing method removed: " + old.Key);
            if (SyntaxFactory.AreEquivalent(old.Value, updated)) preserved++;
            else if (!allowed.Contains(old.Value.Identifier.Text)) throw new Exception("Unrelated method changed: " + old.Key);
        }
        Console.WriteLine("PASS: " + preserved + " existing methods unchanged in " + pair.Item1 + "; modifications limited to declared entry/tab scope.");
    }
}

string fixture = File.ReadAllText(Path.Combine(root, "tests/OrcaRiskManager.EntryCheck/Fixture.cs"));
fixture = fixture.Replace("/* SELECTED_TAB */", Method(risk, "GetSelectedChartTab"))
    .Replace("/* QUEUED_REFRESH */", Method(risk, "QueueChartWindowRefresh"))
    .Replace("/* ENTRY_ROUTER */", Method(router, "TryResolveEntryInstrument") + "\n" + Method(router, "GetRoot"))
    .Replace("/* ENTRY_CONTEXT */", Type(risk, "EntryContext") + "\n" + Method(risk, "TryCaptureEntryContext") + "\n" + Method(risk, "ValidateEntryContext"));
string[] entries = { "ExecuteTrade", "ExecuteFastCommand", "PlaceDragOrderAt", "SubmitStagedBracket", "SubmitAltSpaceQuickEntryAt" };
fixture = fixture.Replace("/* ENTRY_METHODS */", string.Join("\n", entries.Select(n => Method(risk, n))));
foreach (string name in entries)
{
    string method = Method(risk, name);
    if (method.IndexOf("ValidateEntryContext(entry)", StringComparison.Ordinal) > method.IndexOf(".CreateOrder(", StringComparison.Ordinal)
        || !method.Contains("TryCaptureEntryContext(out entry)")) throw new Exception("Entry guard missing: " + name);
}
if (!Method(risk, "AttachToTab").Contains("entryTabBindingVersion++")) throw new Exception("Tab round-trip invalidation missing");
if (!Method(risk, "AttachChartWindowBinding").Contains("ReferenceEquals(e.OriginalSource, chartWindow.MainTabControl)")) throw new Exception("Nested selector filter missing");
// Existing trade management must not depend on the new entry admission guard.
foreach (string name in new[] { "OnExecutionUpdate", "ClosePosition", "Flatten", "MoveToBreakeven", "SubmitRoutedProtectionDrag", "SubmitRoutedOrderChangeDrag" })
    if (Method(risk, name).Contains("EntryContext")) throw new Exception("Existing order management changed: " + name);
Console.WriteLine("PASS: all five real entry methods are linked into the fixture; management paths remain outside entry admission.");

string output = Path.Combine(root, "tests/OrcaRiskManager.EntryCheck/bin/fixture");
Directory.CreateDirectory(output);
string executable = Path.Combine(output, "OrcaRiskEntryFixture.exe");
var compiled = CSharpCompilation.Create("OrcaRiskEntryFixture", new[] { CSharpSyntaxTree.ParseText(fixture, parse) }, references,
    new CSharpCompilationOptions(OutputKind.ConsoleApplication));
var emitted = compiled.Emit(executable);
if (!emitted.Success) { foreach (var diagnostic in emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)) Console.WriteLine(diagnostic); return 1; }
File.WriteAllText(executable + ".config", "<configuration><startup><supportedRuntime version=\"v4.0\" sku=\".NETFramework,Version=v4.8\" /></startup></configuration>");
foreach (string mode in new[] { "legacy", "modern" })
{
    using var process = Process.Start(new ProcessStartInfo(executable, mode) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true });
    Console.Write(process!.StandardOutput.ReadToEnd());
    Console.Write(process.StandardError.ReadToEnd());
    process.WaitForExit();
    if (process.ExitCode != 0) return process.ExitCode;
}
return 0;
