using System;
using UnityEngine;

namespace RoboRougelike.MOV009Integration
{
    public static class MOV009TerrainCourse
    {
        // The same geometry is used by the playable range and physical pilots.
        public static GameObject BuildLane(Transform parent,Vector3 origin,Material material,string terrain,bool createFloor)
        {
            var lane=new GameObject("MOV009 "+terrain);lane.transform.SetParent(parent,false);lane.transform.position=origin;
            if(createFloor)Box(lane.transform,material,"Podłoże",new Vector3(0,-.5f,35),new Vector3(12,1,100));
            AddObstacle(lane.transform,material,terrain,8);return lane;
        }
        public static void BuildPracticeCourse(Transform parent,Material material)
        {
            var course=new GameObject("Tor blokowy MOV009");course.transform.SetParent(parent,false);course.transform.localPosition=new Vector3(12,0,-25);
            AddObstacle(course.transform,material,"LowSteps",7);
            AddObstacle(course.transform,material,"Uneven",15);
            AddObstacle(course.transform,material,"Slope",24);
            AddObstacle(course.transform,material,"Adjacent",38);
        }
        static void AddObstacle(Transform parent,Material material,string kind,float z)
        {
            switch(kind)
            {
                case "Flat":break;
                case "LowSteps":
                    for(int i=0;i<3;i++){float h=i==1?.16f:.10f;Box(parent,material,"Niski stopień",new Vector3(0,h/2,z+i*2),new Vector3(10,h,2));}
                    break;
                case "Uneven":
                    for(int row=0;row<3;row++)for(int col=-2;col<=2;col++)
                    {float h=((row+col+2)%2==0)?.08f:.12f;Box(parent,material,"Nierówny blok",new Vector3(col*2,h/2,z+row*2),new Vector3(1.9f,h,1.8f));}
                    break;
                case "Slope":case "Edge":
                    float height=kind=="Edge"?.45f:.60f;
                    var ramp=Box(parent,material,"Pochyłość",new Vector3(0,height/2+.06f,z),new Vector3(10,.12f,4));
                    ramp.transform.localRotation=Quaternion.Euler(-Mathf.Atan(height/4)*Mathf.Rad2Deg,0,0);
                    Box(parent,material,"Platforma i krawędź",new Vector3(0,height/2,z+4),new Vector3(10,height,4));
                    break;
                case "Adjacent":
                    for(int i=0;i<4;i++)Box(parent,material,"Sąsiednia przeszkoda",new Vector3(0,.06f,z+i*1.25f),new Vector3(10,.12f,1));
                    break;
                default:throw new ArgumentException("Unknown MOV009 terrain: "+kind,nameof(kind));
            }
        }
        static GameObject Box(Transform parent,Material material,string name,Vector3 position,Vector3 size)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);
            obj.transform.localPosition=position;obj.transform.localScale=size;var renderer=obj.GetComponent<Renderer>();renderer.sharedMaterial=material;
            var props=new MaterialPropertyBlock();props.SetColor("_BaseColor",new Color(.49f,.38f,.62f));renderer.SetPropertyBlock(props);return obj;
        }
    }
}
