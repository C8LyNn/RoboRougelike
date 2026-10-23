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
using RoboRougelike.R5;
using RogueMachine.P12PipelineSafe;

public class RRLW2GameplayTests
{
    R1RunController run;
    static IEnumerator Until(Func<bool> condition,string message,float timeout=12)
    {float end=Time.realtimeSinceStartup+timeout;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Assert.That(condition(),Is.True,message);}
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return Until(()=>R1RunController.Instance?.Progress!=null,"Bootstrap");run=R1RunController.Instance;
        Assert.That(run.Error,Is.Null);Assert.That(run.StartRun(),Is.True);
    }
    IEnumerator Combat()
    {Assert.That(run.ChooseRoute(0),Is.True);yield return Until(()=>!run.Transitioning,"Spawn");run.Encounter.SetCombatActive(false);}
    [UnityTest] public IEnumerator OnePhysicalBonusChangesLiveStatsOnlyAfterMountAndOpensGarageDirectly()
    {
        run.Definition.Upgrades=new[]{run.Definition.Upgrades.First(u=>u.Effect==R1UpgradeEffect.Damage),run.Definition.Upgrades.First(u=>u.Effect==R1UpgradeEffect.Hull),run.Definition.Upgrades.First(u=>u.Effect==R1UpgradeEffect.Core)};
        yield return Combat();float damage=run.Player.Controller.EffectiveDamage;
        float bonus=run.Definition.Upgrades.First(u=>u.Effect==R1UpgradeEffect.Damage).Amount;
        RRLW2TestFlow.DefeatAll(run);yield return Until(()=>run.Progress.State==R1RunState.Salvage,"One reward screen");
        Assert.That(run.ConfirmRewards(Array.Empty<string>(),new[]{0,1,2}),Is.False);
        int index=run.Progress.Offers.ToList().FindIndex(u=>u.Effect==R1UpgradeEffect.Damage);
        Assert.That(run.ConfirmRewards(Array.Empty<string>(),new[]{index}),Is.True);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(run.Progress.Acquired.Count,Is.EqualTo(1));
        Assert.That(run.Player.Controller.EffectiveDamage,Is.EqualTo(damage));
        Assert.That(run.GarageMount(run.VehicleModel.Inventory.Single().Id,new Vector3Int(1,2,0)),Is.True);
        Assert.That(run.Player.Controller.EffectiveDamage,Is.EqualTo(damage*(1+bonus)).Within(.001));
        int scrap=run.VehicleModel.Scrap;
        Assert.That(run.ConfirmRewards(Array.Empty<string>(),new[]{0,1,2}),Is.False);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(run.StartNextEncounter(),Is.True);yield return Until(()=>run.Progress.State==R1RunState.Route,"Next route");
    }
    [UnityTest] public IEnumerator RealDestroyedEnemyGunIsStoredRebuiltForFullPriceAndMounted()
    {
        yield return Combat();var enemy=run.Encounter.Enemies[0];
        enemy.WeaponBlock.ApplyDamage(100000,enemy.WeaponBlock.transform.position,Vector3.up);
        RRLW2TestFlow.DefeatAll(run);yield return Until(()=>run.Progress.State==R1RunState.Salvage,"Actual loot");
        var wreck=run.Salvage.Items.Single(p=>p.Kind==R3PartKind.Gun&&p.Health==0);
        var wheel=run.Salvage.Items.First(p=>p.Kind==R3PartKind.Wheel&&p.Health>0);
        Assert.That(run.ConfirmRewards(new[]{wreck.Id,wheel.Id},new[]{0}),Is.True);
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(3));Assert.That(run.VehicleModel.Inventory.Single(p=>p.Id==wreck.Id).Health,Is.Zero);
        var cell=new Vector3Int(1,2,0);int before=run.VehicleModel.Scrap;
        Assert.That(run.GarageMount(wreck.Id,cell),Is.False);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before));
        Assert.That(run.GarageRepairInventory(wreck.Id),Is.True);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-36));
        Assert.That(run.GarageRepairInventory(wreck.Id),Is.False);Assert.That(run.GarageMount(wreck.Id,cell),Is.True);
        var mounted=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==wreck.Id);
        Assert.That(mounted.Damageable.CurrentHp,Is.EqualTo(mounted.Damageable.MaxHp));
    }
    [UnityTest] public IEnumerator SixStagesUseActualDenseRosterSafeSpawnsNormalEscortAndCompleteLoot()
    {
        int[] counts={2,3,4,2,3,1};
        for(int stage=0;stage<6;stage++)
        {
            yield return Combat();var foes=run.Encounter.Enemies.ToArray();
            Assert.That(foes.Length,Is.EqualTo(counts[stage]));Assert.That(run.CurrentEncounter.SpawnCount,Is.EqualTo(counts[stage]));
            Assert.That(R5RunPlan.Threat(run.CurrentEncounter),Is.LessThanOrEqualTo(run.CurrentEncounter.ThreatBudget));
            if(stage<5){Assert.That(foes.Last().Archetype,Is.EqualTo(R3EnemyArchetype.Scout));Assert.That(foes.Last().Variant,Is.EqualTo(R5EnemyVariant.Normal));}
            else Assert.That(foes.Single().Variant,Is.EqualTo(R5EnemyVariant.SiegeBoss));
            Physics.SyncTransforms();
            foreach(var foe in foes)
                foreach(var part in foe.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))
                    foreach(var wall in run.Arena.Layout.GetComponentsInChildren<Collider>())
                        if(part.bounds.Intersects(wall.bounds))Assert.That(Physics.ComputePenetration(part,part.transform.position,part.transform.rotation,wall,wall.transform.position,wall.transform.rotation,out _,out float distance)&&distance>.02f,Is.False,"Spawn inside wall: "+foe.name+" / "+wall.name);
            for(int i=0;i<foes.Length;i++)for(int j=i+1;j<foes.Length;j++)
                Assert.That(Vector3.Distance(foes[i].transform.position,foes[j].transform.position),Is.GreaterThan(6));
            // Every physically mounted defense is loot in addition to legacy guns/wheels.
            int defenses=foes.Sum(e=>e.GetComponentsInChildren<RoboRougelike.WeaponIntegration.RRLShieldBinding>().Length+
                e.GetComponentsInChildren<RoboRougelike.WeaponIntegration.RRLPointDefenseBinding>().Length);
            int expected=foes.Sum(e=>e.InitialWheelCount+Math.Max(1,e.VariantWeapons.Count))+defenses;
            RRLW2TestFlow.DefeatAll(run);yield return Until(()=>run.Progress.State==R1RunState.Salvage,"Full encounter victory");
            Assert.That(run.Salvage.Items.Count,Is.EqualTo(expected));Assert.That(run.Salvage.Items.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(expected));
            Assert.That(run.ConfirmRewards(Array.Empty<string>(),new[]{0}),Is.True);
            if(stage<5){Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(run.StartNextEncounter(),Is.True);}
            else Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Won));
        }
    }
    [UnityTest] public IEnumerator ConnectedIslandDetachesAsOneMovingBodyWithoutBreakingCoreGraph()
    {
        yield return Combat();
        var d=new R3VehicleDesign{Name="Chunk fixture",Speed=7,TurnSpeed=60};
        foreach(var p in new[]{new R3PartState(Vector3Int.zero,R3PartKind.Core),new R3PartState(Vector3Int.up,R3PartKind.Gun),
            new R3PartState(Vector3Int.left,R3PartKind.Wheel),new R3PartState(Vector3Int.right,R3PartKind.Wheel),
            new R3PartState(Vector3Int.forward,R3PartKind.Armor),new R3PartState(Vector3Int.forward*2,R3PartKind.Armor),new R3PartState(Vector3Int.forward*2+Vector3Int.right,R3PartKind.Armor)})d.Parts.Add(p);
        var model=new R3VehicleModel(d);var rig=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(0,8,-8),Quaternion.identity);
        yield return null;yield return null;var a=rig.GetComponent<R3VehicleAssembly>();
        var first=a.Parts.Single(p=>p.Cell==Vector3Int.forward*2);var second=a.Parts.Single(p=>p.Cell==Vector3Int.forward*2+Vector3Int.right);
        var bridge=a.Parts.Single(p=>p.Cell==Vector3Int.forward);bridge.Damageable.ApplyDamage(10000,bridge.transform.position,Vector3.forward);rig.Structure.EvaluateStructure();
        var chunk=first.GetComponentInParent<Rigidbody>();Assert.That(chunk,Is.Not.SameAs(rig.GetComponent<Rigidbody>()));
        Assert.That(second.GetComponentInParent<Rigidbody>(),Is.SameAs(chunk));Assert.That(chunk.GetComponent<R3Debris>(),Is.Not.Null);
        Assert.That(chunk.GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True);
        var position=chunk.position;float end=Time.fixedTime+.25f;while(Time.fixedTime<end)yield return null;
        Assert.That(chunk!=null,Is.True,"Chunk must survive its initial separation.");
        Assert.That(rig!=null,Is.True,"Owner must survive bridge loss.");
        Assert.That(rig.Structure!=null,Is.True,"Owner structure binding must remain valid.");
        Assert.That(Vector3.Distance(position,chunk.position),Is.GreaterThan(.7f));
        Assert.That(rig.Structure.CoreAlive,Is.True);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
