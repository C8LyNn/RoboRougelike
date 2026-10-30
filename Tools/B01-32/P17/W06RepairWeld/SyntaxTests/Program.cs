using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
string root=Path.Combine(Directory.GetCurrentDirectory(),"Tools","B01-32","P17","W06RepairWeld");
string[] paths={"Runtime/WeldVisualCore.cs","Runtime/B0132RepairWeldVfx.cs","Tests/RepairWeldPlayModeTests.cs"};
foreach (string relative in paths) {
 var path=Path.Combine(root,relative.Replace('/',Path.DirectorySeparatorChar));
 var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(path),new CSharpParseOptions(LanguageVersion.CSharp9),path);
 var errors=tree.GetDiagnostics().Where(d=>d.Severity==DiagnosticSeverity.Error).ToArray();
 if(errors.Length>0)throw new Exception("REPAIR_WELD_CSHARP9_SYNTAX_FAIL "+string.Join(";",errors.Select(x=>x.ToString())));
 Console.WriteLine("REPAIR_WELD_CSHARP9_SOURCE_PASS "+relative);
}
Console.WriteLine("REPAIR_WELD_CSHARP9_SYNTAX_PASS 3/3; this is NOT UnityEngine compile");
