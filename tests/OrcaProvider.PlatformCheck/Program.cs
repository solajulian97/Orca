using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

string root = Path.GetFullPath(args.FirstOrDefault() ?? Directory.GetCurrentDirectory());
string source = Path.Combine(root, "Orca Trades/Working_Suite/Indicators");
var trees = Directory.GetFiles(source, "OrcaProvider*.cs").Select(path => CSharpSyntaxTree.ParseText(
    File.ReadAllText(path), new CSharpParseOptions(LanguageVersion.CSharp7_3), path)).ToArray();
string framework = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET/Framework64/v4.0.30319");
string platform = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NinjaTrader 8/bin");
var paths = new List<string>();
foreach (string name in new[] { "mscorlib", "System", "System.Core", "System.Xml", "System.ComponentModel.DataAnnotations", "System.Xaml", "System.Data" })
    paths.Add(Path.Combine(framework, name + ".dll"));
foreach (string name in new[] { "WindowsBase", "PresentationCore", "PresentationFramework" })
    paths.Add(Path.Combine(framework, "WPF", name + ".dll"));
foreach (string name in new[] { "NinjaTrader.Core", "NinjaTrader.Gui", "SharpDX", "SharpDX.Direct2D1", "SharpDX.DXGI" })
    paths.Add(Path.Combine(platform, name + ".dll"));
paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8/bin/Custom/NinjaTrader.Custom.dll"));
var compilation = CSharpCompilation.Create("OrcaProviderPlatformCheck", trees,
    paths.Select(p => MetadataReference.CreateFromFile(p)),
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
foreach (var error in errors) Console.WriteLine(error);
Console.WriteLine("Offline provider platform semantic check: " + errors.Length + " errors across " + trees.Length + " sources.");
if (errors.Length != 0) return 1;
var probe = trees.Single(t => Path.GetFileName(t.FilePath) == "OrcaProviderProbe.cs").GetRoot();
var methods = probe.DescendantNodes().OfType<MethodDeclarationSyntax>().ToDictionary(m => m.Identifier.Text);
if (methods.ContainsKey("OnRender")) throw new Exception("Probe must not add rendering work");
if (probe.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i => i.Expression.ToString().Contains("AddDataSeries")))
    throw new Exception("Probe must not add secondary series");
if (methods["OnBarUpdate"].ToString().Contains("OnTrade(")) throw new Exception("Do not ingest from both callbacks");
if (!methods["OnMarketData"].ToString().Contains("MarketDataType.Last")) throw new Exception("Last filter missing");
Console.WriteLine("PASS: no renderer, no added series, no bar-path trade ingestion. NinjaTrader F5/runtime still required.");
return 0;
