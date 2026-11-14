using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R1;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RailgunFriendlyFixture : MonoBehaviour,IRRLTeamMember
{ public int TeamId=>1; }
public sealed class RailgunWallFixture : MonoBehaviour,IRRLHitReceiver
{
    public int HitCount {get;private set;}
    public bool ReceiveHit(float damage,Vector3 point,Vector3 direction,Transform source){HitCount++;return true;}
}

public sealed class BacklogProjectileTierTests
{
    [TestCase(R4WeaponKind.Shotgun)]
    [TestCase(R4WeaponKind.Railgun)]
    [TestCase(R4WeaponKind.RocketLauncher)]
    public void SameProjectileWeaponFusesAndDoesNotCreateAmmo(R4WeaponKind weapon)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(1000);
        for(int i=0;i<3;i++)Assert.That(model.PurchaseToInventory(R3PartKind.Gun,weapon),Is.True);
        var input=model.Inventory.ToArray();input[0].Ammo=-1;input[1].Ammo=2;input[2].Ammo=5;
        input[1].ReloadReadyAt=50;
        Assert.That(model.FuseInventory(input.Select(p=>p.Id).ToArray()),Is.True);
        var result=model.Inventory.Single();Assert.That(result.Tier,Is.EqualTo(2));
        Assert.That(result.Weapon,Is.EqualTo(weapon));Assert.That(result.Ammo,Is.EqualTo(2));
        Assert.That(result.ReloadReadyAt,Is.EqualTo(50));
    }
}

public sealed class BacklogProjectileTierPhysicsTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);
    }
    R3MountedPart Build(R4WeaponKind weapon,out VehicleEnergyBank bank,bool doubleShot=false,R1UpgradeDefinition bonus=null)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=weapon;state.Tier=2;
        if(bonus!=null)
        {
            Assert.That(model.ReceiveBonuses(new[]{bonus}),Is.True);var id=model.Inventory.Single().Id;
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct().First(c=>model.OwnedPlacementError(id,c,default)==null);
            Assert.That(model.Mount(id,cell),Is.True);
        }
        if(doubleShot)
        {
            model.Economy.DoubleShotChancePerUnit=1;model.Award(1000);
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.PlacementError(c,R3PartKind.DoubleShot)==null);
            Assert.That(model.Buy(cell,R3PartKind.DoubleShot),Is.True);
        }
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;bank=vehicle.GetComponent<VehicleEnergyBank>();
        return vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
    }
    P12PipelineSafeDamageable Target(R3MountedPart gun,float distance,float width=4)
    {
        var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.transform.position=gun.Muzzle.position+gun.Muzzle.forward*distance;
        item.transform.localScale=new Vector3(width,4,1);
        var hp=item.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);Physics.SyncTransforms();return hp;
    }
    [UnityTest] public IEnumerator DefeatedEnemyDropsPhysicalPartThatPlayerActuallyCollects()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        var drops=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None);
        Assert.That(drops.Length,Is.GreaterThan(0),"Defeating the real enemy must spawn an owned ledger drop");
        Assert.That(drops.Length,Is.EqualTo(run.Salvage.Items.Count(p=>p.PhysicalLootEligible)),"100% must create one pickup per eligible physical item");
        string id=drops[0].ItemId;Assert.That(run.Salvage.Items.Any(p=>p.Id==id),Is.True);
        for(int i=1;i<drops.Length;i++)drops[i].transform.position+=Vector3.right*(20+i*2);
        var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;player.position=drops[0].transform.position;
        Physics.SyncTransforms();float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&!run.VehicleModel.Owns(id))yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(run.VehicleModel.Owns(id),Is.True,"Touching the physical pickup must grant its actual part");
        Assert.That(run.Salvage.Items.Any(p=>p.Id==id),Is.False,"Collected identity must no longer be offered in rewards");
        Assert.That(run.VehicleModel.Inventory.Count(p=>p.Id==id),Is.EqualTo(1));
        R1EncounterManager.ClearTransientObjects();yield return null;
        Assert.That(Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None),Is.Empty,"Uncollected world objects must be removed on encounter cleanup");
    }
    [UnityTest] public IEnumerator ZeroDropChanceKeepsExistingRewardLedgerWithoutWorldLoot()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=0;
        var enemy=run.Encounter.Enemies.First();run.Encounter.SetCombatActive(true);
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);yield return null;
        Assert.That(Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None),Is.Empty);
        Assert.That(run.Salvage.Items.Count,Is.GreaterThan(0),"Zero world-drop chance must preserve ordinary rewards");
    }
    [UnityTest] public IEnumerator MissedDropCanBePaidForThenBonusClaimedWithoutDuplicatingItem()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;run.Encounter.SetCombatActive(true);
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        var drops=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None);Assert.That(drops.Length,Is.GreaterThan(0));
        string id=drops[0].ItemId;Assert.That(run.TryRecoverMissedLoot(id),Is.False,"Paid recovery is available only after victory");
        var ordinary=new R3PartState(default,R3PartKind.Gun){Id=System.Guid.NewGuid().ToString("N"),Weapon=R4WeaponKind.Basic,Source="test/ordinary"};
        Assert.That(run.Salvage.Record("test/ordinary-enemy",new[]{new R4SalvageItem(ordinary,1)},R3EnemyArchetype.Scout),Is.True);
        yield return null;
        typeof(R1RunController).GetField("pickupGraceDeadline",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(run,Time.time);
        float deadline=Time.realtimeSinceStartup+8;
        while(Time.realtimeSinceStartup<deadline&&run.Progress.State!=R1RunState.Salvage)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        Assert.That(run.Salvage.MissedPhysicalItems.Any(p=>p.Id==id),Is.True);
        Assert.That(run.RecoveryWindow.VisibleOptions.Any(p=>p.InstanceId==id),Is.False);
        Assert.That(run.RecoveryWindow.VisibleOptions.Any(p=>p.InstanceId==ordinary.Id),Is.True);
        Assert.That(run.VehicleModel.TrySpendScrap(run.VehicleModel.Scrap),Is.True);
        var previousWindow=run.RecoveryWindow;
        Assert.That(run.TryRecoverMissedLoot(id),Is.False,"Insufficient funds must leave recovery and rewards intact");
        Assert.That(run.RecoveryWindow,Is.SameAs(previousWindow));Assert.That(run.Salvage.MissedPhysicalItems.Any(p=>p.Id==id),Is.True);
        run.VehicleModel.Award(50);int before=run.VehicleModel.Scrap;
        var quarantine=previousWindow.GetType().GetField("quarantined",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        quarantine.SetValue(previousWindow,true);Assert.That(run.TryRecoverMissedLoot(id),Is.False,"Recovery must not bypass a quarantined reward transaction");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before));quarantine.SetValue(previousWindow,false);
        Assert.That(run.TryRecoverMissedLoot(id),Is.True);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-run.VehicleModel.Economy.MissedLootRecoveryCost));
        Assert.That(run.TryRecoverMissedLoot(id),Is.False);Assert.That(run.VehicleModel.Inventory.Count(p=>p.Id==id),Is.EqualTo(1));
        Assert.That(run.RecoveryWindow.VisibleOptions.Any(p=>p.InstanceId==ordinary.Id),Is.True,"Recovering missed loot must preserve ordinary reward offers");
        Assert.That(run.ConfirmRewards(new[]{ordinary.Id},new[]{0}),Is.True);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(run.VehicleModel.Inventory.Count(p=>p.Id==id),Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator DestroyedGunDropsBeforeCoreAndIsNotDuplicatedAtVictory()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        var gun=enemy.WeaponBlock;Assert.That(gun,Is.Not.Null);
        gun.ApplyDamageResolvedTyped(gun.CurrentHp/.9f+1,gun.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(enemy.IsDefeated,Is.False,"The enemy core must still be alive");
        var drop=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).FirstOrDefault();
        Assert.That(drop,Is.Not.Null,"Destroying an enemy gun must produce a pickup before the core dies");
        string id=drop.ItemId;var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;player.position=drop.transform.position;
        Physics.SyncTransforms();float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&!run.VehicleModel.Owns(id))yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(run.VehicleModel.Owns(id),Is.True);
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(run.Salvage.Items.Any(p=>p.Id==id),Is.False,"Wreck capture must not recreate the already collected gun");
        Assert.That(Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).Any(p=>p.ItemId==id),Is.False,"Wreck capture must not spawn a second pickup for an early drop");
        Assert.That(run.VehicleModel.Inventory.Count(p=>p.Id==id),Is.EqualTo(1));
        Assert.That(run.Salvage.Items.Count,Is.GreaterThan(0),"Remaining enemy parts must still be recorded");
    }
    [UnityTest] public IEnumerator DestroyedEnemyArmorDropsCollectibleFivePiecePackBeforeVictory()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        var baseRoot=enemy.transform.Find("Base");Assert.That(baseRoot,Is.Not.Null);
        var armor=baseRoot.GetComponentsInChildren<P12PipelineSafeDamageable>()
            .Where(p=>p.name=="P12_EnemyBlock"&&p!=enemy.Core).OrderByDescending(p=>(p.transform.position-enemy.Core.transform.position).sqrMagnitude).First();
        armor.ApplyDamageResolvedTyped(armor.CurrentHp/.75f+.1f,armor.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(enemy.IsDefeated,Is.False,"The armor drop must be available before core defeat.");
        var item=run.Salvage.Items.FirstOrDefault(p=>p.Kind==R3PartKind.Armor);Assert.That(item,Is.Not.Null,"Destroyed enemy armor must enter the canonical loot ledger.");
        var drop=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).FirstOrDefault(p=>p.ItemId==item.Id);Assert.That(drop,Is.Not.Null);
        var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;player.position=drop.transform.position;Physics.SyncTransforms();
        float deadline=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<deadline&&!run.VehicleModel.Owns(item.Id))yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(run.VehicleModel.Owns(item.Id),Is.True);
        Assert.That(run.VehicleModel.Inventory.Count(p=>p.Kind==R3PartKind.Armor&&p.Source==item.Source),Is.EqualTo(5));
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(run.Salvage.Items.Any(p=>p.Id==item.Id),Is.False,"Victory cannot duplicate collected armor.");
    }
    [UnityTest] public IEnumerator SurvivingEnemyArmorCanDropWhenCoreIsDefeated()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        enemy.Core.ApplyDamageResolvedTyped(enemy.Core.CurrentHp/.75f+.1f,enemy.Core.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(enemy.IsDefeated,Is.True);
        var armor=run.Salvage.Items.Where(p=>p.Kind==R3PartKind.Armor&&p.Health>0).ToArray();
        Assert.That(armor,Is.Not.Empty,"Surviving armor should be eligible for a physical drop when the core dies.");
        var drops=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None);
        Assert.That(armor.All(p=>drops.Count(d=>d.ItemId==p.Id)==1),Is.True,"Each dropped block has a single world object.");
        Assert.That(run.Salvage.CreateBasketOffers(1,0).SelectMany(b=>b.Items).Any(p=>p.Kind==R3PartKind.Armor),Is.False);
        yield return null;
    }
    [UnityTest] public IEnumerator UncollectedEarlyArmorDropIsNotDuplicatedAtCoreDefeat()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        var armor=enemy.transform.Find("Base").GetComponentsInChildren<P12PipelineSafeDamageable>()
            .Where(p=>p.name=="P12_EnemyBlock"&&p!=enemy.Core).OrderByDescending(p=>(p.transform.position-enemy.Core.transform.position).sqrMagnitude).First();
        armor.ApplyDamageResolvedTyped(armor.CurrentHp/.75f+.1f,armor.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        var item=run.Salvage.Items.First(p=>p.Kind==R3PartKind.Armor);string id=item.Id;
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).Count(p=>p.ItemId==id),Is.EqualTo(1));
        Assert.That(run.Salvage.Items.Count(p=>p.Id==id),Is.EqualTo(1));
        yield return null;
    }
    [UnityTest] public IEnumerator UncollectedEarlyDropIsNotSpawnedAgainWhenEnemyDies()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        var gun=enemy.WeaponBlock;gun.ApplyDamageResolvedTyped(gun.CurrentHp/.9f+1,gun.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(enemy.IsDefeated,Is.False);
        var drop=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).First();string id=drop.ItemId;
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).Count(p=>p.ItemId==id),Is.EqualTo(1));
        Assert.That(run.Salvage.Items.Count(p=>p.Id==id),Is.EqualTo(1));
        yield return null;
    }
    [UnityTest] public IEnumerator DestroyedEnemyWheelDropsItsTwoPartPackageBeforeCoreDies()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        var wheel=enemy.GetComponentsInChildren<P12PipelineSafeDamageable>().First(p=>p.gameObject.name=="Koło przeciwnika");
        wheel.ApplyDamageResolvedTyped(wheel.CurrentHp/.75f+1,wheel.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(wheel.IsDead,Is.True,"The test impact must actually destroy the wheel");
        Assert.That(enemy.IsDefeated,Is.False);
        var drop=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None)
            .First(p=>run.Salvage.Items.Any(item=>item.Id==p.ItemId&&item.Kind==R3PartKind.Wheel));
        string id=drop.ItemId;var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;player.position=drop.transform.position;
        Physics.SyncTransforms();float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&!run.VehicleModel.Owns(id))yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(run.VehicleModel.Owns(id),Is.True);
        Assert.That(run.VehicleModel.Inventory.Count(p=>p.Kind==R3PartKind.Wheel),Is.EqualTo(2));
    }
    [UnityTest] public IEnumerator DestroyedEnemyBatteryDropsBeforeCoreDefeat()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.Last();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        var battery=enemy.GetComponentInChildren<VehicleEnergyBattery>();Assert.That(battery,Is.Not.Null,"The scenario must contain a physically mounted enemy battery");
        var hp=battery.GetComponent<P12PipelineSafeDamageable>();hp.ApplyDamageResolvedTyped(hp.CurrentHp/.75f+1,hp.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(hp.IsDead,Is.True);Assert.That(enemy.IsDefeated,Is.False);
        var drop=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None)
            .FirstOrDefault(p=>run.Salvage.Items.Any(item=>item.Id==p.ItemId&&item.Kind==R3PartKind.Battery));
        Assert.That(drop,Is.Not.Null,"The destroyed battery must be recoverable before the core dies");
        string id=drop.ItemId;var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;player.position=drop.transform.position;Physics.SyncTransforms();
        float deadline=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<deadline&&!run.VehicleModel.Owns(id))yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(run.VehicleModel.Owns(id),Is.True);Assert.That(run.VehicleModel.Inventory.Single(p=>p.Id==id).Kind,Is.EqualTo(R3PartKind.Battery));
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(run.Salvage.Items.Any(p=>p.Id==id),Is.False);
        Assert.That(run.VehicleModel.Inventory.Count(p=>p.Id==id),Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator DestroyedEnemyShieldDropsBeforeCoreDefeat()=>VerifyEnemyModuleDrop(R3PartKind.DirectionalShield);
    [UnityTest] public IEnumerator DestroyedEnemyPointDefenseDropsBeforeCoreDefeat()=>VerifyEnemyModuleDrop(R3PartKind.PointDefense);
    IEnumerator VerifyEnemyModuleDrop(R3PartKind kind)
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=new Vector3(100,5,100);run.Encounter.SetCombatActive(true);Physics.SyncTransforms();
        Assert.That(kind==R3PartKind.PointDefense?enemy.EquipPointDefense():enemy.EquipDirectionalShield(),Is.True);
        var hp=kind==R3PartKind.PointDefense?enemy.GetComponentInChildren<RRLPointDefenseBinding>().Device.Damageable:
            enemy.GetComponentInChildren<RRLShieldBinding>().Device.Damageable;
        hp.RestoreHealthRatio(.01f);hp.ApplyDamageResolvedTyped(hp.CurrentHp/.9f+1,hp.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Piercing);
        Assert.That(hp.IsDead,Is.True);Assert.That(enemy.IsDefeated,Is.False);
        var drop=Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None)
            .FirstOrDefault(p=>run.Salvage.Items.Any(item=>item.Id==p.ItemId&&item.Kind==kind));
        Assert.That(drop,Is.Not.Null,"The destroyed module must drop before the core dies");
        string id=drop.ItemId;var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;player.position=drop.transform.position;Physics.SyncTransforms();
        float deadline=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<deadline&&!run.VehicleModel.Owns(id))yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(run.VehicleModel.Owns(id),Is.True);enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(run.Salvage.Items.Any(p=>p.Id==id),Is.False);Assert.That(run.VehicleModel.Inventory.Count(p=>p.Id==id),Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator ShotgunTierTwoConsumesSixEnergyAndDamagesWithRealPellets()
    {
        var gun=Build(R4WeaponKind.Shotgun,out var energy);var hp=Target(gun,6);float before=energy.Current;
        var binding=gun.GetComponent<RRLShotgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-6).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(11));
        float end=Time.time+.35f;while(Time.time<end)yield return null;
        Assert.That(hp.CurrentHp,Is.EqualTo(356).Within(.2),"Eight real pellets each deal 18 damage.");
    }
    [UnityTest] public IEnumerator DoubleShotDuplicatesShotgunPelletsWithoutExtraAmmoOrEnergy()
    {
        var gun=Build(R4WeaponKind.Shotgun,out var energy,true);var hp=Target(gun,6);float before=energy.Current;
        var binding=gun.GetComponent<RRLShotgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-6).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(11));
        float end=Time.time+.4f;while(Time.time<end)yield return null;
        Assert.That(hp.CurrentHp,Is.EqualTo(212).Within(.2),"Sixteen pellets deal 18 damage each, without recursive procs.");
    }
    [UnityTest] public IEnumerator RailgunTierTwoConsumesEnergyAndPenetratesRealTarget()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var hp=Target(gun,6);float before=energy.Current;
        var binding=gun.GetComponent<RRLRailgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-10.8f).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(11));
        float end=Time.time+.35f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(100f,130f));
    }
    [UnityTest] public IEnumerator RailgunHitsHostileDomeBeforeProtectedTarget()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var target=Target(gun,8);
        var dome=new GameObject("shield rail fixture");dome.transform.position=gun.Muzzle.position+gun.Muzzle.forward*5+gun.Muzzle.up;
        dome.AddComponent<Rigidbody>().isKinematic=true;dome.AddComponent<SphereCollider>().isTrigger=true;
        var hp=dome.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);dome.AddComponent<R3ShieldDome>().Configure(2,2);
        Physics.SyncTransforms();Assert.That(gun.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
        float end=Time.time+.35f;while(Time.time<end)yield return null;
        Assert.That(hp.CurrentHp,Is.LessThan(500),"The physical rail shot must damage the boundary");
        Assert.That(target.CurrentHp,Is.EqualTo(500),"The protected target must remain undamaged");
        Object.Destroy(dome);
    }
    [UnityTest] public IEnumerator RailgunHitsWallBeforeDomeWithoutDamagingIt()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var target=Target(gun,8);
        var dome=new GameObject("shield behind wall");dome.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6+gun.Muzzle.up;
        dome.AddComponent<Rigidbody>().isKinematic=true;dome.AddComponent<SphereCollider>().isTrigger=true;
        var hp=dome.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);dome.AddComponent<R3ShieldDome>().Configure(2,1.5f);
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=gun.Muzzle.position+gun.Muzzle.forward*2;
        wall.transform.localScale=new Vector3(3,3,.2f);var receiver=wall.AddComponent<RailgunWallFixture>();Physics.SyncTransforms();
        Assert.That(gun.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
        float end=Time.time+.35f;while(Time.time<end)yield return null;
        Assert.That(receiver.HitCount,Is.GreaterThan(0));Assert.That(hp.CurrentHp,Is.EqualTo(500));Assert.That(target.CurrentHp,Is.EqualTo(500));
        Object.Destroy(dome);Object.Destroy(wall);
    }
    [UnityTest] public IEnumerator DoubleShotDuplicatesRailgunWithoutExtraAmmoOrEnergy()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy,true);var hp=Target(gun,6);float before=energy.Current;
        var binding=gun.GetComponent<RRLRailgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-10.8f).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(11));
        float end=Time.time+.4f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(200f,260f),"Two real rail projectiles must hit without recursive procs.");
    }
    [UnityTest] public IEnumerator RailgunDoesNotVanishAmongManyIgnoredFriendlyColliders()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var hp=Target(gun,6);
        var fixture=new GameObject("Ignored friendly colliders");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*2.5f;
        fixture.AddComponent<RailgunFriendlyFixture>();
        for(int i=0;i<150;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.05f;
        Physics.SyncTransforms();
        int crowded=Physics.SphereCastAll(gun.Muzzle.position,.075f,gun.Muzzle.forward,3f,~0,QueryTriggerInteraction.Ignore)
            .Count(h=>h.collider.gameObject==fixture);
        Assert.That(crowded,Is.EqualTo(150),"The real cast must overflow the old 128-hit buffer.");
        try
        {
            Assert.That(gun.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
            float end=Time.time+.5f;while(Time.time<end)yield return null;
            Assert.That(hp.CurrentHp,Is.LessThan(500),"Ignored friendly colliders must not destroy the projectile before it reaches a valid target.");
        }
        finally{Object.Destroy(fixture);}
    }
    [UnityTest] public IEnumerator CrowdedRailgunStillStopsAtWorldWallBeforeTarget()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var hp=Target(gun,6);
        var fixture=new GameObject("Ignored friendly colliders");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*2.5f;
        fixture.AddComponent<RailgunFriendlyFixture>();
        for(int i=0;i<150;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.05f;
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name="Solid wall";wall.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4f;
        var receiver=wall.AddComponent<RailgunWallFixture>();
        wall.transform.localScale=new Vector3(4,4,.25f);Physics.SyncTransforms();
        try
        {
            Assert.That(gun.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
            float end=Time.time+.5f;while(Time.time<end)yield return null;
            Assert.That(receiver.HitCount,Is.EqualTo(1),"The projectile must actually reach the wall.");
            Assert.That(hp.CurrentHp,Is.EqualTo(500),"Complete queries must still process the first solid obstacle before the target.");
        }
        finally{Object.Destroy(fixture);Object.Destroy(wall);}
    }
    [UnityTest] public IEnumerator RailgunFallsBackToCompleteQueryBeyondMaximumBuffer()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var hp=Target(gun,6);
        var fixture=new GameObject("Extreme friendly collider crowd");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*2.5f;
        fixture.AddComponent<RailgunFriendlyFixture>();
        for(int i=0;i<4100;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.05f;
        Physics.SyncTransforms();
        Assert.That(Physics.SphereCastAll(gun.Muzzle.position,.075f,gun.Muzzle.forward,3f,~0,QueryTriggerInteraction.Ignore)
            .Count(h=>h.collider.gameObject==fixture),Is.EqualTo(4100));
        try
        {
            Assert.That(gun.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
            float end=Time.time+.5f;while(Time.time<end)yield return null;
            Assert.That(hp.CurrentHp,Is.LessThan(500),"The bounded buffer must fall back to a complete query without losing the shot.");
        }
        finally{Object.Destroy(fixture);}
    }
    [UnityTest] public IEnumerator RocketTierTwoConsumesEnergyAndExplodesAgainstRealTarget()
    {
        var gun=Build(R4WeaponKind.RocketLauncher,out var energy);var hp=Target(gun,6,2);float before=energy.Current;
        var binding=gun.GetComponent<RRLRocketBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-9f).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(9));
        float end=Time.time+.8f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(100f,120f));
    }
    [UnityTest] public IEnumerator BlastScopedBonusReachesActualRocketExplosion()
    {
        Assert.That(System.Enum.GetNames(typeof(R1BonusScope)),Does.Contain("BlastDamage"));
        var bonus=ScriptableObject.CreateInstance<R1UpgradeDefinition>();bonus.Id="rocket-blast";bonus.Effect=R1UpgradeEffect.Damage;
        bonus.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"BlastDamage");bonus.Amount=.4f;
        var ordinary=Build(R4WeaponKind.RocketLauncher,out _);var stronger=Build(R4WeaponKind.RocketLauncher,out _,bonus:bonus);
        var strongRoot=stronger.GetComponentInParent<R3VehicleAssembly>();strongRoot.transform.position+=Vector3.right*20;Physics.SyncTransforms();
        var target1=Target(ordinary,6,2);var target2=Target(stronger,6,2);
        Assert.That(ordinary.GetComponent<RRLRocketBinding>().Fire(),Is.True);Assert.That(stronger.GetComponent<RRLRocketBinding>().Fire(),Is.True);
        float deadline=Time.time+1;while(Time.time<deadline)yield return null;
        float baseline=500-target1.CurrentHp,boosted=500-target2.CurrentHp;
        Assert.That(baseline,Is.GreaterThan(0));Assert.That(boosted,Is.EqualTo(baseline*1.4f).Within(.2f),"Only the explosion receives the blast bonus.");
        Object.Destroy(bonus);Object.Destroy(target1.gameObject);Object.Destroy(target2.gameObject);
    }
    [UnityTest] public IEnumerator PiercingScopedBonusReachesActualRailgunWithoutBuffingBasicCannon()
    {
        Assert.That(System.Enum.GetNames(typeof(R1BonusScope)),Does.Contain("PiercingDamage"));
        var bonus=ScriptableObject.CreateInstance<R1UpgradeDefinition>();bonus.Id="rail-piercing";bonus.Effect=R1UpgradeEffect.Damage;
        bonus.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"PiercingDamage");bonus.Amount=.4f;
        var ordinary=Build(R4WeaponKind.Railgun,out _);var stronger=Build(R4WeaponKind.Railgun,out _,bonus:bonus);
        var strongRoot=stronger.GetComponentInParent<R3VehicleAssembly>();strongRoot.transform.position+=Vector3.right*20;Physics.SyncTransforms();
        Assert.That(strongRoot.GetComponent<P12PipelineSafeTankController>().EffectiveDamage,Is.EqualTo(40).Within(.01),"Basic kinetic shots must not gain piercing damage.");
        var target1=Target(ordinary,6);var target2=Target(stronger,6);
        Assert.That(ordinary.GetComponent<RRLRailgunBinding>().Fire(),Is.True);Assert.That(stronger.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
        float deadline=Time.time+.8f;while(Time.time<deadline)yield return null;
        float baseline=500-target1.CurrentHp,boosted=500-target2.CurrentHp;
        Assert.That(baseline,Is.GreaterThan(0));Assert.That(boosted,Is.EqualTo(baseline*1.4f).Within(.2f),"The real rail shot must receive its piercing bonus.");
        Object.Destroy(bonus);Object.Destroy(target1.gameObject);Object.Destroy(target2.gameObject);
    }
    [UnityTest] public IEnumerator KineticScopedBonusReachesActualShotgunPellets()
    {
        Assert.That(System.Enum.GetNames(typeof(R1BonusScope)),Does.Contain("KineticDamage"));
        var bonus=ScriptableObject.CreateInstance<R1UpgradeDefinition>();bonus.Id="shotgun-kinetic";bonus.Effect=R1UpgradeEffect.Damage;
        bonus.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"KineticDamage");bonus.Amount=.4f;
        var ordinary=Build(R4WeaponKind.Shotgun,out _);var stronger=Build(R4WeaponKind.Shotgun,out _,bonus:bonus);
        stronger.GetComponentInParent<R3VehicleAssembly>().transform.position+=Vector3.right*20;Physics.SyncTransforms();
        var target1=Target(ordinary,6);var target2=Target(stronger,6);
        Assert.That(ordinary.GetComponent<RRLShotgunBinding>().Fire(),Is.True);Assert.That(stronger.GetComponent<RRLShotgunBinding>().Fire(),Is.True);
        float deadline=Time.time+.8f;while(Time.time<deadline)yield return null;
        float baseline=500-target1.CurrentHp,boosted=500-target2.CurrentHp;
        Assert.That(baseline,Is.GreaterThan(0));Assert.That(boosted,Is.EqualTo(baseline*1.4f).Within(.2f),"Every real pellet must receive the kinetic bonus.");
        Object.Destroy(bonus);Object.Destroy(target1.gameObject);Object.Destroy(target2.gameObject);
    }
    [UnityTest] public IEnumerator DoubleShotDuplicatesRocketWithoutExtraAmmoOrEnergy()
    {
        var gun=Build(R4WeaponKind.RocketLauncher,out var energy,true);var hp=Target(gun,6,2);float before=energy.Current;
        var binding=gun.GetComponent<RRLRocketBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-9f).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(9));
        float end=Time.time+1f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(200f,240f),"Two real rockets must explode without recursive procs.");
    }
    [UnityTest] public IEnumerator RocketBlastDamagesTargetAmongHundredsOfUnrelatedColliders()
    {
        var gun=Build(R4WeaponKind.RocketLauncher,out var energy);var hp=Target(gun,6,2);
        var fixture=new GameObject("Crowded explosion fixture");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6f+gun.Muzzle.right*2f;
        for(int i=0;i<300;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.02f;
        Physics.SyncTransforms();
        Assert.That(Physics.OverlapSphere(fixture.transform.position,1).Count(c=>c.gameObject==fixture),Is.EqualTo(300));
        try
        {
            Assert.That(gun.GetComponent<RRLRocketBinding>().Fire(),Is.True);
            float end=Time.time+1f;while(Time.time<end)yield return null;
            Assert.That(hp.CurrentHp,Is.LessThan(500),"Unrelated nearby colliders must not erase an otherwise valid explosion.");
        }
        finally{Object.Destroy(fixture);}
    }
    [UnityTest] public IEnumerator RocketBlastStillFailsClosedBeyondConfiguredBound()
    {
        var gun=Build(R4WeaponKind.RocketLauncher,out var energy);var hp=Target(gun,6,2);
        var fixture=new GameObject("Extreme explosion saturation fixture");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6f+gun.Muzzle.right*2f;
        for(int i=0;i<1030;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.02f;
        Physics.SyncTransforms();
        try
        {
            LogAssert.Expect(LogType.Warning,"WPN-004: blast overlap overflow; no explosion damage applied.");
            Assert.That(gun.GetComponent<RRLRocketBinding>().Fire(),Is.True);
            float end=Time.time+1f;while(Time.time<end)yield return null;
            Assert.That(hp.CurrentHp,Is.EqualTo(500),"An incomplete query must not apply a partial explosion.");
        }
        finally{Object.Destroy(fixture);}
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
