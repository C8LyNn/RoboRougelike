using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AutomaticScaleTwoModelTests
{
    [Test] public void NewAutomaticHasVersionedFootprintWhileLegacyRetainsOneCell()
    {
        var design=new R3VehicleDesign{Name="Automatic QA",CoreHp=100,Speed=7,TurnSpeed=82};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        var model=new R3VehicleModel(design);model.Award(1000);
        Assert.That(model.PurchaseToInventory(R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
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
        Assert.That(direct.Buy(anchor,R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        Assert.That(direct.At(anchor).FootprintRevision,Is.EqualTo(2),"Direct purchase must use the same footprint.");
    }

}

public sealed class AutomaticScaleTwoTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
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
        var first=art.Muzzle.position;art.AdvanceMuzzle();var second=art.Muzzle.position;
        Assert.That(Vector3.Distance(first,second),Is.GreaterThan(.20f),"Both scaled barrels stay distinct.");
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
