// W10 independent authored original PhysX PlayMode test, not executed in GitHub hosted source CI.
using System.Collections;
using B01.DebrisPlayground001;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.DockHardening001.Tests
{
    public class W10PublicDockGuardPlayModeTests
    {
        [UnityTest]
        public IEnumerator DirectPublicReattachFailsWhenDockBlockedAndThenAllowsCleanDock()
        {
            var host=new GameObject("W10 original public API security native QA host");
            var demo=host.AddComponent<DebrisRigPlayground>();
            demo.BuildDemo();
            for(int index=0;index<2;index++)
            {
                Assert.IsTrue(demo.Detach(index));
                Rigidbody body=demo.PartBody(index);
                Transform oldParent=body.transform.parent;
                Vector3 oldLinear=body.linearVelocity;
                bool oldKinematic=body.isKinematic;
                int oldApplied=demo.AppliedCount(index);
                var spec=DebrisRigDemoCatalog.ByIndex(index);
                var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name="W10 hidden-layer real blocker (layer 8)";
                obstacle.layer=8;
                obstacle.transform.position=new Vector3((float)(spec.RootX+spec.PartOffsetX),1.2f,0);
                obstacle.transform.localScale=new Vector3(.25f,.25f,.25f);
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
                Assert.IsFalse(demo.Reattach(index),
                    "Direct external W07 public method must reject a real layer-8 mount obstacle");
                Assert.IsTrue(demo.IsDetached(index));
                Assert.AreSame(oldParent,body.transform.parent);
                Assert.AreEqual(oldLinear,body.linearVelocity);
                Assert.AreEqual(oldKinematic,body.isKinematic);
                Assert.AreEqual(oldApplied,demo.AppliedCount(index));
                Object.Destroy(obstacle);
                yield return new WaitForFixedUpdate();
                Physics.SyncTransforms();
                Assert.IsTrue(demo.Reattach(index),"unblocked actual target must allow exactly one reattach");
                Assert.IsFalse(demo.IsDetached(index));
                Assert.IsFalse(demo.Reattach(index),"repeated public direct dock rejected");
                Assert.IsTrue(demo.Detach(index),"detached ID must rearm only after valid dock");
            }
            Object.Destroy(host);
            yield return null;
        }
    }
}
