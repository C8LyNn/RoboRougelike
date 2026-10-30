using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
var root=Path.Combine(Directory.GetCurrentDirectory(),"Tools","B01-32","P17","W08ScanSweep");
string[] sources={"Runtime/ScanSweepCore.cs","Runtime/B0132ScanSweepVisual.cs","Tests/ScanSweepPlayModeTests.cs"};
foreach(var rel in sources){
 var file=Path.Combine(root,rel.Replace('/',Path.DirectorySeparatorChar));
 var errors=CSharpSyntaxTree.ParseText(File.ReadAllText(file),new CSharpParseOptions(LanguageVersion.CSharp9),file).GetDiagnostics().Where(d=>d.Severity==DiagnosticSeverity.Error).ToArray();
 if(errors.Length>0)throw new Exception("SCAN_ROSLYN_SYNTAX_RED "+string.Join("; ",errors.Select(d=>d.ToString())));
 Console.WriteLine("SCAN_ROSLYN_ORIGINAL_CSHARP9_PASS "+rel);
}
Console.WriteLine("SCAN_ROSLYN_SYNTAX_PASS 3/3, native Unity compile NOT_RUN");
