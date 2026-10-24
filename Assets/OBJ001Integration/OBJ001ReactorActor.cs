using System;
using UnityEngine;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.Mechanics.OBJ001;
using RoboRougelike.WeaponIntegration;

namespace RoboRougelike.OBJ001Integration
{
    public sealed class OBJ001ReactorActor : MonoBehaviour,IReactorIntegritySource,IRRLTeamMember
    {
        public P12PipelineSafeDamageable Damageable {get;private set;}
        public ReactorDefenseObjective Objective {get;private set;}
        float configuredMaximum;
        public int TeamId=>1;
        public float CurrentIntegrity=>Damageable!=null&&!Damageable.IsDead?Mathf.Max(0,Damageable.CurrentHp):0;
        public float MaximumIntegrity=>Damageable!=null?Damageable.MaxHp:configuredMaximum;
        public static OBJ001ReactorActor Build(Transform parent,Material material,Vector3 position,float health)
        {
            if(float.IsNaN(health)||float.IsInfinity(health)||health<1)throw new ArgumentOutOfRangeException(nameof(health));
            var root=new GameObject("OBJ001 — Reaktor");root.transform.SetParent(parent,false);root.transform.position=position;
            var actor=root.AddComponent<OBJ001ReactorActor>();actor.configuredMaximum=health;
            var core=GameObject.CreatePrimitive(PrimitiveType.Cube);core.name="Rdzeń reaktora";core.transform.SetParent(root.transform,false);core.transform.localScale=Vector3.one*2.4f;
            core.GetComponent<Renderer>().sharedMaterial=material;
            actor.Damageable=core.AddComponent<P12PipelineSafeDamageable>();actor.Damageable.Configure(health,false);
            actor.Damageable.SetBaseTint(new Color(.15f,.85f,.65f));
            actor.Objective=root.AddComponent<ReactorDefenseObjective>();actor.Objective.SetAutomaticClock(false);
            if(!actor.Objective.BindIntegritySource(actor))throw new InvalidOperationException("Reactor integrity binding failed");
            actor.Damageable.Died+=actor.Objective.SynchronizeIntegrity;return actor;
        }
        public void Damage(float amount){if(Damageable!=null)Damageable.ApplyDamage(amount,Damageable.transform.position,Vector3.zero);}
        public void Repair(float amount)
        {
            if(Damageable!=null&&!Damageable.IsDead&&amount>0&&!float.IsNaN(amount)&&!float.IsInfinity(amount))
                Damageable.RestoreHealthRatio(Mathf.Clamp01((Damageable.CurrentHp+amount)/Damageable.MaxHp));
        }
        public void ResetIntegrity(float maximum)
        {
            // A destroyed physical actor is replaced by the host on restart, never resurrected here.
            if(Damageable!=null&&!Damageable.IsDead){configuredMaximum=maximum;Damageable.Configure(maximum,false);}
        }
        void OnDestroy(){if(Damageable!=null&&Objective!=null)Damageable.Died-=Objective.SynchronizeIntegrity;}
    }
}
