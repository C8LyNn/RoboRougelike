using System;
using RoboRougelike.B0131.Art;
using RoboRougelike.B0131.Art.Editor;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

sealed class W29OnlySyntheticIntrusiveScript : MonoBehaviour { public W29OnlySyntheticIntrusiveScript() {} }

static class W28RealSourceHarness
{
    private static int count;
    private static void Check(bool yes,string name)
    {
        if(!yes)throw new InvalidOperationException("W28_REAL_EDITOR_PHYSICS_ISOLATION_RED "+name);
        Console.WriteLine("W28_FIVE_REAL_CSHARP_FILES_SOURCE_PASS "+(++count)+" "+name);
    }

    private static GameObject Visual(bool mark=true,bool render=true)
    {
        var parent=new GameObject("W28_AUTHORED_VISUAL_INSTANCE");
        if(mark)parent.AddComponent<B0131W24ArmorDamageVisual>();
        if(render)
        {
            var mesh=GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.transform.SetParent(parent.transform,false);
        }
        return parent;
    }

    public static void Main()
    {
        var isolated=Visual();
        var physicsOwner=new GameObject("ORIGINAL_P12_REAL_GAMEPLAY_OWNER");
        var p12=physicsOwner.AddComponent<P12PipelineSafeDamageable>();
        p12.Configure(100f,false);
        var gameplayCollider=physicsOwner.AddComponent<BoxCollider>();
        var gameplayBody=physicsOwner.AddComponent<Rigidbody>();
        isolated.AddComponent<Rigidbody>();
        Check(isolated.GetComponentsInChildren<Collider>(true).Length==1,
              "01-actual-primitive-has-physical-collider");
        Check(isolated.GetComponentsInChildren<Rigidbody>(true).Length==1,
              "02-owned-import-can-include-rigidbody");
        Check(B0131W27CreateArmorLiveDemoScene.StripVisualOnlyPhysics(isolated),
              "03-actual-editor-CSharp-sanitizer-accepted-visual-only-clone");
        Check(isolated.GetComponentsInChildren<Collider>(true).Length==0,
              "04-all-original-child-colliders-removed-on-clone");
        Check(isolated.GetComponentsInChildren<Rigidbody>(true).Length==0,
              "05-all-original-child-rigidbodies-removed-on-clone");
        Check(isolated.GetComponentsInChildren<Renderer>(true).Length==1,
              "06-visual-renderer-preserved");
        Check(physicsOwner.GetComponents<Rigidbody>().Length==1 &&
              physicsOwner.GetComponents<Collider>().Length==1 &&
              gameplayBody!=null&&gameplayCollider!=null,
              "07-real-P12-gameplay-physics-owner-untouched");
        Check(!p12.IsDead&&p12.CurrentHp==100,
              "08-real-P12-gameplay-authoritative-HP-untouched");

        var hidden=Visual();hidden.SetActive(false);
        Check(B0131W27CreateArmorLiveDemoScene.StripVisualOnlyPhysics(hidden) &&
              hidden.GetComponentsInChildren<Collider>(true).Length==0,
              "09-inactive-variant-colliders-stripped-too");

        var badGameplay=Visual();
        badGameplay.AddComponent<P12PipelineSafeDamageable>();
        Check(!B0131W27CreateArmorLiveDemoScene.StripVisualOnlyPhysics(badGameplay),
              "10-owned-visual-contains-P12-rejected");
        Check(badGameplay.GetComponentsInChildren<Collider>(true).Length==1,
              "11-real-gameplay-misplacement-fails-with-zero-partial-mutation");

        var joint=Visual();joint.AddComponent<FixedJoint>();
        Check(!B0131W27CreateArmorLiveDemoScene.StripVisualOnlyPhysics(joint),
              "12-physical-joint-rejected");
        Check(joint.GetComponentsInChildren<Collider>(true).Length==1,
              "13-physical-joint-rejected-before-mutating-colliders");

        var noMarker=Visual(false,true);
        Check(!B0131W27CreateArmorLiveDemoScene.StripVisualOnlyPhysics(noMarker),
              "14-unknown-foreign-prefab-root-rejected");
        Check(noMarker.GetComponentsInChildren<Collider>(true).Length==1,
              "15-foreign-source-untouched");

        var noRenderer=Visual(true,false);noRenderer.AddComponent<Rigidbody>();
        Check(!B0131W27CreateArmorLiveDemoScene.StripVisualOnlyPhysics(noRenderer),
              "16-empty-non-rendering-prefab-rejected");
        Check(noRenderer.GetComponents<Rigidbody>().Length==1,
              "17-empty-invalid-visual-untouched");

        Check(!B0131W27CreateArmorLiveDemoScene.StripVisualOnlyPhysics(null!),
              "18-null-visual-rejected");
        Check(B0131W27CreateArmorLiveDemoScene.StripVisualOnlyPhysics(isolated),
              "19-repeat-sanitization-idempotent");

        var actualVisual=isolated.GetComponent<B0131W24ArmorDamageVisual>()!;
        var ok=new GameObject("OK");var damaged=new GameObject("DAMAGED");var destroyed=new GameObject("DESTROYED");
        ok.transform.SetParent(isolated.transform,false);
        damaged.transform.SetParent(isolated.transform,false);
        destroyed.transform.SetParent(isolated.transform,false);
        Check(actualVisual.ConfigureVisuals(ok,damaged,destroyed) && ok.activeSelf,
              "20-real-W24-configures-three-armor-geometries");
        var binder=isolated.AddComponent<B0131W25P12DamageVisualBinding>();
        Check(binder.Bind(p12,actualVisual),"21-real-W25-bind-to-original-P12");
        p12.ApplyDamage(60f,Vector3.zero,Vector3.up);
        Check(binder.RefreshVisual() && damaged.activeSelf && !ok.activeSelf,
              "22-real-P12-damage-affects-visual-without-physics-collider");
        Check(physicsOwner.GetComponents<Collider>().Length==1 &&
              isolated.GetComponentsInChildren<Collider>(true).Length==0,
              "23-actual-owner-physics-stays-original-during-damaged-stage");
        p12.RestoreHealthRatio(1f);
        Check(binder.RefreshVisual() && ok.activeSelf && !damaged.activeSelf,
              "24-real-P12-repair-affects-rendering-only");
        p12.ApplyDamage(200f,Vector3.zero,Vector3.up);
        Check(p12.IsDead && destroyed.activeSelf && !ok.activeSelf,
              "25-real-original-P12-Die-Died-visual-dispatch-with-collider-free-scene");
        // W29 original Unity Editor source: a DETACHED clone must not delete
        // the original three-state W24 prefab mesh or Physics components.
        var originalPrefab=Visual();
        originalPrefab.AddComponent<Rigidbody>();
        var originalColliderCount=originalPrefab.GetComponentsInChildren<Collider>(true).Length;
        Check(originalColliderCount==1 &&
              originalPrefab.GetComponentsInChildren<Rigidbody>(true).Length==1,
              "26-original-prefab-source-has-physical-components-before-copy");
        var copy=B0131W27CreateArmorLiveDemoScene.CreateDetachedVisualClone(
            originalPrefab,new UnityEngine.SceneManagement.Scene{isLoaded=true});
        Check(copy!=null && !object.ReferenceEquals(copy,originalPrefab),
              "27-original-editor-created-independent-clone");
        Check(!UnityEditor.PrefabUtility.IsPartOfPrefabInstance(copy),
              "28-original-unity-editor-unpacks-cloned-prefab-link");
        Check(copy.GetComponentsInChildren<Renderer>(true).Length==
              originalPrefab.GetComponentsInChildren<Renderer>(true).Length,
              "29-original-render-meshes-survive-prefab-copy");
        Check(B0131W27CreateArmorLiveDemoScene.StripVisualOnlyPhysics(copy),
              "30-real-editor-visual-clone-passes-zero-foreign-authority-preflight");
        Check(copy.GetComponentsInChildren<Collider>(true).Length==0 &&
              copy.GetComponentsInChildren<Rigidbody>(true).Length==0,
              "31-only-clone-physics-stripped");
        Check(originalPrefab.GetComponentsInChildren<Collider>(true).Length==originalColliderCount &&
              originalPrefab.GetComponentsInChildren<Rigidbody>(true).Length==1,
              "32-original-prefab-asset-physics-unchanged");
        var foreign=Visual();
        foreign.AddComponent<W29OnlySyntheticIntrusiveScript>();
        var priorForeignColliders=foreign.GetComponentsInChildren<Collider>(true).Length;
        Check(!B0131W27CreateArmorLiveDemoScene.StripVisualOnlyPhysics(foreign) &&
              foreign.GetComponentsInChildren<Collider>(true).Length==priorForeignColliders,
              "33-foreign-script-rejected-without-mutating-original-hierarchy");
        var invalid=B0131W27CreateArmorLiveDemoScene.CreateDetachedVisualClone(
            originalPrefab,new UnityEngine.SceneManagement.Scene{isLoaded=false});
        Check(invalid==null,"34-invalid-isolated-scene-rejected-before-allocating-clone");
        Check(B0131W27CreateArmorLiveDemoScene.CreateDetachedVisualClone(
              new GameObject("unmarked-foreign-prefab"),new UnityEngine.SceneManagement.Scene{isLoaded=true})==null,
              "35-unmarked-foreign-model-refused");
        Check(!p12.CurrentHp.Equals(float.NaN) && physicsOwner.GetComponents<Collider>().Length==1,
              "36-real-authoritative-P12-physics-unchanged-after-prefab-unpack");
        Check(count==36,"37-final-exact-regression-count");
        Console.WriteLine("B0131_W29_REAL_P12_W24_W25_W27_EDITOR_PREFAB_DETACHED_CSHARP_SOURCE_37_CASES_PASS__NATIVE_UNITY_G1_NOT_RUN");
    }
}
