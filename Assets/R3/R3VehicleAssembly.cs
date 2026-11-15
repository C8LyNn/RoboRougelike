using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRougelike.R1;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.R4;
using RoboRougelike.R4_1;

namespace RoboRougelike.R3
{
    public sealed class R3VehicleAssembly : MonoBehaviour
    {
        public readonly List<R3MountedPart> Parts = new List<R3MountedPart>();
        public R3VehicleModel Model { get; private set; }
        private P12PipelineSafeTankController controller;
        RaycastHit[] shotPathHits=new RaycastHit[32];
        Collider[] shotPathOverlaps=new Collider[32];
        int previousShotPathOverlapCount;
        const int MaxShotPathBuffer=8192;
        public void Bind(R3VehicleModel model, P12PipelineSafeTankController tank) { Model = model; controller = tank; R3BuildValidation.Bind(this,model); }
        public float PassiveDetectionRange(R3PartKind kind)
        {
            if(Model==null||(kind!=R3PartKind.RadarModule&&kind!=R3PartKind.MinimapModule))return 0;
            float perUnit=kind==R3PartKind.RadarModule?Model.Economy.RadarRangePerUnit:Model.Economy.MinimapRangePerUnit;
            if(float.IsNaN(perUnit)||float.IsInfinity(perUnit)||perUnit<=0)return 0;
            double range=0;
            foreach(var part in Parts)
                if(part!=null&&part.Kind==kind&&part.gameObject.activeInHierarchy&&part.Operational&&
                   part.transform.IsChildOf(transform)&&!RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject))
                    range+=(double)perUnit*R3VehicleModel.TierEffectUnits(part.Tier);
            return (float)System.Math.Min(float.MaxValue,range);
        }
        // Prototype Cargo Uplink affects only the reach of real physical pickups.
        public float CargoPickupRadius
        {
            get
            {
                double radius=R3PhysicalLoot.BasePickupRadius;
                float extra=Model?.Economy.CargoPickupRangePerUnit??0;
                if(float.IsNaN(extra)||float.IsInfinity(extra)||extra<=0)return (float)radius;
                foreach(var part in Parts)
                    if(part!=null&&part.Kind==R3PartKind.CargoUplink&&part.gameObject.activeInHierarchy&&part.Operational&&
                       part.transform.IsChildOf(transform)&&!RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject))
                        radius+=(double)extra*R3VehicleModel.TierEffectUnits(part.Tier);
                return (float)System.Math.Min(float.MaxValue,radius);
            }
        }
        public void Capture()
        {
            foreach(var emp in GetComponentsInChildren<RoboRougelike.WeaponIntegration.RRLEmpPartState>())emp.CaptureState();
            foreach(var emitter in GetComponentsInChildren<RoboRougelike.WeaponIntegration.RRLEmpBinding>())emitter.CaptureState();
            foreach(var saw in GetComponentsInChildren<RoboRougelike.WeaponIntegration.RRLSawBinding>())saw.CaptureState();
            foreach(var beam in GetComponentsInChildren<RoboRougelike.WeaponIntegration.RRLBeamBinding>())beam.CaptureState();
            foreach(var defense in GetComponentsInChildren<RoboRougelike.WeaponIntegration.RRLPointDefenseBinding>())defense.CaptureState();
            foreach(var shield in GetComponentsInChildren<RoboRougelike.WeaponIntegration.RRLShieldBinding>())shield.CaptureState();
            GetComponent<P12PipelineSafeVehicleStructure>().EvaluateStructure();
            GetComponent<P12PipelineSafeVehicleModuleSystem>().RefreshModules();
            foreach (var state in Model.Parts)
            {
                var part = Parts.FirstOrDefault(p => p != null && p.Cell == state.Cell);
                state.Health = part != null && part.Operational && part.transform.IsChildOf(transform)
                    ? Mathf.Clamp01(part.Damageable.CurrentHp / part.Damageable.MaxHp) : 0;
            }
        }
        public void TuneChassis()
        {
            if(!gameObject.activeInHierarchy)return;
            float mass=0,power=0;int wheels=0;
            foreach(var part in Parts)
            {
                if(part==null||!part.gameObject.activeInHierarchy||!part.Operational||!part.transform.IsChildOf(transform))continue;
                mass+=RoboRougelike.R6.R6VehicleDrive.PartMass(part.Damageable);
                if(part.Kind==R3PartKind.Wheel)wheels++;
                if(part.Kind==R3PartKind.DrivePower&&Model.SelectedDrive==R3DriveKind.Wheels&&
                   !RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject))
                    power+=R3VehicleModel.TierEffectUnits(part.Tier);
            }
            float steering=(float)System.Math.Min(float.MaxValue,TurnFor(mass)*(1d+.12d*power));
            controller.ConfigureChassis(SpeedFor(mass,wheels),steering,mass);
        }
        public static float SpeedFor(float mass,int wheels)
        {
            float weight=MassFactor(mass);
            // Wheels above the basic support threshold add real traction and
            // acceleration through R6; they do not grant free top speed.
            float support=Mathf.Clamp(Mathf.Sqrt(Mathf.Max(0,wheels)/4f),.7f,1f);
            return 7.5f*weight*support;
        }
        // Keep the denominator representable even for malformed or empty chassis.
        static float MassFactor(float mass)=>float.IsNaN(mass)||float.IsInfinity(mass)?0f:Mathf.Sqrt(350f/Mathf.Max(1f,mass));
        public static float TurnFor(float mass)=>82f*MassFactor(mass);
        public void AimGuns(Vector3 point)
        {
            foreach(var part in Parts)
                if(part!=null && part.Kind==R3PartKind.Gun && part.gameObject.activeInHierarchy && part.Operational &&
                   part.transform.IsChildOf(transform) && part.GunPivot!=null&&!RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject))
                {
                    // Aim from the actual firing point, not the turret pivot. The muzzle moves with the barrel, so this converges while the turret turns.
                    var direction = point - (part.Muzzle!=null?part.Muzzle.position:part.GunPivot.position);
                    if(direction.sqrMagnitude>0.001f) part.GunPivot.rotation = Quaternion.RotateTowards(
                        part.GunPivot.rotation,Quaternion.LookRotation(direction),140*Time.deltaTime);
                    part.GetComponent<RoboRougelike.R4_1.R4WeaponArt>()?.Sync();
                }
        }
        public void SetContinuousFireHeld(bool held)
        {
            foreach(var part in Parts)
                if(part!=null&&part.Weapon==R4WeaponKind.BeamCutter)
                    part.GetComponent<RoboRougelike.WeaponIntegration.RRLBeamBinding>()?.SetTriggerHeld(held);
            foreach(var part in Parts)
                if(part!=null&&part.Weapon==R4WeaponKind.RotarySaw)
                    part.GetComponent<RoboRougelike.WeaponIntegration.RRLSawBinding>()?.SetMotorRequested(held);
        }
        public void SetGroupTriggers(bool groupA,bool groupB)
            =>SetSlotTriggers(groupA,groupB,false,false);
        public void SetSlotTriggers(bool groupA,bool groupB,bool groupC,bool groupD)
        {
            var run=RoboRougelike.R1.R1RunController.Instance;
            if(run!=null&&run.Player!=null&&run.Player.gameObject==gameObject&&run.EmergencyRescueAttackBlocked)
                groupA=groupB=groupC=groupD=false;
            var pressed=new[]{groupA,groupB,groupC,groupD};var slots=Model.GetWeaponSlots();
            foreach(var part in Parts)
            {
                if(part==null||part.Kind!=R3PartKind.Gun)continue;
                bool rawHeld=false;
                for(int slot=0;slot<4;slot++)
                    if(pressed[slot]&&R3VehicleModel.MatchesWeaponSlot(part.Weapon,slot,slots,Model.CreativeMode)){rawHeld=true;break;}
                part.GetComponent<RoboRougelike.WeaponIntegration.RRLDamageAuraBinding>()?.SetTriggerHeld(rawHeld);
                bool held=false;
                if(part.Operational&&part.gameObject.activeInHierarchy&&part.transform.IsChildOf(transform)&&
                   !RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject))
                    for(int slot=0;slot<4;slot++)
                        if(pressed[slot]&&R3VehicleModel.MatchesWeaponSlot(part.Weapon,slot,slots,Model.CreativeMode)){held=true;break;}
                if(!held&&part.Weapon<=R4WeaponKind.Heavy)part.StopConventionalCadence();
                part.GetComponent<RoboRougelike.WeaponIntegration.RRLBeamBinding>()?.SetTriggerHeld(held);
                part.GetComponent<RoboRougelike.WeaponIntegration.RRLSawBinding>()?.SetMotorRequested(held);
            }
        }
        public void FireGuns() => FireGroup(false);

        RaycastHit[] ShotPathCast(Vector3 origin,float radius,Vector3 direction,float distance,out int count)
        {
            while(true)
            {
                count=Physics.SphereCastNonAlloc(origin,radius,direction,shotPathHits,distance,~0,QueryTriggerInteraction.Ignore);
                if(count<shotPathHits.Length)return shotPathHits;
                if(shotPathHits.Length>=MaxShotPathBuffer)
                {
                    var all=Physics.SphereCastAll(origin,radius,direction,distance,~0,QueryTriggerInteraction.Ignore);
                    count=all.Length;return all;
                }
                System.Array.Resize(ref shotPathHits,Mathf.Min(shotPathHits.Length*2,MaxShotPathBuffer));
            }
        }
        Collider[] ShotPathOverlap(Vector3 origin,float radius,out int count)
        {
            while(true)
            {
                count=Physics.OverlapSphereNonAlloc(origin,radius,shotPathOverlaps,~0,QueryTriggerInteraction.Ignore);
                if(count<shotPathOverlaps.Length)
                {
                    if(previousShotPathOverlapCount>count)
                        System.Array.Clear(shotPathOverlaps,count,previousShotPathOverlapCount-count);
                    previousShotPathOverlapCount=count;
                    return shotPathOverlaps;
                }
                if(shotPathOverlaps.Length>=MaxShotPathBuffer)
                {
                    System.Array.Clear(shotPathOverlaps,0,shotPathOverlaps.Length);
                    previousShotPathOverlapCount=0;
                    var all=Physics.OverlapSphere(origin,radius,~0,QueryTriggerInteraction.Ignore);
                    count=all.Length;return all;
                }
                System.Array.Resize(ref shotPathOverlaps,Mathf.Min(shotPathOverlaps.Length*2,MaxShotPathBuffer));
            }
        }
        static T FindShotComponent<T>(Transform current) where T:Component
        {
            while(current!=null)
            {
                if(current.TryGetComponent<T>(out var found))return found;
                current=current.parent;
            }
            return null;
        }

        // Projectiles intentionally ignore their owner after spawning. Check the
        // shot path first so that this does not allow firing through our armor.
        // Only the gun's own colliders are ignored; another gun is an obstacle.
        public bool ClearProjectilePath(R3MountedPart part)
        {
            if(part==null||part.Muzzle==null)return false;
            if(part.TryGetComponent<RoboRougelike.R4_1.R4WeaponArt>(out var art))art.Sync();
            float radius=Mathf.Max(.025f,RoboRougelike.WeaponIntegration.RRLAimFeedback.ProjectileRadius(part));
            float range=RoboRougelike.WeaponIntegration.RRLAimFeedback.Range(part,controller);
            return ClearProjectilePath(part,part.Muzzle.forward,range,radius);
        }
        public bool ClearProjectilePath(R3MountedPart part,Vector3 direction,float range,float radius)
        {
            if(!ValidShotDirection(direction,range)||!PrepareShotPath(part,radius,out var origin))return false;
            return ClearPreparedShotPath(part,origin,direction,range,radius);
        }
        // A bounded step makes it possible to spread directional checks over
        // several updates. The current firing path still completes them at once.
        // A caller yielding between steps must also handle changes to the world,
        // gun and owner before committing the shot.
        internal sealed class ShotgunVolleyPathCheck
        {
            readonly R3VehicleAssembly assembly;
            readonly R3MountedPart part;
            readonly IReadOnlyList<RoboRougelike.WPN001Shotgun.ShotgunPelletSpawn> pellets;
            readonly Vector3 origin;
            readonly float radius;
            readonly Transform muzzle,pivot;
            readonly Matrix4x4 ownerPose,muzzlePose,pivotPose;
            bool clear;
            public int CheckedCount { get; private set; }
            public bool Finished { get; private set; }
            public bool Clear
            {
                get { if(clear&&!PoseIsCurrent())clear=false;return clear; }
                private set { clear=value; }
            }
            internal ShotgunVolleyPathCheck(R3VehicleAssembly assembly,R3MountedPart part,
                IReadOnlyList<RoboRougelike.WPN001Shotgun.ShotgunPelletSpawn> pellets,float radius,Vector3 origin,bool ready)
            {
                this.assembly=assembly;this.part=part;this.pellets=pellets;this.radius=radius;this.origin=origin;Finished=!ready;
                if(ready)
                {
                    muzzle=part.Muzzle;pivot=part.GunPivot;
                    ownerPose=assembly.transform.localToWorldMatrix;muzzlePose=muzzle.localToWorldMatrix;pivotPose=pivot.localToWorldMatrix;
                }
            }
            public void Advance(int maxChecks)
            {
                if(maxChecks<=0)throw new System.ArgumentOutOfRangeException(nameof(maxChecks));
                if(Finished)return;
                if(!PoseIsCurrent())
                {Finished=true;return;}
                int end=(int)System.Math.Min(pellets.Count,(long)CheckedCount+maxChecks);
                while(CheckedCount<end)
                {
                    var pellet=pellets[CheckedCount++];
                    var velocity=pellet.Direction*pellet.ProjectileSpeed+pellet.InheritedVelocity;
                    float range=Mathf.Min(pellet.MaxDistance,velocity.magnitude*pellet.Lifetime);
                    if(!ValidShotDirection(velocity,range)||!assembly.ClearPreparedShotPath(part,origin,velocity,range,radius))
                    {Finished=true;return;}
                }
                if(CheckedCount==pellets.Count){Clear=true;Finished=true;}
            }
            bool PoseIsCurrent()=>assembly!=null&&part!=null&&muzzle!=null&&pivot!=null&&part.gameObject.activeInHierarchy&&
                part.transform.IsChildOf(assembly.transform)&&part.Muzzle==muzzle&&part.GunPivot==pivot&&
                ownerPose.Equals(assembly.transform.localToWorldMatrix)&&muzzlePose.Equals(muzzle.localToWorldMatrix)&&
                pivotPose.Equals(pivot.localToWorldMatrix);
        }
        internal ShotgunVolleyPathCheck BeginShotgunVolleyPathCheck(R3MountedPart part,
            IReadOnlyList<RoboRougelike.WPN001Shotgun.ShotgunPelletSpawn> pellets,float radius)
        {
            Vector3 origin=default;
            bool ready=pellets!=null&&pellets.Count>0&&PrepareShotPath(part,radius,out origin);
            return new ShotgunVolleyPathCheck(this,part,pellets,radius,origin,ready);
        }
        // All directions in a volley use the same muzzle and barrel. Keep the
        // per-pellet cast, but do the shared overlap/barrel checks only once.
        public bool ClearShotgunVolley(R3MountedPart part,IReadOnlyList<RoboRougelike.WPN001Shotgun.ShotgunPelletSpawn> pellets,float radius)
        {
            var check=BeginShotgunVolleyPathCheck(part,pellets,radius);
            check.Advance(int.MaxValue);
            return check.Clear;
        }
        static bool ValidShotDirection(Vector3 direction,float range)=>direction.sqrMagnitude>=.000001f&&
            range>0&&!float.IsNaN(range)&&!float.IsInfinity(range);
        bool PrepareShotPath(R3MountedPart part,float radius,out Vector3 origin)
        {
            origin=default;
            if(part==null||part.Muzzle==null||radius<=0||float.IsNaN(radius)||float.IsInfinity(radius))return false;
            if(part.TryGetComponent<RoboRougelike.R4_1.R4WeaponArt>(out var art))art.Sync();
            origin=part.Muzzle.position;
            bool IsGun(Collider c)=>c!=null&&c.transform.IsChildOf(part.transform);
            bool IsOwner(Collider c)=>c!=null&&c.transform.IsChildOf(transform);
            // The barrel itself can rotate into another part even if its tip
            // is already beyond that part. Inspect the whole muzzle segment.
            if(part.GunPivot==null)return false;
            var barrel=origin-part.GunPivot.position;
            if(barrel.sqrMagnitude>0.000001f)
            {
                var barrelHits=ShotPathCast(part.GunPivot.position,radius,barrel.normalized,barrel.magnitude,out var barrelCount);
                for(int i=0;i<barrelCount;i++)if(IsOwner(barrelHits[i].collider)&&!IsGun(barrelHits[i].collider))return false;
            }
            var overlaps=ShotPathOverlap(origin,radius,out var overlapCount);
            for(int i=0;i<overlapCount;i++)if(IsOwner(overlaps[i])&&!IsGun(overlaps[i]))return false;
            return true;
        }
        bool ClearPreparedShotPath(R3MountedPart part,Vector3 origin,Vector3 direction,float range,float radius)
        {
            bool IsGun(Collider c)=>c!=null&&c.transform.IsChildOf(part.transform);
            bool IsOwner(Collider c)=>c!=null&&c.transform.IsChildOf(transform);
            bool IsOwnProjectile(Collider c)
            {
                var shell=FindShotComponent<P17PhysicalShell>(c.transform);
                if(shell!=null&&shell.OwnerRoot==transform)return true;
                var rocket=FindShotComponent<RoboRougelike.Mechanics.Rockets.RocketProjectile>(c.transform);
                return rocket!=null&&rocket.OwnerRoot==transform;
            }
            float own=float.PositiveInfinity,other=float.PositiveInfinity;
            var hits=ShotPathCast(origin,radius,direction.normalized,range,out var hitCount);
            for(int i=0;i<hitCount;i++)
            {
                var hit=hits[i];
                if(hit.collider==null||IsGun(hit.collider)||IsOwnProjectile(hit.collider))continue;
                if(IsOwner(hit.collider))own=Mathf.Min(own,hit.distance);
                else other=Mathf.Min(other,hit.distance);
            }
            return own>other || float.IsPositiveInfinity(own);
        }
        public void FireGroup(bool secondary)
            =>FireSlot(secondary?1:0);
        struct PendingConventionalShot
        {
            public Transform Muzzle;
            public R4WeaponKind Kind;
            public string PartId;
            public Collider Primary;
            public RoboRougelike.WeaponIntegration.RRLRailgunBinding Rail;
            public RoboRougelike.WeaponIntegration.RRLShotgunBinding Shotgun;
            public System.Numerics.BigInteger Requested;
            public System.Numerics.BigInteger CadenceShots;
            public DoubleShotRoll CadenceRoll;
            public float CadenceCost;
        }
        struct PendingRocketShot
        {
            public RoboRougelike.WeaponIntegration.RRLRocketBinding Binding;
            public System.Numerics.BigInteger Requested;
        }
        internal readonly struct DoubleShotRoll
        {
            readonly System.Numerics.BigInteger active;
            readonly double chance;
            public DoubleShotRoll(System.Numerics.BigInteger activeUnits,double passiveChance){active=activeUnits;chance=passiveChance;}
            public int Roll(int available)
            {
                if(available<=0)return 0;
                int extra=(int)System.Numerics.BigInteger.Min(available,active);available-=extra;
                if(available<=0||chance<=0)return extra;
                double whole=System.Math.Floor(chance);
                int guaranteed=(int)System.Math.Min(available,whole);
                double fractional=chance-whole;
                if(guaranteed<available&&fractional>0&&UnityEngine.Random.value<fractional)guaranteed++;
                return extra+guaranteed;
            }
            public System.Numerics.BigInteger RollExact()
            {
                if(chance<=0)return active;
                double whole=System.Math.Floor(chance);
                var extra=active+new System.Numerics.BigInteger(whole);
                double fractional=chance-whole;
                if(fractional>0&&UnityEngine.Random.value<fractional)extra+=System.Numerics.BigInteger.One;
                return extra;
            }
        }
        internal DoubleShotRoll CaptureDoubleShotRoll()
        {
            if(Model==null)return default;
            var active=GetComponent<R3OffensiveDevices>()?.AdditionalProjectileUnitsExact??System.Numerics.BigInteger.Zero;
            float configured=Model.Economy.DoubleShotChancePerUnit;
            if(float.IsNaN(configured)||float.IsInfinity(configured)||configured<=0)return new DoubleShotRoll(active,0);
            long units=0;
            foreach(var part in Parts)
                if(part!=null&&part.Kind==R3PartKind.DoubleShot&&part.Operational&&part.gameObject.activeInHierarchy&&
                   part.transform.IsChildOf(transform)&&!RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject))
                    units+=R3VehicleModel.TierEffectUnits(part.Tier);
            return new DoubleShotRoll(active,(double)configured*units);
        }
        public int RollDoubleShotExtras(int available)=>available<=0?0:CaptureDoubleShotRoll().Roll(available);
        internal System.Numerics.BigInteger RollDoubleShotExtrasExact()=>CaptureDoubleShotRoll().RollExact();
        public void FireSlot(int slot)
        {
            var run=RoboRougelike.R1.R1RunController.Instance;
            if(run!=null&&run.Player!=null&&run.Player.gameObject==gameObject&&run.EmergencyRescueAttackBlocked)return;
            var slots=Model.GetWeaponSlots();if(slot<0||slot>=4||!slots[slot].HasValue)return;
            List<PendingConventionalShot> pending=null;
            List<PendingRocketShot> pendingRockets=null;
            foreach(var part in Parts)
                if(part!=null && part.Kind==R3PartKind.Gun && R3VehicleModel.MatchesWeaponSlot(part.Weapon,slot,slots,Model.CreativeMode) && part.Operational && part.gameObject.activeInHierarchy && part.transform.IsChildOf(transform)&&Time.time>=part.NextFireTime)
                {
                    part.GetComponent<RoboRougelike.R4_1.R4WeaponArt>()?.Sync();
                    if(RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject))continue;
                    if(part.Weapon==R4WeaponKind.EmpEmitter)continue; // toggled by the slot's press edge
                    if(part.Weapon==R4WeaponKind.BeamCutter||part.Weapon==R4WeaponKind.RotarySaw)continue;
                    if((part.Weapon<=R4WeaponKind.Railgun||part.Weapon==R4WeaponKind.RocketLauncher)
                       &&!ClearProjectilePath(part)){if(part.Weapon<=R4WeaponKind.Heavy)part.StopConventionalCadence();continue;}
                    if(part.Weapon==R4WeaponKind.Shotgun)
                    {
                        var binding=part.GetComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
                        if(binding!=null&&binding.FirePrimary())
                        {
                            part.NextFireTime=binding.State.DeviceReadyAt;
                            if(binding.HasAdditionalRequested)
                            {
                                if(pending==null)pending=new List<PendingConventionalShot>();
                                pending.Add(new PendingConventionalShot{Shotgun=binding});
                            }
                        }
                        continue;
                    }
                    if(part.Weapon==R4WeaponKind.RocketLauncher)
                    {
                        var rocket=part.GetComponent<RoboRougelike.WeaponIntegration.RRLRocketBinding>();
                        if(rocket!=null&&rocket.FirePrimary())
                        {
                            part.NextFireTime=rocket.State.DeviceReadyAt;
                            var requested=RollDoubleShotExtrasExact();
                            if(requested>0)
                            {
                                if(pendingRockets==null)pendingRockets=new List<PendingRocketShot>();
                                pendingRockets.Add(new PendingRocketShot{Binding=rocket,Requested=requested});
                            }
                        }
                        continue;
                    }
                    if(part.Weapon==R4WeaponKind.Railgun)
                    {
                        var rail=part.GetComponent<RoboRougelike.WeaponIntegration.RRLRailgunBinding>();
                        if(rail!=null&&rail.FirePrimary())
                        {
                            part.NextFireTime=rail.State.DeviceReadyAt;
                            var requested=RollDoubleShotExtrasExact();
                            if(requested>0)
                            {
                                if(pending==null)pending=new List<PendingConventionalShot>();
                                pending.Add(new PendingConventionalShot{Rail=rail,Requested=requested});
                            }
                        }
                        continue;
                    }
                    float interval=controller.WeaponInterval(part.Weapon);
                    if(Time.time+interval<=Time.time&&part.LastConventionalShotTime==Time.time)continue;
                    var cadence=part.Weapon<=R4WeaponKind.Heavy?part.ConventionalCadence:null;
                    if(cadence!=null)
                    {
                        if(float.IsNaN(interval)||float.IsInfinity(interval)||interval<0){part.StopConventionalCadence();continue;}
                        cadence.Observe(Time.time,interval,true);
                        if(cadence.PendingShots.IsZero)continue;
                    }
                    var energy=GetComponent<VehicleEnergyBank>();
                    float shotCost=VehicleEnergyCosts.ConventionalShot(part.Weapon)*R3VehicleModel.TierActivationCost(part.Tier);
                    if(energy!=null&&!energy.TrySpend(shotCost)){part.StopConventionalCadence();continue;}
                    if(cadence==null)part.NextFireTime=Time.time+interval;
                    part.LastConventionalShotTime=Time.time;
                    var firedMuzzle=part.Muzzle;
                    var primary=controller.FirePrimaryFrom(firedMuzzle,part.Weapon,part.Id);
                    if(primary!=null)
                    {
                        var roll=CaptureDoubleShotRoll();var requested=roll.RollExact();
                        var due=cadence!=null?cadence.TakeAll():System.Numerics.BigInteger.One;
                        if(cadence!=null)part.NextFireTime=cadence.NextFireTime;
                        var additionalPrimaries=due-System.Numerics.BigInteger.One;
                        if(requested>0||additionalPrimaries>0)
                        {
                            if(pending==null)pending=new List<PendingConventionalShot>();
                            pending.Add(new PendingConventionalShot{Muzzle=firedMuzzle,Kind=part.Weapon,PartId=part.Id,Primary=primary,Requested=requested,CadenceShots=additionalPrimaries,CadenceRoll=roll,CadenceCost=shotCost});
                        }
                    }
                    var art=part.GetComponent<RoboRougelike.R4_1.R4WeaponArt>();
                    if(art!=null){art.AdvanceMuzzle();part.Muzzle=art.Muzzle;}
                }
            if(pendingRockets!=null)
                foreach(var shot in pendingRockets)shot.Binding.FireAdditionalExact(shot.Requested);
            if(pending==null)return;
            foreach(var shot in pending)
                if(shot.Shotgun!=null)shot.Shotgun.FireAdditional();
                else if(shot.Rail!=null)shot.Rail.FireAdditionalExact(shot.Requested);
                else
                {
                    var energy=GetComponent<VehicleEnergyBank>();
                    var cadenceShots=shot.CadenceShots;
                    if(energy!=null&&cadenceShots>0)cadenceShots=energy.SpendUpTo(shot.CadenceCost,cadenceShots);
                    if(cadenceShots>0&&shot.Primary!=null)
                    {
                        var queue=GetComponent<R3ConventionalProjectileQueue>();
                        if(queue==null)queue=gameObject.AddComponent<R3ConventionalProjectileQueue>();
                        queue.EnqueueCadence(shot.Primary.GetComponent<P17PhysicalShell>(),cadenceShots,shot.Requested,shot.CadenceRoll);
                    }
                    else controller.FireAdditionalExact(shot.Muzzle,shot.Kind,shot.PartId,shot.Primary,shot.Requested);
                }
        }

        public static R1VehicleState Build(R3VehicleModel model, R1RunStats stats, R3VisualPalette palette,
            Vector3 position, Quaternion rotation)
        {
            var root=new GameObject("Pojazd " + model.Design.Name); root.SetActive(false);
            root.transform.SetPositionAndRotation(position,rotation);
            var assembly=root.AddComponent<R3VehicleAssembly>();
            var wheels=new List<P12PipelineSafeFunctionalModule>(); var weapons=new List<P12PipelineSafeFunctionalModule>();
            P12_PipelineSafePlayerBlock core=null;
            foreach(var state in model.Parts.Where(p=>p.Health>0))
            {
                var item=R41PartGeometry.CreateMountingBlock(state.Kind==R3PartKind.Armor?state.ArmorShape:R3ArmorShape.Cube);
                item.name=state.Kind+" "+state.Cell; item.transform.SetParent(root.transform,false);
                item.transform.localPosition=(Vector3)state.Cell*0.55f;
                item.transform.localRotation=R3MountingGrid.Rotation(state.RotationQuarterTurns);
                item.transform.localScale=Vector3.one*0.52f;
                if(state.Kind==R3PartKind.Wheel)R41PartGeometry.ConfigureWheelMount(item,state.WheelStyle);
                bool bonus=state.Kind==R3PartKind.BonusModule;
                bool wide=R3VehicleModel.UsesWideFootprint(state.Kind);
                item.GetComponent<Renderer>().sharedMaterial=state.Kind==R3PartKind.Gun?palette.Weapons[(int)state.Weapon]:
                    palette.Parts[bonus?(int)R3PartKind.Gyroscope:(int)state.Kind];
                if(wide)R3BonusModuleGeometry.ConfigureCube(item,true);
                var hp=item.AddComponent<P12PipelineSafeDamageable>();
                float baseHp=state.Kind==R3PartKind.Core?model.Design.CoreHp:state.Kind==R3PartKind.Gun?R4WeaponDefinition.Get(state.Weapon).Hp:
                    state.Kind==R3PartKind.BonusModule?250:state.Kind==R3PartKind.DirectionalShield?180:state.Kind==R3PartKind.PointDefense?160:state.Kind==R3PartKind.Wheel?80:state.Kind==R3PartKind.CrawlerTrack?140:100;
                hp.Configure(baseHp*R3VehicleModel.TierHealth(state.Tier),true);
                var block=item.AddComponent<P12_PipelineSafePlayerBlock>();
                var mountPoints=item.AddComponent<R3BlockMountPoints>();mountPoints.Configure(state.Id,
                    R3MountingGrid.WorldPortMask(state.RotationQuarterTurns,state.ContactMask));
                if(state.FootprintRevision!=0)
                {
                    var offsets=R3VehicleModel.OccupiedCells(state).Select(cell=>cell-state.Cell).ToArray();
                    mountPoints.SetFootprint(offsets);
                    int axleMask=R3MountingGrid.WorldPortMask(state.RotationQuarterTurns,state.ContactMask);
                    mountPoints.SetCellPortMasks(offsets.ToDictionary(offset=>offset,offset=>offset==Vector3Int.zero?axleMask:0));
                }
                if(state.Kind==R3PartKind.Armor&&state.ArmorShape!=R3ArmorShape.Cube&&
                    R4MountDefinitionAdapter.TryCreateCellPortMasks(R41PartGeometry.ArmorCatalogId(state.ArmorShape),
                        true,state.RotationQuarterTurns,out var cellPortMasks))
                    mountPoints.SetCellPortMasks(cellPortMasks);
                if(wide)R3BonusModuleGeometry.AddStructuralSockets(item.transform,state);
                if(state.Kind==R3PartKind.Core)core=block;
                var part=item.AddComponent<R3MountedPart>(); part.Cell=state.Cell;part.Kind=state.Kind;part.Tier=R3VehicleModel.NormalizedTier(state.Tier);part.Damageable=hp;part.Block=block;
                part.Id=state.Id;part.Weapon=state.Weapon;part.WheelStyle=state.WheelStyle;
                part.UpgradeEffect=state.UpgradeEffect;part.UpgradeOperation=state.UpgradeOperation;part.UpgradeScope=state.UpgradeScope;part.UpgradeAmount=state.UpgradeAmount;
                assembly.Parts.Add(part);
                if(state.Kind==R3PartKind.Armor&&!wide)R41PartGeometry.TryAttachArmor(item.transform,state.ArmorShape);
                Transform payload=null;Transform partMuzzle=null;
                bool wheelMirror=state.Kind==R3PartKind.Wheel&&R4WheelArt.MirrorForMount(model,state);
                if(!wide)payload=R41PartGeometry.Payload(item.transform,state.Kind,state.Weapon,
                    state.Kind==R3PartKind.Gun?palette.Weapons[(int)state.Weapon]:palette.Parts[(int)state.Kind],true,out partMuzzle,state.WheelStyle,wheelMirror);
                if(state.Kind==R3PartKind.Gun&&state.FootprintRevision==2)
                    R41PartGeometry.ConfigureGunScale2(item,payload,state.Weapon);
                if(state.Kind==R3PartKind.Wheel&&payload!=null)
                    R4WheelArt.TryAttach(item.transform,payload,wheelMirror,state.WheelStyle);
                if(state.Kind==R3PartKind.Booster&&!wide)
                    RoboRougelike.R4_1.R4BoosterArt.TryAttach(item.transform);
                if(state.Kind==R3PartKind.Gyroscope&&!wide)
                    RoboRougelike.R4_1.R4GyroArt.TryAttach(item.transform);
                if(state.Kind==R3PartKind.Suspension&&!wide)
                    RoboRougelike.R4_1.R4SuspensionArt.TryAttach(item.transform);
                if(state.Kind==R3PartKind.JumpDrive&&!wide)
                    RoboRougelike.R4_1.R4JumpArt.TryAttach(item.transform);
                if(state.Kind==R3PartKind.Blink&&!wide)
                    RoboRougelike.R4_1.R4BlinkArt.TryAttach(item.transform);
                if(state.Kind==R3PartKind.MagneticGrapple&&!wide)
                    RoboRougelike.R4_1.R4GrappleArt.TryAttach(item.transform);
                if(state.Kind==R3PartKind.HoverEngine&&!wide)
                    RoboRougelike.R4_1.R4HoverArt.TryAttach(item.transform);
                if(state.Kind==R3PartKind.OmniDrive&&!wide)
                    RoboRougelike.R4_1.R4OmniArt.TryAttach(item.transform,state.Cell.x>0);
                if(state.Kind==R3PartKind.CrawlerTrack&&!wide)
                    RoboRougelike.R4_1.R4TracksArt.TryAttach(item.transform,state.Cell.x>0);
                if(state.Kind==R3PartKind.IndependentSteering&&!wide)
                    RoboRougelike.R4_1.R4IndependentSteeringArt.TryAttach(item.transform);
                if(state.Kind==R3PartKind.Gun&&payload!=null)
                {
                    var art=RoboRougelike.R4_1.R4WeaponArt.TryAttach(item.transform,payload,partMuzzle,state.Weapon,state.FootprintRevision==2);
                    if(art!=null&&state.Weapon!=R4WeaponKind.RotarySaw)partMuzzle=art.Muzzle;
                }
                if(state.Kind==R3PartKind.Gun){part.GunPivot=payload;part.Muzzle=partMuzzle;}
                if(payload!=null&&(state.Kind==R3PartKind.Wheel||state.Kind==R3PartKind.Gun))
                {
                    var module=item.AddComponent<P12PipelineSafeFunctionalModule>();
                    module.Configure(state.Kind==R3PartKind.Wheel?P12FunctionalModuleType.Wheel:P12FunctionalModuleType.Weapon,
                        state.Cell.x<0?-1:1,block,payload,root.transform);
                    if(state.Kind==R3PartKind.Wheel) wheels.Add(module);else weapons.Add(module);
                }
            }
            var rig=new GameObject("Turret_Yaw").transform;rig.SetParent(root.transform,false);
            var primary=model.Parts.FirstOrDefault(p=>p.Kind==R3PartKind.Gun);
            // A garage vehicle may have no guns while its owner is rebuilding.
            rig.localPosition=primary!=null?(Vector3)primary.Cell*0.55f+Vector3.up*0.31f:Vector3.up*0.31f;
            var pitch=new GameObject("Cannon_Pitch").transform;pitch.SetParent(rig,false);
            var muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(pitch,false);muzzle.localPosition=Vector3.forward*0.85f;
            var pivot=new GameObject("CameraPivot").transform;pivot.SetParent(root.transform,false);pivot.localPosition=Vector3.up*1.5f;
            var structure=root.AddComponent<P12PipelineSafeVehicleStructure>();structure.SetDesignatedCore(core);structure.RebuildNow();
            var modules=root.AddComponent<P12PipelineSafeVehicleModuleSystem>();
            modules.ConfigureExternalModules(wheels,weapons,model.Parts.Count(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x<0),model.Parts.Count(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x>0));
            var tank=root.AddComponent<P12PipelineSafeTankController>();
            assembly.Bind(model,tank);
            root.AddComponent<VehicleEnergyBank>().Configure(assembly);
            var vehicle=root.AddComponent<R1VehicleState>();vehicle.Bind(tank,pivot);
            root.SetActive(true);tank.SetGameplayInputEnabled(false);
            vehicle.Apply(stats);
            foreach(var part in assembly.Parts)part.Damageable.RestoreHealthRatio(model.At(part.Cell).Health);
            assembly.TuneChassis();modules.RefreshModules();
            if(model.Parts.Any(p=>p.Kind==R3PartKind.Suspension))
                root.AddComponent<RoboRougelike.MOV009Integration.MOV009VehicleAdapter>().Configure(assembly);
            if(model.Parts.Any(p=>p.Kind>=R3PartKind.Booster&&p.Kind<=R3PartKind.CrawlerTrack))
                root.AddComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>().Configure(assembly);
            root.AddComponent<RoboRougelike.MOVSeries.MOVSeriesInput>().Bind(root.GetComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>());
            root.AddComponent<R3MountedBonusEffects>().Configure(assembly,vehicle,stats);
            if(assembly.Parts.Any(p=>p.Kind==R3PartKind.RepairPulse))
                root.AddComponent<R3RepairPulse>().Configure(assembly);
            if(assembly.Parts.Any(p=>p.Kind==R3PartKind.Overdrive||p.Kind==R3PartKind.RapidFire||p.Kind==R3PartKind.DoubleShotOverdrive))
                root.AddComponent<R3OffensiveDevices>().Configure(assembly);
            if(assembly.Parts.Any(p=>p.Kind==R3PartKind.TargetedEmp||p.Kind==R3PartKind.AreaEmp))
                root.AddComponent<R3EmpDevices>().Configure(assembly);
            if(assembly.Parts.Any(p=>p.Kind==R3PartKind.RechargePulse))
                root.AddComponent<R3RechargePulse>().Configure(assembly);
            if(assembly.Parts.Any(p=>R3VehicleModel.IsDeployableSlotPart(p.Kind)))
                root.AddComponent<R3DeployableService>().Configure(assembly);
            foreach(var part in assembly.Parts)
                if(part.Kind==R3PartKind.Gun&&part.Weapon==R4WeaponKind.Shotgun)
                    part.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>()
                        .Bind(model.Parts.First(p=>p.Id==part.Id),root.transform,part.Muzzle);
            foreach(var part in assembly.Parts)
                if(part.Kind==R3PartKind.Gun&&part.Weapon==R4WeaponKind.Railgun)
                    part.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLRailgunBinding>()
                        .Bind(model.Parts.First(p=>p.Id==part.Id),root.transform,part.Muzzle);
            foreach(var part in assembly.Parts)
                if(part.Kind==R3PartKind.Gun&&part.Weapon==R4WeaponKind.BeamCutter)
                    part.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLBeamBinding>()
                        .Bind(model.Parts.First(p=>p.Id==part.Id),root.transform,part.Muzzle);
            foreach(var part in assembly.Parts)
                if(part.Kind==R3PartKind.Gun&&part.Weapon==R4WeaponKind.RocketLauncher)
                    part.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLRocketBinding>()
                        .Bind(model.Parts.First(p=>p.Id==part.Id),root.transform,part.Muzzle);
            foreach(var part in assembly.Parts)
                if(part.Kind==R3PartKind.Gun&&part.Weapon==R4WeaponKind.RotarySaw)
                    part.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLSawBinding>()
                        .Bind(model.Parts.First(p=>p.Id==part.Id),root.transform,part.Muzzle);
            if(assembly.Parts.Any(p=>p.Kind==R3PartKind.Gun&&p.Weapon>=R4WeaponKind.Shotgun))
                root.AddComponent<RoboRougelike.WeaponIntegration.RRLWeaponControls>();
            foreach(var part in assembly.Parts)
                if(part.Kind==R3PartKind.DirectionalShield)
                    part.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLShieldBinding>()
                        .Bind(model.Parts.First(p=>p.Id==part.Id),root.transform);
            foreach(var part in assembly.Parts)
                if(part.Kind==R3PartKind.PointDefense)
                    part.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLPointDefenseBinding>()
                        .Bind(model.Parts.First(p=>p.Id==part.Id),root.transform);
            if(root.GetComponent<RoboRougelike.WeaponIntegration.RRLWeaponControls>()==null&&assembly.Parts.Any(p=>p.Kind==R3PartKind.PointDefense))
                root.AddComponent<RoboRougelike.WeaponIntegration.RRLWeaponControls>();
            foreach(var part in assembly.Parts)
                if(part.Kind==R3PartKind.Gun&&part.Weapon==R4WeaponKind.EmpEmitter)
                    part.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLDamageAuraBinding>().Bind(model.Parts.First(p=>p.Id==part.Id),root.transform);
            foreach(var part in assembly.Parts)
                if(part.Kind==R3PartKind.StructuralScanner)
                    part.gameObject.AddComponent<RoboRougelike.PackageIntegration.RRLStructuralScannerBinding>()
                        .Bind(model.Parts.First(p=>p.Id==part.Id),root.transform);
            foreach(var part in assembly.Parts)
                if(part.Kind==R3PartKind.ScannerPulse)
                    part.gameObject.AddComponent<RoboRougelike.PackageIntegration.RRLPulseScannerBinding>()
                        .Bind(model.Parts.First(p=>p.Id==part.Id),root.transform);
            if(assembly.Parts.Any(p=>p.Kind==R3PartKind.StructuralScanner||p.Kind==R3PartKind.ScannerPulse))root.AddComponent<RoboRougelike.PackageIntegration.RRLScannerControls>();
            foreach(var part in assembly.Parts)
                if(part.Kind==R3PartKind.IndependentSteering)
                    part.gameObject.AddComponent<RoboRougelike.PackageIntegration.RRLIndependentSteeringBinding>()
                        .Bind(model.Parts.First(p=>p.Id==part.Id),root.transform);
            RoboRougelike.WeaponIntegration.RRLEmpVehicleAdapter.Configure(root.transform);
            root.AddComponent<RoboRougelike.R6.R6CoreRecovery>().Configure(assembly);
            return vehicle;
        }
    }
}
