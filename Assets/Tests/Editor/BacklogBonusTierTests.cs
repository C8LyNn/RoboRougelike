using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;

public sealed class BacklogBonusTierTests
{
    internal static R1UpgradeDefinition OperationBonus(string id,float amount,string operation,R1UpgradeEffect effect=R1UpgradeEffect.Damage)
    {
        var field=typeof(R1UpgradeDefinition).GetField("Operation");Assert.That(field,Is.Not.Null,"Reward definitions need an explicit modifier operation.");
        var reward=ScriptableObject.CreateInstance<R1UpgradeDefinition>();reward.Id=id;reward.Effect=effect;reward.Amount=amount;
        field.SetValue(reward,System.Enum.Parse(field.FieldType,operation));return reward;
    }
    [Test] public void BonusOperationSurvivesFusionCheckpointAndBlueprintIdentity()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var more=OperationBonus("damage",.2f,"More");var increased=OperationBonus("damage",.2f,"Increased");
        Assert.That(model.ReceiveBonuses(new[]{more,more,more,increased}),Is.True);
        var mixed=new[]{model.Inventory[0].Id,model.Inventory[1].Id,model.Inventory[3].Id};
        Assert.That(model.FuseInventory(mixed),Is.False,"Different operations must never fuse even with the same reward ID.");
        Assert.That(model.FuseInventory(model.Inventory.Take(3).Select(p=>p.Id)),Is.True);
        var fused=model.Inventory.Single(p=>p.Tier==2);
        var op=typeof(R3PartState).GetField("UpgradeOperation");Assert.That(op,Is.Not.Null);Assert.That(op.GetValue(fused).ToString(),Is.EqualTo("More"));
        var blueprint=R3BlueprintPart.FromPart(fused,Vector3Int.zero);
        Assert.That(blueprint.Matches(fused),Is.True);var altered=fused.Copy();op.SetValue(altered,System.Enum.Parse(op.FieldType,"Increased"));
        Assert.That(blueprint.Matches(altered),Is.False,"Blueprint matching must preserve the operation.");
        var saved=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(op.GetValue(restored.Inventory.Single(p=>p.Id==fused.Id)).ToString(),Is.EqualTo("More"));
        string json=JsonUtility.ToJson(model.MakeCheckpoint(0));
        string legacy=System.Text.RegularExpressions.Regex.Replace(json,",\\\"UpgradeOperation\\\":[0-9]+","");
        Assert.That(legacy,Does.Not.Contain("\"UpgradeOperation\""));
        var old=JsonUtility.FromJson<R3ModelCheckpoint>(legacy);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(old,R3VehicleKind.Light,model.Economy,0,out var oldModel,out error),Is.True,error);
        Assert.That(oldModel.Inventory.All(p=>op.GetValue(p).ToString()=="Increased"),Is.True,"Old saves default to the original additive behavior.");
        var textMethod=typeof(R1RunHUD).GetMethod("BonusInstanceText",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        Assert.That(((string)textMethod.Invoke(null,new object[]{fused})),Does.Contain("osobny mnożnik"));
        Object.DestroyImmediate(more);Object.DestroyImmediate(increased);
    }
    [Test] public void RewardCatalogContainsAVisibleMultiplicativeDamageModule()
    {
        var run=Resources.Load<R1RunDefinition>("R1/Run");Assert.That(run,Is.Not.Null);
        var reward=run.Upgrades.SingleOrDefault(p=>p!=null&&p.Id=="damage_more");Assert.That(reward,Is.Not.Null);
        var operation=typeof(R1UpgradeDefinition).GetField("Operation");Assert.That(operation,Is.Not.Null);
        Assert.That(operation.GetValue(reward).ToString(),Is.EqualTo("More"));Assert.That(reward.Effect,Is.EqualTo(R1UpgradeEffect.Damage));
    }
    [Test] public void ScopedBonusKeepsItsIdentityInSavesAndBlueprints()
    {
        var scope=typeof(R1UpgradeDefinition).GetField("Scope");Assert.That(scope,Is.Not.Null);
        var reward=OperationBonus("same-id",.3f,"Increased");scope.SetValue(reward,System.Enum.Parse(scope.FieldType,"BasicCannon"));
        var global=OperationBonus("same-id",.3f,"Increased");
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        Assert.That(model.ReceiveBonuses(new[]{reward,reward,global}),Is.True);
        Assert.That(model.FuseInventory(model.Inventory.Select(p=>p.Id)),Is.False,"Different scopes are different items.");
        var item=model.Inventory[0];var blueprint=R3BlueprintPart.FromPart(item,Vector3Int.zero);
        var roundTrip=JsonUtility.FromJson<R3BlueprintPart>(JsonUtility.ToJson(blueprint));
        Assert.That(roundTrip.Matches(item),Is.True);Assert.That(roundTrip.Matches(model.Inventory[2]),Is.False);
        var save=JsonUtility.ToJson(model.MakeCheckpoint(0));var data=JsonUtility.FromJson<R3ModelCheckpoint>(save);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(roundTrip.Matches(restored.Inventory.Single(p=>p.Id==item.Id)),Is.True);
        var legacy=System.Text.RegularExpressions.Regex.Replace(save,",\\\"UpgradeScope\\\":[0-9]+","");
        Assert.That(legacy,Does.Not.Contain("\"UpgradeScope\""));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(JsonUtility.FromJson<R3ModelCheckpoint>(legacy),R3VehicleKind.Light,model.Economy,0,out var old,out error),Is.True,error);
        Assert.That(old.Inventory.All(p=>roundTrip.Matches(p)==false),Is.True,"Old saves default to global scope.");
        var textMethod=typeof(R1RunHUD).GetMethod("BonusInstanceText",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        Assert.That((string)textMethod.Invoke(null,new object[]{item}),Does.Contain("działa podstawowego"));
        Object.DestroyImmediate(reward);Object.DestroyImmediate(global);
    }
    [Test] public void RewardCatalogContainsScopedBasicCannonBonus()
    {
        var run=Resources.Load<R1RunDefinition>("R1/Run");
        var reward=run.Upgrades.SingleOrDefault(p=>p!=null&&p.Id=="damage_basic_cannon");
        Assert.That(reward,Is.Not.Null);Assert.That(reward.Effect,Is.EqualTo(R1UpgradeEffect.Damage));
        Assert.That(typeof(R1UpgradeDefinition).GetField("Scope").GetValue(reward).ToString(),Is.EqualTo("BasicCannon"));
    }
    [Test] public void ModifierArithmeticIsOrderedAndStaysFiniteAtExtremeValues()
    {
        var type=typeof(R1RunStats).Assembly.GetType("RoboRougelike.R1.R1ModifierGroup");Assert.That(type,Is.Not.Null);
        object Group()=>System.Activator.CreateInstance(type,true);
        void Add(object group,string operation,float amount,int copies,bool inverse=false)
        {
            var method=type.GetMethod("Add");var mode=System.Enum.Parse(method.GetParameters()[0].ParameterType,operation);
            method.Invoke(group,new object[]{mode,amount,copies,inverse});
        }
        float Value(object group)=>(float)type.GetMethod("Apply").Invoke(group,new object[]{1f});
        var ordinary=Group();Add(ordinary,"More",.5f,2);Add(ordinary,"Increased",.2f,1);Add(ordinary,"Less",.2f,1);Add(ordinary,"Reduced",.1f,1);
        Assert.That(Value(ordinary),Is.EqualTo(1.98f).Within(.001));
        var reload=Group();Add(reload,"Increased",.2f,1,true);Add(reload,"More",.25f,1,true);
        Assert.That(Value(reload),Is.EqualTo(.6f).Within(.001),"Faster reload must reduce the interval.");
        var huge=Group();Add(huge,"More",1,1000000);Assert.That(Value(huge),Is.EqualTo(float.MaxValue));
        Add(huge,"Less",1,1);Assert.That(Value(huge),Is.Zero,"Zero times a huge multiplier must stay finite.");
        var invalid=Group();Add(invalid,"More",float.NaN,1);Assert.That(Value(invalid),Is.EqualTo(1));
    }
    [Test] public void IdenticalRewardModulesFuseAndPreserveTheirEffect()
    {
        var model = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var reward = ScriptableObject.CreateInstance<R1UpgradeDefinition>();
        reward.Id = "damage"; reward.Effect = R1UpgradeEffect.Damage; reward.Amount = .2f;
        Assert.That(model.ReceiveBonuses(new[] {reward, reward, reward}), Is.True);
        var ids = model.Inventory.Select(p => p.Id).ToArray();
        Assert.That(model.FuseInventory(ids), Is.True);
        var result = model.Inventory.Single();
        Assert.That(result.Tier, Is.EqualTo(2));
        Assert.That(result.UpgradeId, Is.EqualTo("damage"));
        Assert.That(result.UpgradeEffect, Is.EqualTo(R1UpgradeEffect.Damage));
        Assert.That(result.UpgradeAmount, Is.EqualTo(.2f));
        var checkpoint = model.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint, R3VehicleKind.Light, model.Economy, 0, out var restored, out var error), Is.True, error);
        Assert.That(restored.Inventory.Single().Tier, Is.EqualTo(2));
        Assert.That(restored.Inventory.Single().UpgradeId, Is.EqualTo("damage"));
        Object.DestroyImmediate(reward);
    }

    [Test] public void DifferentRewardEffectsCannotBeCombined()
    {
        var model = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var damage = ScriptableObject.CreateInstance<R1UpgradeDefinition>();
        var mobility = ScriptableObject.CreateInstance<R1UpgradeDefinition>();
        damage.Id = "damage"; damage.Effect = R1UpgradeEffect.Damage; damage.Amount = .2f;
        mobility.Id = "mobility"; mobility.Effect = R1UpgradeEffect.Mobility; mobility.Amount = .2f;
        Assert.That(model.ReceiveBonuses(new[] {damage, damage, mobility}), Is.True);
        var ids = model.Inventory.Select(p => p.Id).ToArray();
        Assert.That(model.FuseInventory(ids), Is.False);
        Assert.That(model.Inventory.Select(p => p.Id), Is.EqualTo(ids));
        model.Inventory[2].UpgradeId = "damage";
        model.Inventory[2].UpgradeEffect = R1UpgradeEffect.Damage;
        model.Inventory[2].UpgradeAmount = .4f;
        Assert.That(model.FuseInventory(ids), Is.False, "Same ID with different strength must not be combined.");
        Object.DestroyImmediate(damage); Object.DestroyImmediate(mobility);
    }
}

public sealed class BacklogBonusTierGameplayTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        for (int i = 0; i < 300 && R1RunController.Instance?.Progress == null; i++) yield return null;
    }
    [UnityTest] public IEnumerator MountedTierTwoDamageRewardWorksAndStopsWhenDestroyed()
    {
        var run = R1RunController.Instance;
        Assert.That(run.SelectVehicle(R3VehicleKind.Light) && run.OpenSandboxGarage(), Is.True);
        var reward = ScriptableObject.CreateInstance<R1UpgradeDefinition>();
        reward.Id = "damage"; reward.Effect = R1UpgradeEffect.Damage; reward.Amount = .2f;
        var model = run.VehicleModel;
        Assert.That(model.ReceiveBonuses(new[] {reward, reward, reward}), Is.True);
        Assert.That(run.GarageFuse(model.Inventory.Select(p => p.Id).ToArray()), Is.True);
        var state = model.Inventory.Single();
        var cells = model.Parts.SelectMany(p => R3VehicleModel.Neighbours.Select(d => p.Cell + d)).Distinct();
        var cell = cells.First(c => model.OwnedPlacementError(state.Id, c, default) == null);
        Assert.That(run.GarageMount(state.Id, cell), Is.True);
        yield return null;
        var effects = run.Player.GetComponent<R3MountedBonusEffects>();
        Assert.That(effects.EffectiveStats.Damage, Is.EqualTo(1.4f).Within(.001f));
        var part = run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p => p.Id == state.Id);
        part.Damageable.ApplyDamage(100000, part.transform.position, Vector3.up);
        yield return null;
        Assert.That(effects.EffectiveStats.Damage, Is.EqualTo(1f).Within(.001f));
        Object.Destroy(reward);
    }
    [UnityTest] public IEnumerator MountedDamageOperationsCombineAdditivelyAndMultiplicatively()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var inc=BacklogBonusTierTests.OperationBonus("inc",.2f,"Increased");
        var red=BacklogBonusTierTests.OperationBonus("red",.1f,"Reduced");
        var more=BacklogBonusTierTests.OperationBonus("more",.5f,"More");
        var less=BacklogBonusTierTests.OperationBonus("less",.2f,"Less");
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{inc,red,more,more,more,less}),Is.True);
        Assert.That(run.GarageFuse(model.Inventory.Where(p=>p.UpgradeId=="more").Select(p=>p.Id).ToArray()),Is.True);
        var itemIds=model.Inventory.Select(p=>p.Id).ToArray();
        foreach(var id in itemIds)
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>model.OwnedPlacementError(id,c,default)==null);Assert.That(run.GarageMount(id,cell),Is.True);
        }
        yield return null;var effects=run.Player.GetComponent<R3MountedBonusEffects>();
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.98f).Within(.001),"(1 + .2 - .1) × 1.5 × 1.5 × .8");
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        effects=run.Player.GetComponent<R3MountedBonusEffects>();
        var muzzle=new GameObject("Test mnożników pocisku").transform;muzzle.position=new Vector3(1000,1000,1000);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=muzzle.position+Vector3.forward*4;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);Physics.SyncTransforms();
        run.Player.Controller.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Basic,"modifier-test");
        float deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(1000-RoboRougelike.R4.R4WeaponDefinition.Get(RoboRougelike.R4.R4WeaponKind.Basic).Damage*1.98f).Within(.02),"The combined multiplier must reach a real physical shot.");
        var morePart=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.Tier==2);
        morePart.gameObject.SetActive(false);yield return null;
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(.88f).Within(.001),"Removing the multiplicative module must remove both tier contributions.");
        Object.Destroy(muzzle.gameObject);Object.Destroy(target);Object.Destroy(inc);Object.Destroy(red);Object.Destroy(more);Object.Destroy(less);
    }
    [UnityTest] public IEnumerator BasicCannonBonusAffectsOnlyBasicShotsAndCombinesWithGlobalBonuses()
    {
        var scope=typeof(R1UpgradeDefinition).GetField("Scope");
        Assert.That(scope,Is.Not.Null,"A reward needs an explicit scope before it can target one weapon family.");
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var global=BacklogBonusTierTests.OperationBonus("global",.2f,"Increased");
        var more=BacklogBonusTierTests.OperationBonus("global-more",.5f,"More");
        var basic=BacklogBonusTierTests.OperationBonus("basic-only",.3f,"Increased");
        scope.SetValue(basic,System.Enum.Parse(scope.FieldType,"BasicCannon"));
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{global,more,basic}),Is.True);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>model.OwnedPlacementError(id,c,default)==null);Assert.That(run.GarageMount(id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var muzzle=new GameObject("Scoped damage muzzle").transform;muzzle.position=new Vector3(1000,1000,1000);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=muzzle.position+Vector3.forward*4;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);Physics.SyncTransforms();
        run.Player.Controller.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Basic,"scope-basic");
        float deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(1000-40*2.25f).Within(.02),"(1 + global .2 + basic .3) × global more 1.5");
        hp.RestoreHealthRatio(1);run.Player.Controller.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Automatic,"scope-auto");
        deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(1000-12*1.8f).Within(.02),"The automatic weapon gets only the global bonuses.");
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeScope.ToString()=="BasicCannon");
        part.gameObject.SetActive(false);yield return null;
        hp.RestoreHealthRatio(1);run.Player.Controller.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Basic,"scope-removed");
        deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(1000-40*1.8f).Within(.02),"Disabling the scoped module removes only its contribution.");
        Object.Destroy(muzzle.gameObject);Object.Destroy(target);Object.Destroy(global);Object.Destroy(more);Object.Destroy(basic);
    }
    [UnityTest] public IEnumerator OperationsReachHullCoreMobilityAndReload()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var hull=BacklogBonusTierTests.OperationBonus("hull-more",.5f,"More",R1UpgradeEffect.Hull);
        var core=BacklogBonusTierTests.OperationBonus("core-reduced",.1f,"Reduced",R1UpgradeEffect.Core);
        var mobility=BacklogBonusTierTests.OperationBonus("mobility-less",.2f,"Less",R1UpgradeEffect.Mobility);
        var reload=BacklogBonusTierTests.OperationBonus("reload-more",.25f,"More",R1UpgradeEffect.Reload);
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{hull,core,mobility,reload}),Is.True);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>model.OwnedPlacementError(id,c,default)==null);Assert.That(run.GarageMount(id,cell),Is.True);
        }
        yield return null;var stats=run.Player.GetComponent<R3MountedBonusEffects>().EffectiveStats;
        Assert.That(stats.Hull,Is.EqualTo(1.5f).Within(.001));Assert.That(stats.Core,Is.EqualTo(.9f).Within(.001));
        Assert.That(stats.Mobility,Is.EqualTo(.8f).Within(.001));Assert.That(stats.FireInterval,Is.EqualTo(.75f).Within(.001));
        Object.Destroy(hull);Object.Destroy(core);Object.Destroy(mobility);Object.Destroy(reload);
    }
    [UnityTest] public IEnumerator LiveMountedBonusUpdatesWhenItsPropertiesChange()
    {
        var run=R1RunController.Instance;
        Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var reward=ScriptableObject.CreateInstance<R1UpgradeDefinition>();
        reward.Id="damage";reward.Effect=R1UpgradeEffect.Damage;reward.Amount=.2f;
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);
        var state=model.Inventory.Single();
        var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        var cell=cells.First(c=>model.OwnedPlacementError(state.Id,c,default)==null);
        Assert.That(run.GarageMount(state.Id,cell),Is.True);yield return null;
        var effects=run.Player.GetComponent<R3MountedBonusEffects>();
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.2f).Within(.001f));
        part.gameObject.SetActive(false);yield return null;
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1f).Within(.001f),"An inactive module cannot grant a bonus.");
        part.gameObject.SetActive(true);yield return null;
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.2f).Within(.001f));
        part.Tier=2;yield return null;
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.4f).Within(.001f),"The live tier is part of the effect, even if the part ID is unchanged.");
        part.UpgradeAmount=.3f;yield return null;
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.6f).Within(.001f));
        part.UpgradeEffect=R1UpgradeEffect.Mobility;yield return null;
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1f).Within(.001f));
        Assert.That(effects.EffectiveStats.Mobility,Is.EqualTo(1.6f).Within(.001f));
        Object.Destroy(reward);
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }
}
