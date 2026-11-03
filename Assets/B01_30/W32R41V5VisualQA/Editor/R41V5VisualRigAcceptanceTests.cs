#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoboRougelike.B0130.W32.NativeTests
{
    // Actual original FBX is REQUIRED. Never count missing input as a native PASS.
    public sealed class R41V5VisualRigAcceptanceTests
    {
        const string Incoming="Assets/B01_31/W30R41V5Adapter/Incoming/R41_Turret_v5.fbx";

        static GameObject Build()
        {
            var real=AssetDatabase.LoadAssetAtPath<GameObject>(Incoming);
            if(real==null) Assert.Inconclusive("Original R41 v5 FBX absent. Native visual QA NOT_RUN, not PASS.");
            Type sourceType=null;
            foreach(var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                sourceType=asm.GetType("RoboRougelike.B0131.W30.Editor.B0131W30CreateR41V5AdapterCandidate",false);
                if(sourceType!=null)break;
            }
            Assert.NotNull(sourceType,"Original B01-31 W31 Editor producer missing");
            var method=sourceType.GetMethod("CreateCandidatePreview",BindingFlags.Public|BindingFlags.Static);
            Assert.NotNull(method);
            object[] args={real,""};
            var candidate=method.Invoke(null,args) as GameObject;
            Assert.NotNull(candidate,"Original SHA-baked FBX gate denied: "+args[1]);
            return candidate;
        }

        static void Inspect(Action<Transform,Transform,Transform> action)
        {
            var before=AssetDatabase.GetAssetDependencyHash(Incoming);
            GameObject candidate=null;
            try
            {
                candidate=Build();
                var yaw=candidate.transform.Find("R41_ADAPTER_YAW");
                Assert.NotNull(yaw);
                var pitch=yaw.Find("R41_ADAPTER_PITCH");
                Assert.NotNull(pitch);
                Transform visual=null;
                foreach(var t in candidate.GetComponentsInChildren<Transform>(true))
                    if(t.name=="SOURCE_R41V5_VISUAL_NOT_GAMEPLAY")
                    {
                        Assert.IsNull(visual,"Duplicate imported full-source visual");
                        visual=t;
                    }
                Assert.NotNull(visual,"Original visual clone not present");
                action(yaw,pitch,visual);
                Assert.AreEqual(before,AssetDatabase.GetAssetDependencyHash(Incoming),
                    "Original imported producer FBX must remain unmodified");
            }
            finally {if(candidate!=null)Object.DestroyImmediate(candidate);}
        }

        [Test]
        public void WholeImportedV5TurretMustNotBeChildOfPitch()
        {
            Inspect((yaw,pitch,visual)=>{
                Assert.IsFalse(visual.IsChildOf(pitch),
                    "VISUAL_P0: entire FBX under pitch; fixed mount/hull tilts with barrels");
            });
        }

        [Test]
        public void ImportedV5FixedRootStaysStaticAcrossElevation()
        {
            Inspect((yaw,pitch,visual)=>{
                yaw.localRotation=Quaternion.identity;
                pitch.localRotation=Quaternion.identity;
                var p0=visual.position;
                var q0=visual.rotation;
                foreach(var angle in new[]{-20f,0f,10f,20f})
                {
                    pitch.localRotation=Quaternion.Euler(angle,0f,0f);
                    Assert.Less(Vector3.Distance(p0,visual.position),.001f,
                        "VISUAL_P0: mounting base moves during pitch="+angle);
                    Assert.Less(Quaternion.Angle(q0,visual.rotation),.05f,
                        "VISUAL_P0: mounting base rotates during pitch="+angle);
                }
            });
        }
    }
}
#endif
