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
