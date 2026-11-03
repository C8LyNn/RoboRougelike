#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RoboRougelike.B0131.W29.NativeTests
{
    /// <summary>
    /// Real Unity 6000.3.14f1 EditMode gate on the actual W24 prefab asset.
    /// Does not modify asset source or the canonical Rig/Save. Requires W24
    /// original three-OBJ import + human-consented prefab generation first.
    /// All native cases are AUTHORED, NOT_RUN until Editor.log/NUnit exist.
    /// </summary>
    public sealed class B0131W29DetachedArmorPrefabTests
    {
        private const string OriginalPrefab =
            "Assets/B01_31/W24ArmorStates/Generated/B0131_W24_ArmorVisual_ONLY.prefab";
        private readonly List<Scene> isolatedScenes = new List<Scene>();

        private static Type FindOriginalType(string qualified)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = a.GetType(qualified, false);
                if (t != null) return t;
            }
            Assert.Fail("Original W24/W29 production type missing: " + qualified);
            return null;
        }

        private static MethodInfo RealMethod(string name)
        {
            var editor = FindOriginalType(
                "RoboRougelike.B0131.Art.Editor.B0131W27CreateArmorLiveDemoScene");
            var m = editor.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(m, "Real Unity W29 Editor production method missing: " + name);
            return m;
        }

        private static GameObject Original()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(OriginalPrefab);
            Assert.NotNull(model,
                "W24 original 3-OBJ prefab missing: import W24 package and invoke " +
                "Tools/B01-31/W24/Create Isolated Armor Visual Prefab... in owner-approved sandbox");
            Assert.NotNull(model.GetComponent(FindOriginalType(
                "RoboRougelike.B0131.Art.B0131W24ArmorDamageVisual")),
                "Real W24 three-state adapter missing on original prefab");
            return model;
        }

        private Scene DisposableScene()
        {
            var s = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            isolatedScenes.Add(s);
            return s;
        }

        private GameObject Clone(GameObject prefab, Scene scene)
        {
            var cloned = (GameObject)RealMethod("CreateDetachedVisualClone")
                .Invoke(null, new object[] {prefab, scene});
            Assert.NotNull(cloned);
            Assert.IsFalse(PrefabUtility.IsPartOfPrefabInstance(cloned),
                "W29 clone must be independent before component stripping");
            Assert.AreEqual(scene, cloned.scene);
            return cloned;
        }

        private static bool Strip(GameObject copy) =>
            (bool)RealMethod("StripVisualOnlyPhysics")
                .Invoke(null, new object[] {copy});

        [TearDown]
        public void CloseOnlyOurNewDisposableScenes()
        {
            foreach (var s in isolatedScenes)
                if (s.IsValid() && s.isLoaded)
                    EditorSceneManager.CloseScene(s, true);
            isolatedScenes.Clear();
        }

        [Test]
        public void ActualW24PrefabCopyIsUnpackedAndOriginalAssetHashUnchanged()
        {
            var original = Original();
            var before = AssetDatabase.GetAssetDependencyHash(OriginalPrefab);
            var clone = Clone(original, DisposableScene());
            Assert.AreNotSame(original, clone);
            Assert.IsFalse(PrefabUtility.IsPartOfPrefabInstance(clone));
            Assert.AreEqual(original.GetComponentsInChildren<Renderer>(true).Length,
                            clone.GetComponentsInChildren<Renderer>(true).Length);
            Assert.AreEqual(before, AssetDatabase.GetAssetDependencyHash(OriginalPrefab),
                "Original approved W24 prefab model asset must never be rewritten");
        }

        [Test]
        public void AttachedRigidbodyAndInactiveChildColliderAreRemovedOnlyFromCopy()
        {
            var original = Original();
            var originalBodies = original.GetComponentsInChildren<Rigidbody>(true).Length;
            var originalColliders = original.GetComponentsInChildren<Collider>(true).Length;
            var originalHash = AssetDatabase.GetAssetDependencyHash(OriginalPrefab);
            var clone = Clone(original, DisposableScene());
            var renderers = clone.GetComponentsInChildren<Renderer>(true);
            Assert.Greater(renderers.Length, 0);
            clone.AddComponent<Rigidbody>();
            var child = renderers[renderers.Length - 1].gameObject;
            child.AddComponent<BoxCollider>();
            child.SetActive(false);
            Assert.IsTrue(Strip(clone));
            Assert.AreEqual(0, clone.GetComponentsInChildren<Rigidbody>(true).Length);
            Assert.AreEqual(0, clone.GetComponentsInChildren<Collider>(true).Length);
            Assert.AreEqual(renderers.Length, clone.GetComponentsInChildren<Renderer>(true).Length);
            Assert.AreEqual(originalBodies, original.GetComponentsInChildren<Rigidbody>(true).Length);
            Assert.AreEqual(originalColliders, original.GetComponentsInChildren<Collider>(true).Length);
            Assert.AreEqual(originalHash, AssetDatabase.GetAssetDependencyHash(OriginalPrefab));
        }

        [Test]
        public void OriginalThreeModelReferencesSurviveUnpackingAndPhaseChanges()
        {
            var clone = Clone(Original(), DisposableScene());
            Assert.IsTrue(Strip(clone));
            var visual = clone.GetComponent(FindOriginalType(
                "RoboRougelike.B0131.Art.B0131W24ArmorDamageVisual"));
            var setter = visual.GetType().GetMethod("SetVisualPhase",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(setter);
            var phaseType = setter.GetParameters()[0].ParameterType;
            // Actual W24 phase 0=OK,1=DAMAGED,2=DESTROYED.
            for (int phase = 0; phase < 3; ++phase)
            {
                var success = (bool)setter.Invoke(visual,
                    new[] {Enum.ToObject(phaseType, phase)});
                Assert.IsTrue(success, "Real W24 serialized 3-state object refs lost on unpack");
                var state = visual.GetType().GetProperty("CurrentVisual").GetValue(visual);
                Assert.AreEqual(phase, Convert.ToInt32(state));
            }
            Assert.AreEqual(0, clone.GetComponentsInChildren<Collider>(true).Length);
        }

        [Test]
        public void UnexpectedImportedMonoBehaviourFailsClosedWithoutRemovingColliders()
        {
            var clone = Clone(Original(), DisposableScene());
            var render = clone.GetComponentsInChildren<Renderer>(true)[0];
            var foreign = render.gameObject.AddComponent<B0131W29IntrusiveVisualProbe>();
            var collider = render.gameObject.AddComponent<BoxCollider>();
            int before = clone.GetComponentsInChildren<Collider>(true).Length;
            Assert.IsFalse(Strip(clone), "Foreign script must never enter visual-only sandbox");
            Assert.IsNotNull(foreign);
            Assert.IsNotNull(collider);
            Assert.AreEqual(before, clone.GetComponentsInChildren<Collider>(true).Length);
        }

        [Test]
        public void InvalidSceneOrUnmarkedForeignSourceCannotCreateVisualCopy()
        {
            var source = Original();
            var untouched = AssetDatabase.GetAssetDependencyHash(OriginalPrefab);
            Assert.IsNull(RealMethod("CreateDetachedVisualClone")
                .Invoke(null, new object[] {source, default(Scene)}));
            var foreign = new GameObject("W29_UNMARKED_FOREIGN_TEST_OBJECT");
            try
            {
                Assert.IsNull(RealMethod("CreateDetachedVisualClone")
                    .Invoke(null, new object[] {foreign, DisposableScene()}));
                Assert.AreEqual(untouched, AssetDatabase.GetAssetDependencyHash(OriginalPrefab));
            }
            finally {Object.DestroyImmediate(foreign);}
        }
    }

    /// <summary>Negative-only: never attached to original model asset.</summary>
    public sealed class B0131W29IntrusiveVisualProbe : MonoBehaviour {}
}
#endif
