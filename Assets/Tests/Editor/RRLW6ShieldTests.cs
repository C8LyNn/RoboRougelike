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
    [Test] public void ShieldUsesExistingFourCellXyzGridWithoutReindexingBonuses()
    {
        Assert.That((int)R3PartKind.BonusModule,Is.EqualTo(13));
        Assert.That(Enum.TryParse("DirectionalShield",out R3PartKind kind),Is.True,"DEF001 has no physical catalog identity");
        for(int x=0;x<4;x++)for(int y=0;y<4;y++)for(int z=0;z<4;z++)
            Assert.That(R3VehicleModel.OccupiedCells(Vector3Int.zero,kind,new Vector3Int(x,y,z)).Distinct().Count(),Is.EqualTo(4));
    }
}

public class RRLW6ShieldPhysicalTests
{
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
        Vector3Int? mount=null;
        for(int z=-6;z<=-2&&!mount.HasValue;z++)for(int x=-5;x<=4&&!mount.HasValue;x++)for(int y=0;y<4&&!mount.HasValue;y++)
        {var cell=new Vector3Int(x,y,z);if(model.PlacementError(cell,kind)==null)mount=cell;}
        Assert.That(mount.HasValue,Is.True,"Four-cell rear mount must use real reciprocal contacts");
        Assert.That(model.Buy(mount.Value,kind),Is.True);
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,1,100),Quaternion.identity);
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var part=assembly.Parts.First(p=>p.Kind==kind);
        Assert.That(part.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Length,Is.EqualTo(1));
        Assert.That(part.GetComponentsInChildren<Collider>().Count(c=>!c.isTrigger),Is.EqualTo(1));
        var generator=part.GetComponent<DirectionalShieldGenerator>();Assert.That(generator,Is.Not.Null);
        var target=assembly.Parts.First(p=>p.Kind==R3PartKind.Armor&&p.transform.position.z>part.transform.position.z+.5f&&Vector3.Distance(p.transform.position,part.transform.position)<8);
        float before=target.Damageable.CurrentHp;
        target.Damageable.ApplyDamage(40,target.transform.position,Vector3.back);
        Assert.That(target.Damageable.CurrentHp,Is.EqualTo(before),"Front damage must be intercepted before HP");
        Assert.That(generator.Charge,Is.EqualTo(60).Within(.001));
        target.Damageable.ApplyDamage(40,target.transform.position,Vector3.forward);
        Assert.That(target.Damageable.CurrentHp,Is.EqualTo(before-40),"Rear damage passes the directional arc");
        part.Damageable.ApplyDamage(10000,part.transform.position,Vector3.up);
        Assert.That(generator.Operational,Is.False,"Death disables defense in the same damage operation");
        target.Damageable.ApplyDamage(40,target.transform.position,Vector3.back);
        Assert.That(target.Damageable.CurrentHp,Is.EqualTo(before-80));
        assembly.Capture();int scrap=model.Scrap;
        Assert.That(model.Repair(part.Cell),Is.True);Assert.That(model.Scrap,Is.EqualTo(scrap-60));
        var restored=R3VehicleAssembly.Build(model.DeepCopy(),new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(150,1,100),Quaternion.identity);
        var again=restored.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==part.Id).GetComponent<DirectionalShieldGenerator>();
        Assert.That(again.Charge,Is.EqualTo(60).Within(.001),"Rebuild cannot farm a full new shield charge");
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
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
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
