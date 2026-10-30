using System;
using System.Collections.Generic;

namespace B01.UXFeed
{
    // Host-owned, authoritative gameplay events ONLY. No UnityEngine or gameplay state mutation.
    public enum TacticalKind { CoreHit, PartDetached, WeaponOffline, EnemyDestroyed, SalvageRecovered, ObjectiveChanged }
    public enum TacticalPriority { Normal, Critical }
    public enum RecordOutcome { Accepted, Coalesced, IgnoredPreviousRun }

    public sealed class TacticalInput
    {
        public int RunId;
        public long Sequence;
        public long TimeMs;
        public TacticalKind Kind;
        public string SubjectId;
        public int Amount = 1;
    }

    public sealed class TacticalItem
    {
        public readonly int RunId;
        public readonly long FirstSequence;
        public readonly long LastSequence;
        public readonly long FirstTimeMs;
        public readonly long LastTimeMs;
        public readonly TacticalKind Kind;
        public readonly TacticalPriority Priority;
        public readonly string SubjectId;
        public readonly long TotalAmount;
        public readonly int Occurrences;

        internal TacticalItem(TacticalInput x, TacticalPriority priority)
        {
            RunId=x.RunId;FirstSequence=x.Sequence;LastSequence=x.Sequence;
            FirstTimeMs=x.TimeMs;LastTimeMs=x.TimeMs;Kind=x.Kind;
            Priority=priority;SubjectId=x.SubjectId;TotalAmount=x.Amount;Occurrences=1;
        }
        internal TacticalItem(TacticalItem previous,TacticalInput x)
        {
            RunId=previous.RunId;FirstSequence=previous.FirstSequence;
            LastSequence=x.Sequence;FirstTimeMs=previous.FirstTimeMs;
            LastTimeMs=x.TimeMs;Kind=previous.Kind;Priority=previous.Priority;
            SubjectId=previous.SubjectId;TotalAmount=checked(previous.TotalAmount+x.Amount);
            Occurrences=checked(previous.Occurrences+1);
        }
    }

    // Per-run finite history; severity-prioritized transient HUD view; no platform clock needed.
    public sealed class TacticalFeed
    {
        public const long CombineWindowMs = 750;
        public const long NormalTtlMs = 3000;
        public const long CriticalTtlMs = 6500;
        readonly int _capacity;
        readonly int _visibleSlots;
        readonly List<TacticalItem> _history=new List<TacticalItem>();
        // Active critical HUD alerts must outlive chronological history eviction.
        // Bound this independent queue by visibleSlots (<=6), not by history capacity.
        readonly List<TacticalItem> _criticalVisible=new List<TacticalItem>();
        int _runId;
        long _nowMs;
        long _lastSequence;

        public TacticalFeed(int initialRunId,int capacity=32,int visibleSlots=3)
        {
            if(initialRunId<0||capacity<1||capacity>256||
                visibleSlots<1||visibleSlots>6||visibleSlots>capacity)
                throw new ArgumentOutOfRangeException("feed configuration");
            _runId=initialRunId;_capacity=capacity;_visibleSlots=visibleSlots;
        }
        public int RunId {get{return _runId;}}
        public long NowMs {get{return _nowMs;}}
        public int Count {get{return _history.Count;}}

        public void StartNewRun(int runId)
        {
            if(runId<=_runId)throw new ArgumentOutOfRangeException("runId");
            _history.Clear();_criticalVisible.Clear();_runId=runId;_nowMs=0;_lastSequence=0;
        }
        public void AdvanceTo(long nowMs)
        {
            if(nowMs<_nowMs)throw new ArgumentOutOfRangeException("nowMs");
            _nowMs=nowMs;
        }

