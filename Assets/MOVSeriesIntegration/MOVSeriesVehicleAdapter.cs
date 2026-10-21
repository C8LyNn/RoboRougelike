using System.Linq;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.MOVSeries
{
    [DisallowMultipleComponent,DefaultExecutionOrder(-300)]
    public sealed partial class MOVSeriesVehicleAdapter : MonoBehaviour
    {
        R3VehicleAssembly assembly;
        Rigidbody body;R6VehicleDrive drive;P12PipelineSafeTankController tank;
        P12PipelineSafeVehicleStructure structure;
        bool graphDirty;
        public R3VehicleModel Model=>assembly.Model;
        public string Feedback {get;private set;}
        public bool CanControl=>assembly!=null&&body!=null&&!body.isKinematic&&tank.GameplayInputEnabled&&structure.CoreAlive;
        public void Configure(R3VehicleAssembly vehicle)
        {
            assembly=vehicle;body=GetComponent<Rigidbody>();drive=GetComponent<R6VehicleDrive>();
            tank=GetComponent<P12PipelineSafeTankController>();structure=GetComponent<P12PipelineSafeVehicleStructure>();
            foreach(var part in assembly.Parts)part.Damageable.Died+=MarkGraphDirty;
            // Compound wheel contacts amplify Coulomb stiction. The drive already models
            // finite rolling/lateral resistance; retain a small physical contact friction.
            drive.RefreshMass();drive.EnableSuspensionPhysics(.05f);ConfigureDevices();ConfigureSpecialDevices();ConfigureDrives();SyncDevices();SyncDrives();
            gameObject.AddComponent<MOVSeriesInput>().Bind(this);
        }
        internal bool Available(R3MountedPart part)=>part!=null&&part.Operational&&
            part.transform.IsChildOf(transform)&&part.GetComponentInParent<Rigidbody>()==body&&structure!=null&&structure.CoreAlive&&structure.IsBlockConnected(part.Block);
        internal R3PartState State(R3MountedPart part)=>Model.Parts.First(p=>p.Id==part.Id);
        internal bool PrepareAction()
        {
            if(assembly==null)return false;structure.EvaluateStructure();drive.RefreshMass();SyncDevices();return CanControl;
        }
        public static bool IsTerrain(Collider c)=>c!=null&&c.attachedRigidbody==null&&
            c.GetComponentInParent<P12PipelineSafeDamageable>()==null&&c.GetComponentInParent<R3Debris>()==null;
        void FixedUpdate()
        {
            if(assembly==null)return;
            if(graphDirty){graphDirty=false;structure.EvaluateStructure();}
            drive.RefreshMass();SyncDevices();SyncDrives();
            if(!CanControl)CancelActions();
        }
        public void CancelActions()
        {
            foreach(var b in boosters)if(b!=null)b.StopBoost();foreach(var g in gyros)if(g!=null)g.SetRequestedActive(false);
            ReleaseGrapples();ClearDriveInput();
            if(omni!=null)omni.SetOperational(false);if(tracks!=null)tracks.SetDriveEnabled(false);if(hover!=null)hover.SetHoverMode(false);
        }
        void OnDisable(){CancelActions();}
        void MarkGraphDirty(){graphDirty=true;}
        void OnDestroy()
        {
            if(assembly!=null)foreach(var part in assembly.Parts)
                if(part!=null&&part.Damageable!=null)part.Damageable.Died-=MarkGraphDirty;
        }
    }
}
