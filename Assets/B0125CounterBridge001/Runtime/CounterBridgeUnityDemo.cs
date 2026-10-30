using UnityEngine;
using B01.LevelChallenges.CounterBridge;

namespace B0125.CounterBridge
{
    /// <summary>
    /// Additive standalone Unity6 bridge: real kinematic Rigidbody and BoxCollider
    /// MoveRotation plus separate gravity-driven capsule and bridge Collider.Raycast.
    /// Controller calculates a deterministic spring/pressure response, not a
    /// simulated native PhysX test. No canonical gameplay or Save/Load references.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CounterBridgeUnityDemo : MonoBehaviour
    {
        private CounterBridgeController model;
        private Rigidbody bridgeBody,vehicle;
        private Collider bridgeCollider;
        private Renderer bridgeRenderer;
        private bool paused,heavy;
        private Vector2 drive;
        private int fallRestarts;
        private BridgeState last;
        public Rigidbody BridgeBody=>bridgeBody;
        public Rigidbody Vehicle=>vehicle;
        public Collider BridgeCollider=>bridgeCollider;
        public bool Paused=>paused;
        public bool Heavy=>heavy;
        public double AngleDegrees=>model==null?0:model.Last.AngleDeg;
        public double LastTorqueNm=>last.PressureTorqueNm;
        public bool Supported=>last.VehicleSupported;
        public double Clock=>model==null?0:model.Clock;
        public int FallRestarts=>fallRestarts;

        private GameObject Floor(string name,Vector3 position,Vector3 scale,Color color)
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name=name;
            cube.transform.SetParent(transform,false);
            cube.transform.position=position;
            cube.transform.localScale=scale;
            cube.GetComponent<Renderer>().material.color=color;
            return cube;
        }
        private static Vector3 Spawn()=>new Vector3(0,1.3f,-7.5f);
        private void Awake()
        {
            model=new CounterBridgeController(new CounterBridgeConfig(1.5,3,250,500,600,18));
            Floor("SOUTH APPROACH actual BoxCollider",new Vector3(0,-.25f,-6),
                new Vector3(6,.5f,6),Color.gray);
            Floor("NORTH EXIT actual BoxCollider",new Vector3(0,-.25f,6),
                new Vector3(6,.5f,6),Color.gray);
            Floor("PERMANENT EAST BYPASS x4 actual BoxCollider",
                new Vector3(4,-.25f,0),new Vector3(2.5f,.5f,18),Color.green);
            var board=Floor("COUNTERWEIGHT BRIDGE real collider 3x6",
                new Vector3(0,-.15f,0),new Vector3(3,.3f,6),Color.yellow);
            bridgeCollider=board.GetComponent<BoxCollider>();
            bridgeRenderer=board.GetComponent<Renderer>();
            bridgeBody=board.AddComponent<Rigidbody>();
            bridgeBody.isKinematic=true;
            bridgeBody.useGravity=false;
            bridgeBody.interpolation=RigidbodyInterpolation.Interpolate;
            var capsule=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name="Light or Heavy real dynamic Rigidbody";
            capsule.transform.SetParent(transform,false);
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
            vehicle.transform.localScale=value?new Vector3(1.55f,1,1.55f):Vector3.one;
            Restart();
        }
        public void SetDrive(float sideways,float forward)
        {
            drive=Vector2.ClampMagnitude(new Vector2(Mathf.Clamp(sideways,-1,1),
                Mathf.Clamp(forward,-1,1)),1);
        }
        public void TeleportTo(Vector3 position)
        {
            if(vehicle==null)return;
            vehicle.linearVelocity=Vector3.zero;
            vehicle.angularVelocity=Vector3.zero;
            vehicle.position=position;
            vehicle.transform.position=position;
            Physics.SyncTransforms();
        }
        public void SetPaused(bool pause)
        {
            paused=pause;
            if(vehicle==null)return;
            if(pause)
            {
                vehicle.linearVelocity=Vector3.zero;
                vehicle.angularVelocity=Vector3.zero;
            }
            vehicle.isKinematic=pause;
            if(!pause)vehicle.WakeUp();
        }
        public void Restart()
        {
            if(model==null||vehicle==null||bridgeBody==null)return;
            paused=false;drive=Vector2.zero;fallRestarts=0;
            model.Reset();
            bridgeBody.position=new Vector3(0,-.15f,0);
            bridgeBody.rotation=Quaternion.identity;
            bridgeBody.transform.rotation=Quaternion.identity;
            vehicle.isKinematic=false;
            TeleportTo(Spawn());
            last=model.Last;
            bridgeRenderer.material.color=Color.yellow;
        }
        private bool ActualBridgeSupport()
        {
            if(vehicle==null||bridgeCollider==null)return false;
            Vector3 p=vehicle.position;
            // Physical contact probe against THIS genuine platform collider only.
            // No trigger or mass flag can fake a collision. Short downward range
            // follows the actual world-space capsule collider vertical envelope.
            var capsule=vehicle.GetComponent<Collider>();
            float extent=capsule==null?0:capsule.bounds.extents.y;
            if(extent<=0)return false;
            return bridgeCollider.Raycast(new Ray(p+Vector3.up*.1f,Vector3.down),
                out _,extent+.35f);
        }
        private void FixedUpdate()
        {
            if(model==null||vehicle==null||paused)return;
            float dt=Time.fixedDeltaTime;
            if(dt<=0||dt>.05f)return;
            Vector3 p=vehicle.position;
            bool supported=ActualBridgeSupport();
            try
            {
                last=model.Advance(new BridgeSample(dt,p.x,p.z,vehicle.mass,supported,false,p.y>=-4));
            }
            catch(System.ArgumentException)
            {
                // Invalid host input is never allowed to advance the bridge.
                return;
            }
            bridgeBody.MoveRotation(Quaternion.Euler((float)last.AngleDeg,0,0));
            bridgeRenderer.material.color=last.AtMechanicalStop?Color.red:
                supported?Color.yellow:Color.cyan;
            if(drive.sqrMagnitude>.0001f)
                vehicle.AddForce(new Vector3(drive.x,0,drive.y)*45f,ForceMode.Force);
            if(p.y< -6)
            {
                fallRestarts++;
                model.Reset();
                bridgeBody.MoveRotation(Quaternion.identity);
                TeleportTo(Spawn());
            }
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12,12,350,340),GUI.skin.box);
            GUILayout.Label("B01-25 COUNTERBRIDGE001 R1 - ACTUAL PHYSICAL DEMO");
            GUILayout.Label("Bridge X tilt "+AngleDegrees.ToString("F2")+
                "deg | torque "+LastTorqueNm.ToString("F1")+"Nm");
            GUILayout.Label((heavy?"HEAVY 12kg":"LIGHT 1.5kg")+
                " | contact "+Supported+" | fall restarts "+fallRestarts);
            GUILayout.Label("Tilt limit +/-18 degrees; spring recovers after departure");
            GUILayout.Label("SAFE ALTERNATIVE: x=4, permanently separate floor");
            if(GUILayout.Button("Switch Light / Heavy"))SetHeavy(!heavy);
            if(GUILayout.Button(paused?"Resume":"Pause"))SetPaused(!paused);
            if(GUILayout.Button("Restart"))Restart();
            float x=0,z=0;
            if(GUILayout.RepeatButton("Drive forward (+Z)"))z=1;
            if(GUILayout.RepeatButton("Drive back (-Z)"))z=-1;
            if(GUILayout.RepeatButton("Steer toward bypass (+X)"))x=1;
            if(GUILayout.RepeatButton("Steer left (-X)"))x=-1;
            SetDrive(x,z);
            GUILayout.EndArea();
        }
    }
}
