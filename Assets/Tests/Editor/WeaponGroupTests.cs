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
