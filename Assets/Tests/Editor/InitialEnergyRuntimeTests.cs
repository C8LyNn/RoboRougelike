using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class InitialEnergyRuntimeTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;}
    static float StarterCapacity(R3VehicleKind kind)
    {
        var parts=R3VehicleCatalog.Create(kind).Parts;
        return VehicleEnergyState.CoreCapacity+
            parts.Where(p=>p.Kind==R3PartKind.Battery).Sum(p=>VehicleEnergyState.BatteryCapacity*R3VehicleModel.TierEffectUnits(p.Tier))-
            parts.Where(p=>R3VehicleModel.IsPassiveEnergyPart(p.Kind)).Sum(p=>VehicleEnergyState.PassiveReservation+R3VehicleModel.TierPassiveExtra(p.Tier));
    }
    IEnumerator CheckFirstEncounter(R3VehicleKind kind)
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(kind),Is.True);
        Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        Assert.That(run.StartNextEncounter(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        float deadline=Time.realtimeSinceStartup+25;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);var bank=run.Player.GetComponent<VehicleEnergyBank>();Assert.That(bank,Is.Not.Null);
        Assert.That(bank.Maximum,Is.EqualTo(StarterCapacity(kind)).Within(.01f));
        Assert.That(bank.Current,Is.EqualTo(bank.Maximum).Within(.01f),"The real initial encounter starts fully charged, including starter batteries.");
    }
    [UnityTest] public IEnumerator LightStartsAtFullEnergy()=>CheckFirstEncounter(R3VehicleKind.Light);
    [UnityTest] public IEnumerator StandardStartsAtFullEnergy()=>CheckFirstEncounter(R3VehicleKind.Standard);
    [UnityTest] public IEnumerator HeavyStartsAtFullEnergy()=>CheckFirstEncounter(R3VehicleKind.Heavy);
    [UnityTest] public IEnumerator RangeBeforeFirstBattleDoesNotConsumeInitialChargeForNewBattery()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Standard),Is.True);
        Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        Assert.That(run.EnterTestRange(),Is.True);
        float deadline=Time.realtimeSinceStartup+30;
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.TestDrive));
        var rangeBank=run.Player.GetComponent<VehicleEnergyBank>();
        Assert.That(rangeBank.Current,Is.EqualTo(rangeBank.Maximum).Within(.01f),"The test copy starts fully charged.");
        Assert.That(run.ReturnFromTestRange(),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
        run.VehicleModel.Award(100);
        var batteryCell=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>run.VehicleModel.PlacementError(c,R3PartKind.Battery)==null);
        Assert.That(run.GarageBuy(batteryCell,R3PartKind.Battery),Is.True);
        Assert.That(run.StartNextEncounter(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();Assert.That(bank.Maximum,Is.EqualTo(StarterCapacity(R3VehicleKind.Standard)+VehicleEnergyState.BatteryCapacity).Within(.01f));
        Assert.That(bank.Current,Is.EqualTo(bank.Maximum).Within(.01f),"The first real battle must include the battery installed after the test range.");
    }
    [UnityTest] public IEnumerator InactiveMountedBatteryAndMotorDoNotAffectAvailableCapacity()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Standard)&&run.OpenSandboxGarage(),Is.True);
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var bank=run.Player.GetComponent<VehicleEnergyBank>();
        var battery=assembly.Parts.First(p=>p.Kind==R3PartKind.Battery&&p.Operational);
        var motor=assembly.Parts.First(p=>p.Kind==R3PartKind.DrivePower&&p.Operational);
        float baseline=bank.Maximum;
        battery.gameObject.SetActive(false);
        Assert.That(bank.Maximum,Is.EqualTo(baseline-VehicleEnergyState.BatteryCapacity*R3VehicleModel.TierEffectUnits(battery.Tier)).Within(.01f));
        motor.gameObject.SetActive(false);
        Assert.That(bank.Maximum,Is.EqualTo(baseline-VehicleEnergyState.BatteryCapacity*R3VehicleModel.TierEffectUnits(battery.Tier)+VehicleEnergyState.PassiveReservation+R3VehicleModel.TierPassiveExtra(motor.Tier)).Within(.01f));
        battery.gameObject.SetActive(true);motor.gameObject.SetActive(true);
        Assert.That(bank.Maximum,Is.EqualTo(baseline).Within(.01f));
        float charge=bank.Current;run.Player.gameObject.SetActive(false);
        Assert.That(bank.Maximum,Is.EqualTo(baseline).Within(.01f));
        Assert.That(bank.Current,Is.EqualTo(charge).Within(.01f),"Suspending the entire actor must not erase stored energy.");
        run.Player.gameObject.SetActive(true);
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
