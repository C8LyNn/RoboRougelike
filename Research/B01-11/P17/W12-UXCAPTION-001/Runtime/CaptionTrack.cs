using System;
using System.Collections.Generic;

namespace B01.UXCaption
{
    // Authoritative, host-provided sound cues. No microphone, audio capture or free-text content.
    public enum SoundCueKind { Footsteps, WeaponFire, HeavyImpact, PartDetached, CriticalWarning, Pickup, Machinery }
    public enum CaptionPriority { Normal, Urgent, Critical }
    public enum CaptionDirection { Front, FrontRight, Right, BackRight, Back, BackLeft, Left, FrontLeft }
    public enum CaptionOutcome { Accepted, Combined, DroppedNormalForUrgent, IgnoredPreviousRun, DroppedLowerPriorityForCritical }

    public sealed class SoundCueInput
    {
        public int RunId;
        public long Sequence;
        public long TimeMs;
        public SoundCueKind Kind;
        public string LocalizationKey;
        public string SourceId;
        // Degrees relative to player's facing. Positive = clockwise/right, negative = left.
        public int BearingDegrees;
    }

    // Immutable, trusted-data snapshot. Localization, color, visuals and rendering are host owned.
    public sealed class Caption
    {
        public readonly int RunId;
        public readonly long FirstSequence, LastSequence, FirstTimeMs, LastTimeMs;
        public readonly SoundCueKind Kind;
        public readonly string LocalizationKey, SourceId;
        public readonly CaptionDirection Direction;
        public readonly CaptionPriority Priority;
        public readonly int Occurrences;

        internal Caption(SoundCueInput x)
        {
            RunId=x.RunId; FirstSequence=x.Sequence; LastSequence=x.Sequence;
            FirstTimeMs=x.TimeMs; LastTimeMs=x.TimeMs; Kind=x.Kind;
            LocalizationKey=x.LocalizationKey; SourceId=x.SourceId;
            Direction=CaptionTrack.DirectionOf(x.BearingDegrees);
            Priority=CaptionTrack.PriorityOf(x.Kind);
            Occurrences=1;
        }
        internal Caption(Caption older, SoundCueInput x)
        {
            RunId=older.RunId;FirstSequence=older.FirstSequence;LastSequence=x.Sequence;
            FirstTimeMs=older.FirstTimeMs;LastTimeMs=x.TimeMs;Kind=older.Kind;
            LocalizationKey=older.LocalizationKey;SourceId=older.SourceId;
            Direction=older.Direction;Priority=older.Priority;
            Occurrences=checked(older.Occurrences+1);
        }
    }

    // A finite audio accessibility caption buffer, independent from gameplay event HUD/history.
    public sealed class CaptionTrack
    {
        public const long CombineWithinMs=450;
        public const long NormalDurationMs=2400;
        public const long UrgentDurationMs=6000;
        readonly int _capacity, _visibleSlots;
        readonly List<Caption> _active=new List<Caption>();
        int _run;
        long _clock, _lastSequence;

        public CaptionTrack(int runId,int capacity=6,int visibleSlots=4)
        {
            if(runId<0 || capacity<1 || capacity>32 || visibleSlots<1 ||
               visibleSlots>6 || visibleSlots>capacity)
                throw new ArgumentOutOfRangeException("caption configuration");
            _run=runId;_capacity=capacity;_visibleSlots=visibleSlots;
        }
        public int RunId {get{return _run;}}
        public long NowMs {get{return _clock;}}
        public int ActiveCount {get{return _active.Count;}}

