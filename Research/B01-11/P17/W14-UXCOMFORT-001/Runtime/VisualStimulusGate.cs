using System;
using System.Collections.Generic;

namespace B01.UXComfort
{
    // Pure deterministic visual stimulus scheduling. NEVER an epilepsy-safety certification.
    public enum VisualCueKind { ExplosionFlash, DamageFlash, CriticalFlash,
        CollisionShake, PartDetachShake, PickupHighlight, CriticalWarning }
    public enum ComfortMode { Standard, ReducedMotion, NoFlashes }
    public enum RenderMode { TimedEffect, ReducedEffect, StaticIndicator }
    public enum Indicator { Explosion, Damage, Critical, Collision, Detach, Pickup }
    public enum CueOutcome { Scheduled, ReplacedByStatic, IgnoredPreviousRun }

    public sealed class VisualCue
    {
        public int RunId;
        public long Sequence;
        public long TimeMs;
        public VisualCueKind Kind;
        public int Intensity;
    }

    public sealed class VisualDecision
    {
        public readonly int RunId;
        public readonly long Sequence, TimeMs;
        public readonly VisualCueKind Kind;
        public readonly RenderMode RenderMode;
        public readonly Indicator Indicator;
        public readonly int Intensity;
        public readonly CueOutcome Outcome;
        internal VisualDecision(VisualCue input, RenderMode mode, int intensity, Indicator indicator)
        {
            RunId=input.RunId; Sequence=input.Sequence; TimeMs=input.TimeMs;
            Kind=input.Kind; RenderMode=mode; Intensity=intensity; Indicator=indicator;
            Outcome=mode==RenderMode.StaticIndicator?CueOutcome.ReplacedByStatic:CueOutcome.Scheduled;
        }
    }

    // Host supplies clock, events and settings. This returns a plan; it never displays effects.
    public sealed class VisualStimulusGate
    {
        public const long FlashWindowMs=1000;
        readonly List<long> _flashTimes=new List<long>();
        readonly int _maxFlashes, _minFlashGapMs, _maxShakeIntensity;
        int _runId;
        long _nowMs,_lastSequence;
        ComfortMode _mode;

        public VisualStimulusGate(int runId, ComfortMode mode=ComfortMode.Standard,
            int maxFlashesPerSecond=2,int minFlashGapMs=250,int maxShakeIntensity=45)
        {
            if(runId<0 || !Enum.IsDefined(typeof(ComfortMode),mode) ||
                maxFlashesPerSecond<0 || maxFlashesPerSecond>4 ||
                minFlashGapMs<0 || minFlashGapMs>1000 ||
                maxShakeIntensity<0 || maxShakeIntensity>100)
                throw new ArgumentOutOfRangeException("gate");
            _runId=runId; _mode=mode; _maxFlashes=maxFlashesPerSecond;
            _minFlashGapMs=minFlashGapMs;_maxShakeIntensity=maxShakeIntensity;
        }
        public int RunId {get{return _runId;}}
        public long NowMs {get{return _nowMs;}}
        public ComfortMode Mode {get{return _mode;}}
        public int FlashCountWindow {get{return _flashTimes.Count;}}

