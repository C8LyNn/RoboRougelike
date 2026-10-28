using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;

string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "B0132P17WheelWake");
string[] files = {
 Path.Combine(root, "Runtime", "WheelWakeProfile.cs"),
 Path.Combine(root, "Runtime", "B0132WheelWakeEmitter.cs"),
 Path.Combine(root, "Runtime", "B0132WheelWakeDemo.cs"),
 Path.Combine(root, "Tests", "PlayMode", "B0132WheelWakePlayModeTests.cs")
};
foreach(string path in files)
{
 if(!File.Exists(path)) throw new FileNotFoundException("Original Unity input missing",path);
 var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(path),
     new CSharpParseOptions(LanguageVersion.CSharp9),path);
 var errors=tree.GetDiagnostics().Where(d=>d.Severity==DiagnosticSeverity.Error).ToList();
 foreach(var e in errors) Console.Error.WriteLine(e.ToString());
 if(errors.Count>0) throw new Exception("Original syntax FAIL "+path);
 Console.WriteLine("REAL_ROSLYN_ORIGINAL_SOURCE_PASS "+Path.GetFileName(path));
}
Console.WriteLine("ROSLYN_SYNTAX_PASS 4/4 original C# sources, Unity APIs NOT compiled");
