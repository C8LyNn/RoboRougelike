using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RoboRougelike.WeaponIntegration;
using RoboRougelike.Mechanics.EMP;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AutomaticScaleTwoModelTests
{
    [SetUp] public void UseEnlargedEquipment()=>R3VehicleModel.SetEquipmentScaleForEditorTests(true);
    [TearDown] public void RestoreNormalEquipment()=>R3VehicleModel.SetEquipmentScaleForEditorTests(false);
    [Test] public void DrawOffersSelectsThreeDistinctVehiclesFromTheEntirePool()
    {
        var method=typeof(R3VehicleCatalog).GetMethod("DrawOffers",new[]{typeof(System.Random)});
        Assert.That(method,Is.Not.Null);
        var seen=new System.Collections.Generic.HashSet<int>();bool repeatedCategory=false;
        for(int seed=0;seed<100;seed++)
        {
            var offers=(int[])method.Invoke(null,new object[]{new System.Random(seed)});
            Assert.That(offers.Length,Is.EqualTo(3));Assert.That(offers.Distinct().Count(),Is.EqualTo(3));
            Assert.That(offers.All(v=>v>=0&&v<12),Is.True);
            foreach(var v in offers)seen.Add(v);
            repeatedCategory|=offers.Select(v=>v/4).Distinct().Count()<3;
        }
        Assert.That(seen.Count,Is.EqualTo(12));Assert.That(repeatedCategory,Is.True,"Offers can include multiple vehicles from one category.");
    }
    [Test] public void TwelveStarterBlueprintsAreDistinctConnectedAndScaled()
    {
        var create=typeof(R3VehicleCatalog).GetMethod("Create",new[]{typeof(R3VehicleKind),typeof(int)});
        Assert.That(create,Is.Not.Null,"Each category needs four selectable starter designs.");
        var signatures=new System.Collections.Generic.HashSet<string>();
        foreach(R3VehicleKind kind in System.Enum.GetValues(typeof(R3VehicleKind)))
        for(int variant=0;variant<4;variant++)
        {
            var design=(R3VehicleDesign)create.Invoke(null,new object[]{kind,variant});
            Assert.That(design.Parts.Count(p=>p.Kind==R3PartKind.Core),Is.EqualTo(1));
            Assert.That(design.Parts.Where(p=>p.Kind==R3PartKind.Gun).All(p=>p.FootprintRevision==2),Is.True);
            foreach(var wheel in design.Parts.Where(p=>p.Kind==R3PartKind.Wheel))
                Assert.That(wheel.ContactMask,Is.EqualTo(R3MountingGrid.FaceBit(wheel.Cell.x<0?Vector3Int.right:Vector3Int.left)));
            var cells=design.Parts.SelectMany(R3VehicleModel.OccupiedCells).ToArray();
            Assert.That(cells.Distinct().Count(),Is.EqualTo(cells.Length),kind+"/"+variant);
            Assert.That(cells.Length,Is.LessThanOrEqualTo(180));
            Assert.That(R3VehicleModel.Connected(design.Parts),Is.True,kind+"/"+variant);
            Assert.That(new R3VehicleModel(design).DeployError,Is.Null,kind+"/"+variant);
            string signature=string.Join(";",design.Parts.Select(p=>$"{p.Kind}:{p.Cell.x},{p.Cell.y},{p.Cell.z}:{p.Weapon}").OrderBy(s=>s));
            Assert.That(signatures.Add(signature),Is.True,"Every vehicle needs its own construction.");
        }
        var balance=typeof(R3VehicleCatalog).GetMethod("StartingScrap",new[]{typeof(R3VehicleKind)});
        Assert.That(balance,Is.Not.Null);
        int light=(int)balance.Invoke(null,new object[]{R3VehicleKind.Light});
        int medium=(int)balance.Invoke(null,new object[]{R3VehicleKind.Standard});
        int heavy=(int)balance.Invoke(null,new object[]{R3VehicleKind.Heavy});
        Assert.That(heavy,Is.Zero);Assert.That(medium,Is.GreaterThan(0));Assert.That(light,Is.GreaterThan(medium));
    }
    [Test] public void LoadingLegacyMountedGunMovesItSafelyIntoInventoryAtModernScale()
    {
        var design=new R3VehicleDesign{Name="Old save",CoreHp=100,Speed=7,TurnSpeed=82};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.up,R3PartKind.Gun,.45f){FootprintRevision=0,ContactMask=R3MountingGrid.FaceBit(Vector3Int.down)});
        var model=new R3VehicleModel(design);var original=model.Parts.Single(p=>p.Kind==R3PartKind.Gun);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,null,0,out var loaded,out var error),Is.True,error);
        var recovered=loaded.Inventory.Single(p=>p.Id==original.Id);
        Assert.That(recovered.FootprintRevision,Is.EqualTo(2));Assert.That(recovered.Health,Is.EqualTo(.45f));
        Assert.That(loaded.Parts.Any(p=>p.Id==original.Id),Is.False);
        Assert.That(data.mounted.Single(p=>p.Id==original.Id).FootprintRevision,Is.Zero,"Migration must not edit the source document.");
    }
    [Test] public void FullInventoryKeepsOldMountedGunAndSaveIntactUntilThereIsSpace()
    {
        var design=new R3VehicleDesign{Name="Full inventory",CoreHp=100,Speed=7,TurnSpeed=82};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.up,R3PartKind.Gun){ContactMask=R3MountingGrid.FaceBit(Vector3Int.down)});
        var data=new R3VehicleModel(design).MakeCheckpoint(0);var id=data.mounted.Single(p=>p.Kind==R3PartKind.Gun).Id;
        data.inventory=Enumerable.Range(0,R3VehicleModel.InventoryCapacity).Select(_=>new R3PartState(Vector3Int.zero,R3PartKind.Armor){DefenseRechargeAge=float.MaxValue}).ToArray();
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,null,0,out var loaded,out var error),Is.True,error);
        Assert.That(loaded.LegacyGunScalePending,Is.EqualTo(1));
        Assert.That(loaded.Parts.Single(p=>p.Id==id).FootprintRevision,Is.Zero);
        Assert.That(loaded.Inventory.Count,Is.EqualTo(R3VehicleModel.InventoryCapacity));
        Assert.That(data.mounted.Single(p=>p.Id==id).FootprintRevision,Is.Zero);
    }
    [Test] public void RecoveredAndRemovedLegacyGunsAdoptModernSizeWithoutLosingIdentity()
    {
        var design=new R3VehicleDesign{Name="Migration",CoreHp=100,Speed=7,TurnSpeed=82};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        var old=new R3PartState(Vector3Int.up,R3PartKind.Gun,.45f){FootprintRevision=0};
        design.Parts.Add(old);var model=new R3VehicleModel(design);var id=model.At(old.Cell).Id;
        Assert.That(model.Remove(old.Cell),Is.True);
        var stored=model.Inventory.Single(p=>p.Id==id);
        Assert.That(stored.FootprintRevision,Is.EqualTo(2));Assert.That(stored.Health,Is.EqualTo(.45f));
        var salvage=new R3PartState(Vector3Int.zero,R3PartKind.Gun,.3f){FootprintRevision=0};
        Assert.That(model.ReceiveSalvage(new[]{salvage}),Is.True);
        var recovered=model.Inventory.Single(p=>p.Id==salvage.Id);
        Assert.That(recovered.FootprintRevision,Is.EqualTo(2));Assert.That(recovered.Health,Is.EqualTo(.3f));
        var save=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        save.inventory.Single(p=>p.Id==id).FootprintRevision=0;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(save,R3VehicleKind.Light,null,0,out var loaded,out var error),Is.True,error);
        Assert.That(loaded.Inventory.Single(p=>p.Id==id).FootprintRevision,Is.EqualTo(2));
        Assert.That(save.inventory.Single(p=>p.Id==id).FootprintRevision,Is.Zero,"Loading must not mutate the original checkpoint.");
    }
    [TestCase(R3VehicleKind.Light)]
    [TestCase(R3VehicleKind.Standard)]
    [TestCase(R3VehicleKind.Heavy)]
    public void FactoryVehiclesStartWithEnlargedGunsAndNonOverlappingCells(R3VehicleKind kind)
    {
        var design=R3VehicleCatalog.Create(kind);
        var guns=design.Parts.Where(p=>p.Kind==R3PartKind.Gun).ToArray();
        Assert.That(guns,Is.Not.Empty);
        Assert.That(guns.All(p=>p.FootprintRevision==2),Is.True,"Starter guns use the same physical scale as purchased guns.");
        var cells=design.Parts.SelectMany(R3VehicleModel.OccupiedCells).ToArray();
        Assert.That(cells.Distinct().Count(),Is.EqualTo(cells.Length));
        Assert.That(R3VehicleModel.Connected(design.Parts),Is.True);
        Assert.That(new R3VehicleModel(design).DeployError,Is.Null);
    }
    [TestCase(R4WeaponKind.Automatic)]
    [TestCase(R4WeaponKind.Basic)]
    [TestCase(R4WeaponKind.Heavy)]
    [TestCase(R4WeaponKind.RocketLauncher)]
    [TestCase(R4WeaponKind.Shotgun)]
    [TestCase(R4WeaponKind.Railgun)]
    [TestCase(R4WeaponKind.BeamCutter)]
    [TestCase(R4WeaponKind.EmpEmitter)]
    [TestCase(R4WeaponKind.RotarySaw)]
    public void NewGunHasVersionedFootprintWhileLegacyRetainsOneCell(R4WeaponKind weapon)
    {
        var design=new R3VehicleDesign{Name="Automatic QA",CoreHp=100,Speed=7,TurnSpeed=82};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        var model=new R3VehicleModel(design);model.Award(1000);
        Assert.That(model.PurchaseToInventory(R3PartKind.Gun,weapon),Is.True);
        var item=model.Inventory.Single();var old=item.Copy();old.FootprintRevision=0;
        Assert.That(item.FootprintRevision,Is.EqualTo(2));
        Assert.That(R3VehicleModel.OccupiedCells(item).Distinct().Count(),Is.EqualTo(18));
        Assert.That(R3VehicleModel.OccupiedCells(old).Count(),Is.EqualTo(1));
        Assert.That(R3VehicleModel.SameFusionType(old,item),Is.False);
        var anchor=Vector3Int.up;
        Assert.That(model.OwnedPlacementError(item.Id,anchor,Vector3Int.zero),Is.Null);
        Assert.That(model.Mount(item.Id,anchor),Is.True);
        Assert.That(model.At(anchor+Vector3Int.right+Vector3Int.up).Id,Is.EqualTo(item.Id));
        var installed=model.At(anchor);
        Assert.That(R3MountingGrid.PortOnFace(installed,anchor+Vector3Int.right,Vector3Int.right),Is.False,
            "An occupied side cell is not an extra mounting socket.");
        Assert.That(R3MountingGrid.PortOnFace(installed,anchor,Vector3Int.down),Is.True);
        var save=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(save,R3VehicleKind.Light,null,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.At(anchor+Vector3Int.right+Vector3Int.up).Id,Is.EqualTo(item.Id));
        var direct=new R3VehicleModel(design);direct.Award(1000);
        Assert.That(direct.Buy(anchor,R3PartKind.Gun,weapon),Is.True);
        Assert.That(direct.At(anchor).FootprintRevision,Is.EqualTo(2),"Direct purchase must use the same footprint.");
    }

}

