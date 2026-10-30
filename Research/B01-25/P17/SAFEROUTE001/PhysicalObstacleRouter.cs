using System;
using System.Collections.Generic;

namespace B01.LevelChallenges.SafeRoute
{
    /// <summary>
    /// R3 actual physical moving-collider occupancy supplement. R1/R2 frozen
    /// planner remains unchanged, this validated source is a superset for
    /// current-world obstacles. NEVER treats physical blockers as walkable.
    /// </summary>
    public static class PhysicalObstacleRouter
    {
        private static readonly int[] DX={0,1,0,-1};
        private static readonly int[] DZ={1,0,-1,0};
        private static bool[] Snapshot(LevelGrid map,IReadOnlyCollection<GridCell> blockers)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            if(blockers==null)throw new ArgumentNullException(nameof(blockers));
            int cap=checked(map.Width*map.Height);
            if(blockers.Count>cap)throw new ArgumentException("Physical blocker budget exceeded");
            var mask=new bool[cap];
            foreach(var p in blockers)
            {
                if(!map.Contains(p.X,p.Z))throw new ArgumentException("Physical blocker outside level");
                mask[p.Z*map.Width+p.X]=true;
            }
            return mask;
        }
        private static bool Clear(LevelGrid map,GridCell p,int radius,
            RouteConditions state,bool[] mask)
        {
            if(!SafeRoutePlanner.IsClear(map,p,radius,state))return false;
            for(int dz=-radius;dz<=radius;dz++)
            for(int dx=-radius;dx<=radius;dx++)
            {
                int x=p.X+dx,z=p.Z+dz;
                if(mask[z*map.Width+x])return false;
            }
            return true;
        }
        public static SafeRoutePlan FindFrom(LevelGrid map,GridCell origin,int radius,
            RouteConditions state,IReadOnlyCollection<GridCell> blockers)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            if(radius<0||radius>2)throw new ArgumentOutOfRangeException(nameof(radius));
            // Validate the entire physical occupancy snapshot even if the route
            // later fails. No partial accepting unsafe/out-of-bounds telemetry.
            bool[] mask=Snapshot(map,blockers);
            if(!Clear(map,origin,radius,state,mask)||
               !Clear(map,map.Exit,radius,state,mask))
                return new SafeRoutePlan(false,Array.Empty<GridCell>(),0);
            int cap=checked(map.Width*map.Height);
            var visited=new bool[cap];var parent=new int[cap];
            Array.Fill(parent,-1);
            var queue=new int[cap];int head=0,tail=0;
            int source=origin.Z*map.Width+origin.X;
            int goal=map.Exit.Z*map.Width+map.Exit.X;
            queue[tail++]=source;visited[source]=true;
            while(head<tail)
            {
                int here=queue[head++];
                if(here==goal)
                {
                    var nodes=new List<GridCell>();
                    for(int at=goal;at!=-1;at=parent[at])
                    {
                        nodes.Add(new GridCell(at%map.Width,at/map.Width));
                        if(nodes.Count>cap)throw new InvalidOperationException("Parent cycle");
                    }
                    nodes.Reverse();
                    return new SafeRoutePlan(true,nodes.ToArray(),head);
                }
                int px=here%map.Width,pz=here/map.Width;
                for(int i=0;i<4;i++)
                {
                    int nx=px+DX[i],nz=pz+DZ[i];
                    if(!map.Contains(nx,nz))continue;
                    int next=nz*map.Width+nx;
                    if(visited[next]||
                       !Clear(map,new GridCell(nx,nz),radius,state,mask))continue;
                    visited[next]=true;parent[next]=here;queue[tail++]=next;
                }
            }
            return new SafeRoutePlan(false,Array.Empty<GridCell>(),head);
        }
        public static RouteAudit AuditAllEight(LevelGrid map,
            IReadOnlyCollection<GridCell> blockers)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            // All eight real Light/Heavy + gate/hazard states, not only
            // the current easy configuration; NO_PATH is explicit.
            Snapshot(map,blockers);
            var cases=new RouteAuditCase[8];int index=0;bool safe=true;
            for(int r=0;r<=1;r++)
            for(int gate=0;gate<=1;gate++)
            for(int hazard=0;hazard<=1;hazard++)
            {
                var state=new RouteConditions(gate==1,hazard==1);
                var route=FindFrom(map,map.Start,r,state,blockers);
                cases[index++]=new RouteAuditCase(r,state,route.Reachable,route.Cells.Length);
                if(!route.Reachable)safe=false;
            }
            return new RouteAudit(cases,safe);
        }
    }
}
