using System;
using System.IO;
using System.Text.Json;
using System.Globalization;
using System.Collections.Generic;
using RoboRougelike.B0132P17.ScanSweep;
int checks=0;
void Check(bool ok,string why){if(!ok)throw new Exception("SCAN_SWEEP_TEST_FAIL "+why);checks++;}
var core=new ScanSweepCore();var off=core.Sample();
Check(off.State==ScanState.Off && !off.Visible && off.Opacity==0f,"idle");
Check(!core.Start(float.NaN),"reject NAN start");
Check(!core.Start(float.PositiveInfinity),"reject INF heading");
Check(core.Start(0f),"start");
Check(core.Sample().Visible && core.Sample().Opacity==1f,"start visual");
core.Advance(-1f);Check(core.Sample().State==ScanState.Sweeping,"reject negative");
core.Advance(float.NaN);Check(core.Sample().State==ScanState.Sweeping,"reject NaN dt");
core.Advance(float.PositiveInfinity);Check(core.Sample().State==ScanState.Sweeping,"reject inf dt");
core.Advance(1.4f);Check(core.State==ScanState.Fading,"fade transition");
core.Advance(.21f);Check(core.Sample().Opacity>0f && core.Sample().Opacity<1f,"mid fade");
core.Advance(.21f);Check(core.State==ScanState.Off,"exact end off");
core.Start(0f);core.SetVisualsEnabled(false);
Check(core.State==ScanState.Off && !core.Start(0f),"off suppresses");
core.SetVisualsEnabled(true);Check(core.Start(6.3f),"on permits new trigger");
core.Cancel();Check(!core.Sample().Visible,"cancel hides");
var v=new ScanVertex[ScanSweepCore.VertexCount];
var triangles=new int[ScanSweepCore.IndexCount];
ScanSweepCore.BuildSector(v,triangles);
Check(v.Length==128 && triangles.Length==192,"actual mesh 128/192");
for(int i=0;i<v.Length;i++){
 var p=v[i];
 float radius=(float)Math.Sqrt(p.X*p.X+p.Z*p.Z);
 Check(ScanSweepCore.Finite(p.X)&&ScanSweepCore.Finite(p.Y)&&ScanSweepCore.Finite(p.Z),"mesh finite"+i);
 Check(radius>=ScanSweepCore.InnerRadius-.0001f && radius<=ScanSweepCore.OuterRadius+.0001f,"mesh annular bounds"+i);
 Check(p.Alpha>0f&&p.Alpha<=1f,"alpha bound"+i);
}
for(int i=0;i<triangles.Length;i++)Check(triangles[i]>=0&&triangles[i]<v.Length,"index bounds"+i);
for(int i=0;i<ScanSweepCore.SegmentCount;i++){
 int j=i*4;
 Check(triangles[i*6]==j && triangles[i*6+5]==j+3,"winding schema"+i);
 float ax=v[j+1].X-v[j].X, az=v[j+1].Z-v[j].Z;
 float bx=v[j+2].X-v[j].X, bz=v[j+2].Z-v[j].Z;
 float area=Math.Abs(ax*bz-az*bx)/2f;
 Check(area>.005f,"nondegenerate original segment"+i);
 if(i>0){
    Check(v[j].X==v[j-1].X && v[j].Z==v[j-1].Z,"inner seam continuous"+i);
    Check(v[j+1].X==v[j-2].X && v[j+1].Z==v[j-2].Z,"outer seam continuous"+i);
 }
}
uint rng=0x12345678u;
for(int i=0;i<2048;i++){
 unchecked{rng=rng*1664525u+1013904223u;}
 float heading=(rng%6283u)/1000f;
 var a=new ScanSweepCore();var b=new ScanSweepCore();
 Check(a.Start(heading)&&b.Start(heading),"seeded start"+i);
 for(int step=0;step<8;step++){
   float dt=((rng>>(step%16))%200u)/1000f;
   a.Advance(dt);b.Advance(dt);
   var x=a.Sample();var y=b.Sample();
   Check(x.State==y.State && x.AngleRadians==y.AngleRadians && x.Opacity==y.Opacity,"determinism"+i+"/"+step);
   Check(x.Opacity>=0f&&x.Opacity<=1f,"opacity bounds"+i+"/"+step);
   Check(!x.Visible || x.AngleRadians>=0f&&x.AngleRadians<=6.283186f,"angle bounds"+i+"/"+step);
 }
 a.SetVisualsEnabled(false);
 Check(a.State==ScanState.Off&&!a.Sample().Visible,"cancel vector"+i);
}
var frames=new List<object>();
void Capture(string name,ScanSweepCore f){var p=f.Sample();frames.Add(new {name,angle=p.AngleRadians,opacity=p.Opacity,visible=p.Visible,state=p.State.ToString()});}
core.Cancel();Capture("off",core);
core.Start(.3f);Capture("start",core);
core.Advance(.35f);Capture("quarter",core);
core.Advance(.35f);Capture("half",core);
core.Advance(.35f);Capture("three_quarters",core);
core.Advance(.35f);Capture("fade_start",core);
core.Advance(.21f);Capture("fade_half",core);
core.Advance(.21f);Capture("fade_end",core);
if(args.Length>=2){
 string target=Path.GetFullPath(args[0]);
 Directory.CreateDirectory(Path.GetDirectoryName(target)!);
 File.WriteAllText(target,JsonSerializer.Serialize(frames,new JsonSerializerOptions{WriteIndented=true}));
 string obj=Path.GetFullPath(args[1]);Directory.CreateDirectory(Path.GetDirectoryName(obj)!);
 using(var writer=new StreamWriter(obj,false,new System.Text.UTF8Encoding(false))){
  writer.NewLine="\n";writer.WriteLine("# ORIGINAL B01-32 compiled C#9 ScanSweepCore sector; 128 vertices 64 triangles");
  foreach(var p in v)
   writer.WriteLine("v "+p.X.ToString("R",CultureInfo.InvariantCulture)+" "+
      p.Y.ToString("R",CultureInfo.InvariantCulture)+" "+p.Z.ToString("R",CultureInfo.InvariantCulture));
  for(int k=0;k<triangles.Length;k+=3)
   writer.WriteLine("f "+(triangles[k]+1)+" "+(triangles[k+1]+1)+" "+(triangles[k+2]+1));
 }
}
Console.WriteLine("ORIGINAL_SCAN_SWEEP_EXEC_PASS "+checks+"/"+checks+" (2048 original deterministic scenarios ×8 increments, mesh triangles, seams, bounds)");
