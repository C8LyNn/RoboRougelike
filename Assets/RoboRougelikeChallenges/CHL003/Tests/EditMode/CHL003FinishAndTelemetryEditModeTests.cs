using System;
using NUnit.Framework;

namespace B01.CHL003.Tests
{
    public sealed class CHL003FinishAndTelemetryEditModeTests
    {
        [Test] public void E11_FinishContactBeforeFinalCheckpointCannotWinSameFixedStep()
        {
            var a=new CHL003FinishArbiter();
            Assert.IsTrue(a.ObserveFinishContact(17,true));
            a.LastCheckpointAccepted(17);
            Assert.IsFalse(a.TryCommit(17,true,true,false));
            Assert.IsTrue(a.TryCommit(18,true,true,false));
            Assert.IsFalse(a.TryCommit(19,true,true,false));
        }

        [Test] public void E12_FinalCheckpointBeforeFinishAlsoDeferred()
        {
            var a=new CHL003FinishArbiter();
            a.LastCheckpointAccepted(18);
            Assert.IsTrue(a.ObserveFinishContact(18,true));
            Assert.IsFalse(a.TryCommit(18,true,true,false));
            Assert.IsTrue(a.TryCommit(19,true,true,false));
        }

        [Test] public void E13_ExpiredContactPausedAndRetryNeverAutoWin()
        {
            var a=new CHL003FinishArbiter();
            Assert.IsFalse(a.ObserveFinishContact(1,false));
            Assert.IsFalse(a.TryCommit(3,true,true,false));
            a.LastCheckpointAccepted(3);
            a.ObserveFinishContact(3,true);
            Assert.IsFalse(a.TryCommit(4,true,false,false));
            Assert.IsFalse(a.TryCommit(5,true,true,false));
            a.ObserveFinishContact(6,true);
            Assert.IsFalse(a.TryCommit(7,true,true,true));
            a.Reset();
            Assert.IsFalse(a.TryCommit(8,true,true,false));
            Assert.Throws<ArgumentOutOfRangeException>(()=>
                a.ObserveFinishContact(-1,true));
        }

        [Test] public void E14_ActualContactFramesGenerateInspectableReproducibleCounters()
        {
            var m=new CHL003TrialMetrics();
            m.Reset("LIGHT",1,270,2.0f,2.30f);
            m.Begin();
            m.RecordPhysics(0,1,0,0,0);
            m.RecordPhysics(0,1.5f,5,8f,.2f);
            m.RecordCollision("CHL003_REAL_LOW_BARRIER",4,true);
            m.RecordCollision("CHL003_SOLID_RAMP",2,true);
            m.RecordCollision("CHL003_NARROW_SOLID_BRIDGE",3,true);
            m.RecordCheckpoint(1,.2f);
            m.RecordCompleted(.3f);
            Assert.AreEqual(3,m.CollisionEntries);
            Assert.AreEqual(9,m.ContactPoints);
            Assert.AreEqual(4,m.BarrierContacts);
            Assert.AreEqual(2,m.RampContacts);
            Assert.AreEqual(3,m.BridgeContacts);
            Assert.AreEqual(2,m.PhysicsSamples);
            Assert.Greater(m.DistanceMeters,5f);
            Assert.AreEqual(1,m.Completions);
            Assert.IsTrue(m.ToCsv().Contains("COLLISION"));
            StringAssert.Contains("Completed",m.SummaryCsv());
        }

        [Test] public void E15_RetryHeavyKeepsRigDifferenceClearsFallAndTime()
        {
            var m=new CHL003TrialMetrics();
            m.Reset("HEAVY",2,1150,2.82f,3.40f);
            m.Begin();
            m.RecordPhysics(0,1,0,0,0);
            m.RecordFailed("FellOffCourse",3f);
            Assert.AreEqual(1,m.Falls);
            m.Reset("HEAVY",3,1150,2.82f,3.40f);
            Assert.AreEqual(0,m.Falls);
            Assert.AreEqual(0,m.Completions);
            Assert.AreEqual(0,m.PhysicsSamples);
            Assert.AreEqual(0,m.ContactPoints);
            Assert.AreEqual(0f,m.ElapsedSeconds);
            Assert.AreEqual(1150,m.MassKg);
            Assert.AreEqual(2.82f,m.WidthMeters);
            Assert.Throws<ArgumentException>(()=>
                m.Reset("FAKE",1,1,1,1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>
                m.RecordCollision("BARRIER",-1,true));
        }

        [Test] public void E16_ActualForwardFullHullCrossesGateWithClearance()
        {
            Assert.IsTrue(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.1f,.12f,-1.41f,1.41f,-2.20f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
            Assert.IsTrue(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.1f,.12f,-1f,1f,-2.20f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
        }

        [Test] public void E17_ReverseAndWheelOnlyHitDoNotCreditGate()
        {
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                .15f,-.15f,-1f,1f,-2.20f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.1f,.12f,-1.0f,1.65f,-2.20f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.1f,-.01f,-1f,1f,-2.20f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.1f,.12f,-1f,1f,-2.20f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,-1f));
        }

        [Test] public void E18_NonFiniteAndApertureVerticalOverflowFailClosed()
        {
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                float.NaN,.1f,-1f,1f,-2.20f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.1f,.1f,-1f,1f,-2.50f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.1f,.1f,-1f,1f,-2.20f,2.7f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
        }
    }
}
