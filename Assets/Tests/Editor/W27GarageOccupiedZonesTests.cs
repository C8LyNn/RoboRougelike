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
    [UnityTest] public IEnumerator OccupiedGridIsVisibleAndTracksActualGarageConstructionWithoutColliders()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        run.SelectVehicle(R3VehicleKind.Light);Assert.That(run.OpenSandboxGarage(),Is.True);
        var lines=run.Player.GetComponentsInChildren<LineRenderer>().Where(l=>l.name=="W27 occupied cell").ToArray();
        Assert.That(lines.Length,Is.EqualTo(20),"Light construction has 15 armor/core cells, 4 wheels and 1 gun.");
        Assert.That(lines.All(l=>l.GetComponent<Collider>()==null),Is.True,"The overlay cannot affect hit tests or physics.");
        Assert.That(lines.All(l=>l.positionCount>=16&&!l.useWorldSpace),Is.True);
        Assert.That(lines.All(l=>l.sharedMaterial.color.r>l.sharedMaterial.color.g),Is.True);
        Assert.That(lines.All(l=>l.sharedMaterial.GetColor("_BaseColor").a>=.9f&&l.startWidth>=.02f),Is.True,
            "Occupied-cell outlines must remain visible against both bright and dark vehicle surfaces.");
        Assert.That(lines.Any(l=>Vector3.Distance(l.transform.localPosition,new Vector3(.55f,0,1.1f))<.001f),Is.True,"The displayed cell uses the real 0.55m grid.");
        Assert.That(run.GarageBuy(new Vector3Int(1,1,1),R3PartKind.Armor),Is.True);
        lines=run.Player.GetComponentsInChildren<LineRenderer>().Where(l=>l.name=="W27 occupied cell").ToArray();
        Assert.That(lines.Length,Is.EqualTo(21),"The overlay must refresh after an actual successful purchase.");
        Assert.That(lines.Any(l=>Vector3.Distance(l.transform.localPosition,new Vector3(.55f,.55f,.55f))<.001f),Is.True);
        Assert.That(run.GarageBuy(new Vector3Int(1,1,2),R3PartKind.DirectionalShield,new Vector3Int(3,0,0)),Is.True);
        lines=run.Player.GetComponentsInChildren<LineRenderer>().Where(l=>l.name=="W27 occupied cell").ToArray();
        Assert.That(lines.Length,Is.EqualTo(22),"The rotated shield reserves exactly one cell.");
        Assert.That(lines.Any(l=>Vector3.Distance(l.transform.localPosition,new Vector3(.55f,.55f,1.1f))<.001f),Is.True,"The rotated shield's occupied cell is shown.");
        run.Garage.Close();
        Assert.That(run.Player.GetComponentsInChildren<LineRenderer>().Any(l=>l.name=="W27 occupied cell"),Is.False,"Closing the garage must immediately hide its overlay.");
    }
    [UnityTest] public IEnumerator WheelGhostDistinguishesItsProtrudingGeometryDuringRotation()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        run.SelectVehicle(R3VehicleKind.Light);Assert.That(run.OpenSandboxGarage(),Is.True);
        var garage=run.Garage;garage.SelectedPart=R3PartKind.Wheel;garage.Select(new Vector3Int(1,1,1));
        foreach(var turn in new[]{0,1})
        {
            if(turn!=0)garage.RotateSelected(0,turn);
            var visual=garage.Preview.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name=="Koło");
            Assert.That(visual,Is.Not.Null,"The ghost uses the actual wheel geometry.");
            var color=visual.sharedMaterial.color;
            Assert.That(color.b,Is.GreaterThan(color.r));Assert.That(color.b,Is.GreaterThan(color.g));
            Assert.That(visual.sharedMaterial.GetColor("_BaseColor").a,Is.GreaterThanOrEqualTo(.65f),
                "The protruding blue geometry must remain distinguishable over an invalid red ghost.");
            Assert.That(visual.sharedMaterial.renderQueue,Is.GreaterThan(garage.Preview.GetComponent<Renderer>().sharedMaterial.renderQueue),
                "The blue protruding geometry must render after the translucent placement ghost.");
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
        var root=run.Player.transform;var face=new Vector3(2.5f,1,2)*RoboRougelike.R4_1.R41PartGeometry.Grid;
        var ray=new Ray(root.TransformPoint(face+Vector3.right*.4f),-root.right);
        Assert.That(garage.Point(ray),Is.False,
            "An upright gun must not snap to a side socket which was not offered as compatible.");
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
