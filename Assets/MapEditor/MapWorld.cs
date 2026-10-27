using System.Collections.Generic;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;

namespace RoboRougelike.MapEditor
{
    public sealed class MapWorld : MonoBehaviour
    {
        public const float Grid=4f;
        readonly List<Mesh> meshes=new List<Mesh>(),ghostMeshes=new List<Mesh>();
        GameObject ghost;
        MapItemKind ghostKind;
        public Transform Layout {get;private set;}
        public bool Finished {get;private set;}
        public static Vector3 Position(MapItem item)
        {
            var offset=new Vector3((item.scaleX-1)*Grid*.5f,0,(item.scaleZ-1)*Grid*.5f);
            return new Vector3(item.x*Grid,0,item.z*Grid)+Quaternion.Euler(0,item.rotation*90,0)*offset;
        }
        public static Vector2Int Cell(Vector3 point)=>new Vector2Int(Mathf.FloorToInt((point.x+Grid*.5f)/Grid),Mathf.FloorToInt((point.z+Grid*.5f)/Grid));
        static void Colorize(Renderer r,Color color){var p=new MaterialPropertyBlock();p.SetColor("_BaseColor",color);p.SetColor("_Color",color);r.SetPropertyBlock(p);}
        public void Build(MapDraft draft,R3VisualPalette palette)
        {
            Clear();Finished=false;Layout=new GameObject("Mapa gracza").transform;Layout.SetParent(transform,false);
            if(draft.GlobalFloor)
            {
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Podłoże całej mapy";floor.transform.SetParent(Layout,false);
                float width=(MapDraft.MaxCoordinate*2+1)*Grid;
                floor.transform.localPosition=new Vector3(0,-.25f,0);floor.transform.localScale=new Vector3(width,.5f,width);
                floor.GetComponent<Renderer>().sharedMaterial=palette.Parts[0];Colorize(floor.GetComponent<Renderer>(),new Color(.3f,.38f,.4f));
            }
            foreach(var item in draft.Items)
                if(!draft.GlobalFloor||item.kind!=MapItemKind.Ground)CreateItem(item,Layout,palette,false,meshes);
            Physics.SyncTransforms();
        }
        GameObject CreateItem(MapItem item,Transform parent,R3VisualPalette palette,bool preview,List<Mesh> ownedMeshes)
        {
            var o=new GameObject(item.kind.ToString());o.transform.SetParent(parent,false);
            ApplyTransform(o,item);
            var material=preview?palette.Valid:palette.Parts[0];
            if(item.kind==MapItemKind.Ramp)
            {
                var mesh=RampMesh();ownedMeshes.Add(mesh);o.AddComponent<MeshFilter>().sharedMesh=mesh;
                var render=o.AddComponent<MeshRenderer>();render.sharedMaterial=material;
                if(!preview)o.AddComponent<MeshCollider>().sharedMesh=mesh;
                if(!preview)Colorize(render,new Color(.42f,.55f,.55f));return o;
            }
            if(item.kind==MapItemKind.Spawn||item.kind==MapItemKind.Finish)
            {
                var marker=Primitive(o,PrimitiveType.Cylinder,"Znacznik",new Vector3(0,.04f,0),new Vector3(2,.04f,2),material,true);
                if(!preview)Colorize(marker.GetComponent<Renderer>(),item.kind==MapItemKind.Spawn?Color.cyan:Color.yellow);
                if(item.kind==MapItemKind.Spawn)
                {
                    var arrow=Primitive(o,PrimitiveType.Cube,"Kierunek startu",new Vector3(0,.12f,.75f),new Vector3(.22f,.12f,1.1f),material,true);
                    if(!preview)Colorize(arrow.GetComponent<Renderer>(),Color.white);
                }
                if(!preview&&item.kind==MapItemKind.Finish){var trigger=o.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=new Vector3(0,1,0);trigger.size=new Vector3(3.5f,2,3.5f);o.AddComponent<MapFinish>().Owner=this;}
                return o;
            }
            if(item.kind==MapItemKind.Steps)
            {
                for(int i=0;i<4;i++)
                {
                    float h=(i+1)*.25f;var step=Primitive(o,PrimitiveType.Cube,"Stopień",new Vector3(0,h*.5f,-1.5f+i),new Vector3(4,h,1),material,preview);
                    if(!preview)Colorize(step.GetComponent<Renderer>(),new Color(.48f,.55f,.59f));
                }
                return o;
            }
            Vector3 position,scale;Color color;
            switch(item.kind)
            {
                case MapItemKind.Ground:position=new Vector3(0,-.25f,0);scale=new Vector3(Grid,.5f,Grid);color=new Color(.3f,.38f,.4f);break;
                case MapItemKind.Wall:position=new Vector3(0,2,0);scale=new Vector3(Grid,4,.5f);color=new Color(.65f,.48f,.3f);break;
                case MapItemKind.Platform:position=new Vector3(0,.5f,0);scale=new Vector3(4,1,4);color=new Color(.37f,.47f,.55f);break;
                case MapItemKind.Pillar:position=new Vector3(0,2,0);scale=new Vector3(1.5f,2,1.5f);color=new Color(.5f,.5f,.65f);break;
                case MapItemKind.LowBarrier:position=new Vector3(0,.5f,0);scale=new Vector3(4,1,.5f);color=new Color(.75f,.55f,.2f);break;
                default:position=new Vector3(0,.5f,0);scale=new Vector3(2,1,2);color=new Color(.7f,.32f,.18f);break;
            }
            var block=Primitive(o,item.kind==MapItemKind.Pillar?PrimitiveType.Cylinder:PrimitiveType.Cube,"Bryła",position,scale,material,preview);
            if(!preview)Colorize(block.GetComponent<Renderer>(),color);
            return o;
        }
        static GameObject Primitive(GameObject parent,PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material,bool noCollision)
        {
            var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent.transform,false);
            o.transform.localPosition=position;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;
            if(noCollision)o.GetComponent<Collider>().enabled=false;
            return o;
        }
        static void ApplyTransform(GameObject o,MapItem item)
        {o.transform.position=Position(item);o.transform.rotation=Quaternion.Euler(0,item.rotation*90,0);o.transform.localScale=new Vector3(item.scaleX,item.scaleY,item.scaleZ);}
        public void ShowGhost(MapItem item,bool valid,R3VisualPalette palette)
        {
            if(item==null){HideGhost();return;}
            if(ghost==null||ghostKind!=item.kind)
            {
                HideGhost();ghostKind=item.kind;ghost=CreateItem(item,transform,palette,true,ghostMeshes);ghost.name="Widmo edytora map";
            }
            ghost.SetActive(true);ApplyTransform(ghost,item);
            foreach(var renderer in ghost.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=valid?palette.Valid:palette.Invalid;
        }
        public void HideGhost()
        {
            if(ghost!=null){ghost.SetActive(false);Destroy(ghost);ghost=null;}
            foreach(var mesh in ghostMeshes)if(mesh!=null)Destroy(mesh);ghostMeshes.Clear();
        }
        public void MarkFinish(Collider other)
        {
            var run=R1RunController.Instance;
            if(run!=null&&run.MapEditorTesting&&run.Player!=null&&other.attachedRigidbody==run.Player.GetComponent<Rigidbody>())Finished=true;
        }
        public void Clear(){if(Layout!=null){Layout.gameObject.SetActive(false);Destroy(Layout.gameObject);Layout=null;}foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);meshes.Clear();}
        void OnDestroy(){HideGhost();Clear();}
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
