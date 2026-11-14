using System.Collections;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogManualRescueTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        for(int i=0;i<300&&run.Transitioning;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));
    }
    [UnityTest] public IEnumerator ManualRescueIsAvailableWhileUprightAndCannotRepeatDuringCooldown()
    {
        var player=run.Player;Assert.That(player,Is.Not.Null);
        run.Encounter.SetCombatActive(false);
        var body=player.GetComponent<Rigidbody>();
        body.rotation=Quaternion.identity;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
        Physics.SyncTransforms();
        var action=typeof(R1RunController).GetMethod("TryEmergencyRescue",BindingFlags.Instance|BindingFlags.Public);
        Assert.That(action,Is.Not.Null,"Combat needs a manually available rescue action");
        float hpBefore=player.GetComponentsInChildren<P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        Assert.That((bool)action.Invoke(run,null),Is.True,"An upright but stuck robot must be eligible");
        Assert.That(player.GetComponentsInChildren<P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.EqualTo(hpBefore).Within(.01f),"Activation must never inflict direct damage");
        Assert.That(run.EmergencyRescueActive,Is.True);Assert.That(run.EmergencyRescueAttackBlocked,Is.True);
        Assert.That(Vector3.Dot(body.rotation*Vector3.up,Vector3.up),Is.GreaterThan(.99f));
        Assert.That((bool)action.Invoke(run,null),Is.False,"A second rescue must respect the cooldown");
        Assert.That(run.EmergencyRescueCooldownLeft,Is.InRange(4.9f,5.05f));
        float retryAt=Time.time+run.EmergencyRescueCooldownLeft+.05f;while(Time.time<retryAt)yield return null;
        body.rotation=Quaternion.Euler(0,0,180);body.linearVelocity=body.angularVelocity=Vector3.zero;Physics.SyncTransforms();
        Assert.That((bool)action.Invoke(run,null),Is.True,"Rescue must be available again after cooldown, regardless of orientation");
        Assert.That(Vector3.Dot(body.rotation*Vector3.up,Vector3.up),Is.GreaterThan(.99f));
    }

    [UnityTest] public IEnumerator CombatRescueHalvesIncomingDamageThenReturnsToNormal()
    {
        run.Encounter.SetCombatActive(false);var player=run.Player;
        var parts=player.GetComponentsInChildren<P12PipelineSafeDamageable>();var ratios=parts.Select(p=>p.CurrentHp/p.MaxHp).ToArray();
        var core=player.Core;float before=parts.Sum(p=>p.CurrentHp);
        var kinds=new[]{R3DamagePropagation.DamageKind.Kinetic,R3DamagePropagation.DamageKind.Piercing,R3DamagePropagation.DamageKind.Blast};
        var normal=new float[kinds.Length];
        for(int k=0;k<kinds.Length;k++)
        {
            core.ApplyDamageResolvedTyped(20,core.transform.position,Vector3.up,kinds[k]);normal[k]=before-parts.Sum(p=>p.CurrentHp);
            Assert.That(normal[k],Is.GreaterThan(0));for(int i=0;i<parts.Length;i++)parts[i].RestoreHealthRatio(ratios[i]);
        }
        Assert.That(run.TryEmergencyRescue(),Is.True);
        Assert.That(parts.Sum(p=>p.CurrentHp),Is.EqualTo(before).Within(.01f));
        var enemy=run.Encounter.Enemies.First().Core;
        float enemyResolved=enemy.ApplyDamageResolvedTyped(20,enemy.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic);
        Assert.That(enemyResolved,Is.EqualTo(20).Within(.001f),"Player rescue must not protect enemy parts");
        for(int k=0;k<kinds.Length;k++)
        {
            core.ApplyDamageResolvedTyped(20,core.transform.position,Vector3.up,kinds[k]);
            float protectedDamage=before-parts.Sum(p=>p.CurrentHp);
            Assert.That(protectedDamage,Is.EqualTo(normal[k]*.5f).Within(.03f),kinds[k].ToString());
            for(int i=0;i<parts.Length;i++)parts[i].RestoreHealthRatio(ratios[i]);
        }
        float end=Time.time+run.EmergencyRescueDurationSeconds+.1f;while(Time.time<end)yield return null;
        Assert.That(run.EmergencyRescueActive,Is.False);Assert.That(run.EmergencyRescueAttackBlocked,Is.False);
        core.ApplyDamageResolvedTyped(20,core.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Blast);
        Assert.That(before-parts.Sum(p=>p.CurrentHp),Is.EqualTo(normal[2]).Within(.03f));
    }
    [UnityTest] public IEnumerator WeaponsAreBlockedOnlyDuringRescue()
    {
        run.Encounter.SetCombatActive(false);var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun&&p.Operational);
        Assert.That(run.TryEmergencyRescue(),Is.True);
        assembly.AimGuns(run.Player.transform.position+run.Player.transform.forward*100+Vector3.up*20);Physics.SyncTransforms();
        gun.NextFireTime=0;assembly.FireGroup(false);
        Assert.That(gun.NextFireTime,Is.Zero,"A locked weapon must not fire or start its shot cooldown");
        float end=Time.time+run.EmergencyRescueDurationSeconds+.1f;while(Time.time<end)yield return null;
        assembly.AimGuns(run.Player.transform.position+run.Player.transform.forward*100+Vector3.up*20);Physics.SyncTransforms();
        Assert.That(assembly.ClearProjectilePath(gun),Is.True,"The baseline must have a real clear firing path");
        gun.NextFireTime=0;assembly.FireGroup(false);
        Assert.That(gun.NextFireTime,Is.GreaterThan(Time.time),"The same weapon must fire again after rescue");
    }
    [UnityTest] public IEnumerator AlreadyActiveAuraDoesNotAttackOrConsumeEnergyDuringRescue()
    {
        run.Encounter.SetCombatActive(false);var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun&&p.Operational);
        var state=run.VehicleModel.Parts.Single(p=>p.Id==gun.Id);
        var aura=gun.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLDamageAuraBinding>();
        aura.Bind(state,run.Player.transform);Assert.That(aura.Fire(),Is.True);
        Assert.That(run.TryEmergencyRescue(),Is.True);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();float energy=bank.Current;
        var tick=aura.GetType().GetMethod("FixedUpdate",BindingFlags.NonPublic|BindingFlags.Instance);
        tick.Invoke(aura,null);Assert.That(bank.Current,Is.EqualTo(energy).Within(.001f));
        float end=Time.time+run.EmergencyRescueDurationSeconds+.1f;while(Time.time<end)yield return null;
        energy=bank.Current;tick.Invoke(aura,null);Assert.That(bank.Current,Is.LessThan(energy),"Aura resumes after protection ends");
    }
    [UnityTest] public IEnumerator ExistingOffensiveTurretPausesItsFireDuringRescue()
    {
        run.Encounter.SetCombatActive(false);var enemy=run.Encounter.Enemies.First();
        var turret=GameObject.CreatePrimitive(PrimitiveType.Cube);turret.transform.position=enemy.Core.transform.position+Vector3.up*8;
        turret.transform.localScale=Vector3.one*.4f;var body=turret.AddComponent<Rigidbody>();body.isKinematic=true;
        var weapon=turret.AddComponent<R3FlyingDrone>();weapon.ConfigureTurret(run.Player.transform,.2f,1);
        Physics.SyncTransforms();Assert.That(run.TryEmergencyRescue(),Is.True);
        var nextShot=typeof(R3FlyingDrone).GetField("nextShot",BindingFlags.NonPublic|BindingFlags.Instance);
        float during=Time.time+.6f;while(Time.time<during)yield return new WaitForFixedUpdate();
        Assert.That((float)nextShot.GetValue(weapon),Is.Zero,"An already deployed turret must not fire while its owner is being rescued.");
        float deadline=Time.time+run.EmergencyRescueDurationSeconds+2f;
        while(Time.time<deadline&&(float)nextShot.GetValue(weapon)<=0)yield return new WaitForFixedUpdate();
        Assert.That((float)nextShot.GetValue(weapon),Is.GreaterThan(0),"The turret must resume firing after rescue.");
        Object.Destroy(turret);
    }
    [UnityTest] public IEnumerator BlockedDestinationsDoNotChargeOrStartRescue()
    {
        run.Encounter.SetCombatActive(false);
        var spawn=(Vector3)typeof(R1RunController).GetField("playerSpawn",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=spawn+Vector3.up*10;wall.transform.localScale=new Vector3(60,20,60);Physics.SyncTransforms();
        float hp=run.Player.Core.CurrentHp;Vector3 old=run.Player.GetComponent<Rigidbody>().position;
        Assert.That(run.TryEmergencyRescue(),Is.False);Assert.That(run.EmergencyRescueCooldownLeft,Is.Zero);
        Assert.That(run.Player.Core.CurrentHp,Is.EqualTo(hp));Assert.That(run.Player.GetComponent<Rigidbody>().position,Is.EqualTo(old));
        Assert.That(run.EmergencyRescueMessage,Does.Contain("Brak bezpiecznego"));Object.Destroy(wall);yield return null;
    }
    [UnityTest] public IEnumerator AutomaticRecoveryUsesTheSameCombatProtectionAndAttackLock()
    {
        run.Encounter.SetCombatActive(false);
        var recovery=run.Player.GetComponent<RoboRougelike.R6.R6CoreRecovery>();Assert.That(recovery,Is.Not.Null);
        var state=recovery.GetType().GetProperty("IsRecovering").GetSetMethod(true);
        state.Invoke(recovery,new object[]{true});Assert.That(run.EmergencyRescueActive,Is.True);Assert.That(run.EmergencyRescueAttackBlocked,Is.True);
        var core=run.Player.Core;
        Assert.That(core.ApplyDamageResolvedTyped(20,core.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic),Is.EqualTo(10).Within(.001f));
        state.Invoke(recovery,new object[]{false});Assert.That(run.EmergencyRescueActive,Is.False);
        Assert.That(core.ApplyDamageResolvedTyped(20,core.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Kinetic),Is.EqualTo(20).Within(.001f));
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}

public sealed class BacklogManualRescueRangeTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);Assert.That(run.EnterTestRange(),Is.True);
        for(int i=0;i<300&&run.Transitioning;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.TestDrive));
    }
    [UnityTest] public IEnumerator OutsideCombatRescueBlocksDamageWithoutChargingHealth()
    {
        var core=run.Player.Core;float hp=core.CurrentHp;
        Assert.That(run.TryEmergencyRescue(),Is.True);
        Assert.That(core.CurrentHp,Is.EqualTo(hp));Assert.That(run.EmergencyRescueAttackBlocked,Is.True);
        foreach(var kind in new[]{R3DamagePropagation.DamageKind.Kinetic,R3DamagePropagation.DamageKind.Piercing,R3DamagePropagation.DamageKind.Blast})
        {core.ApplyDamageResolvedTyped(20,core.transform.position,Vector3.up,kind);Assert.That(core.CurrentHp,Is.EqualTo(hp));}
        float end=Time.time+run.EmergencyRescueDurationSeconds+.1f;while(Time.time<end)yield return null;
        core.ApplyDamage(20,core.transform.position,Vector3.up);Assert.That(core.CurrentHp,Is.LessThan(hp));
    }
    [UnityTest] public IEnumerator GarageRescueProtectsForBoundedRealTimeEvenWithPausedPhysics()
    {
        Assert.That(run.ReturnFromTestRange(),Is.True);
        for(int i=0;i<300&&run.Transitioning;i++)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(Time.timeScale,Is.Zero);
        var core=run.Player.Core;float hp=core.CurrentHp;
        Assert.That(run.TryEmergencyRescue(),Is.True);Assert.That(run.EmergencyRescueActive,Is.True);
        core.ApplyDamage(20,core.transform.position,Vector3.up);Assert.That(core.CurrentHp,Is.EqualTo(hp));
        float end=Time.unscaledTime+run.EmergencyRescueDurationSeconds+.1f;while(Time.unscaledTime<end)yield return null;
        Assert.That(run.EmergencyRescueActive,Is.False);
        core.ApplyDamage(20,core.transform.position,Vector3.up);Assert.That(core.CurrentHp,Is.LessThan(hp));
    }
    [UnityTest] public IEnumerator AutomaticRecoveryOutsideCombatPreventsDamage()
    {
        var recovery=run.Player.GetComponent<RoboRougelike.R6.R6CoreRecovery>();
        var state=recovery.GetType().GetProperty("IsRecovering").GetSetMethod(true);state.Invoke(recovery,new object[]{true});
        var core=run.Player.Core;float hp=core.CurrentHp;
        Assert.That(run.EmergencyRescueActive,Is.True);core.ApplyDamage(20,core.transform.position,Vector3.up);Assert.That(core.CurrentHp,Is.EqualTo(hp));
        state.Invoke(recovery,new object[]{false});Assert.That(run.EmergencyRescueActive,Is.False);yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
