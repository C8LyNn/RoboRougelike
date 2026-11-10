using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.MapSystemR4;
using RogueMachine.P12PipelineSafe;
using B01.CAMERA001;
public sealed class FullMergeAdditionalRegression
{
    R1RunController run;
    [UnitySetUp]public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;run.SelectVehicle(R3VehicleKind.Light);Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;yield return null;}
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
    [UnityTest]public IEnumerator CameraHullQueriesReuseColliderStorageForDenseVehicles()
    {
        var follow=Camera.main.GetComponent<P12PipelineSafeCamera>();var query=follow.ReceiverRig.ReceiverHullBounds;
        Assert.That(query,Is.Not.Null);var fixture=new GameObject("camera dense collider fixture");
        fixture.transform.SetParent(run.Player.transform,false);
        try
        {
            for(int i=0;i<300;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.01f;
            Physics.SyncTransforms();var initial=query(run.Player.transform);
            for(int i=0;i<3;i++)query(run.Player.transform);
            using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",16))
            {
                Assert.That(recorder.Valid,Is.True);yield return null;yield return null;long idle=recorder.LastValue;
                Bounds last=default;for(int i=0;i<50;i++)last=query(run.Player.transform);
                yield return null;long sample=recorder.LastValue;
                var control=new byte[1024*1024];control[0]=1;yield return null;long controlBytes=recorder.LastValue;System.GC.KeepAlive(control);
                Debug.Log($"[CAMERA_HULL_ALLOC] queries=50 extra_colliders=300 idle={idle} sample={sample} control={controlBytes}");
                Assert.That(last.center,Is.EqualTo(initial.center));Assert.That(last.size,Is.EqualTo(initial.size));
                Assert.That(controlBytes,Is.GreaterThanOrEqualTo(1024*1024),"The allocation profiler must detect a known allocation.");
                Assert.That(sample-idle,Is.LessThan(32000),"Repeated hull queries should not allocate a collider array per frame.");
            }
        }
        finally{Object.Destroy(fixture);}
        yield return null;
    }
    [UnityTest]public IEnumerator CameraOwnHullRayReusesColliderStorage()
    {
        var rig=Camera.main.GetComponent<P12PipelineSafeCamera>().ReceiverRig;
        var method=typeof(Camera001Rig).GetMethod("HitsOwnHullFromActualPose",BindingFlags.Instance|BindingFlags.NonPublic);
        var query=(System.Func<Vector3,Vector3,Bounds,bool>)System.Delegate.CreateDelegate(typeof(System.Func<Vector3,Vector3,Bounds,bool>),rig,method);
        var fixture=new GameObject("camera own ray dense fixture");fixture.transform.SetParent(run.Player.transform,false);
        try
        {
            for(int i=0;i<300;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.01f;
            Physics.SyncTransforms();var bounds=new Bounds(run.Player.transform.position,Vector3.one*10);
            var origin=run.Player.transform.position+Vector3.right*100;var direction=Vector3.up;
            var unrelatedBounds=new Bounds(run.Player.transform.position+Vector3.up*100,Vector3.one);
            Assert.That(query(fixture.transform.position,Vector3.forward,unrelatedBounds),Is.True,
                "Starting inside an actual own collider must still be detected.");
            for(int i=0;i<3;i++)Assert.That(query(origin,direction,bounds),Is.False);
            using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",16))
            {
                Assert.That(recorder.Valid,Is.True);yield return null;yield return null;long idle=recorder.LastValue;
                for(int i=0;i<50;i++)query(origin,direction,bounds);
                yield return null;long sample=recorder.LastValue;
                var control=new byte[1024*1024];control[0]=1;yield return null;long controlBytes=recorder.LastValue;System.GC.KeepAlive(control);
                Debug.Log($"[CAMERA_OWN_RAY_ALLOC] queries=50 extra_colliders=300 idle={idle} sample={sample} control={controlBytes}");
                Assert.That(controlBytes,Is.GreaterThanOrEqualTo(1024*1024));
                Assert.That(sample-idle,Is.LessThan(32000),"The own-hull ray should not allocate a collider array on every query.");
            }
        }
        finally{Object.Destroy(fixture);}
    }
    [UnityTest]public IEnumerator CameraOverlapIgnoresCrowdedOwnCollidersButDetectsWorld()
    {
        var rig=Camera.main.GetComponent<P12PipelineSafeCamera>().ReceiverRig;
        var overlap=typeof(Camera001Rig).GetMethod("IsWorldOverlapping",BindingFlags.Instance|BindingFlags.NonPublic);
        var fixture=new GameObject("crowded camera owner fixture");fixture.transform.SetParent(run.Player.transform,false);
        fixture.transform.position=new Vector3(12000,500,12000);GameObject wall=null;
        try
        {
            for(int i=0;i<180;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.01f;
            Physics.SyncTransforms();var point=fixture.transform.position;
            Assert.That(Physics.OverlapSphere(point,.4f).Length,Is.GreaterThan(64));
            Assert.That((bool)overlap.Invoke(rig,new object[]{point,.3f}),Is.False,"Own colliders are not world obstacles, even beyond the buffer.");
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=point;wall.transform.localScale=Vector3.one*.1f;Physics.SyncTransforms();
            Assert.That((bool)overlap.Invoke(rig,new object[]{point,.3f}),Is.True,"A real wall must still block the camera.");
        }
        finally{Object.Destroy(fixture);if(wall!=null)Object.Destroy(wall);}
        yield return null;
    }
    [UnityTest]public IEnumerator CameraSweepIgnoresCrowdedOwnCollidersButStopsAtWorld()
    {
        var rig=Camera.main.GetComponent<P12PipelineSafeCamera>().ReceiverRig;
        var cast=typeof(Camera001Rig).GetMethod("CastWorldClearance",BindingFlags.Instance|BindingFlags.NonPublic);
        var fixture=new GameObject("crowded camera sweep fixture");fixture.transform.SetParent(run.Player.transform,false);
        fixture.transform.position=new Vector3(12000,500,12000);GameObject wall=null;
        try
        {
            for(int i=0;i<180;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.01f;
            Physics.SyncTransforms();var origin=fixture.transform.position-Vector3.forward*2;
            Assert.That(Physics.SphereCastAll(origin,.1f,Vector3.forward,4f).Length,Is.GreaterThan(128));
            Assert.That((float)cast.Invoke(rig,new object[]{origin,Vector3.forward,4f,.1f}),Is.EqualTo(4f).Within(.001f),"Own colliders must not collapse camera distance.");
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=fixture.transform.position;wall.transform.localScale=Vector3.one*.1f;Physics.SyncTransforms();
            Assert.That((float)cast.Invoke(rig,new object[]{origin,Vector3.forward,4f,.1f}),Is.LessThan(4f),"A real wall must still stop the sweep.");
        }
        finally{Object.Destroy(fixture);if(wall!=null)Object.Destroy(wall);}
        yield return null;
    }
    [UnityTest]public IEnumerator ExtremeCameraQueryOverflowStillSeesARealWall()
    {
        var rig=Camera.main.GetComponent<P12PipelineSafeCamera>().ReceiverRig;
        var overlap=typeof(Camera001Rig).GetMethod("IsWorldOverlapping",BindingFlags.Instance|BindingFlags.NonPublic);
        var cast=typeof(Camera001Rig).GetMethod("CastWorldClearance",BindingFlags.Instance|BindingFlags.NonPublic);
        var fixture=new GameObject("extreme camera query fixture");fixture.transform.SetParent(run.Player.transform,false);
        fixture.transform.position=new Vector3(12000,500,12000);GameObject wall=null;
        try
        {
            for(int i=0;i<4100;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.01f;
            Physics.SyncTransforms();var point=fixture.transform.position;var origin=point-Vector3.forward*2;
            Assert.That(Physics.OverlapSphere(point,.4f).Length,Is.GreaterThan(4096));
            Assert.That(Physics.SphereCastAll(origin,.1f,Vector3.forward,4f).Length,Is.GreaterThan(4096));
            Assert.That((bool)overlap.Invoke(rig,new object[]{point,.3f}),Is.False);
            Assert.That((float)cast.Invoke(rig,new object[]{origin,Vector3.forward,4f,.1f}),Is.EqualTo(4f).Within(.001f));
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=point;wall.transform.localScale=Vector3.one*.1f;Physics.SyncTransforms();
            Assert.That((bool)overlap.Invoke(rig,new object[]{point,.3f}),Is.True);
            Assert.That((float)cast.Invoke(rig,new object[]{origin,Vector3.forward,4f,.1f}),Is.LessThan(4f));
        }
        finally{Object.Destroy(fixture);if(wall!=null)Object.Destroy(wall);}
        yield return null;
    }
    [UnityTest]public IEnumerator MinimumPitchFramesActualRobotInsideViewport()
    {
        run.Garage.Close();yield return null;
        var follow=Object.FindFirstObjectByType<P12PipelineSafeCamera>();follow.Configure(run.Player.transform,run.Player.CameraPivot);follow.SetGameplayInputEnabled(true);
        Set(follow,"yaw",0f);Set(follow,"pitch",18f);Set(follow,"distance",7.8f);
        typeof(P12PipelineSafeCamera).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(follow,null);
        var b=Camera001Rig.GetHullBounds(run.Player.transform);var camera=Camera.main;
        foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1}){
            var p=camera.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
            Assert.That(p.z,Is.GreaterThan(0));Assert.That(p.y,Is.InRange(.02f,.98f),"Own hull clipped below image despite safe centre ray");
            Assert.That(p.x,Is.InRange(.02f,.98f));
        }
    }
    [UnityTest]public IEnumerator CameraKeepsUpWithFastActualBodyWithoutBypassingItsSweep()
    {
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        run.Player.Controller.enabled=false;var body=run.Player.GetComponent<Rigidbody>();var rig=Camera.main.GetComponent<Camera001Rig>();var start=body.position;
        // Explicit velocity-controlled camera stress fixture, NOT a driving acceptance.
        float deadline=Time.realtimeSinceStartup+8;
        for(int i=0;i<100;i++){body.linearVelocity=Vector3.forward*22;float before=Time.fixedTime;while(Time.fixedTime<=before){Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Owned fixed-clock fixture must not hang");yield return null;}}
        yield return null;
        Assert.That(body.position.z-start.z,Is.GreaterThan(30));
        Assert.That(Vector3.Distance(rig.transform.position,rig.LastSolution.DesiredCameraWorld),Is.LessThan(5),"Total15m/s cap loses a22m/s robot indefinitely");
        Assert.That(rig.WorldOverlapAtCamera,Is.False);
    }
    [UnityTest]public IEnumerator LostPartRestorationCannotSpendOrRecreateThroughForeignCollider()
    {
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var part=assembly.Parts.Single(p=>p.Cell==new Vector3Int(1,0,2));var cell=part.Cell;var id=part.Id;
        part.Damageable.ApplyDamage(100000,part.transform.position,Vector3.zero);assembly.Capture();yield return null;yield return null;
        // This fixture isolates foreign geometry, not secondary damage from the
        // newer connected-part damage distribution.
        foreach(var state in run.VehicleModel.Parts.Where(p=>p.Id!=id))state.Health=1f;
        Assert.That(run.VehicleModel.At(cell).Health,Is.Zero);
        var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=run.Player.transform.TransformPoint((Vector3)cell*.55f);obstacle.transform.localScale=Vector3.one*.3f;Physics.SyncTransforms();
        int scrap=run.VehicleModel.Scrap;Assert.That(run.GarageRepair(cell),Is.False);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));Assert.That(run.VehicleModel.At(cell).Health,Is.Zero);
        Object.Destroy(obstacle);yield return null;Assert.That(run.GarageRepair(cell),Is.True);
    }
    [UnityTest]public IEnumerator BulkRepairQuoteExcludesPhysicallyBlockedRestoration()
    {
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        var part=assembly.Parts.Single(p=>p.Cell==new Vector3Int(1,0,2));var cell=part.Cell;var id=part.Id;
        part.Damageable.ApplyDamage(100000,part.transform.position,Vector3.zero);assembly.Capture();yield return null;yield return null;
        foreach(var state in run.VehicleModel.Parts.Where(p=>p.Id!=id))state.Health=1f;
        Assert.That(run.VehicleModel.At(cell).Health,Is.Zero);
        var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.transform.position=run.Player.transform.TransformPoint((Vector3)cell*.55f);
        obstacle.transform.localScale=Vector3.one*.3f;Physics.SyncTransforms();
        int scrap=run.VehicleModel.Scrap;
        Assert.That(run.VehicleModel.AffordableRepairCost,Is.Zero,"A blocked repair cannot be quoted as available.");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(scrap));Assert.That(run.VehicleModel.At(cell).Health,Is.Zero);
        Assert.That(run.GarageRepairAffordable(),Is.False);
        Object.Destroy(obstacle);yield return null;
        int quote=run.VehicleModel.AffordableRepairCost;Assert.That(quote,Is.GreaterThan(0));
        Assert.That(run.GarageRepairAffordable(),Is.True);Assert.That(scrap-run.VehicleModel.Scrap,Is.EqualTo(quote));
    }
    [UnityTest]public IEnumerator FactoryWheelCanReturnAndRestoreWithoutIgnoringForgedForeignColliders()
    {
        var old=run;run.RestartRun();while(R1RunController.Instance==old||R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;run.SelectVehicle(R3VehicleKind.Standard);Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;yield return null;
        var cell=new Vector3Int(-3,0,0);string id=run.VehicleModel.At(cell).Id;int money=run.VehicleModel.Scrap;
        Assert.That(run.GarageRemove(cell),Is.True);yield return null;
        Assert.That(run.VehicleModel.OwnedPlacementError(id,cell,default),Is.Null);
        Assert.That(run.GarageMount(id,cell),Is.True);yield return null;
        Assert.That(run.VehicleModel.At(cell).Id,Is.EqualTo(id));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(money));
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var wheel=assembly.Parts.Single(p=>p.Id==id);
        wheel.Damageable.ApplyDamage(100000,wheel.transform.position,Vector3.zero);assembly.Capture();yield return null;yield return null;
        var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=run.Player.transform.TransformPoint((Vector3)cell*.55f);obstacle.transform.localScale=Vector3.one*.2f;
        obstacle.AddComponent<R3MountedPart>().Id=id;Physics.SyncTransforms();
        Assert.That(run.GarageRepair(cell),Is.False,"A forged matching ID outside the vehicle never gains factory exemption");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(money));Assert.That(run.VehicleModel.At(cell).Health,Is.Zero);
        Object.Destroy(obstacle);yield return null;Assert.That(run.GarageRepair(cell),Is.True);
        Assert.That(run.VehicleModel.At(cell).Id,Is.EqualTo(id));Assert.That(run.VehicleModel.At(cell).Health,Is.EqualTo(1));
    }
    IEnumerator FreshVehicle(R3VehicleKind kind){
        var old=run;run.RestartRun();while(R1RunController.Instance==old||R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;Assert.That(run.SelectVehicle(kind),Is.True);Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;yield return null;
    }
    void AssertQuarterOrbitFraming(){
        run.Garage.Close();Physics.SyncTransforms();
        var follow=Object.FindFirstObjectByType<P12PipelineSafeCamera>();follow.Configure(run.Player.transform,run.Player.CameraPivot);follow.SetGameplayInputEnabled(true);
        foreach(float pitch in new[]{18f,38f,58f})foreach(float yaw in new[]{0f,45f,90f,135f,180f,225f,270f,315f}){
            Set(follow,"yaw",yaw);Set(follow,"pitch",pitch);Set(follow,"distance",3f);follow.ReceiverRig.ResetSmoothing();
            typeof(P12PipelineSafeCamera).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(follow,null);
            var b=Camera001Rig.GetHullBounds(run.Player.transform);
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1}){
                var p=Camera.main.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
                string detail=$"pitch={pitch} yaw={yaw} fit={follow.ReceiverRig.ReceiverFitDistance} viewport={p}";
                Assert.That(p.z,Is.GreaterThan(0),detail);Assert.That(p.x,Is.InRange(.02f,.98f),detail);Assert.That(p.y,Is.InRange(.02f,.98f),detail);
            }
        }
    }
    [UnityTest]public IEnumerator HeavyHullFitsAtAllGameplayPitches(){yield return FreshVehicle(R3VehicleKind.Heavy);AssertQuarterOrbitFraming();}
    [UnityTest]public IEnumerator WideHullFitsAtQuarterOrbits(){
        foreach(int side in new[]{-1,1})for(int x=1;x<=6;x++)Assert.That(run.GarageBuy(new Vector3Int(side*x,1,0),R3PartKind.Armor),Is.True);
        yield return null;AssertQuarterOrbitFraming();
    }
    [UnityTest]public IEnumerator ActivityApisRejectMissingPhysicalPlayer(){
        var old=run;run.RestartRun();while(R1RunController.Instance==old||R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;Assert.That(run.StartRun(),Is.True);Assert.That(run.Player,Is.Null);
        Assert.That(run.CanChooseCheckpointRace,Is.False);Assert.That(run.ChooseCheckpointRace(),Is.False);
        foreach(MapR4Mode mode in System.Enum.GetValues(typeof(MapR4Mode)))Assert.That(run.ChooseRoadChallenge(mode),Is.False);
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Route));Assert.That(run.RoadWorld,Is.Null);Assert.That(run.GeneratedWorld,Is.Null);
    }
    void StaticMeshEvidence(bool penetrating){
        var candidate=GameObject.CreatePrimitive(PrimitiveType.Cube);candidate.transform.position=new Vector3(100,5,100);
        var terrain=GameObject.CreatePrimitive(PrimitiveType.Cube);terrain.transform.position=candidate.transform.position+(penetrating?Vector3.right*.8f:Vector3.right*3);
        terrain.GetComponent<BoxCollider>().enabled=false;var mesh=terrain.AddComponent<MeshCollider>();mesh.sharedMesh=terrain.GetComponent<MeshFilter>().sharedMesh;mesh.convex=false;Physics.SyncTransforms();
        var c=candidate.GetComponent<Collider>();
        bool hit=Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,mesh,mesh.transform.position,mesh.transform.rotation,out _,out float depth);
        Assert.That(hit,Is.EqualTo(penetrating));if(hit)Assert.That(depth,Is.GreaterThan(.005f));
        var evidence=new B01.BUILD001.BuildPhysicsEvidence(new[]{c},new Collider[]{mesh},true);
        Assert.That(B01.BUILD001.BuildVolumeValidator.Check(evidence),Is.EqualTo(penetrating?B01.BUILD001.BuildPlacementResult.ColliderPenetration:B01.BUILD001.BuildPlacementResult.Valid));
        mesh.enabled=false;var rb=terrain.AddComponent<Rigidbody>();rb.isKinematic=true;mesh.enabled=true;Physics.SyncTransforms();
        Assert.That(B01.BUILD001.BuildVolumeValidator.Check(evidence),Is.EqualTo(B01.BUILD001.BuildPlacementResult.PhysicsUnverified),"Moving non-convex mesh is not an admitted world surface");
        Object.Destroy(candidate);Object.Destroy(terrain);
    }
    [UnityTest]public IEnumerator StaticWorldMeshAllowsCertifiedClearance(){StaticMeshEvidence(false);yield return null;}
    [UnityTest]public IEnumerator StaticWorldMeshPenetrationStillBlocksInstallation(){StaticMeshEvidence(true);yield return null;}
    [UnityTest]public IEnumerator RaisedJunctionDoesNotPutACurbAcrossItsIncomingGrade(){
        var old=run;run.RestartRun();while(R1RunController.Instance==old||R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;run.SelectVehicle(R3VehicleKind.Heavy);Assert.That(run.PrepareUnifiedRun(),Is.True);Assert.That(run.ChooseRoadChallenge(MapR4Mode.TimeAttack),Is.True);
        while(run.Transitioning)yield return null;
        var mesh=run.RoadWorld.Edges["junction_upper"].GetComponent<MeshFilter>().sharedMesh;var v=mesh.vertices;
        Physics.SyncTransforms();
        for(int i=16;i<=31;i++){
            var p=(v[2*i]+v[2*i+1])*.5f;
            var hits=Physics.RaycastAll(p+Vector3.up*5,Vector3.down,10,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.transform.IsChildOf(run.RoadWorld.Root.transform)).ToArray();
            Assert.That(hits.Length,Is.GreaterThan(0));Assert.That(hits.Max(h=>h.point.y),Is.EqualTo(p.y).Within(.03f),"Junction curb above actual source grade at sample "+i);
        }
    }
    [UnityTest]public IEnumerator ArticulatingGunCannotFeedBackIntoTheCameraAimAnchor(){
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        run.Player.Controller.enabled=false;
        var follow=Object.FindFirstObjectByType<P12PipelineSafeCamera>();var gun=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.Gun);
        Vector3? first=null;
        foreach(var rotation in new[]{Quaternion.identity,Quaternion.Euler(35,90,0),Quaternion.Euler(-20,170,0),Quaternion.Euler(60,45,0)}){
            gun.GunPivot.localRotation=rotation;Physics.SyncTransforms();Set(follow,"yaw",0f);Set(follow,"pitch",38f);Set(follow,"distance",10f);follow.ReceiverRig.ResetSmoothing();
            typeof(P12PipelineSafeCamera).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(follow,null);
            var anchor=follow.ReceiverRig.LastSolution.AimWorld;if(!first.HasValue)first=anchor;
            Assert.That(Vector3.Distance(anchor,first.Value),Is.LessThan(.001f),"Unchanged chassis must not move its camera aim anchor as the gun follows that same reticle");
        }
        yield return null;
    }
    [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
