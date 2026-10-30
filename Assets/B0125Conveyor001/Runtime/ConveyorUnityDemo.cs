using UnityEngine;
using B01.LevelChallenges.Conveyor;

namespace B0125.Conveyor
{
    [DisallowMultipleComponent]
    public sealed class ConveyorUnityDemo : MonoBehaviour
    {
        private ConveyorController logic;
        private Rigidbody vehicle;
        private Collider physicalBelt;
        private Renderer beltRenderer;
        private bool heavy,paused;
        private Vector2 drive;
        private int fallRestarts;
        private ConveyorFrame last;
        public Rigidbody Vehicle=>vehicle;
        public Collider BeltCollider=>physicalBelt;
        public double ForceNewtons=>last.ForceZ;
        public BeltPhase Phase=>last.Phase;
        public bool Supported=>last.Supported;
        public double Clock=>logic==null?0:logic.Last.ElapsedSeconds;
        public bool Heavy=>heavy;
        public bool Paused=>paused;
        public int FallRestarts=>fallRestarts;

        private GameObject Slab(string name,Vector3 center,Vector3 scale,Color color)
        {
            var slab=GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name=name;slab.transform.SetParent(transform,false);
            slab.transform.position=center;slab.transform.localScale=scale;
            slab.GetComponent<Renderer>().material.color=color;
            return slab;
        }
        private void Awake()
        {
            logic=new ConveyorController(new ConveyorConfig(1.5,5,2.5,.5,3.5,.4,65));
            Slab("REAL SOUTH APPROACH floor BoxCollider",new Vector3(0,-.25f,-7),
                new Vector3(6,.5f,4),Color.gray);
            Slab("REAL NORTH EXIT floor BoxCollider",new Vector3(0,-.25f,7),
                new Vector3(6,.5f,4),Color.gray);
            Slab("PERMANENT SIDE BYPASS X4 separate BoxCollider",
                new Vector3(4,-.25f,0),new Vector3(2.5f,.5f,18),Color.green);
            var strip=Slab("CONVEYOR ACTIVE PHYSICAL STRIP X3 Z10",
                new Vector3(0,-.15f,0),new Vector3(3,.3f,10),Color.cyan);
            physicalBelt=strip.GetComponent<BoxCollider>();
            beltRenderer=strip.GetComponent<Renderer>();
            var capsule=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name="REAL Light or Heavy dynamic Rigidbody";
            capsule.transform.SetParent(transform,false);
            vehicle=capsule.AddComponent<Rigidbody>();
            vehicle.useGravity=true;
            vehicle.interpolation=RigidbodyInterpolation.Interpolate;
            vehicle.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            vehicle.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
            SetHeavy(false);
        }
        private static Vector3 Start()=>new Vector3(0,1.2f,-7.3f);
        public void TeleportTo(Vector3 point)
        {
            if(vehicle==null)return;
            vehicle.linearVelocity=Vector3.zero;
            vehicle.angularVelocity=Vector3.zero;
            vehicle.position=point;
            vehicle.transform.position=point;
            Physics.SyncTransforms();
        }
        public void SetHeavy(bool value)
        {
            heavy=value;
            if(vehicle==null)return;
            vehicle.mass=value?12f:1.5f;
            vehicle.transform.localScale=value?new Vector3(1.5f,1,1.5f):Vector3.one;
            Restart();
        }
        public void SetDrive(float leftRight,float forwardBack)
        {
            drive=Vector2.ClampMagnitude(new Vector2(Mathf.Clamp(leftRight,-1,1),
                Mathf.Clamp(forwardBack,-1,1)),1);
        }
        public void SetPaused(bool value)
        {
            paused=value;if(vehicle==null)return;
            if(value){vehicle.linearVelocity=Vector3.zero;vehicle.angularVelocity=Vector3.zero;}
            vehicle.isKinematic=value;
            if(!value)vehicle.WakeUp();
        }
        public void Restart()
        {
            if(logic==null||vehicle==null)return;
            paused=false;drive=Vector2.zero;fallRestarts=0;
            logic.Reset();last=logic.Last;
            vehicle.isKinematic=false;TeleportTo(Start());
            Paint();
        }
        private bool RealBeltColliderSupport()
        {
            if(vehicle==null||physicalBelt==null)return false;
            var hull=vehicle.GetComponent<Collider>();
            if(hull==null||hull.bounds.extents.y<=0)return false;
            Vector3 p=vehicle.position;
            // A raycast ONLY against this genuine belt's own collider;
            // no trigger, material, script flag or proximity alone authorizes force.
            return physicalBelt.Raycast(new Ray(p+Vector3.up*.1f,Vector3.down),
                out _,hull.bounds.extents.y+.4f);
        }
        private void FixedUpdate()
        {
            if(logic==null||vehicle==null||paused)return;
            float dt=Time.fixedDeltaTime;
            if(dt<=0||dt>.05f)return;
            Vector3 p=vehicle.position;
            bool support=RealBeltColliderSupport();
            try
            {
                last=logic.Advance(new ConveyorSample(dt,p.x,p.z,vehicle.mass,
                    vehicle.linearVelocity.z,support,p.y>=-4));
            }
            catch(System.ArgumentException)
            {
                // No partial advance or unauthorized force after bad physics data.
                return;
            }
            if(last.ForceZ!=0)
                vehicle.AddForce(Vector3.forward*(float)last.ForceZ,ForceMode.Force);
            if(drive.sqrMagnitude>.0001f)
                vehicle.AddForce(new Vector3(drive.x,0,drive.y)*20f,ForceMode.Force);
            if(p.y< -6)
            {
                fallRestarts++;
                logic.Reset();last=logic.Last;
                TeleportTo(Start());
            }
            Paint();
        }
        private void Paint()
        {
            if(beltRenderer==null||logic==null)return;
            switch(last.Phase)
            {
                case BeltPhase.Forward:beltRenderer.material.color=Color.green;break;
                case BeltPhase.ReversalWarning:beltRenderer.material.color=Color.yellow;break;
                case BeltPhase.Reverse:beltRenderer.material.color=Color.red;break;
                case BeltPhase.ForwardWarning:beltRenderer.material.color=Color.yellow;break;
            }
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12,12,365,340),GUI.skin.box);
            GUILayout.Label("B01-25 CONVEYOR001 / REAL CONTACT FORCE");
            GUILayout.Label("Phase "+Phase+" | belt Fz "+ForceNewtons.ToString("F2")+"N");
            GUILayout.Label("Supported "+Supported+" | "+(heavy?"HEAVY 12kg":"LIGHT 1.5kg"));
            GUILayout.Label("Time "+Clock.ToString("F2")+"s | falls "+FallRestarts);
            GUILayout.Label("Green +Z / Yellow WARNING 0.5s / Red -Z");
            GUILayout.Label("SAFE SIDE ROUTE: X4 always outside belt");
            if(GUILayout.Button("Switch Light / Heavy"))SetHeavy(!heavy);
            if(GUILayout.Button(paused?"Resume":"Pause"))SetPaused(!paused);
            if(GUILayout.Button("Restart"))Restart();
            float x=0,z=0;
            if(GUILayout.RepeatButton("Drive +Z"))z=1;
            if(GUILayout.RepeatButton("Drive -Z"))z=-1;
            if(GUILayout.RepeatButton("Steer to safe bypass +X"))x=1;
            if(GUILayout.RepeatButton("Steer -X"))x=-1;
            SetDrive(x,z);
            GUILayout.EndArea();
        }
    }
}
