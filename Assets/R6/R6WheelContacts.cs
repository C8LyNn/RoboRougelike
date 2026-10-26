using System.Collections.Generic;
using UnityEngine;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.R3;
using RoboRougelike.WeaponIntegration;

namespace RoboRougelike.R6
{
    // Contact sensing only. R6 remains the sole wheel force authority.
    public sealed class R6WheelContacts
    {
        readonly RaycastHit[] hits=new RaycastHit[64];
        readonly Dictionary<P12PipelineSafeDamageable,Collider[]> wheelColliders=new Dictionary<P12PipelineSafeDamageable,Collider[]>();
        public void Invalidate()=>wheelColliders.Clear();
        public int Count {get;private set;}
        public float Traction {get;private set;}
        public float Left {get;private set;}
        public float Right {get;private set;}
        public float Track {get;private set;}
        public Vector3 Normal {get;private set;}=Vector3.up;
        public Vector3 CenterOfTraction {get;private set;}
        public static bool Terrain(Collider c)=>c!=null&&!c.isTrigger&&c.enabled&&c.gameObject.activeInHierarchy&&
            RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter.IsTerrain(c);

        public void Refresh(Rigidbody body,P12PipelineSafeDamageable[] parts)
        {
            Count=0;Traction=Left=Right=Track=0;Normal=Vector3.up;CenterOfTraction=Vector3.zero;Vector3 normals=Vector3.zero;
            if(body==null||parts==null)return;
            var structure=body.GetComponent<P12PipelineSafeVehicleStructure>();
            foreach(var part in parts)
            {
                if(part==null||part.IsDead||!R6VehicleDrive.IsWheel(part)||!part.transform.IsChildOf(body.transform)||
                    part.GetComponentInParent<Rigidbody>()!=body||RRLEmpPartState.IsBlocked(part.gameObject))continue;
                var mounted=part.GetComponent<R3MountedPart>();if(mounted!=null&&!mounted.Operational)continue;
                var block=part.GetComponent<P12_PipelineSafePlayerBlock>();if(block!=null&&block.DetachedFromVehicle)continue;
                if(block!=null&&structure!=null&&(!structure.CoreAlive||!structure.IsBlockConnected(block)))continue;
                if(!FindSupport(body,part,out var normal,out var point))continue;
                float weight=Mathf.Lerp(R6Tuning.Current.DamageTractionFloor,1,Mathf.Clamp01(part.CurrentHp/part.MaxHp));
                float x=body.transform.InverseTransformPoint(part.transform.position).x;
                Count++;Traction+=weight;if(x<0)Left+=weight;else Right+=weight;Track+=Mathf.Abs(x)*weight;normals+=normal*weight;CenterOfTraction+=point*weight;
            }
            Normal=normals.sqrMagnitude>.001f?normals.normalized:Vector3.up;
            Track=Traction>0?Mathf.Max(.1f,Track/Traction):0;
            if(Traction>0)CenterOfTraction/=Traction;
        }

        bool FindSupport(Rigidbody body,P12PipelineSafeDamageable part,out Vector3 normal,out Vector3 point)
        {
            normal=Vector3.up;point=Vector3.zero;
            // Gravity-facing side of the chassis; legacy enemy HP lives on an axle-rotated
            // cylinder, so part.up is its axle, not the tread direction. Actual shape pose
            // below still determines reach for every mounted XYZ orientation.
            var down=-(body.rotation*Vector3.up);
            if(Vector3.Dot(-down,Vector3.up)<.35f)return false;
            if(!wheelColliders.TryGetValue(part,out var colliders))wheelColliders.Add(part,colliders=part.GetComponentsInChildren<Collider>());
            bool hasTread=false;
            foreach(var c in colliders)if(Eligible(c,body)&&c.transform!=part.transform)hasTread=true;
            foreach(var c in colliders)
            {
                if(!Eligible(c,body)||(hasTread&&c.transform==part.transform))continue;
                Vector3 center=c.bounds.center;
                float radius=TreadReach(c,body,down);if(radius<.005f)continue;
                // Tolerates the existing 5cm starter clearance, never a long suspension/air ray.
                float skin=Mathf.Clamp(radius*.2f,.015f,.08f);
                int count=Physics.RaycastNonAlloc(center,down,hits,radius+skin,~0,QueryTriggerInteraction.Ignore);
                if(count==hits.Length)continue; // ambiguous/truncated query fails closed
                float nearest=float.PositiveInfinity;Vector3 best=Vector3.zero;
                for(int i=0;i<count;i++)
                {
                    var hit=hits[i];if(!Terrain(hit.collider)||hit.collider.transform.IsChildOf(body.transform))continue;
                    if(hit.distance<radius-skin||Vector3.Dot(hit.normal,Vector3.up)<.5f||Vector3.Dot(hit.normal,-down)<.5f)continue;
                    if(hit.distance<nearest){nearest=hit.distance;best=hit.normal;point=hit.point;}
                }
                if(best.sqrMagnitude>.1f){normal=best;return true;}
            }
            return false;
        }
        static float TreadReach(Collider c,Rigidbody body,Vector3 direction)
        {
            var scale=c.transform.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            var rotation=body.rotation*Quaternion.Inverse(body.transform.rotation)*c.transform.rotation;
            if(c is CapsuleCollider capsule)
            {
                int axis=capsule.direction;float radial=capsule.radius*Mathf.Max(scale[(axis+1)%3],scale[(axis+2)%3]);
                var localAxis=Vector3.zero;localAxis[axis]=1;
                return radial+Mathf.Max(0,capsule.height*scale[axis]*.5f-radial)*Mathf.Abs(Vector3.Dot(rotation*localAxis,direction));
            }
            if(c is SphereCollider sphere)return sphere.radius*Mathf.Max(scale.x,scale.y,scale.z);
            if(c is BoxCollider box)
            {
                var d=Quaternion.Inverse(rotation)*direction;var half=Vector3.Scale(box.size*.5f,scale);
                return Mathf.Abs(d.x)*half.x+Mathf.Abs(d.y)*half.y+Mathf.Abs(d.z)*half.z;
            }
            // Query the actual owned shape from outside its physics bounds, never a render-pose ClosestPoint.
            float length=c.bounds.extents.magnitude+1;
            return c.Raycast(new Ray(c.bounds.center+direction*length,-direction),out var hit,length*2)?Vector3.Dot(hit.point-c.bounds.center,direction):0;
        }
        static bool Eligible(Collider c,Rigidbody body)=>c!=null&&c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy&&c.attachedRigidbody==body;
    }
}
