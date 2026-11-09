using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R6;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

public sealed class EnemyWheelDriveInputTests
{
    [Test]
    public void CrowdedShotLineCannotHideALiveAlly()
    {
        var root=new GameObject("shooting enemy");root.transform.position=new Vector3(12000,0,12000);
        var shooter=root.AddComponent<P12PipelineSafeEnemyTurret>();
        var muzzle=new GameObject("muzzle");muzzle.transform.SetParent(root.transform,false);muzzle.transform.localPosition=Vector3.up;
        typeof(P12PipelineSafeEnemyTurret).GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(shooter,muzzle.transform);
        var allyRoot=new GameObject("ally");allyRoot.transform.position=root.transform.position+new Vector3(0,1,10);
        var ally=allyRoot.AddComponent<P12PipelineSafeEnemyTurret>();
        var core=allyRoot.AddComponent<P12PipelineSafeDamageable>();core.Configure(100,true);
        allyRoot.AddComponent<BoxCollider>();
        typeof(P12PipelineSafeEnemyTurret).GetField("core",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(ally,core);
        try
        {
            for(int i=0;i<300;i++)
            {
                var child=new GameObject("own collider "+i);child.transform.SetParent(root.transform,false);
                child.transform.localPosition=new Vector3(0,1,1+i*.01f);child.AddComponent<BoxCollider>().size=Vector3.one*.005f;
            }
            Physics.SyncTransforms();var hits=new RaycastHit[256];
            int count=Physics.RaycastNonAlloc(muzzle.transform.position,Vector3.forward,hits,40,~0,QueryTriggerInteraction.Ignore);
            Assert.That(count,Is.EqualTo(hits.Length));
            Assert.That(System.Array.Exists(hits,h=>h.collider!=null&&h.collider.transform.IsChildOf(allyRoot.transform)),Is.False,
                "The crowded first query must omit the real ally.");
            var check=typeof(P12PipelineSafeEnemyTurret).GetMethod("FriendlyInLine",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That((bool)check.Invoke(shooter,new object[]{muzzle.transform.position+Vector3.forward*40}),Is.True,
                "A living ally must still prevent a shot through it.");
            var variantCheck=typeof(P12PipelineSafeEnemyTurret).GetMethod("FriendlyBetween",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That(variantCheck,Is.Not.Null);
            Assert.That((bool)variantCheck.Invoke(shooter,new object[]{muzzle.transform.position,muzzle.transform.position+Vector3.forward*40}),Is.True,
                "Additional weapons must use the same complete ally check.");
            allyRoot.SetActive(false);Physics.SyncTransforms();
            Assert.That((bool)check.Invoke(shooter,new object[]{muzzle.transform.position+Vector3.forward*40}),Is.False);
        }
        finally{Object.DestroyImmediate(allyRoot);Object.DestroyImmediate(root);}
    }

    [Test]
    public void CrowdedOwnCollidersDoNotFreezeEnemyButRealWallStillBlocks()
    {
        var root=new GameObject("enemy movement fixture");root.transform.position=new Vector3(10000,0,10000);
        var body=root.AddComponent<Rigidbody>();body.isKinematic=true;
        var enemy=root.AddComponent<P12PipelineSafeEnemyTurret>();
        typeof(P12PipelineSafeEnemyTurret).GetField("movementBody",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,body);
        GameObject wall=null;
        try
        {
            for(int i=0;i<300;i++)
            {
                var child=new GameObject("own collider "+i);child.transform.SetParent(root.transform,false);
                child.transform.localPosition=new Vector3(0,.75f,1.1f);child.AddComponent<BoxCollider>().size=Vector3.one*.01f;
            }
            Physics.SyncTransforms();
            var origin=body.position+Vector3.up*.75f;
            Assert.That(Physics.BoxCastAll(origin,new Vector3(.88f,.67f,.88f),Vector3.forward,Quaternion.identity,2,~0,QueryTriggerInteraction.Ignore).Length,Is.GreaterThan(256));
            var probe=typeof(P12PipelineSafeEnemyTurret).GetMethod("CanMove",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That((bool)probe.Invoke(enemy,new object[]{Vector3.forward,2f,false}),Is.True,
                "Own geometry alone must not be treated as an impassable wall.");
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=root.transform.position+new Vector3(0,.75f,1.5f);
            Physics.SyncTransforms();
            Assert.That((bool)probe.Invoke(enemy,new object[]{Vector3.forward,2f,false}),Is.False,
                "A real wall must not be missed behind the crowded results.");
        }
        finally{if(wall!=null)Object.DestroyImmediate(wall);Object.DestroyImmediate(root);}
    }

    [Test]
    public void WorldWishUsesForwardThrottleAndSteeringInsteadOfSidewaysForce()
    {
        var forward=R6VehicleDrive.WorldSteering(Vector3.forward,Vector3.forward,Vector3.up);
        Assert.That(forward.x,Is.EqualTo(1).Within(.001));Assert.That(forward.y,Is.Zero.Within(.001));
        var right=R6VehicleDrive.WorldSteering(Vector3.forward,Vector3.right,Vector3.up);
        Assert.That(right.x,Is.Zero.Within(.001));Assert.That(right.y,Is.GreaterThan(.99f));
        var back=R6VehicleDrive.WorldSteering(Vector3.forward,Vector3.back,Vector3.up);
        Assert.That(back.x,Is.EqualTo(-1).Within(.001));Assert.That(back.y,Is.Zero.Within(.001));
        Assert.That(R6VehicleDrive.WorldSteering(Vector3.forward,Vector3.zero,Vector3.up),Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void SteeringMaintainsDesiredTurnWhenVehicleBeginsReversing()
    {
        var target=new Vector3(.5f,0,-1).normalized;
        var stopped=R6VehicleDrive.WorldSteering(Vector3.forward,target,Vector3.up,0);
        var reversing=R6VehicleDrive.WorldSteering(Vector3.forward,target,Vector3.up,-1);
        Assert.That(stopped.x,Is.LessThan(0));Assert.That(stopped.y,Is.LessThan(0));
        Assert.That(reversing.y,Is.GreaterThan(0),"R6 inverts steering while reversing.");
    }

    [Test]
    public void EnemyWheelVisualDoesNotOwnOrRotateThePhysicalCollider()
    {
        var root=new GameObject("enemy root");root.AddComponent<Rigidbody>();
        try
        {
            var method=typeof(P12PipelineSafeEnemyTurret).GetMethod("MakeProfileBlock",BindingFlags.NonPublic|BindingFlags.Static);
            Assert.That(method,Is.Not.Null);
            var part=(P12PipelineSafeDamageable)method.Invoke(null,new object[]{root.transform,Vector3Int.zero,null,true});
            var visual=part.GetComponentInChildren<P12EnemyWheelVisual>();
            Assert.That(visual,Is.Not.Null);Assert.That(visual.GetComponent<Collider>(),Is.Null);
            Assert.That(part.GetComponent<Collider>(),Is.Not.Null);
            Assert.That(part.GetComponent<Renderer>().enabled,Is.False);
            var art=part.GetComponent<RoboRougelike.R4_1.R4EnemyWheelArt>();
            Assert.That(art,Is.Not.Null,"The supplied model replaces the old tread rendering.");
            Assert.That(visual.GetComponent<Renderer>().enabled,Is.False,"Fallback is retained but not drawn twice.");
            Assert.That(art.GetComponentsInChildren<Renderer>().Length,Is.GreaterThan(0));
        }
        finally{Object.DestroyImmediate(root);}
    }

    [Test]
    public void EnemyWheelsHaveOneInwardSocketAndDoNotConnectToAdjacentWheels()
    {
        var root=new GameObject("enemy root");root.AddComponent<Rigidbody>();
        try
        {
            var make=typeof(P12PipelineSafeEnemyTurret).GetMethod("MakeProfileBlock",BindingFlags.NonPublic|BindingFlags.Static);
            var left=(P12PipelineSafeDamageable)make.Invoke(null,new object[]{root.transform,new Vector3Int(-3,0,0),null,true});
            var right=(P12PipelineSafeDamageable)make.Invoke(null,new object[]{root.transform,new Vector3Int(3,0,0),null,true});
            var next=(P12PipelineSafeDamageable)make.Invoke(null,new object[]{root.transform,new Vector3Int(3,0,1),null,true});
            Assert.That(left.GetComponent<R3BlockMountPoints>()?.WorldMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.right)));
            Assert.That(right.GetComponent<R3BlockMountPoints>()?.WorldMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.left)));
            var wheel=right.gameObject.AddComponent<P12_PipelineSafePlayerBlock>();
            var neighbouringWheel=next.gameObject.AddComponent<P12_PipelineSafePlayerBlock>();
            var armor=new GameObject("armor");armor.transform.SetParent(root.transform);
            var armorBlock=armor.AddComponent<P12_PipelineSafePlayerBlock>();
            Assert.That(R3MountingGrid.Compatible(wheel,armorBlock,Vector3Int.left),Is.True);
            Assert.That(R3MountingGrid.Compatible(wheel,neighbouringWheel,Vector3Int.forward),Is.False);
        }
        finally{Object.DestroyImmediate(root);}
    }

    [Test]
    public void EnemyDevicesChooseChassisEvenWhenAParentWheelIsCloser()
    {
        var root=new GameObject("enemy");root.AddComponent<P12PipelineSafeEnemyTurret>();
        try
        {
            var chassis=new GameObject("Base");chassis.transform.SetParent(root.transform);
            var wheel=GameObject.CreatePrimitive(PrimitiveType.Cube);wheel.name="Koło przeciwnika";
            wheel.transform.SetParent(chassis.transform);wheel.AddComponent<P12PipelineSafeDamageable>().Configure(100,true);
            var armor=GameObject.CreatePrimitive(PrimitiveType.Cube);armor.name="P12_EnemyBlock";
            armor.transform.SetParent(chassis.transform);armor.transform.position=Vector3.right*2;
            armor.AddComponent<P12PipelineSafeDamageable>().Configure(100,true);
            var gun=GameObject.CreatePrimitive(PrimitiveType.Cube);gun.name="gun";gun.transform.SetParent(root.transform);gun.transform.position=Vector3.up;gun.transform.localScale=Vector3.one*.52f;
            Physics.SyncTransforms();
            RoboRougelike.WeaponIntegration.RRLEnemyDeviceMount.Attach(gun,root.transform,new R3PartState(Vector3Int.zero,R3PartKind.Gun));
            Assert.That(gun.transform.parent,Is.EqualTo(armor.transform),"A wheel has only an inward axle socket, not a top device socket.");
            Assert.That(gun.GetComponent<R3BlockMountPoints>()?.WorldMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.down)),
                "An enemy gun connects through its base, just like a newly mounted player gun.");
        }
        finally{Object.DestroyImmediate(root);}
    }

