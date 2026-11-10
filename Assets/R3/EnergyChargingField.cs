using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.R3
{
    /// <summary>A shared charging area; each vehicle is charged once per physics step.</summary>
    public sealed class EnergyChargingField : MonoBehaviour
    {
        public const float ChargeDuration = 10f;
        readonly Dictionary<VehicleEnergyBank,float> nextUse = new Dictionary<VehicleEnergyBank,float>();
        const int MaxContactBuffer=4096;
        Collider[] hits = new Collider[1024];
        int previousHits;
        readonly HashSet<VehicleEnergyBank> vehicles = new HashSet<VehicleEnergyBank>();
        Material visualMaterial;

        void Awake()
        {
            var visual=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name="Pole ładowania energii";visual.transform.SetParent(transform,false);
            visual.transform.localPosition=new Vector3(0,.035f,0);
            visual.transform.localScale=new Vector3(5f,.025f,5f);
            var collider=visual.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            var shader=Shader.Find("Universal Render Pipeline/Unlit");
            if(shader==null)shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader!=null)
            {
                visualMaterial=new Material(shader);
                if(visualMaterial.HasProperty("_BaseColor"))visualMaterial.SetColor("_BaseColor",new Color(.05f,.85f,.9f));
                if(visualMaterial.HasProperty("_Color"))visualMaterial.SetColor("_Color",new Color(.05f,.85f,.9f));
                visual.GetComponent<Renderer>().sharedMaterial=visualMaterial;
            }
        }

        void FixedUpdate()
        {
            vehicles.Clear();
            var center=transform.position+Vector3.up;
            var extents=new Vector3(2.5f,1.5f,2.5f);
            int count;Collider[] candidates;
            while(true)
            {
                count=Physics.OverlapBoxNonAlloc(center,extents,hits,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
                if(count<hits.Length)
                {
                    if(previousHits>count)System.Array.Clear(hits,count,previousHits-count);
                    previousHits=count;candidates=hits;break;
                }
                // Multiple vehicles share the field, so inspect the complete
                // result even when some were already found in a partial query.
                if(hits.Length>=MaxContactBuffer)
                {
                    System.Array.Clear(hits,0,hits.Length);previousHits=0;
                    candidates=Physics.OverlapBox(center,extents,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
                    count=candidates.Length;break;
                }
                System.Array.Resize(ref hits,Mathf.Min(hits.Length*2,MaxContactBuffer));
            }
            for(int i=0;i<count;i++)
            {
                var bank=candidates[i]!=null?candidates[i].GetComponentInParent<VehicleEnergyBank>():null;
                if(bank!=null&&bank.isActiveAndEnabled)vehicles.Add(bank);
            }
            foreach(var bank in vehicles)
            {
                if(nextUse.TryGetValue(bank,out var ready)&&Time.time<ready)continue;
                nextUse[bank]=Time.time+ChargeDuration;bank.ApplyFieldCharge();
            }
        }

        void OnDestroy(){if(visualMaterial!=null)Destroy(visualMaterial);}
    }
}
