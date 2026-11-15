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
    [UnityTest] public IEnumerator SelectingRecoveredWheelReportsAllRequiredBundleSlots()
    {
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));Call("TrackRewardLedger",run.Salvage);
        var wheel=Choices(run).First(p=>p.Kind==R3PartKind.Wheel);
        Call("PortraitRewardToggleLoot",wheel);
        Assert.That((int)Call("SelectedRewardPartCount",run),Is.EqualTo(2),"The UI must reserve both wheels in the reward package.");
        Call("PortraitRewardSelectBonus",0);
        Assert.That(run.VehicleModel.ReceiveSalvage(Enumerable.Range(0,198).Select(_=>new R3PartState(default,R3PartKind.Gun))),Is.True);
        int funds=run.VehicleModel.Scrap;
        Assert.That((bool)Call("PortraitRewardConfirm",run),Is.False,"Two wheels and the bonus cannot fit in two slots.");
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(198));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(funds));
        Assert.That(run.Salvage.Closed,Is.False);
    }
    [UnityTest] public IEnumerator RerollChargesOnceKeepsBonusAndRequiresANewBasketSelection()
    {
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));Call("TrackRewardLedger",run.Salvage);
        Call("PortraitRewardSelectBonus",0);Call("ChooseRewardBasket",0);
        var old=(IReadOnlyList<R4SalvageBasket>)Field("rewardBaskets");
        Assert.That(old.Count,Is.GreaterThan(0));
        var reroll=typeof(R1RunHUD).GetMethod("RerollRewardBaskets",BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.That(reroll,Is.Not.Null,"The live reward interface must support rerolling.");
        var price=typeof(R1RunDefinition).GetField("RewardBasketRerollCost");Assert.That(price,Is.Not.Null);
        int cost=(int)price.GetValue(run.Definition),before=run.VehicleModel.Scrap;
        Assert.That(before,Is.GreaterThanOrEqualTo(cost));
        Assert.That((bool)reroll.Invoke(hud,new object[]{run}),Is.True);
        var updated=(IReadOnlyList<R4SalvageBasket>)Field("rewardBaskets");
        string Signature(IReadOnlyList<R4SalvageBasket> offers)=>string.Join("|",offers.Select(b=>string.Join(",",b.Items.Select(p=>p.Id).OrderBy(id=>id))));
        Assert.That(Signature(updated),Is.Not.EqualTo(Signature(old)));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-cost));Assert.That(run.Salvage.Closed,Is.False);
        Assert.That((int)Field("selectedBonus"),Is.EqualTo(0));Assert.That((int)Field("selectedBasket"),Is.EqualTo(-1));
        Assert.That((HashSet<string>)Field("lootSelection"),Is.Empty);
        Assert.That(cost,Is.GreaterThan(0));Call("ChooseRewardBasket",0);
        Assert.That(run.VehicleModel.TrySpendScrap(run.VehicleModel.Scrap),Is.True);
        Assert.That((bool)reroll.Invoke(hud,new object[]{run}),Is.False);
        Assert.That(run.VehicleModel.Scrap,Is.Zero);Assert.That((int)Field("selectedBasket"),Is.EqualTo(0));
        Assert.That((string)Field("rewardClaimFeedback"),Does.Contain("Brakuje"));
        run.VehicleModel.Award(100);
        var single=new R4SalvageLedger(3);
        Assert.That(single.Record("only-enemy",new[]{new R4SalvageItem("only-gun","fixture",R3PartKind.Gun,R4WeaponKind.Basic,1)},R3EnemyArchetype.Scout),Is.True);
        typeof(R1RunController).GetProperty("Salvage").GetSetMethod(true).Invoke(run,new object[]{single});
        Call("TrackRewardLedger",single);Call("ChooseRewardBasket",0);int funds=run.VehicleModel.Scrap;
        Assert.That((bool)reroll.Invoke(hud,new object[]{run}),Is.False,"Identical offers must not incur a fee.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(funds));Assert.That((int)Field("selectedBasket"),Is.EqualTo(0));
        Assert.That((string)Field("rewardClaimFeedback"),Does.Contain("Nie pobrano opłaty"));
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
        Field("rewardMissedOpen",true);yield return null;run.HandleEscape();
        Assert.That((bool)Field("rewardMissedOpen"),Is.False);
        Assert.That(run.Paused,Is.False,"Closing missed loot must not also pause the game.");
        yield return null;run.HandleEscape();
        Assert.That(run.Paused,Is.True,"The next Escape still opens the session pause menu.");
    }
    [UnityTest] public IEnumerator SwitchingRewardInventoryFilterCancelsPendingDismantle()
    {
        Assert.That(run.VehicleModel.ReceiveSalvage(new[]{new R3PartState(Vector3Int.zero,R3PartKind.Wheel,.5f)}),Is.True);
        var item=run.VehicleModel.Inventory.Last();int funds=run.VehicleModel.Scrap;
        Field("rewardInventoryPage",4);Field("rewardDismantleConfirmation",item.Id);
        Call("ToggleRewardInventoryFilter");
        Assert.That((bool)Field("rewardInventoryDamagedOnly"),Is.True);
        Assert.That((int)Field("rewardInventoryPage"),Is.Zero);
        Assert.That(Field("rewardDismantleConfirmation"),Is.Null);
        Assert.That(ContainsId(run.VehicleModel,item.Id),Is.True);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(funds));
        yield return null;
    }
    [UnityTest] public IEnumerator CancelDismantleKeepsThePartWalletAndInventoryOpen()
    {
        Assert.That(run.VehicleModel.ReceiveSalvage(new[]{new R3PartState(Vector3Int.zero,R3PartKind.Wheel,.5f)}),Is.True);
        var item=run.VehicleModel.Inventory.Last();int funds=run.VehicleModel.Scrap;
        Field("rewardInventoryOpen",true);Field("rewardDismantleConfirmation",item.Id);
        Call("CancelRewardDismantle");
        Assert.That(Field("rewardDismantleConfirmation"),Is.Null);
        Assert.That((bool)Field("rewardInventoryOpen"),Is.True);
        Assert.That(ContainsId(run.VehicleModel,item.Id),Is.True);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(funds));
        Assert.That((string)Field("rewardInventoryFeedback"),Does.Contain("Anulowano"));
        yield return null;
    }
