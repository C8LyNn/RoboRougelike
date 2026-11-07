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
    [UnityTearDown] public IEnumerator Cleanup()
    {if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}if(!string.IsNullOrEmpty(path)&&File.Exists(path))File.Delete(path);}
}
