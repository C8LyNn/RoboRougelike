using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
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
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(item.Id));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));Assert.That(item.Health,Is.Zero);
        var playerBeforeRepair=run.Player;
        Invoke(hud,"PortraitCatalogRepair",run,item.Id);
        Assert.That(item.Health,Is.EqualTo(1));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap-quote));
        Assert.That(run.Player,Is.SameAs(playerBeforeRepair),"Repairing an inventory-only item must not rebuild the assembled vehicle.");
        Assert.That(run.VehicleModel.Inventory.Any(p=>p.Id==item.Id),Is.True,"Repair must retain the owned instance.");
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
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
}
