using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.WeaponIntegration;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class W27SawClearanceTests
{
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
    [UnityTest] public IEnumerator OwnPartInSawDiscBlocksMotorAndHeatUntilRemoved()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var colors=new GameObject("W27 saw palette");var palette=colors.AddComponent<R3VisualPalette>();
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        for(int i=model.Parts.Count-1;i>=0;i--)if(model.Parts[i].Kind!=R3PartKind.Core&&model.Parts[i].Kind!=R3PartKind.Gun)model.Parts.RemoveAt(i);
        R3PartState state=null;foreach(var part in model.Parts)if(part.Kind==R3PartKind.Gun){state=part;break;}
        Assert.That(state,Is.Not.Null);state.Weapon=R4WeaponKind.RotarySaw;
        var rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        rig.GetComponent<Rigidbody>().isKinematic=true;yield return null;
        R3MountedPart gun=null;foreach(var part in rig.GetComponent<R3VehicleAssembly>().Parts)if(part.Id==state.Id){gun=part;break;}
        Assert.That(gun,Is.Not.Null);var saw=gun.GetComponent<RRLSawBinding>();
        Assert.That(saw.Device.Available,Is.True);
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="W27 owned saw obstruction";
        blocker.transform.SetParent(rig.transform,true);blocker.transform.position=gun.Muzzle.position+gun.Muzzle.forward*.6f;
        blocker.transform.rotation=gun.Muzzle.rotation;blocker.transform.localScale=Vector3.one*.2f;Physics.SyncTransforms();
        saw.SetMotorRequested(true);saw.SendMessage("FixedUpdate");saw.SendMessage("FixedUpdate");
        Assert.That(saw.Module.Motor.Rpm,Is.EqualTo(0).Within(.001f),"Own armor in the cutting disc must prevent activation.");
        Assert.That(saw.Module.Motor.Heat,Is.EqualTo(0).Within(.001f),"A blocked activation must not spend thermal capacity.");
        blocker.SetActive(false);Physics.SyncTransforms();saw.SetMotorRequested(true);saw.SendMessage("FixedUpdate");saw.SendMessage("FixedUpdate");
        Assert.That(saw.Module.Motor.Rpm,Is.GreaterThan(0),"Removing the obstacle must restore the motor.");
        Object.Destroy(rig.gameObject);Object.Destroy(colors);Object.Destroy(material);
    }
}
