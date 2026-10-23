using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Weapons.RotarySaw.Tests
{
    public sealed class RotarySawMathAndMotorTests
    {
        private static RotarySawConfiguration Fast()
        {
            return new RotarySawConfiguration {
                maximumRpm = 1000f, minimumCutRpm = 100f,
                accelerationRpmPerSecond = 2000f, decelerationRpmPerSecond = 2000f,
                heatCapacity = 100f, heatPerSecondAtFullRpm = 0f, coolingPerSecond = 20f,
                jamSeconds = 1f, jamHeatPenalty = 10f
            };
        }

        [Test] public void StoppedByDefault() { Assert.AreEqual(0, new RotarySawMotor(Fast()).Rpm); }
        [Test] public void RpmRampsInTime() { var m=new RotarySawMotor(Fast()); m.Tick(true,true,true,.1f); Assert.AreEqual(200f,m.Rpm,.001f); }
        [Test] public void RpmCannotExceedMaximum() { var m=new RotarySawMotor(Fast()); m.Tick(true,true,true,1f); Assert.AreEqual(1000f,m.Rpm,.001f); }
        [Test] public void MotorStopsWhenRequestedOff() { var m=new RotarySawMotor(Fast()); m.Tick(true,true,true,.5f); m.Tick(false,true,true,.2f); Assert.AreEqual(600f,m.Rpm,.001f); Assert.IsFalse(m.CanCut); }
        [Test] public void DeactivatedCannotCut() { var m=new RotarySawMotor(Fast()); m.Tick(true,true,false,.2f); Assert.IsFalse(m.CanCut); }
        [Test] public void MissingEnergyCannotCut() { var m=new RotarySawMotor(Fast()); m.Tick(true,false,true,.2f); Assert.IsFalse(m.CanCut); }
        [Test] public void SlowMotorCannotCut() { var m=new RotarySawMotor(Fast()); m.Tick(true,true,true,.01f); Assert.IsFalse(m.CanCut); }
        [Test] public void AtMinimumRpmCanCut() { var m=new RotarySawMotor(Fast()); m.Tick(true,true,true,.05f); Assert.IsTrue(m.CanCut); }
        [Test] public void JamStopsImmediately() { var m=new RotarySawMotor(Fast()); m.Tick(true,true,true,.5f); m.Jam(); Assert.AreEqual(0,m.Rpm); Assert.IsFalse(m.CanCut); }
        [Test] public void JamCannotEndEarly() { var m=new RotarySawMotor(Fast()); m.Jam(); m.Tick(true,true,true,.5f); Assert.IsFalse(m.CanCut); }
        [Test] public void JamEventuallyRecovers() { var m=new RotarySawMotor(Fast()); m.Jam(); m.Tick(true,true,true,1f); m.Tick(true,true,true,.2f); Assert.IsTrue(m.CanCut); }
        [Test] public void JamAddsHeat() { var m=new RotarySawMotor(Fast()); m.Jam(); Assert.AreEqual(10f,m.Heat,.001f); }
        [Test] public void ResetClearsJamHeatAndSpin() { var m=new RotarySawMotor(Fast()); m.Tick(true,true,true,.5f); m.Jam(); m.Reset(); Assert.AreEqual(0f,m.Heat); Assert.AreEqual(0f,m.JamRemaining); Assert.AreEqual(0f,m.Rpm); }
        [Test] public void StopImmediatelyResetsRpm() { var m=new RotarySawMotor(Fast()); m.Tick(true,true,true,.5f); m.StopImmediately(); Assert.AreEqual(0f,m.Rpm); }
        [Test] public void OverheatBlocksCutting() { var c=Fast(); c.heatCapacity=2f; c.heatPerSecondAtFullRpm=100f; var m=new RotarySawMotor(c); m.Tick(true,true,true,.5f); Assert.IsTrue(m.Overheated); Assert.IsFalse(m.CanCut); }
        [Test] public void OverheatReleasesBelowThreshold() { var c=Fast(); c.heatCapacity=2f; c.heatPerSecondAtFullRpm=100f; c.coolingPerSecond=20f; var m=new RotarySawMotor(c); m.Tick(true,true,true,.5f); m.Tick(false,true,true,.2f); Assert.IsFalse(m.Overheated); }
        [Test] public void ArcDoesNotHitOutsideBlade() { Assert.IsFalse(RotarySawContactMath.IsOnCuttingDisc(new Vector3(0,0,2),Vector3.right,1,.2f,.1f)); }
        [Test] public void DiscRejectsAxialSeparation() { Assert.IsFalse(RotarySawContactMath.IsOnCuttingDisc(new Vector3(.4f,0,.5f),Vector3.right,1,.2f,.1f)); }
        [Test] public void DiscAcceptsCuttingRim() { Assert.IsTrue(RotarySawContactMath.IsOnCuttingDisc(new Vector3(0,0,.7f),Vector3.right,1,.2f,.1f)); }
        [Test] public void DiscRejectsHub() { Assert.IsFalse(RotarySawContactMath.IsOnCuttingDisc(Vector3.zero,Vector3.right,1,.2f,.1f)); }
        [Test] public void DiscRejectsInvalidAxle() { Assert.IsFalse(RotarySawContactMath.IsOnCuttingDisc(Vector3.forward,Vector3.zero,1,.2f,0)); }
        [Test] public void DamageScalesWithTime() { Assert.AreEqual(10f,RotarySawContactMath.Damage(100f,.5f,.2f,1f),.001f); }
        [Test] public void DamageScalesWithResistance() { Assert.AreEqual(5f,RotarySawContactMath.Damage(100f,.5f,.2f,2f),.001f); }
        [Test] public void InvalidResistanceCannotCauseDamage() { Assert.AreEqual(0f,RotarySawContactMath.Damage(100f,1f,.2f,float.NaN)); }
        [Test] public void DamageCannotBeNegative() { Assert.AreEqual(0f,RotarySawContactMath.Damage(-100f,1f,.2f,1f)); }
        [Test] public void NegativeDeltaDoesNotSpinMotor() { var m=new RotarySawMotor(Fast()); m.Tick(true,true,true,-1f); Assert.AreEqual(0,m.Rpm); }
        [Test] public void ClampInvalidFloatToFallback() { Assert.AreEqual(.9f,RotarySawConfiguration.ClampFinite(float.NaN,0,2,.9f),.001f); }
        [Test] public void ConfigurationClampsDangerousValues() { var c=Fast(); c.maxOverlapHits=1; c.maximumRpm=-5; c.bladeRadius=0; c.ClampToSafeValues(); Assert.GreaterOrEqual(c.maxOverlapHits,4); Assert.Greater(c.maximumRpm,0); Assert.Greater(c.bladeRadius,0); }
        [Test] public void ConfigCopiedByMotor() { var c=Fast(); var m=new RotarySawMotor(c); c.maximumRpm=9000f; m.Tick(true,true,true,1f); Assert.AreEqual(1000f,m.Rpm,.001f); }
    }
}
