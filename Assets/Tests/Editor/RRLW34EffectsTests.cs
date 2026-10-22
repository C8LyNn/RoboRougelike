using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.MOVSeries;
using RoboRougelike.Mechanics.Booster;
using RoboRougelike.Mechanics.MOV005OmniDrive;
using RogueMachine.P12PipelineSafe;
using Object=UnityEngine.Object;

public class RRLW34EffectsTests
{
    GameObject fixtures;R1RunController run;R3VisualPalette palette;
    readonly List<R1UpgradeDefinition> definitions=new List<R1UpgradeDefinition>();
    static IEnumerator Until(Func<bool> predicate,string message)
    {
        float end=Time.realtimeSinceStartup+12;
        while(!predicate()&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(predicate(),Is.True,message);
    }
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return Until(()=>R1RunController.Instance?.Progress!=null,"Bootstrap");
        run=R1RunController.Instance;palette=Object.FindFirstObjectByType<R3VisualPalette>();
        fixtures=new GameObject("W34 mounted effect fixtures");Time.timeScale=1;
    }
    R1UpgradeDefinition Upgrade(R1UpgradeEffect effect,float amount=.25f)
    {
        var d=ScriptableObject.CreateInstance<R1UpgradeDefinition>();d.Id=Guid.NewGuid().ToString("N");
        d.Title=effect.ToString();d.Effect=effect;d.Amount=amount;definitions.Add(d);return d;
    }
    static Vector3Int Slot(int i)=>i<6?new Vector3Int(i<3?1:-2,2,(i%3)*2):new Vector3Int(1,3,(i-6)*2);
    R3VehicleModel Model(params R1UpgradeDefinition[] bonuses)
    {
        var m=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        Assert.That(m.ReceiveBonuses(bonuses),Is.True);
        var ids=m.Inventory.Select(p=>p.Id).ToArray();
        for(int i=0;i<ids.Length;i++)Assert.That(m.Mount(ids[i],Slot(i)),Is.True,"mount "+i);
        return m;
    }
    R1VehicleState Build(R3VehicleModel model,R1RunStats baseline=null,float x=0)
    {
        var v=R3VehicleAssembly.Build(model,baseline??new R1RunStats(),palette,new Vector3(x,20,1000),Quaternion.identity);
        v.transform.SetParent(fixtures.transform,true);v.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;
        return v;
    }
    static R3MountedBonusEffects Effects(R1VehicleState v)=>v.GetComponent<R3MountedBonusEffects>();
    static R3MountedPart Module(R1VehicleState v,R1UpgradeEffect effect)=>
        v.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeEffect==effect);
    [UnityTest] public IEnumerator InventoryIsInactiveAndAllMountedEffectsReachLiveWeaponsHpAndOmni()
    {
        var m=Model(Upgrade(R1UpgradeEffect.Damage,.25f),Upgrade(R1UpgradeEffect.Damage,.2f),
            Upgrade(R1UpgradeEffect.Hull,.4f),Upgrade(R1UpgradeEffect.Core,.5f),
            Upgrade(R1UpgradeEffect.Reload,.2f),Upgrade(R1UpgradeEffect.Mobility,.3f));
        Assert.That(m.ReceiveBonuses(new[]{Upgrade(R1UpgradeEffect.Damage,1)}),Is.True);
        m.Parts.Add(new R3PartState(new Vector3Int(0,2,1),R3PartKind.OmniDrive));m.SelectedDrive=R3DriveKind.Omni;
        var unboosted=m.DeepCopy();unboosted.Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeEffect==R1UpgradeEffect.Mobility).UpgradeAmount=0;
        var reference=Build(unboosted,x:-40);float unboostedSpeed=reference.Controller.EffectiveForwardSpeed;reference.gameObject.SetActive(false);
        var v=Build(m);var tank=v.Controller;var stats=Effects(v).EffectiveStats;
        Assert.That(stats.Damage,Is.EqualTo(1.5f).Within(.001));Assert.That(stats.Mobility,Is.EqualTo(1.3f).Within(.001));
        Assert.That(tank.EffectiveDamage,Is.EqualTo(60).Within(.001));
        Assert.That(tank.WeaponInterval(R4WeaponKind.Basic),Is.EqualTo(R4WeaponDefinition.Get(R4WeaponKind.Basic).Interval*.8f).Within(.001));
        Assert.That(v.Core.MaxHp,Is.EqualTo(210).Within(.001));
        var gun=v.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Gun);
        Assert.That(gun.Damageable.MaxHp,Is.EqualTo(R4WeaponDefinition.Get(gun.Weapon).Hp*1.4f).Within(.001));
        v.Controller.SetGameplayInputEnabled(true);v.GetComponent<MOVSeriesVehicleAdapter>().TryDrive(1,0,tank.EffectiveForwardSpeed,tank.EffectiveReverseSpeed,tank.EffectiveTurnSpeed);
        Assert.That(v.GetComponentInChildren<OmniDriveModule>().MaximumSpeed,Is.EqualTo(tank.EffectiveForwardSpeed).Within(.001));
        Assert.That(tank.EffectiveForwardSpeed,Is.EqualTo(unboostedSpeed*1.3f).Within(.001));
        var barrel=new GameObject("Actual bonus weapon muzzle").transform;barrel.SetParent(fixtures.transform);
        barrel.position=new Vector3(40,20,1000);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.SetParent(fixtures.transform);
        target.transform.position=barrel.position+Vector3.forward*4;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        tank.FireFrom(barrel,R4WeaponKind.Basic,"w34-live-bonus");
        yield return Until(()=>hp.CurrentHp<1000,"Physical bonus shot reaches target");
        Assert.That(hp.CurrentHp,Is.EqualTo(1000-R4WeaponDefinition.Get(R4WeaponKind.Basic).Damage*1.5f).Within(.01));
        Assert.That(run.Progress.Stats.Damage,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator DestructionAndBridgeDetachmentRemoveEffectsWithoutHealing()
    {
        var m=Model(Upgrade(R1UpgradeEffect.Hull,.4f),Upgrade(R1UpgradeEffect.Damage,.25f));
        m.Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeEffect==R1UpgradeEffect.Hull).Cell=new Vector3Int(1,2,2);
        m.Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeEffect==R1UpgradeEffect.Damage).Cell=new Vector3Int(1,2,4);
        var v=Build(m);v.Core.RestoreHealthRatio(.4f);var hull=Module(v,R1UpgradeEffect.Hull);
        hull.Damageable.ApplyDamage(10000,hull.transform.position,Vector3.right);
        v.Structure.EvaluateStructure();yield return null;yield return null;
        // Second module was supported exclusively through the first footprint.
        Assert.That(Module(v,R1UpgradeEffect.Damage).Operational,Is.False);
        Assert.That(Effects(v).EffectiveStats.Damage,Is.EqualTo(1));Assert.That(Effects(v).EffectiveStats.Hull,Is.EqualTo(1));
        Assert.That(v.Core.MaxHp,Is.EqualTo(100).Within(.001));Assert.That(v.Core.CurrentHp,Is.EqualTo(40).Within(.001));
        Assert.That(v.Controller.EffectiveDamage,Is.EqualTo(40).Within(.001));
    }
    [UnityTest] public IEnumerator RepeatedCaptureAndRebuildUseOriginalBaselineAndPreserveCondition()
    {
        var baseline=new R1RunStats();baseline.Apply(R1UpgradeEffect.Damage,.1f);
        var m=Model(Upgrade(R1UpgradeEffect.Damage,.25f),Upgrade(R1UpgradeEffect.Hull,.4f));
        var v=Build(m,baseline);v.Core.RestoreHealthRatio(.37f);
        for(int i=0;i<3;i++)
        {
            v.GetComponent<R3VehicleAssembly>().Capture();v.gameObject.SetActive(false);Object.Destroy(v.gameObject);
            v=Build(m.DeepCopy(),baseline);
            Assert.That(v.Controller.EffectiveDamage,Is.EqualTo(40*1.1f*1.25f).Within(.001));
            Assert.That(v.Core.CurrentHp/v.Core.MaxHp,Is.EqualTo(.37f).Within(.001));
            Assert.That(baseline.Damage,Is.EqualTo(1.1f).Within(.001));yield return null;
        }
    }
    [UnityTest] public IEnumerator FieldRepairOnlyHealsLivingConnectedPartsDuringTestAndStopsOnLoss()
    {
        Assert.That(run.OpenSandboxGarage(),Is.True);Time.timeScale=1;
        var model=Model(Upgrade(R1UpgradeEffect.Repair));var v=Build(model);v.Core.RestoreHealthRatio(.5f);
        float end=Time.time+.4f;while(Time.time<end)yield return null;
        Assert.That(v.Core.CurrentHp/v.Core.MaxHp,Is.EqualTo(.5f).Within(.001),"Garage never grants passive repair.");
        Assert.That(run.Progress.BeginTest(),Is.True);end=Time.time+.6f;while(Time.time<end)yield return null;
        Assert.That(v.Core.CurrentHp/v.Core.MaxHp,Is.GreaterThan(.505f));
        var module=Module(v,R1UpgradeEffect.Repair);module.Damageable.ApplyDamage(10000,module.transform.position,Vector3.right);
        yield return null;yield return null;float ratio=v.Core.CurrentHp/v.Core.MaxHp;
        end=Time.time+.6f;while(Time.time<end)yield return null;
        Assert.That(v.Core.CurrentHp/v.Core.MaxHp,Is.EqualTo(ratio).Within(.001));
    }
    [UnityTest] public IEnumerator RangeResetAndReturnPreserveWorkshopModuleIdentityWithoutStacking()
    {
        Assert.That(run.OpenSandboxGarage(),Is.True);
        Assert.That(run.VehicleModel.ReceiveBonuses(new[]{Upgrade(R1UpgradeEffect.Damage)}),Is.True);
        string id=run.VehicleModel.Inventory.Single().Id;
        Assert.That(run.GarageMount(id,new Vector3Int(1,2,0)),Is.True);
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(1.25f).Within(.001));
        Assert.That(run.EnterTestRange(),Is.True);yield return Until(()=>!run.Transitioning,"Range entry");
        var module=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(1.25f).Within(.001));
        module.Damageable.ApplyDamage(10000,module.transform.position,Vector3.right);yield return null;yield return null;
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(1));
        Assert.That(run.ResetTestRange(),Is.True);yield return Until(()=>!run.Transitioning,"Range reset");
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(1.25f).Within(.001));
        Assert.That(run.ReturnFromTestRange(),Is.True);yield return Until(()=>!run.Transitioning,"Workshop restored");
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Kind==R3PartKind.BonusModule).Id,Is.EqualTo(id));
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(1.25f).Within(.001));Assert.That(run.Progress.Stats.Damage,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator MountedBonusSurvivesNextFightAndMapThenCanBePaidRepairedAndRemounted()
    {
        Assert.That(run.StartRun(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        yield return Until(()=>!run.Transitioning,"First fight");run.Encounter.SetCombatActive(false);
        RRLW2TestFlow.DefeatAll(run);yield return Until(()=>run.Progress.State==R1RunState.Salvage,"Victory");
        var ids=run.Salvage.Items.Where(p=>p.Selectable).Take(3).Select(p=>p.Id).ToArray();
        Assert.That(ids.Length,Is.EqualTo(3));Assert.That(run.ConfirmRewards(ids,new[]{0}),Is.True);
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(4));
        var bonus=run.VehicleModel.Inventory.Single(p=>p.Kind==R3PartKind.BonusModule);string id=bonus.Id;
        Assert.That(run.GarageMount(id,new Vector3Int(1,2,0)),Is.True);
        var expected=run.ActiveStats.Copy();Assert.That(run.StartNextEncounter(),Is.True);
        Assert.That(run.ChooseRoute(0),Is.True);yield return Until(()=>!run.Transitioning,"Next map");run.Encounter.SetCombatActive(false);
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(expected.Damage));Assert.That(run.ActiveStats.Hull,Is.EqualTo(expected.Hull));
        var module=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        module.Damageable.ApplyDamage(10000,module.transform.position,Vector3.right);yield return null;yield return null;
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(1));Assert.That(run.ActiveStats.Hull,Is.EqualTo(1));
        Assert.That(run.ActiveStats.Mobility,Is.EqualTo(1));Assert.That(run.ActiveStats.Core,Is.EqualTo(1));
        RRLW2TestFlow.DefeatAll(run);yield return Until(()=>run.Progress.State==R1RunState.Salvage,"Second victory");
        Assert.That(run.ConfirmRewards(Array.Empty<string>(),new[]{0}),Is.True);
        var state=run.VehicleModel.Parts.Single(p=>p.Id==id);Assert.That(state.Health,Is.Zero);
        run.VehicleModel.Award(100);int before=run.VehicleModel.Scrap;
        Assert.That(run.GarageRepair(state.Cell),Is.True);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-60));
        Assert.That(run.GarageRemove(state.Cell),Is.True);Assert.That(run.GarageMount(id,new Vector3Int(1,2,0)),Is.True);
        Assert.That(run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id).Operational,Is.True);
        Assert.That(run.ActiveStats.Damage,Is.EqualTo(expected.Damage));Assert.That(run.ActiveStats.Hull,Is.EqualTo(expected.Hull));
    }
    [UnityTest] public IEnumerator BoosterForceFollowsRotatedPhysicalMountAroundAllAxes()
    {
        foreach(var turns in new[]{new Vector3Int(0,1,0),new Vector3Int(1,0,0),new Vector3Int(0,0,1),new Vector3Int(-1,2,1)})
        {
            var m=Model();m.Parts.Add(new R3PartState(new Vector3Int(1,2,0),R3PartKind.Booster){RotationQuarterTurns=turns});
            var v=Build(m);var body=v.GetComponent<Rigidbody>();body.constraints=RigidbodyConstraints.FreezeRotation;body.useGravity=false;
            v.Controller.SetGameplayInputEnabled(true);
            var part=v.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.Booster);
            var direction=part.transform.forward;
            Assert.That(v.GetComponent<MOVSeriesVehicleAdapter>().TryBoost(),Is.True);
            float end=Time.fixedTime+.12f;while(Time.fixedTime<end)yield return null;
            Assert.That(Vector3.Dot(body.linearVelocity.normalized,direction),Is.GreaterThan(.95f),"Actual thrust direction "+turns);
            v.gameObject.SetActive(false);Object.Destroy(v.gameObject);yield return null;
        }
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Time.timeScale=1;if(fixtures!=null)Object.Destroy(fixtures);
        foreach(var d in definitions)if(d!=null)Object.Destroy(d);definitions.Clear();
        if(Application.isPlaying)yield return new ExitPlayMode();
    }
}
