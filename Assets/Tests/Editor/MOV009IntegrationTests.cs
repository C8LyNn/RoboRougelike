using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.MOV009;
using RoboRougelike.MOV009Integration;
using RogueMachine.P12PipelineSafe;

public class MOV009IntegrationTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;run.SelectVehicle(R3VehicleKind.Light);Assert.That(run.OpenSandboxGarage(),Is.True);
    }
    public static IEnumerator Seconds(float value){float end=Time.time+value;while(Time.time<end)yield return null;}
    [UnityTest] public IEnumerator HigherTierSuspensionAppliesMorePhysicalSupport()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(3000);
        for(int i=0;i<4;i++)
        {
            Assert.That(model.PurchaseToInventory(R3PartKind.Suspension),Is.True);
            var part=model.Inventory.Last();
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.OwnedPlacementError(part.Id,c,default)==null);
            Assert.That(model.Mount(part.Id,cell),Is.True);
        }
        var stronger=model.DeepCopy();foreach(var part in stronger.Parts.Where(p=>p.Kind==R3PartKind.Suspension))part.Tier=2;
        var strongest=model.DeepCopy();foreach(var part in strongest.Parts.Where(p=>p.Kind==R3PartKind.Suspension))part.Tier=3;
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(120,-.5f,100);floor.transform.localScale=new Vector3(100,1,60);
        var basic=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,.72f,100),Quaternion.identity);
        var advanced=R3VehicleAssembly.Build(stronger,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(120,.72f,100),Quaternion.identity);
        var elite=R3VehicleAssembly.Build(strongest,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(140,.72f,100),Quaternion.identity);
        foreach(var rig in new[]{basic,advanced,elite})
        {
            foreach(var collider in rig.GetComponentsInChildren<Collider>())collider.enabled=false;
            var body=rig.GetComponent<Rigidbody>();body.useGravity=false;body.constraints=RigidbodyConstraints.FreezePositionX|RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation;
        }
        yield return null;Physics.SyncTransforms();
        var normal=basic.GetComponent<ActiveSuspensionController>();var improved=advanced.GetComponent<ActiveSuspensionController>();
        var strongestController=elite.GetComponent<ActiveSuspensionController>();
        advanced.GetComponent<MOV009VehicleAdapter>().Configure(advanced.GetComponent<R3VehicleAssembly>());
        advanced.GetComponent<Rigidbody>().useGravity=false;
        Assert.That(improved.RegisteredModuleCount,Is.EqualTo(4));
        var step=typeof(ActiveSuspensionController).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(step,Is.Not.Null);
        foreach(var controller in new[]{normal,improved,strongestController})step.Invoke(controller,null);
        Assert.That(normal.LastTotalLiftNewtons,Is.GreaterThan(0));
        Assert.That(improved.LastTotalLiftNewtons,Is.EqualTo(normal.LastTotalLiftNewtons*2).Within(normal.LastTotalLiftNewtons*.05f));
        Assert.That(strongestController.LastTotalLiftNewtons,Is.EqualTo(normal.LastTotalLiftNewtons*4).Within(normal.LastTotalLiftNewtons*.1f));
        var previousMode=Physics.simulationMode;
        try
        {
            Physics.simulationMode=SimulationMode.Script;Physics.Simulate(Time.fixedDeltaTime);
            Assert.That(advanced.GetComponent<Rigidbody>().linearVelocity.y,Is.GreaterThan(basic.GetComponent<Rigidbody>().linearVelocity.y));
            Assert.That(elite.GetComponent<Rigidbody>().linearVelocity.y,Is.GreaterThan(advanced.GetComponent<Rigidbody>().linearVelocity.y));
        }
        finally{Physics.simulationMode=previousMode;}
        UnityEngine.Object.Destroy(basic.gameObject);UnityEngine.Object.Destroy(advanced.gameObject);UnityEngine.Object.Destroy(elite.gameObject);UnityEngine.Object.Destroy(floor);
    }
    IEnumerator RangeWithSupports()
    {
        Assert.That(Enum.IsDefined(typeof(R3PartKind),MOV009ModelTests.Suspension),Is.True,"Missing real construction part category.");
        foreach(var cell in MOV009ModelTests.Cells(R3VehicleKind.Light))Assert.That(run.GarageBuy(cell,MOV009ModelTests.Suspension),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
    }
    [UnityTest] public IEnumerator FourRealConstructionModulesSupportChassisWithGravity()
    {
        yield return RangeWithSupports();
        var controller=run.Player.GetComponent<ActiveSuspensionController>();
        Assert.That(controller,Is.Not.Null,"Imported components must support the real player chassis.");
        Assert.That(controller.RegisteredModuleCount,Is.EqualTo(4));
        var body=run.Player.GetComponent<Rigidbody>();Assert.That(body.useGravity,Is.True);
        Assert.That(body.constraints,Is.EqualTo(RigidbodyConstraints.None));
        yield return Seconds(1.8f);
        Debug.Log($"MOV009 Light flat: y={body.position.y:F3}, vy={body.linearVelocity.y:F3}, lift={controller.LastTotalLiftNewtons:F1}, mass={body.mass:F0}");
        Assert.That(body.position.y,Is.InRange(.55f,.92f));
        Assert.That(Mathf.Abs(body.linearVelocity.y),Is.LessThan(.5f));
        Assert.That(controller.LastTotalLiftNewtons,Is.GreaterThan(body.mass*5));
        Assert.That(controller.LastTotalLiftNewtons,Is.LessThanOrEqualTo(body.mass*18+.1f));
    }
    [UnityTest] public IEnumerator VehicleWithoutModulesRetainsGroundedR6WithoutAddingSuspension()
    {
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var body=run.Player.GetComponent<Rigidbody>();Assert.That(body.useGravity,Is.True);
        Assert.That(body.constraints,Is.EqualTo(RigidbodyConstraints.None));
        Assert.That(run.Player.GetComponent<ActiveSuspensionController>(),Is.Null);
        yield return Seconds(.7f);var drive=run.Player.GetComponent<RoboRougelike.R6.R6VehicleDrive>();drive.RefreshMass();
        Assert.That(body.position.y,Is.InRange(.55f,.85f));Assert.That(drive.GroundedWheels,Is.EqualTo(4));
    }
    [UnityTest] public IEnumerator DetachedFlagImmediatelyDisablesSupportBeforeReparenting()
    {
        yield return RangeWithSupports();var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Suspension);
        var module=part.GetComponentInChildren<ActiveSuspensionModule>();var body=run.Player.GetComponent<Rigidbody>();
        Assert.That(module.IsAvailableFor(body),Is.True);part.Block.MarkDetached();
        Assert.That(module.IsAvailableFor(body),Is.False,"Structural detachment must be effective before transform hierarchy changes.");
    }
    [UnityTest] public IEnumerator ReconfiguringDoesNotDuplicateSuspensionModules()
    {
        yield return RangeWithSupports();
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        var adapter=run.Player.GetComponent<MOV009VehicleAdapter>();
        adapter.Configure(assembly);
        Assert.That(adapter.Controller.RegisteredModuleCount,Is.EqualTo(4));
        foreach(var part in assembly.Parts.Where(p=>p.Kind==R3PartKind.Suspension))
        {
            Assert.That(part.GetComponentsInChildren<ActiveSuspensionModule>().Length,Is.EqualTo(1));
            Assert.That(part.GetComponents<MOV009PartBinding>().Length,Is.EqualTo(1));
        }
    }
    [UnityTest] public IEnumerator StaticDamageablePartIsNotMistakenForArenaGround()
    {
        yield return RangeWithSupports();var module=run.Player.GetComponentsInChildren<ActiveSuspensionModule>().First();
        var fake=GameObject.CreatePrimitive(PrimitiveType.Cube);fake.name="Static loose damageable";
        fake.transform.position=new Vector3(module.transform.position.x,.12f,module.transform.position.z);fake.transform.localScale=new Vector3(.4f,.2f,.4f);
        fake.AddComponent<P12PipelineSafeDamageable>().Configure(100,false);Physics.SyncTransforms();
        yield return Seconds(.12f);
        Assert.That(module.LastGroundContact,Is.True);Assert.That(module.LastGroundDistanceMeters,Is.GreaterThan(.6f),"The probe must skip loose parts even without Rigidbody.");
        UnityEngine.Object.Destroy(fake);
    }
    [UnityTest] public IEnumerator LostSupportsDoNotRetuneRemainingSpringsOrFreezeTheChassis()
    {
        yield return RangeWithSupports();yield return Seconds(1);
        var adapter=run.Player.GetComponent<MOV009VehicleAdapter>();var body=run.Player.GetComponent<Rigidbody>();
        var parts=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.Suspension).ToArray();
        float stiffness=adapter.StiffnessPerModule,limit=adapter.MaximumForcePerModule;
        for(int i=0;i<4;i++)
        {
            parts[i].Damageable.ApplyDamage(10000,parts[i].transform.position,Vector3.zero);
            Assert.That(parts[i].GetComponentInChildren<ActiveSuspensionModule>().IsAvailableFor(body),Is.False);
            Assert.That(adapter.Controller.RegisteredModuleCount,Is.EqualTo(3-i),"Detached support must unregister from its old controller.");
            yield return Seconds(.15f);
            Assert.That(adapter.OperationalSupports,Is.EqualTo(3-i));
            Assert.That(adapter.StiffnessPerModule,Is.EqualTo(stiffness));Assert.That(adapter.MaximumForcePerModule,Is.EqualTo(limit));
            Assert.That(adapter.Controller.LastTotalLiftNewtons,Is.LessThanOrEqualTo(body.mass*18+.1f));
        }
        Assert.That(body.useGravity,Is.True);Assert.That(body.constraints,Is.EqualTo(RigidbodyConstraints.None));
        Assert.That(adapter.Controller.LastTotalLiftNewtons,Is.EqualTo(0));
    }
    [UnityTest] public IEnumerator CompetingWheelColliderForcesAssistMode()
    {
        yield return RangeWithSupports();var wheel=new GameObject("Competing spring");wheel.transform.SetParent(run.Player.transform,false);
        var collider=wheel.AddComponent<WheelCollider>();var spring=collider.suspensionSpring;spring.spring=35000;collider.suspensionSpring=spring;
        var adapter=run.Player.GetComponent<MOV009VehicleAdapter>();run.VehicleModel.SuspensionMode=ActiveSuspensionSupportMode.FullSupport;adapter.ApplySettings();
        Assert.That(adapter.Controller.SupportMode,Is.EqualTo(ActiveSuspensionSupportMode.AssistOnly));
        Assert.That(adapter.ProtectionReason,Is.Not.Empty);UnityEngine.Object.Destroy(wheel);
    }
    [UnityTest] public IEnumerator HeightAdjustmentAndDamageStayInsideTheRangeCopy()
    {
        foreach(var cell in MOV009ModelTests.Cells(R3VehicleKind.Light))Assert.That(run.GarageBuy(cell,MOV009ModelTests.Suspension),Is.True);
        var original=run.VehicleModel;int money=original.Scrap;var ids=original.Parts.Select(p=>p.Id).ToArray();
        var configure=run.GetType().GetMethod("ConfigureSuspension");Assert.That(configure,Is.Not.Null,"Missing usable garage/range configuration action.");
        Assert.That((bool)configure.Invoke(run,new object[]{true,ActiveSuspensionSupportMode.FullSupport,0f}),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        var body=run.Player.GetComponent<Rigidbody>();yield return Seconds(1.2f);float low=body.position.y;
        var before=body.position;
        Assert.That((bool)configure.Invoke(run,new object[]{true,ActiveSuspensionSupportMode.FullSupport,.2f}),Is.True);
        Assert.That(body.position,Is.EqualTo(before),"Height command must not teleport the chassis.");
        yield return Seconds(2);Assert.That(body.position.y,Is.GreaterThan(low+.12f),"Ride height must move by spring force.");
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Suspension);
        part.Damageable.ApplyDamage(10000,part.transform.position,Vector3.zero);
        Assert.That(run.ResetTestRange(),Is.True);while(run.Transitioning)yield return null;
        Assert.That(run.Player.GetComponent<MOV009VehicleAdapter>().OperationalSupports,Is.EqualTo(4));
        Assert.That(run.ReturnFromTestRange(),Is.True);while(run.Transitioning)yield return null;
        Assert.That(run.VehicleModel,Is.SameAs(original));Assert.That(original.Scrap,Is.EqualTo(money));
        CollectionAssert.AreEqual(ids,original.Parts.Select(p=>p.Id));Assert.That(original.SuspensionHeightOffset,Is.EqualTo(0));
        Assert.That(original.Parts.Where(p=>p.Kind==R3PartKind.Suspension).All(p=>p.Health==1),Is.True);
    }
    [UnityTest] public IEnumerator MassRefreshPrecedesSuspensionForceBudget()
    {
        yield return RangeWithSupports();var body=run.Player.GetComponent<Rigidbody>();var adapter=run.Player.GetComponent<MOV009VehicleAdapter>();
        var armor=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor&&p.Cell==new Vector3Int(1,0,1));
        float mass=body.mass;armor.Damageable.ApplyDamage(10000,armor.transform.position,Vector3.zero);
        adapter.SendMessage("FixedUpdate");
        Assert.That(body.mass,Is.LessThanOrEqualTo(mass-7.9f),"The lift cap must use the live mass before applying forces.");
    }
    [UnityTest] public IEnumerator RebuildingAllDestroyedSuspensionPlansDoesNotRestoreFlatHoverLock()
    {
        yield return RangeWithSupports();var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        foreach(var part in assembly.Parts.Where(p=>p.Kind==R3PartKind.Suspension))part.Damageable.ApplyDamage(10000,part.transform.position,Vector3.zero);
        assembly.Capture();
        var copy=R3VehicleAssembly.Build(run.VehicleModel.DeepCopy(),new R1RunStats(),UnityEngine.Object.FindFirstObjectByType<R3VisualPalette>(),new Vector3(18,.7f,-26),Quaternion.identity);
        Assert.That(copy.GetComponent<Rigidbody>().useGravity,Is.True,"A rebuild of lost suspension must not regain frozen-Y support.");
        Assert.That(copy.GetComponent<ActiveSuspensionController>().RegisteredModuleCount,Is.EqualTo(0));
        yield return Seconds(.25f);Assert.That(copy.transform.position.y,Is.LessThan(.69f));
    }
    [UnityTest] public IEnumerator GarageRepairedAndRemountedSupportWorksWithoutDuplicatePurchase()
    {
        foreach(var cell in MOV009ModelTests.Cells(R3VehicleKind.Light))Assert.That(run.GarageBuy(cell,R3PartKind.Suspension),Is.True);
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var part=assembly.Parts.First(p=>p.Kind==R3PartKind.Suspension);var cellToRepair=part.Cell;string id=part.Id;
        part.Damageable.ApplyDamage(10000,part.transform.position,Vector3.zero);assembly.Capture();
        Assert.That(run.GarageRepair(cellToRepair),Is.True);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(491));
        Assert.That(run.Player.GetComponent<MOV009VehicleAdapter>().OperationalSupports,Is.EqualTo(4));
        Assert.That(run.GarageRemove(cellToRepair),Is.True);Assert.That(run.VehicleModel.Inventory.Single().Id,Is.EqualTo(id));
        Assert.That(run.Player.GetComponent<MOV009VehicleAdapter>().OperationalSupports,Is.EqualTo(3));
        Assert.That(run.GarageMount(id,cellToRepair),Is.True);Assert.That(run.VehicleModel.Inventory,Is.Empty);
        Assert.That(run.Player.GetComponent<MOV009VehicleAdapter>().OperationalSupports,Is.EqualTo(4));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(491));
        yield return null;
    }
    [UnityTest] public IEnumerator RaisedSuspensionCanStillHitTheRangeCoreWithARealProjectile()
    {
        yield return RangeWithSupports();Assert.That(run.ConfigureSuspension(true,ActiveSuspensionSupportMode.FullSupport,.2f),Is.True);yield return Seconds(1.5f);
        var target=GameObject.Find("Poligon R6").GetComponentsInChildren<P12PipelineSafeDamageable>().Where(p=>p.name=="Rdzeń celu").OrderBy(p=>Mathf.Abs(p.transform.position.x)).First();
        float hp=target.CurrentHp;var assembly=run.Player.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun);
        gun.GunPivot.rotation=Quaternion.LookRotation(target.transform.position-gun.GunPivot.position);assembly.FireGuns();
        yield return Seconds(2);Assert.That(target.CurrentHp,Is.LessThan(hp),"Height change must preserve actual physical gun hits.");
    }
    [UnityTest] public IEnumerator GarageRebuildAndRangeReturnPreserveElevatedThreeDimensionalPose()
    {
        foreach(var cell in MOV009ModelTests.Cells(R3VehicleKind.Light))Assert.That(run.GarageBuy(cell,R3PartKind.Suspension),Is.True);
        var position=new Vector3(4,2.3f,-15);var rotation=Quaternion.Euler(12,33,-4);
        run.Player.transform.SetPositionAndRotation(position,rotation);var body=run.Player.GetComponent<Rigidbody>();body.position=position;body.rotation=rotation;Physics.SyncTransforms();
        Assert.That(run.GarageBuy(new Vector3Int(1,1,0),R3PartKind.Armor),Is.True);
        Assert.That(Vector3.Distance(run.Player.GetComponent<Rigidbody>().position,position),Is.LessThan(.001f),"Garage edits must not replace elevated Y with the planar R6 lock.");
        Assert.That(Quaternion.Angle(run.Player.transform.rotation,rotation),Is.LessThan(.01f));
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        Assert.That(run.ReturnFromTestRange(),Is.True);while(run.Transitioning)yield return null;
        Assert.That(Vector3.Distance(run.Player.GetComponent<Rigidbody>().position,position),Is.LessThan(.001f));
        Assert.That(Quaternion.Angle(run.Player.transform.rotation,rotation),Is.LessThan(.01f));
    }
    [UnityTest] public IEnumerator CompetingSpringIsGuardedBeforeTheSuspensionFixedUpdate()
    {
        yield return RangeWithSupports();var wheel=new GameObject("Competing spring order");wheel.transform.SetParent(run.Player.transform,false);
        wheel.AddComponent<WheelCollider>();var probe=run.Player.gameObject.AddComponent<MOV009GuardProbe>();probe.Controller=run.Player.GetComponent<ActiveSuspensionController>();
        yield return Seconds(.12f);
        Assert.That(probe.Ticks,Is.GreaterThan(0));Assert.That(probe.SawFullSupport,Is.False,"The guard must precede the force-producing FixedUpdate, including the first frame.");
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
