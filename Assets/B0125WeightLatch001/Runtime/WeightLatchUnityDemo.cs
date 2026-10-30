using UnityEngine;
using B01.LevelChallenges.WeightLatch;

namespace B0125.WeightLatch
{
    [DisallowMultipleComponent]
    public sealed class WeightLatchUnityDemo : MonoBehaviour
    {
        private WeightLatchController policy;
        private Rigidbody gateBody,vehicle;
        private Collider plateCollider;
        private Renderer gateRenderer,plateRenderer;
        private bool heavy,paused;
        private Vector2 drive;
        private int falls;
        private GateState last;
        public Rigidbody GateBody=>gateBody;
        public Rigidbody Vehicle=>vehicle;
        public Collider PlateCollider=>plateCollider;
        public double OpenFraction=>policy==null?0:policy.State.OpenFraction;
        public double HoldRemaining=>policy==null?0:policy.State.HoldRemainingSeconds;
        public double Clock=>policy==null?0:policy.State.ElapsedSeconds;
        public GateMotion Motion=>policy==null?GateMotion.Closed:policy.State.Motion;
        public int ActivationCount=>policy==null?0:policy.State.ActivationCount;
        public bool PlatePressed=>last.QualifiedHeavyContact;
        public bool Heavy=>heavy;
        public bool Paused=>paused;
        public int Falls=>falls;

