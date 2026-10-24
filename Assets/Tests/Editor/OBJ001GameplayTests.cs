using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.OBJ001Integration;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using Object=UnityEngine.Object;

public class OBJ001GameplayTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return Until(()=>R1RunController.Instance?.Progress!=null);run=R1RunController.Instance;
        Assert.That(run.SelectVehicle(R3VehicleKind.Heavy),Is.True);Assert.That(run.PrepareReactorRun(),Is.True);
        Assert.That(run.ChooseReactorDefense(),Is.True);yield return Until(()=>!run.Transitioning);
        run.Encounter.SetCombatActive(false);
    }
    static IEnumerator Until(Func<bool> predicate,float timeout=8)
    {float end=Time.realtimeSinceStartup+timeout;while(!predicate()&&Time.realtimeSinceStartup<end)yield return null;Assert.That(predicate(),Is.True,"Timed out waiting for real game transition");}
    static IEnumerator Seconds(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}
    void ClearWave(){foreach(var e in run.Encounter.Enemies.ToArray())e.Core.ApplyDamage(100000,e.Core.transform.position,Vector3.zero);}
    IEnumerator NextWave(int number){ClearWave();yield return Until(()=>run.DefenseWave==number);run.Encounter.SetCombatActive(false);}
    [UnityTest] public IEnumerator ThreeWavesPreserveDamageAndOneSalvageLedgerThenReturnToOrdinaryFight()
    {
        var ledger=run.Salvage;var damaged=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor);
        damaged.Damageable.ApplyDamage(10,damaged.transform.position,Vector3.zero);float condition=damaged.Damageable.CurrentHp/damaged.Damageable.MaxHp;
        for(int wave=1;wave<=3;wave++)
        {
            Assert.That(run.DefenseWave,Is.EqualTo(wave));Assert.That(run.Encounter.AliveCount,Is.EqualTo(wave==3?3:2));
            Assert.That(run.Salvage,Is.SameAs(ledger));Assert.That(run.VehicleModel.Scrap,Is.Zero);Assert.That(run.Progress.CompletedEncounters,Is.Zero);
            ClearWave();if(wave<3){yield return Until(()=>run.DefenseWave==wave+1);run.Encounter.SetCombatActive(false);}
        }
        yield return Until(()=>run.Progress.State==R1RunState.Salvage);
        Assert.That(run.DefeatedEnemies,Is.EqualTo(7));Assert.That(run.Progress.CompletedEncounters,Is.EqualTo(1));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(run.CurrentEncounter.ScrapReward));Assert.That(run.LastActivityWasReactor,Is.True);
        Assert.That(run.ReactorActive,Is.False);Assert.That(run.ReactorHp,Is.GreaterThan(0));
        Assert.That(ledger.Items.Count,Is.GreaterThan(0));Assert.That(ledger.Items.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(ledger.Items.Count));
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==damaged.Id).Health,Is.EqualTo(condition).Within(.001f));
        string loot=ledger.Items.First(p=>p.Selectable).Id;
        Assert.That(run.ConfirmRewards(new[]{loot},new[]{0}),Is.True);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(2));Assert.That(run.ConfirmRewards(new[]{loot},new[]{0}),Is.False);
        Assert.That(run.StartNextEncounter()&&run.ChooseRoute(0),Is.True);yield return Until(()=>!run.Transitioning);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));Assert.That(run.ReactorActive||run.LastActivityWasReactor,Is.False);
        Assert.That(Object.FindObjectsByType<OBJ001ReactorActor>(FindObjectsSortMode.None),Is.Empty);
    }
    [UnityTest] public IEnumerator FatalReactorDamageBeatsFinalClearAndRestartRemovesActivityObjects()
    {
        yield return NextWave(2);yield return NextWave(3);
        var hp=run.ReactorActor.Damageable;hp.ApplyDamage(100000,hp.transform.position,Vector3.zero);ClearWave();
        yield return Until(()=>run.Progress.State==R1RunState.Lost);
        Assert.That(run.Progress.DefeatReason,Does.Contain("Reaktor"));Assert.That(run.VehicleModel.Scrap,Is.Zero);
        Assert.That(run.Progress.CompletedEncounters,Is.Zero);Assert.That(run.ConfirmRewards(Array.Empty<string>(),new[]{0}),Is.False);
        var before=run;run.RestartRun();yield return Until(()=>R1RunController.Instance!=before&&R1RunController.Instance?.Progress!=null);run=R1RunController.Instance;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Ready));Assert.That(run.DefenseWave,Is.Zero);
        Assert.That(Object.FindObjectsByType<OBJ001ReactorActor>(FindObjectsSortMode.None),Is.Empty);
        Assert.That(Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None),Is.Empty);
        Assert.That(run.PrepareReactorRun()&&run.ChooseReactorDefense(),Is.True);yield return Until(()=>!run.Transitioning);
        Assert.That(run.ReactorHp,Is.EqualTo(600));Assert.That(run.DefenseWave,Is.EqualTo(1));Assert.That(run.Encounter.AliveCount,Is.EqualTo(2));
    }
    [UnityTest] public IEnumerator PauseCannotAdvanceIntermissionAndDuplicateEntryCannotGrantBudget()
    {
        Assert.That(run.ChooseReactorDefense()||run.PrepareReactorRun(),Is.False);
        Assert.That(Object.FindObjectsByType<OBJ001ReactorActor>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
        ClearWave();yield return Until(()=>run.DefenseIntermission>0);run.TogglePause();float remaining=run.DefenseIntermission;
        yield return new WaitForSecondsRealtime(.3f);
        Assert.That(run.DefenseWave,Is.EqualTo(1));Assert.That(run.DefenseIntermission,Is.EqualTo(remaining).Within(.001f));
        Assert.That(run.VehicleModel.Scrap,Is.Zero);run.TogglePause();yield return Until(()=>run.DefenseWave==2);
    }
    [UnityTest] public IEnumerator SandboxWorkshopCannotLaunderItsFreeBudgetIntoRewardActivity()
    {
        var before=run;run.RestartRun();yield return Until(()=>R1RunController.Instance!=before&&R1RunController.Instance?.Progress!=null);run=R1RunController.Instance;
        Assert.That(run.OpenSandboxGarage(),Is.True);int scrap=run.VehicleModel.Scrap;
        Assert.That(run.CanChooseReactorDefense||run.ChooseReactorDefense()||run.PrepareReactorRun(),Is.False);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
    }
    GameObject Shot(Transform owner)
    {
        var actor=run.ReactorActor;var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="OBJ001 explicit physical test round";
        g.transform.position=actor.transform.position+Vector3.back*5;g.transform.localScale=Vector3.one*.12f;
        var body=g.AddComponent<Rigidbody>();body.useGravity=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearVelocity=Vector3.forward*25;
        g.AddComponent<P17PhysicalShell>().Configure(owner,25,2,Vector3.forward,actor.transform);return g;
    }
    [UnityTest] public IEnumerator RealHostileProjectileDamagesCanonicalReactorWhileFriendlyFireIsFiltered()
    {
        var actor=run.ReactorActor;Assert.That(RRLHitRouter.Team(actor.transform),Is.EqualTo(1));
        var friendly=Shot(run.Player.transform);yield return Seconds(.4f);Assert.That(actor.CurrentIntegrity,Is.EqualTo(600));Object.Destroy(friendly);
        Shot(run.Encounter.Enemies[0].transform);yield return Seconds(.4f);
        Assert.That(actor.CurrentIntegrity,Is.EqualTo(575));Assert.That(actor.Objective.Integrity,Is.EqualTo(actor.Damageable.CurrentHp));
    }
    static void PositionEnemy(P12PipelineSafeEnemyTurret enemy,Vector3 point,Vector3 aim)
    {
        var rb=enemy.GetComponent<Rigidbody>();rb.interpolation=RigidbodyInterpolation.None;
        enemy.transform.SetPositionAndRotation(point,Quaternion.identity);rb.position=point;rb.rotation=Quaternion.identity;
        rb.linearVelocity=rb.angularVelocity=Vector3.zero;enemy.ConfigureMovement(0,12,0);
        var yaw=enemy.GetComponentsInChildren<Transform>().Single(t=>t.name=="Enemy_Yaw");yaw.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(aim-point,Vector3.up));
    }
    [UnityTest] public IEnumerator RealEnemiesRetargetAndPhysicallyShootBothPlayerAndReactor()
    {
        run.Player.MoveTo(new Vector3(15,.7f,-17),Quaternion.identity);
        var enemies=run.Encounter.Enemies.ToArray();PositionEnemy(enemies[0],new Vector3(-16,0,-13),run.ReactorActor.transform.position);
        PositionEnemy(enemies[1],new Vector3(15,0,-5),run.Player.transform.position);Physics.SyncTransforms();
        float before=run.Player.GetComponent<R3VehicleAssembly>().Parts.Sum(p=>p.Damageable!=null?p.Damageable.CurrentHp:0);
        run.Encounter.SetCombatActive(true);yield return Seconds(5);
        float after=run.Player.GetComponent<R3VehicleAssembly>().Parts.Sum(p=>p.Damageable!=null?p.Damageable.CurrentHp:0);
        Debug.Log($"OBJ001 actual AI reactorHP={run.ReactorHp}, playerHP={before}->{after}, targets={enemies[0].EncounterTarget?.name}/{enemies[1].EncounterTarget?.name}");
        Assert.That(enemies[0].EncounterTarget,Is.EqualTo(run.ReactorActor.transform));Assert.That(enemies[1].EncounterTarget,Is.EqualTo(run.Player.transform));
        Assert.That(run.ReactorHp,Is.LessThan(600));Assert.That(after,Is.LessThan(before));
    }
    [UnityTest] public IEnumerator NearbyPlayerDrawsAnAttackerAwayFromReactorAndDistanceRestoresItsObjective()
    {
        var enemy=run.Encounter.Enemies[0];PositionEnemy(enemy,new Vector3(0,0,6),run.ReactorActor.transform.position);Physics.SyncTransforms();
        yield return Seconds(.6f);Assert.That(enemy.EncounterTarget,Is.EqualTo(run.ReactorActor.transform));
        run.Player.MoveTo(new Vector3(0,.7f,12),Quaternion.identity);yield return Seconds(.6f);Assert.That(enemy.EncounterTarget,Is.EqualTo(run.Player.transform));
        run.Player.MoveTo(new Vector3(0,.7f,-22),Quaternion.identity);yield return Seconds(.6f);Assert.That(enemy.EncounterTarget,Is.EqualTo(run.ReactorActor.transform));
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
