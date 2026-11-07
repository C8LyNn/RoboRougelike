using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4WheelVariantsTests
{
    [UnitySetUp]public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;}
    [UnityTest]public IEnumerator RealGarageSwitchesWheelAppearanceAndPersistsIt()
    {
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        var state=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Wheel);var id=state.Id;var cell=state.Cell;
        var before=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        var collider=before.transform.Find("Koło").GetComponent<Collider>();Assert.That(collider.enabled,Is.True);
        Assert.That(run.GarageToggleWheelStyle(cell),Is.True);yield return null;
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==id).WheelStyle,Is.EqualTo(R3WheelStyle.Slick));
        var wheel=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        Assert.That(wheel.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4M_WheelM_Side_Slick_Rotor"),Is.True);
        Assert.That(wheel.transform.Find("Koło").GetComponent<Collider>().enabled,Is.True);
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(run.VehicleModel.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Standard,run.VehicleModel.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==id).WheelStyle,Is.EqualTo(R3WheelStyle.Slick));
        Assert.That(R3VehicleModel.SameFusionType(state,new R3PartState(cell,R3PartKind.Wheel)),Is.False);
        Assert.That(run.GarageToggleWheelStyle(cell),Is.True);yield return null;
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==id).WheelStyle,Is.EqualTo(R3WheelStyle.Offroad));
    }
    [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
