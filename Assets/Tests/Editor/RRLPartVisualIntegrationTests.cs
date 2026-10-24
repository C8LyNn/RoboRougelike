using NUnit.Framework;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.VisualIntegration;

public class RRLPartVisualIntegrationTests
{
    [Test]
    public void MissingCatalogKeepsLegacyVisuals()
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.transform.SetParent(block.transform, false);
        try
        {
            Assert.IsFalse(PartVisualBinder.TryApply(block.transform, Vector3Int.left,
                R3PartKind.Wheel, R4WeaponKind.Basic, wheel.transform,
                ScriptableObject.CreateInstance<PartVisualCatalog>()));
            Assert.IsTrue(wheel.GetComponent<Renderer>().enabled);
            Assert.IsNotNull(wheel.GetComponent<Collider>());
        }
        finally { Object.DestroyImmediate(block); }
    }

    [Test]
    public void ResolvesWheelSideWithoutSwappingLeftAndRight()
    {
        var catalog = ScriptableObject.CreateInstance<PartVisualCatalog>();
        var left = new GameObject("left");
        var right = new GameObject("right");
        try
        {
            catalog.entries.Add(new PartVisualCatalog.Entry {
                kind = R3PartKind.Wheel, side = PartVisualSide.Left, prefab = left });
            catalog.entries.Add(new PartVisualCatalog.Entry {
                kind = R3PartKind.Wheel, side = PartVisualSide.Right, prefab = right });
            Assert.AreSame(left, catalog.Resolve(R3PartKind.Wheel, R4WeaponKind.Basic, Vector3Int.left).prefab);
            Assert.AreSame(right, catalog.Resolve(R3PartKind.Wheel, R4WeaponKind.Basic, Vector3Int.right).prefab);
        }
        finally { Object.DestroyImmediate(left); Object.DestroyImmediate(right); Object.DestroyImmediate(catalog); }
    }

    [Test]
    public void OverrideKeepsExistingPhysicsAndHidesOnlyLegacyMesh()
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var logical = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        logical.transform.SetParent(block.transform, false);
        logical.transform.localScale = new Vector3(1.5f, .35f, 1.5f);
        var candidate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var catalog = ScriptableObject.CreateInstance<PartVisualCatalog>();
        catalog.entries.Add(new PartVisualCatalog.Entry {
            kind = R3PartKind.Wheel, side = PartVisualSide.Any, prefab = candidate });
        try
        {
            int oldColliders = block.GetComponentsInChildren<Collider>().Length;
            Assert.IsTrue(PartVisualBinder.TryApply(block.transform, Vector3Int.left,
                R3PartKind.Wheel, R4WeaponKind.Basic, logical.transform, catalog));
            Assert.IsFalse(logical.GetComponent<Renderer>().enabled);
            Assert.IsTrue(block.GetComponent<Renderer>().enabled);
            Assert.AreEqual(oldColliders + 1, block.GetComponentsInChildren<Collider>(true).Length);
            var visual = block.GetComponentInChildren<PartVisualMarker>();
            Assert.IsNotNull(visual);
            Assert.IsFalse(visual.GetComponentInChildren<Collider>(true).enabled);
            Assert.IsNull(visual.GetComponent<Rigidbody>());
            Assert.IsTrue(PartVisualBinder.TryApply(block.transform, Vector3Int.left,
                R3PartKind.Wheel, R4WeaponKind.Basic, logical.transform, catalog));
            Assert.AreEqual(1, block.GetComponentsInChildren<PartVisualMarker>().Length);
        }
        finally { Object.DestroyImmediate(block); Object.DestroyImmediate(candidate); Object.DestroyImmediate(catalog); }
    }

    [Test]
    public void WheelSpinHasCorrectTravelSignAndGuardedZeroRadius()
    {
        Assert.AreEqual(180f / Mathf.PI, PartVisualWheelSpin.SpinDegrees(1f, 1f, 1f), .0001f);
        Assert.AreEqual(-180f / Mathf.PI, PartVisualWheelSpin.SpinDegrees(-1f, 1f, 1f), .0001f);
        Assert.AreEqual(0f, PartVisualWheelSpin.SpinDegrees(10f, 0f, 1f));
    }

    [Test]
    public void PoseFollowerDoesNotOverwriteNestedWheelRotation()
    {
        var block = new GameObject("block");
        var logical = new GameObject("logical");
        logical.transform.SetParent(block.transform, false);
        var source = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        var catalog = ScriptableObject.CreateInstance<PartVisualCatalog>();
        catalog.entries.Add(new PartVisualCatalog.Entry
        {
            kind = R3PartKind.Wheel, prefab = source,
            followLogicalPivot = true
        });
        try
        {
            Assert.IsTrue(PartVisualBinder.TryApply(block.transform, Vector3Int.right,
                R3PartKind.Wheel, R4WeaponKind.Basic, logical.transform, catalog));
            var marker = block.GetComponentInChildren<PartVisualMarker>();
            var child = marker.transform.Find("Model");
            child.localRotation = Quaternion.Euler(31f, 0f, 0f);
            marker.GetComponent<PartVisualFollowPose>().SyncNow();
            Assert.That(Quaternion.Angle(child.localRotation, Quaternion.Euler(31f, 0f, 0f)),
                Is.LessThan(.01f));
            Assert.IsNotNull(marker.GetComponent<PartVisualWheelSpin>());
        }
        finally
        {
            Object.DestroyImmediate(block);
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(catalog);
        }
    }

    [Test]
    public void HoverLiftNormalizationNeverExceedsBounds()
    {
        Assert.AreEqual(0f, PartVisualHoverRotor.NormalizedLift(0f, 160f));
        Assert.AreEqual(0f, PartVisualHoverRotor.NormalizedLift(100f, 0f));
        Assert.AreEqual(1f, PartVisualHoverRotor.NormalizedLift(999999f, 160f));
        Assert.That(PartVisualHoverRotor.NormalizedLift(160f * 9.81f / 2f, 160f),
            Is.EqualTo(.5f).Within(.0001f));
    }

    [Test]
    public void W01SpinTargetsNamedWheelPivotNotImportedRoot()
    {
        var root = new GameObject("ImportedW01Root");
        var child = new GameObject("WheelRotationPivot");
        child.transform.SetParent(root.transform, false);
        var owner = new GameObject("Driver");
        try
        {
            var spin = owner.AddComponent<PartVisualWheelSpin>();
            spin.Configure(root.transform, null, 0.39f);
            Assert.AreSame(child.transform, spin.RotationTarget);
            Assert.AreNotSame(root.transform, spin.RotationTarget);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void RejectsPrefabWithRigidbodyWithoutDisablingFallback()
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fallback.transform.SetParent(block.transform, false);
        var candidate = new GameObject("unsafe");
        candidate.AddComponent<Rigidbody>();
        var catalog = ScriptableObject.CreateInstance<PartVisualCatalog>();
        catalog.entries.Add(new PartVisualCatalog.Entry {
            kind = R3PartKind.Wheel, prefab = candidate });
        try
        {
            Assert.IsFalse(PartVisualBinder.TryApply(block.transform, Vector3Int.left,
                R3PartKind.Wheel, R4WeaponKind.Basic, fallback.transform, catalog));
            Assert.IsTrue(fallback.GetComponent<Renderer>().enabled);
        }
        finally { Object.DestroyImmediate(block); Object.DestroyImmediate(candidate); Object.DestroyImmediate(catalog); }
    }
}
