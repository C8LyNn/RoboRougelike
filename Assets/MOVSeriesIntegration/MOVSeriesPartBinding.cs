using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.Booster;
using RoboRougelike.Mechanics.MOV002JumpDrive;
using RoboRougelike.Mechanics.MOV004GyroscopicStabilizer;
using RoboRougelike.Mechanics.MOV003Blink;
using RoboRougelike.Mechanics.MOV006MagneticGrapple;
using RoboRougelike.Mechanics.MOV005OmniDrive;
using RoboRougelike.Mechanics.MOV007;
using RoboRougelike.Mechanics.MOV008;

namespace RoboRougelike.MOVSeries
{
    public sealed class MOVSeriesPartBinding : MonoBehaviour
    {
        MOVSeriesVehicleAdapter owner;R3MountedPart part;
        public void Configure(MOVSeriesVehicleAdapter adapter,R3MountedPart mounted)
        {owner=adapter;part=mounted;part.Damageable.Died+=Invalidate;}
        void Invalidate()
        {
            if(TryGetComponent<VehicleBooster>(out var b))b.SetOperational(false);
            if(TryGetComponent<JumpDrive>(out var j))j.SetOperational(false);
            if(TryGetComponent<GyroStabilizer>(out var g))g.SetOperational(false);
            if(TryGetComponent<OmniDriveModule>(out var omni))omni.SetOperational(false);
            if(TryGetComponent<HoverLiftPod>(out var pod))pod.SetOperational(false);
            if(TryGetComponent<CrawlerTrackModule>(out var track))track.SetOperational(false);
            if(TryGetComponent<BlinkDrive>(out var blink))blink.SetOperational(false);
            if(TryGetComponent<MagneticGrappleEmitter>(out var emitter))
            {
                emitter.SetOperational(false);owner.InvalidateEmitter(emitter);
            }
        }
        void OnTransformParentChanged(){if(owner!=null&&!owner.Available(part))Invalidate();}
        void OnDestroy(){if(part!=null&&part.Damageable!=null)part.Damageable.Died-=Invalidate;}
    }
}
