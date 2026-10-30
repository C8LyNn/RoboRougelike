// B01-24 / PASS 17 WAVE 06 — Original deterministic enemy squad pressure MECHANIC.
// Pure C#9 compatible with Unity 6, compiles directly together with unchanged actual R3EnemyArchetype.cs.
// IMPORTANT: advisory special-action reservations; DOES NOT fire guns, move Rigidbody, deal damage,
// update target/LOS, override Codex AI/ram, or write canonical gameplay Assets/**.
using System;
using System.Collections.Generic;
using RoboRougelike.R3;
namespace RoboRougelike.B0124.SquadPressure
{
    public enum PressureIntent { None, ScoutMark, GunnerVolley, BruiserPush, FlankerPincer }
    public enum PressureState { Idle, Active, Started, Ended }
    public readonly struct PressureMember
    {
        public readonly int Id;
        public readonly R3EnemyArchetype Archetype;
        public PressureMember(int id, R3EnemyArchetype role) { Id=id; Archetype=role; }
    }
    public readonly struct PressureObservation
    {
        public readonly int Id;
        public readonly bool Alive;
        public readonly bool TargetVisible;
        public readonly bool CanExecute;
        public readonly float Distance;
        public PressureObservation(int id, bool alive, bool targetVisible, bool canExecute, float distance)
        { Id=id; Alive=alive; TargetVisible=targetVisible; CanExecute=canExecute; Distance=distance; }
    }
    public readonly struct PressurePulse
    {
        public readonly PressureState State;
        public readonly int MemberId;
        public readonly PressureIntent Intent;
        public readonly long UntilTick;
        public readonly bool MarkedByScout;
        public PressurePulse(PressureState state, int id, PressureIntent intent, long until, bool marked)
        { State=state; MemberId=id; Intent=intent; UntilTick=until; MarkedByScout=marked; }
        public static PressurePulse Idle => new PressurePulse(PressureState.Idle,-1,PressureIntent.None,0,false);
    }
    // Policy is authored game-playable rule DATA in C#; deliberately not canonical balanced.
    public sealed class SquadPressurePolicy
    {
        public int WindowTicks { get; }
        public int GlobalGapTicks { get; }
        public int ScoutMarkTicks { get; }
        private readonly int[] roleCooldownTicks;
        private readonly float[] roleRangeMetres;
        public int CooldownFor(R3EnemyArchetype role) => roleCooldownTicks[(int)role];
        public float RangeFor(R3EnemyArchetype role) => roleRangeMetres[(int)role];
        public SquadPressurePolicy(int windowTicks,int globalGapTicks,int scoutMarkTicks,
            int[] roleCooldownTicks,float[] roleRangeMetres)
        {
            if(windowTicks<1||windowTicks>120||globalGapTicks<0||globalGapTicks>120||
                scoutMarkTicks<1||scoutMarkTicks>240||roleCooldownTicks==null||roleRangeMetres==null||
                roleCooldownTicks.Length!=4||roleRangeMetres.Length!=4)
                throw new ArgumentException("Invalid pressure timing and role policy");
            for(int i=0;i<4;i++)
                if(roleCooldownTicks[i]<windowTicks||roleCooldownTicks[i]>480||
                    float.IsNaN(roleRangeMetres[i])||float.IsInfinity(roleRangeMetres[i])||
                    roleRangeMetres[i]<1f||roleRangeMetres[i]>200f)
                    throw new ArgumentException("Invalid role cooldown/range "+i);
            WindowTicks=windowTicks;GlobalGapTicks=globalGapTicks;ScoutMarkTicks=scoutMarkTicks;
            this.roleCooldownTicks=(int[])roleCooldownTicks.Clone();
            this.roleRangeMetres=(float[])roleRangeMetres.Clone();
        }
        public static SquadPressurePolicy Standard()
        {
            // indices: real R3EnemyArchetype [Scout=0,Bruiser=1,Gunner=2,Flanker=3].
            // Novel, provisional authored research tuning, not shipping balance.
            return new SquadPressurePolicy(3,2,12,
                new []{10,15,12,13},new []{35f,16f,60f,32f});
        }
    }
    /// <summary>One distinct squad, deterministic event arbitration per monotonic simulation tick.
    /// The caller (Codex if accepted) decides if an actual special action is executed.
    /// </summary>
    public sealed class SquadPressureScheduler
    {
        private readonly PressureMember[] members;
        private readonly long[] ready;
        private readonly SquadPressurePolicy rule;
        private readonly int[] ids;
        private int cursor;
        private int active=-1;
        private PressureIntent activeIntent;
        private long activeUntil, nextGlobalTick, markUntil, lastTick=-1;
        public int SquadSize => members.Length;
        public bool HasActiveReservation => active>=0;
        public SquadPressureScheduler(PressureMember[] roster,SquadPressurePolicy policy)
        {
            if(roster==null||policy==null||roster.Length<1||roster.Length>4)
                throw new ArgumentException("Squad size must be 1–4 with nonnull policy");
            members=(PressureMember[])roster.Clone();rule=policy;
            ready=new long[members.Length];ids=new int[members.Length];
            var distinct=new HashSet<int>();
            for(int i=0;i<members.Length;i++)
            {
                if(members[i].Id<0||!distinct.Add(members[i].Id)||
                    !Enum.IsDefined(typeof(R3EnemyArchetype),members[i].Archetype))
                    throw new ArgumentException("Duplicate ID or invalid original R3 archetype");
                ids[i]=members[i].Id;
            }
        }
        private static PressureIntent ToIntent(R3EnemyArchetype a)
        {
            switch(a)
            {
                case R3EnemyArchetype.Scout: return PressureIntent.ScoutMark;
                case R3EnemyArchetype.Gunner: return PressureIntent.GunnerVolley;
                case R3EnemyArchetype.Bruiser: return PressureIntent.BruiserPush;
                case R3EnemyArchetype.Flanker: return PressureIntent.FlankerPincer;
                default: throw new ArgumentException("Unknown R3 archetype");
            }
        }
        // Caller supplies observations in ANY order; every active roster ID must be present exactly once.
        // Fail-closed on impossible values BEFORE changing scheduler state.
        public PressurePulse Tick(long tick,PressureObservation[] observations)
        {
            if(tick<0||tick<=lastTick||tick>long.MaxValue-1000||
                observations==null||observations.Length!=members.Length)
                throw new ArgumentException("Invalid monotonic tick or observation count");
            var byIndex=new PressureObservation[members.Length];
            var seen=new bool[members.Length];
            for(int n=0;n<observations.Length;n++)
            {
                PressureObservation o=observations[n];
                if(float.IsNaN(o.Distance)||float.IsInfinity(o.Distance)||o.Distance<0||o.Distance>2000)
                    throw new ArgumentException("Invalid distance");
                int i=Array.IndexOf(ids,o.Id);
                if(i<0||seen[i])throw new ArgumentException("Foreign or duplicate squad member observation");
                byIndex[i]=o;seen[i]=true;
            }
            for(int i=0;i<seen.Length;i++)if(!seen[i])throw new ArgumentException("Missing observation");
            lastTick=tick;
            // An active member that was killed/disarmed/cannot see target releases its slot immediately.
            if(active>=0)
            {
                var observed=byIndex[active];
                if(tick>=activeUntil||!observed.Alive||!observed.CanExecute||!observed.TargetVisible)
                {
                    var ended=new PressurePulse(PressureState.Ended,members[active].Id,activeIntent,tick,false);
                    active=-1;activeIntent=PressureIntent.None;
                    // Releasing does not start another special action in the same tick.
                    return ended;
                }
                return new PressurePulse(PressureState.Active,members[active].Id,activeIntent,activeUntil,false);
            }
            if(tick<nextGlobalTick)return PressurePulse.Idle;
            for(int offset=0;offset<members.Length;offset++)
            {
                int i=(cursor+offset)%members.Length;
                var o=byIndex[i];var role=members[i].Archetype;
                if(!o.Alive||!o.TargetVisible||!o.CanExecute||tick<ready[i]||
                    o.Distance>rule.RangeFor(role))continue;
                active=i;activeIntent=ToIntent(role);activeUntil=tick+rule.WindowTicks;
                ready[i]=tick+rule.CooldownFor(role);
                nextGlobalTick=activeUntil+rule.GlobalGapTicks;
                cursor=(i+1)%members.Length;
                bool tagged=activeIntent!=PressureIntent.ScoutMark && tick<markUntil;
                if(activeIntent==PressureIntent.ScoutMark)markUntil=tick+rule.ScoutMarkTicks;
                return new PressurePulse(PressureState.Started,members[i].Id,activeIntent,activeUntil,tagged);
            }
            return PressurePulse.Idle;
        }
    }
}