        public void SetMode(ComfortMode mode)
        {
            if(!Enum.IsDefined(typeof(ComfortMode),mode))
                throw new ArgumentOutOfRangeException("mode");
            // Mode changes intentionally retain prior flash budget timestamps.
            _mode=mode;
        }
        public void StartNewRun(int newRunId)
        {
            if(newRunId<=_runId)
                throw new ArgumentOutOfRangeException("newRunId");
            _runId=newRunId;_nowMs=0;_lastSequence=0;_flashTimes.Clear();
        }
        public void AdvanceTo(long timeMs)
        {
            if(timeMs<_nowMs)throw new ArgumentOutOfRangeException("timeMs");
            _nowMs=timeMs;
            ExpireFlashes();
        }
        static bool IsFlash(VisualCueKind kind)
        {return kind==VisualCueKind.ExplosionFlash || kind==VisualCueKind.DamageFlash ||
            kind==VisualCueKind.CriticalFlash;}
        static bool IsShake(VisualCueKind kind)
        {return kind==VisualCueKind.CollisionShake || kind==VisualCueKind.PartDetachShake;}
        static Indicator IndicatorOf(VisualCueKind kind)
        {
            switch(kind)
            {
                case VisualCueKind.ExplosionFlash:return Indicator.Explosion;
                case VisualCueKind.DamageFlash:return Indicator.Damage;
                case VisualCueKind.CriticalFlash:
                case VisualCueKind.CriticalWarning:return Indicator.Critical;
                case VisualCueKind.CollisionShake:return Indicator.Collision;
                case VisualCueKind.PartDetachShake:return Indicator.Detach;
                default:return Indicator.Pickup;
            }
        }
        void ExpireFlashes()
        {
            while(_flashTimes.Count>0 && _nowMs-_flashTimes[0]>=FlashWindowMs)
                _flashTimes.RemoveAt(0);
        }
        public VisualDecision Record(VisualCue cue)
        {
            if(cue==null)throw new ArgumentNullException("cue");
            if(cue.RunId<_runId)return null; // Explicit old-run ignore, no state mutation.
            if(cue.RunId!=_runId || cue.Sequence<=_lastSequence ||
                cue.TimeMs<0 || cue.TimeMs<_nowMs ||
                cue.Intensity<0 || cue.Intensity>100 ||
                !Enum.IsDefined(typeof(VisualCueKind),cue.Kind))
                throw new ArgumentException("Invalid authoritative visual cue", "cue");
            _nowMs=cue.TimeMs;
            _lastSequence=cue.Sequence;
            ExpireFlashes();
            var indicator=IndicatorOf(cue.Kind);
            if(IsFlash(cue.Kind))
            {
                // Zero-strength cues are not emitted flashes. Returning a static
                // semantic indicator preserves feedback without spending quota or
                // delaying subsequent nonzero real visual effects.
                if(cue.Intensity==0)
                    return new VisualDecision(cue,RenderMode.StaticIndicator,0,indicator);
                int maxPerWindow=_mode==ComfortMode.Standard?_maxFlashes:
                    _mode==ComfortMode.ReducedMotion?Math.Min(1,_maxFlashes):0;
                int gap=_mode==ComfortMode.ReducedMotion?
                    Math.Max(500,_minFlashGapMs):_minFlashGapMs;
                bool timeGapOk=_flashTimes.Count==0 ||
                    _nowMs-_flashTimes[_flashTimes.Count-1]>=gap;
                if(maxPerWindow==0||_flashTimes.Count>=maxPerWindow||!timeGapOk)
                    return new VisualDecision(cue,RenderMode.StaticIndicator,0,indicator);
                _flashTimes.Add(_nowMs);
                var reduced=_mode==ComfortMode.ReducedMotion;
                return new VisualDecision(cue,reduced?RenderMode.ReducedEffect:RenderMode.TimedEffect,
                    Math.Min(cue.Intensity,reduced?20:60),indicator);
            }
            if(IsShake(cue.Kind))
            {
                // Do not schedule a zero-amplitude camera motion.
                if(cue.Intensity==0)
                    return new VisualDecision(cue,RenderMode.StaticIndicator,0,indicator);
                if(_mode==ComfortMode.ReducedMotion||_maxShakeIntensity==0)
                    return new VisualDecision(cue,RenderMode.StaticIndicator,0,indicator);
                int value=Math.Min(cue.Intensity,_maxShakeIntensity);
                return new VisualDecision(cue,RenderMode.TimedEffect,value,indicator);
            }
            // Informational cues are always non-flashing symbolic indicators.
            return new VisualDecision(cue,RenderMode.StaticIndicator,0,indicator);
        }
    }
}
