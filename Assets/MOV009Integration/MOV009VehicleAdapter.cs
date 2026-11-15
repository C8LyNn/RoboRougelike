using System.Linq;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RoboRougelike.Mechanics.MOV009;

namespace RoboRougelike.MOV009Integration
{
    // Only assemblies containing actual suspension parts opt into vertical dynamics.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public sealed class MOV009VehicleAdapter : MonoBehaviour
    {
        R3VehicleAssembly assembly;
        Rigidbody body;
        ActiveSuspensionController suspension;
        R6VehicleDrive drive;
        public ActiveSuspensionController Controller => suspension;
        public float NominalMass {get;private set;}
        public float StiffnessPerModule {get;private set;}
        public float DampingPerModule {get;private set;}
        public float MaximumForcePerModule {get;private set;}
        public float RideHeightMeters {get;private set;}
        public const float ChassisContactFriction=.15f;
        public string ProtectionReason {get;private set;}
        public string GroundingNotice => assembly!=null&&suspension!=null&&drive!=null&&
            assembly.Model.SelectedDrive==R3DriveKind.Wheels&&drive.Wheels>0&&drive.GroundedWheels==0&&
            suspension.SuspensionEnabled&&suspension.SupportMode==ActiveSuspensionSupportMode.FullSupport&&
            suspension.CurrentRideHeightOffsetMeters>0
            ?"Koła bez kontaktu — obniż zawieszenie, aby odzyskać napęd.":null;
        public int OperationalSupports => GetComponentsInChildren<ActiveSuspensionModule>().Count(m=>m.IsAvailableFor(body));

        public void Configure(R3VehicleAssembly vehicle)
        {
            assembly=vehicle;body=GetComponent<Rigidbody>();
            drive=GetComponent<R6VehicleDrive>();drive.RefreshMass();drive.EnableSuspensionPhysics(ChassisContactFriction);
            suspension=GetComponent<ActiveSuspensionController>();
            if(suspension==null)suspension=gameObject.AddComponent<ActiveSuspensionController>();
            var parts=assembly.Parts.Where(p=>p.Kind==R3PartKind.Suspension&&p.Operational).ToArray();
            NominalMass=body.mass;float supportedMass=NominalMass/Mathf.Max(4,parts.Length);
            StiffnessPerModule=supportedMass*Physics.gravity.magnitude/.12f;
            DampingPerModule=.85f*2*Mathf.Sqrt(StiffnessPerModule*supportedMass);
            MaximumForcePerModule=2.4f*supportedMass*Physics.gravity.magnitude;
            // Wheel tread reaches .65m below the root; add the nominal .12m spring compression.
            // A higher default unloads the rigid wheel treads and loses real traction under roll.
            RideHeightMeters=.77f;
            foreach(var part in parts)
            {
                // The package measures and applies force at its own transform. Project
                // onto the chassis reference plane so a module mounted high on the
                // vehicle retains the same calibrated ride height and probe reach.
                var chassisPoint=transform.InverseTransformPoint(part.transform.position);
                ActiveSuspensionModule module;
                if(Mathf.Abs(chassisPoint.y)<.001f)
                {
                    // Keep the original force point and hierarchy for floor-level mounts.
                    module=part.GetComponent<ActiveSuspensionModule>();
                    if(module==null)module=part.gameObject.AddComponent<ActiveSuspensionModule>();
                }
                else
                {
                    chassisPoint.y=0f;
                    var point=part.transform.Find("Suspension support point");
                    if(point==null)
                    {
                        point=new GameObject("Suspension support point").transform;
                        point.SetParent(part.transform,false);
                    }
                    point.position=transform.TransformPoint(chassisPoint);
                    module=point.GetComponent<ActiveSuspensionModule>();
                    if(module==null)module=point.gameObject.AddComponent<ActiveSuspensionModule>();
                }
                int units=R3VehicleModel.TierEffectUnits(part.Tier);
                module.ConfigureSpring(RideHeightMeters,StiffnessPerModule*units,DampingPerModule*Mathf.Sqrt(units),MaximumForcePerModule*units);
                var binding=part.GetComponent<MOV009PartBinding>();
                if(binding==null)binding=part.gameObject.AddComponent<MOV009PartBinding>();
                binding.Configure(part,module);
            }
            suspension.RefreshModules();ApplySettings();
        }
        public void ApplySettings()
        {
            if(assembly==null||suspension==null)return;
            var model=assembly.Model;
            bool hover=model.SelectedDrive==R3DriveKind.Hover;
            bool competing=GetComponentsInChildren<WheelCollider>().Any(c=>c.enabled&&c.gameObject.activeInHierarchy&&c.suspensionSpring.spring>0);
            ProtectionReason=hover?"Hover aktywny — zawieszenie nie podaje siły.":competing?"FullSupport zablokowane: aktywne sprężyny WheelCollider.":null;
            if(competing&&model.SuspensionMode==ActiveSuspensionSupportMode.FullSupport)model.SuspensionMode=ActiveSuspensionSupportMode.AssistOnly;
            suspension.SetSupportMode(model.SuspensionMode);
            suspension.SetSuspensionEnabled(model.SuspensionEnabled&&!hover);
            suspension.SetRideHeightOffset(model.SuspensionHeightOffset);
            model.SuspensionHeightOffset=suspension.RequestedRideHeightOffsetMeters;
        }
        void FixedUpdate()
        {
            // Runs before the package controller: its force budget uses this live mass.
            if(drive!=null)drive.RefreshMass();
            if(suspension!=null&&assembly.Model.SelectedDrive==R3DriveKind.Hover&&suspension.SuspensionEnabled)ApplySettings();
            if(suspension!=null&&suspension.SupportMode==ActiveSuspensionSupportMode.FullSupport&&
                GetComponentsInChildren<WheelCollider>().Any(c=>c.enabled&&c.gameObject.activeInHierarchy&&c.suspensionSpring.spring>0))ApplySettings();
        }
    }
}
