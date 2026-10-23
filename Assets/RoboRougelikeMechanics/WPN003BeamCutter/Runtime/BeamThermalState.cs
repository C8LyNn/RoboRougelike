using UnityEngine;

namespace RoboRougelike.Mechanics.BeamCutter
{
    /// <summary>Pure heat / lockout state. Does not know about physics, energy or Unity frame rate.</summary>
    public sealed class BeamThermalState
    {
        private readonly float _capacity;
        private readonly float _heatPerSecond;
        private readonly float _coolPerSecond;
        private readonly float _restartHeat;

        public float Heat { get; private set; }
        public bool IsOverheated { get; private set; }
        public float HeatFraction => _capacity > 0f ? Heat / _capacity : 0f;

        public BeamThermalState(BeamCutterSettings settings)
        {
            BeamCutterSettings cfg = (settings ?? new BeamCutterSettings()).Sanitized();
            _capacity = cfg.heatCapacity;
            _heatPerSecond = cfg.heatPerSecond;
            _coolPerSecond = cfg.coolPerSecond;
            _restartHeat = cfg.heatCapacity * cfg.restartHeatFraction;
        }

        public float AvailableFireSeconds(float stepSeconds)
        {
            if (!BeamCutterSettings.IsFinite(stepSeconds) || stepSeconds <= 0f || IsOverheated) return 0f;
            if (_heatPerSecond <= 0f) return stepSeconds;
            return Mathf.Min(stepSeconds, Mathf.Max(0f, (_capacity - Heat) / _heatPerSecond));
        }

        public void AdvanceFiring(float firedSeconds, float stepSeconds)
        {
            if (!BeamCutterSettings.IsFinite(stepSeconds) || stepSeconds <= 0f) return;
            if (!BeamCutterSettings.IsFinite(firedSeconds) || firedSeconds < 0f) firedSeconds = 0f;
            firedSeconds = Mathf.Clamp(firedSeconds, 0f, stepSeconds);
            Heat = Mathf.Min(_capacity, Heat + firedSeconds * _heatPerSecond);
            if (_heatPerSecond > 0f && Heat >= _capacity - 0.00001f) IsOverheated = true;
            float remaining = stepSeconds - firedSeconds;
            if (remaining > 0f) AdvanceIdle(remaining);
        }

        public void AdvanceIdle(float stepSeconds)
        {
            if (!BeamCutterSettings.IsFinite(stepSeconds) || stepSeconds <= 0f) return;
            Heat = Mathf.Max(0f, Heat - _coolPerSecond * stepSeconds);
            if (IsOverheated && Heat <= _restartHeat + 0.00001f) IsOverheated = false;
        }

        public void Restore(float heat,bool locked)
        {
            Heat=BeamCutterSettings.IsFinite(heat)?Mathf.Clamp(heat,0,_capacity):0;
            IsOverheated=Heat>=_capacity||(locked&&Heat>_restartHeat);
        }
        public void Reset()
        {
            Heat = 0f;
            IsOverheated = false;
        }
    }
}
