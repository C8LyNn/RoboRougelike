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

    [UnityTest] public IEnumerator HomeFramesVehicleWithoutLosingBuildSelection()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var item=run.VehicleModel.Inventory.Last();var garage=run.Garage;garage.ChooseInventory(item.Id);
        garage.Zoom(-100);Assert.That(garage.Distance,Is.EqualTo(3));
        garage.HandleGuiPointer(new Event{type=EventType.KeyDown,keyCode=KeyCode.Home},Vector2.zero);
        Assert.That(garage.Distance,Is.GreaterThanOrEqualTo(4.5f));
        Assert.That(garage.SelectedInventoryId,Is.EqualTo(item.Id));
        garage.Zoom(-100);
        garage.HandleGuiPointer(new Event{type=EventType.KeyDown,keyCode=KeyCode.Home,control=true},Vector2.zero);
        Assert.That(garage.Distance,Is.EqualTo(3),"Modified keys must not trigger the shortcut.");
        yield return null;
    }

    [UnityTest] public IEnumerator GarageUndersideUsesReassignedKey()
    {
        var profile=R1ControlProfile.Current;var old=profile.Snapshot();
        try
        {
            profile.ResetDefaults();Assert.That(profile.TryBind("GarageUnderside","K"),Is.True);
            var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
            var garage=run.Garage;var field=typeof(R3GarageView).GetField("pitch",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            float before=(float)field.GetValue(garage);
            garage.HandleGuiPointer(new Event{type=EventType.KeyDown,keyCode=KeyCode.U},Vector2.zero);
            Assert.That((float)field.GetValue(garage),Is.EqualTo(before));
            garage.HandleGuiPointer(new Event{type=EventType.KeyDown,keyCode=KeyCode.K},Vector2.zero);
            Assert.That((float)field.GetValue(garage),Is.Not.EqualTo(before));
            float changed=(float)field.GetValue(garage);
            garage.HandleGuiPointer(new Event{type=EventType.KeyDown,keyCode=KeyCode.K,control=true},Vector2.zero);
            Assert.That((float)field.GetValue(garage),Is.EqualTo(changed));
            yield return null;
        }
        finally{typeof(R1ControlProfile).GetMethod("RestoreValues",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(profile,new object[]{old});}
    }

    [UnityTest] public IEnumerator EscapeClosesEnemyMenuBeforeResumingSimulation()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);
        while(run.Transitioning)yield return null;
        run.TogglePause();
        Assert.That(run.SpawnCreativeEnemy(R3EnemyArchetype.Gunner),Is.True);
        var hud=run.GetComponent<R1RunHUD>();
        var menu=typeof(R1RunHUD).GetField("creativeEnemyMenu",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        menu.SetValue(hud,true);
        var enemy=run.GetComponent<R6TestRange>().Layout.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().First();
        var active=enemy.GetType().GetField("combatActive",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        run.HandleEscape();
        Assert.That(run.Paused,Is.True,"Closing a submenu must not resume combat.");
        Assert.That((bool)menu.GetValue(hud),Is.False);
        Assert.That((bool)active.GetValue(enemy),Is.False);
        yield return null;
        run.HandleEscape();
        Assert.That(run.Paused,Is.False);
        Assert.That((bool)active.GetValue(enemy),Is.True);
    }

    [UnityTest] public IEnumerator SpawnedCreativeEnemyIsVisibleToAutonomousTurretAndScanners()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var range=run.GetComponent<R6TestRange>();var training=range.TrainingTarget;
        Assert.That(run.SpawnCreativeEnemy(R3EnemyArchetype.Gunner),Is.True);
        var enemy=range.Layout.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().Single(e=>e!=training);
        enemy.SetCombatActive(false);enemy.GetComponent<Rigidbody>().isKinematic=true;
        if(training!=null)training.gameObject.SetActive(false);range.SetTrainingTarget(null);Physics.SyncTransforms();
        Assert.That(RoboRougelike.PackageIntegration.RRLScanWorld.LiveEnemies(run),Does.Contain(enemy),"Enemy spawned from the range menu must be a discoverable target.");
        var registryObject=new GameObject("Test rejestru skanera");
        var registry=registryObject.AddComponent<RoboRougelike.Mechanics.StructuralScan.StructuralScanRegistry>();
        RoboRougelike.PackageIntegration.RRLScanWorld.Register(registry,run);
        Assert.That(registry.Parts.Any(p=>p.OwnerKey==enemy.GetInstanceID()),Is.True,"The structural scanner must register the spawned enemy's parts.");
        RoboRougelike.PackageIntegration.RRLPulseSignal.Prepare(run);
        Assert.That(enemy.GetComponent<RoboRougelike.PackageIntegration.RRLPulseSignal>()?.Tag,Is.Not.Null,"The pulse scanner must prepare an enemy signal.");
        var turret=GameObject.CreatePrimitive(PrimitiveType.Cube);turret.transform.position=enemy.Core.transform.position+Vector3.up*8;
        turret.transform.localScale=Vector3.one*.4f;var body=turret.AddComponent<Rigidbody>();body.isKinematic=true;
        var weapon=turret.AddComponent<R3FlyingDrone>();weapon.ConfigureTurret(run.Player.transform,.2f,1);Physics.SyncTransforms();
        var nextShot=typeof(R3FlyingDrone).GetField("nextShot",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        float deadline=Time.time+3;while(Time.time<deadline&&(float)nextShot.GetValue(weapon)<=0)yield return new WaitForFixedUpdate();
        Assert.That((float)nextShot.GetValue(weapon),Is.GreaterThan(0),"An autonomous turret must fire at the spawned enemy.");
        run.ClearCreativeEnemies();Assert.That(RoboRougelike.PackageIntegration.RRLScanWorld.LiveEnemies(run).Contains(enemy),Is.False);
        Object.Destroy(turret);Object.Destroy(registryObject);
    }

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
        var rotation=new Vector3Int(0,1,0);
        Assert.That(run.GarageBuy(Vector3Int.up,R3PartKind.Armor,rotation),Is.True);
        yield return null;Physics.SyncTransforms();
        var origin=run.Player.transform.TransformPoint(new Vector3(0,4,0));
        var ray=new Ray(origin,run.Player.transform.TransformDirection(Vector3.down));
        int scrap=run.VehicleModel.Scrap;
        Assert.That(run.Garage.SelectPartFromRay(ray),Is.True,"The mounted armor should be selectable via a real ray.");
        var selected=run.VehicleModel.Inventory.Single(p=>p.Id==run.Garage.SelectedInventoryId);
        Assert.That(selected.Kind,Is.EqualTo(R3PartKind.Armor));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(run.Garage.SelectedRotation,Is.EqualTo(rotation),"The placement preview keeps the pointed part's rotation.");
    }

    [UnityTest] public IEnumerator MountedRotationExplainsCoreAndWheelRestrictions()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        var garage=run.Garage;
        var method=typeof(R3GarageView).GetMethod("RotatePartAt");Assert.That(method,Is.Not.Null);
        Assert.That((bool)method.Invoke(garage,new object[]{Vector3Int.zero,Vector3Int.up}),Is.False);
        Assert.That(garage.Message,Does.Contain("rdzenia"));
        Assert.That((bool)method.Invoke(garage,new object[]{new Vector3Int(5,5,5),Vector3Int.up}),Is.False);
        Assert.That(garage.Message,Does.Contain("Wskaż"));
        Assert.That(run.GarageBuy(new Vector3Int(1,0,0),R3PartKind.Wheel),Is.True);
        Assert.That((bool)method.Invoke(garage,new object[]{new Vector3Int(1,0,0),Vector3Int.right}),Is.False);
        Assert.That(garage.Message,Does.Contain("Koło").And.Contain("pionowo"));
        Assert.That(R3VehicleModel.ValidWheelOrientation(run.VehicleModel.At(new Vector3Int(1,0,0)).RotationQuarterTurns),Is.True);
        Assert.That(run.GarageBuy(Vector3Int.up,R3PartKind.Armor),Is.True);
        var armor=run.VehicleModel.At(Vector3Int.up);string id=armor.Id;int scrap=run.VehicleModel.Scrap;
        Assert.That((bool)method.Invoke(garage,new object[]{Vector3Int.up,Vector3Int.up}),Is.True);
        Assert.That(run.VehicleModel.At(Vector3Int.up).Id,Is.EqualTo(id));
        Assert.That(run.VehicleModel.At(Vector3Int.up).RotationQuarterTurns,Is.EqualTo(Vector3Int.up));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
        yield return null;
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
        Assert.That(model.Buy(new Vector3Int(0,1,12),R3PartKind.Gun,R4WeaponKind.Shotgun),Is.True);
        Assert.That(model.Buy(new Vector3Int(0,1,15),R3PartKind.Gun,R4WeaponKind.Railgun),Is.True);
        Assert.That(model.WeaponInSlot(R4WeaponKind.Heavy,2),Is.True,"Trzeci typ ma własny slot.");
        Assert.That(model.WeaponInSlot(R4WeaponKind.Railgun,1),Is.True,"Nadmiarowe typy kreatywne obsługuje PPM.");
        var groups=typeof(R1RunHUD).GetMethod("GarageWeaponGroups",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        Assert.That((string)groups.Invoke(null,new object[]{model}),Does.Contain("PPM").And.Contain("pozostałe"));
        Assert.That(model.AssignWeaponSlot(R4WeaponKind.Railgun,2),Is.True,"Nadmiarowy rodzaj w trybie kreatywnym musi dać się wybrać do slotu.");
        Assert.That(model.WeaponAtSlot(2),Is.EqualTo(R4WeaponKind.Railgun));
        Assert.That(model.Buy(new Vector3Int(0,1,20),R3PartKind.Booster),Is.True);
        Assert.That(model.Buy(new Vector3Int(0,1,22),R3PartKind.JumpDrive),Is.True);
        Assert.That(model.Buy(new Vector3Int(0,1,24),R3PartKind.Blink),Is.True);
        Assert.That(model.Buy(new Vector3Int(0,1,26),R3PartKind.MagneticGrapple),Is.True);
        Assert.That(model.Buy(new Vector3Int(0,1,28),R3PartKind.RepairPulse),Is.True);
        Assert.That(model.AssignActiveSlot(R3PartKind.RepairPulse,3),Is.True,"Nadmiarowy moduł kreatywny musi dać się wybrać do slotu.");
        Assert.That(model.GetActiveSlots()[3],Is.EqualTo(R3PartKind.RepairPulse));
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

    [UnityTest] public IEnumerator CreativeCopyFromRayCreatesCorrectTierWithoutChangingExistingParts()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        Assert.That(run.GarageBuy(Vector3Int.up,R3PartKind.Armor,new Vector3Int(0,1,0)),Is.True);
        var source=run.VehicleModel.At(Vector3Int.up);source.Tier=2;
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var lower=run.VehicleModel.Inventory.Last();string lowerId=lower.Id;
        yield return null;Physics.SyncTransforms();
        var origin=run.Player.transform.TransformPoint(new Vector3(0,4,0));
        var ray=new Ray(origin,run.Player.transform.TransformDirection(Vector3.down));
        Assert.That(run.Garage.SelectPartFromRay(ray),Is.True);
        R3PartState selected=null;
        foreach(var item in run.VehicleModel.Inventory)if(item.Id==run.Garage.SelectedInventoryId){selected=item;break;}
        Assert.That(selected,Is.Not.Null);Assert.That(selected.Tier,Is.EqualTo(2));
        Assert.That(selected.Id,Is.Not.EqualTo(lowerId));Assert.That(lower.Tier,Is.EqualTo(1));
        Assert.That(run.Garage.SelectedRotation,Is.EqualTo(source.RotationQuarterTurns));
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(2));Assert.That(run.VehicleModel.Scrap,Is.Zero);
        Assert.That(run.Garage.SelectPartFromRay(ray),Is.True);
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(2),"Re-selecting must not mint duplicates.");
    }

    [UnityTest] public IEnumerator CreativeRepeatedPlacementPreservesSelectedTier()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var first=run.VehicleModel.Inventory.Last();first.Tier=2;string id=first.Id;
        var garage=run.Garage;garage.ChooseInventory(id);garage.Select(Vector3Int.up);
        Assert.That(garage.PreviewValid,Is.True,garage.PlacementIssue);
        Assert.That(garage.ClickTarget(),Is.True,garage.Message);
        var next=run.VehicleModel.Inventory.Single(p=>p.Id==garage.SelectedInventoryId);
        Assert.That(next.Id,Is.Not.EqualTo(id));Assert.That(next.Tier,Is.EqualTo(2));
        Assert.That(run.VehicleModel.Scrap,Is.Zero);
        yield return null;
    }

    [UnityTest] public IEnumerator CreativeRepeatedBonusKeepsItsEffectAndTier()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenCreativeGarage(),Is.True);
        var definition=run.Definition.Upgrades.First(item=>R3VehicleModel.CreateBonusReward(item)!=null);
        Assert.That(run.AddCreativeBonus(definition.Id),Is.True);
        var first=run.VehicleModel.Inventory.Last();first.Tier=3;
        first.ContactMask=R3MountingGrid.FaceBit(Vector3Int.down);string id=first.Id;
        Assert.That(run.AddCreativeBonus(definition.Id),Is.True);
        var other=run.VehicleModel.Inventory.Last();other.Tier=3;
        var garage=run.Garage;garage.ChooseInventory(id);garage.Select(Vector3Int.up);
        Assert.That(garage.PreviewValid,Is.True,garage.PlacementIssue);
        Assert.That(garage.ClickTarget(),Is.True,garage.Message);
        R3PartState next=null;
        foreach(var item in run.VehicleModel.Inventory)if(item.Id==garage.SelectedInventoryId){next=item;break;}
        Assert.That(next,Is.Not.Null);Assert.That(next.Id,Is.Not.EqualTo(id));Assert.That(next.Tier,Is.EqualTo(3));
        Assert.That(next.Id,Is.Not.EqualTo(other.Id),"A different socket mask must not be selected silently.");
        Assert.That(R3VehicleModel.SameBonusType(first,next),Is.True);
        Assert.That(next.ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.down)));
        Assert.That(run.VehicleModel.Scrap,Is.Zero);
        yield return null;
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
