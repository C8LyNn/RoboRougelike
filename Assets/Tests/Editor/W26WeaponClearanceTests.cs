using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class W26WeaponClearanceTests
{
    R1RunController Run=>R1RunController.Instance;
    static IEnumerator Until(Func<bool> ready) {
        float end=Time.realtimeSinceStartup+20f;
        while(!ready()&&Time.realtimeSinceStartup<end) yield return null;
        Assert.That(ready(),Is.True,"game transition timed out");
    }
    [UnitySetUp] public IEnumerator Setup() {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return Until(()=>R1RunController.Instance?.Progress!=null);
    }
    [UnityTearDown] public IEnumerator Cleanup() { if(Application.isPlaying) yield return new ExitPlayMode(); }
    [UnityTest] public IEnumerator CrowdedPivotIgnoresForeignCollidersButStillRejectsOwnPart()
    {CheckCrowdedFunctionalPlacement(R4WeaponKind.Basic,false);yield return null;}
    [UnityTest] public IEnumerator CrowdedBarrelIgnoresForeignCollidersButStillRejectsOwnPart()
    {CheckCrowdedFunctionalPlacement(R4WeaponKind.Basic,true);yield return null;}
    [UnityTest] public IEnumerator CrowdedSawIgnoresForeignCollidersButStillRejectsOwnPart()
    {CheckCrowdedFunctionalPlacement(R4WeaponKind.RotarySaw,false);yield return null;}
    [UnityTest] public IEnumerator ClearanceBeyondCachedCapacityStillFindsOwnBlocker()
    {CheckCrowdedFunctionalPlacement(R4WeaponKind.Basic,false,4100);yield return null;}
    [UnityTest] public IEnumerator BarrelBeyondCachedCapacityStillFindsOwnBlocker()
    {CheckCrowdedFunctionalPlacement(R4WeaponKind.Basic,true,4100);yield return null;}
    static void CheckCrowdedFunctionalPlacement(R4WeaponKind weapon,bool barrel,int crowd=270)
    {
        var owner=new GameObject("Clearance owner");owner.transform.position=new Vector3(1000,1000,1000);
        var gate=owner.AddComponent<R3BuildValidation>();var probe=new GameObject("Clearance proposed gun");probe.transform.SetParent(owner.transform,false);
        var pivot=new GameObject("Obrót broni");pivot.transform.SetParent(probe.transform,false);
        var muzzle=new GameObject(weapon==R4WeaponKind.RotarySaw?"Środek piły":"Wylot");muzzle.transform.SetParent(pivot.transform,false);muzzle.transform.localPosition=Vector3.forward;
        var clutter=new GameObject("Foreign clearance clutter");
        Vector3 point=weapon==R4WeaponKind.RotarySaw?muzzle.transform.position:barrel?Vector3.Lerp(pivot.transform.position,muzzle.transform.position,.5f):pivot.transform.position;
        try
        {
            for(int i=0;i<crowd;i++)
            {
                var item=new GameObject("Foreign "+i);item.transform.SetParent(clutter.transform,false);item.transform.position=point;
                item.AddComponent<SphereCollider>().radius=.01f;
            }
            Physics.SyncTransforms();
            if(barrel)
                Assert.That(Physics.SphereCastNonAlloc(pivot.transform.position,.035f,Vector3.forward,new RaycastHit[256],1f,~0,QueryTriggerInteraction.Ignore),Is.EqualTo(256));
            else Assert.That(Physics.OverlapSphereNonAlloc(point,.035f,new Collider[256],~0,QueryTriggerInteraction.Ignore),Is.EqualTo(256));
            var candidate=new R3PartState(Vector3Int.zero,R3PartKind.Gun){Weapon=weapon};
            var check=typeof(R3BuildValidation).GetMethod("FunctionalWeaponPlacementIssue",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Assert.That(check,Is.Not.Null);
            string result=(string)check.Invoke(gate,new object[]{candidate,probe});
            Assert.That(result,Is.Null,"Foreign colliders cannot block this owner-only clearance rule.");
            var buffer=typeof(R3BuildValidation).GetField(barrel?"functionalCast":"functionalOverlap",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var cached=(System.Array)buffer.GetValue(gate);Assert.That(cached.Length,Is.GreaterThan(256));
            var own=new GameObject("Own physical blocker");own.transform.SetParent(owner.transform,false);own.transform.position=point;
            own.AddComponent<SphereCollider>().radius=.02f;Physics.SyncTransforms();
            result=(string)check.Invoke(gate,new object[]{candidate,probe});
            Assert.That(result,Is.Not.Null,"An owned obstacle must still block placement even beyond the original buffer.");
            Assert.That(buffer.GetValue(gate),Is.SameAs(cached),"Repeated checks must reuse the grown buffer.");
        }
        finally{UnityEngine.Object.DestroyImmediate(clutter);UnityEngine.Object.DestroyImmediate(owner);}
    }
    [UnityTest] public IEnumerator RealGunDoesNotFireThroughOwnArmorAndResumesWhenClear()
    {
        Assert.That(Run.StartRun(),Is.True);
        Assert.That(Run.ChooseRoute(0),Is.True);
        yield return Until(()=>Run.Progress.State==R1RunState.Combat && Run.Encounter!=null && Run.Player!=null && !Run.Transitioning);
        Run.Encounter.SetCombatActive(false);
        var assembly=Run.Player.GetComponent<R3VehicleAssembly>();
        var kind=assembly.Model.WeaponGroupA;
        Assert.That(kind.HasValue,Is.True);
        var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun&&p.Weapon==kind.Value);
        Assert.That(gun.Muzzle,Is.Not.Null);
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.name="W26 own armor witness";
        blocker.transform.SetParent(assembly.transform,true);
        blocker.transform.position=Vector3.Lerp(gun.GunPivot.position,gun.Muzzle.position,.45f);
        blocker.transform.rotation=gun.Muzzle.rotation;
        blocker.transform.localScale=Vector3.one*.12f;
        Physics.SyncTransforms();
        assembly.FireGroup(false);
        Assert.That(UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Any(p=>p.PartId==gun.Id),Is.False,"armor crossing the barrel before the muzzle must suppress the shot");
        blocker.transform.position=gun.Muzzle.position+gun.Muzzle.forward*0.6f;
        blocker.transform.rotation=gun.Muzzle.rotation;
        blocker.transform.localScale=Vector3.one*.3f;
        Physics.SyncTransforms();
        float cooldown=gun.NextFireTime;
        assembly.FireGroup(false);
        Assert.That(UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Any(p=>p.PartId==gun.Id),Is.False,"own armor must suppress the real gun");
        Assert.That(gun.NextFireTime,Is.EqualTo(cooldown),"blocked shot must not consume cooldown");
        var cover=GameObject.CreatePrimitive(PrimitiveType.Cube);
        cover.name="W26 external cover witness";
        cover.transform.position=gun.Muzzle.position+gun.Muzzle.forward*0.3f;
        cover.transform.rotation=gun.Muzzle.rotation;cover.transform.localScale=Vector3.one*.1f;
        Physics.SyncTransforms();assembly.FireGroup(false);
        Assert.That(UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Any(p=>p.PartId==gun.Id),Is.True,"external cover before owned armor must receive a shot");
        foreach(var shell in UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>p.PartId==gun.Id))UnityEngine.Object.DestroyImmediate(shell.gameObject);
        gun.NextFireTime=0;cover.SetActive(false);blocker.SetActive(false);Physics.SyncTransforms();
        assembly.FireGroup(false);
        Assert.That(UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Any(p=>p.PartId==gun.Id),Is.True,"gun must fire again after the path clears");
        foreach(var shell in UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Where(p=>p.PartId==gun.Id))UnityEngine.Object.DestroyImmediate(shell.gameObject);
        gun.NextFireTime=0;assembly.transform.position=Vector3.up*1000f;
        blocker.SetActive(true);
        blocker.transform.position=gun.Muzzle.position+gun.Muzzle.forward*(R4WeaponDefinition.Get(gun.Weapon).Speed*Run.Player.Controller.PhysicalShellLifetime+2f);
        Physics.SyncTransforms();assembly.FireGroup(false);
        Assert.That(UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Any(p=>p.PartId==gun.Id),Is.True,"owned geometry beyond projectile lifetime must not block fire");
        UnityEngine.Object.Destroy(blocker);UnityEngine.Object.Destroy(cover);
    }
}
