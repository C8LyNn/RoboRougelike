// B01-41 W28 independent Unity 6000.3.14f1 NATIVE integration regression.
// This class tests the real Codex #824 R3 rig and original WPN-005/WPN-006
// modules without replacing or editing author/gameplay sources.
// Tests are AUTHORED, not EXECUTED in native Unity until real Editor/NUnit logs.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.WeaponIntegration;
using RoboRougelike.Mechanics.EMP;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CI41W28SawEmpNativeRegressionTests
{
    const int TargetLayer=8;
    const int WallLayer=9;

    [UnityTearDown]
    public IEnumerator ExitPlay()
    {
        if(Application.isPlaying)yield return new ExitPlayMode();
    }

    static R3MountedPart SpawnActualR3Weapon(R4WeaponKind kind,
        out GameObject rig,out GameObject paletteObject,out Material material,
        out R3PartState state)
    {
        material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        paletteObject=new GameObject("CI41 W28 original R3 palette");
        var palette=paletteObject.AddComponent<R3VisualPalette>();
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
        state.Weapon=kind;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,
            new Vector3(0,20,0),Quaternion.identity);
        rig=vehicle.gameObject;
        return rig.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
    }

    static void Cleanup(GameObject rig,GameObject palette,Material material)
    {
        if(rig!=null)Object.Destroy(rig);
        if(palette!=null)Object.Destroy(palette);
        if(material!=null)Object.Destroy(material);
        R1EncounterManager.ClearTransientObjects();
    }

    [UnityTest]
    public IEnumerator Saw_ActualRotatingOwnedArmor_StopsBlockedMotorHeat_ThenResumes()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var gun=SpawnActualR3Weapon(R4WeaponKind.RotarySaw,
            out var rig,out var palette,out var material,out var state);
        yield return null;
        Assert.That(gun,Is.Not.Null);
        var saw=gun.GetComponent<RRLSawBinding>();
        Assert.That(saw,Is.Not.Null);
        Assert.That(saw.Device.Available,Is.True,"a healthy actual R3 saw should be enabled");
        Assert.That(saw.Module.IsOperational,Is.True);
        // Physical owned armor in the actual active cutting disc, not a shot ray.
        var own=GameObject.CreatePrimitive(PrimitiveType.Cube);
        own.name="CI41 owned physical rotating-saw envelope blocker";
        own.transform.SetParent(rig.transform,true);
        own.transform.position=gun.Muzzle.position+gun.Muzzle.forward*.45f;
        own.transform.localScale=Vector3.one*.3f;
        Physics.SyncTransforms();
        float initialHeat=saw.Module.Motor.Heat;
        saw.SetMotorRequested(true);
        saw.SendMessage("FixedUpdate");
        // Desired W28 acceptance: no own-disc contact, motor heat or RPM expenditure
        // while actual own physical blade sweep is obstructed. W27 behavior may RED.
        Assert.That(saw.Module.Motor.Rpm,Is.EqualTo(0).Within(.001f),
            "RED until Codex guards rotating saw swept OWN contact; native G1 required");
        Assert.That(saw.Module.Motor.Heat,Is.EqualTo(initialHeat).Within(.001f),
            "blocked own rotating disc must not spend heat");
        own.SetActive(false);Physics.SyncTransforms();
        saw.SetMotorRequested(true);saw.SendMessage("FixedUpdate");
        Assert.That(saw.Module.Motor.Rpm,Is.GreaterThan(0),
            "unblocked actual original saw must spin without a manual reset");
        Assert.That(saw.Module.Motor.Heat,Is.GreaterThanOrEqualTo(initialHeat));
        Assert.That(state.Health,Is.GreaterThan(0),"owner's healthy gun remains alive");
        Cleanup(rig,palette,material);
    }

    [UnityTest]
    public IEnumerator Saw_OriginalOwnerContactsNeverReceiveDamageFromRotatingBlade()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var gun=SpawnActualR3Weapon(R4WeaponKind.RotarySaw,
            out var rig,out var palette,out var material,out var state);
        yield return null;
        var saw=gun.GetComponent<RRLSawBinding>();
        Assert.That(saw,Is.Not.Null);
        Assert.That(gun.Muzzle,Is.Not.Null);
        var health=gun.Damageable.CurrentHp;
        var own=GameObject.CreatePrimitive(PrimitiveType.Cube);
        own.transform.SetParent(rig.transform,true);
        own.transform.position=gun.Muzzle.position+gun.Muzzle.forward*.4f;
        own.transform.localScale=Vector3.one*.3f;
        Physics.SyncTransforms();
        saw.SetMotorRequested(true);
        saw.SendMessage("FixedUpdate");
        Assert.That(saw.Module.DamageEventsLastTick,Is.Zero,
            "a real owner collider in rotating cutting disc cannot be a damage target");
        Assert.That(gun.Damageable.CurrentHp,Is.EqualTo(health),
            "owner gun HP cannot decrease through own saw");
        Cleanup(rig,palette,material);
    }

    static EmpReceiver AddEnemyAt(Vector3 point)
    {
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name="CI41 EMP external real receiver";
        target.layer=TargetLayer;
        target.transform.position=point;
        target.transform.localScale=Vector3.one*.55f;
        var receiver=target.AddComponent<EmpReceiver>();
        receiver.ConfigureOwnership(target.transform,202,2);
        receiver.ConfigureSusceptibility(0);
        return receiver;
    }

    [UnityTest]
    public IEnumerator Emp_OriginalDirectModule_ExternalWallShieldsTarget_NotOwnVehicle()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var gun=SpawnActualR3Weapon(R4WeaponKind.EmpEmitter,
            out var rig,out var palette,out var material,out var state);
        yield return null;
        var binding=gun.GetComponent<RRLEmpBinding>();
        Assert.That(binding,Is.Not.Null);
        binding.Module.ConfigurePhysics(1<<TargetLayer,1<<WallLayer,64,32);
        var own=gun.GetComponent<RRLEmpPartState>();
        Assert.That(own,Is.Not.Null,
            "actual EMP vehicle assembly must provide own module identity");
        gun.gameObject.layer=TargetLayer;
        var start=gun.transform.position;
        var direction=gun.transform.forward;
        var enemy=AddEnemyAt(start+direction*3f);
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name="CI41 EMP external occluding PhysX wall";
        wall.layer=WallLayer;wall.transform.position=start+direction*1.5f;
        wall.transform.localScale=Vector3.one*.5f;
        Physics.SyncTransforms();
        var ready=state.DeviceReadyAt;
        Assert.That(binding.Fire(),Is.True,
            "an EMP pulse can legitimately activate while an enemy is behind cover");
        Assert.That(binding.LastReport.ReceiversAffected,Is.Zero,
            "external physical wall must shield electronic module");
        Assert.That(enemy.IsDisrupted,Is.False,"EMP blocked by a real wall cannot disable target");
        Assert.That(own.Receiver.IsDisrupted,Is.False,
            "EMP must never disable own emitter vehicle by default");
        Assert.That(binding.Module.CooldownRemaining,Is.GreaterThan(0),
            "successful area EMP activation consumes cooldown even when target was occluded");
        Assert.That(state.DeviceReadyAt,Is.GreaterThan(ready));
        Object.Destroy(enemy.gameObject);Object.Destroy(wall);
        Cleanup(rig,palette,material);
    }

    [UnityTest]
    public IEnumerator Emp_ActualDirectBinding_DedupOwnPhysicalModuleAndConsumesCooldownOnce()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var gun=SpawnActualR3Weapon(R4WeaponKind.EmpEmitter,
            out var rig,out var palette,out var material,out var state);
        yield return null;
        var binding=gun.GetComponent<RRLEmpBinding>();
        Assert.That(binding,Is.Not.Null);
        binding.Module.ConfigurePhysics(1<<TargetLayer,1<<WallLayer,64,32);
        gun.gameObject.layer=TargetLayer; // own live receiver is included in overlap.
        var own=gun.GetComponent<RRLEmpPartState>();
        Assert.That(own,Is.Not.Null);
        var enemy=AddEnemyAt(gun.transform.position+gun.transform.forward*3);
        // Multiple colliders on ONE original receiver may not double-spend.
        var extra=new GameObject("CI41 second real collider same enemy receiver");
        extra.layer=TargetLayer;extra.transform.SetParent(enemy.transform,false);
        extra.AddComponent<BoxCollider>().size=Vector3.one*.45f;
        Physics.SyncTransforms();
        int initialAmmo=state.Ammo;
        Assert.That(binding.Fire(),Is.True,"genuine direct EMP emitter fires once");
        Assert.That(binding.LastReport.ReceiversAffected,Is.EqualTo(1),
            "one enemy module, two colliders => one EMP disruption");
        Assert.That(enemy.IsDisrupted,Is.True);
        Assert.That(own.Receiver.IsDisrupted,Is.False,"owner is immune");
        var ready=state.DeviceReadyAt;
        Assert.That(binding.Fire(),Is.False,"direct immediate repeat denied by cooldown");
        Assert.That(state.DeviceReadyAt,Is.EqualTo(ready),
            "rejected repeat must not consume/extend cooldown");
        Assert.That(state.Ammo,Is.EqualTo(initialAmmo),
            "non-projectile EMP does not secretly consume bullet ammunition");
        Object.Destroy(enemy.gameObject);
        Cleanup(rig,palette,material);
    }
}
