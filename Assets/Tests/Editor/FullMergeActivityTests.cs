using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.MapSystemR4;
using RoboRougelike.Mechanics.RAC001;

public sealed class FullMergeActivityTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;run.RequestedSeed=7;Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;yield return null;
    }
    object Get(string name)
    {var p=typeof(R1RunController).GetProperty(name);Assert.That(p,Is.Not.Null,"Missing actual activity receiver property "+name);return p.GetValue(run);}
    bool Choose(string name,params object[] args)
    {var method=typeof(R1RunController).GetMethod(name);Assert.That(method,Is.Not.Null,"Missing player-reachable activity "+name);return (bool)method.Invoke(run,args);}
    IEnumerator Ready()
    {float end=Time.realtimeSinceStartup+8;while(run.Transitioning&&Time.realtimeSinceStartup<end)yield return null;Assert.That(run.Transitioning,Is.False);}
    [UnityTest] public IEnumerator RaceUsesSameRobotWithoutEmptyEnemyAutoVictoryAndPausesItsClock()
    {
        var model=run.VehicleModel;Assert.That(Choose("ChooseCheckpointRace"),Is.True);yield return Ready();
        var race=(CheckpointRaceChallenge)Get("CheckpointRace");Assert.That(race,Is.Not.Null);Assert.That(race.Running,Is.True);
        Assert.That(run.VehicleModel,Is.SameAs(model));Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));
        Assert.That(run.Encounter.AliveCount,Is.Zero);Assert.That(run.GeneratedWorld,Is.Not.Null);
        yield return new WaitForSeconds(.1f);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));
        run.TogglePause();float elapsed=race.ElapsedSeconds;yield return new WaitForSecondsRealtime(.2f);
        Assert.That(race.ElapsedSeconds,Is.EqualTo(elapsed).Within(.0001f));run.TogglePause();
    }
    [UnityTest] public IEnumerator RaceTimeoutUsesCanonicalLossAndCleansItsMap()
    {
        Assert.That(Choose("ChooseCheckpointRace"),Is.True);yield return Ready();
        var race=(CheckpointRaceChallenge)Get("CheckpointRace");race.enabled=false;race.Advance(1000);
        yield return null;yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Lost));Assert.That(run.VehicleModel.Inventory,Is.Empty);
        Assert.That(run.GeneratedWorld,Is.Null);Assert.That(Get("CheckpointRace"),Is.Null);
    }
    [UnityTest] public IEnumerator RoadTimeAttackHasRealSourceWorldAndSharedVehicle()
    {
        var model=run.VehicleModel;Assert.That(Choose("ChooseRoadChallenge",MapR4Mode.TimeAttack),Is.True);yield return Ready();
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));Assert.That(run.VehicleModel,Is.SameAs(model));
        var world=(MapR4BuildResult)Get("RoadWorld");var challenge=(MapR4ChallengeRuntime)Get("RoadChallenge");
        Assert.That(world,Is.Not.Null);Assert.That(world.Edges.Count,Is.GreaterThan(4));
        Assert.That(world.Root.GetComponentsInChildren<MeshCollider>().Length,Is.GreaterThan(4));
        Assert.That(challenge.Mode,Is.EqualTo(MapR4Mode.TimeAttack));Assert.That(challenge.Status,Is.EqualTo(MapR4Status.Running));
        yield return new WaitForSeconds(.1f);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));
        Assert.That(run.Player.GetComponent<Rigidbody>(),Is.Not.Null);
    }
    [UnityTest] public IEnumerator RoadResultUsesOneCanonicalRewardAndAllowsNextActivity()
    {
        Assert.That(Choose("ChooseRoadChallenge",MapR4Mode.Escape),Is.True);yield return Ready();
        var challenge=(MapR4ChallengeRuntime)Get("RoadChallenge");
        // Explicit API-controlled result test; real driving is a separate Player acceptance.
        challenge.NotifyAnchor("exit_east");yield return null;yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));Assert.That(run.Progress.CompletedEncounters,Is.EqualTo(1));
        Assert.That(Get("RoadWorld"),Is.Null);Assert.That(run.ConfirmRewards(Array.Empty<string>(),new[]{0}),Is.True);
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(1));int scrap=run.VehicleModel.Scrap;
        Assert.That(run.ConfirmRewards(Array.Empty<string>(),new[]{0}),Is.False);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(Choose("ChooseCheckpointRace"),Is.True,run.Garage.Message+" deploy="+run.VehicleModel.DeployError);yield return Ready();
        Assert.That(Get("CheckpointRace"),Is.Not.Null);Assert.That(Get("RoadWorld"),Is.Null);
    }
    [UnityTest] public IEnumerator RoadFailureDoesNotRewardAndRestartsWithoutActivityObjects()
    {
        Assert.That(Choose("ChooseRoadChallenge",MapR4Mode.TimeAttack),Is.True);yield return Ready();
        ((MapR4ChallengeRuntime)Get("RoadChallenge")).NotifyPlayerDestroyed();yield return null;yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Lost));Assert.That(run.VehicleModel.Inventory,Is.Empty);
        Assert.That(Get("RoadWorld"),Is.Null);var old=run;run.RestartRun();
        float end=Time.realtimeSinceStartup+8;while((R1RunController.Instance==old||R1RunController.Instance?.Progress==null)&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(R1RunController.Instance,Is.Not.SameAs(old));Assert.That(R1RunController.Instance.Progress.State,Is.EqualTo(R1RunState.Ready));
        Assert.That(UnityEngine.Object.FindObjectsByType<CheckpointRaceChallenge>(FindObjectsSortMode.None),Is.Empty);
    }
    [UnityTest] public IEnumerator ApiControlledRaceCompletionActuallySettlesOnce()
    {
        Assert.That(Choose("ChooseCheckpointRace"),Is.True);yield return Ready();
        var race=(CheckpointRaceChallenge)Get("CheckpointRace");race.enabled=false;
        // API-controlled outcome fixture ONLY, not physical driving evidence.
        var state=(CheckpointRaceState)typeof(CheckpointRaceChallenge).GetField("state",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(race);
        state.Reset();Assert.That(state.Begin(Vector3.zero,new[]{Vector3.forward*5},1,180,250),Is.True);
        state.Advance(Vector3.forward*5,.1f);Assert.That(race.Won,Is.True);
        yield return null;yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));Assert.That(run.Progress.CompletedEncounters,Is.EqualTo(1));
        Assert.That(Get("CheckpointRace"),Is.Null);Assert.That(run.GeneratedWorld,Is.Null);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
