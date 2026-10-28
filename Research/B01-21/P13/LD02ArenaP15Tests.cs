// B01-21 PASS15: original immutable LD02_ARENA vs independently corrected original MapDocument v2.
// Executes ORIGINAL Codex MapDraft.TryLoad/TestError; uses source-pinned MapWorld
// geometry as an explicit mathematical collision-box oracle (NOT Unity/PhysX).
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using RoboRougelike.MapEditor;

internal static class LD02ArenaP15Tests
{
    readonly record struct Box(float MinX,float MaxX,float MinZ,float MaxZ,float MinY,float MaxY);
    static void Assert(bool value,string msg)
    {
        if (!value) throw new InvalidOperationException(msg);
    }
    static MapDraft Load(string path)
    {
        var d=new MapDraft();
        Assert(d.TryLoad(File.ReadAllText(path),out var error),"Original MapDraft.TryLoad rejected "+path+" / "+error);
        Assert(d.TestError()==null,"Original MapDraft.TestError: "+path+" / "+d.TestError());
        Assert(!d.GlobalFloor && d.Items.Count==168,"Arena header and item count");
        Assert(d.Items.Count(x=>x.kind==MapItemKind.Ground)==120,"Original 120 local Ground");
        Assert(d.Items.Count(x=>x.kind==MapItemKind.Spawn)==1 &&
               d.Spawn.x==2 && d.Spawn.z==5,"Original Spawn moved");
        Assert(d.Items.Count(x=>x.kind==MapItemKind.Finish)==1 &&
               d.Finish.x==9 && d.Finish.z==5,"Original Finish moved");
        return d;
    }
    // Exact unscaled source shapes in pinned MapWorld.cs:
    // Wall Cube position (0,2,0), size (Grid=4,4,.5), yaw 0/90.
    // Platform Cube position (0,.5,0), size (4,1,4), with item scaleY=4.
    // This is a source geometry oracle only; NOT actual Unity Collider.bounds.
    static Box SourceBox(MapItem m)
    {
        float x=4*m.x,z=4*m.z;
        if(m.kind==MapItemKind.Wall)
        {
            Assert(m.scaleX==1&&m.scaleY==1&&m.scaleZ==1,"unexpected scaled Wall");
            Assert(m.rotation==0||m.rotation==1,"unexpected Wall yaw");
            return m.rotation==0 ?
                new Box(x-2,x+2,z-.25f,z+.25f,0,4):
                new Box(x-.25f,x+.25f,z-2,z+2,0,4);
        }
        Assert(m.kind==MapItemKind.Platform && m.rotation==0 &&
               m.scaleX==1 && m.scaleY==4 && m.scaleZ==1,
               "corner is not a 4x4x4 source-solid Platform");
        return new Box(x-2,x+2,z-2,z+2,0,4);
    }
    static float Gap(Box a,Box b)
    {
        var dx=Math.Max(0,Math.Max(a.MinX,b.MinX)-Math.Min(a.MaxX,b.MaxX));
        var dz=Math.Max(0,Math.Max(a.MinZ,b.MinZ)-Math.Min(a.MaxZ,b.MaxZ));
        return (float)Math.Sqrt(dx*dx+dz*dz);
    }
    static bool Connected(Box a,Box b)
    {
        // Collider AABB faces touching count as topologically closed source geometry.
        // Actual BoxCollider contact/penetration still requires PhysX acceptance.
        const float eps=.0001f;
        bool xTouch=a.MaxX+eps>=b.MinX && b.MaxX+eps>=a.MinX;
        bool zTouch=a.MaxZ+eps>=b.MinZ && b.MaxZ+eps>=a.MinZ;
        return xTouch&&zTouch;
    }
    static int Components(IReadOnlyList<Box> list)
    {
        var known=new bool[list.Count];int count=0;
        for(int i=0;i<list.Count;i++)
        {
            if(known[i])continue;
            count++;known[i]=true;
            var queue=new Queue<int>();queue.Enqueue(i);
            while(queue.Count>0)
            {
                int j=queue.Dequeue();
                for(int k=0;k<list.Count;k++)
                    if(!known[k]&&Connected(list[j],list[k]))
                        {known[k]=true;queue.Enqueue(k);}
            }
        }
        return count;
    }
    public static void Run()
    {
        var old=Load("Research/B01-25/P14/LD02_ARENA.json");
        var fixedMap=Load("Research/B01-21/P15/LD02_ARENA_SEALED_CORNERS.json");
        Assert(old.Items.Count==fixedMap.Items.Count,"No size change permitted");
        var corners=new HashSet<string>{"0,0","0,9","11,0","11,9"};
        for(int i=0;i<old.Items.Count;i++)
        {
            var a=old.Items[i];var f=fixedMap.Items[i];
            string cell=a.x+","+a.z;
            if(corners.Contains(cell)&&a.kind==MapItemKind.Wall)
            {
                Assert(f.kind==MapItemKind.Platform &&
                       f.x==a.x && f.z==a.z &&
                       f.rotation==0 && f.scaleX==1&&f.scaleY==4&&f.scaleZ==1,
                       "Unexpected corner correction");
            }
            else Assert(a.kind==f.kind&&a.x==f.x&&a.z==f.z&&
                        a.rotation==f.rotation&&a.scaleX==f.scaleX&&
                        a.scaleY==f.scaleY&&a.scaleZ==f.scaleZ,
                        "Unexpected non-corner asset change at item "+i);
        }
        var oldWall=old.Items.Where(p=>p.kind==MapItemKind.Wall).Select(SourceBox).ToArray();
        var newRing=fixedMap.Items.Where(p=>p.kind==MapItemKind.Wall||
                                                  p.kind==MapItemKind.Platform).Select(SourceBox).ToArray();
        Assert(oldWall.Length==40&&newRing.Length==40,"perimeter count changed");
        Assert(fixedMap.Items.Count(p=>p.kind==MapItemKind.Wall)==36 &&
               fixedMap.Items.Count(p=>p.kind==MapItemKind.Platform)==4,
               "corner replacement counts");
        Assert(Components(oldWall)==4,"Prior source wall perimeter unexpectedly has connected corners");
        Assert(Components(newRing)==1,"Corrected source 40-solid ring not connected");
        foreach(var (x,z) in new[]{(0,0),(11,0),(0,9),(11,9)})
        {
            var left=old.Items.Single(p=>p.kind==MapItemKind.Wall&&p.x==x&&p.z==z);
            var correction=fixedMap.Items.Single(p=>p.kind==MapItemKind.Platform&&p.x==x&&p.z==z);
            var horizontal=old.Items.Single(p=>p.kind==MapItemKind.Wall&&
                           p.x==(x==0?1:10)&&p.z==z);
            var vertical=old.Items.Single(p=>p.kind==MapItemKind.Wall&&
                           p.x==x&&p.z==(z==0?1:8));
            Assert(Math.Abs(Gap(SourceBox(left),SourceBox(horizontal))-1.75f)<.0001f,
                   "Original physical source corner gap not 1.75m");
            Assert(Gap(SourceBox(correction),SourceBox(horizontal))==0,
                   "New corner doesn't close horizontal source gap");
            Assert(Gap(SourceBox(correction),SourceBox(vertical))==0,
                   "New corner doesn't close vertical source gap");
        }
        Console.WriteLine("P15_SOURCE_GEOMETRY_OLD=4_COMPONENTS_AND_4_CORNERS_AT_1.75m");
        Console.WriteLine("P15_SOURCE_GEOMETRY_FIXED=1_COMPONENT_AND_4_CORNER_CUBES_4x4x4m");
        Console.WriteLine("P15_ORIGINAL_MAPDRAFT_TRYLOAD_BOTH=PASS; UNITY_PHYSX_NOT_RUN");
    }
}
