using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Defenses.DirectionalShield;

public class RRLW6ShieldModelTests
{
    [Test] public void ShieldFusionUsesEachInputsStoredCapacityAndLoadsLegacyCheckpoint()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(1000);
        for(int i=0;i<3;i++)Assert.That(model.PurchaseToInventory(R3PartKind.DirectionalShield),Is.True);
        var parts=model.Inventory.ToArray();parts[0].DefenseCapacity=200;parts[0].DefenseCharge=100;
        parts[1].DefenseCapacity=100;parts[1].DefenseCharge=25;parts[2].DefenseCharge=75;
        Assert.That(model.FuseInventory(parts.Select(p=>p.Id)),Is.True);
        var fused=model.Inventory.Single();Assert.That(fused.DefenseCapacity,Is.EqualTo(200));Assert.That(fused.DefenseCharge,Is.EqualTo(100).Within(.001));
        string json=JsonUtility.ToJson(model.MakeCheckpoint(0));
        string legacy=System.Text.RegularExpressions.Regex.Replace(json,",\"DefenseCapacity\":[^,}]+","");
        Assert.That(legacy,Does.Not.Contain("DefenseCapacity"));
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(legacy);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Inventory.Single().DefenseCapacity,Is.Zero);
        Assert.That(restored.Inventory.Single().DefenseCharge,Is.EqualTo(100).Within(.001));
    }
    [Test] public void FusingShieldsPreservesMeanConditionAndProportionalStoredCharge()
    {
        Assert.That(R3VehicleModel.SupportsFusion(R3PartKind.DirectionalShield),Is.True);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(1000);
        for(int i=0;i<3;i++)Assert.That(model.PurchaseToInventory(R3PartKind.DirectionalShield),Is.True);
        var parts=model.Inventory.ToArray();parts[0].Health=.5f;parts[1].Health=0;parts[2].Health=1;
        parts[0].DefenseCharge=0;parts[1].DefenseCharge=50;parts[2].DefenseCharge=100;
        parts[0].DefenseRechargeAge=5;parts[1].DefenseRechargeAge=2;parts[2].DefenseRechargeAge=7;
        Assert.That(model.FuseInventory(parts.Select(p=>p.Id)),Is.True);var result=model.Inventory.Single();
        Assert.That(result.Tier,Is.EqualTo(2));Assert.That(result.Health,Is.EqualTo(.5f).Within(.001));
        Assert.That(result.DefenseCharge,Is.EqualTo(100).Within(.001));Assert.That(result.DefenseRechargeAge,Is.EqualTo(2));
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Inventory.Single().DefenseCharge,Is.EqualTo(100).Within(.001));
    }
    [Test] public void ShieldUsesOneCellForEveryRotationWithoutReindexingBonuses()
    {
        Assert.That((int)R3PartKind.BonusModule,Is.EqualTo(13));
        Assert.That(Enum.TryParse("DirectionalShield",out R3PartKind kind),Is.True,"DEF001 has no physical catalog identity");
        for(int x=0;x<4;x++)for(int y=0;y<4;y++)for(int z=0;z<4;z++)
            Assert.That(R3VehicleModel.OccupiedCells(Vector3Int.zero,kind,new Vector3Int(x,y,z)).Distinct().Count(),Is.EqualTo(1));
    }
}

