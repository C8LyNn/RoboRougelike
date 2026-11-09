using UnityEngine;

namespace RogueMachine.P12PipelineSafe
{
    // Brief rams remain possible, but long contact causes a committed retreat.
    public sealed class EnemyContactRecovery
    {
        const float ContactLimit = 1.25f, RetreatDuration = 2.25f;
        float contactTime, retreatRemaining;
        public bool Recovering => retreatRemaining > 0f;
        public bool Advance(bool contact, float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f) return Recovering;
            if (Recovering)
            {
                retreatRemaining = Mathf.Max(0f, retreatRemaining - deltaTime);
                contactTime = 0f;
                return Recovering;
            }
            contactTime = contact ? contactTime + deltaTime : 0f;
            if (contactTime >= ContactLimit)
            {
                contactTime = 0f;
                retreatRemaining = RetreatDuration;
            }
            return Recovering;
        }
        public void Reset() { contactTime = 0f; retreatRemaining = 0f; }
    }
}
