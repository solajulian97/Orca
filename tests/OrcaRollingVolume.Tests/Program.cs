using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
string root = Directory.GetCurrentDirectory();
bool liveGenerated = args.Contains("--live-generated");
string source = File.ReadAllText(Path.Combine(root, "Orca Trades/Working_Suite/Indicators/OrcaRollingProfiles.cs"));
VolumeTests.Run(source, root);
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
