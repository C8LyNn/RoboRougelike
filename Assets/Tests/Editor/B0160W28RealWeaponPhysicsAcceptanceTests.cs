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

// B01-60 W28: real R3 gameplay objects/Unity PhysX acceptance, NOT an isolated
// weapon-module mock. Deliberately NO changes to any original Codex runtime.
public sealed class B0160W28RealWeaponPhysicsAcceptanceTests
{
    static R3VisualPalette Palette(out GameObject holder, out Material material)
    {
        holder = new GameObject("B01-60 W28 isolated palette");
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var palette = holder.AddComponent<R3VisualPalette>();
        for (int i=0;i<palette.Parts.Length;i++) palette.Parts[i]=material;
        for (int i=0;i<palette.Weapons.Length;i++) palette.Weapons[i]=material;
        return palette;
    }

    static R3MountedPart Gun(R1VehicleState vehicle, R3PartState state)
    {
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();
        var gun=assembly.Parts.Single(p=>p.Id==state.Id);
        Assert.That(gun.GunPivot,Is.Not.Null);
        Assert.That(gun.Muzzle,Is.Not.Null);
        return gun;
    }

    static GameObject OwnedObstacle(R1VehicleState vehicle,R3MountedPart gun,float meters)
    {
        var block=GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name="B01-60 W28 owned dynamic physical collider";
        block.transform.SetParent(vehicle.transform,true);
        block.transform.position=gun.Muzzle.position+gun.Muzzle.forward*meters;
        block.transform.rotation=gun.Muzzle.rotation;
        block.transform.localScale=Vector3.one*.3f;
        return block;
    }

