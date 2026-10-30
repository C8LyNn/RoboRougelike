// B01-60 PASS17 W09: independent matrix source contract against unchanged original B01-25 C#9.
// GeneratedPulseCore.cs is a byte-exact Git object of either frozen RED or B01-25 candidate.
// This is NOT Unity Editor or actual HP/PhysX, and does not modify producer source.
using System;
using System.Collections.Generic;
using System.Linq;
using B01.LevelChallenges;

internal static class BoundaryMatrix
{
    static readonly (string Name,double[] Steps)[] Partitions = new[] {
        ("10x0.1",Enumerable.Repeat(.1,10).ToArray()),
        ("4x0.25",Enumerable.Repeat(.25,4).ToArray()),
        ("8x0.125",Enumerable.Repeat(.125,8).ToArray()),
        ("5x0.2",Enumerable.Repeat(.2,5).ToArray()),
        ("20x0.05",Enumerable.Repeat(.05,20).ToArray()),
        ("4x(0.1+0.15)",Enumerable.Repeat(new[]{.1,.15},4).SelectMany(x=>x).ToArray()),
        ("4x(0.2+0.05)",Enumerable.Repeat(new[]{.2,.05},4).SelectMany(x=>x).ToArray()),
        ("8x(0.075+0.05)",Enumerable.Repeat(new[]{.075,.05},8).SelectMany(x=>x).ToArray())
    };
    static void Require(bool yes,string message)
    {
        if(!yes)throw new InvalidOperationException(message);
    }
    static PulseZoneController New(double offset)=>
        new PulseZoneController(new PulseZoneConfig("B01_60_W09",28,32,1.35,1,2,3,offset));
    static PulseSample Inside(double dt)=>new PulseSample(dt,28,32,true,false);
    public static int Main(string[] args)
    {
        string mode=Environment.GetEnvironmentVariable("B01_60_MODE")??"BASELINE";
        bool candidate=string.Equals(mode,"CANDIDATE",StringComparison.OrdinalIgnoreCase);
        int mismatches=0, boundaryChecks=0, pulseChecks=0;
        bool exactOriginalReproduced=false;
        var rows=new List<string>();
        foreach(double offset in new[]{0d,3d})
        foreach(var partition in Partitions)
        {
            var c=New(offset);
            int totalPulses=0;
            for(int whole=1;whole<=24;whole++)
            {
                PulseSnapshot result=default;
                foreach(double dt in partition.Steps)
                {
                    result=c.Advance(Inside(dt));
                    if(result.DamagePulse)totalPulses++;
                }
                // Time in mathematical whole seconds, unlike actual repeated binary float accumulation.
                double officialTime=whole;
                PulsePhase expected=c.PhaseAt(officialTime);
                boundaryChecks++;
                bool wrong=result.Phase!=expected;
                if(wrong)
                {
                    mismatches++;
                    if(rows.Count<20)
                        rows.Add("MISMATCH offset="+offset+" steps="+partition.Name+
                            " boundary="+whole+" observed="+result.Phase+
                            " expected="+expected+" elapsed="+c.ElapsedSeconds.ToString("R"));
                }
                if(offset==0d && partition.Name=="10x0.1" && whole==1)
                {
                    exactOriginalReproduced=wrong &&
                        result.Phase==PulsePhase.Warning && expected==PulsePhase.Active;
                }
                // Callers supply a valid point throughout. The 6-second phase cycle always contains
                // four complete activation windows in 24 seconds, regardless of host frame partition.
                if(whole==24)
                {
                    pulseChecks++;
                    if(totalPulses!=4)
                    {
                        mismatches++;
                        rows.Add("MISMATCH damage pulses offset="+offset+
                            " steps="+partition.Name+" actual="+totalPulses+" expected=4");
                    }
                }
                Require(Math.Abs(c.ElapsedSeconds-officialTime)<1e-8,
                        "clock drift too large "+partition.Name+" whole="+whole);
            }
        }
        foreach(var row in rows)Console.WriteLine(row);
        Console.WriteLine("B01_60_W09_PHASE_MATRIX boundary_checks="+boundaryChecks+
                          " pulse_checks="+pulseChecks+" mismatches="+mismatches+
                          " partitions="+Partitions.Length+" cycles=24 offsets=2");
        Console.WriteLine("B01_60_W09_FROZEN_ORIGINAL_0_1_REPRO="+exactOriginalReproduced);
        Console.WriteLine("B01_60_W09_MODE="+mode);
        Console.WriteLine("B01_60_W09_UNITY_NATIVE=NOT_RUN G1=NOT_RUN CODEX_ACK=PENDING");
        if(candidate)
        {
            Require(mismatches==0,"PATCH_REJECTED: revised producer source is still partition-dependent");
            Console.WriteLine("B01_60_W09_CANDIDATE_MATRIX_ACCEPTED");
            return 0;
        }
        Require(exactOriginalReproduced,"BASELINE_INVALID: known exact #430 original RED signature no longer reproduces");
        Require(mismatches>0,"BASELINE_INVALID: no source RED reproduced");
        Console.WriteLine("B01_60_W09_EXPECTED_ORIGINAL_RED_CONFIRMED");
        return 0;
    }
}
