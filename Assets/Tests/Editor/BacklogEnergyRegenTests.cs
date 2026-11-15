using System;
using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R1;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogEnergyRegenTests
{
    [Test] public void SmallCostsAccumulateEvenWhenCurrentDisplayCannotRepresentEachDeduction()
    {
        var state=new VehicleEnergyState();state.Fill();
        for(int i=0;i<10;i++)Assert.That(state.TrySpend(.00001f),Is.True);
        Assert.That(state.TrySpend(500),Is.False,"Small real costs cannot disappear because the displayed balance rounds to 500.");
        Assert.That(state.CanSpend(state.Current),Is.True,"The reported available energy must itself be spendable.");
        Assert.That(state.TrySpend(499.9998f),Is.True);
    }
    [Test] public void SmallCostsAndRechargeRemainExactAcrossBulkTransactions()
    {
        var state=new VehicleEnergyState();state.Fill();
        var method=typeof(VehicleEnergyState).GetMethod("SpendFair",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var requested=new[]{new System.Numerics.BigInteger(10)};
        var paid=(System.Numerics.BigInteger[])method.Invoke(state,new object[]{new[]{.00001f},requested});
        Assert.That(paid,Is.EqualTo(requested));Assert.That(state.CanSpend(500),Is.False);
        for(int i=0;i<10;i++)state.Recharge(.00001f);
        Assert.That(state.CanSpend(500),Is.True);Assert.That(state.TrySpend(500),Is.True);
    }
    [Test] public void CopyOfVehicleRetainsCostsSmallerThanDisplayPrecision()
    {
        var design=new R3VehicleDesign();design.Parts.Add(new R3PartState(default,R3PartKind.Core));
        var model=new R3VehicleModel(design);model.RefreshEnergyLayout();model.Energy.Fill();
        Assert.That(model.Energy.TrySpend(.00001f),Is.True);
        var copy=model.DeepCopy();Assert.That(copy.Energy.CanSpend(500),Is.False,"Cloning a vehicle must not restore energy spent below display precision.");
    }
    [Test] public void SharedShotBudgetKeepsAffordableUnequalCostsAndHugeRequests()
    {
        var state=new VehicleEnergyState();
        var method=typeof(VehicleEnergyState).GetMethod("SpendFair",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var huge=System.Numerics.BigInteger.One<<149;
        System.Numerics.BigInteger[] Spend(float[] costs,System.Numerics.BigInteger[] requests)=>(System.Numerics.BigInteger[])method.Invoke(state,new object[]{costs,requests});
        state.Restore(7);var paid=Spend(new[]{4f,1f},new[]{huge,huge});
        Assert.That(paid,Is.EqualTo(new[]{new System.Numerics.BigInteger(1),new System.Numerics.BigInteger(3)}));
        Assert.That(state.Current,Is.Zero);
        state.Restore(5);paid=Spend(new[]{0f,2.5f},new[]{huge,huge});
        Assert.That(paid,Is.EqualTo(new[]{huge,new System.Numerics.BigInteger(2)}));Assert.That(state.Current,Is.Zero);
    }
    [Test] public void BulkShotCostHandlesExactEnergyAndExtremelyLargeCounts()
    {
        var state=new VehicleEnergyState();
        var method=typeof(VehicleEnergyState).GetMethod("SpendUpTo",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        System.Numerics.BigInteger Spend(float cost,System.Numerics.BigInteger count)=>(System.Numerics.BigInteger)method.Invoke(state,new object[]{cost,count});
        state.Restore(8);Assert.That(Spend(2.5f,10),Is.EqualTo(new System.Numerics.BigInteger(3)));
        Assert.That(state.Current,Is.EqualTo(.5f));Assert.That(Spend(2.5f,10),Is.EqualTo(System.Numerics.BigInteger.Zero));
        Assert.That(Spend(0,10),Is.EqualTo(new System.Numerics.BigInteger(10)));Assert.That(state.Current,Is.EqualTo(.5f));
        state.SetResourceMultipliers(float.MaxValue,1);state.Refresh(0,0,true);state.Fill();
        var available=((System.Numerics.BigInteger.One<<24)-1)<<253;
        Assert.That(Spend(float.Epsilon,available+10),Is.EqualTo(available));Assert.That(state.Current,Is.Zero);
    }
    [Test] public void OpposingLargeAdditiveBonusesDoNotEraseSmallerBonus()
    {
        var design=new R3VehicleDesign();design.Parts.Add(new R3PartState(default,R3PartKind.Core));
        var model=new R3VehicleModel(design);
        var huge=new R3PartState(default,R3PartKind.BonusModule){UpgradeEffect=R1UpgradeEffect.EnergyCapacity,UpgradeAmount=float.MaxValue};
        var small=new R3PartState(default,R3PartKind.BonusModule){UpgradeEffect=R1UpgradeEffect.EnergyCapacity,UpgradeAmount=.5f};
        var reduction=new R3PartState(default,R3PartKind.BonusModule){UpgradeEffect=R1UpgradeEffect.EnergyCapacity,UpgradeOperation=R1ModifierOperation.Reduced,UpgradeAmount=float.MaxValue};
        model.Parts.Add(huge);model.Parts.Add(small);model.Parts.Add(reduction);
        const float expected=500*1.5f-3*25;
        model.RefreshEnergyLayout();Assert.That(model.Energy.Maximum,Is.EqualTo(expected).Within(.001),"Small bonus between opposing large values");
        model.Parts.Remove(small);model.Parts.Add(small);model.RefreshEnergyLayout();
        Assert.That(model.Energy.Maximum,Is.EqualTo(expected).Within(.001),"Small bonus after opposing large values");
    }
    [Test] public void OpposingLargeMultiplierGroupsRemainOrderIndependent()
    {
        var design=new R3VehicleDesign();design.Parts.Add(new R3PartState(default,R3PartKind.Core));
        var model=new R3VehicleModel(design);
        for(int i=0;i<160;i++)model.Parts.Add(new R3PartState(default,R3PartKind.Battery));
        for(int i=0;i<300;i++)model.Parts.Add(new R3PartState(default,R3PartKind.BonusModule)
            {Tier=3,UpgradeEffect=R1UpgradeEffect.EnergyCapacity,UpgradeOperation=R1ModifierOperation.More,UpgradeAmount=1});
        for(int i=0;i<300;i++)model.Parts.Add(new R3PartState(default,R3PartKind.BonusModule)
            {Tier=3,UpgradeEffect=R1UpgradeEffect.EnergyCapacity,UpgradeOperation=R1ModifierOperation.Less,UpgradeAmount=.5f});
        // Each T3 contributes four factors: 2^1200 * 0.5^1200 == 1.
        const float expected=500+160*100-600*25;
        model.RefreshEnergyLayout();Assert.That(model.Energy.Maximum,Is.EqualTo(expected).Within(.01),"Increasing factors first");
        model.Parts.Reverse();model.RefreshEnergyLayout();
        Assert.That(model.Energy.Maximum,Is.EqualTo(expected).Within(.01),"Decreasing factors first");
    }
    [Test] public void BatteryCapacityScopeStacksWithGlobalWithoutBuffingCore()
    {
        Assert.That(Enum.TryParse("BatteryCapacity",out R1BonusScope scope),Is.True);
        Assert.That(R3VehicleModel.ValidBonusScope(R1UpgradeEffect.EnergyCapacity,scope),Is.True);
        Assert.That(R3VehicleModel.ValidBonusScope(R1UpgradeEffect.EnergyRegeneration,scope),Is.False);
        var design=new R3VehicleDesign();design.Parts.Add(new R3PartState(default,R3PartKind.Core));
        var model=new R3VehicleModel(design);
        var battery=new R3PartState(default,R3PartKind.Battery){Tier=2};model.Parts.Add(battery);
        var global=new R3PartState(default,R3PartKind.BonusModule){UpgradeEffect=R1UpgradeEffect.EnergyCapacity,UpgradeAmount=.2f};
        var specific=new R3PartState(default,R3PartKind.BonusModule){UpgradeEffect=R1UpgradeEffect.EnergyCapacity,UpgradeScope=scope,UpgradeAmount=.3f};
        var more=new R3PartState(default,R3PartKind.BonusModule){UpgradeEffect=R1UpgradeEffect.EnergyCapacity,UpgradeOperation=R1ModifierOperation.More,UpgradeAmount=.5f};
        model.Parts.Add(global);model.Parts.Add(specific);model.Parts.Add(more);model.RefreshEnergyLayout();
        Assert.That(model.Energy.Maximum,Is.EqualTo(500*1.2f*1.5f+200*1.5f*1.5f-75).Within(.001));
        specific.UpgradeOperation=R1ModifierOperation.More;model.RefreshEnergyLayout();
        Assert.That(model.Energy.Maximum,Is.EqualTo(500*1.2f*1.5f+200*1.2f*1.5f*1.3f-75).Within(.001));
        battery.Health=0;model.RefreshEnergyLayout();
        Assert.That(model.Energy.Maximum,Is.EqualTo(500*1.2f*1.5f-75).Within(.001),"Battery-only bonus cannot increase the core.");
    }
    [Test] public void PassiveModulesReserveEnergyWhileBatteriesProvideFullCapacity()
    {
        foreach(R3PartKind kind in Enum.GetValues(typeof(R3PartKind)))
        {
            if(!R3VehicleModel.IsPassiveSlotPart(kind)||kind==R3PartKind.Battery)continue;
            Assert.That(R3VehicleModel.IsPassiveEnergyPart(kind),Is.True,kind+" is passive but does not reserve energy.");
        }
        Assert.That(R3VehicleModel.IsPassiveEnergyPart(R3PartKind.Battery),Is.False,"A T1 battery must provide its full +100 maximum energy.");
    }
    [Test] public void ResourceBonusesRespectReservationsAndOnlyAffectNaturalRegeneration()
    {
        Assert.That(Enum.TryParse("EnergyCapacity",out R1UpgradeEffect capacity),Is.True);
        Assert.That(Enum.TryParse("EnergyRegeneration",out R1UpgradeEffect regeneration),Is.True);
        var design=new R3VehicleDesign();design.Parts.Add(new R3PartState(default,R3PartKind.Core));
        var model=new R3VehicleModel(design);
        model.Parts.Add(new R3PartState(default,R3PartKind.BonusModule){UpgradeEffect=capacity,UpgradeAmount=.2f});
        var regen=new R3PartState(default,R3PartKind.BonusModule){UpgradeEffect=regeneration,UpgradeAmount=.5f};
        model.Parts.Add(regen);model.RefreshEnergyLayout();
        Assert.That(model.Energy.Maximum,Is.EqualTo(550).Within(.001),"The two passives each reserve 25 after the capacity bonus.");
        model.Energy.Fill();Assert.That(model.Energy.TrySpend(model.Energy.Current),Is.True);
        model.Energy.Advance(1);Assert.That(model.Energy.Current,Is.EqualTo(15).Within(.001));
        model.Energy.AdvanceWithMotion(1,true);Assert.That(model.Energy.Current,Is.EqualTo(75).Within(.001));
        model.Energy.ApplyEnergyPickup();model.Energy.Advance(1);
        Assert.That(model.Energy.Current,Is.EqualTo(145).Within(.001),"The pickup retains 10% maximum per second.");
        regen.Health=0;model.RefreshEnergyLayout();Assert.That(model.Energy.Maximum,Is.EqualTo(575).Within(.001));
        model.Energy.Advance(1);Assert.That(model.Energy.Current,Is.EqualTo(212.5).Within(.001));
    }
    [Test] public void ResourceBonusesAreAvailableInRewardCatalog()
    {
        var run=Resources.Load<R1RunDefinition>("R1/Run");
        Assert.That(run.Upgrades.Any(u=>u!=null&&u.Id=="energy_capacity"),Is.True);
        Assert.That(run.Upgrades.Any(u=>u!=null&&u.Id=="energy_regeneration"),Is.True);
    }
    [Test] public void BatteryBonusRewardKeepsItsScopeInCheckpointBlueprintAndDescription()
    {
        Assert.That(Enum.TryParse("BatteryCapacity",out R1BonusScope scope),Is.True);
        var reward=Resources.Load<R1RunDefinition>("R1/Run").Upgrades.Single(u=>u!=null&&u.Id=="battery_capacity");
        Assert.That(reward.Scope,Is.EqualTo(scope));Assert.That(reward.Effect,Is.EqualTo(R1UpgradeEffect.EnergyCapacity));
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);var part=model.Inventory.Single();
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Inventory.Single().UpgradeScope,Is.EqualTo(scope));
        Assert.That(R3BlueprintPart.FromPart(part,Vector3Int.zero).Matches(restored.Inventory.Single()),Is.True);
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(part.Id,c,default)==null);
        Assert.That(model.Mount(part.Id,cell),Is.True);
        checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var mounted,out error),Is.True,error);
        Assert.That(R3BlueprintPart.FromPart(part,Vector3Int.zero).Matches(mounted.Parts.Single(p=>p.Id==part.Id)),Is.True);
        foreach(var name in new[]{"BonusText","BonusInstanceText"})
        {
            var description=typeof(R1RunHUD).GetMethod(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            Assert.That((string)description.Invoke(null,new object[]{part}),Does.Contain("baterii"));
        }
    }
    [Test] public void CapacityBonusGroupsAndStoredEnergySurviveCheckpoint()
    {
        Assert.That(Enum.TryParse("EnergyCapacity",out R1UpgradeEffect capacity),Is.True);
        var increased=BacklogBonusTierTests.OperationBonus("capacity-increased",.2f,"Increased",capacity);
        var more=BacklogBonusTierTests.OperationBonus("capacity-more",.5f,"More",capacity);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        float oldMaximum=model.Energy.Maximum;
        Assert.That(model.ReceiveBonuses(new[]{increased,more}),Is.True);
        foreach(var id in model.Inventory.Select(p=>p.Id).ToArray())
        {
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(id,c,default)==null);
            Assert.That(model.Mount(id,cell),Is.True);
        }
        model.RefreshEnergyLayout();
        var oldReservation=model.Parts.Count(p=>R3VehicleModel.IsPassiveEnergyPart(p.Kind))-2;
        var gross=oldMaximum+oldReservation*25;
        Assert.That(model.Energy.Maximum,Is.EqualTo(gross*1.2f*1.5f-(oldReservation+2)*25).Within(.001));
        model.PrepareInitialEnergy();Assert.That(model.Energy.Current,Is.EqualTo(model.Energy.Maximum));
        Assert.That(model.Energy.TrySpend(model.Energy.Current-123),Is.True);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Energy.Maximum,Is.EqualTo(model.Energy.Maximum).Within(.001));
        Assert.That(restored.Energy.Current,Is.EqualTo(123).Within(.001));
        var bonus=restored.Parts.Single(p=>p.UpgradeId=="capacity-more");bonus.Health=0;restored.RefreshEnergyLayout();
        Assert.That(restored.Energy.Current,Is.EqualTo(123).Within(.001));
        bonus.Health=1;restored.RefreshEnergyLayout();
        Assert.That(restored.Energy.Current,Is.EqualTo(123).Within(.001),"Restoring capacity must not create energy.");
        UnityEngine.Object.DestroyImmediate(increased);UnityEngine.Object.DestroyImmediate(more);
    }
    [TestCase(1)][TestCase(2)][TestCase(3)]
    public void EveryPassiveTierReservesTwentyFiveAndDestructionReleasesIt(int tier)
    {
        foreach(var kind in new[]{R3PartKind.Suspension,R3PartKind.Gyroscope,R3PartKind.BonusModule,R3PartKind.DirectionalShield,R3PartKind.IndependentSteering,R3PartKind.DrivePower,R3PartKind.EnergyRegenerator})
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));float before=model.Energy.Maximum;
            var module=new R3PartState(default,kind){Tier=tier};model.Parts.Add(module);model.RefreshEnergyLayout();
            Assert.That(model.Energy.Maximum,Is.EqualTo(before-25),$"{kind} T{tier}");
            module.Health=0;model.RefreshEnergyLayout();Assert.That(model.Energy.Maximum,Is.EqualTo(before),$"Destroyed {kind} T{tier}");
        }
    }
    [Test] public void MixedPassiveTiersEachReserveTwentyFive()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));float before=model.Energy.Maximum;
        model.Parts.Add(new R3PartState(default,R3PartKind.DrivePower){Tier=1});
        model.Parts.Add(new R3PartState(default,R3PartKind.EnergyRegenerator){Tier=2});
        model.Parts.Add(new R3PartState(default,R3PartKind.Gyroscope){Tier=3});model.RefreshEnergyLayout();
        Assert.That(model.Energy.Maximum,Is.EqualTo(before-75));
    }
    [Test] public void CheckpointPreservesTieredBatteryPassiveReservationAndCurrentCharge()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(500);
        float baseline=model.Energy.Maximum;
        var existingIds=model.Parts.Select(p=>p.Id).ToHashSet();
        foreach(var kind in new[]{R3PartKind.Battery,R3PartKind.DrivePower})
        {
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.PlacementError(c,kind,false)==null);
            Assert.That(model.Buy(cell,kind),Is.True);
        }
        var battery=model.Parts.Single(p=>p.Kind==R3PartKind.Battery&&!existingIds.Contains(p.Id));battery.Tier=2;
        var motor=model.Parts.Single(p=>p.Kind==R3PartKind.DrivePower&&!existingIds.Contains(p.Id));motor.Tier=3;
        model.PrepareInitialEnergy();Assert.That(model.Energy.Maximum,Is.EqualTo(baseline+175));
        Assert.That(model.Energy.TrySpend(model.Energy.Current-123),Is.True);
        var checkpoint=model.MakeCheckpoint(10);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,10,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==battery.Id).Tier,Is.EqualTo(2));
        Assert.That(restored.Parts.Single(p=>p.Id==motor.Id).Tier,Is.EqualTo(3));
        Assert.That(restored.Energy.Maximum,Is.EqualTo(baseline+175));Assert.That(restored.Energy.Current,Is.EqualTo(123));
        restored.PrepareInitialEnergy();Assert.That(restored.Energy.Current,Is.EqualTo(123),"Loading a started run must not refill the battery.");
    }
    [Test]
    public void MountedRegeneratorReservesEnergyAndIncreasesRecovery()
    {
        Assert.That(Enum.TryParse("EnergyRegenerator",out R3PartKind kind),Is.True);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Award(500);float maximum=model.Energy.Maximum;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,kind,false)==null);
        Assert.That(model.Buy(cell,kind),Is.True);model.RefreshEnergyLayout();
        Assert.That(model.Energy.Maximum,Is.EqualTo(maximum-25));
        Assert.That(model.Energy.TrySpend(model.Energy.Current),Is.True);
        model.Energy.AdvanceWithMotion(1,false);
        Assert.That(model.Energy.Current,Is.EqualTo(15).Within(.001));
        model.Energy.AdvanceWithMotion(1,true);
        Assert.That(model.Energy.Current,Is.EqualTo(75).Within(.001));
    }
    [Test]
    public void MultipleDevicesStackAndDestroyedDevicesStopContributing()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(500);
        float baseline=model.Energy.Maximum;
        for(int i=0;i<2;i++)
        {
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.PlacementError(c,R3PartKind.EnergyRegenerator,false)==null);
            Assert.That(model.Buy(cell,R3PartKind.EnergyRegenerator),Is.True);
        }
        model.RefreshEnergyLayout();Assert.That(model.Energy.Maximum,Is.EqualTo(baseline-50));
        Assert.That(model.Energy.TrySpend(model.Energy.Current),Is.True);model.Energy.Advance(1);
        Assert.That(model.Energy.Current,Is.EqualTo(20).Within(.001));
        model.Parts.First(p=>p.Kind==R3PartKind.EnergyRegenerator).Health=0;model.RefreshEnergyLayout();
        Assert.That(model.Energy.Maximum,Is.EqualTo(baseline-25));model.Energy.Advance(1);
        Assert.That(model.Energy.Current,Is.EqualTo(35).Within(.001));
    }
}

