using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Weapons.RotarySaw.Tests
{
    public sealed class RotarySawPhysicsTests
    {
        private readonly List<GameObject> created = new List<GameObject>();
        private RotarySawModule saw;
        private Rigidbody body;
        private GameObject owner;
        private RotarySawConfiguration config;

        private GameObject Make(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            return go;
        }

        private void SetupSaw(bool kinetic = true)
        {
            owner = Make("R6 adapter TEST owner");
            body = owner.AddComponent<Rigidbody>();
            body.isKinematic = kinetic;
            body.useGravity = false;
            body.mass = 50f;
            var blade = Make("blade");
            blade.transform.SetParent(owner.transform);
            saw = blade.AddComponent<RotarySawModule>();
            var sink = blade.AddComponent<RotarySawDemoDamageSink>();
            config = new RotarySawConfiguration {
                bladeRadius = .9f, bladeHalfThickness = .2f, hubDeadZone = .05f,
                maximumRpm = 1000f, minimumCutRpm = 10f,
                accelerationRpmPerSecond = 100000f, decelerationRpmPerSecond = 100000f,
                damagePerSecondAtFullRpm = 100f, heatPerSecondAtFullRpm = 0f,
                maxCuttableResistance = 3f
            };
            saw.Configure(config);
            saw.Bind(body, owner.transform, blade.transform);
            saw.SetDamageSink(sink);
            saw.SetMotorRequested(true);
        }

        private RotarySawDemoTarget Target(Vector3 position, float resistance = 1f)
        {
            GameObject go = Make("destructible block");
            go.transform.position = position;
            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.size = Vector3.one * .3f;
            RotarySawDemoTarget target = go.AddComponent<RotarySawDemoTarget>();
            target.SetResistance(resistance);
            Physics.SyncTransforms();
            return target;
        }

        [TearDown] public void Cleanup()
        {
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        [UnityTest] public IEnumerator TouchingBlockTakesDamage()
        {
            SetupSaw(); var t=Target(new Vector3(0f,0f,.55f));
            yield return new WaitForFixedUpdate();
            Assert.Greater(t.TotalDamage,0f);
        }
        [UnityTest] public IEnumerator BlockOutsideRadiusIsSafe()
        {
            SetupSaw(); var t=Target(new Vector3(0f,0f,2f));
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f,t.TotalDamage);
        }
        [UnityTest] public IEnumerator OffPlaneBlockIsSafe()
        {
            SetupSaw(); var t=Target(new Vector3(.8f,0f,.55f));
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f,t.TotalDamage);
        }
        [UnityTest] public IEnumerator NotRequestedDoesNoDamage()
        {
            SetupSaw(); saw.SetMotorRequested(false); var t=Target(new Vector3(0f,0f,.55f));
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f,t.TotalDamage);
        }
        [UnityTest] public IEnumerator DetachedModuleDoesNoDamage()
        {
            SetupSaw(); saw.SetOperational(false); var t=Target(new Vector3(0f,0f,.55f));
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f,t.TotalDamage);
        }
        [UnityTest] public IEnumerator DisabledModuleDoesNoDamage()
        {
            SetupSaw(); saw.enabled=false; var t=Target(new Vector3(0f,0f,.55f));
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f,t.TotalDamage);
        }
        [UnityTest] public IEnumerator MissingEnergySourceFailsClosed()
        {
            SetupSaw(); config.energyPerSecond=10f; saw.Configure(config); var t=Target(new Vector3(0f,0f,.55f));
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f,t.TotalDamage);
        }
        [UnityTest] public IEnumerator EmptyEnergySourceFailsClosed()
        {
            SetupSaw(); config.energyPerSecond=10f; saw.Configure(config);
            var energy=owner.AddComponent<RotarySawEnergyReservoir>(); energy.SetChargeForTesting(0f);
            saw.SetEnergySource(energy); var t=Target(new Vector3(0f,0f,.55f));
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f,t.TotalDamage);
        }
        [UnityTest] public IEnumerator MultipleCollidersOnPartDamageOnlyOncePerStep()
        {
            SetupSaw(); var t=Target(new Vector3(0f,0f,.55f));
            var second=Make("extra part collider"); second.transform.SetParent(t.transform);
            second.transform.localPosition = new Vector3(0f,.05f,0f);
            second.AddComponent<BoxCollider>().size=Vector3.one*.3f;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(1,t.HitCount);
            Assert.AreEqual(1,saw.DamageEventsLastTick);
        }
        // W28: an owned part in the rotating disc must stop the motor *before*
        // energy/heat and external damage, then immediately permit recovery after removal.
        [UnityTest] public IEnumerator OwnArmorInBladeDiscStopsMotorAndResumesAfterClearance()
        {
            SetupSaw();
            config.heatPerSecondAtFullRpm=15f; saw.Configure(config);
            var enemy=Target(new Vector3(0f,0f,-.55f));
            var armor=Make("W28 owned armor blocking blade");
            armor.transform.SetParent(owner.transform);
            armor.transform.localPosition=new Vector3(0f,0f,.55f);
            armor.AddComponent<BoxCollider>().size=Vector3.one*.3f;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(saw.OwnGeometryBlockedLastTick);
            Assert.AreEqual(0f,saw.Motor.Heat,"blocked own armor must not heat the saw");
            Assert.AreEqual(0f,saw.Motor.Rpm,"blocked own armor must not spin the saw");
            Assert.AreEqual(0f,enemy.TotalDamage,"own armor must not permit damage to enemy beyond");
            armor.SetActive(false); Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(saw.OwnGeometryBlockedLastTick);
            Assert.Greater(saw.Motor.Rpm,0f,"unchanged motor request should resume after clearance");
            Assert.Greater(enemy.TotalDamage,0f,"nonblocked contact target must be cut");
        }

        [UnityTest] public IEnumerator RegisteredDetachedOwnPartStillStopsSawUntilCleared()
        {
            SetupSaw();
            var former=Make("W28 registered former owned part");
            former.transform.position=new Vector3(0f,0f,.55f);
            var collision=former.AddComponent<BoxCollider>();collision.size=Vector3.one*.3f;
            saw.RegisterOwnerCollider(collision);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(saw.OwnGeometryBlockedLastTick,"registered detached collider remains a safety blocker");
            Assert.AreEqual(0f,saw.Motor.Rpm);
            former.transform.position=new Vector3(0f,0f,3f);Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(saw.OwnGeometryBlockedLastTick);
            Assert.Greater(saw.Motor.Rpm,0f);
        }

        [UnityTest] public IEnumerator OwnPartsNotCut()
        {
            SetupSaw(); var t=Target(new Vector3(0f,0f,.55f));
            t.transform.SetParent(owner.transform);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f,t.TotalDamage);
        }
        [UnityTest] public IEnumerator HardMaterialJamsWithoutDamage()
        {
            SetupSaw(); var t=Target(new Vector3(0f,0f,.55f),12f);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f,t.TotalDamage);
            Assert.Greater(saw.Motor.JamRemaining,0f);
        }
        [UnityTest] public IEnumerator JamDoesNotPartiallyDamageOtherContacts()
        {
            SetupSaw(); var weak=Target(new Vector3(0f,0f,.45f),1f);
            var hard=Target(new Vector3(0f,0f,-.45f),10f);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f,weak.TotalDamage);
            Assert.AreEqual(0f,hard.TotalDamage);
        }
        // W29: an OWN-GEOMETRY broadphase overflow must remain visible in
        // OverflowedLastTick, even though the legacy DAMAGE broadphase is skipped.
        [UnityTest] public IEnumerator OwnerOverlapOverflowFailsClosedAndRecoversWithDiagnostic()
        {
            SetupSaw();
            config.maxOverlapHits = 4;
            config.heatPerSecondAtFullRpm = 18f;
            saw.Configure(config);
            var target = Target(new Vector3(0f, 0f, -.55f));
            var owned = new GameObject[4];
            for (int i = 0; i < owned.Length; i++)
            {
                owned[i] = Make("W29 owner buffer saturator " + i);
                owned[i].transform.SetParent(owner.transform, false);
                owned[i].transform.localPosition = new Vector3(0f, (i - 1.5f) * .1f, .4f);
                owned[i].AddComponent<BoxCollider>().size = Vector3.one * .2f;
            }
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(saw.OverflowedLastTick,
                "fail-closed owner broadphase saturation must retain the public overflow diagnostic");
            Assert.IsTrue(saw.OwnGeometryBlockedLastTick);
            Assert.AreEqual(0f, saw.Motor.Rpm);
            Assert.AreEqual(0f, saw.Motor.Heat);
            Assert.AreEqual(0f, target.TotalDamage);

            for (int i = 0; i < owned.Length; i++) owned[i].SetActive(false);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(saw.OverflowedLastTick, "diagnostic must reset after real clearance");
            Assert.IsFalse(saw.OwnGeometryBlockedLastTick);
            Assert.Greater(saw.Motor.Rpm, 0f);
            Assert.Greater(target.TotalDamage, 0f, "motor must still cut an external contact after the obstruction clears");
        }

        [UnityTest] public IEnumerator OverlapOverflowDoesNotPartiallyDamage()
        {
            SetupSaw(); config.maxOverlapHits=4; saw.Configure(config);
            for(int i=0;i<5;i++) Target(new Vector3(0f,0f,.25f+i*.10f));
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(saw.OverflowedLastTick);
            Assert.AreEqual(0,saw.DamageEventsLastTick);
        }
        [UnityTest] public IEnumerator RegisteredExternalOwnerColliderIsIgnored()
        {
            SetupSaw(); var t=Target(new Vector3(0f,0f,.55f));
            saw.RegisterOwnerCollider(t.GetComponent<Collider>());
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f,t.TotalDamage);
        }
        [UnityTest] public IEnumerator ContactReactionCanMoveVehicle()
        {
            SetupSaw(false); config.reactionForceNewtonsPerContact=200f; config.maximumTotalReactionForceNewtons=200f; saw.Configure(config);
            Target(new Vector3(0f,0f,.55f));
            for(int i=0;i<8;i++) yield return new WaitForFixedUpdate();
            Assert.Less(body.linearVelocity.z,0f);
        }
        [UnityTest] public IEnumerator NoSinkNeverDealsDamage()
        {
            SetupSaw(); saw.SetDamageSink(null); var t=Target(new Vector3(0f,0f,.55f));
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f,t.TotalDamage);
        }
        [UnityTest] public IEnumerator SawCanResumeAfterRepaired()
        {
            SetupSaw(); var t=Target(new Vector3(0f,0f,.55f));
            saw.SetOperational(false);
            yield return new WaitForFixedUpdate();
            saw.SetOperational(true); saw.SetMotorRequested(true);
            yield return new WaitForFixedUpdate();
            Assert.Greater(t.TotalDamage,0f);
        }
        [UnityTest] public IEnumerator DisablingDamageSinkBlocksDamage()
        {
            SetupSaw(); var sink=saw.GetComponent<RotarySawDemoDamageSink>(); sink.enabled=false;
            var t=Target(new Vector3(0f,0f,.55f));
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f,t.TotalDamage);
        }
    }
}
