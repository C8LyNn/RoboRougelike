// B01-24 PASS17 W07 — Original deterministic enemy hearing / last-seen investigation.
// C#9, pure System.Numerics. ORIGINAL Codex R3 roles compiled as-is by acceptance suite.
// Advisory destination output only: does not fire guns, move Rigidbody, aim, modify existing AI or game source.
using System;
using System.Collections.Generic;
using System.Numerics;
using RoboRougelike.R3;

namespace RoboRougelike.B0124.Investigation
{
    public enum PerceptionMode { Idle, PursueVisible, MoveToClue, SearchClue, Hold }
    public enum ClueSource { None, Sight, Noise }
    public readonly struct NoiseCue
    {
        public readonly int Id;
        public readonly long Tick;
        public readonly Vector2 Position;
        public readonly float Strength;
        public readonly bool Occluded;
        public NoiseCue(int id, long tick, Vector2 position, float strength, bool occluded)
        { Id=id;Tick=tick;Position=position;Strength=strength;Occluded=occluded; }
    }
    public readonly struct PerceptionFrame
    {
        public readonly long Tick;
        public readonly Vector2 Self;
        public readonly bool PlayerVisible;
        public readonly Vector2 PlayerPosition;
        public readonly bool CanMove;
        public readonly NoiseCue[] Noises;
        public PerceptionFrame(long tick, Vector2 self, bool playerVisible,
            Vector2 playerPosition, bool canMove, NoiseCue[] noises)
        { Tick=tick;Self=self;PlayerVisible=playerVisible;PlayerPosition=playerPosition;CanMove=canMove;Noises=noises; }
    }
    public readonly struct PerceptionDecision
    {
        public readonly PerceptionMode Mode;
        public readonly ClueSource Source;
        public readonly Vector2 Target;
        public readonly int NoiseId;
        public readonly long ClueExpiryTick;
        public PerceptionDecision(PerceptionMode mode, ClueSource source, Vector2 target, int noiseId,long expires)
        { Mode=mode; Source=source;Target=target;NoiseId=noiseId;ClueExpiryTick=expires; }
    }
    public sealed class InvestigationPolicy
    {
        public readonly float HearingDistance;
        public readonly float MinimumSignal;
        public readonly float OcclusionFactor;
        public readonly float ArrivalDistance;
        public readonly int MemoryTicks;
        public readonly int SearchTicks;
        public readonly float SearchRadius;
        public InvestigationPolicy(float hearingDistance,float minimumSignal,float occlusionFactor,
            float arrivalDistance,int memoryTicks,int searchTicks,float searchRadius)
        {
            if(!Finite(hearingDistance)||hearingDistance<1||hearingDistance>200||
               !Finite(minimumSignal)||minimumSignal<=0||minimumSignal>1||
               !Finite(occlusionFactor)||occlusionFactor<=0||occlusionFactor>1||
               !Finite(arrivalDistance)||arrivalDistance<=0||arrivalDistance>10||
               memoryTicks<1||memoryTicks>240||searchTicks<1||searchTicks>240||
               !Finite(searchRadius)||searchRadius<=arrivalDistance||searchRadius>50)
                throw new ArgumentException("Invalid investigation policy");
            HearingDistance=hearingDistance;MinimumSignal=minimumSignal;OcclusionFactor=occlusionFactor;
            ArrivalDistance=arrivalDistance;MemoryTicks=memoryTicks;SearchTicks=searchTicks;SearchRadius=searchRadius;
        }
        public static InvestigationPolicy ForRole(R3EnemyArchetype role)
        {
            // Research-only provisional tuning. Values not approved as final game balance.
            switch(role)
            {
                case R3EnemyArchetype.Scout: return new InvestigationPolicy(42,.24f,.3f,1.5f,16,12,4);
                case R3EnemyArchetype.Bruiser: return new InvestigationPolicy(22,.24f,.3f,1.5f,10,8,3);
                case R3EnemyArchetype.Gunner: return new InvestigationPolicy(32,.24f,.3f,1.5f,14,10,4);
                case R3EnemyArchetype.Flanker: return new InvestigationPolicy(38,.24f,.3f,1.5f,15,12,5);
                default: throw new ArgumentException("Unknown original R3 archetype");
            }
        }
        private static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
    }
    public sealed class EnemyInvestigation
    {
        private static readonly Vector2[] SearchOffset={
            new Vector2(1,0),new Vector2(0,1),new Vector2(-1,0),new Vector2(0,-1)
        };
        private readonly InvestigationPolicy policy;
        private long lastTick=-1, clueExpires=-1, searchExpires=-1;
        private int lastNoiseId=-1, clueNoiseId=-1, searchIndex;
        private bool hasClue,searching;
        private ClueSource source=ClueSource.None;
        private Vector2 cluePosition;
        public EnemyInvestigation(R3EnemyArchetype role)
        { policy=InvestigationPolicy.ForRole(role); }
        public EnemyInvestigation(InvestigationPolicy policy)
        { this.policy=policy??throw new ArgumentNullException(nameof(policy)); }
        public static bool Finite(Vector2 v)=>!float.IsNaN(v.X)&&!float.IsInfinity(v.X)&&!float.IsNaN(v.Y)&&!float.IsInfinity(v.Y);
        private static float Distance(Vector2 a,Vector2 b)=>Vector2.Distance(a,b);
        private static void Validate(PerceptionFrame f,long last)
        {
            if(f.Tick<0||f.Tick<=last||f.Tick>long.MaxValue-1000||!Finite(f.Self)||
                !Finite(f.PlayerPosition)||f.Noises==null||f.Noises.Length>128)
                throw new ArgumentException("Invalid frame or monotonic simulation tick");
            var ids=new HashSet<int>();
            for(int i=0;i<f.Noises.Length;i++)
            {
                var n=f.Noises[i];
                if(n.Id<0||!ids.Add(n.Id)||n.Tick<0||n.Tick>f.Tick||
                    !Finite(n.Position)||float.IsNaN(n.Strength)||float.IsInfinity(n.Strength)||
                    n.Strength<0||n.Strength>1)
                    throw new ArgumentException("Invalid or duplicated noise cue");
            }
        }
        private PerceptionDecision Output(PerceptionMode mode,Vector2 target)=>
            new PerceptionDecision(mode,source,target,clueNoiseId,clueExpires);
        private void Clear()
        {
            hasClue=false;searching=false;source=ClueSource.None;clueNoiseId=-1;
            searchIndex=0;clueExpires=-1;searchExpires=-1;
        }
        public PerceptionDecision Tick(PerceptionFrame frame)
        {
            // Full validation first; rejection must not advance lastTick, consume noise or mutate memory.
            Validate(frame,lastTick);
            lastTick=frame.Tick;
            var recent=frame.Noises;
            int newest=lastNoiseId;
            float best=-1;
            int bestId=int.MaxValue;
            Vector2 bestPoint=default;
            for(int i=0;i<recent.Length;i++)
            {
                var cue=recent[i];
                if(cue.Id<=lastNoiseId||frame.Tick-cue.Tick>policy.MemoryTicks)continue;
                if(cue.Id>newest)newest=cue.Id;
                float dist=Distance(frame.Self,cue.Position);
                if(float.IsNaN(dist)||float.IsInfinity(dist))continue;
                // Hearing falloff; world-occlusion is supplied by Codex owner, not fabricated by this module.
                float signal=cue.Strength*policy.HearingDistance/(policy.HearingDistance+dist)*
                    (cue.Occluded?policy.OcclusionFactor:1f);
                if(signal>=policy.MinimumSignal&&(signal>best||
                    (signal==best&&cue.Id<bestId)))
                { best=signal;bestId=cue.Id;bestPoint=cue.Position; }
            }
            lastNoiseId=newest;
            if(frame.PlayerVisible)
            {
                hasClue=true;searching=false;source=ClueSource.Sight;clueNoiseId=-1;
                cluePosition=frame.PlayerPosition;clueExpires=frame.Tick+policy.MemoryTicks;
                searchIndex=0;
                return Output(frame.CanMove?PerceptionMode.PursueVisible:PerceptionMode.Hold,cluePosition);
            }
            // An audible NEW shot overrides an older clue. Old replayed cue IDs never reactivate.
            if(best>=0)
            {
                hasClue=true;searching=false;source=ClueSource.Noise;clueNoiseId=bestId;
                cluePosition=bestPoint;clueExpires=frame.Tick+policy.MemoryTicks;searchIndex=0;
            }
            if(!hasClue)return new PerceptionDecision(PerceptionMode.Idle,ClueSource.None,frame.Self,-1,-1);
            if(frame.Tick>=clueExpires)
            {
                Clear();return new PerceptionDecision(PerceptionMode.Idle,ClueSource.None,frame.Self,-1,-1);
            }
            if(!frame.CanMove)return Output(PerceptionMode.Hold,frame.Self);
            if(!searching)
            {
                if(Distance(frame.Self,cluePosition)>policy.ArrivalDistance)
                    return Output(PerceptionMode.MoveToClue,cluePosition);
                searching=true;searchExpires=frame.Tick+policy.SearchTicks;searchIndex=0;
            }
            if(frame.Tick>=searchExpires||searchIndex>=SearchOffset.Length)
            {
                Clear();return new PerceptionDecision(PerceptionMode.Idle,ClueSource.None,frame.Self,-1,-1);
            }
            Vector2 seek=cluePosition+SearchOffset[searchIndex]*policy.SearchRadius;
            if(Distance(frame.Self,seek)<=policy.ArrivalDistance)
            {
                searchIndex++;
                if(searchIndex>=SearchOffset.Length)
                {
                    Clear();return new PerceptionDecision(PerceptionMode.Idle,ClueSource.None,frame.Self,-1,-1);
                }
                seek=cluePosition+SearchOffset[searchIndex]*policy.SearchRadius;
            }
            return Output(PerceptionMode.SearchClue,seek);
        }
    }
}
