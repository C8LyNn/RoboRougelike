using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RoboRougelike.PackageIntegration;
using RoboRougelike.Mechanics.EMP;
using RoboRougelike.Mechanics.SEN001;
using RoboRougelike.Mechanics.HAZ001;
using RogueMachine.P12PipelineSafe;

public class RRLW8PulseScannerTests
{
    R1RunController run;R3PartKind kind;R3MountedPart module;ScannerPulse pulse;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
    }
    IEnumerator Build(bool withStructural=false)
    {
        Assert.That(run.OpenSandboxGarage(),Is.True);
        Assert.That(run.GarageBuy(new Vector3Int(0,2,-2),kind),Is.True);
        if(withStructural)Assert.That(run.GarageBuy(new Vector3Int(-2,2,1),R3PartKind.StructuralScanner),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        module=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==kind);
        pulse=module.GetComponent<ScannerPulse>();Assert.That(pulse,Is.Not.Null);Physics.SyncTransforms();yield return new WaitForFixedUpdate();
    }
    bool Activate(R3MountedPart part=null)
    {
        var binding=(part??module).GetComponents<MonoBehaviour>().SingleOrDefault(c=>c.GetType().Name=="RRLPulseScannerBinding");
        Assert.That(binding,Is.Not.Null,"SEN must use the shared physical device boundary");
        return (bool)binding.GetType().GetMethod("TryActivate").Invoke(binding,null);
    }
    [UnityTest] public IEnumerator PulseRevealsRealEnemyThroughWallWhileStructuralScanCannotSeeHp()
    {
        Assert.That(Enum.TryParse("ScannerPulse",out kind),Is.True,"SEN must be an owned physical garage module");
        yield return Build(true);var enemy=run.GetComponent<R6TestRange>().TrainingTarget;
        var parts=enemy.GetComponentsInChildren<P12PipelineSafeDamageable>();var hp=parts.Select(p=>p.CurrentHp).ToArray();
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,3,-23.5f);wall.transform.localScale=new Vector3(50,10,1);Physics.SyncTransforms();yield return new WaitForFixedUpdate();
        var structural=run.Player.GetComponentInChildren<RRLStructuralScannerBinding>();Assert.That(structural.TryActivate(),Is.False);Assert.That(structural.Scanner.Contacts,Is.Empty);
        Assert.That(Activate(),Is.True);Assert.That(pulse.LastHits.Count,Is.EqualTo(1),"One enemy signal per physical actor, not per part collider");
        var signal=pulse.Revealed.Single();Assert.That(signal.SignalKind,Is.EqualTo(ScannerSignalKind.Enemy));Assert.That(signal.transform.IsChildOf(enemy.transform),Is.True);
        Assert.That(pulse.IsRevealed(signal),Is.True);Assert.That(parts.Select(p=>p.CurrentHp),Is.EqualTo(hp));
        Assert.That(pulse.Revealed.Any(p=>p.transform.IsChildOf(run.Player.transform)),Is.False);
    }
    [UnityTest] public IEnumerator DensePhysicalVehiclesDoNotTruncateSignalsOrAlterOwnerPhysics()
    {
        Assert.That(Enum.TryParse("ScannerPulse",out kind),Is.True,"SEN must be an owned physical garage module");
        yield return Build();
        for(int i=0;i<2;i++)
        {
            var rig=R3VehicleAssembly.Build(new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Heavy)),new R1RunStats(),run.GetComponent<R3VisualPalette>(),run.Player.transform.position+new Vector3(i*2-1,0,3),Quaternion.identity);
            rig.GetComponent<Rigidbody>().isKinematic=true;
        }
        Physics.SyncTransforms();var body=run.Player.GetComponent<Rigidbody>();float mass=body.mass;var inertia=body.inertiaTensor;var position=body.position;var velocity=body.linearVelocity;
        var target=run.GetComponent<R6TestRange>().TrainingTarget.GetComponent<Rigidbody>();
        float targetMass=target.mass;var targetInertia=target.inertiaTensor;var targetPosition=target.position;var targetVelocity=target.linearVelocity;
        Assert.That(Activate(),Is.True);Assert.That(pulse.LastPulseTruncated,Is.False);Assert.That(pulse.LastHits.Count,Is.EqualTo(1));
        Assert.That(body.mass,Is.EqualTo(mass));Assert.That(body.inertiaTensor,Is.EqualTo(inertia));Assert.That(body.position,Is.EqualTo(position));Assert.That(body.linearVelocity,Is.EqualTo(velocity));
        Assert.That(target.mass,Is.EqualTo(targetMass));Assert.That(target.inertiaTensor,Is.EqualTo(targetInertia));Assert.That(target.position,Is.EqualTo(targetPosition));Assert.That(target.linearVelocity,Is.EqualTo(targetVelocity));
    }
    [UnityTest] public IEnumerator ValidEmptyPulseCommitsCooldownAndRetryCannotSpendAgain()
    {
        Assert.That(Enum.TryParse("ScannerPulse",out kind),Is.True,"SEN must be an owned physical garage module");
        yield return Build();run.GetComponent<R6TestRange>().TrainingTarget.gameObject.SetActive(false);Physics.SyncTransforms();
        Assert.That(Activate(),Is.True,"Original empty pulse is accepted, unlike COM empty scan");Assert.That(pulse.LastHits,Is.Empty);
        Assert.That(pulse.RemainingCooldown,Is.InRange(4.9f,5.01f));float ready=run.VehicleModel.Parts.Single(p=>p.Id==module.Id).DeviceReadyAt;
        Assert.That(Activate(),Is.False);Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==module.Id).DeviceReadyAt,Is.EqualTo(ready));
    }
    [UnityTest] public IEnumerator PauseFreezesRevealButEmpClearsItWithoutGrantingFreeCooldown()
    {
        Assert.That(Enum.TryParse("ScannerPulse",out kind),Is.True,"SEN must be an owned physical garage module");
        yield return Build();Assert.That(Activate(),Is.True);var signal=pulse.Revealed.Single();float cooldown=pulse.RemainingCooldown;
        run.TogglePause();yield return new WaitForSecondsRealtime(.2f);Assert.That(Activate(),Is.False);Assert.That(pulse.IsRevealed(signal),Is.True);Assert.That(pulse.RemainingCooldown,Is.EqualTo(cooldown).Within(.001));
        run.TogglePause();float hp=module.Damageable.CurrentHp;module.GetComponent<EmpReceiver>().ApplyEmp(1);Assert.That(Activate(),Is.False);Assert.That(pulse.Revealed,Is.Empty);Assert.That(module.Damageable.CurrentHp,Is.EqualTo(hp));
        float end=Time.time+1.1f;while(Time.time<end)yield return null;
        Assert.That(module.GetComponent<EmpReceiver>().IsDisrupted,Is.False);Assert.That(pulse.RemainingCooldown,Is.GreaterThan(3));Assert.That(Activate(),Is.False);
    }
    [UnityTest] public IEnumerator PulseCooldownAndIdentitySurviveDestructionPaidRepairAndRebuild()
    {
        Assert.That(Enum.TryParse("ScannerPulse",out kind),Is.True,"SEN must be an owned physical garage module");
        yield return Build();Assert.That(Activate(),Is.True);var assembly=run.Player.GetComponent<R3VehicleAssembly>();var state=run.VehicleModel.Parts.Single(p=>p.Id==module.Id);
        string id=state.Id;float ready=state.DeviceReadyAt;Assert.That(R3VehicleModel.OccupiedCells(state).Count(),Is.EqualTo(4));
        module.Damageable.ApplyDamage(100000,module.transform.position,Vector3.up);assembly.Capture();Assert.That(state.Health,Is.Zero);Assert.That(Activate(),Is.False);
        var copy=run.VehicleModel.DeepCopy();int scrap=copy.Scrap;Assert.That(copy.Repair(state.Cell),Is.True);Assert.That(copy.Scrap,Is.LessThan(scrap));
        var vehicle=R3VehicleAssembly.Build(copy,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        var restored=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);Assert.That(restored.GetComponent<ScannerPulse>().RemainingCooldown,Is.GreaterThan(0));Assert.That(Activate(restored),Is.False);
        Assert.That(copy.Parts.Single(p=>p.Id==id).DeviceReadyAt,Is.EqualTo(ready));yield return null;
    }
    [UnityTest] public IEnumerator PlayerGuiLUsesOwnModuleAndRejectsPausedOrDestroyedActivation()
    {
        Assert.That(Enum.TryParse("ScannerPulse",out kind),Is.True,"SEN must be an owned physical garage module");
        yield return Build();var controls=run.Player.GetComponent<RRLScannerControls>();Assert.That(controls,Is.Not.Null);
        var key=new Event{type=EventType.KeyDown,keyCode=KeyCode.None,character='l'};
        run.TogglePause();Assert.That(controls.HandleGuiInput(key),Is.False);Assert.That(pulse.RemainingCooldown,Is.Zero);
        run.TogglePause();Assert.That(controls.HandleGuiInput(key),Is.True);Assert.That(pulse.Revealed,Is.Not.Empty);
        Assert.That(controls.HandleGuiInput(key),Is.False);module.Damageable.ApplyDamage(100000,module.transform.position,Vector3.up);Assert.That(controls.HandleGuiInput(key),Is.False);
    }
    [UnityTest] public IEnumerator ActualGauntletHazardsAndDestroyedEnemyDebrisHaveTypedWorldSignals()
    {
        run.StartRun();run.ChooseRoute(0);while(run.Transitioning)yield return null;
        run.Encounter.SetCombatActive(false);R1EncounterManager.ClearTransientObjects();
        // Source-query fixture: gameplay activation and five-second resource timing are covered above.
        var query=run.Player.gameObject.AddComponent<ScannerPulse>();query.Configure(20,0,3,1<<RRLPulseSignal.SignalLayer);
        var zone=run.Arena.Layout.GetComponentsInChildren<TelegraphedHazardZone>().First();
        var enemy=run.Encounter.Enemies.First();RRLPulseSignal.Prepare(run);Physics.SyncTransforms();
        Assert.That(query.TryPulse(zone.transform.position),Is.GreaterThan(0));
        Assert.That(query.Revealed.Any(signal=>signal.SignalKind==ScannerSignalKind.Hazard&&signal.transform.IsChildOf(zone.transform)),Is.True);
        Assert.That(query.TryPulse(enemy.transform.position),Is.GreaterThan(0));
        var enemySignal=enemy.GetComponent<RRLPulseSignal>().Tag;Assert.That(query.IsRevealed(enemySignal),Is.True);
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(query.IsRevealed(enemySignal),Is.False,"A dead enemy must disappear immediately, before TTL expires");
        var wreck=UnityEngine.Object.FindObjectsByType<R3Debris>(FindObjectsSortMode.None).First();
        RRLPulseSignal.Prepare(run);Physics.SyncTransforms();Assert.That(query.TryPulse(wreck.transform.position),Is.GreaterThan(0));
        Assert.That(query.Revealed.Any(signal=>signal.SignalKind==ScannerSignalKind.Salvage&&signal.GetComponentInParent<R3Debris>()!=null),Is.True);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
