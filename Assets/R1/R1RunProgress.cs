using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoboRougelike.R1
{
    [Serializable] public sealed class R1ProgressCheckpoint
    {
        public int encounterIndex,completed;
        public float hull,damage,fireInterval,mobility,core;
        public string[] acquired;
    }
    public enum R1RunState { Ready, Combat, UpgradeChoice, Won, Lost, Garage, Salvage, Route, TestDrive }

    public sealed class R1RunStats
    {
        public float Hull { get; private set; } = 1;
        public float Damage { get; private set; } = 1;
        public float FireInterval { get; private set; } = 1;
        public float Mobility { get; private set; } = 1;
        public float Core { get; private set; } = 1;
        public R1RunStats Copy() => new R1RunStats {Hull=Hull,Damage=Damage,FireInterval=FireInterval,Mobility=Mobility,Core=Core};
        internal static bool TryRestore(R1ProgressCheckpoint c,out R1RunStats stats)
        {
            stats=null;
            bool In(float x,float a,float b)=>!float.IsNaN(x)&&!float.IsInfinity(x)&&x>=a&&x<=b;
            if(c==null||!In(c.hull,1,3)||!In(c.damage,1,3)||!In(c.fireInterval,.35f,1)||
                !In(c.mobility,1,2)||!In(c.core,1,4))return false;
            stats=new R1RunStats{Hull=c.hull,Damage=c.damage,FireInterval=c.fireInterval,Mobility=c.mobility,Core=c.core};return true;
        }
        public void Apply(R1UpgradeDefinition upgrade)
        {
            if(upgrade!=null)Apply(upgrade.Effect,upgrade.Amount);
        }
        public void Apply(R1UpgradeEffect effect,float rawAmount)
        {
            if(float.IsNaN(rawAmount)||float.IsInfinity(rawAmount))return;
            float amount=Mathf.Clamp01(rawAmount);
            switch (effect)
            {
                case R1UpgradeEffect.Hull: Hull = Mathf.Min(3, Hull * (1 + amount)); break;
                case R1UpgradeEffect.Damage: Damage = Mathf.Min(3, Damage * (1 + amount)); break;
                case R1UpgradeEffect.Reload: FireInterval = Mathf.Max(0.35f, FireInterval * (1 - amount)); break;
                case R1UpgradeEffect.Mobility: Mobility = Mathf.Min(2, Mobility * (1 + amount)); break;
                case R1UpgradeEffect.Core: Core = Mathf.Min(4, Core * (1 + amount)); break;
            }
        }
    }

    public sealed class R1RunProgress
    {
        public R1RunState State { get; private set; } = R1RunState.Ready;
        public int EncounterIndex { get; private set; }
        public int CompletedEncounters { get; private set; }
        public int TotalEncounters { get; }
        public R1RunStats Stats { get; private set; } = new R1RunStats();
        public IReadOnlyList<R1UpgradeDefinition> Offers => offers;
        public IReadOnlyList<string> Acquired => acquired;
        public string DefeatReason { get; private set; }
        private readonly List<R1UpgradeDefinition> offers = new List<R1UpgradeDefinition>();
        private readonly List<string> acquired = new List<string>();
        private R1RunState afterSalvage;
        private readonly bool routes;
        public R1RunProgress(int total = 3, bool useRoutes = false) { TotalEncounters = Math.Max(3, total); routes=useRoutes; }
        public R1ProgressCheckpoint MakeGarageCheckpoint()
        {
            if(State!=R1RunState.Garage)return null;
            return new R1ProgressCheckpoint{encounterIndex=EncounterIndex,completed=CompletedEncounters,
                hull=Stats.Hull,damage=Stats.Damage,fireInterval=Stats.FireInterval,mobility=Stats.Mobility,core=Stats.Core,
                acquired=acquired.ToArray()};
        }
        public static bool TryRestoreGarage(R1ProgressCheckpoint data,int total,out R1RunProgress progress)
        {
            progress=null;
            if(data==null||total<3||data.completed<0||data.completed>=total||data.encounterIndex!=data.completed||
                data.acquired==null||data.acquired.Length!=data.completed||data.acquired.Any(s=>string.IsNullOrWhiteSpace(s)||s.Length>256)||
                !R1RunStats.TryRestore(data,out var stats))return false;
            var p=new R1RunProgress(total,true){State=R1RunState.Garage,EncounterIndex=data.encounterIndex,
                CompletedEncounters=data.completed,Stats=stats,DefeatReason=""};
            p.acquired.AddRange(data.acquired);progress=p;return true;
        }
        public bool Start()
        {
            if (State != R1RunState.Ready && State != R1RunState.Won && State != R1RunState.Lost) return false;
            Stats = new R1RunStats(); offers.Clear(); acquired.Clear();
            EncounterIndex = 0; CompletedEncounters = 0; DefeatReason = "";
            State = routes ? R1RunState.Route : R1RunState.Combat;
            return true;
        }
        public bool WinEncounter(IEnumerable<R1UpgradeDefinition> catalog, System.Random random)
        {
            if (State != R1RunState.Combat) return false;
            var pool = (catalog ?? Array.Empty<R1UpgradeDefinition>()).Where(item => item != null &&
                !string.IsNullOrWhiteSpace(item.Id)).GroupBy(item => item.Id).Select(group => group.First()).ToList();
            if (pool.Count < 3)
                throw new InvalidOperationException("Wymagane są co najmniej trzy różne ulepszenia.");
            CompletedEncounters++;
            offers.Clear();
            random = random ?? new System.Random();
            for (int i = 0; i < 3; i++)
            {
                int choice = random.Next(pool.Count);
                offers.Add(pool[choice]); pool.RemoveAt(choice);
            }
            State = CompletedEncounters >= TotalEncounters ? R1RunState.Won : R1RunState.UpgradeChoice;
            return true;
        }
        public R1UpgradeDefinition ChooseUpgrade(int index)
        {
            if (State != R1RunState.UpgradeChoice || index < 0 || index >= offers.Count) return null;
            var chosen = offers[index];
            // The upgrade is a physical garage module, never a permanent stat.
            acquired.Add(chosen.Title);
            offers.Clear(); EncounterIndex++; State = R1RunState.Garage;
            return chosen;
        }
        public bool OpenSalvage()
        {
            if(State!=R1RunState.UpgradeChoice&&State!=R1RunState.Won)return false;
            afterSalvage=State;State=R1RunState.Salvage;return true;
        }
        // One module from three is mandatory and INDEPENDENT of up to three salvaged parts.
        // Legacy CompleteSalvage/ChooseUpgrade remain for older QA fixtures.
        public bool CanCompleteUnifiedRewards(IEnumerable<int> selectedBonuses)
        {
            if(State!=R1RunState.Salvage||selectedBonuses==null)return false;
            var indices=selectedBonuses.ToArray();
            return indices.Length==(offers.Count>0?1:0) &&
                indices.Distinct().Count()==indices.Length &&
                indices.All(index=>index>=0&&index<offers.Count);
        }
        public bool CompleteUnifiedRewards(IEnumerable<int> selectedBonuses)
        {
            if(!CanCompleteUnifiedRewards(selectedBonuses))return false;
            var indices=selectedBonuses.ToArray();
            foreach(var index in indices)
            {
                // Taking a bonus yields a module in inventory. It must be mounted to work.
                acquired.Add(offers[index].Title);
            }
            offers.Clear();
            if(afterSalvage==R1RunState.UpgradeChoice)
            {
                EncounterIndex++;
                State=R1RunState.Garage;
            }
            else State=afterSalvage;
            return true;
        }
        public bool CompleteSalvage()
        {
            if(State!=R1RunState.Salvage)return false;State=afterSalvage;return true;
        }
        public bool LeaveGarage()
        {
            if (State != R1RunState.Garage) return false;
            State = routes ? R1RunState.Route : R1RunState.Combat; return true;
        }
        public bool OpenWorkshop(){if(State!=R1RunState.Ready)return false;State=R1RunState.Garage;return true;}
        public bool PrepareGarage(){if(State!=R1RunState.Route||EncounterIndex!=0||CompletedEncounters!=0)return false;State=R1RunState.Garage;return true;}
        public bool BeginTest(){if(State!=R1RunState.Garage)return false;State=R1RunState.TestDrive;return true;}
        public bool EndTest(){if(State!=R1RunState.TestDrive)return false;State=R1RunState.Garage;return true;}
        public bool ChooseRoute()
        {
            if(State!=R1RunState.Route)return false;
            State=R1RunState.Combat;return true;
        }
        public void Lose(string reason)
        {
            if (State != R1RunState.Combat) return;
            State = R1RunState.Lost; DefeatReason = reason; offers.Clear();
        }
    }
}
