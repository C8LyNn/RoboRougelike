using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

// Pure EditMode scene objects: no gameplay scene, OS window, cursor lock or input.
public sealed class PerWeaponReticleTests
{
    GameObject root,cameraObject,wall,ownerCollider;
    R3VehicleAssembly assembly;
    P12PipelineSafeTankController controller;
    Camera view;
    R3MountedPart left,right;
    readonly Vector3 target=new Vector3(0,0,50);

    [SetUp]public void Setup()
    {
        root=new GameObject("reticle test vehicle");assembly=root.AddComponent<R3VehicleAssembly>();
        controller=root.AddComponent<P12PipelineSafeTankController>();
        cameraObject=new GameObject("reticle test camera");view=cameraObject.AddComponent<Camera>();
        view.transform.position=new Vector3(0,0,-10);view.transform.rotation=Quaternion.identity;
        view.fieldOfView=60;view.aspect=16f/9;
        left=Gun("left",new Vector3(-2,0,0));right=Gun("right",new Vector3(2,0,0));
    }
    R3MountedPart Gun(string id,Vector3 position)
    {
        var item=new GameObject(id);item.transform.SetParent(root.transform);item.transform.position=position;
        var part=item.AddComponent<R3MountedPart>();part.Id=id;part.Kind=R3PartKind.Gun;part.Weapon=R4WeaponKind.Basic;
        part.Damageable=item.AddComponent<P12PipelineSafeDamageable>();part.Block=item.AddComponent<P12_PipelineSafePlayerBlock>();
        part.GunPivot=part.Muzzle=item.transform;assembly.Parts.Add(part);return part;
    }
    IList Collect(Vector3 point)
    {
        var type=typeof(R3VehicleAssembly).Assembly.GetType("RoboRougelike.WeaponIntegration.RRLWeaponReticles");
        Assert.That(type,Is.Not.Null,"The game needs individual, real-muzzle reticles for its guns");
        var marker=type.GetNestedType("Marker",BindingFlags.Public);Assert.That(marker,Is.Not.Null);
        var list=(IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(marker));
        var collect=type.GetMethod("Collect",BindingFlags.Public|BindingFlags.Static);Assert.That(collect,Is.Not.Null);
        collect.Invoke(null,new object[]{view,assembly,controller,point,list});return list;
    }
    static T Field<T>(object marker,string name)=>(T)marker.GetType().GetField(name).GetValue(marker);

