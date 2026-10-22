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
    [UnityTest] public IEnumerator DirectFaceClickMountsExactlyAtGhostWithoutMovingTheVehicle()
    {
        var garage=Run.Garage;var anchor=new Vector3Int(2,1,2);var target=anchor+Vector3Int.up;
        Assert.That(garage.Point(TopRay(anchor)),Is.True);Assert.That(garage.Cell,Is.EqualTo(target));
        Assert.That(garage.PreviewValid,Is.True);Vector3 ghost=garage.Preview.position;
        var validColor=garage.Preview.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor");Assert.That(validColor.g,Is.GreaterThan(validColor.r));
        var rotation=Run.Player.transform.rotation;var position=Run.Player.transform.position;int funds=Run.VehicleModel.Scrap;
        Assert.That(garage.ClickTarget(),Is.True);var mounted=Run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Cell==target);
        Assert.That(Vector3.Distance(mounted.transform.position,ghost),Is.LessThan(0.001f),"Część po kliknięciu musi pokrywać się z ghostem w świecie.");
        Assert.That(Quaternion.Angle(Run.Player.transform.rotation,rotation),Is.LessThan(0.001));
        Assert.That(Run.Player.transform.position,Is.EqualTo(position));Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds-5));
        Assert.That(garage.PreviewValid,Is.False,"Zajęta komórka blokuje ponowny montaż.");
        var invalidColor=garage.Preview.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor");Assert.That(invalidColor.r,Is.GreaterThan(invalidColor.g));
        Assert.That(garage.ClickTarget(),Is.False);Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds-5));
        yield return null;
    }
    [UnityTest] public IEnumerator PointedRepairRemovalAndInventoryMountUseActualPartAndSurviveNextFight()
    {
        var garage=Run.Garage;var cell=new Vector3Int(2,2,2);garage.Point(TopRay(cell-Vector3Int.up));garage.ClickTarget();
        var mounted=Run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Cell==cell);string id=mounted.Id;
        mounted.Damageable.ApplyDamage(mounted.Damageable.MaxHp*0.4f,mounted.transform.position,Vector3.up);
        Run.Player.GetComponent<R3VehicleAssembly>().Capture();garage.SetTool(R41GarageTool.Repair);
        Assert.That(garage.Point(TopRay(cell)),Is.True);Assert.That(garage.Cell,Is.EqualTo(cell));
        int funds=Run.VehicleModel.Scrap;Assert.That(garage.ClickTarget(),Is.True);
        Assert.That(Run.VehicleModel.At(cell).Health,Is.EqualTo(1));Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds-1),"ceil(armor5 * missing0.4 * 0.35) = 1.");
        garage.SetTool(R41GarageTool.Remove);Assert.That(garage.Point(TopRay(cell)),Is.True);Assert.That(garage.ClickTarget(),Is.True);
        Assert.That(Run.VehicleModel.At(cell),Is.Null);Assert.That(Run.VehicleModel.Inventory.Single(p=>p.Id==id).Health,Is.EqualTo(1));
        garage.SelectedInventoryId=id;garage.SetTool(R41GarageTool.Mount);Assert.That(garage.Point(TopRay(cell-Vector3Int.up)),Is.True);
        funds=Run.VehicleModel.Scrap;Assert.That(garage.ClickTarget(),Is.True);Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        Assert.That(Run.VehicleModel.At(cell).Id,Is.EqualTo(id));Assert.That(Run.StartNextEncounter(),Is.True);
        yield return Until(()=>!Run.Transitioning,"Następne starcie");Run.Encounter.SetCombatActive(false);
        Assert.That(Run.VehicleModel.At(cell).Id,Is.EqualTo(id));Assert.That(Run.Player.Modules.WeaponOnline,Is.True);
        R1EncounterManager.ClearTransientObjects();Run.Player.GetComponent<R3VehicleAssembly>().FireGuns();
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
        Assert.That(Run.VehicleModel.At(cell).Health,Is.EqualTo(1));Assert.That(Run.Player.Modules.ActiveWheelCount,Is.EqualTo(12));
        Assert.That(Run.Garage.Preview.GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True);
        yield return null;
    }
    [UnityTest] public IEnumerator WeaponGhostGeometryAndOrientationMatchMountedHeavyCannon()
    {
        Run.VehicleModel.Award(100);var garage=Run.Garage;garage.SelectedPart=R3PartKind.Gun;garage.SelectedWeapon=R4WeaponKind.Heavy;
        garage.SetTool(R41GarageTool.Mount);garage.Point(TopRay(new Vector3Int(2,1,2)));
        var ghostBarrel=garage.Preview.Find("Obrót broni/Lufa");var size=ghostBarrel.localScale;var local=ghostBarrel.localPosition;
        var localRotation=garage.Preview.localRotation;var cell=garage.Cell;
        Assert.That(garage.ClickTarget(),Is.True);var barrel=Run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Cell==cell).GunPivot.Find("Lufa");
        Assert.That(barrel.localScale,Is.EqualTo(size));Assert.That(barrel.localPosition,Is.EqualTo(local));
        Assert.That(localRotation,Is.EqualTo(Quaternion.identity));
        Assert.That(garage.Preview.GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True);
        garage.Select(new Vector3Int(6,5,7));Assert.That(garage.PreviewValid,Is.False);
        int funds=Run.VehicleModel.Scrap;Assert.That(garage.ClickTarget(),Is.False);Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        yield return null;
    }
    [UnityTest] public IEnumerator IndustrialGarageKeepsVehicleVisibleAndRestoresArenaWithoutChangingVehicle()
    {
        var garage=Run.Garage;garage.Close();float near=Camera.main.nearClipPlane;Run.Arena.Build(RoboRougelike.R5.R5ArenaKind.Industrial);
        Run.Player.GetComponent<Rigidbody>().interpolation=RigidbodyInterpolation.None;
        Run.Player.transform.SetPositionAndRotation(new Vector3(-17,.7f,0),Quaternion.identity);Physics.SyncTransforms();
        var position=Run.Player.transform.position;var rotation=Run.Player.transform.rotation;
        var model=Run.VehicleModel;string before=string.Join("|",model.Parts.Concat(model.Inventory).Select(p=>JsonUtility.ToJson(p)));int funds=model.Scrap;
        var arena=Run.Arena.Layout.gameObject;var colliders=arena.GetComponentsInChildren<Collider>();
        garage.Open();garage.Tilt(-100);yield return null;yield return null;
        var camera=Camera.main;var target=Run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Core).transform.position;
        Assert.That(target.x,Is.EqualTo(-17).Within(.1f),"The fixture must actually move the paused vehicle beside the industrial hall.");
        var direction=target-camera.transform.position;
        Debug.Log("[W2 Garage fixture] camera="+camera.transform.position+" target="+target+" arena="+arena.transform.position);
        var sight=new Ray(target,-direction.normalized);
        var blocking=arena.GetComponentsInChildren<Renderer>().Where(r=>r.gameObject.activeInHierarchy&&r.enabled&&r.bounds.IntersectRay(sight,out float entry)&&entry<direction.magnitude).ToArray();
        Assert.That(blocking,Is.Empty,"Industrial halls must not obscure the vehicle while orbiting in the garage.");
        garage.Open();garage.Close();
        Assert.That(arena.activeSelf,Is.True,"Repeated Open must preserve the original combat arena state.");
        Assert.That(colliders.All(c=>c.enabled&&c.gameObject.activeInHierarchy),Is.True,"Combat cover must return on leaving the garage.");
        Assert.That(Run.Player.transform.position,Is.EqualTo(position));Assert.That(Run.Player.transform.rotation,Is.EqualTo(rotation));
        Assert.That(Run.VehicleModel,Is.SameAs(model));Assert.That(string.Join("|",model.Parts.Concat(model.Inventory).Select(p=>JsonUtility.ToJson(p))),Is.EqualTo(before));
        Assert.That(Camera.main.nearClipPlane,Is.EqualTo(near));Assert.That(model.Scrap,Is.EqualTo(funds));
        arena.SetActive(false);garage.Open();garage.Close();Assert.That(arena.activeSelf,Is.False,"An already hidden arena must stay hidden.");
    }
    [TestCase(1280,720)] [TestCase(1024,768)] public void PanelsCannotClickThroughAtBothAspectRatios(int width,int height)
    {
        Assert.That(R3GarageView.InBuildArea(new Vector2(width*0.1f,height*0.5f),width,height),Is.False);
        Assert.That(R3GarageView.InBuildArea(new Vector2(width*0.85f,height*0.5f),width,height),Is.False);
        Assert.That(R3GarageView.InBuildArea(new Vector2(width*0.5f,height*0.5f),width,height),Is.True);
    }
    [UnityTest] public IEnumerator SpentInventorySelectionNeverFallsBackToPaidPurchase()
    {
        var garage=Run.Garage;var cell=new Vector3Int(2,2,2);Assert.That(garage.Point(TopRay(cell-Vector3Int.up)),Is.True);garage.ClickTarget();
        string id=Run.VehicleModel.At(cell).Id;Run.GarageRemove(cell);
        garage.SelectedInventoryId=id;garage.SetTool(R41GarageTool.Mount);garage.Point(TopRay(cell-Vector3Int.up));
        int funds=Run.VehicleModel.Scrap;Assert.That(garage.ClickTarget(),Is.True);Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        garage.Point(TopRay(new Vector3Int(-2,1,2)));
        Assert.That(garage.ClickTarget(),Is.False,"Po zużyciu przedmiotu klik nie może kupić części bez świadomego wyboru zakupu.");
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        garage.UseInventory=true;garage.SelectedInventoryId=null;garage.Point(TopRay(new Vector3Int(-2,1,2)));
        Assert.That(garage.ClickTarget(),Is.False,"Pusty wybór w otwartym magazynie również nie jest zakupem.");
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
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
