using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using RoboRougelike.B0132P17.DualRecoil;
int checks = 0;
void Check(bool condition, string detail) {
    if (!condition) throw new Exception("RECOIL_ORIGINAL_FAIL " + detail);
    checks++;
}
void Near(float expected, float actual, float tol, string why) {
    Check(Math.Abs(expected - actual) <= tol, why + ":" + actual);
}
var motion = new DualRecoilCore();
var zero = motion.Sample();
Check(zero.LeftTravel == 0 && zero.RightTravel == 0, "initial rest");
Check(!motion.Fire((RecoilLane)(-1)), "invalid negative lane");
Check(!motion.Fire((RecoilLane)8), "invalid positive lane");
Check(!motion.Fire(RecoilLane.Left, float.NaN), "reject NaN shot");
Check(!motion.Fire(RecoilLane.Right, float.PositiveInfinity), "reject infinite shot");
Check(!motion.Fire(RecoilLane.Left, -1f), "reject negative shot");
Check(motion.Fire(RecoilLane.Left), "left pulse");
Check(motion.Sample().LeftTravel == 0f, "kick has continuous position at event");
motion.Advance(.016f);
Check(motion.Sample().LeftTravel > 0f, "left moves after pulse");
Check(motion.Sample().RightTravel == 0f, "right untouched");
Check(motion.Sample().LeftTravel <= DualRecoilCore.MaxTravel, "left cap");
var before = motion.Sample();
motion.Advance(float.NaN);
Near(before.LeftTravel, motion.Sample().LeftTravel, 0f, "NaN time ignored");
motion.Advance(-10f);
Near(before.LeftTravel, motion.Sample().LeftTravel, 0f, "negative time ignored");
motion.Advance(float.PositiveInfinity);
Near(before.LeftTravel, motion.Sample().LeftTravel, 0f, "infinity time ignored");
motion.Advance(5f);
Near(0f, motion.Sample().LeftTravel, 0.00001f, "fully settled");
Check(motion.Fire(RecoilLane.Right,.75f), "right shot");
motion.Advance(.01f);
Check(motion.Sample().RightTravel > 0f, "right moves");
Check(motion.Sample().LeftTravel == 0f, "left still rest");
motion.SetVisualsEnabled(false);
Check(!motion.Fire(RecoilLane.Left), "disable rejects new shot");
Check(motion.Sample().LeftTravel == 0f && motion.Sample().RightTravel == 0f, "off clears both");
motion.SetVisualsEnabled(true);
Check(motion.Sample().LeftTravel == 0f && motion.Sample().RightTravel == 0f, "on does not restore old kick");
for(int i=0;i<300;i++)motion.Fire(RecoilLane.Left);
motion.Advance(.02f);
Check(motion.Sample().LeftTravel <= DualRecoilCore.MaxTravel + 0.0001f, "spam clamps excursion");
motion.Clear();
Check(motion.Sample().LeftTravel == 0f, "clear resets");
uint seed=0xC0DEC0DEu;
for(int i=0;i<2048;i++){
    unchecked{seed=seed*1664525u+1013904223u;}
    float intensity=0.1f+(seed%91u)/100f;
    var a=new DualRecoilCore();
    var b=new DualRecoilCore();
    if((seed&1u)==1u){a.Fire(RecoilLane.Right,intensity);b.Fire(RecoilLane.Right,intensity);}
    else {a.Fire(RecoilLane.Left,intensity);b.Fire(RecoilLane.Left,intensity);}
    for(int j=0;j<6;j++){
        float step=(j+1)*.007f;
        a.Advance(step);b.Advance(step);
        var x=a.Sample();var y=b.Sample();
        Check(x.LeftTravel==y.LeftTravel && x.RightTravel==y.RightTravel,"deterministic position "+i+"/"+j);
        Check(x.LeftFlash==y.LeftFlash && x.RightFlash==y.RightFlash,"deterministic flash "+i+"/"+j);
        Check(x.LeftTravel>=0f && x.LeftTravel<=DualRecoilCore.MaxTravel,"left hard bounds "+i+"/"+j);
        Check(x.RightTravel>=0f && x.RightTravel<=DualRecoilCore.MaxTravel,"right hard bounds "+i+"/"+j);
        Check(x.LeftFlash>=0f && x.LeftFlash<=1f && x.RightFlash>=0f && x.RightFlash<=1f,"flash hard bounds "+i+"/"+j);
    }
    a.SetVisualsEnabled(false);
    Check(a.Sample().LeftTravel==0f && a.Sample().RightTravel==0f,"off cannot leave stale motion "+i);
}
var frames=new List<object>();
void Frame(string name, DualRecoilCore x){frames.Add(new {name,left=x.Sample().LeftTravel,right=x.Sample().RightTravel,flashL=x.Sample().LeftFlash,flashR=x.Sample().RightFlash});}
motion.Clear();Frame("rest",motion);
motion.Fire(RecoilLane.Left);motion.Advance(.015f);Frame("left_start",motion);
motion.Advance(.020f);Frame("left_peak",motion);
motion.Advance(.040f);Frame("left_recover",motion);
motion.Fire(RecoilLane.Right);motion.Advance(.015f);Frame("right_start",motion);
motion.Fire(RecoilLane.Left);motion.Advance(.025f);Frame("both",motion);
motion.Advance(.60f);Frame("settle",motion);
motion.SetVisualsEnabled(false);Frame("vfx_off",motion);
if(args.Length>0) {
    var path=Path.GetFullPath(args[0]);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path,JsonSerializer.Serialize(frames,new JsonSerializerOptions{WriteIndented=true}));
}
Console.WriteLine("ORIGINAL_DUAL_RECOIL_EXEC_PASS "+checks+"/"+checks+" 2048 deterministic vectors x6 updates, dual lane independence");
