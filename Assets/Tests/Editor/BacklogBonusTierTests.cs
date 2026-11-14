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
    [Test] public void RepairEfficiencyRewardIsAvailableAndDescribesItsEffect()
    {
        Assert.That(System.Enum.TryParse("RepairEfficiency",out R1UpgradeEffect effect),Is.True);
        var reward=Resources.Load<R1RunDefinition>("R1/Run").Upgrades.SingleOrDefault(u=>u!=null&&u.Id=="repair_efficiency");
        Assert.That(reward,Is.Not.Null);Assert.That(reward.Effect,Is.EqualTo(effect));
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);
        var part=model.Inventory.Single();var blueprint=R3BlueprintPart.FromPart(part,Vector3Int.zero);
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(blueprint.Matches(restored.Inventory.Single()),Is.True);
        var label=typeof(R1RunHUD).GetMethod("BonusInstanceText",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        Assert.That((string)label.Invoke(null,new object[]{part}),Does.Contain("napraw"));
    }
    [Test] public void WeaponCategoryRewardSurvivesCheckpointAndNamesItsScope()
    {
        Assert.That(System.Enum.GetNames(typeof(R1BonusScope)),Does.Contain("Weapons"));
        var reward=Resources.Load<R1RunDefinition>("R1/Run").Upgrades.SingleOrDefault(u=>u!=null&&u.Id=="damage_weapons");
        Assert.That(reward,Is.Not.Null);Assert.That(reward.Scope.ToString(),Is.EqualTo("Weapons"));
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);
        var item=model.Inventory.Single();var blueprint=R3BlueprintPart.FromPart(item,Vector3Int.zero);
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(blueprint.Matches(restored.Inventory.Single()),Is.True);
        var label=typeof(R1RunHUD).GetMethod("BonusInstanceText",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        Assert.That((string)label.Invoke(null,new object[]{item}),Does.Contain("broni"));
    }
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
    [Test] public void DroneRewardScopeSurvivesInventoryAndIsVisible()
    {
        var run=Resources.Load<R1RunDefinition>("R1/Run");
        var reward=run.Upgrades.SingleOrDefault(p=>p!=null&&p.Id=="damage_drones");
        Assert.That(reward,Is.Not.Null);Assert.That(reward.Scope.ToString(),Is.EqualTo("Drones"));
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);
        var item=model.Inventory.Single();var blueprint=R3BlueprintPart.FromPart(item,Vector3Int.zero);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(blueprint.Matches(restored.Inventory.Single()),Is.True);
        var text=typeof(R1RunHUD).GetMethod("BonusInstanceText",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        Assert.That((string)text.Invoke(null,new object[]{item}),Does.Contain("dronów"));
    }
    [TestCase("damage_turrets","Turrets","wieżyczek")]
    [TestCase("damage_traps","Traps","pułapek")]
    [TestCase("damage_blast","BlastDamage","wybuchowe")]
    [TestCase("damage_piercing","PiercingDamage","przebijające")]
    [TestCase("damage_kinetic","KineticDamage","kinetyczne")]
    public void DeployableCategoryRewardsSurviveSaveAndShowScope(string id,string scope,string label)
    {
        var run=Resources.Load<R1RunDefinition>("R1/Run");var reward=run.Upgrades.SingleOrDefault(p=>p!=null&&p.Id==id);
        Assert.That(reward,Is.Not.Null);Assert.That(reward.Scope.ToString(),Is.EqualTo(scope));
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);
        var item=model.Inventory.Single();var blueprint=R3BlueprintPart.FromPart(item,Vector3Int.zero);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(blueprint.Matches(restored.Inventory.Single()),Is.True);
        var text=typeof(R1RunHUD).GetMethod("BonusInstanceText",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        Assert.That((string)text.Invoke(null,new object[]{item}),Does.Contain(label));
    }
    [Test] public void HybridRoundsRewardSurvivesSaveAndIsClearlyDescribed()
    {
        var run=Resources.Load<R1RunDefinition>("R1/Run");var reward=run.Upgrades.SingleOrDefault(p=>p!=null&&p.Id=="hybrid_basic_rounds");
        Assert.That(reward,Is.Not.Null);Assert.That(reward.Effect.ToString(),Is.EqualTo("HybridBlastRounds"));
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);
        var item=model.Inventory.Single();var blueprint=R3BlueprintPart.FromPart(item,Vector3Int.zero);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(blueprint.Matches(restored.Inventory.Single()),Is.True);
        var text=typeof(R1RunHUD).GetMethod("BonusInstanceText",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        Assert.That((string)text.Invoke(null,new object[]{item}),Does.Contain("wybuchowej"));
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
    [UnityTest] public IEnumerator ExtremeFiniteWeaponDamageDoesNotOverflowIntoZeroOrInvalidHits()
    {
        R1EncounterManager.ClearTransientObjects();
        var owner=new GameObject("Extreme weapon owner");owner.transform.position=new Vector3(1500,1500,1500);
        owner.AddComponent<Rigidbody>().isKinematic=true;
        GameObject Weapon(string label,out Transform muzzle)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=label;part.transform.SetParent(owner.transform,false);
            part.GetComponent<Collider>().enabled=false;part.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Configure(100,false);
            muzzle=new GameObject("muzzle").transform;muzzle.SetParent(part.transform,false);muzzle.localPosition=Vector3.forward;return part;
        }
        var shotgunObject=Weapon("extreme shotgun",out var shotgunMuzzle);
        var shotgun=shotgunObject.AddComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
        shotgun.Bind(new R3PartState(default,R3PartKind.Gun){Weapon=RoboRougelike.R4.R4WeaponKind.Shotgun,Tier=3},owner.transform,shotgunMuzzle,float.MaxValue);
        Assert.That(shotgun.Fire(),Is.True);
        var pellets=Object.FindObjectsByType<RogueMachine.P12PipelineSafe.P17PhysicalShell>(FindObjectsSortMode.None)
            .Where(p=>p.name=="WPN001 physical pellet").ToArray();
        Assert.That(pellets.Length,Is.GreaterThan(0));
        Assert.That(pellets.All(p=>!float.IsNaN(p.DamageAmount)&&!float.IsInfinity(p.DamageAmount)&&p.DamageAmount>1e30f),Is.True,"Pellets must retain finite positive damage.");
        R1EncounterManager.ClearTransientObjects();yield return null;
        var rocketObject=Weapon("extreme rocket",out var rocketMuzzle);
        var rocket=rocketObject.AddComponent<RoboRougelike.WeaponIntegration.RRLRocketBinding>();
        rocket.Bind(new R3PartState(default,R3PartKind.Gun){Weapon=RoboRougelike.R4.R4WeaponKind.RocketLauncher,Tier=3},owner.transform,rocketMuzzle,float.MaxValue);
        Assert.That(rocket.Fire(),Is.True);
        Assert.That(rocket.Module.CurrentSpec.CenterDamage,Is.GreaterThan(1e30f));
        Assert.That(float.IsInfinity(rocket.Module.CurrentSpec.CenterDamage),Is.False);
        R1EncounterManager.ClearTransientObjects();yield return null;
        var railObject=Weapon("extreme rail",out var railMuzzle);
        var rail=railObject.AddComponent<RoboRougelike.WeaponIntegration.RRLRailgunBinding>();
        rail.Bind(new R3PartState(default,R3PartKind.Gun){Weapon=RoboRougelike.R4.R4WeaponKind.Railgun,Tier=3},owner.transform,railMuzzle,float.MaxValue);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=railMuzzle.position+Vector3.forward*5;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(500,false);Physics.SyncTransforms();
        Assert.That(rail.Fire(),Is.True);
        var railShot=Object.FindObjectsByType<RoboRougelike.WeaponIntegration.RRLRailPenetration>(FindObjectsSortMode.None).Single();
        railShot.Step(.1f);
        float until=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<until&&hp.CurrentHp>=500)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.LessThan(500),"The rail projectile must deal damage instead of passing an infinite value to HP.");
        R1EncounterManager.ClearTransientObjects();Object.Destroy(owner);Object.Destroy(target);
    }
    [UnityTest] public IEnumerator WeaponCategoryBonusAffectsWeaponHitsButNotDrones()
    {
        Assert.That(System.Enum.GetNames(typeof(R1BonusScope)),Does.Contain("Weapons"));
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var global=BacklogBonusTierTests.OperationBonus("global-weapons",.2f,"Increased");
        var more=BacklogBonusTierTests.OperationBonus("global-more-weapons",.5f,"More");
        var weapons=BacklogBonusTierTests.OperationBonus("weapons-only",.4f,"Increased");
        weapons.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"Weapons");
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{global,more,weapons}),Is.True);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(id,c,default)==null);
            Assert.That(run.GarageMount(id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var tank=run.Player.Controller;Assert.That(tank.PackageDamageScale,Is.EqualTo(2.4f).Within(.001));
        Assert.That(tank.PackageBlastDamageScale,Is.EqualTo(2.4f).Within(.001));
        Assert.That(tank.PackagePiercingDamageScale,Is.EqualTo(2.4f).Within(.001));
        var muzzle=new GameObject("Weapon bonus muzzle").transform;muzzle.position=new Vector3(1000,1000,1000);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=muzzle.position+Vector3.forward*4;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);Physics.SyncTransforms();
        tank.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Basic,"weapon-scope");float deadline=Time.time+3;
        while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(904).Within(.02));hp.RestoreHealthRatio(1);
        var device=GameObject.CreatePrimitive(PrimitiveType.Cube);device.transform.position=muzzle.position;
        var body=device.AddComponent<Rigidbody>();body.isKinematic=true;var drone=device.AddComponent<R3FlyingDrone>();
        drone.Configure(run.Player.transform,0,.5f,10);drone.enabled=false;Physics.SyncTransforms();
        typeof(R3FlyingDrone).GetMethod("Fire",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(drone,new object[]{Vector3.forward});
        deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(982).Within(.02),"Weapon bonuses must not apply to drones.");
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==model.Parts.Single(s=>s.UpgradeId=="weapons-only").Id);
        part.gameObject.SetActive(false);yield return null;
        Assert.That(tank.PackageDamageScale,Is.EqualTo(1.8f).Within(.001));
        Object.Destroy(muzzle.gameObject);Object.Destroy(target);Object.Destroy(device);Object.Destroy(global);Object.Destroy(more);Object.Destroy(weapons);
    }
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
    [UnityTest] public IEnumerator GlobalDamageBonusReachesDroneAndTurretButBasicCannonBonusDoesNot()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var global=BacklogBonusTierTests.OperationBonus("global-device",.25f,"Increased");
        var basic=BacklogBonusTierTests.OperationBonus("basic-only-device",.3f,"Increased");basic.Scope=R1BonusScope.BasicCannon;
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{global,basic}),Is.True);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>model.OwnedPlacementError(id,c,default)==null);Assert.That(run.GarageMount(id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var globalPart=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeScope==R1BonusScope.Global);
        var fire=typeof(R3FlyingDrone).GetMethod("Fire",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        Assert.That(fire,Is.Not.Null);
        for(int i=0;i<2;i++)
        {
            var device=GameObject.CreatePrimitive(PrimitiveType.Cube);device.transform.position=new Vector3(1000+i*10,1000,1000);
            var body=device.AddComponent<Rigidbody>();body.isKinematic=true;var gun=device.AddComponent<R3FlyingDrone>();
            if(i==0)gun.Configure(run.Player.transform,0,.5f,10);else gun.ConfigureTurret(run.Player.transform,.5f,10);
            gun.enabled=false;
            var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=device.transform.position+Vector3.forward*4;
            var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);Physics.SyncTransforms();
            fire.Invoke(gun,new object[]{Vector3.forward});
            float deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
            Assert.That(hp.CurrentHp,Is.EqualTo(987.5f).Within(.02),i==0?"Drone shot":"Turret shot");
            globalPart.gameObject.SetActive(false);yield return null;hp.RestoreHealthRatio(1);fire.Invoke(gun,new object[]{Vector3.forward});
            deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
            Assert.That(hp.CurrentHp,Is.EqualTo(990).Within(.02),"An already deployed device must lose a disabled owner's bonus.");
            globalPart.gameObject.SetActive(true);yield return null;
            Object.Destroy(device);Object.Destroy(target);
        }
        Object.Destroy(global);Object.Destroy(basic);
    }
    [UnityTest] public IEnumerator GlobalDamageBonusReachesBlastTrapShieldImpact()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var bonus=BacklogBonusTierTests.OperationBonus("global-trap",.25f,"Increased");var model=run.VehicleModel;
        Assert.That(model.ReceiveBonuses(new[]{bonus}),Is.True);var id=model.Inventory.Single().Id;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct().First(c=>model.OwnedPlacementError(id,c,default)==null);
        Assert.That(run.GarageMount(id,cell),Is.True);Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var enemy=run.GetComponent<RoboRougelike.R6.R6TestRange>().TrainingTarget;
        enemy.GetComponent<Rigidbody>().isKinematic=true;enemy.transform.position=new Vector3(1000,1000,1000);Physics.SyncTransforms();
        var dome=GameObject.CreatePrimitive(PrimitiveType.Cube);dome.GetComponent<Collider>().enabled=false;dome.transform.position=enemy.Core.transform.position;
        var boundary=dome.AddComponent<SphereCollider>();boundary.isTrigger=true;
        var hp=dome.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(500,false);dome.AddComponent<R3ShieldDome>().Configure(2,6);
        var root=new GameObject("Bonus trap");root.transform.position=dome.transform.position+Vector3.forward*8;
        var sensor=root.AddComponent<SphereCollider>();sensor.isTrigger=true;sensor.radius=10;
        var trap=root.AddComponent<R3DeployableTrap>();trap.Configure(root.transform,run.Player.transform,R3PartKind.BlastTrapDeployer,10,20,3,6,12,5,15,.5f,2);
        Physics.SyncTransforms();float deadline=Time.time+2;
        while(Time.time<deadline&&hp.CurrentHp>=500)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(475).Within(.02),"The global +25% must also apply to the trap's single shield impact.");
        if(root!=null)Object.Destroy(root);Object.Destroy(dome);Object.Destroy(bonus);
    }
    [UnityTest] public IEnumerator DroneScopedBonusStacksWithGlobalButDoesNotAffectTurrets()
    {
        Assert.That(System.Enum.GetNames(typeof(R1BonusScope)),Does.Contain("Drones"),"The damage system needs a separate drone category.");
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var global=BacklogBonusTierTests.OperationBonus("global-drone",.25f,"Increased");
        var more=BacklogBonusTierTests.OperationBonus("global-more-drone",.5f,"More");
        var scoped=BacklogBonusTierTests.OperationBonus("drone-only",.4f,"Increased");
        scoped.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"Drones");
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{global,more,scoped}),Is.True);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>model.OwnedPlacementError(id,c,default)==null);Assert.That(run.GarageMount(id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        Assert.That(run.Player.Controller.EffectiveDamage,Is.EqualTo(40*1.875f).Within(.01),"Drone bonuses must not affect the player's cannon.");
        var dronePart=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeScope.ToString()=="Drones");
        var fire=typeof(R3FlyingDrone).GetMethod("Fire",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        for(int i=0;i<2;i++)
        {
            var device=GameObject.CreatePrimitive(PrimitiveType.Cube);device.transform.position=new Vector3(1020+i*10,1000,1000);
            var body=device.AddComponent<Rigidbody>();body.isKinematic=true;var gun=device.AddComponent<R3FlyingDrone>();
            if(i==0)gun.Configure(run.Player.transform,0,.5f,10);else gun.ConfigureTurret(run.Player.transform,.5f,10);gun.enabled=false;
            var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=device.transform.position+Vector3.forward*4;
            var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);Physics.SyncTransforms();
            fire.Invoke(gun,new object[]{Vector3.forward});float deadline=Time.time+3;
            while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
            Assert.That(hp.CurrentHp,Is.EqualTo(i==0?975.25f:981.25f).Within(.02),"Only drone damage receives its category's +40% before the global ×1.5.");
            if(i==0)
            {
                dronePart.gameObject.SetActive(false);yield return null;hp.RestoreHealthRatio(1);fire.Invoke(gun,new object[]{Vector3.forward});
                deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
                Assert.That(hp.CurrentHp,Is.EqualTo(981.25f).Within(.02),"Existing drones lose the category bonus when its module is disabled.");
                dronePart.gameObject.SetActive(true);yield return null;
            }
            Object.Destroy(device);Object.Destroy(target);
        }
        Object.Destroy(global);Object.Destroy(more);Object.Destroy(scoped);
    }
    [UnityTest] public IEnumerator TurretAndTrapScopedBonusesAffectOnlyTheirOwnDevices()
    {
        var names=System.Enum.GetNames(typeof(R1BonusScope));
        Assert.That(names,Does.Contain("Turrets"));Assert.That(names,Does.Contain("Traps"));
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var turret=BacklogBonusTierTests.OperationBonus("turret-only",.4f,"Increased");turret.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"Turrets");
        var trapBonus=BacklogBonusTierTests.OperationBonus("trap-only",.5f,"Increased");trapBonus.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"Traps");
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{turret,trapBonus}),Is.True);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>model.OwnedPlacementError(id,c,default)==null);Assert.That(run.GarageMount(id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        Assert.That(run.Player.Controller.EffectiveDamage,Is.EqualTo(40).Within(.01));
        var fire=typeof(R3FlyingDrone).GetMethod("Fire",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        for(int i=0;i<2;i++)
        {
            var device=GameObject.CreatePrimitive(PrimitiveType.Cube);device.transform.position=new Vector3(1050+i*10,1000,1000);
            device.AddComponent<Rigidbody>().isKinematic=true;var gun=device.AddComponent<R3FlyingDrone>();
            if(i==0)gun.ConfigureTurret(run.Player.transform,.5f,10);else gun.Configure(run.Player.transform,0,.5f,10);gun.enabled=false;
            var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=device.transform.position+Vector3.forward*4;
            var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);Physics.SyncTransforms();
            fire.Invoke(gun,new object[]{Vector3.forward});float deadline=Time.time+3;
            while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
            Assert.That(hp.CurrentHp,Is.EqualTo(i==0?986f:990f).Within(.02),"Only the turret receives its category's +40%.");
            Object.Destroy(device);Object.Destroy(target);
        }
        var enemy=run.GetComponent<RoboRougelike.R6.R6TestRange>().TrainingTarget;
        enemy.GetComponent<Rigidbody>().isKinematic=true;enemy.transform.position=new Vector3(1000,1000,1000);Physics.SyncTransforms();
        var dome=GameObject.CreatePrimitive(PrimitiveType.Cube);dome.GetComponent<Collider>().enabled=false;dome.transform.position=enemy.Core.transform.position;
        var boundary=dome.AddComponent<SphereCollider>();boundary.isTrigger=true;
        var shield=dome.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();shield.Configure(500,false);dome.AddComponent<R3ShieldDome>().Configure(2,6);
        var root=new GameObject("Scoped bonus trap");root.transform.position=dome.transform.position+Vector3.forward*8;
        var sensor=root.AddComponent<SphereCollider>();sensor.isTrigger=true;sensor.radius=10;
        var trap=root.AddComponent<R3DeployableTrap>();trap.Configure(root.transform,run.Player.transform,R3PartKind.BlastTrapDeployer,10,20,3,6,12,5,15,.5f,2);
        Physics.SyncTransforms();float end=Time.time+2;while(Time.time<end&&shield.CurrentHp>=500)yield return new WaitForFixedUpdate();
        Assert.That(shield.CurrentHp,Is.EqualTo(470).Within(.02),"The trap receives only its own +50%.");
        if(root!=null)Object.Destroy(root);Object.Destroy(dome);Object.Destroy(turret);Object.Destroy(trapBonus);
    }
    [UnityTest] public IEnumerator BlastBonusStacksWithGlobalAndTrapBonusesWithoutBuffingKineticShots()
    {
        Assert.That(System.Enum.GetNames(typeof(R1BonusScope)),Does.Contain("BlastDamage"),"Blast damage needs its own modifier scope.");
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var global=BacklogBonusTierTests.OperationBonus("blast-global",.25f,"Increased");
        var more=BacklogBonusTierTests.OperationBonus("blast-more",.5f,"More");
        var blast=BacklogBonusTierTests.OperationBonus("blast-only",.4f,"Increased");blast.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"BlastDamage");
        var trapBonus=BacklogBonusTierTests.OperationBonus("blast-trap",.3f,"Increased");trapBonus.Scope=R1BonusScope.Traps;
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{global,more,blast,trapBonus}),Is.True);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>model.OwnedPlacementError(id,c,default)==null);Assert.That(run.GarageMount(id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        Assert.That(run.Player.Controller.EffectiveDamage,Is.EqualTo(40*1.875f).Within(.01),"Kinetic cannon shots do not get blast damage.");
        var effects=run.Player.GetComponent<R3MountedBonusEffects>();
        Assert.That(effects.TrapDamageMultiplier,Is.EqualTo(2.325f).Within(.001),"A kinetic damage field receives the trap but not the blast modifier.");
        Assert.That(effects.BlastDamageMultiplier,Is.EqualTo(2.475f).Within(.001),"A rocket receives blast but not trap category modifiers.");
        var enemy=run.GetComponent<RoboRougelike.R6.R6TestRange>().TrainingTarget;
        enemy.GetComponent<Rigidbody>().isKinematic=true;enemy.transform.position=new Vector3(1000,1000,1000);Physics.SyncTransforms();
        var dome=GameObject.CreatePrimitive(PrimitiveType.Cube);dome.GetComponent<Collider>().enabled=false;dome.transform.position=enemy.Core.transform.position;
        var boundary=dome.AddComponent<SphereCollider>();boundary.isTrigger=true;
        var shield=dome.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();shield.Configure(500,false);dome.AddComponent<R3ShieldDome>().Configure(2,6);
        var root=new GameObject("Blast type bonus trap");root.transform.position=dome.transform.position+Vector3.forward*8;
        var sensor=root.AddComponent<SphereCollider>();sensor.isTrigger=true;sensor.radius=10;
        var trap=root.AddComponent<R3DeployableTrap>();trap.Configure(root.transform,run.Player.transform,R3PartKind.BlastTrapDeployer,10,20,3,6,12,5,15,.5f,2);
        Physics.SyncTransforms();float end=Time.time+2;while(Time.time<end&&shield.CurrentHp>=500)yield return new WaitForFixedUpdate();
        Assert.That(shield.CurrentHp,Is.EqualTo(441.5f).Within(.02),"20 × (1 + global .25 + trap .3 + blast .4) × 1.5");
        if(root!=null)Object.Destroy(root);Object.Destroy(dome);Object.Destroy(global);Object.Destroy(more);Object.Destroy(blast);Object.Destroy(trapBonus);
    }
    [UnityTest] public IEnumerator KineticBonusStacksWithWeaponAndDroneButNotBlastOrPiercing()
    {
        Assert.That(System.Enum.GetNames(typeof(R1BonusScope)),Does.Contain("KineticDamage"));
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var global=BacklogBonusTierTests.OperationBonus("kinetic-global",.25f,"Increased");
        var more=BacklogBonusTierTests.OperationBonus("kinetic-more",.5f,"More");
        var kinetic=BacklogBonusTierTests.OperationBonus("kinetic-only",.4f,"Increased");kinetic.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"KineticDamage");
        var basic=BacklogBonusTierTests.OperationBonus("kinetic-basic",.3f,"Increased");basic.Scope=R1BonusScope.BasicCannon;
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{global,more,kinetic,basic}),Is.True);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>model.OwnedPlacementError(id,c,default)==null);Assert.That(run.GarageMount(id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        Assert.That(run.Player.Controller.EffectiveDamage,Is.EqualTo(40*2.925f).Within(.01),"Basic: (1 + .25 + .3 + .4) × 1.5");
        Assert.That(run.Player.Controller.PackageBlastDamageScale,Is.EqualTo(1.875f).Within(.001));
        Assert.That(run.Player.Controller.PackagePiercingDamageScale,Is.EqualTo(1.875f).Within(.001));
        var muzzle=new GameObject("Kinetic bonus muzzle").transform;muzzle.position=new Vector3(1000,1000,1000);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=muzzle.position+Vector3.forward*4;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);Physics.SyncTransforms();
        run.Player.Controller.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Automatic,"kinetic-auto");
        float deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(1000-12*2.475f).Within(.02),"Automatic: (1 + .25 + .4) × 1.5");
        var device=GameObject.CreatePrimitive(PrimitiveType.Cube);device.transform.position=new Vector3(1020,1000,1000);
        device.AddComponent<Rigidbody>().isKinematic=true;var drone=device.AddComponent<R3FlyingDrone>();drone.Configure(run.Player.transform,0,.5f,10);drone.enabled=false;
        target.transform.position=device.transform.position+Vector3.forward*4;hp.RestoreHealthRatio(1);Physics.SyncTransforms();
        typeof(R3FlyingDrone).GetMethod("Fire",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(drone,new object[]{Vector3.forward});
        deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(1000-10*2.475f).Within(.02),"Drone kinetic damage receives the same global and kinetic groups.");
        Object.Destroy(device);Object.Destroy(muzzle.gameObject);Object.Destroy(target);Object.Destroy(global);Object.Destroy(more);Object.Destroy(kinetic);Object.Destroy(basic);
    }
    [UnityTest] public IEnumerator HybridCannonShotAppliesEachTypeBonusAndHitsShieldOnce()
    {
        Assert.That(System.Enum.GetNames(typeof(R1UpgradeEffect)),Does.Contain("HybridBlastRounds"),"A mounted module must enable a mixed projectile.");
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var hybrid=BacklogBonusTierTests.OperationBonus("hybrid",.5f,"Increased",(R1UpgradeEffect)System.Enum.Parse(typeof(R1UpgradeEffect),"HybridBlastRounds"));
        var kinetic=BacklogBonusTierTests.OperationBonus("hybrid-kinetic",.5f,"Increased");kinetic.Scope=R1BonusScope.KineticDamage;
        var blast=BacklogBonusTierTests.OperationBonus("hybrid-blast",.2f,"Increased");blast.Scope=R1BonusScope.BlastDamage;
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{hybrid,hybrid,kinetic,blast}),Is.True);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>model.OwnedPlacementError(id,c,default)==null);Assert.That(run.GarageMount(id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var hybrids=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeEffect.ToString()=="HybridBlastRounds").ToArray();
        Assert.That(hybrids.Length,Is.EqualTo(2));hybrids[1].gameObject.SetActive(false);yield return null;
        var muzzle=new GameObject("Hybrid muzzle").transform;muzzle.position=new Vector3(1000,1000,1000);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=muzzle.position+Vector3.forward*4;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);Physics.SyncTransforms();
        run.Player.Controller.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Basic,"hybrid-test");
        hybrids[0].gameObject.SetActive(false);yield return null;
        float deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(946).Within(.02),"The fired shot retains 40 × .5 × 1.5 kinetic + 40 × .5 × 1.2 blast = 54 after its module is disabled.");
        hybrids[0].gameObject.SetActive(true);yield return null;
        target.SetActive(false);var dome=GameObject.CreatePrimitive(PrimitiveType.Cube);dome.GetComponent<Collider>().enabled=false;
        dome.transform.position=muzzle.position+Vector3.forward*6;var sphere=dome.AddComponent<SphereCollider>();sphere.isTrigger=true;
        var shield=dome.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();shield.Configure(500,false);dome.AddComponent<R3ShieldDome>().Configure(2,2);
        Physics.SyncTransforms();run.Player.Controller.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Basic,"hybrid-shield");
        deadline=Time.time+3;while(Time.time<deadline&&shield.CurrentHp>=500)yield return new WaitForFixedUpdate();
        Assert.That(shield.CurrentHp,Is.EqualTo(446).Within(.02),"A hybrid projectile hits the shield once for the sum of its components.");
        dome.SetActive(false);target.SetActive(true);hp.RestoreHealthRatio(1);hybrids[1].gameObject.SetActive(true);yield return null;
        run.Player.Controller.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Basic,"hybrid-full-blast");
        deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(952).Within(.02),"Two modules convert the whole shot; the kinetic bonus no longer applies.");
        hp.RestoreHealthRatio(1);foreach(var part in hybrids)part.gameObject.SetActive(false);yield return null;
        run.Player.Controller.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Basic,"hybrid-removed");
        deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(940).Within(.02),"Removing conversion modules restores the full kinetic shot.");
        Object.Destroy(dome);Object.Destroy(target);Object.Destroy(muzzle.gameObject);Object.Destroy(hybrid);Object.Destroy(kinetic);Object.Destroy(blast);
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
