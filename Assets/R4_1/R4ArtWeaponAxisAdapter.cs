using System;
using System.Linq;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Converts the imported FBX node space into the game's +Y-up/+Z-forward
    // mount space. Visual only: no collider, projectile, HP or physics changes.
    [DisallowMultipleComponent]
    public sealed class R4ArtWeaponAxisAdapter : MonoBehaviour
    {
        Transform yaw,pitch;
        Quaternion yawRest,pitchRest;
        Vector3 yawAxis,pitchAxis;
        public Transform Muzzle {get;private set;}

        public bool Bind(Transform visual,string modelName)
        {
            if(Muzzle!=null||visual==null||visual.parent!=transform||string.IsNullOrWhiteSpace(modelName))return false;
            var scale=transform.lossyScale;
            if(float.IsNaN(scale.x)||float.IsInfinity(scale.x)||float.IsNaN(scale.y)||float.IsInfinity(scale.y)||
                float.IsNaN(scale.z)||float.IsInfinity(scale.z)||scale.x<=.0001f||
                Mathf.Abs(scale.x-scale.y)>.0001f||Mathf.Abs(scale.x-scale.z)>.0001f)return false;
            var nodes=visual.GetComponentsInChildren<Transform>(true);
            var pivot=nodes.FirstOrDefault(t=>t.name=="GunPivot");
            var candidateYaw=nodes.FirstOrDefault(t=>t.name==modelName+"_Yaw");
            var candidatePitch=nodes.FirstOrDefault(t=>t.name==modelName+"_Pitch");
            var sourceMuzzle=nodes.FirstOrDefault(t=>t.name=="Muzzle");
            if(pivot==null||candidateYaw==null||sourceMuzzle==null||
                (candidatePitch==null&&!string.Equals(modelName,"R4W_EmpEmitter",StringComparison.Ordinal)))return false;

            // R3's collider block is uniformly scaled to 0.52; the FBX is
            // authored in metres and must not inherit that scale a second time.
            visual.localScale/=scale.x;
            // The art's mount point remains fixed on the Y axis. Rotate only
            // around that axis to put the physical barrel in front of the mount.
            var planar=Vector3.ProjectOnPlane(sourceMuzzle.position-pivot.position,transform.up);
            float correction=planar.sqrMagnitude>1e-8f?Vector3.SignedAngle(planar,transform.forward,transform.up):180f;
            visual.rotation=Quaternion.AngleAxis(correction,transform.up)*visual.rotation;
            yaw=candidateYaw;pitch=candidatePitch;
            yawRest=yaw.localRotation;yawAxis=yaw.InverseTransformDirection(transform.up).normalized;
            if(pitch!=null){pitchRest=pitch.localRotation;pitchAxis=pitch.InverseTransformDirection(-transform.right).normalized;}

            // The FBX muzzle's +Z can face away from its barrel. The child is
            // the direction used by gameplay while the original art stays intact.
            var proxy=new GameObject("R4 Gameplay Muzzle").transform;
            proxy.SetParent(sourceMuzzle,false);
            proxy.rotation=Quaternion.LookRotation(transform.forward,transform.up);
            Muzzle=proxy;
            return true;
        }

        public void SetAim(float yawDegrees,float pitchDegrees)
        {
            if(Muzzle==null||float.IsNaN(yawDegrees)||float.IsInfinity(yawDegrees)||
                float.IsNaN(pitchDegrees)||float.IsInfinity(pitchDegrees))return;
            yaw.localRotation=yawRest*Quaternion.AngleAxis(yawDegrees,yawAxis);
            if(pitch!=null)pitch.localRotation=pitchRest*Quaternion.AngleAxis(pitchDegrees,pitchAxis);
        }
    }
}
