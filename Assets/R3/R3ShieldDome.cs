using System.Collections.Generic;
using RoboRougelike.Mechanics.Rockets;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

namespace RoboRougelike.R3
{
    // The trigger never blocks vehicles or friendly outgoing projectiles. Ray and
    // sweep weapons explicitly query the same boundary through TryFirstHit.
    public sealed class R3ShieldDome : MonoBehaviour, IRRLTeamMember
    {
        static readonly List<R3ShieldDome> active = new List<R3ShieldDome>();
        P12PipelineSafeDamageable hp;
        SphereCollider boundary;
        int team;
        public int TeamId => team;
        public void Configure(int ownerTeam, float radius)
        {
            team = ownerTeam;
            hp = GetComponent<P12PipelineSafeDamageable>();
            boundary = GetComponent<SphereCollider>();
            boundary.isTrigger = true;
            boundary.radius = radius;
            var renderer = GetComponent<Renderer>();
            if(renderer == null) return;
            var material = renderer.sharedMaterial;
            if(material == null) return;
            for(int axis=0;axis<3;axis++)
            {
                var ring=new GameObject("Obrys kopuły "+axis);ring.transform.SetParent(transform,false);
                var line=ring.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=64;
                line.sharedMaterial=material;line.startColor=line.endColor=new Color(.15f,.8f,1f,.8f);line.startWidth=line.endWidth=.06f;
                for(int i=0;i<line.positionCount;i++)
                {
                    float angle=i*Mathf.PI*2/line.positionCount;float a=Mathf.Cos(angle)*radius,b=Mathf.Sin(angle)*radius;
                    line.SetPosition(i,axis==0?new Vector3(a,b,0):axis==1?new Vector3(a,0,b):new Vector3(0,a,b));
                }
            }
        }
        void OnEnable() { if(!active.Contains(this)) active.Add(this); }
        void OnDisable() { active.Remove(this); }
        bool Live => isActiveAndEnabled && boundary != null && boundary.enabled && hp != null && !hp.IsDead;
        bool Enemy(Transform source)
        {
            if(source == null) return false;
            int other = RRLHitRouter.Team(source);
            return team == 0 || other == 0 || team != other;
        }
        Vector3 Center => boundary.transform.TransformPoint(boundary.center);
        float Radius => boundary.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
        void OnTriggerEnter(Collider other)
        {
            if(!Live || other == null) return;
            var shell = other.GetComponentInParent<P17PhysicalShell>();
            if(shell != null && shell.IsActiveProjectile && shell.DamageAmount > 0 && Enemy(shell.OwnerRoot) &&
               (shell.LaunchPosition-Center).sqrMagnitude>Radius*Radius &&
               Vector3.Dot(other.transform.position-Center, shell.FlightVelocity) < 0)
            {
                float damage = shell.DamageAmount;
                Vector3 direction = shell.FlightVelocity.normalized;
                if(shell.Intercept(1f)) hp.ApplyDamage(damage, other.transform.position, direction);
                return;
            }
            var rocket = other.GetComponentInParent<RocketProjectile>();
            if(rocket == null || !rocket.IsInitialized || rocket.IsResolved || !Enemy(rocket.OwnerRoot) ||
               Vector3.Dot(other.transform.position-Center, rocket.FlightVelocity) >= 0) return;
            var payload = rocket.GetComponent<R3ShieldRocketPayload>();
            Vector3 launch=payload!=null?payload.LaunchPosition:rocket.OwnerRoot.position;
            if((launch-Center).sqrMagnitude<=Radius*Radius)return;
            float amount = payload != null ? payload.Damage : 60f;
            Vector3 point = other.transform.position, directionRocket = rocket.FlightVelocity.normalized;
            rocket.ApplyInterceptionDamage(float.MaxValue);
            hp.ApplyDamage(amount, point, directionRocket);
        }
        public static bool TryFirstHit(Vector3 start, Vector3 end, Transform source, out R3ShieldDome shield, out float distance)
        {
            shield = null; distance = float.PositiveInfinity;
            Vector3 delta = end-start; float length = delta.magnitude;
            if(length <= .00001f) return false;
            Vector3 direction = delta/length;
            foreach(var candidate in active)
            {
                if(candidate == null || !candidate.Live || !candidate.Enemy(source)) continue;
                Vector3 offset = start-candidate.Center; float radius = candidate.Radius;
                float c = Vector3.Dot(offset,offset)-radius*radius;
                if(c <= 0) continue; // A projectile starting inside may always leave.
                float b = Vector3.Dot(offset,direction), discriminant = b*b-c;
                if(discriminant < 0) continue;
                float hit = -b-Mathf.Sqrt(discriminant);
                if(hit < 0 || hit > length || hit >= distance) continue;
                distance=hit; shield=candidate;
            }
            return shield != null;
        }
        public void Absorb(float amount, Vector3 point, Vector3 direction)
        {
            if(Live && amount > 0 && !float.IsNaN(amount) && !float.IsInfinity(amount)) hp.ApplyDamage(amount,point,direction);
        }
    }
    public sealed class R3ShieldRocketPayload : MonoBehaviour,IRocketFlightBoundary
    {
        public float Damage = 60f;
        public Vector3 LaunchPosition {get;private set;}
        void OnEnable()=>LaunchPosition=transform.position;
        public bool TryIntercept(RocketProjectile projectile,Vector3 origin,Vector3 direction,float distance)
        {
            if(projectile==null||!projectile.IsInitialized||projectile.IsResolved||projectile.OwnerRoot==null||
               !R3ShieldDome.TryFirstHit(origin,origin+direction*distance,projectile.OwnerRoot,out var shield,out float hitDistance))return false;
            Vector3 point=origin+direction*hitDistance;var body=projectile.GetComponent<Rigidbody>();
            if(body!=null)body.position=point;projectile.transform.position=point;
            projectile.ApplyInterceptionDamage(float.MaxValue);
            if(!projectile.IsResolved)return false;
            shield.Absorb(Damage,point,direction);return true;
        }
    }
}
