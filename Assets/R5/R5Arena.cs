using System.Collections.Generic;
using UnityEngine;
namespace RoboRougelike.R5
{
    public sealed class R5Arena : MonoBehaviour
    {
        public R5ArenaKind Kind { get; private set; }
        public Transform Layout { get; private set; }
        public Vector3 PlayerSpawn => new Vector3(0,.7f,-22);
        private Material material;
        public void Initialize(Material source)
        {
            material=new Material(source);
            var oldWalls=GameObject.Find("P12_TargetWalls");if(oldWalls!=null)oldWalls.SetActive(false);
        }
        public void Build(R5ArenaKind kind)
        {
            if(Layout!=null){Layout.gameObject.SetActive(false);Destroy(Layout.gameObject);}
            Kind=kind;Layout=new GameObject("Arena R5").transform;Layout.SetParent(transform,false);
            var color=kind==R5ArenaKind.SalvageYard?new Color(.38f,.27f,.17f):kind==R5ArenaKind.Wasteland?new Color(.66f,.49f,.26f):new Color(.21f,.34f,.43f);
            material.SetColor("_BaseColor",color);
            Box("Północ",new Vector3(0,2,42),new Vector3(86,4,2));Box("Południe",new Vector3(0,2,-42),new Vector3(86,4,2));
            Box("Zachód",new Vector3(-42,2,0),new Vector3(2,4,82));Box("Wschód",new Vector3(42,2,0),new Vector3(2,4,82));
            if(kind==R5ArenaKind.SalvageYard)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    Box("Prasa złomu",new Vector3(side*10,.85f,0),new Vector3(3,1.7f,5),true);
                    Box("Wrak osłony",new Vector3(side*21,.6f,13),new Vector3(5,1.2f,2),true);
                    Box("Niska osłona",new Vector3(side*20,.6f,-13),new Vector3(4,1.2f,3),true);
                }
            }
            else if(kind==R5ArenaKind.Wasteland)
            {
                Box("Ostaniec zachodni",new Vector3(-26,1,-5),new Vector3(4,2,6),true);
                Box("Ostaniec wschodni",new Vector3(26,.7f,8),new Vector3(6,1.4f,3),true);
            }
            else
            {
                for(int side=-1;side<=1;side+=2)for(int row=-1;row<=1;row+=2)
                    Box("Hala przemysłowa",new Vector3(side*17,2.5f,row*7),new Vector3(10,5,9));
                Box("Łącznik zachodni",new Vector3(-31,1.5f,0),new Vector3(3,3,14),true);
                Box("Łącznik wschodni",new Vector3(31,1.5f,0),new Vector3(3,3,14),true);
            }
            Hazard(new Vector3(-6,0,-6));Hazard(new Vector3(6,0,8));
            // Floor tint is an instance property, never a modification to the scene asset.
            var floor=GameObject.Find("P12_Ground")?.GetComponent<Renderer>();
            if(floor!=null){var properties=new MaterialPropertyBlock();properties.SetColor("_BaseColor",color*.6f);floor.SetPropertyBlock(properties);}
            Physics.SyncTransforms();
        }
        private void Box(string name,Vector3 position,Vector3 size,bool destructible=false)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(Layout,false);
            obj.transform.localPosition=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;
            if(destructible)obj.AddComponent<RoboRougelike.PackageIntegration.RRLCoverAdapter>();
        }
        private void Hazard(Vector3 position)
        {
            var obj=new GameObject("Strefa wyładowań • żółte ostrzeżenie / czerwone zagrożenie");
            obj.transform.SetParent(Layout,false);obj.transform.localPosition=position;
            obj.AddComponent<RoboRougelike.PackageIntegration.RRLHazardAdapter>();
        }
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