    [Test]
    public void EnemyDevicesCannotBeEquippedWhenNoValidChassisSocketExists()
    {
        var root=new GameObject("enemy");
        try
        {
            var enemy=root.AddComponent<P12PipelineSafeEnemyTurret>();
            var chassis=new GameObject("Base");chassis.transform.SetParent(root.transform);
            var core=GameObject.CreatePrimitive(PrimitiveType.Cube);core.name="Core";core.transform.SetParent(chassis.transform);
            var hp=core.AddComponent<P12PipelineSafeDamageable>();hp.Configure(100,true);
            typeof(P12PipelineSafeEnemyTurret).GetField("core",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,hp);
            typeof(P12PipelineSafeEnemyTurret).GetField("weaponBlock",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,hp);
            var wheel=GameObject.CreatePrimitive(PrimitiveType.Cube);wheel.name="Koło przeciwnika";wheel.transform.SetParent(chassis.transform);
            wheel.AddComponent<P12PipelineSafeDamageable>().Configure(100,true);
            Assert.That(enemy.EquipBattery(),Is.False,"A battery cannot float independently of its support.");
            Assert.That(enemy.EquipDirectionalShield(),Is.False,"A shield cannot float independently of its support.");
            Assert.That(enemy.EquipPointDefense(),Is.False,"Point defense cannot float independently of its support.");
        }
        finally{Object.DestroyImmediate(root);}
    }

