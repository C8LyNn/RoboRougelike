using NUnit.Framework;
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
}
