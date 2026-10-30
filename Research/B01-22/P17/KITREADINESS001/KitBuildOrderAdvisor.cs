// B01-22 P17/W12, separate Research-only C#9 experiment.
// Reorders up to six requested kit builds to maximize original planner's
// READY count. Does NOT add/consume real stock, purchase, mount or save.
using System;
using System.Collections.Generic;
using System.Linq;
using B01KitReadiness001;

internal sealed class KitBuildOrderAdvice
{
    public string Status;
    public int ExactSearchRequestLimit, OrdersEvaluated;
    public int OriginalReadyCount, SuggestedReadyCount;
    public bool Improved;
    public string[] SuggestedRequests;
}

internal static class KitBuildOrderAdvisor
{
    // 6! worst-case 720 exact original planner evaluations.
    // For longer user lists fail open in the sense of returning a *clearly*
    // NOT_EVALUATED advice, never masquerading as an optimal schedule.
    internal const int MaxExactRequests = 6;

    internal static KitBuildOrderAdvice Advise(string sourceSha, KitRecipe[] approved,
        OwnedPart[] owned, string[] originalRequests)
    {
        // Re-check all original stock and 154 source-pinned BOM/price fields.
        // This is a physical-kit *preview*, not a game state transaction.
        ReadinessPlan baseline = KitReadinessPlanner.Preview(
            sourceSha, approved, owned, originalRequests);
        var answer = new KitBuildOrderAdvice {
            Status = "NOT_EVALUATED_REQUEST_LIMIT",
            ExactSearchRequestLimit = MaxExactRequests,
            OrdersEvaluated = 0,
            OriginalReadyCount = baseline.ReadyCount,
            SuggestedReadyCount = baseline.ReadyCount,
            Improved = false,
            SuggestedRequests = (string[])originalRequests.Clone()
        };
        if (originalRequests.Length > MaxExactRequests)
            return answer;

        // All unique ordered combinations are actually evaluated with
        // KitReadinessPlanner.Preview: no duplicate rule implementation.
        answer.Status = "EXACT_EVALUATED";
        var used = new bool[originalRequests.Length];
        var permutation = new string[originalRequests.Length];

        int bestReady = baseline.ReadyCount;
        int bestMoved = 0; // unchanged original sequence wins exact ties
        void Search(int depth)
        {
            if (depth == permutation.Length)
            {
                int score = KitReadinessPlanner.Preview(
                    sourceSha, approved, owned, permutation).ReadyCount;
                answer.OrdersEvaluated = checked(answer.OrdersEvaluated + 1);
                int moved = 0;
                for (int i = 0; i < permutation.Length; ++i)
                    if (!String.Equals(permutation[i], originalRequests[i],
                        StringComparison.Ordinal)) ++moved;

                if (score > bestReady || (score == bestReady && moved < bestMoved))
                {
                    bestReady = score;
                    bestMoved = moved;
                    answer.SuggestedRequests = (string[])permutation.Clone();
                }
                return;
            }
            // Duplicate kit labels at the same depth are equivalent orders.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < originalRequests.Length; ++i)
            {
                if (used[i] || !seen.Add(originalRequests[i])) continue;
                used[i] = true;
                permutation[depth] = originalRequests[i];
                Search(depth + 1);
                used[i] = false;
            }
        }
        Search(0);
        answer.SuggestedReadyCount = bestReady;
        answer.Improved = bestReady > baseline.ReadyCount;
        return answer;
    }
}
