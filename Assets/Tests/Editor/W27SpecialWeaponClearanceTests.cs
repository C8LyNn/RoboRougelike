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
