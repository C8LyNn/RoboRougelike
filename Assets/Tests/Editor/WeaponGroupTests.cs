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
        model.Parts.Add(new R3PartState(new Vector3Int(0,3,0),R3PartKind.Gun){Weapon=R4WeaponKind.Automatic});
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
        for(int i=0;i<11;i++)model.Parts.Add(new R3PartState(new Vector3Int(i%7-3,2,i/7-3),R3PartKind.Gun){Weapon=i%2==0?R4WeaponKind.Automatic:R4WeaponKind.Basic});
        var paletteObject=new GameObject("palette");var palette=paletteObject.AddComponent<R3VisualPalette>();var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);yield return null;
        var assembly=rig.GetComponent<R3VehicleAssembly>();R1EncounterManager.ClearTransientObjects();
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
        model.Parts.Add(new R3PartState(new Vector3Int(0,3,0),R3PartKind.Gun){Weapon=R4WeaponKind.Automatic});
        var paletteObject=new GameObject("palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        yield return null;
        var assembly=rig.GetComponent<R3VehicleAssembly>();
        var first=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.Basic);
        var second=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.Automatic);
        assembly.FireGuns();
        Assert.That(first.NextFireTime,Is.GreaterThan(0),"primary gun fires");
        Assert.That(second.NextFireTime,Is.Zero,"secondary gun must wait for its own trigger");
        assembly.FireGroup(true);
        Assert.That(second.NextFireTime,Is.GreaterThan(0),"secondary trigger fires its gun");
        first.NextFireTime=second.NextFireTime=0;
        model.SwapWeaponGroups();assembly.FireGuns();
        Assert.That(first.NextFireTime,Is.Zero);Assert.That(second.NextFireTime,Is.GreaterThan(0));
        second.Damageable.ApplyDamage(10000,second.transform.position,Vector3.up);
        second.NextFireTime=0;assembly.FireGuns();Assert.That(second.NextFireTime,Is.Zero,"destroyed gun must not fire");
        Object.Destroy(rig.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);
        yield return new ExitPlayMode();
    }
}
