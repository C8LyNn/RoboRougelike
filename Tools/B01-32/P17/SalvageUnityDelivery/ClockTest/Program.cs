using RoboRougelike.B0132P17.SalvageUnity;
int checks=0;
void Verify(bool p,string label){if(!p)throw new Exception("ORIGINAL_BEACON_FAIL "+label);checks++;}
var clock=new SalvageBeaconClock();
Verify(clock.Frame==0,"starts zero");
Verify(clock.Advance(-1f,true)==0,"negative delta ignored");
Verify(clock.Advance(float.NaN,true)==0,"NaN ignored");
Verify(clock.Advance(float.PositiveInfinity,true)==0,"infinity ignored");
Verify(clock.Advance(.01f,true)==0,"small positive not yet a frame");
Verify(clock.Advance(0f,false)==0,"OFF resets");
Verify(clock.Advance(.13f,true)==1,"progresses first phase");
Verify(clock.Advance(.24f,true)==3,"progresses sequentially");
Verify(clock.Advance(0f,false)==0,"OFF drops old phase");
Verify(clock.Frame==0,"OFF zero confirmed");
Verify(clock.Advance(1.92f,true)==0,"long pause wraps 16 frames");
Verify(clock.Advance(100000f,true)==0,"huge delta clamps bounded and wraps");
for(int i=0;i<8;i++){clock.Reset();clock.Advance(.12f*(i+1)+.001f,true);Verify(clock.Frame==(i+1)%8,"frame "+i);}
uint seed=1337u;
for(int i=0;i<2048;i++){
 seed=unchecked(seed*1664525u+1013904223u);
 float v=(seed%101)/1000f;
 var a=new SalvageBeaconClock();
 var b=new SalvageBeaconClock();
 for(int n=0;n<4;n++){
   int av=a.Advance(v,true),bv=b.Advance(v,true);
   Verify(av==bv&&av>=0&&av<8,"repeatable vector "+i+" step "+n);
 }
 a.Advance(0f,false);
 Verify(a.Frame==0,"VFX OFF returns frame0 vector "+i);
}
Console.WriteLine("ORIGINAL_BEACON_CLOCK_EXEC_PASS "+checks+"/"+checks+" checks 2048 deterministic vectors, no Unity claim");
