using System.Collections.Generic;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Copies the existing damage/flash state onto each supplied model's own
    // material colours. It does not create HP, colliders or a second damage system.
    public sealed class R4ReplacementDamageVisual : MonoBehaviour
    {
        private struct Slot
        {
            public Renderer Renderer;
            public int Index;
            public bool HasBase,HasColor;
            public Color Base,Color;
        }
        private readonly List<Slot> slots=new List<Slot>();
        private P12PipelineSafeDamageable owner;
        private MaterialPropertyBlock properties;

        public static void Attach(GameObject visual,Transform block)
        {
            if(visual==null||block==null)return;
            var hp=block.GetComponentInParent<P12PipelineSafeDamageable>();
            if(hp==null)return; // Garage ghosts have no HP and retain their own tint.
            var view=visual.AddComponent<R4ReplacementDamageVisual>();view.Bind(hp);
        }
        private void Bind(P12PipelineSafeDamageable hp)
        {
            owner=hp;properties=new MaterialPropertyBlock();
            foreach(var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if(!(renderer is MeshRenderer)&&!(renderer is SkinnedMeshRenderer))continue;
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var material=materials[i];if(material==null)continue;
                    bool hasBase=material.HasProperty("_BaseColor"),hasColor=material.HasProperty("_Color");
                    if(!hasBase&&!hasColor)continue;
                    slots.Add(new Slot{Renderer=renderer,Index=i,HasBase=hasBase,HasColor=hasColor,
                        Base=hasBase?material.GetColor("_BaseColor"):default,
                        Color=hasColor?material.GetColor("_Color"):default});
                }
            }
            owner.VisualDamageChanged+=Refresh;Refresh();
        }
        private Color Tint(Color original)
        {
            if(owner.IsDead)return new Color(.12f,.1f,.09f,original.a);
            if(owner.VisualFlashing)return Color.Lerp(original,Color.red,.72f);
            float damage=Mathf.Clamp01(1-owner.CurrentHp/Mathf.Max(1,owner.MaxHp))*.8f;
            return Color.Lerp(original,new Color(.16f,.09f,.06f,original.a),damage);
        }
        private void Refresh()
        {
            if(owner==null)return;
            foreach(var slot in slots)
            {
                if(slot.Renderer==null)continue;
                slot.Renderer.GetPropertyBlock(properties,slot.Index);
                if(slot.HasBase)properties.SetColor("_BaseColor",Tint(slot.Base));
                if(slot.HasColor)properties.SetColor("_Color",Tint(slot.Color));
                slot.Renderer.SetPropertyBlock(properties,slot.Index);
            }
        }
        private void OnDestroy(){if(owner!=null)owner.VisualDamageChanged-=Refresh;}
    }
}
