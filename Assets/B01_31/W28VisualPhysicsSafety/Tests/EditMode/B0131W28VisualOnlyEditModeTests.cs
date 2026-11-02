#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoboRougelike.B0131.W28.NativeTests
{
    /// <summary>
    /// Genuine Unity EditMode tests for the W28 change to W27 authored scene
    /// generator. Invokes actual B01-31 Editor utility via reflection, since
    /// separate test asmdef cannot directly reference Assembly-CSharp-Editor.
    /// Not runnable as native G1 without actual Unity Editor 6000.3.14f1.
    /// </summary>
    public sealed class B0131W28VisualOnlyEditModeTests
    {
        private readonly List<GameObject> created = new List<GameObject>();

        private static Type ActualType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var found = assembly.GetType(fullName, false);
                if (found != null) return found;
            }
            Assert.Fail("Actual original runtime/Editor type missing: " + fullName);
            return null;
        }
        private static bool Strip(GameObject root)
        {
            var editor = ActualType("RoboRougelike.B0131.Art.Editor.B0131W27CreateArmorLiveDemoScene");
            var method = editor.GetMethod("StripVisualOnlyPhysics",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Real W28 production Editor helper method absent");
            return (bool)method.Invoke(null, new object[] {root});
        }
        private GameObject Root()
        {
            var root = new GameObject("W28_OWN_ISOLATED_VISUAL_CLONE");
            created.Add(root);
            root.AddComponent(ActualType("RoboRougelike.B0131.Art.B0131W24ArmorDamageVisual"));
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "original-3D-render-child";
            mesh.transform.SetParent(root.transform, false);
            // Standard original Unity primitive has actual MeshRenderer+BoxCollider.
            return root;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var go in created)
                if (go != null) Object.DestroyImmediate(go);
            created.Clear();
        }

        [Test]
        public void OriginalImportedMeshColliderRemovedOnOwnedVisualCloneOnly()
        {
            var visual = Root();
            Assert.Greater(visual.GetComponentsInChildren<Collider>(true).Length, 0);
            Assert.IsTrue(Strip(visual));
            Assert.AreEqual(0, visual.GetComponentsInChildren<Collider>(true).Length);
            Assert.AreEqual(0, visual.GetComponentsInChildren<Rigidbody>(true).Length);
            Assert.Greater(visual.GetComponentsInChildren<Renderer>(true).Length, 0);
        }

        [Test]
        public void OriginalImportedRigidbodiesCannotAlterP12GamePhysics()
        {
            var visual = Root();
            visual.AddComponent<Rigidbody>();
            var outside = new GameObject("REAL_P12_SOURCE_OUTSIDE_VISUAL");
            created.Add(outside);
            var gameplayBody = outside.AddComponent<Rigidbody>();
            var gameplayCollider = outside.AddComponent<BoxCollider>();
            outside.AddComponent(ActualType("RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable"));
            Assert.IsTrue(Strip(visual));
            Assert.AreEqual(0, visual.GetComponentsInChildren<Rigidbody>(true).Length);
            Assert.AreEqual(0, visual.GetComponentsInChildren<Collider>(true).Length);
            Assert.IsNotNull(gameplayBody);
            Assert.IsNotNull(gameplayCollider);
            Assert.AreEqual(1, outside.GetComponents<Rigidbody>().Length);
            Assert.AreEqual(1, outside.GetComponents<Collider>().Length);
        }

        [Test]
        public void RefuseVisualTreeContainingOriginalGameplayP12WithoutAnyMutation()
        {
            var visual = Root();
            visual.AddComponent(ActualType("RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable"));
            int before = visual.GetComponentsInChildren<Collider>(true).Length;
            Assert.IsFalse(Strip(visual), "Must never disable real P12 gameplay root physics");
            Assert.AreEqual(before, visual.GetComponentsInChildren<Collider>(true).Length);
        }

        [Test]
        public void RefusePhysicsJointsWithoutPartialMutation()
        {
            var visual = Root();
            visual.AddComponent<FixedJoint>();
            int before = visual.GetComponentsInChildren<Collider>(true).Length;
            Assert.IsFalse(Strip(visual));
            Assert.AreEqual(before, visual.GetComponentsInChildren<Collider>(true).Length);
        }

        [Test]
        public void RefuseUnrenderableOrUnmarkedRootBeforeMutation()
        {
            var noVisual = new GameObject("wrong-root");
            created.Add(noVisual);
            var collider = noVisual.AddComponent<BoxCollider>();
            Assert.IsFalse(Strip(noVisual));
            Assert.IsNotNull(collider);
            var visual = new GameObject("marker-but-no-real-mesh");
            created.Add(visual);
            visual.AddComponent(ActualType("RoboRougelike.B0131.Art.B0131W24ArmorDamageVisual"));
            visual.AddComponent<Rigidbody>();
            Assert.IsFalse(Strip(visual));
            Assert.AreEqual(1, visual.GetComponentsInChildren<Rigidbody>(true).Length);
        }
    }
}
#endif
