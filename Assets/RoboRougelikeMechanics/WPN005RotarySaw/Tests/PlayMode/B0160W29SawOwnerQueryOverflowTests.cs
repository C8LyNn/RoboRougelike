using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Weapons.RotarySaw.Tests
{
    /// <summary>
    /// B01-60 independent W29 original Unity Physics acceptance on #828.
    /// Specifically regress the original owner-clearance query's public overflow
    /// receipt. The 20th original test catches this too; this test distinguishes
    /// owner-guard overflow from downstream enemy-contact overflow and observes
    /// heat/RPM/energy/damage before and after physical clearance.
    /// NO fake UnityEngine API and NO replacement runtime implementation.
    /// </summary>
    public sealed class B0160W29SawOwnerQueryOverflowTests
    {
        private readonly List<GameObject> created = new List<GameObject>();
        private GameObject Create(string name, Vector3 where)
        {
            var obj = new GameObject(name);
            obj.transform.position = where;
            created.Add(obj);
            return obj;
        }

        [TearDown] public void Cleanup()
        {
            for(int i=created.Count-1; i>=0; i--)
                if(created[i]!=null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        [UnityTest]
        public IEnumerator SaturatedOwnerSafetyBroadphase_SetsOverflowAndStopsResources_ThenClearedMotorCuts()
        {
            // This is the exact original WPN005 real MonoBehaviour, real
            // Rigidbody and real Unity BoxCollider/PhysX query path.
            var owner=Create("B0160-W29 physical saw owner",Vector3.zero);
            var body=owner.AddComponent<Rigidbody>();
            body.isKinematic=true;body.useGravity=false;
            var rotor=Create("B0160-W29 real blade",Vector3.zero);
            rotor.transform.SetParent(owner.transform,true);
            var saw=rotor.AddComponent<RotarySawModule>();
            var sink=rotor.AddComponent<RotarySawDemoDamageSink>();
            var cfg=new RotarySawConfiguration
            {
                bladeRadius=.9f, bladeHalfThickness=.2f, hubDeadZone=.05f,
                minimumCutRpm=10f, maximumRpm=1000f,
                accelerationRpmPerSecond=100000f, decelerationRpmPerSecond=100000f,
                damagePerSecondAtFullRpm=100f, heatPerSecondAtFullRpm=15f,
                maxOverlapHits=4, maxCuttableResistance=3f, energyPerSecond=5f
            };
            saw.Configure(cfg);
            saw.Bind(body,owner.transform,rotor.transform);
             saw.SetDamageSink(sink);
            var energy=owner.AddComponent<RotarySawEnergyReservoir>();
            energy.SetChargeForTesting(20f);
            saw.SetEnergySource(energy);
            saw.SetAutomaticSimulation(false);
            saw.SetMotorRequested(true);

            var targets=new List<RotarySawDemoTarget>();
            for(int i=0;i<5;i++)
            {
                var block=Create("B0160-W29 physical contact "+i,new Vector3(0f,0f,.45f+i*.05f));
                block.AddComponent<BoxCollider>().size=Vector3.one*.30f;
                var target=block.AddComponent<RotarySawDemoTarget>();
                target.SetResistance(1f);
                targets.Add(target);
            }

            yield return null;
            Physics.SyncTransforms();
            saw.Advance(.02f);

            Assert.That(saw.OwnGeometryBlockedLastTick,Is.True,
                "Full safety query must fail closed; do not trust an incomplete own-collider set");
            Assert.That(saw.OverflowedLastTick,Is.True,
                "FULL owner-safety query must set the same public overflow diagnostic as external contact query");
            Assert.That(saw.DamageEventsLastTick,Is.Zero,
                "Any overflow must reject the ENTIRE physical contact batch");
            Assert.That(saw.Motor.Rpm,Is.Zero,
                "Own-clearance unknown must not spin the saw");
            Assert.That(saw.Motor.Heat,Is.Zero,
                "Own-clearance unknown must not generate thermal heat");
            Assert.That(energy.Charge,Is.EqualTo(20f),
                "Incomplete owner-clearance query must never debit actual rotary motor energy");
            foreach(var target in targets)
                Assert.That(target.TotalDamage,Is.Zero,
                    "No external enemy target may be damaged during incomplete owner-clearance query");

            // Clear four physical colliders. There is now one valid enemy
            // contact and unchanged request; both motor and damage must
            // genuinely resume without silently keeping overflow latched.
            for(int i=0;i<4;i++) targets[i].gameObject.SetActive(false);
            Physics.SyncTransforms();
            saw.Advance(.02f);
            Assert.That(saw.OverflowedLastTick,Is.False,
                "Overflow diagnostic must clear after full bounded physical query");
            Assert.That(saw.OwnGeometryBlockedLastTick,Is.False,
                "No separate own part remains in blade disc");
            Assert.That(saw.Motor.Rpm,Is.GreaterThan(0f),
                "Real original rotary motor must resume after query becomes trustworthy");
            Assert.That(saw.Motor.Heat,Is.GreaterThan(0f),
                "Real motor only heats once unblocked");
            Assert.That(energy.Charge,Is.LessThan(20f),
                "Only a genuinely unblocked rotary motor may spend energy");
            Assert.That(targets[4].TotalDamage,Is.GreaterThan(0f),
                "A remaining genuine enemy collider must receive actual saw damage");
            Assert.That(saw.DamageEventsLastTick,Is.EqualTo(1));
        }
    }
}
