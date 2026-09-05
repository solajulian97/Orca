using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
if (args.Length != 2) throw new Exception("Arguments: repository-root pre-edit-source");
string file = Path.Combine(args[0], "Orca Trades/Working_Suite/Indicators/OrcaExecutionLines.cs");
string current = File.ReadAllText(file);
string Authored(string text) { int index=text.IndexOf("#region NinjaScript generated code", StringComparison.Ordinal); return index<0?text:text[..index]; }
var before=CSharpSyntaxTree.ParseText(Authored(File.ReadAllText(args[1]))).GetRoot();
var after=CSharpSyntaxTree.ParseText(Authored(current)).GetRoot();
string Tokens(SyntaxNode node) => string.Join(" ",node.DescendantTokens().Select(t=>t.Text));
string Key(MethodDeclarationSyntax m) => m.Identifier.Text+":"+m.ParameterList.Parameters.Count;
var methods=after.DescendantNodes().OfType<MethodDeclarationSyntax>().ToDictionary(Key);
foreach(var old in before.DescendantNodes().OfType<MethodDeclarationSyntax>()) {
 if(old.Identifier.Text=="ProcessExecution" && old.ParameterList.Parameters.Count==7) {
  var updated=methods["ProcessExecution:8"];
  var added=updated.Body!.DescendantNodes().OfType<StatementSyntax>().Where(n=>
   n is LocalDeclarationStatementSyntax l && l.Declaration.Variables.Any(v=>v.Identifier.Text=="identity") ||
   n is ExpressionStatementSyntax e && e.Expression is AssignmentExpressionSyntax x && x.Left.ToString()=="st.CurrentRT.IdentityJson").ToArray();
  if(added.Length!=2) throw new Exception("Unexpected identity insertion count");
  var stripped=updated.Body.RemoveNodes(added,SyntaxRemoveOptions.KeepNoTrivia)!;
  if(Tokens(old.Body!)!=Tokens(stripped)) throw new Exception("Execution calculations changed beyond identity sidecar");
 } else if(!new[]{"OnExecutionUpdate","LoadTradeNotes","SaveTradeNotes"}.Contains(old.Identifier.Text) && Tokens(old)!=Tokens(methods[Key(old)])) throw new Exception("Unexpected method change: "+Key(old));
}
if(!before.DescendantNodes().OfType<PropertyDeclarationSyntax>().Select(Tokens).SequenceEqual(after.DescendantNodes().OfType<PropertyDeclarationSyntax>().Select(Tokens))) throw new Exception("Settings/properties changed");
Console.WriteLine("PASS: chart calculations, settings, renderers and unrelated methods preserved; two identity statements only in model path.");
int generated = current.IndexOf("#region NinjaScript generated code", StringComparison.Ordinal);
if (generated >= 0) current = current[..generated];
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
var compilation = CSharpCompilation.Create("ExecutionStartupCheck",
    new[] { CSharpSyntaxTree.ParseText(current, new CSharpParseOptions(LanguageVersion.CSharp7_3), file), CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(args[0], "Orca Trades/Working_Suite/AddOns/OrcaTradeIdentity.cs")), new CSharpParseOptions(LanguageVersion.CSharp7_3)) },
    paths.Select(p => MetadataReference.CreateFromFile(p)),
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
foreach (var error in errors) Console.WriteLine(error);
Console.WriteLine("Offline semantic check: " + errors.Length + " errors; NinjaTrader F5/runtime remain separate gates.");
return errors.Length == 0 ? 0 : 1;
