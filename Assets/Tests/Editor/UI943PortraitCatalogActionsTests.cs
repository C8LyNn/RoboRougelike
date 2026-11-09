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
    static object Invoke(R1RunHUD hud,string name,params object[] args)
    {
        var method=typeof(R1RunHUD).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null,$"Brakuje operacji katalogu: {name}");
        return method.Invoke(hud,args);
    }
    static void Field(R1RunHUD hud,string name,object value)=>typeof(R1RunHUD).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(hud,value);
    static object Field(R1RunHUD hud,string name)=>typeof(R1RunHUD).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
    static bool ContainsId(R3VehicleModel model,string id)=>model.Inventory.Any(p=>p.Id==id);

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
    [TestCase(0,false,false,"Koło × 0 • blokada")]
    [TestCase(0,false,true,"Koło × 0")]
    public void StackCaptionDistinguishesWrecksFromUsableOrLockedParts(int count,bool usable,bool unlocked,string expected)
    {
        var method=typeof(R1RunHUD).GetMethod("CatalogStackCaption",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null);
        Assert.That((string)method.Invoke(null,new object[]{"Koło",count,usable,unlocked}),Is.EqualTo(expected));
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
        Assert.That(run.VehicleModel.DeployError,Does.Contain("2 różne typy"));
        Assert.That(run.VehicleModel.CriticalReadinessMessage,Does.Contain("napędu"));
        var feedback=(string)Invoke(run.GetComponent<R1RunHUD>(),"SimpleGarageFeedback",run);
        Assert.That(feedback,Does.Contain(run.VehicleModel.DeployError));
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
        Assert.That(run.Garage.SelectedInventoryId,Is.Null);
        Invoke(hud,"RepairCatalogStack",run,new[]{item});
        Assert.That(item.Health,Is.EqualTo(1));Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(item.Id));
        Assert.That(run.Garage.Tool,Is.EqualTo(R41GarageTool.Mount));
        Assert.That(run.Garage.Message,Does.Contain("miejsce montażu"));

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
        var owned=((IEnumerable)Invoke(hud,"BuildQuickCatalogEntries",run.VehicleModel,0,true)).Cast<object>().ToArray();
        Assert.That(owned.Length,Is.EqualTo(1),"A wreck must remain listed so the player can repair it.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));Assert.That(ContainsId(run.VehicleModel,wreck.Id),Is.True);
        Field(hud,"catalogOpen",true);
        Invoke(hud,"PortraitCatalogSelectGroup",run,R3PartKind.Wheel,R4WeaponKind.Basic,null);
        Invoke(hud,"ClosePartCatalog");
        Assert.That((bool)Field(hud,"quickOwnedOnly"),Is.False,"A group deliberately selected in the full catalog must remain visible on return.");
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
}
