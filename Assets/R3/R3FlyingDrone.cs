using System;
using System.Collections.Generic;
using RoboRougelike.R1;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

namespace RoboRougelike.R3
{
    public sealed class R3SupportTurret : MonoBehaviour,IRRLTeamMember
    {
        R3VehicleAssembly owner;Rigidbody ownerBody;VehicleEnergyBank energy;float radius,rate;
        Collider[] hits=new Collider[64];readonly HashSet<VehicleEnergyBank> recipients=new HashSet<VehicleEnergyBank>();
        public int TeamId=>owner!=null?RRLHitRouter.Team(owner.transform):0;
        public void Configure(R3VehicleAssembly assembly,float range,float energyPerSecond)
        {owner=assembly;ownerBody=assembly!=null?assembly.GetComponent<Rigidbody>():null;energy=assembly!=null?assembly.GetComponent<VehicleEnergyBank>():null;radius=range;rate=energyPerSecond;}
        void FixedUpdate()
        {
            if(owner==null||ownerBody==null||energy==null){Destroy(gameObject);return;}
            recipients.Clear();
            if((ownerBody.position-transform.position).sqrMagnitude<=radius*radius)recipients.Add(energy);
            int count;
            while(true)
            {
                count=Physics.OverlapSphereNonAlloc(transform.position,radius,hits,~0,QueryTriggerInteraction.Ignore);
                if(count<hits.Length)break;
                if(hits.Length>=4096){hits=Physics.OverlapSphere(transform.position,radius,~0,QueryTriggerInteraction.Ignore);count=hits.Length;break;}
                Array.Resize(ref hits,hits.Length*2);
            }
            int team=TeamId;
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];if(hit==null||team==0)continue;
                var bank=hit.GetComponentInParent<VehicleEnergyBank>();
                if(bank==null||bank==energy||!bank.gameObject.activeInHierarchy||RRLHitRouter.Team(bank.transform)!=team)continue;
                var body=bank.GetComponent<Rigidbody>();
                if(body!=null&&(body.position-transform.position).sqrMagnitude<=radius*radius)recipients.Add(bank);
            }
            Array.Clear(hits,0,count);
            foreach(var recipient in recipients)if(recipient!=null)recipient.Recharge(rate*Time.fixedDeltaTime);
        }
    }
    // Uses the same physical HP as Repair Pulse; never revives destroyed parts.
    public sealed class R3HealingTurret : MonoBehaviour,IRRLTeamMember
    {
        R3VehicleAssembly owner;Rigidbody ownerBody;float radius,rate;Collider[] hits=new Collider[64];
        public int TeamId=>owner!=null?RRLHitRouter.Team(owner.transform):0;
        public void Configure(R3VehicleAssembly assembly,float range,float ratioPerSecond)
        {owner=assembly;ownerBody=assembly!=null?assembly.GetComponent<Rigidbody>():null;radius=range;rate=ratioPerSecond;}
        void FixedUpdate()
        {
            if(owner==null||ownerBody==null){Destroy(gameObject);return;}
            P12PipelineSafeDamageable selected=null;float weakest=1;
            foreach(var part in owner.Parts)
            {
                if(part==null||!part.Operational||!part.transform.IsChildOf(owner.transform)||part.Damageable==null||part.Damageable.MaxHp<=0)continue;
                Vector3 local=owner.transform.InverseTransformPoint(part.transform.position);
                Vector3 point=ownerBody.position+ownerBody.rotation*local;
                if((point-transform.position).sqrMagnitude>radius*radius)continue;
                float ratio=part.Damageable.CurrentHp/part.Damageable.MaxHp;
                if(ratio<weakest-.00001f){weakest=ratio;selected=part.Damageable;}
            }
            int count;
            while(true)
            {
                count=Physics.OverlapSphereNonAlloc(transform.position,radius,hits,~0,QueryTriggerInteraction.Ignore);
                if(count<hits.Length)break;
                if(hits.Length>=4096){hits=Physics.OverlapSphere(transform.position,radius,~0,QueryTriggerInteraction.Ignore);count=hits.Length;break;}
                Array.Resize(ref hits,hits.Length*2);
            }
            int team=TeamId;
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];if(hit==null||team==0||RRLHitRouter.Team(hit.transform)!=team||hit.transform.IsChildOf(transform))continue;
                var part=hit.GetComponentInParent<R3MountedPart>();var vehicle=hit.GetComponentInParent<R3VehicleAssembly>();
                bool vehiclePart=part!=null&&part.Operational&&vehicle!=null&&part.transform.IsChildOf(vehicle.transform);
                if(!vehiclePart&&hit.GetComponentInParent<R3FlyingDrone>()==null&&hit.GetComponentInParent<R3HealingTurret>()==null&&hit.GetComponentInParent<R3SupportTurret>()==null&&hit.GetComponentInParent<R3ShieldDome>()==null)continue;
                var hp=vehiclePart?part.Damageable:hit.GetComponentInParent<P12PipelineSafeDamageable>();
                if(hp==null||hp.IsDead||hp.MaxHp<=0)continue;
                float ratio=hp.CurrentHp/hp.MaxHp;if(ratio<weakest-.00001f){weakest=ratio;selected=hp;}
            }
            if(selected!=null)selected.RestoreHealthRatio(Mathf.Min(1,weakest+rate*Time.fixedDeltaTime));
        }
    }
    // A physical, temporary ally. It follows a loose radius rather than a fixed offset.
    public sealed class R3FlyingDrone : MonoBehaviour,IRRLTeamMember
    {
        Transform owner;Rigidbody body;Collider ownCollider;float speed,interval,damage,nextShot,nextScan;
        P12PipelineSafeEnemyTurret target;Collider aimCollider;RoboRougelike.R6.R6VehicleDrive groundDrive;bool stationary;
        public bool IsGround=>groundDrive!=null;
        public bool IsTurret=>stationary;
        public int TeamId=>RRLHitRouter.Team(owner);
        public void Configure(Transform source,float moveSpeed,float fireInterval,float shotDamage)
        {owner=source;speed=moveSpeed;interval=fireInterval;damage=shotDamage;body=GetComponent<Rigidbody>();ownCollider=GetComponent<Collider>();groundDrive=GetComponent<RoboRougelike.R6.R6VehicleDrive>();}
        public void ConfigureTurret(Transform source,float fireInterval,float shotDamage)
        {Configure(source,0,fireInterval,shotDamage);stationary=true;}
        void FixedUpdate()
        {
            if(owner==null||body==null){Destroy(gameObject);return;}
            Vector3 delta=owner.position-body.position;Vector3 horizontal=Vector3.ProjectOnPlane(delta,Vector3.up);
            if(!stationary&&groundDrive!=null)
            {
                if(horizontal.magnitude>6)groundDrive.StepToward(horizontal.normalized,speed,90);
                else groundDrive.Step(0,0,speed,speed,90);
            }
            else if(!stationary)
            {
                Vector3 desired=horizontal.magnitude>6?horizontal.normalized*speed:Vector3.zero;
                desired.y=Mathf.Clamp((owner.position.y+3-body.position.y)*2,-speed,speed);
                body.AddForce(Vector3.ClampMagnitude((desired-body.linearVelocity)*3,speed*4),ForceMode.Acceleration);
            }
            var run=R1RunController.Instance;
            if(run!=null&&run.Player!=null&&owner==run.Player.transform&&run.EmergencyRescueAttackBlocked)return;
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
