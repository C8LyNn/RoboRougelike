using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AutomaticScaleTwoModelTests
{
    [TestCase(R4WeaponKind.Automatic)]
    [TestCase(R4WeaponKind.Basic)]
    [TestCase(R4WeaponKind.Heavy)]
    [TestCase(R4WeaponKind.RocketLauncher)]
    [TestCase(R4WeaponKind.Shotgun)]
    [TestCase(R4WeaponKind.Railgun)]
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
        while(R1RunController.Instance?.Progress==null)yield return null;
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
