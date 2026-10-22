using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R6;
using RoboRougelike.MOVSeries;
using RoboRougelike.Mechanics.MOV007;
using RoboRougelike.Mechanics.MOV008;
using RogueMachine.P12PipelineSafe;
using Object=UnityEngine.Object;

public class RRLW34ReviewRegressionTests
{
    R1RunController run;GameObject fixtures;R3VisualPalette palette;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;palette=Object.FindFirstObjectByType<R3VisualPalette>();
        fixtures=new GameObject("W34 final review physical fixtures");Time.timeScale=1;
    }
    static IEnumerator Until(Func<bool> condition,string message)
    {
        float end=Time.realtimeSinceStartup+12;
        while(!condition()&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(condition(),Is.True,message);
    }
    static IEnumerator Fixed(float seconds)
    {float end=Time.fixedTime+seconds;yield return Until(()=>Time.fixedTime>=end,"Physics progresses");}
    GameObject Surface(Vector3 center,Vector3 scale)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.transform.SetParent(fixtures.transform);
        g.transform.position=center;g.transform.localScale=scale;return g;
    }
    R1VehicleState Build(R3VehicleModel model)
    {
        var v=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,1000),Quaternion.identity);
        v.transform.SetParent(fixtures.transform,true);v.Controller.SetGameplayInputEnabled(true);
        v.Controller.enabled=false; // Test owns module commands; no keyboard/throttle overwrite.
        var body=v.GetComponent<Rigidbody>();body.useGravity=false;body.constraints=RigidbodyConstraints.FreezeRotation;
        return v;
    }
    [UnityTest] public IEnumerator CoreDeathCountsAndReleasesEachPhysicalOwnerOnlyOnce()
    {
        var d=new R3VehicleDesign{Name="Core failure with bonus",Speed=5,TurnSpeed=60};
        d.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        d.Parts.Add(new R3PartState(Vector3Int.up,R3PartKind.Gun));
        d.Parts.Add(new R3PartState(Vector3Int.right,R3PartKind.BonusModule){UpgradeId="core-test",UpgradeAmount=.2f});
        var v=Build(new R3VehicleModel(d));yield return null;
        var assembly=v.GetComponent<R3VehicleAssembly>();
        var core=v.Core;var bonus=assembly.Parts.Single(p=>p.Kind==R3PartKind.BonusModule);
        var gun=assembly.Parts.Single(p=>p.Kind==R3PartKind.Gun);
        float expectedMass=R6VehicleDrive.PartMass(bonus.Damageable)+R6VehicleDrive.PartMass(gun.Damageable);
        core.ApplyDamage(10000,core.transform.position,Vector3.right);v.Structure.EvaluateStructure();
        Assert.That(v.Structure.DetachedBlockCountTotal,Is.EqualTo(2),"Gun and one four-cell bonus are two physical owners.");
        var chunk=bonus.GetComponentInParent<Rigidbody>();
        Assert.That(gun.GetComponentInParent<Rigidbody>(),Is.SameAs(chunk));
        Assert.That(chunk.GetComponentsInChildren<P12_PipelineSafePlayerBlock>().Length,Is.EqualTo(2));
        Assert.That(chunk.mass,Is.EqualTo(expectedMass).Within(.001));
        Assert.That(v.Structure.ConnectedBlockCount,Is.Zero);Assert.That(v.Structure.CoreAlive,Is.False);
        Object.Destroy(chunk.gameObject);Object.Destroy(core.gameObject);yield return null;
    }
    [UnityTest] public IEnumerator FullInventoryHasExplicitRecoveryForMandatoryRewardsIncludingFinalVictory()
    {
        Assert.That(run.StartRun(),Is.True);
        var items=Enumerable.Range(0,200).Select(i=>new R3PartState(default,R3PartKind.Wheel){Id="full-inventory-"+i}).ToArray();
        Assert.That(run.VehicleModel.ReceiveSalvage(items),Is.True);
        for(int stage=0;stage<6;stage++)
        {
            Assert.That(run.ChooseRoute(0),Is.True);yield return Until(()=>!run.Transitioning,"Encounter");
            run.Encounter.SetCombatActive(false);RRLW2TestFlow.DefeatAll(run);
            yield return Until(()=>run.Progress.State==R1RunState.Salvage,"Mandatory rewards");
            var picked=stage==0||stage==5?run.Salvage.Items.Where(p=>p.Selectable).Take(3).Select(p=>p.Id).ToArray():Array.Empty<string>();
            int before=run.VehicleModel.Scrap;
            Assert.That(run.ConfirmRewards(picked,new[]{0}),Is.False);
            Assert.That(run.Salvage.Closed,Is.False);Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(200));
            Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before));Assert.That(run.Progress.Acquired.Count,Is.EqualTo(stage));
            var recover=typeof(R1RunController).GetMethod("DismantleRewardInventoryItem");
            Assert.That(recover,Is.Not.Null,"The reward UI needs an explicit controller operation to free owned inventory without leaving salvage.");
            for(int i=0;i<picked.Length+1;i++)
            {
                var old=run.VehicleModel.Inventory.First();int value=run.VehicleModel.DismantleValue(old),earned=run.EarnedScrap;
                Assert.That((bool)recover.Invoke(run,new object[]{old.Id}),Is.True);
                Assert.That(run.EarnedScrap,Is.EqualTo(earned+value));Assert.That(run.Salvage.Closed,Is.False);
                Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
            }
            Assert.That(run.ConfirmRewards(picked,new[]{0}),Is.True);
            Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(200));Assert.That(run.Progress.Acquired.Count,Is.EqualTo(stage+1));
            Assert.That(run.Progress.Stats.Damage,Is.EqualTo(1));
            Assert.That((bool)recover.Invoke(run,new object[]{run.VehicleModel.Inventory.First().Id}),Is.False,"This recovery action belongs only to the reward screen.");
            if(stage<5){Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(run.StartNextEncounter(),Is.True);}
            else Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Won));
        }
    }
    [UnityTest] public IEnumerator RotatedTrackTractionFollowsMountForwardAndInvertedTracksCannotProbeTheFloor()
    {
        Surface(new Vector3(0,19.1f,1000),new Vector3(50,.2f,50));
        foreach(var turns in new[]{new Vector3Int(0,1,0),new Vector3Int(0,2,0),new Vector3Int(0,3,0),new Vector3Int(2,0,0)})
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));model.SelectedDrive=R3DriveKind.Tracks;
            foreach(int side in new[]{-1,1})model.Parts.Add(new R3PartState(new Vector3Int(side*4,0,0),R3PartKind.CrawlerTrack){RotationQuarterTurns=turns});
            var v=Build(model);var body=v.GetComponent<Rigidbody>();body.constraints|=RigidbodyConstraints.FreezePositionY;
            var tracks=v.GetComponent<CrawlerTrackController>();tracks.SetInput(1,0);Physics.SyncTransforms();yield return Fixed(.15f);
            if(turns.x==2)
            {Assert.That(tracks.GroundedModuleCount,Is.Zero,"Inverted contact faces point away from the floor.");Assert.That(body.linearVelocity.magnitude,Is.LessThan(.01f));}
            else
            {
                var forward=v.GetComponentInChildren<CrawlerTrackModule>().transform.forward;
                Assert.That(tracks.GroundedModuleCount,Is.EqualTo(2));Assert.That(body.linearVelocity.magnitude,Is.GreaterThan(.05f));
                Assert.That(Vector3.Dot(body.linearVelocity.normalized,forward),Is.GreaterThan(.95f),"Actual traction axis "+turns);
            }
            v.gameObject.SetActive(false);Object.Destroy(v.gameObject);yield return null;
        }
    }
    [UnityTest] public IEnumerator SidewaysHoverProbesItsNozzleAxisAndAppliesActualLateralForce()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));model.SelectedDrive=R3DriveKind.Hover;
        model.Parts.Add(new R3PartState(new Vector3Int(1,2,0),R3PartKind.HoverEngine){RotationQuarterTurns=new Vector3Int(0,0,1)});
        var v=Build(model);var body=v.GetComponent<Rigidbody>();body.constraints|=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezePositionZ;
        Surface(new Vector3(1.45f,21.1f,1000),new Vector3(.2f,.2f,10));Physics.SyncTransforms();yield return Fixed(.15f);
        var pod=v.GetComponentInChildren<HoverLiftPod>();
        Assert.That(pod.HasGroundContact,Is.True,"Probe follows -mount.up to the wall, not world-down into empty air.");
        Assert.That(pod.LastLiftForceNewtons,Is.GreaterThan(0));Assert.That(body.linearVelocity.x,Is.LessThan(-.05f));
        Assert.That(Mathf.Abs(body.linearVelocity.z),Is.LessThan(.01f));
    }
    [UnityTest] public IEnumerator InvertedHoverDoesNotProduceUpwardLiftFromTheFloorBelow()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));model.SelectedDrive=R3DriveKind.Hover;model.HoverHeightMeters=2;
        model.Parts.Add(new R3PartState(new Vector3Int(1,2,0),R3PartKind.HoverEngine){RotationQuarterTurns=new Vector3Int(2,0,0)});
        var v=Build(model);Surface(new Vector3(0,19.1f,1000),new Vector3(50,.2f,50));Physics.SyncTransforms();yield return Fixed(.15f);
        var pod=v.GetComponentInChildren<HoverLiftPod>();
        Assert.That(pod.HasGroundContact,Is.False);Assert.That(pod.LastLiftForceNewtons,Is.Zero);
        Assert.That(v.GetComponent<Rigidbody>().linearVelocity.y,Is.EqualTo(0).Within(.01f));
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {Time.timeScale=1;if(fixtures!=null)Object.Destroy(fixtures);if(Application.isPlaying)yield return new ExitPlayMode();}
}
