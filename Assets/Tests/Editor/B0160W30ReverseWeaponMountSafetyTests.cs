using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.Weapons.RotarySaw;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// B01-60 P17 W30 independent failing-first NATIVE Unity regression for the
/// REAL existing #877 R3 game and canonical placement/purchase authority.
/// The original #877 tests candidate Gun, but does not revalidate installed
/// Gun after buying an ordinary Armor module inside its physical blade disc.
/// No fake UnityEngine implementation, no independent garage authority.
/// </summary>
public sealed class B0160W30ReverseWeaponMountSafetyTests
{
    [UnityTest]
    public IEnumerator RealGarage_DoesNotAllowNewArmorToEnterAlreadyMountedSawDisc()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20f;
        while(R1RunController.Instance?.Progress==null && Time.realtimeSinceStartup<deadline)
            yield return null;
        var run=R1RunController.Instance;
        Assert.That(run,Is.Not.Null,"Real current R3 game controller must exist");
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);

        // Start with a real supported gun, not a standalone or simulated
        // R3Body. Its exterior cell is already accepted by the #877 fixture.
        var gun=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Gun);
        string gunId=gun.Id;
        Assert.That(run.GarageRemove(gun.Cell),Is.True);
        var owned=run.VehicleModel.Inventory.Single(p=>p.Id==gunId);
        owned.Weapon=R4WeaponKind.RotarySaw;
        Vector3Int safeExteriorSaw=new Vector3Int(0,1,4);
        Assert.That(run.VehicleModel.OwnedPlacementError(gunId,safeExteriorSaw,default),Is.Null);
        Assert.That(run.GarageMount(gunId,safeExteriorSaw,default),Is.True);
        yield return null;

        // The real Standard chassis has structural Armor up to z=3. Build a
        // connected Armor side-rail at x=+1 so (0,1,5) is supported by genuine
        // ARMOR at (1,1,5), not by a Gun (which is not a valid support).
        // This is a normal legal subsequent placement, not an invalid
        // disconnected/overlapping candidate disguised as a saw safety test.
        run.VehicleModel.Award(1000); // Test setup only, NOT a real run reward.
        foreach(var cell in new[]{new Vector3Int(1,1,4),new Vector3Int(1,1,5)})
        {
            string prerequisite=run.VehicleModel.PlacementError(cell,R3PartKind.Armor);
            Assert.That(prerequisite,Is.Null,
                "The structural side-rail must be legal before checking reverse saw protection at "+cell+
                "; got "+prerequisite);
            Assert.That(run.GarageBuy(cell,R3PartKind.Armor),Is.True,
                "Live R3 garage must build a real supporting armor rail at "+cell);
            yield return null; // Allow old physics/probe Destroy() to be retired.
        }

        var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        Assert.That(assembly,Is.Not.Null);
        var mountedSaw=assembly.Parts.Single(p=>p.Id==gunId);
        Assert.That(mountedSaw.Muzzle,Is.Not.Null);
        Assert.That(mountedSaw.Weapon,Is.EqualTo(R4WeaponKind.RotarySaw));

        // Use the real Unity PhysX BoxCollider to prove that a future
        // normal R3 Armor cube at (0,1,5) REALLY intersects the *existing*
        // WPN005 cutting DISC. A blade is a swept-volume contact weapon, not
        // a projectile ray; the decorative visual rotor has no solid collider.
        Vector3Int dangerousArmor=new Vector3Int(0,1,5);
        var physicalWitness=GameObject.CreatePrimitive(PrimitiveType.Cube);
        physicalWitness.name="B01-60 W30 REVERSE ARMOR physical witness (not bought)";
        physicalWitness.transform.SetParent(assembly.transform,false);
        physicalWitness.transform.localPosition=(Vector3)dangerousArmor*.55f;
        physicalWitness.transform.localScale=Vector3.one*.52f;
        var box=physicalWitness.GetComponent<BoxCollider>();
        Physics.SyncTransforms();
        var cfg=new RotarySawConfiguration();
        Vector3 discCenter=mountedSaw.Muzzle.position;
        bool cutsOwnedArmor=box.bounds.Contains(discCenter)||
            RotarySawContactMath.IsOnCuttingDisc(
                box.ClosestPoint(discCenter)-discCenter,
                mountedSaw.Muzzle.right,cfg.bladeRadius,cfg.bladeHalfThickness,cfg.hubDeadZone);
        Assert.That(cutsOwnedArmor,Is.True,
            "Fixture must verify actual physical Collider enters the genuine installed saw disc; "+
            "otherwise a placement rejection is NOT proof of reverse weapon safety.");

        // Never leave our diagnostic collider in the authoritative
        // installation physics query. Neither it nor source-only metadata
        // can be the reason the real candidate is rejected.
        physicalWitness.SetActive(false);
        Object.Destroy(physicalWitness);
        yield return null;
        Physics.SyncTransforms();

        int scrapBefore=run.VehicleModel.Scrap;
        int partsBefore=run.VehicleModel.Parts.Count;
        int inventoryBefore=run.VehicleModel.Inventory.Count;
        string[] idsBefore=run.VehicleModel.Parts.Concat(run.VehicleModel.Inventory)
            .Select(p=>p.Id).OrderBy(id=>id).ToArray();

        // #877 source currently omits installed-weapon reverse exclusion when
        // candidate.Kind != Gun. FIRST require the SAME canonical preview
        // R3VehicleModel placement gate to deny this genuine physical hazard.
        string issue=run.VehicleModel.PlacementError(dangerousArmor,R3PartKind.Armor);
        Assert.That(issue,Is.Not.Null.And.Contain("Fioletowa strefa"),
            "NEW armor must be rejected when it enters an EXISTING saw disc. "+
            "Original #877 guard only runs for candidate.Kind==Gun; preview returned: "+issue);
        Assert.That(run.GarageBuy(dangerousArmor,R3PartKind.Armor),Is.False,
            "True Buy must invoke the SAME reverse weapon safety validator as preview");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrapBefore),
            "Rejected reverse sweep must not debit player's Scrap");
        Assert.That(run.VehicleModel.Parts.Count,Is.EqualTo(partsBefore),
            "Rejected reverse sweep must not add/create a phantom part");
        Assert.That(run.VehicleModel.Inventory.Count,Is.EqualTo(inventoryBefore),
            "Rejected reverse sweep must not consume inventory");
        Assert.That(run.VehicleModel.Parts.Concat(run.VehicleModel.Inventory)
            .Select(p=>p.Id).OrderBy(id=>id),Is.EqualTo(idsBefore),
            "Rejected reverse sweep must leave every existing stable Part ID unchanged");
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==gunId).Cell,Is.EqualTo(safeExteriorSaw),
            "Previously mounted legal Saw must never be silently moved");
        yield return new ExitPlayMode();
    }
}
