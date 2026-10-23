using UnityEngine;
using RoboRougelike.Mechanics.PointDefense;
using RoboRougelike.Mechanics.Rockets;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLPointDefenseThreat : MonoBehaviour,IPointDefenseThreat
    {
        RocketProjectile rocket;P17PhysicalShell shell;
        void Awake()
        {
            rocket=GetComponent<RocketProjectile>();shell=GetComponent<P17PhysicalShell>();
            var proxy=GetComponent<PointDefenseThreatProxy>();if(proxy==null)proxy=gameObject.AddComponent<PointDefenseThreatProxy>();proxy.Bind(this);
        }
        public int FactionId=>RRLHitRouter.Team(rocket!=null?rocket.OwnerRoot:shell!=null?shell.OwnerRoot:null);
        public bool IsActiveThreat=>isActiveAndEnabled&&(rocket!=null?(rocket.IsInitialized&&!rocket.IsResolved):shell!=null&&shell.IsActiveProjectile);
        public Transform AimTransform=>transform;
        public Vector3 Velocity=>rocket!=null?rocket.FlightVelocity:shell!=null?shell.FlightVelocity:Vector3.zero;
        public bool TryIntercept(float damage)
        {
            if(!IsActiveThreat||damage<=0||float.IsNaN(damage)||float.IsInfinity(damage))return false;
            if(rocket!=null){rocket.ApplyInterceptionDamage(damage);return rocket.IsResolved;}
            return shell!=null&&shell.Intercept(damage);
        }
    }
}
