using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.Mechanics.Rockets;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLRocketBinding : MonoBehaviour,IRRLDeviceLifecycle,IRocketAmmoSource
    {
        public void RefreshAvailability(){if(Module!=null)Module.SetOperational(Device.Available&&State.ReloadReadyAt<=Time.time);}
        public const int Magazine=10;
        public RRLDeviceBinding Device {get;private set;}
        public R3PartState State=>Device.State;
        public RocketLauncher Module {get;private set;}
        P12PipelineSafeTankController tank;
        Transform muzzle;
        RocketProjectile projectileTemplate;
        float damageScale=1,intervalScale=1;
        public void Bind(R3PartState state,Transform owner,Transform barrel,float damageMultiplier=1,float intervalMultiplier=1)
        {
            Device=GetComponent<RRLDeviceBinding>();if(Device==null)Device=gameObject.AddComponent<RRLDeviceBinding>();Device.Bind(state,owner);
            tank=owner.GetComponent<P12PipelineSafeTankController>();
            muzzle=barrel;damageScale=damageMultiplier;intervalScale=intervalMultiplier;if(state.Ammo<0)state.Ammo=Magazine;
            var template=new GameObject("WPN004 inactive projectile template");template.SetActive(false);template.transform.SetParent(transform,false);
            var rb=template.AddComponent<Rigidbody>();rb.useGravity=false;rb.isKinematic=true;template.AddComponent<SphereCollider>().isTrigger=true;
            var prefab=template.AddComponent<RocketProjectile>();projectileTemplate=prefab;template.AddComponent<RRLPointDefenseThreat>();
            var visual=GameObject.CreatePrimitive(PrimitiveType.Capsule);visual.name="Rocket visual";visual.transform.SetParent(template.transform,false);
            visual.transform.localRotation=Quaternion.Euler(90,0,0);visual.transform.localScale=new Vector3(.16f,.36f,.16f);
            var col=visual.GetComponent<Collider>();col.enabled=false;Destroy(col);
            var material=GetComponent<Renderer>().sharedMaterial;visual.GetComponent<Renderer>().sharedMaterial=material;
            for(int i=0;i<4;i++)
            {
                var fin=GameObject.CreatePrimitive(PrimitiveType.Cube);fin.name="Rocket fin "+i;fin.transform.SetParent(template.transform,false);
                float angle=i*90f;fin.transform.localRotation=Quaternion.Euler(0,0,angle);
                fin.transform.localPosition=Quaternion.Euler(0,0,angle)*new Vector3(.1f,0,-.21f);
                fin.transform.localScale=new Vector3(.14f,.025f,.22f);fin.GetComponent<Renderer>().sharedMaterial=material;
                var finCollider=fin.GetComponent<Collider>();finCollider.enabled=false;Destroy(finCollider);
            }
            var origin=template.AddComponent<R4ProjectileOrigin>();origin.Weapon=R4WeaponKind.RocketLauncher;origin.PartId=state.Id;
            Module=gameObject.AddComponent<RocketLauncher>();Module.Bind(Device.Body,owner,muzzle);Module.SetAmmoSource(this);Module.SetPrefab(prefab);
            Device.Damageable.Died+=Stop;Refresh();
        }
        void Refresh()
        {
            if(State.ReloadReadyAt>0&&Time.time>=State.ReloadReadyAt){State.Ammo=Magazine;State.ReloadReadyAt=0;}
            Module.SetOperational(Device.Available&&State.ReloadReadyAt<=Time.time);
        }
        public bool Fire()
        {
            Refresh();if(!Device.Available||Time.time<State.DeviceReadyAt)return false;
            var assembly=Device.Owner.GetComponent<R3VehicleAssembly>();
            if(assembly!=null&&!assembly.ClearProjectilePath(GetComponent<R3MountedPart>()))return false;
            int live=0;foreach(var rocket in Object.FindObjectsByType<RocketProjectile>(FindObjectsSortMode.None))if(rocket.IsInitialized&&!rocket.IsResolved)live++;
            if(live>=32)return false;
            // The package fails closed on an incomplete physics query. The
            // integrated game admits large multi-collider vehicles, so size
            // both bounded buffers for that workload without weakening it.
            Module.SetConfig(new RocketConfig{damageAtCenter=60*(tank!=null?tank.PackageDamageScale:damageScale)*R3VehicleModel.TierEffectUnits(State.Tier),cooldown=1.7f*(tank!=null?tank.PackageIntervalScale:intervalScale),maxBlastColliders=1024,maxCastHits=512});
            var energy=Device.Owner.GetComponent<VehicleEnergyBank>();
            float cost=VehicleEnergyCosts.Rocket*R3VehicleModel.TierActivationCost(State.Tier);
            if(energy!=null&&!energy.TrySpend(cost))return false;
            if(Module.TryFire()!=RocketFireResult.Fired){if(energy!=null)energy.Refund(cost);return false;}
            State.DeviceReadyAt=Module.NextReadyTime;
            int extras=assembly!=null?assembly.RollDoubleShotExtras(Mathf.Max(0,31-live)):0;
            for(int i=0;i<extras;i++)
            {
                RocketProjectile extra=null;
                try
                {
                    extra=Object.Instantiate(projectileTemplate,muzzle.position,muzzle.rotation);
                    if(!extra.TryInitialize(Module.CurrentSpec,Device.Body,Device.Owner,gameObject,null,muzzle.forward))
                    {Object.Destroy(extra.gameObject);continue;}
                    if(!extra.gameObject.activeSelf)extra.gameObject.SetActive(true);
                }
                catch(System.Exception problem)
                {if(extra!=null)Object.Destroy(extra.gameObject);Debug.LogException(problem,this);}
            }
            RoboRougelike.R6.R6CombatFeedback.Muzzle(muzzle,R4WeaponKind.RocketLauncher);return true;
        }
        public bool BeginReload(){Refresh();if(!Device.Available||State.Ammo>=Magazine||State.ReloadReadyAt>0)return false;State.ReloadReadyAt=Time.time+2.2f;return true;}
        public bool TrySpendAmmo(int units)
        {
            if(!Device.Available||units<0||State.Ammo<units||Time.time<State.DeviceReadyAt||State.ReloadReadyAt>Time.time)return false;
            State.Ammo-=units;return true;
        }
        public void RefundAmmo(int units){if(units>0)State.Ammo=Mathf.Min(Magazine,State.Ammo+units);}
        void Update(){Refresh();if(Device.Available&&State.Ammo==0)BeginReload();}
        void Stop(){if(Module!=null)Module.SetOperational(false);}
        void OnDisable()=>Stop();
        void OnDestroy(){if(Device?.Damageable!=null)Device.Damageable.Died-=Stop;}
    }
}
