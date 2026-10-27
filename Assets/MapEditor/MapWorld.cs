using System.Collections.Generic;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;

namespace RoboRougelike.MapEditor
{
    public sealed class MapWorld : MonoBehaviour
    {
        public const float Grid=4f;
        readonly List<Mesh> meshes=new List<Mesh>();
        public Transform Layout {get;private set;}
        public bool Finished {get;private set;}
        public static Vector3 Position(MapItem item)=>new Vector3(item.x*Grid,0,item.z*Grid);
        public static Vector2Int Cell(Vector3 point)=>new Vector2Int(Mathf.FloorToInt((point.x+Grid*.5f)/Grid),Mathf.FloorToInt((point.z+Grid*.5f)/Grid));
        static void Colorize(Renderer r,Color color){var p=new MaterialPropertyBlock();p.SetColor("_BaseColor",color);p.SetColor("_Color",color);r.SetPropertyBlock(p);}
        public void Build(MapDraft draft,R3VisualPalette palette)
        {
            Clear();Finished=false;Layout=new GameObject("Mapa gracza").transform;Layout.SetParent(transform,false);
            foreach(var item in draft.Items)
            {
                var o=new GameObject(item.kind.ToString());o.transform.SetParent(Layout,false);
                o.transform.position=Position(item);o.transform.rotation=Quaternion.Euler(0,item.rotation*90,0);
                var material=palette.Parts[0];
                if(item.kind==MapItemKind.Ramp)
                {
                    var mesh=RampMesh();meshes.Add(mesh);o.AddComponent<MeshFilter>().sharedMesh=mesh;
                    o.AddComponent<MeshRenderer>().sharedMaterial=material;o.AddComponent<MeshCollider>().sharedMesh=mesh;
                    Colorize(o.GetComponent<Renderer>(),new Color(.42f,.55f,.55f));continue;
                }
                if(item.kind==MapItemKind.Spawn||item.kind==MapItemKind.Finish)
                {
                    var marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);marker.name="Znacznik";marker.transform.SetParent(o.transform,false);
                    marker.transform.localPosition=new Vector3(0,.04f,0);marker.transform.localScale=new Vector3(2,.04f,2);
                    marker.GetComponent<Collider>().enabled=false;marker.GetComponent<Renderer>().sharedMaterial=material;
                    Colorize(marker.GetComponent<Renderer>(),item.kind==MapItemKind.Spawn?Color.cyan:Color.yellow);
                    if(item.kind==MapItemKind.Spawn)
                    {
                        var arrow=GameObject.CreatePrimitive(PrimitiveType.Cube);arrow.name="Kierunek startu";arrow.transform.SetParent(o.transform,false);
                        arrow.transform.localPosition=new Vector3(0,.12f,.75f);arrow.transform.localScale=new Vector3(.22f,.12f,1.1f);
                        arrow.GetComponent<Collider>().enabled=false;arrow.GetComponent<Renderer>().sharedMaterial=material;Colorize(arrow.GetComponent<Renderer>(),Color.white);
                    }
                    if(item.kind==MapItemKind.Finish){var trigger=o.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=new Vector3(0,1,0);trigger.size=new Vector3(3.5f,2,3.5f);o.AddComponent<MapFinish>().Owner=this;}
                    continue;
                }
                var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.SetParent(o.transform,false);block.GetComponent<Renderer>().sharedMaterial=material;
                if(item.kind==MapItemKind.Ground){block.transform.localPosition=new Vector3(0,-.25f,0);block.transform.localScale=new Vector3(Grid,.5f,Grid);Colorize(block.GetComponent<Renderer>(),new Color(.3f,.38f,.4f));}
                else if(item.kind==MapItemKind.Wall){block.transform.localPosition=new Vector3(0,2,0);block.transform.localScale=new Vector3(Grid,4,.5f);Colorize(block.GetComponent<Renderer>(),new Color(.65f,.48f,.3f));}
                else {block.transform.localPosition=new Vector3(0,.5f,0);block.transform.localScale=new Vector3(2,1,2);Colorize(block.GetComponent<Renderer>(),new Color(.7f,.32f,.18f));}
            }
            Physics.SyncTransforms();
        }
        public void MarkFinish(Collider other)
        {
            var run=R1RunController.Instance;
            if(run!=null&&run.MapEditorTesting&&run.Player!=null&&other.attachedRigidbody==run.Player.GetComponent<Rigidbody>())Finished=true;
        }
        public void Clear(){if(Layout!=null){Layout.gameObject.SetActive(false);Destroy(Layout.gameObject);Layout=null;}foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);meshes.Clear();}
        void OnDestroy()=>Clear();
        static Mesh RampMesh()
        {
            var m=new Mesh{name="Rampa mapy gracza"};
            m.vertices=new[]{new Vector3(-2,0,-2),new Vector3(2,0,-2),new Vector3(-2,0,2),new Vector3(2,0,2),new Vector3(-2,1,2),new Vector3(2,1,2)};
            m.triangles=new[]{0,4,5,0,5,1,0,2,4,1,5,3,2,3,5,2,5,4,0,1,3,0,3,2};
            m.RecalculateNormals();m.RecalculateBounds();return m;
        }
    }
    public sealed class MapFinish : MonoBehaviour
    {
        public MapWorld Owner;
        void OnTriggerEnter(Collider other){if(Owner!=null)Owner.MarkFinish(other);}
    }
}
