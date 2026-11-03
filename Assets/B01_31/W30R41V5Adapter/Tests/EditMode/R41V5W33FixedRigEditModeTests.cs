#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoboRougelike.B0131.W30.NativeTests
{
    // SOURCE validator tests use disposable primitives. Actual FBX acceptance
    // requires original SHA-pinned external asset; never convert absent to PASS.
    public sealed class R41V5W33FixedRigEditModeTests
    {
        private const string Incoming =
            "Assets/B01_31/W30R41V5Adapter/Incoming/R41_Turret_v5.fbx";

        private static Type EditorType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(
                    "RoboRougelike.B0131.W30.Editor.B0131W30CreateR41V5AdapterCandidate", false);
                if (type != null) return type;
            }
            Assert.Fail("Original B01-31 Unity Editor producer absent");
            return null;
        }

        private static bool SourceGate(GameObject original, out string refusal)
        {
            var method = EditorType().GetMethod("TryValidateSourceRigHierarchy",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            object[] arguments = { original, "" };
            var accepted = (bool)method.Invoke(null, arguments);
            refusal = (string)arguments[1];
            return accepted;
        }

        private static GameObject Cube(string name, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject SourceFixture()
        {
            var fixedBase = Cube("SOURCE_TEST_FIXED_MOUNT");
            var yaw = Cube("R41_Yaw", fixedBase.transform);
            var pitch = Cube("R41_Pitch", yaw.transform);
            new GameObject("Muzzle_L").transform.SetParent(pitch.transform, false);
            new GameObject("Muzzle_R").transform.SetParent(pitch.transform, false);
            return fixedBase;
        }

        private static Transform FindUnique(Transform parent, string name)
        {
            Transform found = null;
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != name) continue;
                Assert.IsNull(found, "Duplicate Unity QA fixture marker " + name);
                found = t;
            }
            Assert.NotNull(found, "Missing Unity QA marker " + name);
            return found;
        }

        [Test]
        public void RigGateAcceptsThreeExplicitVisualBranchesOnly()
        {
            var source = SourceFixture();
            try
            {
                Assert.IsTrue(SourceGate(source, out var why), why);
                Assert.IsNotNull(source.GetComponent<Collider>(),
                    "Source admission must never strip original source physics");
            }
            finally { Object.DestroyImmediate(source); }
        }

        [Test]
        public void RigGateRefusesMissingYawAndWrongPitchAncestry()
        {
            var source = SourceFixture();
            try
            {
                var yaw = FindUnique(source.transform, "R41_Yaw");
                var pitch = FindUnique(source.transform, "R41_Pitch");
                yaw.name = "UNAPPROVED_ORIGINAL_YAW";
                Assert.IsFalse(SourceGate(source, out var why));
                StringAssert.Contains("MISSING_FBX_TRANSFORM_R41_Yaw", why);
                yaw.name = "R41_Yaw";
                pitch.SetParent(source.transform, true);
                Assert.IsFalse(SourceGate(source, out why));
                Assert.AreEqual("UNPROVEN_R41_YAW_PITCH_HIERARCHY", why);
            }
            finally { Object.DestroyImmediate(source); }
        }

        [Test]
        public void RigGateRefusesDetachedMuzzlesOrAbsentFixedMount()
        {
            var source = SourceFixture();
            try
            {
                var left = FindUnique(source.transform, "Muzzle_L");
                left.SetParent(source.transform, true);
                Assert.IsFalse(SourceGate(source, out var why));
                Assert.AreEqual("MUZZLE_MARKERS_OUTSIDE_PITCH_BRANCH", why);
                left.SetParent(FindUnique(source.transform, "R41_Pitch"), true);
                Object.DestroyImmediate(source.GetComponent<MeshRenderer>());
                Assert.IsFalse(SourceGate(source, out why));
                Assert.AreEqual("UNPROVEN_FIXED_BASE_YAW_AND_PITCH_VISUAL_SEGMENTS", why);
            }
            finally { Object.DestroyImmediate(source); }
        }

        [Test]
        public void RealShaPinnedV5FbxKeepsBaseFixedAndTracksBothMuzzles()
        {
            var real = AssetDatabase.LoadAssetAtPath<GameObject>(Incoming);
            if (real == null)
                Assert.Inconclusive("Exact external Claude R41 v5 FBX absent; no native visual PASS.");
            var importer = AssetImporter.GetAtPath(Incoming) as ModelImporter;
            Assert.NotNull(importer);
            Assert.IsTrue(importer.bakeAxisConversion);
            var before = AssetDatabase.GetAssetDependencyHash(Incoming);
            var create = EditorType().GetMethod("CreateCandidatePreview",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(create);
            GameObject candidate = null;
            try
            {
                object[] args = { real, "" };
                candidate = (GameObject)create.Invoke(null, args);
                Assert.NotNull(candidate, "Verified original FBX preview refused: " + args[1]);
                var yaw = candidate.transform.Find("R41_ADAPTER_YAW");
                Assert.NotNull(yaw);
                var pitch = yaw.Find("R41_ADAPTER_PITCH");
                Assert.NotNull(pitch);
                var visual = candidate.transform.Find("SOURCE_R41V5_VISUAL_NOT_GAMEPLAY");
                Assert.NotNull(visual, "Imported fixed base must remain direct root child");
                Assert.IsFalse(visual.IsChildOf(yaw));
                Assert.IsFalse(visual.IsChildOf(pitch));
                var sourceYaw = FindUnique(yaw, "R41_Yaw");
                var sourcePitch = FindUnique(pitch, "R41_Pitch");
                Assert.IsFalse(sourcePitch.IsChildOf(sourceYaw));
                Assert.IsTrue(sourceYaw.IsChildOf(yaw));
                Assert.IsTrue(sourcePitch.IsChildOf(pitch));
                var srcL = FindUnique(sourcePitch, "Muzzle_L");
                var srcR = FindUnique(sourcePitch, "Muzzle_R");
                var wrapL = pitch.Find("Muzzle_L");
                var wrapR = pitch.Find("Muzzle_R");
                Assert.NotNull(wrapL);
                Assert.NotNull(wrapR);
                var fixedPosition = visual.position;
                var fixedRotation = visual.rotation;
                foreach (var y in new[] { 0f, 45f, -90f, 180f })
                foreach (var p in new[] { -20f, 0f, 10f })
                {
                    yaw.localRotation = Quaternion.Euler(0f, y, 0f);
                    pitch.localRotation = Quaternion.Euler(p, 0f, 0f);
                    Assert.Less(Vector3.Distance(visual.position, fixedPosition), .001f);
                    Assert.Less(Quaternion.Angle(visual.rotation, fixedRotation), .05f);
                    Assert.Less(Vector3.Distance(srcL.position, wrapL.position), .011f);
                    Assert.Less(Vector3.Distance(srcR.position, wrapR.position), .011f);
                    Assert.Greater(Vector3.Distance(srcL.position, srcR.position), .118f);
                    Assert.Less(Vector3.Distance(srcL.position, srcR.position), .122f);
                }
                Assert.AreEqual(0, candidate.GetComponentsInChildren<Rigidbody>(true).Length);
                Assert.AreEqual(0, candidate.GetComponentsInChildren<Collider>(true).Length);
                Assert.AreEqual(before, AssetDatabase.GetAssetDependencyHash(Incoming));
            }
            finally { if (candidate != null) Object.DestroyImmediate(candidate); }
        }
    }
}
#endif
