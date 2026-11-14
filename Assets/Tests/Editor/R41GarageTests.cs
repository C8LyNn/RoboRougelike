using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RogueMachine.P12PipelineSafe;

public class R41GarageTests
{
    private R1RunController Run=>R1RunController.Instance;
    private static IEnumerator Until(Func<bool> condition,string message,float timeout=15)
    {if(R1RunController.Instance?.Progress?.State==R1RunState.Route)Assert.That(R1RunController.Instance.ChooseRoute(0),Is.True);float end=Time.realtimeSinceStartup+timeout;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Assert.That(condition(),Is.True,message);}
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        yield return Until(()=>Run?.Progress!=null,"Bootstrap");Run.StartRun();yield return Until(()=>!Run.Transitioning,"Walka");
        Run.Encounter.SetCombatActive(false);var enemy=Run.Encounter.Enemies.First();RRLW2TestFlow.DefeatAll(Run);
        yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Odzysk");RRLW2TestFlow.Claim(Run,Array.Empty<string>());
        Run.Player.transform.rotation=Quaternion.Euler(0,37,0);yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
    [UnityTest] public IEnumerator PreviewFollowsRealVehicleAxesAfterCombatRotation()
    {
        Run.Garage.Select(new Vector3Int(0,2,1));
        var marker=(GameObject)typeof(R3GarageView).GetField("marker",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Run.Garage);
        Assert.That(Quaternion.Angle(marker.transform.rotation,Run.Player.transform.rotation),Is.LessThan(0.01f),"Ghost musi mieć osie prawdziwego pojazdu, także po skręcie w walce.");
        Assert.That(Vector3.Distance(marker.transform.position,Run.Player.transform.TransformPoint(new Vector3(0,1.1f,0.55f))),Is.LessThan(0.001f));
        foreach(var direction in R3VehicleModel.Neighbours)
            Assert.That(R3GarageView.LocalFace(Run.Player.transform,Run.Player.transform.TransformDirection((Vector3)direction)),Is.EqualTo(direction));
        Run.Garage.Zoom(-100);Assert.That(Run.Garage.Distance,Is.EqualTo(3));
        Run.Garage.Zoom(100);Assert.That(Run.Garage.Distance,Is.EqualTo(22));Run.Garage.FrameVehicle();
        yield return null;
    }
    private Ray TopRay(Vector3Int cell)
    {var root=Run.Player.transform;return new Ray(root.TransformPoint((Vector3)cell*0.55f+Vector3.up*4),-root.up);}
    private string PurchasePart(R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic)
    {
        int funds=Run.VehicleModel.Scrap;int count=Run.VehicleModel.Inventory.Count;
        Assert.That(Run.GaragePurchase(kind,weapon),Is.True);
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds-Run.VehicleModel.Price(kind,weapon)));
        Assert.That(Run.VehicleModel.Inventory.Count,Is.EqualTo(count+1));
        var part=Run.VehicleModel.Inventory.Last();Assert.That(part.Kind,Is.EqualTo(kind));Assert.That(part.Weapon,Is.EqualTo(weapon));
        return part.Id;
    }
    [UnityTest] public IEnumerator WheelRemainsSelectableAndRemovableAfterCameraOrbit()
    {
        var garage=Run.Garage;
        var camera=Camera.main;Assert.That(camera,Is.Not.Null);
        garage.SetTool(R41GarageTool.Remove);
        var assembly=Run.Player.GetComponent<R3VehicleAssembly>();
        Assert.That(assembly,Is.Not.Null);
        string id=null;
        foreach(var candidate in assembly.Parts.Where(p=>p!=null&&p.Kind==R3PartKind.Wheel))
        {
            var screen=camera.WorldToScreenPoint(candidate.transform.position);
            if(screen.z<=0||!R3GarageView.InBuildArea(screen,Screen.width,Screen.height))continue;
            if(!garage.Point(camera.ScreenPointToRay(screen))||garage.Cell!=candidate.Cell)continue;
            id=candidate.Id;break;
        }
        Assert.That(id,Is.Not.Null,"A visible wheel must be selectable.");
        garage.Orbit(3.2f);
        Assert.That(Run.VehicleModel.Parts.Any(p=>p.Id==id),Is.True,"Dragging the camera must not remove a part.");
        yield return null;
        var wheel=Run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        var afterDrag=camera.WorldToScreenPoint(wheel.transform.position);
        Assert.That(R3GarageView.InBuildArea(afterDrag,Screen.width,Screen.height),Is.True,"Wheel must remain inside the clickable viewport.");
        garage.SetTool(R41GarageTool.Remove);
        Assert.That(garage.Point(camera.ScreenPointToRay(afterDrag)),Is.True,"The wheel must remain visible after orbiting.");
        Assert.That(garage.Cell,Is.EqualTo(wheel.Cell),"The pointer must address the same wheel after orbiting.");
        Assert.That(garage.ClickTarget(),Is.True,"The selected wheel can be removed.");
        Assert.That(Run.VehicleModel.Parts.Any(p=>p.Id==id),Is.False,"The selected wheel was removed: "+garage.Message);
        Assert.That(Run.VehicleModel.Inventory.Any(p=>p.Id==id),Is.True,"Removal keeps its identity.");
        yield return null;
    }
    [UnityTest] public IEnumerator DirectFaceClickMountsExactlyAtGhostWithoutMovingTheVehicle()
    {
        var garage=Run.Garage;var anchor=new Vector3Int(2,1,2);var target=anchor+Vector3Int.up;
        string id=PurchasePart(R3PartKind.Armor);garage.ChooseInventory(id);
        Assert.That(garage.Point(TopRay(anchor)),Is.True);Assert.That(garage.Cell,Is.EqualTo(target));
        Assert.That(garage.PreviewValid,Is.True);Vector3 ghost=garage.Preview.position;
        var validColor=garage.Preview.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor");Assert.That(validColor.g,Is.GreaterThan(validColor.r));
        var rotation=Run.Player.transform.rotation;var position=Run.Player.transform.position;int funds=Run.VehicleModel.Scrap;
        Assert.That(garage.ClickTarget(),Is.True);var mounted=Run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Cell==target);
        Assert.That(Vector3.Distance(mounted.transform.position,ghost),Is.LessThan(0.001f),"Część po kliknięciu musi pokrywać się z ghostem w świecie.");
        Assert.That(Quaternion.Angle(Run.Player.transform.rotation,rotation),Is.LessThan(0.001));
        Assert.That(Run.Player.transform.position,Is.EqualTo(position));Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        string secondId=PurchasePart(R3PartKind.Armor);garage.ChooseInventory(secondId);garage.Select(target);funds=Run.VehicleModel.Scrap;
        Assert.That(garage.PlacementIssue,Does.Contain("Zajęte pole"));
        Assert.That(garage.PreviewValid,Is.False,"Zajęta komórka blokuje ponowny montaż.");
        var invalidColor=garage.Preview.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor");Assert.That(invalidColor.r,Is.GreaterThan(invalidColor.g));
        Assert.That(garage.ClickTarget(),Is.False);Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        Assert.That(Run.VehicleModel.Inventory.Any(p=>p.Id==secondId),Is.True);
        yield return null;
    }
    [UnityTest] public IEnumerator PointedMountedPartCanRotateWithoutChangingIdentityOrResources()
    {
        var cell=new Vector3Int(2,1,2);var part=Run.VehicleModel.At(cell);
        Assert.That(part,Is.Not.Null);Assert.That(part.Kind,Is.EqualTo(R3PartKind.Armor));
        string id=part.Id;float health=part.Health;int scrap=Run.VehicleModel.Scrap;
        var previousTool=Run.Garage.Tool;
        Assert.That(Run.Garage.RotatePointed(TopRay(cell),new Vector3Int(0,1,0)),Is.True);
        var rotated=Run.VehicleModel.At(cell);
        Assert.That(rotated.Id,Is.EqualTo(id));Assert.That(rotated.Health,Is.EqualTo(health));
        Assert.That(rotated.RotationQuarterTurns,Is.EqualTo(new Vector3Int(0,1,0)));
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(scrap));
        Assert.That(Run.Garage.Tool,Is.EqualTo(previousTool),"Rotation must preserve the active build tool.");
        Assert.That(Run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id).Block.DetachedFromVehicle,Is.False);
        yield return null;
    }
    [UnityTest] public IEnumerator RealGarageRotatesSideWheelsWithoutLosingTheirMount()
    {
        foreach(int side in new[]{-1,1})
        {
            var model=Run.VehicleModel;
            var wheel=model.Parts.First(p=>p.Kind==R3PartKind.Wheel&&Math.Sign(p.Cell.x)==side);
            var cell=wheel.Cell;string id=wheel.Id;int scrap=model.Scrap;float health=wheel.Health;
            int oldMask=R3MountingGrid.WorldPortMask(wheel.RotationQuarterTurns,wheel.ContactMask);
            Assert.That(Run.GarageRotate(cell,new Vector3Int(0,2,0)),Is.True,"Real physical installation must approve this axle rotation.");
            var rotated=Run.VehicleModel.At(cell);
            Assert.That(rotated.Id,Is.EqualTo(id));Assert.That(rotated.Health,Is.EqualTo(health));
            Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(scrap));
            Assert.That(R3MountingGrid.WorldPortMask(rotated.RotationQuarterTurns,rotated.ContactMask),Is.EqualTo(oldMask));
            var actual=Run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
            Assert.That(actual.Operational,Is.True);Assert.That(actual.Block.DetachedFromVehicle,Is.False);
            var socket=actual.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Socket_Mount");
            var towardSupport=Run.Player.transform.TransformDirection(side<0?Vector3.right:Vector3.left);
            Assert.That(Vector3.Distance(socket.position,actual.transform.position+towardSupport*.26f),Is.LessThan(.015f),
                "The rendered axle socket must still meet the real supporting face.");
            yield return null;
        }
    }
    [UnityTest] public IEnumerator WheelRotationShortcutSkipsTheInvalidSidewaysIntermediatePose()
    {
        var garage=Run.Garage;
        foreach(int side in new[]{-1,1})
        {
            var model=Run.VehicleModel;
            var wheel=model.Parts.First(p=>p.Kind==R3PartKind.Wheel&&Math.Sign(p.Cell.x)==side);
            var id=wheel.Id;var cell=wheel.Cell;var before=wheel.RotationQuarterTurns;
            int mask=R3MountingGrid.WorldPortMask(before,wheel.ContactMask);int scrap=model.Scrap;
            var actual=Run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
            var outward=Run.Player.transform.right*side;
            var ray=new Ray(actual.transform.position+outward*3f,-outward);
            Assert.That(garage.RotatePointed(ray,new Vector3Int(0,side,0)),Is.True,
                "A single horizontal rotation shortcut must reach the next legal upright wheel pose.");
            var rotated=Run.VehicleModel.At(cell);
            Assert.That(rotated.Id,Is.EqualTo(id));Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(scrap));
            Assert.That(rotated.RotationQuarterTurns,Is.EqualTo(R3MountingGrid.Normalize(before+new Vector3Int(0,2,0))));
            Assert.That(R3MountingGrid.WorldPortMask(rotated.RotationQuarterTurns,rotated.ContactMask),Is.EqualTo(mask));
            Assert.That(R3VehicleModel.ValidWheelOrientation(rotated.RotationQuarterTurns),Is.True);
            actual=Run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
            ray=new Ray(actual.transform.position+outward*3f,-outward);
            Assert.That(garage.RotatePointed(ray,new Vector3Int(1,0,0)),Is.False,"Wheels must not be flipped upside down.");
            yield return null;
        }
    }
    [UnityTest] public IEnumerator WheelGhostRotationStaysUprightAndCanStillBeMounted()
    {
        var garage=Run.Garage;var model=Run.VehicleModel;string id=PurchasePart(R3PartKind.Wheel);
        garage.ChooseInventory(id);
        var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        var cell=cells.First(c=>model.OwnedPlacementError(id,c,Vector3Int.zero)==null);
        garage.Select(cell);Assert.That(garage.PreviewValid,Is.True,garage.PlacementIssue);
        garage.RotateSelected(1,1);
        Assert.That(garage.SelectedRotation,Is.EqualTo(new Vector3Int(0,2,0)));
        Assert.That(garage.PreviewValid,Is.True,garage.PlacementIssue);
        garage.RotateSelected(0,1);
        Assert.That(garage.SelectedRotation,Is.EqualTo(new Vector3Int(0,2,0)),"A wheel cannot be flipped on its side.");
        Assert.That(garage.PreviewValid,Is.True,garage.PlacementIssue);
        Assert.That(garage.ClickTarget(),Is.True,garage.Message);
        Assert.That(Run.VehicleModel.At(cell).Id,Is.EqualTo(id));
        string legacyId=PurchasePart(R3PartKind.Wheel);
        var legacy=Run.VehicleModel.Inventory.Single(p=>p.Id==legacyId);
        legacy.RotationQuarterTurns=new Vector3Int(1,0,0);
        garage.ChooseInventory(legacyId);
        Assert.That(R3VehicleModel.ValidWheelOrientation(garage.SelectedRotation),Is.False);
        garage.RotateSelected(0,-1);
        Assert.That(garage.SelectedRotation,Is.EqualTo(Vector3Int.zero),"An invalid legacy wheel pose must remain correctable.");
        yield return null;
    }
    [UnityTest] public IEnumerator MountedLegacyWheelCanBeTurnedBackIntoAValidPose()
    {
        var wheel=Run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x<0);
        string id=wheel.Id;var cell=wheel.Cell;
        // Represent an older stored pose; the pointed hit still uses the real
        // current collider and the proposed replacement uses the physical gate.
        wheel.RotationQuarterTurns=new Vector3Int(0,1,0);
        Assert.That(R3VehicleModel.ValidWheelOrientation(wheel.RotationQuarterTurns),Is.False);
        var actual=Run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        var outward=-Run.Player.transform.right;
        var ray=new Ray(actual.transform.position+outward*3f,-outward);
        Assert.That(Run.Garage.RotatePointed(ray,new Vector3Int(0,1,0)),Is.True,
            "A legacy sideways axle must be correctable using one 90-degree step.");
        var corrected=Run.VehicleModel.At(cell);
        Assert.That(corrected.Id,Is.EqualTo(id));
        Assert.That(corrected.RotationQuarterTurns,Is.EqualTo(new Vector3Int(0,2,0)));
        Assert.That(R3VehicleModel.ValidWheelOrientation(corrected.RotationQuarterTurns),Is.True);
        yield return null;
    }
    [UnityTest] public IEnumerator ShiftEEventRotatesTheWheelUnderTheActualGaragePointer()
    {
        var garage=Run.Garage;var camera=Camera.main;Assert.That(camera,Is.Not.Null);
        garage.SetTool(R41GarageTool.Inspect);
        R3MountedPart pointed=null;Vector3 screen=default;
        foreach(var part in Run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p!=null&&p.Kind==R3PartKind.Wheel))
        {
            var at=camera.WorldToScreenPoint(part.transform.position);
            if(at.z<=0||!R3GarageView.InBuildArea(at,Screen.width,Screen.height))continue;
            if(!garage.Point(camera.ScreenPointToRay(at))||garage.Cell!=part.Cell)continue;
            pointed=part;screen=at;break;
        }
        Assert.That(pointed,Is.Not.Null,"The actual garage viewport must expose a selectable wheel.");
        string id=pointed.Id;var cell=pointed.Cell;int before=Run.VehicleModel.Scrap;
        var key=new Event{type=EventType.KeyDown,keyCode=KeyCode.E,modifiers=EventModifiers.Shift};
        garage.HandleGuiPointer(key,new Vector2(screen.x,Screen.height-screen.y));
        Assert.That(key.type,Is.EqualTo(EventType.Used));
        var rotated=Run.VehicleModel.At(cell);
        Assert.That(rotated.Id,Is.EqualTo(id));Assert.That(rotated.RotationQuarterTurns.y,Is.EqualTo(2));
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(before));Assert.That(garage.Tool,Is.EqualTo(R41GarageTool.Inspect));
        yield return null;
    }
    [UnityTest] public IEnumerator ReassignedRotationKeyTurnsGhostAndPointedWheel()
    {
        var profile=R1ControlProfile.Current;profile.ResetDefaults();
        try
        {
            Assert.That(profile.TryBind("GarageRotateYRight","K"),Is.True);
            var garage=Run.Garage;Run.VehicleModel.Award(100);
            var existing=Run.VehicleModel.Inventory.Select(part=>part.Id).ToArray();Assert.That(Run.GaragePurchase(R3PartKind.Armor),Is.True);
            string armor=Run.VehicleModel.Inventory.First(part=>part.Kind==R3PartKind.Armor&&!existing.Contains(part.Id)).Id;garage.ChooseInventory(armor);
            garage.HandleGuiPointer(new Event{type=EventType.KeyDown,keyCode=KeyCode.K},Vector2.zero);
            Assert.That(garage.SelectedRotation.y,Is.EqualTo(1));
            var camera=Camera.main;garage.SetTool(R41GarageTool.Inspect);R3MountedPart pointed=null;Vector3 screen=default;
            foreach(var part in Run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p!=null&&p.Kind==R3PartKind.Wheel))
            {
                var at=camera.WorldToScreenPoint(part.transform.position);
                if(at.z<=0||!R3GarageView.InBuildArea(at,Screen.width,Screen.height))continue;
                if(!garage.Point(camera.ScreenPointToRay(at))||garage.Cell!=part.Cell)continue;
                pointed=part;screen=at;break;
            }
            Assert.That(pointed,Is.Not.Null);var cell=pointed.Cell;string id=pointed.Id;int wallet=Run.VehicleModel.Scrap;
            garage.HandleGuiPointer(new Event{type=EventType.KeyDown,keyCode=KeyCode.K,shift=true},new Vector2(screen.x,Screen.height-screen.y));
            Assert.That(Run.VehicleModel.At(cell).Id,Is.EqualTo(id));Assert.That(Run.VehicleModel.At(cell).RotationQuarterTurns.y,Is.EqualTo(2));
            Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(wallet));
        }
        finally{profile.ResetDefaults();}
        yield return null;
    }
    [UnityTest] public IEnumerator GhostColorTracksBlockedAndAllowedPlacementAfterReappearing()
    {
        var garage=Run.Garage;string id=PurchasePart(R3PartKind.Armor);garage.ChooseInventory(id);
        var ray=TopRay(new Vector3Int(2,1,2));Assert.That(garage.Point(ray),Is.True);
        var valid=garage.Preview.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor");
        Assert.That(valid.g,Is.GreaterThan(valid.r));
        garage.Select(new Vector3Int(2,1,2));
        var invalid=garage.Preview.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor");
        Assert.That(invalid.r,Is.GreaterThan(invalid.g));
        garage.RefreshMarker();Assert.That(garage.Preview.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor"),Is.EqualTo(invalid));
        garage.SetTool(R41GarageTool.Remove);Assert.That(garage.Preview.gameObject.activeSelf,Is.False);
        garage.ChooseInventory(id);Assert.That(garage.Point(ray),Is.True);
        Assert.That(garage.Preview.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor"),Is.EqualTo(valid),
            "A newly shown ghost must restore the allowed color even after an invalid placement.");
        yield return null;
    }
    [UnityTest] public IEnumerator CrowdedRaycastStillSelectsTheSameRealMountPoint()
    {
        var garage=Run.Garage;string id=PurchasePart(R3PartKind.Armor);garage.ChooseInventory(id);
        var ray=TopRay(new Vector3Int(2,1,2));
        Assert.That(garage.Point(ray),Is.True,"The original mount must be selectable.");
        var original=garage.Cell;
        var obstacles=new GameObject("Crowded ray fixture");
        try
        {
            for(int i=0;i<520;i++)
            {
                var child=new GameObject("overlap "+i);child.transform.SetParent(obstacles.transform,false);
                child.transform.position=ray.GetPoint(.1f+i*.004f);
                var collider=child.AddComponent<BoxCollider>();collider.size=Vector3.one*.01f;collider.isTrigger=true;
            }
            Physics.SyncTransforms();
            Assert.That(Physics.RaycastAll(ray,100,~0,QueryTriggerInteraction.Collide).Length,Is.GreaterThan(512));
            Assert.That(garage.Point(ray),Is.True,"A complete query can still identify the genuine mounting socket.");
            Assert.That(garage.Cell,Is.EqualTo(original),"Crowding must not select another mount.");
        }
        finally {UnityEngine.Object.Destroy(obstacles);}
        yield return null;
    }

    [UnityTest] public IEnumerator CrowdedGaragePointerAllocationDiagnostic()
    {
        var garage=Run.Garage;string id=PurchasePart(R3PartKind.Armor);garage.ChooseInventory(id);
        var ray=TopRay(new Vector3Int(2,1,2));
        var obstacles=new GameObject("Crowded garage allocation fixture");
        try
        {
            for(int i=0;i<520;i++)
            {
                var child=new GameObject("overlap "+i);child.transform.SetParent(obstacles.transform,false);
                child.transform.position=ray.GetPoint(.1f+i*.004f);
                var collider=child.AddComponent<BoxCollider>();collider.size=Vector3.one*.01f;collider.isTrigger=true;
            }
            Physics.SyncTransforms();
            Assert.That(Physics.RaycastAll(ray,100,~0,QueryTriggerInteraction.Collide).Length,Is.GreaterThan(512));
            for(int i=0;i<3;i++)Assert.That(garage.Point(ray),Is.True);
            using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",16))
            {
                Assert.That(recorder.Valid,Is.True);yield return null;yield return null;long idle=recorder.LastValue;
                bool ok=true;for(int i=0;i<20;i++)ok&=garage.Point(ray);
                yield return null;long sample=recorder.LastValue;
                var gate=Run.Player.GetComponent<R3BuildValidation>();
                var installedMethod=typeof(R3BuildValidation).GetMethod("Installed",BindingFlags.NonPublic|BindingFlags.Instance);
                var collect=(Func<System.Collections.Generic.List<B01.BUILD001.BuildInstalledPart>>)Delegate.CreateDelegate(typeof(Func<System.Collections.Generic.List<B01.BUILD001.BuildInstalledPart>>),gate,installedMethod);
                var snapshot=collect();var selected=Run.VehicleModel.Inventory.Single(p=>p.Id==id).Copy();
                selected.Cell=garage.Cell;selected.RotationQuarterTurns=garage.SelectedRotation;
                var definition=(B01.BUILD001.BuildDefinition)typeof(R3BuildValidation).GetMethod("Definition",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(gate,new object[]{selected});
                var candidate=new B01.BUILD001.BuildInstalledPart(id,definition,new B01.BUILD001.BuildPose(selected.Cell,selected.RotationQuarterTurns),true);
                Assert.That(B01.BUILD001.BuildPlacementRules.Check(snapshot,candidate,out _),Is.EqualTo(B01.BUILD001.BuildPlacementResult.Valid));
                yield return null;
                for(int i=0;i<20;i++)snapshot=collect();
                yield return null;long installedBytes=recorder.LastValue;
                for(int i=0;i<20;i++)B01.BUILD001.BuildPlacementRules.Check(snapshot,candidate,out _);
                yield return null;long graphBytes=recorder.LastValue;
                var control=new byte[1024*1024];control[0]=1;yield return null;long controlBytes=recorder.LastValue;GC.KeepAlive(control);
                Debug.Log($"[GARAGE_QUERY_ALLOC] queries=20 colliders=520 idle={idle} sample={sample} installed={installedBytes} graph={graphBytes} control={controlBytes}");
                Assert.That(controlBytes,Is.GreaterThanOrEqualTo(1024*1024));Assert.That(ok,Is.True);
            }
        }
        finally{UnityEngine.Object.Destroy(obstacles);}
    }

    [UnityTest] public IEnumerator InstallationSnapshotTracksColliderEnablement()
    {
        var gate=Run.Player.GetComponent<R3BuildValidation>();
        var assembly=Run.Player.GetComponent<R3VehicleAssembly>();
        var armor=assembly.Parts.First(p=>p!=null&&p.Kind==R3PartKind.Armor&&p.Operational);
        var colliders=armor.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&c.attachedRigidbody==Run.Player.GetComponent<Rigidbody>()).ToArray();
        Assert.That(colliders,Is.Not.Empty);
        var collect=(Func<System.Collections.Generic.List<B01.BUILD001.BuildInstalledPart>>)Delegate.CreateDelegate(
            typeof(Func<System.Collections.Generic.List<B01.BUILD001.BuildInstalledPart>>),gate,
            typeof(R3BuildValidation).GetMethod("Installed",BindingFlags.NonPublic|BindingFlags.Instance));
        try
        {
            Assert.That(collect().Single(p=>p.PartId==armor.Id).Alive,Is.True);
            foreach(var collider in colliders)collider.enabled=false;
            Assert.That(collect().Single(p=>p.PartId==armor.Id).Alive,Is.False,"A colliderless block cannot become support.");
            foreach(var collider in colliders)collider.enabled=true;
            Assert.That(collect().Single(p=>p.PartId==armor.Id).Alive,Is.True,"Re-enabled real support must become available again.");
        }
        finally{foreach(var collider in colliders)if(collider!=null)collider.enabled=true;}
        yield return null;
    }

    [UnityTest] public IEnumerator ForeignVehicleAndPlanDoNotOverrideOwnSocket()
    {
        var garage=Run.Garage;string id=PurchasePart(R3PartKind.Armor);garage.ChooseInventory(id);
        var ray=TopRay(new Vector3Int(2,1,2));Assert.That(garage.Point(ray),Is.True);var original=garage.Cell;
        var other=GameObject.CreatePrimitive(PrimitiveType.Cube);other.name="Foreign part";
        other.transform.position=ray.GetPoint(.1f);other.transform.localScale=Vector3.one*.05f;
        var part=other.AddComponent<R3MountedPart>();part.Id="foreign";part.Kind=R3PartKind.Armor;
        part.Damageable=other.AddComponent<P12PipelineSafeDamageable>();part.Damageable.Configure(100,true);
        part.Block=other.AddComponent<P12_PipelineSafePlayerBlock>();
        var plan=GameObject.CreatePrimitive(PrimitiveType.Cube);plan.name="Foreign plan";
        plan.transform.position=ray.GetPoint(.3f);plan.transform.localScale=Vector3.one*.05f;
        plan.AddComponent<R41MissingPlan>().Part=new R3PartState(Vector3Int.zero,R3PartKind.Armor);
        try
        {
            Physics.SyncTransforms();Assert.That(garage.Point(ray),Is.True);
            Assert.That(garage.Cell,Is.EqualTo(original),"Only this vehicle and its preview can provide the target.");
        }
        finally{UnityEngine.Object.Destroy(other);UnityEngine.Object.Destroy(plan);}
        yield return null;
    }

    [UnityTest] public IEnumerator FullRaycastBufferCannotAuthorizeAHiddenMountTarget()
    {
        var garage=Run.Garage;string id=PurchasePart(R3PartKind.Armor);garage.ChooseInventory(id);
        var ray=TopRay(new Vector3Int(2,1,2));
        Assert.That(garage.Point(ray),Is.True,"The unobstructed mount point is selectable.");
        var obstacles=new GameObject("Overflow raycast fixture");
        try
        {
            for(int i=0;i<520;i++)
            {
                var child=new GameObject("overlap "+i);child.transform.SetParent(obstacles.transform,false);
                child.transform.position=ray.GetPoint(.1f+i*.004f);
                var collider=child.AddComponent<BoxCollider>();collider.size=Vector3.one*.01f;collider.isTrigger=true;
            }
            Run.Player.gameObject.SetActive(false);
            Physics.SyncTransforms();
            Assert.That(garage.Point(ray),Is.False,"Crowding must never manufacture a hidden mount target.");
            Assert.That(garage.HasTarget,Is.False);
        }
        finally {Run.Player.gameObject.SetActive(true);UnityEngine.Object.Destroy(obstacles);}
        yield return null;
        Assert.That(garage.Point(ray),Is.True,"The same mount becomes selectable once the obstruction is gone.");
    }
    [UnityTest] public IEnumerator PointedRepairRemovalAndInventoryMountUseActualPartAndSurviveNextFight()
    {
        var garage=Run.Garage;var cell=new Vector3Int(2,2,2);string purchased=PurchasePart(R3PartKind.Armor);garage.ChooseInventory(purchased);
        Assert.That(garage.Point(TopRay(cell-Vector3Int.up)),Is.True);Assert.That(garage.Cell,Is.EqualTo(cell));Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);
        var mounted=Run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Cell==cell);string id=mounted.Id;
        Assert.That(id,Is.EqualTo(purchased));
        mounted.Damageable.ApplyDamage(mounted.Damageable.MaxHp*0.4f,mounted.transform.position,Vector3.up);
        Run.Player.GetComponent<R3VehicleAssembly>().Capture();garage.SetTool(R41GarageTool.Repair);
        Assert.That(garage.Point(TopRay(cell)),Is.True);Assert.That(garage.Cell,Is.EqualTo(cell));
        int funds=Run.VehicleModel.Scrap;Assert.That(garage.ClickTarget(),Is.True);
        Assert.That(Run.VehicleModel.At(cell).Health,Is.EqualTo(1));Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds-1),"ceil(armor5 * missing0.4 * 0.35) = 1.");
        garage.SetTool(R41GarageTool.Remove);Assert.That(garage.Point(TopRay(cell)),Is.True);Assert.That(garage.ClickTarget(),Is.True);
        Assert.That(Run.VehicleModel.At(cell),Is.Null);Assert.That(Run.VehicleModel.Inventory.Single(p=>p.Id==id).Health,Is.EqualTo(1));
        garage.ChooseInventory(id);Assert.That(garage.Point(TopRay(cell-Vector3Int.up)),Is.True);
        funds=Run.VehicleModel.Scrap;Assert.That(garage.ClickTarget(),Is.True);Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        Assert.That(Run.VehicleModel.At(cell).Id,Is.EqualTo(id));Assert.That(Run.StartNextEncounter(),Is.True);
        yield return Until(()=>!Run.Transitioning,"Następne starcie");Run.Encounter.SetCombatActive(false);
        Assert.That(Run.VehicleModel.At(cell).Id,Is.EqualTo(id));Assert.That(Run.Player.Modules.WeaponOnline,Is.True);
        R1EncounterManager.ClearTransientObjects();var assembly=Run.Player.GetComponent<R3VehicleAssembly>();
        foreach(var gun in assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun))gun.GunPivot.rotation=Quaternion.Euler(-70,0,0);
        Physics.SyncTransforms();
        Assert.That(assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun).Any(assembly.ClearProjectilePath),Is.True,"The physical firing test needs a clear path after armor is mounted.");
        assembly.FireGuns();
        Assert.That(UnityEngine.Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Length,Is.GreaterThan(0));
        Assert.That(garage.Point(TopRay(cell)),Is.False,"Walka nie może przyjmować wejścia garażu.");
    }
    [UnityTest] public IEnumerator MissingPlansCanBePointedAtAndRebuiltWithoutPhantomPhysics()
    {
        var assembly=Run.Player.GetComponent<R3VehicleAssembly>();var wheel=assembly.Parts.First(p=>p.Kind==R3PartKind.Wheel);
        var cell=wheel.Cell;wheel.Damageable.ApplyDamage(10000,wheel.transform.position,Vector3.up);assembly.Capture();Run.Garage.RefreshPreview();
        Run.Garage.SetTool(R41GarageTool.Repair);
        Assert.That(Run.Garage.Point(TopRay(cell)),Is.True);Assert.That(Run.Garage.Cell,Is.EqualTo(cell));
        Assert.That(Run.VehicleModel.At(cell).Health,Is.Zero);Assert.That(Run.Garage.ClickTarget(),Is.True);
        Assert.That(Run.VehicleModel.At(cell).Health,Is.EqualTo(1));Assert.That(Run.Player.Modules.ActiveWheelCount,Is.EqualTo(6));
        Assert.That(Run.Garage.Preview.GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True);
        yield return null;
    }
    [UnityTest] public IEnumerator WeaponGhostGeometryAndOrientationMatchMountedHeavyCannon()
    {
        Run.VehicleModel.Award(100);var garage=Run.Garage;string id=PurchasePart(R3PartKind.Gun,R4WeaponKind.Heavy);garage.ChooseInventory(id);
        // The current starter has raised front armor at z=3; the former
        // (2,2,2) placement legitimately intersects it. Mount on top of that
        // armor instead, and still verify the real physical gate below.
        Assert.That(garage.Point(TopRay(new Vector3Int(1,2,3))),Is.True);Assert.That(garage.Cell,Is.EqualTo(new Vector3Int(1,3,3)));
        Assert.That(garage.PreviewValid,Is.True,garage.PlacementIssue);
        var ghostBarrel=garage.Preview.Find("Obrót broni/Lufa");var size=ghostBarrel.localScale;var local=ghostBarrel.localPosition;
        var ghostArt=garage.Preview.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4W_Heavy_ROOT");
        var artPosition=ghostArt.position;var artRotation=ghostArt.rotation;var artScale=ghostArt.lossyScale;
        var localRotation=garage.Preview.localRotation;var cell=garage.Cell;
        int mountFunds=Run.VehicleModel.Scrap;Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(mountFunds));
        var mounted=Run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Cell==cell);Assert.That(mounted.Id,Is.EqualTo(id));var barrel=mounted.GunPivot.Find("Lufa");
        Assert.That(barrel.localScale,Is.EqualTo(size));Assert.That(barrel.localPosition,Is.EqualTo(local));
        var mountedArt=mounted.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4W_Heavy_ROOT");
        Assert.That(Vector3.Distance(artPosition,mountedArt.position),Is.LessThan(.001f),"Mounted art must start at the ghost position.");
        Assert.That(Quaternion.Angle(artRotation,mountedArt.rotation),Is.LessThan(.01f),"Mounted art must start at the ghost rotation.");
        Assert.That(Vector3.Distance(artScale,mountedArt.lossyScale),Is.LessThan(.001f),"Mounted art must match the ghost scale.");
        Assert.That(localRotation,Is.EqualTo(Quaternion.identity));
        Assert.That(garage.Preview.GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True);
        garage.Select(new Vector3Int(6,5,7));Assert.That(garage.PreviewValid,Is.False);
        int funds=Run.VehicleModel.Scrap;Assert.That(garage.ClickTarget(),Is.False);Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        yield return null;
    }
    [UnityTest] public IEnumerator GhostShowsCurrentWeaponAndWheelModelsWithoutPhysics()
    {
        var garage=Run.Garage;
        garage.UseInventory=false;garage.SelectedInventoryId=null;
        garage.SelectedPart=R3PartKind.Gun;garage.SelectedWeapon=R4WeaponKind.Heavy;
        garage.SetTool(R41GarageTool.Mount);garage.Select(new Vector3Int(2,2,2));
        var gunGhost=garage.Preview;
        Assert.That(gunGhost.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4W_Heavy_ROOT"),Is.True,
            "The preview should show the supplied Heavy model.");
        Assert.That(gunGhost.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),Is.True);
        garage.SelectedPart=R3PartKind.Wheel;garage.Select(new Vector3Int(3,2,2));
        var wheelGhost=garage.Preview;
        Assert.That(wheelGhost.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4M_WheelM_Side_Offroad_ROOT"),Is.True,
            "The preview should show the current side-mounted wheel model.");
        Assert.That(wheelGhost.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),Is.True);
        garage.SelectedPart=R3PartKind.Gun;garage.SelectedWeapon=R4WeaponKind.Automatic;garage.Select(new Vector3Int(3,2,2));
        Assert.That(garage.Preview.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R41_AutomaticGun_ROOT"),Is.True,
            "Automatic must use its supplied turret model in the preview.");
        Assert.That(garage.Preview.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),Is.True);
        yield return null;
    }
    [UnityTest] public IEnumerator MovementGhostsUseSuppliedModelsAndMirrorByVehicleSide()
    {
        var garage=Run.Garage;garage.UseInventory=false;garage.SelectedInventoryId=null;
        var cases=new[]{
            (R3PartKind.Suspension,"R4M_Suspension_ROOT"),
            (R3PartKind.Booster,"R4M_Booster_ROOT"),
            (R3PartKind.JumpDrive,"R4M_JumpDrive_ROOT"),
            (R3PartKind.Blink,"R4M_Blink_ROOT"),
            (R3PartKind.Gyroscope,"R4M_Gyroscope_ROOT"),
            (R3PartKind.OmniDrive,"R4M_OmniDrive_ROOT"),
            (R3PartKind.MagneticGrapple,"R4M_MagneticGrapple_ROOT"),
            (R3PartKind.HoverEngine,"R4M_HoverEngine_ROOT"),
            (R3PartKind.CrawlerTrack,"R4M_CrawlerTrack_ROOT"),
            (R3PartKind.IndependentSteering,"R4M_IndependentSteering_ROOT")
        };
        foreach(var (kind,name) in cases)
        {
            garage.SelectedPart=kind;garage.SetTool(R41GarageTool.Mount);garage.Select(new Vector3Int(-2,2,2));
            Assert.That(garage.Preview.GetComponentsInChildren<Transform>(true).Any(t=>t.name==name),Is.True,kind+" preview");
            Assert.That(garage.Preview.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),Is.True,kind+" preview must stay nonphysical");
        }
        foreach(var kind in new[]{R3PartKind.OmniDrive,R3PartKind.CrawlerTrack})
        {
            var name=kind==R3PartKind.OmniDrive?"R4M_OmniDrive_ROOT":"R4M_CrawlerTrack_ROOT";
            garage.SelectedPart=kind;garage.SetTool(R41GarageTool.Mount);garage.Select(new Vector3Int(-2,2,2));
            var left=garage.Preview.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name).parent.localScale.x;
            garage.Select(new Vector3Int(2,2,2));
            var right=garage.Preview.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name).parent.localScale.x;
            Assert.That(left*right,Is.LessThan(0),kind+" ghost should mirror when moving between sides");
        }
        yield return null;
    }
    [UnityTest] public IEnumerator IndustrialGarageKeepsVehicleVisibleAndRestoresArenaWithoutChangingVehicle()
    {
        var garage=Run.Garage;garage.Close();float near=Camera.main.nearClipPlane;Run.Arena.Build(RoboRougelike.R5.R5ArenaKind.Industrial);
        Run.Player.GetComponent<Rigidbody>().interpolation=RigidbodyInterpolation.None;
        Run.Player.transform.SetPositionAndRotation(new Vector3(-17,.7f,0),Quaternion.identity);Physics.SyncTransforms();
        var oldPosition=Run.Player.transform.position;
        var model=Run.VehicleModel;string before=string.Join("|",model.Parts.Concat(model.Inventory).Select(p=>JsonUtility.ToJson(p)));int funds=model.Scrap;
        var arena=Run.Arena.Layout.gameObject;var colliders=arena.GetComponentsInChildren<Collider>();
        garage.Open();garage.Tilt(-100);yield return null;yield return null;
        var camera=Camera.main;var target=Run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Core).transform.position;
        var workshopPosition=Run.Player.transform.position;
        Assert.That(Vector3.Distance(workshopPosition,oldPosition),Is.GreaterThan(100),"Garaż ma być oddzielony od areny.");
        Assert.That(Quaternion.Angle(Run.Player.transform.rotation,Quaternion.identity),Is.LessThan(.01f),"Po wejściu pojazd ma neutralny obrót.");
        var direction=target-camera.transform.position;
        Debug.Log("[W2 Garage fixture] camera="+camera.transform.position+" target="+target+" arena="+arena.transform.position);
        var sight=new Ray(target,-direction.normalized);
        var blocking=arena.GetComponentsInChildren<Renderer>().Where(r=>r.gameObject.activeInHierarchy&&r.enabled&&r.bounds.IntersectRay(sight,out float entry)&&entry<direction.magnitude).ToArray();
        Assert.That(blocking,Is.Empty,"Industrial halls must not obscure the vehicle while orbiting in the garage.");
        garage.Open();garage.Close();
        Assert.That(arena.activeSelf,Is.True,"Repeated Open must preserve the original combat arena state.");
        Assert.That(colliders.All(c=>c.enabled&&c.gameObject.activeInHierarchy),Is.True,"Combat cover must return on leaving the garage.");
        Assert.That(Run.Player.transform.position,Is.EqualTo(workshopPosition));Assert.That(Quaternion.Angle(Run.Player.transform.rotation,Quaternion.identity),Is.LessThan(.01f));
        Assert.That(Run.VehicleModel,Is.SameAs(model));Assert.That(string.Join("|",model.Parts.Concat(model.Inventory).Select(p=>JsonUtility.ToJson(p))),Is.EqualTo(before));
        Assert.That(Camera.main.nearClipPlane,Is.EqualTo(near));Assert.That(model.Scrap,Is.EqualTo(funds));
        arena.SetActive(false);garage.Open();garage.Close();Assert.That(arena.activeSelf,Is.False,"An already hidden arena must stay hidden.");
    }
    [TestCase(1280,720)] [TestCase(1024,768)] public void PanelsCannotClickThroughAtBothAspectRatios(int width,int height)
    {
        Assert.That(R3GarageView.InBuildArea(new Vector2(width*0.1f,height*0.5f),width,height),Is.False);
        Assert.That(R3GarageView.InBuildArea(new Vector2(width*0.5f,height*0.05f),width,height),Is.False);
        Assert.That(R3GarageView.InBuildArea(new Vector2(width*0.5f,height*0.5f),width,height),Is.True);
    }
    [UnityTest] public IEnumerator GarageOverlaysDoNotChangeSelectionOrMountParts()
    {
        var garage=Run.Garage;
        var chosen=new Vector3Int(0,2,1);garage.Select(chosen);
        int funds=Run.VehicleModel.Scrap, parts=Run.VehicleModel.Parts.Count, inventory=Run.VehicleModel.Inventory.Count;
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
        var offset=new Vector2((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2);
        foreach(var point in new[]{new Vector2(100,300),new Vector2(500,40),new Vector2(500,680)})
        {
            var raw=offset+point*scale;
            garage.HandleGuiPointer(new Event{type=EventType.MouseDown,button=0,mousePosition=raw},raw);
            Assert.That(garage.HasTarget,Is.True,"Klik nakładki nie powinien zmieniać celu montażu.");
            Assert.That(garage.Cell,Is.EqualTo(chosen));
            Assert.That(Run.VehicleModel.Parts.Count,Is.EqualTo(parts));
            Assert.That(Run.VehicleModel.Inventory.Count,Is.EqualTo(inventory));
            Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        }
        Assert.That(Camera.main.rect,Is.EqualTo(R3GarageView.ViewportForScreen(Screen.width,Screen.height)));
        yield return null;
    }
    [UnityTest] public IEnumerator SpentInventorySelectionNeverFallsBackToPaidPurchase()
    {
        var garage=Run.Garage;var cell=new Vector3Int(2,2,2);string id=PurchasePart(R3PartKind.Armor);garage.ChooseInventory(id);
        Assert.That(garage.Point(TopRay(cell-Vector3Int.up)),Is.True);Assert.That(garage.Cell,Is.EqualTo(cell));Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);
        Assert.That(Run.VehicleModel.At(cell).Id,Is.EqualTo(id));Assert.That(Run.GarageRemove(cell),Is.True);
        garage.ChooseInventory(id);Assert.That(garage.Point(TopRay(cell-Vector3Int.up)),Is.True);
        int funds=Run.VehicleModel.Scrap;Assert.That(garage.ClickTarget(),Is.True);Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        Assert.That(garage.Point(TopRay(new Vector3Int(-2,1,2))),Is.True);
        Assert.That(garage.Cell,Is.EqualTo(new Vector3Int(-2,2,2)));Assert.That(garage.PlacementIssue,Does.Contain("Wybierz kolejną część"));
        Assert.That(garage.ClickTarget(),Is.False,"Po zużyciu przedmiotu klik nie może kupić części bez świadomego wyboru zakupu.");
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        garage.UseInventory=true;garage.SelectedInventoryId=null;Assert.That(garage.Point(TopRay(new Vector3Int(-2,1,2))),Is.True);
        Assert.That(garage.ClickTarget(),Is.False,"Pusty wybór w otwartym magazynie również nie jest zakupem.");
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        yield return null;
    }
    [UnityTest] public IEnumerator RepeatedMountUsesNextOwnedItemWithoutBuying()
    {
        var garage=Run.Garage;string first=PurchasePart(R3PartKind.Armor),second=PurchasePart(R3PartKind.Armor);
        garage.ChooseInventory(first);int funds=Run.VehicleModel.Scrap;
        Assert.That(garage.Point(TopRay(new Vector3Int(2,1,2))),Is.True);
        Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);
        Assert.That(garage.SelectedInventoryId,Is.EqualTo(second),"Następny egzemplarz stosu powinien być gotowy do montażu.");
        Assert.That(garage.Point(TopRay(new Vector3Int(-2,1,2))),Is.True);
        Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);
        Assert.That(Run.VehicleModel.At(new Vector3Int(2,2,2)).Id,Is.EqualTo(first));
        Assert.That(Run.VehicleModel.At(new Vector3Int(-2,2,2)).Id,Is.EqualTo(second));
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds),"Powtarzanie montażu nie może nic automatycznie kupić.");
        Assert.That(garage.SelectedInventoryId,Is.Null);
        yield return null;
    }
    [UnityTest] public IEnumerator RepeatedBonusMountNeverSwitchesToDifferentEffectWithSameId()
    {
        var damage=ScriptableObject.CreateInstance<R1UpgradeDefinition>();damage.Id="shared";damage.Title="Moduł";damage.Effect=R1UpgradeEffect.Damage;damage.Amount=.2f;
        var speed=ScriptableObject.CreateInstance<R1UpgradeDefinition>();speed.Id="shared";speed.Title="Moduł";speed.Effect=R1UpgradeEffect.Mobility;speed.Amount=.2f;
        int before=Run.VehicleModel.Inventory.Count;
        Assert.That(Run.VehicleModel.ReceiveBonuses(new[]{damage,speed,damage}),Is.True);
        var items=Run.VehicleModel.Inventory.Skip(before).ToArray();var garage=Run.Garage;
        garage.ChooseInventory(items[0].Id);int wallet=Run.VehicleModel.Scrap;
        Assert.That(garage.Point(TopRay(new Vector3Int(2,1,2))),Is.True);
        Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);
        Assert.That(garage.SelectedInventoryId,Is.EqualTo(items[2].Id),"Next mounted part must retain the chosen bonus effect.");
        Assert.That(garage.Point(TopRay(new Vector3Int(-2,1,2))),Is.True);
        Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);
        Assert.That(garage.SelectedInventoryId,Is.Null,"A different effect must never be silently selected.");
        Assert.That(Run.VehicleModel.Inventory.Any(part=>part.Id==items[1].Id),Is.True);
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(wallet));
        UnityEngine.Object.Destroy(damage);UnityEngine.Object.Destroy(speed);yield return null;
    }
    [UnityTest] public IEnumerator MountingLastHealthyItemExplainsRemainingWreckWithoutBuying()
    {
        Run.VehicleModel.Award(100);
        string first=PurchasePart(R3PartKind.Armor),second=PurchasePart(R3PartKind.Armor);
        Run.VehicleModel.Inventory.Single(part=>part.Id==second).Health=0;
        var garage=Run.Garage;garage.ChooseInventory(first);
        Assert.That(garage.Point(TopRay(new Vector3Int(2,1,2))),Is.True);
        int wallet=Run.VehicleModel.Scrap;
        Assert.That(garage.ClickTarget(),Is.True);
        Assert.That(garage.SelectedInventoryId,Is.Null);
        Assert.That(garage.Message,Does.Contain("zniszczone"));
        Assert.That(Run.VehicleModel.Inventory.Any(part=>part.Id==second),Is.True);
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(wallet),"Mounting must never auto-buy a replacement.");
        yield return null;
    }
    [UnityTest] public IEnumerator ToolShortcutsChangeToolWithoutPurchasingOrEditing()
    {
        var garage=Run.Garage;int wallet=Run.VehicleModel.Scrap,parts=Run.VehicleModel.Parts.Count;
        garage.SetTool(R41GarageTool.Mount);
        var repair=new Event{type=EventType.KeyDown,keyCode=KeyCode.Alpha3};
        garage.HandleGuiPointer(repair,Vector2.zero);
        Assert.That(garage.Tool,Is.EqualTo(R41GarageTool.Repair));
        var modified=new Event{type=EventType.KeyDown,keyCode=KeyCode.Alpha4,modifiers=EventModifiers.Control};
        garage.HandleGuiPointer(modified,Vector2.zero);
        Assert.That(garage.Tool,Is.EqualTo(R41GarageTool.Repair));
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(wallet));Assert.That(Run.VehicleModel.Parts.Count,Is.EqualTo(parts));
        yield return null;
    }
    [UnityTest] public IEnumerator ReassignedToolUsesGarageContextWithoutEditingVehicle()
    {
        var profile=R1ControlProfile.Current;profile.ResetDefaults();
        try
        {
            Assert.That(profile.TryBind("GarageToolRepair","K"),Is.True);
            Assert.That(profile.TryBind("Weapon1","K"),Is.True);
            var garage=Run.Garage;int wallet=Run.VehicleModel.Scrap,parts=Run.VehicleModel.Parts.Count;
            garage.SetTool(R41GarageTool.Mount);garage.HandleGuiPointer(new Event{type=EventType.KeyDown,keyCode=KeyCode.K},Vector2.zero);
            Assert.That(garage.Tool,Is.EqualTo(R41GarageTool.Repair));
            garage.SetTool(R41GarageTool.Mount);garage.HandleGuiPointer(new Event{type=EventType.KeyDown,keyCode=KeyCode.Alpha3},Vector2.zero);
            Assert.That(garage.Tool,Is.EqualTo(R41GarageTool.Mount));
            Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(wallet));Assert.That(Run.VehicleModel.Parts.Count,Is.EqualTo(parts));
        }
        finally{profile.ResetDefaults();}
        yield return null;
    }
    [UnityTest] public IEnumerator GuiPointerEventsOrbitZoomAndNeverBuyThroughPanels()
    {
        var garage=Run.Garage;garage.Point(TopRay(new Vector3Int(2,1,2)));int funds=Run.VehicleModel.Scrap;
        garage.HandleGuiPointer(new Event{type=EventType.MouseDown,button=0},new Vector2(100,300));
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        var camera=Camera.main;var before=camera.transform.position;var middle=new Vector2(Screen.width*0.5f,Screen.height*0.5f);
        var down=new Event{button=1,mousePosition=middle,type=EventType.MouseDown};
        garage.HandleGuiPointer(down,middle);
        var drag=new Event{button=1,mousePosition=middle,delta=new Vector2(50,-20),type=EventType.MouseDrag};
        garage.HandleGuiPointer(drag,middle);
        yield return Until(()=>Vector3.Distance(camera.transform.position,before)>0.2f,"PPM musi obrócić kamerę w LateUpdate.");
        garage.HandleGuiPointer(new Event{type=EventType.MouseUp,button=1},middle);
        float distance=garage.Distance;garage.HandleGuiPointer(new Event{type=EventType.ScrollWheel,delta=new Vector2(0,2)},middle);
        Assert.That(garage.Distance,Is.EqualTo(distance+0.8f).Within(0.001));Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
    }
}
