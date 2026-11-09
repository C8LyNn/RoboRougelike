using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class W27GarageOccupiedZonesTests
{
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
    [UnityTest] public IEnumerator CompatibleMountPointsAreVisibleBeforeAimingAtOne()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        run.SelectVehicle(R3VehicleKind.Light);Assert.That(run.OpenSandboxGarage(),Is.True);
        var garage=run.Garage;garage.UseInventory=false;garage.SelectedPart=R3PartKind.Armor;
        garage.SetTool(RoboRougelike.R4_1.R41GarageTool.Mount);
        Assert.That(garage.HasTarget,Is.False);
        var overlay=UnityEngine.Object.FindFirstObjectByType<RoboRougelike.B01.GarageSpatialW28.B0112GarageSpatialOverlay>();
        Assert.That(overlay,Is.Not.Null);overlay.RebuildReadout(garage);
        Assert.That(overlay.VisibleBoxCount,Is.GreaterThan(0),
            "The garage should show usable attachment points before the player aims at one.");
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        var gun=new R3PartState(Vector3Int.zero,R3PartKind.Gun){
            ContactMask=R3VehicleModel.PlacementContacts(R3PartKind.Gun)};
        var points=R3BuildValidation.PotentialMountPoints(assembly,gun);
        Assert.That(points,Is.Not.Empty,"The light vehicle offers places for the bottom of a gun.");
        var surfaces=assembly.GetComponentsInChildren<B01.BUILD001.BuildMountSurface>();
        foreach(var point in points)
        {
            Assert.That(Vector3.Dot(point.Rotation*Vector3.forward,assembly.transform.up),Is.GreaterThan(.99f),
                "An upright turret's base must only match upward-facing supports.");
            var surface=surfaces.FirstOrDefault(s=>Vector3.Distance(s.transform.position,point.Position)<.001f);
            Assert.That(surface,Is.Not.Null,"Every visible point comes from an actual registered collider.");
            var box=surface.GetComponent<BoxCollider>();Assert.That(box,Is.Not.Null);
            Assert.That(box.size.x*surface.transform.lossyScale.x,Is.EqualTo(R3BuildValidation.GarageSocketSize).Within(.001f),
                "The selectable area matches the visible indicator.");
        }
        garage.SelectedPart=R3PartKind.Gun;
        var before=garage.AvailableMountPoints;
        Assert.That(before,Is.Not.Empty);
        garage.RotateSelected(0,1);
        var after=garage.AvailableMountPoints;
        Assert.That(after,Is.Not.SameAs(before),"Changing orientation must refresh the candidate points.");
        Assert.That(after,Is.Not.Empty);
        var expected=-R3MountingGrid.Rotate(Vector3Int.down,garage.SelectedRotation);
        foreach(var point in after)
            Assert.That(Vector3.Dot(point.Rotation*Vector3.forward,assembly.transform.TransformDirection(expected)),Is.GreaterThan(.99f),
                "The offered socket follows the rotated local gun base.");
    }
    [UnityTest] public IEnumerator SimplifiedGarageHidesGridAndStillAllowsConstruction()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        yield return VerifySimplifiedGrid();
    }
    static IEnumerator VerifySimplifiedGrid()
    {
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        run.SelectVehicle(R3VehicleKind.Light);Assert.That(run.OpenSandboxGarage(),Is.True);
        var lines=run.Player.GetComponentsInChildren<LineRenderer>().Where(l=>l.name=="W27 occupied cell").ToArray();
        Assert.That(lines,Is.Empty,"The garage no longer overlays every existing part with a distracting grid.");
        var garage=run.Garage;garage.UseInventory=false;garage.SelectedPart=R3PartKind.Armor;
        var free=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>run.VehicleModel.PlacementError(c,R3PartKind.Armor)==null);
        garage.Select(free);Assert.That(garage.PreviewValid,Is.True);
        Assert.That(garage.ClickTarget(),Is.True,"Construction still works without the old grid overlay.");
        Assert.That(run.Player.GetComponentsInChildren<LineRenderer>().Where(l=>l.name=="W27 occupied cell"),Is.Empty);
        run.Garage.Close();
        Assert.That(run.Player.GetComponentsInChildren<LineRenderer>().Any(l=>l.name=="W27 occupied cell"),Is.False,"Closing the garage must immediately hide its overlay.");
    }
    [UnityTest] public IEnumerator InvalidWheelGhostRemainsRedDuringRotation()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        run.SelectVehicle(R3VehicleKind.Light);Assert.That(run.OpenSandboxGarage(),Is.True);
        var garage=run.Garage;garage.UseInventory=false;garage.SelectedPart=R3PartKind.Wheel;garage.Select(run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Core).Cell);
        foreach(var turn in new[]{0,1})
        {
            if(turn!=0)garage.RotateSelected(0,turn);
            var visual=garage.Preview.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name=="Koło");
            Assert.That(visual,Is.Not.Null,"The ghost uses the actual wheel geometry.");
            var color=visual.sharedMaterial.color;
            Assert.That(color.r,Is.GreaterThan(color.g),"The whole invalid wheel ghost is red.");
            Assert.That(visual.GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True,"Ghost geometry cannot affect physics.");
        }
    }
    [UnityTest] public IEnumerator WedgePreviewUsesTheRealWedgeRatherThanACube()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        run.SelectVehicle(R3VehicleKind.Light);Assert.That(run.OpenSandboxGarage(),Is.True);
        run.VehicleModel.Award(100);
        Assert.That(run.GaragePurchase(R3PartKind.Armor,RoboRougelike.R4.R4WeaponKind.Basic,
            R3WheelStyle.Offroad,R3ArmorShape.Wedge),Is.True);
        var part=run.VehicleModel.Inventory.Last(p=>p.ArmorShape==R3ArmorShape.Wedge);
        run.Garage.ChooseInventory(part.Id);run.Garage.Select(new Vector3Int(0,1,1));
        var actual=run.Garage.Preview.GetComponent<MeshFilter>().sharedMesh;
        var wedge=RoboRougelike.R4_1.R41PartGeometry.CreateMountingBlock(R3ArmorShape.Wedge);
        Assert.That(actual,Is.SameAs(wedge.GetComponent<MeshFilter>().sharedMesh));
        Assert.That(run.Garage.Preview.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),Is.True);
        UnityEngine.Object.Destroy(wedge);
    }
    [UnityTest] public IEnumerator GunCannotSnapToUnmarkedSideSocket()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        run.SelectVehicle(R3VehicleKind.Standard);Assert.That(run.OpenSandboxGarage(),Is.True);
        var garage=run.Garage;garage.UseInventory=false;garage.SelectedPart=R3PartKind.Gun;
        garage.SetTool(RoboRougelike.R4_1.R41GarageTool.Mount);
        var root=run.Player.transform;var face=new Vector3(3.5f,0,2)*RoboRougelike.R4_1.R41PartGeometry.Grid;
        var ray=new Ray(root.TransformPoint(face+Vector3.right*.4f),-root.right);
        Assert.That(garage.Point(ray),Is.False,
            "An upright gun must not snap to a side socket which was not offered as compatible.");
        Assert.That(garage.Preview.gameObject.activeInHierarchy,Is.True,"An invalid surface still needs a visible red ghost.");
        Assert.That(garage.PreviewValid,Is.False);
        int scrap=run.VehicleModel.Scrap;
        Assert.That(garage.ClickTarget(),Is.False,"A red ghost must never authorize a purchase.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));
    }
    [UnityTest] public IEnumerator HeavyVehicleReusesMountPointListUntilItsSelectionChanges()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        run.SelectVehicle(R3VehicleKind.Heavy);Assert.That(run.OpenSandboxGarage(),Is.True);
        var garage=run.Garage;garage.UseInventory=false;garage.SelectedPart=R3PartKind.Gun;
        garage.SetTool(RoboRougelike.R4_1.R41GarageTool.Mount);
        var timer=System.Diagnostics.Stopwatch.StartNew();var first=garage.AvailableMountPoints;timer.Stop();
        Debug.Log("[MOUNT-POINT-QA] Heavy count="+first.Count+" first calculation ms="+timer.ElapsedMilliseconds);
        Assert.That(first,Is.Not.Empty);
        Assert.That(first.Count,Is.LessThan(RoboRougelike.B01.GarageSpatialW28.B0112GarageSpatialOverlay.MaxBoxes));
        Assert.That(garage.AvailableMountPoints,Is.SameAs(first),"Pointer movement must not rebuild the full graph.");
        garage.RotateSelected(0,1);
        Assert.That(garage.AvailableMountPoints,Is.Not.SameAs(first),"Rotation must refresh the compatible sockets.");
    }
}
