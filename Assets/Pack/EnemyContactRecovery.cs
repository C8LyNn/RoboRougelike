using UnityEngine;

namespace RogueMachine.P12PipelineSafe
{
    // Brief rams remain possible, but long contact causes a committed retreat.
    public sealed class EnemyContactRecovery
    {
        const float ContactLimit = 1.25f, RetreatDuration = 2.25f, ContactGapTolerance = .25f;
        float contactTime, contactGap, retreatRemaining;
        public bool Recovering => retreatRemaining > 0f;
        public bool Advance(bool contact, float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f) return Recovering;
            if (Recovering)
            {
                retreatRemaining = Mathf.Max(0f, retreatRemaining - deltaTime);
                contactTime = contactGap = 0f;
                return Recovering;
            }
            if(contact) { contactTime += deltaTime; contactGap=0f; }
            else { contactGap+=deltaTime; if(contactGap>ContactGapTolerance)contactTime=0f; }
            if (contactTime >= ContactLimit)
            {
                contactTime = contactGap = 0f;
                retreatRemaining = RetreatDuration;
            }
            return Recovering;
        }
        public void Reset() { contactTime = contactGap = retreatRemaining = 0f; }
    }
}
