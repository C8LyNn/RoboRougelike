using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RoboRougelike.R4;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

public class CreativeRangeTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null) yield return null;
    }

    [UnityTearDown] public IEnumerator Teardown()
    { if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();} }

    [UnityTest] public IEnumerator CreativeGarageStartsWithCoreAndOffersFreeParts()
    {
        var run=R1RunController.Instance;
        var open=typeof(R1RunController).GetMethod("OpenCreativeGarage");
        Assert.That(open,Is.Not.Null,"Brak wejścia do kreatywnego garażu.");
        Assert.That((bool)open.Invoke(run,null),Is.True);
        Assert.That(run.VehicleModel.Parts.Count,Is.EqualTo(1));
        Assert.That(run.VehicleModel.Parts.Single().Kind,Is.EqualTo(R3PartKind.Core));
        int scrap=run.VehicleModel.Scrap;
        Assert.That(run.GaragePurchase(R3PartKind.Wheel),Is.True);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(run.VehicleModel.Price(R3PartKind.Wheel),Is.Zero);
        var bonus=run.Definition.Upgrades.First(item=>R3VehicleModel.CreateBonusReward(item)!=null);
        Assert.That(run.AddCreativeBonus(bonus.Id),Is.True);
        Assert.That(run.VehicleModel.Inventory.Any(item=>item.Kind==R3PartKind.BonusModule&&item.UpgradeId==bonus.Id),Is.True);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(run.StartNextEncounter(),Is.False);
        Assert.That(run.SaveRunCheckpoint(),Is.False,"Darmowe części nie mogą wejść do zapisu wyprawy.");
        yield return null;
    }

    [UnityTest] public IEnumerator CreativeCatalogExplainsHowToAddUnownedParts()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        var hud=run.GetComponent<R1RunHUD>();
        var select=typeof(R1RunHUD).GetMethod("SelectQuickPart",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(select,Is.Not.Null);
        select.Invoke(hud,new object[]{run,R3PartKind.Armor,R4WeaponKind.Basic,null});
        Assert.That(run.Garage.Message,Does.Contain("za darmo"));
        Assert.That(run.Garage.Message,Does.Not.Contain("Kup"));
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(1),"Jedno kliknięcie przygotowuje część kreatywną.");
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(run.VehicleModel.Inventory[0].Id));
        select.Invoke(hud,new object[]{run,R3PartKind.Armor,R4WeaponKind.Basic,null});
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(1),"Ponowny wybór nie tworzy duplikatu.");
        var bonus=run.Definition.Upgrades.Select(R3VehicleModel.CreateBonusReward).First(item=>item!=null);
        var key=typeof(R1RunHUD).GetMethod("CatalogBonusKey",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        select.Invoke(hud,new object[]{run,R3PartKind.BonusModule,R4WeaponKind.Basic,key.Invoke(null,new object[]{bonus})});
        Assert.That(run.Garage.Message,Does.Contain("za darmo"));
        Assert.That(run.Garage.Message,Does.Not.Contain("napraw"));
        Assert.That(run.VehicleModel.Inventory.Any(p=>p.Id==run.Garage.SelectedInventoryId&&p.UpgradeId==bonus.UpgradeId),Is.True);
        int count=run.VehicleModel.Inventory.Count;
        select.Invoke(hud,new object[]{run,R3PartKind.BonusModule,R4WeaponKind.Basic,"nieistniejący bonus"});
        Assert.That(run.Garage.Message,Does.Contain("niedostępna"));
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(count));
        yield return null;
    }

    [UnityTest] public IEnumerator CopyFromVehicleRaySuppliesFreePartForCreativeBuilding()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        Assert.That(run.GarageBuy(Vector3Int.up,R3PartKind.Armor),Is.True);
        yield return null;Physics.SyncTransforms();
        var origin=run.Player.transform.TransformPoint(new Vector3(0,4,0));
        var ray=new Ray(origin,run.Player.transform.TransformDirection(Vector3.down));
        int scrap=run.VehicleModel.Scrap;
        Assert.That(run.Garage.SelectPartFromRay(ray),Is.True,"The mounted armor should be selectable via a real ray.");
        var selected=run.VehicleModel.Inventory.Single(p=>p.Id==run.Garage.SelectedInventoryId);
        Assert.That(selected.Kind,Is.EqualTo(R3PartKind.Armor));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
    }

    [UnityTest] public IEnumerator CreativeModeRemovesPartAndStockLimitsAndKeepsWeaponControls()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        var model=run.VehicleModel.DeepCopy();
        for(int z=1;z<=181;z++)
            Assert.That(model.Buy(new Vector3Int(0,0,z),R3PartKind.Armor),Is.True,$"blok {z}");
        Assert.That(model.Parts.Count,Is.GreaterThan(180));
        for(int i=0;i<201;i++)Assert.That(model.PurchaseToInventory(R3PartKind.Wheel),Is.True);
        Assert.That(model.Inventory.Count,Is.GreaterThan(200));
        Assert.That(model.Buy(new Vector3Int(0,1,3),R3PartKind.Gun,R4WeaponKind.Basic),Is.True);
        Assert.That(model.Buy(new Vector3Int(0,1,6),R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        Assert.That(model.Buy(new Vector3Int(0,1,9),R3PartKind.Gun,R4WeaponKind.Heavy),Is.True);
        Assert.That(model.WeaponInGroup(R4WeaponKind.Heavy,true),Is.True,"Dodatkowe typy obsługuje PPM.");
        var groups=typeof(R1RunHUD).GetMethod("GarageWeaponGroups",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        Assert.That((string)groups.Invoke(null,new object[]{model}),Does.Contain("PPM").And.Contain("pozostałe"));
        Assert.That(model.Scrap,Is.Zero);
        yield return null;
    }

    [UnityTest] public IEnumerator UnarmedVehicleCanUseLargeRangeAndSpawnAndClearEnemies()
    {
        var run=R1RunController.Instance;
        Assert.That(run.OpenCreativeGarage(),Is.True);
        Assert.That(run.GarageBuy(new Vector3Int(-1,0,0),R3PartKind.Wheel),Is.True);
        Assert.That(run.GarageBuy(new Vector3Int(1,0,0),R3PartKind.Wheel),Is.True);
        Assert.That(run.EnterTestRange(),Is.True,"Jazda próbna nie wymaga broni.");
        while(run.Transitioning)yield return null;
        yield return null;yield return null;
        Assert.That(run.Paused,Is.False,"Brak broni nie może zatrzymać jazdy próbnej.");
        Assert.That(run.Player.Controller.GameplayInputEnabled,Is.True);
        var range=run.GetComponent<R6TestRange>();
        var floor=range.Layout.Find("Podłoże kreatywnego poligonu");
        Assert.That(floor,Is.Not.Null);
        Assert.That(floor.GetComponent<Collider>().bounds.size.x,Is.EqualTo(300).Within(.1f));
        run.TogglePause();Assert.That(run.Paused,Is.True);
        Assert.That(run.SpawnCreativeEnemy(R3EnemyArchetype.Gunner),Is.True);
        Assert.That(range.CreativeEnemyCount,Is.EqualTo(1));
        var enemy=range.Layout.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().First();
        var active=enemy.GetType().GetField("combatActive",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That((bool)active.GetValue(enemy),Is.False,"Nowy przeciwnik czeka w pauzie.");
        run.TogglePause();Assert.That((bool)active.GetValue(enemy),Is.True,"Przeciwnik działa po wznowieniu.");
        run.ClearCreativeEnemies();
        Assert.That(range.CreativeEnemyCount,Is.Zero);
        Assert.That(run.ReturnFromTestRange(),Is.True);
        while(run.Transitioning)yield return null;
        Assert.That(run.VehicleModel.Parts.Count,Is.EqualTo(3));
        Assert.That(run.VehicleModel.CreativeMode,Is.True);
        Assert.That(GameObject.Find("P12_Ground"),Is.Not.Null);
    }

    [UnityTest] public IEnumerator CreativeGarageKeepsSelectedPartForRepeatedPlacement()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var garage=run.Garage;garage.ChooseInventory(run.VehicleModel.Inventory.Last().Id);
        yield return null;
        var socket=garage.AvailableMountPoints.First(p=>garage.Point(new Ray(
            p.Position+(p.Rotation*Vector3.forward)*2,-(p.Rotation*Vector3.forward)))&&garage.PreviewValid);
        Assert.That(garage.ClickTarget(),Is.True);
        Assert.That(run.VehicleModel.Parts.Count,Is.EqualTo(2));
        Assert.That(garage.SelectedInventoryId,Is.Not.Null.And.Not.Empty);
        Assert.That(run.VehicleModel.Inventory.Any(p=>p.Id==garage.SelectedInventoryId&&p.Kind==R3PartKind.Armor),Is.True);
        Assert.That(run.VehicleModel.Scrap,Is.Zero);
    }

#if ENABLE_INPUT_SYSTEM
    [UnityTest] public IEnumerator UnarmedVehicleActuallyDrivesOnCreativeGround()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        Assert.That(run.GarageBuy(new Vector3Int(0,0,1),R3PartKind.Armor),Is.True);
        Assert.That(run.GarageBuy(new Vector3Int(0,0,2),R3PartKind.Armor),Is.True);
        foreach(var cell in new[]{new Vector3Int(-1,0,0),new Vector3Int(1,0,0),new Vector3Int(-1,0,2),new Vector3Int(1,0,2)})
            Assert.That(run.GarageBuy(cell,R3PartKind.Wheel),Is.True,cell.ToString());
        Assert.That(run.EnterTestRange(),Is.True);
        while(run.Transitioning)yield return null;
        float settleUntil=Time.time+.3f;while(Time.time<settleUntil)yield return null;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        var background=InputSystem.settings.backgroundBehavior;
        var runInBackground=Application.runInBackground;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        Application.runInBackground=true;
        var start=run.Player.transform.position;
        try
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputSystem.Update();
            // In a background EditMode-hosted Play Mode test the editor does not
            // dispatch the unfocused player's Update; poll the real input handler.
            var inputUpdate=typeof(RogueMachine.P12PipelineSafe.P12PipelineSafeTankController).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            float end=Time.time+1.5f;while(Time.time<end){inputUpdate.Invoke(run.Player.Controller,null);yield return null;}
            Assert.That(run.Paused,Is.False);
            var drive=run.Player.GetComponent<R6VehicleDrive>();
            var move=typeof(RogueMachine.P12PipelineSafe.P12PipelineSafeTankController).GetField("moveInput",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var rb=run.Player.GetComponent<Rigidbody>();
            TestContext.WriteLine($"Dystans={Vector3.Distance(start,run.Player.transform.position):F3}; input={move.GetValue(run.Player.Controller)}; wheels={drive.Wheels}; grounded={drive.GroundedWheels}; position={run.Player.transform.position}; keyboard={keyboard.wKey.isPressed} current={Keyboard.current==keyboard}; enabled={run.Player.Controller.isActiveAndEnabled}; inputEnabled={run.Player.Controller.GameplayInputEnabled}; bodyKinematic={rb.isKinematic}; gravity={rb.useGravity}; timescale={Time.timeScale}; fixed={Time.fixedTime}; rootActive={run.Player.gameObject.activeInHierarchy}");
            Assert.That(Vector3.Distance(start,run.Player.transform.position),Is.GreaterThan(.1f),"Pojazd musi rzeczywiście przejechać dystans.");
        }
        finally{InputSystem.settings.backgroundBehavior=background;Application.runInBackground=runInBackground;InputSystem.RemoveDevice(keyboard);}
    }
#endif
}
