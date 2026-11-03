using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// B01-60 W31 positive acceptance on real scene, canonical R3 installation
/// and restoration authorities, and the same paid wallet/ID ownership.
/// Source authored only; Unity 6000.3.14f1 execution is a separate G1 gate.
/// Unlike existing #877 rejection cases, these MUST succeed to prove the
/// new fail-closed repair gate does not softlock a legitimately clear robot.
/// </summary>
public sealed class B0160W31SafeRestorationGameAcceptanceTests
{
    [UnityTest]
    public IEnumerator LiveExteriorRotarySaw_PaidRebuildRestoresPhysicalGunAndSameIdOnce()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        var deadline = Time.realtimeSinceStartup + 20f;
        while (R1RunController.Instance?.Progress == null &&
               Time.realtimeSinceStartup < deadline) yield return null;
        var run = R1RunController.Instance;
        Assert.That(run, Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard), Is.True);
        Assert.That(run.OpenSandboxGarage(), Is.True);
        Assert.That(run.Player, Is.Not.Null);
        var model = run.VehicleModel;
        var stockGun = model.Parts.Single(p => p.Kind == R3PartKind.Gun);
        var gunId = stockGun.Id;
        var originalCell = stockGun.Cell;
        Assert.That(run.GarageRemove(originalCell), Is.True);

        // Exactly the approved original #877 real exterior attachment from
        // its existing forward-placement rejection/resumption regression.
        var ownedGun = model.Inventory.Single(p => p.Id == gunId);
        ownedGun.Weapon = R4WeaponKind.RotarySaw;
        var exterior = new Vector3Int(0, 1, 4);
        Assert.That(model.OwnedPlacementError(gunId, exterior, default), Is.Null,
            "A legal exterior original WPN005 mount must remain possible");
        Assert.That(run.GarageMount(gunId, exterior, default), Is.True);
        yield return null; // Real garage RebuildGarageVehicle retires the old rig.

        // Set up a physically damaged state, not a fake victory or a second
        // writer. The canonical paid repair must not consume Scrap until the
        // single physical restoration gate has approved the same swept disc.
        var destroyed = model.Parts.Single(p => p.Id == gunId);
        destroyed.Health = 0f;
        var former = run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p => p.Id == gunId);
        former.gameObject.SetActive(false);
        Physics.SyncTransforms();
        Assert.That(model.CanDeploy, Is.False, "Last destroyed genuine gun prevents deployment");
        model.Award(1000); // One-time fixture funds, NOT earned in a battle.
        int price = model.RepairCost(destroyed);
        Assert.That(price, Is.GreaterThan(0));
        int wallet = model.Scrap, count = model.Parts.Count, inventory = model.Inventory.Count;
        var ids = model.Parts.Concat(model.Inventory).Select(p => p.Id).OrderBy(v => v).ToArray();

        Assert.That(run.GarageRepair(exterior), Is.True,
            "Legal exterior saw must pass the SAME restoration physical gate used for negative legacy repairs");
        Assert.That(model.Scrap, Is.EqualTo(wallet - price),
            "A successful paid repair consumes EXACTLY its published Scrap quote once");
        Assert.That(model.Parts.Count, Is.EqualTo(count));
        Assert.That(model.Inventory.Count, Is.EqualTo(inventory));
        Assert.That(model.Parts.Concat(model.Inventory).Select(p => p.Id).OrderBy(v => v),
            Is.EqualTo(ids), "Paid rebuild must not replace or duplicate physical Part IDs");
        var restored = model.Parts.Single(p => p.Id == gunId);
        Assert.That(restored.Health, Is.EqualTo(1f));
        Assert.That(restored.Cell, Is.EqualTo(exterior));
        Assert.That(restored.Weapon, Is.EqualTo(R4WeaponKind.RotarySaw));
        var rebuiltGun = run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p => p.Id == gunId);
        Assert.That(rebuiltGun.Operational, Is.True);
        Assert.That(rebuiltGun.Muzzle, Is.Not.Null);
        Assert.That(rebuiltGun.gameObject.activeInHierarchy, Is.True,
            "Successful paid recovery must recreate actual operational R3 physical mount");
        Assert.That(model.CanDeploy, Is.True);
        Assert.That(run.GarageRepair(exterior), Is.False,
            "Second paid repair with full HP must not charge the same part again");
        Assert.That(model.Scrap, Is.EqualTo(wallet - price));

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator LiveDefaultBasicGun_EmergencyRepairRestoresDeployabilityWithoutDuplicateWalletDebit()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        var deadline = Time.realtimeSinceStartup + 20f;
        while (R1RunController.Instance?.Progress == null &&
               Time.realtimeSinceStartup < deadline) yield return null;
        var run = R1RunController.Instance;
        Assert.That(run, Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard), Is.True);
        Assert.That(run.OpenSandboxGarage(), Is.True);
        Assert.That(run.Player, Is.Not.Null);
        var model = run.VehicleModel;
        var gun = model.Parts.Single(p => p.Kind == R3PartKind.Gun);
        string originalId = gun.Id;
        var originalCell = gun.Cell;
        Assert.That(gun.Weapon, Is.EqualTo(R4WeaponKind.Basic),
            "Default original gun is a legal directional weapon, not the disabled legacy Saw");
        gun.Health = 0f;
        var oldMounted = run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p => p.Id == originalId);
        oldMounted.gameObject.SetActive(false);
        Physics.SyncTransforms();
        Assert.That(model.CanDeploy, Is.False);
        model.Award(1000);
        int quote = model.EmergencyRepairCost;
        Assert.That(quote, Is.GreaterThan(0),
            "Actual nondeployable R3 must compute a nonzero affordable minimal repair plan");
        Assert.That(quote, Is.LessThanOrEqualTo(model.Scrap));
        int before = model.Scrap, partCount = model.Parts.Count, stockCount = model.Inventory.Count;
        var beforeIds = model.Parts.Concat(model.Inventory).Select(p => p.Id).OrderBy(v => v).ToArray();
        Assert.That(run.GarageEmergencyRepair(), Is.True,
            "An existing legal Basic Gun must not be rejected just because hard weapon and rollback gates were strengthened");
        Assert.That(model.Scrap, Is.EqualTo(before - quote));
        Assert.That(model.Parts.Count, Is.EqualTo(partCount));
        Assert.That(model.Inventory.Count, Is.EqualTo(stockCount));
        Assert.That(model.Parts.Concat(model.Inventory).Select(p => p.Id).OrderBy(v => v),
            Is.EqualTo(beforeIds));
        var repaired = model.Parts.Single(p => p.Id == originalId);
        Assert.That(repaired.Health, Is.EqualTo(1f));
        Assert.That(repaired.Cell, Is.EqualTo(originalCell));
        Assert.That(model.CanDeploy, Is.True);
        var realGun = run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p => p.Id == originalId);
        Assert.That(realGun.Operational && realGun.gameObject.activeInHierarchy, Is.True);
        Assert.That(realGun.Muzzle, Is.Not.Null);
        Assert.That(run.GarageEmergencyRepair(), Is.False,
            "Repeating emergency recovery on a functional rig must not spend again");
        Assert.That(model.Scrap, Is.EqualTo(before - quote));

        yield return new ExitPlayMode();
    }
}
