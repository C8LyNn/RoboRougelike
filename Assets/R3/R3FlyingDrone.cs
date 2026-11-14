using System;
using RoboRougelike.R1;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

namespace RoboRougelike.R3
{
    // A physical, temporary ally. It follows a loose radius rather than a fixed offset.
    public sealed class R3FlyingDrone : MonoBehaviour,IRRLTeamMember
    {
        Transform owner;Rigidbody body;Collider ownCollider;float speed,interval,damage,nextShot,nextScan;
        P12PipelineSafeEnemyTurret target;Collider aimCollider;RoboRougelike.R6.R6VehicleDrive groundDrive;
        public bool IsGround=>groundDrive!=null;
        public int TeamId=>RRLHitRouter.Team(owner);
        public void Configure(Transform source,float moveSpeed,float fireInterval,float shotDamage)
        {owner=source;speed=moveSpeed;interval=fireInterval;damage=shotDamage;body=GetComponent<Rigidbody>();ownCollider=GetComponent<Collider>();groundDrive=GetComponent<RoboRougelike.R6.R6VehicleDrive>();}
        void FixedUpdate()
        {
            if(owner==null||body==null){Destroy(gameObject);return;}
            Vector3 delta=owner.position-body.position;Vector3 horizontal=Vector3.ProjectOnPlane(delta,Vector3.up);
            if(groundDrive!=null)
            {
                if(horizontal.magnitude>6)groundDrive.StepToward(horizontal.normalized,speed,90);
                else groundDrive.Step(0,0,speed,speed,90);
            }
            else
            {
                Vector3 desired=horizontal.magnitude>6?horizontal.normalized*speed:Vector3.zero;
                desired.y=Mathf.Clamp((owner.position.y+3-body.position.y)*2,-speed,speed);
                body.AddForce(Vector3.ClampMagnitude((desired-body.linearVelocity)*3,speed*4),ForceMode.Acceleration);
            }
            if(Time.time>=nextScan){nextScan=Time.time+.25f;FindTarget();}
            if(target==null||target.IsDefeated||aimCollider==null||Time.time<nextShot)return;
            Vector3 deltaAim=aimCollider.bounds.center-body.position;float distance=deltaAim.magnitude;
            if(distance<.1f||distance>40||!ClearShot(deltaAim/distance,distance))return;
            if(P17PhysicalShell.AvailableSlots<=0)return;
            Fire(deltaAim/distance);nextShot=Time.time+interval;
        }
        void FindTarget()
        {
            target=null;aimCollider=null;float nearest=40*40;
            var encounter=R1RunController.Instance?.Encounter;if(encounter==null)return;
            foreach(var enemy in encounter.Enemies)
            {
                if(enemy==null||enemy.IsDefeated||RRLHitRouter.Team(enemy.transform)==TeamId)continue;
                foreach(var hp in enemy.GetComponentsInChildren<P12PipelineSafeDamageable>())
                {
                    if(hp==null||hp.IsDead)continue;var collider=hp.GetComponent<Collider>();
                    if(collider==null||!collider.enabled||collider.isTrigger)continue;
                    float distance=(collider.bounds.center-body.position).sqrMagnitude;
                    if(distance<nearest){nearest=distance;target=enemy;aimCollider=collider;}
                }
            }
        }
        bool ClearShot(Vector3 direction,float distance)
        {
            var hits=Physics.RaycastAll(body.position,direction,distance,~0,QueryTriggerInteraction.Ignore);
            Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                if(hit.collider==null||hit.collider==ownCollider||hit.transform.IsChildOf(transform))continue;
                if(target!=null&&hit.transform.IsChildOf(target.transform))
                {if(hit.collider.GetComponentInParent<P12PipelineSafeDamageable>()!=null)return true;continue;}
                return false;
            }
            return true;
        }
        void Fire(Vector3 direction)
        {
            var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.name="P17_DronePhysicalShell";
            shell.transform.position=body.position+direction*.65f;shell.transform.localScale=Vector3.one*.16f;
            var renderer=GetComponent<Renderer>();if(renderer!=null)shell.GetComponent<Renderer>().sharedMaterial=renderer.sharedMaterial;
            var shellCollider=shell.GetComponent<Collider>();foreach(var c in GetComponentsInChildren<Collider>())if(c!=null)Physics.IgnoreCollision(shellCollider,c,true);
            var rb=shell.AddComponent<Rigidbody>();rb.mass=1;rb.useGravity=false;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.linearVelocity=direction*38;
            shell.AddComponent<P17PhysicalShell>().Configure(owner,damage,3,direction,target!=null?target.transform:null,45);
        }
    }
}
