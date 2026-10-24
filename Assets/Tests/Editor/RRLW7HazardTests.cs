using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.HAZ001;
using RogueMachine.P12PipelineSafe;

public class RRLW7HazardTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;run.StartRun();run.ChooseRoute(0);
        while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);R1EncounterManager.ClearTransientObjects();
    }
    TelegraphedHazardZone Zone()
    {
        var zone=run.Arena.Layout.GetComponentsInChildren<TelegraphedHazardZone>().FirstOrDefault();
        Assert.That(zone,Is.Not.Null,"HAZ001 must warn and damage in the real Gauntlet arena");return zone;
    }
    void MoveInto(TelegraphedHazardZone zone)
    {
        run.Player.MoveTo(new Vector3(zone.transform.position.x,run.Player.transform.position.y,zone.transform.position.z),Quaternion.identity);
        Physics.SyncTransforms();
    }
    [UnityTest] public IEnumerator RealHazardTelegraphsBeforeDamagingOneInsidePhysicalPart()
    {
        var zone=Zone();MoveInto(zone);var parts=run.Player.GetComponentsInChildren<P12PipelineSafeDamageable>();
        var before=parts.ToDictionary(p=>p,p=>p.CurrentHp);float total=before.Values.Sum();
        float until=Time.time+.25f;while(Time.time<until)yield return null;
        Assert.That(zone.Phase,Is.EqualTo(HazardPhase.Telegraph));Assert.That(parts.Sum(p=>p.CurrentHp),Is.EqualTo(total));
        var ring=zone.GetComponentInChildren<LineRenderer>();Assert.That(ring,Is.Not.Null);Assert.That(ring.enabled,Is.True);
        Assert.That(ring.startColor,Is.EqualTo(Color.yellow));
        until=Time.time+2;while(Time.time<until&&parts.Sum(p=>p.CurrentHp)==total)yield return null;
        var changed=parts.Where(p=>p.CurrentHp<before[p]).ToArray();Assert.That(changed.Length,Is.EqualTo(1),"Multi-collider vehicle takes one physical part hit per cadence");
        Assert.That(total-parts.Sum(p=>p.CurrentHp),Is.EqualTo(5).Within(.001));Assert.That(zone.Phase,Is.EqualTo(HazardPhase.Active));
        var hit=changed[0].GetComponent<Collider>();Assert.That(Vector3.Distance(hit.ClosestPoint(zone.transform.position),zone.transform.position),Is.LessThanOrEqualTo(zone.Radius));
        Assert.That(ring.startColor,Is.EqualTo(Color.red));
    }
    [UnityTest] public IEnumerator PauseAndLeavingCombatStopHazardDamageAndOwnedWarning()
    {
        var zone=Zone();MoveInto(zone);yield return null;
        zone.ResetCycle();Assert.That(zone.Begin(),Is.True);zone.ManualTick(1.5f);
        var parts=run.Player.GetComponentsInChildren<P12PipelineSafeDamageable>();float before=parts.Sum(p=>p.CurrentHp);
        run.TogglePause();zone.ScanAtTime(Time.time+10);Assert.That(parts.Sum(p=>p.CurrentHp),Is.EqualTo(before),"Even explicit scan requests cannot bypass paused gameplay");
        run.TogglePause();foreach(var enemy in run.Encounter.Enemies)enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        while(run.Progress.State==R1RunState.Combat)yield return null;yield return null;
        Assert.That(zone.enabled,Is.False);Assert.That(zone.Phase,Is.EqualTo(HazardPhase.Idle));
        Assert.That(zone.GetComponentInChildren<LineRenderer>().enabled,Is.False);
        before=parts.Sum(p=>p.CurrentHp);Assert.That(zone.ScanAtTime(Time.time+20),Is.Zero);Assert.That(parts.Sum(p=>p.CurrentHp),Is.EqualTo(before));
    }
    [UnityTest] public IEnumerator LiveQueryBudgetHandlesTwoPartDenseVehiclesWithoutTruncation()
    {
        var zone=Zone();var groups=new P12PipelineSafeDamageable[2][];
        for(int i=0;i<2;i++)
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Heavy));
            var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),
                zone.transform.position+new Vector3(i*.15f,.7f,0),Quaternion.identity);
            vehicle.GetComponent<Rigidbody>().isKinematic=true;groups[i]=vehicle.GetComponentsInChildren<P12PipelineSafeDamageable>();
        }
        Physics.SyncTransforms();float settle=Time.time+.04f;while(Time.time<settle)yield return null;
        zone.ResetCycle();Assert.That(zone.Begin(),Is.True);zone.ManualTick(1.5f);
        float[] before=groups.Select(parts=>parts.Sum(p=>p.CurrentHp)).ToArray();zone.ScanAtTime(Time.time);
        Assert.That(zone.LastScanTruncated,Is.False,"Live hazard query must fit real physical-part density, not silently truncate the original64-collider buffer");
        for(int i=0;i<2;i++)Assert.That(before[i]-groups[i].Sum(p=>p.CurrentHp),Is.EqualTo(5).Within(.001),"Both real vehicles receive one part hit, not one per collider");
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
