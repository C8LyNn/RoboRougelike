// B01-61 W30: independent exact #877 Native Unity regression fixture.
// QA adversarial legacy/stale recovered model state; production code unchanged.
// [UnityTest] is AUTHORED, not executed, until B01-60 supplies native Editor.log/XML.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;

public sealed class B0161W30GarageRestoreHardZoneRegressionTests
{
    [UnityTest]
    public IEnumerator DestroyedCenterSawMustNotBypassCanonicalWeaponSafetyViaRepair()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();

        var until = Time.realtimeSinceStartup + 15f;
        while (R1RunController.Instance?.Progress == null &&
               Time.realtimeSinceStartup < until) yield return null;

        var run = R1RunController.Instance;
        Assert.That(run, Is.Not.Null, "real original playable R3 host not booted");
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard), Is.True);
        Assert.That(run.OpenSandboxGarage(), Is.True);
        var vehicle = run.VehicleModel;
        Assert.That(vehicle, Is.Not.Null);
        Assert.That(UnityEngine.Object.FindObjectsOfType<R3BuildValidation>().Length,
            Is.GreaterThan(0), "physical R3BuildValidation must be installed");

        // A previously saved legacy chassis could have a formerly permitted
        // central saw. Damage can set HP=0 before restoration in a new session.
        // Changing the model in this QA fixture simulates that STATE only;
        // this is not an assertion that this current UI can buy such a mount.
        var gun = vehicle.Parts.First(p => p.Kind == R3PartKind.Gun);
        var originalId = gun.Id;
        var originalCell = gun.Cell;
        gun.Weapon = R4WeaponKind.RotarySaw;
        gun.Health = 0f;
        vehicle.Award(1000);

        Assert.That(vehicle.InvalidWeaponTypes, Is.False,
            "fixture cannot pass by unrelated weapon-type rejection");
        Assert.That(vehicle.RepairCost(gun), Is.GreaterThan(0),
            "fixture must have actual positive repair economics");
        Assert.That(vehicle.Scrap, Is.GreaterThanOrEqualTo(vehicle.RepairCost(gun)));
        Assert.That(vehicle.Parts.Any(p => p != gun && p.Health > 0 &&
            (p.Cell - gun.Cell).sqrMagnitude == 1), Is.True,
            "fixture needs an actual connected neighboring part, not a cheap structural denial");

        int scrap = vehicle.Scrap;
        int count = vehicle.Parts.Count;
        int inventory = vehicle.Inventory.Count;
        // EXPECTED FAIL on original #877 if restoration only tests cube volumes
        // and does not enforce the physical disc hard-zone added to Validate().
        bool repairAccepted = vehicle.Repair(originalCell);

        Assert.That(repairAccepted, Is.False,
            "P0: destroyed central saw must not be restored by the old " +
            "volume-only restoration gate when a new mount is prohibited " +
            "because the real spinning disc intersects own chassis");
        Assert.That(vehicle.Scrap, Is.EqualTo(scrap),
            "rejected restoration must never spend Scrap");
        Assert.That(gun.Health, Is.EqualTo(0f),
            "failed restoration cannot resurrect the weapon");
        Assert.That(vehicle.Parts.Count, Is.EqualTo(count));
        Assert.That(vehicle.Inventory.Count, Is.EqualTo(inventory));
        Assert.That(gun.Id, Is.EqualTo(originalId),
            "rejected restore must preserve stable physical part identity");

        yield return new ExitPlayMode();
    }
}
