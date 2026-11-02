// B01-61 W28 independent REAL Unity EditMode test source.
// Scoped exactly to frozen Codex #824 gameplay SHA e03d36f2b86f8c73a067e574b67237a8921f622a.
// This is an added QA fixture; test execution requires licensed Unity 6000.3.14f1.
// Do not confuse source preflight CI with native EditMode PASS.
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.WeaponIntegration;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class B0161W28DynamicOwnClearanceRegressionTests
{
    static Material material;
    static R3VisualPalette palette;
    static GameObject colors;

    static void SetupPalette()
    {
        Assert.That(Shader.Find("Universal Render Pipeline/Lit"),Is.Not.Null,"URP shader missing");
        material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        colors=new GameObject("B01-61 W28 QA palette");
        palette=colors.AddComponent<R3VisualPalette>();
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
    }

    static (R3VehicleAssembly assembly,R3MountedPart gun,R3PartState state,GameObject rig)
        Build(R4WeaponKind weapon,int separation=0)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
        state.Weapon=weapon;
        var rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,
            new Vector3(separation*40,20,0),Quaternion.identity);
        Assert.That(rig,Is.Not.Null);
        var assembly=rig.GetComponent<R3VehicleAssembly>();
        var gun=assembly.Parts.Single(p=>p.Id==state.Id);
        Assert.That(gun.Muzzle,Is.Not.Null);
        return (assembly,gun,state,rig.gameObject);
    }

    static bool DirectFire(R3MountedPart part)
    {
        switch(part.Weapon)
        {
            case R4WeaponKind.Shotgun:
                return part.GetComponent<RRLShotgunBinding>().Fire();
            case R4WeaponKind.Railgun:
                return part.GetComponent<RRLRailgunBinding>().Fire();
            case R4WeaponKind.RocketLauncher:
                return part.GetComponent<RRLRocketBinding>().Fire();
            default:throw new ArgumentException("Not a test-owned projectile gun");
        }
    }

    static GameObject OwnedMovingBlocker(Transform rigRoot,string name)
    {
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.name=name;
        blocker.transform.SetParent(rigRoot,true);
        blocker.transform.localScale=Vector3.one*.30f;
        var rb=blocker.AddComponent<Rigidbody>();
        rb.isKinematic=true;rb.useGravity=false;
        return blocker;
    }

    [UnityTearDown]
    public IEnumerator Clean()
    {
        if(Application.isPlaying)
        {
            R1EncounterManager.ClearTransientObjects();
            yield return new ExitPlayMode();
        }
    }

    [UnityTest]
    public IEnumerator MovingOwnedArmorBlocksBothDirectAndGroupSpecialGunsWithoutAmmoOrCooldown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        SetupPalette();
        var kinds=new[]{R4WeaponKind.Shotgun,R4WeaponKind.Railgun,R4WeaponKind.RocketLauncher};
        for(int n=0;n<kinds.Length;n++)
        {
            var obj=Build(kinds[n],n);
            yield return null;
            Assert.That(obj.assembly.ClearProjectilePath(obj.gun),Is.True,
                kinds[n]+" baseline path not clear; cannot exercise moving obstruction");
            var blocker=OwnedMovingBlocker(obj.rig.transform,"QA moving owned armor");
            var rb=blocker.GetComponent<Rigidbody>();
            rb.position=obj.gun.Muzzle.position+obj.gun.Muzzle.right*3f;
            Physics.SyncTransforms();
            Assert.That(obj.assembly.ClearProjectilePath(obj.gun),Is.True,
                kinds[n]+" remote owned armor must not block");
            int ammo=obj.state.Ammo;
            float ready=obj.state.DeviceReadyAt, groupReady=obj.gun.NextFireTime;
            rb.position=obj.gun.Muzzle.position+obj.gun.Muzzle.forward*.6f;
            Physics.SyncTransforms();
            Assert.That(obj.assembly.ClearProjectilePath(obj.gun),Is.False,
                kinds[n]+" moved owned armor must obstruct before firing");
            Assert.That(DirectFire(obj.gun),Is.False,
                kinds[n]+" direct module Fire bypassed the new moving armor");
            obj.assembly.FireGroup(false);
            Assert.That(obj.state.Ammo,Is.EqualTo(ammo),
                kinds[n]+" blocked direct/group shot spent ammo");
            Assert.That(obj.state.DeviceReadyAt,Is.EqualTo(ready),
                kinds[n]+" blocked direct/group shot spent device cooldown");
            Assert.That(obj.gun.NextFireTime,Is.EqualTo(groupReady),
                kinds[n]+" blocked group attack spent group cooldown");
            rb.position=obj.gun.Muzzle.position+obj.gun.Muzzle.right*3f;
            Physics.SyncTransforms();
            Assert.That(obj.assembly.ClearProjectilePath(obj.gun),Is.True,
                kinds[n]+" moved-away armor remains stale in physics");
            obj.assembly.FireGroup(false);
            Assert.That(obj.state.Ammo,Is.EqualTo(ammo-1),
                kinds[n]+" legitimate shot did not consume one round");
            Assert.That(obj.state.DeviceReadyAt,Is.GreaterThan(ready),
                kinds[n]+" legitimate shot did not start cooldown");
            UnityEngine.Object.Destroy(obj.rig);
            R1EncounterManager.ClearTransientObjects();
            yield return null;
        }
        UnityEngine.Object.Destroy(colors);
        UnityEngine.Object.Destroy(material);
    }

    [UnityTest]
    public IEnumerator RotatedGunMustTreatDifferentGunColliderAsOwnedButExternalCoverAsFirstHit()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        SetupPalette();
        var obj=Build(R4WeaponKind.Basic);
        yield return null;
        Assert.That(obj.gun.GunPivot,Is.Not.Null);
        var cover=OwnedMovingBlocker(obj.rig.transform,"QA second gun moving through aim lane");
        var rb=cover.GetComponent<Rigidbody>();
        rb.position=obj.gun.Muzzle.position+obj.gun.Muzzle.forward*1f;
        Physics.SyncTransforms();
        Assert.That(obj.assembly.ClearProjectilePath(obj.gun),Is.False,
            "different owned gun/component is a physical obstruction");
        // Rotate real payload/muzzle, then move another owned component into new lane.
        obj.gun.GunPivot.rotation=Quaternion.AngleAxis(35f,Vector3.up)*obj.gun.GunPivot.rotation;
        rb.position=obj.gun.Muzzle.position+obj.gun.Muzzle.right*3f;
        Physics.SyncTransforms();
        Assert.That(obj.assembly.ClearProjectilePath(obj.gun),Is.True,
            "newly rotated muzzle with foreign owned component cleared");
        rb.position=obj.gun.Muzzle.position+obj.gun.Muzzle.forward*1f;
        Physics.SyncTransforms();
        Assert.That(obj.assembly.ClearProjectilePath(obj.gun),Is.False,
            "rotating muzzle must refresh the own-part trajectory each call");
        // A nearer EXTERNAL collider can legally absorb the projectile before
        // a farther owner part. Do not reject a shot into front cover.
        var external=GameObject.CreatePrimitive(PrimitiveType.Cube);
        external.name="QA foreign environment cover";
        external.transform.position=obj.gun.Muzzle.position+obj.gun.Muzzle.forward*.45f;
        external.transform.localScale=Vector3.one*.18f;
        Physics.SyncTransforms();
        Assert.That(obj.assembly.ClearProjectilePath(obj.gun),Is.True,
            "external cover lies physically before later owned armor");
        external.SetActive(false);
        Physics.SyncTransforms();
        Assert.That(obj.assembly.ClearProjectilePath(obj.gun),Is.False,
            "after removing closer external obstacle, owner armor must block");
        UnityEngine.Object.Destroy(external);
        UnityEngine.Object.Destroy(obj.rig);
        UnityEngine.Object.Destroy(colors);
        UnityEngine.Object.Destroy(material);
    }

    [UnityTest]
    public IEnumerator ContinuousBeamCoolsOrStaysStableWhileMovingOwnedArmorEntersLane()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        SetupPalette();
        var obj=Build(R4WeaponKind.BeamCutter);
        yield return null;
        var beam=obj.gun.GetComponent<RRLBeamBinding>();
        Assert.That(beam,Is.Not.Null);
        Assert.That(obj.assembly.ClearProjectilePath(obj.gun,
            obj.gun.Muzzle.forward,beam.Module.Settings.rangeMeters,.001f),Is.True);
        beam.SetTriggerHeld(true);
        beam.SendMessage("FixedUpdate");beam.SendMessage("FixedUpdate");
        var initial=beam.Module.Heat;
        Assert.That(initial,Is.GreaterThan(0f),"clear continuous beam should heat");
        var blocker=OwnedMovingBlocker(obj.rig.transform,"QA changing beam occluder");
        var rb=blocker.GetComponent<Rigidbody>();
        rb.position=obj.gun.Muzzle.position+obj.gun.Muzzle.forward*.65f;
        Physics.SyncTransforms();
        Assert.That(obj.assembly.ClearProjectilePath(obj.gun,
            obj.gun.Muzzle.forward,beam.Module.Settings.rangeMeters,.001f),Is.False);
        beam.SendMessage("FixedUpdate");beam.SendMessage("FixedUpdate");
        Assert.That(beam.Module.Heat,Is.LessThanOrEqualTo(initial+.0001f),
            "blocked continuous beam must not accumulate extra heat");
        rb.position=obj.gun.Muzzle.position+obj.gun.Muzzle.right*3f;
        Physics.SyncTransforms();
        beam.SendMessage("FixedUpdate");beam.SendMessage("FixedUpdate");
        Assert.That(beam.Module.Heat,Is.GreaterThan(initial),
            "same held beam should resume and accumulate heat after moving obstruction");
        beam.SetTriggerHeld(false);
        UnityEngine.Object.Destroy(obj.rig);
        UnityEngine.Object.Destroy(colors);
        UnityEngine.Object.Destroy(material);
    }
}
