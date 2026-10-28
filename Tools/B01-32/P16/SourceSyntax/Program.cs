using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

string baseDir = Directory.GetCurrentDirectory();
string root = Path.Combine(baseDir, "Assets", "B0132P16VfxDemo");
string[] inputs =
{
    Path.Combine(root, "Runtime", "B0132Effect.cs"),
    Path.Combine(root, "Runtime", "B0132Showcase.cs"),
    Path.Combine(root, "Tests", "PlayMode", "B0132EffectPlayModeTests.cs")
};
foreach (var path in inputs)
{
    if (!File.Exists(path)) throw new FileNotFoundException("Original Unity C# missing", path);
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path),
        new CSharpParseOptions(LanguageVersion.CSharp9), path);
    var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    foreach (var error in errors) Console.Error.WriteLine(error.ToString());
    if (errors.Count > 0) throw new Exception("Original C# syntax RED: " + path);
    Console.WriteLine("ORIGINAL_ROSLYN_PARSE_PASS " + Path.GetFileName(path));
}
Console.WriteLine("REAL_ROSLYN_SOURCE_SYNTAX_PASS 3/3 C# sources; NOT Unity compile or runtime");
