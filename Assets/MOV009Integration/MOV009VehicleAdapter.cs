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
                var module=part.GetComponent<ActiveSuspensionModule>();
                if(module==null)module=part.gameObject.AddComponent<ActiveSuspensionModule>();
                module.ConfigureSpring(RideHeightMeters,StiffnessPerModule,DampingPerModule,MaximumForcePerModule);
                part.gameObject.AddComponent<MOV009PartBinding>().Configure(part,module);
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