public class RRLW6ShieldPhysicalTests
{
    [UnityTest] public IEnumerator DirectionalShieldTiersScaleAbsorptionAndRegeneration()
    {
        var run=R1RunController.Instance;
        for(int tier=1;tier<=3;tier++)
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
            var cell=MOVSeriesDeviceTests.FindFixtureCell(model,R3PartKind.DirectionalShield,new Vector3Int(0,2,0));
            var state=new R3PartState(cell,R3PartKind.DirectionalShield){Tier=tier};model.Parts.Add(state);
            var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(tier*20,20,100),Quaternion.identity);
            var part=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
            var generator=part.GetComponent<DirectionalShieldGenerator>();int units=R3VehicleModel.TierEffectUnits(tier);
            Assert.That(generator.Capacity,Is.EqualTo(100*units).Within(.001));
            generator.RestoreState(0,3);generator.Advance(1);
            Assert.That(generator.Charge,Is.EqualTo(12*units).Within(.001));
            generator.RestoreState(100*units,0);var hit=part.transform.position+part.transform.forward*2;
            Assert.That(generator.Absorb(1000,hit,-part.transform.forward),Is.EqualTo(100*units).Within(.001));
            UnityEngine.Object.Destroy(vehicle.gameObject);
        }
        yield return null;
    }
    static float TotalHealth(R3VehicleAssembly assembly)=>assembly.Parts
        .Where(p=>p!=null&&p.Damageable!=null&&!p.Damageable.IsDead)
        .Sum(p=>p.Damageable.CurrentHp);
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;run.StartRun();run.ChooseRoute(0);
        while(run.Transitioning)yield return null;
        run.Encounter.SetCombatActive(false);
    }
    [UnityTest] public IEnumerator DirectionalShieldInterceptsBeforeP12HpAndStopsImmediatelyOnLoss()
    {
        Assert.That(Enum.TryParse("DirectionalShield",out R3PartKind kind),Is.True,"DEF001 not integrated into real parts");
        var run=R1RunController.Instance;var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));model.Award(1000);
        var physicalBase=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,1,100),Quaternion.identity);Physics.SyncTransforms();
        Vector3Int? mount=null;
        for(int z=-6;z<=-2&&!mount.HasValue;z++)for(int x=-5;x<=4&&!mount.HasValue;x++)for(int y=0;y<4&&!mount.HasValue;y++)
        {var cell=new Vector3Int(x,y,z);if(model.PlacementError(cell,kind)==null)mount=cell;}
        Assert.That(mount.HasValue,Is.True,"Four-cell rear mount must use real reciprocal contacts");
        Assert.That(model.Buy(mount.Value,kind),Is.True);
        physicalBase.gameObject.SetActive(false);UnityEngine.Object.Destroy(physicalBase.gameObject);
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,1,100),Quaternion.identity);
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var part=assembly.Parts.First(p=>p.Kind==kind);
        Assert.That(part.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Length,Is.EqualTo(1));
        Assert.That(part.GetComponentsInChildren<Collider>().Count(c=>!c.isTrigger),Is.EqualTo(1));
        var generator=part.GetComponent<DirectionalShieldGenerator>();Assert.That(generator,Is.Not.Null);
        var target=assembly.Parts.First(p=>p.Kind==R3PartKind.Armor&&p.transform.position.z>part.transform.position.z+.5f&&Vector3.Distance(p.transform.position,part.transform.position)<8);
        float before=target.Damageable.CurrentHp;
        float totalBefore=TotalHealth(assembly);
        target.Damageable.ApplyDamage(40,target.transform.position,Vector3.back);
        Assert.That(target.Damageable.CurrentHp,Is.EqualTo(before),"Front damage must be intercepted before HP");
        Assert.That(TotalHealth(assembly),Is.EqualTo(totalBefore).Within(.001f),"A blocked hit must not leak to neighbours.");
        Assert.That(generator.Charge,Is.EqualTo(60).Within(.001));
        target.Damageable.ApplyDamage(40,target.transform.position,Vector3.forward);
        Assert.That(target.Damageable.CurrentHp,Is.EqualTo(before-30).Within(.001f),"Rear damage reaches the target and connected parts.");
        Assert.That(TotalHealth(assembly),Is.EqualTo(totalBefore-40).Within(.001f),"The shield cannot multiply or delete a rear hit.");
        part.Damageable.ApplyDamage(part.Damageable.CurrentHp/.75f+1,part.transform.position,Vector3.up);
        Assert.That(generator.Operational,Is.False,"Death disables defense in the same damage operation");
        before=target.Damageable.CurrentHp;totalBefore=TotalHealth(assembly);
        target.Damageable.ApplyDamage(10,target.transform.position,Vector3.back);
        Assert.That(target.Damageable.CurrentHp,Is.EqualTo(before-7.5f).Within(.001f));
        Assert.That(TotalHealth(assembly),Is.EqualTo(totalBefore-10).Within(.001f));
        assembly.Capture();int scrap=model.Scrap;
        Assert.That(model.Repair(part.Cell),Is.True);Assert.That(model.Scrap,Is.EqualTo(scrap-21));
        var restored=R3VehicleAssembly.Build(model.DeepCopy(),new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(150,1,100),Quaternion.identity);
        var again=restored.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==part.Id).GetComponent<DirectionalShieldGenerator>();
        Assert.That(again.Charge,Is.EqualTo(60).Within(.001),"Rebuild cannot farm a full new shield charge");
        yield return null;
    }
    [UnityTest] public IEnumerator EnemyShieldKeepsPassiveReservationDuringEmpUntilDestroyed()
    {
        var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        var bank=enemy.GetComponent<VehicleEnergyBank>();Assert.That(bank,Is.Not.Null);
        float before=bank.Maximum;Assert.That(enemy.EquipDirectionalShield(),Is.True);
        var binding=enemy.GetComponentInChildren<RoboRougelike.WeaponIntegration.RRLShieldBinding>();
        RoboRougelike.WeaponIntegration.RRLEmpVehicleAdapter.Configure(enemy.transform);
        var receiver=binding.GetComponent<RoboRougelike.Mechanics.EMP.EmpReceiver>();Assert.That(receiver,Is.Not.Null);
        bank.Refresh();float reserved=bank.Maximum;Assert.That(reserved,Is.EqualTo(before-25));
        receiver.ApplyEmp(2);Assert.That(receiver.IsDisrupted,Is.True);Assert.That(binding.Device.HardwareAvailable,Is.False);
        bank.Refresh();Assert.That(bank.Maximum,Is.EqualTo(reserved),"EMP blocks operation but does not remove a mounted passive module.");
        receiver.Advance(2);bank.Refresh();Assert.That(bank.Maximum,Is.EqualTo(reserved));
        binding.Device.Damageable.ApplyDamage(100000,binding.transform.position,Vector3.up);
        bank.Refresh();Assert.That(bank.Maximum,Is.EqualTo(before),"Destroying the shield releases its reservation.");
        yield return null;
    }
    [UnityTest] public IEnumerator ActualEnemyShieldSnapshotPreservesConditionChargeAndRotation()
    {
        var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        Assert.That(enemy.EquipDirectionalShield(),Is.True);
        var binding=enemy.GetComponentInChildren<RoboRougelike.WeaponIntegration.RRLShieldBinding>();
        binding.Device.Damageable.ApplyDamage(45,binding.transform.position,Vector3.up);
        binding.Generator.RestoreState(35,1.2f);binding.CaptureState();
        string id=binding.Device.State.Id;
        // A huge overkill now correctly propagates into the shield, changing the
        // snapshot under test. Finish the weakened core with a bounded real hit.
        enemy.Core.RestoreHealthRatio(.001f);
        enemy.Core.ApplyDamageResolvedTyped(enemy.Core.CurrentHp/.9f+1,enemy.Core.transform.position,Vector3.up,
            R3DamagePropagation.DamageKind.Piercing);
        var item=run.Salvage.Items.Single(p=>p.Id==id);var recovered=item.ToPart();
        Assert.That(item.Selectable,Is.True);Assert.That(item.Health,Is.EqualTo(.75f).Within(.001));
        Assert.That(recovered.Kind,Is.EqualTo(R3PartKind.DirectionalShield));
        Assert.That(recovered.DefenseCharge,Is.EqualTo(35).Within(.001));
        Assert.That(recovered.DefenseRechargeAge,Is.EqualTo(1.2f).Within(.001));
        Assert.That(recovered.RotationQuarterTurns,Is.EqualTo(new Vector3Int(0,2,0)));
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        Assert.That(model.ReceiveSalvage(new[]{recovered}),Is.True);
        Assert.That(model.DeepCopy().Inventory.Single().Id,Is.EqualTo(id));
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
