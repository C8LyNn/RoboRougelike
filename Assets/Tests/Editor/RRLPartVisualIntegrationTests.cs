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
            Assert.IsFalse(visual.GetComponent<Collider>().enabled);
            Assert.IsNull(visual.GetComponent<Rigidbody>());
            Assert.IsTrue(PartVisualBinder.TryApply(block.transform, Vector3Int.left,
                R3PartKind.Wheel, R4WeaponKind.Basic, logical.transform, catalog));
            Assert.AreEqual(1, block.GetComponentsInChildren<PartVisualMarker>().Length);
        }
        finally { Object.DestroyImmediate(block); Object.DestroyImmediate(candidate); Object.DestroyImmediate(catalog); }
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
