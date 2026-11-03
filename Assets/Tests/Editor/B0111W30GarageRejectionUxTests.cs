using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;

// Native Unity regression source for the ACTUAL W29 #877 garage, not a
// replacement garage or a self-contained prototype. Must be executed by
// licensed Unity 6000.3.14f1; source-transport CI is not Unity G1.
public sealed class B0111W30GarageRejectionUxTests
{
    [UnityTest]
    public IEnumerator ClickingWithoutTargetErasesOldSuccessWithoutSpendingScrap()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        var deadline=Time.realtimeSinceStartup+15f;
        while(R1RunController.Instance?.Progress==null && Time.realtimeSinceStartup<deadline)
            yield return null;
        var run=R1RunController.Instance;
        Assert.That(run,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        var garage=run.Garage;
        var model=run.VehicleModel;
        var ids=model.Parts.Select(p=>p.Id).OrderBy(x=>x).ToArray();
        int scrap=model.Scrap;
        garage.SetTool(R41GarageTool.Mount); // genuine missing-target state
        garage.Message="Zamontowano dokładnie w miejscu podglądu."; // stale last-success witness
        Assert.That(garage.HasTarget,Is.False);
        Assert.That(garage.ClickTarget(),Is.False);
        Assert.That(garage.Message,Does.Contain("Najedź"));
        Assert.That(garage.Message,Does.Not.Contain("Zamontowano"));
        Assert.That(model.Scrap,Is.EqualTo(scrap));
        CollectionAssert.AreEqual(ids,model.Parts.Select(p=>p.Id).OrderBy(x=>x).ToArray());
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator AuthoritativeRejectionAfterValidPreviewShowsCurrentReasonAndPreservesIds()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        var deadline=Time.realtimeSinceStartup+15f;
        while(R1RunController.Instance?.Progress==null && Time.realtimeSinceStartup<deadline)
            yield return null;
        var run=R1RunController.Instance;
        Assert.That(run,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        var model=run.VehicleModel;
        var view=run.Garage;
        model.Award(1000); // test fixture: guarantee sufficient wallet for a legal armor purchase
        view.UseInventory=false;
        view.SelectedInventoryId=null;
        view.SelectedPart=R3PartKind.Armor;
        Vector3Int? cell=null;
        foreach(var part in model.Parts.Where(p=>p.Health>0))
        {
            foreach(var occupied in R3VehicleModel.OccupiedCells(part))
            {
                foreach(var face in R3MountingGrid.FaceDirections)
                {
                    var candidate=occupied+face;
                    if(model.PlacementError(candidate,R3PartKind.Armor)==null)
                    {cell=candidate;break;}
                }
                if(cell.HasValue)break;
            }
            if(cell.HasValue)break;
        }
        Assert.That(cell.HasValue,Is.True,"original #877 canonical physical gate needs one legal fixture");
        view.Select(cell.Value);
        Assert.That(view.PreviewValid,Is.True,"original preview must be valid before race witness");
        var ids=model.Parts.Select(p=>p.Id).OrderBy(x=>x).ToArray();
        int scrap=model.Scrap;
        const string currentReason="Kolizja po zmianie przeszkody: montaż zabroniony.";
        int calls=0;
        // Native test-only deterministic gate transition. First read = valid
        // ghost; second read = rejection during original Buy; third = exact
        // live canonical reason. This is NOT a PhysX dynamic-obstacle PASS.
        var method=typeof(R3VehicleModel).GetMethod("SetInstallationGate",BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var injected=(Func<R3PartState,string>)(_=>++calls==1?null:currentReason);
        method.Invoke(model,new object[]{injected});
        try
        {
            Assert.That(view.ClickTarget(),Is.False);
            Assert.That(calls,Is.GreaterThanOrEqualTo(3));
            Assert.That(view.Message,Is.EqualTo(currentReason),
                "R3 UI must not replace the actual canonical denial with generic text");
            Assert.That(model.Scrap,Is.EqualTo(scrap));
            CollectionAssert.AreEqual(ids,model.Parts.Select(p=>p.Id).OrderBy(x=>x).ToArray());
        }
        finally
        {
            // Restore exactly one original R3/BUILD001 canonical live gate.
            R3BuildValidation.Bind(run.Player.GetComponent<R3VehicleAssembly>(),model);
        }
        yield return new ExitPlayMode();
    }
    [UnityTest]
    public IEnumerator HealthyPartRepairRejectsWithActualReasonAndNoMutation()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        var deadline=Time.realtimeSinceStartup+15f;
        while(R1RunController.Instance?.Progress==null && Time.realtimeSinceStartup<deadline)
            yield return null;
        var run=R1RunController.Instance;
        Assert.That(run,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        var model=run.VehicleModel;
        var part=model.Parts.First(p=>p.Kind==R3PartKind.Armor && p.Health>=1f);
        var ids=model.Parts.Select(p=>p.Id).OrderBy(x=>x).ToArray();
        float health=part.Health;
        int scrap=model.Scrap;
        var garage=run.Garage;
        garage.SetTool(R41GarageTool.Repair);
        garage.Select(part.Cell);
        Assert.That(garage.ClickTarget(),Is.False);
        Assert.That(garage.Message,Does.Contain("nie wymaga naprawy"));
        Assert.That(part.Health,Is.EqualTo(health));
        Assert.That(model.Scrap,Is.EqualTo(scrap));
        CollectionAssert.AreEqual(ids,model.Parts.Select(p=>p.Id).OrderBy(x=>x).ToArray());
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator InsufficientScrapRepairExplainsRealQuoteWithoutChangingParts()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        var deadline=Time.realtimeSinceStartup+15f;
        while(R1RunController.Instance?.Progress==null && Time.realtimeSinceStartup<deadline)
            yield return null;
        var run=R1RunController.Instance;
        Assert.That(run,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        var model=run.VehicleModel;
        var part=model.Parts.First(p=>p.Kind==R3PartKind.Armor && p.Health>=1f);
        float oldHp=part.Health;int oldScrap=model.Scrap;
        var originalIds=model.Parts.Select(p=>p.Id).OrderBy(x=>x).ToArray();
        // Explicit TEST-ONLY injected damaged/empty wallet state. This does not
        // claim a natural battle reward/Save or human playtest.
        var setter=typeof(R3VehicleModel).GetProperty("Scrap",
            BindingFlags.Public|BindingFlags.Instance)?.GetSetMethod(true);
        Assert.That(setter,Is.Not.Null);
        try
        {
            part.Health=.5f;
            setter.Invoke(model,new object[]{0});
            int required=model.RepairCost(part);
            Assert.That(required,Is.GreaterThan(0));
            var garage=run.Garage;
            garage.SetTool(R41GarageTool.Repair);
            garage.Select(part.Cell);
            Assert.That(garage.ClickTarget(),Is.False);
            Assert.That(garage.Message,Does.Contain("Za mało Złomu"));
            Assert.That(garage.Message,Does.Contain("Koszt: "+required+" Scrap"));
            Assert.That(model.Scrap,Is.Zero);
            Assert.That(part.Health,Is.EqualTo(.5f));
            CollectionAssert.AreEqual(originalIds,
                model.Parts.Select(p=>p.Id).OrderBy(x=>x).ToArray());
        }
        finally
        {
            part.Health=oldHp;
            setter.Invoke(model,new object[]{oldScrap});
        }
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator ZeroScrapPaidRebuildQuoteDoesNotProduceFalseUnavailablePriceMessage()
    {
        // Actual original R3 game / garage; model values injected by QA only.
        // This checks DISPLAY correctness whether today's legacy model rejects
        // paid-zero or the approved V7 repair model accepts it. It deliberately
        // does NOT certify V7 price parity (#897 owns that RED witness).
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        var deadline=Time.realtimeSinceStartup+15f;
        while(R1RunController.Instance?.Progress==null && Time.realtimeSinceStartup<deadline)
            yield return null;
        var run=R1RunController.Instance;
        Assert.That(run,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        var model=run.VehicleModel;
        var wheel=model.Parts.First(p=>p.Kind==R3PartKind.Wheel && p.Health>=1f);
        int oldPrice=model.Economy.WheelCost, oldScrap=model.Scrap;
        float oldHp=wheel.Health;
        var ids=model.Parts.Select(p=>p.Id).OrderBy(x=>x).ToArray();
        var setter=typeof(R3VehicleModel).GetProperty("Scrap",
            BindingFlags.Public|BindingFlags.Instance)?.GetSetMethod(true);
        Assert.That(setter,Is.Not.Null);
        try
        {
            model.Economy.WheelCost=1;
            wheel.Health=0f;
            setter.Invoke(model,new object[]{0});
            Assert.That(model.Price(R3PartKind.Wheel),Is.EqualTo(1));
            Assert.That(model.RepairCost(wheel),Is.EqualTo(0),
                "A priced 0-Scrap quote is not an earned free-win entitlement.");
            var view=run.Garage;
            view.SetTool(R41GarageTool.Repair);
            view.Select(wheel.Cell);
            bool repaired=view.ClickTarget();
            Assert.That(model.Scrap,Is.Zero);
            CollectionAssert.AreEqual(ids,
                model.Parts.Select(p=>p.Id).OrderBy(x=>x).ToArray());
            if(repaired)
            {
                Assert.That(wheel.Health,Is.EqualTo(1f));
                Assert.That(view.Message,Does.Contain("Naprawiono"));
            }
            else
            {
                Assert.That(wheel.Health,Is.Zero);
                Assert.That(view.Message,Does.Contain("Odbudowa za 0 Scrap"));
                Assert.That(view.Message,Does.Not.Contain("Brak dopuszczonej wyceny"));
                Assert.That(view.Message,Does.Contain("Nie pobrano Złomu"));
            }
        }
        finally
        {
            wheel.Health=oldHp;
            model.Economy.WheelCost=oldPrice;
            setter.Invoke(model,new object[]{oldScrap});
        }
        yield return new ExitPlayMode();
    }

}
