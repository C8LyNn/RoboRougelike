// B01-21 PASS15 continuation: execute UNMODIFIED original Codex MapDraft.cs
// against new, exact original B01-25 LD02_COVER_CHOICE.json Git blob.
// LOS geometry uses pinned original MapWorld.cs Wall box dimensions
// only (no Unity Physics/PhysX, no AI, no real vehicle clearance).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoboRougelike.MapEditor;

internal static class LD02CoverChoiceP15Tests
{
    static void Need(bool b,string why)
    {
        if(!b)throw new InvalidOperationException(why);
    }
    static bool SegmentMeetsSourceWall(float ax,float az,float bx,float bz,MapItem w)
    {
        Need(w.kind==MapItemKind.Wall,"not wall");
        Need(w.scaleX==1 && w.scaleY==1 && w.scaleZ==1,"scaled wall shape not in pinned source contract");
        Need(w.rotation==0||w.rotation==1,"unhandled wall yaw");
        float cx=w.x*4f,cz=w.z*4f;
        float rx=w.rotation==0?2f:.25f,rz=w.rotation==0?.25f:2f;
        float tMin=0f,tMax=1f;
        // Finite segment and axis aligned solid 4×0.5 source rectangle (not Unity Raycast).
        foreach(var bounds in new[]{(a:ax,b:bx,min:cx-rx,max:cx+rx),(a:az,b:bz,min:cz-rz,max:cz+rz)})
        {
            float diff=bounds.b-bounds.a;
            if(Math.Abs(diff)<1e-6f)
            {
                if(bounds.a<bounds.min||bounds.a>bounds.max)return false;
                continue;
            }
            float u=(bounds.min-bounds.a)/diff,v=(bounds.max-bounds.a)/diff;
            if(u>v)(u,v)=(v,u);
            tMin=Math.Max(tMin,u);
            tMax=Math.Min(tMax,v);
            if(tMax<tMin)return false;
        }
        return tMax>=tMin && tMin<1f && tMax>0f;
    }
    static int CountWallOccluders(IReadOnlyList<MapItem> walls,int x,int z)
        =>walls.Count(w=>SegmentMeetsSourceWall(9*4f,6*4f,x*4f,z*4f,w));
    static void VerifyRoute(MapDraft d,(int x,int z)[] path,int transitions)
    {
        Need(path.Length-1==transitions,"wrong route step count");
        Need(path[0].x==d.Spawn.x&&path[0].z==d.Spawn.z,"wrong route start");
        Need(path[path.Length-1].x==d.Finish.x&&path[path.Length-1].z==d.Finish.z,"wrong route finish");
        var ground=new HashSet<UnityEngine.Vector2Int>(
            d.Items.Where(p=>p.kind==MapItemKind.Ground).SelectMany(MapDraft.Footprint));
        var occupied=new HashSet<UnityEngine.Vector2Int>(
            d.Items.Where(p=>p.kind!=MapItemKind.Ground
                          &&p.kind!=MapItemKind.Spawn
                          &&p.kind!=MapItemKind.Finish).SelectMany(MapDraft.Footprint));
        for(int i=0;i<path.Length;i++)
        {
            var (x,z)=path[i];
            var cell=new UnityEngine.Vector2Int(x,z);
            Need(ground.Contains(cell),"route left authored local ground at ("+x+","+z+")");
            Need(!occupied.Contains(cell),"route crosses logical obstacle at ("+x+","+z+")");
            if(i>0)
                Need(Math.Abs(x-path[i-1].x)+Math.Abs(z-path[i-1].z)==1,
                    "non-adjacent route waypoints");
        }
    }
    public static void Run()
    {
        const string file="Research/B01-25/P15/LD02_COVER_CHOICE.json";
        var d=new MapDraft();
        Need(d.TryLoad(File.ReadAllText(file),out string error),"original MapDraft.TryLoad rejected map: "+error);
        Need(d.TestError()==null,"original MapDraft.TestError not null: "+d.TestError());
        Need(!d.GlobalFloor&&d.Items.Count==173,"wrong original P15 metadata");
        foreach(var (kind,num) in new[]{
            (MapItemKind.Ground,120),(MapItemKind.Wall,45),
            (MapItemKind.Pillar,4),(MapItemKind.LowBarrier,2),
            (MapItemKind.Spawn,1),(MapItemKind.Finish,1)})
            Need(d.Items.Count(v=>v.kind==kind)==num,"kind count mismatch: "+kind);
        Need(d.Spawn.x==2&&d.Spawn.z==5&&d.Finish.x==9&&d.Finish.z==5,"wrong original markers");

        var shortPath=Enumerable.Range(2,8).Select(x=>(x,5)).ToArray();
        var safe=new List<(int x,int z)>{(2,5),(2,4),(2,3),(2,2)};
        for(int x=3;x<=10;x++)safe.Add((x,2));
        safe.Add((10,3));safe.Add((10,4));safe.Add((10,5));safe.Add((9,5));
        VerifyRoute(d,shortPath,7);VerifyRoute(d,safe.ToArray(),15);

        var wall=d.Items.Where(p=>p.kind==MapItemKind.Wall).ToArray();
        var interior=wall.Where(p=>p.z==4&&p.x>=4&&p.x<=8).ToArray();
        Need(interior.Length==5,"Five contiguous inner Wall pieces expected");
        for(int x=4;x<=8;x++)Need(interior.Count(w=>w.x==x)==1,"Gap in internal wall source chain");
        int hidden=0,visible=0;
        for(int x=3;x<=7;x++){Need(CountWallOccluders(wall,x,2)>0,"unshielded north detour source point x="+x);hidden++;}
        for(int x=3;x<=8;x++){Need(CountWallOccluders(wall,x,5)==0,"short direct branch unexpectedly screened x="+x);visible++;}
        Need(hidden==5&&visible==6,"expected original LOS witness control cardinality");

        // NEW independent guard: original P15 still contains old four corner Wall entries.
        // Do not mark B01-25 owner FIX green until they publish a NEW input commit.
        int openCornerCells=0;
        foreach(var (x,z) in new[]{(0,0),(0,9),(11,0),(11,9)})
        {
            var w=wall.Single(p=>p.x==x&&p.z==z);
            Need(w.rotation==1,"source geometry corner arrangement changed: rebaseline");
            openCornerCells++;
        }
        Need(openCornerCells==4,"original P15 prior corner gap assessment changed");

        Console.WriteLine("P15_COVER_ORIGINAL_MAPDRAFT_TRYLOAD=PASS; ORIGINAL_ITEMS=173; SHORT_TRANSITIONS=7; DETOUR_TRANSITIONS=15");
        Console.WriteLine("P15_COVER_SOURCE_OCCLUSION=5_SHIELDED_DETOUR+6_EXPOSED_SHORT");
        Console.WriteLine("P15_COVER_OWNER_CORNER_FIX_PENDING=4_OLD_WALL_CORNERS");
        Console.WriteLine("P15_COVER_PHYSX_AND_ENEMY_AI_NOT_RUN");
    }
}
