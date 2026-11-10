using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4_1;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class GaragePointerStabilityTests
{
    [UnityTest] public IEnumerator CancellingBuildInputHidesGhostWithoutLosingSelectedPart()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return VerifyCancelBuildInput();
        yield return new ExitPlayMode();
    }
    static IEnumerator VerifyCancelBuildInput()
    {
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light,0),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(1000);yield return null;
        var garage=run.Garage;garage.UseInventory=false;garage.SelectedPart=R3PartKind.Armor;
        garage.SetTool(R41GarageTool.Mount);
        var socket=garage.AvailableMountPoints.First(p=>garage.Point(new Ray(
            p.Position+(p.Rotation*Vector3.forward)*2,-(p.Rotation*Vector3.forward)))&&garage.PreviewValid);
        Assert.That(garage.Preview.gameObject.activeSelf,Is.True);
        // HUD uses this when a modal or pause prevents build input, and the
        // garage uses it on focus loss. No purchase or selection is changed.
        garage.CancelPointerGesture();
        Assert.That(garage.HasTarget,Is.False,"Blocked input must not leave an old mounting target.");
        Assert.That(garage.Preview.gameObject.activeSelf,Is.False);
        Assert.That(garage.MountKind,Is.EqualTo(R3PartKind.Armor));
        var normal=socket.Rotation*Vector3.forward;
        Assert.That(garage.Point(new Ray(socket.Position+normal*2,-normal)),Is.True);
        Assert.That(garage.Preview.gameObject.activeSelf,Is.True);
    }

    [UnityTest] public IEnumerator LeavingBuildAreaHidesGhostWithoutLosingSelectedPart()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return VerifyLeavingBuildArea();
        yield return new ExitPlayMode();
    }
    static IEnumerator VerifyLeavingBuildArea()
    {
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light,0),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(1000);yield return null;
        var garage=run.Garage;garage.UseInventory=false;garage.SelectedPart=R3PartKind.Armor;
        garage.SetTool(R41GarageTool.Mount);
        var socket=garage.AvailableMountPoints.First(p=>garage.Point(new Ray(
            p.Position+(p.Rotation*Vector3.forward)*2,-(p.Rotation*Vector3.forward)))&&garage.PreviewValid);
        Assert.That(garage.Preview,Is.Not.Null);
        Assert.That(garage.Preview.gameObject.activeSelf,Is.True);
        garage.HandleGuiPointer(new Event{type=EventType.Repaint},Vector2.zero);
        Assert.That(garage.HasTarget,Is.False,"Leaving the build viewport must clear the old target.");
        Assert.That(garage.Preview.gameObject.activeSelf,Is.False,"The ghost must not remain over an old cell while the pointer is over UI.");
        Assert.That(garage.MountKind,Is.EqualTo(R3PartKind.Armor),"Moving to the UI must not discard the selected part type.");
        var normal=socket.Rotation*Vector3.forward;
        Assert.That(garage.Point(new Ray(socket.Position+normal*2,-normal)),Is.True);
        Assert.That(garage.Preview.gameObject.activeSelf,Is.True,"Returning to a socket must restore the ghost.");
    }

    [UnityTest] public IEnumerator ObliqueSideSocketWorksForWheel()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return VerifyWheel();
        yield return new ExitPlayMode();
    }
    static IEnumerator VerifyWheel()
    {
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light,0),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(1000);yield return null;
        var garage=run.Garage;garage.UseInventory=false;garage.SelectedPart=R3PartKind.Wheel;
        garage.SetTool(R41GarageTool.Mount);
        var socket=garage.AvailableMountPoints.Where(p=>Mathf.Abs(Vector3.Dot(p.Rotation*Vector3.forward,run.Player.transform.right))>.99f)
            .OrderByDescending(p=>p.Position.y).First(p=>{
                var normal=p.Rotation*Vector3.forward;
                var direction=Vector3Int.RoundToInt(run.Player.transform.InverseTransformDirection(normal));
                return garage.Point(new Ray(p.Position+normal*2,-normal))&&garage.PreviewValid&&
                    garage.Cell==p.Surface.GetComponentInParent<R3MountedPart>().Cell+direction;
            });
        var face=socket.Rotation*Vector3.forward;var tangent=socket.Rotation*Vector3.right;
        var expected=socket.Surface.GetComponentInParent<R3MountedPart>().Cell+
            Vector3Int.RoundToInt(run.Player.transform.InverseTransformDirection(face));
        var target=socket.Position+tangent*.088f;var offset=tangent*2+face*2;
        Assert.That(garage.Point(new Ray(target+offset,-offset.normalized)),Is.True,garage.PlacementIssue);
        Assert.That(garage.Cell,Is.EqualTo(expected));Assert.That(garage.PreviewValid,Is.True,garage.PlacementIssue);
    }
    [UnityTest] public IEnumerator ObliqueSocketWorksForBatteryAndWeapon()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return VerifyOtherParts();
        yield return new ExitPlayMode();
    }
    static IEnumerator VerifyOtherParts()
    {
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light,0),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(1000);yield return null;
        var garage=run.Garage;garage.UseInventory=false;
        foreach(var kind in new[]{R3PartKind.Battery,R3PartKind.Gun})
        {
            garage.SelectedPart=kind;garage.SelectedWeapon=RoboRougelike.R4.R4WeaponKind.Basic;
            garage.SetTool(R41GarageTool.Mount);
            var socket=garage.AvailableMountPoints.Where(p=>Vector3.Dot(p.Rotation*Vector3.forward,run.Player.transform.up)>.99f)
                .OrderByDescending(p=>p.Position.y).First(p=>garage.Point(new Ray(p.Position+Vector3.up*2,Vector3.down))&&
                    garage.Cell==p.Surface.GetComponentInParent<R3MountedPart>().Cell+Vector3Int.up&&garage.PreviewValid);
            var expected=socket.Surface.GetComponentInParent<R3MountedPart>().Cell+Vector3Int.up;
            Assert.That(garage.Point(new Ray(socket.Position+Vector3.up*2,Vector3.down)),Is.True);
            Assert.That(garage.Cell,Is.EqualTo(expected),"The straight ray must select the intended socket.");
            var target=socket.Position+run.Player.transform.right*.088f;
            var offset=run.Player.transform.right*2+run.Player.transform.up*2;
            var ray=new Ray(target+offset,-offset.normalized);
            foreach(var hit in Physics.RaycastAll(ray,100,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance).Take(4))
                Debug.Log("[GARAGE-TYPES] kind="+kind+" expected="+expected+" collider="+hit.collider.name+" normal="+hit.normal+" selected="+(hit.collider==socket.Surface));
            Assert.That(garage.Point(ray),Is.True,kind+": "+garage.PlacementIssue);
            Assert.That(garage.Cell,Is.EqualTo(expected));Assert.That(garage.PreviewValid,Is.True,kind+": "+garage.PlacementIssue);
        }
    }
    [UnityTest] public IEnumerator VisibleSocketRemainsUsableFromAnObliqueAngle()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return VerifyVisibleSocket();
        yield return new ExitPlayMode();
    }
    static IEnumerator VerifyVisibleSocket()
    {
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light,0),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var garage=run.Garage;garage.UseInventory=false;garage.SelectedPart=R3PartKind.Armor;
        garage.SetTool(R41GarageTool.Mount);
        var top=garage.AvailableMountPoints.Where(p=>Vector3.Dot(p.Rotation*Vector3.forward,run.Player.transform.up)>.99f)
            .OrderByDescending(p=>p.Position.y).First(p=>garage.Point(new Ray(p.Position+Vector3.up*2,Vector3.down))&&
                garage.Cell==p.Surface.GetComponentInParent<R3MountedPart>().Cell+Vector3Int.up&&garage.PreviewValid);
        var centerRay=new Ray(top.Position+Vector3.up*2,Vector3.down);
        Assert.That(garage.Point(centerRay),Is.True);Assert.That(garage.PreviewValid,Is.True,garage.PlacementIssue);
        var expected=garage.Cell;
        // The target remains inside the visible 18 cm socket, but the ray
        // grazes the thin trigger's side before it reaches its front face.
        var target=top.Position+run.Player.transform.right*.088f;
        var offset=run.Player.transform.right*2+run.Player.transform.up*2;
        var oblique=new Ray(target+offset,-offset.normalized);
        foreach(var hit in Physics.RaycastAll(oblique,100,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance).Take(5))
            Debug.Log("[GARAGE-POINTER] collider="+hit.collider.name+" normal="+hit.normal+" point="+hit.point+" distance="+hit.distance+" selected="+(hit.collider==top.Surface));
        Assert.That(garage.Point(oblique),Is.True,"A visible socket must remain selectable when viewed obliquely: "+garage.PlacementIssue);
        Assert.That(garage.Cell,Is.EqualTo(expected));Assert.That(garage.PreviewValid,Is.True,garage.PlacementIssue);
        var outside=top.Position+run.Player.transform.right*.092f;
        Assert.That(garage.Point(new Ray(outside+offset,-offset.normalized)),Is.False,
            "Grazing the trigger must not authorize a cursor outside the visible socket.");
        Assert.That(garage.PreviewValid,Is.False);
        Assert.That(garage.Point(oblique),Is.True);Assert.That(garage.ClickTarget(),Is.True,garage.Message);
        Assert.That(run.VehicleModel.At(expected),Is.Not.Null,"The actual edit must match the preview cell.");
        Assert.That(run.GarageRemove(expected),Is.True);yield return null;
        Assert.That(garage.Point(oblique),Is.True,"The same socket must still work after the vehicle is rebuilt.");
        Assert.That(garage.Cell,Is.EqualTo(expected));Assert.That(garage.PreviewValid,Is.True,garage.PlacementIssue);
    }
}
