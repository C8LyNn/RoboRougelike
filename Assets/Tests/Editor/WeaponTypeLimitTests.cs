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
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Heavy));
        model.Award(10000);
        return model;
    }

    static Vector3Int Cell(int index) => new Vector3Int(index%7-3,2,index/7-3);

    [TestCase(1)] [TestCase(2)] [TestCase(4)] [TestCase(12)]
    public void TwoTypesPermitRequestedNumberOfGuns(int count)
    {
        var model=Model();
        for(int i=1;i<count;i++)
            Assert.That(model.Buy(Cell(i-1),R3PartKind.Gun,i%2==0?R4WeaponKind.Basic:R4WeaponKind.Automatic),Is.True,$"gun {i+1}");
        Assert.That(model.Parts.Count(p=>p.Kind==R3PartKind.Gun),Is.EqualTo(count));
        Assert.That(model.CanDeploy,Is.True);
    }

    [Test] public void ThirdTypeRejectedWithoutSpendingAndAnotherExistingTypeAllowed()
    {
        var model=Model();Assert.That(model.Buy(Cell(0),R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        var funds=model.Scrap;var count=model.Parts.Count;
        Assert.That(model.PlacementError(Cell(1),R3PartKind.Gun,false,R4WeaponKind.Heavy),Is.Not.Null);
        Assert.That(model.Buy(Cell(1),R3PartKind.Gun,R4WeaponKind.Heavy),Is.False);
        Assert.That(model.Scrap,Is.EqualTo(funds));Assert.That(model.Parts.Count,Is.EqualTo(count));
        Assert.That(model.Buy(Cell(1),R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
    }

    [Test] public void LostPlanReservesItsTypeUntilRemovedSoRepairCannotBypassLimit()
    {
        var model=Model();Assert.That(model.Buy(Cell(0),R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        var automatic=model.At(Cell(0));automatic.Health=0;
        Assert.That(model.Buy(Cell(1),R3PartKind.Gun,R4WeaponKind.Heavy),Is.False);
        Assert.That(model.Repair(Cell(0)),Is.True);
        Assert.That(model.Buy(Cell(1),R3PartKind.Gun,R4WeaponKind.Heavy),Is.False);
        Assert.That(model.Remove(Cell(0)),Is.True);
        Assert.That(model.Buy(Cell(1),R3PartKind.Gun,R4WeaponKind.Heavy),Is.True);
    }

    [Test] public void RecoveredThirdTypeMayBeStoredAndRepairedButNotMounted()
    {
        var model=Model();Assert.That(model.Buy(Cell(0),R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        var recovered=new R3PartState(Vector3Int.zero,R3PartKind.Gun,0){Weapon=R4WeaponKind.Heavy};
        Assert.That(model.ReceiveSalvage(new[]{recovered}),Is.True);
        Assert.That(model.RepairInventory(recovered.Id),Is.True);
        Assert.That(model.OwnedPlacementError(recovered.Id,Cell(1),Vector3Int.zero),Is.Not.Null);
        Assert.That(model.Mount(recovered.Id,Cell(1)),Is.False);
        Assert.That(model.Inventory.Any(p=>p.Id==recovered.Id),Is.True);
    }

    [Test] public void CopyAndDeserializedInvalidDesignCannotDeployOrRestoreThirdType()
    {
        var source=Model();Assert.That(source.Buy(Cell(0),R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        var copied=source.DeepCopy();Assert.That(copied.Buy(Cell(1),R3PartKind.Gun,R4WeaponKind.Heavy),Is.False);
        var design=new R3VehicleDesign();
        foreach(var p in source.Parts)design.Parts.Add(JsonUtility.FromJson<R3PartState>(JsonUtility.ToJson(p)));
        design.Parts.Add(new R3PartState(Cell(1),R3PartKind.Gun,0){Weapon=R4WeaponKind.Heavy});
        var loaded=new R3VehicleModel(design);loaded.Award(10000);
        Assert.That(loaded.CanDeploy,Is.False);
        Assert.That(loaded.Repair(Cell(1)),Is.False);
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
            var cells=(from z in new[]{-4,-2,2,4} from x in new[]{-3,-1,1,3} select new Vector3Int(x,2,z)).Take(11).ToArray();
            for(int i=0;i<cells.Length;i++)
            {
                rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
                var kind=i%2==0?R4WeaponKind.Automatic:R4WeaponKind.Basic;
                Assert.That(model.PlacementError(cells[i],R3PartKind.Gun,false,kind),Is.Null,$"physical gate gun {i+2}");
                Assert.That(model.Buy(cells[i],R3PartKind.Gun,kind),Is.True,$"buy gun {i+2}");
                rig.gameObject.SetActive(false);Object.Destroy(rig.gameObject);rig=null;
                yield return null;
            }
            rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
            Assert.That(rig.GetComponent<R3VehicleAssembly>().Parts.Count(p=>p.Kind==R3PartKind.Gun),Is.EqualTo(12));
            Assert.That(model.PlacementError(new Vector3Int(3,2,4),R3PartKind.Gun,false,R4WeaponKind.Heavy),Is.Not.Null);
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
