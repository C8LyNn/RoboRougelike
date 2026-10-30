using UnityEngine;
using B01.LevelChallenges.SinkPlate;

namespace B0125.SinkPlate
{
    /// <summary>Isolated Unity 6 URP graybox with a real physics-simulated
    /// vehicle and collider-bearing collapse/restore plate. Not canonical gameplay.</summary>
    [DisallowMultipleComponent]
    public sealed class SinkPlateUnityDemo : MonoBehaviour
    {
        private SinkPlateController rule;
        private Rigidbody vehicle;
        private BoxCollider sinkingCollider;
        private Transform sinkingTransform;
        private Renderer sinkingRenderer;
        private Vector3 originalPlatePosition;
        private float motorX;
        private float motorZ;
        private bool heavy;
        private bool paused;
        private int falls;
        private PlateState latest;
        private const float MotorNewton = 70f;

        public Rigidbody Vehicle => vehicle;
        public BoxCollider PlateCollider => sinkingCollider;
        public PlatePhase Phase => rule == null ? PlatePhase.Ready : rule.Phase;
        public bool Heavy => heavy;
        public bool Paused => paused;
        public int Collapses => rule == null ? 0 : rule.CompletedCollapses;
        public int Falls => falls;
        public bool PlateSupportsVehicle => sinkingCollider != null && sinkingCollider.enabled;
        public double PhaseTime => rule == null ? 0 : rule.PhaseElapsed;

        private GameObject BuildCube(string label,Vector3 pos,Vector3 size)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name=label;
            g.transform.SetParent(transform,false);
            g.transform.position=pos;
            g.transform.localScale=size;
            // Primitive provides an actual physical BoxCollider.
            return g;
        }

        private void Awake()
        {
            rule=new SinkPlateController(new PlateConfig(4,.8,.35,1.2,.4));
            BuildCube("West Approach - collision floor",new Vector3(-7,-.25f,0),new Vector3(8,.5f,10));
            BuildCube("East Exit - collision floor",new Vector3(7,-.25f,0),new Vector3(8,.5f,10));
            BuildCube("PERMANENT SAFE NORTHERN BYPASS",new Vector3(0,-.25f,4),new Vector3(6,.5f,2));
            var plate=BuildCube("MASS-SENSITIVE COLLAPSIBLE PLATE",new Vector3(0,-.25f,0),new Vector3(6,.5f,3));
            sinkingTransform=plate.transform;
            sinkingCollider=plate.GetComponent<BoxCollider>();
            sinkingRenderer=plate.GetComponent<Renderer>();
            originalPlatePosition=plate.transform.position;

            var rig=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rig.name="Light or Heavy rigidbody (physical capsule)";
            rig.transform.SetParent(transform,false);
            vehicle=rig.AddComponent<Rigidbody>();
            vehicle.useGravity=true;
            vehicle.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            vehicle.interpolation=RigidbodyInterpolation.Interpolate;
            vehicle.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
            SetVehicle(false);
        }
        private static Vector3 StartPosition()=>new Vector3(-8,1.1f,0);
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
        public void MoveVehicleTo(Vector3 worldPosition)
        {
            if(vehicle==null)return;
            vehicle.linearVelocity=Vector3.zero;
            vehicle.angularVelocity=Vector3.zero;
            vehicle.position=worldPosition;
            vehicle.transform.position=worldPosition;
            Physics.SyncTransforms();
        }
        public void SetDrive(float x,float z)
        {motorX=Mathf.Clamp(x,-1,1);motorZ=Mathf.Clamp(z,-1,1);}
        public void SetPaused(bool value)
        {
            paused=value;
            if(vehicle!=null)
            {
                vehicle.isKinematic=value;
                if(!value)vehicle.WakeUp();
            }
        }
        public void Restart()
        {
            if(rule==null||vehicle==null)return;
            rule.Reset();
            paused=false;falls=0;motorX=0;motorZ=0;
            vehicle.isKinematic=false;
            MoveVehicleTo(StartPosition());
            latest=rule.Snapshot();
            UpdatePlate();
        }
        private void FixedUpdate()
        {
            if(rule==null||vehicle==null||paused)return;
            double dt=Time.fixedDeltaTime;
            if(dt<0||dt>.1)return; // host must substep; never silently skip a phase
            Vector3 pos=vehicle.position;
            bool onPlate=pos.y>-2f && Mathf.Abs(pos.x)<2.85f && Mathf.Abs(pos.z)<1.35f;
            latest=rule.Advance(new PlateInput(dt,onPlate,vehicle.mass));
            UpdatePlate();
            if(pos.y<-5f)
            {
                falls++;
                MoveVehicleTo(StartPosition());
            }
            if(Mathf.Abs(motorX)+Mathf.Abs(motorZ)>.01f)
            {
                Vector3 motion=new Vector3(motorX,0,motorZ).normalized*MotorNewton;
                vehicle.AddForce(motion,ForceMode.Force);
            }
        }
        private void UpdatePlate()
        {
            if(sinkingCollider==null||rule==null)return;
            var snap=rule.Snapshot();
            sinkingCollider.enabled=snap.SupportsVehicle;
            float y=0;
            if(snap.Phase==PlatePhase.Dropped)y=-3;
            if(snap.Phase==PlatePhase.Recovering)
                y=Mathf.Lerp(-3f,0f,(float)(snap.PhaseElapsed/rule.Config.RecoverySeconds));
            sinkingTransform.position=originalPlatePosition+Vector3.up*y;
            sinkingRenderer.material.color=snap.Phase==PlatePhase.Warning
                ? Color.yellow
                : snap.Phase==PlatePhase.Dropped?Color.red
                : snap.Phase==PlatePhase.Recovering?Color.cyan
                : Color.green;
            Physics.SyncTransforms();
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12,12,310,285),GUI.skin.box);
            GUILayout.Label("B01-25 SINKPLATE001 - ISOLATED DEMO");
            GUILayout.Label("State: "+Phase+" / collapses: "+Collapses+" / falls: "+Falls);
            GUILayout.Label("Vehicle: "+(heavy?"HEAVY 12 kg":"LIGHT 1.5 kg"));
            GUILayout.Label("SAFE ROUTE: north at world Z=4");
            if(GUILayout.Button("Switch LIGHT / HEAVY"))SetVehicle(!heavy);
            if(GUILayout.Button(paused?"RESUME":"PAUSE"))SetPaused(!paused);
            if(GUILayout.Button("RESTART"))Restart();
            float dx=0,dz=0;
            if(GUILayout.RepeatButton("DRIVE EAST (+X)"))dx=1;
            if(GUILayout.RepeatButton("DRIVE WEST (-X)"))dx=-1;
            if(GUILayout.RepeatButton("DRIVE NORTH (+Z)"))dz=1;
            if(GUILayout.RepeatButton("DRIVE SOUTH (-Z)"))dz=-1;
            SetDrive(dx,dz);
            GUILayout.EndArea();
        }
    }
}
