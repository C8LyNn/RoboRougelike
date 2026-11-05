using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class W27SpecialWeaponClearanceTests
{
    static bool DirectFire(R3MountedPart gun) => gun.Weapon switch {
        R4WeaponKind.Shotgun=>gun.GetComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>().Fire(),
        R4WeaponKind.Railgun=>gun.GetComponent<RoboRougelike.WeaponIntegration.RRLRailgunBinding>().Fire(),
        R4WeaponKind.RocketLauncher=>gun.GetComponent<RoboRougelike.WeaponIntegration.RRLRocketBinding>().Fire(), _=>false };
    [UnityTest] public IEnumerator ShotgunRejectsSpreadPelletCrossingOwnArmor()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var paletteObject=new GameObject("W27 spread palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        R3PartState state=null;foreach(var candidate in model.Parts)if(candidate.Kind==R3PartKind.Gun){state=candidate;break;}
        Assert.That(state,Is.Not.Null);state.Weapon=R4WeaponKind.Shotgun;
        var rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        yield return null;
        Assert.That(rig,Is.Not.Null,"rig survived");
        Assert.That(state,Is.Not.Null,"state survived");
        Assert.That(rig.GetComponent<R3VehicleAssembly>(),Is.Not.Null,"assembly survived");
        R3MountedPart gun=null;foreach(var candidate in rig.GetComponent<R3VehicleAssembly>().Parts)if(candidate.Id==state.Id){gun=candidate;break;}
        Assert.That(gun,Is.Not.Null,"gun survived");
        Assert.That(gun.Muzzle,Is.Not.Null,"muzzle survived");
        var direction=Quaternion.AngleAxis(7,gun.Muzzle.up)*gun.Muzzle.forward;
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.SetParent(rig.transform,true);
        blocker.transform.position=gun.Muzzle.position+direction*5f;blocker.transform.localScale=Vector3.one*.25f;
        Physics.SyncTransforms();
        var pellet=new RoboRougelike.WPN001Shotgun.ShotgunPelletSpawn(0,0,gun.Muzzle.position,direction,Vector3.zero,58,9,40,2,rig.GetComponent<Rigidbody>(),rig.gameObject);
        var binding=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
        Assert.That(binding.TrySpawnVolley(new[]{pellet}),Is.False,"a pellet crossing owned armor must be rejected even when the centre line is clear");
        blocker.SetActive(false);Physics.SyncTransforms();
        Assert.That(binding.TrySpawnVolley(new[]{pellet}),Is.True,"unobstructed pellet must spawn");
        Object.Destroy(rig.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
    [UnityTest] public IEnumerator ProjectileWeaponsSuppressBlockedFireWithoutSpendingAndResumeWhenClear()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var paletteObject=new GameObject("W27 palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var kinds=new[]{R4WeaponKind.Shotgun,R4WeaponKind.Railgun,R4WeaponKind.RocketLauncher};
        for(int index=0;index<kinds.Length;index++)
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
            var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=kinds[index];
            var rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(index*40,20,0),Quaternion.identity);
            yield return null;
            var assembly=rig.GetComponent<R3VehicleAssembly>();
            var gun=assembly.Parts.Single(p=>p.Id==state.Id);
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="W27 owned armor witness";
            blocker.transform.SetParent(rig.transform,true);
            blocker.transform.position=gun.Muzzle.position+gun.Muzzle.forward*.6f;
            blocker.transform.rotation=gun.Muzzle.rotation;blocker.transform.localScale=Vector3.one*.3f;
            Physics.SyncTransforms();int ammo=state.Ammo;float ready=state.DeviceReadyAt;
            Assert.That(DirectFire(gun),Is.False,kinds[index]+" direct blocked shot must be denied");
            Assert.That(state.Ammo,Is.EqualTo(ammo),kinds[index]+" direct blocked shot consumed ammo");
            Assert.That(state.DeviceReadyAt,Is.EqualTo(ready),kinds[index]+" direct blocked shot consumed cooldown");
            assembly.FireGroup(false);
            Assert.That(state.Ammo,Is.EqualTo(ammo),kinds[index]+" blocked shot consumed ammo");
            Assert.That(state.DeviceReadyAt,Is.EqualTo(ready),kinds[index]+" blocked shot consumed cooldown");
            blocker.SetActive(false);Physics.SyncTransforms();assembly.FireGroup(false);
            Assert.That(state.Ammo,Is.EqualTo(ammo-1),kinds[index]+" did not fire after clearing");
            Assert.That(state.DeviceReadyAt,Is.GreaterThan(ready),kinds[index]+" cooldown not started after shot");
            Object.Destroy(rig.gameObject);R1EncounterManager.ClearTransientObjects();yield return null;
        }
        Object.Destroy(paletteObject);Object.Destroy(material);
    }
}
