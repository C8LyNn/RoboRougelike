using System.Linq;
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R1;
using UnityEngine;
using UnityEngine.TestTools;

// Model-level EditMode tests; these do not create the game scene or capture input.
public sealed class WeaponTypeLimitTests
{
    static R3VehicleModel Model()
    {
        // A deliberately spacious, connected vehicle isolates the weapon-type
        // rule from the evolving starter catalog and its mounted guns.
        var design=new R3VehicleDesign {Name="Weapon limit fixture",Speed=7.5f,TurnSpeed=82,CoreHp=100};
        for(int x=-6;x<=6;x++)
        {
            design.Parts.Add(new R3PartState(new Vector3Int(x,0,0),x==0?R3PartKind.Core:R3PartKind.Armor));
            for(int z=-4;z<=4;z++)design.Parts.Add(new R3PartState(new Vector3Int(x,1,z),R3PartKind.Armor));
        }
        design.Parts.Add(new R3PartState(new Vector3Int(-4,0,1),R3PartKind.Wheel));
        design.Parts.Add(new R3PartState(new Vector3Int(4,0,1),R3PartKind.Wheel));
        design.Parts.Add(new R3PartState(new Vector3Int(0,2,-4),R3PartKind.Gun));
        var model=new R3VehicleModel(design);
        model.Award(10000);
        return model;
    }

    static Vector3Int Cell(int index) => new Vector3Int((index%6)*2-5,2,(index/6)*2-2);

    [Test] public void ActiveTypeLimitCountsTypesRatherThanCopies()
    {
        var model=Model();
        var kinds=new[]{R3PartKind.Blink,R3PartKind.MagneticGrapple,R3PartKind.Booster,R3PartKind.RepairPulse};
        for(int i=0;i<kinds.Length;i++)model.Parts.Add(new R3PartState(new Vector3Int(30+i,1,0),kinds[i]));
        for(int i=0;i<6;i++)model.Parts.Add(new R3PartState(new Vector3Int(40+i,1,0),R3PartKind.Booster));
        Assert.That(model.PlacementError(Cell(0),R3PartKind.Booster),Is.Null,"Copies do not use new type slots.");
        Assert.That(model.PlacementError(Cell(0),R3PartKind.ScannerPulse),Does.Contain("4"));
    }
    [Test] public void PassiveTypeLimitCountsBonusIdentityAndPermitsCopies()
    {
        var model=Model();
        var kinds=new[]{R3PartKind.Suspension,R3PartKind.Gyroscope,R3PartKind.DirectionalShield,R3PartKind.PointDefense,
            R3PartKind.IndependentSteering,R3PartKind.Battery,R3PartKind.DrivePower};
        for(int i=0;i<kinds.Length;i++)model.Parts.Add(new R3PartState(new Vector3Int(30+i,1,1),kinds[i]));
        model.Parts.Add(new R3PartState(new Vector3Int(40,1,1),R3PartKind.BonusModule){UpgradeId="damage"});
        Assert.That(model.PlacementError(Cell(0),R3PartKind.Battery),Is.Null);
        Assert.That(model.PlacementError(Cell(0),R3PartKind.EnergyRegenerator),Does.Contain("8"));
    }

    [Test] public void FourTypesCanMountAndFifthIsRejectedWithoutSpending()
    {
        var model=Model();
        var kinds=new[]{R4WeaponKind.Automatic,R4WeaponKind.Heavy,R4WeaponKind.Shotgun};
        for(int i=0;i<kinds.Length;i++)Assert.That(model.Buy(Cell(i),R3PartKind.Gun,kinds[i]),Is.True,$"type {kinds[i]}");
        var funds=model.Scrap;
        Assert.That(model.Buy(Cell(3),R3PartKind.Gun,R4WeaponKind.Railgun),Is.False);
        Assert.That(model.Scrap,Is.EqualTo(funds));
        Assert.That(model.Parts.Where(p=>p.Kind==R3PartKind.Gun).Select(p=>p.Weapon).Distinct().Count(),Is.EqualTo(4));
        Assert.That(model.Buy(Cell(3),R3PartKind.Gun,R4WeaponKind.Basic),Is.True,"Additional copies do not use another type slot.");
    }

    [TestCase(1)] [TestCase(2)] [TestCase(4)] [TestCase(12)]
    public void TwoTypesPermitRequestedNumberOfGuns(int count)
    {
        var model=Model();
        for(int i=1;i<count;i++)
            Assert.That(model.Buy(Cell(i-1),R3PartKind.Gun,i%2==0?R4WeaponKind.Basic:R4WeaponKind.Automatic),Is.True,$"gun {i+1}");
        Assert.That(model.Parts.Count(p=>p.Kind==R3PartKind.Gun),Is.EqualTo(count));
        Assert.That(model.CanDeploy,Is.True);
    }

    [Test] public void TwelveIdenticalGunsAreAllowed()
    {
        var model=Model();
        for(int i=0;i<11;i++)Assert.That(model.Buy(Cell(i),R3PartKind.Gun,R4WeaponKind.Basic),Is.True,$"gun {i+2}");
        Assert.That(model.Parts.Count(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.Basic),Is.EqualTo(12));
    }

