using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.B01.Rewards;
using RogueMachine.P12PipelineSafe;

public sealed class RewardRC2IntegrationTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.StartRun()&&run.ChooseRoute(0),Is.True);
        for(int i=0;i<300&&run.Transitioning;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));run.Encounter.SetCombatActive(false);
    }
    RewardKeep3Service Window
    {
        get {var p=typeof(R1RunController).GetProperty("RecoveryWindow");Assert.That(p,Is.Not.Null,"The source policy must be connected to the actual winning encounter");return (RewardKeep3Service)p.GetValue(run);}
    }
    IEnumerator Win()
    {
        foreach(var e in run.Encounter.Enemies.ToArray())e.Core.ApplyDamage(100000,e.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));Assert.That(Window?.HasOpenWindow,Is.True);
    }
    [UnityTest] public IEnumerator KilledEnemyDropsUseCanonicalPhysicalIds()
    {
        var enemy=run.Encounter.Enemies.First();enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        yield return null;Assert.That(run.Salvage.Items.Count,Is.GreaterThan(0));
        foreach(var item in run.Salvage.Items)
        {Assert.That(Guid.TryParseExact(item.Id,"N",out var id)&&id!=Guid.Empty&&item.Id==id.ToString("N"),Is.True,item.Id);Assert.That(item.ToPart().Id,Is.EqualTo(item.Id));}
        Assert.That(run.Salvage.Items.Select(x=>x.Id).Distinct().Count(),Is.EqualTo(run.Salvage.Items.Count));
    }
    [UnityTest] public IEnumerator SlotIdentityIsStableAcrossRepeatedSalvageBinding()
    {
        var enemy=run.Encounter.Enemies.First();var method=enemy.GetType().GetMethod("EquipmentInstanceId");
        Assert.That(method,Is.Not.Null,"Physical identity must exist before reward DTO creation");
        string Gun()=>(string)method.Invoke(enemy,new object[]{"gun"});string before=Gun();
        string owner=(string)enemy.GetType().GetField("salvageId",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(enemy);
        enemy.ConfigureSalvage(run.Salvage,owner);Assert.That(Gun(),Is.EqualTo(before));
        Assert.That((string)method.Invoke(enemy,new object[]{"wheel/0"}),Is.Not.EqualTo(before));
        var other=run.Encounter.Enemies.Last();Assert.That((string)method.Invoke(other,new object[]{"gun"}),Is.Not.EqualTo(before));
        enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);yield return null;
        Assert.That(run.Salvage.Items.Any(x=>x.Id==before&&x.Kind==R3PartKind.Gun),Is.True);
    }
    [UnityTest] public IEnumerator NoRecoveryWindowBeforeAuthenticatedVictory()
    {Assert.That(Window,Is.Null);yield return null;}
    [UnityTest] public IEnumerator LiveVictoryUsesOneKeep3WindowAndOriginalAtomicSettlement()
    {
        yield return Win();var choices=run.Salvage.Items.Where(x=>x.Selectable).Take(3).ToArray();Assert.That(choices.Length,Is.EqualTo(3));
        Assert.That(Window.VisibleOptions.Select(x=>x.InstanceId),Is.EquivalentTo(run.Salvage.Items.Select(x=>x.Id)));
        Assert.That(run.ConfirmRewards(choices.Select(x=>x.Id),new[]{0}),Is.True);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(run.Salvage.Closed,Is.True);
        Assert.That(Window.HasOpenWindow,Is.False);Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(4));
        foreach(var original in choices)
        {var part=run.VehicleModel.Inventory.Single(x=>x.Id==original.Id);Assert.That(part.Health,Is.EqualTo(original.Health));Assert.That(part.Ammo,Is.EqualTo(original.Ammo));Assert.That(part.Weapon,Is.EqualTo(original.Weapon));Assert.That(part.RotationQuarterTurns,Is.EqualTo(original.Rotation));Assert.That(part.ContactMask,Is.EqualTo(original.Contacts));}
        int scrap=run.VehicleModel.Scrap;Assert.That(run.ConfirmRewards(choices.Select(x=>x.Id),new[]{0}),Is.False);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(4));
    }
    [UnityTest] public IEnumerator FourthOrOwnedSelectionCannotMutateCanonicalRewardState()
    {
        yield return Win();var ids=run.Salvage.Items.Where(x=>x.Selectable).Take(4).Select(x=>x.Id).ToArray();Assert.That(ids.Length,Is.EqualTo(4));
        int scrap=run.VehicleModel.Scrap;Assert.That(run.ConfirmRewards(ids,new[]{0}),Is.False);
        Assert.That(run.ConfirmRewards(new[]{run.VehicleModel.Parts[0].Id},new[]{0}),Is.False);
        Assert.That(run.Salvage.Closed,Is.False);Assert.That(run.VehicleModel.Inventory,Is.Empty);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        Assert.That(run.ConfirmRewards(Array.Empty<string>(),new[]{0}),Is.True,"Zero salvage is a real choice; independent bonus remains mandatory");
        Assert.That(run.VehicleModel.Inventory.Single().Kind,Is.EqualTo(R3PartKind.BonusModule));
    }
    [UnityTest] public IEnumerator FullInventoryRefusalKeepsTheSameWindowAndCanRetry()
    {
        yield return Win();var window=Window;
        Assert.That(run.VehicleModel.ReceiveSalvage(Enumerable.Range(0,200).Select(_=>new R3PartState(default,R3PartKind.Wheel))),Is.True);
        var ids=run.VehicleModel.Inventory.Select(p=>p.Id).ToArray();int scrap=run.VehicleModel.Scrap;
        Assert.That(run.ConfirmRewards(Array.Empty<string>(),new[]{0}),Is.False);
        Assert.That(Window,Is.SameAs(window));Assert.That(window.HasOpenWindow,Is.True);
        Assert.That(run.VehicleModel.Inventory.Select(p=>p.Id),Is.EqualTo(ids));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(run.Salvage.Closed,Is.False);
        Assert.That(run.DismantleRewardInventoryItem(ids[0]),Is.True);
        Assert.That(run.ConfirmRewards(Array.Empty<string>(),new[]{0}),Is.True);Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(200));
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
