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
using RoboRougelike.WeaponIntegration;
using RoboRougelike.Mechanics.EMP;
using RogueMachine.P12PipelineSafe;

public class RRLW6EmpPhysicalTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;run.StartRun();run.ChooseRoute(0);
        while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);
    }
    R3VehicleAssembly Build(R4WeaponKind kind,out R3VehicleModel model,int index=0)
    {
        model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));model.Parts.First(p=>p.Kind==R3PartKind.Gun).Weapon=kind;
        return R3VehicleAssembly.Build(model,new R1RunStats(),R1RunController.Instance.GetComponent<R3VisualPalette>(),new Vector3(100+index*20,4,100),Quaternion.identity).GetComponent<R3VehicleAssembly>();
    }
    static bool Fire(R3MountedPart gun)
    {
        var binding=gun.GetComponents<MonoBehaviour>().Single(p=>p.GetType().Name=="RRLEmpBinding");return (bool)binding.GetType().GetMethod("Fire").Invoke(binding,null);
    }
    [UnityTest] public IEnumerator ActualEmpPulseStopsEnemyGunWithoutHpDamagePhysicalLossOrMomentumReset()
    {
        Assert.That(Enum.TryParse("EmpEmitter",out R4WeaponKind kind),Is.True,"WPN006 absent from real gun catalog");
        var assembly=Build(kind,out _);var gun=assembly.Parts.Single(p=>p.Kind==R3PartKind.Gun);
        var run=R1RunController.Instance;
        var template=(P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,run.Player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner}},run.Salvage,"emp-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();enemy.SetCombatActive(true);Assert.That(enemy.EquipShotgun(),Is.True);enemy.ConfigureMovement(0,12,0);
        // Emit from above the target armor: a frontal low pulse legitimately
        // reaches exposed wheels while the original LOS rule protects its gun.
        assembly.GetComponent<Rigidbody>().position=new Vector3(100,5,100);
        var body=enemy.GetComponent<Rigidbody>();body.position=new Vector3(100,4,106);Physics.SyncTransforms();
        // The teleported body's ClosestPoint pose catches up on an actual PhysX step.
        float fixedBefore=Time.fixedTime;while(Time.fixedTime<=fixedBefore)yield return null;
        body.linearVelocity=Vector3.right*2;
        var target=enemy.GetComponentInChildren<RRLShotgunBinding>();var receiver=target.GetComponent<EmpReceiver>();Assert.That(receiver,Is.Not.Null);
        float hp=enemy.GetComponentsInChildren<P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);Vector3 momentum=body.linearVelocity;
        EmpPulseReport report=default;gun.GetComponent<EmpEmitter>().Pulsed+=r=>report=r;
        Physics.SyncTransforms();Assert.That(Fire(gun),Is.True);Assert.That(Fire(gun),Is.False,"Cooldown is a committed pulse resource");
        Assert.That(receiver.IsDisrupted,Is.True,$"Visible electronic target: detected={report.ReceiversDetected},occluded={report.ReceiversOccluded},affected={report.ReceiversAffected},distance={Vector3.Distance(gun.transform.position,target.GetComponent<Collider>().ClosestPoint(gun.transform.position)):0.00}");
        Assert.That(target.Module.Operational,Is.False,"Temporary status must revoke source module operation synchronously");
        Assert.That(body.linearVelocity,Is.EqualTo(momentum));Assert.That(enemy.GetComponentsInChildren<P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.EqualTo(hp));
        Assert.That(enemy.WeaponOnline,Is.True,"EMP is not structural turret loss");
        Assert.That(enemy.GetComponentsInChildren<R3Debris>(),Is.Empty);
        float recoveryDeadline=Time.realtimeSinceStartup+8;
        while(receiver.IsDisrupted&&Time.realtimeSinceStartup<recoveryDeadline)yield return null;
        Assert.That(receiver.IsDisrupted,Is.False,"The production receiver must recover on its original enabled clock");
        Assert.That(target.Module.Operational,Is.True);
        receiver.ApplyEmp(2);target.GetComponent<P12PipelineSafeDamageable>().ApplyDamage(10000,target.transform.position,Vector3.up);
        receiver.ClearEmp();Assert.That(target.Module.Operational,Is.False,"Recovery must never resurrect destroyed hardware");
    }
    [UnityTest] public IEnumerator DisruptedWheelsKeepMassMomentumAndHealthySavedParts()
    {
        var assembly=Build(R4WeaponKind.Basic,out var model);var wheels=assembly.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray();
        var drive=assembly.GetComponent<R6VehicleDrive>();drive.RefreshMass();int count=drive.Wheels;Assert.That(count,Is.GreaterThan(0));
        var body=assembly.GetComponent<Rigidbody>();float mass=body.mass;body.linearVelocity=Vector3.right*3;
        foreach(var wheel in wheels){var receiver=wheel.GetComponent<EmpReceiver>();Assert.That(receiver,Is.Not.Null,"Wheel electronics receiver missing");receiver.ApplyEmp(3);}
        Assert.That(drive.Wheels,Is.Zero);Assert.That(body.mass,Is.EqualTo(mass));Assert.That(body.linearVelocity,Is.EqualTo(Vector3.right*3));
        Assert.That(assembly.GetComponent<R1VehicleState>().DefeatReason,Is.Null,"Temporary wheel EMP must not end the expedition as destroyed hardware");
        assembly.Capture();Assert.That(model.Parts.Where(p=>p.Kind==R3PartKind.Wheel).All(p=>p.Health==1),Is.True,"Capture must not turn temporary EMP into destroyed parts");
        var rebuilt=R3VehicleAssembly.Build(model.DeepCopy(),new R1RunStats(),R1RunController.Instance.GetComponent<R3VisualPalette>(),new Vector3(150,4,100),Quaternion.identity).GetComponent<R3VehicleAssembly>();
        Assert.That(rebuilt.Parts.Where(p=>p.Kind==R3PartKind.Wheel).All(p=>p.GetComponent<EmpReceiver>().IsDisrupted),Is.True);
        foreach(var wheel in wheels)wheel.GetComponent<EmpReceiver>().ClearEmp();Assert.That(drive.Wheels,Is.EqualTo(count));yield return null;
    }
    [UnityTest] public IEnumerator RepairedEmpEmitterPreservesCommittedCooldown()
    {
        Assert.That(Enum.TryParse("EmpEmitter",out R4WeaponKind kind),Is.True,"EMP absent from player catalog");
        var assembly=Build(kind,out var model);var gun=assembly.Parts.Single(p=>p.Kind==R3PartKind.Gun);Assert.That(Fire(gun),Is.True);
        gun.Damageable.ApplyDamage(10000,gun.transform.position,Vector3.up);assembly.Capture();model.Award(500);Assert.That(model.Repair(gun.Cell),Is.True);
        var rebuilt=R3VehicleAssembly.Build(model.DeepCopy(),new R1RunStats(),R1RunController.Instance.GetComponent<R3VisualPalette>(),new Vector3(150,4,100),Quaternion.identity).GetComponent<R3VehicleAssembly>();
        var restored=rebuilt.Parts.Single(p=>p.Id==gun.Id);Assert.That(restored.GetComponent<EmpEmitter>().CooldownRemaining,Is.GreaterThan(11));Assert.That(Fire(restored),Is.False);yield return null;
    }
    [UnityTest] public IEnumerator EveryIntegratedWeaponUsesTemporaryDeviceGateWithoutLosingPhysicalPower()
    {
        int index=0;
        foreach(var kind in new[]{R4WeaponKind.Shotgun,R4WeaponKind.Railgun,R4WeaponKind.BeamCutter,R4WeaponKind.RocketLauncher,R4WeaponKind.RotarySaw})
        {
            var assembly=Build(kind,out _,index++);var gun=assembly.Parts.Single(p=>p.Kind==R3PartKind.Gun);var receiver=gun.GetComponent<EmpReceiver>();
            Assert.That(receiver,Is.Not.Null,kind+" receiver missing");var device=gun.GetComponent<RRLDeviceBinding>();Assert.That(device.Available,Is.True);
            receiver.ApplyEmp(2);Assert.That(device.Available,Is.False);Assert.That(gun.Operational,Is.True,"Physical graph/HP must remain intact under EMP");
            Assert.That(assembly.GetComponent<R1VehicleState>().DefeatReason,Is.Null,"Temporary weapon EMP must not produce a structural defeat");
            receiver.ClearEmp();Assert.That(device.Available,Is.True);
        }
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
    [UnityTest] public IEnumerator ActorTeardownDoesNotEraseOwnedPartEmpBeforeNextFight()
    {
        var assembly=Build(R4WeaponKind.Basic,out var model);var gun=assembly.Parts.Single(p=>p.Kind==R3PartKind.Gun);
        gun.GetComponent<EmpReceiver>().ApplyEmp(3);assembly.Capture();string id=gun.Id;
        assembly.gameObject.SetActive(false);UnityEngine.Object.Destroy(assembly.gameObject);yield return null;
        var saved=model.DeepCopy();
        Assert.That(saved.Parts.Single(p=>p.Id==id).DisruptionRemaining,Is.GreaterThan(2.5f),"Destroying the old runtime view must not erase persistent part state");
        var rebuilt=R3VehicleAssembly.Build(saved,new R1RunStats(),R1RunController.Instance.GetComponent<R3VisualPalette>(),new Vector3(150,4,100),Quaternion.identity).GetComponent<R3VehicleAssembly>();
        Assert.That(rebuilt.Parts.Single(p=>p.Id==id).GetComponent<EmpReceiver>().IsDisrupted,Is.True);
    }
    [UnityTest] public IEnumerator EliteBatterySalvageKeepsTemporaryEmpWithoutConvertingItToDamage()
    {
        var run=R1RunController.Instance;
        var template=(P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,run.Player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner},Variants=new[]{RoboRougelike.R5.R5EnemyVariant.EliteGunner}},run.Salvage,"emp-elite-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();var gun=enemy.VariantWeapons[0];
        var receiver=gun.Block.GetComponent<EmpReceiver>();Assert.That(receiver,Is.Not.Null);receiver.ApplyEmp(2);
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        var item=run.Salvage.Items.Single(p=>p.Id=="emp-elite-fixture/enemy/0/gun/0").ToPart();
        Assert.That(item.Health,Is.GreaterThan(0));
        var remaining=(float)typeof(R3PartState).GetField("DisruptionRemaining").GetValue(item);
        Assert.That(remaining,Is.EqualTo(2).Within(.001),"Elite battery must retain temporary electronic state through actual salvage");
    }
    [UnityTest] public IEnumerator ExpiredEnemyPartsRemainRecoverableAndRepairable()
    {
        var run=R1RunController.Instance;
        var template=(P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(run);
        int verified=0;
        foreach(var variant in new[]{RoboRougelike.R5.R5EnemyVariant.Normal,RoboRougelike.R5.R5EnemyVariant.EliteGunner})
        {
            var ledger=new R4SalvageLedger(3);
            run.Encounter.Spawn(template,run.Player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner},Variants=new[]{variant}},ledger,"expired-parts");
            run.Encounter.SetCombatActive(false);
            int ready=Time.frameCount+3;while(Time.frameCount<ready)yield return null;
            var enemy=run.Encounter.Enemies.First();var gun=enemy.WeaponBlock;
            var wheel=enemy.GetComponentsInChildren<P12PipelineSafeDamageable>().First(R6VehicleDrive.IsWheel);
            gun.ApplyDamage(100000,gun.transform.position,Vector3.up);
            wheel.ApplyDamage(100000,wheel.transform.position,Vector3.up);
            // Expire detached wrecks while their owner core still survives.
            UnityEngine.Object.Destroy(gun.gameObject);UnityEngine.Object.Destroy(wheel.gameObject);
            ready=Time.frameCount+3;while(Time.frameCount<ready)yield return null;
            Assert.That(gun==null&&wheel==null,Is.True);
            Assert.That(enemy.IsDefeated,Is.False);
            Assert.DoesNotThrow(()=>enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up));
            string gunId=variant==RoboRougelike.R5.R5EnemyVariant.Normal?"expired-parts/enemy/0/gun":"expired-parts/enemy/0/gun/0";
            string wheelId="expired-parts/enemy/0/wheel/0";
            Assert.That(ledger.Items.Single(p=>p.Id==gunId).Health,Is.Zero);
            Assert.That(ledger.Items.Single(p=>p.Id==wheelId).Health,Is.Zero);
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
            Assert.That(ledger.Claim(model,new[]{gunId,wheelId}),Is.True);model.Award(500);
            Assert.That(model.RepairInventory(gunId),Is.True);Assert.That(model.RepairInventory(wheelId),Is.True);
            Assert.That(model.Inventory.Where(p=>p.Id==gunId||p.Id==wheelId).All(p=>p.Health==1),Is.True);verified++;
        }
        Assert.That(verified,Is.EqualTo(2));Debug.Log("[W6 EXPIRY QA] Normal and elite expired gun/wheel recovery and paid repair verified");
    }
    [UnityTest] public IEnumerator ExpiredPackageDevicesKeepFiniteResourcesAndLootIdentity()
    {
        var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        Assert.That(enemy.EquipShotgun(),Is.True);Assert.That(enemy.EquipDirectionalShield(),Is.True);Assert.That(enemy.EquipPointDefense(),Is.True);
        var gun=enemy.GetComponentInChildren<RRLShotgunBinding>();var shield=enemy.GetComponentInChildren<RRLShieldBinding>();var defense=enemy.GetComponentInChildren<RRLPointDefenseBinding>();
        enemy.SetCombatActive(true);enemy.ConfigureMovement(0,12,0);
        Assert.That(gun.Fire(),Is.True);int ammo=gun.State.Ammo;Assert.That(ammo,Is.EqualTo(11));
        shield.Generator.RestoreState(40,0);shield.CaptureState();defense.Controller.RestoreState(7,.4f);defense.CaptureState();
        string gunId=gun.State.Id,shieldId=shield.Device.State.Id,defenseId=defense.State.Id;
        foreach(var hp in new[]{gun.GetComponent<RRLDeviceBinding>().Damageable,shield.Device.Damageable,defense.Device.Damageable})
        {hp.ApplyDamage(100000,hp.transform.position,Vector3.up);UnityEngine.Object.Destroy(hp.gameObject);}
        enemy.SetCombatActive(false);
        int ready=Time.frameCount+3;while(Time.frameCount<ready)yield return null;
        Assert.That(gun==null&&shield==null&&defense==null,Is.True);
        Assert.That(enemy.IsDefeated,Is.False);enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        var shot=run.Salvage.Items.Single(p=>p.Id==gunId);Assert.That(shot.Ammo,Is.EqualTo(ammo),"Wreck expiry must not refill finite ammunition");
        var shieldLoot=run.Salvage.Items.Single(p=>p.Id==shieldId);Assert.That(shieldLoot.DefenseCharge,Is.EqualTo(40).Within(.001));
        Assert.That(run.Salvage.Items.Single(p=>p.Id==defenseId).Ammo,Is.EqualTo(7));
        Assert.That(new[]{shot,shieldLoot,run.Salvage.Items.Single(p=>p.Id==defenseId)}.All(p=>p.Health==0),Is.True);
        Assert.That(run.Salvage.Claim(run.VehicleModel,new[]{gunId,shieldId,defenseId}),Is.True);run.VehicleModel.Award(500);
        foreach(var id in new[]{gunId,shieldId,defenseId})Assert.That(run.VehicleModel.RepairInventory(id),Is.True);
        var saved=run.VehicleModel.DeepCopy();Assert.That(saved.Inventory.Single(p=>p.Id==gunId).Ammo,Is.EqualTo(ammo));
        Assert.That(saved.Inventory.Single(p=>p.Id==shieldId).DefenseCharge,Is.EqualTo(40).Within(.001));Assert.That(saved.Inventory.Single(p=>p.Id==defenseId).Ammo,Is.EqualTo(7));
        Debug.Log("[W6 EXPIRY QA] Expired shotgun/shield/PDS resources, identities and paid repair verified");
    }
    [UnityTest] public IEnumerator ActualExpeditionAndAlternativeDriveSurviveTemporaryEmp()
    {
        var run=R1RunController.Instance;var player=run.Player;
        var receivers=player.GetComponentsInChildren<EmpReceiver>();Assert.That(receivers,Is.Not.Empty);
        foreach(var receiver in receivers)receiver.ApplyEmp(3);
        for(int frame=0;frame<3;frame++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat),"A real expedition must remain playable while hardware is temporarily EMP-disabled");
        foreach(var receiver in receivers)receiver.ClearEmp();
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));model.Award(1000);
        int pods=0;
        for(int y=0;y<4&&pods<4;y++)for(int z=-4;z<4&&pods<4;z++)for(int x=-4;x<4&&pods<4;x++)
        {var cell=new Vector3Int(x,y,z);if(model.PlacementError(cell,R3PartKind.HoverEngine)==null&&model.Buy(cell,R3PartKind.HoverEngine))pods++;}
        Assert.That(pods,Is.EqualTo(4));model.SelectedDrive=R3DriveKind.Hover;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        var movement=vehicle.GetComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>();Assert.That(movement.HasActiveDrive,Is.True);
        var body=vehicle.GetComponent<Rigidbody>();float mass=body.mass;body.linearVelocity=Vector3.right*2;
        foreach(var part in vehicle.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.HoverEngine))part.GetComponent<EmpReceiver>().ApplyEmp(3);
        Assert.That(movement.HasActiveDrive,Is.False);Assert.That(movement.HasSurvivingDrive,Is.True);
        Assert.That(vehicle.DefeatReason,Is.Null);Assert.That(body.mass,Is.EqualTo(mass));Assert.That(body.linearVelocity,Is.EqualTo(Vector3.right*2));
    }
    [UnityTest] public IEnumerator DynamicallyMountedEnemyDevicesContributeRealRigidbodyMass()
    {
        var enemy=R1RunController.Instance.Encounter.Enemies.First();var drive=enemy.GetComponent<R6VehicleDrive>();drive.RefreshMass();
        var body=enemy.GetComponent<Rigidbody>();float initial=body.mass;
        Assert.That(enemy.EquipDirectionalShield(),Is.True);drive.RefreshMass();Assert.That(body.mass,Is.EqualTo(initial+32).Within(.001),"New physical shield must enter existing R6 mass model");
        Assert.That(enemy.EquipPointDefense(),Is.True);drive.RefreshMass();Assert.That(body.mass,Is.EqualTo(initial+67).Within(.001));
        var defense=enemy.GetComponentInChildren<RRLPointDefenseBinding>();defense.Device.Damageable.ApplyDamage(10000,defense.transform.position,Vector3.up);
        drive.RefreshMass();Assert.That(body.mass,Is.EqualTo(initial+32).Within(.001),"Detached defense must leave the vehicle mass exactly once");yield return null;
    }
    [UnityTest] public IEnumerator ArmorFragmentCarriesItsAttachedDeviceMassAndConservesOwnerMass()
    {
        var enemy=R1RunController.Instance.Encounter.Enemies.First();Assert.That(enemy.EquipPointDefense(),Is.True);
        var defense=enemy.GetComponentInChildren<RRLPointDefenseBinding>();
        var support=defense.transform.parent.GetComponentInParent<P12PipelineSafeDamageable>();
        Assert.That(support.GetComponentsInChildren<P12PipelineSafeDamageable>().Length,Is.EqualTo(2));
        var owner=enemy.GetComponent<Rigidbody>();float before=owner.mass;
        support.ApplyDamage(10000,support.transform.position,Vector3.up);
        var fragment=defense.GetComponentInParent<Rigidbody>();Assert.That(fragment,Is.Not.SameAs(owner));
        Assert.That(fragment.mass,Is.EqualTo(43).Within(.001),"Fragment is8kg armor plus35kg live attached PDS");
        Assert.That(owner.mass,Is.EqualTo(before-43).Within(.001));
        enemy.GetComponent<R6VehicleDrive>().RefreshMass();Assert.That(owner.mass,Is.EqualTo(before-43).Within(.001),"Live refresh must not remove fragment mass twice");yield return null;
    }
    [UnityTest] public IEnumerator EnemyWheelEmpStopsActiveDriveWithoutDestroyingHardware()
    {
        var enemy=R1RunController.Instance.Encounter.Enemies.First();var body=enemy.GetComponent<Rigidbody>();body.linearVelocity=Vector3.right*2;
        var wheels=enemy.GetComponentsInChildren<P12PipelineSafeDamageable>().Where(R6VehicleDrive.IsWheel).ToArray();Assert.That(wheels,Is.Not.Empty);
        foreach(var wheel in wheels){var receiver=wheel.GetComponent<EmpReceiver>();Assert.That(receiver,Is.Not.Null);receiver.ApplyEmp(3);}
        Assert.That(enemy.ActiveWheelCount,Is.Zero,"Enemy movement must respect disabled wheel electronics");
        Assert.That(wheels.All(p=>!p.IsDead),Is.True);Assert.That(body.linearVelocity,Is.EqualTo(Vector3.right*2));Assert.That(enemy.IsDefeated,Is.False);yield return null;
    }
}
