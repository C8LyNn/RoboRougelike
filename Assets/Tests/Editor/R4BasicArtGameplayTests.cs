using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class R4BasicArtGameplayTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();}
    [UnityTest] public IEnumerator BasicModelIsMountedOnRealVehicleAndFiresFromItsVisibleMuzzle()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=R4WeaponKind.Basic;
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.Single(p=>p.Id==state.Id);
        var type=Type.GetType("RoboRougelike.R4_1.R4ArtWeaponAxisAdapter, Assembly-CSharp");
        var adapter=gun.GetComponent(type);Assert.That(adapter,Is.Not.Null,"Basic art must be attached to the real physical gun.");
        var visualMuzzle=(Transform)type.GetProperty("Muzzle").GetValue(adapter);
        Assert.That(gun.Muzzle,Is.EqualTo(visualMuzzle),"The actual projectile uses the visible barrel origin.");
        Assert.That(gun.GunPivot.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled),Is.True,"Original gun physics collider remains.");
        Assert.That(gun.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4W_Basic_ROOT"),Is.True);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*5;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        foreach(var r in gun.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled))
            foreach(var m in r.sharedMaterials.Where(m=>m!=null))Debug.Log("R4_BASIC_MATERIAL "+m.name+" shader="+m.shader.name);
        Physics.SyncTransforms();assembly.FireGroup(false);
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(960).Within(.01),"The real gun's projectile must hit.");
        Object.Destroy(vehicle.gameObject);Object.Destroy(target);Object.Destroy(paletteObject);Object.Destroy(material);
    }
    [UnityTest] public IEnumerator AutomaticGunKeepsItsExistingVisualAndMuzzle()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=R4WeaponKind.Automatic;
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        var gun=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
        var type=Type.GetType("RoboRougelike.R4_1.R4ArtWeaponAxisAdapter, Assembly-CSharp");
        Assert.That(gun.GetComponent(type),Is.Null);Assert.That(gun.Muzzle,Is.Not.Null);
        Assert.That(gun.GunPivot.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),Is.True);
        Object.Destroy(vehicle.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);yield return null;
    }
    [UnityTest] public IEnumerator RotatedPhysicalMountAimsTheVisualAndStillBlocksOwnArmor()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=R4WeaponKind.Basic;
        state.RotationQuarterTurns=new Vector3Int(0,1,0);
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.Single(p=>p.Id==state.Id);
        var direction=new Vector3(.45f,.25f,1).normalized;
        gun.GunPivot.rotation=Quaternion.LookRotation(direction);
        assembly.AimGuns(gun.GunPivot.position+direction*50);
        Assert.That(Vector3.Dot(gun.Muzzle.forward,direction),Is.GreaterThan(.999f));
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.True,"The empty trajectory must be clear.");
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.SetParent(vehicle.transform,true);
        blocker.transform.position=gun.Muzzle.position+gun.Muzzle.forward*1.4f;blocker.transform.localScale=Vector3.one*.5f;
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.False,"Own armor must block the real muzzle path.");
        assembly.FireGroup(false);
        Assert.That(Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Length,Is.Zero,"Blocked gun must not create a projectile.");
        Object.Destroy(vehicle.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
