using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class UI943PortraitRewardsActionsTests
{
    R1RunController run;
    R1RunHUD hud;
    object Call(string name,params object[] args)
    {var m=typeof(R1RunHUD).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic);Assert.That(m,Is.Not.Null,name);return m.Invoke(hud,args);}
    object Field(string name)=>typeof(R1RunHUD).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
    void Field(string name,object value)=>typeof(R1RunHUD).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(hud,value);
    static R4SalvageItem[] Choices(R1RunController r)=>r.Salvage.Items.Where(p=>p.Selectable).ToArray();
    static bool ContainsId(R3VehicleModel model,string id)=>model.Inventory.Any(p=>p.Id==id);
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);hud=run.GetComponent<R1RunHUD>();
        Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.StartRun()&&run.ChooseRoute(0),Is.True);
        for(int i=0;i<300&&run.Transitioning;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));run.Encounter.SetCombatActive(false);
    }
    [UnityTest] public IEnumerator PagesCapacityDismantleAndConfirmUseRealLedgerAndInventory()
    {
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));Call("TrackRewardLedger",run.Salvage);
        var choices=Choices(run);Assert.That(choices.Length,Is.GreaterThanOrEqualTo(8),"Fixture wymaga łupów na dwóch stronach.");
        Call("PortraitRewardToggleLoot",choices[0]);Call("PortraitRewardToggleLoot",choices[1]);
        Call("PortraitRewardMoveLootPage",1,choices.Length);Assert.That((int)Field("lootPage"),Is.EqualTo(1));
        Call("PortraitRewardToggleLoot",choices[6]);Call("PortraitRewardToggleLoot",choices[7]);
        var selected=(HashSet<string>)Field("lootSelection");
        Assert.That(selected,Is.EquivalentTo(new[]{choices[0].Id,choices[1].Id,choices[6].Id}),"Maksymalnie trzy oryginalne ID, niezależnie od strony.");
        Assert.That((bool)Call("PortraitRewardConfirm",run),Is.False,"Bonus jest wymagany.");
        Assert.That(run.Salvage.Closed,Is.False);
        Call("PortraitRewardSelectBonus",0);
        Assert.That(run.VehicleModel.ReceiveSalvage(Enumerable.Range(0,197).Select(_=>new R3PartState(default,R3PartKind.Wheel))),Is.True);
        int before=run.VehicleModel.Scrap;
        Assert.That((bool)Call("PortraitRewardConfirm",run),Is.False,"Cztery nagrody nie mieszczą się w trzech miejscach.");
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(197));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before));
        var owned=run.VehicleModel.Inventory[0];string id=owned.Id;int value=run.VehicleModel.DismantleValue(owned);
        Assert.That((bool)Call("PortraitRewardDismantle",run,id),Is.False,"Pierwsze kliknięcie wymaga potwierdzenia.");
        Assert.That(ContainsId(run.VehicleModel,id),Is.True);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before));
        Assert.That((bool)Call("PortraitRewardDismantle",run,id),Is.True);
        Assert.That(ContainsId(run.VehicleModel,id),Is.False);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before+value));
        int scrap=run.VehicleModel.Scrap,preview=run.Salvage.PreviewScrap(run.VehicleModel,selected);
        Assert.That((bool)Call("PortraitRewardConfirm",run),Is.True);
        Assert.That(run.Salvage.Closed,Is.True);Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(200));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap+preview));
        foreach(var index in new[]{0,1,6})Assert.That(ContainsId(run.VehicleModel,choices[index].Id),Is.True,"Zachowaj wybrane ID i rzeczywiste egzemplarze.");
        Assert.That(ContainsId(run.VehicleModel,choices[7].Id),Is.False);
        Assert.That((bool)Call("PortraitRewardConfirm",run),Is.False,"Nie wolno wypłacić nagród dwukrotnie.");
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
