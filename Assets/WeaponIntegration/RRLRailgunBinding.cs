using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.WPN002Railgun;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLRailgunBinding : MonoBehaviour,IRRLDeviceLifecycle,IRailgunShotSink,IRailgunShotResourceBank
    {
        public void RefreshAvailability(){if(Module!=null)Module.SetOperational(Device.Available&&State.ReloadReadyAt<=Time.time);}
        public const int Magazine=12;
        public RRLDeviceBinding Device {get;private set;}
        public R3PartState State=>Device.State;
        public RailgunModule Module {get;private set;}
        Transform muzzle;
        P12PipelineSafeTankController tank;
        P12PipelineSafeEnemyTurret enemy;
        float damageScale=1,intervalScale=1;
        bool debitPending;
        public void Bind(R3PartState state,Transform owner,Transform barrel,float damageMultiplier=1,float intervalMultiplier=1)
        {
            Device=GetComponent<RRLDeviceBinding>();if(Device==null)Device=gameObject.AddComponent<RRLDeviceBinding>();Device.Bind(state,owner);
            muzzle=barrel;tank=owner.GetComponent<P12PipelineSafeTankController>();enemy=owner.GetComponent<P12PipelineSafeEnemyTurret>();
            damageScale=damageMultiplier;intervalScale=intervalMultiplier;if(state.Ammo<0)state.Ammo=Magazine;
            Module=gameObject.AddComponent<RailgunModule>();Module.Bind(Device.Body,muzzle,owner.gameObject);
            Module.BindSink(this);Module.BindResourceBank(this);Module.SetOperational(Device.Available);
            Device.Damageable.Died+=Stop;
        }
        void Refresh()
        {
            if(State.ReloadReadyAt>0&&Time.time>=State.ReloadReadyAt){State.Ammo=Magazine;State.ReloadReadyAt=0;}
            Module.SetOperational(Device.Available&&State.ReloadReadyAt<=Time.time);
            Module.Configure(new RailgunConfig{cooldownSeconds=2.4f*(tank!=null?tank.PackageIntervalScale:intervalScale)});
        }
        public bool Fire(){Refresh();if(!Device.Available||Time.time<State.DeviceReadyAt)return false;return Module.TryFire();}
        public bool BeginReload()
        {Refresh();if(!Device.Available||State.Ammo>=Magazine||State.ReloadReadyAt>0)return false;State.ReloadReadyAt=Time.time+2;return true;}
        void Update(){Refresh();if(enemy!=null&&Device.Available&&State.Ammo==0)BeginReload();}
        public bool TryConsumeShot()
        {
            if(!Device.Available||State.ReloadReadyAt>Time.time||Time.time<State.DeviceReadyAt||State.Ammo<=0)return false;
            State.Ammo--;debitPending=true;return true;
        }
        public void RefundShot(){if(debitPending){State.Ammo=Mathf.Min(Magazine,State.Ammo+1);debitPending=false;}}
        public bool TrySpawnShot(RailgunShot shot)
        {
            if(!Device.Available||shot.OwnerBody!=Device.Body||shot.OwnerRoot!=Device.Owner.gameObject||P17PhysicalShell.AvailableSlots<1)return false;
            GameObject shell=null;
            try
            {
                shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.name="WPN002 physical rail";
                shell.transform.SetPositionAndRotation(shot.Origin,Quaternion.LookRotation(shot.Direction));shell.transform.localScale=Vector3.one*shot.Spec.Radius*2;
                shell.GetComponent<Collider>().isTrigger=true;
                shell.GetComponent<Renderer>().sharedMaterial=GetComponent<Renderer>().sharedMaterial;
                var rb=shell.AddComponent<Rigidbody>();rb.useGravity=false;rb.isKinematic=true;rb.interpolation=RigidbodyInterpolation.Interpolate;
                var origin=shell.AddComponent<R4ProjectileOrigin>();origin.Weapon=R4WeaponKind.Railgun;origin.PartId=State.Id;
                var projectile=shell.AddComponent<P17PhysicalShell>();projectile.Configure(Device.Owner,0,30,shot.Direction,null,shot.Spec.Range);
                projectile.ConfigureRailgun(shot,tank!=null?tank.PackageDamageScale:damageScale);
                State.DeviceReadyAt=Time.time+shot.Spec.Cooldown;debitPending=false;
                RoboRougelike.R6.R6CombatFeedback.Muzzle(muzzle,R4WeaponKind.Railgun);return true;
            }
            catch{if(shell!=null){shell.SetActive(false);Destroy(shell);}return false;}
        }
        void Stop(){if(Module!=null)Module.SetOperational(false);}
        void OnDisable()=>Stop();
        void OnDestroy(){if(Device?.Damageable!=null)Device.Damageable.Died-=Stop;}
    }
}
