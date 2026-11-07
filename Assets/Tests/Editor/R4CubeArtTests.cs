using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class R4CubeArtTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();}
    [UnityTest] public IEnumerator ArmorUsesSuppliedCubeWithoutReplacingItsPhysicsOrHealth()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        var armor=vehicle.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor);
        Assert.That(armor.GetComponent<Renderer>().enabled,Is.False,"Old cube renderer is replaced.");
        Assert.That(armor.GetComponent<Collider>().enabled,Is.True,"Original physical collider remains.");
        var visual=armor.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R4B_Cube_ROOT");
        Assert.That(visual,Is.Not.Null,"Actual supplied cube appears on an owned armor part.");
        Assert.That(visual.GetComponentsInChildren<Collider>(true),Is.Empty);
        Assert.That(armor.Damageable,Is.Not.Null);Assert.That(armor.Block,Is.Not.Null);
        Object.Destroy(vehicle.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
}
