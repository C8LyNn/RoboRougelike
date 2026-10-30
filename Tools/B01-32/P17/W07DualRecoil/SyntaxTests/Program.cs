using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
string root=Path.Combine(Directory.GetCurrentDirectory(),"Tools","B01-32","P17","W07DualRecoil");
string[] files={"Runtime/DualRecoilCore.cs","Runtime/B0132DualRecoilVisual.cs","Tests/DualRecoilPlayModeTests.cs"};
foreach(var rel in files){
 var path=Path.Combine(root,rel.Replace('/',Path.DirectorySeparatorChar));
 var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(path),new CSharpParseOptions(LanguageVersion.CSharp9),path);
 var errors=tree.GetDiagnostics().Where(x=>x.Severity==DiagnosticSeverity.Error).ToArray();
 if(errors.Length>0)throw new Exception("ORIGINAL_RECOIL_CSHARP_SYNTAX_FAIL "+string.Join("; ",errors.Select(x=>x.ToString())));
 Console.WriteLine("ORIGINAL_RECOIL_SYNTAX_PASS "+rel);
}
Console.WriteLine("ORIGINAL_RECOIL_ROSLYN_PASS 3/3 C#9 source syntax; UnityEngine compilation NOT_RUN");
