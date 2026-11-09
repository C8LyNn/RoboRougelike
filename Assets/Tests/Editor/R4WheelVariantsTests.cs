using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RoboRougelike.R4_1;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4WheelVariantsTests
{
    [UnitySetUp]public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;}
    [UnityTest]public IEnumerator RealGarageSwitchesWheelAppearanceAndPersistsIt()
    {
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        var state=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Wheel);var id=state.Id;var cell=state.Cell;
        var before=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        var collider=before.transform.Find("Koło").GetComponent<Collider>();Assert.That(collider.enabled,Is.True);
        Assert.That(run.GarageToggleWheelStyle(cell),Is.True);yield return null;
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==id).WheelStyle,Is.EqualTo(R3WheelStyle.Slick));
        var wheel=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        Assert.That(wheel.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4M_WheelM_Side_Slick_Rotor"),Is.True);
        Assert.That(wheel.transform.Find("Koło").GetComponent<Collider>().enabled,Is.True);
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(run.VehicleModel.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Standard,run.VehicleModel.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==id).WheelStyle,Is.EqualTo(R3WheelStyle.Slick));
        Assert.That(R3VehicleModel.SameFusionType(state,new R3PartState(cell,R3PartKind.Wheel)),Is.False);
        Assert.That(run.GarageToggleWheelStyle(cell),Is.True);yield return null;
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==id).WheelStyle,Is.EqualTo(R3WheelStyle.Offroad));
    }
    [UnityTest]public IEnumerator BothWheelAppearancesCanBePurchasedDirectlyAndSurviveCheckpoint()
    {
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);int startingScrap=model.Scrap;
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.Offroad),Is.True);
        var offroad=model.Inventory.Last();
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.Slick),Is.True);
        var slick=model.Inventory.Last();
        Assert.That(offroad.WheelStyle,Is.EqualTo(R3WheelStyle.Offroad));
        Assert.That(slick.WheelStyle,Is.EqualTo(R3WheelStyle.Slick));
        Assert.That(R3VehicleModel.SameFusionType(offroad,slick),Is.False);
        Assert.That(model.Scrap,Is.EqualTo(startingScrap-2*model.Price(R3PartKind.Wheel)));
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,(R3WheelStyle)99),Is.False);
        Assert.That(run.GaragePurchase(R3PartKind.Armor,default,R3WheelStyle.Slick),Is.False);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Inventory.Single(p=>p.Id==offroad.Id).WheelStyle,Is.EqualTo(R3WheelStyle.Offroad));
        Assert.That(restored.Inventory.Single(p=>p.Id==slick.Id).WheelStyle,Is.EqualTo(R3WheelStyle.Slick));
    }
    [UnityTest]public IEnumerator SideMountedWheelsUseSidePortsAndLegacyStoredWheelsMigrateSafely()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        foreach(var style in new[]{R3WheelStyle.Offroad,R3WheelStyle.Slick,R3WheelStyle.LargeOffroad,R3WheelStyle.Monster})
        {
            var design=new R3VehicleDesign{Name="Port QA"};var coreCell=Vector3Int.up;
            design.Parts.Add(new R3PartState(coreCell,R3PartKind.Core));
            var model=new R3VehicleModel(design);model.Award(100);
            Assert.That(model.PurchaseToInventory(R3PartKind.Wheel,default,style),Is.True);
            var part=model.Inventory.Last();Assert.That(part.ContactMask,Is.EqualTo(3));
            Assert.That(R3MountingGrid.PortOnFace(part,Vector3Int.down),Is.False,style+" has no upper or lower axle socket.");
            Assert.That(model.OwnedPlacementError(part.Id,coreCell+Vector3Int.up,Vector3Int.zero),Is.Not.Null,style+" must not mount by its top face.");
            var side=coreCell+Vector3Int.right;
            Assert.That(model.OwnedPlacementError(part.Id,side,Vector3Int.zero),Is.Null,style+" must mount by its side.");
            Assert.That(model.Mount(part.Id,side),Is.True);
            Assert.That(model.At(side).ContactMask,Is.EqualTo(3));
        }
        var current=run.VehicleModel;var original=current.Parts.First(p=>p.Kind==R3PartKind.Wheel);var originalId=original.Id;
        Assert.That(run.GarageRemove(original.Cell),Is.True);Assert.That(current.Inventory.Single(p=>p.Id==originalId).ContactMask,Is.EqualTo(3));
        var old=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(current.MakeCheckpoint(Time.time)));
        old.inventory.Single(p=>p.Id==originalId).ContactMask=R3MountingGrid.AllFaces;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(old,R3VehicleKind.Standard,current.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Inventory.Single(p=>p.Id==originalId).ContactMask,Is.EqualTo(3));
        Assert.That(old.inventory.Single(p=>p.Id==originalId).ContactMask,Is.EqualTo(R3MountingGrid.AllFaces),"Reading must not mutate the source document.");
        Assert.That(run.GarageMount(originalId,original.Cell,original.RotationQuarterTurns),Is.True,"Factory wheels remain remountable after port migration.");
    }
    [UnityTest]public IEnumerator ChangingWheelStackUpdatesGhostAndRepeatedMountKeepsSameAppearance()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var support=model.Inventory.Last();var garage=run.Garage;garage.ChooseInventory(support.Id);
        garage.Select(new Vector3Int(2,2,2));Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        Assert.That(run.GaragePurchase(R3PartKind.Wheel),Is.True);var offroad=model.Inventory.Last();
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.Slick),Is.True);var slick=model.Inventory.Last();
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.Slick),Is.True);var nextSlick=model.Inventory.Last();
        garage.ChooseInventory(offroad.Id);garage.Select(Vector3Int.zero);
        Assert.That(garage.Preview.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4M_WheelM_Side_Offroad_Rotor"),Is.True);
        garage.ChooseInventory(slick.Id);garage.Select(Vector3Int.zero);
        Assert.That(garage.Preview.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4M_WheelM_Side_Slick_Rotor"),Is.True);
        var candidates=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct().ToArray();
        var target=candidates.FirstOrDefault(c=>model.OwnedPlacementError(slick.Id,c,Vector3Int.zero)==null);
        Assert.That(model.OwnedPlacementError(slick.Id,target,Vector3Int.zero),Is.Null,"Expected a valid real wheel mount; " +
            string.Join(", ",candidates.Select(c=>model.OwnedPlacementError(slick.Id,c,Vector3Int.zero)).GroupBy(e=>e).Select(g=>$"{g.Key}: {g.Count()}"))+
            $" wheels={model.Parts.Count(p=>p.Kind==R3PartKind.Wheel)}");
        garage.Select(target);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);
        Assert.That(garage.SelectedInventoryId,Is.EqualTo(nextSlick.Id));
    }
    [UnityTest]public IEnumerator LargeWheelUsesItsRealSizeContactAndOwnedStyle()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);var garage=run.Garage;
        foreach(var cell in new[]{new Vector3Int(2,2,2),new Vector3Int(2,3,2)})
        {
            Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);var support=model.Inventory.Last();
            garage.ChooseInventory(support.Id);garage.Select(cell);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        }
        Assert.That(run.GaragePurchase(R3PartKind.Wheel),Is.True);var medium=model.Inventory.Last();
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.LargeOffroad),Is.True);var large=model.Inventory.Last();
        Assert.That(R3VehicleModel.SameFusionType(medium,large),Is.False);
        var candidates=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct().ToArray();
        var target=candidates.FirstOrDefault(c=>model.OwnedPlacementError(large.Id,c,Vector3Int.zero)==null);
        Assert.That(model.OwnedPlacementError(large.Id,target,Vector3Int.zero),Is.Null,"Expected room for a large wheel.");
        Assert.That(model.OwnedPlacementError(medium.Id,target,Vector3Int.zero),Is.Null);
        var pose=large.Copy();pose.Cell=target;
        bool mirror=R4WheelArt.MirrorForMount(model,pose);
        var tireCenter=run.Player.transform.TransformPoint((Vector3)target*.55f+new Vector3(mirror ? -.062f : .062f,0,0));
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=tireCenter+run.Player.transform.forward*.48f;
        blocker.transform.localScale=Vector3.one*.06f;Physics.SyncTransforms();
        Assert.That(model.OwnedPlacementError(medium.Id,target,Vector3Int.zero),Is.Null,"The medium tire clears the obstacle.");
        Assert.That(model.OwnedPlacementError(large.Id,target,Vector3Int.zero),Is.Not.Null,"The real larger tire must not intersect the obstacle.");
        Object.Destroy(blocker);yield return null;Physics.SyncTransforms();
        garage.ChooseInventory(large.Id);garage.Select(target);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==large.Id);
        var tire=part.transform.Find("Koło").GetComponent<Collider>();
        Assert.That(tire.bounds.extents.y,Is.EqualTo(.56f).Within(.02f));
        var rotor=part.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4M_WheelL_Side_Offroad_Rotor");
        Assert.That(Vector3.Distance(rotor.position,tire.bounds.center),Is.LessThan(.01f));
        Assert.That(R6VehicleDrive.PartMass(part.Damageable),Is.EqualTo(18).Within(.01f));
        Assert.That(run.GarageToggleWheelStyle(target),Is.False,"Changing geometry must require a validated remount.");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.localScale=new Vector3(.4f,.2f,.4f);
        floor.transform.position=new Vector3(tire.bounds.center.x,tire.bounds.min.y-.1f,tire.bounds.center.z);Physics.SyncTransforms();
        var contacts=new R6WheelContacts();contacts.Refresh(run.Player.GetComponent<Rigidbody>(),new[]{part.Damageable});
        Assert.That(contacts.Count,Is.EqualTo(1));Assert.That(contacts.Traction,Is.EqualTo(1.25f).Within(.01f));
        Object.Destroy(floor);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==large.Id).WheelStyle,Is.EqualTo(R3WheelStyle.LargeOffroad));
    }
    [UnityTest]public IEnumerator MonsterWheelHasRealClearanceAndGroundedTraction()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);var garage=run.Garage;
        foreach(var cell in new[]{new Vector3Int(2,2,2),new Vector3Int(2,3,2),new Vector3Int(2,4,2)})
        {
            Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);var support=model.Inventory.Last();
            garage.ChooseInventory(support.Id);garage.Select(cell);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        }
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.LargeOffroad),Is.True);var large=model.Inventory.Last();
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.Monster),Is.True);var monster=model.Inventory.Last();
        var candidates=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct().ToArray();
        var target=candidates.FirstOrDefault(c=>model.OwnedPlacementError(monster.Id,c,Vector3Int.zero)==null&&model.OwnedPlacementError(large.Id,c,Vector3Int.zero)==null);
        Assert.That(model.OwnedPlacementError(monster.Id,target,Vector3Int.zero),Is.Null,"Expected room for a monster wheel.");
        var pose=monster.Copy();pose.Cell=target;bool mirror=R4WheelArt.MirrorForMount(model,pose);
        var center=run.Player.transform.TransformPoint((Vector3)target*.55f+new Vector3(mirror ? -.142f : .142f,0,0));
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=center+run.Player.transform.forward*.68f;
        blocker.transform.localScale=Vector3.one*.06f;Physics.SyncTransforms();
        Assert.That(model.OwnedPlacementError(large.Id,target,Vector3Int.zero),Is.Null);
        Assert.That(model.OwnedPlacementError(monster.Id,target,Vector3Int.zero),Is.Not.Null,"XL's larger tire must be blocked by the obstacle.");
        Object.Destroy(blocker);yield return null;Physics.SyncTransforms();
        garage.ChooseInventory(monster.Id);garage.Select(target);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==monster.Id);
        var tire=part.transform.Find("Koło").GetComponent<Collider>();Assert.That(tire.bounds.extents.y,Is.EqualTo(.8f).Within(.02f));
        Assert.That(part.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4M_WheelXL_Side_Monster_Rotor"),Is.True);
        Assert.That(R6VehicleDrive.PartMass(part.Damageable),Is.EqualTo(24).Within(.01f));
        Assert.That(run.GarageToggleWheelStyle(target),Is.False);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.localScale=new Vector3(.4f,.2f,.4f);
        floor.transform.position=new Vector3(tire.bounds.center.x,tire.bounds.min.y-.1f,tire.bounds.center.z);Physics.SyncTransforms();
        var contacts=new R6WheelContacts();contacts.Refresh(run.Player.GetComponent<Rigidbody>(),new[]{part.Damageable});
        Assert.That(contacts.Count,Is.EqualTo(1));Assert.That(contacts.Traction,Is.EqualTo(1.5f).Within(.01f));Object.Destroy(floor);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==monster.Id).WheelStyle,Is.EqualTo(R3WheelStyle.Monster));
    }
    [UnityTest]public IEnumerator NewMonsterWheelReservesItsPhysicalCellsAndSurvivesRepairAndSave()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);
        foreach(var cell in model.Parts.Where(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x>0).Select(p=>p.Cell).ToArray())
            Assert.That(run.GarageRemove(cell),Is.True,"Clear the factory wheels from the larger wheel's area.");
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.Monster),Is.True);
        var wheel=model.Inventory.Last();string id=wheel.Id;
        Assert.That(wheel.FootprintRevision,Is.EqualTo(1),"A newly purchased Monster uses its 3x3 tire footprint.");
        var low=new Vector3Int(3,0,0);
        Assert.That(model.OwnedPlacementError(id,low,Vector3Int.zero),Does.Contain("siatkę"),"The tire cannot extend below the build grid.");
        var target=new Vector3Int(3,1,0);
        Assert.That(model.OwnedPlacementError(id,target,Vector3Int.zero),Is.Null,"The centered axle has chassis support and free space.");
        var garage=run.Garage;garage.ChooseInventory(id);garage.Select(target);
        Assert.That(garage.PreviewValid,Is.True,garage.PlacementIssue);
        Assert.That(garage.CandidateOccupiedCells().Count(),Is.EqualTo(9),"The preview uses the same reserved cells.");
        Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        var mounted=model.Parts.Single(p=>p.Id==id);Assert.That(R3VehicleModel.OccupiedCells(mounted).Count(),Is.EqualTo(9));
        Assert.That(model.At(new Vector3Int(3,2,1)).Id,Is.EqualTo(id));
        var actual=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        Assert.That(actual.GetComponent<R3BlockMountPoints>().GridOffsets.Count(),Is.EqualTo(9));
        Assert.That(actual.transform.Find("Koło").GetComponent<Collider>().bounds.extents.y,Is.EqualTo(.8f).Within(.02f));
        var saved=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(R3VehicleModel.OccupiedCells(restored.Parts.Single(p=>p.Id==id)).Count(),Is.EqualTo(9));
        actual.Damageable.ApplyDamage(actual.Damageable.CurrentHp/.75f+1,actual.transform.position,Vector3.up);
        run.Player.GetComponent<R3VehicleAssembly>().Capture();Assert.That(model.At(target).Health,Is.Zero);
        Assert.That(run.GarageRepair(target),Is.True,"The destroyed large wheel can be restored with its whole footprint.");
        yield return null;
        Assert.That(R3VehicleModel.OccupiedCells(model.Parts.Single(p=>p.Id==id)).Count(),Is.EqualTo(9));
        Assert.That(run.GarageRemove(new Vector3Int(3,2,1)),Is.True,"Removing any reserved cell removes one physical item.");
        Assert.That(model.Inventory.Count(p=>p.Id==id),Is.EqualTo(1));
    }
    [UnityTest]public IEnumerator SmallForkNeedsUpperSupportAndUsesItsOwnTire()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);var garage=run.Garage;
        foreach(var cell in new[]{new Vector3Int(2,2,2),new Vector3Int(2,3,2)})
        {
            Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);var support=model.Inventory.Last();
            garage.ChooseInventory(support.Id);garage.Select(cell);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        }
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.SmallFork),Is.True);var wheel=model.Inventory.Last();
        Assert.That(wheel.ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.up)));
        var target=new Vector3Int(3,2,2);
        Assert.That(model.OwnedPlacementError(wheel.Id,target,Vector3Int.zero),Is.Not.Null,"Side support must not replace the upper socket.");
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);var shelf=model.Inventory.Last();
        garage.ChooseInventory(shelf.Id);garage.Select(new Vector3Int(3,3,2));Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        garage.ChooseInventory(wheel.Id);garage.HandleGuiPointer(new Event{type=EventType.KeyDown,keyCode=KeyCode.U},Vector2.zero);
        typeof(R3GarageView).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(garage,null);
        var camera=(Camera)typeof(R3GarageView).GetField("view",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(garage);
        var workshopFloor=GameObject.Find("Podłoga warsztatu").GetComponent<Renderer>();
        Assert.That(camera.transform.position.y,Is.LessThan(workshopFloor.transform.position.y+.1f));
        Assert.That(workshopFloor.enabled,Is.False,"The floor must not hide the underside view.");
        var shelfPosition=run.Player.transform.TransformPoint(new Vector3(3,3,2)*.55f);
        Assert.That(garage.Point(new Ray(shelfPosition-Vector3.up*.4f,Vector3.up)),Is.True,"The real underside socket must be pointable.");
        Assert.That(garage.Cell,Is.EqualTo(target));Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        garage.HandleGuiPointer(new Event{type=EventType.KeyDown,keyCode=KeyCode.U},Vector2.zero);
        typeof(R3GarageView).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(garage,null);
        Assert.That(workshopFloor.enabled,Is.True,"Restore the floor when looking from above.");
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==wheel.Id);
        var tire=part.transform.Find("Koło").GetComponent<Collider>();Assert.That(tire.bounds.extents.y,Is.EqualTo(.205f).Within(.015f));
        Assert.That(part.GetComponent<BoxCollider>().bounds.min.y,Is.GreaterThan(tire.bounds.min.y+.1f),"The hub must not be a full-cell cube touching the ground first.");
        Assert.That(part.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4M_WheelS_Fork_Road_Rotor"),Is.True);
        Assert.That(R6VehicleDrive.PartMass(part.Damageable),Is.EqualTo(6).Within(.01f));
        Assert.That(run.GarageToggleWheelStyle(target),Is.False);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.localScale=new Vector3(.3f,.2f,.3f);
        floor.transform.position=new Vector3(tire.bounds.center.x,tire.bounds.min.y-.1f,tire.bounds.center.z);Physics.SyncTransforms();
        var contacts=new R6WheelContacts();contacts.Refresh(run.Player.GetComponent<Rigidbody>(),new[]{part.Damageable});
        Assert.That(contacts.Count,Is.EqualTo(1));Assert.That(contacts.Traction,Is.EqualTo(.65f).Within(.01f));Object.Destroy(floor);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==wheel.Id).WheelStyle,Is.EqualTo(R3WheelStyle.SmallFork));
        data.mounted.Single(p=>p.Id==wheel.Id).ContactMask=R3MountingGrid.AllFaces;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out _,out _),Is.False);
        var recovered=wheel.Copy();recovered.Id=System.Guid.NewGuid().ToString("N");recovered.Health=.4f;
        Assert.That(model.ReceiveSalvage(new[]{recovered}),Is.True);
        Assert.That(model.Inventory.Single(p=>p.Id==recovered.Id).WheelStyle,Is.EqualTo(R3WheelStyle.SmallFork));
        var invalid=recovered.Copy();invalid.Id=System.Guid.NewGuid().ToString("N");invalid.ContactMask=R3MountingGrid.AllFaces;
        Assert.That(model.ReceiveSalvage(new[]{invalid}),Is.False,"Recovered parts must not bypass their mounting restrictions.");
    }
    [UnityTest]public IEnumerator BalloonForkUsesTopMountAndActualGroundedSize()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);var garage=run.Garage;
        foreach(var cell in new[]{new Vector3Int(2,2,2),new Vector3Int(2,3,2),new Vector3Int(3,3,2)})
        {
            Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);var support=model.Inventory.Last();
            garage.ChooseInventory(support.Id);garage.Select(cell);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        }
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.BalloonFork),Is.True);var wheel=model.Inventory.Last();
        Assert.That(wheel.ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.up)));
        var crowded=new Vector3Int(3,2,2);garage.ChooseInventory(wheel.Id);garage.Select(crowded);
        Assert.That(garage.ClickTarget(),Is.False,"The larger balloon tire must not overlap the existing lower wheel.");
        Assert.That(model.Inventory.Any(p=>p.Id==wheel.Id),Is.True);
        Assert.That(run.GarageRemove(new Vector3Int(3,3,2)),Is.True);yield return null;
        foreach(var cell in new[]{new Vector3Int(2,4,2),new Vector3Int(3,4,2)})
        {
            Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);var support=model.Inventory.Last();
            garage.ChooseInventory(support.Id);garage.Select(cell);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        }
        var target=new Vector3Int(3,3,2);garage.ChooseInventory(wheel.Id);garage.Select(target);
        Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==wheel.Id);
        var tire=part.transform.Find("Koło").GetComponent<Collider>();Assert.That(tire.bounds.extents.y,Is.EqualTo(.5f).Within(.02f));
        Assert.That(part.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4M_WheelL_Fork_Balloon_Rotor"),Is.True);
        Assert.That(R6VehicleDrive.PartMass(part.Damageable),Is.EqualTo(16).Within(.01f));
        Assert.That(run.GarageToggleWheelStyle(target),Is.False);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.localScale=new Vector3(.4f,.2f,.4f);
        floor.transform.position=new Vector3(tire.bounds.center.x,tire.bounds.min.y-.1f,tire.bounds.center.z);Physics.SyncTransforms();
        var contacts=new R6WheelContacts();contacts.Refresh(run.Player.GetComponent<Rigidbody>(),new[]{part.Damageable});
        Assert.That(contacts.Count,Is.EqualTo(1));Assert.That(contacts.Traction,Is.EqualTo(1.15f).Within(.01f));Object.Destroy(floor);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==wheel.Id).WheelStyle,Is.EqualTo(R3WheelStyle.BalloonFork));
        data.mounted.Single(p=>p.Id==wheel.Id).ContactMask=R3MountingGrid.AllFaces;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out _,out _),Is.False);
    }
    [UnityTest]public IEnumerator SpikedForkIsAvailableWithTopMountAndSourceGeometry()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);
        var style=(R3WheelStyle)6;
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,style),Is.True,"The new model must be purchasable.");
        var wheel=model.Inventory.Last();
        Assert.That(wheel.ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.up)));
        Assert.That(R3VehicleModel.WheelRadius(style),Is.EqualTo(.37f).Within(.001f));
        Assert.That(R3VehicleModel.WheelWidth(style),Is.EqualTo(.30f).Within(.001f));
        Assert.That(R3VehicleModel.ForkAxleDrop(style),Is.EqualTo(.24f).Within(.001f));
        var garage=run.Garage;
        foreach(var cell in new[]{new Vector3Int(2,2,2),new Vector3Int(2,3,2),new Vector3Int(2,4,2),new Vector3Int(3,4,2)})
        {
            Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);var support=model.Inventory.Last();
            garage.ChooseInventory(support.Id);garage.Select(cell);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        }
        var target=new Vector3Int(3,3,2);garage.ChooseInventory(wheel.Id);garage.Select(target);
        Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==wheel.Id);
        Assert.That(part.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4M_WheelM_Fork_Spiked_Rotor"),Is.True);
        Assert.That(part.transform.Find("Koło").GetComponent<Collider>().bounds.extents.y,Is.EqualTo(.37f).Within(.02f));
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==wheel.Id).WheelStyle,Is.EqualTo(style));
    }
    [UnityTest]public IEnumerator TwinRoadHasTwoActualTyreCollidersAndOneItem()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);var style=(R3WheelStyle)7;
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,style),Is.True);
        var part=model.Inventory.Last();Assert.That(part.ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.up)));
        Assert.That(R3VehicleModel.WheelRadius(style),Is.EqualTo(.33f).Within(.001f));
        Assert.That(R3VehicleModel.WheelWidth(style),Is.EqualTo(.16f).Within(.001f));
        var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        R41PartGeometry.ConfigureWheelMount(block,style);
        var wheel=R41PartGeometry.Payload(block.transform,R3PartKind.Wheel,default,material,true,out _,style);
        yield return null;Physics.SyncTransforms();
        var tires=wheel.GetComponentsInChildren<Collider>().Where(c=>c.enabled).ToArray();
        Assert.That(tires.Length,Is.EqualTo(2),"Both tyres need distinct physical contact surfaces.");
        Assert.That(Mathf.Abs(tires[0].bounds.center.x-tires[1].bounds.center.x),Is.EqualTo(.18f).Within(.01f));
        Assert.That(R4WheelArt.TryAttach(block.transform,wheel,false,style),Is.Not.Null);
        Assert.That(block.GetComponentsInChildren<Transform>().Any(t=>t.name=="R4M_WheelM_Twin_Road_Rotor"),Is.True);
        Object.Destroy(block);Object.Destroy(material);
        var garage=run.Garage;
        foreach(var cell in new[]{new Vector3Int(2,2,2),new Vector3Int(2,3,2),new Vector3Int(2,4,2),new Vector3Int(3,4,2)})
        {
            Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);var support=model.Inventory.Last();
            garage.ChooseInventory(support.Id);garage.Select(cell);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        }
        var target=new Vector3Int(3,3,2);garage.ChooseInventory(part.Id);garage.Select(target);
        Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        var mounted=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==part.Id);
        Assert.That(mounted.transform.Find("Koło").GetComponentsInChildren<Collider>().Count(c=>c.enabled),Is.EqualTo(2));
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==part.Id).WheelStyle,Is.EqualTo(style));
    }
    [UnityTest]public IEnumerator RoadSteerKeepsMountStillAndMovesKnuckle()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);var style=(R3WheelStyle)8;
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,style),Is.True);
        Assert.That(model.Inventory.Last().ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.up)));
        Assert.That(R3VehicleModel.WheelRadius(style),Is.EqualTo(.34f).Within(.001f));
        Assert.That(R3VehicleModel.WheelWidth(style),Is.EqualTo(.26f).Within(.001f));
        Assert.That(R3VehicleModel.ForkAxleDrop(style),Is.EqualTo(.315f).Within(.001f));
        var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));R41PartGeometry.ConfigureWheelMount(block,style);
        var wheel=R41PartGeometry.Payload(block.transform,R3PartKind.Wheel,default,material,true,out _,style);
        var art=R4WheelArt.TryAttach(block.transform,wheel,false,style);Assert.That(art,Is.Not.Null);
        var socket=block.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Socket_Mount");
        var knuckle=block.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4M_WheelM_Steer_Road_Knuckle");
        var rotor=block.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4M_WheelM_Steer_Road_Rotor");
        var pivot=new GameObject("test pivot").transform;pivot.SetParent(block.transform,false);pivot.position=knuckle.position;
        Assert.That(art.AttachToSteeringPivot(pivot),Is.True);var socketRotation=socket.rotation;var socketPosition=socket.position;var rotorRotation=rotor.rotation;
        pivot.localRotation=Quaternion.Euler(0,25,0);
        Assert.That(Quaternion.Angle(socket.rotation,socketRotation),Is.LessThan(.01f));
        Assert.That(Vector3.Distance(socket.position,socketPosition),Is.LessThan(.001f));
        Assert.That(Quaternion.Angle(rotor.rotation,rotorRotation),Is.GreaterThan(20));
        Object.Destroy(block);Object.Destroy(material);yield return null;
        var purchased=model.Inventory.Single(p=>p.Kind==R3PartKind.Wheel&&p.WheelStyle==style);var garage=run.Garage;
        foreach(var cell in new[]{new Vector3Int(2,2,2),new Vector3Int(2,3,2),new Vector3Int(2,4,2),new Vector3Int(3,4,2)})
        {
            Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);var support=model.Inventory.Last();
            garage.ChooseInventory(support.Id);garage.Select(cell);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        }
        var target=new Vector3Int(3,3,2);garage.ChooseInventory(purchased.Id);garage.Select(target);
        Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        var mounted=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==purchased.Id);
        Assert.That(mounted.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4M_WheelM_Steer_Road_Rotor"),Is.True);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==purchased.Id).WheelStyle,Is.EqualTo(style));
    }
    [UnityTest]public IEnumerator LargeSteerUsesTopSocketAndRealArticulatedModel()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);var style=(R3WheelStyle)9;
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,style),Is.True);
        Assert.That(model.Inventory.Last().ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.up)));
        Assert.That(R3VehicleModel.WheelRadius(style),Is.EqualTo(.48f).Within(.001f));
        Assert.That(R3VehicleModel.WheelWidth(style),Is.EqualTo(.34f).Within(.001f));
        Assert.That(R3VehicleModel.ForkAxleDrop(style),Is.EqualTo(.455f).Within(.001f));
        var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));R41PartGeometry.ConfigureWheelMount(block,style);
        var wheel=R41PartGeometry.Payload(block.transform,R3PartKind.Wheel,default,material,true,out _,style);
        var art=R4WheelArt.TryAttach(block.transform,wheel,false,style);Assert.That(art,Is.Not.Null);
        var socket=block.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Socket_Mount");
        var knuckle=block.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4M_WheelL_Steer_Offroad_Knuckle");
        var rotor=block.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4M_WheelL_Steer_Offroad_Rotor");
        var pivot=new GameObject("test pivot").transform;pivot.SetParent(block.transform,false);pivot.position=knuckle.position;
        Assert.That(art.AttachToSteeringPivot(pivot),Is.True);var socketPosition=socket.position;var before=rotor.rotation;
        pivot.localRotation=Quaternion.Euler(0,25,0);
        Assert.That(Vector3.Distance(socket.position,socketPosition),Is.LessThan(.001f));Assert.That(Quaternion.Angle(rotor.rotation,before),Is.GreaterThan(20));
        Object.Destroy(block);Object.Destroy(material);yield return null;
        var purchased=model.Inventory.Single(p=>p.Kind==R3PartKind.Wheel&&p.WheelStyle==style);var garage=run.Garage;
        foreach(var cell in new[]{new Vector3Int(2,2,2),new Vector3Int(2,3,2),new Vector3Int(2,4,2),new Vector3Int(3,4,2)})
        {
            Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);var support=model.Inventory.Last();
            garage.ChooseInventory(support.Id);garage.Select(cell);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        }
        var target=new Vector3Int(3,3,2);garage.ChooseInventory(purchased.Id);garage.Select(target);
        Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==purchased.Id).WheelStyle,Is.EqualTo(style));
    }
    [UnityTest]public IEnumerator CasterFollowsVelocityAndKeepsTyreAlignedWithArt()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);var style=(R3WheelStyle)10;
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,style),Is.True);
        Assert.That(model.Inventory.Last().ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.up)));
        Assert.That(R3VehicleModel.WheelRadius(style),Is.EqualTo(.17f).Within(.001f));
        var owner=new GameObject("caster QA");owner.transform.position=new Vector3(1000,50,1000);var body=owner.AddComponent<Rigidbody>();body.useGravity=false;
        var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.SetParent(owner.transform,false);block.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));R41PartGeometry.ConfigureWheelMount(block,style);
        var wheel=R41PartGeometry.Payload(block.transform,R3PartKind.Wheel,default,material,true,out _,style);
        var art=R4WheelArt.TryAttach(block.transform,wheel,false,style);Assert.That(art,Is.Not.Null);
        var socket=block.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Socket_Mount");
        var swivel=block.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4M_WheelS_Caster_Slick_Swivel");
        var rotor=block.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4M_WheelS_Caster_Slick_Rotor");
        var before=swivel.localRotation;var fixedSocket=socket.localPosition;Time.timeScale=1;body.linearVelocity=Vector3.right*3;
        float begin=Time.fixedTime,deadline=Time.realtimeSinceStartup+10;
        while(Time.fixedTime-begin<.3f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(Time.fixedTime-begin,Is.GreaterThanOrEqualTo(.3f),"Physics time must advance.");
        Assert.That(Quaternion.Angle(swivel.localRotation,before),Is.GreaterThan(15),"Swivel must follow travel direction.");
        Assert.That(Vector3.Distance(wheel.position,rotor.position),Is.LessThan(.01f),$"Collider and imported axle must move together. physical={block.transform.InverseTransformPoint(wheel.position)}, rotor={block.transform.InverseTransformPoint(rotor.position)}, swivel={block.transform.InverseTransformPoint(swivel.position)}");
        Assert.That(Vector3.Distance(socket.localPosition,fixedSocket),Is.LessThan(.001f));
        Object.Destroy(owner);Object.Destroy(material);
    }
    [UnityTest]public IEnumerator CasterCanBeMountedAndSaved()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);var garage=run.Garage;
        foreach(var cell in new[]{new Vector3Int(2,2,2),new Vector3Int(2,3,2),new Vector3Int(3,3,2)})
        {
            Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);var support=model.Inventory.Last();
            garage.ChooseInventory(support.Id);garage.Select(cell);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        }
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.CasterSlick),Is.True);var item=model.Inventory.Last();
        garage.ChooseInventory(item.Id);garage.Select(new Vector3Int(3,2,2));Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==item.Id).WheelStyle,Is.EqualTo(R3WheelStyle.CasterSlick));
    }
    [UnityTest]public IEnumerator SwingArmCanBeMountedAndSaved()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);var garage=run.Garage;
        foreach(var cell in new[]{new Vector3Int(2,2,2),new Vector3Int(2,3,2),new Vector3Int(2,4,2),new Vector3Int(3,4,2)})
        {
            Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);var support=model.Inventory.Last();
            garage.ChooseInventory(support.Id);garage.Select(cell);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        }
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.SwingOffroad),Is.True);var item=model.Inventory.Last();
        garage.ChooseInventory(item.Id);garage.Select(new Vector3Int(3,3,2));Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==item.Id).WheelStyle,Is.EqualTo(R3WheelStyle.SwingOffroad));
    }
    [UnityTest]public IEnumerator SwingArmMovesWithGroundAndKeepsColliderAligned()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);var style=(R3WheelStyle)11;
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,style),Is.True);
        Assert.That(model.Inventory.Last().ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.up)));
        var owner=new GameObject("swing QA");owner.transform.position=new Vector3(1000,10,1000);
        var body=owner.AddComponent<Rigidbody>();body.isKinematic=true;
        var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.SetParent(owner.transform,false);block.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));R41PartGeometry.ConfigureWheelMount(block,style);
        var wheel=R41PartGeometry.Payload(block.transform,R3PartKind.Wheel,default,material,true,out _,style);
        var art=R4WheelArt.TryAttach(block.transform,wheel,false,style);Assert.That(art,Is.Not.Null);
        var arm=block.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4M_WheelM_SwingArm_Offroad_Arm");
        var rotor=block.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4M_WheelM_SwingArm_Offroad_Rotor");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.localScale=new Vector3(2,.1f,2);
        floor.transform.position=new Vector3(wheel.position.x,wheel.position.y-.33f-.05f+.045f,wheel.position.z);Physics.SyncTransforms();
        var before=arm.rotation;Time.timeScale=1;float start=Time.fixedTime,deadline=Time.realtimeSinceStartup+10;
        while(Time.fixedTime-start<.35f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(Time.fixedTime-start,Is.GreaterThanOrEqualTo(.35f));
        Assert.That(Quaternion.Angle(arm.rotation,before),Is.GreaterThan(1f),"Raised terrain must compress the arm.");
        Assert.That(Vector3.Distance(wheel.position,rotor.position),Is.LessThan(.01f),"The physical tire must track the arm axle.");
        Object.Destroy(owner);Object.Destroy(floor);Object.Destroy(material);
    }
    [UnityTest]public IEnumerator SwingArmCannotMoveTireThroughOwnArmor()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var owner=new GameObject("swing clearance QA");owner.transform.position=new Vector3(1000,10,1000);
        var body=owner.AddComponent<Rigidbody>();body.isKinematic=true;
        var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.SetParent(owner.transform,false);block.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));R41PartGeometry.ConfigureWheelMount(block,R3WheelStyle.SwingOffroad);
        var wheel=R41PartGeometry.Payload(block.transform,R3PartKind.Wheel,default,material,true,out _,R3WheelStyle.SwingOffroad);
        Assert.That(R4WheelArt.TryAttach(block.transform,wheel,false,R3WheelStyle.SwingOffroad),Is.Not.Null);
        var tire=wheel.GetComponents<Collider>().Single(c=>c.enabled);var armor=GameObject.CreatePrimitive(PrimitiveType.Cube);
        armor.transform.SetParent(owner.transform,true);armor.transform.localScale=new Vector3(.5f,.1f,.5f);
        armor.transform.position=wheel.position+Vector3.up*(.33f+.01f+.05f);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.localScale=new Vector3(2,.1f,2);
        floor.transform.position=new Vector3(wheel.position.x,wheel.position.y-.33f-.05f+.045f,wheel.position.z);Physics.SyncTransforms();
        Time.timeScale=1;float start=Time.fixedTime,deadline=Time.realtimeSinceStartup+10;
        while(Time.fixedTime-start<.35f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(Time.fixedTime-start,Is.GreaterThanOrEqualTo(.35f));
        Assert.That(Physics.ComputePenetration(tire,tire.transform.position,tire.transform.rotation,armor.GetComponent<Collider>(),armor.transform.position,armor.transform.rotation,out _,out var depth)&&depth>.001f,Is.False,"The moving tire must not penetrate own armor.");
        Object.Destroy(owner);Object.Destroy(floor);Object.Destroy(material);
    }
    [UnityTest]public IEnumerator LargeWheelsDriveActualRigidVehicleOnGround(){yield return DriveSizedWheels(R3WheelStyle.LargeOffroad);}
    [UnityTest]public IEnumerator MonsterWheelsDriveActualRigidVehicleOnGround(){yield return DriveSizedWheels(R3WheelStyle.Monster);}
    [UnityTest]public IEnumerator SmallForkWheelsDriveActualRigidVehicleOnGround(){yield return DriveSizedWheels(R3WheelStyle.SmallFork);}
    [UnityTest]public IEnumerator BalloonWheelsDriveActualRigidVehicleOnGround(){yield return DriveSizedWheels(R3WheelStyle.BalloonFork);}
    [UnityTest]public IEnumerator SpikedWheelsDriveActualRigidVehicleOnGround(){yield return DriveSizedWheels(R3WheelStyle.SpikedFork);}
    [UnityTest]public IEnumerator TwinWheelsDriveActualRigidVehicleOnGround(){yield return DriveSizedWheels(R3WheelStyle.TwinRoad);}
    [UnityTest]public IEnumerator RoadSteerWheelsDriveActualRigidVehicleOnGround(){yield return DriveSizedWheels(R3WheelStyle.SteerRoad);}
    [UnityTest]public IEnumerator LargeSteerWheelsDriveActualRigidVehicleOnGround(){yield return DriveSizedWheels(R3WheelStyle.SteerOffroad);}
    [UnityTest]public IEnumerator CasterWheelsDriveActualRigidVehicleOnGround(){yield return DriveSizedWheels(R3WheelStyle.CasterSlick);}
    [UnityTest]public IEnumerator SwingWheelsDriveWithExistingSuspension(){yield return DriveSizedWheels(R3WheelStyle.SwingOffroad,true);}
    [UnityTest]public IEnumerator SwingWheelsDriveOntoRamp(){yield return DriveSizedWheels(R3WheelStyle.SwingOffroad,true,true);}
    [UnityTest]public IEnumerator RoadSteerArtUsesRealIndependentSteeringWithoutMovingMount(){yield return SteeringVariant(R3WheelStyle.SteerRoad,"R4M_WheelM_Steer_Road_Rotor");}
    [UnityTest]public IEnumerator LargeSteerArtUsesRealIndependentSteeringWithoutMovingMount(){yield return SteeringVariant(R3WheelStyle.SteerOffroad,"R4M_WheelL_Steer_Offroad_Rotor");}
    IEnumerator SteeringVariant(R3WheelStyle style,string rotorName)
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var palette=(R3VisualPalette)typeof(R1RunController).GetField("palette",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        var design=new R3VehicleDesign{Name="Road steering QA",Speed=7,TurnSpeed=90,CoreHp=100};
        design.Parts.Add(new R3PartState(new Vector3Int(0,1,0),R3PartKind.Core));
        for(int z=1;z<=4;z++)design.Parts.Add(new R3PartState(new Vector3Int(0,1,z),R3PartKind.Armor));
        design.Parts.Add(new R3PartState(new Vector3Int(0,2,0),R3PartKind.IndependentSteering));
        foreach(int x in new[]{-1,1})foreach(int z in new[]{0,4})
        {
            design.Parts.Add(new R3PartState(new Vector3Int(x,1,z),R3PartKind.Armor));
            design.Parts.Add(new R3PartState(new Vector3Int(x,0,z),R3PartKind.Wheel){WheelStyle=style,ContactMask=R3MountingGrid.FaceBit(Vector3Int.up)});
        }
        var model=new R3VehicleModel(design);var vehicle=R3VehicleAssembly.Build(model,run.Progress.Stats,palette,new Vector3(1000,1,1000),Quaternion.identity);
        try
        {
            var binding=vehicle.GetComponentInChildren<RoboRougelike.PackageIntegration.RRLIndependentSteeringBinding>();
            Assert.That(binding,Is.Not.Null);Assert.That(binding.GeometrySupported,Is.True);
            var front=vehicle.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Wheel&&p.Cell.z==4);
            var socket=front.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Socket_Mount");
            var rotor=front.GetComponentsInChildren<Transform>(true).Single(t=>t.name==rotorName);
            var beforeSocket=socket.localToWorldMatrix;var beforeRotor=rotor.rotation;
            Assert.That(binding.TrySolve(1,.1f,out _),Is.True);
            Assert.That(Vector3.Distance(socket.position,beforeSocket.GetColumn(3)),Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(rotor.rotation,beforeRotor),Is.GreaterThan(1f));
        }
        finally{Object.Destroy(vehicle.gameObject);}
    }
    [UnityTest]public IEnumerator TwinWheelFindsEitherTireButNotEmptyCenter()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var palette=(R3VisualPalette)typeof(R1RunController).GetField("palette",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        var design=new R3VehicleDesign{Name="Twin support QA",Speed=7,TurnSpeed=90,CoreHp=100};
        design.Parts.Add(new R3PartState(new Vector3Int(0,1,0),R3PartKind.Core));
        design.Parts.Add(new R3PartState(new Vector3Int(1,1,0),R3PartKind.Armor));
        var wheel=new R3PartState(new Vector3Int(1,0,0),R3PartKind.Wheel){WheelStyle=R3WheelStyle.TwinRoad,ContactMask=R3MountingGrid.FaceBit(Vector3Int.up)};
        design.Parts.Add(wheel);var model=new R3VehicleModel(design);
        var vehicle=R3VehicleAssembly.Build(model,run.Progress.Stats,palette,new Vector3(1000,.53f,1000),Quaternion.identity);
        var part=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==model.Parts.Single(s=>s.Kind==R3PartKind.Wheel).Id);
        var tires=part.transform.Find("Koło").GetComponentsInChildren<Collider>().Where(c=>c.enabled).OrderBy(c=>c.bounds.center.x).ToArray();
        Assert.That(tires.Length,Is.EqualTo(2));
        var pad=GameObject.CreatePrimitive(PrimitiveType.Cube);pad.transform.localScale=new Vector3(.04f,.1f,.4f);
        try
        {
            var contacts=new R6WheelContacts();var body=vehicle.GetComponent<Rigidbody>();
            foreach(var tire in tires)
            {
                pad.transform.position=new Vector3(tire.bounds.center.x,tire.bounds.min.y-.05f,tire.bounds.center.z);Physics.SyncTransforms();
                contacts.Refresh(body,new[]{part.Damageable});Assert.That(contacts.Count,Is.EqualTo(1),"Each tire must detect ground independently.");
            }
            pad.transform.position=new Vector3((tires[0].bounds.center.x+tires[1].bounds.center.x)*.5f,tires[0].bounds.min.y-.05f,tires[0].bounds.center.z);
            Physics.SyncTransforms();contacts.Refresh(body,new[]{part.Damageable});
            Assert.That(contacts.Count,Is.EqualTo(0),"Empty space between the tires must not create support.");
        }
        finally{Object.Destroy(vehicle.gameObject);Object.Destroy(pad);}
    }
    IEnumerator DriveSizedWheels(R3WheelStyle style,bool withSuspension=false,bool withRamp=false)
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        Assert.That(run.EnterTestRange(),Is.True);
        while(run.Transitioning)yield return null;
        var palette=(R3VisualPalette)typeof(R1RunController).GetField("palette",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        var design=new R3VehicleDesign{Name="Large wheel drive QA",Speed=7,TurnSpeed=90,CoreHp=100};
        bool small=R3VehicleModel.HasForkMount(style);int frameY=small?1:0;
        design.Parts.Add(new R3PartState(new Vector3Int(0,frameY,0),R3PartKind.Core));
        for(int z=1;z<=4;z++)design.Parts.Add(new R3PartState(new Vector3Int(0,frameY,z),R3PartKind.Armor));
        if(withSuspension)design.Parts.Add(new R3PartState(new Vector3Int(0,frameY+1,2),R3PartKind.Suspension));
        foreach(int x in new[]{-1,1})foreach(int z in new[]{0,4})
        {
            if(small)design.Parts.Add(new R3PartState(new Vector3Int(x,1,z),R3PartKind.Armor));
            design.Parts.Add(new R3PartState(new Vector3Int(x,0,z),R3PartKind.Wheel){WheelStyle=style,
                ContactMask=small?R3MountingGrid.FaceBit(Vector3Int.up):3});
        }
        var model=new R3VehicleModel(design);var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position=new Vector3(1000,-.1f,1000);floor.transform.localScale=new Vector3(30,.2f,30);
        GameObject ramp=null;
        if(withRamp)
        {
            ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.transform.position=new Vector3(1000,.56f,1006);
            ramp.transform.rotation=Quaternion.Euler(-10,0,0);
            ramp.transform.localScale=new Vector3(5,.1f,7);
            Physics.SyncTransforms();
        }
        float spawnHeight=R3VehicleModel.WheelRadius(style)+(small ? R3VehicleModel.ForkAxleDrop(style) : 0);
        var vehicle=R3VehicleAssembly.Build(model,run.Progress.Stats,palette,new Vector3(1000,spawnHeight,1000),Quaternion.identity);
        var tank=vehicle.GetComponent<P12PipelineSafeTankController>();tank.enabled=false;
        var drive=vehicle.GetComponent<R6VehicleDrive>();Assert.That(drive,Is.Not.Null);Time.timeScale=1;
        if(withSuspension)Assert.That(vehicle.GetComponent<RoboRougelike.Mechanics.MOV009.ActiveSuspensionController>(),Is.Not.Null);
        try
        {
            for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();
            var body=vehicle.GetComponent<Rigidbody>();float start=body.position.z;int grounded=0;
            float fixedStart=Time.fixedTime;float deadline=Time.realtimeSinceStartup+10;
            var trace=new System.Text.StringBuilder();
            int sample=0;float previousFixed=float.NegativeInfinity;
            while(Time.fixedTime-fixedStart<1.6f&&Time.realtimeSinceStartup<deadline){
                // The editor test runner can resume multiple times per physics tick.
                // Never add more than one drive force per actual fixed-time step.
                if(Time.fixedTime>previousFixed){previousFixed=Time.fixedTime;drive.Step(1,0,7,4,90);grounded=Mathf.Max(grounded,drive.GroundedWheels);
                    if(sample++%20==0)trace.Append($"[{sample}: t={Time.fixedTime:F2} p={body.position} v={body.linearVelocity} ground={drive.GroundedWheels} force={drive.AvailableForce:0}] ");}
                yield return new WaitForFixedUpdate();}
            Debug.Log(style+" wheel R6 trace "+trace+"final="+body.position);
            Assert.That(Time.fixedTime-fixedStart,Is.GreaterThanOrEqualTo(1.6f),"The physics clock must really advance.");
            Assert.That(sample,Is.InRange(60,90),"Apply one input per actual physics step.");
            Assert.That(grounded,Is.GreaterThanOrEqualTo(2),"Real large tires must register ground support.");
            Assert.That(body.position.z-start,Is.GreaterThan(.5f),"The actual R6 body must travel forward.");
            if(withRamp)Assert.That(body.position.y,Is.GreaterThan(spawnHeight+.05f),"The actual vehicle must rise onto the ramp.");
        }
        finally{Object.Destroy(vehicle.gameObject);Object.Destroy(floor);if(ramp!=null)Object.Destroy(ramp);}
    }
    [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