    [Test]
    public void EnemyDeviceDoesNotMountInsideBlockAboveNearestSupport()
    {
        var root=new GameObject("enemy");root.AddComponent<P12PipelineSafeEnemyTurret>();
        try
        {
            var chassis=new GameObject("Base");chassis.transform.SetParent(root.transform);
            var lower=GameObject.CreatePrimitive(PrimitiveType.Cube);lower.transform.SetParent(chassis.transform);
            lower.transform.localScale=Vector3.one*.52f;lower.AddComponent<P12PipelineSafeDamageable>().Configure(100,true);
            var upper=GameObject.CreatePrimitive(PrimitiveType.Cube);upper.transform.SetParent(chassis.transform);
            upper.transform.localPosition=Vector3.up*.55f;upper.transform.localScale=Vector3.one*.52f;
            upper.AddComponent<P12PipelineSafeDamageable>().Configure(100,true);
            var gun=GameObject.CreatePrimitive(PrimitiveType.Cube);gun.name="gun";gun.transform.SetParent(root.transform);gun.transform.position=lower.transform.position;gun.transform.localScale=Vector3.one*.52f;
            Physics.SyncTransforms();
            Assert.That(RoboRougelike.WeaponIntegration.RRLEnemyDeviceMount.Attach(gun,root.transform,new R3PartState(Vector3Int.zero,R3PartKind.Gun)),Is.True);
            Assert.That(gun.transform.parent,Is.EqualTo(upper.transform),"The nearest lower support has an occupied top cell.");
            Assert.That(Vector3.Distance(gun.transform.position,upper.transform.position),Is.GreaterThan(.5f));
        }
        finally{Object.DestroyImmediate(root);}
    }
}
