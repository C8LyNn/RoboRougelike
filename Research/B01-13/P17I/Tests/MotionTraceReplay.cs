using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Swarm.B01.Playtesting;
using S = Swarm.B01.Playtesting.MotionStallLedger.State;

internal static class MotionTraceReplay
{
    private const string Header = "trace,t_ms,x_mm,z_mm,held,state,event,stalls,recoveries,discontinuities";

    private static int ParseNonnegative(string s, string name)
    {
        int n;
        if (!int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out n))
            throw new InvalidDataException("INVALID_" + name + ": " + s);
        return n;
    }

    private static bool ParseBit(string s, string name)
    {
        if (s == "0") return false;
        if (s == "1") return true;
        throw new InvalidDataException("INVALID_" + name + ": " + s);
    }

    private static S ParseState(string s)
    {
        S result;
        if (!Enum.TryParse<S>(s, false, out result) ||
            !string.Equals(Enum.GetName(typeof(S), result), s, StringComparison.Ordinal))
            throw new InvalidDataException("INVALID_STATE: " + s);
        return result;
    }

    public static int Main(string[] args)
    {
        if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
        {
            Console.Error.WriteLine("USAGE: MotionTraceReplay <immutable-fixture.csv>");
            return 2;
        }

        try { return Execute(args[0]); }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException ||
            e is InvalidDataException || e is ArgumentException)
        {
            Console.Error.WriteLine("INVALID_INPUT: " + e.Message);
            return 2;
        }
    }

    private static int Execute(string file)
    {
        string[] lines = File.ReadAllLines(file);
        if (lines.Length < 2 || lines[0] != Header)
            throw new InvalidDataException("BAD_HEADER_OR_NO_SAMPLES");
        var observed = new HashSet<string>(StringComparer.Ordinal);
        string current = null;
        var ledger = new MotionStallLedger();
        int lastTime = -1;
        int samples = 0, cases = 0;
        int totalStalls = 0, totalRec = 0, totalGaps = 0;
        for (int lineno = 1; lineno < lines.Length; ++lineno)
        {
            string line = lines[lineno];
            string[] parts = line.Split(',');
            if (parts.Length != 10) throw new InvalidDataException("BAD_COLUMN_COUNT at line " + (lineno + 1));
            string id = parts[0];
            if (id.Length < 2 || id.Length > 32)
                throw new InvalidDataException("INVALID_TRACE_ID at line " + (lineno + 1));
            int ms = ParseNonnegative(parts[1], "TIME");
            int x = ParseNonnegative(parts[2], "X_MM");
            int z = ParseNonnegative(parts[3], "Z_MM");
            bool held = ParseBit(parts[4], "HELD");
            S wanted = ParseState(parts[5]);
            bool eventWanted = ParseBit(parts[6], "EVENT");
            int stalls = ParseNonnegative(parts[7], "STALLS");
            int rec = ParseNonnegative(parts[8], "RECOVERIES");
            int gap = ParseNonnegative(parts[9], "DISCONTINUITIES");

            if (id != current)
            {
                if (observed.Contains(id))
                    throw new InvalidDataException("NON_CONTIGUOUS_TRACE " + id);
                if (current != null)
                {
                    Console.WriteLine("TRACE_PASS " + current + " stalls=" + ledger.SuspectedStallCount
                        + " recoveries=" + ledger.RecoveryCount + " gaps=" + ledger.DiscontinuityCount);
                    totalStalls += ledger.SuspectedStallCount;
                    totalRec += ledger.RecoveryCount;
                    totalGaps += ledger.DiscontinuityCount;
                }
                if (ms != 0) throw new InvalidDataException("FIRST_SAMPLE_NOT_ZERO " + id);
                observed.Add(id);
                current = id;
                lastTime = -1;
                ledger.Begin(0, x / 1000.0, z / 1000.0);
                cases++;
            }
            if (ms <= lastTime)
                throw new InvalidDataException("NON_MONOTONIC_TIME " + id + " line " + (lineno + 1));
            lastTime = ms;

            bool emitted = ledger.Observe(ms / 1000.0, x / 1000.0, z / 1000.0, held);
            samples++;
            if (ledger.CurrentState != wanted || emitted != eventWanted ||
                ledger.SuspectedStallCount != stalls || ledger.RecoveryCount != rec ||
                ledger.DiscontinuityCount != gap)
            {
                Console.Error.WriteLine("MISMATCH_TRACE " + id + " line=" + (lineno + 1)
                    + " ms=" + ms + " expected=" + wanted + "/" + (eventWanted ? 1 : 0)
                    + "/" + stalls + "/" + rec + "/" + gap + " actual=" + ledger.CurrentState
                    + "/" + (emitted ? 1 : 0) + "/" + ledger.SuspectedStallCount
                    + "/" + ledger.RecoveryCount + "/" + ledger.DiscontinuityCount);
                return 1;
            }
        }
        if (current != null)
        {
            Console.WriteLine("TRACE_PASS " + current + " stalls=" + ledger.SuspectedStallCount
                + " recoveries=" + ledger.RecoveryCount + " gaps=" + ledger.DiscontinuityCount);
            totalStalls += ledger.SuspectedStallCount;
            totalRec += ledger.RecoveryCount;
            totalGaps += ledger.DiscontinuityCount;
        }
        if (cases < 4 || samples < 50)
            throw new InvalidDataException("INADEQUATE_COVERAGE");
        Console.WriteLine("REPLAY_PASS traces=" + cases + " samples=" + samples +
            " stalls=" + totalStalls + " recoveries=" + totalRec + " discontinuities=" + totalGaps);
        return 0;
    }
}
