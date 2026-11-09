using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    // Shared transient query buffers; Unity calls these presentation paths on
    // the main thread and each caller consumes the result before the next query.
    internal static class RRLAimPhysicsQuery
    {
        const int MaxBuffer=4096;
        static Collider[] overlaps=new Collider[64];
        static RaycastHit[] hits=new RaycastHit[64];
        internal static Collider[] Overlap(Vector3 origin,float radius,out int count)
        {
            while(true)
            {
                count=Physics.OverlapSphereNonAlloc(origin,radius,overlaps,~0,QueryTriggerInteraction.Ignore);
                if(count<overlaps.Length)return overlaps;
                if(overlaps.Length>=MaxBuffer)
                {var all=Physics.OverlapSphere(origin,radius,~0,QueryTriggerInteraction.Ignore);count=all.Length;return all;}
                System.Array.Resize(ref overlaps,Mathf.Min(overlaps.Length*2,MaxBuffer));
            }
        }
        internal static RaycastHit[] Cast(Vector3 origin,float radius,Vector3 direction,float distance,out int count)
        {
            while(true)
            {
                count=radius>0?Physics.SphereCastNonAlloc(origin,radius,direction,hits,distance,~0,QueryTriggerInteraction.Ignore):
                    Physics.RaycastNonAlloc(origin,direction,hits,distance,~0,QueryTriggerInteraction.Ignore);
                if(count<hits.Length)return hits;
                if(hits.Length>=MaxBuffer)
                {
                    var all=radius>0?Physics.SphereCastAll(origin,radius,direction,distance,~0,QueryTriggerInteraction.Ignore):
                        Physics.RaycastAll(origin,direction,distance,~0,QueryTriggerInteraction.Ignore);
                    count=all.Length;return all;
                }
                System.Array.Resize(ref hits,Mathf.Min(hits.Length*2,MaxBuffer));
            }
        }
    }

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
        internal static float ProjectileRadius(R3MountedPart part)
        {
            if(part.Weapon==R4WeaponKind.RocketLauncher)return part.GetComponent<RRLRocketBinding>()?.Module?.CurrentSpec.CollisionRadius??0;
            if(part.Weapon==R4WeaponKind.Railgun)return part.GetComponent<RRLRailgunBinding>()?.Module?.CurrentSpec.Radius??0;
            if(part.Weapon==R4WeaponKind.BeamCutter||part.Weapon==R4WeaponKind.EmpEmitter||part.Weapon==R4WeaponKind.RotarySaw)return 0;
            return R4WeaponDefinition.Get(part.Weapon).Diameter*.5f;
        }
        // Use the same own-hull rule as the actual firing path.
        internal static bool OwnArmorBlocksShot(R3VehicleAssembly assembly,R3MountedPart part)
        {
            if(part.Weapon==R4WeaponKind.BeamCutter)
            {
                var module=part.GetComponent<RRLBeamBinding>()?.Module;
                return module!=null&&!assembly.ClearProjectilePath(part,part.Muzzle.forward,module.Settings.rangeMeters,.001f);
            }
            if(part.Weapon>R4WeaponKind.Railgun&&part.Weapon!=R4WeaponKind.RocketLauncher)return false;
            return !assembly.ClearProjectilePath(part);
        }
        static bool Blocks(Collider c,Collider target,Transform owner)
        {
            if(c==null||c==target||c.transform.IsChildOf(owner)||c.GetComponentInParent<R3Debris>()!=null)return false;
            var shell=c.GetComponentInParent<P17PhysicalShell>();if(shell!=null&&shell.OwnerRoot==owner)return false;
            var rocket=c.GetComponentInParent<RoboRougelike.Mechanics.Rockets.RocketProjectile>();if(rocket!=null&&rocket.OwnerRoot==owner)return false;
            return true;
        }
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
                if(part==null||part.Kind!=R3PartKind.Gun||!part.Operational||!part.gameObject.activeInHierarchy||!part.transform.IsChildOf(assembly.transform)||
                    part.Muzzle==null||RRLEmpPartState.IsBlocked(part.gameObject))continue;
                result.Guns++;
                Vector3 origin=part.Weapon==R4WeaponKind.EmpEmitter?part.transform.position:part.Muzzle.position;
                Vector3 direction=point-origin;float distance=direction.magnitude;
                if(distance>Range(part,controller))continue;
                result.InRange++;
                bool radial=part.Weapon==R4WeaponKind.EmpEmitter||part.Weapon==R4WeaponKind.RotarySaw;
                if(!radial&&Vector3.Angle(part.Muzzle.forward,direction)>.25f)continue;
                result.Aligned++;
                float radius=ProjectileRadius(part);
                var overlaps=RRLAimPhysicsQuery.Overlap(origin,Mathf.Max(.001f,radius),out int initial);
                bool blocked=false;
                for(int i=0;i<initial&&!blocked;i++)blocked=Blocks(overlaps[i],target,assembly.transform);
                var hits=RRLAimPhysicsQuery.Cast(origin,radius,direction.normalized,Mathf.Max(0,distance-.03f),out int count);
                for(int i=0;i<count&&!blocked;i++)
                    blocked=Blocks(hits[i].collider,target,assembly.transform);
                if(!blocked&&!OwnArmorBlocksShot(assembly,part))result.Clear++;
            }
            return result;
        }
    }
}
