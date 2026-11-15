using NUnit.Framework;
using System;
using System.IO;
using System.Collections;
using System.Linq;
using System.Reflection;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogTechnologyTests
{
    [Test]
    public void PersistentTechnologyProgressUsesTheExistingGameAssembly()
    {
        var type=typeof(R1RunController).Assembly.GetType("RoboRougelike.R1.R1TechnologyProgress");
        Assert.That(type,Is.Not.Null,"Defeated enemy equipment needs a durable technology registry.");
    }
    [Test]
    public void DiscoverySurvivesNewInstanceWithoutGrantingAPhysicalWeapon()
    {
        string path=Path.Combine(Path.GetTempPath(),"rrl-tech-"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            var tech=new R1TechnologyProgress(path);var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
            Assert.That(tech.IsUnlocked(R3PartKind.Gun,R4WeaponKind.Railgun),Is.False);
            var found=new R3PartState(default,R3PartKind.Gun){Weapon=R4WeaponKind.Railgun};
            Assert.That(tech.Discover(new[]{found}),Is.EqualTo(1));
            Assert.That(model.Inventory,Is.Empty,"Discovery unlocks a purchase, not an item.");
            var restarted=new R1TechnologyProgress(path);
            Assert.That(restarted.IsUnlocked(R3PartKind.Gun,R4WeaponKind.Railgun),Is.True);
            Assert.That(restarted.IsUnlocked(R3PartKind.Gun,R4WeaponKind.RocketLauncher),Is.False);
            Assert.That(restarted.Discover(new[]{found}),Is.Zero);
        }
        finally {if(File.Exists(path))File.Delete(path);}
    }
    [Test]
    public void CorruptProfileIsPreservedAndDoesNotRevokeAccess()
    {
        string path=Path.Combine(Path.GetTempPath(),"rrl-tech-corrupt-"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            File.WriteAllText(path,"broken");var tech=new R1TechnologyProgress(path);
            Assert.That(tech.Error,Is.Not.Null);
            Assert.That(tech.IsUnlocked(R3PartKind.Gun,R4WeaponKind.Railgun),Is.True);
            tech.Discover(new[]{new R3PartState(default,R3PartKind.Gun){Weapon=R4WeaponKind.Railgun}});
            Assert.That(File.ReadAllText(path),Is.EqualTo("broken"));
        }
        finally {if(File.Exists(path))File.Delete(path);}
    }
    [Test]
    public void LegacyOwnedWeaponsBecomePermanentlyAvailable()
    {
        string path=Path.Combine(Path.GetTempPath(),"rrl-tech-legacy-"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
            model.Parts.First(p=>p.Kind==R3PartKind.Gun).Weapon=R4WeaponKind.RocketLauncher;
            var tech=new R1TechnologyProgress(path);Assert.That(tech.Discover(model.Parts.Concat(model.Inventory)),Is.EqualTo(1));
            Assert.That(new R1TechnologyProgress(path).IsUnlocked(R3PartKind.Gun,R4WeaponKind.RocketLauncher),Is.True);
        }
        finally {if(File.Exists(path))File.Delete(path);}
    }
}

