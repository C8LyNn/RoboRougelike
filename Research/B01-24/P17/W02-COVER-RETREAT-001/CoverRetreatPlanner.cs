// B01-24 W02 COVER-RETREAT-001: independent, runnable .NET8 enemy low-HP COVER RETREAT.
// This is an advisory tactic and data tool, NOT integrated Codex enemy AI or approved HP balance.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace B0124.CoverRetreat;

public sealed class CoverCatalog {
    public string Schema { get; set; } = "";
    public OriginalSource GameSource { get; set; } = new();
    public WorldFrame World { get; set; } = new();
    public ArenaCover[] Maps { get; set; } = Array.Empty<ArenaCover>();
}
public sealed class OriginalSource {
    public string ArenaPath { get; set; } = "";
    public string ArenaBlob { get; set; } = "";
    public string EnemyPath { get; set; } = "";
    public string EnemyBlob { get; set; } = "";
}
public sealed class WorldFrame {
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float MinZ { get; set; }
    public float MaxZ { get; set; }
    public float ClearanceRadius { get; set; }
    public float GridStep { get; set; }
    public Point2 Player { get; set; } = new();
}
public class Point2 {
    public float X { get; set; }
    public float Z { get; set; }
    public Vector2 V => new(X,Z);
}
public sealed class CoverSite : Point2 {
    public string Id { get; set; } = "";
}
public sealed class StaticBox {
    public string Id { get; set; } = "";
    public float X { get; set; }
    public float Z { get; set; }
    public float W { get; set; }
    public float H { get; set; }
}
public sealed class ArenaCover {
    public string Id { get; set; } = "";
    public int ArenaKind { get; set; }
    public CoverSite[] CoverSites { get; set; } = Array.Empty<CoverSite>();
    public StaticBox[] Obstacles { get; set; } = Array.Empty<StaticBox>();
}
public sealed class RetreatAnswer {
    public string Mode { get; set; } = "";
    public string Reason { get; set; } = "";
    public bool RetreatActive { get; set; }
    public string CoverId { get; set; } = "";
    public Vector2[] Route { get; set; } = Array.Empty<Vector2>();
    public float RouteMetres { get; set; }
}
public static class RetreatPlanner {
    private const float Eps = 0.00001f;
    public static CoverCatalog Read(string json) {
        var settings=new JsonSerializerOptions { PropertyNameCaseInsensitive=true };
        return JsonSerializer.Deserialize<CoverCatalog>(json,settings)
            ?? throw new ArgumentException("Missing cover catalog");
    }
    private static bool Finite(float v)=>float.IsFinite(v);
    private static void VerifyWorld(WorldFrame w) {
        if(!Finite(w.MinX)||!Finite(w.MinZ)||!Finite(w.MaxX)||!Finite(w.MaxZ)||
           w.MinX>=w.MaxX||w.MinZ>=w.MaxZ||!Finite(w.ClearanceRadius)||
           w.ClearanceRadius<1.5f||w.ClearanceRadius>4f||
           !Finite(w.GridStep)||MathF.Abs(w.GridStep-2f)>Eps||
           !Finite(w.Player.X)||!Finite(w.Player.Z))
            throw new ArgumentException("Unsafe or malformed world frame");
    }
    public static bool ClearSegment(ArenaCover arena,WorldFrame world,Vector2 a,Vector2 b,float radius) {
        if(!ValidPoint(world,a,radius)||!ValidPoint(world,b,radius))return false;
        return !arena.Obstacles.Any(box=>HitsBox(a,b,box,radius));
    }
    public static bool ValidPoint(WorldFrame world,Vector2 p,float radius) {
        return Finite(p.X)&&Finite(p.Y)&&Finite(radius)&&radius>=0&&
            p.X>=world.MinX+radius-Eps&&p.X<=world.MaxX-radius+Eps&&
            p.Y>=world.MinZ+radius-Eps&&p.Y<=world.MaxZ-radius+Eps;
    }
    // Conservative swept AABB (square clearance around a circular car).
    // Never treats a diagonal/tangent as a safe penetration.
    public static bool HitsBox(Vector2 a,Vector2 b,StaticBox box,float radius) {
        double near=0,far=1;
        for(int axis=0;axis<2;axis++) {
            double p=axis==0?a.X:a.Y,q=axis==0?b.X:b.Y;
            double center=axis==0?box.X:box.Z, half=(axis==0?box.W:box.H)/2.0+radius;
            double speed=q-p,low=center-half,high=center+half;
            if(Math.Abs(speed)<1e-10){if(p<low||p>high)return false;continue;}
            double u=(low-p)/speed,v=(high-p)/speed;
            near=Math.Max(near,Math.Min(u,v));far=Math.Min(far,Math.Max(u,v));
            if(near>far)return false;
        }
        return far>=0&&near<=1;
    }
    public static bool BehindCover(ArenaCover arena,Vector2 player,Vector2 cover) {
        return arena.Obstacles.Skip(4).Any(box=>HitsBox(player,cover,box,0f));
    }
    public static void Validate(CoverCatalog c) {
        if(c.Schema!="B01-24-COVER-RETREAT-001-v1"||c.Maps==null||c.Maps.Length!=3)
            throw new ArgumentException("Unexpected catalog schema/arena count");
        VerifyWorld(c.World);
        var seen=new HashSet<int>();
        foreach(var a in c.Maps) {
            if(a.ArenaKind<0||a.ArenaKind>2||!seen.Add(a.ArenaKind)||a.Obstacles==null||
               a.CoverSites==null||a.CoverSites.Length<2||a.CoverSites.Length>6||
               a.Obstacles.Length<6)throw new ArgumentException("Invalid arena coverage");
            var siteIds=new HashSet<string>();
            foreach(var box in a.Obstacles)
                if(!Finite(box.X)||!Finite(box.Z)||!Finite(box.W)||!Finite(box.H)||
                   box.W<=0||box.H<=0)throw new ArgumentException("Invalid static R5 box");
            foreach(var site in a.CoverSites) {
                var v=site.V;
                if(string.IsNullOrWhiteSpace(site.Id)||!siteIds.Add(site.Id)||
                   !ClearSegment(a,c.World,v,v,c.World.ClearanceRadius)||
                   !BehindCover(a,c.World.Player.V,v))
                    throw new ArgumentException("Invalid or exposed retreat site: "+site.Id);
            }
        }
    }
    public static void VerifyGitBlob(string repositoryRoot,string relativePath,string sha) {
        if(string.IsNullOrWhiteSpace(sha)||sha.Length!=40)throw new ArgumentException("Missing original Git SHA");
        var bytes=File.ReadAllBytes(Path.Combine(repositoryRoot,relativePath));
        var head=Encoding.ASCII.GetBytes("blob "+bytes.Length+"\0");
        var buff=new byte[head.Length+bytes.Length];
        Buffer.BlockCopy(head,0,buff,0,head.Length);
        Buffer.BlockCopy(bytes,0,buff,head.Length,bytes.Length);
        var got=Convert.ToHexString(SHA1.HashData(buff)).ToLowerInvariant();
        if(!string.Equals(got,sha,StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("SOURCE_DRIFT "+relativePath+" expected "+sha+" got "+got);
    }
    private static Vector2 GridPoint(int idx,int width,float step) {
        int x=idx%width,z=idx/width;
        return new Vector2(-38+x*step,-38+z*step);
    }
    // Deterministic 8-neighbour A*, with full swept-box edges and no corner-cut.
    // Route includes exact world start and exact site, not only snapped grid nodes.
    public static Vector2[] Route(ArenaCover arena,WorldFrame world,Vector2 start,Vector2 goal) {
        float radius=world.ClearanceRadius, step=world.GridStep;
        if(!ClearSegment(arena,world,start,start,radius)||
           !ClearSegment(arena,world,goal,goal,radius))return Array.Empty<Vector2>();
        const int width=39,count=width*width;
        var free=new bool[count];
        for(int i=0;i<count;i++) {
            var p=GridPoint(i,width,step);
            free[i]=ClearSegment(arena,world,p,p,radius);
        }
        int Nearest(Vector2 p) {
            int best=-1;
            float distance=float.PositiveInfinity;
            for(int i=0;i<count;i++){
                if(!free[i])continue;
                var node=GridPoint(i,width,step);
                float d=Vector2.DistanceSquared(node,p);
                if(d<distance&&ClearSegment(arena,world,p,node,radius)){
                    distance=d;best=i;
                }
            }
            return best;
        }
        int src=Nearest(start), dst=Nearest(goal);
        if(src<0||dst<0)return Array.Empty<Vector2>();
        var g=Enumerable.Repeat(float.PositiveInfinity,count).ToArray();
        var parent=Enumerable.Repeat(-1,count).ToArray();
        var closed=new bool[count];
        var pending=new PriorityQueue<int,(float Score,int Tie)>();
        g[src]=0;pending.Enqueue(src,(Vector2.Distance(GridPoint(src,width,step),GridPoint(dst,width,step)),src));
        int[] dx={0,1,0,-1,1,1,-1,-1}, dz={1,0,-1,0,1,-1,1,-1};
        while(pending.Count>0){
            int at=pending.Dequeue();
            if(closed[at])continue;closed[at]=true;
            if(at==dst)break;
            int ix=at%width,iz=at/width;
            for(int d=0;d<8;d++){
                int jx=ix+dx[d],jz=iz+dz[d];
                if(jx<0||jx>=width||jz<0||jz>=width)continue;
                int next=jz*width+jx;
                if(!free[next]||closed[next])continue;
                var a=GridPoint(at,width,step), b=GridPoint(next,width,step);
                if(!ClearSegment(arena,world,a,b,radius))continue;
                float candidate=g[at]+Vector2.Distance(a,b);
                if(candidate+Eps>=g[next])continue;
                g[next]=candidate;parent[next]=at;
                float h=Vector2.Distance(b,GridPoint(dst,width,step));
                pending.Enqueue(next,(candidate+h,next));
            }
        }
        if(!float.IsFinite(g[dst]))return Array.Empty<Vector2>();
        var route=new List<Vector2>{goal};int cursor=dst;
        for(int n=0;n<=count;n++){
            route.Add(GridPoint(cursor,width,step));
            if(cursor==src)break;
            cursor=parent[cursor];
            if(cursor<0)return Array.Empty<Vector2>();
        }
        route.Add(start);route.Reverse();
        // Fail closed if any exact segment crosses an expanded box.
        for(int i=1;i<route.Count;i++)
            if(!ClearSegment(arena,world,route[i-1],route[i],radius))
                throw new InvalidOperationException("A* emitted penetrative segment");
        return route.ToArray();
    }
    public static float RouteLength(IReadOnlyList<Vector2> points) {
        float metres=0;
        for(int i=1;i<points.Count;i++)metres+=Vector2.Distance(points[i-1],points[i]);
        return metres;
    }
    public static RetreatAnswer Evaluate(CoverCatalog c,int arenaKind,Vector2 enemy,float healthFraction,
        bool mobilityOnline,bool weaponOnline,bool previousRetreat,float enterFraction=.35f,float exitFraction=.60f) {
        Validate(c);
        if(!Finite(healthFraction)||healthFraction<0||healthFraction>1||
           !Finite(enterFraction)||!Finite(exitFraction)||enterFraction<=0||
           enterFraction>=exitFraction||exitFraction>1||!ValidPoint(c.World,enemy,0))
            throw new ArgumentException("Invalid health/hysteresis/location");
        if(!mobilityOnline)return new RetreatAnswer{Mode="HOLD",Reason="DRIVE_OFFLINE"};
        if(!weaponOnline)return new RetreatAnswer{Mode="DEFER_TO_CODEX_RAM",Reason="WEAPON_OFFLINE"};
        bool retreat=previousRetreat?healthFraction<exitFraction:healthFraction<=enterFraction;
        if(!retreat)return new RetreatAnswer{Mode="NORMAL_COMBAT",Reason=previousRetreat?"RECOVERED":"HEALTHY"};
        var arena=c.Maps.Single(x=>x.ArenaKind==arenaKind);
        string bestId="";Vector2[] best=Array.Empty<Vector2>();float score=float.PositiveInfinity;
        foreach(var cover in arena.CoverSites){
            var route=Route(arena,c.World,enemy,cover.V);
            if(route.Length==0)continue;
            float metres=RouteLength(route);
            if(metres+Eps<score){bestId=cover.Id;best=route;score=metres;}
        }
        return best.Length==0
            ?new RetreatAnswer{Mode="HOLD",Reason="NO_REACHABLE_COVER",RetreatActive=true}
            :new RetreatAnswer{Mode="RETREAT_TO_COVER",Reason="LOW_HEALTH_ADVISORY",
                RetreatActive=true,CoverId=bestId,Route=best,RouteMetres=score};
    }
}