    [UnityTearDown] public IEnumerator Teardown()
    {
        if(Application.isPlaying)yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator DynamicOwnedRigidbody_EntersAndLeavesShotgunPath_WithoutSpendingBlockedAmmo()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var palette=Palette(out var colors,out var material);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
        state.Weapon=R4WeaponKind.Shotgun;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,
            new Vector3(0,20,0),Quaternion.identity);
        yield return null;
        var gun=Gun(vehicle,state);
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();
        var obstacle=OwnedObstacle(vehicle,gun,.65f);
        var movingBody=obstacle.AddComponent<Rigidbody>();
        movingBody.useGravity=false;movingBody.isKinematic=true;
        Physics.SyncTransforms();
        Assert.That(assembly.ClearProjectilePath(gun),Is.False,"moving OWN Rigidbody currently crosses shot path");
        var ammo=state.Ammo;var deviceAt=state.DeviceReadyAt;var nextAt=gun.NextFireTime;
        assembly.FireGroup(false);
        Assert.That(state.Ammo,Is.EqualTo(ammo),"dynamic own blocker spent ammo");
        Assert.That(state.DeviceReadyAt,Is.EqualTo(deviceAt),"dynamic own blocker spent weapon cooldown");
        Assert.That(gun.NextFireTime,Is.EqualTo(nextAt),"dynamic own blocker spent group cooldown");
        var clear=gun.Muzzle.position+gun.Muzzle.right*4f;
        movingBody.MovePosition(clear);
        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();
        Assert.That(assembly.ClearProjectilePath(gun),Is.True,"same moving collider left the shot path");
        assembly.FireGroup(false);
        Assert.That(state.Ammo,Is.EqualTo(ammo-1),"shotgun must fire after physical blocker moves");
        Assert.That(state.DeviceReadyAt,Is.GreaterThan(deviceAt),"real shot must start cooldown");
        Object.Destroy(vehicle.gameObject);
        R1EncounterManager.ClearTransientObjects();
        Object.Destroy(colors);Object.Destroy(material);
        yield return null;
    }

    [UnityTest]
    public IEnumerator RotarySaw_RotatingAgainstOwnArmor_DoesNotDamageItsPhysicalOwner()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var palette=Palette(out var colors,out var material);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
        state.Weapon=R4WeaponKind.RotarySaw;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,
            new Vector3(0,20,0),Quaternion.identity);
        yield return null;
        var gun=Gun(vehicle,state);
        var saw=gun.GetComponent<RRLSawBinding>();
        Assert.That(saw,Is.Not.Null,"actual R3 built saw binding must exist");
        Assert.That(saw.Module,Is.Not.Null,"actual rotary motor module must exist");
        var blocker=OwnedObstacle(vehicle,gun,.12f);
        var ownCollider=blocker.GetComponent<Collider>();
        Assert.That(saw.TryResolve(ownCollider,out _),Is.False,
            "direct saw sink must never resolve ANY own-vehicle collider as enemy");
        var core=vehicle.GetComponent<R3VehicleAssembly>().Parts
            .First(p=>p.Kind==R3PartKind.Core);
        var ownHp=core.Damageable.CurrentHp;
        saw.SetMotorRequested(true);
        Physics.SyncTransforms();
        for(int i=0;i<60;i++)
        {
            // Real barrel rotates physically while the saw samples actual
            // colliders; do not use projectile-specific muzzle clearance.
            gun.Muzzle.Rotate(Vector3.up,6f,Space.Self);
            Physics.SyncTransforms();
            saw.SendMessage("FixedUpdate");
            Assert.That(saw.Module.DamageEventsLastTick,Is.Zero,
                "rotary contact must filter own collider even during rotation");
        }
        Assert.That(core.Damageable.CurrentHp,Is.EqualTo(ownHp),
            "rotating saw caused friendly damage");
        saw.SetMotorRequested(false);
        saw.SendMessage("FixedUpdate");
        Assert.That(saw.Module.IsMotorRequested,Is.False,"released input must stop requested motor");
        Object.Destroy(vehicle.gameObject);Object.Destroy(colors);Object.Destroy(material);
        yield return null;
    }

    [UnityTest]
    public IEnumerator EmpArea_OwnModulesStaySafe_ExternalWallOccludes_ThenClearPulseRecovers()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var palette=Palette(out var colors,out var material);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
        state.Weapon=R4WeaponKind.EmpEmitter;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,
            new Vector3(0,20,0),Quaternion.identity);
        yield return null;
        var gun=Gun(vehicle,state);
        var binding=gun.GetComponent<RRLEmpBinding>();
        Assert.That(binding,Is.Not.Null,"actual R3 EMP weapon was not wired");
        Assert.That(binding.Module,Is.Not.Null);
        // EMP is RADIAL, not a projectile: own armor is transparent to its
        // ray checks, while external cover blocks receivers behind it.
        binding.Module.ConfigurePhysics(~0,1<<9,64,32);
        var ownReceiver=gun.GetComponent<EmpReceiver>();
        Assert.That(ownReceiver,Is.Not.Null,"EMP module must participate in real per-part ownership");
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name="B01-60 W28 external EMP target";
        target.transform.position=gun.transform.position+Vector3.right*4f;
        var receiver=target.AddComponent<EmpReceiver>();
        receiver.ConfigureOwnership(target.transform,202,2);
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name="B01-60 W28 external physical EMP occluder";
        wall.layer=9;
        wall.transform.position=(gun.transform.position+target.transform.position)*.5f;
        wall.transform.localScale=new Vector3(.5f,4f,4f);
        Physics.SyncTransforms();
        var before=state.DeviceReadyAt;
        Assert.That(binding.Fire(),Is.True,"EMP radial pulse is emitted even when its targets are behind external walls");
        Assert.That(receiver.IsDisrupted,Is.False,"physical external wall must occlude EMP");
        Assert.That(ownReceiver.IsDisrupted,Is.False,"owner's own electronic module must never self-disrupt");
        Assert.That(state.DeviceReadyAt,Is.GreaterThan(before),"emitted EMP consumes exactly one cooldown");
        var cooldown=state.DeviceReadyAt;
        Assert.That(binding.Fire(),Is.False,"immediate second EMP pulse cannot fire");
        Assert.That(state.DeviceReadyAt,Is.EqualTo(cooldown),"rejected repeated EMP pulse must not spend another cooldown");
        wall.SetActive(false);Physics.SyncTransforms();
        binding.Module.RestoreCooldown(0f);state.DeviceReadyAt=0f;
        Assert.That(binding.Fire(),Is.True,"when external wall clears, EMP pulse must be possible");
        Assert.That(receiver.IsDisrupted,Is.True,"real external receiver must be disrupted without cover");
        Assert.That(ownReceiver.IsDisrupted,Is.False);
        Object.Destroy(vehicle.gameObject);
        Object.Destroy(target);Object.Destroy(wall);
        Object.Destroy(colors);Object.Destroy(material);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TwoWeaponGroups_OwnBlockedShotgunDoesNotSuppressUnblockedBasicGroup()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var palette=Palette(out var colors,out var material);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var first=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
        first.Weapon=R4WeaponKind.Shotgun;
        // Second genuinely mounted gun, adjacent to the first one, same
        // logical 0.55 m physical mounting grid; distinct stable part ID.
        var second=new R3PartState(new Vector3Int(0,2,0),R3PartKind.Gun);
        second.Weapon=R4WeaponKind.Basic;
        model.Parts.Add(second);model.SyncWeaponGroups();
        Assert.That(model.WeaponGroupA,Is.EqualTo(R4WeaponKind.Shotgun));
        Assert.That(model.WeaponGroupB,Is.EqualTo(R4WeaponKind.Basic));
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,
            new Vector3(0,20,0),Quaternion.identity);
        yield return null;
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();
        var shot=Gun(vehicle,first);var basic=Gun(vehicle,second);
        var block=OwnedObstacle(vehicle,shot,.65f);
        Physics.SyncTransforms();
        Assert.That(assembly.ClearProjectilePath(shot),Is.False);
        Assert.That(assembly.ClearProjectilePath(basic),Is.True);
        int ammo=first.Ammo;float shotgunAt=first.DeviceReadyAt;
        float basicAt=basic.NextFireTime;
        assembly.FireGroup(false);
        Assert.That(first.Ammo,Is.EqualTo(ammo),"blocked LPM spent shotgun ammo");
        Assert.That(first.DeviceReadyAt,Is.EqualTo(shotgunAt),"blocked LPM spent shotgun cooldown");
        assembly.FireGroup(true);
        Assert.That(basic.NextFireTime,Is.GreaterThan(basicAt),
            "clear RPM Basic group must still fire despite blocked LPM Shotgun");
        Assert.That(first.Ammo,Is.EqualTo(ammo),"firing other group cannot touch shotgun ammo");
        Object.Destroy(vehicle.gameObject);Object.Destroy(colors);Object.Destroy(material);
        R1EncounterManager.ClearTransientObjects();
        yield return null;
    }
}
