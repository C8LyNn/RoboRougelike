using UnityEngine;

namespace RoboRougelike.Weapons.RotarySaw
{
    // Pure, deterministic tick-based state; independent of MonoBehaviour and Physics.
    public sealed class RotarySawMotor
    {
        private readonly RotarySawConfiguration config;
        public float Rpm { get; private set; }
        public float Heat { get; private set; }
        public float JamRemaining { get; private set; }
        public bool Overheated { get; private set; }
        public bool Powered { get; private set; }
        public float NormalizedRpm { get { return Mathf.Clamp01(Rpm / config.maximumRpm); } }
        public bool CanCut { get { return Powered && !Overheated && JamRemaining <= 0f && Rpm >= config.minimumCutRpm; } }

        public RotarySawMotor(RotarySawConfiguration configuration)
        {
            config = (configuration ?? new RotarySawConfiguration()).Copy();
            config.ClampToSafeValues();
        }

        public void Tick(bool motorRequested, bool energyAvailable, bool operational, float deltaSeconds)
        {
            float dt = RotarySawConfiguration.ClampFinite(deltaSeconds, 0f, 1f, 0f);
            bool wasJammed = JamRemaining > 0f;
            JamRemaining = Mathf.Max(0f, JamRemaining - dt);
            bool canSpin = operational && motorRequested && energyAvailable && !wasJammed && !Overheated;
            Rpm = Mathf.MoveTowards(Rpm, canSpin ? config.maximumRpm : 0f,
                (canSpin ? config.accelerationRpmPerSecond : config.decelerationRpmPerSecond) * dt);
            Powered = canSpin;

            if (canSpin)
                Heat = Mathf.Min(config.heatCapacity, Heat + config.heatPerSecondAtFullRpm * NormalizedRpm * dt);
            else
                Heat = Mathf.Max(0f, Heat - config.coolingPerSecond * dt);

            if (Heat >= config.heatCapacity) Overheated = true;
            else if (Overheated && Heat <= config.heatCapacity * config.resumeHeatFraction)
                Overheated = false;

            if (Overheated) Powered = false;
        }

        public void Jam()
        {
            JamRemaining = Mathf.Max(JamRemaining, config.jamSeconds);
            Heat = Mathf.Min(config.heatCapacity, Heat + config.jamHeatPenalty);
            Rpm = 0f;
            Powered = false;
            if (Heat >= config.heatCapacity) Overheated = true;
        }

        public void StopImmediately()
        {
            Rpm = 0f;
            Powered = false;
        }

        public void RestoreState(float heat,bool locked,float jam)
        {
            Rpm=0;Powered=false;
            Heat=RotarySawConfiguration.ClampFinite(heat,0,config.heatCapacity,0);
            Overheated=Heat>=config.heatCapacity||(locked&&Heat>config.heatCapacity*config.resumeHeatFraction);
            JamRemaining=RotarySawConfiguration.ClampFinite(jam,0,config.jamSeconds,0);
        }
        public void Reset()
        {
            Rpm = 0f;
            Heat = 0f;
            JamRemaining = 0f;
            Powered = false;
            Overheated = false;
        }
    }
}
