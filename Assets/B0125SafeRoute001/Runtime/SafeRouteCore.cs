using System;
using System.Collections.Generic;

namespace B01.LevelChallenges.SafeRoute
{
    public readonly struct GridCell : IEquatable<GridCell>
    {
        public readonly int X,Z;
        public GridCell(int x,int z){X=x;Z=z;}
        public bool Equals(GridCell other)=>X==other.X&&Z==other.Z;
        public override bool Equals(object obj)=>obj is GridCell c&&Equals(c);
        public override int GetHashCode()=>unchecked((X*397)^Z);
        public override string ToString()=>X+","+Z;
    }
    public readonly struct RouteConditions
    {
        public readonly bool GateOpen,HazardActive;
        public RouteConditions(bool gateOpen,bool hazardActive)
        {GateOpen=gateOpen;HazardActive=hazardActive;}
    }
    public sealed class LevelGrid
    {
        private readonly char[] tiles;
        public int Width{get;}
        public int Height{get;}
        public GridCell Start{get;}
        public GridCell Exit{get;}
        private LevelGrid(int w,int h,char[] data,GridCell start,GridCell exit)
        {Width=w;Height=h;tiles=data;Start=start;Exit=exit;}
        public bool Contains(int x,int z)=>x>=0&&z>=0&&x<Width&&z<Height;
        public char At(int x,int z)
        {
            if(!Contains(x,z))throw new ArgumentOutOfRangeException("Outside level bounds");
            return tiles[z*Width+x];
        }
        public static LevelGrid Parse(params string[] rows)
        {
            if(rows==null||rows.Length<3||rows.Length>64||
               rows[0]==null||rows[0].Length<3||rows[0].Length>64)
                throw new ArgumentException("Grid dimensions must be finite and bounded");
            int w=rows[0].Length,h=rows.Length;
            if((long)w*h>4096)throw new ArgumentException("Grid cell budget exceeded");
            char[] tiles=new char[w*h];
            GridCell start=default,exit=default;
            int ns=0,ne=0;
            for(int z=0;z<h;z++)
            {
                if(rows[z]==null||rows[z].Length!=w)
                    throw new ArgumentException("Nonrectangular map");
                for(int x=0;x<w;x++)
                {
                    char tile=rows[z][x];
                    if(tile!='#'&&tile!='.'&&tile!='S'&&tile!='E'&&
                       tile!='G'&&tile!='H')
                        throw new ArgumentException("Invalid cell character");
                    if(tile=='S'){ns++;start=new GridCell(x,z);}
                    if(tile=='E'){ne++;exit=new GridCell(x,z);}
                    tiles[z*w+x]=tile;
                }
            }
            if(ns!=1||ne!=1)throw new ArgumentException("Exactly one spawn and exit required");
            return new LevelGrid(w,h,tiles,start,exit);
        }
    }
    public sealed class SafeRoutePlan
    {
        public readonly bool Reachable;
        public readonly GridCell[] Cells;
        public readonly int NodesExplored;
        internal SafeRoutePlan(bool ok,GridCell[] cells,int explored)
        {Reachable=ok;Cells=cells;NodesExplored=explored;}
    }
    public readonly struct RouteAuditCase
    {
        public readonly int FootprintRadius;
        public readonly RouteConditions Conditions;
        public readonly bool Reachable;
        public readonly int Steps;
        public RouteAuditCase(int radius,RouteConditions conditions,bool reachable,int steps)
        {FootprintRadius=radius;Conditions=conditions;Reachable=reachable;Steps=steps;}
    }
    public sealed class RouteAudit
    {
        public readonly RouteAuditCase[] Cases;
        public readonly bool AllReachable;
        internal RouteAudit(RouteAuditCase[] cases,bool all)
        {Cases=cases;AllReachable=all;}
    }
    /// <summary>
    /// Actual deterministic four-neighbor BFS used by the Unity runtime guide.
    /// Conservative SQUARE clearance for Light radius 0 and Heavy radius 1.
    /// Traversal never assumes movable doors are open, nor that a hazardous
    /// conveyor is safe while active. No Unity dependency or canonical save.
    /// </summary>
    public static class SafeRoutePlanner
    {
        private static readonly int[] DX={0,1,0,-1};
        private static readonly int[] DZ={1,0,-1,0};
        public static bool IsClear(LevelGrid map,GridCell c,int radius,RouteConditions condition)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            if(radius<0||radius>2)throw new ArgumentOutOfRangeException(nameof(radius));
            for(int dz=-radius;dz<=radius;dz++)
            for(int dx=-radius;dx<=radius;dx++)
            {
                int x=c.X+dx,z=c.Z+dz;
                if(!map.Contains(x,z))return false;
                char tile=map.At(x,z);
                if(tile=='#'||(tile=='G'&&!condition.GateOpen)||
                  (tile=='H'&&condition.HazardActive))return false;
            }
            return true;
        }
        public static SafeRoutePlan Find(LevelGrid map,int radius,RouteConditions condition)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            if(radius<0||radius>2)throw new ArgumentOutOfRangeException(nameof(radius));
            if(!IsClear(map,map.Start,radius,condition)||
               !IsClear(map,map.Exit,radius,condition))
                return new SafeRoutePlan(false,Array.Empty<GridCell>(),0);
            int total=checked(map.Width*map.Height);
            bool[] seen=new bool[total];
            int[] parent=new int[total];
            Array.Fill(parent,-1);
            int[] queue=new int[total];
            int head=0,tail=0;
            int origin=map.Start.Z*map.Width+map.Start.X;
            int end=map.Exit.Z*map.Width+map.Exit.X;
            seen[origin]=true;queue[tail++]=origin;
            while(head<tail)
            {
                int id=queue[head++];
                if(id==end)
                {
                    var path=new List<GridCell>();
                    int cur=end;
                    while(cur!=-1)
                    {
                        path.Add(new GridCell(cur%map.Width,cur/map.Width));
                        if(path.Count>total)throw new InvalidOperationException("Parent loop");
                        cur=parent[cur];
                    }
                    path.Reverse();
                    return new SafeRoutePlan(true,path.ToArray(),head);
                }
                int cx=id%map.Width,cz=id/map.Width;
                for(int i=0;i<4;i++)
                {
                    int x=cx+DX[i],z=cz+DZ[i];
                    if(!map.Contains(x,z))continue;
                    int next=z*map.Width+x;
                    if(seen[next]||!IsClear(map,new GridCell(x,z),radius,condition))continue;
                    seen[next]=true;parent[next]=id;queue[tail++]=next;
                }
            }
            return new SafeRoutePlan(false,Array.Empty<GridCell>(),head);
        }
        public static RouteAudit Audit(LevelGrid map)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            var cases=new RouteAuditCase[8];
            int i=0;
            bool all=true;
            for(int radius=0;radius<=1;radius++)
            for(int gate=0;gate<=1;gate++)
            for(int hazard=0;hazard<=1;hazard++)
            {
                var c=new RouteConditions(gate==1,hazard==1);
                var r=Find(map,radius,c);
                cases[i++]=new RouteAuditCase(radius,c,r.Reachable,r.Cells.Length);
                if(!r.Reachable)all=false;
            }
            return new RouteAudit(cases,all);
        }
    }
}