        public RecordOutcome Record(TacticalInput x)
        {
            if(x==null)throw new ArgumentNullException("x");
            if(x.RunId<_runId)return RecordOutcome.IgnoredPreviousRun;
            if(x.RunId!=_runId)throw new ArgumentException("Run does not match active session","x");
            if(!Enum.IsDefined(typeof(TacticalKind),x.Kind) ||
                x.Sequence<=_lastSequence || x.TimeMs<_nowMs || x.TimeMs<0 ||
                x.Amount<1 || x.Amount>1000000 || !ValidId(x.SubjectId))
                throw new ArgumentException("Invalid authoritative event","x");

            var priority=PriorityOf(x.Kind);
            if(_history.Count>0 && IsCombinable(x.Kind) &&
               _history[_history.Count-1].Kind==x.Kind &&
               _history[_history.Count-1].SubjectId==x.SubjectId &&
               x.TimeMs>=_history[_history.Count-1].LastTimeMs &&
               x.TimeMs-_history[_history.Count-1].LastTimeMs<=CombineWindowMs &&
               _history[_history.Count-1].Occurrences<int.MaxValue)
            {
                var prev=_history[_history.Count-1];
                _history[_history.Count-1]=new TacticalItem(prev,x);
                _lastSequence=x.Sequence;_nowMs=x.TimeMs;
                return RecordOutcome.Coalesced;
            }
            var item=new TacticalItem(x,priority);
            _history.Add(item);
            if(_history.Count>_capacity)_history.RemoveAt(0);
            if(priority==TacticalPriority.Critical)
            {
                _criticalVisible.Add(item);
                if(_criticalVisible.Count>_visibleSlots)_criticalVisible.RemoveAt(0);
            }
            _lastSequence=x.Sequence;_nowMs=x.TimeMs;
            return RecordOutcome.Accepted;
        }

        public TacticalItem[] History()
        {
            // Immutable item snapshots in chronological order, even after visible TTL expires.
            return _history.ToArray();
        }

        public TacticalItem[] Visible(bool includeNormal=true)
        {
            var candidates=new List<TacticalItem>();
            // Critical alerts are transient UI state, not dependent on history survival.
            foreach(var item in _criticalVisible)
            {
                long expires=item.LastTimeMs>long.MaxValue-CriticalTtlMs
                    ?long.MaxValue:item.LastTimeMs+CriticalTtlMs;
                if(_nowMs<expires)candidates.Add(item);
            }
            if(includeNormal)
            {
                foreach(var item in _history)
                {
                    if(item.Priority!=TacticalPriority.Normal)continue;
                    long expires=item.LastTimeMs>long.MaxValue-NormalTtlMs
                        ?long.MaxValue:item.LastTimeMs+NormalTtlMs;
                    if(_nowMs<expires)candidates.Add(item);
                }
            }
            candidates.Sort((a,b)=>
            {
                var priority=b.Priority.CompareTo(a.Priority);
                if(priority!=0)return priority;
                var time=b.LastTimeMs.CompareTo(a.LastTimeMs);
                return time!=0?time:b.LastSequence.CompareTo(a.LastSequence);
            });
            if(candidates.Count>_visibleSlots)candidates.RemoveRange(_visibleSlots,candidates.Count-_visibleSlots);
            return candidates.ToArray();
        }


        // Optional UI-only acknowledgement. Exact object identity prevents an older
        // visible snapshot from dismissing a newer alert for the same subject.
        // This never changes authoritative gameplay data or chronological history.
        public bool AcknowledgeCritical(TacticalItem ticket)
        {
            if(ticket==null)throw new ArgumentNullException("ticket");
            if(ticket.RunId!=_runId || ticket.Priority!=TacticalPriority.Critical)
                return false;
            for(int i=0;i<_criticalVisible.Count;i++)
            {
                var current=_criticalVisible[i];
                if(!object.ReferenceEquals(current,ticket))continue;
                long expires=current.LastTimeMs>long.MaxValue-CriticalTtlMs
                    ?long.MaxValue:current.LastTimeMs+CriticalTtlMs;
                if(_nowMs>=expires)return false;
                _criticalVisible.RemoveAt(i);
                return true;
            }
            return false;
        }

        public static TacticalPriority PriorityOf(TacticalKind kind)
        {
            if(!Enum.IsDefined(typeof(TacticalKind),kind))
                throw new ArgumentOutOfRangeException("kind");
            return kind==TacticalKind.PartDetached||kind==TacticalKind.WeaponOffline
                ?TacticalPriority.Critical:TacticalPriority.Normal;
        }

        static bool IsCombinable(TacticalKind kind)
        {return kind==TacticalKind.CoreHit||kind==TacticalKind.SalvageRecovered;}

        static bool ValidId(string id)
        {
            if(string.IsNullOrEmpty(id)||id.Length>64)return false;
            foreach(char c in id)
                if(!((c>='a'&&c<='z')||(c>='A'&&c<='Z')||
                     (c>='0'&&c<='9')||c=='_'||c=='-'||c==':'||c=='.'))
                    return false;
            return true;
        }
    }
}
