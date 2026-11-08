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
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

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
    [UnityTest] public IEnumerator EscapeClosesRewardInventoryBeforePausingAndCancelsConfirmation()
    {
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State==R1RunState.Combat&&run.PickupGraceRemaining<=0;i++)yield return null;
        if(run.PickupGraceRemaining>0)
            typeof(R1RunController).GetField("pickupGraceDeadline",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(run,Time.time);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        Field("rewardInventoryOpen",true);Field("rewardDismantleConfirmation","pending");
        run.HandleEscape();
        Assert.That((bool)Field("rewardInventoryOpen"),Is.False);
        Assert.That(Field("rewardDismantleConfirmation"),Is.Null);
        Assert.That(run.Paused,Is.False,"Closing the top overlay must not also pause the game.");
        yield return null;run.HandleEscape();
        Assert.That(run.Paused,Is.True,"The next Escape still opens the session pause menu.");
    }
#if ENABLE_INPUT_SYSTEM
    [UnityTest] public IEnumerator CollectedFinalPickupDoesNotConsumeRewardConfirmationKey()
    {
        // Defeated enemies only drop energy while combat is active. The shared
        // fixture disables combat to keep other reward tests deterministic.
        run.Encounter.SetCombatActive(true);
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        run.Encounter.SetCombatActive(false);
        for(int i=0;i<300&&run.Progress.State==R1RunState.Combat&&run.PickupGraceRemaining<=0;i++)yield return null;
        Assert.That(run.PickupGraceRemaining,Is.GreaterThan(0),"Expected the post-battle pickup window.");
        foreach(var pickup in UnityEngine.Object.FindObjectsByType<EnergyPickup>(FindObjectsSortMode.None))pickup.gameObject.SetActive(false);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        Assert.That(run.PickupGraceRemaining,Is.Zero,"Pickup grace must not remain active in rewards.");
        Assert.That((float)typeof(R1RunController).GetField("pickupGraceDeadline",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run),Is.EqualTo(-1));
        Call("TrackRewardLedger",run.Salvage);Call("PortraitRewardSelectBonus",0);Call("ChooseRewardBasket",0);
        var oldSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(oldSettings);
        InputSystem.settings=settings;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));InputSystem.Update();
            typeof(R1RunController).GetMethod("ReadMenuInput",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(run,null);
            Assert.That(run.Salvage.Closed,Is.True,"The first Enter must confirm a valid reward selection.");
        }
        finally {if(keyboard.added)InputSystem.RemoveDevice(keyboard);InputSystem.settings=oldSettings;UnityEngine.Object.Destroy(settings);}
    }
    [UnityTest] public IEnumerator BasketKeysSelectWithoutClaimAndAreIgnoredInInventory()
    {
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State==R1RunState.Combat&&run.PickupGraceRemaining<=0;i++)yield return null;
        if(run.PickupGraceRemaining>0)
            typeof(R1RunController).GetField("pickupGraceDeadline",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(run,Time.time);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));Call("TrackRewardLedger",run.Salvage);
        var baskets=(IReadOnlyList<R4SalvageBasket>)Field("rewardBaskets");
        Assert.That(baskets.Count,Is.GreaterThan(1));
        var oldSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(oldSettings);
        InputSystem.settings=settings;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputSystem.Update();
            hud.ReadRewardInput(keyboard);
            Assert.That((int)Field("selectedBasket"),Is.EqualTo(1));
            Assert.That((HashSet<string>)Field("lootSelection"),Is.EquivalentTo(baskets[1].Items.Select(p=>p.Id)));
            Assert.That(run.Salvage.Closed,Is.False,"Selecting a basket must not claim the rewards.");
            Field("rewardInventoryOpen",true);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q));InputSystem.Update();
            hud.ReadRewardInput(keyboard);
            Assert.That((int)Field("selectedBasket"),Is.EqualTo(1),"Inventory must own its input.");
        }
        finally {if(keyboard.added)InputSystem.RemoveDevice(keyboard);InputSystem.settings=oldSettings;UnityEngine.Object.Destroy(settings);}
    }
#endif
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
