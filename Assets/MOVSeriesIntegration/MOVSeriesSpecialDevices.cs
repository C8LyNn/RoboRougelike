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
                grapple.Settings.useEnergy=false;grapple.Settings.maximumVehicleAcceleration=6;
                grapple.Settings.maximumTargetAcceleration=4;grapple.Settings.maximumTensionNewtons=8000;
                grapple.Settings.cooldownSeconds=6;grapple.TargetFilter=CanGrappleTarget;
                grapple.Released+=()=>{ClearGrappleTarget();if(assembly!=null)Model.GrappleReadyAt=Time.time+6;};
            }
            foreach(var part in assembly.Parts)
            {
                if(part.Kind!=R3PartKind.Blink&&part.Kind!=R3PartKind.MagneticGrapple)continue;
                part.gameObject.AddComponent<MOVSeriesPartBinding>().Configure(this,part);
                if(part.Kind==R3PartKind.Blink)
                {
                    var blink=part.gameObject.AddComponent<BlinkDrive>();blink.BindToVehicle(body);
                    blink.Configure(new BlinkSettings{DistanceMeters=8,CooldownSeconds=6});
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
        public bool TryBlink(Vector3 direction)
        {
            if(!PrepareAction())return false;
            if(GrappleAttached||FindObjectsByType<MagneticGrappleController>(FindObjectsSortMode.None).Any(g=>g.IsAttached&&g.AttachedRigidbody==body))
            {Feedback="Blink zablokowany: aktywna lina";return false;}
            foreach(var blink in blinks)
            {
                if(blink==null)continue;var state=State(blink.GetComponent<R3MountedPart>());
                if(Time.time<state.DeviceReadyAt)continue;
                var result=blink.TryActivate(direction);Feedback="Blink: "+result;
                if(result!=BlinkResult.Activated)continue;
                state.DeviceReadyAt=Time.time+6;return true;
            }
            return false;
        }
        public bool TryGrapple(Vector3 direction)
        {
            if(!PrepareAction()||grapple==null)return false;
            if(grapple.IsAttached){ReleaseGrapples();return true;}
            if(Time.time<Model.GrappleReadyAt)return false;
            foreach(var emitter in emitters)
            {
                if(emitter==null||!grapple.TryFire(emitter,direction))continue;
                Model.GrappleReadyAt=Time.time+6;Feedback="Zaczep aktywny • E zwalnia linę";return true;
            }
            ClearGrappleTarget();
            Feedback="Zaczep: brak celu / niesprawny moduł";return false;
        }
        public void ReleaseGrapples(){if(grapple!=null)grapple.Release();ClearGrappleTarget();}
    }
}
