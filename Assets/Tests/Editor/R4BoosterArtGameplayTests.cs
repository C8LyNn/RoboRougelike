using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.Booster;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4BoosterArtGameplayTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;R1RunController.Instance.OpenSandboxGarage();Time.timeScale=1;}
    [UnityTest] public IEnumerator BoosterUsesExistingPhysicsAndShowsThrustOnlyWhenActive()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Parts.Add(new R3PartState(new Vector3Int(-1,1,0),R3PartKind.Booster));
        var fixtures=new GameObject("R4 booster art fixture");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixtures.transform);
        floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(80,1,80);
        var palette=Object.FindFirstObjectByType<R3VisualPalette>();
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,.7f,1000),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);vehicle.Controller.SetGameplayInputEnabled(true);
        var part=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.Booster);
        var booster=part.GetComponent<VehicleBooster>();Assert.That(booster,Is.Not.Null);
        var art=part.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R4M_Booster_ROOT");Assert.That(art,Is.Not.Null,"Supplied booster model should be attached.");
        var flame=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Booster_Flame");
        var nozzle=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Thruster_Nozzle");
        var socket=art.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Socket_Mount");
        Assert.That(Vector3.Distance(socket.position,part.transform.TransformPoint(Vector3.up*.5f)),Is.LessThan(.001f));
        Assert.That(art.GetComponentsInChildren<Collider>(true),Is.Empty,"Visual must not change physical collisions.");
        Assert.That(part.transform.Find("MOV • Booster").GetComponent<Collider>().enabled,Is.True);
        Assert.That(Vector3.Dot(nozzle.forward,part.transform.forward),Is.LessThan(-.9f),"Exhaust points opposite existing thrust.");
        Assert.That(Vector3.Dot(-socket.forward,part.transform.up),Is.GreaterThan(.9f),"The imported mount normal points up.");
        yield return null;Assert.That(flame.gameObject.activeSelf,Is.False);
        Assert.That(booster.TryActivate(),Is.True);yield return null;
        Assert.That(flame.gameObject.activeSelf,Is.True,"Flame indicates actual boost activity.");
        booster.StopBoost();yield return null;Assert.That(flame.gameObject.activeSelf,Is.False);
        Object.Destroy(vehicle.gameObject);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
