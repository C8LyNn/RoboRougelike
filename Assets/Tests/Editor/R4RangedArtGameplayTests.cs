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

public sealed class R4RangedArtGameplayTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();}
    [UnityTest] public IEnumerator SuppliedRangedModelsAttachToRealVehicleAndKeepPhysicalColliders()
    {
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var type=Type.GetType("RoboRougelike.R4_1.R4ArtWeaponAxisAdapter, Assembly-CSharp");
        foreach(var weapon in new[]{R4WeaponKind.Heavy,R4WeaponKind.Shotgun,R4WeaponKind.Railgun,
            R4WeaponKind.RocketLauncher,R4WeaponKind.BeamCutter,R4WeaponKind.EmpEmitter})
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
            var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=weapon;state.RotationQuarterTurns=new Vector3Int(0,1,0);
            var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
            vehicle.GetComponent<Rigidbody>().isKinematic=true;
            var assembly=vehicle.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.Single(p=>p.Id==state.Id);
            var adapter=gun.GetComponent(type);Assert.That(adapter,Is.Not.Null,weapon+" must be attached to its physical gun.");
            Assert.That(gun.Muzzle,Is.EqualTo(type.GetProperty("Muzzle").GetValue(adapter)),weapon+" uses the visible muzzle.");
            Assert.That(gun.GunPivot.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled),Is.True,weapon+" keeps the physical collider.");
            var root=gun.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4W_"+weapon+"_ROOT");
            var renderers=root.GetComponentsInChildren<Renderer>(true);Assert.That(renderers.Length,Is.GreaterThan(0),weapon.ToString());
            foreach(var renderer in renderers)foreach(var mat in renderer.sharedMaterials.Where(m=>m!=null))
                Assert.That(mat.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"),weapon+" "+mat.name);
            var direction=new Vector3(.4f,weapon==R4WeaponKind.EmpEmitter?0:.25f,1).normalized;
            gun.GunPivot.rotation=Quaternion.LookRotation(direction);assembly.AimGuns(gun.GunPivot.position+direction*50);
            Assert.That(Vector3.Dot(gun.Muzzle.forward,direction),Is.GreaterThan(.999f),weapon+" muzzle follows the physical aim.");
            Object.Destroy(vehicle.gameObject);yield return null;
        }
        Object.Destroy(paletteObject);Object.Destroy(material);
    }
    [UnityTest] public IEnumerator HeavyArtUsesVisibleMuzzleForActualProjectileAndEnergy()
    {
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=R4WeaponKind.Heavy;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.Single(p=>p.Id==state.Id);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*5;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var energy=vehicle.GetComponent<VehicleEnergyBank>();float before=energy.Current;
        Physics.SyncTransforms();assembly.FireGroup(false);
        Assert.That(energy.Current,Is.EqualTo(before-2.5f).Within(.001));
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(900).Within(.01));
        Object.Destroy(vehicle.gameObject);Object.Destroy(target);Object.Destroy(paletteObject);Object.Destroy(material);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
