using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEngine;

public sealed class R3GaragePointerGestureTests
{
    [Test]
    public void RightClickRemovesOnlyWhenReleasedInsideTheViewport()
    {
        var gesture=new R3GaragePointerGesture();
        Assert.That(gesture.Release(true),Is.False,"A release without a press must do nothing.");
        gesture.Press();Assert.That(gesture.Release(true),Is.True);
        gesture.Press();Assert.That(gesture.Release(false),Is.False,"A release over the UI must not remove a part.");
    }

    [Test]
    public void OrbitAndCancellationCannotBecomeAnAccidentalRemoval()
    {
        var gesture=new R3GaragePointerGesture();
        gesture.Press();Assert.That(gesture.Drag(new Vector2(2,0)),Is.False);
        Assert.That(gesture.Drag(new Vector2(2,0)),Is.True,"A four-pixel drag starts orbiting.");
        Assert.That(gesture.Release(true),Is.False);
        gesture.Press();gesture.Cancel();
        Assert.That(gesture.Release(true),Is.False,"Closing a panel or losing focus cancels the pending click.");
    }

    [TestCase(KeyCode.Q,0,-1,0)]
    [TestCase(KeyCode.E,0,1,0)]
    [TestCase(KeyCode.R,1,0,0)]
    [TestCase(KeyCode.F,-1,0,0)]
    [TestCase(KeyCode.Z,0,0,-1)]
    [TestCase(KeyCode.X,0,0,1)]
    public void RotationShortcutsSpecifyQuarterTurnsOnTheCorrectAxis(KeyCode key,int x,int y,int z)
    {
        var profile=new R1ControlProfile(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"RRL-rotation-default-"+System.Guid.NewGuid()+".json"));
        Assert.That(R3GarageRotationInput.TryStepForProfile(new Event{type=EventType.KeyDown,keyCode=key},profile,out var step),Is.True);
        Assert.That(step,Is.EqualTo(new Vector3Int(x,y,z)));
        Assert.That(R3GarageRotationInput.TryStepForProfile(new Event{type=EventType.KeyDown,keyCode=KeyCode.Space},profile,out _),Is.False);
    }
}
