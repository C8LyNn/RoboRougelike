using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.R3
{
    /// <summary>A shared charging area; each vehicle is charged once per physics step.</summary>
    public sealed class EnergyChargingField : MonoBehaviour
    {
        public const float ChargeDuration = 10f;
        readonly Dictionary<VehicleEnergyBank,float> nextUse = new Dictionary<VehicleEnergyBank,float>();
        readonly Collider[] hits = new Collider[1024];
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
            int count=Physics.OverlapBoxNonAlloc(center,extents,hits,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
            var candidates=hits;
            // Multiple vehicles can share a field; a full buffer may omit any
            // of them even when some vehicles were already found.
            if(count==hits.Length)
            {
                candidates=Physics.OverlapBox(center,extents,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
                count=candidates.Length;
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
