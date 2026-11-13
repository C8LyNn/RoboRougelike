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
using RoboRougelike.WeaponIntegration;
using RoboRougelike.Mechanics.PointDefense;

public class RRLW6PointDefenseTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;run.StartRun();run.ChooseRoute(0);
        while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);
    }
    [UnityTest] public IEnumerator MountedPointDefenseInterceptsActualRocketUsingItsOriginalHp()
    {yield return InterceptActualRocket(1);}
    [UnityTest] public IEnumerator TierTwoPointDefenseInterceptsActualRocket()
    {yield return InterceptActualRocket(2);}
    IEnumerator InterceptActualRocket(int tier)
    {
        Assert.That(Enum.TryParse("PointDefense",out R3PartKind kind),Is.True,"DEF002 missing physical mount");
        var run=R1RunController.Instance;var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));model.Award(1000);
        var physicalBase=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);Physics.SyncTransforms();
        Vector3Int? mount=null;
        // Prefer a supported upper mounting point: the rebuilt starter is smaller,
        // and a lower edge can scrape the raised test platform before interception.
        for(int y=3;y>=0&&!mount.HasValue;y--)for(int z=-6;z<=-2&&!mount.HasValue;z++)for(int x=-5;x<=4&&!mount.HasValue;x++)
        {var c=new Vector3Int(x,y,z);if(model.PlacementError(c,kind)==null)mount=c;}
        Assert.That(mount.HasValue,Is.True);Assert.That(model.Buy(mount.Value,kind),Is.True);model.At(mount.Value).Tier=tier;
        physicalBase.gameObject.SetActive(false);UnityEngine.Object.Destroy(physicalBase.gameObject);
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        // Keep the interception fixture physically supported on a test stand.
        var platform=GameObject.CreatePrimitive(PrimitiveType.Cube);platform.transform.position=new Vector3(100,2.85f,100);platform.transform.localScale=new Vector3(12,1,12);
        Physics.SyncTransforms();yield return MOV009IntegrationTests.Seconds(.7f);
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var part=assembly.Parts.Single(p=>p.Kind==kind);
        var controller=part.GetComponent<PointDefenseController>();Assert.That(controller,Is.Not.Null);
        var enemy=run.Encounter.Enemies.First();enemy.SetCombatActive(true);Assert.That(enemy.EquipRocket(),Is.True);
        enemy.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;
        var bank=enemy.WeaponBlock.GetComponent<RRLRocketBinding>();
        var muzzle=new GameObject("incoming source muzzle").transform;muzzle.SetParent(enemy.transform,false);
        muzzle.position=part.transform.position+Vector3.up*1.2f+Vector3.forward*18;
        muzzle.rotation=Quaternion.LookRotation((vehicle.transform.position+Vector3.up)-muzzle.position);
        bank.Module.Bind(enemy.GetComponent<Rigidbody>(),enemy.transform,muzzle);
        Assert.That(bank.Fire(),Is.True);
        var rocket=UnityEngine.Object.FindObjectsByType<RoboRougelike.Mechanics.Rockets.RocketProjectile>(FindObjectsSortMode.None).Single(p=>p.IsInitialized);
        PointDefenseInterceptor interceptor=null;float untilShot=Time.time+1;
        while(Time.time<untilShot&&interceptor==null)
        {
            interceptor=UnityEngine.Object.FindObjectsByType<PointDefenseInterceptor>(FindObjectsSortMode.None).FirstOrDefault(p=>p.IsInitialized);
            if(interceptor==null)yield return null;
        }
        Assert.That(interceptor,Is.Not.Null,"A real interceptor must be launched.");
        var damage=typeof(PointDefenseInterceptor).GetField("damage",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That((float)damage.GetValue(interceptor),Is.EqualTo(6*R3VehicleModel.TierEffectUnits(tier)).Within(.001));
        float hp=part.Damageable.CurrentHp;Physics.SyncTransforms();
        float end=Time.time+1;while(Time.time<end&&rocket!=null&&!rocket.IsResolved)yield return null;
        Assert.That(rocket==null||rocket.IsResolved,Is.True,"Real interceptors must resolve the source rocket HP");
        Assert.That(rocket.HitPoints,Is.LessThanOrEqualTo(0),"Natural impact/lifetime resolution is not proof of interception");
        Assert.That(part.Damageable.CurrentHp,Is.EqualTo(hp));
        Assert.That(controller.Ammunition,Is.LessThan(40).And.GreaterThanOrEqualTo(35));
        var state=model.Parts.Single(p=>p.Id==part.Id);assembly.Capture();Assert.That(state.Ammo,Is.EqualTo(controller.Ammunition));
        // A huge overkill now propagates through connection graph and destroys
        // the support, making repair correctly impossible. Use a lethal piercing
        // hit with a small margin so the attached support stays repairable.
        part.Damageable.ApplyDamageResolvedTyped(part.Damageable.CurrentHp/.9f+1f,part.transform.position,Vector3.up,
            R3DamagePropagation.DamageKind.Piercing);
        Assert.That(controller.IsOperational,Is.False,"Destroyed defense must immediately stop.");
        assembly.Capture();Assert.That(model.Repair(part.Cell),Is.True,$"Repair failed at {part.Cell}; scrap={model.Scrap}; health={state.Health}");
        var rebuilt=R3VehicleAssembly.Build(model.DeepCopy(),new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(150,4,100),Quaternion.identity);
        Assert.That(rebuilt.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==part.Id).GetComponent<PointDefenseController>().Ammunition,Is.EqualTo(state.Ammo));
    }
    [UnityTest] public IEnumerator EnemySalvagePreservesCanonicalCooldownAndReloadWithoutReset()
    {
        var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();Assert.That(enemy.EquipShotgun(),Is.True);
        var gun=enemy.GetComponentInChildren<RRLShotgunBinding>();gun.State.Ammo=3;
        gun.State.DeviceReadyAt=Time.time+5;gun.State.ReloadReadyAt=Time.time+3;
        float ready=gun.State.DeviceReadyAt,reload=gun.State.ReloadReadyAt;string id=gun.State.Id;
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        var recovered=run.Salvage.Items.Single(p=>p.Id==id).ToPart();
        Assert.That(recovered.Ammo,Is.EqualTo(3));Assert.That(recovered.DeviceReadyAt,Is.EqualTo(ready));
        Assert.That(recovered.ReloadReadyAt,Is.EqualTo(reload));yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}

    [UnityTest] public IEnumerator EnemyDefenseLosesRealArmorMountAndRemainsRepairableSalvage()
    {
        var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        Assert.That(enemy.EquipPointDefense(),Is.True);
        var binding=enemy.GetComponentInChildren<RRLPointDefenseBinding>();
        var support=binding.transform.parent.GetComponentInParent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();
        Assert.That(support,Is.Not.Null,"Device must be parented to actual physical armor");
        Assert.That(binding.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Length,Is.EqualTo(1));
        binding.Controller.RestoreState(7,.2f);binding.CaptureState();string id=binding.State.Id;float ready=binding.State.DeviceReadyAt;
        support.ApplyDamage(10000,support.transform.position,Vector3.up);
        Assert.That(binding.Controller.IsOperational,Is.False,"Losing the real mounting armor detaches the device immediately");
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        var salvage=run.Salvage.Items.Single(p=>p.Id==id);Assert.That(salvage.Selectable,Is.True);Assert.That(salvage.Health,Is.Zero);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));model.Award(1000);
        Assert.That(ledgerClaim(run.Salvage,model,id),Is.True);
        Assert.That(model.Inventory.Single().Health,Is.EqualTo(1));model.Inventory.Single().Health=0; // Subsequent own damage.
        Assert.That(model.RepairInventory(id),Is.True);
        var recovered=model.DeepCopy().Inventory.Single();Assert.That(recovered.Ammo,Is.EqualTo(7));
        Assert.That(recovered.DeviceReadyAt,Is.EqualTo(ready));yield return null;
    }
    static bool ledgerClaim(R4SalvageLedger ledger,R3VehicleModel model,string id)=>ledger.Claim(model,new[]{id});
}
