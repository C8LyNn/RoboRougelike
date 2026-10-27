// B01-24 PASS13 — original MapWorld/MapDraft/PhysX, not a JS geometry mirror.
// Original level source: B01-25 Research/B01-25/P11/LD02.json @ bdd6c69fc9fd519b863e3a9cb217b3cd39404194.
// Original gameplay source: Codex #310 @ 231005c6d0351945a8ee6684f3856af118f0e94f.
// This isolated physical fixture validates map cover geometry ONLY.
// It deliberately does NOT claim editor kill-objectives or enemy gameplay LOS policy.
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.MapEditor;
using RoboRougelike.R3;

public sealed class B0124LD02PhysicalLosPlayModeTests
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
    }

    [UnityTest]
    public IEnumerator OriginalLD02Pillar_BlockedClearRecovered_UsesActualMapWorldColliders()
    {
        var sceneRoot = new GameObject("B0124_LD02_OriginalMapWorld");
        var paletteRoot = new GameObject("B0124_LD02_Palette");
        try
        {
            var world = sceneRoot.AddComponent<MapWorld>();
            var palette = paletteRoot.AddComponent<R3VisualPalette>();
            // Only color is irrelevant; MapWorld's real default Unity primitives
            // retain real Collider components even if no palette material is set.
            var draft = new MapDraft();
            Assert.That(draft.SetGlobalFloor(true), Is.True);

            // Reproduce EXACT original LD02 map items (all are rotation 0, scale 1)
            // via the real MapDraft.Place API, not a handwritten geometry mock.
            Add(draft, MapItemKind.Spawn, 0, 5);
            Add(draft, MapItemKind.Finish, 11, 5);
            Add(draft, MapItemKind.Pillar, 3, 2);
            Add(draft, MapItemKind.Pillar, 3, 7);
            Add(draft, MapItemKind.Pillar, 8, 2);
            Add(draft, MapItemKind.Pillar, 8, 7);
            Add(draft, MapItemKind.LowBarrier, 5, 4);
            Add(draft, MapItemKind.LowBarrier, 5, 6);
            Add(draft, MapItemKind.Wall, 5, 0);
            Add(draft, MapItemKind.Wall, 5, 9);
            Assert.That(draft.Items.Count, Is.EqualTo(10));
            Assert.That(draft.TestError(), Is.Null);
            world.Build(draft, palette);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();

            // MapWorld.Grid=4m. LD02 Pillar at cell (3,2) has original
            // world origin (12,0,8), primitive Collider center y=2,
            // size (1.5,2,1.5). This ray passes through it at y=1.5.
            var from = new Vector3(12f, 1.5f, 0f);
            var to = new Vector3(12f, 1.5f, 16f);
            var direction = (to - from).normalized;
            var span = Vector3.Distance(from, to);

            Assert.That(Physics.Raycast(from, direction, out var blocked, span, ~0, QueryTriggerInteraction.Ignore),
                Is.True, "Actual MapWorld must build a physical obstacle at cell (3,2).");
            var hitItem = blocked.collider.GetComponentInParent<MapItemReference>();
            Assert.That(hitItem, Is.Not.Null);
            Assert.That(hitItem.Item.kind, Is.EqualTo(MapItemKind.Pillar));
            Assert.That(hitItem.Item.x, Is.EqualTo(3));
            Assert.That(hitItem.Item.z, Is.EqualTo(2));

            // Control: an adjacent parallel 4m-offset lane is clear without
            // touching the live MapWorld or changing any enemy AI parameters.
            var clearFrom = new Vector3(16f, 1.5f, 0f);
            var clearTo = new Vector3(16f, 1.5f, 16f);
            Assert.That(Physics.Raycast(clearFrom, (clearTo-clearFrom).normalized,
                Vector3.Distance(clearFrom, clearTo), ~0, QueryTriggerInteraction.Ignore),
                Is.False, "Adjacent source-defined lane should not intersect this cover.");

            hitItem.gameObject.SetActive(false);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(Physics.Raycast(from, direction, span, ~0, QueryTriggerInteraction.Ignore),
                Is.False, "Removing only the identified original pillar must restore clear ray geometry.");
        }
        finally
        {
            Object.Destroy(sceneRoot);
            Object.Destroy(paletteRoot);
        }
    }

    private static void Add(MapDraft draft, MapItemKind kind, int x, int z)
    {
        Assert.That(draft.Place(kind, x, z, 0, 1, 1, 1), Is.True,
            $"LD02 original fixture cannot place {kind} at ({x},{z})");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (Application.isPlaying)
        {
            Time.timeScale = 1f;
            yield return new ExitPlayMode();
        }
    }
}
