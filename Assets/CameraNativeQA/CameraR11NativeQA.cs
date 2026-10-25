using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using B01.CAMERA001;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using UnityEngine;

// Dedicated QA scene only. Existing camera/vehicle/HP/weapon implementations remain unchanged.
public sealed class CameraR11NativeQA : MonoBehaviour
{
    public Material Template;
    [Serializable] public sealed class Row
    {
        public string id,phase="setup",status="Running",shotComponents="";
        public int width,height,parts,ownReticleHits,enemyBlockedSamples,enemyOffscreenSamples,worldPenetrationSamples,clipSamples,pivotOverlapSamples,samples,fireCalls,newFastBodies,partsAfterDamage;
        public bool fitSaturated,damageDetached;
        public Vector3 hullSize,initialPosition,finalPosition;
        public float maxCameraStep,maxCameraRotationStep,maxMuzzleAimError,maxCenterEnemyAngle,distanceMoved;
    }
    [Serializable] public sealed class Result
    {
        public string source="d48108f56d3fe0790ad715683e77b2de9219e4b1",target="4c9236a4cdc27814ea2e7679872ec4a524150cc8",method="Windows Player; API-driven real R3/R6 actors; camera-only static fixtures plus real drive/fire phase",status="Running";
        public List<Row> rows=new List<Row>();public List<string> errors=new List<string>();
    }
    Result result=new Result();Row row;string output;StreamWriter trace;
    R3VisualPalette palette;R1VehicleState actor,target;R3VehicleAssembly assembly;Rigidbody body;
    Camera view;Camera001Rig rig;List<GameObject> owned=new List<GameObject>();List<GameObject> walls=new List<GameObject>();
    HashSet<int> oldBodies=new HashSet<int>();HashSet<int> measuredBodies=new HashSet<int>();
    Vector3 previous;Quaternion previousRotation;int sampleFrame;
    void Save()=>File.WriteAllText(Path.Combine(output,"functional-results.json"),JsonUtility.ToJson(result,true));
    GameObject Box(string name,Vector3 position,Vector3 size,Color color)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=position;go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=Template;var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);go.GetComponent<Renderer>().SetPropertyBlock(block);return go;
    }
    IEnumerator Start()
    {
        string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-camera-qa-output");output=index>=0?args[index+1]:Path.Combine(Application.persistentDataPath,"CameraR11QA");Directory.CreateDirectory(output);
        QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
        Application.logMessageReceived+=OnLog;
        trace=new StreamWriter(Path.Combine(output,"camera-trace.csv"));trace.WriteLine("case,phase,seconds,x,y,z,step,rotationStep,idealOwnBlocked,actualOwnBlocked,enemyOwnBlocked,enemyViewportX,enemyViewportY,clipped,pivotBlocked");
        palette=new GameObject("QA existing R3 palette").AddComponent<R3VisualPalette>();palette.Initialize(Template);
        Box("QA flat floor",new Vector3(0,-.5f,20),new Vector3(100,1,140),new Color(.22f,.25f,.28f));
        view=Camera.main;view.fieldOfView=62;rig=view.gameObject.AddComponent<Camera001Rig>();rig.enabled=false;
        foreach(int width in new[]{1280,1920})foreach(string kind in new[]{"Light","Heavy","Tall","Wide"})
        {
            row=new Row{id=kind+"-"+width,width=width,height=width==1280?720:1080};result.rows.Add(row);
            Screen.SetResolution(row.width,row.height,false);yield return new WaitForSecondsRealtime(.35f);
            var model=new R3VehicleModel(R3VehicleCatalog.Create(kind=="Light"||kind=="Tall"?R3VehicleKind.Light:R3VehicleKind.Heavy));
            // Real mounted armour and damage components; deliberately extreme QA models, not certified garage blueprints.
            if(kind=="Tall")for(int y=0;y<=15;y++){var cell=new Vector3Int(0,y,0);if(model.At(cell)==null)model.Parts.Add(new R3PartState(cell,R3PartKind.Armor));}
            if(kind=="Wide")for(int x=-9;x<=9;x++){var cell=new Vector3Int(x,0,0);if(model.At(cell)==null)model.Parts.Add(new R3PartState(cell,R3PartKind.Armor));}
            actor=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,1,0),Quaternion.identity);owned.Add(actor.gameObject);
            assembly=actor.GetComponent<R3VehicleAssembly>();owned.AddRange(assembly.Parts.Select(p=>p.gameObject));
            actor.Controller.SetGameplayInputEnabled(true);actor.Controller.enabled=false;body=actor.GetComponent<Rigidbody>();body.isKinematic=true;
            target=R3VehicleAssembly.Build(new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light)),new R1RunStats(),palette,new Vector3(0,1,30),Quaternion.identity);owned.Add(target.gameObject);
            target.Controller.enabled=false;target.GetComponent<Rigidbody>().isKinematic=true;
            foreach(var renderer in target.GetComponentsInChildren<Renderer>()){var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",Color.red);renderer.SetPropertyBlock(block);}
            Physics.SyncTransforms();rig.Configure(actor.transform,view);SetAimOrbit();rig.StepCamera(0);
            row.parts=assembly.Parts.Count;row.hullSize=Camera001Rig.GetHullBounds(actor.transform).size;row.initialPosition=body.position;
            previous=view.transform.position;previousRotation=view.transform.rotation;
            row.phase="framing";for(int i=0;i<20;i++){yield return null;rig.StepCamera(Time.deltaTime);Measure(true);}
            yield return Capture("framing");
            row.phase="orbit360";float yaw=0;
            while(yaw<360){yield return null;yaw=Mathf.Min(360,yaw+95*Time.deltaTime);rig.SetOrbit(yaw,18,8);rig.StepCamera(Time.deltaTime);Measure(false);}
            SetAimOrbit();rig.ResetSmoothing();rig.StepCamera(0);previous=view.transform.position;previousRotation=view.transform.rotation;
            Vector3 aim=rig.LastSolution.AimWorld;Vector3 away=(rig.LastSolution.DesiredCameraWorld-aim).normalized;
            var wall=Box("QA wall",aim+away*4,new Vector3(30,30,.5f),new Color(.42f,.36f,.28f));owned.Add(wall);walls.Add(wall);Physics.SyncTransforms();
            row.phase="wallEntry";for(int i=0;i<25;i++){yield return null;rig.StepCamera(Time.deltaTime);Measure(true);}
            yield return Capture("wall");wall.SetActive(false);walls.Clear();
            var bounds=Camera001Rig.GetHullBounds(actor.transform);float half=bounds.extents.x+.7f;
            foreach(float x in new[]{-half,half}){var side=Box("QA corridor",new Vector3(x,bounds.center.y+2,8),new Vector3(.4f,bounds.size.y+5,32),new Color(.4f,.35f,.28f));owned.Add(side);walls.Add(side);}
            Physics.SyncTransforms();row.phase="corridor";
            for(int i=0;i<70;i++){yield return null;rig.SetOrbit(Mathf.Lerp(0,180,i/69f),18,8);rig.StepCamera(Time.deltaTime);Measure(false);}
            yield return Capture("corridor");foreach(var side in walls)side.SetActive(false);walls.Clear();
            SetAimOrbit();rig.ResetSmoothing();rig.StepCamera(0);previous=view.transform.position;previousRotation=view.transform.rotation;
            var part=assembly.Parts.Where(p=>p.Kind==R3PartKind.Armor&&p.Operational).OrderByDescending(p=>p.transform.position.y).First();
            row.phase="damage";part.Damageable.ApplyDamage(100000,part.transform.position,Vector3.right);
            for(int i=0;i<45;i++){yield return null;rig.StepCamera(Time.deltaTime);Measure(true);}
            row.damageDetached=!part.Operational||!part.transform.IsChildOf(actor.transform);row.partsAfterDamage=assembly.Parts.Count(p=>p.Operational&&p.transform.IsChildOf(actor.transform));
            yield return Capture("damage");
            if(kind=="Light")
            {
                body.isKinematic=false;var drive=actor.GetComponent<R6VehicleDrive>();drive.RefreshParts();row.phase="driveFire";float start=Time.time;
                oldBodies=new HashSet<int>(FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Select(b=>b.GetInstanceID()));
                while(Time.time-start<5)
                {
                    yield return new WaitForFixedUpdate();drive.Step(.65f,0,4,3,90);SetAimOrbit();rig.StepCamera(Time.fixedDeltaTime);
                    Ray ray=view.ViewportPointToRay(new Vector3(.5f,.5f));Vector3 aimPoint=ray.GetPoint(100);assembly.AimGuns(aimPoint);
                    if(Time.time-start>1){assembly.FireGuns();row.fireCalls++;foreach(var gun in assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun&&p.Operational&&p.Muzzle!=null))row.maxMuzzleAimError=Mathf.Max(row.maxMuzzleAimError,Vector3.Angle(gun.Muzzle.forward,aimPoint-gun.Muzzle.position));}
                    foreach(var rb in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))if(!oldBodies.Contains(rb.GetInstanceID())&&rb.linearVelocity.magnitude>10&&measuredBodies.Add(rb.GetInstanceID())){row.newFastBodies++;row.shotComponents=string.Join(";",rb.GetComponents<Component>().Select(c=>c.GetType().Name));owned.Add(rb.gameObject);}
                    Measure(true);
                }
                row.finalPosition=body.position;row.distanceMoved=Vector3.Distance(row.initialPosition,row.finalPosition);yield return Capture("driveFire");
            }
            else row.finalPosition=body.position;
            row.status=row.ownReticleHits==0&&row.worldPenetrationSamples==0&&row.enemyBlockedSamples==0?"PASS_OBSERVED":"FAIL_OBSERVED";Save();
            foreach(var go in owned)if(go!=null)Destroy(go);owned.Clear();yield return null;yield return null;
        }
        result.status=result.errors.Count==0&&result.rows.All(r=>r.status=="PASS_OBSERVED")?"PASS_OBSERVED":"FAIL_OBSERVED";Save();trace.Dispose();Application.Quit(result.status=="PASS_OBSERVED"?0:2);
    }
    void SetAimOrbit()
    {
        var hull=Camera001Rig.GetHullBounds(actor.transform);Vector3 enemy=Camera001Rig.GetHullBounds(target.transform).center;
        float pitch=Mathf.Atan2(hull.max.y+.65f-enemy.y,enemy.z-(hull.center.z+hull.extents.z+1.75f))*Mathf.Rad2Deg;
        rig.SetOrbit(0,pitch,8);
    }
    void Measure(bool facingEnemy)
    {
        row.samples++;row.fitSaturated|=rig.LastSolution.FitSaturated;
        float step=Vector3.Distance(previous,view.transform.position),angle=Quaternion.Angle(previousRotation,view.transform.rotation);row.maxCameraStep=Mathf.Max(row.maxCameraStep,step);row.maxCameraRotationStep=Mathf.Max(row.maxCameraRotationStep,angle);
        previous=view.transform.position;previousRotation=view.transform.rotation;
        Ray ray=view.ViewportPointToRay(new Vector3(.5f,.5f));bool own=Physics.RaycastAll(ray,150).Any(h=>h.collider.transform.IsChildOf(actor.transform));if(own)row.ownReticleHits++;
        bool penetration=walls.Any(w=>w.activeSelf&&w.GetComponent<Collider>().bounds.SqrDistance(view.transform.position)<.01f);if(penetration)row.worldPenetrationSamples++;
        Vector3 enemy=Camera001Rig.GetHullBounds(target.transform).center;Vector3 viewport=view.WorldToViewportPoint(enemy);
        bool blocked=false;if(facingEnemy){Vector3 direction=enemy-view.transform.position;blocked=Physics.RaycastAll(view.transform.position,direction.normalized,direction.magnitude).Any(h=>h.collider.transform.IsChildOf(actor.transform));if(blocked)row.enemyBlockedSamples++;if(viewport.z<=0||viewport.x<0||viewport.x>1||viewport.y<0||viewport.y>1)row.enemyOffscreenSamples++;row.maxCenterEnemyAngle=Mathf.Max(row.maxCenterEnemyAngle,Vector3.Angle(view.transform.forward,direction));}
        if(rig.WorldClipped)row.clipSamples++;if(rig.WorldOverlapAtAim)row.pivotOverlapSamples++;
        trace.WriteLine(string.Join(",",new[]{row.id,row.phase,Time.time.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),view.transform.position.x.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),view.transform.position.y.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),view.transform.position.z.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),step.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),angle.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),rig.LastSolution.OwnHullIntersectsCrosshair.ToString(),own.ToString(),blocked.ToString(),viewport.x.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),viewport.y.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),rig.WorldClipped.ToString(),rig.WorldOverlapAtAim.ToString()}));
    }
    IEnumerator Capture(string phase){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,row.id+"-"+phase+".png"));yield return new WaitForSecondsRealtime(.25f);}
    void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){result.errors.Add(message);Save();}}
    void OnGUI(){if(row==null)return;GUI.Label(new Rect(20,16,1100,25),"CAMERA R1.1 • ISOLATED API QA • "+row.id+" / "+row.phase+" • red = stationary R3 target");GUI.Label(new Rect(Screen.width/2-6,Screen.height/2-12,20,24),"+");}
}
