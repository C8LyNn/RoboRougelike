using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.WeaponIntegration;

public sealed class CheckpointReloadExpiryTests
{
    [UnitySetUp] public IEnumerator Setup()
    {EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();Time.timeScale=1;}
    [UnityTest] public IEnumerator UnmountedExpiredReloadCompletesAfterRestoreAndRemount()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        Assert.That(model,Is.Not.Null);Assert.That(model.Parts,Is.Not.Null);
        Debug.Log("[Reload QA] parts="+model.Parts.Count+" null="+model.Parts.Count(p=>p==null));
        var gun=model.Parts.First(p=>p!=null&&p.Kind==R3PartKind.Gun);
        Assert.That(gun,Is.Not.Null);
        gun.Weapon=R4WeaponKind.Railgun;
        Assert.That(model.Remove(gun.Cell),Is.True);
        gun.Ammo=0;gun.ReloadReadyAt=1;
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(2)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,null,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Mount(gun.Id,gun.Cell,gun.RotationQuarterTurns),Is.True);
        var paletteObject=new GameObject("reload QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var rig=R3VehicleAssembly.Build(restored,new R1RunStats(),palette,new Vector3(0,10,0),Quaternion.identity);
        try
        {
            for(int i=0;i<10;i++)yield return null;
            var state=restored.Parts.Single(p=>p.Id==gun.Id);
            Assert.That(state.Ammo,Is.EqualTo(RRLRailgunBinding.Magazine),"An expired reload must finish when the owned gun is remounted");
            Assert.That(state.ReloadReadyAt,Is.Zero);
        }
        finally{Object.Destroy(rig.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);}
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
