using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RogueMachine.P12PipelineSafe;
using UnityEngine;
using UnityEngine.TestTools;

// Pure EditMode scene objects: no gameplay scene, OS window, cursor lock or input.
public sealed class PerWeaponReticleTests
{
    GameObject root,cameraObject,wall,ownerCollider;
    R3VehicleAssembly assembly;
    P12PipelineSafeTankController controller;
    Camera view;
    R3MountedPart left,right;
    readonly Vector3 target=new Vector3(0,0,50);

    [SetUp]public void Setup()
    {
        // Other integration fixtures can leave their arena colliders in the editor scene.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        root=new GameObject("reticle test vehicle");assembly=root.AddComponent<R3VehicleAssembly>();
        controller=root.AddComponent<P12PipelineSafeTankController>();
        typeof(R3VehicleAssembly).GetField("controller",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(assembly,controller);
        cameraObject=new GameObject("reticle test camera");view=cameraObject.AddComponent<Camera>();
        view.transform.position=new Vector3(0,0,-10);view.transform.rotation=Quaternion.identity;
        view.fieldOfView=60;view.aspect=16f/9;
        left=Gun("left",new Vector3(-2,0,0));right=Gun("right",new Vector3(2,0,0));
    }
    R3MountedPart Gun(string id,Vector3 position)
    {
        var item=new GameObject(id);item.transform.SetParent(root.transform);item.transform.position=position;
        var part=item.AddComponent<R3MountedPart>();part.Id=id;part.Kind=R3PartKind.Gun;part.Weapon=R4WeaponKind.Basic;
        part.Damageable=item.AddComponent<P12PipelineSafeDamageable>();part.Block=item.AddComponent<P12_PipelineSafePlayerBlock>();
        part.GunPivot=part.Muzzle=item.transform;assembly.Parts.Add(part);return part;
    }
    IList Collect(Vector3 point)
    {
        var type=typeof(R3VehicleAssembly).Assembly.GetType("RoboRougelike.WeaponIntegration.RRLWeaponReticles");
        Assert.That(type,Is.Not.Null,"The game needs individual, real-muzzle reticles for its guns");
        var marker=type.GetNestedType("Marker",BindingFlags.Public);Assert.That(marker,Is.Not.Null);
        var list=(IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(marker));
        var collect=type.GetMethod("Collect",BindingFlags.Public|BindingFlags.Static);Assert.That(collect,Is.Not.Null);
        collect.Invoke(null,new object[]{view,assembly,controller,point,list});return list;
    }
    static T Field<T>(object marker,string name)=>(T)marker.GetType().GetField(name).GetValue(marker);

    [Test] public void TwelveConvergedWeaponsHaveDistinctReadableLabelOffsets()
    {
        var type=typeof(R3VehicleAssembly).Assembly.GetType("RoboRougelike.WeaponIntegration.RRLWeaponReticles");
        var method=type.GetMethod("LabelOffset",BindingFlags.Public|BindingFlags.Static);
        Assert.That(method,Is.Not.Null,"Converged weapons need separate label positions");
        var offsets=new Vector2[12];
        for(int i=0;i<offsets.Length;i++)offsets[i]=(Vector2)method.Invoke(null,new object[]{i});
        for(int i=0;i<offsets.Length;i++)for(int j=0;j<i;j++)
            Assert.That(Vector2.Distance(offsets[i],offsets[j]),Is.GreaterThan(20),$"labels {i+1} and {j+1} overlap");
    }
    [Test]public void EachLiveGunHasItsOwnMarkerThatConvergesAsItsBarrelTurns()
    {
        left.Muzzle.forward=(new Vector3(6,0,50)).normalized;right.Muzzle.forward=(new Vector3(-6,0,50)).normalized;
        var before=Collect(target);Assert.That(before.Count,Is.EqualTo(2));
        Assert.That(Field<string>(before[0],"PartId"),Is.EqualTo("left"));Assert.That(Field<string>(before[1],"PartId"),Is.EqualTo("right"));
        var a=Field<Vector2>(before[0],"Viewport");var b=Field<Vector2>(before[1],"Viewport");
        Assert.That(a.x,Is.GreaterThan(.52f));Assert.That(b.x,Is.LessThan(.48f));
        left.Muzzle.forward=(target-left.Muzzle.position).normalized;right.Muzzle.forward=(target-right.Muzzle.position).normalized;
        var after=Collect(target);Assert.That(after.Count,Is.EqualTo(2));
        Assert.That(Field<Vector2>(after[0],"Viewport").x,Is.EqualTo(.5f).Within(.001f));
        Assert.That(Field<Vector2>(after[1],"Viewport").x,Is.EqualTo(.5f).Within(.001f));
    }
    [Test]public void DetachedOrDestroyedWeaponsDoNotLeaveStaleMarkers()
    {
        Assert.That(Collect(target).Count,Is.EqualTo(2));
        right.Block.MarkDetached();Assert.That(Collect(target).Count,Is.EqualTo(1));
        UnityEngine.Object.DestroyImmediate(left.Damageable);Assert.That(Collect(target).Count,Is.Zero);
    }
    [Test]public void ObstacleAndOutOfRangeAreReportedForTheSpecificWeapon()
    {
        right.Block.MarkDetached();left.Muzzle.position=Vector3.zero;left.Muzzle.forward=Vector3.forward;
        ownerCollider=GameObject.CreatePrimitive(PrimitiveType.Cube);ownerCollider.transform.SetParent(root.transform);
        ownerCollider.transform.position=new Vector3(0,0,2);Physics.SyncTransforms();
        Assert.That(Field<bool>(Collect(target)[0],"Obstructed"),Is.True,"Own armor blocks the actual firing path and must be indicated.");
        wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,0,10);Physics.SyncTransforms();
        Assert.That(Field<bool>(Collect(target)[0],"Obstructed"),Is.True,"The actual barrel path hits a wall before the intended point");
        UnityEngine.Object.DestroyImmediate(wall);wall=null;Physics.SyncTransforms();
        Assert.That(Field<bool>(Collect(new Vector3(0,0,250))[0],"OutOfRange"),Is.True,"The basic shell's existing lifetime and speed bound its reach");
    }
    [Test]public void OwnArmorMustWarnWhenRealGunCannotFire()
    {
        right.Block.MarkDetached();left.Muzzle.position=Vector3.zero;left.Muzzle.forward=Vector3.forward;
        ownerCollider=GameObject.CreatePrimitive(PrimitiveType.Cube);ownerCollider.transform.SetParent(root.transform);
        ownerCollider.transform.position=new Vector3(0,0,2);Physics.SyncTransforms();
        Assert.That(assembly.ClearProjectilePath(left),Is.False,"The real firing path must be blocked by this armor.");
        Assert.That(Field<bool>(Collect(target)[0],"Obstructed"),Is.True,"The same gun's reticle must warn about its blocked shot.");
        var feedback=RoboRougelike.WeaponIntegration.RRLAimFeedback.Evaluate(assembly,controller,target,null);
        Assert.That(feedback.Aligned,Is.EqualTo(1));
        Assert.That(feedback.Clear,Is.Zero,"The aim caption must not advertise a clear path.");
    }
    [Test]public void DenseOwnMuzzleCollidersDoNotClaimARealClearShotIsBlocked()
    {
        right.Block.MarkDetached();left.Muzzle.position=Vector3.zero;left.Muzzle.forward=Vector3.forward;
        for(int i=0;i<150;i++)
        {
            var item=new GameObject("own muzzle collider "+i);item.transform.SetParent(left.transform,false);
            item.AddComponent<BoxCollider>().size=Vector3.one*.01f;
        }
        Physics.SyncTransforms();
        Assert.That(Physics.OverlapSphere(left.Muzzle.position,.1f,~0,QueryTriggerInteraction.Ignore).Length,Is.GreaterThan(128));
        Assert.That(assembly.ClearProjectilePath(left),Is.True,"The real shot ignores its own gun.");
        var feedback=RoboRougelike.WeaponIntegration.RRLAimFeedback.Evaluate(assembly,controller,target,null);
        Assert.That(feedback.Clear,Is.EqualTo(1),"The aim caption must agree with the actual clear path.");
        Assert.That(Field<bool>(Collect(target)[0],"Obstructed"),Is.False,"The gun marker must also report a clear path.");
    }
    [Test]public void DenseIgnoredShotPathStillDetectsARealObstacle()
    {
        right.Block.MarkDetached();left.Muzzle.position=Vector3.zero;left.Muzzle.forward=Vector3.forward;
        for(int i=0;i<150;i++)
        {
            var item=new GameObject("own barrel collider "+i);item.transform.SetParent(left.transform,false);
            item.transform.localPosition=new Vector3(0,0,1+i*.15f);item.AddComponent<BoxCollider>().size=Vector3.one*.01f;
        }
        Physics.SyncTransforms();
        Assert.That(Physics.SphereCastAll(Vector3.zero,.1f,Vector3.forward,49,~0,QueryTriggerInteraction.Ignore).Length,Is.GreaterThan(128));
        Assert.That(assembly.ClearProjectilePath(left),Is.True);
        var clear=RoboRougelike.WeaponIntegration.RRLAimFeedback.Evaluate(assembly,controller,target,null);
        Assert.That(clear.Clear,Is.EqualTo(1));
        Assert.That(Field<bool>(Collect(target)[0],"Obstructed"),Is.False);
        wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,0,30);Physics.SyncTransforms();
        var blocked=RoboRougelike.WeaponIntegration.RRLAimFeedback.Evaluate(assembly,controller,target,null);
        Assert.That(blocked.Clear,Is.Zero,"A real obstacle must not be hidden by ignored colliders.");
        Assert.That(Field<bool>(Collect(target)[0],"Obstructed"),Is.True);
    }
    [Test]public void TwelveGunAimFeedbackDiagnostic()
    {
        BuildTwelveGunFixture(out var aim,out var targetCollider);
        var markers=new System.Collections.Generic.List<RoboRougelike.WeaponIntegration.RRLWeaponReticles.Marker>(12);
        RoboRougelike.WeaponIntegration.RRLAimFeedback.Snapshot feedback=default;
        void Caption()=>feedback=RoboRougelike.WeaponIntegration.RRLAimFeedback.Evaluate(assembly,controller,aim,targetCollider);
        void Markers()=>RoboRougelike.WeaponIntegration.RRLWeaponReticles.Collect(view,assembly,controller,aim,markers);
        void Combined(){Caption();Markers();}
        void Measure(string label,Action sample)
        {
            for(int i=0;i<3;i++)sample();
            GC.Collect();int collections=GC.CollectionCount(0);
            const int iterations=100;long before=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<iterations;i++)sample();
            watch.Stop();long bytes=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()-before;
            Debug.Log($"[RETICLE_PERF] {label} guns=12 armor=80 iterations={iterations} elapsed_ms={watch.Elapsed.TotalMilliseconds:F2} mono_delta={bytes} gc_collections={GC.CollectionCount(0)-collections}");
        }
        Measure("caption",Caption);Measure("markers",Markers);Measure("combined",Combined);
        Assert.That(feedback.Guns,Is.EqualTo(12));Assert.That(feedback.Clear,Is.EqualTo(12));
        Assert.That(markers.Count,Is.EqualTo(12));
    }
    void BuildTwelveGunFixture(out Vector3 aim,out Collider targetCollider)
    {
        view.transform.position=new Vector3(0,2,-10);
        aim=new Vector3(0,2,50);
        for(int i=0;i<10;i++)Gun("extra "+i,Vector3.zero);
        for(int i=0;i<assembly.Parts.Count;i++)
        {
            var gun=assembly.Parts[i];gun.transform.position=new Vector3(i-5.5f,2,0);
            gun.Muzzle.forward=(aim-gun.Muzzle.position).normalized;
            gun.gameObject.AddComponent<BoxCollider>().size=Vector3.one*.3f;
        }
        for(int x=0;x<10;x++)for(int z=0;z<8;z++)
        {
            var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.SetParent(root.transform);
            block.transform.position=new Vector3((x-4.5f)*.6f,0,(z-3.5f)*.6f);block.transform.localScale=Vector3.one*.5f;
        }
        wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Diagnostic target";
        wall.transform.position=aim;wall.transform.localScale=new Vector3(30,10,1);
        targetCollider=wall.GetComponent<Collider>();
        Physics.SyncTransforms();
    }
    [UnityTest]public IEnumerator TwelveGunProfilerAllocationDiagnostic()
    {
        BuildTwelveGunFixture(out var aim,out var targetCollider);
        var markers=new System.Collections.Generic.List<RoboRougelike.WeaponIntegration.RRLWeaponReticles.Marker>(12);
        RoboRougelike.WeaponIntegration.RRLAimFeedback.Snapshot feedback=default;
        void Sample()
        {
            feedback=RoboRougelike.WeaponIntegration.RRLAimFeedback.Evaluate(assembly,controller,aim,targetCollider);
            RoboRougelike.WeaponIntegration.RRLWeaponReticles.Collect(view,assembly,controller,aim,markers);
        }
        for(int i=0;i<5;i++)Sample();
        using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",16))
        {
            Assert.That(recorder.Valid,Is.True);yield return null;yield return null;
            long idle=recorder.LastValue;
            const int iterations=100;
            bool disrupted=false;
            for(int i=0;i<iterations;i++)foreach(var part in assembly.Parts)
                disrupted|=RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject);
            yield return null;
            long emp=recorder.LastValue;
            for(int i=0;i<iterations;i++)foreach(var part in assembly.Parts)assembly.ClearProjectilePath(part);
            yield return null;
            long clearance=recorder.LastValue;
            for(int i=0;i<iterations;i++)feedback=RoboRougelike.WeaponIntegration.RRLAimFeedback.Evaluate(assembly,controller,aim,targetCollider);
            yield return null;
            long caption=recorder.LastValue;
            for(int i=0;i<iterations;i++)RoboRougelike.WeaponIntegration.RRLWeaponReticles.Collect(view,assembly,controller,aim,markers);
            yield return null;
            long reticles=recorder.LastValue;
            for(int i=0;i<iterations;i++)Sample();
            yield return null;
            long sample=recorder.LastValue;
            Debug.Log($"[RETICLE_FRAME_ALLOC] guns=12 iterations={iterations} idle_bytes={idle} emp_bytes={emp} clearance_bytes={clearance} caption_bytes={caption} reticles_bytes={reticles} combined_bytes={sample}");
            Assert.That(disrupted,Is.False);
            Assert.That(feedback.Guns,Is.EqualTo(12));Assert.That(feedback.Clear,Is.EqualTo(12));Assert.That(markers.Count,Is.EqualTo(12));
        }
    }
    [Test]public void OwnArmorStillBlocksWhenThereAreManyOtherHitColliders()
    {
        right.Block.MarkDetached();left.Muzzle.position=Vector3.zero;left.Muzzle.forward=Vector3.forward;
        ownerCollider=GameObject.CreatePrimitive(PrimitiveType.Cube);ownerCollider.transform.SetParent(root.transform);
        ownerCollider.transform.position=new Vector3(0,0,2);
        wall=new GameObject("many external colliders");
        for(int i=0;i<150;i++)
        {
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.SetParent(wall.transform);
            obstacle.transform.position=new Vector3(0,0,5+i*.2f);obstacle.transform.localScale=Vector3.one*.1f;
        }
        Physics.SyncTransforms();
        Assert.That(Physics.SphereCastAll(left.Muzzle.position,.1f,left.Muzzle.forward,100,~0,QueryTriggerInteraction.Ignore).Length,
            Is.GreaterThan(128),"The fixture must actually exceed the initial hit-buffer capacity.");
        Assert.That(assembly.ClearProjectilePath(left),Is.False,"Own armor must not be missed when many other colliders are hit.");
    }
    [UnityTest]public IEnumerator FrameAllocationRecorderDetectsKnownAllocation()
    {
        using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",16))
        {
            Assert.That(recorder.Valid,Is.True,"Unity must expose the requested profiler counter.");
            yield return null;
            var probe=new byte[1024*1024];probe[0]=1;
            yield return null;
            long maximum=0;foreach(var sample in recorder.ToArray())maximum=Math.Max(maximum,sample.Value);
            Debug.Log($"[ALLOC_RECORDER_PROBE] samples={recorder.Count} max_bytes={maximum}");
            GC.KeepAlive(probe);
            Assert.That(maximum,Is.GreaterThanOrEqualTo(1024*1024),"A profiler control must observe a known allocation.");
        }
    }
    [Test]public void AuraHasNoAimMarkerButSawAndBarrelsStillHaveIndicators()
    {
        left.Weapon=R4WeaponKind.EmpEmitter;right.Weapon=R4WeaponKind.RotarySaw;
        var area=Collect(target);Assert.That(area.Count,Is.EqualTo(1),"A radial aura has no aiming direction or misleading marker");
        Assert.That(Field<bool>(area[0],"AreaWeapon"),Is.True);
        Assert.That(Field<Vector2>(area[0],"Viewport").x,Is.GreaterThan(.5f),"Saw indicator uses the blade's real position");
        left.Weapon=R4WeaponKind.Basic;left.Muzzle.forward=Vector3.back;
        var away=Collect(target);Assert.That(away.Count,Is.EqualTo(2));
        Assert.That(Field<bool>(away[0],"Offscreen"),Is.True);
        Assert.That(Vector2.Distance(Field<Vector2>(away[0],"Viewport"),new Vector2(.5f,.5f)),Is.GreaterThan(.5f));
    }
    [Test]public void RocketIndicatorAvoidsOwnProjectileFlickerButRecognizesSolidDebris()
    {
        right.Block.MarkDetached();left.Muzzle.position=Vector3.zero;left.Muzzle.forward=Vector3.forward;
        left.Weapon=R4WeaponKind.RocketLauncher;
        var launcher=left.gameObject.AddComponent<RoboRougelike.Mechanics.Rockets.RocketLauncher>();
        var binding=left.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLRocketBinding>();
        typeof(RoboRougelike.WeaponIntegration.RRLRocketBinding).GetProperty("Module").SetValue(binding,launcher);
        wall=GameObject.CreatePrimitive(PrimitiveType.Sphere);wall.transform.position=new Vector3(0,0,10);
        var shell=wall.AddComponent<P17PhysicalShell>();
        typeof(P17PhysicalShell).GetField("ownerRoot",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(shell,root.transform);
        Physics.SyncTransforms();
        Assert.That(Field<bool>(Collect(target)[0],"Obstructed"),Is.False,
            "An outgoing own shell should not make the direction indicator jump while firing");
        UnityEngine.Object.DestroyImmediate(wall);wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position=new Vector3(0,0,10);wall.AddComponent<R3Debris>();
        wall.GetComponent<Collider>().enabled=true;Physics.SyncTransforms();
        Assert.That(Field<bool>(Collect(target)[0],"Obstructed"),Is.True,
            "Rockets collide with detached solid debris even though ordinary shells ignore it");
    }
    [Test]public void ViewportMarkersMapCorrectlyToLetterboxedHud()
    {
        var type=typeof(R3VehicleAssembly).Assembly.GetType("RoboRougelike.WeaponIntegration.RRLWeaponReticles");
        Assert.That(type,Is.Not.Null);var map=type.GetMethod("ToHudPoint");Assert.That(map,Is.Not.Null);
        Vector2 Map(Vector2 viewport,Vector2 screen)=>(Vector2)map.Invoke(null,new object[]{viewport,new Rect(0,0,screen.x,screen.y),screen});
        Assert.That(Map(new Vector2(.5f,.5f),new Vector2(1280,720)).x,Is.EqualTo(640).Within(.01f));
        Assert.That(Map(new Vector2(.5f,.5f),new Vector2(1920,1080)).y,Is.EqualTo(360).Within(.01f));
        var wide=Map(new Vector2(.25f,.5f),new Vector2(2560,1080));
        Assert.That(wide.x,Is.EqualTo(213.3333f).Within(.02f));Assert.That(wide.y,Is.EqualTo(360).Within(.01f));
    }
    [TestCase(1920,1080,.5f,.5f)]
    [TestCase(1920,1080,.01f,.5f)]
    [TestCase(1024,768,.5f,.1f)]
    [TestCase(768,1024,.5f,.1f)]
    public void OnscreenMarkerRemainsAtCameraProjection(int width,int height,float vx,float vy)
    {
        var screen=new Vector2(width,height);var pixelRect=new Rect(0,0,width,height);
        var viewport=new Vector2(vx,vy);
        var drawn=RoboRougelike.WeaponIntegration.RRLWeaponReticles.MarkerHudPoint(viewport,pixelRect,screen,false);
        float scale=Mathf.Min(width/1280f,height/720f);
        var pixel=new Vector2((width-1280*scale)*.5f+drawn.x*scale,
            (height-720*scale)*.5f+drawn.y*scale);
        Assert.That(pixel.x,Is.EqualTo(vx*width).Within(.01f));
        Assert.That(pixel.y,Is.EqualTo((1-vy)*height).Within(.01f));
    }
    [TestCase(1024,768,128,96,512,384,.2f,.8f)]
    [TestCase(768,1024,96,192,576,640,.8f,.2f)]
    [TestCase(1920,1080,200,90,1280,720,.01f,.99f)]
    public void CameraProjectionUsesItsActualPixelRectangle(int width,int height,float rx,float ry,float rw,float rh,float vx,float vy)
    {
        var screen=new Vector2(width,height);var rect=new Rect(rx,ry,rw,rh);
        view.aspect=rw/rh;
        var projected=view.WorldToViewportPoint(view.ViewportToWorldPoint(new Vector3(vx,vy,10)));
        var point=RoboRougelike.WeaponIntegration.RRLWeaponReticles.MarkerHudPoint(projected,rect,screen,false);
        float scale=Mathf.Min(width/1280f,height/720f);
        var pixel=new Vector2((width-1280*scale)*.5f+point.x*scale,(height-720*scale)*.5f+point.y*scale);
        Assert.That(pixel.x,Is.EqualTo(rx+projected.x*rw).Within(.01f));
        Assert.That(pixel.y,Is.EqualTo(height-(ry+projected.y*rh)).Within(.01f));
    }
    [TestCase(768,1024,0,0,768,1024,.5f,-.1f,384f,996.4f)]
    [TestCase(1024,768,128,96,512,384,-.2f,.5f,164.8f,480f)]
    [TestCase(1024,768,128,96,512,384,1.2f,.5f,603.2f,480f)]
    [TestCase(1024,768,-100,0,500,768,-.2f,.5f,36.8f,384f)]
    [TestCase(1024,768,100,100,40,50,-.2f,.5f,120f,643f)]
    [TestCase(1920,1080,200,90,1280,720,.5f,1.2f,840f,339f)]
    public void OffscreenMarkersClampToVisibleCameraViewport(int width,int height,float rx,float ry,float rw,float rh,float vx,float vy,float expectedX,float expectedY)
    {
        var screen=new Vector2(width,height);var rect=new Rect(rx,ry,rw,rh);
        var point=RoboRougelike.WeaponIntegration.RRLWeaponReticles.MarkerHudPoint(new Vector2(vx,vy),rect,screen,true);
        float scale=Mathf.Min(width/1280f,height/720f);
        var pixel=new Vector2((width-1280*scale)*.5f+point.x*scale,(height-720*scale)*.5f+point.y*scale);
        Assert.That(pixel.x,Is.EqualTo(expectedX).Within(.02f));Assert.That(pixel.y,Is.EqualTo(expectedY).Within(.02f));
    }
    [TestCase(1024,768,.5f,.1f)]
    [TestCase(768,1024,.5f,.1f)]
    public void OnscreenLabelCanRemainInScreenLetterboxMargin(int width,int height,float vx,float vy)
    {
        var screen=new Vector2(width,height);var rect=new Rect(0,0,width,height);
        var point=RoboRougelike.WeaponIntegration.RRLWeaponReticles.ToHudPoint(new Vector2(vx,vy),rect,screen);
        var label=RoboRougelike.WeaponIntegration.RRLWeaponReticles.ClampHudToViewport(point,rect,screen,10);
        Assert.That(label.x,Is.EqualTo(point.x).Within(.001f));Assert.That(label.y,Is.EqualTo(point.y).Within(.001f));
    }
    [TearDown]public void Cleanup()
    {
        if(wall!=null)UnityEngine.Object.DestroyImmediate(wall);
        if(root!=null)UnityEngine.Object.DestroyImmediate(root);
        if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);
    }
}
