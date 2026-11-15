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
    [Test] public void RepeatedDurabilityRecalculationPreservesFractionBelowOneHp()
    {
        var item=new GameObject("Low health fraction");
        try
        {
            var hp=item.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(100,false);
            hp.SetDurabilityMultiplier(.002f);hp.RestoreHealthRatio(.5f);
            hp.SetDurabilityMultiplier(.004f);
            Assert.That(hp.MaxHp,Is.EqualTo(.4f).Within(.0001f));
            Assert.That(hp.CurrentHp,Is.EqualTo(.2f).Within(.0001f));
            hp.SetDurabilityMultiplier(1f);Assert.That(hp.CurrentHp,Is.EqualTo(50).Within(.001f));
        }
        finally{Object.DestroyImmediate(item);}
    }
    [Test] public void LowMaximumHealthKeepsItsFractionAcrossRecalculationAndVisuals()
    {
        var root=new GameObject("Low health owner");root.AddComponent<Rigidbody>();
        try
        {
            var part=new GameObject("Low health part");part.transform.SetParent(root.transform);
            var hp=part.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1,false);
            hp.SetDurabilityMultiplier(.0001f);hp.RestoreHealthRatio(.5f);
            var scan=part.AddComponent<RoboRougelike.R6.COM001LiveScanPartAdapter>();
            scan.Bind(root.transform,hp,1,2,RoboRougelike.Mechanics.StructuralScan.StructuralPartType.Armor);
            Assert.That(scan.HealthFraction,Is.EqualTo(.5f).Within(.0001f),"The scanner must use actual maximum HP.");
            var visual=new GameObject("Existing model");visual.transform.SetParent(part.transform);
            RoboRougelike.R4_1.R4ReplacementDamageVisual.Attach(visual,part.transform);
            var view=visual.GetComponent<RoboRougelike.R4_1.R4ReplacementDamageVisual>();
            var tint=typeof(RoboRougelike.R4_1.R4ReplacementDamageVisual).GetMethod("Tint",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Color expected=Color.Lerp(Color.white,new Color(.16f,.09f,.06f,1),.4f);
            Assert.That(((Color)tint.Invoke(view,new object[]{Color.white})).r,Is.EqualTo(expected.r).Within(.0001f));
            hp.SetDurabilityMultiplier(.0002f);
            Assert.That(hp.MaxHp,Is.EqualTo(.0002f).Within(.00000001f));
            Assert.That(hp.CurrentHp/hp.MaxHp,Is.EqualTo(.5f).Within(.0001f),"Recalculating durability must preserve the percentage.");
            hp.SetDurabilityMultiplier(1f);
            Assert.That(hp.CurrentHp,Is.EqualTo(.5f).Within(.0001f));
        }
        finally{Object.DestroyImmediate(root);}
    }
    [Test] public void AutomaticFireRateRewardIsAvailableAndExplainsItsScope()
    {
        var upgrade=Resources.Load<R1RunDefinition>("R1/Run").Upgrades.Single(u=>u!=null&&u.Id=="rate_automatic_weapon");
        Assert.That(upgrade.Effect,Is.EqualTo(R1UpgradeEffect.Reload));
        Assert.That(upgrade.Scope,Is.EqualTo(R1BonusScope.AutomaticWeapon));
        Assert.That(upgrade.Amount,Is.EqualTo(.2f).Within(.001));
        var part=new R3PartState(default,R3PartKind.BonusModule){UpgradeEffect=upgrade.Effect,UpgradeScope=upgrade.Scope,UpgradeAmount=upgrade.Amount};
        var method=typeof(R1RunHUD).GetMethod("BonusInstanceText",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        Assert.That((string)method.Invoke(null,new object[]{part}),Does.Contain("automatu").And.Contain("20%"));
    }
    [Test] public void BonusCatalogAndInstanceLabelsShowFractionalAndExtremeFiniteAmounts()
    {
        var catalog=typeof(R1RunHUD).GetMethod("BonusText",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        var instance=typeof(R1RunHUD).GetMethod("BonusInstanceText",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        var item=new R3PartState(default,R3PartKind.BonusModule){Tier=1,UpgradeEffect=R1UpgradeEffect.Damage,UpgradeAmount=.0025f};
        string fraction=.25.ToString("0.###",System.Globalization.CultureInfo.CurrentCulture)+"%";
        Assert.That((string)catalog.Invoke(null,new object[]{item}),Does.Contain(fraction));
        Assert.That((string)instance.Invoke(null,new object[]{item}),Does.Contain(fraction));
        item.UpgradeOperation=R1ModifierOperation.Reduced;
        Assert.That((string)catalog.Invoke(null,new object[]{item}),Does.Contain(fraction));
        Assert.That((string)instance.Invoke(null,new object[]{item}),Does.Contain(fraction));
        item.UpgradeOperation=R1ModifierOperation.More;
        string multiplier=1.0025.ToString("0.####",System.Globalization.CultureInfo.CurrentCulture);
        Assert.That((string)catalog.Invoke(null,new object[]{item}),Does.Contain(multiplier));
        Assert.That((string)instance.Invoke(null,new object[]{item}),Does.Contain(multiplier));
        item.UpgradeOperation=R1ModifierOperation.Increased;
        item.UpgradeAmount=float.MaxValue;
        Assert.That((string)catalog.Invoke(null,new object[]{item}),Does.Not.Contain("Infinity").And.Not.Contain("∞"));
        Assert.That((string)instance.Invoke(null,new object[]{item}),Does.Not.Contain("Infinity").And.Not.Contain("∞"));
    }
    [Test] public void NewWeaponBonusesCanAppearInRewardsAndBecomePhysicalInventory()
    {
        var ids=new[]{"damage_heavy_weapon","damage_shotgun_weapon","damage_railgun_weapon","damage_rocket_weapon",
            "damage_beam_weapon","damage_saw_weapon","damage_aura_weapon"};
        var definition=Resources.Load<R1RunDefinition>("R1/Run");Assert.That(definition,Is.Not.Null);
        var seen=new System.Collections.Generic.HashSet<string>();
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        for(int seed=0;seed<512&&seen.Count<ids.Length;seed++)
        {
            var progress=new R1RunProgress(3);Assert.That(progress.Start(),Is.True);
            Assert.That(progress.WinEncounter(definition.Upgrades,new System.Random(seed)),Is.True);
            Assert.That(progress.Offers.Count,Is.EqualTo(3));
            Assert.That(progress.Offers.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(3));
            for(int i=0;i<progress.Offers.Count;i++)
            {
                var candidate=progress.Offers[i];
                if(!ids.Contains(candidate.Id)||seen.Contains(candidate.Id))continue;
                var chosen=progress.ChooseUpgrade(i);Assert.That(chosen,Is.Not.Null);
                Assert.That(model.ReceiveBonuses(new[]{chosen}),Is.True);
                var part=model.Inventory.Single(p=>p.UpgradeId==chosen.Id);
                Assert.That(part.Kind,Is.EqualTo(R3PartKind.BonusModule));
                Assert.That(part.UpgradeScope,Is.EqualTo(chosen.Scope));
                seen.Add(chosen.Id);break;
            }
        }
        Assert.That(seen,Is.EquivalentTo(ids),"All new rewards must be reachable through the actual offer selection.");
    }
    [TestCase(R1BonusScope.HeavyWeapon,"ciężkiego działa")]
    [TestCase(R1BonusScope.ShotgunWeapon,"strzelby")]
    [TestCase(R1BonusScope.RailgunWeapon,"railguna")]
    [TestCase(R1BonusScope.RocketWeapon,"wyrzutni rakiet")]
    [TestCase(R1BonusScope.BeamWeapon,"wiązki")]
    [TestCase(R1BonusScope.SawWeapon,"piły")]
    [TestCase(R1BonusScope.AuraWeapon,"aury")]
    public void NewWeaponScopeSurvivesFusionAndHasSpecificDescription(R1BonusScope scope,string expectedName)
    {
        Assert.That(R3VehicleModel.ValidBonusScope(R1UpgradeEffect.Damage,scope),Is.True);
        Assert.That(R3VehicleModel.ValidBonusScope(R1UpgradeEffect.Mobility,scope),Is.False);
        var bonus=OperationBonus("fusion-"+scope,.3f,"Increased");bonus.Scope=scope;
        try
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
            Assert.That(model.ReceiveBonuses(new[]{bonus,bonus,bonus}),Is.True);
            Assert.That(model.FuseInventory(model.Inventory.Select(p=>p.Id).ToArray()),Is.True);
            var fused=model.Inventory.Single();Assert.That(fused.UpgradeScope,Is.EqualTo(scope));Assert.That(fused.Tier,Is.EqualTo(2));
            var caption=typeof(R1RunHUD).GetMethod("BonusText",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Assert.That((string)caption.Invoke(null,new object[]{fused}),Does.Contain(expectedName));
        }
        finally{Object.DestroyImmediate(bonus);}
    }
    [Test] public void LargeSingleBonusSurvivesRewardFusionBlueprintAndCheckpoint()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var reward=OperationBonus("large-bonus",1.5f,"Increased");
        try
        {
            Assert.That(model.ReceiveBonuses(new[]{reward,reward,reward}),Is.True);
            var items=model.Inventory.Where(p=>p.UpgradeId==reward.Id).ToArray();Assert.That(items.Length,Is.EqualTo(3));
            Assert.That(items.All(p=>p.UpgradeAmount==1.5f),Is.True,"A single configured bonus must not be clipped at 100%.");
            Assert.That(model.FuseInventory(items.Select(p=>p.Id)),Is.True);
            var fused=model.Inventory.Single(p=>p.UpgradeId==reward.Id);Assert.That(fused.UpgradeAmount,Is.EqualTo(1.5f));
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(fused.Id,c,default)==null);
            Assert.That(model.Mount(fused.Id,cell),Is.True);
            var blueprint=R3BlueprintLibrary.Capture(model,"Duża premia");Assert.That(R3BlueprintLibrary.Valid(blueprint),Is.True);
            Assert.That(blueprint.parts.Single(p=>p.upgradeId==reward.Id).upgradeAmount,Is.EqualTo(1.5f));
            var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
            Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var loaded,out var error),Is.True,error);
            Assert.That(loaded.Parts.Single(p=>p.Id==fused.Id).UpgradeAmount,Is.EqualTo(1.5f));
            foreach(float bad in new[]{-1f,float.NaN,float.PositiveInfinity})
            {
                blueprint.parts.Single(p=>p.upgradeId==reward.Id).upgradeAmount=bad;
                Assert.That(R3BlueprintLibrary.Valid(blueprint),Is.False,"Invalid bonus amounts must still be rejected.");
                checkpoint.mounted.Single(p=>p.Id==fused.Id).UpgradeAmount=bad;
                Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out _,out _),Is.False);
            }
        }
        finally{Object.DestroyImmediate(reward);}
    }
    [Test] public void ExtremeFiniteBonusLabelDoesNotOverflow()
    {
        var item=new R3PartState(default,R3PartKind.BonusModule){Tier=3,UpgradeEffect=R1UpgradeEffect.Damage,UpgradeAmount=float.MaxValue};
        var text=typeof(R1RunHUD).GetMethod("BonusInstanceText",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        string label=(string)text.Invoke(null,new object[]{item});
        Assert.That(label,Does.Not.Contain("Infinity").And.Not.Contain("∞"));
    }
    [Test] public void RocketZeroCooldownDoesNotRepeatAtTheSameClockValue()
    {
        var gate=new RoboRougelike.Mechanics.Rockets.RocketFireGate();Assert.That(gate.Ready(0),Is.True);
        gate.Commit(0,0);Assert.That(gate.Ready(0),Is.False);Assert.That(gate.Ready(.001f),Is.True);
        gate.Reset();gate.Commit(3600,0);Assert.That(gate.Ready(3600),Is.False);Assert.That(gate.Ready(3600.001f),Is.True);
        gate.Reset();gate.Commit(1,.005f);Assert.That(gate.Ready(1.006f),Is.True);
        gate.Reset();gate.Commit(1,240);Assert.That(gate.Ready(130),Is.False);
        gate.Reset();Assert.That(gate.Ready(float.NaN),Is.False);Assert.That(gate.Ready(float.PositiveInfinity),Is.False);
    }
    [Test] public void ZeroFireIntervalDoesNotTurnIntoTenMillisecondPenaltyOrRepeatAtSameTime()
    {
        Assert.That(new RoboRougelike.WPN001Shotgun.ShotgunConfig{cooldownSeconds=0}.Snapshot().CooldownSeconds,Is.Zero);
        Assert.That(new RoboRougelike.WPN002Railgun.RailgunConfig{cooldownSeconds=0}.Snapshot().Cooldown,Is.Zero);
        var shotgun=new RoboRougelike.WPN001Shotgun.ShotgunFireGate();var rail=new RoboRougelike.WPN002Railgun.RailgunFireGate();
        Assert.That(shotgun.RegisterSuccessfulVolley(0,0),Is.True);Assert.That(rail.Commit(0,0),Is.True);
        Assert.That(shotgun.CanFire(0),Is.False);Assert.That(rail.CanFire(0),Is.False);
        Assert.That(shotgun.CanFire(.001f),Is.True);Assert.That(rail.CanFire(.001f),Is.True);
        shotgun.Reset();rail.Reset();Assert.That(shotgun.RegisterSuccessfulVolley(3600,0),Is.True);Assert.That(rail.Commit(3600,0),Is.True);
        Assert.That(shotgun.CanFire(3600),Is.False);Assert.That(rail.CanFire(3600),Is.False);
        Assert.That(shotgun.CanFire(3600.001f),Is.True);Assert.That(rail.CanFire(3600.001f),Is.True);
    }
    [Test] public void ShotgunAndRailgunPreserveVeryShortAndLongFiniteCooldowns()
    {
        var shotgun=new RoboRougelike.WPN001Shotgun.ShotgunConfig{cooldownSeconds=.005f}.Snapshot();
        var rail=new RoboRougelike.WPN002Railgun.RailgunConfig{cooldownSeconds=.005f}.Snapshot();
        Assert.That(shotgun.CooldownSeconds,Is.EqualTo(.005f));Assert.That(rail.Cooldown,Is.EqualTo(.005f));
        Assert.That(new RoboRougelike.WPN001Shotgun.ShotgunConfig{cooldownSeconds=240}.Snapshot().CooldownSeconds,Is.EqualTo(240));
        Assert.That(new RoboRougelike.WPN002Railgun.RailgunConfig{cooldownSeconds=240}.Snapshot().Cooldown,Is.EqualTo(240));
        var sg=new RoboRougelike.WPN001Shotgun.ShotgunFireGate();var rg=new RoboRougelike.WPN002Railgun.RailgunFireGate();
        Assert.That(sg.RegisterSuccessfulVolley(1,.005f),Is.True);Assert.That(rg.Commit(1,.005f),Is.True);
        Assert.That(sg.CanFire(1.006f),Is.True);Assert.That(rg.CanFire(1.006f),Is.True);
        sg.Reset();rg.Reset();Assert.That(sg.RegisterSuccessfulVolley(1,240),Is.True);Assert.That(rg.Commit(1,240),Is.True);
        Assert.That(sg.CanFire(130),Is.False);Assert.That(rg.CanFire(130),Is.False);
        sg.Reset();rg.Reset();Assert.That(sg.RegisterSuccessfulVolley(3600,.00001f),Is.True);Assert.That(rg.Commit(3600,.00001f),Is.True);
        Assert.That(sg.CanFire(3600),Is.False);Assert.That(rg.CanFire(3600),Is.False);
    }
    [Test] public void ScopedDeviceRateRewardsCanBeChosenFromActualOffers()
    {
        var ids=new[]{"rate_drone","rate_turret"};var seen=new System.Collections.Generic.HashSet<string>();
        var definition=Resources.Load<R1RunDefinition>("R1/Run");var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        for(int seed=0;seed<512&&seen.Count<ids.Length;seed++)
        {
            var progress=new R1RunProgress(3);Assert.That(progress.Start(),Is.True);
            Assert.That(progress.WinEncounter(definition.Upgrades,new System.Random(seed)),Is.True);
            for(int i=0;i<progress.Offers.Count;i++)
            {
                var candidate=progress.Offers[i];if(!ids.Contains(candidate.Id)||seen.Contains(candidate.Id))continue;
                var chosen=progress.ChooseUpgrade(i);Assert.That(model.ReceiveBonuses(new[]{chosen}),Is.True);
                Assert.That(model.Inventory.Last().UpgradeScope,Is.EqualTo(chosen.Scope));seen.Add(chosen.Id);break;
            }
        }
        Assert.That(seen,Is.EquivalentTo(ids));
    }

    [TestCase("rate_drone",R1BonusScope.Drones,"dronów")]
    [TestCase("rate_turret",R1BonusScope.Turrets,"wieżyczek")]
    public void ScopedDeviceRateRewardKeepsItsScopeThroughMountedCheckpoint(string id,R1BonusScope scope,string expected)
    {
        var reward=Resources.Load<R1RunDefinition>("R1/Run").Upgrades.SingleOrDefault(u=>u!=null&&u.Id==id);
        Assert.That(reward,Is.Not.Null);Assert.That(reward.Effect,Is.EqualTo(R1UpgradeEffect.Reload));Assert.That(reward.Scope,Is.EqualTo(scope));
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);
        var part=model.Inventory.Single();var blueprint=R3BlueprintPart.FromPart(part,Vector3Int.zero);
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(part.Id,c,default)==null);
        Assert.That(model.Mount(part.Id,cell),Is.True);
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(blueprint.Matches(restored.Parts.Single(p=>p.Id==part.Id)),Is.True);
        foreach(var name in new[]{"BonusText","BonusInstanceText"})
        {var method=typeof(R1RunHUD).GetMethod(name,System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);Assert.That((string)method.Invoke(null,new object[]{part}),Does.Contain(expected));}
    }

    [Test] public void ScopedRepairRewardsCanBeChosenFromActualOffers()
    {
        var ids=new[]{"repair_drone","repair_turret"};var seen=new System.Collections.Generic.HashSet<string>();
        var definition=Resources.Load<R1RunDefinition>("R1/Run");var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        for(int seed=0;seed<512&&seen.Count<ids.Length;seed++)
        {
            var progress=new R1RunProgress(3);Assert.That(progress.Start(),Is.True);
            Assert.That(progress.WinEncounter(definition.Upgrades,new System.Random(seed)),Is.True);
            for(int i=0;i<progress.Offers.Count;i++)
            {
                var candidate=progress.Offers[i];if(!ids.Contains(candidate.Id)||seen.Contains(candidate.Id))continue;
                var chosen=progress.ChooseUpgrade(i);Assert.That(model.ReceiveBonuses(new[]{chosen}),Is.True);
                Assert.That(model.Inventory.Last().UpgradeScope,Is.EqualTo(chosen.Scope));seen.Add(chosen.Id);break;
            }
        }
        Assert.That(seen,Is.EquivalentTo(ids));
    }

    [TestCase("repair_drone",R1BonusScope.Drones,"przez drony")]
    [TestCase("repair_turret",R1BonusScope.Turrets,"przez wieżyczki")]
    public void ScopedRepairRewardsPreserveIdentityAndDescribeTheirCategory(string id,R1BonusScope scope,string expected)
    {
        var reward=Resources.Load<R1RunDefinition>("R1/Run").Upgrades.SingleOrDefault(u=>u!=null&&u.Id==id);
        Assert.That(reward,Is.Not.Null);Assert.That(reward.Effect,Is.EqualTo(R1UpgradeEffect.RepairEfficiency));Assert.That(reward.Scope,Is.EqualTo(scope));
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);
        var part=model.Inventory.Single();var blueprint=R3BlueprintPart.FromPart(part,Vector3Int.zero);
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(blueprint.Matches(restored.Inventory.Single()),Is.True);Assert.That(restored.Inventory.Single().UpgradeScope,Is.EqualTo(scope));
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(part.Id,c,default)==null);
        Assert.That(model.Mount(part.Id,cell),Is.True);
        var mountedCheckpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(mountedCheckpoint,R3VehicleKind.Light,model.Economy,0,out var mounted,out var mountedError),Is.True,mountedError);
        Assert.That(blueprint.Matches(mounted.Parts.Single(p=>p.Id==part.Id)),Is.True,"A mounted scoped bonus must survive a real model checkpoint.");
        foreach(var name in new[]{"BonusText","BonusInstanceText"})
        {
            var method=typeof(R1RunHUD).GetMethod(name,System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Assert.That((string)method.Invoke(null,new object[]{part}),Does.Contain(expected));
        }
    }

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
    [TestCase("hull_construction","Construction","klocków")]
    [TestCase("mobility_wheels","WheelDrive","kół")]
    [TestCase("mobility_tracks","TrackDrive","gąsienic")]
    [TestCase("mobility_omni","OmniDrive","Omni")]
    [TestCase("mobility_hover","HoverDrive","zawisu")]
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
        var cancellation=Group();Add(cancellation,"Increased",float.MaxValue,1);Add(cancellation,"Increased",.5f,1);Add(cancellation,"Reduced",float.MaxValue,1);
        Assert.That(Value(cancellation),Is.EqualTo(1.5f).Within(.001));
        type.GetMethod("Reset").Invoke(cancellation,null);Assert.That(Value(cancellation),Is.EqualTo(1),"A reused modifier group must clear its compensation.");
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
    [UnityTest] public IEnumerator ConventionalGunProducesAllShotsDueDuringHeldTrigger()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var tank=run.Player.Controller;
        var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun&&p.Weapon==RoboRougelike.R4.R4WeaponKind.Basic&&p.Operational);
        int slot=System.Array.FindIndex(run.VehicleModel.GetWeaponSlots(),weapon=>weapon==RoboRougelike.R4.R4WeaponKind.Basic);
        Assert.That(slot,Is.GreaterThanOrEqualTo(0));
        assembly.AimGuns(run.Player.transform.position+run.Player.transform.forward*100+Vector3.up*20);
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.True);
        R1EncounterManager.ClearTransientObjects();tank.SetRunModifiers(1,.01f,1);gun.NextFireTime=0;tank.enabled=false;
        float interval=tank.WeaponInterval(gun.Weapon);Assert.That(interval,Is.GreaterThan(0));
        int Shots()=>Object.FindObjectsByType<RogueMachine.P12PipelineSafe.P17PhysicalShell>(FindObjectsSortMode.None)
            .Count(p=>p.OwnerRoot==run.Player.transform&&p.IsActiveProjectile&&p.GetComponent<RoboRougelike.R4.R4ProjectileOrigin>().PartId==gun.Id);
        void Hold()=>assembly.SetSlotTriggers(slot==0,slot==1,slot==2,slot==3);
        Hold();assembly.FireSlot(slot);float start=Time.time;Assert.That(Shots(),Is.EqualTo(1));
        yield return new WaitForFixedUpdate();
        float elapsed=Time.time-start;Assert.That(elapsed,Is.GreaterThan(interval*2),"The test needs several due firing intervals.");
        Hold();assembly.FireSlot(slot);int expected=1+Mathf.FloorToInt(elapsed/interval);
        for(int i=0;i<8&&Shots()<expected;i++)yield return null;
        Assert.That(Shots(),Is.EqualTo(expected),"A held trigger must not lose shots merely because several cooldowns fit in one frame.");
        R1EncounterManager.ClearTransientObjects();
    }
    [UnityTest] public IEnumerator BasicGunDoesNotAccumulateShotsWhileTriggerIsReleased()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var tank=run.Player.Controller;
        var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun&&p.Weapon==RoboRougelike.R4.R4WeaponKind.Basic&&p.Operational);
        int slot=System.Array.FindIndex(run.VehicleModel.GetWeaponSlots(),weapon=>weapon==RoboRougelike.R4.R4WeaponKind.Basic);
        assembly.AimGuns(run.Player.transform.position+run.Player.transform.forward*100+Vector3.up*20);
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.True);
        R1EncounterManager.ClearTransientObjects();tank.SetRunModifiers(1,.01f,1);gun.NextFireTime=0;tank.enabled=false;
        void Hold(bool held)=>assembly.SetSlotTriggers(held&&slot==0,held&&slot==1,held&&slot==2,held&&slot==3);
        int Shots()=>Object.FindObjectsByType<RogueMachine.P12PipelineSafe.P17PhysicalShell>(FindObjectsSortMode.None)
            .Count(p=>p.OwnerRoot==run.Player.transform&&p.IsActiveProjectile&&p.GetComponent<RoboRougelike.R4.R4ProjectileOrigin>().PartId==gun.Id);
        Hold(true);assembly.FireSlot(slot);Assert.That(Shots(),Is.EqualTo(1));Hold(false);
        for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
        Hold(true);assembly.FireSlot(slot);Assert.That(Shots(),Is.EqualTo(2),"Released time must not be converted into a backlog.");
        R1EncounterManager.ClearTransientObjects();
    }
    [UnityTest] public IEnumerator BasicCadenceRollsDoubleShotForEveryPrimaryWithoutExtraEnergy()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var model=run.VehicleModel;Assert.That(run.GaragePurchase(R3PartKind.DoubleShot),Is.True);
        var bonus=model.Inventory.Last(p=>p.Kind==R3PartKind.DoubleShot);model.Economy.DoubleShotChancePerUnit=1f;
        var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        Assert.That(run.GarageMount(bonus.Id,cells.First(c=>model.OwnedPlacementError(bonus.Id,c,default)==null)),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var tank=run.Player.Controller;
        var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun&&p.Weapon==RoboRougelike.R4.R4WeaponKind.Basic&&p.Operational);
        int slot=System.Array.FindIndex(model.GetWeaponSlots(),weapon=>weapon==RoboRougelike.R4.R4WeaponKind.Basic);
        assembly.AimGuns(run.Player.transform.position+run.Player.transform.forward*100+Vector3.up*20);
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.True);
        R1EncounterManager.ClearTransientObjects();tank.SetRunModifiers(1,.01f,1);gun.NextFireTime=0;tank.enabled=false;
        var bank=run.Player.GetComponent<VehicleEnergyBank>();Assert.That(bank.TrySpend(50),Is.True);
        void Hold()=>assembly.SetSlotTriggers(slot==0,slot==1,slot==2,slot==3);
        int Shots()=>Object.FindObjectsByType<RogueMachine.P12PipelineSafe.P17PhysicalShell>(FindObjectsSortMode.None)
            .Count(p=>p.OwnerRoot==run.Player.transform&&p.IsActiveProjectile&&p.GetComponent<RoboRougelike.R4.R4ProjectileOrigin>().PartId==gun.Id);
        float before=bank.Current;Hold();assembly.FireSlot(slot);float start=Time.time;
        Assert.That(bank.Current,Is.EqualTo(before));
        yield return new WaitForFixedUpdate();
        float elapsed=Time.time-start;Assert.That(elapsed,Is.GreaterThan(tank.WeaponInterval(gun.Weapon)*2));
        R1EncounterManager.ClearTransientObjects();
        var queue=run.Player.GetComponent<R3ConventionalProjectileQueue>();
        typeof(R3ConventionalProjectileQueue).GetField("workBudgetMilliseconds",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(queue,1000d);
        before=bank.Current;Hold();assembly.FireSlot(slot);Assert.That(bank.Current,Is.EqualTo(before));
        int expected=2*Mathf.FloorToInt(elapsed/tank.WeaponInterval(gun.Weapon));
        for(int i=0;i<30&&Shots()<expected;i++)yield return null;
        Assert.That(Shots(),Is.EqualTo(expected),"Each basic shot needs its own non-recursive extra projectile.");
        var shots=Object.FindObjectsByType<RogueMachine.P12PipelineSafe.P17PhysicalShell>(FindObjectsSortMode.None)
            .Where(p=>p.OwnerRoot==run.Player.transform&&p.IsActiveProjectile&&p.GetComponent<RoboRougelike.R4.R4ProjectileOrigin>().PartId==gun.Id).ToArray();
        for(int i=0;i<shots.Length;i++)for(int j=i+1;j<shots.Length;j++)
            Assert.That(Physics.GetIgnoreCollision(shots[i].GetComponent<Collider>(),shots[j].GetComponent<Collider>()),Is.True,"Shots emitted from the same cadence sample must not collide with each other.");
        R1EncounterManager.ClearTransientObjects();
    }
    [UnityTest] public IEnumerator BasicCadenceShotsRemainIndependentAndDamageTheTarget()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var tank=run.Player.Controller;
        var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun&&p.Weapon==RoboRougelike.R4.R4WeaponKind.Basic&&p.Operational);
        int slot=System.Array.FindIndex(run.VehicleModel.GetWeaponSlots(),weapon=>weapon==RoboRougelike.R4.R4WeaponKind.Basic);
        run.Player.GetComponent<Rigidbody>().isKinematic=true;
        assembly.AimGuns(run.Player.transform.position+run.Player.transform.forward*100+Vector3.up*20);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.transform.SetPositionAndRotation(gun.Muzzle.position+gun.Muzzle.forward*6,Quaternion.LookRotation(gun.Muzzle.forward));
        target.transform.localScale=new Vector3(3,3,.5f);
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(10000,false);
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.True);
        R1EncounterManager.ClearTransientObjects();tank.SetRunModifiers(1,.01f,1);gun.NextFireTime=0;tank.enabled=false;
        void Hold()=>assembly.SetSlotTriggers(slot==0,slot==1,slot==2,slot==3);
        try
        {
            Hold();assembly.FireSlot(slot);float start=Time.time;
            yield return new WaitForFixedUpdate();
            float elapsed=Time.time-start,interval=tank.WeaponInterval(gun.Weapon);
            Assert.That(elapsed,Is.GreaterThan(interval*2));Hold();assembly.FireSlot(slot);
            int expected=1+Mathf.FloorToInt(elapsed/interval);
            float deadline=Time.realtimeSinceStartup+4;
            while(hp.CurrentHp>10000-40*expected+.1f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            Assert.That(hp.CurrentHp,Is.EqualTo(10000-40*expected).Within(.1f),"Each due projectile must deliver its own physical hit.");
        }
        finally{Object.Destroy(target);R1EncounterManager.ClearTransientObjects();}
    }
    [UnityTest] public IEnumerator UnrelatedBonusChangesDoNotDamageLowHealthArmor()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var fragile=BacklogBonusTierTests.OperationBonus("fragile-armor-test",.995f,"Less",R1UpgradeEffect.Hull);
        var damage=BacklogBonusTierTests.OperationBonus("unrelated-damage-test",.2f,"Increased",R1UpgradeEffect.Damage);
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{fragile,damage}),Is.True);
        var damageId=model.Inventory.Single(p=>p.UpgradeId==damage.Id).Id;
        foreach(var item in model.Inventory.ToArray())
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            Assert.That(run.GarageMount(item.Id,cells.First(c=>model.OwnedPlacementError(item.Id,c,default)==null)),Is.True);
        }
        yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var armor=assembly.Parts.First(p=>p.Kind==R3PartKind.Armor).Damageable;
        var bonus=assembly.Parts.Single(p=>p.Id==damageId);var effects=run.Player.GetComponent<R3MountedBonusEffects>();
        try
        {
            Assert.That(armor.MaxHp,Is.LessThan(1));armor.RestoreHealthRatio(.5f);
            bonus.gameObject.SetActive(false);Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1).Within(.001));
            Assert.That(armor.CurrentHp/armor.MaxHp,Is.EqualTo(.5f).Within(.001),"Removing a damage bonus must not alter armor health.");
            bonus.gameObject.SetActive(true);Assert.That(effects.EffectiveStats.Damage,Is.GreaterThan(1));
            Assert.That(armor.CurrentHp/armor.MaxHp,Is.EqualTo(.5f).Within(.001));
        }
        finally{Object.Destroy(fragile);Object.Destroy(damage);}
    }
    [UnityTest] public IEnumerator DriveSpecificMobilitySurvivesActualNextEncounter()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.StartRun(),Is.True);
        Assert.That(run.ChooseRoute(0),Is.True);
        float deadline=Time.realtimeSinceStartup+30;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);run.Encounter.SetCombatActive(false);RRLW2TestFlow.DefeatAll(run);
        deadline=Time.realtimeSinceStartup+30;while(run.Progress.State!=R1RunState.Salvage&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        Assert.That(run.ConfirmRewards(System.Array.Empty<string>(),new[]{0}),Is.True);
        var reward=Resources.Load<R1RunDefinition>("R1/Run").Upgrades.Single(p=>p!=null&&p.Id=="mobility_wheels");
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);
        string id=model.Inventory.Last(p=>p.UpgradeId==reward.Id).Id;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(id,c,default)==null);
        Assert.That(run.GarageMount(id,cell),Is.True);yield return null;
        var effects=run.Player.GetComponent<R3MountedBonusEffects>();
        Assert.That(effects.DriveMobilityMultiplier,Is.EqualTo(1.2f).Within(.001f));
        float speed=run.Player.Controller.EffectiveForwardSpeed;Assert.That(speed,Is.GreaterThan(0));
        Assert.That(run.StartNextEncounter()&&run.ChooseRoute(0),Is.True);
        deadline=Time.realtimeSinceStartup+30;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);
        Assert.That(run.Player.Controller.EffectiveForwardSpeed,Is.EqualTo(speed).Within(.01f));
    }
    [UnityTest] public IEnumerator DriveSpecificMobilityFollowsSelectedDriveAndStacksWithGlobalBonus()
    {
        foreach(string name in new[]{"WheelDrive","TrackDrive","OmniDrive","HoverDrive"})
            Assert.That(System.Enum.GetNames(typeof(R1BonusScope)),Does.Contain(name));
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var model=run.VehicleModel;
        var hoverCell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.HoverEngine,false)==null);
        Assert.That(run.GarageBuy(hoverCell,R3PartKind.HoverEngine),Is.True);
        var global=BacklogBonusTierTests.OperationBonus("global-move",.2f,"Increased",R1UpgradeEffect.Mobility);
        var wheel=BacklogBonusTierTests.OperationBonus("wheel-move",.3f,"Increased",R1UpgradeEffect.Mobility);
        var hover=BacklogBonusTierTests.OperationBonus("hover-move",.5f,"More",R1UpgradeEffect.Mobility);
        var track=BacklogBonusTierTests.OperationBonus("track-move",.4f,"Increased",R1UpgradeEffect.Mobility);
        var omni=BacklogBonusTierTests.OperationBonus("omni-move",.1f,"Increased",R1UpgradeEffect.Mobility);
        wheel.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"WheelDrive");
        hover.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"HoverDrive");
        track.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"TrackDrive");
        omni.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"OmniDrive");
        Assert.That(model.ReceiveBonuses(new[]{global,wheel,hover,track,omni}),Is.True);
        string globalId=model.Inventory.Single(p=>p.UpgradeId==global.Id).Id;
        string wheelId=model.Inventory.Single(p=>p.UpgradeId==wheel.Id).Id;
        string hoverId=model.Inventory.Single(p=>p.UpgradeId==hover.Id).Id;
        string trackId=model.Inventory.Single(p=>p.UpgradeId==track.Id).Id;
        string omniId=model.Inventory.Single(p=>p.UpgradeId==omni.Id).Id;
        foreach(var id in new[]{globalId,wheelId,hoverId,trackId,omniId})
        {
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(id,c,default)==null);
            Assert.That(run.GarageMount(id,cell),Is.True);
        }
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        var pieces=new[]{globalId,wheelId,hoverId,trackId,omniId}.Select(id=>assembly.Parts.Single(p=>p.Id==id)).ToArray();
        foreach(var part in pieces)part.gameObject.SetActive(false);yield return null;
        float baseSpeed=run.Player.Controller.EffectiveForwardSpeed;
        foreach(var part in pieces)part.gameObject.SetActive(true);yield return null;
        Assert.That(run.Player.Controller.EffectiveForwardSpeed,Is.EqualTo(baseSpeed*1.5f).Within(.01f));
        Assert.That(run.ConfigureDrive(R3DriveKind.Hover),Is.True);yield return null;
        Assert.That(run.Player.Controller.EffectiveForwardSpeed,Is.EqualTo(baseSpeed*1.8f).Within(.01f));
        // Verify the remaining modifier groups independently of hardware placement.
        model.SelectedDrive=R3DriveKind.Tracks;yield return null;
        Assert.That(run.Player.Controller.EffectiveForwardSpeed,Is.EqualTo(baseSpeed*1.6f).Within(.01f));
        model.SelectedDrive=R3DriveKind.Omni;yield return null;
        Assert.That(run.Player.Controller.EffectiveForwardSpeed,Is.EqualTo(baseSpeed*1.3f).Within(.01f));
        Assert.That(run.ConfigureDrive(R3DriveKind.Wheels),Is.True);yield return null;
        Assert.That(run.Player.Controller.EffectiveForwardSpeed,Is.EqualTo(baseSpeed*1.5f).Within(.01f));
        pieces[1].gameObject.SetActive(false);yield return null;
        Assert.That(run.Player.Controller.EffectiveForwardSpeed,Is.EqualTo(baseSpeed*1.2f).Within(.01f));
        Object.Destroy(global);Object.Destroy(wheel);Object.Destroy(hover);Object.Destroy(track);Object.Destroy(omni);
    }
    [UnityTest] public IEnumerator ConstructionHullBonusSurvivesActualNextEncounter()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.StartRun(),Is.True);
        Assert.That(run.ChooseRoute(0),Is.True);
        float deadline=Time.realtimeSinceStartup+30;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);run.Encounter.SetCombatActive(false);RRLW2TestFlow.DefeatAll(run);
        deadline=Time.realtimeSinceStartup+30;while(run.Progress.State!=R1RunState.Salvage&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        Assert.That(run.ConfirmRewards(System.Array.Empty<string>(),new[]{0}),Is.True);
        var reward=Resources.Load<R1RunDefinition>("R1/Run").Upgrades.Single(p=>p!=null&&p.Id=="hull_construction");
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);
        string id=model.Inventory.Last(p=>p.UpgradeId==reward.Id).Id;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var armor=assembly.Parts.First(p=>p.Kind==R3PartKind.Armor);
        string armorId=armor.Id;float baseHp=armor.Damageable.MaxHp;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(id,c,default)==null);
        Assert.That(run.GarageMount(id,cell),Is.True);
        assembly=run.Player.GetComponent<R3VehicleAssembly>();
        Assert.That(assembly.Parts.Single(p=>p.Id==armorId).Damageable.MaxHp,Is.EqualTo(baseHp*1.3f).Within(.01f));
        Assert.That(run.StartNextEncounter()&&run.ChooseRoute(0),Is.True);
        deadline=Time.realtimeSinceStartup+30;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);
        assembly=run.Player.GetComponent<R3VehicleAssembly>();
        Assert.That(assembly.Parts.Single(p=>p.Id==armorId).Damageable.MaxHp,Is.EqualTo(baseHp*1.3f).Within(.01f));
    }
    [UnityTest] public IEnumerator ConstructionHullBonusAffectsOnlyArmorAndStacksWithGlobalHull()
    {
        Assert.That(System.Enum.GetNames(typeof(R1BonusScope)),Does.Contain("Construction"));
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var construction=BacklogBonusTierTests.OperationBonus("construction-hull",.3f,"Increased",R1UpgradeEffect.Hull);
        construction.Scope=(R1BonusScope)System.Enum.Parse(typeof(R1BonusScope),"Construction");
        var global=BacklogBonusTierTests.OperationBonus("global-hull",.2f,"Increased",R1UpgradeEffect.Hull);
        var more=BacklogBonusTierTests.OperationBonus("global-hull-more",.5f,"More",R1UpgradeEffect.Hull);
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{construction,global,more}),Is.True);
        string constructionId=model.Inventory.Single(p=>p.UpgradeId==construction.Id).Id;
        var original=run.Player.GetComponent<R3VehicleAssembly>();
        var armorId=original.Parts.First(p=>p.Kind==R3PartKind.Armor).Id;
        var wheelId=original.Parts.First(p=>p.Kind==R3PartKind.Wheel).Id;
        var coreId=original.Parts.First(p=>p.Kind==R3PartKind.Core).Id;
        float OriginalHp(string id)=>original.Parts.Single(p=>p.Id==id).Damageable.MaxHp;
        float armorHp=OriginalHp(armorId),wheelHp=OriginalHp(wheelId),coreHp=OriginalHp(coreId);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(id,c,default)==null);
            Assert.That(run.GarageMount(id,cell),Is.True);
        }
        yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        float Hp(string id)=>assembly.Parts.Single(p=>p.Id==id).Damageable.MaxHp;
        Assert.That(Hp(armorId),Is.EqualTo(armorHp*2.25f).Within(.01f));
        Assert.That(Hp(wheelId),Is.EqualTo(wheelHp*1.8f).Within(.01f));
        Assert.That(Hp(coreId),Is.EqualTo(coreHp*1.8f).Within(.01f));
        var module=assembly.Parts.Single(p=>p.Id==constructionId);
        module.gameObject.SetActive(false);yield return null;
        Assert.That(Hp(armorId),Is.EqualTo(armorHp*1.8f).Within(.01f));
        Assert.That(Hp(wheelId),Is.EqualTo(wheelHp*1.8f).Within(.01f));
        module.gameObject.SetActive(true);yield return null;
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        assembly=run.Player.GetComponent<R3VehicleAssembly>();
        Assert.That(Hp(armorId),Is.EqualTo(armorHp*2.25f).Within(.01f),"The construction modifier must survive leaving the garage.");
        Assert.That(Hp(wheelId),Is.EqualTo(wheelHp*1.8f).Within(.01f));
        Object.Destroy(construction);Object.Destroy(global);Object.Destroy(more);
    }
    [UnityTest] public IEnumerator ExtremeRailgunCooldownRemainsFiniteAndLong()
    { yield return ExtremeCooldownForPackageWeapon(false); }
    [UnityTest] public IEnumerator ExtremeRocketCooldownRemainsFiniteAndLong()
    { yield return ExtremeCooldownForPackageWeapon(true); }
    static IEnumerator ExtremeCooldownForPackageWeapon(bool rocketWeapon)
    {
        R1EncounterManager.ClearTransientObjects();
        var owner=new GameObject("Extreme cooldown owner");owner.transform.position=new Vector3(1400,1400,1400);
        owner.AddComponent<Rigidbody>().isKinematic=true;
        var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.transform.SetParent(owner.transform,false);
        part.GetComponent<Collider>().enabled=false;part.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Configure(100,false);
        var muzzle=new GameObject("muzzle").transform;muzzle.SetParent(part.transform,false);muzzle.localPosition=Vector3.forward;
        if(rocketWeapon)
        {
            var binding=part.AddComponent<RoboRougelike.WeaponIntegration.RRLRocketBinding>();
            binding.Bind(new R3PartState(default,R3PartKind.Gun){Weapon=RoboRougelike.R4.R4WeaponKind.RocketLauncher},owner.transform,muzzle,1,float.MaxValue);
            Assert.That(binding.Fire(),Is.True);
            Assert.That(binding.Module.CurrentSpec.Cooldown,Is.EqualTo(float.MaxValue));
            Assert.That(binding.Fire(),Is.False);
        }
        else
        {
            var binding=part.AddComponent<RoboRougelike.WeaponIntegration.RRLRailgunBinding>();
            binding.Bind(new R3PartState(default,R3PartKind.Gun){Weapon=RoboRougelike.R4.R4WeaponKind.Railgun},owner.transform,muzzle,1,float.MaxValue);
            Assert.That(binding.Fire(),Is.True);
            Assert.That(binding.Module.CurrentSpec.Cooldown,Is.EqualTo(float.MaxValue));
            Assert.That(binding.Fire(),Is.False);
        }
        R1EncounterManager.ClearTransientObjects();Object.Destroy(owner);yield return null;
    }
    [UnityTest] public IEnumerator ExtremeConventionalWeaponIntervalStaysFinite()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var tank=run.Player.Controller;tank.SetRunModifiers(1,float.MaxValue,1);
        Assert.That(tank.WeaponInterval(RoboRougelike.R4.R4WeaponKind.Heavy),Is.EqualTo(float.MaxValue));
    }
    [UnityTest] public IEnumerator ExtremeEmpCooldownStillAllowsFirstPulse()
    {
        var owner=new GameObject("Extreme EMP cooldown owner");owner.transform.position=new Vector3(1400,1400,1400);
        owner.AddComponent<Rigidbody>().isKinematic=true;
        var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.transform.SetParent(owner.transform,false);
        part.GetComponent<Collider>().enabled=false;part.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Configure(100,false);
        var binding=part.AddComponent<RoboRougelike.WeaponIntegration.RRLEmpBinding>();
        binding.Bind(new R3PartState(default,R3PartKind.Gun){Weapon=RoboRougelike.R4.R4WeaponKind.EmpEmitter},owner.transform,float.MaxValue);
        Assert.That(binding.Fire(),Is.True);
        Assert.That(binding.Module.CooldownRemaining,Is.EqualTo(float.MaxValue));
        Assert.That(binding.Fire(),Is.False);
        Object.Destroy(owner);yield return null;
    }
    [UnityTest] public IEnumerator ConventionalZeroIntervalFiresOnlyOncePerClockValue()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var tank=run.Player.Controller;
        var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun&&p.Weapon==RoboRougelike.R4.R4WeaponKind.Basic&&p.Operational);
        int slot=System.Array.FindIndex(run.VehicleModel.GetWeaponSlots(),weapon=>weapon==RoboRougelike.R4.R4WeaponKind.Basic);
        Assert.That(slot,Is.GreaterThanOrEqualTo(0));assembly.AimGuns(run.Player.transform.position+run.Player.transform.forward*100+Vector3.up*20);
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.True);
        R1EncounterManager.ClearTransientObjects();tank.SetRunModifiers(1,0,1);gun.NextFireTime=0;
        int Shots()=>Object.FindObjectsByType<RogueMachine.P12PipelineSafe.P17PhysicalShell>(FindObjectsSortMode.None)
            .Count(p=>p.OwnerRoot==run.Player.transform&&p.IsActiveProjectile);
        assembly.FireSlot(slot);Assert.That(Shots(),Is.EqualTo(1));assembly.FireSlot(slot);
        Assert.That(Shots(),Is.EqualTo(1),"Repeated calls at the same clock value must not create another shot.");
        yield return null;assembly.FireSlot(slot);Assert.That(Shots(),Is.EqualTo(2),"A later frame may fire again.");
        R1EncounterManager.ClearTransientObjects();
    }
    [UnityTest] public IEnumerator LargeHullAndCoreBonusesKeepCoreAndHealthFractionFinite()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var hull=BacklogBonusTierTests.OperationBonus("large-hull",1e38f,"Increased");hull.Effect=R1UpgradeEffect.Hull;
        var core=BacklogBonusTierTests.OperationBonus("large-core",10f,"Increased");core.Effect=R1UpgradeEffect.Core;
        Assert.That(run.VehicleModel.ReceiveBonuses(new[]{hull,core}),Is.True);var model=run.VehicleModel;
        string hullId=model.Inventory.Single(p=>p.UpgradeId==hull.Id).Id;
        foreach(var bonus in model.Inventory.Where(p=>p.UpgradeId==hull.Id||p.UpgradeId==core.Id).ToArray())
        {
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(bonus.Id,c,default)==null);
            Assert.That(run.GarageMount(bonus.Id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var vehicle=run.Player;Assert.That(vehicle.Core.MaxHp,Is.EqualTo(float.MaxValue),"Core and hull products must not become infinity and lose the bonus.");
        Assert.That(float.IsNaN(vehicle.HealthFraction)||float.IsInfinity(vehicle.HealthFraction),Is.False);
        Assert.That(vehicle.HealthFraction,Is.EqualTo(1).Within(.001));
        var hullPart=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==hullId);
        hullPart.gameObject.SetActive(false);yield return null;
        Assert.That(vehicle.Core.MaxHp,Is.LessThan(10000));Assert.That(vehicle.HealthFraction,Is.EqualTo(1).Within(.001));
        Object.Destroy(hull);Object.Destroy(core);
    }
    [UnityTest] public IEnumerator BonusAboveOneHundredPercentAffectsARealShotAndStopsOnLoss()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var reward=BacklogBonusTierTests.OperationBonus("large-physical-bonus",1.5f,"Increased");
        Assert.That(run.VehicleModel.ReceiveBonuses(new[]{reward}),Is.True);
        var model=run.VehicleModel;var bonus=model.Inventory.Single(p=>p.UpgradeId==reward.Id);
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(bonus.Id,c,default)==null);
        Assert.That(run.GarageMount(bonus.Id,cell),Is.True);Assert.That(run.EnterTestRange(),Is.True);
        while(run.Transitioning)yield return null;
        var tank=run.Player.Controller;Assert.That(tank.PackageDamageScale,Is.EqualTo(2.5f).Within(.001));
        var muzzle=new GameObject("Large bonus muzzle").transform;muzzle.position=new Vector3(1200,1200,1200);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=muzzle.position+Vector3.forward*4;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);Physics.SyncTransforms();
        tank.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Basic,"large-bonus");float end=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<end&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(900).Within(.05f));
        var mounted=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==bonus.Id);mounted.gameObject.SetActive(false);yield return null;
        Assert.That(tank.PackageDamageScale,Is.EqualTo(1).Within(.001));hp.RestoreHealthRatio(1);Physics.SyncTransforms();
        tank.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Basic,"large-bonus");end=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<end&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(960).Within(.05f));
        Object.Destroy(muzzle.gameObject);Object.Destroy(target);Object.Destroy(reward);
    }
    [UnityTest] public IEnumerator ZeroCooldownDoesNotAllowSecondShotAtTheSameClockValue()
    {
        R1EncounterManager.ClearTransientObjects();var owner=new GameObject("Zero cooldown owner");
        owner.transform.position=new Vector3(1300,1300,1300);owner.AddComponent<Rigidbody>().isKinematic=true;
        GameObject Weapon(out Transform muzzle)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.transform.SetParent(owner.transform,false);part.GetComponent<Collider>().enabled=false;
            part.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Configure(100,false);
            muzzle=new GameObject("muzzle").transform;muzzle.SetParent(part.transform,false);muzzle.localPosition=Vector3.forward;return part;
        }
        var sg=Weapon(out var sm).AddComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
        sg.Bind(new R3PartState(default,R3PartKind.Gun){Weapon=RoboRougelike.R4.R4WeaponKind.Shotgun},owner.transform,sm,1,0);
        Assert.That(sg.Fire(),Is.True);Assert.That(sg.Module.CurrentSpec.CooldownSeconds,Is.Zero);
        int shotgunAmmo=sg.State.Ammo;Assert.That(sg.Fire(),Is.False);Assert.That(sg.State.Ammo,Is.EqualTo(shotgunAmmo));
        var rg=Weapon(out var rm).AddComponent<RoboRougelike.WeaponIntegration.RRLRailgunBinding>();
        rg.Bind(new R3PartState(default,R3PartKind.Gun){Weapon=RoboRougelike.R4.R4WeaponKind.Railgun},owner.transform,rm,1,0);
        Assert.That(rg.Fire(),Is.True);Assert.That(rg.Module.CurrentSpec.Cooldown,Is.Zero);
        int railAmmo=rg.State.Ammo;Assert.That(rg.Fire(),Is.False);Assert.That(rg.State.Ammo,Is.EqualTo(railAmmo));
        var rocket=Weapon(out var rocketMuzzle).AddComponent<RoboRougelike.WeaponIntegration.RRLRocketBinding>();
        rocket.Bind(new R3PartState(default,R3PartKind.Gun){Weapon=RoboRougelike.R4.R4WeaponKind.RocketLauncher},owner.transform,rocketMuzzle,1,0);
        Assert.That(rocket.Fire(),Is.True);Assert.That(rocket.Module.CurrentSpec.Cooldown,Is.Zero);
        int rocketAmmo=rocket.State.Ammo;Assert.That(rocket.Fire(),Is.False);Assert.That(rocket.State.Ammo,Is.EqualTo(rocketAmmo));
        R1EncounterManager.ClearTransientObjects();Object.Destroy(owner);yield return null;
    }
    [UnityTest] public IEnumerator FastCooldownReachesActualShotgunAndRailgunBindings()
    {
        R1EncounterManager.ClearTransientObjects();
        var owner=new GameObject("Fast weapon owner");owner.transform.position=new Vector3(1400,1400,1400);
        owner.AddComponent<Rigidbody>().isKinematic=true;
        GameObject Weapon(out Transform muzzle)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.transform.SetParent(owner.transform,false);
            part.GetComponent<Collider>().enabled=false;part.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Configure(100,false);
            muzzle=new GameObject("muzzle").transform;muzzle.SetParent(part.transform,false);muzzle.localPosition=Vector3.forward;return part;
        }
        var sg=Weapon(out var sm).AddComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
        sg.Bind(new R3PartState(default,R3PartKind.Gun){Weapon=RoboRougelike.R4.R4WeaponKind.Shotgun},owner.transform,sm,1,.001f);
        Assert.That(sg.Fire(),Is.True);Assert.That(sg.Module.CurrentSpec.CooldownSeconds,Is.EqualTo(.00085f).Within(.0000001f));
        var rg=Weapon(out var rm).AddComponent<RoboRougelike.WeaponIntegration.RRLRailgunBinding>();
        rg.Bind(new R3PartState(default,R3PartKind.Gun){Weapon=RoboRougelike.R4.R4WeaponKind.Railgun},owner.transform,rm,1,.001f);
        Assert.That(rg.Fire(),Is.True);Assert.That(rg.Module.CurrentSpec.Cooldown,Is.EqualTo(.0024f).Within(.0000001f));
        R1EncounterManager.ClearTransientObjects();Object.Destroy(owner);yield return null;
    }
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
    [UnityTest] public IEnumerator FieldRepairCountsLongFrameButNotDisabledTime()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var reward=ScriptableObject.CreateInstance<R1UpgradeDefinition>();reward.Id="field-repair-clock-qa";reward.Effect=R1UpgradeEffect.Repair;
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);string id=model.Inventory.Single().Id;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(id,c,default)==null);
        Assert.That(run.GarageMount(id,cell),Is.True);Assert.That(run.EnterTestRange(),Is.True);
        while(run.Transitioning)yield return null;yield return null;
        var effects=run.Player.GetComponent<R3MountedBonusEffects>();
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor&&p.Operational);
        part.Damageable.RestoreHealthRatio(.3f);
        var clock=typeof(R3MountedBonusEffects).GetField("lastRepairPulse",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var apply=typeof(R3MountedBonusEffects).GetMethod("ApplyRepairPulse",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        clock.SetValue(effects,Time.time-2f);apply.Invoke(effects,null);
        Assert.That(part.Damageable.CurrentHp/part.Damageable.MaxHp,Is.EqualTo(.35f).Within(.001),"Two active seconds must provide two seconds of repair.");
        effects.enabled=false;yield return new WaitForSeconds(.8f);
        float before=part.Damageable.CurrentHp;effects.enabled=true;apply.Invoke(effects,null);
        Assert.That(part.Damageable.CurrentHp,Is.EqualTo(before).Within(.001),"Disabled time must not be converted to healing.");
        Object.Destroy(reward);
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
    [UnityTest] public IEnumerator WeaponScopedFireRateStacksAndSurvivesCheckpoint()
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var global=BacklogBonusTierTests.OperationBonus("rate-global",.1f,"Increased",R1UpgradeEffect.Reload);
        var weapons=BacklogBonusTierTests.OperationBonus("rate-weapons",.1f,"Increased",R1UpgradeEffect.Reload);weapons.Scope=R1BonusScope.Weapons;
        var automatic=BacklogBonusTierTests.OperationBonus("rate-automatic",.2f,"Increased",R1UpgradeEffect.Reload);automatic.Scope=R1BonusScope.AutomaticWeapon;
        var more=BacklogBonusTierTests.OperationBonus("rate-more",.2f,"More",R1UpgradeEffect.Reload);
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{global,weapons,automatic,more}),Is.True);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct().First(c=>model.OwnedPlacementError(id,c,default)==null);
            Assert.That(run.GarageMount(id,cell),Is.True);
        }
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.UpgradeId=="rate-automatic").UpgradeScope,Is.EqualTo(R1BonusScope.AutomaticWeapon));
        var rebuilt=R3VehicleAssembly.Build(restored,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(1000,50,1000),Quaternion.identity);
        Assert.That(rebuilt.Controller.WeaponInterval(RoboRougelike.R4.R4WeaponKind.Automatic)/RoboRougelike.R4.R4WeaponDefinition.Get(RoboRougelike.R4.R4WeaponKind.Automatic).Interval,Is.EqualTo(.48f).Within(.001),"Restored mounted modules must apply their scoped effect.");
        Object.Destroy(rebuilt.gameObject);yield return null;
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var tank=run.Player.Controller;
        float Scale(RoboRougelike.R4.R4WeaponKind kind)=>tank.WeaponInterval(kind)/RoboRougelike.R4.R4WeaponDefinition.Get(kind).Interval;
        Assert.That(Scale(RoboRougelike.R4.R4WeaponKind.Automatic),Is.EqualTo(.48f).Within(.001));
        Assert.That(Scale(RoboRougelike.R4.R4WeaponKind.Basic),Is.EqualTo(.64f).Within(.001));
        Assert.That(Scale(RoboRougelike.R4.R4WeaponKind.Heavy),Is.EqualTo(.64f).Within(.001));
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==model.Parts.Single(s=>s.UpgradeId=="rate-automatic").Id);
        part.gameObject.SetActive(false);yield return null;
        Assert.That(Scale(RoboRougelike.R4.R4WeaponKind.Automatic),Is.EqualTo(.64f).Within(.001));
        Object.Destroy(global);Object.Destroy(weapons);Object.Destroy(automatic);Object.Destroy(more);
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
    [UnityTest] public IEnumerator ProjectileScopesCombineGlobalWeaponTypeAndMoreWithoutCrossBuffing()
    {
        Assert.That((int)R1BonusScope.EnergyDamage,Is.EqualTo(15),"Existing saved enum values must stay unchanged.");
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var specs=new[]{("global",.2f,R1BonusScope.Global), ("weapons",.1f,R1BonusScope.Weapons),
            ("kinetic",.15f,R1BonusScope.KineticDamage), ("heavy",.3f,R1BonusScope.HeavyWeapon),
            ("shotgun",.25f,R1BonusScope.ShotgunWeapon)};
        var bonuses=specs.Select(s=>{var b=BacklogBonusTierTests.OperationBonus("scope-combo-"+s.Item1,s.Item2,"Increased");b.Scope=s.Item3;return b;}).ToList();
        bonuses.Add(BacklogBonusTierTests.OperationBonus("scope-combo-more",.5f,"More"));
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(bonuses.ToArray()),Is.True);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var options=cells.Select(c=>new{Cell=c,Error=model.OwnedPlacementError(id,c,default)}).ToArray();
            Assert.That(options.Any(o=>o.Error==null),Is.True,"No mount for "+id+": "+string.Join("; ",options.GroupBy(o=>o.Error).Select(g=>$"{g.Key}: {g.Count()}")));
            var cell=options.First(o=>o.Error==null).Cell;Assert.That(run.GarageMount(id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var effects=run.Player.GetComponent<R3VehicleAssembly>().GetComponent<R3MountedBonusEffects>();
        Assert.That(effects.HeavyDamageMultiplier,Is.EqualTo(2.625f).Within(.001));
        Assert.That(effects.ShotgunDamageMultiplier,Is.EqualTo(2.55f).Within(.001));
        Assert.That(effects.RailgunDamageMultiplier,Is.EqualTo(1.95f).Within(.001));
        Assert.That(effects.RocketDamageMultiplier,Is.EqualTo(1.95f).Within(.001));
        Assert.That(effects.AutomaticDamageMultiplier,Is.EqualTo(2.175f).Within(.001));
        effects.SetOverdriveIncrease(.1f);
        Assert.That(effects.HeavyDamageMultiplier,Is.EqualTo(2.775f).Within(.001));
        Assert.That(effects.RocketDamageMultiplier,Is.EqualTo(2.10f).Within(.001));
        foreach(var bonus in bonuses)Object.Destroy(bonus);
    }
    [UnityTest] public IEnumerator ContinuousScopesCombineEnergyWeaponAndMoreWithoutCrossBuffing()
    {
        Assert.That((int)R1BonusScope.RocketWeapon,Is.EqualTo(19),"Existing saved enum values must stay unchanged.");
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var specs=new[]{("global",.2f,R1BonusScope.Global), ("weapons",.1f,R1BonusScope.Weapons),
            ("energy",.15f,R1BonusScope.EnergyDamage), ("beam",.3f,R1BonusScope.BeamWeapon),
            ("aura",.25f,R1BonusScope.AuraWeapon)};
        var bonuses=specs.Select(s=>{var b=BacklogBonusTierTests.OperationBonus("continuous-combo-"+s.Item1,s.Item2,"Increased");b.Scope=s.Item3;return b;}).ToList();
        bonuses.Add(BacklogBonusTierTests.OperationBonus("continuous-combo-more",.5f,"More"));
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(bonuses.ToArray()),Is.True);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>model.OwnedPlacementError(id,c,default)==null);Assert.That(run.GarageMount(id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var effects=run.Player.GetComponent<R3MountedBonusEffects>();
        Assert.That(effects.BeamDamageMultiplier,Is.EqualTo(2.625f).Within(.001));
        Assert.That(effects.AuraDamageMultiplier,Is.EqualTo(2.55f).Within(.001));
        Assert.That(effects.SawDamageMultiplier,Is.EqualTo(1.95f).Within(.001));
        Assert.That(effects.AutomaticDamageMultiplier,Is.EqualTo(1.95f).Within(.001));
        effects.SetOverdriveIncrease(.1f);
        Assert.That(effects.BeamDamageMultiplier,Is.EqualTo(2.775f).Within(.001));
        Assert.That(effects.SawDamageMultiplier,Is.EqualTo(2.10f).Within(.001));
        foreach(var bonus in bonuses)Object.Destroy(bonus);
    }
    [UnityTest] public IEnumerator AutomaticScopedBonusAffectsOnlyAutomaticShotsAndCombinesWithGlobalBonuses()
    {
        Assert.That(System.Enum.TryParse("AutomaticWeapon",out R1BonusScope automaticScope),Is.True);
        var reward=Resources.Load<R1RunDefinition>("R1/Run").Upgrades.Single(p=>p!=null&&p.Id=="damage_automatic_weapon");
        Assert.That(reward.Scope,Is.EqualTo(automaticScope));Assert.That(reward.Amount,Is.EqualTo(.3f));
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var global=BacklogBonusTierTests.OperationBonus("auto-global",.2f,"Increased");
        var more=BacklogBonusTierTests.OperationBonus("auto-more",.5f,"More");
        var automatic=BacklogBonusTierTests.OperationBonus("auto-only",.3f,"Increased");automatic.Scope=automaticScope;
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{global,more,automatic}),Is.True);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>model.OwnedPlacementError(id,c,default)==null);Assert.That(run.GarageMount(id,cell),Is.True);
        }
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Any(p=>p.UpgradeScope==automaticScope),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var muzzle=new GameObject("Automatic scoped muzzle").transform;muzzle.position=new Vector3(1000,1000,1000);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=muzzle.position+Vector3.forward*4;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);Physics.SyncTransforms();
        run.Player.Controller.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Automatic,"scope-auto");
        float deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(1000-12*2.25f).Within(.02));
        hp.RestoreHealthRatio(1);run.Player.Controller.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Basic,"scope-basic");
        deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(1000-40*1.8f).Within(.02));
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeScope==automaticScope);
        part.gameObject.SetActive(false);yield return null;hp.RestoreHealthRatio(1);
        run.Player.Controller.FireFrom(muzzle,RoboRougelike.R4.R4WeaponKind.Automatic,"scope-auto-removed");
        deadline=Time.time+3;while(Time.time<deadline&&hp.CurrentHp>=1000)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(1000-12*1.8f).Within(.02));
        Object.Destroy(muzzle.gameObject);Object.Destroy(target);Object.Destroy(global);Object.Destroy(more);Object.Destroy(automatic);
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