public sealed class BacklogTechnologyIntegrationTests
{
    R1RunController run;string path;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        path=Path.Combine(Path.GetTempPath(),"rrl-tech-live-"+Guid.NewGuid().ToString("N")+".json");
        typeof(R1RunController).GetProperty("Technology").GetSetMethod(true).Invoke(run,new object[]{new R1TechnologyProgress(path)});
        Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        for(int i=0;i<300&&run.Transitioning;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));
    }
    [UnityTest] public IEnumerator ActualEnemyDefeatUnlocksItsWeaponImmediatelyWithoutGrantingIt()
    {
        Assert.That(run.CanBuyTechnology(R3PartKind.Gun,R4WeaponKind.Railgun),Is.False);
        var enemy=run.Encounter.Enemies.First();Assert.That(enemy.EquipRailgun(),Is.True);
        int inventory=run.VehicleModel.Inventory.Count;
        enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.up);
        Assert.That(run.CanBuyTechnology(R3PartKind.Gun,R4WeaponKind.Railgun),Is.True);
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(inventory));
        Assert.That(new R1TechnologyProgress(path).IsUnlocked(R3PartKind.Gun,R4WeaponKind.Railgun),Is.True);
        foreach(var other in run.Encounter.Enemies.Where(e=>!e.IsDefeated).ToArray())
            other.Core.ApplyDamage(100000,other.transform.position,Vector3.up);
        for(int i=0;i<500&&run.Progress.State==R1RunState.Combat;i++)
        {
            if(run.PickupGraceRemaining>0)typeof(R1RunController).GetField("pickupGraceDeadline",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(run,Time.time);
            yield return null;
        }
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        Assert.That(run.ConfirmRewards(Array.Empty<string>(),new[]{0}),Is.True);
        run.VehicleModel.Award(500);int scrap=run.VehicleModel.Scrap;
        Assert.That(run.GaragePurchase(R3PartKind.Gun,R4WeaponKind.EmpEmitter),Is.False,"Undiscovered technology cannot be bought.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(run.GaragePurchase(R3PartKind.Gun,R4WeaponKind.Railgun),Is.True,"Defeated enemy unlocks future purchases.");
    }
    [UnityTest] public IEnumerator DisarmedEnemyStillUnlocksItsRailgunAfterCoreDefeat()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();enemy.GetComponent<Rigidbody>().isKinematic=true;
        enemy.SetCombatActive(true);Assert.That(enemy.EquipRailgun(),Is.True);
        Assert.That(run.CanBuyTechnology(R3PartKind.Gun,R4WeaponKind.Railgun),Is.False);
        string id=enemy.EquipmentInstanceId("gun");var weapon=enemy.WeaponBlock;
        weapon.ApplyDamageResolvedTyped(weapon.CurrentHp/.9f+1,weapon.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(weapon.IsDead,Is.True);Assert.That(enemy.IsDefeated,Is.False);
        Assert.That(run.Salvage.Items.Any(p=>p.Id==id&&p.Weapon==R4WeaponKind.Railgun),Is.True,
            "The early wreck must retain the actual type, even though the enemy started with another gun.");
        Assert.That(run.CanBuyTechnology(R3PartKind.Gun,R4WeaponKind.Railgun),Is.False,"Dropping a gun is not core defeat.");
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(enemy.IsDefeated,Is.True);
        Assert.That(run.CanBuyTechnology(R3PartKind.Gun,R4WeaponKind.Railgun),Is.True);
        Assert.That(new R1TechnologyProgress(path).IsUnlocked(R3PartKind.Gun,R4WeaponKind.Railgun),Is.True);
        Assert.That(run.Salvage.Items.Count(p=>p.Id==id),Is.EqualTo(1));
        yield return null;
    }
    [UnityTest] public IEnumerator AdditionalGunEquippedAfterEnemyStartCanDropWhenDetached()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();enemy.GetComponent<Rigidbody>().isKinematic=true;enemy.SetCombatActive(true);
        var list=(System.Collections.IList)enemy.GetType().GetField("additionalGuns",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(enemy);
        foreach(var existing in enemy.AdditionalWeapons.ToArray())
            if(existing.Block!=null){existing.Block.gameObject.SetActive(false);UnityEngine.Object.Destroy(existing.Block.gameObject);}
        list.Clear();
        Assert.That(enemy.EquipAdditionalGun(),Is.True,"This weapon is equipped after the enemy's Start method.");
        var extra=enemy.AdditionalWeapons.Single();string id=enemy.EquipmentInstanceId("extra-gun/0");
        var block=extra.Block.GetComponent<RogueMachine.P12PipelineSafe.P12_PipelineSafePlayerBlock>();Assert.That(block,Is.Not.Null);
        block.MarkDetached();
        Assert.That(run.Salvage.Items.Count(p=>p.Id==id&&p.Weapon==R4WeaponKind.Automatic),Is.EqualTo(1));
        Assert.That(UnityEngine.Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).Count(p=>p.ItemId==id),Is.EqualTo(1));
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        Assert.That(enemy.IsDefeated,Is.True);
        Assert.That(run.Salvage.Items.Count(p=>p.Id==id),Is.EqualTo(1),"Core defeat must not duplicate the already dropped weapon.");
        Assert.That(UnityEngine.Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).Count(p=>p.ItemId==id),Is.EqualTo(1));
        Assert.That(run.VehicleModel.Owns(id),Is.False,"Discovery and defeat must not grant an uncollected part.");
        yield return null;
    }
    [UnityTest] public IEnumerator AdditionalGunEquippedAfterEnemyStartCanDropWhenDestroyed()
    {
        run.VehicleModel.Economy.PhysicalLootDropChance=1;
        var enemy=run.Encounter.Enemies.First();enemy.GetComponent<Rigidbody>().isKinematic=true;enemy.SetCombatActive(true);
        var list=(System.Collections.IList)enemy.GetType().GetField("additionalGuns",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(enemy);
        foreach(var existing in enemy.AdditionalWeapons.ToArray())
            if(existing.Block!=null){existing.Block.gameObject.SetActive(false);UnityEngine.Object.Destroy(existing.Block.gameObject);}
        list.Clear();Assert.That(enemy.EquipAdditionalGun(),Is.True);
        var gun=enemy.AdditionalWeapons.Single().Block;string id=enemy.EquipmentInstanceId("extra-gun/0");
        gun.ApplyDamageResolvedTyped(gun.CurrentHp/.75f+1,gun.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(gun.IsDead,Is.True);Assert.That(enemy.IsDefeated,Is.False);
        Assert.That(run.Salvage.Items.Count(p=>p.Id==id&&p.Weapon==R4WeaponKind.Automatic&&p.Health==0),Is.EqualTo(1));
        Assert.That(UnityEngine.Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None).Count(p=>p.ItemId==id),Is.EqualTo(1));
        yield return null;
    }
    [UnityTest] public IEnumerator RecoveredTechnologySaveDoesNotKeepTheOldErrorMessage()
    {
        string parent=path+"-blocked";
        string target=Path.Combine(parent,"technology.json");
        try
        {
            // A regular file in place of the parent directory forces a real,
            // recoverable write error without touching the player's profile.
            File.WriteAllText(parent,"temporary blocker");
            var technology=new R1TechnologyProgress(target);
            typeof(R1RunController).GetProperty("Technology").GetSetMethod(true).Invoke(run,new object[]{technology});
            var part=new R3PartState(default,R3PartKind.Gun){Weapon=R4WeaponKind.Railgun};
            var item=new R4SalvageItem(part,1);
            run.DiscoverEnemyTechnology(new[]{item});
            Assert.That(technology.Error,Is.Not.Null,"The first real write must fail.");
            Assert.That(run.TechnologyMessage,Is.EqualTo(technology.Error));
            File.Delete(parent);Directory.CreateDirectory(parent);
            run.DiscoverEnemyTechnology(new[]{item});
            Assert.That(technology.Error,Is.Null,"A later discovery retries the dirty profile.");
            Assert.That(File.Exists(target),Is.True);
            Assert.That(new R1TechnologyProgress(target).IsUnlocked(R3PartKind.Gun,R4WeaponKind.Railgun),Is.True);
            Assert.That(run.TechnologyMessage,Does.Not.Contain("Nie zapisano"),"The reward screen must not retain a resolved error.");
        }
        finally
        {
            if(File.Exists(target))File.Delete(target);
            if(Directory.Exists(parent))Directory.Delete(parent);
            if(File.Exists(parent))File.Delete(parent);
        }
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}if(!string.IsNullOrEmpty(path)&&File.Exists(path))File.Delete(path);}
}
