using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class EquipmentScaleOneTests
{
    [SetUp] public void UseNormalSize()=>R3VehicleModel.SetEquipmentScaleForEditorTests(false);
    [TearDown] public void RestoreNormalSize()=>R3VehicleModel.SetEquipmentScaleForEditorTests(false);
    [TestCase(R4WeaponKind.Basic)]
    [TestCase(R4WeaponKind.Automatic)]
    [TestCase(R4WeaponKind.Heavy)]
    [TestCase(R4WeaponKind.RocketLauncher)]
    [TestCase(R4WeaponKind.Shotgun)]
    [TestCase(R4WeaponKind.Railgun)]
    [TestCase(R4WeaponKind.BeamCutter)]
    [TestCase(R4WeaponKind.EmpEmitter)]
    [TestCase(R4WeaponKind.RotarySaw)]
    public void NewGunsUseOneCellAndKeepLargeFormatAvailable(R4WeaponKind kind)
    {
        Assert.That(R3VehicleModel.SupportsGunScale2(kind),Is.True);
        var design=new R3VehicleDesign{Name="scale",CoreHp=100};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        var model=new R3VehicleModel(design);model.Award(1000);
        Assert.That(model.PurchaseToInventory(R3PartKind.Gun,kind),Is.True);
        var gun=model.Inventory.Single();Assert.That(gun.FootprintRevision,Is.Zero);
        Assert.That(R3VehicleModel.OccupiedCells(gun).Count(),Is.EqualTo(1));
        var large=gun.Copy();large.FootprintRevision=2;
        Assert.That(R3VehicleModel.OccupiedCells(large).Count(),Is.EqualTo(18));
    }
    [TestCase(R3WheelStyle.LargeOffroad)]
    [TestCase(R3WheelStyle.Monster)]
    public void EnlargedWheelTypesDefaultToOneCellAndNormalRadius(R3WheelStyle style)
    {
        Assert.That(R3VehicleModel.NewPurchaseFootprint(R3PartKind.Wheel,R4WeaponKind.Basic,style),Is.Zero);
        Assert.That(R3VehicleModel.WheelRadius(style),Is.LessThanOrEqualTo(.39f+.001f));
        var wheel=new R3PartState(Vector3Int.zero,R3PartKind.Wheel){WheelStyle=style,FootprintRevision=1};
        Assert.That(R3VehicleModel.OccupiedCells(wheel).Count(),Is.EqualTo(9),"The previous larger footprint remains available.");
    }
    [Test] public void LoadingLargeGunsRestoresSmallSizeWithoutMovingOrChangingParts()
    {
        var design=R3VehicleCatalog.Create(R3VehicleKind.Standard,0);
        foreach(var p in design.Parts.Where(p=>p.Kind==R3PartKind.Gun))p.FootprintRevision=2;
        var source=new R3VehicleModel(design);var data=source.MakeCheckpoint(0);
        var guns=data.mounted.Where(p=>p.Kind==R3PartKind.Gun).Select(p=>p.Id).ToArray();
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,null,0,out var result,out var error),Is.True,error);
        Assert.That(result.Parts.Count,Is.EqualTo(data.mounted.Length));
        foreach(string id in guns)Assert.That(result.Parts.Single(p=>p.Id==id).FootprintRevision,Is.Zero);
        Assert.That(data.mounted.Where(p=>p.Kind==R3PartKind.Gun).All(p=>p.FootprintRevision==2),Is.True,"Loading must not mutate the source checkpoint.");
        Assert.That(R3VehicleModel.Connected(result.Parts),Is.True);
    }
    [Test] public void FullInventoryDoesNotPreventShrinkingMountedGuns()
    {
        var design=R3VehicleCatalog.Create(R3VehicleKind.Light,0);
        foreach(var part in design.Parts.Where(p=>p.Kind==R3PartKind.Gun))part.FootprintRevision=2;
        var data=new R3VehicleModel(design).MakeCheckpoint(0);
        data.inventory=Enumerable.Range(0,R3VehicleModel.InventoryCapacity)
            .Select(_=>new R3PartState(Vector3Int.zero,R3PartKind.Armor){DefenseRechargeAge=float.MaxValue}).ToArray();
        var gun=data.mounted.Single(p=>p.Kind==R3PartKind.Gun);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,null,0,out var result,out var error),Is.True,error);
        Assert.That(result.Inventory.Count,Is.EqualTo(R3VehicleModel.InventoryCapacity));
        Assert.That(result.Parts.Single(p=>p.Id==gun.Id).FootprintRevision,Is.Zero);
        Assert.That(gun.FootprintRevision,Is.EqualTo(2));
    }
    [TestCase(R3WheelStyle.LargeOffroad)]
    [TestCase(R3WheelStyle.Monster)]
    public void LoadingLargeWheelRetainsItsConnectionAndIdentity(R3WheelStyle style)
    {
        var design=new R3VehicleDesign{Name="wheel save",CoreHp=100};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.left,R3PartKind.Wheel,.6f){
            WheelStyle=style,FootprintRevision=1,ContactMask=R3MountingGrid.FaceBit(Vector3Int.right)});
        var data=new R3VehicleModel(design).MakeCheckpoint(0);
        var saved=data.mounted.Single(p=>p.Kind==R3PartKind.Wheel);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,null,0,out var result,out var error),Is.True,error);
        var restored=result.Parts.Single(p=>p.Id==saved.Id);
        Assert.That(restored.FootprintRevision,Is.Zero);
        Assert.That(restored.Cell,Is.EqualTo(saved.Cell));Assert.That(restored.Health,Is.EqualTo(.6f));
        Assert.That(restored.ContactMask,Is.EqualTo(saved.ContactMask));
        Assert.That(saved.FootprintRevision,Is.EqualTo(1));
        Assert.That(R3VehicleModel.Connected(result.Parts),Is.True);
    }
    [Test] public void LoadingLargeInventoryEquipmentKeepsIdentityAndDurability()
    {
        var design=new R3VehicleDesign{Name="inventory save",CoreHp=100};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        var model=new R3VehicleModel(design);model.Award(1000);
        Assert.That(model.PurchaseToInventory(R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        Assert.That(model.PurchaseToInventory(R3PartKind.Wheel,R4WeaponKind.Basic,R3WheelStyle.Monster),Is.True);
        var data=model.MakeCheckpoint(0);
        data.inventory.Single(p=>p.Kind==R3PartKind.Gun).FootprintRevision=2;
        data.inventory.Single(p=>p.Kind==R3PartKind.Wheel).FootprintRevision=1;
        foreach(var item in data.inventory)item.Health=.6f;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,null,0,out var result,out var error),Is.True,error);
        foreach(var original in data.inventory)
        {
            var restored=result.Inventory.Single(p=>p.Id==original.Id);
            Assert.That(restored.Health,Is.EqualTo(.6f));Assert.That(restored.FootprintRevision,Is.Zero);
            Assert.That(original.FootprintRevision,Is.Not.Zero,"Loading must not mutate the source checkpoint.");
        }
    }
    [Test] public void ReturningToLargeWheelSizeSafelyMovesSmallWheelsToInventory()
    {
        var design=new R3VehicleDesign{Name="future wheel resize",CoreHp=100};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.left,R3PartKind.Wheel,.6f){WheelStyle=R3WheelStyle.Monster,
            ContactMask=R3MountingGrid.FaceBit(Vector3Int.right)});
        var data=new R3VehicleModel(design).MakeCheckpoint(0);
        var original=data.mounted.Single(p=>p.Kind==R3PartKind.Wheel);
        R3VehicleModel.SetEquipmentScaleForEditorTests(true);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,null,0,out var result,out var error),Is.True,error);
        var recovered=result.Inventory.Single(p=>p.Id==original.Id);
        Assert.That(recovered.FootprintRevision,Is.EqualTo(1));Assert.That(recovered.Health,Is.EqualTo(.6f));
        Assert.That(original.FootprintRevision,Is.Zero);
    }
    [UnityTest] public IEnumerator AllStarterVehiclesKeepClearPhysicalGunsAtNormalSize()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return VerifyStarterVehicles();
        yield return new ExitPlayMode();
    }
    static IEnumerator VerifyStarterVehicles()
    {
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var palette=R1RunController.Instance.GetComponent<R3VisualPalette>();Assert.That(palette,Is.Not.Null);
        foreach(R3VehicleKind kind in System.Enum.GetValues(typeof(R3VehicleKind)))for(int variant=0;variant<4;variant++)
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(kind,variant));
            Assert.That(model.Parts.Where(p=>p.Kind==R3PartKind.Gun).All(p=>p.FootprintRevision==0),Is.True);
            Assert.That(model.DeployError,Is.Null,kind+"/"+variant);
            var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(1000,20,1000),Quaternion.identity);
            var assembly=vehicle.GetComponent<R3VehicleAssembly>();Physics.SyncTransforms();
            foreach(var gun in assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun))
                Assert.That(assembly.ClearProjectilePath(gun),Is.True,kind+"/"+variant+": starter gun is blocked by the vehicle");
            foreach(var gun in assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun))
            foreach(var a in gun.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))
            foreach(var other in assembly.Parts.Where(p=>p.Id!=gun.Id))
            foreach(var b in other.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))
                if(a.bounds.Intersects(b.bounds)&&Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out _,out float depth))
                    Assert.That(depth,Is.LessThan(.002f),kind+"/"+variant+": weapon intersects another part");
            Object.Destroy(vehicle.gameObject);yield return null;
        }
    }
    [UnityTest] public IEnumerator ResizedSideWheelsHaveRealClearCollidersOnBothSides()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return VerifySideWheels();
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator NormalSizeGunMountsAndFiresInRealGameplay()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return VerifyNormalGun();
        yield return new ExitPlayMode();
    }
    static IEnumerator VerifyNormalGun()
    {
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light,0),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(1000);
        var anchor=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Gun).Cell;
        foreach(var part in run.VehicleModel.Parts.Where(p=>p.Kind==R3PartKind.Gun).ToArray())
        {Assert.That(run.GarageRemove(part.Cell),Is.True);yield return null;}
        Assert.That(run.GarageBuyWeapon(anchor,R4WeaponKind.Basic),Is.True);
        var state=run.VehicleModel.At(anchor);Assert.That(state.FootprintRevision,Is.Zero);
        var installed=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
        Assert.That(installed.GetComponent<BoxCollider>().bounds.size.x,Is.LessThan(.6f));
        Assert.That(installed.GetComponent<R4WeaponArt>(),Is.Not.Null);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();installed=assembly.Parts.Single(p=>p.Id==state.Id);
        run.Player.GetComponent<Rigidbody>().isKinematic=true;run.Player.GetComponent<P12PipelineSafeTankController>().enabled=false;
        run.Player.transform.SetPositionAndRotation(new Vector3(1000,20,1000),Quaternion.identity);
        installed.GunPivot.rotation=Quaternion.identity;installed.GetComponent<R4WeaponArt>().Sync();
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=installed.Muzzle.position+installed.Muzzle.forward*5;
        target.transform.localScale=new Vector3(2,2,.5f);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();float energy=bank.Current;
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(installed),Is.True);
        assembly.FireGroup(false);Assert.That(bank.Current,Is.EqualTo(energy).Within(.001f));
        float deadline=Time.realtimeSinceStartup+5;while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(960).Within(.01f));Object.Destroy(target);
    }
    static IEnumerator VerifySideWheels()
    {
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run,Is.Not.Null);
        var palette=run.GetComponent<R3VisualPalette>();Assert.That(palette,Is.Not.Null);
        foreach(var style in new[]{R3WheelStyle.LargeOffroad,R3WheelStyle.Monster})
        foreach(int side in new[]{-1,1})
        {
            var design=new R3VehicleDesign{Name="wheel physical QA",CoreHp=100};
            design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
            design.Parts.Add(new R3PartState(Vector3Int.right*side,R3PartKind.Wheel){WheelStyle=style,
                ContactMask=R3MountingGrid.FaceBit(side<0?Vector3Int.right:Vector3Int.left)});
            var vehicle=R3VehicleAssembly.Build(new R3VehicleModel(design),new R1RunStats(),palette,
                new Vector3(1000,20,1000),Quaternion.identity);
            var assembly=vehicle.GetComponent<R3VehicleAssembly>();var wheel=assembly.Parts.Single(p=>p.Kind==R3PartKind.Wheel);
            var core=assembly.Parts.Single(p=>p.Kind==R3PartKind.Core);
            var tire=wheel.GetComponentsInChildren<Collider>().First(c=>c.enabled&&!c.isTrigger&&c.name=="Koło");
            Assert.That(tire.bounds.extents.y,Is.EqualTo(.39f).Within(.02f));
            foreach(var other in core.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))
                if(Physics.ComputePenetration(tire,tire.transform.position,tire.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float depth))
                    Assert.That(depth,Is.LessThan(.002f),style+" side "+side+": wheel intersects its support");
            Object.Destroy(vehicle.gameObject);yield return null;
        }
    }
}
