using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
string root=Path.Combine(Directory.GetCurrentDirectory(),"Tools","B01-32","P17","SalvageUnityDelivery");
string[] paths={"Runtime/SalvageBeaconClock.cs","Runtime/B0132SalvageBeaconLoop.cs","Tests/PlayMode/SalvageBeaconPlayModeTests.cs"};
foreach(string p in paths)
{
 string path=Path.Combine(root,p.Replace('/',Path.DirectorySeparatorChar));
 var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(path),new CSharpParseOptions(LanguageVersion.CSharp9),path);
 var errors=tree.GetDiagnostics().Where(x=>x.Severity==DiagnosticSeverity.Error).ToArray();
 if(errors.Length>0)throw new Exception("ROSLYN ORIGINAL SOURCE SYNTAX FAIL "+string.Join("; ",errors.Select(x=>x.ToString())));
 Console.WriteLine("SALVAGE_ROSLYN_SOURCE_PASS "+p);
}
Console.WriteLine("SALVAGE_ROSLYN_SYNTAX_PASS 3/3 exact original C# sources, NOT compiled against UnityEngine");