    static R3VehicleModel ModelWithFourTypes()
    {
        var model=Model();
        Assert.That(model.Buy(Cell(0),R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        Assert.That(model.Buy(Cell(1),R3PartKind.Gun,R4WeaponKind.Heavy),Is.True);
        Assert.That(model.Buy(Cell(2),R3PartKind.Gun,R4WeaponKind.Shotgun),Is.True);
        return model;
    }
    [Test] public void FifthTypeRejectedWithoutSpendingAndAnotherExistingTypeAllowed()
    {
        var model=ModelWithFourTypes();
        var funds=model.Scrap;var count=model.Parts.Count;
        Assert.That(model.PlacementError(Cell(3),R3PartKind.Gun,false,R4WeaponKind.Railgun),Is.Not.Null);
        Assert.That(model.Buy(Cell(3),R3PartKind.Gun,R4WeaponKind.Railgun),Is.False);
        Assert.That(model.Scrap,Is.EqualTo(funds));Assert.That(model.Parts.Count,Is.EqualTo(count));
        Assert.That(model.Buy(Cell(3),R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
    }

    [Test] public void LostPlanReservesItsTypeUntilRemovedSoRepairCannotBypassLimit()
    {
        var model=ModelWithFourTypes();
        var shotgun=model.At(Cell(2));shotgun.Health=0;
        Assert.That(model.Buy(Cell(3),R3PartKind.Gun,R4WeaponKind.Railgun),Is.False);
        Assert.That(model.Repair(Cell(2)),Is.True);
        Assert.That(model.Buy(Cell(3),R3PartKind.Gun,R4WeaponKind.Railgun),Is.False);
        Assert.That(model.Remove(Cell(2)),Is.True);
        Assert.That(model.Buy(Cell(3),R3PartKind.Gun,R4WeaponKind.Railgun),Is.True);
    }

    [Test] public void RecoveredFifthTypeMayBeStoredAndRepairedButNotMounted()
    {
        var model=ModelWithFourTypes();
        var recovered=new R3PartState(Vector3Int.zero,R3PartKind.Gun,0){Weapon=R4WeaponKind.Railgun};
        Assert.That(model.ReceiveSalvage(new[]{recovered}),Is.True);
        Assert.That(model.RepairInventory(recovered.Id),Is.True);
        Assert.That(model.OwnedPlacementError(recovered.Id,Cell(3),Vector3Int.zero),Is.Not.Null);
        Assert.That(model.Mount(recovered.Id,Cell(3)),Is.False);
        Assert.That(model.Inventory.Any(p=>p.Id==recovered.Id),Is.True);
        var restored=model.DeepCopy();
        Assert.That(restored.Mount(recovered.Id,Cell(3)),Is.False);
        Assert.That(restored.Inventory.Any(p=>p.Id==recovered.Id),Is.True);
    }

    [Test] public void CopyAndDeserializedInvalidDesignCannotDeployOrRestoreFifthType()
    {
        var source=ModelWithFourTypes();
        var copied=source.DeepCopy();Assert.That(copied.Buy(Cell(3),R3PartKind.Gun,R4WeaponKind.Railgun),Is.False);
        var design=new R3VehicleDesign();
        foreach(var p in source.Parts)design.Parts.Add(JsonUtility.FromJson<R3PartState>(JsonUtility.ToJson(p)));
        design.Parts.Add(new R3PartState(Cell(3),R3PartKind.Gun,0){Weapon=R4WeaponKind.Railgun});
        var loaded=new R3VehicleModel(design);loaded.Award(10000);
        Assert.That(loaded.CanDeploy,Is.False);
        Assert.That(loaded.Repair(Cell(3)),Is.False);
        Assert.That(loaded.EmergencyRepair(),Is.False);
        Assert.That(loaded.EmergencyRepairCost,Is.Zero);
        Assert.That(loaded.DeepCopy().CanDeploy,Is.False);
    }

    [UnityTest] public IEnumerator TwelveGunsPassRealGaragePlacementGateAndProduceTwelveMountedParts()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var model=Model();var paletteObject=new GameObject("Weapon type test palette");
        var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        R1VehicleState rig=null;
        try
        {
            // Two outward-facing rows leave a cell between guns and preserve
            // a physically unobstructed firing lane for every weapon.
            var front=new[]{-6,-4,-2,2,4,6}.Select(x=>new Vector3Int(x,2,-4));
            var back=new[]{-4,-2,0,2,4}.Select(x=>new Vector3Int(x,2,4));
            var cells=front.Concat(back).ToArray();
            for(int i=0;i<cells.Length;i++)
            {
                rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
                var kind=i%2==0?R4WeaponKind.Automatic:R4WeaponKind.Basic;
                var rotation=i<6?Vector3Int.zero:new Vector3Int(0,2,0);
                Assert.That(model.PlacementError(cells[i],R3PartKind.Gun,false,kind,rotation),Is.Null,$"physical gate gun {i+2}");
                Assert.That(model.Buy(cells[i],R3PartKind.Gun,kind,rotation),Is.True,$"buy gun {i+2}");
                rig.gameObject.SetActive(false);Object.Destroy(rig.gameObject);rig=null;
                yield return null;
            }
            rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
            Assert.That(rig.GetComponent<R3VehicleAssembly>().Parts.Count(p=>p.Kind==R3PartKind.Gun),Is.EqualTo(12));
            var existing=model.Parts.Where(p=>p.Kind==R3PartKind.Gun).ToArray();
            existing[0].Weapon=R4WeaponKind.Heavy;existing[1].Weapon=R4WeaponKind.Shotgun;
            Assert.That(model.PlacementError(new Vector3Int(6,2,4),R3PartKind.Gun,false,R4WeaponKind.Railgun,new Vector3Int(0,2,0)),Does.Contain("4 różne typy"));
        }
        finally
        {
            if(rig!=null)Object.Destroy(rig.gameObject);
            for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=null;
            for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=null;
            Object.Destroy(paletteObject);Object.Destroy(material);
        }
        yield return new ExitPlayMode();
    }
}
