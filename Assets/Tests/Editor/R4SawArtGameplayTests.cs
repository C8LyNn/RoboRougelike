using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.WeaponIntegration;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4SawArtGameplayTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();}
    [UnityTest] public IEnumerator ImportedBladeUsesExistingPhysicalCenterSizeAndMotor()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
        Assert.That(model.Remove(state.Cell),Is.True);state.Weapon=R4WeaponKind.RotarySaw;
        // Current starter ends at z=3; the legacy z=4 fixture is detached.
        model.Award(1000);
        Assert.That(model.Buy(new Vector3Int(0,3,3),R3PartKind.Armor),Is.True);
        Assert.That(model.Buy(new Vector3Int(0,4,3),R3PartKind.Armor),Is.True);
        Assert.That(model.Mount(state.Id,new Vector3Int(0,5,3)),Is.True);
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var gun=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
        var art=gun.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R4W_RotarySaw_Blade");
        Assert.That(art,Is.Not.Null,"The supplied saw blade must be mounted on the real vehicle.");
        var binding=gun.GetComponent<RRLSawBinding>();Assert.That(binding,Is.Not.Null);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        Assert.That(typeof(RRLSawBinding).GetField("rotor",flags).GetValue(binding),Is.EqualTo(art),"The existing motor drives the supplied blade.");
        Assert.That(typeof(RRLSawBinding).GetField("blade",flags).GetValue(binding),Is.EqualTo(gun.Muzzle));
        Assert.That(Vector3.Distance(art.position,gun.Muzzle.position),Is.LessThan(.001),"The visible blade and physical cutting centre coincide.");
        Assert.That(Mathf.Abs(Vector3.Dot(art.right,gun.Muzzle.right)),Is.GreaterThan(.999f),"Blade spin axis matches the physical cutting-disc axle.");
        var size=art.GetComponentInChildren<Renderer>().bounds.size;
        Assert.That(Mathf.Max(size.y,size.z),Is.EqualTo(1.8f).Within(.08),"The visible disc matches the 0.9 m gameplay radius.");
        var direction=new Vector3(.4f,.3f,1).normalized;
        gun.GunPivot.rotation=Quaternion.LookRotation(direction);
        vehicle.GetComponent<R3VehicleAssembly>().AimGuns(gun.GunPivot.position+direction*50);
        Assert.That(Vector3.Distance(art.position,gun.Muzzle.position),Is.LessThan(.001));
        Assert.That(Mathf.Abs(Vector3.Dot(art.right,gun.Muzzle.right)),Is.GreaterThan(.999f));
        var before=art.localRotation;binding.SetMotorRequested(true);
        float deadline=Time.realtimeSinceStartup+3;
        while(binding.Module.Motor.Rpm<=100&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        yield return null;
        Assert.That(binding.Module.Motor.Rpm,Is.GreaterThan(100));
        Assert.That(Quaternion.Angle(before,art.localRotation),Is.GreaterThan(.1f));
        Assert.That(art.GetComponentsInChildren<Collider>(true).Length,Is.Zero);
        Object.Destroy(vehicle.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
