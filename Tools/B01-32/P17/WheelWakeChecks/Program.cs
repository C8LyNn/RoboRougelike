using System;
using RoboRougelike.B0132P17;

int checks = 0;
void Verify(bool ok, string reason)
{
    if (!ok) throw new Exception("ORIGINAL_SOURCE_FAIL " + reason);
    checks++;
}
WheelWakeSample Eval(WheelWakeMode m, float s = 8f, float slip = .5f, bool contact = true, bool on = true)
    => WheelWakeProfile.Evaluate(m, s, slip, contact, on);

Verify(!Eval(WheelWakeMode.Off).Visible, "off must suppress visuals");
Verify(!Eval(WheelWakeMode.Cruise, contact:false).Visible, "airborne must suppress visuals");
Verify(!Eval(WheelWakeMode.Cruise, on:false).Visible, "VFX OFF must suppress visuals");
Verify(!Eval(WheelWakeMode.Cruise, s:0f).Visible, "idle speed");
Verify(!Eval(WheelWakeMode.Cruise, s:-1f).Visible, "negative speed");
Verify(!Eval(WheelWakeMode.Drift, s:float.NaN).Visible, "NaN speed");
Verify(!Eval(WheelWakeMode.Brake, s:float.PositiveInfinity).Visible, "infinite speed");
Verify(!Eval(WheelWakeMode.Cruise, slip:float.NaN).Visible, "NaN slip");
Verify(!Eval(WheelWakeMode.Cruise, slip:float.NegativeInfinity).Visible, "infinite slip");
Verify(!Eval(WheelWakeMode.Drift, slip:1.01f).Visible, "slip overflow");
Verify(!Eval(WheelWakeMode.Drift, slip:-.01f).Visible, "negative slip");
Verify(!Eval((WheelWakeMode)500).Visible, "unknown enum fail-closed");
Console.WriteLine("NEGATIVE_CONTROL_PASS 12/12 invalid/off samples returned invisible");

// Reproducible, bounded results on all valid modes, exact original source linked in project.
foreach (var mode in new[]{WheelWakeMode.Cruise,WheelWakeMode.Drift,WheelWakeMode.Brake})
{
    var x = Eval(mode, s:6f, slip:.7f);
    var y = Eval(mode, s:12f, slip:.7f);
    Verify(x.Visible && y.Visible, mode+" visibly emitted on valid contact");
    Verify(y.RatePerSecond>x.RatePerSecond, mode+" rate increases with speed");
    Verify(x.ParticleLifetime>0f && x.ParticleLifetime<=2f, mode+" positive bounded lifetime");
    Verify(x.ParticleSize>0f && x.ParticleSize<=1f, mode+" bounded size");
    Verify(x.ParticleSpeed>0f && x.ParticleSpeed<=3f, mode+" bounded particle speed");
    Verify(x.Alpha>0f && x.Alpha<=1f, mode+" valid alpha");
    Verify(x.Red>=0f && x.Red<=1f && x.Green>=0f && x.Green<=1f && x.Blue>=0f && x.Blue<=1f, mode+" RGB");
    var cap=Eval(mode,s:14f,slip:.7f);
    var high=Eval(mode,s:1000f,slip:.7f);
    Verify(cap.RatePerSecond==high.RatePerSecond && cap.ParticleSize==high.ParticleSize,mode+" speed clamp");
}
Console.WriteLine("POSITIVE_MODE_PASS 3 distinct modes, 8 invariants each");
Verify(Eval(WheelWakeMode.Cruise).Red != Eval(WheelWakeMode.Brake).Red, "cruise vs brake visual palette");
Verify(Eval(WheelWakeMode.Drift).ParticleSize>Eval(WheelWakeMode.Brake).ParticleSize,"drift dust vs brake sparks size");
Verify(Eval(WheelWakeMode.Drift,slip:.9f).RatePerSecond>Eval(WheelWakeMode.Drift,slip:.3f).RatePerSecond,"slip modulation");
Verify(Eval(WheelWakeMode.Cruise,slip:.8f).RatePerSecond==Eval(WheelWakeMode.Cruise,slip:.1f).RatePerSecond,"cruise independent of slip");

// Deterministic repeatability sweep, no randomized model behavior.
uint seed=0xC0FFEEu;
for(int i=0;i<2048;i++)
{
    seed = unchecked(seed*1664525u+1013904223u);
    float speed=(seed%25000)/1000f;
    seed = unchecked(seed*1664525u+1013904223u);
    float slip=(seed%1001)/1000f;
    var mode=(WheelWakeMode)(i%3+1);
    var a=Eval(mode,speed,slip);
    var b=Eval(mode,speed,slip);
    Verify(a.RatePerSecond==b.RatePerSecond && a.ParticleLifetime==b.ParticleLifetime
        && a.ParticleSize==b.ParticleSize && a.ParticleSpeed==b.ParticleSpeed
        && a.Alpha==b.Alpha, "determinism vector "+i);
    Verify(a.Visible && a.RatePerSecond <= 100f && a.ParticleSize<1f
        && a.ParticleLifetime<2f && a.Alpha<=1f,"bound vector "+i);
}
Console.WriteLine("SWEEP_PASS 2048/2048 exact original input vectors, repeatable and bounded");
Console.WriteLine("ORIGINAL_CPPROFILE_EXEC_PASS "+checks+"/"+checks+" assertions; Unity native remains NOT_RUN");
