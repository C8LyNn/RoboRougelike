#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace RoboRougelike.B0131.W30.NativeTests
{
    /// <summary>W31 original Unity native test code; four checks can execute
    /// WITHOUT the missing external R41 v5 FBX. Native G1 NOT_RUN until
    /// Editor.log and NUnit XML from Unity6000.3.14f1 exist.</summary>
    public sealed class R41V5W31PrecloneEditModeTests
    {
        private static MethodInfo OriginalPreflight()
        {
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type=assembly.GetType(
                    "RoboRougelike.B0131.W30.Editor.B0131W30CreateR41V5AdapterCandidate",false);
                if(type!=null)
                {
                    var method=type.GetMethod("TryValidateSourceVisualHierarchy",
                        BindingFlags.Public|BindingFlags.Static);
                    Assert.NotNull(method,"Real W31 production pre-clone gate missing");
                    return method;
                }
            }
            Assert.Fail("Real W31 Unity Editor production source assembly missing");
            return null;
        }

        private static bool Gate(GameObject root,out string why)
        {
            object[] args={root,""};
            bool ok=(bool)OriginalPreflight().Invoke(null,args);
            why=args[1] as string;
            return ok;
        }

        private static GameObject VisualFixture()
        {
            var root=GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name="W31_DISPOSABLE_NATIVE_FIXTURE";
            new GameObject("Muzzle_L").transform.SetParent(root.transform,false);
            new GameObject("Muzzle_R").transform.SetParent(root.transform,false);
            return root;
        }

        [Test]
        public void W31OriginalFbxPathAnchoredToUnityProjectNotProcessCwd()
        {
            Assert.IsTrue(R41V5AssetPaths.TryResolveFbxPath(
                Application.dataPath,R41V5AssetPaths.ExactV5AssetPath,
                out var full,out var refusal),refusal);
            Assert.IsTrue(System.IO.Path.IsPathRooted(full));
            Assert.IsTrue(full.Replace('\\','/').EndsWith(
                "/Assets/B01_31/W30R41V5Adapter/Incoming/R41_Turret_v5.fbx",
                StringComparison.Ordinal));
            Assert.IsFalse(R41V5AssetPaths.TryResolveFbxPath(
                Application.dataPath,"Assets/../Other/R41_Turret_v5.fbx",
                out _,out _));
        }

        [Test]
        public void W31OriginalGameplayMonoBehaviourRefusedBeforeAnyClone()
        {
            var source=VisualFixture();
            try
            {
                Type script=null;
                foreach(var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    script=a.GetType(
                        "RoboRougelike.B0131.Art.B0131W24ArmorDamageVisual",false);
                    if(script!=null)break;
                }
                Assert.NotNull(script,"W24 original runtime model dependency missing");
                var added=source.AddComponent(script);
                int originalColliders=source.GetComponentsInChildren<Collider>(true).Length;
                Assert.IsFalse(Gate(source,out var rejection));
                Assert.AreEqual("FOREIGN_SCRIPT_BEFORE_CLONE",rejection);
                Assert.IsNotNull(added);
                Assert.AreEqual(originalColliders,
                    source.GetComponentsInChildren<Collider>(true).Length,
                    "Unsafe source refused without any visual physics mutation");
            }
            finally {Object.DestroyImmediate(source);}
        }

        [Test]
        public void W31ImportedJointRefusedBeforeCloneAndLeavesPhysicsUntouched()
        {
            var source=VisualFixture();
            try
            {
                var rigidbody=source.AddComponent<Rigidbody>();
                var joint=source.AddComponent<FixedJoint>();
                int originalColliders=source.GetComponentsInChildren<Collider>(true).Length;
                Assert.IsFalse(Gate(source,out var reason));
                Assert.AreEqual("FOREIGN_PHYSICAL_JOINT_BEFORE_CLONE",reason);
                Assert.IsNotNull(rigidbody);
                Assert.IsNotNull(joint);
                Assert.AreEqual(originalColliders,
                    source.GetComponentsInChildren<Collider>(true).Length);
            }
            finally {Object.DestroyImmediate(source);}
        }

        [Test]
        public void W31DuplicateOrMissingMuzzleFailsClosedWithoutMutatingSource()
        {
            var source=VisualFixture();
            try
            {
                Assert.IsTrue(Gate(source,out var reason),reason);
                var dup=new GameObject("Muzzle_L");
                dup.transform.SetParent(source.transform,false);
                Assert.IsFalse(Gate(source,out reason));
                StringAssert.Contains("DUPLICATE_FBX_TRANSFORM_Muzzle_L",reason);
                Object.DestroyImmediate(dup);
                Object.DestroyImmediate(source.transform.Find("Muzzle_R").gameObject);
                Assert.IsFalse(Gate(source,out reason));
                StringAssert.Contains("MISSING_FBX_TRANSFORM_Muzzle_R",reason);
                Assert.IsNotNull(source.GetComponent<Collider>(),
                    "No source collider may be destroyed on preflight reject");
            }
            finally {Object.DestroyImmediate(source);}
        }
    }
}
#endif
