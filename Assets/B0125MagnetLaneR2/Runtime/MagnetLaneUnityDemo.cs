using UnityEngine;
using B01.LevelChallenges.MagnetLane.R2;

namespace B0125.MagnetLane.R2
{
    /// <summary>
    /// Optional standalone real Unity physics graybox. Source controller produces
    /// ForceNewtons; host AddForce applies it to an actual Rigidbody, mass matters.
    /// No canonical vehicle, armor, weapon or Codex-owned scene is modified.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MagnetLaneUnityDemo : MonoBehaviour
    {
        private MagnetLaneController controller;
        private Rigidbody vehicle;
        private Renderer fieldRenderer;
        private bool heavy,paused;
        private Vector2 drive;
        private MagnetOutput last;
        private Vector3 previousPhysicsPosition;
        private int fallRespawns;
        public Rigidbody Vehicle=>vehicle;
        public bool Heavy=>heavy;
        public bool Paused=>paused;
        public MagnetPhase Phase=>controller==null?MagnetPhase.Warning:controller.PhaseAt(controller.Clock);
        public int Polarity=>controller==null?1:controller.Polarity;
        public int Cycles=>controller==null?0:controller.CompletedCycles;
        public int FallRespawns=>fallRespawns;
        public double Clock=>controller==null?0:controller.Clock;
        public double LastForceZ=>last.ForceZ;

        private GameObject CreateFloor(string label,Vector3 at,Vector3 scale)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name=label;
            part.transform.SetParent(transform,false);
            part.transform.position=at;
            part.transform.localScale=scale;
            return part; // Real primitive BoxCollider always enabled.
        }

        private void Awake()
        {
            controller=new MagnetLaneController(new MagnetConfig(.6,2.4,1,60,3,1.5));
            CreateFloor("WEST physical approach",new Vector3(-7,-.25f,0),new Vector3(8,.5f,10));
            CreateFloor("EAST physical exit",new Vector3(7,-.25f,0),new Vector3(8,.5f,10));
            CreateFloor("PERMANENT SAFE BYPASS world Z4",new Vector3(0,-.25f,4),new Vector3(6,.5f,2));
            var field=CreateFloor("MAGNETIC CENTRAL LANE 6x3",new Vector3(0,-.25f,0),new Vector3(6,.5f,3));
            fieldRenderer=field.GetComponent<Renderer>();
            var capsule=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name="Actual real Light or Heavy Rigidbody";
            capsule.transform.SetParent(transform,false);
            vehicle=capsule.AddComponent<Rigidbody>();
            vehicle.useGravity=true;
            vehicle.interpolation=RigidbodyInterpolation.Interpolate;
            vehicle.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            vehicle.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
            SetVehicle(false);
        }

        private static Vector3 Spawn()=>new Vector3(-8f,1.1f,0f);
        public void TeleportTo(Vector3 position)
        {
            if(vehicle==null)return;
            vehicle.linearVelocity=Vector3.zero;
            vehicle.angularVelocity=Vector3.zero;
            vehicle.position=position;
            vehicle.transform.position=position;
            previousPhysicsPosition=position; // teleports/restarts are NOT physical swept travel
            Physics.SyncTransforms();
        }
        public void SetVehicle(bool useHeavy)
        {
            heavy=useHeavy;
            if(vehicle!=null)
            {
                vehicle.mass=heavy?12f:1.5f;
                vehicle.transform.localScale=heavy?new Vector3(1.7f,1f,1.5f):Vector3.one;
                Restart();
            }
        }
        public void SetDrive(float x,float z)
        {
            drive=Vector2.ClampMagnitude(new Vector2(Mathf.Clamp(x,-1,1),Mathf.Clamp(z,-1,1)),1);
        }
        public void SetPaused(bool value)
        {
            paused=value;
            if(vehicle!=null)
            {
                if(value)
                {
                    vehicle.linearVelocity=Vector3.zero;
                    vehicle.angularVelocity=Vector3.zero;
                }
                vehicle.isKinematic=value;
                if(!value)vehicle.WakeUp();
            }
        }
        public void Restart()
        {
            if(controller==null||vehicle==null)return;
            controller.Reset();
            paused=false;drive=Vector2.zero;fallRespawns=0;
            vehicle.isKinematic=false;
            TeleportTo(Spawn());
            last=new MagnetOutput(MagnetPhase.Warning,0,0,0,0,1,0,false);
            PaintPhase();
        }
        private void FixedUpdate()
        {
            if(controller==null||vehicle==null||paused)return;
            float dt=Time.fixedDeltaTime;
            if(dt<=0||dt>.1f)return; // host must substep; never skip an entire hazard cycle
            Vector3 pos=vehicle.position;
            last=controller.Advance(new MagnetSample(dt,previousPhysicsPosition.x,previousPhysicsPosition.z,
                pos.x,pos.z,vehicle.mass,false,pos.y>=-2&&previousPhysicsPosition.y>=-2));
            previousPhysicsPosition=pos;
            if(last.ForceZ!=0)
                vehicle.AddForce(Vector3.forward*(float)last.ForceZ,ForceMode.Force);
            if(drive.sqrMagnitude>0.001f)
                vehicle.AddForce(new Vector3(drive.x,0,drive.y)*48f,ForceMode.Force);
            if(pos.y<-5)
            {
                fallRespawns++;
                TeleportTo(Spawn());
            }
            PaintPhase();
        }
        private void PaintPhase()
        {
            if(fieldRenderer==null||controller==null)return;
            fieldRenderer.material.color=Phase==MagnetPhase.Warning?Color.yellow:
                Phase==MagnetPhase.Active?(Polarity==1?Color.magenta:Color.cyan):Color.green;
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12,12,310,280),GUI.skin.box);
            GUILayout.Label("MAGNETLANE001 R2 W15 - swept force demo");
            GUILayout.Label("Phase "+Phase+" | polarity "+Polarity+" | cycles "+Cycles);
            GUILayout.Label("Field force Z="+LastForceZ.ToString("F1")+"N");
            GUILayout.Label("Rig "+(heavy?"HEAVY 12kg":"LIGHT 1.5kg")+
                            " | falls "+FallRespawns);
            GUILayout.Label("BYPASS: Northern world Z=4 (always safe)");
            if(GUILayout.Button("Switch LIGHT / HEAVY"))SetVehicle(!heavy);
            if(GUILayout.Button(paused?"RESUME":"PAUSE"))SetPaused(!paused);
            if(GUILayout.Button("RESTART"))Restart();
            float x=0,z=0;
            if(GUILayout.RepeatButton("DRIVE +X"))x=1;
            if(GUILayout.RepeatButton("DRIVE -X"))x=-1;
            if(GUILayout.RepeatButton("DRIVE +Z"))z=1;
            if(GUILayout.RepeatButton("DRIVE -Z"))z=-1;
            SetDrive(x,z);
            GUILayout.EndArea();
        }
    }
}
