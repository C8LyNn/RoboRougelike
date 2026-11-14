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
        R3MountedBonusEffects bonuses;
        float appliedDurability=1f;
        int team;
        public int TeamId => team;
        public void Configure(int ownerTeam, float radius,R3MountedBonusEffects ownerBonuses=null)
        {
            team = ownerTeam;
            hp = GetComponent<P12PipelineSafeDamageable>();
            bonuses=ownerBonuses;RefreshDurability();
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
        void Update()=>RefreshDurability();
        void RefreshDurability()
        {
            float multiplier=bonuses!=null?bonuses.ShieldDurabilityMultiplier:1f;
            if(hp==null||multiplier==appliedDurability)return;
            hp.SetDurabilityMultiplier(multiplier);appliedDurability=multiplier;
        }
        bool Live => isActiveAndEnabled && boundary != null && boundary.enabled && hp != null && !hp.IsDead;
        bool Enemy(Transform source)
        {
            if(source == null) return false;
            int other = RRLHitRouter.Team(source);
            return team == 0 || other == 0 || team != other;
        }
        Vector3 Center => boundary.transform.TransformPoint(boundary.center);
        float Radius => boundary.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
        public readonly struct BlastBoundary
        {
            public readonly R3ShieldDome Dome;
            public readonly Vector3 Center,Point;
            public readonly float Radius,Distance;
            public BlastBoundary(R3ShieldDome dome,Vector3 center,float radius,Vector3 point,float distance)
            {Dome=dome;Center=center;Radius=radius;Point=point;Distance=distance;}
        }
        public static void CollectBlastBoundaries(Vector3 origin,float blastRadius,Transform source,List<BlastBoundary> output)
        {
            output.Clear();
            foreach(var dome in active)
            {
                if(dome==null||!dome.Live||!dome.Enemy(source))continue;
                Vector3 center=dome.Center,offset=origin-center;float radius=dome.Radius,fromCenter=offset.magnitude;
                if(fromCenter<=radius||fromCenter-radius>blastRadius||fromCenter<=.00001f)continue;
                float distance=fromCenter-radius;Vector3 point=center+offset*(radius/fromCenter);
                output.Add(new BlastBoundary(dome,center,radius,point,distance));
            }
        }
        public static void CollectExposedBlastBoundaries(Vector3 origin,float blastRadius,Transform source,List<BlastBoundary> all,List<BlastBoundary> exposed)
        {
            CollectBlastBoundaries(origin,blastRadius,source,all);exposed.Clear();
            foreach(var boundary in all)
            {
                if(TryFirstHit(origin,boundary.Point,source,out var first,out _)&&first!=boundary.Dome)continue;
                exposed.Add(boundary);
            }
        }
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
    public sealed class R3ShieldRocketPayload : MonoBehaviour,IRocketFlightBoundary,IRocketBlastBoundary
    {
        public float Damage = 60f;
        public Vector3 LaunchPosition {get;private set;}
        readonly List<R3ShieldDome.BlastBoundary> blastBoundaries=new List<R3ShieldDome.BlastBoundary>();
        readonly List<R3ShieldDome.BlastBoundary> visibleBlastBoundaries=new List<R3ShieldDome.BlastBoundary>();
        Vector3 blastOrigin;RocketSpec blastSpec;
        void OnEnable()=>LaunchPosition=transform.position;
        public bool TryIntercept(RocketProjectile projectile,Vector3 origin,Vector3 direction,float distance)
        {
            if(projectile==null||!projectile.IsInitialized||projectile.IsResolved||projectile.OwnerRoot==null||
               !R3ShieldDome.TryFirstHit(origin,origin+direction*distance,projectile.OwnerRoot,out var shield,out float hitDistance))return false;
            Vector3 point=origin+direction*hitDistance,stop=point-direction*.02f;var body=projectile.GetComponent<Rigidbody>();
            if(body!=null)body.position=stop;projectile.transform.position=stop;
            bool explodes=projectile.ExplodesWhenIntercepted;
            projectile.ApplyInterceptionDamage(float.MaxValue);
            if(!projectile.IsResolved)return false;
            if(!explodes)shield.Absorb(Damage,point,direction);return true;
        }
        public bool PrepareBlast(Vector3 center,RocketSpec spec,Transform ownerRoot,RaycastHit[] rayHits)
        {
            blastOrigin=center;blastSpec=spec;visibleBlastBoundaries.Clear();
            R3ShieldDome.CollectBlastBoundaries(center,spec.Radius,ownerRoot,blastBoundaries);
            foreach(var boundary in blastBoundaries)
            {
                if(BlocksBlastBefore(boundary.Point,boundary.Dome))continue;
                if(boundary.Distance<=.001f){visibleBlastBoundaries.Add(boundary);continue;}
                Vector3 direction=(boundary.Point-center)/boundary.Distance;
                int count=Physics.RaycastNonAlloc(center,direction,rayHits,Mathf.Max(0,boundary.Distance-.001f),spec.OcclusionMask,QueryTriggerInteraction.Ignore);
                if(count>=rayHits.Length)return false;
                if(count==0)visibleBlastBoundaries.Add(boundary);
            }
            return true;
        }
        public bool BlocksBlast(Vector3 targetPoint)=>BlocksBlastBefore(targetPoint,null);
        bool BlocksBlastBefore(Vector3 targetPoint,R3ShieldDome ignored)
        {
            Vector3 delta=targetPoint-blastOrigin;float length=delta.magnitude;if(length<=.00001f)return false;
            Vector3 direction=delta/length;
            foreach(var boundary in blastBoundaries)
            {
                if(boundary.Dome==ignored)continue;
                Vector3 offset=blastOrigin-boundary.Center;float c=Vector3.Dot(offset,offset)-boundary.Radius*boundary.Radius;
                if(c<=0)continue;
                float b=Vector3.Dot(offset,direction),discriminant=b*b-c;
                if(discriminant<0)continue;
                float distance=-b-Mathf.Sqrt(discriminant);
                if(distance>=0&&distance<length-.001f)return true;
            }
            return false;
        }
        public void CommitBlast()
        {
            foreach(var boundary in visibleBlastBoundaries)
            {
                float amount=RocketMath.BlastDamage(boundary.Distance,blastSpec.Radius,blastSpec.CenterDamage,blastSpec.EdgeDamageFraction);
                boundary.Dome?.Absorb(amount,boundary.Point,(boundary.Point-blastOrigin).normalized);
            }
        }
    }
}
