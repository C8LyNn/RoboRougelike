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
            damageScale=Mathf.Max(0,damageMultiplier);intervalScale=Mathf.Max(.01f,intervalMultiplier);
            if(State.Ammo<0)State.Ammo=MagazineSalvos;
            Module=GetComponent<ShotgunModule>();if(Module==null)Module=gameObject.AddComponent<ShotgunModule>();
            Module.Bind(body,muzzle,owner.gameObject);Module.BindSink(this);Module.BindResourceBank(this);
            Module.Configure(new ShotgunConfig{cooldownSeconds=.85f*intervalScale});
            Module.SetPatternSeed(StableSeed(State.Id));Module.SetOperational(Available);
            if(hp!=null)hp.Died+=Stop;
            projectileMaterial=GetComponent<Renderer>()?.sharedMaterial;
        }
        static uint StableSeed(string id)
        {
            uint seed=2166136261;foreach(char c in id??""){seed^=c;seed*=16777619;}return seed;
        }
        public bool Fire()
        {
            RefreshResources();
            if(!Available||Time.time<State.DeviceReadyAt||State.ReloadReadyAt>Time.time)return false;
            Module.SetOperational(true);
            Module.Configure(new ShotgunConfig{cooldownSeconds=.85f*(tank!=null?tank.PackageIntervalScale:intervalScale)});
            bool result=Module.TryFire();
            if(result)State.DeviceReadyAt=Module.NextReadyTime;
            return result;
        }
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
            if(!Available||pellets==null||pellets.Count==0||P17PhysicalShell.AvailableSlots<pellets.Count)return false;
            var created=new List<GameObject>(pellets.Count);
            try
            {
                foreach(var pellet in pellets)
                {
                    if(pellet.OwnerRoot!=Owner.gameObject||pellet.OwnerBody!=body)throw new System.InvalidOperationException("Wrong projectile owner");
                    var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);created.Add(shell);
                    shell.name="WPN001 physical pellet";shell.transform.SetPositionAndRotation(pellet.Origin,Quaternion.LookRotation(pellet.Direction));
                    shell.transform.localScale=Vector3.one*.1f;
                    // A volley shares one muzzle position. Its pellets must not consume
                    // each other as zero-distance collisions before reaching the world.
                    var pelletCollider=shell.GetComponent<Collider>();
                    for(int i=0;i<created.Count-1;i++)
                        Physics.IgnoreCollision(pelletCollider,created[i].GetComponent<Collider>(),true);
                    if(projectileMaterial!=null)shell.GetComponent<Renderer>().sharedMaterial=projectileMaterial;
                    var origin=shell.AddComponent<R4ProjectileOrigin>();origin.Weapon=R4WeaponKind.Shotgun;origin.PartId=State.Id;
                    var rb=shell.AddComponent<Rigidbody>();rb.mass=.2f;rb.useGravity=false;
                    rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                    rb.linearVelocity=pellet.Direction*pellet.ProjectileSpeed+pellet.InheritedVelocity;
                    shell.AddComponent<P17PhysicalShell>().Configure(Owner,pellet.Damage*(tank!=null?tank.PackageDamageScale:damageScale),pellet.Lifetime,pellet.Direction,null,pellet.MaxDistance);
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
        void Stop(){if(Module!=null)Module.SetOperational(false);}
        void OnDisable()=>Stop();
        void OnDestroy(){if(hp!=null)hp.Died-=Stop;}
    }
}