#if ENABLE_INPUT_SYSTEM
    [UnityTest] public IEnumerator MouseBoundPickupSkipIgnoresClicksConsumedByUi()
    {
        run.Encounter.SetCombatActive(true);
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        run.Encounter.SetCombatActive(false);
        for(int i=0;i<300&&run.Progress.State==R1RunState.Combat&&run.PickupGraceRemaining<=0;i++)yield return null;
        Assert.That(run.PickupGraceRemaining,Is.GreaterThan(0));
        var profile=R1ControlProfile.Current;profile.ResetDefaults();
        try
        {
            Assert.That(profile.TryBind("SkipPickup","MouseBack"),Is.True);
            var handler=typeof(R1RunHUD).GetMethod("TryPickupSkipMouse",BindingFlags.Static|BindingFlags.NonPublic);Assert.That(handler,Is.Not.Null);
            var used=new Event{type=EventType.MouseDown,button=3};used.Use();handler.Invoke(null,new object[]{run,used});
            Assert.That(run.PickupGraceRemaining,Is.GreaterThan(0));
            handler.Invoke(null,new object[]{run,new Event{type=EventType.MouseDown,button=3}});
            Assert.That(run.PickupGraceRemaining,Is.Zero);
        }
        finally {profile.ResetDefaults();}
    }
    [UnityTest] public IEnumerator ConfiguredKeySkipsPickupWindowAndUpdatesButtonHint()
    {
        run.Encounter.SetCombatActive(true);
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        run.Encounter.SetCombatActive(false);
        for(int i=0;i<300&&run.Progress.State==R1RunState.Combat&&run.PickupGraceRemaining<=0;i++)yield return null;
        Assert.That(run.PickupGraceRemaining,Is.GreaterThan(0));
        var profile=R1ControlProfile.Current;profile.ResetDefaults();Assert.That(profile.TryBind("SkipPickup","K"),Is.True);
        var caption=typeof(R1RunHUD).GetMethod("PickupSkipCaption",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(caption,Is.Not.Null);Assert.That((string)caption.Invoke(null,null),Does.Contain("K"));
        var oldSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(oldSettings);
        InputSystem.settings=settings;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            var input=typeof(R1RunController).GetMethod("ReadMenuInput",BindingFlags.Instance|BindingFlags.NonPublic);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));InputSystem.Update();input.Invoke(run,null);
            Assert.That(run.PickupGraceRemaining,Is.GreaterThan(0),"Changing the binding replaces the old action key.");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.K));InputSystem.Update();input.Invoke(run,null);
            Assert.That(run.PickupGraceRemaining,Is.Zero);
        }
        finally {profile.ResetDefaults();if(keyboard.added)InputSystem.RemoveDevice(keyboard);InputSystem.settings=oldSettings;UnityEngine.Object.Destroy(settings);}
    }
    [UnityTest] public IEnumerator PickupWindowCountsRemainingPartsAndEnergyCubes()
    {
        foreach(var pickup in UnityEngine.Object.FindObjectsByType<EnergyPickup>(FindObjectsSortMode.None))pickup.gameObject.SetActive(false);
        foreach(var pickup in UnityEngine.Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None))pickup.gameObject.SetActive(false);
        var parts=Enumerable.Range(0,3).Select(i=>new GameObject("Pickup part fixture "+i)).ToArray();
        var energy=Enumerable.Range(0,2).Select(i=>new GameObject("Pickup energy fixture "+i)).ToArray();
        foreach(var p in parts)p.AddComponent<R3PhysicalLoot>();foreach(var e in energy)e.AddComponent<EnergyPickup>();
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.PickupGraceRemaining<=0;i++)yield return null;
        var partCount=typeof(R1RunController).GetProperty("PickupPartsRemaining");var energyCount=typeof(R1RunController).GetProperty("PickupEnergyRemaining");
        Assert.That(partCount,Is.Not.Null);Assert.That(energyCount,Is.Not.Null);
        Assert.That((int)partCount.GetValue(run),Is.EqualTo(3));Assert.That((int)energyCount.GetValue(run),Is.EqualTo(2));
        parts[0].SetActive(false);energy[0].SetActive(false);yield return null;
        Assert.That((int)partCount.GetValue(run),Is.EqualTo(2));Assert.That((int)energyCount.GetValue(run),Is.EqualTo(1));
        foreach(var p in parts)UnityEngine.Object.Destroy(p);foreach(var e in energy)UnityEngine.Object.Destroy(e);
    }
    [UnityTest] public IEnumerator PlayerCanSkipThePickupWindowButNotWhilePaused()
    {
        run.Encounter.SetCombatActive(true);
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        run.Encounter.SetCombatActive(false);
        for(int i=0;i<300&&run.Progress.State==R1RunState.Combat&&run.PickupGraceRemaining<=0;i++)yield return null;
        Assert.That(run.PickupGraceRemaining,Is.GreaterThan(0));
        var skip=typeof(R1RunController).GetMethod("SkipPickupGrace");Assert.That(skip,Is.Not.Null);
        run.TogglePause();Assert.That((bool)skip.Invoke(run,null),Is.False);Assert.That(run.PickupGraceRemaining,Is.GreaterThan(0));
        run.TogglePause();Assert.That((bool)skip.Invoke(run,null),Is.True);Assert.That(run.PickupGraceRemaining,Is.Zero);
        Assert.That((bool)skip.Invoke(run,null),Is.False,"The transition can only be requested once.");
    }
    [UnityTest] public IEnumerator PauseDoesNotSkipThePostBattlePickupWindow()
    {
        run.Encounter.SetCombatActive(true);
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        run.Encounter.SetCombatActive(false);
        for(int i=0;i<300&&run.Progress.State==R1RunState.Combat&&run.PickupGraceRemaining<=0;i++)yield return null;
        Assert.That(run.PickupGraceRemaining,Is.GreaterThan(0));run.TogglePause();Assert.That(run.Paused,Is.True);
        var oldSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(oldSettings);
        InputSystem.settings=settings;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));InputSystem.Update();
            typeof(R1RunController).GetMethod("ReadMenuInput",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(run,null);
            Assert.That(run.PickupGraceRemaining,Is.GreaterThan(0),"Pausing must preserve time to collect physical drops.");
        }
        finally {if(keyboard.added)InputSystem.RemoveDevice(keyboard);InputSystem.settings=oldSettings;UnityEngine.Object.Destroy(settings);}
    }
    [UnityTest] public IEnumerator PausedRewardScreenDoesNotConfirmBehindPauseMenu()
    {
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        Call("TrackRewardLedger",run.Salvage);Call("PortraitRewardSelectBonus",0);Call("ChooseRewardBasket",0);
        var oldSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(oldSettings);
        InputSystem.settings=settings;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            run.TogglePause();Assert.That(run.Paused,Is.True);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));InputSystem.Update();
            typeof(R1RunController).GetMethod("ReadMenuInput",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(run,null);
            Assert.That(run.Salvage.Closed,Is.False,"The pause menu must prevent actions on the screen behind it.");
            hud.ReadRewardInput(keyboard);Assert.That(run.Salvage.Closed,Is.False,"The reward input entry point must also respect pause.");
            run.TogglePause();InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));InputSystem.Update();
            typeof(R1RunController).GetMethod("ReadMenuInput",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(run,null);
            Assert.That(run.Salvage.Closed,Is.True,"The reward can still be confirmed after resuming.");
        }
        finally {if(keyboard.added)InputSystem.RemoveDevice(keyboard);InputSystem.settings=oldSettings;UnityEngine.Object.Destroy(settings);}
    }
    [UnityTest] public IEnumerator ExtraBasketPageKeysSelectThatPageAndOnlyOneBasket()
    {
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        run.VehicleModel.Parts.Add(new R3PartState(new Vector3Int(50,0,0),R3PartKind.SalvageOptimizer){Health=1});
        Call("TrackRewardLedger",run.Salvage);
        var baskets=(IReadOnlyList<R4SalvageBasket>)Field("rewardBaskets");
        Assert.That(baskets.Count,Is.GreaterThanOrEqualTo(4));
        Field("basketPage",1);
        var oldSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(oldSettings);
        InputSystem.settings=settings;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q));InputSystem.Update();hud.ReadRewardInput(keyboard);
            Assert.That((int)Field("selectedBasket"),Is.EqualTo(3));
            Assert.That((HashSet<string>)Field("lootSelection"),Is.EquivalentTo(baskets[3].Items.Select(p=>p.Id)));
            Assert.That(run.Salvage.Closed,Is.False);
        }
        finally {if(keyboard.added)InputSystem.RemoveDevice(keyboard);InputSystem.settings=oldSettings;UnityEngine.Object.Destroy(settings);}
    }
    [UnityTest] public IEnumerator LargeBasketBonusesLoadPagesOnDemandWithoutChangingSeededOffers()
    {
        var advance=typeof(R1RunHUD).GetMethod("AdvanceBasketPage",BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.That(advance,Is.Not.Null,"Reward paging must not generate a huge offer list up front.");
        var ledger=new R4SalvageLedger(8);
        var items=Enumerable.Range(0,40).Select(i=>new R4SalvageItem("page-"+i,"foe",R3PartKind.Gun,R4WeaponKind.Basic,1)).ToArray();
        Assert.That(ledger.Record("foe",items,R3EnemyArchetype.Scout),Is.True);
        run.VehicleModel.Parts.Add(new R3PartState(new Vector3Int(50,0,0),R3PartKind.SalvageOptimizer){Health=1});
        run.VehicleModel.Economy.SalvageOptimizerExtraOffersPerUnit=int.MaxValue;
        Call("TrackRewardLedger",ledger);
        var baskets=(IReadOnlyList<R4SalvageBasket>)Field("rewardBaskets");
        Assert.That(baskets.Count,Is.EqualTo(6),"Only the first two pages should be materialized initially.");
        Call("AdvanceBasketPage");Assert.That((int)Field("basketPage"),Is.EqualTo(1));
        Assert.That(baskets.Count,Is.EqualTo(9));
        Call("AdvanceBasketPage");Assert.That((int)Field("basketPage"),Is.EqualTo(2));
        Assert.That(baskets.Count,Is.EqualTo(12));
        int stage=run.Progress.EncounterIndex;int seed=run.Seed^(stage*397);
        string Signature(R4SalvageBasket basket)=>basket.Archetype+":"+string.Join(",",basket.Items.Select(p=>p.Id).OrderBy(id=>id));
        Assert.That(baskets.Select(Signature),Is.EqualTo(ledger.CreateBasketOffers(seed,stage,12).Select(Signature)));
        Call("ChooseRewardBasket",6);
        Assert.That((HashSet<string>)Field("lootSelection"),Is.EquivalentTo(baskets[6].Items.Select(p=>p.Id)));
        yield return null;
    }
    [UnityTest] public IEnumerator ChangedLootInvalidatesPreparedBasketsButKeepsBonusAndOpenInventory()
    {
        var ledger=new R4SalvageLedger(3);
        var items=Enumerable.Range(0,20).Select(i=>new R4SalvageItem("change-"+i,"foe",R3PartKind.Gun,R4WeaponKind.Basic,1)).ToArray();
        Assert.That(ledger.Record("foe",items,R3EnemyArchetype.Scout),Is.True);
        run.VehicleModel.Parts.Add(new R3PartState(new Vector3Int(50,0,0),R3PartKind.SalvageOptimizer){Health=1});
        run.VehicleModel.Economy.SalvageOptimizerExtraOffersPerUnit=20;
        Call("TrackRewardLedger",ledger);Call("PortraitRewardSelectBonus",0);Call("ChooseRewardBasket",0);
        var old=(IReadOnlyList<R4SalvageBasket>)Field("rewardBaskets");string unavailable=old[0].Items[0].Id;
        Field("rewardInventoryOpen",true);Assert.That(ledger.MarkPhysicalDrop(unavailable),Is.True);
        Call("TrackRewardLedger",ledger);
        var updated=(IReadOnlyList<R4SalvageBasket>)Field("rewardBaskets");
        Assert.That(updated,Is.Not.SameAs(old));
        Assert.That(updated.SelectMany(b=>b.Items).Any(item=>item.Id==unavailable),Is.False);
        Assert.That((int)Field("selectedBasket"),Is.EqualTo(-1));Assert.That((HashSet<string>)Field("lootSelection"),Is.Empty);
        Assert.That((int)Field("selectedBonus"),Is.Zero);Assert.That((bool)Field("rewardInventoryOpen"),Is.True);
        Assert.That((string)Field("rewardClaimFeedback"),Does.Contain("Wybierz"));
        Call("AdvanceBasketPage");Call("AdvanceBasketPage");
        Assert.That(updated.SelectMany(b=>b.Items).Any(item=>item.Id==unavailable),Is.False);
        yield return null;
    }
    [UnityTest] public IEnumerator CollectingAlreadyExcludedLootDoesNotResetBasketChoice()
    {
        var ledger=new R4SalvageLedger(3);
        var items=Enumerable.Range(0,8).Select(i=>new R4SalvageItem("pickup-"+i,"foe",R3PartKind.Gun,R4WeaponKind.Basic,1)).ToArray();
        Assert.That(ledger.Record("foe",items,R3EnemyArchetype.Scout),Is.True);
        Assert.That(ledger.MarkPhysicalDrop(items[0].Id),Is.True);
        Call("TrackRewardLedger",ledger);Call("ChooseRewardBasket",0);
        var before=(IReadOnlyList<R4SalvageBasket>)Field("rewardBaskets");var selected=(HashSet<string>)Field("lootSelection");
        var ids=selected.ToArray();Assert.That(ledger.TryCollectPhysical(run.VehicleModel,items[0].Id),Is.True);
        Call("TrackRewardLedger",ledger);
        Assert.That(Field("rewardBaskets"),Is.SameAs(before));Assert.That((int)Field("selectedBasket"),Is.Zero);
        Assert.That(selected,Is.EquivalentTo(ids));
        yield return null;
    }
    [UnityTest] public IEnumerator LootChangeCannotConsumeTheSameConfirmationKeyPress()
    {
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));Call("TrackRewardLedger",run.Salvage);
        Call("PortraitRewardSelectBonus",0);Call("ChooseRewardBasket",0);
        var ledger=run.Salvage;
        foreach(var item in ledger.RewardItems.Where(p=>p.Selectable).ToArray())Assert.That(ledger.MarkPhysicalDrop(item.Id),Is.True);
        var oldSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(oldSettings);
        InputSystem.settings=settings;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));InputSystem.Update();
            hud.ReadRewardInput(keyboard);
            Assert.That(ledger.Closed,Is.False,"The input that discovers changed loot must not also confirm a different reward.");
            Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));Assert.That((int)Field("selectedBasket"),Is.EqualTo(-1));
            yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));InputSystem.Update();hud.ReadRewardInput(keyboard);
            Assert.That(ledger.Closed,Is.True,"A fresh confirmation may accept the now-empty basket list.");
        }
        finally{if(keyboard.added)InputSystem.RemoveDevice(keyboard);InputSystem.settings=oldSettings;UnityEngine.Object.Destroy(settings);}
    }
    [UnityTest] public IEnumerator IdenticalSeedRerollFindsAnotherRealBasketAndChargesOnce()
    {
        var ledger=run.Salvage;
        var items=Enumerable.Range(0,8).Select(i=>new R4SalvageItem(Guid.NewGuid().ToString("N"),"fixture",R3PartKind.Gun,R4WeaponKind.Basic,1)).ToArray();
        Assert.That(ledger.Record("fallback-enemy",items,R3EnemyArchetype.Scout),Is.True);
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        Call("TrackRewardLedger",ledger);
        int seed=unchecked((run.Seed^(run.Progress.EncounterIndex*397))+7919);
        Field("rewardBasketSeed",seed);Call("RebuildRewardBasketStream",ledger,run,seed);
        Call("PortraitRewardSelectBonus",0);Call("ChooseRewardBasket",0);
        string Signature(R4SalvageBasket basket)=>basket.Archetype+":"+string.Join(",",basket.Items.Select(item=>item.Id).OrderBy(id=>id));
        var previous=((IReadOnlyList<R4SalvageBasket>)Field("rewardBaskets")).Take(3).Select(Signature).ToArray();
        int before=run.VehicleModel.Scrap;
        Assert.That((bool)Call("RerollRewardBaskets",run),Is.True,"An identical first draw must fall back to an actual alternative.");
        Assert.That((int)Field("rewardBasketSeed"),Is.EqualTo(seed),"The fallback must use the candidate draw, rather than hoping a later seed differs.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-run.Definition.RewardBasketRerollCost));
        var next=(IReadOnlyList<R4SalvageBasket>)Field("rewardBaskets");
        Assert.That(next.Count,Is.EqualTo(3),"The fallback must respect the configured offer count.");
        Assert.That(previous,Does.Not.Contain(Signature(next[0])),"The first new offer must differ from every currently visible offer.");
        var expected=next[0].Items.Select(item=>item.Id).ToArray();
        var eligible=ledger.RewardItems.Where(item=>item.Selectable).ToArray();
        Assert.That(expected.All(id=>eligible.Any(item=>item.Id==id)),Is.True,"The fallback must only use real enemy parts.");
        string unavailable=eligible.First(item=>!expected.Contains(item.Id)).Id;
        Assert.That(ledger.MarkPhysicalDrop(unavailable),Is.True);Call("TrackRewardLedger",ledger);
        next=(IReadOnlyList<R4SalvageBasket>)Field("rewardBaskets");
        Assert.That(next.SelectMany(basket=>basket.Items).Any(item=>item.Id==unavailable),Is.False,"Refreshing a fallback must discard unavailable parts.");
        Assert.That((int)Field("rewardBasketSeed"),Is.EqualTo(seed));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-run.Definition.RewardBasketRerollCost));
        yield return null;expected=next[0].Items.Select(item=>item.Id).ToArray();
        Call("ChooseRewardBasket",0);Assert.That((bool)Call("ConfirmSelectedRewards",run),Is.True);
        Assert.That(expected.All(id=>run.VehicleModel.Inventory.Any(part=>part.Id==id)),Is.True);
        Assert.That((bool)Call("ConfirmSelectedRewards",run),Is.False,"The chosen parts must not be granted twice.");
        yield return null;
    }
    [UnityTest] public IEnumerator LargeBasketRerollAndLaterPageCanBeClaimedOnce()
    {
        var ledger=run.Salvage;
        var items=Enumerable.Range(0,40).Select(i=>new R4SalvageItem(Guid.NewGuid().ToString("N"),"foe",R3PartKind.Gun,R4WeaponKind.Basic,1)).ToArray();
        Assert.That(ledger.Record("foe",items,R3EnemyArchetype.Scout),Is.True);
        foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        for(int i=0;i<300&&run.Progress.State!=R1RunState.Salvage;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        run.VehicleModel.Parts.Add(new R3PartState(new Vector3Int(50,0,0),R3PartKind.SalvageOptimizer){Health=1});
        run.VehicleModel.Economy.SalvageOptimizerExtraOffersPerUnit=int.MaxValue;
        run.VehicleModel.Award(1000);Call("TrackRewardLedger",ledger);
        Call("PortraitRewardSelectBonus",0);Call("AdvanceBasketPage");Call("AdvanceBasketPage");Call("ChooseRewardBasket",6);
        var old=(IReadOnlyList<R4SalvageBasket>)Field("rewardBaskets");Assert.That(old.Count,Is.EqualTo(12));
        string Signature(R4SalvageBasket basket)=>basket.Archetype+":"+string.Join(",",basket.Items.Select(p=>p.Id).OrderBy(id=>id));
        var visibleBefore=old.Skip(6).Take(3).Select(Signature).OrderBy(s=>s).ToArray();
        int before=run.VehicleModel.Scrap;Assert.That((bool)Call("RerollRewardBaskets",run),Is.True);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-run.Definition.RewardBasketRerollCost));
        Assert.That((int)Field("selectedBonus"),Is.Zero);Assert.That((int)Field("selectedBasket"),Is.EqualTo(-1));
        var next=(IReadOnlyList<R4SalvageBasket>)Field("rewardBaskets");
        Assert.That(next.Count,Is.EqualTo(6),"A reroll returns to page one; it must not regenerate every previously visited page.");
        Assert.That(next.Take(3).Select(Signature).OrderBy(s=>s),Is.Not.EqualTo(visibleBefore),"The player must see different offers immediately after paying.");
        string unavailable=next[0].Items[0].Id;int rerollSeed=(int)Field("rewardBasketSeed");
        Assert.That(ledger.MarkPhysicalDrop(unavailable),Is.True);Call("TrackRewardLedger",ledger);
        next=(IReadOnlyList<R4SalvageBasket>)Field("rewardBaskets");
        Assert.That(next.Select(Signature),Is.EqualTo(ledger.CreateBasketOffers(rerollSeed,run.Progress.EncounterIndex,6).Select(Signature)));
        Assert.That(next.SelectMany(b=>b.Items).Any(item=>item.Id==unavailable),Is.False);
        Assert.That((int)Field("basketRerollCount"),Is.EqualTo(1));Assert.That((int)Field("selectedBonus"),Is.Zero);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-run.Definition.RewardBasketRerollCost));
        Call("ChooseRewardBasket",0);Assert.That((int)Field("selectedBasket"),Is.EqualTo(-1));
        Assert.That((bool)Call("RerollRewardBaskets",run),Is.False);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-run.Definition.RewardBasketRerollCost));
        yield return null;
        Call("AdvanceBasketPage");Call("AdvanceBasketPage");Call("ChooseRewardBasket",6);
        var ids=next[6].Items.Select(item=>item.Id).ToArray();
        Assert.That((bool)Call("ConfirmSelectedRewards",run),Is.True);
        Assert.That(ledger.Closed,Is.True);
        Assert.That(ids.All(id=>run.VehicleModel.Inventory.Count(part=>part.Id==id)==1),Is.True);
        Assert.That((bool)Call("ConfirmSelectedRewards",run),Is.False);
        yield return null;
    }
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
        foreach(var pickup in UnityEngine.Object.FindObjectsByType<R3PhysicalLoot>(FindObjectsSortMode.None))pickup.gameObject.SetActive(false);
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
