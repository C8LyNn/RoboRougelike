using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R1;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogRepairPulseTests
{
    [Test]
    public void ActiveRepairIsASeparatePhysicalPart()
    {
        Assert.That(Enum.TryParse("RepairPulse",out R3PartKind kind),Is.True,
            "The active repair device must be distinct from legacy passive repair bonuses.");
        Assert.That(kind,Is.Not.EqualTo(R3PartKind.BonusModule));
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Award(1000);Assert.That(model.PurchaseToInventory(kind),Is.True);
        Assert.That(model.Inventory.Single().Kind,Is.EqualTo(kind));
    }

}

public sealed class BacklogRepairPulseIntegrationTests
{
    R1RunController run;
    [UnitySetUp]
    public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;
        Assert.That(run?.Progress,Is.Not.Null,"Game controller must finish initialization before the test.");
    }
    [UnityTest]
    public IEnumerator RealMountedModuleSpendsEnergyHealsGraduallyAndStopsWhenDestroyed()
    {
        Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True,"Vehicle selection");
        Assert.That(run.OpenSandboxGarage(),Is.True,"Garage open");
        run.VehicleModel.Award(500);
        Assert.That(run.GaragePurchase(R3PartKind.RepairPulse),Is.True);
        var part=run.VehicleModel.Inventory.Single(p=>p.Kind==R3PartKind.RepairPulse);
        var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        var cell=cells.First(c=>run.VehicleModel.OwnedPlacementError(part.Id,c,default)==null);
        Assert.That(run.GarageMount(part.Id,cell),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);
        for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        Assert.That(run.CanUseMovement,Is.True);
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        var pulse=run.Player.GetComponent<R3RepairPulse>();
        var energy=run.Player.GetComponent<VehicleEnergyBank>();
        Assert.That(pulse,Is.Not.Null,"Mounted device must be attached to the vehicle");
        Assert.That(energy,Is.Not.Null);
        var armor=assembly.Parts.First(p=>p.Kind==R3PartKind.Armor&&p.Operational);
        float fullEnergy=energy.Current;
        Assert.That(pulse.Status,Is.EqualTo("brak uszkodzeń"));
        Assert.That(pulse.TryActivate(),Is.Zero,"No damaged target means no cost or cooldown.");
        Assert.That(energy.Current,Is.EqualTo(fullEnergy).Within(.001f));
        Assert.That(energy.TrySpend(energy.Current-1),Is.True);
        armor.Damageable.RestoreHealthRatio(.5f);
        Assert.That(pulse.Status,Is.EqualTo("brak energii"));
        Assert.That(pulse.TryActivate(),Is.Zero,"Insufficient energy must not activate repair.");
        Assert.That(energy.Current,Is.EqualTo(1).Within(.001f));
        energy.Recharge(100);float energyBefore=energy.Current;
        Assert.That(pulse.TryActivate(),Is.EqualTo(1));
        Assert.That(pulse.Status,Is.EqualTo("naprawia"));
        Assert.That(energy.Current,Is.EqualTo(energyBefore-R3RepairPulse.EnergyCost).Within(.001f));
        Assert.That(pulse.TryActivate(),Is.Zero);
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==part.Id).DeviceReadyAt,Is.GreaterThan(Time.time));
        var tick=typeof(R3RepairPulse).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic);
        tick.Invoke(pulse,new object[]{2f});
        Assert.That(armor.Damageable.CurrentHp/armor.Damageable.MaxHp,Is.EqualTo(.6f).Within(.001f));
        var device=assembly.Parts.Single(p=>p.Id==part.Id);
        device.Damageable.ApplyDamage(100000,device.transform.position,Vector3.up);
        tick.Invoke(pulse,new object[]{1f});
        Assert.That(pulse.ActiveCount,Is.Zero);
        Assert.That(pulse.Status,Is.EqualTo("nieaktywny"));
        Assert.That(armor.Damageable.CurrentHp/armor.Damageable.MaxHp,Is.EqualTo(.6f).Within(.001f));
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
