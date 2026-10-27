using UnityEngine;

namespace RoboRougelike.R3
{
    public sealed class R3VisualPalette : MonoBehaviour
    {
        public readonly Material[] Parts = new Material[13];
        public Material Valid, Invalid, Missing,Selection;
        public readonly Material[] Weapons=new Material[3];
        public void Initialize(Material template)
        {
            Color[] colors = { new Color(0.22f,0.6f,0.67f), new Color(0.16f,0.19f,0.22f),
                new Color(0.9f,0.62f,0.22f), new Color(0.1f,0.5f,1), new Color(.72f,.35f,.95f),
                new Color(1,.35f,.12f),new Color(.3f,.9f,.55f),new Color(.2f,.75f,1),new Color(.9f,.8f,.2f),
                new Color(.2f,.8f,.8f),new Color(.85f,.4f,.7f),new Color(.4f,.6f,1),new Color(.4f,.45f,.3f) };
            for (int i=0;i<Parts.Length;i++) Parts[i] = Make(template, colors[i]);
            Valid=Make(template,Color.green); Invalid=Make(template,new Color(1,0.2f,0.12f));
            Missing=Make(template,new Color(0.4f,0.2f,0.22f));
            Selection=Make(template,new Color(0.1f,0.9f,1,0.2f));
            Transparent(Valid,0.42f);Transparent(Invalid,0.42f);Transparent(Missing,0.23f);Transparent(Selection,0.2f);
            for(int i=0;i<3;i++)Weapons[i]=Make(template,RoboRougelike.R4.R4WeaponDefinition.Get((RoboRougelike.R4.R4WeaponKind)i).Color);
        }
        private static Material Make(Material source, Color color)
        {
            var mat = new Material(source);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color",color);
            return mat;
        }
        private static void Transparent(Material material,float alpha)
        {
            var color=material.GetColor("_BaseColor");color.a=alpha;material.SetColor("_BaseColor",color);
            material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.SetOverrideTag("RenderType","Transparent");material.renderQueue=3000;
        }
        private void OnDestroy()
        {
            foreach(var mat in Parts) if(mat!=null) Destroy(mat);
            foreach(var mat in Weapons)if(mat!=null)Destroy(mat);
            if(Valid!=null)Destroy(Valid); if(Invalid!=null)Destroy(Invalid); if(Missing!=null)Destroy(Missing);
            if(Selection!=null)Destroy(Selection);
        }
    }
}
