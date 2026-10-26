using UnityEngine;
namespace RoboRougelike.R1
{
    public sealed class R1RoadAnchor : MonoBehaviour
    {
        R1RunController owner;string id;
        public void Bind(R1RunController run,string anchor){owner=run;id=anchor;}
        void OnTriggerEnter(Collider other)=>owner?.NotifyRoadAnchor(other,id);
    }
}