        public void StartNewRun(int newRunId)
        {
            if(newRunId<=_run)throw new ArgumentOutOfRangeException("newRunId");
            _active.Clear();_run=newRunId;_clock=0;_lastSequence=0;
        }
        public void AdvanceTo(long timeMs)
        {
            if(timeMs<_clock)throw new ArgumentOutOfRangeException("timeMs");
            _clock=timeMs;
            PurgeExpired();
        }
        public CaptionOutcome Record(SoundCueInput x)
        {
            if(x==null)throw new ArgumentNullException("x");
            if(x.RunId<_run)return CaptionOutcome.IgnoredPreviousRun;
            if(x.RunId!=_run || x.Sequence<=_lastSequence ||
                x.TimeMs<0 || x.TimeMs<_clock || !Enum.IsDefined(typeof(SoundCueKind),x.Kind) ||
                x.BearingDegrees< -180 || x.BearingDegrees>180 ||
                !ValidKey(x.LocalizationKey) || !ValidSource(x.SourceId))
                throw new ArgumentException("Invalid authoritative sound cue", "x");

            _clock=x.TimeMs;
            PurgeExpired();
            var direction=DirectionOf(x.BearingDegrees);
            for(int i=_active.Count-1;i>=0;i--)
            {
                var old=_active[i];
                if(old.Kind==x.Kind && old.SourceId==x.SourceId &&
                   old.LocalizationKey==x.LocalizationKey && old.Direction==direction &&
                   x.TimeMs-old.LastTimeMs<=CombineWithinMs && old.Occurrences<int.MaxValue)
                {
                    _active.RemoveAt(i);
                    _active.Add(new Caption(old,x));
                    _lastSequence=x.Sequence;
                    return CaptionOutcome.Combined;
                }
            }

            var priority=PriorityOf(x.Kind);
            if(_active.Count>=_capacity)
            {
                // Evict the oldest item in the LOWEST tier at or below the incoming
                // tier; never evict Critical for a PartDetached or Normal event.
                int victim=-1,lowestTier=int.MaxValue;
                for(int i=0;i<_active.Count;i++)
                {
                    int tier=(int)_active[i].Priority;
                    if(tier<=(int)priority && tier<lowestTier)
                    {
                        victim=i;
                        lowestTier=tier;
                    }
                }
                if(victim<0)
                {
                    _lastSequence=x.Sequence; // Even dropped source cues cannot replay.
                    return priority==CaptionPriority.Normal
                        ?CaptionOutcome.DroppedNormalForUrgent
                        :CaptionOutcome.DroppedLowerPriorityForCritical;
                }
                _active.RemoveAt(victim);
            }
            _active.Add(new Caption(x));
            _lastSequence=x.Sequence;
            return CaptionOutcome.Accepted;
        }
        public Caption[] Visible(bool urgentOnly=false)
        {
            var available=new List<Caption>();
            foreach(var c in _active)
                if(IsLive(c) && (!urgentOnly || c.Priority>=CaptionPriority.Urgent))
                    available.Add(c);
            available.Sort((a,b)=>
            {
                int priority=b.Priority.CompareTo(a.Priority);
                if(priority!=0)return priority;
                int time=b.LastTimeMs.CompareTo(a.LastTimeMs);
                return time!=0?time:b.LastSequence.CompareTo(a.LastSequence);
            });
            if(available.Count>_visibleSlots)
                available.RemoveRange(_visibleSlots,available.Count-_visibleSlots);
            return available.ToArray();
        }
        bool IsLive(Caption c)
        {
            long ttl=c.Priority>=CaptionPriority.Urgent?UrgentDurationMs:NormalDurationMs;
            return _clock>=c.LastTimeMs && _clock-c.LastTimeMs<ttl;
        }
        void PurgeExpired()
        {
            for(int i=_active.Count-1;i>=0;i--)
                if(!IsLive(_active[i]))_active.RemoveAt(i);
        }
        public static CaptionPriority PriorityOf(SoundCueKind kind)
        {
            if(!Enum.IsDefined(typeof(SoundCueKind),kind))
                throw new ArgumentOutOfRangeException("kind");
            if(kind==SoundCueKind.CriticalWarning)return CaptionPriority.Critical;
            return kind==SoundCueKind.PartDetached?CaptionPriority.Urgent:CaptionPriority.Normal;
        }
        public static CaptionDirection DirectionOf(int bearingDegrees)
        {
            if(bearingDegrees< -180 || bearingDegrees>180)
                throw new ArgumentOutOfRangeException("bearingDegrees");
            int normalized=(bearingDegrees+360)%360;
            return (CaptionDirection)(((normalized*2+45)/90)%8);
        }
        static bool ValidSource(string id)
        {
            if(string.IsNullOrEmpty(id)||id.Length>40)return false;
            foreach(char c in id)
                if(!((c>='a'&&c<='z')||(c>='A'&&c<='Z')||
                     (c>='0'&&c<='9')||c=='_'||c=='-'||c=='.'||c==':'))
                    return false;
            return true;
        }
        static bool ValidKey(string key)
        {
            if(string.IsNullOrEmpty(key)||key.Length<5||key.Length>64||
                !key.StartsWith("cue.",StringComparison.Ordinal))return false;
            foreach(char c in key)
                if(!((c>='a'&&c<='z')||(c>='0'&&c<='9')||
                     c=='.'||c=='_'||c=='-'))
                    return false;
            return true;
        }
    }
}
