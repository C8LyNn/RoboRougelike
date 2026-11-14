using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RoboRougelike.PackageIntegration;
using RoboRougelike.Mechanics.EMP;
using RoboRougelike.Mechanics.SEN001;
using RoboRougelike.Mechanics.StructuralScan;
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
    IEnumerator Build(bool withStructural=false,int copies=1)
    {
        Assert.That(run.OpenSandboxGarage(),Is.True);
        for(int i=0;i<copies;i++)MountScanner(kind);
        if(withStructural)MountScanner(R3PartKind.StructuralScanner);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        module=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==kind);
        pulse=module.GetComponent<ScannerPulse>();Assert.That(pulse,Is.Not.Null);Physics.SyncTransforms();yield return new WaitForFixedUpdate();
    }
    void MountScanner(R3PartKind scanner)
    {
        var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .OrderBy(c=>c.y).ThenBy(c=>Math.Abs(c.x)).ToArray();
        foreach(var cell in cells)
            if(run.VehicleModel.PlacementError(cell,scanner)==null&&run.GarageBuy(cell,scanner))return;
        Assert.Fail("No valid physical garage placement for "+scanner);
    }
    bool Activate(R3MountedPart part=null)
    {
        var binding=(part??module).GetComponents<MonoBehaviour>().SingleOrDefault(c=>c.GetType().Name=="RRLPulseScannerBinding");
        Assert.That(binding,Is.Not.Null,"SEN must use the shared physical device boundary");
        return (bool)binding.GetType().GetMethod("TryActivate").Invoke(binding,null);
    }
    [UnityTest] public IEnumerator StructuralScannerCopiesUseSharedEnergyAndCooldown()
    {
        Assert.That(run.OpenSandboxGarage(),Is.True);MountScanner(R3PartKind.StructuralScanner);MountScanner(R3PartKind.StructuralScanner);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var parts=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.StructuralScanner).ToArray();
        var controls=run.Player.GetComponent<RRLScannerControls>();Assert.That(parts.Length,Is.EqualTo(2));Physics.SyncTransforms();yield return new WaitForFixedUpdate();
        run.VehicleModel.Energy.Restore(5);Assert.That(controls.Activate(RRLScanKind.Structural),Is.False,"A group cannot scan when only one of two copies is affordable.");
        Assert.That(run.VehicleModel.Energy.Current,Is.EqualTo(5).Within(.01));
        // Position this isolated test above the range so its walls cannot
        // obscure a target beyond the standard 40 m perimeter.
        var target=run.GetComponent<R6TestRange>().TrainingTarget;
        foreach(var other in run.Encounter.Enemies.Where(e=>e!=null&&e!=target))other.gameObject.SetActive(false);
        var body=run.Player.GetComponent<Rigidbody>();body.isKinematic=true;var elevated=body.position+Vector3.up*150;body.position=elevated;body.transform.position=elevated;
        target.transform.position+=Vector3.up*150;Physics.SyncTransforms();
        parts[0].transform.rotation=Quaternion.LookRotation(Vector3.forward,Vector3.up);
        var coreOffset=target.Core.transform.position-target.transform.position;
        target.transform.position=parts[0].transform.position+parts[0].transform.forward*85-coreOffset;Physics.SyncTransforms();
        run.VehicleModel.Energy.Restore(100);float before=run.VehicleModel.Energy.Current;
        run.VehicleModel.Economy.StructuralScanRange=30;
        Assert.That(controls.Activate(RRLScanKind.Structural),Is.False,"Two configured 30 m copies must not reach a target 85 m away.");
        Assert.That(run.VehicleModel.Energy.Current,Is.EqualTo(before).Within(.01));run.VehicleModel.Economy.StructuralScanRange=50;
        var delta=target.Core.transform.position-parts[0].transform.position;
        var hits=Physics.RaycastAll(parts[0].transform.position,delta.normalized,delta.magnitude+1).OrderBy(h=>h.distance).Select(h=>h.collider.name+":"+h.distance.ToString("0.0"));
        Assert.That(controls.Activate(RRLScanKind.Structural),Is.True,$"Target distance {delta.magnitude:0.0}; angle {Vector3.Angle(parts[0].transform.forward,delta):0.0}; hits {string.Join(", ",hits)}");Assert.That(before-run.VehicleModel.Energy.Current,Is.EqualTo(10).Within(.01));
        Assert.That(parts.Select(p=>p.GetComponent<StructuralScanController>()).Any(s=>s.Contacts.Any(c=>c.OwnerKey==target.GetInstanceID())),Is.True,"Two scanners must reveal the real visible target beyond the one-copy range.");
        Assert.That(parts.Select(p=>p.GetComponent<StructuralScanController>()).All(s=>s.CooldownLeft>0),Is.True);
        Assert.That(controls.Activate(RRLScanKind.Structural),Is.False);
        foreach(var part in parts){part.GetComponent<StructuralScanController>().RestoreCooldown(0);part.GetComponent<RRLStructuralScannerBinding>().Device.State.DeviceReadyAt=0;}
        Assert.That(controls.Activate(RRLScanKind.Structural),Is.False,"Newly installed copies must not bypass the vehicle-wide cooldown.");
    }
    [UnityTest] public IEnumerator DestroyedStructuralScannerCopyDoesNotChargeEnergy()
    {
        Assert.That(run.OpenSandboxGarage(),Is.True);MountScanner(R3PartKind.StructuralScanner);MountScanner(R3PartKind.StructuralScanner);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var parts=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.StructuralScanner).ToArray();
        var damaged=parts[0];damaged.Damageable.ApplyDamageResolvedTyped(damaged.Damageable.CurrentHp/.75f+.01f,damaged.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(damaged.Operational,Is.False);Assert.That(parts[1].Operational,Is.True);Physics.SyncTransforms();yield return new WaitForFixedUpdate();
        run.VehicleModel.Energy.Restore(100);float before=run.VehicleModel.Energy.Current;
        Assert.That(run.Player.GetComponent<RRLScannerControls>().Activate(RRLScanKind.Structural),Is.True);
        Assert.That(before-run.VehicleModel.Energy.Current,Is.EqualTo(5).Within(.01));
    }
    [UnityTest] public IEnumerator StructuralScannerCopiesReachVisibleTargetBeyondOldOneKilometerCap()
    {
        Assert.That(run.OpenSandboxGarage(),Is.True);MountScanner(R3PartKind.StructuralScanner);MountScanner(R3PartKind.StructuralScanner);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;yield return new WaitForFixedUpdate();
        var parts=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.StructuralScanner).ToArray();
        var target=run.GetComponent<R6TestRange>().TrainingTarget;
        foreach(var other in run.Encounter.Enemies.Where(e=>e!=null&&e!=target))other.gameObject.SetActive(false);
        var body=run.Player.GetComponent<Rigidbody>();body.isKinematic=true;body.position+=Vector3.up*150;body.transform.position=body.position;
        target.transform.position+=Vector3.up*150;Physics.SyncTransforms();
        parts[0].transform.rotation=Quaternion.LookRotation(Vector3.forward,Vector3.up);
        var offset=target.Core.transform.position-target.transform.position;
        target.transform.position=parts[0].transform.position+Vector3.forward*1200-offset;Physics.SyncTransforms();
        var controls=run.Player.GetComponent<RRLScannerControls>();run.VehicleModel.Energy.Restore(100);
        run.VehicleModel.Economy.StructuralScanRange=500;
        Assert.That(controls.Activate(RRLScanKind.Structural),Is.False,"At a combined 1000 m the 1200 m target is outside range.");
        run.VehicleModel.Economy.StructuralScanRange=650;
        var delta=target.Core.transform.position-parts[0].transform.position;
        var hits=Physics.RaycastAll(parts[0].transform.position,delta.normalized,delta.magnitude+1).OrderBy(h=>h.distance).Select(h=>h.collider.name+":"+h.distance.ToString("0.0"));
        Assert.That(controls.Activate(RRLScanKind.Structural),Is.True,$"Two 650 m copies must reach the target; distance {delta.magnitude:0.0}, angle {Vector3.Angle(parts[0].transform.forward,delta):0.0}, hits {string.Join(", ",hits)}.");
        Assert.That(parts.Select(p=>p.GetComponent<StructuralScanController>()).Any(s=>s.Contacts.Any(c=>c.OwnerKey==target.GetInstanceID())),Is.True);
        yield return null;
    }
    [UnityTest] public IEnumerator TierThreeStructuralScannerRevealsDistantVisibleEnemyForItsTierCost()
    {
        Assert.That(run.OpenSandboxGarage(),Is.True);MountScanner(R3PartKind.StructuralScanner);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.StructuralScanner);
        yield return new WaitForFixedUpdate();
        part.GetComponent<RRLStructuralScannerBinding>().Device.State.Tier=3;
        var target=run.GetComponent<R6TestRange>().TrainingTarget;
        var body=run.Player.GetComponent<Rigidbody>();body.isKinematic=true;var elevated=body.position+Vector3.up*150;body.position=elevated;body.transform.position=elevated;
        target.transform.position+=Vector3.up*150;Physics.SyncTransforms();part.transform.rotation=Quaternion.LookRotation(Vector3.forward,Vector3.up);
        var offset=target.Core.transform.position-target.transform.position;
        target.transform.position=part.transform.position+part.transform.forward*85-offset;Physics.SyncTransforms();
        run.VehicleModel.Energy.Restore(100);float before=run.VehicleModel.Energy.Current;
        Assert.That(run.Player.GetComponent<RRLScannerControls>().Activate(RRLScanKind.Structural),Is.True);
        Assert.That(before-run.VehicleModel.Energy.Current,Is.EqualTo(7).Within(.01));
        Assert.That(part.GetComponent<StructuralScanController>().Contacts.Any(c=>c.OwnerKey==target.GetInstanceID()),Is.True);
        yield return null;
    }
    [UnityTest] public IEnumerator StructuralScannerUsesConfiguredCostAndSharedCooldown()
    {
        Assert.That(run.OpenSandboxGarage(),Is.True);MountScanner(R3PartKind.StructuralScanner);MountScanner(R3PartKind.StructuralScanner);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;yield return new WaitForFixedUpdate();
        var parts=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.StructuralScanner).ToArray();
        var controls=run.Player.GetComponent<RRLScannerControls>();var config=run.VehicleModel.Economy;
        config.StructuralScanRange=100;config.StructuralScanEnergy=float.NaN;config.StructuralScanCooldown=12;
        run.VehicleModel.Energy.Restore(100);float before=run.VehicleModel.Energy.Current;
        Assert.That(controls.Activate(RRLScanKind.Structural),Is.False);Assert.That(run.VehicleModel.Energy.Current,Is.EqualTo(before).Within(.01));
        config.StructuralScanEnergy=3;
        Assert.That(controls.Activate(RRLScanKind.Structural),Is.True);Assert.That(before-run.VehicleModel.Energy.Current,Is.EqualTo(6).Within(.01));
        Assert.That(parts.All(p=>p.GetComponent<StructuralScanController>().CooldownLeft>11.9f&&p.GetComponent<StructuralScanController>().CooldownLeft<=12.01f),Is.True);
        Assert.That(run.VehicleModel.StructuralScannerReadyAt-Time.time,Is.InRange(11.9f,12.01f));
    }
    [UnityTest] public IEnumerator PulseCopiesUseOneRangeCostAndCooldown()
    {
        kind=R3PartKind.ScannerPulse;yield return Build(copies:2);
        var controls=run.Player.GetComponent<RRLScannerControls>();var parts=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==kind).ToArray();
        Assert.That(parts.Length,Is.EqualTo(2));run.VehicleModel.Energy.Restore(5);
        Assert.That(controls.Activate(RRLScanKind.Pulse),Is.False,"Both operational modules must be affordable before scanning.");
        Assert.That(run.VehicleModel.Energy.Current,Is.EqualTo(5).Within(.001));
        run.VehicleModel.Energy.Restore(100);var before=run.VehicleModel.Energy.Current;
        var target=run.GetComponent<R6TestRange>().TrainingTarget;target.transform.position=run.Player.transform.position+Vector3.forward*30;Physics.SyncTransforms();
        Assert.That(controls.Activate(RRLScanKind.Pulse),Is.True);
        Assert.That(before-run.VehicleModel.Energy.Current,Is.EqualTo(10).Within(.01));
        Assert.That(parts.Select(p=>p.GetComponent<ScannerPulse>()).Any(p=>p.Revealed.Any(s=>s.transform.IsChildOf(target.transform))),Is.True,"Both copies extend the actual detection range.");
        Assert.That(parts.All(p=>p.GetComponent<ScannerPulse>().RemainingCooldown>0),Is.True);
        Assert.That(controls.Activate(RRLScanKind.Pulse),Is.False,"A second copy cannot bypass the shared cooldown.");
        // Simulate freshly mounted hardware: its individual timers are empty,
        // while the vehicle-wide cooldown must still deny activation.
        foreach(var part in parts){part.GetComponent<ScannerPulse>().RestoreCooldown(0);part.GetComponent<RRLPulseScannerBinding>().Device.State.DeviceReadyAt=0;}
        float remainingEnergy=run.VehicleModel.Energy.Current;
        Assert.That(controls.Activate(RRLScanKind.Pulse),Is.False);
        Assert.That(run.VehicleModel.Energy.Current,Is.EqualTo(remainingEnergy).Within(.001));
    }
    [UnityTest] public IEnumerator DestroyedPulseCopyStopsContributingRangeAndCost()
    {
        kind=R3PartKind.ScannerPulse;yield return Build(copies:2);
        var controls=run.Player.GetComponent<RRLScannerControls>();var parts=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==kind).ToArray();
        var destroyed=parts[0];destroyed.Damageable.ApplyDamage(destroyed.Damageable.CurrentHp/.75f+.01f,destroyed.transform.position,Vector3.up);
        Assert.That(destroyed.Operational,Is.False);Assert.That(parts[1].Operational,Is.True);
        run.VehicleModel.Energy.Restore(100);float before=run.VehicleModel.Energy.Current;
        Assert.That(controls.Activate(RRLScanKind.Pulse),Is.True);
        Assert.That(before-run.VehicleModel.Energy.Current,Is.EqualTo(5).Within(.01));
        Assert.That(parts[1].GetComponent<ScannerPulse>().RadiusMeters,Is.EqualTo(20).Within(.01));
    }
    [UnityTest] public IEnumerator TierThreePulseIncreasesRealRangeAndActivationCost()
    {
        kind=R3PartKind.ScannerPulse;yield return Build();
        var binding=module.GetComponent<RRLPulseScannerBinding>();binding.Device.State.Tier=3;
        var controls=run.Player.GetComponent<RRLScannerControls>();
        var target=run.GetComponent<R6TestRange>().TrainingTarget;target.transform.position=run.Player.transform.position+Vector3.forward*65;Physics.SyncTransforms();
        run.VehicleModel.Energy.Restore(100);float before=run.VehicleModel.Energy.Current;
        Assert.That(controls.Activate(RRLScanKind.Pulse),Is.True);
        Assert.That(pulse.RadiusMeters,Is.EqualTo(80).Within(.01));
        Assert.That(pulse.Revealed.Any(s=>s.transform.IsChildOf(target.transform)),Is.True,"A tier-three pulse must detect the actual target beyond tier-one range.");
        Assert.That(before-run.VehicleModel.Energy.Current,Is.EqualTo(7).Within(.01));
    }
    [UnityTest] public IEnumerator MixedTierPulseCopiesCombineRangeAndCost()
    {
        kind=R3PartKind.ScannerPulse;yield return Build(copies:2);
        var parts=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==kind).ToArray();
        parts[1].GetComponent<RRLPulseScannerBinding>().Device.State.Tier=2;
        var controls=run.Player.GetComponent<RRLScannerControls>();
        var target=run.GetComponent<R6TestRange>().TrainingTarget;target.transform.position=run.Player.transform.position+Vector3.forward*50;Physics.SyncTransforms();
        run.VehicleModel.Energy.Restore(10);Assert.That(controls.Activate(RRLScanKind.Pulse),Is.False);
        run.VehicleModel.Energy.Restore(100);float before=run.VehicleModel.Energy.Current;
        Assert.That(controls.Activate(RRLScanKind.Pulse),Is.True);
        Assert.That(before-run.VehicleModel.Energy.Current,Is.EqualTo(11).Within(.01));
        var scans=parts.Select(p=>p.GetComponent<ScannerPulse>()).ToArray();
        Assert.That(scans.Any(s=>Mathf.Abs(s.RadiusMeters-60)<.01f&&s.Revealed.Any(t=>t.transform.IsChildOf(target.transform))),Is.True);
    }
    [UnityTest] public IEnumerator ScannerOnlyVehicleRespondsToItsAssignedActiveSlot()
    {
        kind=R3PartKind.ScannerPulse;yield return Build();
        Assert.That(run.Player.GetComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>(),Is.Null,"The fixture must not rely on a movement module.");
        int slot=Array.FindIndex(run.VehicleModel.GetActiveSlots(),p=>p==kind);Assert.That(slot,Is.InRange(0,3));
        var oldSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(oldSettings);
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings=settings;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            var key=(Key)((int)Key.Digit1+slot);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));
            yield return null;yield return null;
            Assert.That(pulse.RemainingCooldown,Is.GreaterThan(0),"The real input loop must activate the installed scanner.");
        }
        finally{InputSystem.RemoveDevice(keyboard);InputSystem.settings=oldSettings;UnityEngine.Object.Destroy(settings);}
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
        string id=state.Id;float ready=state.DeviceReadyAt;Assert.That(R3VehicleModel.OccupiedCells(state).Count(),Is.EqualTo(1));
        // Destroy this device without wiping out its support through the current
        // connected-damage model; its recovery is the behavior under test.
        module.Damageable.ApplyDamage(module.Damageable.CurrentHp/.75f+.01f,module.transform.position,Vector3.up);assembly.Capture();Assert.That(state.Health,Is.Zero);Assert.That(Activate(),Is.False);
        var copy=run.VehicleModel.DeepCopy();int scrap=copy.Scrap;Assert.That(copy.Repair(state.Cell),Is.True);Assert.That(copy.Scrap,Is.LessThan(scrap));
        var vehicle=R3VehicleAssembly.Build(copy,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        var restored=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);Assert.That(restored.GetComponent<ScannerPulse>().RemainingCooldown,Is.GreaterThan(0));Assert.That(Activate(restored),Is.False);
        Assert.That(copy.Parts.Single(p=>p.Id==id).DeviceReadyAt,Is.EqualTo(ready));yield return null;
    }
    [UnityTest] public IEnumerator PlayerGuiSlotUsesOwnModuleAndRejectsPausedOrDestroyedActivation()
    {
        Assert.That(Enum.TryParse("ScannerPulse",out kind),Is.True,"SEN must be an owned physical garage module");
        yield return Build();var controls=run.Player.GetComponent<RRLScannerControls>();Assert.That(controls,Is.Not.Null);
        int slot=Array.FindIndex(run.VehicleModel.GetActiveSlots(),p=>p==R3PartKind.ScannerPulse);
        Assert.That(slot,Is.InRange(0,3));
        var key=new Event{type=EventType.KeyDown,keyCode=(KeyCode)((int)KeyCode.Alpha1+slot)};
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
