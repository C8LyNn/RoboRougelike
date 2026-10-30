// B01-24 PASS17 W08 — original per-squad tactical radio sighting relay, C#9, no Unity dependency.
// Advisory information ONLY: no weapon/target selection, Rigidbody movement, damage or canonical Assets changes.
// Uses actual unchanged Codex R3EnemyArchetype.cs during original-source acceptance.
using System;
using System.Collections.Generic;
using System.Numerics;
using RoboRougelike.R3;

namespace RoboRougelike.B0124.AllyAlert
{
    public readonly struct RadioMember
    {
        public readonly int Id;
        public readonly R3EnemyArchetype Role;
        public readonly Vector2 Position;
        public readonly bool Alive, HasVisualContact, CanTransmit, CanReceive;
        public RadioMember(int id, R3EnemyArchetype role, Vector2 position,
            bool alive, bool hasVisualContact, bool canTransmit, bool canReceive)
        {
            Id=id; Role=role; Position=position; Alive=alive;
            HasVisualContact=hasVisualContact; CanTransmit=canTransmit; CanReceive=canReceive;
        }
    }
    public readonly struct SightMessage
    {
        public readonly int Id, SenderId;
        public readonly long Tick;
        public readonly Vector2 PlayerLastSeenXZ;
        public SightMessage(int id,int senderId,long tick,Vector2 playerLastSeenXZ)
        { Id=id;SenderId=senderId;Tick=tick;PlayerLastSeenXZ=playerLastSeenXZ; }
    }
    public readonly struct RadioIntel
    {
        public readonly int ReceiverId, SenderId, MessageId;
        public readonly Vector2 PlayerLastSeenXZ;
        public readonly long OriginalSightTick, ExpiryTick;
        public bool Valid => SenderId>=0;
        public RadioIntel(int receiverId,int senderId,int messageId,Vector2 point,long sight,long expiry)
        {
            ReceiverId=receiverId;SenderId=senderId;MessageId=messageId;
            PlayerLastSeenXZ=point;OriginalSightTick=sight;ExpiryTick=expiry;
        }
        public static RadioIntel Empty(int receiverId)=>new RadioIntel(receiverId,-1,-1,default,-1,-1);
    }
    public sealed class RelayPolicy
    {
        private readonly float[] ranges;
        public readonly int DelayTicks, IntelTtlTicks;
        public RelayPolicy(int delayTicks,int intelTtlTicks,float[] roleRange)
        {
            if(delayTicks<1||delayTicks>30||intelTtlTicks<=delayTicks||intelTtlTicks>240||
                roleRange==null||roleRange.Length!=4)
                throw new ArgumentException("Invalid radio timing and range policy");
            for(int i=0;i<4;i++)
                if(!Finite(roleRange[i])||roleRange[i]<1||roleRange[i]>200)
                    throw new ArgumentException("Invalid role radio range");
            ranges=(float[])roleRange.Clone();DelayTicks=delayTicks;IntelTtlTicks=intelTtlTicks;
        }
        public float RangeFor(R3EnemyArchetype role)=>ranges[(int)role];
        public static RelayPolicy Standard()=>new RelayPolicy(2,12,new[]{65f,30f,52f,55f});
        internal static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
        internal static bool Finite(Vector2 v)=>Finite(v.X)&&Finite(v.Y);
    }
    public sealed class AllyAlertRelay
    {
        private readonly int[] ids;
        private readonly R3EnemyArchetype[] roles;
        private readonly RadioIntel[] current;
        private readonly List<Queued> queue=new List<Queued>();
        private readonly RelayPolicy policy;
        private long lastTick=-1;
        private int lastMessageId=-1;
        private readonly struct Queued
        {
            public readonly int Receiver,SenderId,MessageId;
            public readonly long Sent,Due,Expiry;
            public readonly Vector2 Point;
            public Queued(int receiver,int senderId,int messageId,long sent,long due,long expiry,Vector2 point)
            { Receiver=receiver;SenderId=senderId;MessageId=messageId;Sent=sent;Due=due;Expiry=expiry;Point=point; }
        }
        public AllyAlertRelay(int[] stableIds,R3EnemyArchetype[] roles,RelayPolicy policy)
        {
            if(stableIds==null||roles==null||policy==null||stableIds.Length<1||stableIds.Length>4||
                stableIds.Length!=roles.Length)throw new ArgumentException("Squad must have 1–4 real R3 members");
            var seen=new HashSet<int>();
            for(int i=0;i<stableIds.Length;i++)
                if(stableIds[i]<0||!seen.Add(stableIds[i])||!Enum.IsDefined(typeof(R3EnemyArchetype),roles[i]))
                    throw new ArgumentException("Duplicated, negative or invalid canonical R3 squad member");
            this.ids=(int[])stableIds.Clone();this.roles=(R3EnemyArchetype[])roles.Clone();this.policy=policy;
            current=new RadioIntel[ids.Length];
            for(int i=0;i<ids.Length;i++)current[i]=RadioIntel.Empty(ids[i]);
        }
        public int SquadSize=>ids.Length;
        public int PendingCount=>queue.Count;
        public RadioIntel[] Tick(long tick,RadioMember[] members,SightMessage[] reports)
        {
            if(tick<0||tick<=lastTick||tick>long.MaxValue-1000||
                members==null||members.Length!=ids.Length||
                reports==null||reports.Length>64)throw new ArgumentException("Invalid monotonic radio frame");
            var observations=new RadioMember[ids.Length];
            var seen=new bool[ids.Length];
            for(int m=0;m<members.Length;m++)
            {
                var member=members[m];
                int slot=Array.IndexOf(ids,member.Id);
                if(slot<0||seen[slot]||member.Role!=roles[slot]||!RelayPolicy.Finite(member.Position))
                    throw new ArgumentException("Invalid, duplicate or incorrect R3 member observation");
                observations[slot]=member;seen[slot]=true;
            }
            for(int i=0;i<seen.Length;i++)if(!seen[i])throw new ArgumentException("Missing member");
            var reportIds=new HashSet<int>();
            foreach(var message in reports)
                if(message.Id<0||message.SenderId<0||message.Tick!=tick||
                    !RelayPolicy.Finite(message.PlayerLastSeenXZ)||!reportIds.Add(message.Id)||
                    Array.IndexOf(ids,message.SenderId)<0)
                    throw new ArgumentException("Invalid or duplicate current sight report");
            // Validation completed; rejects do NOT consume the tick or mutate radio memory.
            lastTick=tick;
            for(int i=0;i<current.Length;i++)
            {
                if(!observations[i].Alive||!observations[i].CanReceive||tick>=current[i].ExpiryTick)
                    current[i]=RadioIntel.Empty(ids[i]);
            }
            // Deliver previously queued messages first; same-tick reports cannot bypass latency.
            for(int i=queue.Count-1;i>=0;i--)
            {
                var q=queue[i];
                if(q.Due>tick)continue;
                queue.RemoveAt(i);
                int rec=q.Receiver;
                if(tick>=q.Expiry||!observations[rec].Alive||!observations[rec].CanReceive)continue;
                var old=current[rec];
                // Newest confirmed observation wins; low ID is deterministic tie-breaker.
                if(!old.Valid||q.Sent>old.OriginalSightTick||
                    (q.Sent==old.OriginalSightTick&&q.MessageId<old.MessageId))
                    current[rec]=new RadioIntel(ids[rec],q.SenderId,q.MessageId,q.Point,q.Sent,q.Expiry);
            }
            // Enumerating reports in any order must yield the same future decisions.
            var ordered=(SightMessage[])reports.Clone();
            Array.Sort(ordered,(a,b)=>a.Id.CompareTo(b.Id));
            int watermark=lastMessageId;
            foreach(var report in ordered)
            {
                if(report.Id<=lastMessageId)continue; // Retransmission of consumed ID: no reaction.
                if(report.Id>watermark)watermark=report.Id;
                int sender=Array.IndexOf(ids,report.SenderId);
                var transmitting=observations[sender];
                // Security: do not share fabricated player position from an unseeing/jammed/dead source.
                if(!transmitting.Alive||!transmitting.HasVisualContact||!transmitting.CanTransmit)continue;
                for(int receiver=0;receiver<ids.Length;receiver++)
                {
                    if(receiver==sender)continue;
                    var target=observations[receiver];
                    if(!target.Alive||!target.CanReceive)continue;
                    float allowable=Math.Min(policy.RangeFor(transmitting.Role),policy.RangeFor(target.Role));
                    if(Vector2.DistanceSquared(transmitting.Position,target.Position)>allowable*allowable)continue;
                    queue.Add(new Queued(receiver,report.SenderId,report.Id,tick,
                        tick+policy.DelayTicks,tick+policy.IntelTtlTicks,report.PlayerLastSeenXZ));
                }
            }
            lastMessageId=watermark;
            return (RadioIntel[])current.Clone();
        }
    }
}
