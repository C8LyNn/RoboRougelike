using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CameraStartAndReplacementArtTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float until=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        Assert.That(R1RunController.Instance,Is.Not.Null);
    }
    [UnityTest] public IEnumerator TeleportSynchronizesVisualTransformWhilePhysicsIsPaused()
    {
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);
        var body=run.Player.GetComponent<Rigidbody>();Assert.That(body.interpolation,Is.EqualTo(RigidbodyInterpolation.Interpolate));
        Assert.That(Time.timeScale,Is.Zero);Assert.That(run.Player.transform.position.y,Is.GreaterThan(400));
        var position=new Vector3(3,.7f,-22);var rotation=Quaternion.Euler(0,90,0);
        run.Player.MoveTo(position,rotation);
        Assert.That(Vector3.Distance(body.position,position),Is.LessThan(.001f));
        Assert.That(Vector3.Distance(run.Player.transform.position,position),Is.LessThan(.001f),"Rendered pose must not stay in the workshop while physics is paused.");
        Assert.That(Quaternion.Angle(run.Player.transform.rotation,rotation),Is.LessThan(.01f));yield return null;
    }
    [UnityTest] public IEnumerator StartingBattleFramesTheActualVehicleWithoutWaitingForCameraToTravelFromGarage()
    {
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);
        Assert.That(run.Player.transform.position.y,Is.GreaterThan(400));
        Assert.That(run.StartNextEncounter(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        float until=Time.realtimeSinceStartup+20;while(run.Transitioning&&Time.realtimeSinceStartup<until)yield return null;
        Assert.That(run.Transitioning,Is.False);run.Encounter.SetCombatActive(false);yield return null;
        var camera=Object.FindFirstObjectByType<P12PipelineSafeCamera>();var body=run.Player.GetComponent<Rigidbody>();
        var distance=Vector3.Distance(camera.transform.position,body.position);
        Debug.Log($"CAM_START_QA camera={camera.transform.position} body={body.position} transform={run.Player.transform.position} distance={distance} requested={camera.RequestedZoomDistance} fit={camera.ReceiverRig.ReceiverFitDistance} bounds={camera.ReceiverRig.LastHullBounds}");
        Assert.That(distance,Is.LessThan(35f),"Camera must immediately frame the battle vehicle, not the old workshop.");
        Assert.That(Vector3.Distance(camera.ReceiverRig.LastHullBounds.center,body.position),Is.LessThan(15f));
    }
    [UnityTest] public IEnumerator SuppliedModelsReplaceTheOldVisibleMountingCubes()
    {
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        var parts=run.Player.GetComponent<R3VehicleAssembly>().Parts;
        var wheels=parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray();var guns=parts.Where(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.Basic).ToArray();
        Assert.That(wheels.Length,Is.GreaterThan(0));Assert.That(guns.Length,Is.GreaterThan(0));
        foreach(var part in wheels.Concat(guns))
        {
            Assert.That(part.GetComponent<Renderer>().enabled,Is.False,part.Kind+" must not retain a visible primitive cube.");
            Assert.That(part.GetComponent<Collider>().enabled,Is.True,"Original collider is retained for construction and damage.");
            Assert.That(part.GetComponentsInChildren<Renderer>().Any(r=>r!=part.GetComponent<Renderer>()&&r.enabled),Is.True,"Replacement model must be visible.");
        }
        var preview=(GameObject)typeof(R3GarageView).GetField("previewRoot",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(run.Garage);
        var outlines=preview.GetComponentsInChildren<Transform>().Where(t=>t.name=="W27 occupied cell").ToArray();
        Assert.That(outlines.Length,Is.GreaterThan(0),"Armor grid remains visible.");
        foreach(var part in wheels.Concat(guns))
            Assert.That(outlines.Any(t=>Vector3.Distance(t.localPosition,(Vector3)part.Cell*R41PartGeometry.Grid)<.001f),Is.False,
                "The garage must not draw an old cubic outline over replacement art.");
    }
    [UnityTest] public IEnumerator MovementModelsReplaceCubesWithoutRemovingPhysicalColliders()
    {
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        foreach(var kind in new[]{R3PartKind.Suspension,R3PartKind.Booster,R3PartKind.JumpDrive,R3PartKind.Blink,
            R3PartKind.Gyroscope,R3PartKind.MagneticGrapple,R3PartKind.HoverEngine,R3PartKind.OmniDrive,
            R3PartKind.CrawlerTrack,R3PartKind.IndependentSteering})
        {
            var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
            R41PartGeometry.Payload(block.transform,kind,R4WeaponKind.Basic,material,true,out _);
            bool attached=kind switch {
                R3PartKind.Suspension=>R4SuspensionArt.TryAttach(block.transform),
                R3PartKind.Booster=>R4BoosterArt.TryAttach(block.transform)!=null,
                R3PartKind.JumpDrive=>R4JumpArt.TryAttach(block.transform)!=null,
                R3PartKind.Blink=>R4BlinkArt.TryAttach(block.transform)!=null,
                R3PartKind.Gyroscope=>R4GyroArt.TryAttach(block.transform)!=null,
                R3PartKind.MagneticGrapple=>R4GrappleArt.TryAttach(block.transform),
                R3PartKind.HoverEngine=>R4HoverArt.TryAttach(block.transform)!=null,
                R3PartKind.OmniDrive=>R4OmniArt.TryAttach(block.transform,false)!=null,
                R3PartKind.CrawlerTrack=>R4TracksArt.TryAttach(block.transform,false)!=null,
                R3PartKind.IndependentSteering=>R4IndependentSteeringArt.TryAttach(block.transform),_=>false};
            Assert.That(attached,Is.True,kind+" must have its supplied model.");
            Assert.That(block.GetComponent<Renderer>().enabled,Is.False,kind+" must not render the old cube.");
            Assert.That(block.GetComponent<Collider>().enabled,Is.True,kind+" keeps its collision and mount cell.");
            Assert.That(block.GetComponentsInChildren<Renderer>().Any(r=>r!=block.GetComponent<Renderer>()&&r.enabled),Is.True,kind+" replacement remains visible.");
            Object.Destroy(block);
        }
        Object.Destroy(material);yield return null;
    }
    [UnityTest] public IEnumerator MissingModelsLeaveOldMountAndPayloadVisible()
    {
        var movement=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
        var weapons=Resources.Load<R4WeaponArtCatalog>("R4Art/WeaponCatalog");
        Assert.That(movement,Is.Not.Null);Assert.That(weapons,Is.Not.Null);
        var oldMovement=movement.Models;var oldWeapons=weapons.Models;
        movement.Models=(R4MovementArtEntry[])oldMovement.Clone();weapons.Models=(R4WeaponArtEntry[])oldWeapons.Clone();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        try
        {
            for(int i=0;i<movement.Models.Length;i++)if(movement.Models[i].Kind==R3PartKind.Wheel||movement.Models[i].Kind==R3PartKind.Booster)
                movement.Models[i]=new R4MovementArtEntry{Kind=movement.Models[i].Kind,Model=null};
            for(int i=0;i<weapons.Models.Length;i++)if(weapons.Models[i].Kind==R4WeaponKind.Basic)
                weapons.Models[i]=new R4WeaponArtEntry{Kind=R4WeaponKind.Basic,Model=null};
            foreach(var kind in new[]{R3PartKind.Wheel,R3PartKind.Gun,R3PartKind.Booster})
            {
                var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
                var payload=R41PartGeometry.Payload(block.transform,kind,R4WeaponKind.Basic,material,true,out var muzzle);
                bool attached=kind==R3PartKind.Wheel?R4WheelArt.TryAttach(block.transform,payload)!=null:
                    kind==R3PartKind.Gun?R4WeaponArt.TryAttach(block.transform,payload,muzzle,R4WeaponKind.Basic)!=null:
                    R4BoosterArt.TryAttach(block.transform)!=null;
                Assert.That(attached,Is.False,kind+" should use fallback if the model is unavailable.");
                Assert.That(block.GetComponent<Renderer>().enabled,Is.True,kind+" fallback cube must remain visible.");
                Assert.That(block.GetComponent<Collider>().enabled,Is.True);Object.Destroy(block);
            }
        }
        finally{movement.Models=oldMovement;weapons.Models=oldWeapons;Object.Destroy(material);}
        yield return null;
    }
    [UnityTest] public IEnumerator ReplacementModelStillShowsDamageAndFollowsDetachedPart()
    {
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        var wheel=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Wheel);
        var model=wheel.GetComponentsInChildren<Transform>(true).First(t=>t.name=="R4M_Wheel_Rotor");
        Assert.That(wheel.GetComponentsInChildren<R4ReplacementDamageVisual>(true).Length,Is.EqualTo(1),"The model must track its real damage owner.");
        var renderer=model.GetComponentsInChildren<Renderer>(true).First(r=>r.sharedMaterials.Length>0&&r.sharedMaterials[0]!=null&&r.sharedMaterials[0].HasProperty("_BaseColor"));
        var original=renderer.sharedMaterials[0].GetColor("_BaseColor");var hp=wheel.Damageable;
        Time.timeScale=1;hp.ApplyDamage(hp.MaxHp*.5f,wheel.transform.position,Vector3.up);
        Assert.That(hp.VisualFlashing,Is.True,"Damage flash must begin on the real part.");
        var flashProps=new MaterialPropertyBlock();renderer.GetPropertyBlock(flashProps,0);
        Assert.That(flashProps.GetColor("_BaseColor").r,Is.EqualTo(Color.Lerp(original,Color.red,.72f).r).Within(.02f));
        var deadline=Time.realtimeSinceStartup+4f;
        while(hp.VisualFlashing&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(hp.VisualFlashing,Is.False,"Damage flash must complete.");
        var props=new MaterialPropertyBlock();renderer.GetPropertyBlock(props,0);
        var expected=Color.Lerp(original,new Color(.16f,.09f,.06f,original.a),.4f);
        var actual=props.GetColor("_BaseColor");
        Assert.That(actual.r,Is.EqualTo(expected.r).Within(.02f),"The replacement model must still show damage.");
        Assert.That(actual.g,Is.EqualTo(expected.g).Within(.02f));Assert.That(actual.b,Is.EqualTo(expected.b).Within(.02f));
        hp.ApplyDamage(100000,wheel.transform.position,Vector3.up);yield return null;
        Assert.That(hp.IsDead,Is.True);Assert.That(model.IsChildOf(wheel.transform),Is.True,"Replacement stays with the detached wreck.");
        Assert.That(wheel.GetComponent<Collider>().enabled,Is.True);
    }
    [UnityTest] public IEnumerator GhostModelShowsInvalidPlacementTintWithoutAnOpaqueCube()
    {
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        run.Garage.UseInventory=false;run.Garage.SelectedPart=R3PartKind.Wheel;
        run.Garage.Select(run.VehicleModel.Parts.First().Cell);
        Assert.That(run.Garage.PreviewValid,Is.False);
        var marker=(GameObject)typeof(R3GarageView).GetField("marker",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(run.Garage);
        var palette=(R3VisualPalette)typeof(R3GarageView).GetField("palette",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(run.Garage);
        Assert.That(marker.GetComponent<Renderer>().enabled,Is.False);
        var rotor=marker.GetComponentsInChildren<Transform>(true).First(t=>t.name=="R4M_Wheel_Rotor");
        var renderers=rotor.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
        Assert.That(renderers.Length,Is.GreaterThan(0));
        Assert.That(renderers.All(r=>r.sharedMaterial==palette.Invalid),Is.True,"Invalid placement must tint the model, not a hidden cube.");
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
