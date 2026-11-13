using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboRougelike.R3
{
    [Serializable] public sealed class VehicleChargePoolCheckpoint
    {
        public string id;
        public int baseCapacity,current;
        public float progress;
    }

    // One pool per device type, independent of physical copies and energy.
    // Capacity loss never discards already earned charges.
    public sealed class VehicleChargeState
    {
        sealed class Pool {public int Base,Current;public float Progress;}
        readonly Dictionary<string,Pool> pools=new Dictionary<string,Pool>(StringComparer.Ordinal);
        int capacityBonus;
        int Capacity(Pool pool)=>(int)Math.Min(int.MaxValue,(long)pool.Base+capacityBonus);
        public bool EnsurePool(string id,int baseCapacity)
        {
            if(string.IsNullOrWhiteSpace(id)||id.Length>128||baseCapacity<0)return false;
            if(pools.TryGetValue(id,out var existing)){existing.Base=baseCapacity;return true;}
            var pool=new Pool{Base=baseCapacity};pool.Current=Capacity(pool);pools.Add(id,pool);return true;
        }
        public void SetCapacityBonus(int amount)=>capacityBonus=Math.Max(0,amount);
        public int Current(string id)=>id!=null&&pools.TryGetValue(id,out var pool)?pool.Current:0;
        public int Maximum(string id)=>id!=null&&pools.TryGetValue(id,out var pool)?Capacity(pool):0;
        public bool TrySpend(string id,int amount)
        {
            if(amount<=0||id==null||!pools.TryGetValue(id,out var pool)||pool.Current<amount)return false;
            pool.Current-=amount;return true;
        }
        public int Recharge(string id,int amount)
        {
            if(amount<=0||id==null||!pools.TryGetValue(id,out var pool))return 0;
            int added=(int)Math.Min((long)amount,Math.Max(0L,(long)Capacity(pool)-pool.Current));
            pool.Current+=added;return added;
        }
        public void Advance(float seconds,float chargesPerSecond)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0||
               float.IsNaN(chargesPerSecond)||float.IsInfinity(chargesPerSecond)||chargesPerSecond<=0)return;
            foreach(var pool in pools.Values)
            {
                int capacity=Capacity(pool);if(pool.Current>=capacity)continue;
                double progress=pool.Progress+(double)seconds*chargesPerSecond;
                int added=(int)Math.Min((double)(capacity-pool.Current),Math.Floor(progress));
                pool.Current+=added;
                pool.Progress=pool.Current>=capacity?0f:(float)(progress-added);
            }
        }
        public void RefillAll()
        {foreach(var pool in pools.Values){pool.Current=Math.Max(pool.Current,Capacity(pool));pool.Progress=0;}}
        public VehicleChargePoolCheckpoint[] Snapshot()=>pools.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>
            new VehicleChargePoolCheckpoint{id=p.Key,baseCapacity=p.Value.Base,current=p.Value.Current,progress=p.Value.Progress}).ToArray();
        public bool Restore(VehicleChargePoolCheckpoint[] source)
        {
            if(source==null||source.Any(p=>p==null||string.IsNullOrWhiteSpace(p.id)||p.id.Length>128||
                p.baseCapacity<0||p.current<0||float.IsNaN(p.progress)||float.IsInfinity(p.progress)||p.progress<0||p.progress>=1)||
                source.Select(p=>p.id).Distinct(StringComparer.Ordinal).Count()!=source.Length)return false;
            var replacement=source.ToDictionary(p=>p.id,p=>new Pool{Base=p.baseCapacity,Current=p.current,Progress=p.progress},StringComparer.Ordinal);
            pools.Clear();foreach(var item in replacement)pools.Add(item.Key,item.Value);return true;
        }
    }
}