public sealed class AutomaticScaleTwoTests
{
    static void AssertPhysicalBarrelMatchesVisibleMuzzle(R3MountedPart gun)
    {
        var physicalMuzzle=gun.GunPivot.Find("Wylot");Assert.That(physicalMuzzle,Is.Not.Null);
        var axis=gun.GunPivot.forward;
        float physical=Vector3.Dot(physicalMuzzle.position-gun.GunPivot.position,axis);
        float visible=Vector3.Dot(gun.Muzzle.position-gun.GunPivot.position,axis);
        Assert.That(physical,Is.EqualTo(visible).Within(.02f),
            "The placement/repair barrel proxy must end at the real visible muzzle.");
    }
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        R3VehicleModel.SetEquipmentScaleForEditorTests(true);
        while(R1RunController.Instance?.Progress==null)yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        R3VehicleModel.SetEquipmentScaleForEditorTests(false);
        if(Application.isPlaying)yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator StarterLightLargeGunsRemainPhysicallyClear()=>VerifyStarterPhysicalGuns(R3VehicleKind.Light);
    [UnityTest] public IEnumerator StarterStandardLargeGunsRemainPhysicallyClear()=>VerifyStarterPhysicalGuns(R3VehicleKind.Standard);
    [UnityTest] public IEnumerator StarterHeavyLargeGunsRemainPhysicallyClear()=>VerifyStarterPhysicalGuns(R3VehicleKind.Heavy);
    [UnityTest] public IEnumerator LightVariantStartsWithItsRealBudget()=>VerifyStarterBudget(R3VehicleKind.Light,200);
    [UnityTest] public IEnumerator StandardVariantStartsWithItsRealBudget()=>VerifyStarterBudget(R3VehicleKind.Standard,75);
    [UnityTest] public IEnumerator HeavyVariantStartsWithItsRealBudget()=>VerifyStarterBudget(R3VehicleKind.Heavy,0);
    [UnityTest] public IEnumerator MainMenuOffersThreeRandomVehiclesAndSelectedOfferEntersGarage()
    {
        var run=R1RunController.Instance;var hud=run.GetComponent<R1RunHUD>();Assert.That(hud,Is.Not.Null);
        var field=typeof(R1RunHUD).GetField("starterOffers",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(field,Is.Not.Null);
        float deadline=Time.realtimeSinceStartup+5;int[] offers=null;
        while(offers==null&&Time.realtimeSinceStartup<deadline){yield return null;offers=(int[])field.GetValue(hud);}
        Assert.That(offers,Is.Not.Null);Assert.That(offers.Length,Is.EqualTo(3));Assert.That(offers.Distinct().Count(),Is.EqualTo(3));
        int offer=offers[2];var kind=(R3VehicleKind)(offer/4);int variant=offer%4;
        Assert.That(run.SelectVehicle(kind,variant),Is.True);Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        Assert.That(run.VehicleModel.Design.Name,Is.EqualTo(R3VehicleCatalog.Create(kind,variant).Name));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(R3VehicleCatalog.StartingScrap(kind)));
    }
    [UnityTest] public IEnumerator InitialMenuSelectionIsAnOfferedVehicleBeforeRendering()
    {
        var run=R1RunController.Instance;var hud=run.GetComponent<R1RunHUD>();
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        int[] offers={4,8,9};
        typeof(R1RunHUD).GetField("starterOffers",flags).SetValue(hud,offers);
        typeof(R1RunHUD).GetField("starterDesigns",flags).SetValue(hud,offers.Select(id=>R3VehicleCatalog.Create((R3VehicleKind)(id/4),id%4)).ToArray());
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard,0),Is.True);
        var refresh=typeof(R1RunHUD).GetMethod("RefreshStarterOffers",flags);refresh.Invoke(hud,new object[]{run});
        Assert.That(run.SelectedVehicle,Is.EqualTo(R3VehicleKind.Light));Assert.That(run.SelectedVehicleVariant,Is.Zero);
        Assert.That(run.SelectVehicle(R3VehicleKind.Heavy,1),Is.True);refresh.Invoke(hud,new object[]{run});
        Assert.That(run.SelectedVehicle,Is.EqualTo(R3VehicleKind.Heavy));Assert.That(run.SelectedVehicleVariant,Is.EqualTo(1));
        var caption=typeof(R1RunHUD).GetMethod("StarterActionCaption",flags);Assert.That(caption,Is.Not.Null);
        Assert.That((string)caption.Invoke(hud,new object[]{run}),Does.Contain(R3VehicleCatalog.Create(R3VehicleKind.Heavy,1).Name));
        Assert.That(run.PrepareUnifiedRun(),Is.True);
        Assert.That(run.VehicleModel.Design.Name,Is.EqualTo(R3VehicleCatalog.Create(R3VehicleKind.Heavy,1).Name));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(R3VehicleCatalog.StartingScrap(R3VehicleKind.Heavy)));
        yield return null;
    }
    [UnityTest] public IEnumerator AllTwelveVariantsBuildWithPhysicallyClearGuns()
    {
        var palette=R1RunController.Instance.GetComponent<R3VisualPalette>();Assert.That(palette,Is.Not.Null);
        foreach(R3VehicleKind kind in System.Enum.GetValues(typeof(R3VehicleKind)))
        for(int variant=0;variant<4;variant++)
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(kind,variant));
            var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(1000,20,1000),Quaternion.identity);
            var assembly=vehicle.GetComponent<R3VehicleAssembly>();Physics.SyncTransforms();
            foreach(var gun in assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun))
            foreach(var a in gun.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))
            foreach(var other in assembly.Parts.Where(p=>p.Id!=gun.Id))
            foreach(var b in other.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))
                if(a.bounds.Intersects(b.bounds)&&Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out _,out var depth))
                    Assert.That(depth,Is.LessThan(.002f),$"{kind}/{variant}: {gun.Cell}/{a.name} intersects {other.Cell}/{b.name}.");
            Object.Destroy(vehicle.gameObject);yield return null;
        }
    }
    [UnityTest] public IEnumerator AllTwelveVariantsDriveWithRealWheelContacts()
    {
        var palette=R1RunController.Instance.GetComponent<R3VisualPalette>();Assert.That(palette,Is.Not.Null);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.5f,1000);floor.transform.localScale=new Vector3(100,1,100);
        Time.timeScale=1;
        foreach(R3VehicleKind kind in System.Enum.GetValues(typeof(R3VehicleKind)))
        for(int variant=0;variant<4;variant++)
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(kind,variant));
            var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,.39f,1000),Quaternion.identity);
            vehicle.Controller.SetGameplayInputEnabled(true);vehicle.Controller.enabled=false;
            var body=vehicle.GetComponent<Rigidbody>();body.position=new Vector3(0,R3VehicleModel.WheelRadius(R3WheelStyle.Offroad),1000);
            body.useGravity=false;body.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
            foreach(var component in vehicle.GetComponents<MonoBehaviour>())if(component.GetType().Name=="R6CoreRecovery")component.enabled=false;
            var drive=vehicle.GetComponent<RoboRougelike.R6.R6VehicleDrive>();Physics.SyncTransforms();
            for(int i=0;i<20&&drive.GroundedWheels==0;i++){drive.Step(0,0,10,5,90);yield return MOVSeriesDeviceTests.FixedStep();}
            Assert.That(drive.GroundedWheels,Is.GreaterThan(0),$"{kind}/{variant}: wheels must touch the ground.");
            float start=body.position.z;
            for(int i=0;i<50;i++){drive.Step(1,0,10,5,90);yield return MOVSeriesDeviceTests.FixedStep();}
            Assert.That(body.position.z-start,Is.GreaterThan(.1f),$"{kind}/{variant}: real wheel drive must move the vehicle.");
            Object.Destroy(vehicle.gameObject);yield return null;
        }
        Object.Destroy(floor);
    }
    IEnumerator VerifyStarterBudget(R3VehicleKind kind,int expected)
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(kind,3),Is.True);
        Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(expected));
        Assert.That(run.SelectedVehicleVariant,Is.EqualTo(3));
        Assert.That(run.VehicleModel.Design.Name,Is.EqualTo(R3VehicleCatalog.Create(kind,3).Name));
    }
    IEnumerator VerifyStarterPhysicalGuns(R3VehicleKind kind)
    {
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(kind),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;Physics.SyncTransforms();
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        foreach(var gun in assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun))
        {
            Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==gun.Id).FootprintRevision,Is.EqualTo(2));
            var own=gun.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
            foreach(var other in assembly.Parts.Where(p=>p.Id!=gun.Id))
            foreach(var a in own)
            foreach(var b in other.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))
                if(a.bounds.Intersects(b.bounds)&&Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out _,out var depth))
                    Assert.That(depth,Is.LessThan(.002f),$"{kind}: {gun.Cell}/{a.name} intersects {other.Cell}/{b.name}.");
        }
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        Assert.That(run.Player.GetComponent<R3VehicleAssembly>().Parts.Count(p=>p.Kind==R3PartKind.Gun),Is.GreaterThan(0));
    }

    [UnityTest] public IEnumerator EnlargedBasicMountsAndFiresForFreeFromItsModel()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);
        foreach(var part in model.Parts.Where(p=>p.Kind==R3PartKind.Gun).ToArray())
        {Assert.That(run.GarageRemove(part.Cell),Is.True);yield return null;}
        var anchor=new Vector3Int(0,2,0);
        Assert.That(run.GarageBuyWeapon(anchor,R4WeaponKind.Basic),Is.True);
        var state=model.At(anchor);Assert.That(state.FootprintRevision,Is.EqualTo(2));
        Assert.That(R3VehicleModel.OccupiedCells(state).Count(),Is.EqualTo(18));
        var installed=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
        Assert.That(installed.GetComponent<BoxCollider>().bounds.size.x,Is.GreaterThan(1f));
        AssertPhysicalBarrelMatchesVisibleMuzzle(installed);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();installed=assembly.Parts.Single(p=>p.Id==state.Id);
        run.Player.GetComponent<Rigidbody>().isKinematic=true;
        run.Player.GetComponent<P12PipelineSafeTankController>().enabled=false;
        run.Player.transform.SetPositionAndRotation(new Vector3(1000,20,1000),Quaternion.identity);
        installed.GunPivot.rotation=Quaternion.identity;installed.GetComponent<R4WeaponArt>().Sync();
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.transform.position=installed.Muzzle.position+installed.Muzzle.forward*5;
        target.transform.localScale=new Vector3(2,2,.5f);
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();float energy=bank.Current;
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(installed),Is.True);
        assembly.FireGroup(false);Assert.That(bank.Current,Is.EqualTo(energy).Within(.001f));
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(960).Within(.01f));Object.Destroy(target);
    }

    [UnityTest] public IEnumerator EnlargedBasicRejectsRealBarrelOverlapButAllowsSpaceBeyondMuzzle()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);
        foreach(var part in model.Parts.Where(p=>p.Kind==R3PartKind.Gun).ToArray())
        {Assert.That(run.GarageRemove(part.Cell),Is.True);yield return null;}
        var anchor=new Vector3Int(0,2,0);var root=run.Player.transform;
        var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.SetParent(root,false);
        obstacle.transform.localScale=Vector3.one*.08f;
        var barrelOrigin=(Vector3)anchor*R41PartGeometry.Grid+Vector3.up*.53f;
        obstacle.transform.localPosition=barrelOrigin+Vector3.forward*1.0f;Physics.SyncTransforms();
        int before=model.Scrap;
        Assert.That(run.GarageBuyWeapon(anchor,R4WeaponKind.Basic),Is.False,"A solid object inside the visible barrel prevents placement.");
        Assert.That(model.Scrap,Is.EqualTo(before));
        obstacle.transform.localPosition=barrelOrigin+Vector3.forward*1.55f;Physics.SyncTransforms();
        Assert.That(run.GarageBuyWeapon(anchor,R4WeaponKind.Basic),Is.True,"Empty space beyond the visible muzzle is not part of the physical barrel.");
        Object.Destroy(obstacle);
    }

    [UnityTest] public IEnumerator EnlargedHeavyMountsAndFiresWithItsNormalCost()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);
        foreach(var part in model.Parts.Where(p=>p.Kind==R3PartKind.Gun).ToArray())
        {Assert.That(run.GarageRemove(part.Cell),Is.True);yield return null;}
        var anchor=new Vector3Int(0,2,0);
        Assert.That(run.GarageBuyWeapon(anchor,R4WeaponKind.Heavy),Is.True);
        var state=model.At(anchor);Assert.That(state.FootprintRevision,Is.EqualTo(2));
        Assert.That(R3VehicleModel.OccupiedCells(state).Count(),Is.EqualTo(18));
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var installed=assembly.Parts.Single(p=>p.Id==state.Id);
        Assert.That(installed.GetComponent<BoxCollider>().bounds.size.x,Is.GreaterThan(1f));
        AssertPhysicalBarrelMatchesVisibleMuzzle(installed);
        run.Player.GetComponent<Rigidbody>().isKinematic=true;run.Player.GetComponent<P12PipelineSafeTankController>().enabled=false;
        run.Player.transform.SetPositionAndRotation(new Vector3(1000,20,1000),Quaternion.identity);
        installed.GunPivot.rotation=Quaternion.identity;installed.GetComponent<R4WeaponArt>().Sync();
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=installed.Muzzle.position+installed.Muzzle.forward*5;
        target.transform.localScale=new Vector3(2,2,.5f);
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();float energy=bank.Current;
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(installed),Is.True);
        assembly.FireGroup(false);Assert.That(bank.Current,Is.EqualTo(energy-2.5f).Within(.001f));
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(900).Within(.01f));Object.Destroy(target);
    }

    [UnityTest] public IEnumerator EnlargedRocketMountsHitsAndAutomaticallyReloads()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);
        foreach(var part in model.Parts.Where(p=>p.Kind==R3PartKind.Gun).ToArray())
        {Assert.That(run.GarageRemove(part.Cell),Is.True);yield return null;}
        var anchor=new Vector3Int(0,2,0);
        Assert.That(run.GarageBuyWeapon(anchor,R4WeaponKind.RocketLauncher),Is.True);
        var state=model.At(anchor);Assert.That(state.FootprintRevision,Is.EqualTo(2));
        Assert.That(R3VehicleModel.OccupiedCells(state).Count(),Is.EqualTo(18));
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var installed=assembly.Parts.Single(p=>p.Id==state.Id);
        AssertPhysicalBarrelMatchesVisibleMuzzle(installed);
        run.Player.GetComponent<Rigidbody>().isKinematic=true;run.Player.GetComponent<P12PipelineSafeTankController>().enabled=false;
        run.Player.transform.SetPositionAndRotation(new Vector3(1000,20,1000),Quaternion.identity);
        installed.GunPivot.rotation=Quaternion.identity;installed.GetComponent<R4WeaponArt>().Sync();
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=installed.Muzzle.position+installed.Muzzle.forward*5;
        target.transform.localScale=new Vector3(2,2,.5f);
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var binding=installed.GetComponent<RRLRocketBinding>();Assert.That(binding,Is.Not.Null);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();float energy=bank.Current;
        Physics.SyncTransforms();Assert.That(binding.Fire(),Is.True);
        Assert.That(bank.Current,Is.EqualTo(energy-7.5f).Within(.001f));Assert.That(binding.State.Ammo,Is.EqualTo(9));
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.LessThan(1000),"The actual rocket explosion damages the target.");
        binding.State.Ammo=0;yield return null;
        Assert.That(binding.State.ReloadReadyAt,Is.GreaterThan(Time.time),"Empty launcher starts reloading automatically.");
        deadline=Time.realtimeSinceStartup+5;
        while(binding.State.Ammo==0&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(binding.State.Ammo,Is.EqualTo(RRLRocketBinding.Magazine));Object.Destroy(target);
    }

    [UnityTest] public IEnumerator EnlargedShotgunMountsAndHits() => VerifyPackageGun(R4WeaponKind.Shotgun,5f);
    [UnityTest] public IEnumerator EnlargedRailgunMountsAndHits() => VerifyPackageGun(R4WeaponKind.Railgun,9f);

    IEnumerator VerifyPackageGun(R4WeaponKind weapon,float cost)
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);
        foreach(var part in model.Parts.Where(p=>p.Kind==R3PartKind.Gun).ToArray())
        {Assert.That(run.GarageRemove(part.Cell),Is.True);yield return null;}
        var anchor=new Vector3Int(0,2,0);
        Assert.That(run.GarageBuyWeapon(anchor,weapon),Is.True);
        var state=model.At(anchor);Assert.That(state.FootprintRevision,Is.EqualTo(2));
        Assert.That(R3VehicleModel.OccupiedCells(state).Count(),Is.EqualTo(18));
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var installed=assembly.Parts.Single(p=>p.Id==state.Id);
        AssertPhysicalBarrelMatchesVisibleMuzzle(installed);
        run.Player.GetComponent<Rigidbody>().isKinematic=true;run.Player.GetComponent<P12PipelineSafeTankController>().enabled=false;
        run.Player.transform.SetPositionAndRotation(new Vector3(1000,20,1000),Quaternion.identity);
        installed.GunPivot.rotation=Quaternion.identity;installed.GetComponent<R4WeaponArt>().Sync();
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=installed.Muzzle.position+installed.Muzzle.forward*5;
        target.transform.localScale=new Vector3(4,4,.5f);
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();float energy=bank.Current;
        Physics.SyncTransforms();
        bool fired=weapon==R4WeaponKind.Shotgun?installed.GetComponent<RRLShotgunBinding>().Fire():installed.GetComponent<RRLRailgunBinding>().Fire();
        Assert.That(fired,Is.True);Assert.That(bank.Current,Is.EqualTo(energy-cost).Within(.001f));
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.LessThan(1000),"The actual package projectile hits.");
        int ammo=weapon==R4WeaponKind.Shotgun?installed.GetComponent<RRLShotgunBinding>().State.Ammo:installed.GetComponent<RRLRailgunBinding>().State.Ammo;
        Assert.That(ammo,Is.EqualTo(11));Object.Destroy(target);
    }

    [UnityTest] public IEnumerator EnlargedBeamHitsAndConsumesEnergy() => VerifyBeamOrEmp(R4WeaponKind.BeamCutter);
    [UnityTest] public IEnumerator EnlargedEmpDisruptsRealReceiver() => VerifyBeamOrEmp(R4WeaponKind.EmpEmitter);

    [UnityTest] public IEnumerator EnlargedSawMountsAndCutsAtItsVisibleExtendedRadius()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);
        foreach(var part in model.Parts.Where(p=>p.Kind==R3PartKind.Gun).ToArray())
        {Assert.That(run.GarageRemove(part.Cell),Is.True);yield return null;}
        Assert.That(run.GarageBuy(new Vector3Int(0,2,0),R3PartKind.Armor),Is.True);yield return null;
        Assert.That(run.GarageBuy(new Vector3Int(0,3,0),R3PartKind.Armor),Is.True);yield return null;
        var anchor=new Vector3Int(0,4,0);
        Assert.That(run.GarageBuyWeapon(anchor,R4WeaponKind.RotarySaw),Is.True,"A raised support permits the disc to clear the vehicle.");
        var state=model.At(anchor);Assert.That(state.FootprintRevision,Is.EqualTo(2));
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var installed=assembly.Parts.Single(p=>p.Id==state.Id);
        run.Player.GetComponent<Rigidbody>().isKinematic=true;run.Player.GetComponent<P12PipelineSafeTankController>().enabled=false;
        run.Player.transform.SetPositionAndRotation(new Vector3(1000,20,1000),Quaternion.identity);
        installed.GunPivot.rotation=Quaternion.identity;var art=installed.GetComponent<R4WeaponArt>();art.Sync();
        var binding=installed.GetComponent<RRLSawBinding>();Assert.That(binding.Module.ContactRadius,Is.EqualTo(1.8f).Within(.001f));
        Assert.That(Vector3.Distance(art.SawRotor.position,installed.Muzzle.position),Is.LessThan(.01f));
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.localScale=Vector3.one*.2f;
        target.transform.position=installed.Muzzle.position+installed.Muzzle.up*1.4f;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);Physics.SyncTransforms();
        binding.Module.SetMotorRequested(true);for(int i=0;i<12;i++)binding.Module.Advance(.1f);
        Assert.That(hp.CurrentHp,Is.LessThan(500),"The actual blade damages targets beyond the old .9 m radius.");
        Object.Destroy(target);
    }

    IEnumerator VerifyBeamOrEmp(R4WeaponKind weapon)
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);
        foreach(var part in model.Parts.Where(p=>p.Kind==R3PartKind.Gun).ToArray())
        {Assert.That(run.GarageRemove(part.Cell),Is.True);yield return null;}
        var anchor=new Vector3Int(0,2,0);Assert.That(run.GarageBuyWeapon(anchor,weapon),Is.True);
        var state=model.At(anchor);Assert.That(state.FootprintRevision,Is.EqualTo(2));
        Assert.That(R3VehicleModel.OccupiedCells(state).Count(),Is.EqualTo(18));
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var installed=assembly.Parts.Single(p=>p.Id==state.Id);
        run.Player.GetComponent<Rigidbody>().isKinematic=true;run.Player.GetComponent<P12PipelineSafeTankController>().enabled=false;
        run.Player.transform.SetPositionAndRotation(new Vector3(1000,20,1000),Quaternion.identity);
        installed.GunPivot.rotation=Quaternion.identity;installed.GetComponent<R4WeaponArt>().Sync();
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.localScale=new Vector3(2,2,.5f);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();float energy=bank.Current;
        if(weapon==R4WeaponKind.BeamCutter)
        {
            AssertPhysicalBarrelMatchesVisibleMuzzle(installed);
            target.transform.position=installed.Muzzle.position+installed.Muzzle.forward*4;
            var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);Physics.SyncTransforms();
            var binding=installed.GetComponent<RRLBeamBinding>();binding.Module.SetTriggerHeld(true);
            var result=binding.Module.ManualFixedStep(.1f);
            Assert.That(result.Status.ToString(),Is.EqualTo("Firing"));
            Assert.That(hp.CurrentHp,Is.EqualTo(496.5f).Within(.01f));
            Assert.That(bank.Current,Is.EqualTo(energy-.6f).Within(.01f));
        }
        else
        {
            Assert.That(installed.GunPivot.GetComponentsInChildren<Collider>().Any(c=>c.enabled),Is.False,
                "EMP has no physical gun barrel that could block neighboring parts.");
            target.transform.position=installed.transform.position+Vector3.forward*3;
            var receiver=target.AddComponent<EmpReceiver>();receiver.ConfigureOwnership(target.transform,12345,2);Physics.SyncTransforms();
            Assert.That(installed.GetComponent<RRLEmpBinding>().Fire(),Is.True);
            Assert.That(receiver.IsDisrupted,Is.True);Assert.That(bank.Current,Is.EqualTo(energy-12.5f).Within(.01f));
        }
        Object.Destroy(target);
    }

    [UnityTest] public IEnumerator RealGarageShowsAndMountsEnlargedAutomaticWithTwoWorkingMuzzles()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);
        foreach(var part in model.Parts.Where(p=>p.Kind==R3PartKind.Gun).ToArray())
        {Assert.That(run.GarageRemove(part.Cell),Is.True);yield return null;}
        var anchor=new Vector3Int(0,2,0);
        Assert.That(run.GarageBuyWeapon(anchor,R4WeaponKind.Automatic),Is.True,"Direct garage purchase must use the real enlarged collider.");
        Assert.That(model.At(anchor).FootprintRevision,Is.EqualTo(2));
        Assert.That(run.GarageRemove(anchor),Is.True);yield return null;
        Assert.That(run.GaragePurchase(R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        yield return null;
        Assert.That(run.VehicleModel,Is.SameAs(model));
        var items=model.Inventory;Assert.That(items,Is.Not.Null);Assert.That(items.Count,Is.GreaterThan(0));
        var gun=items[items.Count-1];
        Assert.That(gun,Is.Not.Null);
        var garage=run.Garage;Assert.That(garage,Is.Not.Null);
        garage.ChooseInventory(gun.Id);
        var socket=garage.AvailableMountPoints.First(p=>
            p.Surface.GetComponentInParent<R3MountedPart>()?.Cell==anchor+Vector3Int.down&&
            Vector3.Dot(p.Rotation*Vector3.forward,run.Player.transform.up)>.99f);
        Assert.That(garage.Point(new Ray(socket.Position+run.Player.transform.up*3,-run.Player.transform.up)),Is.True,
            "The actual pointer can select the displayed mounting socket.");
        Assert.That(garage.Cell,Is.EqualTo(anchor));
        Assert.That(garage.PreviewValid,Is.True,garage.PlacementIssue);
        Assert.That(garage.CandidateOccupiedCells().Count(),Is.EqualTo(18));
        Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        var actual=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==gun.Id);
        Assert.That(actual.GetComponent<R3BlockMountPoints>().GridOffsets.Count(),Is.EqualTo(18));
        var bounds=actual.GetComponent<BoxCollider>().bounds;
        Assert.That(bounds.size.x,Is.GreaterThan(1f));Assert.That(bounds.size.z,Is.GreaterThan(1f));
        var art=actual.GetComponent<R4WeaponArt>();Assert.That(art,Is.Not.Null);
        AssertPhysicalBarrelMatchesVisibleMuzzle(actual);
        var barrelProxies=actual.GunPivot.GetComponentsInChildren<BoxCollider>();
        Assert.That(barrelProxies.Length,Is.EqualTo(2));
        var proxySpacing=Vector3.Distance(barrelProxies[0].bounds.center,barrelProxies[1].bounds.center);
        var first=art.Muzzle.position;art.AdvanceMuzzle();var second=art.Muzzle.position;
        Assert.That(Vector3.Distance(first,second),Is.GreaterThan(.20f),"Both scaled barrels stay distinct.");
        Assert.That(proxySpacing,Is.EqualTo(Vector3.Distance(first,second)).Within(.01f),"Physical barrels follow visible spacing.");
        Assert.That((first-actual.transform.position).magnitude,Is.GreaterThan(1.2f));
        Assert.That(model.Parts.Count(p=>p.Id==gun.Id),Is.EqualTo(1));
        Assert.That(run.GarageRotate(anchor,Vector3Int.up),Is.True,"A valid rotation keeps the one mounted part.");
        yield return null;
        actual=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==gun.Id);
        Assert.That(R3VehicleModel.OccupiedCells(model.Parts.Single(p=>p.Id==gun.Id)).Count(),Is.EqualTo(18));
        actual.Damageable.ApplyDamage(actual.Damageable.CurrentHp/.75f+1,actual.transform.position,Vector3.up);
        run.Player.GetComponent<R3VehicleAssembly>().Capture();
        Assert.That(model.Parts.Single(p=>p.Id==gun.Id).Health,Is.Zero);
        Assert.That(run.GarageRepair(anchor),Is.True,"The enlarged gun can be repaired at its original location.");
        yield return null;
        Assert.That(run.EnterTestRange(),Is.True);
        while(run.Transitioning)yield return null;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        actual=assembly.Parts.Single(p=>p.Id==gun.Id);
        var body=run.Player.GetComponent<Rigidbody>();body.isKinematic=true;
        // Hold the aim fixed while real projectiles travel to the test target.
        run.Player.GetComponent<P12PipelineSafeTankController>().enabled=false;
        run.Player.transform.SetPositionAndRotation(new Vector3(1000,20,1000),Quaternion.identity);
        actual.GunPivot.rotation=Quaternion.identity;
        actual.GetComponent<R4WeaponArt>().Sync();
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.transform.position=actual.Muzzle.position+actual.Muzzle.forward*5;
        target.transform.localScale=new Vector3(2,2,.5f);
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        Physics.SyncTransforms();
        Assert.That(assembly.ClearProjectilePath(actual),Is.True,"The real muzzle has an unobstructed path.");
        assembly.FireGroup(false);
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(988).Within(.01f),"A real projectile from the enlarged gun hits.");
        actual.NextFireTime=0;assembly.FireGroup(false);
        deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=988&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(976).Within(.01f),"The second barrel also fires a real projectile.");
        var blockedMuzzle=actual.Muzzle;
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.transform.SetParent(run.Player.transform,true);
        blocker.transform.position=blockedMuzzle.position+blockedMuzzle.forward*.7f;
        blocker.transform.localScale=Vector3.one*.35f;Physics.SyncTransforms();
        Assert.That(assembly.ClearProjectilePath(actual),Is.False,"Own armor must block the larger weapon's active barrel.");
        int shells=Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Length;
        actual.NextFireTime=0;assembly.FireGroup(false);
        Assert.That(Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Length,Is.EqualTo(shells));
        Assert.That(actual.Muzzle,Is.SameAs(blockedMuzzle),"A blocked shot must not advance to the other barrel.");
        Object.Destroy(blocker);
        Object.Destroy(target);
    }
}
