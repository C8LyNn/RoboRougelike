using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R6;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

public sealed class EnemyWheelDriveInputTests
{
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
}
