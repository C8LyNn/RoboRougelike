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
        var state=run.VehicleModel.Parts.Single(p=>p.Id==part.Id);
        state.DeviceReadyAt=Time.time+5f;
        Assert.That(pulse.Status,Does.StartWith("odnowienie "));
        state.DeviceReadyAt=0;
        Assert.That(pulse.Status,Is.EqualTo("brak energii"));
        Assert.That(pulse.TryActivate(),Is.Zero,"Insufficient energy must not activate repair.");
        Assert.That(energy.Current,Is.EqualTo(1).Within(.001f));
        energy.Recharge(100);float energyBefore=energy.Current;
        Assert.That(pulse.Status,Is.EqualTo("gotowy"));
        Assert.That(pulse.TryActivate(),Is.EqualTo(1));
        Assert.That(pulse.Status,Is.EqualTo("naprawia"));
        Assert.That(energy.Current,Is.EqualTo(energyBefore-R3RepairPulse.EnergyCost).Within(.001f));
        Assert.That(pulse.TryActivate(),Is.Zero);
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==part.Id).DeviceReadyAt,Is.GreaterThan(Time.time));
        var tick=typeof(R3RepairPulse).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic);
        tick.Invoke(pulse,new object[]{2f});
        Assert.That(armor.Damageable.CurrentHp/armor.Damageable.MaxHp,Is.EqualTo(.6f).Within(.001f));
        var device=assembly.Parts.Single(p=>p.Id==part.Id);
        armor.gameObject.SetActive(false);tick.Invoke(pulse,new object[]{1f});
        Assert.That(armor.Damageable.CurrentHp/armor.Damageable.MaxHp,Is.EqualTo(.6f).Within(.001f),"Inactive targets must not be repaired.");
        armor.gameObject.SetActive(true);tick.Invoke(pulse,new object[]{1f});
        Assert.That(armor.Damageable.CurrentHp/armor.Damageable.MaxHp,Is.EqualTo(.65f).Within(.001f));
        device.gameObject.SetActive(false);tick.Invoke(pulse,new object[]{1f});
        Assert.That(pulse.ActiveCount,Is.Zero);Assert.That(pulse.Status,Is.EqualTo("nieaktywny"));
        Assert.That(armor.Damageable.CurrentHp/armor.Damageable.MaxHp,Is.EqualTo(.65f).Within(.001f),"An inactive device must stop its pulse.");
        device.gameObject.SetActive(true);state.DeviceReadyAt=0;
        Assert.That(pulse.TryActivate(),Is.EqualTo(1));
        // A killing blow can also damage connected armor. Use a bounded hit
        // and measure after routing instead of assuming neighbours stay intact.
        device.Damageable.ApplyDamage(device.Damageable.CurrentHp*1.4f,device.transform.position,Vector3.up);
        Assert.That(device.Damageable.IsDead,Is.True);
        Assert.That(armor.Damageable.IsDead,Is.False);
        float healthAfterHit=armor.Damageable.CurrentHp;
        tick.Invoke(pulse,new object[]{1f});
        Assert.That(pulse.ActiveCount,Is.Zero);
        Assert.That(pulse.Status,Is.EqualTo("nieaktywny"));
        Assert.That(armor.Damageable.CurrentHp,Is.EqualTo(healthAfterHit).Within(.001f),"A destroyed device must not continue healing its target.");
        yield return null;
    }
    [UnityTest]
    public IEnumerator StatusPollingDoesNotAllocatePerFrameForIntactVehicle()
    {
        Assert.That(run.SelectVehicle(R3VehicleKind.Heavy),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        run.VehicleModel.Award(500);
        Assert.That(run.GaragePurchase(R3PartKind.RepairPulse),Is.True);
        var part=run.VehicleModel.Inventory.Single(p=>p.Kind==R3PartKind.RepairPulse);
        var cell=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>run.VehicleModel.OwnedPlacementError(part.Id,c,default)==null);
        Assert.That(run.GarageMount(part.Id,cell),Is.True);
        var pulse=run.Player.GetComponent<R3RepairPulse>();
        Assert.That(pulse.Status,Is.EqualTo("brak uszkodzeń"));
        for(int i=0;i<3;i++)GC.KeepAlive(pulse.Status);
        using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",16))
        {
            Assert.That(recorder.Valid,Is.True);yield return null;yield return null;
            long idle=recorder.LastValue;
            for(int i=0;i<50;i++)GC.KeepAlive(pulse.Status);
            yield return null;long sample=recorder.LastValue;
            var control=new byte[1024*1024];control[0]=1;yield return null;long controlBytes=recorder.LastValue;GC.KeepAlive(control);
            Debug.Log($"[REPAIR_STATUS_ALLOC] queries=50 parts={run.VehicleModel.Parts.Count} idle={idle} sample={sample} control={controlBytes}");
            Assert.That(controlBytes,Is.GreaterThanOrEqualTo(1024*1024),"The profiler must detect a known allocation.");
            Assert.That(sample-idle,Is.LessThan(20000),"Repeated status polling must not create temporary lists each frame.");
        }
    }
    [UnityTest]
    public IEnumerator StatusUsesAvailableDeviceWhenOtherIsCoolingOrDisrupted()
    {
        Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(500);
        for(int i=0;i<2;i++)
        {
            Assert.That(run.GaragePurchase(R3PartKind.RepairPulse),Is.True);
            var part=run.VehicleModel.Inventory.Last(p=>p.Kind==R3PartKind.RepairPulse);
            var cell=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>run.VehicleModel.OwnedPlacementError(part.Id,c,default)==null);
            Assert.That(run.GarageMount(part.Id,cell),Is.True);
        }
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        var pulse=run.Player.GetComponent<R3RepairPulse>();
        var devices=assembly.Parts.Where(p=>p.Kind==R3PartKind.RepairPulse).ToArray();
        Assert.That(devices.Length,Is.EqualTo(2));
        assembly.Parts.First(p=>p.Kind==R3PartKind.Armor&&p.Operational).Damageable.RestoreHealthRatio(.5f);
        var cooling=run.VehicleModel.Parts.Single(p=>p.Id==devices[0].Id);
        cooling.DeviceReadyAt=Time.time+10f;
        Assert.That(pulse.Status,Is.EqualTo("gotowy"));
        var emp=devices[1].GetComponent<RoboRougelike.Mechanics.EMP.EmpReceiver>();
        Assert.That(emp,Is.Not.Null);emp.ApplyEmp(3f);
        Assert.That(pulse.Status,Does.StartWith("odnowienie "));
        emp.Advance(3f);
        Assert.That(pulse.Status,Is.EqualTo("gotowy"));
        devices[1].gameObject.SetActive(false);
        Assert.That(pulse.Status,Does.StartWith("odnowienie "),"An inactive available device must not mask the other device's cooldown.");
        devices[0].gameObject.SetActive(false);
        Assert.That(pulse.Status,Is.EqualTo("nieaktywny"));
        devices[0].gameObject.SetActive(true);devices[1].gameObject.SetActive(true);
        Assert.That(pulse.Status,Is.EqualTo("gotowy"));
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
