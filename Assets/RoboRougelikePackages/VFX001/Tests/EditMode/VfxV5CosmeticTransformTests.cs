using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.VFX001.Tests
{
    public sealed class VfxV5CosmeticTransformTests
    {
        [Test]
        public void MechanicalJolt_RejectsPhysicsRoot()
        {
            var go = new GameObject("Root");
            var jolt = go.AddComponent<VfxMechanicalJolt>();
            Assert.IsFalse(jolt.Bind(go.transform));
            Assert.IsFalse(jolt.IsBound);
            Object.DestroyImmediate(go);
        }
        [Test]
        public void MechanicalJolt_RecoveryRestoresOriginalChildPose()
        {
            var root = new GameObject("Root");
            var child = new GameObject("Visual");
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = new Vector3(.7f, -.1f, .23f);
            child.transform.localRotation = Quaternion.Euler(2f, 5f, 7f);
            var restP = child.transform.localPosition;
            var restR = child.transform.localRotation;
            var jolt = root.AddComponent<VfxMechanicalJolt>();
            Assert.IsTrue(jolt.Bind(child.transform));
            jolt.React(1f, Vector3.left);
            jolt.Tick(.03f);
            Assert.Greater(Vector3.Distance(child.transform.localPosition, restP), .001f);
            jolt.SetCondition(0f, false);
            jolt.Tick(.5f);
            Assert.Less(Vector3.Distance(child.transform.localPosition, restP), .0001f);
            Assert.Less(Quaternion.Angle(child.transform.localRotation, restR), .01f);
            root.SetActive(false);
            Assert.Less(Vector3.Distance(child.transform.localPosition, restP), .0001f);
            Assert.IsNull(root.GetComponent<Rigidbody>());
            Object.DestroyImmediate(root);
        }
        [Test]
        public void CameraImpulse_RejectsRigAndFadesToRest()
        {
            var rig = new GameObject("Rig");
            rig.transform.position = new Vector3(5f, 2f, -2f);
            var camera = new GameObject("VisualCamera");
            camera.transform.SetParent(rig.transform, false);
            camera.transform.localPosition = new Vector3(.12f, .2f, 0f);
            var rest = camera.transform.localPosition;
            var impulse = rig.AddComponent<VfxCameraImpulseReceiver>();
            Assert.IsFalse(impulse.Bind(rig.transform));
            Assert.IsTrue(impulse.Bind(camera.transform));
            Assert.IsTrue(impulse.AddImpulse(rig.transform.position, 1f));
            impulse.Tick(.025f);
            Assert.AreEqual(1, impulse.ActiveCount);
            Assert.Less(Vector3.Distance(camera.transform.localPosition, rest), .1f);
            impulse.Tick(2f);
            Assert.AreEqual(0, impulse.ActiveCount);
            Assert.Less(Vector3.Distance(camera.transform.localPosition, rest), .0001f);
            Assert.AreEqual(new Vector3(5f, 2f, -2f), rig.transform.position);
            Object.DestroyImmediate(rig);
        }
        [Test]
        public void CameraImpulse_IgnoresRemoteAndUnrelatedEvents()
        {
            var rig = new GameObject("Rig");
            var child = new GameObject("Visual");
            child.transform.SetParent(rig.transform, false);
            var receiver = rig.AddComponent<VfxCameraImpulseReceiver>();
            receiver.Bind(child.transform);
            Assert.IsFalse(receiver.AddImpulse(new Vector3(1000f,0f,0f), 1f));
            Assert.IsFalse(receiver.Accept(new VfxEvent(VfxEventKind.LandingDust, Vector3.zero, Vector3.up)));
            Assert.AreEqual(0, receiver.ActiveCount);
            Object.DestroyImmediate(rig);
        }
        [Test]
        public void CameraImpulse_OverlappingEventsRemainWithinCap()
        {
            var rig = new GameObject("Rig");
            var child = new GameObject("Visual");
            child.transform.SetParent(rig.transform, false);
            var receiver = rig.AddComponent<VfxCameraImpulseReceiver>();
            receiver.Bind(child.transform);
            for (int i=0;i<20;i++) Assert.IsTrue(receiver.AddImpulse(Vector3.zero,1f));
            Assert.LessOrEqual(receiver.ActiveCount, 6);
            receiver.Tick(.02f);
            Assert.LessOrEqual(child.transform.localPosition.magnitude, .045f + .0001f);
            receiver.ResetImpulses();
            Assert.AreEqual(Vector3.zero, child.transform.localPosition);
            Object.DestroyImmediate(rig);
        }
    }
}
