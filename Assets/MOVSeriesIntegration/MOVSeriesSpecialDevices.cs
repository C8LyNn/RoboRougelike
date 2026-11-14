using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.Mechanics.MOV003Blink;
using RoboRougelike.Mechanics.MOV006MagneticGrapple;

namespace RoboRougelike.MOVSeries
{
    public sealed partial class MOVSeriesVehicleAdapter
    {
        readonly List<BlinkDrive> blinks=new List<BlinkDrive>();
        readonly List<MagneticGrappleEmitter> emitters=new List<MagneticGrappleEmitter>();
        MagneticGrappleController grapple;
        float grappleSpring,grappleDamping,grappleTension,grappleVehicleAcceleration,grappleTargetAcceleration;
        P12PipelineSafeVehicleStructure grappleTargetGraph;
        P12PipelineSafeDamageable[] grappleTargetParts;
        bool grappleTargetDirty;
        public bool GrappleAttached=>grapple!=null&&grapple.IsAttached;
        public Vector3 GrappleAimOrigin=>emitters.FirstOrDefault(e=>e!=null&&e.IsOperational)?.Muzzle.position??body.worldCenterOfMass;
        void ConfigureSpecialDevices()
        {
            if(Model.Parts.Any(p=>p.Kind==R3PartKind.MagneticGrapple))
            {
                grapple=gameObject.AddComponent<MagneticGrappleController>();grapple.BindVehicle(body);
                grapple.Settings.useEnergy=true;grapple.Settings.activationEnergy=VehicleEnergyCosts.Grapple;
                grapple.SetEnergyProvider(GetComponent<VehicleEnergyBank>());grapple.Settings.maximumVehicleAcceleration=6;
                grapple.Settings.maximumTargetAcceleration=4;grapple.Settings.maximumTensionNewtons=8000;
                grapple.Settings.cooldownSeconds=6;grapple.TargetFilter=CanGrappleTarget;
                grappleSpring=grapple.Settings.springStiffness;grappleDamping=grapple.Settings.damping;
                grappleTension=grapple.Settings.maximumTensionNewtons;grappleVehicleAcceleration=grapple.Settings.maximumVehicleAcceleration;
                grappleTargetAcceleration=grapple.Settings.maximumTargetAcceleration;
                grapple.Released+=()=>{ClearGrappleTarget();if(assembly!=null)Model.GrappleReadyAt=Time.time+6;};
            }
            foreach(var part in assembly.Parts)
            {
                if(part.Kind!=R3PartKind.Blink&&part.Kind!=R3PartKind.MagneticGrapple)continue;
                part.gameObject.AddComponent<MOVSeriesPartBinding>().Configure(this,part);
                if(part.Kind==R3PartKind.Blink)
                {
                    var blink=part.gameObject.AddComponent<BlinkDrive>();blink.BindToVehicle(body);
                    blink.Configure(new BlinkSettings{DistanceMeters=8,CooldownSeconds=6,EnergyCost=VehicleEnergyCosts.Blink});
                    blink.SetEnergySource(GetComponent<VehicleEnergyBank>());
                    blink.SetAvailabilityFilter(()=>Available(part));blink.SetGroundFilter(IsTerrain);blinks.Add(blink);
                }
                else
                {
                    var emitter=part.gameObject.AddComponent<MagneticGrappleEmitter>();
                    var muzzle=new GameObject("Wylot zaczepu").transform;muzzle.SetParent(part.transform,false);muzzle.localPosition=Vector3.forward*1.05f;
                    emitter.SetMuzzle(muzzle);emitter.SetAvailabilityFilter(()=>Available(part));emitters.Add(emitter);
                    if(grapple.GetComponent<LineRenderer>()==null)
                    {
                        var line=gameObject.AddComponent<LineRenderer>();line.sharedMaterial=part.GetComponent<Renderer>().sharedMaterial;
                        line.startWidth=line.endWidth=.04f;line.startColor=line.endColor=Color.cyan;
                        line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;grapple.SetRopeRenderer(line);
                    }
                }
            }
        }
        bool CanGrappleTarget(Collider c)
        {
            if(c==null||c.attachedRigidbody==body||c.GetComponentInParent<R3Debris>()!=null)return false;
            var graph=c.GetComponentInParent<P12PipelineSafeVehicleStructure>();
            if(graph!=grappleTargetGraph)
            {
                ClearGrappleTarget();grappleTargetGraph=graph;
                if(graph!=null)
                {
                    grappleTargetParts=graph.GetComponentsInChildren<P12PipelineSafeDamageable>();
                    foreach(var hpPart in grappleTargetParts)hpPart.Died+=MarkGrappleTargetDirty;
                    grappleTargetDirty=true;
                }
            }
            if(grappleTargetDirty&&graph!=null){grappleTargetDirty=false;graph.EvaluateStructure();}
            // Graph evaluation can mark or reparent the anchor. Revalidate afterward, before force.
            var block=c.GetComponentInParent<P12_PipelineSafePlayerBlock>();
            if(block!=null&&(block.DetachedFromVehicle||graph!=null&&!graph.IsBlockConnected(block)))return false;
            if(c.GetComponentInParent<R3Debris>()!=null)return false;
            var hp=c.GetComponentInParent<P12PipelineSafeDamageable>();if(hp!=null&&hp.IsDead)return false;
            var part=c.GetComponentInParent<R3MountedPart>();if(part!=null&&!part.Operational)return false;
            return graph==null||graph.CoreAlive;
        }
        void MarkGrappleTargetDirty(){grappleTargetDirty=true;}
        void ClearGrappleTarget()
        {
            if(grappleTargetParts!=null)foreach(var hp in grappleTargetParts)if(hp!=null)hp.Died-=MarkGrappleTargetDirty;
            grappleTargetGraph=null;grappleTargetParts=null;grappleTargetDirty=false;
        }
        internal void InvalidateEmitter(MagneticGrappleEmitter emitter)
        {if(grapple!=null&&grapple.ActiveEmitter==emitter)ReleaseGrapples();}
        void RefreshGrappleTuning()
        {
            if(grapple==null)return;
            int units=0;float cost=0;
            foreach(var emitter in emitters)
            {
                if(emitter==null||!emitter.IsOperational)continue;
                var part=emitter.GetComponent<R3MountedPart>();if(part==null)continue;
                units+=R3VehicleModel.TierEffectUnits(part.Tier);
                cost+=VehicleEnergyCosts.Grapple*R3VehicleModel.TierActivationCost(part.Tier);
            }
            if(units==0)return;
            var settings=grapple.Settings;settings.activationEnergy=cost;
            settings.springStiffness=grappleSpring*units;settings.damping=grappleDamping*units;
            settings.maximumTensionNewtons=grappleTension*units;
            settings.maximumVehicleAcceleration=grappleVehicleAcceleration*units;
            settings.maximumTargetAcceleration=grappleTargetAcceleration*units;
        }
        public bool TryBlink(Vector3 direction)
        {
            if(!PrepareAction())return false;
            if(GrappleAttached||FindObjectsByType<MagneticGrappleController>(FindObjectsSortMode.None).Any(g=>g.IsAttached&&g.AttachedRigidbody==body))
            {Feedback="Blink zablokowany: aktywna lina";return false;}
            if(Model.BlinkReadyAt>Time.time||Model.Parts.Any(p=>p.Kind==R3PartKind.Blink&&p.DeviceReadyAt>Time.time))return false;
            var eligible=blinks.Where(b=>b!=null&&b.Operational).ToArray();if(eligible.Length==0)return false;
            var primary=eligible[0];primary.Configure(new BlinkSettings{DistanceMeters=8*eligible.Length,CooldownSeconds=6,
                EnergyCost=VehicleEnergyCosts.Blink*eligible.Length});
            var result=primary.TryActivate(direction);Feedback="Blink: "+result;if(result!=BlinkResult.Activated)return false;
            Model.BlinkReadyAt=Time.time+6;foreach(var state in Model.Parts.Where(p=>p.Kind==R3PartKind.Blink))state.DeviceReadyAt=Model.BlinkReadyAt;
            foreach(var blink in eligible)blink.GetComponent<RoboRougelike.R4_1.R4BlinkArt>()?.PlayActivation();
            return true;
        }
        public bool TryGrapple(Vector3 direction)
        {
            if(!PrepareAction()||grapple==null)return false;
            if(grapple.IsAttached){ReleaseGrapples();return true;}
            if(Time.time<Model.GrappleReadyAt)return false;
            RefreshGrappleTuning();
            foreach(var emitter in emitters)
            {
                if(emitter==null||!grapple.TryFire(emitter,direction))continue;
                Model.GrappleReadyAt=Time.time+6;Feedback="Zaczep aktywny • "+Model.ActiveSlotLabel(R3PartKind.MagneticGrapple)+" zwalnia linę";return true;
            }
            ClearGrappleTarget();
            Feedback="Zaczep: brak celu / niesprawny moduł";return false;
        }
        public void ReleaseGrapples(){if(grapple!=null)grapple.Release();ClearGrappleTarget();}
    }
}
