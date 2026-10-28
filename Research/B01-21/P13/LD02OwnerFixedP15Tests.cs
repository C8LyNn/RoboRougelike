// B01-21 PASS15 continuation: independent owner->receiver acceptance on B01-25 NEW source SHA.
// Executes original Codex MapDraft.cs against new actual B01-25 P15 owner JSON.
// Only source-dimension geometric face-contact logic is used; no Unity or PhysX.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoboRougelike.MapEditor;

internal static class LD02OwnerFixedP15Tests
{
    static void Need(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
    static MapDraft Load(string path)
    {
        var d=new MapDraft();
        Need(d.TryLoad(File.ReadAllText(path),out string err),"original Codex TryLoad rejected "+path+": "+err);
        Need(d.TestError()==null,"Original Codex TestError: "+path+" -> "+d.TestError());
        return d;
    }
    static void Walk(MapDraft d,IEnumerable<(int x,int z)> path,int edges)
    {
        var p=path.ToArray();Need(p.Length-1==edges,"route not of expected length");
        Need((p[0].x,p[0].z)==(d.Spawn.x,d.Spawn.z) &&
             (p[p.Length-1].x,p[p.Length-1].z)==(d.Finish.x,d.Finish.z),"bad route endpoints");
        var solid=new HashSet<UnityEngine.Vector2Int>(
            d.Items.Where(x=>x.kind!=MapItemKind.Ground&&x.kind!=MapItemKind.Spawn&&x.kind!=MapItemKind.Finish).SelectMany(MapDraft.Footprint));
        var ground=new HashSet<UnityEngine.Vector2Int>(
            d.Items.Where(x=>x.kind==MapItemKind.Ground).SelectMany(MapDraft.Footprint));
        for(int i=0;i<p.Length;i++)
        {
            var v=new UnityEngine.Vector2Int(p[i].x,p[i].z);
            Need(ground.Contains(v)&&!solid.Contains(v),"original fixed owner map route blocked: "+p[i]);
            if(i>0)Need(Math.Abs(p[i].x-p[i-1].x)+Math.Abs(p[i].z-p[i-1].z)==1,"route discontinuity");
        }
    }
    static float SourcePhysicalGap(float aMin,float aMax,float bMin,float bMax)
        =>Math.Max(0f,Math.Max(aMin,bMin)-Math.Min(aMax,bMax));
    public static void Run()
    {
        var old=Load("Research/B01-25/P15/LD02_COVER_CHOICE.json");
        var d=Load("Research/B01-21/P13/LD02_OWNER_REPAIRED_P15.json");
        Need(old.Items.Count==173&&d.Items.Count==173&&!d.GlobalFloor,"wrong original owner P15 document header");
        var corners=new HashSet<(int,int)>{(0,0),(11,0),(0,9),(11,9)};
        int changes=0;
        for(int i=0;i<old.Items.Count;i++)
        {
            var a=old.Items[i];var z=d.Items[i];
            Need(a.x==z.x&&a.z==z.z&&a.scaleX==z.scaleX&&a.scaleZ==z.scaleZ,"unexpected original P15 item migration");
            if(corners.Contains((a.x,a.z))&&a.kind==MapItemKind.Wall)
            {
                changes++;
                Need(a.rotation==1&&a.scaleY==1,"original corner unexpected");
                Need(z.kind==MapItemKind.Platform&&z.rotation==0&&z.scaleY==4,"owner corner source fix wrong");
            }
            else Need(a.kind==z.kind&&a.rotation==z.rotation&&a.scaleY==z.scaleY,"unrequested owner item changed index="+i);
        }
        Need(changes==4,"only the four source corner Wall entries should change");
        Need(d.Items.Count(p=>p.kind==MapItemKind.Wall)==41&&
             d.Items.Count(p=>p.kind==MapItemKind.Platform)==4,"corner/inner wall owner count");
        Need(d.Items.Count(p=>p.kind==MapItemKind.Ground)==120 &&
             d.Items.Count(p=>p.kind==MapItemKind.Pillar)==4 &&
             d.Items.Count(p=>p.kind==MapItemKind.LowBarrier)==2,
             "original cover/ground assembly altered");
        Need(d.Spawn.x==2&&d.Spawn.z==5&&d.Finish.x==9&&d.Finish.z==5,"markers moved");

        var shortRoute=Enumerable.Range(2,8).Select(x=>(x,5));
        var safe=new List<(int x,int z)>{(2,5),(2,4),(2,3),(2,2)};
        for(int x=3;x<=10;x++)safe.Add((x,2));
        safe.AddRange(new[]{(10,3),(10,4),(10,5),(9,5)});
        Walk(d,shortRoute,7);
        Walk(d,safe,15);

        int sourceClosedFaces=0;
        foreach(var (x,z) in corners)
        {
            var corner=d.Items.Single(p=>p.x==x&&p.z==z&&p.kind==MapItemKind.Platform);
            Need(corner.scaleY==4&&corner.scaleX==1&&corner.scaleZ==1,"not source 4x4x4 cube");
            var horizontal=d.Items.Single(p=>p.kind==MapItemKind.Wall&&p.x==(x==0?1:10)&&p.z==z);
            var vertical=d.Items.Single(p=>p.kind==MapItemKind.Wall&&p.x==x&&p.z==(z==0?1:8));
            Need(horizontal.rotation==0&&vertical.rotation==1,"adjacent source wall yaw changed");

            float cx=x*4f,cz=z*4f;
            float hx=horizontal.x*4f,vz=vertical.z*4f;
            // Platform cube original source covers x±2m/z±2m at full y0..4.
            // Wall original source covers its length ±2m at y0..4.
            Need(SourcePhysicalGap(cx-2,cx+2,hx-2,hx+2)==0,
                 "source X gap at owner-repaired corner");
            Need(SourcePhysicalGap(cz-2,cz+2,vz-2,vz+2)==0,
                 "source Z gap at owner-repaired corner");
            sourceClosedFaces+=2;
        }
        Need(sourceClosedFaces==8,"expected 8 face adjacency witnesses");
        Console.WriteLine("P15_B01_25_OWNER_NEW_SHA_ORIGINAL_CSHARP_TRYLOAD=PASS; FOUR_EXACT_CORNER_CHANGES_ONLY");
        Console.WriteLine("P15_B01_25_OWNER_REPAIRED_SOURCE_GEOMETRY=8_OF_8_ADJACENT_FACES_NO_GAP");
        Console.WriteLine("P15_B01_25_OWNER_REPAIRED_ROUTES=7_AND_15_EDGES_GROUND_CLEAR");
        Console.WriteLine("P15_B01_25_OWNER_REPAIRED_UNITY_PHYSX_NOT_RUN; B01_61_INDEPENDENT_8_POINT_RECEIVER_SEPARATE");
    }
}
