using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    // Read-only geometry feedback, after the final camera pose and gun rotation.
    // It neither steers projectiles nor changes firing, resources or weapon balance.
    public static class RRLAimFeedback
    {
        public struct Snapshot
        {
            public int Guns,InRange,Aligned,Clear;
            public float Distance;
            public bool HasSurface,DamageableSurface;
            public string Caption => Guns==0?"Brak sprawnej broni":
                !HasSurface?"Brak celu • zasięg zależy od broni":
                InRange==0?$"{Distance:0} m • poza zasięgiem":
                Aligned==0?$"{Distance:0} m • obrót luf":
                Clear==0?$"{Distance:0} m • przeszkoda przy lufie":
                $"{Distance:0} m • tor: {Clear}/{Guns}"+(DamageableSurface?"":" • podłoże / osłona");
        }
        static readonly RaycastHit[] hits=new RaycastHit[64];
        public static float Range(R3MountedPart part,P12PipelineSafeTankController controller)
        {
            switch(part.Weapon)
            {
                case R4WeaponKind.Shotgun:
                    var shotgun=part.GetComponent<RRLShotgunBinding>();
                    if(shotgun?.Module==null)return 0;
                    var shot=shotgun.Module.CurrentSpec;
                    return Mathf.Min(shot.MaxTravelDistance,shot.ProjectileSpeed*shot.ProjectileLifetime);
                case R4WeaponKind.Railgun:
                    return part.GetComponent<RRLRailgunBinding>()?.Module?.CurrentSpec.Range??0;
                case R4WeaponKind.BeamCutter:
                    return part.GetComponent<RRLBeamBinding>()?.Module?.Settings.rangeMeters??0;
                case R4WeaponKind.RocketLauncher:
                    var rocket=part.GetComponent<RRLRocketBinding>();
                    if(rocket?.Module==null)return 0;
                    var spec=rocket.Module.CurrentSpec;
                    float accelerate=spec.Acceleration>0?Mathf.Min(spec.Lifetime,(spec.MaxSpeed-spec.InitialSpeed)/spec.Acceleration):0;
                    return spec.InitialSpeed*accelerate+.5f*spec.Acceleration*accelerate*accelerate+
                        (spec.Acceleration>0?spec.MaxSpeed:spec.InitialSpeed)*(spec.Lifetime-accelerate);
                case R4WeaponKind.RotarySaw:
                    return part.GetComponent<RRLSawBinding>()?.Module?.ContactRadius??0;
                case R4WeaponKind.EmpEmitter:
                    return part.GetComponent<RRLEmpBinding>()?.Module?.PulseRadius??0;
                default:return R4WeaponDefinition.Get(part.Weapon).Speed*controller.PhysicalShellLifetime;
            }
        }
        public static Snapshot Evaluate(R3VehicleAssembly assembly,P12PipelineSafeTankController controller,
            Vector3 point,Collider target)
        {
            var result=new Snapshot{HasSurface=target!=null,Distance=Vector3.Distance(controller.transform.position,point),
                DamageableSurface=target!=null&&(target.GetComponentInParent<P12PipelineSafeDamageable>()!=null||target.GetComponentInParent<IRRLHitReceiver>()!=null)};
            if(assembly==null)return result;
            foreach(var part in assembly.Parts)
            {
                if(part==null||part.Kind!=R3PartKind.Gun||!part.Operational||!part.transform.IsChildOf(assembly.transform)||
                    part.Muzzle==null||RRLEmpPartState.IsBlocked(part.gameObject))continue;
                result.Guns++;
                Vector3 origin=part.Weapon==R4WeaponKind.EmpEmitter?part.transform.position:part.Muzzle.position;
                Vector3 direction=point-origin;float distance=direction.magnitude;
                if(distance>Range(part,controller))continue;
                result.InRange++;
                bool radial=part.Weapon==R4WeaponKind.EmpEmitter||part.Weapon==R4WeaponKind.RotarySaw;
                if(!radial&&Vector3.Angle(part.Muzzle.forward,direction)>.25f)continue;
                result.Aligned++;
                int count=Physics.RaycastNonAlloc(origin,direction.normalized,hits,Mathf.Max(0,distance-.03f),~0,QueryTriggerInteraction.Ignore);
                bool blocked=count>=hits.Length;
                for(int i=0;i<count&&!blocked;i++)
                {
                    var c=hits[i].collider;
                    if(c==null||c==target||c.transform.IsChildOf(assembly.transform)||c.GetComponentInParent<R3Debris>()!=null)continue;
                    var shell=c.GetComponentInParent<P17PhysicalShell>();
                    if(shell!=null&&shell.OwnerRoot==assembly.transform)continue;
                    var rocket=c.GetComponentInParent<RoboRougelike.Mechanics.Rockets.RocketProjectile>();
                    if(rocket!=null&&rocket.OwnerRoot==assembly.transform)continue;
                    blocked=true;
                }
                if(!blocked)result.Clear++;
            }
            return result;
        }
    }
}
