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
using RoboRougelike.Mechanics.StructuralScan;
using RoboRougelike.Mechanics.EMP;
using RogueMachine.P12PipelineSafe;

public class RRLW8StructuralScanTests
{
    R1RunController run;R3MountedPart module;StructuralScanController scanner;P12PipelineSafeEnemyTurret target;R3PartKind kind;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
        if(!Enum.TryParse("StructuralScanner",out kind))yield break;
        Assert.That(run.OpenSandboxGarage(),Is.True);Assert.That(run.GarageBuy(new Vector3Int(0,2,-2),kind),Is.True);
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Kind==kind).Id,Is.Not.Empty);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        module=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==kind);
        scanner=module.GetComponent<StructuralScanController>();Assert.That(scanner,Is.Not.Null);
        target=run.GetComponent<R6TestRange>().TrainingTarget;
        Physics.SyncTransforms();yield return new WaitForFixedUpdate();
    }
    bool Activate(R3MountedPart part=null)
    {
        var binding=(part??module).GetComponents<MonoBehaviour>().SingleOrDefault(c=>c.GetType().Name=="RRLStructuralScannerBinding");
        Assert.That(binding,Is.Not.Null,"Original scan controller must be connected to the shared physical capability boundary");
        return (bool)binding.GetType().GetMethod("TryActivate").Invoke(binding,null);
    }
    [UnityTest] public IEnumerator ActualRangeModuleScansForeignVisiblePartsWithoutChangingHpOrPhysics()
    {
        Assert.That(module,Is.Not.Null,"COM must be an owned physical garage module, not a free root component");
        var hp=target.GetComponentsInChildren<P12PipelineSafeDamageable>();var before=hp.Select(p=>p.CurrentHp).ToArray();
        var body=run.Player.GetComponent<Rigidbody>();float mass=body.mass;var position=body.position;var velocity=body.linearVelocity;
        Assert.That(Activate(),Is.True);Assert.That(scanner.Contacts.Count,Is.GreaterThan(0).And.LessThanOrEqualTo(16));
        Assert.That(scanner.Contacts.All(c=>c.OwnerKey==target.GetInstanceID()),Is.True);
        Assert.That(hp.Select(p=>p.CurrentHp),Is.EqualTo(before));Assert.That(body.mass,Is.EqualTo(mass));Assert.That(body.position,Is.EqualTo(position));Assert.That(body.linearVelocity,Is.EqualTo(velocity));
        var contacts=scanner.Contacts.Select(c=>c.PartKey).ToArray();Assert.That(Activate(),Is.False);
        Assert.That(scanner.Contacts.Select(c=>c.PartKey),Is.EqualTo(contacts),"Cooldown retry must preserve the live snapshot");
        Assert.That(scanner.CooldownLeft,Is.InRange(7.9f,8.01f));yield return null;
    }
    [UnityTest] public IEnumerator SolidOccluderBlocksStructuralHpAndFailedScanDoesNotConsumeCooldown()
    {
        Assert.That(module,Is.Not.Null,"COM physical module missing");
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,3,-23.5f);wall.transform.localScale=new Vector3(50,10,1);
        Physics.SyncTransforms();yield return new WaitForFixedUpdate();Assert.That(Activate(),Is.False);
        Assert.That(scanner.Contacts,Is.Empty);Assert.That(scanner.CooldownLeft,Is.Zero);
        UnityEngine.Object.Destroy(wall);yield return null;Physics.SyncTransforms();yield return new WaitForFixedUpdate();
        Assert.That(Activate(),Is.True,"Visible target becomes scannable only after the actual solid blocker is removed");
    }
    [UnityTest] public IEnumerator PausedSnapshotFreezesButEmpClearsResultsWithoutResettingCooldown()
    {
        Assert.That(module,Is.Not.Null,"COM physical module missing");
        Assert.That(Activate(),Is.True);var contacts=scanner.Contacts.Select(c=>c.PartKey).ToArray();float cooldown=scanner.CooldownLeft;
        run.TogglePause();yield return new WaitForSecondsRealtime(.2f);Assert.That(Activate(),Is.False);
        Assert.That(scanner.Contacts.Select(c=>c.PartKey),Is.EqualTo(contacts));Assert.That(scanner.CooldownLeft,Is.EqualTo(cooldown).Within(.001));
        run.TogglePause();float hp=module.Damageable.CurrentHp;var body=run.Player.GetComponent<Rigidbody>();var velocity=body.linearVelocity;float mass=body.mass;
        var receiver=module.GetComponent<EmpReceiver>();Assert.That(receiver,Is.Not.Null);receiver.ApplyEmp(1);
        Assert.That(Activate(),Is.False);Assert.That(scanner.Contacts,Is.Empty);Assert.That(module.Damageable.CurrentHp,Is.EqualTo(hp));Assert.That(body.mass,Is.EqualTo(mass));Assert.That(body.linearVelocity,Is.EqualTo(velocity));
        float recovery=Time.time+1.1f;while(Time.time<recovery)yield return null;
        Assert.That(receiver.IsDisrupted,Is.False);Assert.That(scanner.CooldownLeft,Is.GreaterThan(6));Assert.That(Activate(),Is.False,"EMP expiration must not grant a free second scan");
    }
    [UnityTest] public IEnumerator DestroyedPhysicalScannerKeepsIdentityAndCooldownThroughPaidRepairAndRebuild()
    {
        Assert.That(module,Is.Not.Null,"COM physical module missing");
        Assert.That(Activate(),Is.True);var assembly=run.Player.GetComponent<R3VehicleAssembly>();var model=assembly.Model;
        var state=model.Parts.Single(p=>p.Id==module.Id);Assert.That(R3VehicleModel.OccupiedCells(state).Count(),Is.EqualTo(4));Assert.That(module.GetComponents<P12PipelineSafeDamageable>().Length,Is.EqualTo(1));
        string id=state.Id;float readyAt=state.DeviceReadyAt;Assert.That(readyAt,Is.GreaterThan(Time.time));
        module.Damageable.ApplyDamage(100000,module.transform.position,Vector3.up);assembly.Capture();
        Assert.That(Activate(),Is.False);Assert.That(scanner.Contacts,Is.Empty);Assert.That(state.Health,Is.Zero);Assert.That(state.DeviceReadyAt,Is.EqualTo(readyAt));
        var copy=model.DeepCopy();int scrap=copy.Scrap;Assert.That(copy.Repair(state.Cell),Is.True);Assert.That(copy.Scrap,Is.LessThan(scrap));
        var rig=R3VehicleAssembly.Build(copy,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        var restored=rig.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);var source=restored.GetComponent<StructuralScanController>();
        Assert.That(copy.Parts.Single(p=>p.Id==id).Health,Is.EqualTo(1));Assert.That(source.CooldownLeft,Is.GreaterThan(0));Assert.That(Activate(restored),Is.False);
        Assert.That(copy.Remove(state.Cell),Is.True);Assert.That(copy.DeepCopy().Inventory.Single(p=>p.Id==id).DeviceReadyAt,Is.EqualTo(readyAt));yield return null;
    }
    [UnityTest] public IEnumerator PlayerGuiActivationHonorsPauseCooldownAndPhysicalLoss()
    {
        Assert.That(module,Is.Not.Null,"COM physical module missing");
        var controls=run.Player.GetComponents<MonoBehaviour>().SingleOrDefault(c=>c.GetType().Name=="RRLScannerControls");
        Assert.That(controls,Is.Not.Null,"Player must have an actual scanner input path");
        var method=controls.GetType().GetMethod("HandleGuiInput");Assert.That(method,Is.Not.Null);
        var key=new Event{type=EventType.KeyDown,keyCode=KeyCode.None,character='k'};
        run.TogglePause();Assert.That((bool)method.Invoke(controls,new object[]{key}),Is.False);Assert.That(scanner.CooldownLeft,Is.Zero);
        run.TogglePause();Assert.That((bool)method.Invoke(controls,new object[]{key}),Is.True);Assert.That(scanner.Contacts,Is.Not.Empty);
        float cooldown=scanner.CooldownLeft;Assert.That((bool)method.Invoke(controls,new object[]{key}),Is.False);Assert.That(scanner.CooldownLeft,Is.EqualTo(cooldown));
        module.Damageable.ApplyDamage(100000,module.transform.position,Vector3.up);Assert.That((bool)method.Invoke(controls,new object[]{key}),Is.False);yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
