using System.Collections.Generic;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R3DamagePropagationTests
{
    [UnityTearDown] public IEnumerator Cleanup()
    {if(Application.isPlaying)yield return new ExitPlayMode();}
    static R3DamagePropagation.Node Part(int id,float hp,params int[] neighbours) =>
        new R3DamagePropagation.Node(id,hp,neighbours);
    static (Vector3Int cell,string id,Vector3Int rotation) WheelDescriptor(R1RunController run)
    {
        var wheel=run.VehicleModel.Parts.First(p=>p!=null&&p.Kind==R3PartKind.Wheel);
        return (wheel.Cell,wheel.Id,wheel.RotationQuarterTurns);
    }

    [Test]
    public void DamageIsSharedEquallyWithoutCreatingOrLosingDamage()
    {
        var graph=new[]{Part(0,100,1,2),Part(1,100,0),Part(2,100,0)};
        var result=R3DamagePropagation.Resolve(graph,0,100,.25f,2);
        Assert.That(result.Damage[0],Is.EqualTo(75).Within(.001f));
        Assert.That(result.Damage[1],Is.EqualTo(12.5f).Within(.001f));
        Assert.That(result.Damage[2],Is.EqualTo(12.5f).Within(.001f));
        Assert.That(result.Unused,Is.EqualTo(0).Within(.001f));
    }

    [Test]
    public void CyclesDoNotRepeatDamageAndConvergentPathsAreCombined()
    {
        var graph=new[]{Part(0,100,1,2),Part(1,100,0,3),Part(2,100,0,3),Part(3,100,1,2)};
        var result=R3DamagePropagation.Resolve(graph,0,100,.25f,3);
        Assert.That(result.Damage[0],Is.EqualTo(75).Within(.001f));
        Assert.That(result.Damage[1],Is.EqualTo(9.375f).Within(.001f));
        Assert.That(result.Damage[2],Is.EqualTo(9.375f).Within(.001f));
        Assert.That(result.Damage[3],Is.EqualTo(6.25f).Within(.001f));
        Assert.That(result.Unused,Is.EqualTo(0).Within(.001f));
    }

    [Test]
    public void HopLimitStopsPropagationAndPreservesBudget()
    {
        var graph=new[]{Part(0,100,1),Part(1,100,0,2),Part(2,100,1)};
        var result=R3DamagePropagation.Resolve(graph,0,100,.25f,1);
        Assert.That(result.Damage[0],Is.EqualTo(75).Within(.001f));
        Assert.That(result.Damage[1],Is.EqualTo(25).Within(.001f));
        Assert.That(result.Damage.ContainsKey(2),Is.False);
    }

    [Test]
    public void OverflowCannotDestroyUnconnectedPartsAndIsAccountedFor()
    {
        var graph=new[]{Part(0,20,1),Part(1,30,0),Part(2,100)};
        var result=R3DamagePropagation.Resolve(graph,0,100,.25f,2);
        Assert.That(result.Damage[0],Is.EqualTo(20).Within(.001f));
        Assert.That(result.Damage[1],Is.EqualTo(30).Within(.001f));
        Assert.That(result.Damage.ContainsKey(2),Is.False);
        Assert.That(result.Unused,Is.EqualTo(50).Within(.001f));
    }

    [Test]
    public void DamagedBranchRedistributesItsOverflowToOtherReachableParts()
    {
        var graph=new[]{Part(0,100,1,2),Part(1,1,0),Part(2,100,0)};
        var result=R3DamagePropagation.Resolve(graph,0,100,.25f,2);
        Assert.That(result.Damage[0],Is.EqualTo(75).Within(.001f));
        Assert.That(result.Damage[1],Is.EqualTo(1).Within(.001f));
        Assert.That(result.Damage[2],Is.EqualTo(24).Within(.001f),"A surviving sibling can absorb the other branch's excess.");
        Assert.That(result.Unused,Is.EqualTo(0).Within(.001f),"Reachable HP prevents damage from disappearing.");
    }

    [Test]
    public void ProjectileProfilesChangeTheSpreadWithoutChangingDamageBudget()
    {
        var graph=new[]{Part(0,100,1),Part(1,100,0)};
        var kinetic=R3DamagePropagation.Resolve(graph,0,100,
            R3DamagePropagation.TransferFraction(R3DamagePropagation.DamageKind.Kinetic),2);
        var piercing=R3DamagePropagation.Resolve(graph,0,100,
            R3DamagePropagation.TransferFraction(R3DamagePropagation.DamageKind.Piercing),2);
        var blast=R3DamagePropagation.Resolve(graph,0,100,
            R3DamagePropagation.TransferFraction(R3DamagePropagation.DamageKind.Blast),2);
        Assert.That(piercing.Damage[1],Is.LessThan(kinetic.Damage[1]));
        Assert.That(blast.Damage[1],Is.GreaterThan(kinetic.Damage[1]));
        foreach(var result in new[]{kinetic,piercing,blast})
            Assert.That(result.Damage.Values.Sum()+result.Unused,Is.EqualTo(100).Within(.001f));
    }

    [Test]
    public void DifferentLayoutsTradeCoreProtectionAcrossImpactTypes()
    {
        // Same five full-health parts; 1 is the core, 0 receives the hit.
        var compact=new[]{Part(0,100,1,2,3),Part(1,100,0),Part(2,100,0,4),Part(3,100,0),Part(4,100,2)};
        var routed=new[]{Part(0,100,2),Part(1,100,2,3),Part(2,100,0,1),Part(3,100,1,4),Part(4,100,3)};
        var kinetic=R3DamagePropagation.TransferFraction(R3DamagePropagation.DamageKind.Kinetic);
        var blast=R3DamagePropagation.TransferFraction(R3DamagePropagation.DamageKind.Blast);
        float Core(R3DamagePropagation.Node[] graph,float fraction)=>R3DamagePropagation.Resolve(graph,0,100,fraction,2).Damage[1];
        Assert.That(Core(routed,kinetic),Is.LessThan(Core(compact,kinetic)),"A narrow route can protect the core from kinetic impacts.");
        Assert.That(Core(routed,blast),Is.GreaterThan(Core(compact,blast)),"The same route has a blast trade-off.");
    }

    [Test]
    public void DiverseConnectedLayoutsNeverDuplicateOrEraseTheHitBudget()
    {
        var random=new System.Random(1153);
        for(int trial=0;trial<300;trial++)
        {
            int count=random.Next(2,41);
            var neighbours=Enumerable.Range(0,count).Select(_=>new List<int>()).ToArray();
            void Link(int a,int b) { if(!neighbours[a].Contains(b)){neighbours[a].Add(b);neighbours[b].Add(a);} }
            for(int i=1;i<count;i++)Link(i,random.Next(i));
            for(int i=0;i<count;i++)if(random.NextDouble()<.4) { int j=random.Next(count);if(i!=j)Link(i,j); }
            var graph=Enumerable.Range(0,count).Select(i=>Part(i,random.Next(1,121),neighbours[i].ToArray())).ToArray();
            int hit=random.Next(count),hops=random.Next(0,4);
            float amount=random.Next(1,3001),fraction=(float)random.NextDouble();
            var result=R3DamagePropagation.Resolve(graph,hit,amount,fraction,hops);
            Assert.That(result.Damage.Values.Sum()+result.Unused,Is.EqualTo(amount).Within(.02f),$"trial {trial}");
            foreach(var entry in result.Damage)
                Assert.That(entry.Value,Is.InRange(0,graph[entry.Key].Health+.001f),$"trial {trial}, part {entry.Key}");
            Assert.That(result.Unused,Is.GreaterThanOrEqualTo(0),$"trial {trial}");
        }
    }

    [UnityTest]
    public IEnumerator RemountedSingleContactWheelTransfersDamageThroughItsRealConnection()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return VerifyRemountedWheelDamage();
    }

    static IEnumerator VerifyRemountedWheelDamage()
    {
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        Assert.That(run.VehicleModel,Is.Not.Null);
        var wheel=WheelDescriptor(run);
        var cell=wheel.cell;var id=wheel.id;var rotation=wheel.rotation;
        Assert.That(run.GarageRemove(cell),Is.True);yield return null;
        Assert.That(run.GarageMount(id,cell,rotation),Is.True);yield return null;
        var state=run.VehicleModel.Parts.Single(p=>p.Id==id);
        Assert.That(state.ContactMask==R3MountingGrid.FaceBit(Vector3Int.left)||state.ContactMask==R3MountingGrid.FaceBit(Vector3Int.right),Is.True);
        Assert.That(run.Player,Is.Not.Null);
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();Assert.That(assembly,Is.Not.Null);
        var parts=assembly.Parts.Where(p=>p!=null&&p.Operational).ToArray();
        foreach(var part in parts)Assert.That(part.Damageable,Is.Not.Null,part.name);
        var before=parts.ToDictionary(p=>p.Id,p=>p.Damageable.CurrentHp);var target=parts.Single(p=>p.Id==id);
        target.Damageable.ApplyDamage(8,target.transform.position,Vector3.forward);
        Assert.That(before[id]-target.Damageable.CurrentHp,Is.EqualTo(6).Within(.001f));
        Assert.That(parts.Where(p=>p!=target).Sum(p=>before[p.Id]-p.Damageable.CurrentHp),Is.EqualTo(2).Within(.001f));
    }

    [UnityTest]
    public IEnumerator RealPlayerVehicleRoutesOneHitToConnectedParts()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        yield return null;
        ValidatePlayerHit(run);
    }

    static void ValidatePlayerHit(R1RunController run)
    {
        Assert.That(run.Player,Is.Not.Null);
        var parts=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p!=null&&p.Operational).ToArray();
        Assert.That(parts.Length,Is.GreaterThan(0));
        var before=new Dictionary<string,float>();
        foreach(var part in parts)
        {
            Assert.That(part.Damageable,Is.Not.Null,part.name);
            before.Add(part.Id,part.Damageable.CurrentHp);
        }
        var target=parts.Single(p=>p.Cell==new Vector3Int(1,0,0));
        target.Damageable.ApplyDamage(8,target.transform.position,Vector3.forward);
        Assert.That(before[target.Id]-target.Damageable.CurrentHp,Is.EqualTo(6).Within(.001f),
            "A connected hit retains 75% on the struck part with the prototype profile.");
        Assert.That(parts.Where(p=>p!=target).Sum(p=>before[p.Id]-p.Damageable.CurrentHp),Is.EqualTo(2).Within(.001f),
            "The remaining 25% is applied to connected parts, without duplication.");
        before=parts.ToDictionary(p=>p.Id,p=>p.Damageable.CurrentHp);
        target.Damageable.ApplyDamageResolvedTyped(10,target.transform.position,Vector3.forward,
            R3DamagePropagation.DamageKind.Piercing);
        Assert.That(before[target.Id]-target.Damageable.CurrentHp,Is.EqualTo(9).Within(.001f),"Piercing impacts transfer 10%.");
        Assert.That(parts.Where(p=>p!=target).Sum(p=>before[p.Id]-p.Damageable.CurrentHp),Is.EqualTo(1).Within(.001f));
        before=parts.ToDictionary(p=>p.Id,p=>p.Damageable.CurrentHp);
        var rocket=target.GetComponent<RoboRougelike.WeaponIntegration.RRLRocketDamageReceiver>();
        Assert.That(rocket,Is.Not.Null);
        rocket.ReceiveRocketDamage(10,target.transform.position,Vector3.up,null);
        Assert.That(before[target.Id]-target.Damageable.CurrentHp,Is.EqualTo(6).Within(.001f),"Rocket impacts use the blast profile.");
        Assert.That(parts.Where(p=>p!=target).Sum(p=>before[p.Id]-p.Damageable.CurrentHp),Is.EqualTo(4).Within(.001f));
    }

    [UnityTest]
    public IEnumerator RealEnemyVehicleUsesTheSameConnectedDamageRules()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.StartRun(),Is.True);
        if(run.Progress.State==R1RunState.Route)Assert.That(run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<end)yield return null;
        run.Encounter.SetCombatActive(false);yield return null;
        ValidateEnemyHit(run);
    }

    [UnityTest]
    public IEnumerator BruiserWheelTransfersThroughAxleNotAdjacentWheel()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(run.Transitioning,Is.False);
        var template=(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret)typeof(R1RunController)
            .GetField("enemyTemplate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,run.Player.transform,new R1EncounterDefinition
            {EnemyCount=1,Roster=new[]{R3EnemyArchetype.Bruiser}},run.Salvage,"wheel-port-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();
        var wheels=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>()
            .Where(RoboRougelike.R6.R6VehicleDrive.IsWheel).ToArray();
        var target=wheels.Single(w=>{var c=w.GetComponent<RogueMachine.P12PipelineSafe.P12_PipelineSafePlayerBlock>().LogicalGridPosition;return c.x==3&&c.z==0;});
        var adjacent=wheels.Single(w=>{var c=w.GetComponent<RogueMachine.P12PipelineSafe.P12_PipelineSafePlayerBlock>().LogicalGridPosition;return c.x==3&&c.z==1;});
        float targetBefore=target.CurrentHp,adjacentBefore=adjacent.CurrentHp;
        target.ApplyDamage(8,target.transform.position,Vector3.forward);
        Assert.That(targetBefore-target.CurrentHp,Is.EqualTo(6).Within(.001f),"Only one real axle transfers a quarter of the hit.");
        Assert.That(adjacent.CurrentHp,Is.EqualTo(adjacentBefore).Within(.001f),"Side-by-side wheel meshes do not form a direct socket.");
    }

    [UnityTest]
    public IEnumerator EnemyAdditionalGunTransfersThroughItsActualBase()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(run.Transitioning,Is.False);run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First(e=>e!=null&&e.AdditionalWeapons.Count>0);
        ValidateAdditionalGunDamage(enemy);
    }

    [UnityTest]
    public IEnumerator EliteGunsConnectOnlyToTheirSideSupports()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        float end=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
        var run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(run.Transitioning,Is.False);run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();
        enemy.ConfigureVariant(RoboRougelike.R5.R5EnemyVariant.EliteGunner);
        ValidateEliteGunSockets(enemy);
    }

    static void ValidateEliteGunSockets(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret enemy)
    {
        Assert.That(enemy.VariantWeapons.Count,Is.EqualTo(2));
        foreach(var gun in enemy.VariantWeapons)
        {
            var side=enemy.transform.InverseTransformPoint(gun.Block.transform.position).x;
            var inward=side<0?Vector3Int.right:Vector3Int.left;
            var ports=gun.Block.GetComponent<R3BlockMountPoints>();
            Assert.That(ports,Is.Not.Null,"Elite guns must declare their real attachment.");
            Assert.That(ports.WorldMask,Is.EqualTo(R3MountingGrid.FaceBit(inward)));
        }
    }

    static void ValidateAdditionalGunDamage(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret enemy)
    {
        var weapons=enemy.AdditionalWeapons;
        var gun=weapons[0];
        Assert.That(gun,Is.Not.Null,"The weapon entry must exist.");
        Assert.That(gun.Block!=null,Is.True,"The additional gun must remain attached.");
        Assert.That(gun.Support!=null,Is.True,"The gun must retain its support.");
        var ports=gun.Block.GetComponent<R3BlockMountPoints>();Assert.That(ports,Is.Not.Null);
        Assert.That(ports.WorldMask,Is.EqualTo(R3MountingGrid.FaceBit(Vector3Int.down)));
        float gunBefore=gun.Block.CurrentHp,supportBefore=gun.Support.CurrentHp;
        var others=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>()
            .Where(p=>p!=gun.Block&&p!=gun.Support&&!p.IsDead).ToDictionary(p=>p,p=>p.CurrentHp);
        gun.Block.ApplyDamage(8,gun.Block.transform.position,Vector3.forward);
        Assert.That(gunBefore-gun.Block.CurrentHp,Is.EqualTo(6).Within(.001f));
        Assert.That(supportBefore-gun.Support.CurrentHp,Is.EqualTo(1.5f).Within(.001f),"The base retains its share before the next hop.");
        Assert.That(others.Where(p=>p.Key!=null).Sum(p=>p.Value-p.Key.CurrentHp),Is.EqualTo(.5f).Within(.001f),"The remainder reaches the next connected ring.");
    }

    static void ValidateEnemyHit(R1RunController run)
    {
        var enemy=run.Encounter.Enemies.First();
        var target=enemy.Core;Assert.That(target,Is.Not.Null);
        var parts=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>()
            .Where(p=>p!=null&&!p.IsDead).ToArray();
        var before=parts.ToDictionary(p=>p.GetInstanceID(),p=>p.CurrentHp);
        target.ApplyDamage(8,target.transform.position,Vector3.forward);
        Assert.That(before[target.GetInstanceID()]-target.CurrentHp,Is.EqualTo(6).Within(.001f));
        Assert.That(parts.Where(p=>p!=target).Sum(p=>before[p.GetInstanceID()]-p.CurrentHp),Is.EqualTo(2).Within(.001f));
    }
}
