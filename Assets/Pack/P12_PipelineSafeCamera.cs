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
        [SerializeField] float distance=7.8f,minDistance=3,maxDistance=18,mouseSensitivity=.12f,minPitch=18,maxPitch=58,zoomSpeed=.015f;
        float yaw,pitch=18;
        bool cursorLocked=true,gameplayInputEnabled=true;
        Camera001Rig rig;
        public Transform Pivot=>pivot;
        public Transform TargetRoot=>targetRoot;
        public Camera001Rig ReceiverRig=>rig;
        void EnsureRig()
        {
            if(rig!=null)return;
            rig=GetComponent<Camera001Rig>()??gameObject.AddComponent<Camera001Rig>();
            rig.enabled=false;rig.WorldColliderFilter=IsCameraWorld;
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
            yaw+=delta.x*mouseSensitivity;pitch=Mathf.Clamp(pitch-delta.y*mouseSensitivity,Mathf.Max(18,minPitch),maxPitch);
            distance=Mathf.Clamp(distance-scroll*zoomSpeed,minDistance,Mathf.Max(18,maxDistance));
        }
        void LateUpdate()
        {
            if(!gameplayInputEnabled||targetRoot==null||pivot==null)return;
            EnsureRig();rig.SetOrbit(yaw,Mathf.Max(18,pitch),distance);rig.StepCamera(Time.deltaTime);
        }
        void ApplyCursor(){Cursor.lockState=cursorLocked?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!cursorLocked;}
    }
}
