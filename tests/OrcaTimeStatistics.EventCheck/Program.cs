using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

string root = Directory.GetCurrentDirectory();
string sourcePath = args.FirstOrDefault() ?? Path.Combine(root, "Orca Trades/Working_Suite/Indicators/OrcaTimeStatistics.cs");
var parse = new CSharpParseOptions(LanguageVersion.CSharp7_3);
var source = CSharpSyntaxTree.ParseText(File.ReadAllText(sourcePath).Split("#region NinjaScript generated code")[0], parse);
string Method(string name) => source.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == name).ToString();
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
var compiledSource = CSharpCompilation.Create("OrcaTimeStatisticsPlatformCheck", new[] { source }, platformReferences,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
var errors = compiledSource.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
foreach (var error in errors) Console.WriteLine(error);
if (errors.Length != 0) return 1;
Console.WriteLine("PASS: complete authored Time Statistics compiles against installed platform references (offline, generated wrappers excluded).");

if (args.Length > 1)
{
    string baselineText = File.ReadAllText(args[1]).Split("#region NinjaScript generated code")[0];
    string reverted = source.ToString()
        .Replace("Calculate == Calculate.OnEachTick ? CurrentBar : BarsArray[0].GetBar(e.Time)", "BarsArray[0].GetBar(e.Time)")
        .Replace("primaryIdx >= 0 && primaryIdx < BarsArray[0].Count", "primaryIdx >= 0");
    reverted = System.Text.RegularExpressions.Regex.Replace(reverted,
        @"else if \(State == State.Configure\)\s*\{[^}]*\}\s*", "");
    var baseline = CSharpSyntaxTree.ParseText(baselineText, parse);
    if (!SyntaxFactory.AreEquivalent(baseline.GetRoot(), CSharpSyntaxTree.ParseText(reverted, parse).GetRoot()))
        throw new Exception("Changes extend beyond the declared configuration/bar-assignment correction");
    int preserved = source.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
        .Count(m => m.Identifier.Text != "OnStateChange" && m.Identifier.Text != "OnMarketData");
    Console.WriteLine($"PASS: complete source syntax preserved outside declared edits; {preserved} other methods, settings, reset rules and rendering unchanged.");
}

string fixture = File.ReadAllText(Path.Combine(root, "tests/OrcaTimeStatistics.EventCheck/Fixture.cs"));
fixture = fixture.Replace("/* ACTUAL_METHODS */", string.Join("\n", new[] { "OnMarketData", "EnsureBarLists", "EnsureDeltaStorage", "HasDeltaForBar", "GetFinishDelta" }.Select(Method)));
string output = Path.Combine(root, "tests/OrcaTimeStatistics.EventCheck/bin/fixture");
Directory.CreateDirectory(output);
string executable = Path.Combine(output, "OrcaTimeStatisticsEventFixture.exe");
var compilation = CSharpCompilation.Create("OrcaTimeStatisticsEventFixture", new[] { CSharpSyntaxTree.ParseText(fixture, parse) }, references,
    new CSharpCompilationOptions(OutputKind.ConsoleApplication));
var emitted = compilation.Emit(executable);
if (!emitted.Success) { foreach (var diagnostic in emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)) Console.WriteLine(diagnostic); return 1; }
File.WriteAllText(executable + ".config", "<configuration><startup><supportedRuntime version=\"v4.0\" sku=\".NETFramework,Version=v4.8\" /></startup></configuration>");
using var process = Process.Start(new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true });
Console.Write(process!.StandardOutput.ReadToEnd());
Console.Write(process.StandardError.ReadToEnd());
process.WaitForExit();
return process.ExitCode;
