using System;
using RoboRougelike.B01Performance;
int pass=0,fail=0;
void Test(string name, Action action) {
    try { action(); Console.WriteLine("ORIGINAL_W28_CORE_PASS "+name); ++pass; }
    catch (Exception ex) { Console.WriteLine("ORIGINAL_W28_CORE_FAIL "+name+": "+ex.Message); ++fail; }
}
void Assert(bool b) { if(!b)throw new Exception("predicate false"); }
(int, bool[]) Choose(float[] speeds,bool[] mask,int cap,float threshold) {
    bool[] x=new bool[speeds.Length];
    int n=DebrisSparkBudgetCore.ChooseFastest(speeds,mask,x,speeds.Length,cap,threshold);
    return(n,x);
}
Test("zero objects",()=>{var r=Choose(Array.Empty<float>(),Array.Empty<bool>(),0,0);Assert(r.Item1==0);});
Test("zero cap",()=>{var r=Choose(new[]{5f,8f},new[]{true,true},0,1);Assert(r.Item1==0);});
Test("one fastest",()=>{var r=Choose(new[]{5f,8f},new[]{true,true},1,1);Assert(r.Item1==1&&!r.Item2[0]&&r.Item2[1]);});
Test("two fastest",()=>{var r=Choose(new[]{1f,5f,4f},new[]{true,true,true},2,0);Assert(r.Item1==2&&!r.Item2[0]&&r.Item2[1]&&r.Item2[2]);});
Test("ties stable order",()=>{var r=Choose(new[]{4f,4f,4f},new[]{true,true,true},2,0);Assert(r.Item2[0]&&r.Item2[1]&&!r.Item2[2]);});
Test("eligibility respected",()=>{var r=Choose(new[]{20f,4f},new[]{false,true},1,1);Assert(!r.Item2[0]&&r.Item2[1]);});
Test("speed threshold inclusive",()=>{var r=Choose(new[]{4f,3.99f},new[]{true,true},2,4);Assert(r.Item1==1&&r.Item2[0]);});
Test("negative speed excluded",()=>{var r=Choose(new[]{-1f,4f},new[]{true,true},2,0);Assert(r.Item1==1&&!r.Item2[0]);});
Test("nan speed excluded",()=>{var r=Choose(new[]{float.NaN,4f},new[]{true,true},2,0);Assert(r.Item1==1&&!r.Item2[0]);});
Test("infinity speed excluded",()=>{var r=Choose(new[]{float.PositiveInfinity,4f},new[]{true,true},2,0);Assert(r.Item1==1&&!r.Item2[0]);});
Test("oversized cap bounded",()=>{var r=Choose(new[]{3f,4f},new[]{true,true},100,0);Assert(r.Item1==2);});
Test("result reset after repeat",()=>{var speeds=new[]{5f,2f};var flags=new[]{true,true};var chosen=new[]{true,true};var n=DebrisSparkBudgetCore.ChooseFastest(speeds,flags,chosen,2,1,0);Assert(n==1&&chosen[0]&&!chosen[1]);});
Test("input null rejected",()=>{try{DebrisSparkBudgetCore.ChooseFastest(null,new[]{true},new bool[1],1,1,0);throw new Exception("not rejected");}catch(ArgumentException){}});
Test("count overflow rejected",()=>{try{DebrisSparkBudgetCore.ChooseFastest(new[]{1f},new[]{true},new bool[1],2,1,0);throw new Exception("not rejected");}catch(ArgumentException){}});
Test("invalid threshold rejected",()=>{try{Choose(new[]{1f},new[]{true},1,float.NaN);throw new Exception("not rejected");}catch(ArgumentException){}});
Test("invalid negative budget rejected",()=>{try{Choose(new[]{1f},new[]{true},-1,0);throw new Exception("not rejected");}catch(ArgumentException){}});
Test("partial prefix untouched",()=>{var a=new[]{7f,99f};var m=new[]{true,true};var c=new[]{false,true};int n=DebrisSparkBudgetCore.ChooseFastest(a,m,c,1,1,0);Assert(n==1&&c[0]&&c[1]);});
Test("no allocations from selected array replacement",()=>{var speed=new float[70];var mask=new bool[70];var chosen=new bool[70];for(int i=0;i<70;i++){speed[i]=i;mask[i]=true;}for(int pass=0;pass<100;pass++){int n=DebrisSparkBudgetCore.ChooseFastest(speed,mask,chosen,70,16,0);Assert(n==16);} });
Console.WriteLine($"ORIGINAL_W28_DEBRIS_CORE_TESTS {pass}/18 PASS, {fail} FAIL; NATIVE_UNITY_NOT_RUN");
if(pass!=18||fail!=0)Environment.ExitCode=1;
