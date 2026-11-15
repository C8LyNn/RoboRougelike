using System.Collections.Generic;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.WPN001Shotgun;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLShotgunBinding : MonoBehaviour,IRRLDeviceLifecycle,IShotgunVolleySink,IShotgunShotResourceBank
    {
        public void RefreshAvailability(){if(Module!=null)Module.SetOperational(Available&&State.ReloadReadyAt<=Time.time);}
        public const int MagazineSalvos=12;
        public const float ReloadSeconds=1.5f;
        public R3PartState State { get; private set; }
        public ShotgunModule Module { get; private set; }
        public Transform Owner { get; private set; }
        Transform muzzle;
        Rigidbody body;
        P12PipelineSafeDamageable hp;
        Material projectileMaterial;
        P12PipelineSafeTankController tank;
        P12PipelineSafeEnemyTurret enemy;
        bool debitPending;
        bool deferExtras;
        List<ShotgunPelletSpawn> deferredPellets;
        List<int> deferredRequests;
        List<GameObject> deferredCreated;
        internal int AdditionalRequested { get; private set; }
        float damageScale=1,intervalScale=1;
        RRLDeviceBinding device;
        public bool Available => device!=null&&device.Available;
        public void Bind(R3PartState state,Transform owner,Transform barrel,float damageMultiplier=1,float intervalMultiplier=1)
        {
            State=state;Owner=owner;muzzle=barrel;body=owner.GetComponent<Rigidbody>();
            hp=GetComponent<P12PipelineSafeDamageable>();
            tank=owner.GetComponent<P12PipelineSafeTankController>();enemy=owner.GetComponent<P12PipelineSafeEnemyTurret>();
            device=GetComponent<RRLDeviceBinding>();if(device==null)device=gameObject.AddComponent<RRLDeviceBinding>();
            device.Bind(state,owner);
            damageScale=Mathf.Max(0,damageMultiplier);
            intervalScale=float.IsNaN(intervalMultiplier)||float.IsInfinity(intervalMultiplier)?1f:Mathf.Max(0,intervalMultiplier);
            if(State.Ammo<0)State.Ammo=MagazineSalvos;
            Module=GetComponent<ShotgunModule>();if(Module==null)Module=gameObject.AddComponent<ShotgunModule>();
            Module.Bind(body,muzzle,owner.gameObject);Module.BindSink(this);Module.BindResourceBank(this);
            Module.Configure(ShotgunSettings(intervalScale));
            Module.SetPatternSeed(StableSeed(State.Id));Module.SetOperational(Available);
            if(hp!=null)hp.Died+=Stop;
            projectileMaterial=GetComponent<Renderer>()?.sharedMaterial;
        }
        static uint StableSeed(string id)
        {
            uint seed=2166136261;foreach(char c in id??""){seed^=c;seed*=16777619;}return seed;
        }
        ShotgunConfig ShotgunSettings(float interval)
        {
            float range=Owner!=null?Owner.GetComponent<R3VehicleAssembly>()?.Model?.Economy.ShotgunRange??40f:40f;
            double safeRange=float.IsNaN(range)||float.IsInfinity(range)?40d:System.Math.Max(.1d,range);
            return new ShotgunConfig{cooldownSeconds=R3MountedBonusEffects.SafeIntervalProduct(.85f,interval),maxTravelDistance=range,
                projectileLifetime=(float)System.Math.Min(float.MaxValue,System.Math.Max(2d,safeRange/58d+1d))};
        }
        public bool Fire()
        {
            RefreshResources();
            if(!Available||Time.time<State.DeviceReadyAt||State.ReloadReadyAt>Time.time)return false;
            var assembly=Owner.GetComponent<R3VehicleAssembly>();
            if(assembly!=null&&!assembly.ClearProjectilePath(GetComponent<R3MountedPart>()))return false;
            Module.SetOperational(true);
            Module.Configure(ShotgunSettings(tank!=null?tank.WeaponIntervalScale(R4WeaponKind.Shotgun):intervalScale));
            var energy=Owner.GetComponent<VehicleEnergyBank>();
            float cost=VehicleEnergyCosts.Shotgun*R3VehicleModel.TierActivationCost(State.Tier);
            if(energy!=null&&!energy.TrySpend(cost))return false;
            bool result=Module.TryFire();
            if(!result&&energy!=null)energy.Refund(cost);
            if(result)State.DeviceReadyAt=Module.NextReadyTime;
            return result;
        }
        internal bool FirePrimary()
        {ClearDeferred();deferExtras=true;try{return Fire();}finally{deferExtras=false;if(AdditionalRequested==0)ClearDeferred();}}
        public bool BeginReload()
        {
            RefreshResources();
            if(!Available||State.Ammo>=MagazineSalvos||State.ReloadReadyAt>0)return false;
            State.ReloadReadyAt=Time.time+ReloadSeconds;return true;
        }
        void RefreshResources()
        {
            if(State!=null&&State.ReloadReadyAt>0&&Time.time>=State.ReloadReadyAt)
            {State.Ammo=MagazineSalvos;State.ReloadReadyAt=0;}
        }
        void Update()
        {
            RefreshResources();if(Module!=null)Module.SetOperational(Available&&State.ReloadReadyAt<=Time.time);
            if(enemy!=null&&Available&&State.Ammo==0)BeginReload();
        }
        public bool TryConsumeShot()
        {
            RefreshResources();
            if(!Available||State.ReloadReadyAt>Time.time||State.Ammo<=0||Time.time<State.DeviceReadyAt)return false;
            State.Ammo--;debitPending=true;return true;
        }
        public void RefundShot()
        {if(debitPending&&State!=null){State.Ammo=Mathf.Min(MagazineSalvos,State.Ammo+1);debitPending=false;}}
        public bool TrySpawnVolley(IReadOnlyList<ShotgunPelletSpawn> pellets)
        {
            if(!Available||pellets==null||pellets.Count==0)return false;
            var assembly=Owner.GetComponent<R3VehicleAssembly>();
            var part=GetComponent<R3MountedPart>();
            if(assembly!=null)
                foreach(var pellet in pellets)
                {
                    var velocity=pellet.Direction*pellet.ProjectileSpeed+pellet.InheritedVelocity;
                    var range=Mathf.Min(pellet.MaxDistance,velocity.magnitude*pellet.Lifetime);
                    if(!assembly.ClearProjectilePath(part,velocity,range,.05f))return false;
                }
            var planned=new List<ShotgunPelletSpawn>(pellets);
            int extraSlots=Mathf.Max(0,P17PhysicalShell.AvailableSlots-pellets.Count);
            if(assembly!=null&&!deferExtras)
                foreach(var pellet in pellets)
                {
                    int extras=assembly.RollDoubleShotExtras(extraSlots);
                    for(int i=0;i<extras;i++)planned.Add(pellet);
                    extraSlots-=extras;
                    if(extraSlots<=0)break;
                }
            var created=new List<GameObject>(planned.Count);
            try
            {
                foreach(var pellet in planned)CreatePellet(pellet,created);
                if(deferExtras)
                {
                    deferredPellets=new List<ShotgunPelletSpawn>(pellets);deferredRequests=new List<int>(pellets.Count);
                    deferredCreated=created;AdditionalRequested=0;int available=P17PhysicalShell.AvailableSlots;
                    foreach(var pellet in pellets)
                    {
                        int requested=assembly!=null?assembly.RollDoubleShotExtras(available):0;
                        deferredRequests.Add(requested);AdditionalRequested=(int)System.Math.Min(available,(long)AdditionalRequested+requested);
                    }
                }
                debitPending=false;
                RoboRougelike.R6.R6CombatFeedback.Muzzle(muzzle,R4WeaponKind.Shotgun);
                return true;
            }
            catch
            {
                foreach(var shell in created)if(shell!=null){shell.SetActive(false);Destroy(shell);}
                return false;
            }
        }
        internal void FireAdditional(int count)
        {
            if(!Available||deferredPellets==null||deferredRequests==null||deferredCreated==null){ClearDeferred();return;}
            int remaining=Mathf.Min(Mathf.Min(Mathf.Max(0,count),AdditionalRequested),P17PhysicalShell.AvailableSlots);
            int initial=deferredCreated.Count;var allocated=new int[deferredPellets.Count];
            try
            {
                while(remaining>0)
                {
                    bool created=false;
                    for(int i=0;i<allocated.Length&&remaining>0;i++)
                        if(allocated[i]<deferredRequests[i]){CreatePellet(deferredPellets[i],deferredCreated);allocated[i]++;remaining--;created=true;}
                    if(!created)break;
                }
            }
            catch{for(int i=initial;i<deferredCreated.Count;i++)if(deferredCreated[i]!=null){deferredCreated[i].SetActive(false);Destroy(deferredCreated[i]);}}
            ClearDeferred();
        }
        internal void ClearDeferred(){deferredPellets=null;deferredRequests=null;deferredCreated=null;AdditionalRequested=0;}
        void CreatePellet(ShotgunPelletSpawn pellet,List<GameObject> created)
        {
            if(pellet.OwnerRoot!=Owner.gameObject||pellet.OwnerBody!=body)throw new System.InvalidOperationException("Wrong projectile owner");
            var projectile=P17PhysicalShell.RentConventional();var shell=projectile.gameObject;created.Add(shell);
            shell.name="WPN001 physical pellet";shell.transform.SetPositionAndRotation(pellet.Origin,Quaternion.LookRotation(pellet.Direction));
            shell.transform.localScale=Vector3.one*.1f;
            var pelletCollider=shell.GetComponent<Collider>();
            pelletCollider.enabled=true;pelletCollider.isTrigger=false;
            var renderer=shell.GetComponent<Renderer>();
            if(renderer!=null)renderer.sharedMaterial=projectileMaterial!=null?projectileMaterial:projectile.DefaultMaterial;
            var origin=shell.GetComponent<R4ProjectileOrigin>();origin.Weapon=R4WeaponKind.Shotgun;origin.PartId=State.Id;
            var rb=shell.GetComponent<Rigidbody>();rb.mass=.2f;rb.useGravity=false;rb.isKinematic=false;
            rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            shell.SetActive(true);rb.angularVelocity=Vector3.zero;
            for(int i=0;i<created.Count-1;i++)if(created[i]!=null)
                P17PhysicalShell.IgnoreProjectilePair(pelletCollider,created[i].GetComponent<Collider>());
            rb.linearVelocity=pellet.Direction*pellet.ProjectileSpeed+pellet.InheritedVelocity;
            float pelletDamage=R3MountedBonusEffects.SafeDamageProduct(pellet.Damage,tank!=null?tank.PackageShotgunDamageScale:damageScale);
            pelletDamage=R3MountedBonusEffects.SafeDamageProduct(pelletDamage,R3VehicleModel.TierEffectUnits(State.Tier));
            projectile.Configure(Owner,pelletDamage,pellet.Lifetime,pellet.Direction,null,pellet.MaxDistance);
        }
        void Stop(){if(Module!=null)Module.SetOperational(false);}
        void OnDisable()=>Stop();
        void OnDestroy(){if(hp!=null)hp.Died-=Stop;}
    }
}
