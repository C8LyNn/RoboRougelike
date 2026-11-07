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
        funds=run.VehicleModel.Scrap;int value=run.VehicleModel.DismantleValue(part);
        Invoke(hud,"PortraitCatalogDismantle",run,part.Id);
        Debug.Log("Portrait catalog test: first dismantle request");
        Assert.That(ContainsId(run.VehicleModel,part.Id),Is.True,"Pierwsze kliknięcie tylko prosi o potwierdzenie.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(funds));
        Invoke(hud,"PortraitCatalogDismantle",run,part.Id);
        Assert.That(ContainsId(run.VehicleModel,part.Id),Is.False);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(funds+value));
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
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
}
