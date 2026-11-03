using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

// B01-60 W30 independent in-game acceptance on exact Codex #877.
// R3 owns the ONLY placement/Buy/Mount authority; this class never adds one.
// Genuine scene, ghost, installationGate, live PhysX Collider and Scrap/part IDs.
// Authored Unity tests are NOT actual native Unity PASS until Editor runs them.
public sealed class W30B0160GarageDynamicGateTests
{
    private const string GameplayScene = "Assets/scene 1.unity";
    private static readonly Vector3Int ExteriorSaw = new Vector3Int(0, 1, 4);

    [UnityTearDown]
    public IEnumerator LeaveScene()
    {
        if (Application.isPlaying)
            yield return new ExitPlayMode();
    }

    private static IEnumerator StartRealGarage()
    {
        EditorSceneManager.OpenScene(GameplayScene, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline = Time.realtimeSinceStartup + 15f;
        while (R1RunController.Instance?.Progress == null &&
               Time.realtimeSinceStartup < deadline)
            yield return null;
        var run = R1RunController.Instance;
        Assert.That(run, Is.Not.Null, "actual R3 game controller required");
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard), Is.True);
        Assert.That(run.OpenSandboxGarage(), Is.True,
            "must enter ACTUAL gameplay garage, not a synthetic model");
    }

    private static (R1RunController run, R3GarageView view, string id)
        PrepareOwnedSaw()
    {
        var run = R1RunController.Instance;
        var view = Object.FindFirstObjectByType<R3GarageView>(FindObjectsInactive.Include);
        Assert.That(view, Is.Not.Null, "actual garage 3D ghost required");
        var gun = run.VehicleModel.Parts.First(p => p.Kind == R3PartKind.Gun);
        var id = gun.Id;
        Assert.That(run.GarageRemove(gun.Cell), Is.True);
        var stored = run.VehicleModel.Inventory.Single(p => p.Id == id);
        stored.Weapon = R4WeaponKind.RotarySaw;
        view.ChooseInventory(id);
        view.Select(ExteriorSaw);
        return (run, view, id);
    }

    [UnityTest]
    public IEnumerator LiveOwnedBlockerMovedIntoSawDisc_RefusesWithoutScrapOrIdLoss_ThenRecovers()
    {
        yield return StartRealGarage();
        var setup = PrepareOwnedSaw();
        var run = setup.run;
        var view = setup.view;
        var id = setup.id;
        var model = run.VehicleModel;

        // Original #877 claims a legal exterior physical mount. Fail if
        // the candidate is no longer legal on this EXACT current game head.
        Assert.That(view.PlacementIssue, Is.Null,
            "original legitimate exterior saw mount must be physically available");
        Assert.That(view.PreviewValid, Is.True);

        var center = view.Preview.GetComponentsInChildren<Transform>(true)
            .Single(t => t.name == "Środek piły");
        var own = GameObject.CreatePrimitive(PrimitiveType.Cube);
        own.name = "B01-60 W30 moving owned armor near actual saw cutting disc";
        own.transform.SetParent(run.Player.transform, true);
        own.transform.localScale = Vector3.one * .12f;
        own.transform.position = center.position + Vector3.up * 3f;
        Physics.SyncTransforms();
        Assert.That(view.PreviewValid, Is.True,
            "distant owner Collider cannot reject an otherwise legal mount");

        // This happens AFTER the user first saw a GREEN ghost. Both preview
        // and transaction must observe the new physical geometry immediately.
        own.transform.position = center.position;
        Physics.SyncTransforms();
        var issue = view.PlacementIssue;
        Assert.That(issue, Does.Contain("Fioletowa strefa"),
            "actual disc/owner PhysX collision must be a HARD gate, not only purple UI");
        Assert.That(view.PreviewValid, Is.False);
        Assert.That(issue, Is.EqualTo(model.OwnedPlacementError(id, ExteriorSaw, view.SelectedRotation)));

        int scrap = model.Scrap;
        string[] inventoryIds = model.Inventory.Select(p => p.Id).OrderBy(s => s).ToArray();
        string[] installedIds = model.Parts.Select(p => p.Id).OrderBy(s => s).ToArray();
        for (int i = 0; i != 3; i++)
        {
            Assert.That(view.ClickTarget(), Is.False,
                "stale earlier GREEN cannot authorize a new collision");
            Assert.That(view.Message, Is.EqualTo(issue));
            Assert.That(model.Scrap, Is.EqualTo(scrap));
            CollectionAssert.AreEqual(inventoryIds,
                model.Inventory.Select(p => p.Id).OrderBy(s => s).ToArray(),
                "reject cannot consume or duplicate inventory identity");
            CollectionAssert.AreEqual(installedIds,
                model.Parts.Select(p => p.Id).OrderBy(s => s).ToArray(),
                "reject cannot mutate existing installed parts");
        }

        // Without a new gun, new loaner or bypass rule, remove actual
        // obstruction and retry the SAME part ID via the SAME garage UI.
        own.SetActive(false);
        Object.Destroy(own);
        Physics.SyncTransforms();
        yield return null;

        Assert.That(view.PlacementIssue, Is.Null);
        Assert.That(view.PreviewValid, Is.True);
        Assert.That(view.ClickTarget(), Is.True);
        Assert.That(model.Scrap, Is.EqualTo(scrap),
            "remounting owned part must not silently charge Scrap");
        Assert.That(model.Parts.Single(p => p.Id == id).Cell, Is.EqualTo(ExteriorSaw));
        Assert.That(model.Inventory.Any(p => p.Id == id), Is.False);
        Assert.That(model.Parts.Concat(model.Inventory).Select(p => p.Id).Distinct().Count(),
            Is.EqualTo(model.Parts.Count + model.Inventory.Count));
    }

    [UnityTest]
    public IEnumerator RotateRealSawGhost_UsesSameAuthoritativePlacementGateAndDoesNotMutateOnReject()
    {
        yield return StartRealGarage();
        var setup = PrepareOwnedSaw();
        var run = setup.run;
        var view = setup.view;
        var id = setup.id;
        var model = run.VehicleModel;
        int startingScrap = model.Scrap;
        string[] installed = model.Parts.Select(p => p.Id).OrderBy(s => s).ToArray();
        string[] inventory = model.Inventory.Select(p => p.Id).OrderBy(s => s).ToArray();

        // Four real Y rotations update the live marker. No new interpretation
        // is allowed between ghost, model placement error and physical Buy/Mount.
        for (int turn = 0; turn < 4; turn++)
        {
            var gateIssue = model.OwnedPlacementError(id, ExteriorSaw, view.SelectedRotation);
            Assert.That(view.PlacementIssue, Is.EqualTo(gateIssue),
                "R3 garage ghost and canonical mount MUST share one validator");
            Assert.That(view.PreviewValid, Is.EqualTo(gateIssue == null),
                "real 3D marker must be updated for each quarter-turn");
            if (gateIssue != null)
            {
                Assert.That(view.ClickTarget(), Is.False);
                Assert.That(model.Scrap, Is.EqualTo(startingScrap));
                CollectionAssert.AreEqual(installed,
                    model.Parts.Select(p => p.Id).OrderBy(s => s).ToArray());
                CollectionAssert.AreEqual(inventory,
                    model.Inventory.Select(p => p.Id).OrderBy(s => s).ToArray());
            }
            view.RotateSelected(1, 1);
        }

        // Four 90° turns must normalize to original rotation and legal mount.
        Assert.That(view.SelectedRotation, Is.EqualTo(Vector3Int.zero));
        Assert.That(view.PreviewValid, Is.True);
        Assert.That(view.ClickTarget(), Is.True);
        Assert.That(model.Scrap, Is.EqualTo(startingScrap));
        Assert.That(model.Parts.Single(p => p.Id == id).Cell, Is.EqualTo(ExteriorSaw));
    }
}