    [Test]public void EachLiveGunHasItsOwnMarkerThatConvergesAsItsBarrelTurns()
    {
        left.Muzzle.forward=(new Vector3(6,0,50)).normalized;right.Muzzle.forward=(new Vector3(-6,0,50)).normalized;
        var before=Collect(target);Assert.That(before.Count,Is.EqualTo(2));
        Assert.That(Field<string>(before[0],"PartId"),Is.EqualTo("left"));Assert.That(Field<string>(before[1],"PartId"),Is.EqualTo("right"));
        var a=Field<Vector2>(before[0],"Viewport");var b=Field<Vector2>(before[1],"Viewport");
        Assert.That(a.x,Is.GreaterThan(.52f));Assert.That(b.x,Is.LessThan(.48f));
        left.Muzzle.forward=(target-left.Muzzle.position).normalized;right.Muzzle.forward=(target-right.Muzzle.position).normalized;
        var after=Collect(target);Assert.That(after.Count,Is.EqualTo(2));
        Assert.That(Field<Vector2>(after[0],"Viewport").x,Is.EqualTo(.5f).Within(.001f));
        Assert.That(Field<Vector2>(after[1],"Viewport").x,Is.EqualTo(.5f).Within(.001f));
    }
    [Test]public void DetachedOrDestroyedWeaponsDoNotLeaveStaleMarkers()
    {
        Assert.That(Collect(target).Count,Is.EqualTo(2));
        right.Block.MarkDetached();Assert.That(Collect(target).Count,Is.EqualTo(1));
        UnityEngine.Object.DestroyImmediate(left.Damageable);Assert.That(Collect(target).Count,Is.Zero);
    }
    [Test]public void ObstacleAndOutOfRangeAreReportedForTheSpecificWeapon()
    {
        right.Block.MarkDetached();left.Muzzle.position=Vector3.zero;left.Muzzle.forward=Vector3.forward;
        ownerCollider=GameObject.CreatePrimitive(PrimitiveType.Cube);ownerCollider.transform.SetParent(root.transform);
        ownerCollider.transform.position=new Vector3(0,0,2);Physics.SyncTransforms();
        Assert.That(Field<bool>(Collect(target)[0],"Obstructed"),Is.False,"Own mounted blocks must not obstruct their owner's shells");
        wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,0,10);Physics.SyncTransforms();
        Assert.That(Field<bool>(Collect(target)[0],"Obstructed"),Is.True,"The actual barrel path hits a wall before the intended point");
        UnityEngine.Object.DestroyImmediate(wall);wall=null;Physics.SyncTransforms();
        Assert.That(Field<bool>(Collect(new Vector3(0,0,250))[0],"OutOfRange"),Is.True,"The basic shell's existing lifetime and speed bound its reach");
    }
    [Test]public void AreaWeaponsAndBarrelsFacingAwayStillHaveDistinctIndicators()
    {
        left.Weapon=R4WeaponKind.EmpEmitter;right.Weapon=R4WeaponKind.RotarySaw;
        var area=Collect(target);Assert.That(area.Count,Is.EqualTo(2));
        Assert.That(Field<bool>(area[0],"AreaWeapon"),Is.True);Assert.That(Field<bool>(area[1],"AreaWeapon"),Is.True);
        Assert.That(Field<Vector2>(area[0],"Viewport").x,Is.EqualTo(.5f).Within(.001f),"EMP is radial and tracks the selected point rather than inventing a projectile");
        Assert.That(Field<Vector2>(area[1],"Viewport").x,Is.GreaterThan(.5f),"Saw indicator uses the blade's real position");
        left.Weapon=R4WeaponKind.Basic;left.Muzzle.forward=Vector3.back;
        var away=Collect(target);Assert.That(away.Count,Is.EqualTo(2));
        Assert.That(Field<bool>(away[0],"Offscreen"),Is.True);
        Assert.That(Vector2.Distance(Field<Vector2>(away[0],"Viewport"),new Vector2(.5f,.5f)),Is.GreaterThan(.5f));
    }
    [Test]public void RocketIndicatorAvoidsOwnProjectileFlickerButRecognizesSolidDebris()
    {
        right.Block.MarkDetached();left.Muzzle.position=Vector3.zero;left.Muzzle.forward=Vector3.forward;
        left.Weapon=R4WeaponKind.RocketLauncher;
        var launcher=left.gameObject.AddComponent<RoboRougelike.Mechanics.Rockets.RocketLauncher>();
        var binding=left.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLRocketBinding>();
        typeof(RoboRougelike.WeaponIntegration.RRLRocketBinding).GetProperty("Module").SetValue(binding,launcher);
        wall=GameObject.CreatePrimitive(PrimitiveType.Sphere);wall.transform.position=new Vector3(0,0,10);
        var shell=wall.AddComponent<P17PhysicalShell>();
        typeof(P17PhysicalShell).GetField("ownerRoot",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(shell,root.transform);
        Physics.SyncTransforms();
        Assert.That(Field<bool>(Collect(target)[0],"Obstructed"),Is.False,
            "An outgoing own shell should not make the direction indicator jump while firing");
        UnityEngine.Object.DestroyImmediate(wall);wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position=new Vector3(0,0,10);wall.AddComponent<R3Debris>();
        wall.GetComponent<Collider>().enabled=true;Physics.SyncTransforms();
        Assert.That(Field<bool>(Collect(target)[0],"Obstructed"),Is.True,
            "Rockets collide with detached solid debris even though ordinary shells ignore it");
    }
    [Test]public void ViewportMarkersMapCorrectlyToLetterboxedHud()
    {
        var type=typeof(R3VehicleAssembly).Assembly.GetType("RoboRougelike.WeaponIntegration.RRLWeaponReticles");
        Assert.That(type,Is.Not.Null);var map=type.GetMethod("ToHudPoint");Assert.That(map,Is.Not.Null);
        Vector2 Map(Vector2 viewport,Vector2 screen)=>(Vector2)map.Invoke(null,new object[]{viewport,new Rect(0,0,screen.x,screen.y),screen});
        Assert.That(Map(new Vector2(.5f,.5f),new Vector2(1280,720)).x,Is.EqualTo(640).Within(.01f));
        Assert.That(Map(new Vector2(.5f,.5f),new Vector2(1920,1080)).y,Is.EqualTo(360).Within(.01f));
        var wide=Map(new Vector2(.25f,.5f),new Vector2(2560,1080));
        Assert.That(wide.x,Is.EqualTo(213.3333f).Within(.02f));Assert.That(wide.y,Is.EqualTo(360).Within(.01f));
    }
    [TearDown]public void Cleanup()
    {
        if(wall!=null)UnityEngine.Object.DestroyImmediate(wall);
        if(root!=null)UnityEngine.Object.DestroyImmediate(root);
        if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);
    }
}
