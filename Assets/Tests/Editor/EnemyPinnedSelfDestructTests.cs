using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class EnemyPinnedSelfDestructTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float until=Time.realtimeSinceStartup+20;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<until)yield return null;
        var run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<until)yield return null;
        Assert.That(run.Transitioning,Is.False);run.Encounter.SetCombatActive(false);
    }
    [UnityTest] public IEnumerator DetachedIntactPrimaryGunDoesNotKeepEnemyArmed()
    {
        var enemy=R1RunController.Instance.Encounter.Enemies.First();
        var yaw=enemy.transform.Find("Enemy_Yaw");
        Assert.That(yaw,Is.Not.Null);Assert.That(enemy.WeaponBlock.IsDead,Is.False);
        Assert.That(enemy.AdditionalWeapons,Is.Not.Empty);
        yaw.SetParent(null,true);
        Assert.That(enemy.WeaponOnline,Is.True,"A still attached extra gun remains usable.");
        foreach(var gun in enemy.AdditionalWeapons)gun.Block.transform.SetParent(null,true);
        Assert.That(enemy.WeaponBlock.IsDead,Is.False,"The detached primary remains physically intact.");
        Assert.That(enemy.WeaponOnline,Is.False,"Detached guns cannot arm or fire for the enemy.");
        yield return null;
    }
    [UnityTest] public IEnumerator InactiveOrDetachedWeaponComponentsDoNotKeepEnemyArmed()
    {
        var enemy=R1RunController.Instance.Encounter.Enemies.First();
        var extras=enemy.AdditionalWeapons.ToArray();Assert.That(extras.Length,Is.GreaterThan(0));
        var primary=enemy.WeaponBlock;Assert.That(primary,Is.Not.Null);
        foreach(var gun in extras)gun.Block.gameObject.SetActive(false);
        Assert.That(enemy.WeaponOnline,Is.True,"The primary is still operational.");
        primary.gameObject.SetActive(false);
        Assert.That(enemy.WeaponOnline,Is.False,"Inactive guns cannot keep the enemy armed.");
        primary.gameObject.SetActive(true);
        Assert.That(enemy.WeaponOnline,Is.True,"Reactivating the primary restores its state.");
        primary.gameObject.SetActive(false);
        var extra=extras[0];extra.Block.gameObject.SetActive(true);
        Assert.That(enemy.WeaponOnline,Is.True);
        var pivotParent=extra.Pivot.parent;
        try
        {
            extra.Pivot.SetParent(null,true);
            Assert.That(enemy.WeaponOnline,Is.False,"A detached aiming assembly cannot operate as an attached gun.");
        }
        finally{if(extra.Pivot!=null)extra.Pivot.SetParent(pivotParent,true);}
        Assert.That(enemy.WeaponOnline,Is.True);
        var muzzleParent=extra.Muzzle.parent;
        try
        {
            extra.Muzzle.SetParent(null,true);
            Assert.That(enemy.WeaponOnline,Is.False,"A detached muzzle cannot fire for the vehicle.");
        }
        finally{if(extra.Muzzle!=null)extra.Muzzle.SetParent(muzzleParent,true);}
        yield return null;
    }
    [UnityTest] public IEnumerator TemporarilyInactivePrimaryGunRemainsAttachedDuringCombat()
    {
        var enemy=R1RunController.Instance.Encounter.Enemies.First();
        foreach(var gun in enemy.AdditionalWeapons)gun.Block.gameObject.SetActive(false);
        var primary=enemy.WeaponBlock;var yaw=enemy.transform.Find("Enemy_Yaw");
        Assert.That(primary,Is.Not.Null);Assert.That(yaw,Is.Not.Null);
        enemy.SetCombatActive(true);primary.gameObject.SetActive(false);
        yield return null;
        Assert.That(enemy.WeaponOnline,Is.False);
        Assert.That(enemy.HasAttachedWeapon,Is.True,"Temporarily unavailable guns remain physically attached.");
        Assert.That(yaw.IsChildOf(enemy.transform),Is.True,"Inactive intact weapons must not be dropped as wrecks.");
        primary.gameObject.SetActive(true);
        Assert.That(enemy.WeaponOnline,Is.True,"An intact weapon must be usable again after reactivation.");
    }
    [UnityTest] public IEnumerator TemporarilyInactiveWeaponsDoNotStartSelfDestruct()
    {
        var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        run.Player.GetComponent<Rigidbody>().isKinematic=true;enemy.GetComponent<Rigidbody>().isKinematic=true;
        enemy.transform.position=run.Player.transform.position+Vector3.forward*2;Physics.SyncTransforms();
        foreach(var gun in enemy.AdditionalWeapons)gun.Block.gameObject.SetActive(false);
        enemy.WeaponBlock.gameObject.SetActive(false);enemy.SetCombatActive(true);
        float started=Time.time,deadline=Time.realtimeSinceStartup+15;
        while(Time.time-started<5.5f&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(Time.time-started,Is.GreaterThanOrEqualTo(5f),"The warning duration must actually elapse.");
        Assert.That(enemy.HasAttachedWeapon,Is.True);
        Assert.That(enemy.IsDefeated,Is.False,"An inactive but intact weapon must not trigger self-destruction.");
    }
    [UnityTest] public IEnumerator MobileButUnarmedEnemyExplodesNearPlayerAfterWarning()
    {
        var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;
        var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        enemy.transform.SetPositionAndRotation(run.Player.transform.position+Vector3.forward*2,Quaternion.identity);Physics.SyncTransforms();
        foreach(var gun in enemy.AdditionalWeapons.Where(g=>g.Block!=null).ToArray())gun.Block.ApplyDamage(100000,gun.Block.transform.position,Vector3.up);
        if(enemy.WeaponBlock!=null)enemy.WeaponBlock.ApplyDamage(100000,enemy.WeaponBlock.transform.position,Vector3.up);
        Assert.That(enemy.WeaponOnline,Is.False);Assert.That(enemy.ActiveWheelCount,Is.GreaterThan(0),"Regression requires a mobile but disarmed enemy.");
        var target=run.Player.GetComponentsInChildren<P12PipelineSafeDamageable>().First(p=>!p.IsDead&&Vector3.Distance(p.transform.position,enemy.Core.transform.position)<6);
        float hp=target.CurrentHp;enemy.SetCombatActive(true);float started=Time.time,deadline=Time.realtimeSinceStartup+15;
        while(Time.time-started<4.6f&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.False,"The five-second warning must not be skipped.");
        while(!enemy.IsDefeated&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.True,"Unarmed enemy at close range must no longer push indefinitely.");
        Assert.That(target==null||target.IsDead||target.CurrentHp<hp,Is.True,"The existing blast must affect nearby player parts.");
    }
    [UnityTest] public IEnumerator UnarmedEnemyDetectsNearbyPartOfLargeVehicleOutsideRootRadius()
    {
        var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        var player=run.Player.GetComponent<Rigidbody>();player.isKinematic=true;
        var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        var root=run.Player.transform.position;
        var target=run.Player.GetComponentsInChildren<P12PipelineSafeDamageable>()
            .Where(p=>!p.IsDead).OrderByDescending(p=>Vector3.ProjectOnPlane(p.transform.position-root,Vector3.up).sqrMagnitude).First();
        var direction=Vector3.ProjectOnPlane(target.transform.position-root,Vector3.up).normalized;
        Assert.That(direction.sqrMagnitude,Is.GreaterThan(.9f));
        var desiredCore=target.transform.position+direction*5.5f;
        enemy.transform.position+=desiredCore-enemy.Core.transform.position;
        Physics.SyncTransforms();
        Assert.That(Vector3.Distance(enemy.transform.position,root),Is.GreaterThan(6f),"The vehicle origins must be outside the old trigger radius.");
        Assert.That(Vector3.Distance(enemy.Core.transform.position,target.transform.position),Is.LessThan(6f),"A real player part must be inside the blast radius.");
        foreach(var gun in enemy.AdditionalWeapons.Where(g=>g.Block!=null).ToArray())gun.Block.ApplyDamage(100000,gun.Block.transform.position,Vector3.up);
        if(enemy.WeaponBlock!=null)enemy.WeaponBlock.ApplyDamage(100000,enemy.WeaponBlock.transform.position,Vector3.up);
        Assert.That(enemy.WeaponOnline,Is.False);
        float hp=target.CurrentHp;enemy.SetCombatActive(true);float started=Time.time,deadline=Time.realtimeSinceStartup+15;
        while(Time.time-started<4.6f&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.False,"The five-second warning must remain.");
        while(!enemy.IsDefeated&&Time.time-started<7f&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.True,"A nearby part must trigger the same blast even when the vehicle origin is far away.");
        Assert.That(target==null||target.IsDead||target.CurrentHp<hp,Is.True);
    }
    [UnityTest] public IEnumerator UnarmedEnemyWaitsForPlayerAfterWarningWhenFarAway()
    {
        var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        run.Player.GetComponent<Rigidbody>().isKinematic=true;enemy.GetComponent<Rigidbody>().isKinematic=true;
        enemy.transform.position=run.Player.transform.position+Vector3.forward*30;
        foreach(var gun in enemy.AdditionalWeapons.Where(g=>g.Block!=null).ToArray())gun.Block.ApplyDamage(100000,gun.Block.transform.position,Vector3.up);
        if(enemy.WeaponBlock!=null)enemy.WeaponBlock.ApplyDamage(100000,enemy.WeaponBlock.transform.position,Vector3.up);
        Assert.That(enemy.WeaponOnline,Is.False);enemy.SetCombatActive(true);
        float started=Time.time,deadline=Time.realtimeSinceStartup+15;
        while(Time.time-started<5.5f&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.False,"A distant player must not trigger a blast.");
        enemy.transform.position=run.Player.transform.position+Vector3.forward*2;Physics.SyncTransforms();
        while(!enemy.IsDefeated&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.True,"Once the warning elapsed, a nearby player triggers the blast.");
    }
    [UnityTest] public IEnumerator SelfDestructionUsesBlastDamageOnConnectedPlayerParts()
    {
        var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        run.Player.GetComponent<Rigidbody>().isKinematic=true;enemy.GetComponent<Rigidbody>().isKinematic=true;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        var all=run.Player.GetComponentsInChildren<P12PipelineSafeDamageable>().Where(h=>h!=null&&!h.IsDead).ToArray();
        var directions=new[]{new Vector3(1,0,1).normalized,new Vector3(1,0,-1).normalized,
            new Vector3(-1,0,1).normalized,new Vector3(-1,0,-1).normalized};
        R3MountedPart target=null;Vector3 center=default;
        foreach(var part in assembly.Parts.Where(p=>p!=null&&p.Kind==R3PartKind.Armor&&p.Operational))
        {
            bool linked=assembly.Parts.Any(other=>other!=null&&other!=part&&other.Operational&&
                (other.Cell-part.Cell).sqrMagnitude==1&&R3MountingGrid.Compatible(part.Block,part.Cell,other.Block,other.Cell));
            if(!linked)continue;
            foreach(var direction in directions)
            {
                var proposed=part.transform.position+direction*5.98f;
                if(all.Count(h=>Vector3.Distance(h.transform.position,proposed)<=6f)!=1||
                    Vector3.Distance(part.transform.position,proposed)>6f)continue;
                target=part;center=proposed;break;
            }
            if(target!=null)break;
        }
        Assert.That(target,Is.Not.Null,"The real vehicle must expose one connected armor part at the blast edge.");
        enemy.transform.position+=center-enemy.Core.transform.position;Physics.SyncTransforms();
        foreach(var gun in enemy.AdditionalWeapons.Where(g=>g.Block!=null).ToArray())gun.Block.ApplyDamage(100000,gun.Block.transform.position,Vector3.up);
        if(enemy.WeaponBlock!=null)enemy.WeaponBlock.ApplyDamage(100000,enemy.WeaponBlock.transform.position,Vector3.up);
        Assert.That(enemy.WeaponOnline,Is.False);float before=target.Damageable.CurrentHp;
        float impact=80f*Mathf.Clamp01(1f-Vector3.Distance(target.transform.position,enemy.Core.transform.position)/8f);
        enemy.SetCombatActive(true);float deadline=Time.realtimeSinceStartup+15;
        while(!enemy.IsDefeated&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.True);
        float direct=before-target.Damageable.CurrentHp;
        Assert.That(direct,Is.EqualTo(impact*(1-R3DamagePropagation.TransferFraction(R3DamagePropagation.DamageKind.Blast))).Within(.1f),
            "A self-destruction is blast damage, so the same physical connection rules as other explosions apply.");
    }
    [UnityTest] public IEnumerator UnarmedEnemyDoesNotExplodeWhenOnlyOriginsAreWithinRadius()
    {
        var run=R1RunController.Instance;var enemy=run.Encounter.Enemies.First();
        run.Player.GetComponent<Rigidbody>().isKinematic=true;enemy.GetComponent<Rigidbody>().isKinematic=true;
        // Observe the proximity component in isolation: the live run would
        // otherwise immediately end when this one-part fixture has no gun.
        run.enabled=false;
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        foreach(var part in assembly.Parts)
            if(part!=null&&part.Damageable!=run.Player.Core)
            {part.Block.MarkDetached();part.gameObject.SetActive(false);}
        enemy.transform.rotation=Quaternion.identity;
        enemy.transform.position=run.Player.transform.position+Vector3.forward*5.99f;
        Physics.SyncTransforms();
        Assert.That(Vector3.Distance(enemy.transform.position,run.Player.transform.position),Is.LessThan(6f));
        Assert.That(Vector3.Distance(enemy.Core.transform.position,run.Player.Core.transform.position),Is.GreaterThan(6f),
            "The origins are close but the only surviving player part is outside the actual blast radius.");
        foreach(var gun in enemy.AdditionalWeapons.Where(g=>g.Block!=null).ToArray())gun.Block.ApplyDamage(100000,gun.Block.transform.position,Vector3.up);
        if(enemy.WeaponBlock!=null)enemy.WeaponBlock.ApplyDamage(100000,enemy.WeaponBlock.transform.position,Vector3.up);
        Assert.That(enemy.WeaponOnline,Is.False);enemy.SetCombatActive(true);
        float started=Time.time,deadline=Time.realtimeSinceStartup+30;
        while(Time.time-started<5.3f&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(Time.time-started,Is.GreaterThanOrEqualTo(5.3f),"The five-second warning must actually elapse.");
        Assert.That(enemy.IsDefeated,Is.False,"An empty root position alone must not trigger the blast.");
        enemy.transform.position-=Vector3.forward*.2f;Physics.SyncTransforms();
        Assert.That(Vector3.Distance(enemy.Core.transform.position,run.Player.Core.transform.position),Is.LessThan(6f));
        deadline=Time.realtimeSinceStartup+10;
        while(!enemy.IsDefeated&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.True,"Once the actual player core enters range, the warned enemy must explode.");
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
