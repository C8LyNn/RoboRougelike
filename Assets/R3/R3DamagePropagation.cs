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

            var pending=new Dictionary<int,float>{{target,amount}};
            foreach(var entry in distance.OrderBy(p=>p.Value).ThenBy(p=>p.Key))
            {
                if(!pending.TryGetValue(entry.Key,out float received)||received<=0)continue;
                var node=nodes[entry.Key];
                var children=Neighbours(node).Where(id=>distance.TryGetValue(id,out int depth)&&depth==entry.Value+1).ToArray();
                float retained=children.Length==0?received:received*(1-fraction);
                float applied=Math.Min(node.Health,retained);
                if(applied>0)result.Damage.Add(node.Id,applied);
                float remaining=Math.Max(0,received-applied);
                if(children.Length==0)result.Unused+=remaining;
                else
                {
                    float share=remaining/children.Length;
                    foreach(int child in children)
                        pending[child]=pending.TryGetValue(child,out float old)?old+share:share;
                }
            }
            return result;
        }
    }
}
