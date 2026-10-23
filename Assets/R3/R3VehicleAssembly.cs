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
        public void Bind(R3VehicleModel model, P12PipelineSafeTankController tank) { Model = model; controller = tank; }
        public void Capture()
        {
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
            int alive = Parts.Count(p => p != null && p.Operational && p.transform.IsChildOf(transform));
            float original = Model.Design.Parts.Sum(R4WeaponDefinition.PartMass);
            float mass=Parts.Where(p=>p!=null&&p.Operational).Sum(p=>RoboRougelike.R6.R6VehicleDrive.PartMass(p.Damageable));
            float weightFactor = Mathf.Clamp(Mathf.Sqrt(original / Mathf.Max(1,mass)), 0.65f, 1.15f);
            float wheelBonus = Mathf.Clamp(1 + (Model.Parts.Count(p => p.Kind == R3PartKind.Wheel) -
                Model.Design.Parts.Count(p => p.Kind == R3PartKind.Wheel)) * 0.025f, 0.8f, 1.15f);
            controller.ConfigureChassis(Model.Design.Speed * weightFactor * wheelBonus, Model.Design.TurnSpeed,
                mass);
        }
        public void AimGuns(Vector3 point)
        {
            foreach(var part in Parts)
                if(part!=null && part.Kind==R3PartKind.Gun && part.Operational && part.GunPivot!=null)
                {
                    var direction = point - part.GunPivot.position;
                    if(direction.sqrMagnitude>0.001f) part.GunPivot.rotation = Quaternion.RotateTowards(
                        part.GunPivot.rotation,Quaternion.LookRotation(direction),170*Time.deltaTime);
                }
        }
        public void FireGuns()
        {
            foreach(var part in Parts)
                if(part!=null && part.Kind==R3PartKind.Gun && part.Operational && part.transform.IsChildOf(transform)&&Time.time>=part.NextFireTime)
                {
                    if(part.Weapon==R4WeaponKind.Shotgun)
                    {
                        var binding=part.GetComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
                        if(binding!=null&&binding.Fire())part.NextFireTime=binding.State.DeviceReadyAt;
                        continue;
                    }
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
                hp.Configure(state.Kind==R3PartKind.Core?model.Design.CoreHp:state.Kind==R3PartKind.Gun?R4WeaponDefinition.Get(state.Weapon).Hp:
                    state.Kind==R3PartKind.BonusModule?250:state.Kind==R3PartKind.DirectionalShield?180:state.Kind==R3PartKind.Wheel?80:state.Kind==R3PartKind.CrawlerTrack?140:100,true);
                var block=item.AddComponent<P12_PipelineSafePlayerBlock>();
                item.AddComponent<R3BlockMountPoints>().Configure(state.Id,
                    R3MountingGrid.WorldPortMask(state.RotationQuarterTurns,state.ContactMask));
                if(wide)R3BonusModuleGeometry.AddStructuralSockets(item.transform,state);
                if(state.Kind==R3PartKind.Core)core=block;
                var part=item.AddComponent<R3MountedPart>(); part.Cell=state.Cell;part.Kind=state.Kind;part.Damageable=hp;part.Block=block;
                part.Id=state.Id;part.Weapon=state.Weapon;
                part.UpgradeEffect=state.UpgradeEffect;part.UpgradeAmount=state.UpgradeAmount;
                assembly.Parts.Add(part);
                Transform payload=null;Transform partMuzzle=null;
                if(!wide)payload=R41PartGeometry.Payload(item.transform,state.Kind,state.Weapon,
                    state.Kind==R3PartKind.Gun?palette.Weapons[(int)state.Weapon]:palette.Parts[(int)state.Kind],true,out partMuzzle);
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
            var primary=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
            rig.localPosition=(Vector3)primary.Cell*0.55f+Vector3.up*0.31f;
            var pitch=new GameObject("Cannon_Pitch").transform;pitch.SetParent(rig,false);
            var muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(pitch,false);muzzle.localPosition=Vector3.forward*0.85f;
            var pivot=new GameObject("CameraPivot").transform;pivot.SetParent(root.transform,false);pivot.localPosition=Vector3.up*1.5f;
            var structure=root.AddComponent<P12PipelineSafeVehicleStructure>();structure.SetDesignatedCore(core);structure.RebuildNow();
            var modules=root.AddComponent<P12PipelineSafeVehicleModuleSystem>();
            modules.ConfigureExternalModules(wheels,weapons,model.Parts.Count(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x<0),model.Parts.Count(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x>0));
            var tank=root.AddComponent<P12PipelineSafeTankController>();
            assembly.Bind(model,tank);
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
            foreach(var part in assembly.Parts)
                if(part.Kind==R3PartKind.Gun&&part.Weapon==R4WeaponKind.Shotgun)
                    part.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>()
                        .Bind(model.Parts.First(p=>p.Id==part.Id),root.transform,part.Muzzle);
            if(assembly.Parts.Any(p=>p.Kind==R3PartKind.Gun&&p.Weapon==R4WeaponKind.Shotgun))
                root.AddComponent<RoboRougelike.WeaponIntegration.RRLWeaponControls>();
            foreach(var part in assembly.Parts)
                if(part.Kind==R3PartKind.DirectionalShield)
                    part.gameObject.AddComponent<RoboRougelike.WeaponIntegration.RRLShieldBinding>()
                        .Bind(model.Parts.First(p=>p.Id==part.Id),root.transform);
            return vehicle;
        }
    }
}
