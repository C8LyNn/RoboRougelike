using UnityEngine;
using RoboRougelike.R3;

namespace RoboRougelike.MOVSeries
{
    public static class MOVSeriesRange
    {
        public static void Build(Transform parent,R3VisualPalette palette)
        {
            Box(parent,palette,"Skok • niska przeszkoda",new Vector3(-12,.2f,-13),new Vector3(5,.4f,.6f));
            Box(parent,palette,"Skok • wyższa przeszkoda",new Vector3(-12,.4f,-4),new Vector3(5,.8f,.6f));
            Box(parent,palette,"Grapple • kotwa magnetyczna",new Vector3(-25,3,7),new Vector3(2,6,2));
            // Flat colored lanes retain the scene floor as their support surface.
            Mark(parent,palette,"Blink • wolna strefa 8 m",new Vector3(0,.015f,-20),new Vector3(5,.02f,9),palette.Parts[(int)R3PartKind.Blink]);
            Mark(parent,palette,"Hover / Omni • manewry i obrót",new Vector3(-25,.015f,-23),new Vector3(18,.02f,14),palette.Parts[(int)R3PartKind.HoverEngine]);
        }
        static void Box(Transform parent,R3VisualPalette palette,string name,Vector3 position,Vector3 scale)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);
            obj.transform.localPosition=position;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=palette.Parts[0];
        }
        static void Mark(Transform parent,R3VisualPalette palette,string name,Vector3 position,Vector3 scale,Material material)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);
            obj.transform.localPosition=position;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=material;
            obj.GetComponent<Collider>().enabled=false;Object.Destroy(obj.GetComponent<Collider>());
        }
    }
}
