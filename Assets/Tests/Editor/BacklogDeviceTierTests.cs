using System.Linq;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R1;
using RoboRougelike.R6;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogDeviceTierTests
{
    R3VehicleModel Model()=>new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
    [TestCase("TargetedEmp")][TestCase("AreaEmp")]
    public void EmpDevicesAreActiveSlotsAndSupportFusionAndCheckpoint(string name)
    {
        Assert.That(System.Enum.TryParse(name,out R3PartKind kind),Is.True,"Missing EMP device: "+name);
        Assert.That(R3VehicleModel.IsActiveSlotPart(kind),Is.True);
        var model=Model();var part=FuseThree(model,kind);Mount(model,part);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(model.MakeCheckpoint(0),R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==part.Id).Tier,Is.EqualTo(2));
    }
    R3PartState FuseThree(R3VehicleModel model,R3PartKind kind)
    {
        model.Award(2000);
        for(int i=0;i<3;i++)Assert.That(model.PurchaseToInventory(kind),Is.True);
        var ids=model.Inventory.Where(p=>p.Kind==kind).Take(3).Select(p=>p.Id).ToArray();
        Assert.That(model.FuseInventory(ids),Is.True);
        return model.Inventory.Single(p=>p.Kind==kind&&p.Tier==2);
    }
    void Mount(R3VehicleModel model,R3PartState part)
    {
        var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        var cell=cells.First(c=>model.OwnedPlacementError(part.Id,c,default)==null);
        Assert.That(model.Mount(part.Id,cell),Is.True);model.RefreshEnergyLayout();
    }
    [Test] public void TierTwoBatteryProvidesTwoHundredCapacity()
    {
        var model=Model();float baseline=model.Energy.Maximum;var part=FuseThree(model,R3PartKind.Battery);Mount(model,part);
        Assert.That(model.Energy.Maximum,Is.EqualTo(baseline+200));
    }
    [Test] public void TierTwoSalvageOptimizerCanBeMountedAndRestored()
    {
        var model=Model();float baseline=model.Energy.Maximum;var part=FuseThree(model,R3PartKind.SalvageOptimizer);Mount(model,part);
        Assert.That(model.Energy.Maximum,Is.EqualTo(baseline-25));
        var checkpoint=model.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==part.Id).Tier,Is.EqualTo(2));
        Assert.That(restored.Energy.Maximum,Is.EqualTo(baseline-25));
    }
    [TestCase(R3PartKind.DoubleShot,25)][TestCase(R3PartKind.DoubleShotOverdrive,0)]
    public void TierTwoProjectileModulesFuseAndSurviveCheckpoint(R3PartKind kind,float reservation)
    {
        var model=Model();float baseline=model.Energy.Maximum;var part=FuseThree(model,kind);Mount(model,part);
        Assert.That(model.Energy.Maximum,Is.EqualTo(baseline-reservation));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(model.MakeCheckpoint(0),R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==part.Id).Tier,Is.EqualTo(2));
    }
    [Test] public void PointDefenseCanBeFusedAndRestoredAsTierTwo()
    {
        var model=Model();var part=FuseThree(model,R3PartKind.PointDefense);Mount(model,part);
        Assert.That(part.Tier,Is.EqualTo(2));
        var checkpoint=model.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==part.Id).Tier,Is.EqualTo(2));
    }
    [Test] public void TierTwoRegeneratorImprovesRecoveryAndReservesTwentyFive()
    {
        var model=Model();float baseline=model.Energy.Maximum;var part=FuseThree(model,R3PartKind.EnergyRegenerator);Mount(model,part);
        Assert.That(model.Energy.Maximum,Is.EqualTo(baseline-25));
        Assert.That(model.Energy.TrySpend(model.Energy.Current),Is.True);model.Energy.Advance(1);
        Assert.That(model.Energy.Current,Is.EqualTo(20).Within(.001));
    }
    [Test] public void TierThreeRegeneratorHasFourTimesTheEffectAndTwentyFiveReservation()
    {
        var model=Model();model.Award(2000);float baseline=model.Energy.Maximum;
        for(int i=0;i<9;i++)Assert.That(model.PurchaseToInventory(R3PartKind.EnergyRegenerator),Is.True);
        for(int i=0;i<3;i++)Assert.That(model.FuseInventory(model.Inventory.Where(p=>p.Kind==R3PartKind.EnergyRegenerator&&p.Tier==1).Take(3).Select(p=>p.Id).ToArray()),Is.True);
        Assert.That(model.FuseInventory(model.Inventory.Select(p=>p.Id).ToArray()),Is.True);
        var part=model.Inventory.Single();Assert.That(part.Tier,Is.EqualTo(3));Mount(model,part);
        Assert.That(model.Energy.Maximum,Is.EqualTo(baseline-25));
        Assert.That(model.Energy.TrySpend(model.Energy.Current),Is.True);model.Energy.Advance(1);
        Assert.That(model.Energy.Current,Is.EqualTo(30).Within(.001));
    }
}

public sealed class BacklogDeviceTierGameplayTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        run.VehicleModel.Award(500);
    }
    R3PartState FuseAndMount(R3PartKind kind)
    {
        for(int i=0;i<3;i++)Assert.That(run.GaragePurchase(kind),Is.True);
        var ids=run.VehicleModel.Inventory.Where(p=>p.Kind==kind).Take(3).Select(p=>p.Id).ToArray();
        Assert.That(run.GarageFuse(ids),Is.True);
        var part=run.VehicleModel.Inventory.Single(p=>p.Kind==kind&&p.Tier==2);
        var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        var cell=cells.First(c=>run.VehicleModel.OwnedPlacementError(part.Id,c,default)==null);
        Assert.That(run.GarageMount(part.Id,cell),Is.True);return part;
    }
    [UnityTest] public IEnumerator EmpDevicesDisruptRealEnemyWithoutDamageAndPreserveCooldown()
    {
        FuseAndMount(R3PartKind.TargetedEmp);FuseAndMount(R3PartKind.AreaEmp);
        Assert.That(run.EnterTestRange(),Is.True);
        for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        Assert.That(run.CanUseMovement,Is.True);
        var template=(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,run.Player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner}},run.Salvage,"new-emp-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();enemy.ConfigureMovement(0,12,0);
        run.Player.GetComponent<Rigidbody>().position=new Vector3(100,5,100);
        enemy.GetComponent<Rigidbody>().position=new Vector3(100,4,106);Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        var target=enemy.WeaponBlock;var receiver=target.GetComponent<RoboRougelike.Mechanics.EMP.EmpReceiver>();
        Assert.That(receiver,Is.Not.Null);
        float hp=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        var device=run.Player.GetComponent<R3EmpDevices>();var energy=run.Player.GetComponent<VehicleEnergyBank>();
        Assert.That(device,Is.Not.Null);float before=energy.Current;
        Assert.That(device.TryTargeted(null),Is.False);Assert.That(energy.Current,Is.EqualTo(before));
        Assert.That(device.TryTargeted(run.Player.GetComponentInChildren<Collider>()),Is.False);
        Assert.That(energy.Current,Is.EqualTo(before),"An invalid target must not consume energy");
        Assert.That(device.TryTargeted(target.GetComponent<Collider>()),Is.True);
        Assert.That(receiver.IsDisrupted,Is.True);Assert.That(energy.Current,Is.EqualTo(before-18).Within(.1));
        Assert.That(device.TryTargeted(target.GetComponent<Collider>()),Is.False);
        receiver.ClearEmp();
        var all=enemy.GetComponentsInChildren<RoboRougelike.Mechanics.EMP.EmpReceiver>();
        foreach(var r in all)r.ClearEmp();
        Assert.That(device.TryArea(),Is.True);
        Assert.That(all.Any(r=>r.IsDisrupted),Is.True,"Area pulse must reach at least one electronic component");
        Assert.That(device.TryArea(),Is.False);
        Assert.That(enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.EqualTo(hp));
        var checkpoint=run.VehicleModel.MakeCheckpoint(Time.time);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,run.VehicleModel.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.TargetedEmpReadyAt,Is.GreaterThan(Time.time));Assert.That(restored.AreaEmpReadyAt,Is.GreaterThan(Time.time));
    }
    [UnityTest] public IEnumerator ChargeCapacityAndRechargePulseWorkOnMountedEmpWithoutDeletingOverflow()
    {
        run.VehicleModel.Award(2000); // Three independent 3:1 fusions at standard shop prices.
        Assert.That(System.Enum.TryParse("ChargeCapacity",out R3PartKind capacityKind),Is.True);
        Assert.That(System.Enum.TryParse("RechargePulse",out R3PartKind pulseKind),Is.True);
        Assert.That(System.Enum.TryParse("ChargeRegenerator",out R3PartKind regeneratorKind),Is.True);
        FuseAndMount(R3PartKind.AreaEmp);FuseAndMount(capacityKind);FuseAndMount(pulseKind);FuseAndMount(regeneratorKind);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var model=run.VehicleModel;var emp=player.GetComponent<R3EmpDevices>();
        var charges=model.Charges;const string pool="emp-area";
        Assert.That(charges.Maximum(pool),Is.EqualTo(5));Assert.That(charges.Current(pool),Is.EqualTo(5));
        var pulseType=System.Type.GetType("RoboRougelike.R3.R3RechargePulse, Assembly-CSharp");Assert.That(pulseType,Is.Not.Null);
        var pulse=player.GetComponent(pulseType);Assert.That(pulse,Is.Not.Null);
        var activate=pulseType.GetMethod("TryActivate");var battery=player.GetComponent<VehicleEnergyBank>();float before=battery.Current;
        Assert.That(activate.Invoke(pulse,null),Is.EqualTo(false),"Full charges cannot consume energy or a cooldown");
        Assert.That(battery.Current,Is.EqualTo(before));
        Assert.That(emp.TryArea(),Is.True);Assert.That(charges.Current(pool),Is.EqualTo(4));
        Assert.That(activate.Invoke(pulse,null),Is.EqualTo(true));
        Assert.That(charges.Current(pool),Is.EqualTo(5));Assert.That(activate.Invoke(pulse,null),Is.EqualTo(false));
        var capacity=player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==capacityKind);
        capacity.Damageable.Configure(1,true);capacity.Damageable.ApplyDamage(2,capacity.transform.position,Vector3.up);
        Assert.That(capacity.Damageable.IsDead,Is.True,"Losing an actual capacity module must lower the limit");
        player.GetComponent<VehicleEnergyBank>().Refresh();
        Assert.That(charges.Maximum(pool),Is.EqualTo(3));Assert.That(charges.Current(pool),Is.EqualTo(5));
        charges.Advance(100,1);Assert.That(charges.Current(pool),Is.EqualTo(5));
        Assert.That(charges.TrySpend(pool,3),Is.True);
        var bank=player.GetComponent<VehicleEnergyBank>();
        var tick=typeof(VehicleEnergyBank).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
        for(int i=0;i<501;i++)tick.Invoke(bank,null); // 10.02 simulated seconds at T2's .1 charge/s.
        Assert.That(charges.Current(pool),Is.EqualTo(3),"A mounted T2 regenerator must restore a charge");
        var saved=model.MakeCheckpoint(Time.time);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Charges.Current(pool),Is.EqualTo(3));Assert.That(restored.RechargePulseReadyAt,Is.GreaterThan(Time.time));
    }
    [UnityTest] public IEnumerator EmpWithoutChargeOrEnergyCannotConsumeEitherResource()
    {
        FuseAndMount(R3PartKind.AreaEmp);Assert.That(run.EnterTestRange(),Is.True);
        for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var emp=run.Player.GetComponent<R3EmpDevices>();var charges=run.VehicleModel.Charges;
        var battery=run.Player.GetComponent<VehicleEnergyBank>();
        Assert.That(charges.TrySpend(R3EmpDevices.AreaPool,charges.Current(R3EmpDevices.AreaPool)),Is.True);
        float energy=battery.Current;Assert.That(emp.TryArea(),Is.False);Assert.That(battery.Current,Is.EqualTo(energy));
        Assert.That(emp.Status(R3PartKind.AreaEmp),Does.Contain("brak ładunków"));
        Assert.That(charges.Recharge(R3EmpDevices.AreaPool,1),Is.EqualTo(1));Assert.That(battery.TrySpend(battery.Current),Is.True);
        Assert.That(emp.TryArea(),Is.False);Assert.That(charges.Current(R3EmpDevices.AreaPool),Is.EqualTo(1));
        Assert.That(emp.Status(R3PartKind.AreaEmp),Does.Contain("brak energii"));
    }
    [UnityTest] public IEnumerator RampGeneratorPlacesOneDamageableDrivableSurfaceBehindMovingVehicle()
    {
        run.VehicleModel.Award(2000);FuseAndMount(R3PartKind.RampDeployer);FuseAndMount(R3PartKind.RechargePulse);
        Assert.That(run.EnterTestRange(),Is.True);
        for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(80,1,80);
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();body.linearVelocity=Vector3.forward*3;
        var charges=run.VehicleModel.Charges;var bank=player.GetComponent<VehicleEnergyBank>();
        Assert.That(service,Is.Not.Null);Assert.That(charges.Current(R3DeployableService.RampPool),Is.EqualTo(3));
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=body.position+Vector3.back*4;blocker.transform.localScale=new Vector3(20,10,20);Physics.SyncTransforms();
        float before=bank.Current;Assert.That(service.TryDeployRamp(),Is.False,"An obstructed placement must fail safely");
        Assert.That(bank.Current,Is.EqualTo(before));Assert.That(charges.Current(R3DeployableService.RampPool),Is.EqualTo(3));
        blocker.SetActive(false);Object.Destroy(blocker);Physics.SyncTransforms();
        Assert.That(service.TryDeployRamp(),Is.True);Assert.That(charges.Current(R3DeployableService.RampPool),Is.EqualTo(2));
        Assert.That(bank.Current,Is.EqualTo(before-12).Within(.1));Assert.That(service.TryDeployRamp(),Is.False,"Copies share a cooldown");
        Assert.That(player.GetComponent<R3RechargePulse>().TryActivate(),Is.True);
        Assert.That(charges.Current(R3DeployableService.RampPool),Is.EqualTo(3),"The shared recharge pulse also supports deployables without EMP installed");
        var ramp=GameObject.Find("Rozstawiona rampa");Assert.That(ramp,Is.Not.Null);
        var collider=ramp.GetComponent<Collider>();Assert.That(collider,Is.Not.Null);
        Assert.That(RoboRougelike.R6.R6WheelContacts.Terrain(collider),Is.True,"The existing wheel solver must accept the real ramp surface");
        Assert.That(ramp.GetComponent<R3DeployableSurface>().TeamId,Is.EqualTo(1));
        var otherModel=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var other=R3VehicleAssembly.Build(otherModel,new R1RunStats(),run.GetComponent<R3VisualPalette>(),ramp.transform.position+Vector3.up*1.5f,ramp.transform.rotation);
        var drive=other.GetComponent<RoboRougelike.R6.R6VehicleDrive>();bool wheelsOnRamp=false;
        float deadline=Time.realtimeSinceStartup+6;
        while(Time.realtimeSinceStartup<deadline&&!wheelsOnRamp)
        {
            yield return new WaitForFixedUpdate();drive.RefreshMass();
            wheelsOnRamp=drive.GroundedWheels>0&&Vector3.Dot(drive.GroundNormal,ramp.transform.up)>.98f;
        }
        Assert.That(wheelsOnRamp,Is.True,"An actual R6 vehicle must acquire wheel contact with the deployed ramp");
        Object.Destroy(other.gameObject);
        Assert.That(RoboRougelike.WeaponIntegration.RRLHitRouter.CanHit(collider,player.transform),Is.False,"Friendly damage must be filtered");
        var enemy=new GameObject("enemy source");enemy.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().enabled=false;
        Assert.That(RoboRougelike.WeaponIntegration.RRLHitRouter.Apply(collider,1000,ramp.transform.position,Vector3.forward,enemy.transform),Is.True);
        Assert.That(ramp.GetComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().IsDead,Is.True);
        Object.Destroy(enemy);Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator WallAndJumpPadUseIndependentChargesAndLaunchARealVehicle()
    {
        run.VehicleModel.Award(2000);FuseAndMount(R3PartKind.WallDeployer);FuseAndMount(R3PartKind.JumpPadDeployer);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();body.linearVelocity=Vector3.forward*3;
        Assert.That(service.TryDeploy(R3PartKind.WallDeployer),Is.True);
        var wall=GameObject.Find("Rozstawiony murek");Assert.That(wall,Is.Not.Null);Assert.That(wall.transform.localScale.y,Is.EqualTo(2));
        Assert.That(run.VehicleModel.Charges.Current(R3DeployableService.WallPool),Is.EqualTo(2));
        Assert.That(run.VehicleModel.Charges.Current(R3DeployableService.JumpPadPool),Is.EqualTo(3));
        body.position=new Vector3(120,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();body.linearVelocity=Vector3.forward*3;
        Assert.That(service.TryDeploy(R3PartKind.JumpPadDeployer),Is.True);
        var pad=GameObject.Find("Rozstawiony jump pad");Assert.That(pad,Is.Not.Null);
        var jump=pad.GetComponent<R3JumpPad>();Assert.That(jump,Is.Not.Null);
        Assert.That(run.VehicleModel.Charges.Current(R3DeployableService.JumpPadPool),Is.EqualTo(2));
        Assert.That(jump.ActivationCount,Is.Zero,"The placement must not launch the deploying vehicle");
        body.position=pad.transform.position+Vector3.up*1.3f;body.rotation=Quaternion.identity;body.linearVelocity=Vector3.down;Physics.SyncTransforms();
        float deadline=Time.realtimeSinceStartup+5;bool lifted=false;
        while(Time.realtimeSinceStartup<deadline&&jump.ActivationCount==0)
        {yield return MOVSeriesDeviceTests.FixedStep();if(body.linearVelocity.y>2)lifted=true;}
        Assert.That(jump.ActivationCount,Is.GreaterThan(0),"The actual physics trigger must recognize the vehicle");
        Assert.That(jump.LastActivatedBody,Is.SameAs(body));
        float peak=body.linearVelocity.y;
        for(int i=0;i<10;i++){yield return MOVSeriesDeviceTests.FixedStep();peak=Mathf.Max(peak,body.linearVelocity.y);}
        Assert.That(lifted||peak>2,Is.True,$"The real Rigidbody must receive an upward impulse (peak={peak:0.00}, kinematic={body.isKinematic})");
        Assert.That(jump.ActivationCount,Is.EqualTo(1),"Multiple part colliders must not launch the same vehicle repeatedly");
        Assert.That(peak,Is.LessThan(8),"The pad must not multiply its impulse by the number of vehicle colliders");
        Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator ShieldInterceptsHostileRocketCrossingEntireDomeInOnePhysicsStep()=>FastShieldRocket(false);
    [UnityTest] public IEnumerator WallBeforeDomeStopsFastRocketWithoutDamagingShield()=>FastShieldRocket(true);
    [UnityTest] public IEnumerator FastFriendlyRocketMayCrossDomeFromOutside()=>FastShieldRocketExit(true);
    [UnityTest] public IEnumerator FastHostileRocketLaunchedInsideMayLeaveDome()=>FastShieldRocketExit(false);
    IEnumerator FastShieldRocketExit(bool friendly)
    {
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        Vector3 center=new Vector3(1000,1000,1000);
        var dome=new GameObject("Test przejścia szybkiej rakiety");dome.transform.position=center;
        var boundary=dome.AddComponent<SphereCollider>();boundary.isTrigger=true;
        var hp=dome.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(500,false);
        dome.AddComponent<R3ShieldDome>().Configure(1,2);
        var owner=new GameObject("Źródło rakiety");
        if(friendly)owner.AddComponent<RailgunFriendlyFixture>();else owner.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().enabled=false;
        var ownerBody=owner.AddComponent<Rigidbody>();ownerBody.isKinematic=true;
        var rocket=GameObject.CreatePrimitive(PrimitiveType.Sphere);rocket.transform.position=center+(friendly?Vector3.left*8:Vector3.zero);rocket.transform.localScale=Vector3.one*.16f;
        var body=rocket.AddComponent<Rigidbody>();rocket.AddComponent<R3ShieldRocketPayload>();
        var projectile=rocket.AddComponent<RoboRougelike.Mechanics.Rockets.RocketProjectile>();
        var config=new RoboRougelike.Mechanics.Rockets.RocketConfig{initialSpeed=2000,maxSpeed=2000,acceleration=0,maxLifetime=4};
        Assert.That(projectile.TryInitialize(config.Snapshot(),ownerBody,owner.transform,owner,null,Vector3.right),Is.True);
        Physics.SyncTransforms();float deadline=Time.time+.12f;
        while(Time.time<deadline&&projectile!=null&&!projectile.IsResolved&&body.position.x<center.x+5)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(projectile!=null&&!projectile.IsResolved&&body.position.x>center.x+2,Is.True,"The rocket must cross without interception.");
        Assert.That(hp.CurrentHp,Is.EqualTo(500).Within(.001));
        if(rocket!=null)Object.Destroy(rocket);Object.Destroy(dome);Object.Destroy(owner);
    }
    IEnumerator FastShieldRocket(bool wallFirst)
    {
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        Vector3 center=new Vector3(1000,1000,1000);
        var dome=new GameObject("Test szybkiej rakiety");dome.transform.position=center;
        var boundary=dome.AddComponent<SphereCollider>();boundary.isTrigger=true;
        var hp=dome.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(500,false);
        dome.AddComponent<R3ShieldDome>().Configure(1,2);
        var protectedTarget=GameObject.CreatePrimitive(PrimitiveType.Cube);protectedTarget.transform.position=center;
        var protectedHp=protectedTarget.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();protectedHp.Configure(500,false);
        protectedTarget.AddComponent<RoboRougelike.WeaponIntegration.RRLRocketDamageReceiver>();
        var owner=new GameObject("Źródło wrogiej rakiety");owner.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().enabled=false;
        var ownerBody=owner.AddComponent<Rigidbody>();ownerBody.isKinematic=true;
        GameObject wall=null;if(wallFirst){wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=center+Vector3.left*4;wall.transform.localScale=new Vector3(.5f,8,8);}
        var rocket=GameObject.CreatePrimitive(PrimitiveType.Sphere);rocket.transform.position=center+Vector3.left*8;rocket.transform.localScale=Vector3.one*.16f;
        rocket.AddComponent<Rigidbody>();rocket.AddComponent<R3ShieldRocketPayload>();var projectile=rocket.AddComponent<RoboRougelike.Mechanics.Rockets.RocketProjectile>();
        var config=new RoboRougelike.Mechanics.Rockets.RocketConfig{initialSpeed=2000,maxSpeed=2000,acceleration=0,maxLifetime=4};
        Assert.That(projectile.TryInitialize(config.Snapshot(),ownerBody,owner.transform,owner,null,Vector3.right),Is.True);
        Physics.SyncTransforms();float deadline=Time.time+.12f;
        while(Time.time<deadline&&projectile!=null&&!projectile.IsResolved&&hp.CurrentHp>=500)yield return MOVSeriesDeviceTests.FixedStep();
        if(wallFirst)Assert.That(hp.CurrentHp,Is.EqualTo(500).Within(.001),"A solid wall hit first must protect the dome.");
        else Assert.That(hp.CurrentHp,Is.LessThan(500),"A fast hostile rocket must be intercepted by the dome.");
        Assert.That(protectedHp.CurrentHp,Is.EqualTo(500).Within(.001),"The protected target must not take the intercepted blast.");
        Assert.That(projectile==null||projectile.IsResolved,Is.True);
        if(rocket!=null)Object.Destroy(rocket);if(wall!=null)Object.Destroy(wall);Object.Destroy(protectedTarget);Object.Destroy(dome);Object.Destroy(owner);
    }
    [UnityTest] public IEnumerator ShieldInterceptsHostileShellCrossingEntireDomeInOnePhysicsStep()=>FastShieldShell(false);
    [UnityTest] public IEnumerator SolidWallBeforeDomeStopsFastShellWithoutDamagingShield()=>FastShieldShell(true);
    [UnityTest] public IEnumerator FastFriendlyShellMayCrossDomeFromOutside()=>FastShieldExit(true);
    [UnityTest] public IEnumerator FastHostileShellLaunchedInsideMayLeaveDome()=>FastShieldExit(false);
    IEnumerator FastShieldExit(bool friendly)
    {
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        Vector3 center=new Vector3(1000,1000,1000);
        var dome=new GameObject("Test swobodnego przejścia przez kopułę");dome.transform.position=center;
        var boundary=dome.AddComponent<SphereCollider>();boundary.isTrigger=true;
        var hp=dome.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(500,false);
        dome.AddComponent<R3ShieldDome>().Configure(1,2);
        var owner=new GameObject("Źródło pocisku");
        if(friendly)owner.AddComponent<RailgunFriendlyFixture>();else owner.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().enabled=false;
        var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.transform.position=center+(friendly?Vector3.left*8:Vector3.zero);shell.transform.localScale=Vector3.one*.16f;
        var body=shell.AddComponent<Rigidbody>();body.useGravity=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearVelocity=Vector3.right*2000;
        shell.AddComponent<RogueMachine.P12PipelineSafe.P17PhysicalShell>().Configure(owner.transform,20,4,Vector3.right,null);
        Physics.SyncTransforms();float deadline=Time.time+.12f;
        while(Time.time<deadline&&shell!=null&&shell.activeInHierarchy&&shell.transform.position.x<center.x+5)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(shell!=null&&shell.activeInHierarchy&&shell.transform.position.x>center.x+2,Is.True,"The shell must cross the boundary without interception.");
        Assert.That(hp.CurrentHp,Is.EqualTo(500).Within(.001));
        if(shell!=null)Object.Destroy(shell);Object.Destroy(dome);Object.Destroy(owner);
    }
    IEnumerator FastShieldShell(bool wallFirst)
    {
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        Vector3 center=new Vector3(1000,1000,1000);
        var dome=new GameObject("Test szybkiej kopuły");dome.transform.position=center;
        var boundary=dome.AddComponent<SphereCollider>();boundary.isTrigger=true;
        var hp=dome.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(500,false);
        dome.AddComponent<R3ShieldDome>().Configure(1,2);
        var protectedTarget=GameObject.CreatePrimitive(PrimitiveType.Cube);protectedTarget.transform.position=center;
        var protectedHp=protectedTarget.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();protectedHp.Configure(500,false);
        var owner=new GameObject("Źródło wrogiego pocisku");owner.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().enabled=false;
        GameObject wall=null;
        if(wallFirst){wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=center+Vector3.left*4;wall.transform.localScale=new Vector3(.5f,8,8);}
        var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.transform.position=center+Vector3.left*8;shell.transform.localScale=Vector3.one*.16f;
        var body=shell.AddComponent<Rigidbody>();body.useGravity=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearVelocity=Vector3.right*2000;
        shell.AddComponent<RogueMachine.P12PipelineSafe.P17PhysicalShell>().Configure(owner.transform,20,4,Vector3.right,null);
        Physics.SyncTransforms();float deadline=Time.time+.12f;
        while(Time.time<deadline&&shell!=null&&shell.activeInHierarchy&&hp.CurrentHp>=500)yield return MOVSeriesDeviceTests.FixedStep();
        if(wallFirst){Assert.That(hp.CurrentHp,Is.EqualTo(500).Within(.001),"A solid wall hit first must protect the dome.");Assert.That(shell==null||!shell.activeInHierarchy,Is.True,"The shell must actually hit the wall.");}
        else {Assert.That(hp.CurrentHp,Is.LessThan(500),"A fast hostile shell must not cross the dome without being intercepted.");Assert.That(shell==null||!shell.activeInHierarchy,Is.True);}
        Assert.That(protectedHp.CurrentHp,Is.EqualTo(500).Within(.001),"The target behind the shield must not also take damage.");
        if(shell!=null)Object.Destroy(shell);if(wall!=null)Object.Destroy(wall);Object.Destroy(protectedTarget);Object.Destroy(dome);Object.Destroy(owner);
    }
    [UnityTest] public IEnumerator ShieldDomeLetsFriendlyShotsOutAndBlocksEnemyShellAndRocket()
    {
        Assert.That(System.Enum.TryParse("ShieldTurretGenerator",out R3PartKind kind),Is.True,"Missing shield turret generator");
        run.VehicleModel.Award(2500);FuseAndMount(kind);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();body.linearVelocity=Vector3.forward*3;
        Assert.That(service.TryDeploy(kind),Is.True);var dome=GameObject.Find("Kopuła osłonowa");Assert.That(dome,Is.Not.Null);
        var shield=dome.GetComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();var sphere=dome.GetComponent<SphereCollider>();
        Assert.That(shield,Is.Not.Null);Assert.That(sphere,Is.Not.Null);float radius=sphere.radius*dome.transform.lossyScale.x;
        body.isKinematic=true;body.position=dome.transform.position+new Vector3(0,2,2);Physics.SyncTransforms();
        var template=(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner}},run.Salvage,"shield-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();enemy.ConfigureMovement(0,12,0);var enemyBody=enemy.GetComponent<Rigidbody>();
        enemyBody.position=new Vector3(140,2,140);enemyBody.isKinematic=true;Physics.SyncTransforms();
        GameObject Shell(Vector3 pos,Vector3 velocity,Transform owner)
        {
            var item=GameObject.CreatePrimitive(PrimitiveType.Sphere);item.transform.position=pos;item.transform.localScale=Vector3.one*.16f;
            var rb=item.AddComponent<Rigidbody>();rb.useGravity=false;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.linearVelocity=velocity;
            item.AddComponent<RogueMachine.P12PipelineSafe.P17PhysicalShell>().Configure(owner,20,4,velocity.normalized,null);return item;
        }
        Vector3 center=dome.transform.position;var friendly=Shell(center+Vector3.up*2,Vector3.right*30,player.transform);
        float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&friendly!=null&&friendly.transform.position.x<center.x+radius+1)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(friendly!=null&&friendly.activeInHierarchy&&friendly.transform.position.x>center.x+radius,Is.True,"Friendly shells must leave the dome");
        Object.Destroy(friendly);yield return null; // Do not let opposing test projectiles collide with each other outside the shield.
        // Fly above the parked vehicle: an unprotected enemy shot may otherwise legitimately hit it.
        float hpInside=shield.CurrentHp;var inside=Shell(center+Vector3.right+Vector3.up*4,Vector3.left*30,enemy.transform);
        deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&inside!=null&&inside.transform.position.x>center.x-radius-1)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(inside!=null&&inside.activeInHierarchy&&inside.transform.position.x<center.x-radius,Is.True,"A hostile shot originating inside must not be mistaken for an incoming shot");
        Assert.That(shield.CurrentHp,Is.EqualTo(hpInside));Object.Destroy(inside);yield return null;
        float before=shield.CurrentHp;var hostile=Shell(center+Vector3.right*(radius+2)+Vector3.up*2,Vector3.left*30,enemy.transform);
        deadline=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<deadline&&shield!=null&&shield.CurrentHp>=before)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(shield.CurrentHp,Is.LessThan(before),"The actual incoming shell must damage shield HP");
        Assert.That(hostile==null||!hostile.activeInHierarchy,Is.True,"The incoming shell must be stopped");
        before=shield.CurrentHp;
        var rocket=GameObject.CreatePrimitive(PrimitiveType.Sphere);rocket.transform.position=center+Vector3.right*(radius+2)+Vector3.up*2;
        var rocketBody=rocket.AddComponent<Rigidbody>();rocketBody.useGravity=false;
        var rocketProjectile=rocket.AddComponent<RoboRougelike.Mechanics.Rockets.RocketProjectile>();
        var config=new RoboRougelike.Mechanics.Rockets.RocketConfig{initialSpeed=25,maxSpeed=25,acceleration=0,maxLifetime=4};
        Assert.That(rocketProjectile.TryInitialize(config.Snapshot(),enemyBody,enemy.transform,enemy.gameObject,null,Vector3.left),Is.True);
        deadline=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<deadline&&shield!=null&&shield.CurrentHp>=before)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(shield.CurrentHp,Is.LessThan(before),"The actual incoming rocket must damage shield HP");
        Assert.That(rocketProjectile==null||rocketProjectile.IsResolved,Is.True,"The incoming rocket must be intercepted");
        shield.ApplyDamage(10000,center,Vector3.up);yield return null;Assert.That(dome==null||!dome.activeInHierarchy,Is.True);
        if(friendly!=null)Object.Destroy(friendly);Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator SupportTurretRechargesNearbyOwnerWithoutFiring()
    {
        Assert.That(System.Enum.TryParse("SupportTurretGenerator",out R3PartKind kind),Is.True,"Missing support turret generator");
        run.VehicleModel.Award(2500);FuseAndMount(kind);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();body.linearVelocity=Vector3.forward*3;
        Assert.That(service.TryDeploy(kind),Is.True);var turret=GameObject.Find("Wieżyczka wspierająca");Assert.That(turret,Is.Not.Null);
        Assert.That(turret.GetComponent<R3FlyingDrone>(),Is.Null,"Support turret must not fire weapons");
        var bank=player.GetComponent<VehicleEnergyBank>();Assert.That(bank.TrySpend(100),Is.True);
        bank.enabled=false; // Isolate the support contribution from ordinary regeneration.
        body.isKinematic=true;body.position=turret.transform.position+new Vector3(0,2,2);Physics.SyncTransforms();
        float before=bank.Current;float deadline=Time.realtimeSinceStartup+8;
        while(Time.realtimeSinceStartup<deadline&&bank.Current<=before+.01f)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(bank.Current,Is.GreaterThan(before));Assert.That(bank.Current,Is.LessThanOrEqualTo(bank.Maximum));
        body.position=new Vector3(140,2,140);Physics.SyncTransforms();yield return MOVSeriesDeviceTests.FixedStep();
        float outside=bank.Current;for(int i=0;i<15;i++)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(bank.Current,Is.EqualTo(outside).Within(.001),"Leaving range must stop the support recharge");
        body.position=turret.transform.position+new Vector3(0,2,2);Physics.SyncTransforms();
        var turretHp=turret.GetComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();
        turretHp.ApplyDamage(10000,turret.transform.position,Vector3.up);yield return null;
        float afterDestruction=bank.Current;for(int i=0;i<15;i++)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(turret==null||!turret.activeInHierarchy,Is.True);
        Assert.That(bank.Current,Is.EqualTo(afterDestruction).Within(.001),"Destroyed support must stop providing energy");
        bank.enabled=true;Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator SupportTurretRechargesAlliesOnceButNotEnemiesOrDistantVehicles()
    {
        run.VehicleModel.Award(2500);FuseAndMount(R3PartKind.SupportTurretGenerator);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var playerBody=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        playerBody.position=new Vector3(100,2,100);playerBody.rotation=Quaternion.identity;playerBody.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();playerBody.linearVelocity=Vector3.forward*3;
        Assert.That(service.TryDeploy(R3PartKind.SupportTurretGenerator),Is.True);
        var turret=GameObject.Find("Wieżyczka wspierająca");Assert.That(turret,Is.Not.Null);
        var palette=run.GetComponent<R3VisualPalette>();
        R1VehicleState MakeAlly(Vector3 position)
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
            var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,position,Quaternion.identity);
            vehicle.GetComponent<Rigidbody>().isKinematic=true;var energy=vehicle.GetComponent<VehicleEnergyBank>();
            Assert.That(energy.TrySpend(100),Is.True);energy.enabled=false;return vehicle;
        }
        playerBody.isKinematic=true;playerBody.position=turret.transform.position+new Vector3(0,2,2);
        var ownerEnergy=player.GetComponent<VehicleEnergyBank>();Assert.That(ownerEnergy.TrySpend(100),Is.True);ownerEnergy.enabled=false;
        var ally=MakeAlly(turret.transform.position+new Vector3(3,2,0));var nearby=ally.GetComponent<VehicleEnergyBank>();
        var far=MakeAlly(turret.transform.position+new Vector3(30,2,0));var distant=far.GetComponent<VehicleEnergyBank>();
        var enemy=run.GetComponent<R6TestRange>().TrainingTarget;var enemyBody=enemy.GetComponent<Rigidbody>();enemyBody.isKinematic=true;enemyBody.position=turret.transform.position+new Vector3(-3,2,0);
        var hostile=enemy.GetComponent<VehicleEnergyBank>()??enemy.gameObject.AddComponent<VehicleEnergyBank>();hostile.ConfigureEnemy(enemy.Core);
        Assert.That(hostile.TrySpend(100),Is.True);hostile.enabled=false;
        Physics.SyncTransforms();float ownBefore=ownerEnergy.Current,allyBefore=nearby.Current,farBefore=distant.Current,enemyBefore=hostile.Current;
        for(int i=0;i<15;i++)yield return MOVSeriesDeviceTests.FixedStep();
        float ownerGain=ownerEnergy.Current-ownBefore,allyGain=nearby.Current-allyBefore;
        Assert.That(ownerGain,Is.GreaterThan(.1));Assert.That(allyGain,Is.EqualTo(ownerGain).Within(.1),"Many colliders must not multiply recharge.");
        Assert.That(distant.Current,Is.EqualTo(farBefore).Within(.001));Assert.That(hostile.Current,Is.EqualTo(enemyBefore).Within(.001));
        ownerEnergy.enabled=true;hostile.enabled=true;Object.Destroy(ally.gameObject);Object.Destroy(far.gameObject);Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator HealingTurretRepairsWeakestAlliedVehiclePartButNotEnemyOrDistantPart()
    {
        run.VehicleModel.Award(2500);FuseAndMount(R3PartKind.HealingTurretGenerator);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();body.linearVelocity=Vector3.forward*3;
        Assert.That(service.TryDeploy(R3PartKind.HealingTurretGenerator),Is.True);
        var turret=GameObject.Find("Wieżyczka lecznicza");Assert.That(turret,Is.Not.Null);
        body.isKinematic=true;body.position=turret.transform.position+new Vector3(0,2,2);
        var palette=run.GetComponent<R3VisualPalette>();
        R1VehicleState MakeAlly(Vector3 position)
        {
            var vehicle=R3VehicleAssembly.Build(new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light)),new R1RunStats(),palette,position,Quaternion.identity);
            vehicle.GetComponent<Rigidbody>().isKinematic=true;return vehicle;
        }
        var ally=MakeAlly(turret.transform.position+new Vector3(3,2,0));
        var allyParts=ally.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.Armor).Take(2).ToArray();
        Assert.That(allyParts.Length,Is.EqualTo(2));var weakest=allyParts[0].Damageable;var stronger=allyParts[1].Damageable;
        weakest.RestoreHealthRatio(.3f);stronger.RestoreHealthRatio(.6f);
        var far=MakeAlly(turret.transform.position+new Vector3(30,2,0));var distant=far.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor).Damageable;
        distant.RestoreHealthRatio(.1f);
        var enemy=run.GetComponent<R6TestRange>().TrainingTarget;var enemyBody=enemy.GetComponent<Rigidbody>();enemyBody.isKinematic=true;enemyBody.position=turret.transform.position+new Vector3(-3,2,0);
        var hostile=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().First(p=>p!=enemy.Core);
        hostile.RestoreHealthRatio(.2f);Physics.SyncTransforms();
        float before=weakest.CurrentHp,strongerBefore=stronger.CurrentHp,farBefore=distant.CurrentHp,enemyBefore=hostile.CurrentHp;
        for(int i=0;i<15;i++)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(weakest.CurrentHp,Is.GreaterThan(before),"The weakest allied vehicle part should be repaired.");
        Assert.That(stronger.CurrentHp,Is.EqualTo(strongerBefore).Within(.001),"Only the weakest eligible part is repaired.");
        Assert.That(distant.CurrentHp,Is.EqualTo(farBefore).Within(.001));Assert.That(hostile.CurrentHp,Is.EqualTo(enemyBefore).Within(.001));
        Object.Destroy(ally.gameObject);Object.Destroy(far.gameObject);Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator HealingTurretRepairsTheAlliedShieldEmitter()
    {
        run.VehicleModel.Award(2500);FuseAndMount(R3PartKind.HealingTurretGenerator);FuseAndMount(R3PartKind.ShieldTurretGenerator);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();body.linearVelocity=Vector3.forward*3;
        Assert.That(service.TryDeploy(R3PartKind.HealingTurretGenerator),Is.True);
        var healer=GameObject.Find("Wieżyczka lecznicza");Assert.That(healer,Is.Not.Null);
        body.position+=Vector3.right*5;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        Assert.That(service.TryDeploy(R3PartKind.ShieldTurretGenerator),Is.True);
        var dome=GameObject.Find("Kopuła osłonowa");Assert.That(dome,Is.Not.Null);
        var hp=dome.GetComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.RestoreHealthRatio(.3f);float before=hp.CurrentHp;
        Physics.SyncTransforms();for(int i=0;i<15;i++)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(hp.CurrentHp,Is.GreaterThan(before),"A friendly shield emitter is a repairable allied device.");
        Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator HealingTurretRepairsNearbyOwnedPartsWithoutAttacking()
    {
        Assert.That(System.Enum.TryParse("HealingTurretGenerator",out R3PartKind kind),Is.True,"Missing healing turret generator");
        run.VehicleModel.Award(2500);FuseAndMount(kind);FuseAndMount(R3PartKind.FlyingDroneGenerator);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();body.linearVelocity=Vector3.forward*3;
        Assert.That(service.TryDeploy(kind),Is.True);var turret=GameObject.Find("Wieżyczka lecznicza");Assert.That(turret,Is.Not.Null);
        Assert.That(turret.GetComponent<R3FlyingDrone>(),Is.Null,"Healing turret must not fire the offensive weapon");
        var assembly=player.GetComponent<R3VehicleAssembly>();var part=assembly.Parts.First(p=>p.Kind==R3PartKind.Armor&&p.Operational);
        part.Damageable.RestoreHealthRatio(.5f);float before=part.Damageable.CurrentHp;
        body.isKinematic=true;body.position=turret.transform.position+new Vector3(0,2,2);Physics.SyncTransforms();
        float deadline=Time.realtimeSinceStartup+8;
        while(Time.realtimeSinceStartup<deadline&&part.Damageable.CurrentHp<=before+.01f)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(part.Damageable.CurrentHp,Is.GreaterThan(before),"Nearby physical armor must actually recover HP");
        Assert.That(part.Damageable.CurrentHp,Is.LessThanOrEqualTo(part.Damageable.MaxHp));
        part.Damageable.RestoreHealthRatio(1);
        Assert.That(service.TryDeploy(R3PartKind.FlyingDroneGenerator),Is.True);
        var drone=GameObject.Find("Dron latający");Assert.That(drone,Is.Not.Null);
        drone.GetComponent<R3FlyingDrone>().enabled=false;
        var droneHp=drone.GetComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();droneHp.RestoreHealthRatio(.5f);
        float droneBefore=droneHp.CurrentHp;
        var template=(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner}},run.Salvage,"healing-enemy-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();enemy.ConfigureMovement(0,12,0);var enemyBody=enemy.GetComponent<Rigidbody>();
        enemyBody.position=turret.transform.position+new Vector3(3,1,0);enemyBody.isKinematic=true;Physics.SyncTransforms();
        var enemyHp=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().First(p=>!p.IsDead);enemyHp.RestoreHealthRatio(.25f);float hostileBefore=enemyHp.CurrentHp;
        deadline=Time.realtimeSinceStartup+8;
        while(Time.realtimeSinceStartup<deadline&&droneHp.CurrentHp<=droneBefore+.01f)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(droneHp.CurrentHp,Is.GreaterThan(droneBefore),"The turret must heal a nearby allied drone");
        Assert.That(enemyHp.CurrentHp,Is.EqualTo(hostileBefore).Within(.001),"It must never heal enemies");
        body.position=new Vector3(140,2,140);Physics.SyncTransforms();yield return MOVSeriesDeviceTests.FixedStep();
        float outside=part.Damageable.CurrentHp;for(int i=0;i<15;i++)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(part.Damageable.CurrentHp,Is.EqualTo(outside).Within(.001),"Leaving the radius must stop healing");
        body.position=turret.transform.position+new Vector3(0,2,2);Physics.SyncTransforms();
        var turretHp=turret.GetComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();
        turretHp.ApplyDamage(10000,turret.transform.position,Vector3.up);yield return null;
        float afterDestruction=part.Damageable.CurrentHp;for(int i=0;i<15;i++)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(turret==null||!turret.activeInHierarchy,Is.True);
        Assert.That(part.Damageable.CurrentHp,Is.EqualTo(afterDestruction).Within(.001),"Destroyed turrets must stop healing");
        Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator OffensiveTurretDeploysBehindMovingVehicleAndFiresRealProjectiles()
    {
        Assert.That(System.Enum.TryParse("OffensiveTurretGenerator",out R3PartKind kind),Is.True,"Missing offensive turret generator");
        run.VehicleModel.Award(2500);FuseAndMount(kind);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();body.linearVelocity=Vector3.zero;
        var bank=player.GetComponent<VehicleEnergyBank>();float energy=bank.Current;
        Assert.That(service.TryDeploy(kind),Is.False,"Turrets require movement to deploy behind the vehicle");Assert.That(bank.Current,Is.EqualTo(energy));
        body.linearVelocity=Vector3.forward*3;Assert.That(service.TryDeploy(kind),Is.True);
        var turret=GameObject.Find("Wieżyczka ofensywna");Assert.That(turret,Is.Not.Null);
        Assert.That(turret.transform.position.z,Is.LessThan(body.position.z));var origin=turret.transform.position;
        var durability=turret.GetComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();Assert.That(durability,Is.Not.Null);
        var template=(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner}},run.Salvage,"turret-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();enemy.ConfigureMovement(0,12,0);var enemyBody=enemy.GetComponent<Rigidbody>();
        enemyBody.position=origin+new Vector3(12,1,0);enemyBody.isKinematic=true;Physics.SyncTransforms();
        float hp=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        float deadline=Time.realtimeSinceStartup+12;
        while(Time.realtimeSinceStartup<deadline&&enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp)>=hp)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.LessThan(hp));
        Assert.That(Vector3.Distance(turret.transform.position,origin),Is.LessThan(.01),"Turret must remain stationary");
        durability.ApplyDamage(10000,turret.transform.position,Vector3.up);yield return null;
        Assert.That(turret==null||!turret.activeInHierarchy,Is.True);Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator GroundDroneUsesRealWheelContactsAndMovesTowardOwner()
    {
        Assert.That(System.Enum.TryParse("GroundDroneGenerator",out R3PartKind kind),Is.True,"Missing ground drone generator");
        run.VehicleModel.Award(2500);FuseAndMount(kind);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();Assert.That(service.TryDeploy(kind),Is.True);
        var drone=GameObject.Find("Dron naziemny");Assert.That(drone,Is.Not.Null);
        var rb=drone.GetComponent<Rigidbody>();var drive=drone.GetComponent<RoboRougelike.R6.R6VehicleDrive>();
        Assert.That(rb,Is.Not.Null);Assert.That(drive,Is.Not.Null,"Use the real wheel solver");
        float deadline=Time.realtimeSinceStartup+8;int contacts=0;
        while(Time.realtimeSinceStartup<deadline&&contacts==0){yield return MOVSeriesDeviceTests.FixedStep();contacts=drive.GroundedWheels;}
        Assert.That(contacts,Is.GreaterThan(0),"The actual wheels must touch terrain");
        Vector3 before=rb.position;body.isKinematic=true;body.position=new Vector3(100,2,120);Physics.SyncTransforms();
        float until=Time.fixedTime+3;while(Time.fixedTime<until)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(rb.position.z,Is.GreaterThan(before.z+.5f),"The wheeled drone must physically follow");
        var template=(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner}},run.Salvage,"ground-drone-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();enemy.ConfigureMovement(0,12,0);var enemyBody=enemy.GetComponent<Rigidbody>();
        enemyBody.position=rb.position+new Vector3(12,1,0);enemyBody.isKinematic=true;Physics.SyncTransforms();
        float hp=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        deadline=Time.realtimeSinceStartup+12;
        while(Time.realtimeSinceStartup<deadline&&enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp)>=hp)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.LessThan(hp),"The wheeled drone must hit an enemy with its physical projectile");
        Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator FlyingDroneFollowsAndUsesPhysicalProjectilesAgainstEnemies()
    {
        Assert.That(System.Enum.TryParse("FlyingDroneGenerator",out R3PartKind kind),Is.True,"Missing flying drone generator");
        run.VehicleModel.Award(2500);FuseAndMount(kind);FuseAndMount(R3PartKind.GroundDroneGenerator);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        var template=(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner}},run.Salvage,"drone-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();enemy.ConfigureMovement(0,12,0);var enemyBody=enemy.GetComponent<Rigidbody>();
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.zero;
        enemyBody.position=new Vector3(112,2,100);enemyBody.isKinematic=true;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();
        float hp=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        run.VehicleModel.Economy.DroneCooldown=0;
        Assert.That(service.TryDeploy(kind),Is.True,"Launching a drone must not require driving");
        var drone=GameObject.Find("Dron latający");Assert.That(drone,Is.Not.Null);
        Assert.That(drone.GetComponent<Rigidbody>(),Is.Not.Null);Assert.That(drone.GetComponent<Collider>(),Is.Not.Null);
        var durability=drone.GetComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();Assert.That(durability,Is.Not.Null);
        Assert.That(service.TryDeploy(kind),Is.True,"Two generator units must allow two drones");
        var bank=player.GetComponent<VehicleEnergyBank>();float energy=bank.Current;int charges=run.VehicleModel.Charges.Current(R3DeployableService.FlyingDronePool);
        Assert.That(service.TryDeploy(kind),Is.False,"The active-drone limit must be respected");
        Assert.That(bank.Current,Is.EqualTo(energy));Assert.That(run.VehicleModel.Charges.Current(R3DeployableService.FlyingDronePool),Is.EqualTo(charges));
        Assert.That(service.StatusFor(kind),Does.Contain("limit"));
        float deadline=Time.realtimeSinceStartup+12;
        while(Time.realtimeSinceStartup<deadline&&enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp)>=hp)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.LessThan(hp),"The drone must actually hit the enemy with its projectile");
        run.VehicleModel.Economy.DroneCooldown=20;
        float flyingReady=run.VehicleModel.FlyingDroneReadyAt;
        Assert.That(service.TryDeploy(R3PartKind.GroundDroneGenerator),Is.True,"The other drone type must have its own independent active limit");
        Assert.That(run.VehicleModel.GroundDroneReadyAt,Is.GreaterThan(Time.time),"The ground drone must receive its own cooldown");
        Assert.That(run.VehicleModel.FlyingDroneReadyAt,Is.EqualTo(flyingReady),"Launching a ground drone must not change the flying drone cooldown");
        Assert.That(service.TryDeploy(R3PartKind.GroundDroneGenerator),Is.False,"A ground drone cannot bypass its cooldown");
        Assert.That(run.VehicleModel.Charges.Current(R3DeployableService.GroundDronePool),Is.EqualTo(2));
        Vector3 beforePosition=drone.GetComponent<Rigidbody>().position;body.isKinematic=true;body.position=new Vector3(100,2,120);Physics.SyncTransforms();
        float followUntil=Time.fixedTime+1.5f;while(Time.fixedTime<followUntil)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(drone.GetComponent<Rigidbody>().position.z,Is.GreaterThan(beforePosition.z+1),"The drone must physically follow a distant owner");
        durability.ApplyDamage(10000,drone.transform.position,Vector3.up);yield return null;
        Assert.That(drone==null||!drone.activeInHierarchy,Is.True,"The drone can be destroyed");
        Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator ImmobilizeTrapStopsEnemyDriveWithoutDamageAndExpires()
    {
        Assert.That(System.Enum.TryParse("ImmobilizeTrapDeployer",out R3PartKind kind),Is.True,"Missing immobilize trap");
        run.VehicleModel.Award(2500);FuseAndMount(kind);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        var template=(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner}},run.Salvage,"immobilize-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();enemy.ConfigureMovement(8,12,0);var enemyBody=enemy.GetComponent<Rigidbody>();
        float baseSpeed=enemy.EffectiveMovementSpeed;Assert.That(baseSpeed,Is.GreaterThan(0));
        enemyBody.position=new Vector3(140,2,140);enemyBody.useGravity=false;enemyBody.linearVelocity=Vector3.zero;
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();body.linearVelocity=Vector3.forward*3;
        Assert.That(service.TryDeploy(kind),Is.True);var trap=GameObject.Find("Pułapka unieruchamiająca");Assert.That(trap,Is.Not.Null);
        float hp=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        enemyBody.position=trap.transform.position+Vector3.up;enemyBody.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        var sensor=trap.GetComponentInChildren<R3DeployableTrap>();float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&trap!=null&&!sensor.Activated)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(sensor==null||sensor.Activated,Is.True);Assert.That(enemy.EffectiveMovementSpeed,Is.Zero);
        Assert.That(enemyBody.isKinematic,Is.False,"Immobilization must not replace or freeze physics");
        Assert.That(enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.EqualTo(hp));
        deadline=Time.realtimeSinceStartup+10;
        while(Time.realtimeSinceStartup<deadline&&enemy.EffectiveMovementSpeed<baseSpeed-.01f)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(enemy.EffectiveMovementSpeed,Is.EqualTo(baseSpeed).Within(.01));
        Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator SlowTrapReducesEnemyDriveSpeedAndExpires()
    {
        Assert.That(System.Enum.TryParse("SlowTrapDeployer",out R3PartKind kind),Is.True,"Missing slow trap");
        run.VehicleModel.Award(2500);FuseAndMount(kind);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        var template=(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner}},run.Salvage,"slow-trap-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();enemy.ConfigureMovement(8,12,0);var enemyBody=enemy.GetComponent<Rigidbody>();
        var speed=enemy.GetType().GetProperty("EffectiveMovementSpeed");Assert.That(speed,Is.Not.Null,"The real AI drive must expose its effective speed");
        float baseSpeed=(float)speed.GetValue(enemy);Assert.That(baseSpeed,Is.GreaterThan(0));
        enemyBody.position=new Vector3(140,2,140);enemyBody.useGravity=false;enemyBody.linearVelocity=Vector3.zero;
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();body.linearVelocity=Vector3.forward*3;
        Assert.That(service.TryDeploy(kind),Is.True);var trap=GameObject.Find("Pułapka spowalniająca");Assert.That(trap,Is.Not.Null);
        float hp=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        enemyBody.position=trap.transform.position+Vector3.up;enemyBody.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        var sensor=trap.GetComponentInChildren<R3DeployableTrap>();float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&trap!=null&&!sensor.Activated)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(sensor==null||sensor.Activated,Is.True);Assert.That((float)speed.GetValue(enemy),Is.LessThan(baseSpeed));
        Assert.That(enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.EqualTo(hp));
        float slowed=(float)speed.GetValue(enemy);var debuff=enemy.GetComponent<R3EnemyMovementDebuff>();Assert.That(debuff,Is.Not.Null);
        debuff.Apply(.1f,.2f);Assert.That((float)speed.GetValue(enemy),Is.LessThan(slowed));
        float strongerEnds=Time.fixedTime+.3f;while(Time.fixedTime<strongerEnds)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That((float)speed.GetValue(enemy),Is.EqualTo(slowed).Within(.01),"An earlier slow must remain when a stronger short effect ends");
        deadline=Time.realtimeSinceStartup+10;
        while(Time.realtimeSinceStartup<deadline&&(float)speed.GetValue(enemy)<baseSpeed-.01f)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That((float)speed.GetValue(enemy),Is.EqualTo(baseSpeed).Within(.01),"Slowdown must expire without changing base speed");
        Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator UntriggeredTrapExpires()
    {
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        var root=new GameObject("Untriggered trap fixture");var sensor=new GameObject("Sensor");sensor.transform.SetParent(root.transform);
        sensor.AddComponent<R3DeployableTrap>().Configure(root.transform,run.transform,R3PartKind.BlastTrapDeployer,3,60,3,6,12,1,.1f,.5f,2);
        float deadline=Time.realtimeSinceStartup+5;
        while(root!=null&&Time.realtimeSinceStartup<deadline)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(root==null,Is.True,"Unused traps must still expire");
    }
    [UnityTest] public IEnumerator DamageTrapKeepsHurtingEnemyUntilFieldExpires()
    {
        Assert.That(System.Enum.TryParse("DamageTrapDeployer",out R3PartKind kind),Is.True,"Missing damage-over-time trap");
        run.VehicleModel.Award(2500);FuseAndMount(kind);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        var template=(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner}},run.Salvage,"damage-trap-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();enemy.ConfigureMovement(0,12,0);var enemyBody=enemy.GetComponent<Rigidbody>();
        enemyBody.position=new Vector3(140,2,140);enemyBody.useGravity=false;enemyBody.linearVelocity=Vector3.zero;
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();body.linearVelocity=Vector3.forward*3;
        run.VehicleModel.Economy.TrapDamageDuration=1;run.VehicleModel.Economy.TrapLifetime=.5f;
        Assert.That(service.TryDeploy(kind),Is.True);var trap=GameObject.Find("Pułapka obrażeń");Assert.That(trap,Is.Not.Null);
        float triggerAt=Time.fixedTime+.3f;
        while(Time.fixedTime<triggerAt)yield return MOVSeriesDeviceTests.FixedStep();
        float own=player.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        float before=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        enemyBody.position=trap.transform.position+Vector3.up;enemyBody.linearVelocity=Vector3.zero;enemyBody.isKinematic=true;Physics.SyncTransforms();
        var sensor=trap.GetComponentInChildren<R3DeployableTrap>();float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&trap!=null&&!sensor.Activated)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(trap,Is.Not.Null,"Damage field must remain after activation");Assert.That(sensor.Activated,Is.True);
        var outline=trap.GetComponentInChildren<LineRenderer>();Assert.That(outline,Is.Not.Null,"The active area needs a visible boundary");
        Assert.That(outline.GetComponent<Collider>(),Is.Null,"The visual boundary must not affect physics");
        for(int i=0;i<12;i++)yield return MOVSeriesDeviceTests.FixedStep();
        float first=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        for(int i=0;i<12;i++)yield return MOVSeriesDeviceTests.FixedStep();
        float second=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        Assert.That(first,Is.LessThan(before));Assert.That(second,Is.LessThan(first),"Damage must continue over time");
        Assert.That(player.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.EqualTo(own));
        enemyBody.position=new Vector3(140,2,140);Physics.SyncTransforms();
        for(int i=0;i<8;i++)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.EqualTo(second),"Leaving the field must stop its damage");
        deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&trap!=null)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(trap==null,Is.True,"The activated field must expire");
        Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator RepelTrapPushesRealEnemyAwayWithoutPushingOwner()
    {
        Assert.That(System.Enum.TryParse("RepelTrapDeployer",out R3PartKind kind),Is.True,"Missing repel trap");
        run.VehicleModel.Award(2500);FuseAndMount(kind);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        var template=(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner}},run.Salvage,"repel-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();enemy.ConfigureMovement(0,12,0);var enemyBody=enemy.GetComponent<Rigidbody>();
        enemyBody.position=new Vector3(140,2,140);enemyBody.useGravity=false;enemyBody.linearVelocity=Vector3.zero;
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();body.linearVelocity=Vector3.forward*3;
        run.VehicleModel.Economy.TrapBlastDamage=0;run.VehicleModel.Economy.TrapEmpDuration=0;
        Assert.That(service.TryDeploy(kind),Is.True);var trap=GameObject.Find("Pułapka odrzucająca");Assert.That(trap,Is.Not.Null);
        Vector3 origin=trap.transform.position;float ownHp=player.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        float enemyHp=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        enemyBody.position=origin+new Vector3(1,1,0);enemyBody.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        var sensor=trap.GetComponentInChildren<R3DeployableTrap>();float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&trap!=null&&!sensor.Activated)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(sensor==null||sensor.Activated,Is.True);yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(enemyBody.linearVelocity.x,Is.GreaterThan(.1f),"The enemy must move outward from the trap");
        Assert.That(enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.EqualTo(enemyHp));
        Assert.That(player.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.EqualTo(ownHp));
        Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator BlastAndEmpTrapsTriggerOnRealEnemiesWithoutHurtingTheirOwner()
    {
        run.VehicleModel.Award(2500);FuseAndMount(R3PartKind.BlastTrapDeployer);FuseAndMount(R3PartKind.EmpTrapDeployer);
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;yield return null;
        var player=run.Player;var body=player.GetComponent<Rigidbody>();var service=player.GetComponent<R3DeployableService>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
        var template=(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner}},run.Salvage,"trap-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();enemy.ConfigureMovement(0,12,0);var enemyBody=enemy.GetComponent<Rigidbody>();
        enemyBody.position=new Vector3(140,2,140);enemyBody.useGravity=false;enemyBody.linearVelocity=Vector3.zero;
        body.position=new Vector3(100,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();body.linearVelocity=Vector3.forward*3;
        run.VehicleModel.Economy.TrapEmpDuration=0; // EMP tuning must not block blast traps.
        Assert.That(service.TryDeploy(R3PartKind.BlastTrapDeployer),Is.True);
        run.VehicleModel.Economy.TrapEmpDuration=3;
        var blast=GameObject.Find("Pułapka wybuchowa");Assert.That(blast,Is.Not.Null);
        var blastSensor=blast.GetComponentInChildren<R3DeployableTrap>();Assert.That(blastSensor.Activated,Is.False);
        float own=player.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        float enemyBefore=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        enemyBody.position=blast.transform.position+Vector3.up;enemyBody.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&blast!=null&&!blastSensor.Activated)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(blastSensor==null||blastSensor.Activated,Is.True,"A real enemy must trigger the trap");
        Assert.That(enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.LessThan(enemyBefore));
        Assert.That(player.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.EqualTo(own));
        Assert.That(run.VehicleModel.Charges.Current(R3DeployableService.BlastTrapPool),Is.EqualTo(2));
        enemyBody.position=new Vector3(140,2,140);enemyBody.linearVelocity=Vector3.zero;
        body.position=new Vector3(120,2,100);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*3;Physics.SyncTransforms();
        yield return MOVSeriesDeviceTests.FixedStep();body.linearVelocity=Vector3.forward*3;
        run.VehicleModel.Economy.TrapBlastDamage=0; // Blast tuning must not block EMP traps.
        Assert.That(service.TryDeploy(R3PartKind.EmpTrapDeployer),Is.True);
        var emp=GameObject.Find("Pułapka EMP");Assert.That(emp,Is.Not.Null);
        var empSensor=emp.GetComponentInChildren<R3DeployableTrap>();
        foreach(var r in enemy.GetComponentsInChildren<RoboRougelike.Mechanics.EMP.EmpReceiver>())r.ClearEmp();
        float hp=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        enemyBody.position=emp.transform.position+Vector3.up;enemyBody.linearVelocity=Vector3.zero;Physics.SyncTransforms();deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline&&emp!=null&&!empSensor.Activated)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(empSensor==null||empSensor.Activated,Is.True);
        Assert.That(enemy.GetComponentsInChildren<RoboRougelike.Mechanics.EMP.EmpReceiver>().Any(r=>r.IsDisrupted),Is.True);
        Assert.That(enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.EqualTo(hp),"EMP trap never damages HP");
        Assert.That(run.VehicleModel.Charges.Current(R3DeployableService.EmpTrapPool),Is.EqualTo(2));
        Object.Destroy(floor);
    }
    [UnityTest] public IEnumerator EnergyAuraTogglesOnPressAndDamagesOnlyEnemiesWhileConsumingEnergy()
    {
        Assert.That(run.GaragePurchase(R3PartKind.Gun,RoboRougelike.R4.R4WeaponKind.EmpEmitter),Is.True);
        var gun=run.VehicleModel.Inventory.Last(p=>p.Kind==R3PartKind.Gun&&p.Weapon==RoboRougelike.R4.R4WeaponKind.EmpEmitter);
        var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        var cell=cells.First(c=>run.VehicleModel.OwnedPlacementError(gun.Id,c,default)==null);
        Assert.That(run.GarageMount(gun.Id,cell),Is.True);Assert.That(run.EnterTestRange(),Is.True);
        for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        var template=(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,run.Player.transform,new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner}},run.Salvage,"aura-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.First();enemy.ConfigureMovement(0,12,0);
        var body=run.Player.GetComponent<Rigidbody>();body.position=new Vector3(100,5,100);body.isKinematic=true;
        var enemyBody=enemy.GetComponent<Rigidbody>();enemyBody.position=new Vector3(100,4,104);enemyBody.isKinematic=true;Physics.SyncTransforms();
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var mounted=assembly.Parts.Single(p=>p.Id==gun.Id);
        var type=System.Type.GetType("RoboRougelike.WeaponIntegration.RRLDamageAuraBinding, Assembly-CSharp");
        Assert.That(type,Is.Not.Null,"Energy aura binding must replace the old EMP weapon");
        var aura=mounted.GetComponent(type);Assert.That(aura,Is.Not.Null);
        var active=type.GetProperty("IsActive");Assert.That(active,Is.Not.Null);
        int slot=System.Array.FindIndex(run.VehicleModel.GetWeaponSlots(),s=>s==RoboRougelike.R4.R4WeaponKind.EmpEmitter);
        Assert.That(slot,Is.GreaterThanOrEqualTo(0));
        void Triggers(bool held)=>assembly.SetSlotTriggers(slot==0&&held,slot==1&&held,slot==2&&held,slot==3&&held);
        float hp=enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp);
        var energy=run.Player.GetComponent<VehicleEnergyBank>();float before=energy.Current;
        Triggers(true);Assert.That((bool)active.GetValue(aura),Is.True);
        Triggers(true);Assert.That((bool)active.GetValue(aura),Is.True,"Holding must not toggle repeatedly");
        for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();
        var nearby=Physics.OverlapSphere(body.position,6f).Where(c=>c.GetComponentInParent<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>()==enemy).ToArray();
        Assert.That(enemy.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(p=>p.CurrentHp),Is.LessThan(hp),
            $"active={active.GetValue(aura)}, energy={energy.Current}, enemy colliders={nearby.Length}, eligible={nearby.Count(c=>RoboRougelike.WeaponIntegration.RRLHitRouter.CanHit(c,body.transform))}, withHp={nearby.Count(c=>c.GetComponentInParent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>()!=null)}, targets={((System.Collections.IDictionary)type.GetField("targets",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(aura)).Count}, origin={body.transform.position}, physics={body.position}, enemy={enemy.transform.position}, damageScale={run.Player.Controller.PackageDamageScale}");
        Assert.That(energy.Current,Is.LessThan(before));
        Assert.That(enemy.GetComponentsInChildren<RoboRougelike.Mechanics.EMP.EmpReceiver>().Any(r=>r.IsDisrupted),Is.False);
        Triggers(false);Triggers(true);Assert.That((bool)active.GetValue(aura),Is.False);
        Triggers(false);Assert.That(energy.TrySpend(energy.Current),Is.True);
        Triggers(true);Assert.That((bool)active.GetValue(aura),Is.False,"Empty energy must prevent activation");
        Triggers(false);energy.Recharge(100);Triggers(true);Assert.That((bool)active.GetValue(aura),Is.True);
        mounted.Damageable.ApplyDamage(10000,mounted.transform.position,Vector3.up);
        Assert.That((bool)active.GetValue(aura),Is.False,"Destroying the weapon must stop the aura immediately");
    }
    [UnityTest] public IEnumerator OverdriveCombinesMountedCopiesAndCostsEnergyOnce()
    {
        Assert.That(System.Enum.TryParse("Overdrive",out R3PartKind kind),Is.True);
        for(int i=0;i<2;i++)
        {
            Assert.That(run.GaragePurchase(kind),Is.True);
            var owned=run.VehicleModel.Inventory.Last(p=>p.Kind==kind);
            var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>run.VehicleModel.OwnedPlacementError(owned.Id,c,default)==null);
            Assert.That(run.GarageMount(owned.Id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);
        for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        Assert.That(run.CanUseMovement,Is.True);
        var componentType=System.Type.GetType("RoboRougelike.R3.R3OffensiveDevices, Assembly-CSharp");
        Assert.That(componentType,Is.Not.Null);
        var component=run.Player.GetComponent(componentType);Assert.That(component,Is.Not.Null);
        var activate=componentType.GetMethod("TryActivate");Assert.That(activate,Is.Not.Null);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();
        Assert.That(bank.TrySpend(bank.Current-20),Is.True);
        Assert.That((int)activate.Invoke(component,null),Is.Zero,"Two copies must not partially activate with insufficient energy.");
        Assert.That(bank.Current,Is.EqualTo(20).Within(.01f));bank.Recharge(100);
        float before=bank.Current;
        Assert.That((int)activate.Invoke(component,null),Is.EqualTo(2));
        Assert.That(bank.Current,Is.EqualTo(before-30).Within(.01f));
        var effects=run.Player.GetComponent<R3MountedBonusEffects>();
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.4f).Within(.001f));
        Assert.That((int)activate.Invoke(component,null),Is.Zero,"A running shared effect cannot be activated again.");
        var copy=run.VehicleModel.DeepCopy();copy.Parts.RemoveAll(p=>p.Kind==kind);
        float now=Time.time;var checkpoint=copy.MakeCheckpoint(now);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Light,copy.Economy,now,out var restored,out var error),Is.True,error);
        Assert.That(restored.OverdriveReadyAt,Is.GreaterThan(now),"Removing all copies cannot reset the shared cooldown.");
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==kind);
        part.gameObject.SetActive(false);yield return null;
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.2f).Within(.001f));
        var remaining=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==kind&&p!=part);
        var receiver=remaining.GetComponent<RoboRougelike.Mechanics.EMP.EmpReceiver>();Assert.That(receiver,Is.Not.Null);
        receiver.ApplyEmp(2);yield return null;
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1f).Within(.001f));
        Assert.That((int)activate.Invoke(component,null),Is.Zero,"Shared cooldown remains after all copies stop working.");
    }
    [UnityTest] public IEnumerator OverdriveSlotRaisesActualBasicProjectileDamage()
    {
        Assert.That(run.GaragePurchase(R3PartKind.Overdrive),Is.True);
        var owned=run.VehicleModel.Inventory.Last(p=>p.Kind==R3PartKind.Overdrive);
        var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        var cell=cells.First(c=>run.VehicleModel.OwnedPlacementError(owned.Id,c,default)==null);
        Assert.That(run.GarageMount(owned.Id,cell),Is.True);
        var reward=ScriptableObject.CreateInstance<R1UpgradeDefinition>();
        reward.Id="overdrive-synergy";reward.Effect=R1UpgradeEffect.Damage;reward.Amount=.25f;
        Assert.That(run.VehicleModel.ReceiveBonuses(new[]{reward}),Is.True);
        var bonus=run.VehicleModel.Inventory.Single(p=>p.Kind==R3PartKind.BonusModule);
        cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        var bonusCell=cells.First(c=>run.VehicleModel.OwnedPlacementError(bonus.Id,c,default)==null);
        Assert.That(run.GarageMount(bonus.Id,bonusCell),Is.True);
        Assert.That(run.VehicleModel.ActiveSlotLabel(R3PartKind.Overdrive),Is.Not.EqualTo("—"));
        Assert.That(run.EnterTestRange(),Is.True);
        for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        Assert.That(run.CanUseMovement,Is.True);
        var input=run.Player.GetComponent<RoboRougelike.MOVSeries.MOVSeriesInput>();
        typeof(RoboRougelike.MOVSeries.MOVSeriesInput).GetMethod("Activate",BindingFlags.Instance|BindingFlags.NonPublic)
            .Invoke(input,new object[]{run,R3PartKind.Overdrive});
        var effects=run.Player.GetComponent<R3MountedBonusEffects>();Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.45f).Within(.001f));
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun);
        run.Player.GetComponent<Rigidbody>().isKinematic=true;
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4f;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.True);
        assembly.FireGroup(false);
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(942).Within(.1f),"20% Overdrive and 25% passive damage must add before the shot.");
        Object.Destroy(target);Object.Destroy(reward);
    }
    [UnityTest] public IEnumerator RapidFireCopiesShareCostAndShortenActualWeaponInterval()
    {
        Assert.That(System.Enum.TryParse("RapidFire",out R3PartKind kind),Is.True);
        for(int i=0;i<2;i++)
        {
            Assert.That(run.GaragePurchase(kind),Is.True);
            var owned=run.VehicleModel.Inventory.Last(p=>p.Kind==kind);
            var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            var cell=cells.First(c=>run.VehicleModel.OwnedPlacementError(owned.Id,c,default)==null);
            Assert.That(run.GarageMount(owned.Id,cell),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);
        for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        Assert.That(run.CanUseMovement,Is.True);
        var type=System.Type.GetType("RoboRougelike.R3.R3OffensiveDevices, Assembly-CSharp");
        Assert.That(type,Is.Not.Null);var component=run.Player.GetComponent(type);Assert.That(component,Is.Not.Null);
        var activate=type.GetMethod("TryActivateRapidFire");Assert.That(activate,Is.Not.Null);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();
        Assert.That(bank.TrySpend(bank.Current-20),Is.True);
        Assert.That((int)activate.Invoke(component,null),Is.Zero);
        Assert.That(bank.Current,Is.EqualTo(20).Within(.01f));bank.Recharge(100);float before=bank.Current;
        Assert.That((int)activate.Invoke(component,null),Is.EqualTo(2));
        Assert.That(bank.Current,Is.EqualTo(before-30).Within(.01f));
        var effects=run.Player.GetComponent<R3MountedBonusEffects>();Assert.That(effects.EffectiveStats.FireInterval,Is.EqualTo(.6f).Within(.001f));
        Assert.That(run.Player.Controller.WeaponInterval(RoboRougelike.R4.R4WeaponKind.Basic),Is.EqualTo(.38f*.6f).Within(.001f));
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun);
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.True);
        float firedAt=Time.time;assembly.FireGroup(false);
        Assert.That(gun.NextFireTime-firedAt,Is.EqualTo(.38f*.6f).Within(.001f),"The fired physical gun must use the reduced interval.");
        var copy=run.VehicleModel.DeepCopy();copy.Parts.RemoveAll(p=>p.Kind==kind);float now=Time.time;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(copy.MakeCheckpoint(now),R3VehicleKind.Light,copy.Economy,now,out var restored,out var error),Is.True,error);
        var ready=typeof(R3VehicleModel).GetProperty("RapidFireReadyAt");Assert.That(ready,Is.Not.Null);
        Assert.That((float)ready.GetValue(restored),Is.GreaterThan(now));
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==kind);
        part.gameObject.SetActive(false);yield return null;
        Assert.That(effects.EffectiveStats.FireInterval,Is.EqualTo(.8f).Within(.001f));
    }
    [UnityTest] public IEnumerator RapidFireAndOverdriveHaveIndependentSlotsAndCooldowns()
    {
        foreach(var kind in new[]{R3PartKind.Overdrive,R3PartKind.RapidFire})
        {
            Assert.That(run.GaragePurchase(kind),Is.True);
            var part=run.VehicleModel.Inventory.Last(p=>p.Kind==kind);
            var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            Assert.That(run.GarageMount(part.Id,cells.First(c=>run.VehicleModel.OwnedPlacementError(part.Id,c,default)==null)),Is.True);
        }
        Assert.That(run.VehicleModel.ActiveSlotLabel(R3PartKind.Overdrive),Is.Not.EqualTo(run.VehicleModel.ActiveSlotLabel(R3PartKind.RapidFire)));
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        Assert.That(run.CanUseMovement,Is.True);
        var devices=run.Player.GetComponent<R3OffensiveDevices>();var effects=run.Player.GetComponent<R3MountedBonusEffects>();
        Assert.That(devices.TryActivate(),Is.EqualTo(1));Assert.That(devices.TryActivateRapidFire(),Is.EqualTo(1));
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.2f).Within(.001f));
        Assert.That(effects.EffectiveStats.FireInterval,Is.EqualTo(.8f).Within(.001f));
        Assert.That(run.VehicleModel.OverdriveReadyAt,Is.GreaterThan(Time.time));
        Assert.That(run.VehicleModel.RapidFireReadyAt,Is.GreaterThan(Time.time));
        yield return null;
    }
    [UnityTest] public IEnumerator DoubleShotOverdriveAddsOneProjectileToPassiveShotWithoutRecursion()
    {
        Assert.That(System.Enum.TryParse("DoubleShotOverdrive",out R3PartKind boostKind),Is.True);
        foreach(var kind in new[]{R3PartKind.DoubleShot,boostKind})
        {
            Assert.That(run.GaragePurchase(kind),Is.True);
            var part=run.VehicleModel.Inventory.Last(p=>p.Kind==kind);
            var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
            Assert.That(run.GarageMount(part.Id,cells.First(c=>run.VehicleModel.OwnedPlacementError(part.Id,c,default)==null)),Is.True);
        }
        run.VehicleModel.Economy.DoubleShotChancePerUnit=1;
        Assert.That(run.EnterTestRange(),Is.True);for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        Assert.That(run.CanUseMovement,Is.True);
        var devices=run.Player.GetComponent<R3OffensiveDevices>();
        var activate=typeof(R3OffensiveDevices).GetMethod("TryActivateDoubleShotOverdrive");Assert.That(activate,Is.Not.Null);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();float before=bank.Current;
        Assert.That((int)activate.Invoke(devices,null),Is.EqualTo(1));Assert.That(bank.Current,Is.EqualTo(before-15).Within(.001f));
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun);
        run.Player.GetComponent<Rigidbody>().isKinematic=true;
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);
        Physics.SyncTransforms();Assert.That(assembly.ClearProjectilePath(gun),Is.True);assembly.FireGroup(false);
        Assert.That(bank.Current,Is.EqualTo(before-15).Within(.01f),"Extra projectiles do not spend additional energy.");
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>880.01f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(880).Within(.1f),"One original plus one passive and one active extra, without recursion.");
        var copy=run.VehicleModel.DeepCopy();copy.Parts.RemoveAll(p=>p.Kind==boostKind);float now=Time.time;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(copy.MakeCheckpoint(now),R3VehicleKind.Light,copy.Economy,now,out var restored,out var error),Is.True,error);
        Assert.That(restored.DoubleShotOverdriveReadyAt,Is.GreaterThan(now),"Cooldown survives removing every active copy.");
        var booster=assembly.Parts.Single(p=>p.Kind==boostKind);booster.gameObject.SetActive(false);
        Assert.That(assembly.RollDoubleShotExtras(8),Is.EqualTo(1),"Losing the active device immediately removes only its contribution.");
        Object.Destroy(target);
    }
    [UnityTest] public IEnumerator SalvageOptimizerConsumesEnergyAndCaptureDropsDestroyedBonus()
    {
        var bank=run.Player.GetComponent<VehicleEnergyBank>();float before=bank.Maximum;
        var part=FuseAndMount(R3PartKind.SalvageOptimizer);
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();bank=run.Player.GetComponent<VehicleEnergyBank>();
        bank.Refresh();Assert.That(bank.Maximum,Is.EqualTo(before-25));
        var mounted=assembly.Parts.Single(p=>p.Id==part.Id);
        var ledger=new RoboRougelike.R4.R4SalvageLedger();
        Assert.That(ledger.Record("foe",new[]{new RoboRougelike.R4.R4SalvageItem("drop","foe/drop",R3PartKind.Wheel,0,1)}),Is.True);
        assembly.Capture();int boosted=ledger.PreviewScrap(run.VehicleModel,System.Array.Empty<string>());
        mounted.Damageable.ApplyDamageResolvedTyped(mounted.Damageable.CurrentHp/.9f+1f,mounted.transform.position,Vector3.up,
            R3DamagePropagation.DamageKind.Piercing);assembly.Capture();
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==part.Id).Health,Is.Zero);
        Assert.That(ledger.PreviewScrap(run.VehicleModel,System.Array.Empty<string>()),Is.LessThan(boosted));
        yield return null;
    }
    [UnityTest] public IEnumerator UpgradedMotorUsesTwoPowerUnitsAndTwentyFiveEnergyReservation()
    {
        var drive=run.Player.GetComponent<R6VehicleDrive>();var bank=run.Player.GetComponent<VehicleEnergyBank>();
        drive.RefreshMass();bank.Refresh();float powerBefore=drive.ActivePowerUnits,energyBefore=bank.Maximum;
        var part=FuseAndMount(R3PartKind.DrivePower);
        drive=run.Player.GetComponent<R6VehicleDrive>();bank=run.Player.GetComponent<VehicleEnergyBank>();
        drive.RefreshMass();bank.Refresh();
        Assert.That(part.Tier,Is.EqualTo(2));Assert.That(drive.ActivePowerUnits,Is.EqualTo(powerBefore+2).Within(.001));
        Assert.That(bank.Maximum,Is.EqualTo(energyBefore-25));yield return null;
    }
    [UnityTest] public IEnumerator UpgradedRepairUsesMoreEnergyAndHealsTwiceAsFast()
    {
        FuseAndMount(R3PartKind.RepairPulse);Assert.That(run.EnterTestRange(),Is.True);
        for(int i=0;i<300&&!run.CanUseMovement;i++)yield return null;
        Assert.That(run.CanUseMovement,Is.True);
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var pulse=run.Player.GetComponent<R3RepairPulse>();
        var bank=run.Player.GetComponent<VehicleEnergyBank>();
        var armor=assembly.Parts.First(p=>p.Kind==R3PartKind.Armor&&p.Operational);
        armor.Damageable.RestoreHealthRatio(.5f);float before=bank.Current;
        Assert.That(pulse.TryActivate(),Is.EqualTo(1));
        Assert.That(bank.Current,Is.EqualTo(before-18f).Within(.001));
        typeof(R3RepairPulse).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pulse,new object[]{1f});
        Assert.That(armor.Damageable.CurrentHp/armor.Damageable.MaxHp,Is.EqualTo(.6f).Within(.001));
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
