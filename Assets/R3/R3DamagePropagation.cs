using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboRougelike.R3
{
    /// <summary>
    /// Deterministic accounting for a single hit on a snapshot of a vehicle's
    /// actual connection graph. This class does not mutate Unity objects or HP.
    /// </summary>
    public static class R3DamagePropagation
    {
        public enum DamageKind { Kinetic, Piercing, Blast }
        // Initial playtest values, not final weapon balance.
        public static float TransferFraction(DamageKind kind) =>
            kind==DamageKind.Piercing ? .10f : kind==DamageKind.Blast ? .40f : .25f;

        public readonly struct Node
        {
            public readonly int Id;
            public readonly float Health;
            public readonly int[] Neighbours;
            public Node(int id,float health,int[] neighbours)
            { Id=id;Health=health;Neighbours=neighbours??Array.Empty<int>(); }
        }

        public sealed class Result
        {
            public readonly Dictionary<int,float> Damage=new Dictionary<int,float>();
            public float Unused { get; internal set; }
        }

        // Both components travel through the same snapshot. Each keeps its
        // own transfer fraction and shares a part's remaining HP fairly;
        // callbacks are performed by the caller only after this result exists.
        public static Result ResolveHybrid(IEnumerable<Node> snapshot,int target,float kinetic,float blast,int maxHops)
        {
            if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));
            if(float.IsNaN(kinetic)||float.IsInfinity(kinetic)||kinetic<0)throw new ArgumentOutOfRangeException(nameof(kinetic));
            if(float.IsNaN(blast)||float.IsInfinity(blast)||blast<0)throw new ArgumentOutOfRangeException(nameof(blast));
            if(maxHops<0)throw new ArgumentOutOfRangeException(nameof(maxHops));
            var nodes=snapshot.ToDictionary(n=>n.Id);
            if(nodes.Values.Any(n=>float.IsNaN(n.Health)||float.IsInfinity(n.Health)||n.Health<0))
                throw new ArgumentException("Part health must be finite and nonnegative.",nameof(snapshot));
            double total=(double)kinetic+blast,scale=total>float.MaxValue?float.MaxValue/total:1d;
            var result=new Result();if(total<=0)return result;
            if(!nodes.TryGetValue(target,out var first)||first.Health<=0){result.Unused=(float)(total*scale);return result;}
            IEnumerable<int> Neighbours(Node node)=>node.Neighbours.Distinct().OrderBy(id=>id).Where(id=>
                id!=node.Id&&nodes.TryGetValue(id,out var other)&&other.Health>0&&Array.IndexOf(other.Neighbours,node.Id)>=0);
            var distance=new Dictionary<int,int>{{target,0}};var queue=new Queue<int>();queue.Enqueue(target);
            while(queue.Count>0)
            {
                int id=queue.Dequeue(),depth=distance[id];if(depth>=maxHops)continue;
                foreach(int next in Neighbours(nodes[id]))if(!distance.ContainsKey(next)){distance.Add(next,depth+1);queue.Enqueue(next);}
            }
            var pending=new Dictionary<int,(double kinetic,double blast)>{{target,(kinetic*scale,blast*scale)}};
            double unused=0;
            foreach(var entry in distance.OrderBy(p=>p.Value).ThenBy(p=>p.Key))
            {
                if(!pending.TryGetValue(entry.Key,out var received)||received.kinetic+received.blast<=0)continue;
                var node=nodes[entry.Key];var children=Neighbours(node).Where(id=>distance.TryGetValue(id,out int depth)&&depth==entry.Value+1).ToArray();
                double retainedK=children.Length==0?received.kinetic:received.kinetic*(1-TransferFraction(DamageKind.Kinetic));
                double retainedB=children.Length==0?received.blast:received.blast*(1-TransferFraction(DamageKind.Blast));
                double retained=retainedK+retainedB,applied=Math.Min(node.Health,retained);
                if(applied>0)result.Damage.Add(node.Id,(float)applied);
                double keep=retained>0?applied/retained:0;
                double nextK=Math.Max(0,received.kinetic-retainedK*keep),nextB=Math.Max(0,received.blast-retainedB*keep);
                if(children.Length==0)unused+=nextK+nextB;
                else foreach(int child in children)
                {
                    pending.TryGetValue(child,out var old);
                    pending[child]=(old.kinetic+nextK/children.Length,old.blast+nextB/children.Length);
                }
            }
            foreach(int depth in distance.Values.Distinct().OrderByDescending(value=>value))
            {
                if(unused<=0)break;
                var remaining=distance.Where(p=>p.Value==depth).Select(p=>p.Key).OrderBy(id=>id).ToList();
                while(unused>0&&remaining.Count>0)
                {
                    double share=unused/remaining.Count,spent=0;
                    for(int i=remaining.Count-1;i>=0;i--)
                    {
                        int id=remaining[i];result.Damage.TryGetValue(id,out float previous);
                        double capacity=Math.Max(0,(double)nodes[id].Health-previous),applied=Math.Min(share,capacity);
                        if(applied>0){result.Damage[id]=(float)(previous+applied);spent+=applied;}
                        if(capacity<=share)remaining.RemoveAt(i);
                    }
                    if(spent<=0)break;
                    unused=Math.Max(0,unused-spent);
                }
            }
            result.Unused=(float)Math.Min(float.MaxValue,unused);return result;
        }

        /// <summary>
        /// A fraction of each received amount moves toward the next graph ring;
        /// overkill follows it. A part is visited once even in a cyclic graph.
        /// At the hop limit, or a dead end, the remaining amount hits that part.
        /// </summary>
        public static Result Resolve(IEnumerable<Node> snapshot,int target,float amount,float fraction,int maxHops)
        {
            if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));
            if(float.IsNaN(amount)||float.IsInfinity(amount)||amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
            if(float.IsNaN(fraction)||float.IsInfinity(fraction)||fraction<0||fraction>1)throw new ArgumentOutOfRangeException(nameof(fraction));
            if(maxHops<0)throw new ArgumentOutOfRangeException(nameof(maxHops));
            var nodes=snapshot.ToDictionary(n=>n.Id);
            if(nodes.Values.Any(n=>float.IsNaN(n.Health)||float.IsInfinity(n.Health)||n.Health<0))
                throw new ArgumentException("Part health must be finite and nonnegative.",nameof(snapshot));
            var result=new Result();
            if(amount==0)return result;
            if(!nodes.TryGetValue(target,out var first)||first.Health<=0){result.Unused=amount;return result;}

            // Only reciprocal links belong to the connection snapshot. This
            // also makes a malformed or stale unilateral link fail closed.
            IEnumerable<int> Neighbours(Node node)=>node.Neighbours.Distinct().OrderBy(id=>id).Where(id=>
                id!=node.Id&&nodes.TryGetValue(id,out var other)&&other.Health>0&&
                Array.IndexOf(other.Neighbours,node.Id)>=0);
            var distance=new Dictionary<int,int>{{target,0}};
            var queue=new Queue<int>();queue.Enqueue(target);
            while(queue.Count>0)
            {
                int id=queue.Dequeue(),depth=distance[id];if(depth>=maxHops)continue;
                foreach(int next in Neighbours(nodes[id]))
                    if(!distance.ContainsKey(next)){distance.Add(next,depth+1);queue.Enqueue(next);}
            }

            var pending=new Dictionary<int,double>{{target,amount}};
            double unused=0;
            foreach(var entry in distance.OrderBy(p=>p.Value).ThenBy(p=>p.Key))
            {
                if(!pending.TryGetValue(entry.Key,out double received)||received<=0)continue;
                var node=nodes[entry.Key];
                var children=Neighbours(node).Where(id=>distance.TryGetValue(id,out int depth)&&depth==entry.Value+1).ToArray();
                double retained=children.Length==0?received:received*(1d-fraction);
                double applied=Math.Min(node.Health,retained);
                if(applied>0)result.Damage.Add(node.Id,(float)applied);
                double remaining=Math.Max(0,received-applied);
                if(children.Length==0)unused+=remaining;
                else
                {
                    double share=remaining/children.Length;
                    foreach(int child in children)
                        pending[child]=pending.TryGetValue(child,out double old)?old+share:share;
                }
            }

            // A small or already-damaged branch must not erase damage while
            // another part in the same bounded graph still has health. Reuse
            // capacity in the outermost reachable ring first, sharing evenly.
            // This is the same atomic impact, not another graph traversal.
            foreach(int depth in distance.Values.Distinct().OrderByDescending(value=>value))
            {
                if(unused<=0)break;
                var remaining=distance.Where(p=>p.Value==depth).Select(p=>p.Key).OrderBy(id=>id).ToList();
                while(unused>0&&remaining.Count>0)
                {
                    double share=unused/remaining.Count,spent=0;
                    for(int i=remaining.Count-1;i>=0;i--)
                    {
                        int id=remaining[i];result.Damage.TryGetValue(id,out float previous);
                        double capacity=Math.Max(0,(double)nodes[id].Health-previous);
                        double applied=Math.Min(share,capacity);
                        if(applied>0){result.Damage[id]=(float)(previous+applied);spent+=applied;}
                        if(capacity<=share)remaining.RemoveAt(i);
                    }
                    if(spent<=0)break;
                    unused=Math.Max(0,unused-spent);
                }
            }
            result.Unused=(float)Math.Min(float.MaxValue,unused);
            return result;
        }
    }
}