        private GameObject Floor(string label,Vector3 center,Vector3 size,Color color)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);
            o.name=label;o.transform.SetParent(transform,false);
            o.transform.position=center;o.transform.localScale=size;
            o.GetComponent<Renderer>().material.color=color;
            return o;
        }

        private void Awake()
        {
            policy=new WeightLatchController(new GateConfig(8,1.5,-3,1,1.2,3));
            Floor("SOUTH ARRIVAL real floor",new Vector3(0,-.25f,-6),new Vector3(6,.5f,6),Color.gray);
            Floor("CENTRAL GATE route floor",new Vector3(0,-.25f,0),new Vector3(6,.5f,6),Color.gray);
            Floor("NORTH EXIT real floor",new Vector3(0,-.25f,6),new Vector3(6,.5f,6),Color.gray);
            Floor("PERMANENT BYPASS X4 actual safe physical lane",
                new Vector3(4,-.25f,0),new Vector3(2.5f,.5f,18),Color.green);
            var plate=Floor("PHYSICAL HEAVY PRESSURE PAD zMinus3",
                new Vector3(0,.025f,-3),new Vector3(3,.05f,2),Color.yellow);
            plateCollider=plate.GetComponent<BoxCollider>();
            plateRenderer=plate.GetComponent<Renderer>();
            var door=Floor("PHYSICAL VERTICAL LATCH GATE z0",
                new Vector3(0,1.3f,0),new Vector3(5.8f,2.6f,.7f),Color.red);
            gateBody=door.AddComponent<Rigidbody>();
            gateBody.isKinematic=true;
            gateBody.useGravity=false;
            gateBody.interpolation=RigidbodyInterpolation.Interpolate;
            gateRenderer=door.GetComponent<Renderer>();

            var unit=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unit.name="Real dynamic LIGHT or HEAVY Rigidbody";
            unit.transform.SetParent(transform,false);
            vehicle=unit.AddComponent<Rigidbody>();
            vehicle.useGravity=true;
            vehicle.interpolation=RigidbodyInterpolation.Interpolate;
            vehicle.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            vehicle.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
            SetHeavy(false);
        }
        private static Vector3 Spawn()=>new Vector3(0,1.1f,-7.5f);
        public void TeleportTo(Vector3 pos)
        {
            if(vehicle==null)return;
            vehicle.linearVelocity=Vector3.zero;
            vehicle.angularVelocity=Vector3.zero;
            vehicle.position=pos;
            vehicle.transform.position=pos;
            Physics.SyncTransforms();
        }
        public void SetHeavy(bool isHeavy)
        {
            heavy=isHeavy;
            if(vehicle==null)return;
            vehicle.mass=isHeavy?12f:1.5f;
            vehicle.transform.localScale=isHeavy?
                new Vector3(1.6f,1f,1.6f):Vector3.one;
            Restart();
        }
        public void SetDrive(float sideways,float forward)
        {
            drive=Vector2.ClampMagnitude(new Vector2(
                Mathf.Clamp(sideways,-1,1),Mathf.Clamp(forward,-1,1)),1);
        }
        public void SetPaused(bool isPaused)
        {
            paused=isPaused;
            if(vehicle==null)return;
            if(isPaused){vehicle.linearVelocity=Vector3.zero;vehicle.angularVelocity=Vector3.zero;}
            vehicle.isKinematic=isPaused;
            if(!isPaused)vehicle.WakeUp();
        }
        public void Restart()
        {
            if(policy==null||gateBody==null||vehicle==null)return;
            paused=false;drive=Vector2.zero;falls=0;
            policy.Reset();last=policy.State;
            gateBody.position=new Vector3(0,1.3f,0);
            gateBody.transform.position=gateBody.position;
            vehicle.isKinematic=false;TeleportTo(Spawn());
            Paint();
        }
        private bool RealPressurePlateContact()
        {
            if(vehicle==null||plateCollider==null)return false;
            var capsule=vehicle.GetComponent<Collider>();
            if(capsule==null||capsule.bounds.extents.y<=0)return false;
            Vector3 p=vehicle.position;
            return plateCollider.Raycast(new Ray(p+Vector3.up*.1f,Vector3.down),
                out _,capsule.bounds.extents.y+.4f);
        }
        private void FixedUpdate()
        {
            if(policy==null||vehicle==null||gateBody==null||paused)return;
            float dt=Time.fixedDeltaTime;
            if(dt<=0||dt>.05f)return; // unsupported host cadence: no silent multi-step pretending
            Vector3 p=vehicle.position;
            bool contact=RealPressurePlateContact();
            try
            {
                last=policy.Advance(new GateInput(dt,p.x,p.z,vehicle.mass,
                    contact,false,p.y>=-4));
            }
            catch(System.ArgumentException)
            {
                // Fail closed; no previous frame gate position is promoted.
                return;
            }
            gateBody.MovePosition(new Vector3(0,1.3f+(float)(last.OpenFraction*2.9),0));
            if(drive.sqrMagnitude>.001f)
                vehicle.AddForce(new Vector3(drive.x,0,drive.y)*48f,ForceMode.Force);
            if(p.y< -6)
            {
                falls++;
                policy.Reset();
                last=policy.State;
                gateBody.MovePosition(new Vector3(0,1.3f,0));
                TeleportTo(Spawn());
            }
            Paint();
        }
        private void Paint()
        {
            if(gateRenderer==null||plateRenderer==null||policy==null)return;
            gateRenderer.material.color=Motion==GateMotion.Open?Color.green:
                Motion==GateMotion.Raising?Color.yellow:
                Motion==GateMotion.Lowering?Color.magenta:Color.red;
            plateRenderer.material.color=PlatePressed?Color.green:Color.yellow;
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12,12,365,360),GUI.skin.box);
            GUILayout.Label("WEIGHTLATCH001 R1 / real moving physical gate");
            GUILayout.Label("Gate "+Motion+" | opened "+(OpenFraction*100).ToString("F0")+"%");
            GUILayout.Label("Heavy pressure "+PlatePressed+" / hold "+HoldRemaining.ToString("F1")+"s");
            GUILayout.Label((heavy?"HEAVY 12kg":"LIGHT 1.5kg")+
                " | activations "+ActivationCount+" | falls "+Falls);
            GUILayout.Label("Permanent safe bypass: world X=4, outside gate");
            if(GUILayout.Button("Switch Light/Heavy"))SetHeavy(!heavy);
            if(GUILayout.Button(paused?"Resume":"Pause"))SetPaused(!paused);
            if(GUILayout.Button("Restart"))Restart();
            float x=0,z=0;
            if(GUILayout.RepeatButton("Drive +Z"))z=1;
            if(GUILayout.RepeatButton("Drive -Z"))z=-1;
            if(GUILayout.RepeatButton("Steer to bypass +X"))x=1;
            if(GUILayout.RepeatButton("Steer -X"))x=-1;
            SetDrive(x,z);
            GUILayout.EndArea();
        }
    }
}
