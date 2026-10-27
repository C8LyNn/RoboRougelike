using System.Collections.Generic;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

namespace RoboRougelike.WeaponIntegration
{
    // Read-only presentation data from actual mounted parts, after their aim step.
    // Markers represent barrel directions, not guaranteed projectile impacts.
    public static class RRLWeaponReticles
    {
        public struct Marker
        {
            public string PartId;
            public R4WeaponKind Weapon;
            public Vector2 Viewport;
            public bool Obstructed,OutOfRange,Offscreen,Disabled,AreaWeapon;
        }
        static readonly RaycastHit[] hits=new RaycastHit[128];
        static readonly Collider[] overlaps=new Collider[128];

        static bool IsObstacle(Collider collider,R3MountedPart part,Transform owner)
        {
            if(collider==null||!collider.enabled||collider.isTrigger||!collider.gameObject.activeInHierarchy||
                collider.transform==owner||collider.transform.IsChildOf(owner))return false;
            // Existing outgoing projectiles must not drag the indicator down
            // the barrel every time the player fires.
            var shell=collider.GetComponentInParent<P17PhysicalShell>();
            if(shell!=null&&shell.OwnerRoot==owner)return false;
            var rocket=collider.GetComponentInParent<RoboRougelike.Mechanics.Rockets.RocketProjectile>();
            if(rocket!=null&&rocket.OwnerRoot==owner)return false;
            // RocketProjectile treats solid wreckage as an impact; standard
            // shells and beams use the shared hit router's ignore policy.
            return part.Weapon==R4WeaponKind.RocketLauncher||RRLHitRouter.CanHit(collider,owner);
        }

        // Spread numbered labels even when many barrel markers converge at the same point.
        public static Vector2 LabelOffset(int index)
        {
            float angle=Mathf.Max(0,index)*2.399963f;
            float distance=16+Mathf.Sqrt(Mathf.Max(0,index))*18;
            return new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*distance;
        }

        public static Vector2 ToHudPoint(Vector2 viewport,Rect pixelRect,Vector2 screen)
        {
            float scale=Mathf.Min(screen.x/1280f,screen.y/720f);
            if(scale<=0)return Vector2.zero;
            float left=(screen.x-1280*scale)*.5f,top=(screen.y-720*scale)*.5f;
            return new Vector2((pixelRect.x+viewport.x*pixelRect.width-left)/scale,
                (screen.y-(pixelRect.y+viewport.y*pixelRect.height)-top)/scale);
        }

        public static void Collect(Camera camera,R3VehicleAssembly assembly,P12PipelineSafeTankController controller,
            Vector3 aimPoint,List<Marker> output)
        {
            output.Clear();
            if(camera==null||assembly==null||controller==null)return;
            foreach(var part in assembly.Parts)
            {
                if(part==null||part.Kind!=R3PartKind.Gun||!part.Operational||!part.gameObject.activeInHierarchy||
                    !part.transform.IsChildOf(assembly.transform)||part.Muzzle==null)continue;
                bool emp=part.Weapon==R4WeaponKind.EmpEmitter, saw=part.Weapon==R4WeaponKind.RotarySaw;
                Vector3 origin=emp?part.transform.position:part.Muzzle.position;
                float range=RRLAimFeedback.Range(part,controller);
                var marker=new Marker{PartId=part.Id,Weapon=part.Weapon,Disabled=RRLEmpPartState.IsBlocked(part.gameObject),
                    AreaWeapon=emp||saw,OutOfRange=Vector3.Distance(origin,aimPoint)>range};
                Vector3 visiblePoint;
                if(emp)
                {
                    // The EMP affects an area around its emitter; it has no barrel trajectory.
                    visiblePoint=aimPoint;
                }
                else if(saw)
                {
                    // The saw damages at its real blade position, not at a distant ray impact.
                    visiblePoint=origin;
                }
                else
                {
                    Vector3 direction=part.Muzzle.forward.normalized;
                    float depth=Vector3.Dot(aimPoint-origin,camera.transform.forward);
                    float facing=Vector3.Dot(direction,camera.transform.forward);
                    if(depth<=0||facing<=.0001f)
                    {
                        Vector3 local=camera.transform.InverseTransformDirection(direction);
                        Vector2 edge=new Vector2(local.x,local.y);
                        if(edge.sqrMagnitude<.0001f)edge=Vector2.up;
                        marker.Viewport=Vector2.one*.5f+edge.normalized*2;
                        marker.Offscreen=true;output.Add(marker);continue;
                    }
                    float distance=depth/facing;
                    visiblePoint=origin+direction*distance;
                    // Trace only reachable space; the marker itself stays at the
                    // aiming plane and warns when the weapon cannot reach it.
                    float travel=Mathf.Min(distance,Mathf.Max(0,range));
                    float radius=RRLAimFeedback.ProjectileRadius(part);
                    int count=Physics.OverlapSphereNonAlloc(origin,Mathf.Max(.001f,radius),overlaps,~0,QueryTriggerInteraction.Ignore);
                    marker.Obstructed=count>=overlaps.Length;
                    for(int i=0;i<count&&!marker.Obstructed;i++)
                        marker.Obstructed=IsObstacle(overlaps[i],part,assembly.transform);
                    if(marker.Obstructed)visiblePoint=origin;
                    else if(travel>.001f)
                    {
                        count=radius>0?Physics.SphereCastNonAlloc(origin,radius,direction,hits,travel,~0,QueryTriggerInteraction.Ignore):
                            Physics.RaycastNonAlloc(origin,direction,hits,travel,~0,QueryTriggerInteraction.Ignore);
                        marker.Obstructed=count>=hits.Length;
                        float nearest=float.PositiveInfinity;RaycastHit best=default;
                        for(int i=0;i<count;i++)
                            if(hits[i].distance<nearest&&IsObstacle(hits[i].collider,part,assembly.transform))
                            {nearest=hits[i].distance;best=hits[i];}
                        if(nearest<float.PositiveInfinity)
                        {
                            visiblePoint=best.point;
                            marker.Obstructed=best.collider!=controller.LastConsumedAimCollider;
                        }
                    }
                }
                Vector3 viewport=camera.WorldToViewportPoint(visiblePoint);
                marker.Viewport=new Vector2(viewport.x,viewport.y);
                marker.Offscreen=viewport.z<=0||viewport.x<0||viewport.x>1||viewport.y<0||viewport.y>1;
                if(viewport.z<=0)
                {
                    Vector2 edge=marker.Viewport-Vector2.one*.5f;
                    if(edge.sqrMagnitude<.0001f)edge=Vector2.up;
                    marker.Viewport=Vector2.one*.5f-edge.normalized*2;
                }
                output.Add(marker);
            }
        }
    }
}
