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
        public void Bind(R3VehicleModel model, P12PipelineSafeTankController tank) { Model = model; controller = tank; R3BuildValidation.Bind(this,model); }
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
            var active=Parts.Where(p=>p!=null&&p.Operational&&p.transform.IsChildOf(transform)).ToArray();
            float mass=active.Sum(p=>RoboRougelike.R6.R6VehicleDrive.PartMass(p.Damageable));
            int wheels=active.Count(p=>p.Kind==R3PartKind.Wheel);
            float power=ModelWheelPower(active);
            controller.ConfigureChassis(SpeedFor(mass,wheels),Mathf.Min(130f,TurnFor(mass)*Mathf.Min(1.6f,1f+.12f*power)),mass);
        }
        float ModelWheelPower(R3MountedPart[] active)=>Model.SelectedDrive!=R3DriveKind.Wheels?0:
            active.Where(p=>p.Kind==R3PartKind.DrivePower&&!RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(p.gameObject))
                .Sum(p=>Mathf.Clamp01(p.Damageable.CurrentHp/Mathf.Max(1,p.Damageable.MaxHp))*R3VehicleModel.TierEffectUnits(p.Tier));
        public static float SpeedFor(float mass,int wheels)
        {
            float weight=Mathf.Clamp(Mathf.Sqrt(350f/Mathf.Max(1f,mass)),.6f,1.35f);
            float support=Mathf.Clamp(Mathf.Sqrt(Mathf.Max(0,wheels)/4f),.7f,1.3f);
            return Mathf.Clamp(7.5f*weight*support,2f,14f);
        }
        public static float TurnFor(float mass)=>Mathf.Clamp(82f*Mathf.Clamp(Mathf.Sqrt(350f/Mathf.Max(1f,mass)),.6f,1.35f),35f,130f);        public void AimGuns(Vector3 point)
        {
            foreach(var part in Parts)
                if(part!=null && part.Kind==R3PartKind.Gun && part.Operational && part.GunPivot!=null&&!RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject))
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
        {
            var a=Model.WeaponGroupA;var b=Model.WeaponGroupB;
            foreach(var part in Parts)
            {
                if(part==null||part.Kind!=R3PartKind.Gun)continue;
                bool held=part.Operational&&part.gameObject.activeInHierarchy&&part.transform.IsChildOf(transform)&&
                    !RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject)&&
                    ((groupA&&a==part.Weapon)||(groupB&&b==part.Weapon));
                part.GetComponent<RoboRougelike.WeaponIntegration.RRLBeamBinding>()?.SetTriggerHeld(held);
                part.GetComponent<RoboRougelike.WeaponIntegration.RRLSawBinding>()?.SetMotorRequested(held);
            }
        }
        public void FireGuns() => FireGroup(false);

        // Projectiles intentionally ignore their owner after spawning. Check the
        // shot path first so that this does not allow firing through our armor.
        // Only the gun's own colliders are ignored; another gun is an obstacle.
        public bool ClearProjectilePath(R3MountedPart part)
        {
            if(part==null||part.Muzzle==null)return false;
            part.GetComponent<RoboRougelike.R4_1.R4WeaponArt>()?.Sync();
            float radius=Mathf.Max(.025f,RoboRougelike.WeaponIntegration.RRLAimFeedback.ProjectileRadius(part));
            float range=RoboRougelike.WeaponIntegration.RRLAimFeedback.Range(part,controller);
            return ClearProjectilePath(part,part.Muzzle.forward,range,radius);
        }
        public bool ClearProjectilePath(R3MountedPart part,Vector3 direction,float range,float radius)
        {
            if(part==null||part.Muzzle==null||direction.sqrMagnitude<.000001f||
               range<=0||float.IsNaN(range)||float.IsInfinity(range)||
               radius<=0||float.IsNaN(radius)||float.IsInfinity(radius))return false;
            part.GetComponent<RoboRougelike.R4_1.R4WeaponArt>()?.Sync();
            var origin=part.Muzzle.position;
            bool IsGun(Collider c)=>c!=null&&c.transform.IsChildOf(part.transform);
            bool IsOwner(Collider c)=>c!=null&&c.transform.IsChildOf(transform);
            // The barrel itself can rotate into another part even if its tip
            // is already beyond that part. Inspect the whole muzzle segment.
            if(part.GunPivot==null)return false;
            var barrel=origin-part.GunPivot.position;
            if(barrel.sqrMagnitude>0.000001f)
                foreach(var hit in Physics.SphereCastAll(part.GunPivot.position,radius,
                    barrel.normalized,barrel.magnitude,~0,QueryTriggerInteraction.Ignore))
                    if(IsOwner(hit.collider)&&!IsGun(hit.collider))return false;
            foreach(var c in Physics.OverlapSphere(origin,radius,~0,QueryTriggerInteraction.Ignore))
                if(IsOwner(c)&&!IsGun(c))return false;
            float own=float.PositiveInfinity,other=float.PositiveInfinity;
            foreach(var hit in Physics.SphereCastAll(origin,radius,direction.normalized,range,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.collider==null||IsGun(hit.collider))continue;
                if(IsOwner(hit.collider))own=Mathf.Min(own,hit.distance);
                else other=Mathf.Min(other,hit.distance);
            }
            return own>other || float.IsPositiveInfinity(own);
        }
        public void FireGroup(bool secondary)
        {
            var kind=secondary?Model.WeaponGroupB:Model.WeaponGroupA;
            if(!kind.HasValue)return;
            foreach(var part in Parts)
                if(part!=null && part.Kind==R3PartKind.Gun && part.Weapon==kind.Value && part.Operational && part.gameObject.activeInHierarchy && part.transform.IsChildOf(transform)&&Time.time>=part.NextFireTime)
                {
                    part.GetComponent<RoboRougelike.R4_1.R4WeaponArt>()?.Sync();
                    if(RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject))continue;
                    if(part.Weapon==R4WeaponKind.EmpEmitter)
                    {
                        var emp=part.GetComponent<RoboRougelike.WeaponIntegration.RRLEmpBinding>();
                        if(emp!=null&&emp.Fire())part.NextFireTime=emp.State.DeviceReadyAt;continue;
                    }
                    if(part.Weapon==R4WeaponKind.BeamCutter||part.Weapon==R4WeaponKind.RotarySaw)continue;
                    if((part.Weapon<=R4WeaponKind.Railgun||part.Weapon==R4WeaponKind.RocketLauncher)
                       &&!ClearProjectilePath(part))continue;
                    if(part.Weapon==R4WeaponKind.Shotgun)
                    {
                        var binding=part.GetComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
                        if(binding!=null&&binding.Fire())part.NextFireTime=binding.State.DeviceReadyAt;
                        continue;
                    }
                    if(part.Weapon==R4WeaponKind.RocketLauncher)
                    {
                        var rocket=part.GetComponent<RoboRougelike.WeaponIntegration.RRLRocketBinding>();
                        if(rocket!=null&&rocket.Fire())part.NextFireTime=rocket.State.DeviceReadyAt;
                        continue;
                    }
                    if(part.Weapon==R4WeaponKind.Railgun)
                    {
                        var rail=part.GetComponent<RoboRougelike.WeaponIntegration.RRLRailgunBinding>();
                        if(rail!=null&&rail.Fire())part.NextFireTime=rail.State.DeviceReadyAt;
                        continue;
                    }
                    var energy=GetComponent<VehicleEnergyBank>();
                    if(energy!=null&&!energy.TrySpend(VehicleEnergyCosts.ConventionalShot(part.Weapon)*R3VehicleModel.TierActivationCost(part.Tier)))continue;
                    part.NextFireTime=Time.time+controller.WeaponInterval(part.Weapon);
                    controller.FireFrom(part.Muzzle,part.Weapon,part.Id);
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
                var item=GameObject.CreatePrimitive(PrimitiveType.Cube);
                item.name=state.Kind+" "+state.Cell; item.transform.SetParent(root.transform,false);
                item.transform.localPosition=(Vector3)state.Cell*0.55f;
                item.transform.localRotation=R3MountingGrid.Rotation(state.RotationQuarterTurns);
                item.transform.localScale=Vector3.one*0.52f;
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
                item.AddComponent<R3BlockMountPoints>().Configure(state.Id,
                    R3MountingGrid.WorldPortMask(state.RotationQuarterTurns,state.ContactMask));
                if(wide)R3BonusModuleGeometry.AddStructuralSockets(item.transform,state);
                if(state.Kind==R3PartKind.Core)core=block;
                var part=item.AddComponent<R3MountedPart>(); part.Cell=state.Cell;part.Kind=state.Kind;part.Tier=R3VehicleModel.NormalizedTier(state.Tier);part.Damageable=hp;part.Block=block;
                part.Id=state.Id;part.Weapon=state.Weapon;
                part.UpgradeEffect=state.UpgradeEffect;part.UpgradeAmount=state.UpgradeAmount;
                assembly.Parts.Add(part);
                if(state.Kind==R3PartKind.Armor&&!wide)RoboRougelike.R4_1.R41PartGeometry.TryAttachArmorCube(item.transform);
                Transform payload=null;Transform partMuzzle=null;
                if(!wide)payload=R41PartGeometry.Payload(item.transform,state.Kind,state.Weapon,
                    state.Kind==R3PartKind.Gun?palette.Weapons[(int)state.Weapon]:palette.Parts[(int)state.Kind],true,out partMuzzle);
                if(state.Kind==R3PartKind.Wheel&&payload!=null)
                    RoboRougelike.R4_1.R4WheelArt.TryAttach(item.transform,payload,state.Cell.x<0);
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
                    var art=RoboRougelike.R4_1.R4WeaponArt.TryAttach(item.transform,payload,partMuzzle,state.Weapon);
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
            root.AddComponent<R3MountedBonusEffects>().Configure(assembly,vehicle,stats);
            if(assembly.Parts.Any(p=>p.Kind==R3PartKind.RepairPulse))
                root.AddComponent<R3RepairPulse>().Configure(assembly);
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
                    part.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLEmpBinding>().Bind(model.Parts.First(p=>p.Id==part.Id),root.transform);
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
