using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RoboRougelike.B0132P17.RepairWeld;
int checks=0;
void Check(bool ok,string why){if(!ok)throw new Exception("WELD_CORE_ASSERT_FAIL "+why);checks++;}
var core=new WeldVisualCore(); var frame=new WeldFrame();
core.Sample(frame);Check(!frame.Visible,"idle invisible");
Check(core.Phase==WeldPhase.Idle,"start idle");
core.SetProgress(0.8f);Check(core.Progress==0f,"idle rejects progress");
core.Begin(1337u);core.Sample(frame);Check(frame.Visible,"begin shows");
Check(core.Phase==WeldPhase.Welding,"welding phase");
core.SetProgress(.6f);Check(core.Progress==.6f,"progress 60");
core.SetProgress(.2f);Check(core.Progress==.6f,"monotonic");
core.SetProgress(float.NaN);Check(core.Progress==.6f,"NaN ignored");
core.SetProgress(float.PositiveInfinity);Check(core.Progress==.6f,"infinity ignored");
core.SetProgress(-300f);Check(core.Progress==.6f,"negative bounded");
core.SetProgress(900f);Check(core.Progress==1f,"huge progress clamp");
core.Tick(-1f);Check(core.TimeSeconds==0f,"negative time ignored");
core.Tick(float.NaN);Check(core.TimeSeconds==0f,"NaN time ignored");
core.Tick(float.PositiveInfinity);Check(core.TimeSeconds==0f,"inf time ignored");
core.Tick(.15f);core.Sample(frame);
Check(frame.Sparks.Length==8,"8 actual spark segments");
Check(frame.Heat==1f,"welding heat");
foreach(var s in frame.Sparks){Check(WeldVisualCore.Finite(s.X1)&&WeldVisualCore.Finite(s.Y1)&&s.Intensity>=0f&&s.Intensity<=1f,"finite sample");}
core.Finish();Check(core.Phase==WeldPhase.Cooling,"cooling started");
core.SetProgress(0f);Check(core.Progress==1f,"cooling progress untouched");
core.Tick(.2f);core.Sample(frame);Check(frame.Visible&&frame.Heat<1f&&frame.Heat>0f,"cools over time");
core.Tick(.6f);core.Sample(frame);Check(!frame.Visible&&core.Phase==WeldPhase.Idle,"finished clears");
core.Begin(4u);core.Tick(.1f);core.Cancel();core.Sample(frame);Check(!frame.Visible&&core.Progress==0f,"cancel clears");
core.Finish();Check(core.Phase==WeldPhase.Idle,"cannot finish idle");
uint rng=0xCAFE1234u;
for(int i=0;i<2048;i++)
{
    unchecked{rng=rng*1664525u+1013904223u;}
    var a=new WeldVisualCore();var b=new WeldVisualCore();
    a.Begin(rng);b.Begin(rng);
    float progress=(rng%117)/116f;
    a.SetProgress(progress);b.SetProgress(progress);
    for(int step=0;step<4;step++)
    {
        float delta=(step+1)*.014f;
        a.Tick(delta);b.Tick(delta);
        var fa=new WeldFrame();var fb=new WeldFrame();
        a.Sample(fa);b.Sample(fb);
        Check(fa.Visible==fb.Visible && fa.Progress==fb.Progress && fa.ArcX==fb.ArcX,"deterministic core "+i+" step "+step);
        for(int k=0;k<8;k++)
            Check(fa.Sparks[k].X1==fb.Sparks[k].X1&&fa.Sparks[k].Y1==fb.Sparks[k].Y1&&fa.Sparks[k].Intensity==fb.Sparks[k].Intensity,"deterministic spark "+i+"/"+step+"/"+k);
    }
    a.Cancel();a.Sample(frame);Check(!frame.Visible,"cancel no stale "+i);
}
var captures=new List<object>();
void Capture(string name,WeldVisualCore w)
{
    var f=new WeldFrame();w.Sample(f);
    captures.Add(new {name,visible=f.Visible,phase=w.Phase.ToString(),progress=f.Progress,
        heat=f.Heat,arcX=f.ArcX,arcY=f.ArcY,sparks=f.Sparks});
}
core.Cancel();Capture("idle",core);
core.Begin(123456u);core.SetProgress(.08f);core.Tick(.06f);Capture("weld_early",core);
core.SetProgress(.38f);core.Tick(.17f);Capture("weld_mid",core);
core.SetProgress(.83f);core.Tick(.24f);Capture("weld_late",core);
core.Finish();Capture("cool_start",core);
core.Tick(.30f);Capture("cool_mid",core);
core.Tick(.36f);Capture("cool_end",core);
if(args.Length>0){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);File.WriteAllText(args[0],JsonSerializer.Serialize(captures,new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));}
Console.WriteLine("REPAIR_WELD_ORIGINAL_CSHARP_PASS "+checks+"/"+checks+" 2048 seeded original core vectors x4 steps x8 sparks");
