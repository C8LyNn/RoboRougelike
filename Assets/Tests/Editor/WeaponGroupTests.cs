using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;

public sealed class WeaponGroupTests
{
    [Test] public void ActiveSlotAssignmentsSurviveCheckpointRoundTrip()
    {
        var design=new R3VehicleDesign{Name="Active slots",CoreHp=100};
        for(int x=-3;x<=3;x++)design.Parts.Add(new R3PartState(new Vector3Int(x,0,0),x==0?R3PartKind.Core:R3PartKind.Armor));
        var kinds=new[]{R3PartKind.Booster,R3PartKind.Blink,R3PartKind.RepairPulse,R3PartKind.ScannerPulse};
        for(int i=0;i<4;i++)design.Parts.Add(new R3PartState(new Vector3Int(-3+2*i,1,0),kinds[i]));
        var model=new R3VehicleModel(design);Assert.That(model.AssignActiveSlot(R3PartKind.ScannerPulse,0),Is.True);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(model.MakeCheckpoint(0),R3VehicleKind.Light,model.Economy,0,out var loaded,out var error),Is.True,error);
        Assert.That(loaded.GetActiveSlots()[0],Is.EqualTo(R3PartKind.ScannerPulse));
        Assert.That(loaded.GetActiveSlots()[3],Is.EqualTo(R3PartKind.Booster));
    }
    [Test] public void ActiveTypesUseIndependentSlotsAndCopiesShareTheSlot()
    {
        var get=typeof(R3VehicleModel).GetMethod("GetActiveSlots");
        var assign=typeof(R3VehicleModel).GetMethod("AssignActiveSlot",new[]{typeof(R3PartKind),typeof(int)});
        Assert.That(get,Is.Not.Null);Assert.That(assign,Is.Not.Null);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var kinds=new[]{R3PartKind.Blink,R3PartKind.MagneticGrapple,R3PartKind.Booster,R3PartKind.RepairPulse};
        for(int i=0;i<4;i++)model.Parts.Add(new R3PartState(new Vector3Int(i,4,0),kinds[i]));
        model.Parts.Add(new R3PartState(new Vector3Int(4,4,0),R3PartKind.Blink));
        var slots=(R3PartKind?[])get.Invoke(model,null);
        Assert.That(slots.Where(p=>p.HasValue).Select(p=>p.Value),Is.EquivalentTo(kinds));
        int old=System.Array.FindIndex(slots,p=>p==R3PartKind.Blink);
        int target=old==3?0:3;var displaced=slots[target];
        Assert.That(assign.Invoke(model,new object[]{R3PartKind.Blink,target}),Is.EqualTo(true));
        slots=(R3PartKind?[])get.Invoke(model,null);
        Assert.That(slots[target],Is.EqualTo(R3PartKind.Blink));Assert.That(slots[old],Is.EqualTo(displaced));
    }
    [UnityTest] public IEnumerator FourWeaponSlotsFireTheirOwnPhysicalGuns()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();
        var design=new R3VehicleDesign{Name="Fire slots",CoreHp=100};
        for(int x=-6;x<=6;x++)design.Parts.Add(new R3PartState(new Vector3Int(x,0,0),x==0?R3PartKind.Core:R3PartKind.Armor));
        design.Parts.Add(new R3PartState(new Vector3Int(-6,1,0),R3PartKind.Gun){Weapon=R4WeaponKind.Basic});
        design.Parts.Add(new R3PartState(new Vector3Int(-2,1,0),R3PartKind.Gun){Weapon=R4WeaponKind.Automatic});
        design.Parts.Add(new R3PartState(new Vector3Int(2,1,0),R3PartKind.Gun){Weapon=R4WeaponKind.Heavy});
        design.Parts.Add(new R3PartState(new Vector3Int(6,1,0),R3PartKind.Gun){Weapon=R4WeaponKind.Shotgun});
        var model=new R3VehicleModel(design);
        var paletteObject=new GameObject("palette");var palette=paletteObject.AddComponent<R3VisualPalette>();var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        Assert.That(rig,Is.Not.Null,"The assembled vehicle must survive to the next frame.");
        var assembly=rig.GetComponent<R3VehicleAssembly>();
        Assert.That(assembly,Is.Not.Null,"The vehicle must have an assembly.");
        Assert.That(assembly.Parts.All(p=>p!=null),Is.True,"All mounted parts must exist before firing.");
        var guns=assembly.Parts.Where(p=>p!=null&&p.Kind==R3PartKind.Gun).ToArray();
        Assert.That(guns.Length,Is.EqualTo(4));
        foreach(var gun in guns){Assert.That(gun.GunPivot,Is.Not.Null,$"Missing pivot for {gun.Weapon}");gun.GunPivot.rotation=Quaternion.LookRotation(Vector3.up,Vector3.forward);}
        Physics.SyncTransforms();
        foreach(var gun in guns)Assert.That(assembly.ClearProjectilePath(gun),Is.True,"The fixture must leave the barrel clear: "+gun.Weapon);
        for(int i=0;i<4;i++)
        {
            assembly.FireSlot(i);
            foreach(var gun in guns)
            {
                int gunSlot=System.Array.IndexOf(assembly.Model.GetWeaponSlots(),(R4WeaponKind?)gun.Weapon);
                Assert.That(gun.NextFireTime,gunSlot<=i?Is.GreaterThan(0):Is.Zero,$"slot {i}, gun {gun.Weapon}");
            }
        }
        Object.Destroy(rig.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);yield return new ExitPlayMode();
    }
    [Test] public void FourSlotAssignmentsSurviveCheckpointRoundTrip()
    {
        var design=new R3VehicleDesign{Name="Four slots",CoreHp=100};
        for(int x=-3;x<=3;x++)design.Parts.Add(new R3PartState(new Vector3Int(x,0,0),x==0?R3PartKind.Core:R3PartKind.Armor));
        var kinds=new[]{R4WeaponKind.Basic,R4WeaponKind.Automatic,R4WeaponKind.Heavy,R4WeaponKind.Shotgun};
        for(int i=0;i<4;i++)design.Parts.Add(new R3PartState(new Vector3Int(-3+2*i,1,0),R3PartKind.Gun){Weapon=kinds[i],ContactMask=R3VehicleModel.PlacementContacts(R3PartKind.Gun)});
        var model=new R3VehicleModel(design);
        Assert.That(model.AssignWeaponSlot(R4WeaponKind.Shotgun,0),Is.True);
        var data=model.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,model.Economy,0,out var loaded,out var error),Is.True,error);
        Assert.That(loaded.WeaponAtSlot(0),Is.EqualTo(R4WeaponKind.Shotgun));
        Assert.That(loaded.WeaponAtSlot(1),Is.EqualTo(R4WeaponKind.Automatic));
        Assert.That(loaded.WeaponAtSlot(2),Is.EqualTo(R4WeaponKind.Heavy));
        Assert.That(loaded.WeaponAtSlot(3),Is.EqualTo(R4WeaponKind.Basic));
    }
    [Test] public void FourWeaponTypesGetDistinctSlotsAndCanBeReassigned()
    {
        var get=typeof(R3VehicleModel).GetMethod("WeaponAtSlot",new[]{typeof(int)});
        var assign=typeof(R3VehicleModel).GetMethod("AssignWeaponSlot",new[]{typeof(R4WeaponKind),typeof(int)});
        Assert.That(get,Is.Not.Null,"Four independent slots must be readable.");
        Assert.That(assign,Is.Not.Null,"The garage must be able to change assignments.");
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Parts.RemoveAll(p=>p.Kind==R3PartKind.Gun);
        var kinds=new[]{R4WeaponKind.Basic,R4WeaponKind.Automatic,R4WeaponKind.Heavy,R4WeaponKind.Shotgun};
        for(int i=0;i<kinds.Length;i++)model.Parts.Add(new R3PartState(new Vector3Int(i*2,3,0),R3PartKind.Gun){Weapon=kinds[i]});
        for(int i=0;i<4;i++)Assert.That(get.Invoke(model,new object[]{i}),Is.EqualTo(kinds[i]));
        Assert.That(assign.Invoke(model,new object[]{R4WeaponKind.Shotgun,0}),Is.EqualTo(true));
        Assert.That(get.Invoke(model,new object[]{0}),Is.EqualTo(R4WeaponKind.Shotgun));
        Assert.That(get.Invoke(model,new object[]{3}),Is.EqualTo(R4WeaponKind.Basic));
        var copy=model.DeepCopy();
        Assert.That(get.Invoke(copy,new object[]{0}),Is.EqualTo(R4WeaponKind.Shotgun));
        Assert.That(get.Invoke(copy,new object[]{3}),Is.EqualTo(R4WeaponKind.Basic));
    }
    [Test] public void SingleTypeCanMoveToBAndSurvivesCopy()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        Assert.That(model.WeaponGroupA,Is.EqualTo(R4WeaponKind.Basic));Assert.That(model.WeaponGroupB,Is.Null);
        model.SwapWeaponGroups();var copy=model.DeepCopy();
        Assert.That(copy.WeaponGroupA,Is.Null);Assert.That(copy.WeaponGroupB,Is.EqualTo(R4WeaponKind.Basic));
        copy.Parts.First(p=>p.Kind==R3PartKind.Gun).Health=0;
        Assert.That(copy.WeaponGroupB,Is.EqualTo(R4WeaponKind.Basic),"wreck plan keeps its assignment for repair");
        copy.Parts.RemoveAll(p=>p.Kind==R3PartKind.Gun);
        Assert.That(copy.WeaponGroupA,Is.Null);Assert.That(copy.WeaponGroupB,Is.Null);
    }
    [Test] public void TwoTypesStayDistinctWhenSwappedAndReplaced()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        model.Parts.Add(new R3PartState(new Vector3Int(0,3,0),R3PartKind.Gun){Weapon=R4WeaponKind.Automatic});
        Assert.That(model.WeaponGroupA,Is.EqualTo(R4WeaponKind.Basic));Assert.That(model.WeaponGroupB,Is.EqualTo(R4WeaponKind.Automatic));
        model.SwapWeaponGroups();Assert.That(model.WeaponGroupA,Is.EqualTo(R4WeaponKind.Automatic));Assert.That(model.WeaponGroupB,Is.EqualTo(R4WeaponKind.Basic));
        model.Parts.RemoveAll(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.Automatic);
        model.Parts.Add(new R3PartState(new Vector3Int(0,3,0),R3PartKind.Gun){Weapon=R4WeaponKind.Heavy});
        Assert.That(model.WeaponGroupA,Is.EqualTo(R4WeaponKind.Heavy));Assert.That(model.WeaponGroupB,Is.EqualTo(R4WeaponKind.Basic));
    }
    [UnityTest] public IEnumerator EachGroupFiresARealProjectileThatDamagesItsTarget()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        model.Parts.Add(new R3PartState(new Vector3Int(0,2,2),R3PartKind.Gun){Weapon=R4WeaponKind.Automatic});
        var paletteObject=new GameObject("palette");var palette=paletteObject.AddComponent<R3VisualPalette>();var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);yield return null;
        var assembly=rig.GetComponent<R3VehicleAssembly>();
        for(int group=0;group<2;group++)
        {
            R1EncounterManager.ClearTransientObjects();
            var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun&&p.Weapon==(group==0?R4WeaponKind.Basic:R4WeaponKind.Automatic));
            var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+Vector3.up*5;
            var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);
            gun.GunPivot.rotation=Quaternion.LookRotation(target.transform.position-gun.GunPivot.position);Physics.SyncTransforms();
            assembly.FireGroup(group==1);
            float deadline=Time.realtimeSinceStartup+3;while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(hp.CurrentHp,Is.LessThan(1000),"physical projectile from group "+group+" must damage target");
            Object.Destroy(target);yield return null;
        }
        Object.Destroy(rig.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator TwelveGunsFireOncePerAssignedGroup()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Heavy));
        // Build twelve supported guns with the current one-cell separation.
        model.Parts.RemoveAll(p=>p.Kind==R3PartKind.Gun);
        model.Award(1000);
        foreach(int z in new[]{-2,2})for(int x=-5;x<=5;x++)
        {
            foreach(int y in new[]{0,1})
            {var support=new Vector3Int(x,y,z);if(model.At(support)==null)model.Parts.Add(new R3PartState(support,R3PartKind.Armor));}
        }
        int index=0;
        foreach(int z in new[]{-2,2})foreach(int x in new[]{-5,-3,-1,1,3,5})
        {
            var cell=new Vector3Int(x,2,z);var weapon=index++%2==0?R4WeaponKind.Automatic:R4WeaponKind.Basic;
            Assert.That(model.PlacementError(cell,R3PartKind.Gun,false,weapon),Is.Null,"Test gun must obey current grid and spacing rules.");
            model.Parts.Add(new R3PartState(cell,R3PartKind.Gun){Weapon=weapon,ContactMask=R3VehicleModel.PlacementContacts(R3PartKind.Gun)});
        }
        Assert.That(R3VehicleModel.Connected(model.Parts),Is.True,"The fixture must be connected to the core.");
        var paletteObject=new GameObject("palette");var palette=paletteObject.AddComponent<R3VisualPalette>();var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);yield return null;
        var assembly=rig.GetComponent<R3VehicleAssembly>();R1EncounterManager.ClearTransientObjects();
        foreach(var gun in assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun))gun.GunPivot.rotation=Quaternion.LookRotation(Vector3.up,Vector3.forward);
        Physics.SyncTransforms();
        if(model.WeaponGroupA!=R4WeaponKind.Basic)model.SwapWeaponGroups();
        Assert.That(model.WeaponGroupA,Is.EqualTo(R4WeaponKind.Basic));Assert.That(model.WeaponGroupB,Is.EqualTo(R4WeaponKind.Automatic));
        assembly.FireGroup(false);
        Assert.That(Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.Weapon==R4WeaponKind.Basic),Is.EqualTo(6));
        Assert.That(Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.Weapon==R4WeaponKind.Automatic),Is.Zero);
        assembly.FireGroup(true);
        Assert.That(Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Count(p=>p.Weapon==R4WeaponKind.Automatic),Is.EqualTo(6));
        assembly.FireGroup(false);assembly.FireGroup(true);
        Assert.That(Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Length,Is.EqualTo(12),"cooldowns prevent duplicate shots");
        Object.Destroy(rig.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator DefaultFireOnlyActivatesFirstWeaponType()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        model.Parts.Add(new R3PartState(new Vector3Int(0,2,2),R3PartKind.Gun){Weapon=R4WeaponKind.Automatic});
        var paletteObject=new GameObject("palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        yield return null;
        var assembly=rig.GetComponent<R3VehicleAssembly>();
        var first=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.Basic);
        var second=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.Automatic);
        foreach(var gun in assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun))gun.GunPivot.rotation=Quaternion.LookRotation(Vector3.up,Vector3.forward);
        Physics.SyncTransforms();
        assembly.FireGuns();
        Assert.That(first.NextFireTime,Is.GreaterThan(0),"primary gun fires");
        Assert.That(second.NextFireTime,Is.Zero,"secondary gun must wait for its own trigger");
        assembly.FireGroup(true);
        Assert.That(second.NextFireTime,Is.GreaterThan(0),"secondary trigger fires its gun");
        second.NextFireTime=0;
        var receiver=second.GetComponent<RoboRougelike.Mechanics.EMP.EmpReceiver>();
        Assert.That(receiver,Is.Not.Null);receiver.ApplyEmp(3);
        assembly.FireGroup(true);Assert.That(second.NextFireTime,Is.Zero,"EMP-disrupted group must not fire");
        receiver.ClearEmp();
        first.NextFireTime=second.NextFireTime=0;
        model.SwapWeaponGroups();assembly.FireGuns();
        Assert.That(first.NextFireTime,Is.Zero);Assert.That(second.NextFireTime,Is.GreaterThan(0));
        second.Damageable.ApplyDamage(10000,second.transform.position,Vector3.up);
        second.NextFireTime=0;assembly.FireGuns();Assert.That(second.NextFireTime,Is.Zero,"destroyed gun must not fire");
        Object.Destroy(rig.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);
        yield return new ExitPlayMode();
    }
}
