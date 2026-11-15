using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RoboRougelike.R3
{
    // Each step emits at most one projectile (or discards one invalid batch).
    // Implementations own their snapshots and retain unfinished work.
    internal interface IR3ProjectileWork
    {
        bool HasProjectileWork { get; }
        bool CanProcessProjectileWork { get; }
        bool ProcessProjectileWork();
    }

    // A single frame budget is shared between conventional, rocket and device
    // queues. Rotating queues keeps one busy owner from starving other owners.
    internal static class R3ProjectileWorkScheduler
    {
        static readonly LinkedList<IR3ProjectileWork> pending=new LinkedList<IR3ProjectileWork>();
        static readonly Dictionary<IR3ProjectileWork,LinkedListNode<IR3ProjectileWork>> nodes=
            new Dictionary<IR3ProjectileWork,LinkedListNode<IR3ProjectileWork>>();
        static int budgetFrame=-1;
        static long spentTicks;
        static bool workedThisFrame,pumping;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {pending.Clear();nodes.Clear();budgetFrame=-1;spentTicks=0;workedThisFrame=false;pumping=false;}

        internal static void Register(IR3ProjectileWork work)
        {
            if(work==null||nodes.ContainsKey(work))return;
            nodes.Add(work,pending.AddLast(work));
        }
        internal static void Unregister(IR3ProjectileWork work)
        {
            if(work==null||!nodes.TryGetValue(work,out var node))return;
            pending.Remove(node);nodes.Remove(work);
        }
        internal static void Request(IR3ProjectileWork work,double budgetMilliseconds)
        {
            Register(work);if(pumping)return;
            if(budgetFrame!=Time.frameCount)
            {budgetFrame=Time.frameCount;spentTicks=0;workedThisFrame=false;}
            long requestStart=Stopwatch.GetTimestamp();
            pumping=true;
            try
            {
                int stalled=0;
                while(pending.Count>0&&(!workedThisFrame||
                    (spentTicks+Stopwatch.GetTimestamp()-requestStart)*1000d/Stopwatch.Frequency<budgetMilliseconds))
                {
                    var next=pending.First.Value;Unregister(next);
                    var component=next as MonoBehaviour;
                    if(component==null||!component.isActiveAndEnabled||!next.HasProjectileWork)
                    {stalled=0;continue;}
                    bool progressed=next.CanProcessProjectileWork&&next.ProcessProjectileWork();
                    if(component!=null&&component.isActiveAndEnabled&&next.HasProjectileWork)Register(next);
                    if(progressed){workedThisFrame=true;stalled=0;}
                    else if(!nodes.ContainsKey(next))stalled=0;
                    else if(++stalled>=pending.Count)break;
                }
            }
            finally{spentTicks+=Stopwatch.GetTimestamp()-requestStart;pumping=false;}
        }
    }
}
