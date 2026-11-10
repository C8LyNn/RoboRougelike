using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R1
{
    /// <summary>Places one neutral charger on real ground for each live activity.</summary>
    public sealed class R1EnergyFieldDirector : MonoBehaviour
    {
        R1RunController run;
        EnergyChargingField field;
        float nextAttempt;
        public void Bind(R1RunController owner)=>run=owner;

        public static bool TrySelectFieldSurface(RaycastHit[] hits,float vehicleY,out RaycastHit selected)
        {
            selected=default;bool found=false;float closest=float.PositiveInfinity;
            if(hits==null||float.IsNaN(vehicleY)||float.IsInfinity(vehicleY))return false;
            foreach(var hit in hits)
            {
                if(hit.collider==null||hit.collider.attachedRigidbody!=null||hit.normal.y<.7f)continue;
                float separation=Mathf.Abs(hit.point.y-vehicleY);
                if(separation>=closest)continue;
                selected=hit;closest=separation;found=true;
            }
            return found;
        }

        void FixedUpdate()
        {
            bool live=run!=null&&!run.Transitioning&&run.Player!=null&&run.Progress!=null&&
                (run.Progress.State==R1RunState.Combat||run.Progress.State==R1RunState.TestDrive);
            if(!live)
            {
                if(field!=null){Destroy(field.gameObject);field=null;}
                return;
            }
            if(field!=null||Time.time<nextAttempt)return;
            nextAttempt=Time.time+.5f;
            var player=run.Player.transform;
            foreach(var offset in new[]{new Vector3(5,0,4),new Vector3(-5,0,4),new Vector3(0,0,5)})
            {
                var origin=player.position+offset+Vector3.up*12f;
                var hits=Physics.RaycastAll(origin,Vector3.down,30f,~0,QueryTriggerInteraction.Ignore);
                if(TrySelectFieldSurface(hits,player.position.y,out var hit))
                {
                    var host=new GameObject("Pole ładowania energii");host.transform.SetParent(transform,false);
                    host.transform.position=hit.point;field=host.AddComponent<EnergyChargingField>();return;
                }
            }
        }
    }
}
