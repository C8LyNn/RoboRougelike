using UnityEngine;
using B01.CAMERA001;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace RogueMachine.P12PipelineSafe
{
    // Existing input/API facade is the only automatic pose owner. The source rig
    // stays disabled and is stepped exactly once, before the weapon LateUpdate500.
    [DefaultExecutionOrder(100)]
    public class P12PipelineSafeCamera : MonoBehaviour
    {
        [SerializeField] Transform targetRoot,pivot;
        [SerializeField] float distance=7.8f,minDistance=3,maxDistance=45,mouseSensitivity=.12f,minPitch=-5,maxPitch=58,zoomSpeed=.015f;
        [Header("Camera follow comfort (mouse orbit remains direct)")]
        [SerializeField,Min(.015f)] float horizontalFollowSeconds=.06f,verticalFollowSeconds=.12f,framingFollowSeconds=.22f;
        [SerializeField,Min(.015f)] float zoomFollowSeconds=.10f;
        float yaw,pitch=18;
        float renderedDistance,zoomVelocity;
        bool zoomInitialized;
        bool cursorLocked=true,gameplayInputEnabled=true;
        Camera001Rig rig;
        public Transform Pivot=>pivot;
        public Transform TargetRoot=>targetRoot;
        public Camera001Rig ReceiverRig=>rig;
        public float RequestedZoomDistance=>distance;
        public float SmoothedZoomDistance=>renderedDistance;
        void EnsureRig()
        {
            if(rig!=null)return;
            rig=GetComponent<Camera001Rig>()??gameObject.AddComponent<Camera001Rig>();
            rig.enabled=false;rig.WorldColliderFilter=IsCameraWorld;rig.RequireFullHullFraming=true;rig.ReceiverHullBounds=StableVehicleBounds;
            rig.ReceiverResponsiveMotion=true;
            rig.ReceiverHorizontalDamping=horizontalFollowSeconds;rig.ReceiverVerticalDamping=verticalFollowSeconds;rig.ReceiverFramingDamping=framingFollowSeconds;
            rig.ReceiverZoomMaximum=maxDistance;
        }
        static Bounds StableVehicleBounds(Transform root)
        {
            bool any=false;Bounds total=default;
            foreach(var c in root.GetComponentsInChildren<Collider>()){
                if(c==null||!c.enabled||c.isTrigger||!c.gameObject.activeInHierarchy)continue;
                var part=c.GetComponentInParent<RoboRougelike.R3.R3MountedPart>();
                var pivot=part!=null&&part.GunPivot!=null&&c.transform.IsChildOf(part.GunPivot)?part.GunPivot:null;
                var shape=RenderShapeBounds(c,pivot!=null?pivot.position:Vector3.zero,out float radius);
                // The camera cannot chase a barrel that is itself chasing the
                // camera reticle. Its full rotational envelope stays constant;
                // chassis motion and actual mounting/detachment still update it.
                if(pivot!=null)shape=new Bounds(pivot.position,Vector3.one*radius*2);
                if(!any){total=shape;any=true;}else total.Encapsulate(shape);
            }
            return any?total:Camera001Rig.GetHullBounds(root);
        }
        static Bounds RenderShapeBounds(Collider c,Vector3 pivot,out float radius)
        {
            var t=c.transform;var scale=t.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            if(c is SphereCollider sphere){
                float r=sphere.radius*Mathf.Max(scale.x,scale.y,scale.z);var center=t.TransformPoint(sphere.center);
                radius=Vector3.Distance(center,pivot)+r;return new Bounds(center,Vector3.one*r*2);
            }
            if(c is CapsuleCollider cap){
                int axis=cap.direction;float r=cap.radius*Mathf.Max(scale[(axis+1)%3],scale[(axis+2)%3]);
                float segment=Mathf.Max(0,cap.height*scale[axis]*.5f-r);var direction=Vector3.zero;direction[axis]=1;
                direction=t.TransformDirection(direction).normalized;var center=t.TransformPoint(cap.center);
                radius=Vector3.Distance(center,pivot)+segment+r;
                var extents=Vector3.one*r+new Vector3(Mathf.Abs(direction.x),Mathf.Abs(direction.y),Mathf.Abs(direction.z))*segment;
                return new Bounds(center,extents*2);
            }
            Bounds local;
            if(c is BoxCollider box)local=new Bounds(box.center,box.size);
            else if(c is MeshCollider mesh&&mesh.sharedMesh!=null)local=mesh.sharedMesh.bounds;
            else {var fallback=c.bounds;radius=Vector3.Distance(fallback.center,pivot)+fallback.extents.magnitude;return fallback;}
            var matrix=t.localToWorldMatrix;Bounds world=default;radius=0;
            for(int i=0;i<8;i++){
                var e=local.extents;var corner=local.center+new Vector3((i&1)==0?-e.x:e.x,(i&2)==0?-e.y:e.y,(i&4)==0?-e.z:e.z);
                var p=matrix.MultiplyPoint3x4(corner);radius=Mathf.Max(radius,Vector3.Distance(p,pivot));
                if(i==0)world=new Bounds(p,Vector3.zero);else world.Encapsulate(p);
            }
            return world;
        }
        static bool IsCameraWorld(Collider c)=>c.GetComponentInParent<RoboRougelike.R3.R3Debris>()==null&&
            c.GetComponentInParent<P17PhysicalShell>()==null&&
            c.GetComponentInParent<RoboRougelike.Mechanics.Rockets.RocketProjectile>()==null;
        public void SetGameplayInputEnabled(bool active)
        {
            if(active&&!gameplayInputEnabled&&rig!=null)rig.ResetSmoothing();
            gameplayInputEnabled=active;cursorLocked=active;ApplyCursor();
        }
        public void Configure(Transform root,Transform cameraPivot)
        {
            targetRoot=root;pivot=cameraPivot;EnsureRig();rig.Configure(root,GetComponent<Camera>());
        }
        void Start(){EnsureRig();if(targetRoot!=null){yaw=targetRoot.eulerAngles.y;rig.Configure(targetRoot,GetComponent<Camera>());}ApplyCursor();}
        void OnEnable(){if(rig!=null)rig.ResetSmoothing();}
        void Update()
        {
            if(!gameplayInputEnabled)return;
#if ENABLE_INPUT_SYSTEM
            bool escape=Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame;
            Vector2 delta=Mouse.current!=null?Mouse.current.delta.ReadValue():Vector2.zero;
            float scroll=Mouse.current!=null?Mouse.current.scroll.ReadValue().y:0;
#else
            bool escape=Input.GetKeyDown(KeyCode.Escape);
            Vector2 delta=new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*8;
            float scroll=Input.mouseScrollDelta.y*120;
#endif
            if(escape&&RoboRougelike.R1.R1RunController.Instance==null){cursorLocked=!cursorLocked;ApplyCursor();}
            if(!cursorLocked)return;
            yaw+=delta.x*mouseSensitivity;pitch=Mathf.Clamp(pitch-delta.y*mouseSensitivity,minPitch,maxPitch);
            distance=Mathf.Clamp(distance-scroll*zoomSpeed,minDistance,maxDistance);
        }
        void LateUpdate()
        {
            if(!gameplayInputEnabled||targetRoot==null||pivot==null)return;
            EnsureRig();var body=targetRoot.GetComponent<Rigidbody>();
            rig.ReceiverFollowSpeed=body!=null?body.linearVelocity.magnitude:0;
            if(!zoomInitialized){renderedDistance=distance;zoomInitialized=true;}
            renderedDistance=Mathf.SmoothDamp(renderedDistance,distance,ref zoomVelocity,Mathf.Max(.015f,zoomFollowSeconds),Mathf.Infinity,Time.deltaTime);
            rig.SetOrbit(yaw,pitch,renderedDistance);rig.StepCamera(Time.deltaTime);
        }
        void ApplyCursor(){if(Application.isBatchMode)return;Cursor.lockState=cursorLocked?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!cursorLocked;}
    }
}
