using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.IO.Compression;

static class VolumeTests
{
    public static void Run(string source, string root)
    {
        var syntax = CSharpSyntaxTree.ParseText(source).GetRoot();
        var indicator = syntax.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.Text == "OrcaRollingProfiles");
        using var zip = ZipFile.OpenRead(Path.Combine(root, ".codex-backups/rolling-profiles-pre-settings-2026-09-07_184716/RollingProfiles-pre-settings.zip"));
        using var reader = new StreamReader(zip.GetEntry("workspace/Orca Trades/Working_Suite/Indicators/OrcaRollingProfiles.cs")!.Open());
        var baseline = CSharpSyntaxTree.ParseText(reader.ReadToEnd()).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.Text == "OrcaRollingProfiles");
        string Tokens(SyntaxNode n) => string.Join(" ", n.DescendantTokens().Select(t => t.Text));
        foreach (var original in baseline.Members.OfType<PropertyDeclarationSyntax>())
        {
            var current = indicator.Members.OfType<PropertyDeclarationSyntax>().Single(p => p.Identifier.Text == original.Identifier.Text);
            // The intervening settings organization changed only Display attributes.
            string Identity(PropertyDeclarationSyntax p) => Tokens(p.WithAttributeLists(new SyntaxList<AttributeListSyntax>(p.AttributeLists.Where(a => !a.Attributes.All(x => x.Name.ToString() == "Display")))));
            if (Identity(original) != Identity(current)) throw new Exception("Existing property changed: " + original.Identifier.Text);
        }
        foreach (var original in baseline.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == "OnRender" || m.Identifier.Text == "GetRollingWindowStartTimeUnsafe" || m.Identifier.Text == "GetPeriodMinutes"))
            if (Tokens(original) != Tokens(indicator.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == original.Identifier.Text))) throw new Exception("Preserved method changed: " + original.Identifier.Text);
        Console.WriteLine("PASS: existing property identities, renderer, time-window calculation and period mapping preserved.");
        string[] names = { "ClearProfileDataUnsafe", "AddTradeToRollingProfilesUnsafe", "PruneVolumeWindowUnsafe", "AddActiveTickUnsafe", "SubtractFromMap", "NormalizeToBucketStart", "RebuildTotalProfileFromActiveTicksUnsafe", "PruneActiveTicksUnsafe", "SubtractTickFromTotalUnsafe" };
        string methods = string.Join("\n", indicator.Members.OfType<MethodDeclarationSyntax>().Where(m => names.Contains(m.Identifier.Text)));
        string models = string.Join("\n", syntax.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => c.Identifier.Text == "OrcaProfileBucket" || c.Identifier.Text == "OrcaRollingProfileTick"));
        string harness = File.ReadAllText(Path.Combine(root, "tests/OrcaRollingVolume.Tests/Harness.txt"));
        var tree = CSharpSyntaxTree.ParseText(harness.Replace("// MODELS", models).Replace("// METHODS", methods));
        var refs = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p));
        var compilation = CSharpCompilation.Create("RollingVolumeTests", new[] { tree }, refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success) throw new Exception(string.Join("\n", result.Diagnostics));
        Assembly.Load(stream.ToArray()).GetType("Harness")!.GetMethod("Run")!.Invoke(null, null);
        Console.WriteLine("PASS: production rolling-volume methods, deterministic cases and 20,000 randomized chronological/out-of-order trades checked against independent contract-level reference.");
    }
}
