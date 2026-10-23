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
        P12PipelineSafeEnemyTurret enemy;
        Transform muzzle;
        float damageScale=1,intervalScale=1;
        public void Bind(R3PartState state,Transform owner,Transform barrel,float damageMultiplier=1,float intervalMultiplier=1)
        {
            Device=GetComponent<RRLDeviceBinding>();if(Device==null)Device=gameObject.AddComponent<RRLDeviceBinding>();Device.Bind(state,owner);
            tank=owner.GetComponent<P12PipelineSafeTankController>();enemy=owner.GetComponent<P12PipelineSafeEnemyTurret>();
            muzzle=barrel;damageScale=damageMultiplier;intervalScale=intervalMultiplier;if(state.Ammo<0)state.Ammo=Magazine;
            var template=new GameObject("WPN004 inactive projectile template");template.SetActive(false);template.transform.SetParent(transform,false);
            var rb=template.AddComponent<Rigidbody>();rb.useGravity=false;rb.isKinematic=true;template.AddComponent<SphereCollider>().isTrigger=true;
            var prefab=template.AddComponent<RocketProjectile>();template.AddComponent<RRLPointDefenseThreat>();
            var visual=GameObject.CreatePrimitive(PrimitiveType.Sphere);visual.name="Rocket visual";visual.transform.SetParent(template.transform,false);visual.transform.localScale=Vector3.one*.24f;
            var col=visual.GetComponent<Collider>();col.enabled=false;Destroy(col);
            visual.GetComponent<Renderer>().sharedMaterial=GetComponent<Renderer>().sharedMaterial;
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
            int live=0;foreach(var rocket in Object.FindObjectsByType<RocketProjectile>(FindObjectsSortMode.None))if(rocket.IsInitialized&&!rocket.IsResolved)live++;
            if(live>=32)return false;
            Module.SetConfig(new RocketConfig{damageAtCenter=60*(tank!=null?tank.PackageDamageScale:damageScale),cooldown=1.7f*(tank!=null?tank.PackageIntervalScale:intervalScale)});
            if(Module.TryFire()!=RocketFireResult.Fired)return false;
            State.DeviceReadyAt=Module.NextReadyTime;
            RoboRougelike.R6.R6CombatFeedback.Muzzle(muzzle,R4WeaponKind.RocketLauncher);return true;
        }
        public bool BeginReload(){Refresh();if(!Device.Available||State.Ammo>=Magazine||State.ReloadReadyAt>0)return false;State.ReloadReadyAt=Time.time+2.2f;return true;}
        public bool TrySpendAmmo(int units)
        {
            if(!Device.Available||units<0||State.Ammo<units||Time.time<State.DeviceReadyAt||State.ReloadReadyAt>Time.time)return false;
            State.Ammo-=units;return true;
        }
        public void RefundAmmo(int units){if(units>0)State.Ammo=Mathf.Min(Magazine,State.Ammo+units);}
        void Update(){Refresh();if(enemy!=null&&Device.Available&&State.Ammo==0)BeginReload();}
        void Stop(){if(Module!=null)Module.SetOperational(false);}
        void OnDisable()=>Stop();
        void OnDestroy(){if(Device?.Damageable!=null)Device.Damageable.Died-=Stop;}
    }
}
