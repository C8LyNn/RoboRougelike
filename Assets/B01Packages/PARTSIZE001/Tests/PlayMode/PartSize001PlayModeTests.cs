using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.PARTSIZE001.Tests
{
    public sealed class PartSize001PlayModeTests
    {
        private readonly List<GameObject> created=new List<GameObject>();

        private BoxCollider Make(PartSizeDefinition spec,Cell3 anchor,Cell3 turns,
            Vector3 colliderSize,out Transform root)
        {
            var go=new GameObject("PARTSIZE isolated Physical Owner");
            created.Add(go);
            go.transform.position=new Vector3(anchor.X*.55f,anchor.Y*.55f,anchor.Z*.55f);
            go.transform.rotation=Quaternion.Euler(turns.X*90f,turns.Y*90f,turns.Z*90f);
            root=go.transform;
            var part=new GameObject("Physical registered volume");
            part.transform.SetParent(root,false);
            var c=part.AddComponent<BoxCollider>();
            var center=spec.LocalBoxCenter.Value;
            c.center=new Vector3((float)center.X,(float)center.Y,(float)center.Z);
            c.size=colliderSize;
            return c;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach(var go in created)if(go!=null)Object.Destroy(go);
            created.Clear();
            yield return null;
        }

        [UnityTest] public IEnumerator AdjacentLegacyCubesDoNotPenetrate()
        {
            var d=PartSizeReferenceFixtures.LegacyCube();
            var a=Make(d,new Cell3(0,0,0),new Cell3(0,0,0),
                Vector3.one*.52f,out Transform at);
            var b=Make(d,new Cell3(1,0,0),new Cell3(0,0,0),
                Vector3.one*.52f,out Transform bt);
            yield return new WaitForFixedUpdate();
            Assert.IsNull(PartSizeNativePhysics.Validate(d,new Cell3(1,0,0),
                new Cell3(0,0,0),bt,new Collider[]{b},new Collider[]{a},true));
        }

        [UnityTest] public IEnumerator RealPhysXRejectsTwoBodiesPenetrating()
        {
            var d=PartSizeReferenceFixtures.LegacyCube();
            var a=Make(d,new Cell3(0,0,0),new Cell3(0,0,0),
                Vector3.one*.52f,out Transform at);
            var b=Make(d,new Cell3(0,0,0),new Cell3(0,0,0),
                Vector3.one*.52f,out Transform bt);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual("PHYSICAL_PENETRATION",
                PartSizeNativePhysics.Validate(d,new Cell3(0,0,0),new Cell3(0,0,0),
                    bt,new Collider[]{b},new Collider[]{a},true));
        }

        [UnityTest] public IEnumerator RealColliderBiggerThanDeclaredCellsIsRejected()
        {
            var d=PartSizeReferenceFixtures.LegacyCube();
            var a=Make(d,new Cell3(0,0,0),new Cell3(0,0,0),
                new Vector3(1.2f,.52f,.52f),out Transform at);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual("PHYSICAL_ENVELOPE_OUTSIDE_OWNED_CELLS",
                PartSizeNativePhysics.Validate(d,new Cell3(0,0,0),new Cell3(0,0,0),
                    at,new Collider[]{a},new Collider[0],true));
        }

        [UnityTest] public IEnumerator WidePhysicalColliderYaw90FitsRotatedFourCells()
        {
            var d=PartSizeReferenceFixtures.LegacyWide();
            var a=Make(d,new Cell3(0,0,0),new Cell3(0,1,0),
                new Vector3(1.07f,.52f,1.07f),out Transform at);
            yield return new WaitForFixedUpdate();
            Assert.IsNull(PartSizeNativePhysics.Validate(d,new Cell3(0,0,0),
                new Cell3(0,1,0),at,new Collider[]{a},new Collider[0],true));
        }

        [UnityTest] public IEnumerator UnverifiedBroadphaseNeverCertifiesPhysicalContact()
        {
            var d=PartSizeReferenceFixtures.LegacyCube();
            var a=Make(d,new Cell3(0,0,0),new Cell3(0,0,0),
                Vector3.one*.52f,out Transform at);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual("PHYSICS_UNVERIFIED",
                PartSizeNativePhysics.Validate(d,new Cell3(0,0,0),new Cell3(0,0,0),
                    at,new Collider[]{a},new Collider[0],false));
        }

        [UnityTest] public IEnumerator DecorativeChildCannotBeSilentlyOmittedFromInventory()
        {
            var d=PartSizeReferenceFixtures.LegacyCube();
            var a=Make(d,new Cell3(0,0,0),new Cell3(0,0,0),
                Vector3.one*.52f,out Transform at);
            var external=new GameObject("unregistered physical decorative child");
            external.transform.SetParent(at,false);
            external.transform.localPosition=new Vector3(.65f,0,0);
            var extra=external.AddComponent<BoxCollider>();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual("PHYSICAL_ENVELOPE_OUTSIDE_OWNED_CELLS",
                PartSizeNativePhysics.Validate(d,new Cell3(0,0,0),new Cell3(0,0,0),
                    at,new Collider[]{a,extra},new Collider[0],true));
        }

        [UnityTest] public IEnumerator OmittedPhysicalChildCannotBypassAuthoritativeInventory()
        {
            var d=PartSizeReferenceFixtures.LegacyCube();
            var a=Make(d,new Cell3(0,0,0),new Cell3(0,0,0),
                Vector3.one*.52f,out Transform at);
            var external=new GameObject("unlisted colliding child");
            external.transform.SetParent(at,false);
            external.transform.localPosition=new Vector3(.1f,0,0);
            external.AddComponent<BoxCollider>();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual("UNREGISTERED_PHYSICAL_COLLIDER",
                PartSizeNativePhysics.Validate(d,new Cell3(0,0,0),new Cell3(0,0,0),
                    at,new Collider[]{a},new Collider[0],true));
        }

        [UnityTest] public IEnumerator DemoProducesThreePhysicallyCheckedColorStates()
        {
            var go=new GameObject("PARTSIZE demo native");
            created.Add(go);
            var component=go.AddComponent<PartSizeDemoEntry>();
            component.BuildPreview();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(3,component.Receipts.Count);
            foreach(var item in component.Receipts)
                StringAssert.Contains("assertion PASS",item);
        }
    }
}
