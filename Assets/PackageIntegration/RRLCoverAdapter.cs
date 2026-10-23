using UnityEngine;
using RoboRougelike.Mechanics.COV001;
using RoboRougelike.WeaponIntegration;

namespace RoboRougelike.PackageIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLCoverAdapter : MonoBehaviour,IRRLHitReceiver
    {
        DestructibleArenaCover cover;
        Renderer visual;
        Color intactColor;
        MaterialPropertyBlock properties;
        void Awake()
        {
            cover=GetComponent<DestructibleArenaCover>();
            if(cover==null)cover=gameObject.AddComponent<DestructibleArenaCover>();
            visual=GetComponent<Renderer>();properties=new MaterialPropertyBlock();
            if(visual!=null&&visual.sharedMaterial!=null)
                intactColor=visual.sharedMaterial.GetColor(visual.sharedMaterial.HasProperty("_BaseColor")?"_BaseColor":"_Color");
            cover.StageChanged+=RefreshVisual;
            if(GetComponent<RRLRocketDamageReceiver>()==null)gameObject.AddComponent<RRLRocketDamageReceiver>();
            RefreshVisual(cover.Stage);
        }
        public bool ReceiveHit(float damage,Vector3 point,Vector3 direction,Transform source)
            =>cover!=null&&cover.isActiveAndEnabled&&cover.ApplyDamage(damage);
        void RefreshVisual(CoverDamageStage stage)
        {
            if(visual==null)return;
            visual.enabled=stage!=CoverDamageStage.Broken;
            Color color=stage==CoverDamageStage.Critical?Color.Lerp(intactColor,new Color(.5f,.09f,.03f),.7f):
                stage==CoverDamageStage.Damaged?Color.Lerp(intactColor,new Color(.65f,.3f,.08f),.45f):intactColor;
            properties.SetColor("_BaseColor",color);properties.SetColor("_Color",color);visual.SetPropertyBlock(properties);
        }
        void OnDestroy(){if(cover!=null)cover.StageChanged-=RefreshVisual;}
    }
}
