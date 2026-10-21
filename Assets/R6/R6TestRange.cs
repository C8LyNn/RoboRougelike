using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R6
{
    public sealed class R6TestRange : MonoBehaviour
    {
        public Transform Layout {get;private set;}
        Material material;
        public void Build(R3VisualPalette palette)
        {
            Clear();Layout=new GameObject("Poligon R6").transform;
            Layout.SetParent(transform,false);material=palette.Parts[0];
            Box("Bariera północna",new Vector3(0,2,40),new Vector3(82,4,2));
            Box("Bariera południowa",new Vector3(0,2,-40),new Vector3(82,4,2));
            Box("Bariera zachodnia",new Vector3(-40,2,0),new Vector3(2,4,80));
            Box("Bariera wschodnia",new Vector3(40,2,0),new Vector3(2,4,80));
            Box("Ściana zderzeniowa",new Vector3(23,2,-4),new Vector3(3,4,18));
            for(int i=0;i<4;i++)Box("Slalom "+(i+1),new Vector3(i%2==0?-6:6,1,-10+i*7),new Vector3(2,2,2));
            for(int i=0;i<3;i++)Target(new Vector3((i-1)*13,.65f,22));
            RoboRougelike.MOV009Integration.MOV009TerrainCourse.BuildPracticeCourse(Layout,material);
            RoboRougelike.MOVSeries.MOVSeriesRange.Build(Layout,palette);
            var dummy=R3VehicleAssembly.Build(new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard)),new R1RunStats(),palette,new Vector3(-18,.7f,-5),Quaternion.identity);
            dummy.name="Pojazd do przepychania";dummy.transform.SetParent(Layout,true);dummy.Controller.SetGameplayInputEnabled(false);
            foreach(var r in dummy.GetComponentsInChildren<Renderer>())Tint(r,new Color(.8f,.32f,.17f));
            Physics.SyncTransforms();
        }
        void Target(Vector3 position)
        {
            var target=new GameObject("Cel blokowy");target.transform.SetParent(Layout,false);target.transform.position=position;
            target.AddComponent<Rigidbody>().isKinematic=true;P12_PipelineSafePlayerBlock core=null;
            for(int x=-2;x<=2;x++)for(int y=0;y<3;y++)
            {
                var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=x==0&&y==0?"Rdzeń celu":"Pancerz celu";
                obj.transform.SetParent(target.transform,false);obj.transform.localPosition=new Vector3(x*.55f,y*.55f,0);obj.transform.localScale=Vector3.one*.52f;
                obj.GetComponent<Renderer>().sharedMaterial=material;Tint(obj.GetComponent<Renderer>(),x==0&&y==0?Color.cyan:new Color(.9f,.55f,.16f));
                var hp=obj.AddComponent<P12PipelineSafeDamageable>();hp.Configure(x==0&&y==0?300:100,true);
                hp.SetBaseTint(x==0&&y==0?Color.cyan:new Color(.9f,.55f,.16f));
                var block=obj.AddComponent<P12_PipelineSafePlayerBlock>();if(x==0&&y==0)core=block;
            }
            var graph=target.AddComponent<P12PipelineSafeVehicleStructure>();graph.SetDesignatedCore(core);graph.RebuildNow();
        }
        void Box(string name,Vector3 position,Vector3 size)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(Layout,false);
            obj.transform.localPosition=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;
            Tint(obj.GetComponent<Renderer>(),new Color(.28f,.38f,.4f));
        }
        static void Tint(Renderer renderer,Color color)
        {var props=new MaterialPropertyBlock();props.SetColor("_BaseColor",color);renderer.SetPropertyBlock(props);renderer.GetComponent<P12PipelineSafeDamageable>()?.SetBaseTint(color);}
        public void Clear(){if(Layout!=null){Layout.gameObject.SetActive(false);Destroy(Layout.gameObject);Layout=null;}}
        void OnDestroy(){Clear();}
    }
}