public sealed class BacklogEnergyRegenIntegrationTests
{
    [UnitySetUp] public IEnumerator Setup()
    {EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();}

    [UnityTest] public IEnumerator BatteryScopeMatchesTheLiveVehicleAndStopsOnLoss()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(500);
        var batteryCell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.Battery,false)==null);
        Assert.That(model.Buy(batteryCell,R3PartKind.Battery),Is.True);model.RefreshEnergyLayout();float baseline=model.Energy.Maximum;
        int batteries=model.Parts.Where(p=>p.Kind==R3PartKind.Battery&&p.Health>0).Sum(p=>R3VehicleModel.TierEffectUnits(p.Tier));
        var reward=Resources.Load<R1RunDefinition>("R1/Run").Upgrades.Single(u=>u!=null&&u.Id=="battery_capacity");
        Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);string id=model.Inventory.Single().Id;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(id,c,default)==null);
        Assert.That(model.Mount(id,cell),Is.True);model.RefreshEnergyLayout();
        float expected=baseline+batteries*100*.3f-25;
        Assert.That(model.Energy.Maximum,Is.EqualTo(expected).Within(.001));
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        var bank=vehicle.GetComponent<VehicleEnergyBank>();bank.Refresh();Assert.That(bank.Maximum,Is.EqualTo(expected).Within(.001));
        var bonus=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        bonus.gameObject.SetActive(false);bank.Refresh();Assert.That(bank.Maximum,Is.EqualTo(baseline).Within(.001));
        bonus.gameObject.SetActive(true);bank.Refresh();Assert.That(bank.Maximum,Is.EqualTo(expected).Within(.001));
        UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);
        yield return null;
    }

    [UnityTest] public IEnumerator LiveResourceBonusDisappearsWithItsPhysicalModule()
    {
        Assert.That(Enum.TryParse("EnergyRegeneration",out R1UpgradeEffect effect),Is.True);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var reward=ScriptableObject.CreateInstance<R1UpgradeDefinition>();reward.Id="qa-energy";reward.Effect=effect;reward.Amount=.5f;
        Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);
        var id=model.Inventory.Single().Id;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(id,c,default)==null);
        Assert.That(model.Mount(id,cell),Is.True);model.RefreshEnergyLayout();
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        var bank=vehicle.GetComponent<VehicleEnergyBank>();bank.Refresh();
        Assert.That(bank.TrySpend(bank.Current),Is.True);model.Energy.Advance(1);
        Assert.That(bank.Current,Is.EqualTo(15).Within(.001));
        var bonus=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.BonusModule);
        bonus.gameObject.SetActive(false);bank.Refresh();
        Assert.That(bank.TrySpend(bank.Current),Is.True);model.Energy.Advance(1);
        Assert.That(bank.Current,Is.EqualTo(10).Within(.001));
        UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);UnityEngine.Object.Destroy(reward);
        yield return null;
    }

    [UnityTest] public IEnumerator DestroyingRealRegeneratorRemovesItsRateAndReservation()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(500);
        float startingCapacity=model.Energy.Maximum;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.EnergyRegenerator,false)==null);
        Assert.That(model.Buy(cell,R3PartKind.EnergyRegenerator),Is.True);
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        var bank=vehicle.GetComponent<VehicleEnergyBank>();var assembly=vehicle.GetComponent<R3VehicleAssembly>();
        bank.Refresh();Assert.That(bank.Maximum,Is.EqualTo(startingCapacity-25));
        Assert.That(bank.TrySpend(bank.Current),Is.True);model.Energy.Advance(1);
        Assert.That(bank.Current,Is.EqualTo(15).Within(.001f));
        var device=assembly.Parts.Single(p=>p.Kind==R3PartKind.EnergyRegenerator);
        var receiver=device.GetComponent<RoboRougelike.Mechanics.EMP.EmpReceiver>();Assert.That(receiver,Is.Not.Null);
        receiver.ApplyEmp(2);bank.Refresh();Assert.That(bank.Maximum,Is.EqualTo(startingCapacity-25),"EMP must not remove passive reservation.");
        Assert.That(bank.TrySpend(bank.Current),Is.True);model.Energy.Advance(1);
        Assert.That(bank.Current,Is.EqualTo(10).Within(.001f),"EMP must pause the regenerator bonus.");
        Assert.That(bank.TrySpend(bank.Current),Is.True);receiver.Advance(2);bank.Refresh();model.Energy.Advance(1);
        Assert.That(bank.Current,Is.EqualTo(15).Within(.001f),"Regeneration bonus resumes after EMP.");
        device.Damageable.ApplyDamage(100000,device.transform.position,Vector3.up);bank.Refresh();
        Assert.That(bank.Maximum,Is.EqualTo(startingCapacity));model.Energy.Advance(1);
        Assert.That(bank.Current,Is.EqualTo(25).Within(.001f));
        UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);
        yield return null;
    }
    [UnityTest] public IEnumerator HeavyVehicleEnergyRefreshDiagnostic()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Heavy));
        var paletteObject=new GameObject("Energy diagnostic palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        try
        {
            var bank=vehicle.GetComponent<VehicleEnergyBank>();var assembly=vehicle.GetComponent<R3VehicleAssembly>();
            Assert.That(assembly.Parts.Count,Is.GreaterThan(50));
            long probeBefore=GC.GetAllocatedBytesForCurrentThread();
            long monoProbeBefore=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            var probe=new byte[1024*1024];GC.KeepAlive(probe);
            Debug.Log($"[ENERGY_ALLOC_PROBE] thread_bytes={GC.GetAllocatedBytesForCurrentThread()-probeBefore} mono_delta={UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()-monoProbeBefore}");
            for(int i=0;i<5;i++)bank.Refresh();
            GC.Collect();int collections=GC.CollectionCount(0);
            const int iterations=500;long before=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<iterations;i++)bank.Refresh();
            watch.Stop();long bytes=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()-before;
            Debug.Log($"[ENERGY_PERF] parts={assembly.Parts.Count} iterations={iterations} elapsed_ms={watch.Elapsed.TotalMilliseconds:F2} mono_delta={bytes} gc_collections={GC.CollectionCount(0)-collections}");
            Assert.That(bank.Maximum,Is.GreaterThan(0));
            Assert.That(GC.CollectionCount(0)-collections,Is.Zero,"A collection would make this allocation measurement inconclusive.");
            Assert.That(bytes,Is.LessThan(128*1024),"Repeated energy refresh must not create hundreds of temporary per-vehicle lists.");
        }
        finally{UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);}
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
