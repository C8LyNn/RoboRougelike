using System;

namespace B01.UXHaptic
{
    public enum HapticKind { CriticalDamage, PartDetached, Collision, WeaponImpact, Pickup }
    public enum HapticSide { Center, Left, Right }
    public enum HapticTier { Minor, Major, Critical }
    public enum HapticOutcome { Play, SuppressedDisabled, SuppressedZero, SuppressedBusy, SuppressedCooldown, IgnoredOldRun }

    // An authoritative event from the game host. Never synthesized from microphone/input data.
    public sealed class HapticEvent
    {
        public int RunId;
        public long Sequence;
        public long TimeMs;
        public HapticKind Kind;
        public HapticSide Side;
        public int Strength; // 0..100 requested output; source is validated.
    }

    // Immutable intention. Only a future authorized Unity/Input System adapter can drive motors.
    public sealed class HapticDecision
    {
        public readonly int RunId;
        public readonly long Sequence, TimeMs;
        public readonly HapticKind Kind;
        public readonly HapticSide Side;
        public readonly HapticTier Tier;
        public readonly HapticOutcome Outcome;
        public readonly int LeftPercent, RightPercent, DurationMs;
        public readonly bool PreemptPrevious;
        public readonly bool RequireVisibleCriticalAlert;

        internal HapticDecision(HapticEvent cue, HapticTier tier, HapticOutcome outcome,
            int left, int right, int duration, bool preempt)
        {
            RunId=cue.RunId;Sequence=cue.Sequence;TimeMs=cue.TimeMs;
            Kind=cue.Kind;Side=cue.Side;Tier=tier;Outcome=outcome;
            LeftPercent=left;RightPercent=right;DurationMs=duration;
            PreemptPrevious=preempt;
            // A critical warning must be visible, whether or not motors can play.
            RequireVisibleCriticalAlert=cue.Kind==HapticKind.CriticalDamage;
        }
    }

    // Deterministic, per-run bounded motor-command policy, not a Unity hardware driver.
    public sealed class HapticGate
    {
        public const long NormalCooldownMs=180;
        readonly int _criticalCap, _majorCap, _minorCap;
        int _runId;
        bool _enabled;
        long _nowMs, _lastSequence, _lastPlayedMs=-1, _activeUntilMs;
        HapticTier _activeTier;

        public HapticGate(int runId, bool enabled=true,
            int criticalStrengthCap=80, int majorStrengthCap=60, int minorStrengthCap=30)
        {
            if(runId<0||criticalStrengthCap<0||criticalStrengthCap>100||
                majorStrengthCap<0||majorStrengthCap>100||
                minorStrengthCap<0||minorStrengthCap>100)
                throw new ArgumentOutOfRangeException("haptic configuration");
            _runId=runId;_enabled=enabled;
            _criticalCap=criticalStrengthCap;_majorCap=majorStrengthCap;_minorCap=minorStrengthCap;
        }
        public int RunId { get {return _runId;} }
        public bool Enabled { get {return _enabled;} }
        public long NowMs { get {return _nowMs;} }
        public long ActiveUntilMs { get {return _activeUntilMs;} }
        public long LastPlayedMs { get {return _lastPlayedMs;} }

        // Return true to tell the hardware adapter to stop any currently active motors.
        public bool SetEnabled(bool enabled)
        {
            bool stop=_enabled && !enabled && _activeUntilMs>_nowMs;
            _enabled=enabled;
            if(!enabled)_activeUntilMs=_nowMs;
            // Preserve last-played cooldown across toggles, prevents repeated on/off bypass.
            return stop;
        }
        public void AdvanceTo(long timeMs)
        {
            if(timeMs<_nowMs)throw new ArgumentOutOfRangeException("timeMs");
            _nowMs=timeMs;
        }
        public void StartNewRun(int nextRunId)
        {
            if(nextRunId<=_runId)throw new ArgumentOutOfRangeException("nextRunId");
            _runId=nextRunId;_nowMs=0;_lastSequence=0;
            _lastPlayedMs=-1;_activeUntilMs=0;_activeTier=HapticTier.Minor;
        }

        public static HapticTier TierOf(HapticKind kind)
        {
            if(!Enum.IsDefined(typeof(HapticKind),kind))
                throw new ArgumentOutOfRangeException("kind");
            if(kind==HapticKind.CriticalDamage)return HapticTier.Critical;
            if(kind==HapticKind.PartDetached||kind==HapticKind.Collision)return HapticTier.Major;
            return HapticTier.Minor;
        }

        public HapticDecision Record(HapticEvent cue)
        {
            if(cue==null)throw new ArgumentNullException("cue");
            if(cue.RunId<_runId)
                return new HapticDecision(cue,HapticTier.Minor,HapticOutcome.IgnoredOldRun,0,0,0,false);
            if(cue.RunId!=_runId||cue.Sequence<=_lastSequence||
                cue.TimeMs<0||cue.TimeMs<_nowMs||cue.Strength<0||cue.Strength>100||
                !Enum.IsDefined(typeof(HapticKind),cue.Kind)||
                !Enum.IsDefined(typeof(HapticSide),cue.Side))
                throw new ArgumentException("Invalid authoritative haptic event","cue");

            _nowMs=cue.TimeMs;_lastSequence=cue.Sequence;
            HapticTier tier=TierOf(cue.Kind);
            if(!_enabled)return Rejected(cue,tier,HapticOutcome.SuppressedDisabled);
            if(cue.Strength==0)return Rejected(cue,tier,HapticOutcome.SuppressedZero);
            int cap=tier==HapticTier.Critical?_criticalCap:
                tier==HapticTier.Major?_majorCap:_minorCap;
            int level=Math.Min(cue.Strength,cap);
            if(level==0)return Rejected(cue,tier,HapticOutcome.SuppressedZero);

            bool busy=_nowMs<_activeUntilMs;
            bool preempt=busy&&tier>_activeTier;
            if(busy&&!preempt)
                return Rejected(cue,tier,HapticOutcome.SuppressedBusy);
            // Major over Minor and Critical over any tier may preempt immediately.
            // Minor and unopposed Major obey a global anti-spam refractory interval.
            if(!preempt&&tier!=HapticTier.Critical&&_lastPlayedMs>=0&&
                _nowMs-_lastPlayedMs<NormalCooldownMs)
                return Rejected(cue,tier,HapticOutcome.SuppressedCooldown);

            int duration=tier==HapticTier.Critical?300:tier==HapticTier.Major?160:80;
            int secondary=level/3;
            int left=cue.Side==HapticSide.Right?secondary:level;
            int right=cue.Side==HapticSide.Left?secondary:level;
            _lastPlayedMs=_nowMs;
            _activeTier=tier;
            // Saturation prevents long.MaxValue addition overflow.
            _activeUntilMs=_nowMs>long.MaxValue-duration?long.MaxValue:_nowMs+duration;
            return new HapticDecision(cue,tier,HapticOutcome.Play,left,right,duration,preempt);
        }
        static HapticDecision Rejected(HapticEvent cue,HapticTier tier,HapticOutcome why)
        { return new HapticDecision(cue,tier,why,0,0,0,false); }
    }
}
