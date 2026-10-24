using System;
using System.Linq;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RoboRougelike.WeaponIntegration;
using RoboRougelike.Mechanics.IndependentSteering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RoboRougelike.PackageIntegration
{
    // Original controller produces angles only; the existing R6 drive consumes them once.
    [DisallowMultipleComponent]
    public sealed class RRLIndependentSteeringBinding : MonoBehaviour,IRRLDeviceLifecycle
    {
        public RRLDeviceBinding Device {get;private set;}
        public IndependentSteeringController Controller {get;private set;}
        public bool GeometrySupported {get;private set;}
        R3VehicleAssembly assembly;R3MountedPart[] wheelParts;SteeringWheelModule[] wheels;
        int lastInputFrame=-1;
        public static string ModeName(SteeringMode mode)=>mode==SteeringMode.RearOnly?"Tylna oś":
            mode==SteeringMode.CounterPhase?"Obie osie przeciwnie":mode==SteeringMode.Crab?"Krab":"Przednia oś";
        public string Status=>!GeometrySupported?"MOV011: wymagane 4 proste narożne koła • standardowy skręt R6":
            Device==null||!Device.HardwareAvailable?"MOV011: moduł niesprawny • standardowy skręt R6":
            assembly.Model.SelectedDrive!=R3DriveKind.Wheels?"MOV011: dostępny dla napędu kołowego":$"M • Skręt: {ModeName(Controller.Mode)}";
        public void Bind(R3PartState state,Transform owner)
        {
            Device=gameObject.AddComponent<RRLDeviceBinding>();Device.Bind(state,owner);
            assembly=owner.GetComponent<R3VehicleAssembly>();Controller=gameObject.AddComponent<IndependentSteeringController>();
            Controller.Bind(Device.Body);Controller.SetAutomaticFixedUpdate(false);
            Controller.SetMode(Enum.IsDefined(typeof(SteeringMode),state.SteeringMode)?state.SteeringMode:SteeringMode.FrontOnly);
            var nominal=assembly.Model.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray();
            GeometrySupported=nominal.Length==4;
            if(GeometrySupported)
            {
                int minX=nominal.Min(p=>p.Cell.x),maxX=nominal.Max(p=>p.Cell.x),minZ=nominal.Min(p=>p.Cell.z),maxZ=nominal.Max(p=>p.Cell.z);
                GeometrySupported=minX<maxX&&minZ<maxZ&&nominal.Select(p=>p.Cell.y).Distinct().Count()==1&&
                    nominal.Select(p=>p.Cell).Distinct().Count()==4&&nominal.All(p=>(p.Cell.x==minX||p.Cell.x==maxX)&&(p.Cell.z==minZ||p.Cell.z==maxZ)&&
                        R3MountingGrid.Normalize(p.RotationQuarterTurns)==Vector3Int.zero);
                if(GeometrySupported)
                {
                    Controller.Configure(new SteeringGeometry{wheelbaseMeters=(maxZ-minZ)*.55f,trackMeters=(maxX-minX)*.55f},SteeringProfile.Default);
                    wheelParts=assembly.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray();wheels=new SteeringWheelModule[wheelParts.Length];
                    for(int i=0;i<wheelParts.Length;i++)
                    {
                        var part=wheelParts[i];var role=part.Cell.z==maxZ?(part.Cell.x==minX?SteeringWheelPosition.FrontLeft:SteeringWheelPosition.FrontRight):
                            part.Cell.x==minX?SteeringWheelPosition.RearLeft:SteeringWheelPosition.RearRight;
                        wheels[i]=part.gameObject.AddComponent<SteeringWheelModule>();wheels[i].Configure(role,VisualPivot(part));Controller.RegisterWheel(wheels[i]);
                    }
                }
            }
            owner.GetComponent<R6VehicleDrive>().BindSteering(this);Device.Damageable.Died+=RefreshAvailability;RefreshAvailability();
        }
        static Transform VisualPivot(R3MountedPart part)
        {
            var payload=part.transform.Find("Koło");if(payload==null)return null;
            var original=payload.GetComponent<MeshRenderer>();var mesh=payload.GetComponent<MeshFilter>();if(original==null||mesh==null)return null;
            var pivot=new GameObject("MOV011 visual yaw").transform;pivot.SetParent(part.transform,false);pivot.localPosition=payload.localPosition;
            var visual=new GameObject("Existing wheel mesh");visual.transform.SetParent(pivot,false);
            visual.transform.localRotation=payload.localRotation;visual.transform.localScale=payload.localScale;
            visual.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;visual.AddComponent<MeshRenderer>().sharedMaterials=original.sharedMaterials;
            original.enabled=false;return pivot;
        }
        bool WheelAttached(R3MountedPart part)=>part!=null&&part.Operational&&part.transform.IsChildOf(Device.Owner)&&part.GetComponentInParent<Rigidbody>()==Device.Body;
        public void RefreshAvailability()
        {
            if(Controller==null||Device==null)return;
            if(!isActiveAndEnabled||!Device.HardwareAvailable||!GeometrySupported||assembly.Model.SelectedDrive!=R3DriveKind.Wheels)Controller.ResetSteering();
            if(wheels==null)return;
            for(int i=0;i<wheels.Length;i++)if(wheels[i]!=null)
            {bool attached=WheelAttached(wheelParts[i]);wheels[i].SetStructurallyAttached(attached);wheels[i].SetOperational(attached&&!RRLEmpPartState.IsBlocked(wheelParts[i].gameObject));}
        }
        public bool TrySolve(float input,float deltaTime,out SteeringSolution solution)
        {
            RefreshAvailability();solution=default;
            if(!isActiveAndEnabled||Device==null||!Device.Available||!GeometrySupported||assembly.Model.SelectedDrive!=R3DriveKind.Wheels)return false;
            Controller.SetSteeringInput(input);Controller.Step(deltaTime);solution=Controller.CurrentSolution;return true;
        }
        bool PlayerInput=>Device!=null&&Device.Available&&GeometrySupported&&assembly.Model.SelectedDrive==R3DriveKind.Wheels&&
            R1RunController.Instance?.Player?.transform==Device.Owner;
        public bool SetMode(SteeringMode mode)
        {
            if(!PlayerInput||!Enum.IsDefined(typeof(SteeringMode),mode))return false;
            Device.State.SteeringMode=mode;Controller.SetMode(mode);return true;
        }
        bool CycleMode()
        {
            if(lastInputFrame==Time.frameCount||!SetMode((SteeringMode)(((int)Controller.Mode+1)%4)))return false;
            lastInputFrame=Time.frameCount;return true;
        }
        public bool HandleGuiInput(Event input)=>input!=null&&input.type==EventType.KeyDown&&!input.alt&&!input.control&&!input.command&&
            (input.keyCode==KeyCode.M||input.character=='m'||input.character=='M')&&CycleMode();
        void Update()
        {
            RefreshAvailability();
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current?.mKey.wasPressedThisFrame==true)CycleMode();
#endif
        }
        void OnGUI(){if(HandleGuiInput(Event.current))Event.current.Use();}
        void OnTransformParentChanged()=>RefreshAvailability();
        void OnDisable(){if(Controller!=null)Controller.ResetSteering();}
        void OnDestroy(){if(Controller!=null)Controller.ResetSteering();if(Device?.Damageable!=null)Device.Damageable.Died-=RefreshAvailability;}
    }
}
