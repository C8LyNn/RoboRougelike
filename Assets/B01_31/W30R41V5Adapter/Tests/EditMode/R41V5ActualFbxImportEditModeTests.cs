#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoboRougelike.B0131.W30.NativeTests
{
    /// <summary>
    /// Real importer acceptance for the REAL external Claude R41 v5 FBX.
    /// The FBX is LOCAL TO OPERATOR; it is not included in the PR and cannot
    /// be faked by creating a cube or model stub in Unity.
    /// Source tests do not satisfy these native tests. Any missing model is
    /// INCONCLUSIVE / must not be counted as a native G1 PASS.
    /// </summary>
    public sealed class R41V5ActualFbxImportEditModeTests
    {
        private const string Incoming =
            "Assets/B01_31/W30R41V5Adapter/Incoming/R41_Turret_v5.fbx";
        private static Type EditorType()
        {
            foreach(var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t=a.GetType(
                    "RoboRougelike.B0131.W30.Editor.B0131W30CreateR41V5AdapterCandidate",false);
                if(t!=null)return t;
            }
            Assert.Fail("W30 REAL Unity Editor source importer type is absent");
            return null;
        }
        private static GameObject RealFbx()
        {
            var fb=AssetDatabase.LoadAssetAtPath<GameObject>(Incoming);
            if(fb==null)
                Assert.Inconclusive(
                    "R41_Turret_v5.fbx missing. B01-60 must place ORIGINAL verified producer " +
                    "FBX in own W30/Incoming in an approved scratch Unity project. " +
                    "A missing model is NOT a PASS.");
            return fb;
        }
        private static GameObject Preview(GameObject source)
        {
            var method=EditorType().GetMethod("CreateCandidatePreview",
                BindingFlags.Public|BindingFlags.Static);
            Assert.NotNull(method);
            object[] args={source, ""};
            var preview=(GameObject)method.Invoke(null,args);
            Assert.NotNull(preview,
                "Original FBX rejected by actual production source; reason="+args[1]);
            return preview;
        }
        private static void CheckPose(Transform yaw,Transform pitch,
                                      Transform left,Transform right,
                                      float y,float p)
        {
            yaw.localRotation=Quaternion.Euler(0,y,0);
            pitch.localRotation=Quaternion.Euler(p,0,0);
            // Real Unity physical transforms, not source shim or pure math.
            Assert.AreEqual(.12f,Vector3.Distance(left.position,right.position),.002f,
                "Two physically distinct muzzles must remain 12cm apart");
            Assert.Less(Vector3.Angle(left.forward,right.forward),.05f,
                "Dual barrel directions must coincide after yaw and pitch");
            Assert.Less(Math.Abs(left.forward.magnitude-1f),.001f);
            Assert.Less(Math.Abs(right.forward.magnitude-1f),.001f);
        }

        [Test]
        public void ActualProducerFbxIsBakedAndNotAnApprovedExistingFallback()
        {
            var fb=RealFbx();
            Assert.AreEqual("R41_Turret_v5",fb.name);
            var importer=AssetImporter.GetAtPath(Incoming) as ModelImporter;
            Assert.NotNull(importer);
            Assert.IsTrue(importer.bakeAxisConversion,
                "Original FBX must be imported with baked axes, do NOT mutate import settings here");
            Assert.AreEqual(Vector3.one,fb.transform.localScale,
                "No global scale hack; 0.55m grid is a gameplay invariant");
        }

        [Test]
        public void ActualV5MarkerLocationsMatchMeasuredOriginalMetadata()
        {
            var fb=RealFbx();
            var assetHash=AssetDatabase.GetAssetDependencyHash(Incoming);
            var instance=Object.Instantiate(fb);
            try
            {
                instance.transform.position=Vector3.zero;
                instance.transform.rotation=Quaternion.identity;
                Transform left=null,right=null;
                int lc=0,rc=0;
                foreach(var t in instance.GetComponentsInChildren<Transform>(true))
                {
                    if(t.name=="Muzzle_L"){left=t;lc++;}
                    if(t.name=="Muzzle_R"){right=t;rc++;}
                }
                Assert.AreEqual(1,lc,"exactly one real v5 left imported marker");
                Assert.AreEqual(1,rc,"exactly one real v5 right imported marker");
                Assert.Less(Vector3.Distance(left.position,
                    new Vector3(-.06f,.135f,.70f)),.01f);
                Assert.Less(Vector3.Distance(right.position,
                    new Vector3(.06f,.135f,.70f)),.01f);
                Assert.AreEqual(assetHash,AssetDatabase.GetAssetDependencyHash(Incoming));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void RealOriginalImportedMeshHasDetachedDualMuzzlesForTwelveUnityYawPitchPoses()
        {
            var model=RealFbx();
            var hash=AssetDatabase.GetAssetDependencyHash(Incoming);
            GameObject candidate=null;
            try
            {
                candidate=Preview(model);
                Assert.IsFalse(PrefabUtility.IsPartOfPrefabInstance(candidate));
                var yaw=candidate.transform.Find("R41_ADAPTER_YAW");
                Assert.NotNull(yaw);
                var pitch=yaw.Find("R41_ADAPTER_PITCH");
                Assert.NotNull(pitch);
                var l=pitch.Find("Muzzle_L");
                var r=pitch.Find("Muzzle_R");
                Assert.NotNull(l);
                Assert.NotNull(r);
                foreach(var y in new[]{0f,45f,-90f,180f})
                foreach(var p in new[]{0f,-20f,10f})
                    CheckPose(yaw,pitch,l,r,y,p);
                Assert.AreEqual(0,candidate.GetComponentsInChildren<Collider>(true).Length);
                Assert.AreEqual(0,candidate.GetComponentsInChildren<Rigidbody>(true).Length);
                Assert.Greater(candidate.GetComponentsInChildren<Renderer>(true).Length,0);
                Assert.AreEqual(hash,AssetDatabase.GetAssetDependencyHash(Incoming));
            }
            finally { if(candidate!=null)Object.DestroyImmediate(candidate); }
        }

        [Test]
        public void NonFbxOrForeignModelRefusedWithoutTouchingExistingTurret()
        {
            var foreign=new GameObject("FOREIGN_SAVED_WORKING_TURRET");
            try
            {
                var method=EditorType().GetMethod("CreateCandidatePreview",
                    BindingFlags.Public|BindingFlags.Static);
                object[] args={foreign,""};
                Assert.IsNull(method.Invoke(null,args),
                    "Cannot create a v5 adapter from live real gameplay turret!");
                StringAssert.Contains("WRONG_ASSET",args[1].ToString());
                Assert.IsNotNull(foreign);
                Assert.IsFalse(AssetDatabase.IsValidFolder(
                    "Assets/B01_31/W30R41V5Adapter/Generated"),
                    "This test must run BEFORE owner-approved manual prefab generation");
            }
            finally { Object.DestroyImmediate(foreign); }
        }
    }
}
#endif
