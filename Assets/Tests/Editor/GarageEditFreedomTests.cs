using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;

public sealed class GarageEditFreedomTests
{
    [Test] public void ModelCanRemoveLastGunAndEveryWheelButCannotDeploy()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var gun=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
        Assert.That(model.Remove(gun.Cell),Is.True,"removing the last gun is a valid garage edit");
        Assert.That(model.CanDeploy,Is.False);Assert.That(model.DeployError,Does.Contain("broni"));
        foreach(var wheel in model.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray())
            Assert.That(model.Remove(wheel.Cell),Is.True,"every wheel can be removed");
        Assert.That(model.Parts.Any(p=>p.Kind==R3PartKind.Wheel),Is.False);
        Assert.That(model.CanDeploy,Is.False);Assert.That(model.Remove(Vector3Int.zero),Is.False,"core stays protected");
        Assert.That(model.Inventory.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(model.Inventory.Count));
    }
    [UnityTest] public IEnumerator RealGarageRebuildsAfterLastGunAndEveryWheelAreRemoved()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        var until=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run,Is.Not.Null);Assert.That(run.OpenSandboxGarage(),Is.True);
        var gun=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Gun);
        Assert.That(run.GarageRemove(gun.Cell),Is.True,"last gun removed with live garage rebuild");
        Assert.That(run.Player,Is.Not.Null);Assert.That(run.VehicleModel.CanDeploy,Is.False);
        foreach(var wheel in run.VehicleModel.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray())
            Assert.That(run.GarageRemove(wheel.Cell),Is.True,"wheel removed with live garage rebuild");
        Assert.That(run.Player,Is.Not.Null);Assert.That(run.EnterTestRange(),Is.False);
        Assert.That(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.Gun||p.Kind==R3PartKind.Wheel),Is.False);
        yield return new ExitPlayMode();
    }
}
