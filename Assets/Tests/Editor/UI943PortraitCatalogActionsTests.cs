using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class UI943PortraitCatalogActionsTests
{
    [Test] public void GarageCategoryShortcutFollowsProfileAndDoesNotStealKeypadAliases()
    {
        var method=typeof(R1RunHUD).GetMethod("QuickCategoryShortcutForProfile",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var profile=new R1ControlProfile(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"RRL-categories-"+Guid.NewGuid()+".json"));
        object Match(KeyCode key)=>method.Invoke(null,new object[]{new Event{type=EventType.KeyDown,keyCode=key},profile});
        Assert.That(Match(KeyCode.Alpha6),Is.EqualTo(1));Assert.That(Match(KeyCode.Keypad6),Is.EqualTo(1));
        Assert.That(profile.TryBind("GarageFrame","Numpad6"),Is.True);Assert.That(Match(KeyCode.Keypad6),Is.Null);
        Assert.That(profile.TryBind("GarageFrame","Home"),Is.True);Assert.That(profile.TryBind("GarageWheels","K"),Is.True);
        Assert.That(Match(KeyCode.K),Is.EqualTo(1));Assert.That(Match(KeyCode.Alpha6),Is.Null);Assert.That(Match(KeyCode.Keypad6),Is.Null);
        Assert.That(profile.TryBind("GarageWheels","MouseBack"),Is.True);
        Assert.That(method.Invoke(null,new object[]{new Event{type=EventType.MouseDown,button=3},profile}),Is.EqualTo(1));
    }
    [Test] public void GarageSearchShortcutUsesProfileAndKeepsLegacyKeypadAlias()
    {
        var method=typeof(R1RunHUD).GetMethod("QuickSearchShortcut",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var profile=new R1ControlProfile(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"RRL-search-"+Guid.NewGuid()+".json"));
        bool Match(KeyCode key)=> (bool)method.Invoke(null,new object[]{new Event{type=EventType.KeyDown,keyCode=key},profile});
        Assert.That(Match(KeyCode.Slash),Is.True);Assert.That(Match(KeyCode.KeypadDivide),Is.True);
        Assert.That(profile.TryBind("GarageFrame","NumpadDivide"),Is.True);
        Assert.That(Match(KeyCode.KeypadDivide),Is.False,"The legacy alias must not take an already assigned key.");
        Assert.That(profile.TryBind("GarageFrame","Home"),Is.True);
        Assert.That(profile.TryBind("GarageSearch","K"),Is.True);
        Assert.That(Match(KeyCode.K),Is.True);Assert.That(Match(KeyCode.Slash),Is.False);Assert.That(Match(KeyCode.KeypadDivide),Is.False);
        Assert.That((bool)method.Invoke(null,new object[]{new Event{type=EventType.KeyDown,keyCode=KeyCode.K,control=true},profile}),Is.False);
        Assert.That(profile.TryBind("GarageSearch","MouseBack"),Is.True);
        var mouse=new Event{type=EventType.MouseDown,button=3,modifiers=EventModifiers.None};
        Assert.That(R1ControlProfile.FromEvent(mouse),Is.EqualTo("MouseBack"),$"type={mouse.type}, raw={mouse.rawType}, button={mouse.button}, modifiers={mouse.modifiers}");
        Assert.That((bool)method.Invoke(null,new object[]{mouse,profile}),Is.True);
    }
    static object Invoke(R1RunHUD hud,string name,params object[] args)
    {
        var method=typeof(R1RunHUD).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null,$"Brakuje operacji katalogu: {name}");
        return method.Invoke(hud,args);
    }
    static void Field(R1RunHUD hud,string name,object value)=>typeof(R1RunHUD).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(hud,value);
    static object Field(R1RunHUD hud,string name)=>typeof(R1RunHUD).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
    static bool ContainsId(R3VehicleModel model,string id)=>model.Inventory.Any(p=>p.Id==id);
    [UnityTest] public IEnumerator GarageCatalogShowsAndBuysFiveBlockBundle()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        var hud=run.GetComponent<R1RunHUD>();int price=run.VehicleModel.Price(R3PartKind.Armor),before=run.VehicleModel.Scrap;
        var caption=(string)Invoke(hud,"CatalogPurchaseCaption",run.VehicleModel,R3PartKind.Armor,true,price);
        Assert.That(caption,Does.Contain("×5"));
        Invoke(hud,"PortraitCatalogPurchase",run,R3PartKind.Armor,R4WeaponKind.Basic);
        Assert.That(run.VehicleModel.Inventory.Count(p=>p.Kind==R3PartKind.Armor),Is.EqualTo(5));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-price));
        Assert.That(run.VehicleModel.Inventory.Select(p=>p.Id).Contains(run.Garage.SelectedInventoryId),Is.True);
        yield return new ExitPlayMode();
    }
    // Keep candidate search outside a coroutine: closures allocated before
    // EnterPlayMode can be lost during the editor domain reload.
    static Vector3Int AvailableOwnedMountCell(R3VehicleModel model,R3PartState item)
    {
        foreach(var mounted in model.Parts)
            foreach(var direction in R3VehicleModel.Neighbours)
            {
                var cell=mounted.Cell+direction;
                if(model.OwnedPlacementError(item.Id,cell,item.RotationQuarterTurns)==null)return cell;
            }
        Assert.Fail("The garage fixture needs a reachable valid mounting cell.");
        return default;
    }

    [UnityTest] public IEnumerator GarageActionFeedbackRemainsVisibleBeforePlacementHintReturns()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        var garage=run.Garage;var hud=run.GetComponent<R1RunHUD>();
        garage.UseInventory=false;garage.SelectedInventoryId=null;garage.SelectedPart=R3PartKind.Armor;
        garage.Cell=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Core).Cell;
        typeof(R3GarageView).GetProperty("HasTarget").GetSetMethod(true).Invoke(garage,new object[]{true});
        Assert.That(garage.RemovePartAt(garage.Cell),Is.False);
        Assert.That(garage.Message,Does.Contain("rdzenia"));
        Assert.That((string)Invoke(hud,"SimpleGarageFeedback",run),Does.Contain("rdzenia"),
            "The reason for a rejected action must not immediately disappear behind the ghost hint.");
        yield return new WaitForSecondsRealtime(3.2f);
        Assert.That((string)Invoke(hud,"SimpleGarageFeedback",run),Does.Contain(garage.PlacementIssue),
            "The current placement issue must return after the action feedback expires.");
    }

    [UnityTest] public IEnumerator RotationFailureIsVisibleWhileMountingAnotherPart()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        var garage=run.Garage;var core=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Core);
        garage.UseInventory=false;garage.SelectedInventoryId=null;garage.SelectedPart=R3PartKind.Armor;
        garage.Cell=core.Cell;
        typeof(R3GarageView).GetProperty("HasTarget").GetSetMethod(true).Invoke(garage,new object[]{true});
        Assert.That(garage.RotatePartAt(core.Cell,Vector3Int.up),Is.False);
        Assert.That((string)Invoke(run.GetComponent<R1RunHUD>(),"SimpleGarageFeedback",run),
            Does.Contain("obrócić rdzenia"),"A failed Shift-rotation must explain the protected core.");
        yield return null;
    }

    [TestCase(false,5,2,false,"Brakuje 3 Z")]
    [TestCase(false,5,5,false,"5 Z")]
    [TestCase(true,0,0,false,"0 Z")]
    [TestCase(false,5,2,true,"Brakuje 3 Z")]
    [TestCase(true,0,0,true,"0 Z")]
    public void WreckMountCaptionExplainsAffordability(bool creative,int cost,int scrap,bool portrait,string expected)
    {
        var method=typeof(R1RunHUD).GetMethod("CatalogMountCaption",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var wreck=new R3PartState(default,R3PartKind.Armor){Health=0};
        Assert.That((string)method.Invoke(null,new object[]{wreck,creative,cost,scrap,portrait}),Does.Contain(expected));
    }

    [Test] public void QuickStackRepairDistinguishesEmptyHealthyAndUnrepairable()
    {
        var method=typeof(R1RunHUD).GetMethod("QuickStackRepairCaption",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        string Label(R3PartState[] parts)=>(string)method.Invoke(null,new object[]{parts,0,0,0});
        Assert.That(Label(Array.Empty<R3PartState>()),Does.Contain("pusty"));
        Assert.That(Label(new[]{new R3PartState(default,R3PartKind.Armor){Health=1}}),Does.Contain("sprawny"));
        Assert.That(Label(new[]{new R3PartState(default,R3PartKind.Armor){Health=0}}),Does.Contain("niedostępny"));
    }

    [TestCase(false,R3PartKind.Armor,true,5,20,false,"Kup i wybierz")]
    [TestCase(false,R3PartKind.Armor,true,5,3,false,"Brakuje 2 Z")]
    [TestCase(false,R3PartKind.Armor,true,5,20,true,"Magazyn pełny")]
    [TestCase(false,R3PartKind.Gun,false,20,100,false,"Pokonaj")]
    [TestCase(true,R3PartKind.Armor,true,0,0,false,"Dodaj i wybierz")]
    public void QuickPurchaseExplainsSelectionAndUnavailableActions(bool creative,R3PartKind kind,bool unlocked,int price,int scrap,bool full,string expected)
    {
        var method=typeof(R1RunHUD).GetMethod("QuickPurchaseCaption",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        Assert.That((string)method.Invoke(null,new object[]{creative,kind,unlocked,price,scrap,full}),Does.Contain(expected));
    }

    [TestCase(true,"stary błąd",null)]
    [TestCase(false,"Pojazd jest zbyt szeroki.","Pojazd jest zbyt szeroki.")]
    [TestCase(false,null,"Nie udało się uruchomić aktywności.")]
    [TestCase(false,"   ","Nie udało się uruchomić aktywności.")]
    public void ActivityLaunchFeedbackUsesOnlyCurrentResult(bool started,string detail,string expected)
    {
        var method=typeof(R1RunHUD).GetMethod("ActivityLaunchResult",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        Assert.That((string)method.Invoke(null,new object[]{started,detail}),Is.EqualTo(expected));
    }

    [UnityTest] public IEnumerator FailedActivityLaunchDisplaysCurrentReasonWithoutReusingOldMessage()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;
        Assert.That(run!=null,Is.True,"The scene must have a live run controller.");
        Assert.That(run.Progress,Is.Not.Null,"The run controller must complete initialization.");
        Assert.That(run.OpenSandboxGarage(),Is.True,"The fixture must enter its isolated workshop.");
        Assert.That(run.Garage!=null,Is.True,"The workshop must have a garage view.");
        var hud=run.GetComponent<R1RunHUD>();Assert.That(hud!=null,Is.True,"The workshop must have its HUD.");
        run.Garage.Message="Stary komunikat";
        Invoke(hud,"AttemptGarageActivity",run,new Func<bool>(()=>false));
        Assert.That((string)Field(hud,"activitySaveFeedback"),Is.EqualTo("Nie udało się uruchomić aktywności."));
        Invoke(hud,"AttemptGarageActivity",run,new Func<bool>(()=>{R1RunController.Instance.Garage.Message="Pojazd jest zbyt szeroki.";return false;}));
        Assert.That((string)Field(hud,"activitySaveFeedback"),Is.EqualTo("Pojazd jest zbyt szeroki."));
        Invoke(hud,"AttemptGarageActivity",run,new Func<bool>(()=>true));
        Assert.That(Field(hud,"activitySaveFeedback"),Is.Null);
        yield return null;
    }

    [TestCase(false,R3PartKind.Armor,true,5,100,0,"chwilowo")]
    [TestCase(true,R3PartKind.BonusModule,true,5,100,0,"nagród")]
    [TestCase(true,R3PartKind.Gun,false,10,100,0,"Technologia")]
    [TestCase(true,R3PartKind.Armor,true,5,100,200,"pełny")]
    [TestCase(true,R3PartKind.Armor,true,0,100,0,"niedostępna")]
    [TestCase(true,R3PartKind.Armor,true,5,3,0,"Brakuje 2 Z")]
    [TestCase(true,R3PartKind.Armor,true,5,10,0,null)]
    public void PurchaseFailureExplainsCommonBlocks(bool ready,R3PartKind kind,bool unlocked,int price,int scrap,int count,string expected)
    {
        var method=typeof(R1RunHUD).GetMethod("BasicPurchaseFailure",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var actual=(string)method.Invoke(null,new object[]{ready,kind,unlocked,price,scrap,count});
        if(expected==null)Assert.That(actual,Is.Null);else Assert.That(actual,Does.Contain(expected));
    }

    [TestCase(R3PartKind.Armor,true,5,25,195,true)]
    [TestCase(R3PartKind.Armor,true,5,24,195,false)]
    [TestCase(R3PartKind.Armor,true,5,25,196,false)]
    [TestCase(R3PartKind.Armor,false,5,100,0,false)]
    [TestCase(R3PartKind.BonusModule,true,5,100,0,false)]
    [TestCase(R3PartKind.Armor,true,int.MaxValue,100000,0,false)]
    public void FivePurchaseRequiresFundsCapacityAndUnlockedType(R3PartKind kind,bool unlocked,int price,int scrap,int count,bool expected)
    {
        var method=typeof(R1RunHUD).GetMethod("CanBuyFive",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        Assert.That((bool)method.Invoke(null,new object[]{kind,unlocked,price,scrap,count}),Is.EqualTo(expected));
    }

    [UnityTest] public IEnumerator BuyFiveUsesIndividualPurchaseRulesAndSelectsOwnedInstance()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        var hud=run.GetComponent<R1RunHUD>();Field(hud,"catalogOpen",true);
        Invoke(hud,"PortraitCatalogSelectGroup",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        int before=run.VehicleModel.Scrap,count=run.VehicleModel.Inventory.Count;
        int price=run.VehicleModel.Price(R3PartKind.Armor);
        Invoke(hud,"BuyFiveParts",run,R3PartKind.Armor,R4WeaponKind.Basic);
        var added=run.VehicleModel.Inventory.Skip(count).ToArray();
        Assert.That(added.Length,Is.EqualTo(5));Assert.That(added.All(part=>part.Kind==R3PartKind.Armor),Is.True);
        Assert.That(added.Select(part=>part.Id).Distinct().Count(),Is.EqualTo(5));
        Assert.That(before-run.VehicleModel.Scrap,Is.EqualTo(price*5));
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(added.Last().Id));
        Assert.That((bool)Field(hud,"catalogOpen"),Is.False);
        Assert.That(run.Garage.Message,Does.Contain("5").And.Contain((price*5)+" Z"));
        run.Garage.Select(run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Core).Cell);
        Assert.That((string)Invoke(hud,"SimpleGarageFeedback",run),Does.Contain((price*5)+" Z"),
            "The group purchase receipt must remain visible with a placement target.");
        yield return null;
    }

    [UnityTest] public IEnumerator QuickPurchaseShowsActualCostEvenWhenGhostMoves()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        var hud=run.GetComponent<R1RunHUD>();
        Invoke(hud,"SelectQuickPart",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        int before=run.VehicleModel.Scrap,price=run.VehicleModel.Price(R3PartKind.Armor);
        Invoke(hud,"BuyQuickPart",run);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-price));
        Assert.That(run.Garage.SelectedInventoryId,Is.Not.Null.And.Not.Empty);
        run.Garage.Select(run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Core).Cell);
        Assert.That((string)Invoke(hud,"SimpleGarageFeedback",run),Does.Contain($"{price} Z"),
            "A purchase must disclose the actual cost even after the ghost has a target.");
        yield return null;
    }

    [TestCase(false,"Brak sprawnej broni.",R3DriveKind.Wheels,"Start zablokowany: Brak sprawnej broni.")]
    [TestCase(false,null,R3DriveKind.Omni,"Start zablokowany: Sprawdź konstrukcję.")]
    [TestCase(true,null,R3DriveKind.Omni,"Wyścig, tryby drogowe i generowana arena wymagają kół.")]
    [TestCase(true,null,R3DriveKind.Wheels,null)]
    public void ActivityHintExplainsExistingAvailability(bool ready,string reason,R3DriveKind drive,string expected)
    {
        var method=typeof(R1RunHUD).GetMethod("ActivityReadinessHint",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        Assert.That((string)method.Invoke(null,new object[]{ready,reason,drive}),Is.EqualTo(expected));
    }

    [UnityTest] public IEnumerator ActivityMenuExplainsUnavailableArenasAndFinalStage()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.PrepareUnifiedRun(),Is.True);
        Assert.That(run.VehicleModel.CanDeploy,Is.True);
        Assert.That(run.RouteOffers.Length,Is.GreaterThan(0));
        var offer=run.RouteOffers[0];Assert.That(offer.IsBoss,Is.False);
        offer.EnemyCount=3;
        Assert.That(offer.SpawnCount,Is.EqualTo(4));
        Assert.That(run.CanChooseGeneratedArena,Is.False);
        Assert.That(run.CanChooseLD02Arena,Is.False);
        var hud=run.GetComponent<R1RunHUD>();
        Assert.That((string)Invoke(hud,"ActivityContextHint",run),Does.Contain("3").And.Contain("aren").IgnoreCase);
        typeof(R1RunProgress).GetProperty("EncounterIndex").GetSetMethod(true).Invoke(run.Progress,new object[]{run.Progress.TotalEncounters-1});
        Assert.That(run.CanChooseReactorDefense,Is.False);
        Assert.That((string)Invoke(hud,"ActivityContextHint",run),Does.Contain("finał").IgnoreCase);
        yield return null;
    }

    [TestCase(true,null,"Zapisano wyprawę w garażu.","Zapisano wyprawę w garażu.")]
    [TestCase(false,"stary","Nie zapisano wyprawy: brak miejsca.","Nie zapisano wyprawy: brak miejsca.")]
    [TestCase(false,"Nie zapisano wyprawy: brak miejsca.","Nie zapisano wyprawy: brak miejsca.","Nie zapisano wyprawy: brak miejsca.")]
    [TestCase(false,"Zapisano wyprawę.","Zapisano wyprawę.","Nie udało się zapisać wyprawy.")]
    [TestCase(false,null,null,"Nie udało się zapisać wyprawy.")]
    public void SaveFeedbackNeverReportsStaleSuccess(bool saved,string previous,string current,string expected)
    {
        var method=typeof(R1RunHUD).GetMethod("ActivitySaveResult",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        Assert.That((string)method.Invoke(null,new object[]{saved,previous,current}),Is.EqualTo(expected));
    }

    [UnityTest] public IEnumerator RejectedSaveClearsPreviousErrorBeforeReporting()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        typeof(R1RunController).GetProperty("RunSaveMessage").GetSetMethod(true).Invoke(run,new object[]{"Nie zapisano wyprawy: brak miejsca."});
        Assert.That(run.SaveRunCheckpoint(),Is.False,"Saving in the initial menu is unavailable.");
        Assert.That(run.RunSaveMessage,Is.Null,"A previous error must not describe a new rejected attempt.");
    }

    [UnityTest] public IEnumerator GarageOptionsRepairReportsActualScrapSpent()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        var part=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Armor);part.Health=.5f;
        var hud=run.GetComponent<R1RunHUD>();int before=run.VehicleModel.Scrap;
        int expected=run.VehicleModel.AffordableRepairCost;Assert.That(expected,Is.GreaterThan(0));
        Invoke(hud,"OpenSimpleGarageOptions",0);
        Invoke(hud,"RepairFromGarageOptions",run,false);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-expected));
        Assert.That(part.Health,Is.EqualTo(1));
        Assert.That((string)Field(hud,"garageOptionsFeedback"),Does.Contain("Naprawiono").And.Contain(expected+" Z"));
        Invoke(hud,"OpenSimpleGarageOptions",0);
        Assert.That(Field(hud,"garageOptionsFeedback"),Is.Null,"Reopening must not show the previous transaction as new.");
    }

    [UnityTest] public IEnumerator GarageDriveSummarySuggestsPracticalUphillOptions()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Heavy)&&run.OpenSandboxGarage(),Is.True);
        var method=typeof(R1RunHUD).GetMethod("GarageDriveSummary",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var text=(string)method.Invoke(null,new object[]{run});
        Assert.That(text,Does.Contain("Podjazd").And.Contain("silnik kół").And.Contain("25"));
        run.VehicleModel.SelectedDrive=R3DriveKind.Hover;
        text=(string)method.Invoke(null,new object[]{run});
        Assert.That(text,Does.Contain("Hover").And.Contain("wyłącznie napęd kołowy"));
    }

    [Test] public void BonusStackNameDoesNotInheritFirstItemsTier()
    {
        var method=typeof(R1RunHUD).GetMethod("CatalogBonusGroupName",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var bonus=new R3PartState(default,R3PartKind.BonusModule){UpgradeTitle="Osłona",Tier=2};
        Assert.That((string)method.Invoke(null,new object[]{bonus}),Is.EqualTo("Osłona"));
        Assert.That(R4WeaponDefinition.PartName(bonus),Does.Contain("T2"),"The individual item keeps its tier.");
        bonus.Tier=3;
        Assert.That((string)method.Invoke(null,new object[]{bonus}),Is.EqualTo("Osłona"));
        bonus.UpgradeTitle=null;
        Assert.That((string)method.Invoke(null,new object[]{bonus}),Is.EqualTo("Moduł bonusu"));
    }

    [TestCase(5,false,true,"Koło × 5 • napraw")]
    [TestCase(5,true,true,"Koło × 5")]
    [TestCase(5,true,false,"Koło × 5")]
    [TestCase(5,false,false,"Koło × 5 • napraw")]
    [TestCase(0,false,false,"Koło × 0 • blokada")]
    [TestCase(0,false,true,"Koło × 0")]
    public void StackCaptionDistinguishesWrecksFromUsableOrLockedParts(int count,bool usable,bool unlocked,string expected)
    {
        var method=typeof(R1RunHUD).GetMethod("CatalogStackCaption",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        Assert.That((string)method.Invoke(null,new object[]{"Koło",count,usable,unlocked}),Is.EqualTo(expected));
    }

    [UnityTest] public IEnumerator EmptyBonusStackStillNamesItsMountedPart()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        var definition=run.Definition.Upgrades.First(item=>R3VehicleModel.CreateBonusReward(item)!=null);
        Assert.That(run.VehicleModel.ReceiveBonuses(new[]{definition}),Is.True);
        var bonus=run.VehicleModel.Inventory.Last();var hud=run.GetComponent<R1RunHUD>();
        var key=typeof(R1RunHUD).GetMethod("CatalogBonusKey",BindingFlags.Static|BindingFlags.NonPublic);
        Invoke(hud,"SelectQuickPart",run,R3PartKind.BonusModule,R4WeaponKind.Basic,key.Invoke(null,new object[]{bonus}));
        var garage=run.Garage;yield return null;bool found=false;
        foreach(var socket in garage.AvailableMountPoints.ToArray())
        {
            var normal=socket.Rotation*Vector3.forward;
            if(garage.Point(new Ray(socket.Position+normal*2,-normal))&&garage.PreviewValid){found=true;break;}
        }
        Assert.That(found,Is.True);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);
        Assert.That(garage.SelectedInventoryId,Is.Null);
        var text=(string)Invoke(hud,"GarageSelectionSummary",run,null,null);
        Assert.That(text,Does.Contain(bonus.UpgradeTitle).And.Contain("nagrod"));
        yield return new ExitPlayMode();
    }

    [TestCase(true,R3PartKind.Armor,true,"za darmo")]
    [TestCase(true,R3PartKind.BonusModule,true,"za darmo")]
    [TestCase(false,R3PartKind.Gun,true,"Kup")]
    [TestCase(false,R3PartKind.Gun,false,"odblokuj")]
    [TestCase(false,R3PartKind.BonusModule,true,"nagród")]
    public void EmptyStackExplainsHowToObtainPart(bool creative,R3PartKind kind,bool unlocked,string expected)
    {
        var method=typeof(R1RunHUD).GetMethod("CatalogEmptyStackHint",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        Assert.That((string)method.Invoke(null,new object[]{creative,kind,unlocked}),Does.Contain(expected));
    }

    [UnityTest] public IEnumerator CatalogShowsDamageCountAndListsBestConditionFirst()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        for(int i=0;i<4;i++)Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        Assert.That(run.GaragePurchase(R3PartKind.Wheel),Is.True);
        var original=run.VehicleModel.Inventory.ToArray();
        original[0].Health=.55f;original[1].Health=1;original[2].Health=0;original[3].Health=1;
        var hud=run.GetComponent<R1RunHUD>();
        var sorted=(R3PartState[])Invoke(hud,"CatalogItemsForGroup",run.VehicleModel,R3PartKind.Armor,R4WeaponKind.Basic,null);
        Assert.That(sorted.Select(p=>p.Id).ToArray(),Is.EqualTo(new[]{original[1].Id,original[3].Id,original[0].Id,original[2].Id}));
        Assert.That(run.VehicleModel.Inventory.Select(p=>p.Id).ToArray(),Is.EqualTo(original.Select(p=>p.Id).ToArray()),"Display sorting must not mutate the inventory.");
        string caption=(string)Invoke(hud,"CatalogGroupCaption",run,R3PartKind.Armor,R4WeaponKind.Basic,null,"Pancerz");
        Assert.That(caption,Does.Contain("× 4").And.Contain("2 uszk."));
        Invoke(hud,"SelectQuickPart",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(sorted[0].Id),"Quick selection matches the first item shown.");
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator CatalogMarksStacksWithOnlyDamagedButUsableParts()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        Assert.That(run.GaragePurchase(R3PartKind.Wheel),Is.True);
        Assert.That(run.GaragePurchase(R3PartKind.Wheel),Is.True);
        var wheels=run.VehicleModel.Inventory.Where(p=>p.Kind==R3PartKind.Wheel).ToArray();
        wheels[0].Health=.25f;wheels[1].Health=.8f;
        string Caption()=>(string)Invoke(run.GetComponent<R1RunHUD>(),"CatalogGroupCaption",run,R3PartKind.Wheel,R4WeaponKind.Basic,null,"Koło");
        Assert.That(Caption(),Does.Contain("× 2").And.Contain("uszkodzone"));
        wheels[0].Health=1f;
        Assert.That(Caption(),Does.Not.Contain("uszkodzone"),"A healthy item is available to select.");
        wheels[0].Health=0f;wheels[1].Health=0f;
        Assert.That(Caption(),Does.Contain("napraw").And.Not.Contain("uszkodzone"));
        yield return new ExitPlayMode();
    }

    [Test] public void InspectionSummaryUsesMountedPartHealthAndRepairQuote()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var part=model.Parts.First(p=>p.Kind==R3PartKind.Armor);part.Health=.5f;
        var method=typeof(R1RunHUD).GetMethod("GarageInspectionSummary",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var damaged=(string)method.Invoke(null,new object[]{model,part});
        Assert.That(damaged,Does.Contain("HP 50%").And.Contain(model.RepairCost(part)+" Z"));
        part.Health=1;
        var healthy=(string)method.Invoke(null,new object[]{model,part});
        Assert.That(healthy,Does.Contain("HP 100%").And.Contain("sprawna"));
        Assert.That(healthy,Does.Not.Contain("0 Z"));
    }

    [UnityTest] public IEnumerator CatalogDetailsSelectsHealthyPartWithoutClosingOrSpending()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var item=run.VehicleModel.Inventory.Last();var hud=run.GetComponent<R1RunHUD>();
        Field(hud,"catalogOpen",true);int scrap=run.VehicleModel.Scrap;
        Invoke(hud,"SelectCatalogDetails",run,item.Id);
        Assert.That((bool)Field(hud,"catalogOpen"),Is.True);
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(item.Id));
        Assert.That(ContainsId(run.VehicleModel,item.Id),Is.True);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Invoke(hud,"SelectCatalogItem",run,item.Id);
        Assert.That((bool)Field(hud,"catalogOpen"),Is.False,"Mounting selection should remain one click.");
    }

    [UnityTest] public IEnumerator DismantleConfirmationResetsAfterGroupChangeAndCatalogClose()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var item=run.VehicleModel.Inventory.Last();var hud=run.GetComponent<R1RunHUD>();Field(hud,"catalogOpen",true);
        int scrap=run.VehicleModel.Scrap;
        Invoke(hud,"PortraitCatalogDismantle",run,item.Id);
        Assert.That((string)Field(hud,"catalogFeedback"),Does.Contain("Kliknij ponownie"));
        Invoke(hud,"PortraitCatalogSelectGroup",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        Invoke(hud,"PortraitCatalogDismantle",run,item.Id);
        Assert.That(ContainsId(run.VehicleModel,item.Id),Is.True,"Changing group must cancel a previous confirmation.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That((bool)Invoke(hud,"ClosePartCatalog"),Is.True);Field(hud,"catalogOpen",true);
        Invoke(hud,"PortraitCatalogDismantle",run,item.Id);
        Assert.That(ContainsId(run.VehicleModel,item.Id),Is.True,"Reopening the catalog must require a fresh confirmation.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        item.Health=.5f;int repair=run.VehicleModel.RepairCost(item);
        Invoke(hud,"PortraitCatalogRepair",run,item.Id);
        Assert.That(item.Health,Is.EqualTo(1));scrap-=repair;
        Assert.That((string)Field(hud,"catalogFeedback"),Does.Contain("Naprawiono").And.Contain(repair+" Z"));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Invoke(hud,"PortraitCatalogDismantle",run,item.Id);
        Assert.That(ContainsId(run.VehicleModel,item.Id),Is.True,"Repair must cancel a previous dismantle confirmation.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        int value=run.VehicleModel.DismantleValue(item);
        Invoke(hud,"PortraitCatalogDismantle",run,item.Id);
        Assert.That(ContainsId(run.VehicleModel,item.Id),Is.False);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap+value));
        Assert.That((string)Field(hud,"catalogFeedback"),Does.Contain("Rozmontowano").And.Contain(value+" Z"));
    }

    [UnityTest] public IEnumerator DismantleCanBeCancelledWithoutLosingPartOrClosingCatalog()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        run.VehicleModel.Award(100);Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var item=run.VehicleModel.Inventory.Last();var hud=run.GetComponent<R1RunHUD>();
        Field(hud,"catalogOpen",true);int scrap=run.VehicleModel.Scrap;
        Invoke(hud,"PortraitCatalogDismantle",run,item.Id);
        Assert.That(ContainsId(run.VehicleModel,item.Id),Is.True);
        Invoke(hud,"CancelCatalogDismantle");
        Assert.That((bool)Field(hud,"catalogOpen"),Is.True);
        Assert.That(Field(hud,"dismantleConfirmation"),Is.Null);
        Assert.That(ContainsId(run.VehicleModel,item.Id),Is.True);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Invoke(hud,"PortraitCatalogDismantle",run,item.Id);
        Assert.That(ContainsId(run.VehicleModel,item.Id),Is.True,"A cancelled operation must require a fresh confirmation.");
        Assert.That((bool)Invoke(hud,"ClosePartCatalog"),Is.True);
        Assert.That((bool)Field(hud,"catalogOpen"),Is.True,"Escape cancels the pending decision before leaving the catalog.");
        Assert.That(ContainsId(run.VehicleModel,item.Id),Is.True);
    }

    [UnityTest] public IEnumerator DestroyedItemKeepsCatalogOpenUntilRepaired()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var item=run.VehicleModel.Inventory.Last();item.Health=0;
        var hud=run.GetComponent<R1RunHUD>();Field(hud,"catalogOpen",true);
        int scrap=run.VehicleModel.Scrap,quote=run.VehicleModel.RepairCost(item);
        Invoke(hud,"PortraitCatalogSelectItem",run,item.Id);
        Assert.That((bool)Field(hud,"catalogOpen"),Is.True,"A wreck cannot be mounted yet; retain access to repair and fusion.");
        Assert.That((string)Field(hud,"catalogFeedback"),Does.Contain("napraw"),"Repair guidance must be visible in the still-open catalog.");
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(item.Id));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));Assert.That(item.Health,Is.Zero);
        var playerBeforeRepair=run.Player;
        Invoke(hud,"PortraitCatalogRepair",run,item.Id);
        Assert.That(item.Health,Is.EqualTo(1));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap-quote));
        Assert.That(run.Player,Is.SameAs(playerBeforeRepair),"Repairing an inventory-only item must not rebuild the assembled vehicle.");
        Assert.That(ContainsId(run.VehicleModel,item.Id),Is.True,"Repair must retain the owned instance.");
        Invoke(hud,"PortraitCatalogSelectItem",run,item.Id);
        Assert.That((bool)Field(hud,"catalogOpen"),Is.False,"The repaired part can now be selected for mounting.");
    }

    [UnityTest] public IEnumerator BlockedPlacementShowsTheActualReasonBeforeClicking()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light,0),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(1000);yield return null;
        var garage=run.Garage;garage.UseInventory=false;garage.SelectedPart=R3PartKind.Gun;
        garage.SelectedWeapon=R4WeaponKind.Basic;garage.SetTool(R41GarageTool.Mount);
        bool found=false;
        foreach(var socket in garage.AvailableMountPoints.ToArray())
        {
            var normal=socket.Rotation*Vector3.forward;
            if(!garage.Point(new Ray(socket.Position+normal*2,-normal))||!garage.HasTarget)continue;
            if(garage.PlacementIssue!=null){found=true;break;}
        }
        Assert.That(found,Is.True,"The real vehicle must offer a socket with a blocked gun position.");
        string reason=garage.PlacementIssue;
        garage.Message="Kupiono część. Kliknij pojazd, aby ją zamontować.";
        var feedback=(string)Invoke(run.GetComponent<R1RunHUD>(),"SimpleGarageFeedback",run);
        Assert.That(feedback,Does.Contain(reason),"A red ghost must explain its current restriction in the readable main HUD.");
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator ChoosingGarageToolExplainsItsCurrentAction()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);yield return null;
        var garage=run.Garage;
        var method=typeof(R3GarageView).GetMethod("SelectTool",BindingFlags.Instance|BindingFlags.Public);
        Assert.That(method,Is.Not.Null,"Manual tool choice must update the visible instructions.");
        foreach(var item in new[]{(R41GarageTool.Inspect,"zaznacz"),(R41GarageTool.Repair,"napraw"),(R41GarageTool.Remove,"zdejmij")})
        {
            garage.Message="Stary komunikat o montażu.";
            method.Invoke(garage,new object[]{item.Item1});
            Assert.That(garage.Tool,Is.EqualTo(item.Item1));
            Assert.That(garage.Message.ToLowerInvariant(),Does.Contain(item.Item2));
            Assert.That(garage.Message,Does.Not.Contain("Stary komunikat"));
        }
        method.Invoke(garage,new object[]{R41GarageTool.Mount});
        Assert.That(garage.Message.ToLowerInvariant(),Does.Contain("część"));
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator ValidGhostReplacesStalePlacementErrorWithMountHint()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);yield return null;
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var garage=run.Garage;garage.ChooseInventory(run.VehicleModel.Inventory.Last().Id);
        yield return null;
        bool found=false;
        foreach(var socket in garage.AvailableMountPoints.ToArray())
        {
            var normal=socket.Rotation*Vector3.forward;
            if(garage.Point(new Ray(socket.Position+normal*2,-normal))&&garage.PreviewValid){found=true;break;}
        }
        Assert.That(found,Is.True,"The real garage must provide a valid position for the selected block.");
        garage.Message="Wybrane pole jest zajęte.";
        var feedback=(string)Invoke(run.GetComponent<R1RunHUD>(),"SimpleGarageFeedback",run);
        Assert.That(feedback,Does.Contain("LPM").And.Contain("zamontuj"));
        Assert.That(feedback,Does.Not.Contain("Wybrane pole jest zajęte"));
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator IncompleteGarageKeepsActionFeedbackAlongsideDeploymentWarning()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        foreach(var wheel in run.VehicleModel.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray())
            Assert.That(run.GarageRemove(wheel.Cell),Is.True);
        Assert.That(run.VehicleModel.CanDeploy,Is.False);
        run.Garage.Message="Wybrane pole jest zajęte.";
        var hud=run.GetComponent<R1RunHUD>();
        var feedback=(string)Invoke(hud,"SimpleGarageFeedback",run);
        Assert.That(feedback,Does.Contain("Wybrane pole jest zajęte."));
        Assert.That(feedback,Does.Contain(run.VehicleModel.CriticalReadinessMessage??run.VehicleModel.DeployError));
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator CreativeCoreOnlyExplainsMissingDriveWithoutBlockingTest()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        var model=run.VehicleModel;var hud=run.GetComponent<R1RunHUD>();
        Assert.That(model.CanDeploy,Is.True,"Creative testing must remain unrestricted.");
        string feedback=(string)Invoke(hud,"SimpleGarageFeedback",run);
        Assert.That(feedback,Does.Contain("napędu").And.Contain("poligon").IgnoreCase);
        Assert.That(feedback,Does.Not.Contain("Start zablokowany"));
        Assert.That(feedback,Does.Not.Contain("broni"),"A gun is not required for driving tests.");
        model.Parts.Add(new R3PartState(Vector3Int.left,R3PartKind.Wheel));
        model.Parts.Add(new R3PartState(Vector3Int.right,R3PartKind.Wheel));
        Assert.That(model.CanDeploy,Is.True);
        feedback=(string)Invoke(hud,"SimpleGarageFeedback",run);
        Assert.That(feedback,Does.Not.Contain("bez napędu"));
        yield return null;
    }

    [UnityTest] public IEnumerator GarageExplainsDisconnectedConstructionBeforeMissingEquipment()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        // API-controlled model state representing a disconnected part and a
        // missing weapon; deployment policy reports disconnection first.
        run.VehicleModel.Parts.RemoveAll(p=>p.Kind==R3PartKind.Gun);
        run.VehicleModel.Parts.Add(new R3PartState(new Vector3Int(6,5,7),R3PartKind.Armor));
        Assert.That(run.VehicleModel.DeployError,Does.Contain("odłączone"));
        Assert.That(run.VehicleModel.CriticalReadinessMessage,Does.Contain("broni"));
        run.Garage.Message="Wybrane pole jest zajęte.";
        var feedback=(string)Invoke(run.GetComponent<R1RunHUD>(),"SimpleGarageFeedback",run);
        Assert.That(feedback,Does.Contain(run.VehicleModel.DeployError),
            "The player needs the actual reason deployment is refused.");
        Assert.That(feedback,Does.Contain("Wybrane pole jest zajęte."));
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator GarageStillExplainsBothMissingWeaponAndDrive()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        run.VehicleModel.Parts.RemoveAll(p=>p.Kind==R3PartKind.Gun||p.Kind==R3PartKind.Wheel);
        Assert.That(run.VehicleModel.DeployError,Is.EqualTo("Brak sprawnej broni."));
        var feedback=(string)Invoke(run.GetComponent<R1RunHUD>(),"SimpleGarageFeedback",run);
        Assert.That(feedback,Does.Contain("Brak sprawnej broni").And.Contain("Brak sprawnego napędu"));
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator GarageDoesNotHideWeaponTypeLimitBehindMissingDrive()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        run.VehicleModel.Parts.RemoveAll(p=>p.Kind==R3PartKind.Wheel);
        run.VehicleModel.Parts.Add(new R3PartState(new Vector3Int(6,5,6),R3PartKind.Gun){Weapon=R4WeaponKind.Automatic});
        run.VehicleModel.Parts.Add(new R3PartState(new Vector3Int(6,5,7),R3PartKind.Gun){Weapon=R4WeaponKind.Heavy});
        run.VehicleModel.Parts.Add(new R3PartState(new Vector3Int(6,5,8),R3PartKind.Gun){Weapon=R4WeaponKind.Shotgun});
        run.VehicleModel.Parts.Add(new R3PartState(new Vector3Int(6,5,9),R3PartKind.Gun){Weapon=R4WeaponKind.Railgun});
        Assert.That(run.VehicleModel.DeployError,Does.Contain("4 różne typy"));
        Assert.That(run.VehicleModel.CriticalReadinessMessage,Does.Contain("napędu"));
        var feedback=(string)Invoke(run.GetComponent<R1RunHUD>(),"SimpleGarageFeedback",run);
        Assert.That(feedback,Does.Contain(run.VehicleModel.DeployError));
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator GarageSelectionSummaryNamesThePartAndExplainsNextAction()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        var hud=run.GetComponent<R1RunHUD>();
        Invoke(hud,"SelectQuickPart",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        var text=(string)Invoke(hud,"GarageSelectionSummary",run,null,null);
        Assert.That(text,Does.Contain("Pancerz").And.Contain("Kup"));
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var item=run.VehicleModel.Inventory.Last();item.Health=.6f;
        Invoke(hud,"SelectQuickPart",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        text=(string)Invoke(hud,"GarageSelectionSummary",run,null,item);
        Assert.That(text,Does.Contain("Pancerz").And.Contain("60%"));
        item.Health=0;Invoke(hud,"SelectQuickPart",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        text=(string)Invoke(hud,"GarageSelectionSummary",run,null,null);
        Assert.That(text,Does.Contain("Pancerz").And.Contain("napraw"));
        yield return new ExitPlayMode();
    }

    [TestCase(false,R3PartKind.BonusModule,true,"nagród")]
    [TestCase(false,R3PartKind.Gun,false,"odblokuj")]
    [TestCase(false,R3PartKind.Armor,true,"Kup")]
    [TestCase(true,R3PartKind.Armor,true,"darmow")]
    public void EmptyMountedStackSuggestsAppropriateSource(bool creative,R3PartKind kind,bool unlocked,string expected)
    {
        var method=typeof(R3GarageView).GetMethod("ExhaustedStackFeedback",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var text=(string)method.Invoke(null,new object[]{creative,kind,unlocked});
        Assert.That(text.ToLowerInvariant(),Does.Contain(expected.ToLowerInvariant()));
        if(kind==R3PartKind.BonusModule)Assert.That(text.ToLowerInvariant(),Does.Not.Contain("kup"));
    }

    [UnityTest] public IEnumerator WreckStackPointsToQuickRepairWithoutOpeningCatalog()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var item=run.VehicleModel.Inventory.Last();item.Health=0;
        var hud=run.GetComponent<R1RunHUD>();Field(hud,"catalogOpen",false);
        Invoke(hud,"SelectQuickPart",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        Assert.That(run.Garage.Message.ToLowerInvariant(),Does.Contain("napraw").And.Contain("poniżej"));
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(item.Id),"The player should be able to repair only this part from the quick panel.");
        Assert.That((bool)Field(hud,"catalogOpen"),Is.False);
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator LastReadyPartMountsThenMatchingWreckIsRepairedAndMounted()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        var model=run.VehicleModel;model.Award(100);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        Assert.That(model.Inventory.Count,Is.EqualTo(2));
        var ready=model.Inventory[0];var wreck=model.Inventory[1];wreck.Health=0;
        var hud=run.GetComponent<R1RunHUD>();
        Invoke(hud,"SelectQuickPart",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(ready.Id));
        int balance=model.Scrap;
        run.Garage.Select(AvailableOwnedMountCell(model,ready));
        Assert.That(run.Garage.ClickTarget(),Is.True,"The first owned part mounts through the actual garage.");
        Assert.That(model.Scrap,Is.EqualTo(balance),"Mounting cannot buy another part.");
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(wreck.Id));
        Assert.That(run.Garage.Message,Does.Contain("napraw"));
        Assert.That(run.Garage.ClickTarget(),Is.False,"A selected wreck remains unavailable until explicitly repaired.");
        Assert.That(model.Scrap,Is.EqualTo(balance));
        int quote=model.RepairCost(wreck);
        Assert.That((bool)Invoke(hud,"RepairQuickSelected",run),Is.True);
        Assert.That(model.Scrap,Is.EqualTo(balance-quote));
        Assert.That(wreck.Health,Is.EqualTo(1));
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(wreck.Id));
        Assert.That((string)Invoke(hud,"SimpleGarageFeedback",run),Does.Contain($"koszt {quote} Z"),
            "The quick repair must show the amount actually spent even while a ghost is visible.");
        run.Garage.Select(AvailableOwnedMountCell(model,wreck));
        Assert.That(run.Garage.ClickTarget(),Is.True,"The same repaired part is mountable without reselection.");
        Assert.That(model.Inventory,Is.Empty);
        Assert.That(model.Owns(wreck.Id),Is.True);
        yield return null;
    }


    [UnityTest] public IEnumerator GarageActionsExplainRepairAndRemovalFailures()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        var model=run.VehicleModel;var garage=run.Garage;
        var core=model.Parts.First(p=>p.Kind==R3PartKind.Core);
        garage.SetTool(R41GarageTool.Remove);garage.Select(core.Cell);
        Assert.That(garage.ClickTarget(),Is.False);Assert.That(garage.Message,Does.Contain("rdzeni"));
        garage.SetTool(R41GarageTool.Repair);garage.Select(core.Cell);
        Assert.That(garage.ClickTarget(),Is.False);Assert.That(garage.Message,Does.Contain("sprawna"));
        R3PartState armor=null;foreach(var part in model.Parts)if(part.Kind==R3PartKind.Armor){armor=part;break;}
        Assert.That(armor,Is.Not.Null);armor.Health=.5f;
        typeof(R3VehicleModel).GetProperty("Scrap").SetValue(model,0);
        int price=model.RepairCost(armor);Assert.That(price,Is.GreaterThan(0));
        garage.Select(armor.Cell);Assert.That(garage.ClickTarget(),Is.False);
        Assert.That(garage.Message,Does.Contain($"Brakuje {price} Z"));Assert.That(armor.Health,Is.EqualTo(.5f));
        model.Award(price);garage.Select(armor.Cell);Assert.That(garage.ClickTarget(),Is.True);
        Assert.That(garage.Message,Does.Contain($"{price} Z"));Assert.That(armor.Health,Is.EqualTo(1));
        model.Award(10000);
        while(model.Inventory.Count<model.AvailableInventoryCapacity)Assert.That(model.PurchaseToInventory(R3PartKind.Armor),Is.True);
        garage.SetTool(R41GarageTool.Remove);garage.Select(armor.Cell);
        Assert.That(garage.ClickTarget(),Is.False);Assert.That(garage.Message,Does.Contain("magazyn"));
        Assert.That(model.Parts.Contains(armor),Is.True);
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator RepeatedPlacementNeverSilentlySwitchesToAnotherTier()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(200);
        for(int i=0;i<3;i++)Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        int count=run.VehicleModel.Inventory.Count;
        var first=run.VehicleModel.Inventory[count-3];var same=run.VehicleModel.Inventory[count-2];var higher=run.VehicleModel.Inventory[count-1];
        first.Tier=1;same.Tier=1;same.Health=.5f;higher.Tier=2;higher.Health=1;
        var garage=run.Garage;garage.ChooseInventory(first.Id);yield return null;
        bool found=false;
        foreach(var socket in garage.AvailableMountPoints.ToArray())
        {
            var normal=socket.Rotation*Vector3.forward;
            if(garage.Point(new Ray(socket.Position+normal*2,-normal))&&garage.PreviewValid){found=true;break;}
        }
        Assert.That(found,Is.True,"A valid real mount point must exist.");
        int scrap=run.VehicleModel.Scrap;
        Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);
        Assert.That(garage.SelectedInventoryId,Is.EqualTo(same.Id),"Keep the same tier even if another is healthier.");
        yield return null;found=false;
        foreach(var socket in garage.AvailableMountPoints.ToArray())
        {
            var normal=socket.Rotation*Vector3.forward;
            if(garage.Point(new Ray(socket.Position+normal*2,-normal))&&garage.PreviewValid){found=true;break;}
        }
        Assert.That(found,Is.True);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);
        Assert.That(garage.SelectedInventoryId,Is.Null,"A different tier requires an explicit player choice.");
        Assert.That(garage.Message,Does.Contain("poziom"));
        Assert.That(run.VehicleModel.Inventory.Contains(higher),Is.True);Assert.That(higher.Tier,Is.EqualTo(2));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        yield return new ExitPlayMode();
    }

    [Test] public void MountVariantKeepsSideWheelsSelectableOnEitherSide()
    {
        var left=new R3PartState(default,R3PartKind.Wheel){ContactMask=R3MountingGrid.FaceBit(Vector3Int.left)};
        var right=new R3PartState(default,R3PartKind.Wheel){ContactMask=R3MountingGrid.FaceBit(Vector3Int.right)};
        Assert.That(R3VehicleModel.SameMountVariant(left,right),Is.True,"Stored side wheels can connect on either side.");
        right.Tier=2;
        Assert.That(R3VehicleModel.SameMountVariant(left,right),Is.False,"Tier is still part of the intended variant.");
    }

    [UnityTest] public IEnumerator CopyMountedPartRespectsItsTierWithoutBuying()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        int count=run.VehicleModel.Inventory.Count;
        var lower=run.VehicleModel.Inventory[count-2];var matching=run.VehicleModel.Inventory[count-1];
        lower.Tier=1;lower.Health=1;matching.Tier=2;matching.Health=.4f;
        R3PartState mounted=null;
        foreach(var part in run.VehicleModel.Parts)if(part.Kind==R3PartKind.Armor&&part.ArmorShape==lower.ArmorShape){mounted=part;break;}
        Assert.That(mounted,Is.Not.Null);mounted.Tier=2;
        var hud=run.GetComponent<R1RunHUD>();int scrap=run.VehicleModel.Scrap;
        Assert.That(hud.SelectMountedPartForBuilding(run,mounted),Is.True);
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(matching.Id),"Match tier before preferring more HP.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(count));
        int oldMask=matching.ContactMask;
        matching.ContactMask=mounted.ContactMask==R3MountingGrid.FaceBit(Vector3Int.down)?R3MountingGrid.AllFaces:R3MountingGrid.FaceBit(Vector3Int.down);
        Assert.That(hud.SelectMountedPartForBuilding(run,mounted),Is.False,"A part with different mounting contacts is not an equivalent copy.");
        Assert.That(run.Garage.SelectedInventoryId,Is.Null);Assert.That(run.Garage.Message,Does.Contain("mocowania"));
        matching.ContactMask=oldMask;
        matching.Health=0;
        Assert.That(hud.SelectMountedPartForBuilding(run,mounted),Is.True);
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(matching.Id),"Select the matching wreck, not the healthy lower tier.");
        Assert.That(run.Garage.Message,Does.Contain("T2").And.Contain("napraw"));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));Assert.That(lower.Tier,Is.EqualTo(1));
        var copy=typeof(R1RunController).GetMethod("AddCreativeEquivalent");Assert.That(copy,Is.Not.Null);
        var args=new object[]{mounted,null};Assert.That((bool)copy.Invoke(run,args),Is.False,"The expedition cannot mint free parts.");
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(count));
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator CopyMountedPartSelectsOwnedInstanceWithoutSpending()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        Assert.That(run,Is.Not.Null,"Run controller remains live after purchases.");
        Assert.That(run.VehicleModel,Is.Not.Null,"Vehicle model remains live after purchases.");
        Assert.That(run.VehicleModel.Inventory,Is.Not.Null);
        int inventorySize=run.VehicleModel.Inventory.Count;Assert.That(inventorySize,Is.GreaterThanOrEqualTo(2));
        var first=run.VehicleModel.Inventory[inventorySize-2];var second=run.VehicleModel.Inventory[inventorySize-1];
        Assert.That(first,Is.Not.Null);Assert.That(second,Is.Not.Null);first.Health=.3f;second.Health=.8f;
        R3PartState mounted=null;
        foreach(var part in run.VehicleModel.Parts)
            if(part!=null&&part.Kind==R3PartKind.Armor&&part.ArmorShape==second.ArmorShape){mounted=part;break;}
        Assert.That(mounted,Is.Not.Null);
        int scrap=run.VehicleModel.Scrap,count=run.VehicleModel.Inventory.Count;
        var hud=run.GetComponent<R1RunHUD>();
        var method=typeof(R1RunHUD).GetMethod("SelectMountedPartForBuilding",BindingFlags.Instance|BindingFlags.Public);
        Assert.That(method,Is.Not.Null);
        Assert.That((bool)method.Invoke(hud,new object[]{run,mounted}),Is.True);
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(second.Id),"The healthiest owned instance is selected.");
        Assert.That(run.Garage.Tool,Is.EqualTo(R41GarageTool.Mount));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(count));
        first.Health=0;second.Health=0;
        Assert.That((bool)method.Invoke(hud,new object[]{run,mounted}),Is.True);
        Assert.That(new[]{first.Id,second.Id},Does.Contain(run.Garage.SelectedInventoryId));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(run.Garage.Message,Does.Contain("napraw"));
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator RepairAndMountUsesExplicitActionAndKeepsPartIdentity()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var item=run.VehicleModel.Inventory.Last();item.Health=0;
        int before=run.VehicleModel.Scrap,price=run.VehicleModel.RepairCost(item);
        var hud=run.GetComponent<R1RunHUD>();Field(hud,"catalogOpen",true);
        Invoke(hud,"RepairAndSelectCatalogItem",run,item.Id);
        Assert.That(item.Health,Is.EqualTo(1));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-price));
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(item.Id));Assert.That((bool)Field(hud,"catalogOpen"),Is.False);
        Assert.That(run.Garage.Message,Does.Contain("montaż"));
        Field(hud,"catalogOpen",true);item.Health=0;
        typeof(R3VehicleModel).GetProperty("Scrap").SetValue(run.VehicleModel,0);
        Invoke(hud,"RepairAndSelectCatalogItem",run,item.Id);
        Assert.That(item.Health,Is.Zero);Assert.That((bool)Field(hud,"catalogOpen"),Is.True);
        Assert.That((string)Field(hud,"catalogFeedback"),Does.Contain("Brakuje"));
        yield return new ExitPlayMode();
    }

    [TestCase(false,1280,720)] [TestCase(true,768,1024)]
    public void QuickSearchClearButtonFitsNextToInput(bool portrait,int width,int height)
    {
        var method=typeof(R1RunHUD).GetMethod("QuickSearchRects",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);var args=new object[]{portrait,default(Rect),default(Rect)};method.Invoke(null,args);
        var input=(Rect)args[1];var clear=(Rect)args[2];var canvas=new Rect(0,0,width,height);
        Assert.That(input.Overlaps(clear),Is.False);Assert.That(input.width,Is.GreaterThan(65));
        Assert.That(canvas.Contains(input.min)&&canvas.Contains(clear.max-Vector2.one),Is.True);
    }

    [TestCase(KeyCode.Alpha5,false,0)] [TestCase(KeyCode.Alpha6,false,1)]
    [TestCase(KeyCode.Alpha7,false,2)] [TestCase(KeyCode.Alpha8,false,3)]
    [TestCase(KeyCode.Keypad5,false,0)] [TestCase(KeyCode.Keypad8,false,3)]
    [TestCase(KeyCode.Alpha5,true,null)] [TestCase(KeyCode.Alpha4,false,null)]
    public void CategoryShortcutsIgnoreModifiersAndOtherKeys(KeyCode key,bool modified,object expected)
    {
        var method=typeof(R1RunHUD).GetMethod("QuickCategoryShortcutForProfile",BindingFlags.Static|BindingFlags.NonPublic);
        var profile=new R1ControlProfile(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"RRL-category-keys-"+Guid.NewGuid()+".json"));
        var input=new Event{type=EventType.KeyDown,keyCode=key,modifiers=modified?EventModifiers.Control:EventModifiers.None};
        Assert.That(method,Is.Not.Null);Assert.That(method.Invoke(null,new object[]{input,profile}),Is.EqualTo(expected));
    }

    [UnityTest] public IEnumerator CategoryShortcutClearsSearchWithoutBuyingAndIgnoresTyping()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        var hud=run.GetComponent<R1RunHUD>();Invoke(hud,"SelectQuickPart",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        Invoke(hud,"ApplyQuickSearch",run,"pancerz");int count=run.VehicleModel.Inventory.Count,scrap=run.VehicleModel.Scrap;
        var key=new Event{type=EventType.KeyDown,keyCode=KeyCode.Alpha6};
        Assert.That((bool)Invoke(hud,"HandleQuickGarageKey",run,key,false),Is.True);
        Assert.That((int)Field(hud,"quickPartCategory"),Is.EqualTo(1));Assert.That((string)Field(hud,"quickPartSearch"),Is.Empty);
        Assert.That(run.Garage.SelectedInventoryId,Is.Null);Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(count));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        key=new Event{type=EventType.KeyDown,keyCode=KeyCode.Alpha7};
        Assert.That((bool)Invoke(hud,"HandleQuickGarageKey",run,key,true),Is.False,"Typing must not switch categories.");
        Assert.That((int)Field(hud,"quickPartCategory"),Is.EqualTo(1));
        key=new Event{type=EventType.KeyDown,keyCode=KeyCode.Alpha7,control=true};
        Assert.That((bool)Invoke(hud,"HandleQuickGarageKey",run,key,false),Is.False);
        Assert.That((int)Field(hud,"quickPartCategory"),Is.EqualTo(1));
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator ReassignedCategoryKeyChangesTheActualGarageCatalog()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        var hud=run.GetComponent<R1RunHUD>();var profile=R1ControlProfile.Current;profile.ResetDefaults();
        try
        {
            Assert.That(profile.TryBind("GarageWheels","K"),Is.True);
            Invoke(hud,"ApplyQuickSearch",run,"pancerz");int count=run.VehicleModel.Inventory.Count;
            var key=new Event{type=EventType.KeyDown,keyCode=KeyCode.K};
            Assert.That((bool)Invoke(hud,"HandleQuickGarageKey",run,key,false),Is.True);
            Assert.That((int)Field(hud,"quickPartCategory"),Is.EqualTo(1));Assert.That((string)Field(hud,"quickPartSearch"),Is.Empty);
            Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(count));
            key=new Event{type=EventType.KeyDown,keyCode=KeyCode.Alpha6};
            Assert.That((bool)Invoke(hud,"HandleQuickGarageKey",run,key,false),Is.False);
        }
        finally{profile.ResetDefaults();}
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator BrowsingCategoryAfterSearchKeepsOnlyVisibleSelectionAndNeverBuys()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        Assert.That(run.GaragePurchase(R3PartKind.Wheel),Is.True);
        var hud=run.GetComponent<R1RunHUD>();
        Invoke(hud,"ApplyQuickSearch",run,"kolo");
        Invoke(hud,"SelectQuickPart",run,R3PartKind.Wheel,R4WeaponKind.Basic,null);
        string selected=run.Garage.SelectedInventoryId;int count=run.VehicleModel.Inventory.Count,scrap=run.VehicleModel.Scrap;
        Invoke(hud,"BrowseQuickCategory",run,1);
        Assert.That((string)Field(hud,"quickPartSearch"),Is.Empty);
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(selected),"The selected wheel remains visible in its own category.");
        Invoke(hud,"ApplyQuickSearch",run,"kolo");
        Invoke(hud,"BrowseQuickCategory",run,2);
        Assert.That((int)Field(hud,"quickPartCategory"),Is.EqualTo(2));
        Assert.That((string)Field(hud,"quickPartSearch"),Is.Empty);
        Assert.That(run.Garage.SelectedInventoryId,Is.Null,"Switching away must not leave an invisible item ready to mount.");
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(count));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
    }

    [TestCase(EventType.KeyDown,KeyCode.Escape,"GarageQuickSearch",true,true)]
    [TestCase(EventType.KeyDown,KeyCode.Return,"GarageQuickSearch",true,true)]
    [TestCase(EventType.KeyDown,KeyCode.KeypadEnter,"GarageQuickSearch",true,true)]
    [TestCase(EventType.KeyUp,KeyCode.Escape,"GarageQuickSearch",true,false)]
    [TestCase(EventType.KeyDown,KeyCode.Escape,"GarageQuickSearch",false,false)]
    [TestCase(EventType.KeyDown,KeyCode.Escape,"",true,false)]
    [TestCase(EventType.KeyDown,KeyCode.Q,"GarageQuickSearch",true,false)]
    public void SearchExitKeysOnlyFinishActiveGarageInput(EventType type,KeyCode key,string focus,bool active,bool expected)
    {
        var method=typeof(R1RunHUD).GetMethod("GarageSearchExitKey",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        Assert.That((bool)method.Invoke(null,new object[]{type,key,focus,active}),Is.EqualTo(expected));
    }

    [TestCase(EventType.KeyDown,"GarageQuickSearch",true)]
    [TestCase(EventType.KeyUp,"GarageQuickSearch",true)]
    [TestCase(EventType.MouseDown,"GarageQuickSearch",false)]
    [TestCase(EventType.KeyDown,"",false)]
    public void TypingInQuickSearchDoesNotTriggerGarageShortcuts(EventType type,string focus,bool expected)
    {
        var method=typeof(R1RunHUD).GetMethod("GarageSearchConsumesKey",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        Assert.That((bool)method.Invoke(null,new object[]{type,focus}),Is.EqualTo(expected));
    }

    [UnityTest] public IEnumerator QuickSearchCoversAllCategoriesAndRespectsOwnedFilter()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        var hud=run.GetComponent<R1RunHUD>();
        var matches=((IEnumerable)Invoke(hud,"BuildFilteredQuickCatalogEntries",run.VehicleModel,0,false,"kOLO")).Cast<object>().ToArray();
        Assert.That(matches.Length,Is.GreaterThan(1),"Searching for wheels must work even when Bloki was selected.");
        Assert.That(matches.All(entry=>(R3PartKind)entry.GetType().GetField("Kind").GetValue(entry)==R3PartKind.Wheel),Is.True);
        var owned=((IEnumerable)Invoke(hud,"BuildFilteredQuickCatalogEntries",run.VehicleModel,0,true,"kolo")).Cast<object>().ToArray();
        Assert.That(owned,Is.Empty);
        Assert.That(run.GaragePurchase(R3PartKind.Wheel),Is.True);
        owned=((IEnumerable)Invoke(hud,"BuildFilteredQuickCatalogEntries",run.VehicleModel,0,true,"kolo")).Cast<object>().ToArray();
        Assert.That(owned.Length,Is.EqualTo(1));
        Invoke(hud,"ApplyQuickSearch",run,"kolo");
        Invoke(hud,"SelectQuickPart",run,R3PartKind.Wheel,R4WeaponKind.Basic,null);
        string selected=run.Garage.SelectedInventoryId;
        Assert.That(selected,Is.Not.Null);Assert.That((int)Field(hud,"quickPartCategory"),Is.EqualTo(1));
        Invoke(hud,"ApplyQuickSearch",run,"");
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(selected),"Clearing search keeps the chosen wheel in its category.");
        var category=((IEnumerable)Invoke(hud,"BuildFilteredQuickCatalogEntries",run.VehicleModel,0,false,"")).Cast<object>().ToArray();
        Assert.That(category.All(entry=>(R3PartKind)entry.GetType().GetField("Kind").GetValue(entry)==R3PartKind.Armor),Is.True);
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator QuickSearchFindsPartsAndClearsOnlyHiddenSelection()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        var hud=run.GetComponent<R1RunHUD>();
        Invoke(hud,"SelectQuickPart",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        string selected=run.Garage.SelectedInventoryId;int count=run.VehicleModel.Inventory.Count,scrap=run.VehicleModel.Scrap;
        Invoke(hud,"ApplyQuickSearch",run,"pAnCeRz");
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(selected),"A matching selected item stays ready to mount.");
        var entries=((IEnumerable)Invoke(hud,"BuildFilteredQuickCatalogEntries",run.VehicleModel,0,false,"półklin")).Cast<object>().ToArray();
        Assert.That(entries.Length,Is.EqualTo(1),"Search is case and accent insensitive.");
        Invoke(hud,"ApplyQuickSearch",run,"półklin");
        Assert.That(run.Garage.SelectedInventoryId,Is.Null,"A hidden part cannot remain the target of placement.");
        Assert.That(Field(hud,"catalogGroup"),Is.Null);
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(count));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        yield return new ExitPlayMode();
    }

    [Test] public void QuickCategoriesCoverEveryCatalogKindWithoutOverlap()
    {
        var method=typeof(R1RunHUD).GetMethod("QuickCategoryIncludes",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        foreach(var kind in new[]{R3PartKind.Armor,R3PartKind.Wheel,R3PartKind.Gun,R3PartKind.Suspension,R3PartKind.Battery,R3PartKind.BonusModule})
        {
            int hits=Enumerable.Range(0,4).Count(category=>(bool)method.Invoke(null,new object[]{category,kind}));
            Assert.That(hits,Is.EqualTo(1),kind.ToString());
        }
        Assert.That((bool)method.Invoke(null,new object[]{0,R3PartKind.Armor}),Is.True);
        Assert.That((bool)method.Invoke(null,new object[]{1,R3PartKind.Wheel}),Is.True);
        Assert.That((bool)method.Invoke(null,new object[]{2,R3PartKind.Gun}),Is.True);
    }
    [UnityTest] public IEnumerator CategorySwitchCannotBuyOrMountPreviousPartAndCatalogKeepsCategoryInSync()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var hud=run.GetComponent<R1RunHUD>();Assert.That(hud,Is.Not.Null);
        Invoke(hud,"SelectQuickPart",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        Assert.That(run.Garage.SelectedInventoryId,Is.Not.Null);
        int scrap=run.VehicleModel.Scrap;
        Invoke(hud,"SelectQuickCategory",run,1);
        Assert.That(Field(hud,"catalogGroup"),Is.Null,"A previous category must not remain purchasable.");
        Assert.That(run.Garage.SelectedInventoryId,Is.Null,"The old part must not remain selected for placement.");
        Assert.That(run.Garage.UseInventory,Is.True);
        Assert.That(run.Garage.PlacementIssue,Does.Contain("Wybierz"));
        int inventoryCount=run.VehicleModel.Inventory.Count;
        Invoke(hud,"BuyQuickPart",run);
        run.Garage.Select(Vector3Int.up);
        Assert.That(run.Garage.Preview.gameObject.activeSelf,Is.False,"No previous ghost should appear without a selected item.");
        Assert.That(run.Garage.ClickTarget(),Is.False,"Switching category must not mount the previous item.");
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(inventoryCount));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap),"Browsing categories must not purchase anything.");
        Assert.That(run.GaragePurchase(R3PartKind.Wheel),Is.True);
        Invoke(hud,"SelectQuickPart",run,R3PartKind.Wheel,R4WeaponKind.Basic,null);
        run.Garage.Select(Vector3Int.up);
        Assert.That(run.Garage.Preview.gameObject.activeSelf,Is.True,"A selected item must still show its placement ghost.");
        string selected=run.Garage.SelectedInventoryId;
        Invoke(hud,"SelectQuickCategory",run,1);
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(selected),"Re-selecting the same category keeps the chosen item.");
        Invoke(hud,"PortraitCatalogSelectGroup",run,R3PartKind.Gun,R4WeaponKind.Basic,null);
        Invoke(hud,"DismissCatalog");
        Assert.That((int)Field(hud,"quickPartCategory"),Is.EqualTo(2),"The compact list must follow the full catalog's selection.");
    }
    [UnityTest] public IEnumerator SelectionPurchaseRepairAndConfirmedDismantleKeepRealInventorySemantics()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;
        Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        var hud=run.GetComponent<R1RunHUD>();Assert.That(hud,Is.Not.Null);
        run.VehicleModel.Award(1000);
        var kind=R3PartKind.Armor;var weapon=R4WeaponKind.Basic;
        Field(hud,"catalogOpen",true);
        Invoke(hud,"PortraitCatalogSelectGroup",run,kind,weapon,null);
        int funds=run.VehicleModel.Scrap, count=run.VehicleModel.Inventory.Count;
        int price=run.VehicleModel.Price(kind,weapon);
        Invoke(hud,"PortraitCatalogPurchase",run,kind,weapon);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(funds-price));
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(count+1));
        Assert.That(run,Is.Not.Null,"Kontroler po zakupie.");
        Assert.That(run.VehicleModel,Is.Not.Null,"Model po zakupie.");
        Assert.That(run.Garage,Is.Not.Null,"Garaż po zakupie.");
        Debug.Log("Portrait catalog test: purchased inventory exists");
        var part=run.VehicleModel.Inventory[count];
        Debug.Log("Portrait catalog test: got last item");
        Assert.That(part,Is.Not.Null,"Nowy egzemplarz po zakupie.");
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(part.Id));
        Debug.Log("Portrait catalog test: selected id verified");
        Assert.That((bool)Field(hud,"catalogOpen"),Is.False);
        Debug.Log("Portrait catalog test: selection and close verified");
        part.Health=.5f;funds=run.VehicleModel.Scrap;
        int repair=run.VehicleModel.RepairCost(part);
        Field(hud,"catalogOpen",true);
        Invoke(hud,"PortraitCatalogRepair",run,part.Id);
        Debug.Log("Portrait catalog test: repair returned");
        Assert.That(part.Health,Is.EqualTo(1));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(funds-repair));
        Assert.That(run.GaragePurchase(kind,weapon),Is.True);
        var second=run.VehicleModel.Inventory[run.VehicleModel.Inventory.Count-1];second.Health=.35f;
        Field(hud,"catalogOpen",true);
        Invoke(hud,"PortraitCatalogSelectItem",run,second.Id);
        Debug.Log("Portrait catalog test: existing item selected");
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(second.Id));
        Assert.That(second.Health,Is.EqualTo(.35f));
        Assert.That((bool)Field(hud,"catalogOpen"),Is.False);
        funds=run.VehicleModel.Scrap;int value=run.VehicleModel.DismantleValue(part),earned=run.EarnedScrap;
        var playerBeforeDismantle=run.Player;
        Invoke(hud,"PortraitCatalogDismantle",run,part.Id);
        Debug.Log("Portrait catalog test: first dismantle request");
        Assert.That(ContainsId(run.VehicleModel,part.Id),Is.True,"Pierwsze kliknięcie tylko prosi o potwierdzenie.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(funds));
        Assert.That(run.EarnedScrap,Is.EqualTo(earned));
        Invoke(hud,"PortraitCatalogDismantle",run,part.Id);
        Assert.That(ContainsId(run.VehicleModel,part.Id),Is.False);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(funds+value));
        Assert.That(run.EarnedScrap,Is.EqualTo(earned+value));
        Assert.That(run.Player,Is.SameAs(playerBeforeDismantle),"Dismantling an inventory-only item must not rebuild the assembled vehicle.");
        yield return null;
    }
    [UnityTest] public IEnumerator WheelCatalogSeparatesModelsAndBuysChosenAppearance()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        var hud=run.GetComponent<R1RunHUD>();var model=run.VehicleModel;model.Award(1000);
        Invoke(hud,"PortraitCatalogSelectGroup",run,R3PartKind.Wheel,R4WeaponKind.Basic,"wheel:slick");
        Invoke(hud,"PortraitCatalogPurchase",run,R3PartKind.Wheel,R4WeaponKind.Basic);
        var slick=model.Inventory.Last();Assert.That(slick.WheelStyle,Is.EqualTo(R3WheelStyle.Slick));
        Assert.That((bool)Invoke(hud,"InCatalogGroup",slick,R3PartKind.Wheel,R4WeaponKind.Basic,"wheel:slick"),Is.True);
        Assert.That((bool)Invoke(hud,"InCatalogGroup",slick,R3PartKind.Wheel,R4WeaponKind.Basic,null),Is.False);
        Invoke(hud,"PortraitCatalogSelectGroup",run,R3PartKind.Wheel,R4WeaponKind.Basic,null);
        Invoke(hud,"PortraitCatalogPurchase",run,R3PartKind.Wheel,R4WeaponKind.Basic);
        var offroad=model.Inventory.Last();Assert.That(offroad.WheelStyle,Is.EqualTo(R3WheelStyle.Offroad));
        Assert.That((bool)Invoke(hud,"InCatalogGroup",offroad,R3PartKind.Wheel,R4WeaponKind.Basic,null),Is.True);
        Assert.That((bool)Invoke(hud,"InCatalogGroup",offroad,R3PartKind.Wheel,R4WeaponKind.Basic,"wheel:slick"),Is.False);
        foreach(var pair in new[]{("wheel:large",R3WheelStyle.LargeOffroad),("wheel:monster",R3WheelStyle.Monster),
            ("wheel:small",R3WheelStyle.SmallFork),("wheel:balloon",R3WheelStyle.BalloonFork),("wheel:spiked",R3WheelStyle.SpikedFork),("wheel:twin",R3WheelStyle.TwinRoad),("wheel:steer-road",R3WheelStyle.SteerRoad),("wheel:steer-offroad",R3WheelStyle.SteerOffroad),("wheel:caster",R3WheelStyle.CasterSlick),("wheel:swing",R3WheelStyle.SwingOffroad)})
        {
            Invoke(hud,"PortraitCatalogSelectGroup",run,R3PartKind.Wheel,R4WeaponKind.Basic,pair.Item1);
            Invoke(hud,"PortraitCatalogPurchase",run,R3PartKind.Wheel,R4WeaponKind.Basic);
            var item=model.Inventory.Last();Assert.That(item.WheelStyle,Is.EqualTo(pair.Item2));
            Assert.That((bool)Invoke(hud,"InCatalogGroup",item,R3PartKind.Wheel,R4WeaponKind.Basic,pair.Item1),Is.True);
        }
        yield return null;
    }
    [UnityTest] public IEnumerator ArmorCatalogPurchasesWedgeAsSeparateOwnedStack()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        var hud=run.GetComponent<R1RunHUD>();var model=run.VehicleModel;model.Award(1000);
        Invoke(hud,"PortraitCatalogSelectGroup",run,R3PartKind.Armor,R4WeaponKind.Basic,"armor:wedge");
        Invoke(hud,"PortraitCatalogPurchase",run,R3PartKind.Armor,R4WeaponKind.Basic);
        var wedge=model.Inventory.Last();Assert.That(wedge.ArmorShape,Is.EqualTo(R3ArmorShape.Wedge));
        Assert.That((bool)Invoke(hud,"InCatalogGroup",wedge,R3PartKind.Armor,R4WeaponKind.Basic,"armor:wedge"),Is.True);
        Assert.That((bool)Invoke(hud,"InCatalogGroup",wedge,R3PartKind.Armor,R4WeaponKind.Basic,null),Is.False);
        foreach(var pair in new[]{("armor:half",R3ArmorShape.HalfBlock),("armor:pyramid",R3ArmorShape.Pyramid),
            ("armor:round",R3ArmorShape.RoundEdge),("armor:half-wedge",R3ArmorShape.HalfWedge),("armor:dome",R3ArmorShape.Dome)})
        {
            Invoke(hud,"PortraitCatalogSelectGroup",run,R3PartKind.Armor,R4WeaponKind.Basic,pair.Item1);
            Invoke(hud,"PortraitCatalogPurchase",run,R3PartKind.Armor,R4WeaponKind.Basic);
            var item=model.Inventory.Last();Assert.That(item.ArmorShape,Is.EqualTo(pair.Item2));
            Assert.That((bool)Invoke(hud,"InCatalogGroup",item,R3PartKind.Armor,R4WeaponKind.Basic,pair.Item1),Is.True);
            Assert.That((bool)Invoke(hud,"InCatalogGroup",item,R3PartKind.Armor,R4WeaponKind.Basic,"armor:wedge"),Is.False);
        }
        yield return null;
    }
    [UnityTest] public IEnumerator SearchClearsOldSelectionWithoutChangingInventoryOrWallet()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var item=run.VehicleModel.Inventory.Last();var hud=run.GetComponent<R1RunHUD>();
        Field(hud,"catalogOpen",true);
        Invoke(hud,"PortraitCatalogSelectGroup",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        Invoke(hud,"SelectCatalogDetails",run,item.Id);
        int scrap=run.VehicleModel.Scrap,count=run.VehicleModel.Inventory.Count;
        Invoke(hud,"ApplyCatalogSearch",run,"kolo");
        Assert.That(Field(hud,"catalogGroup"),Is.Null);
        Assert.That(run.Garage.SelectedInventoryId,Is.Null);
        Assert.That((bool)Field(hud,"catalogOpen"),Is.True);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(count));
        Assert.That(ContainsId(run.VehicleModel,item.Id),Is.True);
        yield return null;
    }
    [UnityTest] public IEnumerator CatalogSearchKeepsVisibleSelectionButCancelsPendingDismantle()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        run.VehicleModel.Award(100);Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var item=run.VehicleModel.Inventory.Last();var hud=run.GetComponent<R1RunHUD>();
        Field(hud,"catalogOpen",true);Invoke(hud,"PortraitCatalogSelectGroup",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        Invoke(hud,"SelectCatalogDetails",run,item.Id);Invoke(hud,"PortraitCatalogDismantle",run,item.Id);
        int scrap=run.VehicleModel.Scrap,count=run.VehicleModel.Inventory.Count;
        Invoke(hud,"ApplyCatalogSearch",run,"pancerz");
        Assert.That(Field(hud,"catalogGroup"),Is.EqualTo(R3PartKind.Armor));
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(item.Id));
        Assert.That(Field(hud,"dismantleConfirmation"),Is.Null,"Changing a search must cancel a pending destructive action.");
        Invoke(hud,"ApplyCatalogSearch",run,"nieistniejący element");
        Assert.That(Field(hud,"catalogGroup"),Is.Null);
        Assert.That(run.Garage.SelectedInventoryId,Is.Null,"An invisible part must not remain selected.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(count));Assert.That(ContainsId(run.VehicleModel,item.Id),Is.True);
    }
    [Test] public void BatchRepairLabelExplainsShortfallAndFreeCreativeRepair()
    {
        var model=new R3VehicleModel(new R3VehicleDesign());model.Award(5);
        Assert.That(model.PurchaseToInventory(R3PartKind.Armor),Is.True);
        var item=model.Inventory.Single();item.Health=0;
        int cost=model.RepairCost(item);Assert.That(cost,Is.GreaterThan(0));Assert.That(model.Scrap,Is.Zero);
        var method=typeof(R1RunHUD).GetMethod("CatalogRepairLabel",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        string Label(int planned,int eligible,int total)=>(string)method.Invoke(null,new object[]{model,new[]{item},planned,eligible,total});
        Assert.That(Label(0,1,0),Does.Contain($"Brakuje {cost} Z"));
        Assert.That(Label(1,1,cost),Does.Contain($"{cost} Z"));
        typeof(R3VehicleModel).GetProperty("CreativeMode").SetValue(model,true);
        Assert.That(Label(1,1,0),Does.Contain("za darmo"));
        Assert.That(Label(0,0,0),Does.Contain("Brak części do naprawy"));
    }

    [Test] public void BatchRepairPreviewPicksAffordablePartsAndMatchesRealCharges()
    {
        var model=new R3VehicleModel(new R3VehicleDesign());model.Award(17);
        for(int i=0;i<3;i++)Assert.That(model.PurchaseToInventory(R3PartKind.Armor),Is.True);
        var items=model.Inventory.ToArray();items[0].Health=0;items[1].Health=.8f;items[2].Health=.5f;
        var method=typeof(R1RunHUD).GetMethod("CatalogRepairPlan",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        var arguments=new object[]{model,items,0,0};
        var plan=(R3PartState[])method.Invoke(null,arguments);
        Assert.That((int)arguments[2],Is.EqualTo(3));Assert.That(plan.Length,Is.EqualTo(2));
        Assert.That((int)arguments[3],Is.EqualTo(2));
        Assert.That(items.All(item=>item.Health<1),Is.True,"Preview must not mutate parts.");
        int before=model.Scrap;
        foreach(var item in plan)Assert.That(model.RepairInventory(item.Id),Is.True);
        Assert.That(before-model.Scrap,Is.EqualTo((int)arguments[3]));
        Assert.That(items.Count(item=>item.Health==1),Is.EqualTo(2));
        Assert.That(items[0].Health,Is.Zero,"The cheapest repairs must be selected first.");
    }

    [UnityTest] public IEnumerator BatchRepairOnlyChangesTheSelectedInventoryStack()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        for(int i=0;i<2;i++)Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        Assert.That(run.GaragePurchase(R3PartKind.Wheel),Is.True);
        var armor=run.VehicleModel.Inventory.Where(part=>part.Kind==R3PartKind.Armor).ToArray();
        var wheel=run.VehicleModel.Inventory.Last(part=>part.Kind==R3PartKind.Wheel);
        foreach(var part in armor)part.Health=.5f;wheel.Health=.5f;
        int quote=armor.Sum(run.VehicleModel.RepairCost),before=run.VehicleModel.Scrap;
        var player=run.Player;var hud=run.GetComponent<R1RunHUD>();
        Invoke(hud,"RepairCatalogStack",run,armor);
        Assert.That(armor.All(part=>part.Health==1),Is.True);Assert.That(wheel.Health,Is.EqualTo(.5f));
        Assert.That(before-run.VehicleModel.Scrap,Is.EqualTo(quote));
        Assert.That(run.Player,Is.SameAs(player),"Inventory repairs must not rebuild the vehicle.");
        Assert.That((string)Field(hud,"catalogFeedback"),Does.Contain("2").And.Contain(quote+" Z"));
        Assert.That(run.Garage.Message,Does.Contain("2").And.Contain(quote+" Z"),"A quick garage repair must show its result without opening the catalog.");
        yield return null;
    }
    [Test] public void NextMountSelectionPrefersUsablePartButKeepsWreckAvailableForRepair()
    {
        var select=typeof(R3GarageView).GetMethod("NextOwnedMountPart",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(select,Is.Not.Null);
        var wreck=new R3PartState(default,R3PartKind.Armor){Health=0};
        var damaged=new R3PartState(default,R3PartKind.Armor){Health=.4f};
        var healthy=new R3PartState(default,R3PartKind.Armor){Health=1};
        object Pick(R3PartState[] parts,bool includeWreck)=>select.Invoke(null,new object[]{parts,includeWreck});
        Assert.That(Pick(new[]{wreck,damaged,healthy},true),Is.SameAs(healthy));
        Assert.That(Pick(new[]{wreck,damaged},true),Is.SameAs(damaged));
        Assert.That(Pick(new[]{wreck},false),Is.Null,"Creative mode can first offer another free piece.");
        Assert.That(Pick(new[]{wreck},true),Is.SameAs(wreck));
        Assert.That(Pick(Array.Empty<R3PartState>(),true),Is.Null);
    }

    [UnityTest] public IEnumerator QuickSingleRepairChangesOnlySelectedInstanceAndPreservesSelection()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var items=run.VehicleModel.Inventory.Where(part=>part.Kind==R3PartKind.Armor).ToArray();
        items[0].Health=.5f;items[1].Health=.2f;
        var hud=run.GetComponent<R1RunHUD>();Invoke(hud,"SelectQuickPart",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(items[0].Id));
        int before=run.VehicleModel.Scrap,price=run.VehicleModel.RepairCost(items[0]);var vehicle=run.Player;
        Assert.That((bool)Invoke(hud,"RepairQuickSelected",run),Is.True);
        Assert.That(items[0].Health,Is.EqualTo(1));Assert.That(items[1].Health,Is.EqualTo(.2f));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-price));
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(items[0].Id));Assert.That(run.Player,Is.SameAs(vehicle));
        Assert.That(run.Garage.Message,Does.Contain(price+" Z"));
        Assert.That((bool)Invoke(hud,"RepairQuickSelected",run),Is.False,"A healthy part cannot be charged twice.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(before-price));
        yield return null;
    }
    [UnityTest] public IEnumerator QuickSingleRepairIsFreeInCreativeWorkshop()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var item=run.VehicleModel.Inventory.Last();item.Health=.3f;
        var hud=run.GetComponent<R1RunHUD>();Invoke(hud,"SelectQuickPart",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        int scrap=run.VehicleModel.Scrap;
        Assert.That((bool)Invoke(hud,"RepairQuickSelected",run),Is.True);
        Assert.That(item.Health,Is.EqualTo(1));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(run.Garage.Message,Does.Contain("0 Z"));
        yield return null;
    }

    [UnityTest] public IEnumerator QuickRepairSelectsRecoveredWreckOnlyWhenMounting()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var item=run.VehicleModel.Inventory.Last();item.Health=0;
        var hud=run.GetComponent<R1RunHUD>();Field(hud,"catalogOpen",false);
        Invoke(hud,"SelectQuickPart",run,R3PartKind.Armor,R4WeaponKind.Basic,null);
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(item.Id));
        run.Garage.Select(run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Core).Cell);
        Invoke(hud,"RepairCatalogStack",run,new[]{item});
        Assert.That(item.Health,Is.EqualTo(1));Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(item.Id));
        Assert.That(run.Garage.Tool,Is.EqualTo(R41GarageTool.Mount));
        Assert.That(run.Garage.Message,Does.Contain("Naprawiono"));
        Assert.That((string)Invoke(hud,"SimpleGarageFeedback",run),Does.Contain("Naprawiono"),
            "Bulk repair feedback must remain visible while a placement ghost is present.");

        item.Health=0;run.Garage.SelectedInventoryId=null;run.Garage.SetTool(R41GarageTool.Inspect);
        Invoke(hud,"RepairCatalogStack",run,new[]{item});
        Assert.That(item.Health,Is.EqualTo(1));Assert.That(run.Garage.SelectedInventoryId,Is.Null);
        Assert.That(run.Garage.Tool,Is.EqualTo(R41GarageTool.Inspect),"Repair must not replace the player's current tool.");
        yield return null;
    }

    [UnityTest] public IEnumerator OwnedFilterIncludesWrecksAndCannotKeepAnInvisiblePurchaseSelection()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float timeout=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<timeout)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(100);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var wreck=run.VehicleModel.Inventory.Last();wreck.Health=0;
        var hud=run.GetComponent<R1RunHUD>();int scrap=run.VehicleModel.Scrap;
        Invoke(hud,"SelectQuickPart",run,R3PartKind.Wheel,R4WeaponKind.Basic,null);
        Invoke(hud,"ToggleQuickOwnedOnly",run);
        Assert.That((bool)Field(hud,"quickOwnedOnly"),Is.True);
        Assert.That(Field(hud,"catalogGroup"),Is.Null,"A non-owned group must not remain a hidden purchase target.");
        Assert.That(run.Garage.Message,Does.Contain("wszystkie"),"The filter must explain how to reach other parts.");
        var owned=((IEnumerable)Invoke(hud,"BuildQuickCatalogEntries",run.VehicleModel,0,true)).Cast<object>().ToArray();
        Assert.That(owned.Length,Is.EqualTo(1),"A wreck must remain listed so the player can repair it.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));Assert.That(ContainsId(run.VehicleModel,wreck.Id),Is.True);
        Field(hud,"catalogOpen",true);
        Invoke(hud,"PortraitCatalogSelectGroup",run,R3PartKind.Wheel,R4WeaponKind.Basic,null);
        Invoke(hud,"ClosePartCatalog");
        Assert.That((bool)Field(hud,"quickOwnedOnly"),Is.False,"A group deliberately selected in the full catalog must remain visible on return.");
        yield return null;
    }
    [UnityTest] public IEnumerator EscapeLeavesGarageSearchBeforeOpeningPause()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        var hud=run.GetComponent<R1RunHUD>();
        var editing=typeof(R1RunHUD).GetField("quickSearchEditing",BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.That(editing,Is.Not.Null,"The HUD must remember that the player is editing search between GUI and input updates.");
        editing.SetValue(hud,true);
        run.HandleEscape();
        Assert.That(run.Paused,Is.False,"The first Escape should leave search, not pause.");
        Assert.That((bool)editing.GetValue(hud),Is.False);
        yield return null;
        run.HandleEscape();Assert.That(run.Paused,Is.True,"A second Escape opens pause.");
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
}
