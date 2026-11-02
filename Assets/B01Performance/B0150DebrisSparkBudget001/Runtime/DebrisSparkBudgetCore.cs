using System;

namespace RoboRougelike.B01Performance
{
    // Pure, allocation-free deterministic selection; no UnityEngine dependency.
    // Mutates only the caller-owned 'chosen' array, never Rigidbody or game state.
    public static class DebrisSparkBudgetCore
    {
        public static int ChooseFastest(float[] speedSquared, bool[] eligible,
            bool[] chosen, int count, int budget, float minSpeedSquared)
        {
            if (speedSquared == null || eligible == null || chosen == null ||
                count < 0 || count > speedSquared.Length || count > eligible.Length ||
                count > chosen.Length || budget < 0 ||
                float.IsNaN(minSpeedSquared) || float.IsInfinity(minSpeedSquared) ||
                minSpeedSquared < 0f)
                throw new ArgumentException("Invalid own budget input");

            // Clear only active prefix; caller owns scratch arrays.
            Array.Clear(chosen, 0, count);
            int result = 0;
            int limit = Math.Min(budget, count);
            for (int slot = 0; slot < limit; ++slot)
            {
                int best = -1;
                float bestSpeed = minSpeedSquared;
                for (int i = 0; i < count; ++i)
                {
                    float v = speedSquared[i];
                    if (!eligible[i] || chosen[i] || float.IsNaN(v) ||
                        float.IsInfinity(v) || v < minSpeedSquared) continue;
                    if (best < 0 || v > bestSpeed)
                    {
                        best = i;
                        bestSpeed = v;
                    }
                }
                if (best < 0) break;
                chosen[best] = true;
                ++result;
            }
            return result;
        }
    }
}
