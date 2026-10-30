using UnityEngine;
using B01.LevelChallenges.Airlock;

namespace B0125.Airlock
{
    [DisallowMultipleComponent]
    public sealed class AirlockUnityDemo : MonoBehaviour
    {
        private AirlockController policy;
        private Rigidbody entryGate,exitGate,vehicle;
        private BoxCollider approachZone,chamberZone,exitZone,entryThreshold,exitThreshold;
        private Collider rigCollider;
        private Renderer entryPaint,exitPaint;
        private readonly Collider[] probeHits=new Collider[32];
        private Vector2 drive;
        private bool paused,heavy,emergency;
        private int falls;
        public Rigidbody EntryGate=>entryGate;
        public Rigidbody ExitGate=>exitGate;
        public Rigidbody Vehicle=>vehicle;
        public AirlockState Current=>policy==null?default:policy.State;
        public int Falls=>falls;
        public bool Heavy=>heavy;
        public bool Paused=>paused;
        private GameObject Block(string label,Vector3 pos,Vector3 dimensions,Color color)
        {
            var b=GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name=label;b.transform.SetParent(transform,false);
            b.transform.position=pos;b.transform.localScale=dimensions;
            b.GetComponent<Renderer>().material.color=color;
            return b;
        }
        private BoxCollider Sensor(string label,Vector3 p,Vector3 size)
        {
            var b=Block(label,p,size,Color.clear);
            b.GetComponent<Renderer>().enabled=false;
            var c=b.GetComponent<BoxCollider>();c.isTrigger=true;
            return c;
        }
        private void Awake()
        {
            policy=new AirlockController(new AirlockConfig(.8,1.2));
            Block("SOUTH ENTRY actual physical floor",new Vector3(0,-.25f,-6),
                new Vector3(6,.5f,7),Color.gray);
            Block("CENTRAL EQUALIZING CHAMBER physical floor",new Vector3(0,-.25f,0),
                new Vector3(6,.5f,5),Color.gray);
            Block("NORTH EXIT actual physical floor",new Vector3(0,-.25f,6),
                new Vector3(6,.5f,7),Color.gray);
            Block("ALWAYS SAFE X4 BYPASS no interlock no softlock",
                new Vector3(4,-.25f,0),new Vector3(2.5f,.5f,19),Color.green);
            var a=Block("DOOR A independent real kinematic Rigidbody",new Vector3(0,1.4f,-2),
                new Vector3(5.8f,2.8f,.5f),Color.red);
            entryGate=a.AddComponent<Rigidbody>();
            entryGate.isKinematic=true;entryGate.useGravity=false;
            entryGate.interpolation=RigidbodyInterpolation.Interpolate;
            entryPaint=a.GetComponent<Renderer>();
            var b=Block("DOOR B independent real kinematic Rigidbody",new Vector3(0,1.4f,2),
                new Vector3(5.8f,2.8f,.5f),Color.red);
            exitGate=b.AddComponent<Rigidbody>();
            exitGate.isKinematic=true;exitGate.useGravity=false;
            exitGate.interpolation=RigidbodyInterpolation.Interpolate;
            exitPaint=b.GetComponent<Renderer>();
            approachZone=Sensor("ENTRY SENSOR actual overlap probe",new Vector3(0,1,-4),new Vector3(3,3,1.8f));
            chamberZone=Sensor("CHAMBER SENSOR actual overlap probe",new Vector3(0,1,0),new Vector3(3,3,1.8f));
            exitZone=Sensor("EXIT SENSOR actual overlap probe",new Vector3(0,1,4),new Vector3(3,3,1.8f));
            entryThreshold=Sensor("A DOORWAY obstruction probe",new Vector3(0,1.2f,-2),new Vector3(3,2.5f,1));
            exitThreshold=Sensor("B DOORWAY obstruction probe",new Vector3(0,1.2f,2),new Vector3(3,2.5f,1));
            var capsule=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name="ACTUAL physical Light or Heavy player capsule";
            capsule.transform.SetParent(transform,false);
            rigCollider=capsule.GetComponent<Collider>();
            vehicle=capsule.AddComponent<Rigidbody>();
            vehicle.useGravity=true;
            vehicle.interpolation=RigidbodyInterpolation.Interpolate;
            vehicle.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            vehicle.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
            SetHeavy(false);
        }
        public void SetHeavy(bool value)
        {
            heavy=value;
            if(vehicle==null)return;
            vehicle.mass=value?12f:1.5f;
            vehicle.transform.localScale=value?new Vector3(1.6f,1,1.6f):Vector3.one;
            Restart();
        }
        public void TeleportTo(Vector3 p)
        {
            if(vehicle==null)return;
            vehicle.linearVelocity=Vector3.zero;vehicle.angularVelocity=Vector3.zero;
            vehicle.position=p;vehicle.transform.position=p;
            Physics.SyncTransforms();
        }
        public void SetDrive(float sideways,float forward)
        {
            drive=Vector2.ClampMagnitude(new Vector2(Mathf.Clamp(sideways,-1,1),
                Mathf.Clamp(forward,-1,1)),1);
        }
        public void SetPaused(bool value)
        {
            paused=value;if(vehicle==null)return;
            if(value){vehicle.linearVelocity=Vector3.zero;vehicle.angularVelocity=Vector3.zero;}
            vehicle.isKinematic=value;if(!value)vehicle.WakeUp();
        }
        public void RequestEmergency()=>emergency=true;
        public void Restart()
        {
            if(policy==null||vehicle==null||entryGate==null)return;
            paused=false;drive=Vector2.zero;falls=0;emergency=false;
            policy.Reset();vehicle.isKinematic=false;
            entryGate.position=new Vector3(0,1.4f,-2);
            exitGate.position=new Vector3(0,1.4f,2);
            entryGate.transform.position=entryGate.position;
            exitGate.transform.position=exitGate.position;
            TeleportTo(new Vector3(0,1.1f,-7.5f));
            Paint();
        }
        private bool PhysicalOverlap(BoxCollider zone)
        {
            if(zone==null||rigCollider==null||vehicle==null)return false;
            // Real Unity physics broadphase probes for the exact original physical
            // player's Collider, not a guessed button/state or synthetic trigger.
            Bounds b=zone.bounds;
            int count=Physics.OverlapBoxNonAlloc(b.center,b.extents*.98f,probeHits,
                zone.transform.rotation,~0,QueryTriggerInteraction.Collide);
            for(int i=0;i<count;i++)
            {
                if(probeHits[i]==rigCollider)return true;
            }
            return false;
        }
        private void FixedUpdate()
        {
            if(policy==null||vehicle==null||paused)return;
            float dt=Time.fixedDeltaTime;
            if(dt<=0||dt>.05f)return;
            Vector3 p=vehicle.position;
            bool approach=PhysicalOverlap(approachZone);
            bool chamber=PhysicalOverlap(chamberZone);
            bool exterior=PhysicalOverlap(exitZone);
            bool obstructionA=PhysicalOverlap(entryThreshold);
            bool obstructionB=PhysicalOverlap(exitThreshold);
            try
            {
                var s=policy.Advance(new AirlockInput(dt,vehicle.mass,approach,chamber,
                    exterior,obstructionA,obstructionB,emergency,false,p.y>=-4));
                emergency=false;
                entryGate.MovePosition(new Vector3(0,1.4f+(float)s.EntryOpen*3.1f,-2));
                exitGate.MovePosition(new Vector3(0,1.4f+(float)s.ExitOpen*3.1f,2));
            }
            catch(System.ArgumentException)
            {
                // Never promote rejected physics samples to gate position.
                return;
            }
            if(drive.sqrMagnitude>.0001f)
                vehicle.AddForce(new Vector3(drive.x,0,drive.y)*48f,ForceMode.Force);
            if(p.y< -6)
            {
                falls++;policy.Reset();emergency=false;
                entryGate.MovePosition(new Vector3(0,1.4f,-2));
                exitGate.MovePosition(new Vector3(0,1.4f,2));
                TeleportTo(new Vector3(0,1.1f,-7.5f));
            }
            Paint();
        }
        private void Paint()
        {
            if(entryPaint==null||exitPaint==null||policy==null)return;
            entryPaint.material.color=Current.EntryOpen>=1?Color.green:
                Current.EntryOpen>0?Color.yellow:Color.red;
            exitPaint.material.color=Current.ExitOpen>=1?Color.green:
                Current.ExitOpen>0?Color.yellow:Color.red;
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12,12,390,390),GUI.skin.box);
            GUILayout.Label("B01-25 AIRLOCK001 R1 / TRUE PHYSICAL DUAL INTERLOCK");
            GUILayout.Label("Phase: "+Current.Phase+" | cycles: "+Current.CompletedCycles);
            GUILayout.Label("Entry "+Current.EntryOpen.ToString("F2")+" / Exit "+Current.ExitOpen.ToString("F2"));
            GUILayout.Label("Light / Heavy: "+(heavy?"12kg Heavy":"1.5kg Light")+
                " | emergencies: "+Current.EmergencyRequests+" | falls: "+Falls);
            GUILayout.Label("SAFE always-open bypass X=4. Doors never overlap.");
            if(GUILayout.Button("Switch Light / Heavy"))SetHeavy(!heavy);
            if(GUILayout.Button(paused?"Resume":"Pause"))SetPaused(!paused);
            if(GUILayout.Button("Emergency return / hold safe exit"))RequestEmergency();
            if(GUILayout.Button("Restart"))Restart();
            float x=0,z=0;
            if(GUILayout.RepeatButton("Drive toward exit (+Z)"))z=1;
            if(GUILayout.RepeatButton("Drive backward (-Z)"))z=-1;
            if(GUILayout.RepeatButton("Steer bypass (+X)"))x=1;
            if(GUILayout.RepeatButton("Steer (-X)"))x=-1;
            SetDrive(x,z);
            GUILayout.EndArea();
        }
    }
}
