using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.MapEditor;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;

public sealed class LD02ArenaNativeTests
{
    GameObject paletteObject, root;
    R3VisualPalette palette;
    Material material;
    MapDraft draft;
    MapWorld world;

    [UnitySetUp] public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        Time.timeScale = 1;
        draft = new MapDraft();
        Assert.That(draft.TryLoad(File.ReadAllText("Docs/CX08/Fixtures/LD02_ARENA.json"), out var error), Is.True, error);
        Assert.That(draft.Spawn, Is.Not.Null);
        Assert.That(draft.Finish, Is.Not.Null);
        paletteObject = new GameObject("LD02 palette");
        palette = paletteObject.AddComponent<R3VisualPalette>();
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for (int i = 0; i < palette.Parts.Length; i++) palette.Parts[i] = material;
        for (int i = 0; i < palette.Weapons.Length; i++) palette.Weapons[i] = material;
        root = new GameObject("LD02 geometry");
        world = root.AddComponent<MapWorld>();
        world.Build(draft, palette);
        Physics.SyncTransforms();
    }

    [UnityTest] public IEnumerator PerimeterHasEightActualPhysicalColliders()
    {
        var origins = new[] {
            new Vector3(20, 1.5f, 4), new Vector3(20, 1.5f, 32),
            new Vector3(4, 1.5f, 16), new Vector3(40, 1.5f, 16),
            new Vector3(3, 1.5f, 3), new Vector3(41, 1.5f, 3),
            new Vector3(3, 1.5f, 33), new Vector3(41, 1.5f, 33)
        };
        var directions = new[] {
            Vector3.back, Vector3.forward, Vector3.left, Vector3.right,
            new Vector3(-1, 0, -1), new Vector3(1, 0, -1),
            new Vector3(-1, 0, 1), new Vector3(1, 0, 1)
        };
        for (int i = 0; i < origins.Length; i++)
        {
            var expected = i < 4 ? MapItemKind.Wall : MapItemKind.Platform;
            var hits = Physics.RaycastAll(origins[i], directions[i].normalized, 7f, ~0, QueryTriggerInteraction.Ignore);
            Assert.That(hits.Any(h => h.collider.GetComponentInParent<MapItemReference>()?.Item.kind == expected),
                Is.True, $"Perimeter ray {i} must hit actual {expected} collider");
        }
        yield return null;
    }

    [UnityTest] public IEnumerator LightCrossesUsingRealWheelForces() => Cross(R3VehicleKind.Light);
    [UnityTest] public IEnumerator HeavyCrossesUsingRealWheelForces() => Cross(R3VehicleKind.Heavy);

    IEnumerator Cross(R3VehicleKind kind)
    {
        var model = new R3VehicleModel(R3VehicleCatalog.Create(kind));
        var vehicle = R3VehicleAssembly.Build(model, new R1RunStats(), palette,
            MapWorld.Position(draft.Spawn) + Vector3.up * .7f, Quaternion.Euler(0, 90, 0));
        try
        {
            vehicle.Controller.enabled = false;
            var body = vehicle.GetComponent<Rigidbody>();
            var drive = vehicle.GetComponent<R6VehicleDrive>();
            Assert.That(drive, Is.Not.Null);
            int grounded = 0;
            float maximumX = body.position.x;
            for (int i = 0; i < 650; i++)
            {
                float previous = Time.fixedTime;
                while (Time.fixedTime <= previous) yield return null;
                drive.Step(1, 0, 10, 4, 90);
                if (drive.GroundedWheels > 0) grounded++;
                maximumX = Mathf.Max(maximumX, body.position.x);
                if (i % 100 == 0) Debug.Log($"[LD02 {kind}] step={i} pos={body.position} grounded={drive.GroundedWheels}");
                if (body.position.x >= MapWorld.Position(draft.Finish).x - 2) break;
            }
            Debug.Log($"[LD02 {kind}] end={body.position} maximumX={maximumX:0.00} groundedFrames={grounded}");
            Assert.That(grounded, Is.GreaterThan(0), "Wheels must physically contact the map");
            Assert.That(maximumX, Is.GreaterThanOrEqualTo(MapWorld.Position(draft.Finish).x - 2), "Drive to the finish without teleporting");
            Assert.That(Mathf.Abs(body.position.z - MapWorld.Position(draft.Finish).z), Is.LessThan(4), "Finish corridor");
        }
        finally { Object.Destroy(vehicle.gameObject); }
    }

    [UnityTearDown] public IEnumerator TearDown()
    {
        if (root != null) Object.Destroy(root);
        if (paletteObject != null) Object.Destroy(paletteObject);
        if (material != null) Object.Destroy(material);
        if (Application.isPlaying) yield return new ExitPlayMode();
    }
}
