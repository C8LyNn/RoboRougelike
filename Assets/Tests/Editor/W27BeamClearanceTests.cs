using System.Collections;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.WeaponIntegration;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class W27BeamClearanceTests
{
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
    [UnityTest] public IEnumerator BeamDoesNotHeatWhileOwnArmorBlocksItsPathAndResumesWhenClear()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var colors=new GameObject("W27 beam palette");var palette=colors.AddComponent<R3VisualPalette>();
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        R3PartState state=null;foreach(var candidate in model.Parts)if(candidate.Kind==R3PartKind.Gun){state=candidate;break;}
        Assert.That(state,Is.Not.Null);state.Weapon=R4WeaponKind.BeamCutter;
        var rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        yield return null;
        R3MountedPart gun=null;foreach(var candidate in rig.GetComponent<R3VehicleAssembly>().Parts)if(candidate.Id==state.Id){gun=candidate;break;}
        Assert.That(gun,Is.Not.Null);
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.SetParent(rig.transform,true);
        blocker.transform.position=gun.Muzzle.position+gun.Muzzle.forward*.6f;blocker.transform.localScale=Vector3.one*.3f;
        Physics.SyncTransforms();var beam=gun.GetComponent<RRLBeamBinding>();beam.SetTriggerHeld(true);
        beam.SendMessage("FixedUpdate");beam.SendMessage("FixedUpdate");
        Assert.That(beam.Module.Heat,Is.EqualTo(0).Within(.0001f),"blocked beam must not heat");
        blocker.SetActive(false);Physics.SyncTransforms();
        Assert.That(beam.Device.Available,Is.True,"beam device available");
        Assert.That(rig.GetComponent<R3VehicleAssembly>().ClearProjectilePath(gun,gun.Muzzle.forward,beam.Module.Settings.rangeMeters,.001f),Is.True,"beam line clear after removal");
        beam.SetTriggerHeld(true);
        beam.SendMessage("FixedUpdate");beam.SendMessage("FixedUpdate");
        Assert.That(beam.Module.Heat,Is.GreaterThan(0),"beam resumes after armor is removed");
        Object.Destroy(rig.gameObject);Object.Destroy(colors);Object.Destroy(material);
    }
}
