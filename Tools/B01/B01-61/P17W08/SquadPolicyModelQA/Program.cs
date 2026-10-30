// B01-61 P17 W08 independent parameterized model-based test of immutable B01-24 #429.
// Compiles the ACTUAL unchanged SquadPressure.cs and Codex R3 archetype, never a replacement scheduler.
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using RoboRougelike.R3;
using RoboRougelike.B0124.SquadPressure;

internal sealed class ExternalPolicyOracle
{
    private readonly PressureMember[] roster;
    private readonly SquadPressurePolicy policy;
    private readonly Dictionary<int, long> cooldown = new Dictionary<int, long>();
    private int nextIndex;
    private int reserved = -1;
    private long releaseAt, globalAt, scoutTagAt;
    private PressureIntent reservedIntent;
    public ExternalPolicyOracle(PressureMember[] members, SquadPressurePolicy config)
    {
        roster = (PressureMember[])members.Clone();
        policy = config;
        foreach (var member in roster) cooldown[member.Id] = 0;
    }
    private static PressureIntent ActionFor(R3EnemyArchetype type)
    {
        if (type == R3EnemyArchetype.Scout) return PressureIntent.ScoutMark;
        if (type == R3EnemyArchetype.Bruiser) return PressureIntent.BruiserPush;
        if (type == R3EnemyArchetype.Gunner) return PressureIntent.GunnerVolley;
        if (type == R3EnemyArchetype.Flanker) return PressureIntent.FlankerPincer;
        throw new InvalidOperationException("Unexpected reference role");
    }
    private bool Eligible(PressureMember member, PressureObservation[] current)
    {
        foreach (var view in current)
        {
            if (view.Id != member.Id) continue;
            return view.Alive && view.TargetVisible && view.CanExecute &&
                view.Distance <= policy.RangeFor(member.Archetype);
        }
        throw new InvalidOperationException("Invalid fixture roster");
    }
    public PressurePulse Step(long tick, PressureObservation[] current)
    {
        if (reserved >= 0)
        {
            var actor = roster[reserved];
            if (tick >= releaseAt || !Eligible(actor, current))
            {
                reserved = -1;
                var stopped = new PressurePulse(PressureState.Ended, actor.Id, reservedIntent, tick, false);
                reservedIntent = PressureIntent.None;
                return stopped;
            }
            return new PressurePulse(PressureState.Active, actor.Id, reservedIntent, releaseAt, false);
        }
        if (tick < globalAt) return PressurePulse.Idle;
        for (int step = 0; step < roster.Length; step++)
        {
            int candidate = (nextIndex + step) % roster.Length;
            var actor = roster[candidate];
            if (tick < cooldown[actor.Id] || !Eligible(actor, current)) continue;
            reserved = candidate;
            reservedIntent = ActionFor(actor.Archetype);
            releaseAt = tick + policy.WindowTicks;
            globalAt = releaseAt + policy.GlobalGapTicks;
            cooldown[actor.Id] = tick + policy.CooldownFor(actor.Archetype);
            nextIndex = (candidate + 1) % roster.Length;
            bool tag = reservedIntent != PressureIntent.ScoutMark && tick < scoutTagAt;
            if (reservedIntent == PressureIntent.ScoutMark) scoutTagAt = tick + policy.ScoutMarkTicks;
            return new PressurePulse(PressureState.Started, actor.Id, reservedIntent, releaseAt, tag);
        }
        return PressurePulse.Idle;
    }
}
internal static class SquadPolicyModelQa
{
    private static int passed, failed;
    private static long comparisons;
    private static readonly StringBuilder trace = new StringBuilder();
    private static void Require(bool condition, string detail)
    {
        if (!condition) throw new InvalidOperationException(detail);
    }
    private static void Case(string title, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS " + title); }
        catch (Exception e) { failed++; Console.Error.WriteLine("FAIL " + title + ": " + e); }
    }
    private static void Reject(Action invalid)
    {
        try { invalid(); }
        catch (ArgumentException) { return; }
        throw new InvalidOperationException("Invalid input unexpectedly accepted");
    }
    private static void Equal(PressurePulse found, PressurePulse expected, string message)
    {
        comparisons++;
        Require(found.State == expected.State &&
            found.MemberId == expected.MemberId &&
            found.Intent == expected.Intent &&
            found.UntilTick == expected.UntilTick &&
            found.MarkedByScout == expected.MarkedByScout,
            message + " expected=" + expected.State + "/" + expected.MemberId +
            "/" + expected.Intent + "/" + expected.UntilTick + "/" + expected.MarkedByScout +
            " actual=" + found.State + "/" + found.MemberId +
            "/" + found.Intent + "/" + found.UntilTick + "/" + found.MarkedByScout);
        trace.Append((int)found.State).Append('|').Append(found.MemberId).Append('|')
            .Append((int)found.Intent).Append('|').Append(found.UntilTick).Append('|')
            .Append(found.MarkedByScout ? 1 : 0).Append(';');
    }
    private static SquadPressurePolicy Config(int seed)
    {
        int window = 1 + (seed * 37 % 120);
        int gap = seed * 17 % 121;
        int mark = 1 + seed * 29 % 240;
        var cool = new int[4];
        var range = new float[4];
        for (int role = 0; role < 4; role++)
        {
            cool[role] = window + ((seed * 13 + role * 47) % (481 - window));
            range[role] = 1f + ((seed * 19 + role * 31) % 199);
        }
        return new SquadPressurePolicy(window, gap, mark, cool, range);
    }
    private static void ParameterSweep()
    {
        // Unlike independent QA #434, these are 96 DISTINCT non-default timing/range policies,
        // not repetitions of SquadPressurePolicy.Standard().
        for (int seed = 1; seed <= 96; seed++)
        {
            var policy = Config(seed);
            int size = 1 + seed % 4;
            var roster = new PressureMember[size];
            for (int i = 0; i < size; i++)
                roster[i] = new PressureMember(seed * 100 + i + 1,
                    (R3EnemyArchetype)((seed + i) % 4));
            var real = new SquadPressureScheduler(roster, policy);
            var oracle = new ExternalPolicyOracle(roster, policy);
            long time = 0;
            for (int frame = 0; frame < 512; frame++)
            {
                if (frame > 0) time += 1 + ((frame * 7 + seed * 11) % 23);
                var observations = new PressureObservation[size];
                for (int i = 0; i < size; i++)
                {
                    float r = policy.RangeFor(roster[i].Archetype);
                    int scenario = (frame + 5 * i + seed) % 9;
                    float distance = scenario == 0 ? r + 0.125f :
                        scenario == 1 ? r : scenario == 2 ? r - 0.125f : r * 0.5f;
                    observations[i] = new PressureObservation(roster[i].Id,
                        (frame + seed + i) % 41 != 0, (frame * 3 + i) % 17 != 0,
                        (frame + i * 7) % 19 != 0, distance);
                }
                // Rotate input order, without changing the roster/priority contract.
                for (int i = 0; i < size / 2; i++)
                {
                    var temp = observations[i];
                    observations[i] = observations[size - i - 1];
                    observations[size - i - 1] = temp;
                }
                var target = oracle.Step(time, observations);
                var actual = real.Tick(time, observations);
                Equal(actual, target, "seed=" + seed + " frame=" + frame + " tick=" + time);
            }
        }
        Require(comparisons == 49152, "Parameterized comparison count drift");
    }
    private static void PolicyArrayDefensiveCopy()
    {
        int[] cds = { 4, 8, 5, 7 };
        float[] ranges = { 15f, 16f, 17f, 18f };
        var policy = new SquadPressurePolicy(3, 2, 7, cds, ranges);
        cds[0] = 400; ranges[0] = 200;
        Require(policy.CooldownFor(R3EnemyArchetype.Scout) == 4 &&
            policy.RangeFor(R3EnemyArchetype.Scout) == 15,
            "Policy arrays are aliased to caller");
    }
    private static void InvalidPolicyDoesNotPass()
    {
        Reject(() => new SquadPressurePolicy(1, 0, 2, new[] { 1, 1, 1, 1 },
            new[] { float.NaN, 3f, 3f, 3f }));
        Reject(() => new SquadPressurePolicy(121, 0, 2, new[] { 121, 121, 121, 121 },
            new[] { 3f, 3f, 3f, 3f }));
        Reject(() => new SquadPressurePolicy(1, 0, 2, new[] { 0, 1, 1, 1 },
            new[] { 3f, 3f, 3f, 3f }));
        Reject(() => new SquadPressurePolicy(1, 0, 2, new[] { 1, 1, 1, 1 },
            new[] { 3f, float.PositiveInfinity, 3f, 3f }));
    }
    private static void BadObservationIsAtomic()
    {
        var roster = new[] { new PressureMember(11, R3EnemyArchetype.Scout) };
        var policy = Config(19);
        var bad = new SquadPressureScheduler(roster, policy);
        var control = new SquadPressureScheduler(roster, policy);
        var good = new[] { new PressureObservation(11, true, true, true, 4f) };
        Reject(() => bad.Tick(0, new[] {
            new PressureObservation(11, true, true, true, float.NaN) }));
        Equal(bad.Tick(0, good), control.Tick(0, good), "Invalid tick changed scheduler");
        Reject(() => bad.Tick(0, good));
        Equal(bad.Tick(1, good), control.Tick(1, good), "Repeated tick changed scheduler");
    }
    public static int Main()
    {
        Case("49,152 exact original-CSharp9 vs independent model pulses: 96 variable policies", ParameterSweep);
        Case("policy cooldown and range are defensively copied", PolicyArrayDefensiveCopy);
        Case("malformed variable policies reject NaN, infinity and invalid bounds", InvalidPolicyDoesNotPass);
        Case("bad/duplicate tick is atomic under nondefault policy", BadObservationIsAtomic);
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trace.ToString())))
            .ToLowerInvariant();
        Console.WriteLine("SOURCE_MODEL_COMPARISONS " + comparisons);
        Console.WriteLine("EVENT_TRACE_SHA256 " + hash);
        Console.WriteLine("SOURCE_QA " + passed + " PASS " + failed + " FAIL Unity=NOT_RUN");
        return failed == 0 && comparisons == 49154 ? 0 : 1;
    }
}
