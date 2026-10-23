using System.Collections.Generic;
using UnityEngine;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.Defenses.DirectionalShield;

namespace RoboRougelike.WeaponIntegration
{
    public interface IRRLPreHpFilter
    {
        float FilterDamage(P12PipelineSafeDamageable target,float damage,Vector3 point,Vector3 direction);
    }
    [DisallowMultipleComponent]
    public sealed class RRLShieldDamageFilter : MonoBehaviour,IRRLPreHpFilter
    {
        readonly List<RRLShieldBinding> bindings=new List<RRLShieldBinding>();
        DirectionalShieldReceiver receiver;
        void Awake()
        {
            receiver=GetComponent<DirectionalShieldReceiver>();
            if(receiver==null)receiver=gameObject.AddComponent<DirectionalShieldReceiver>();
            receiver.Bind(GetComponent<Rigidbody>());
        }
        public void Register(RRLShieldBinding binding)
        {if(!bindings.Contains(binding))bindings.Add(binding);receiver.Register(binding.Generator);}
        public void Unregister(RRLShieldBinding binding)
        {bindings.Remove(binding);if(receiver!=null&&binding.Generator!=null)receiver.Unregister(binding.Generator);}
        public float FilterDamage(P12PipelineSafeDamageable target,float damage,Vector3 point,Vector3 direction)
        {
            if(!isActiveAndEnabled||receiver==null)return damage;
            // Resolve current graph/HP/status now, not on the next Update after a bridge breaks.
            foreach(var binding in bindings)if(binding!=null)binding.RefreshOperational();
            return receiver.Mitigate(damage,point,direction,out _);
        }
    }
}
