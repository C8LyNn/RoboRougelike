using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRougelike.R4;

namespace RoboRougelike.R3
{
    public enum R3PartKind { Armor, Wheel, Gun, Core, Suspension, Booster, JumpDrive, Blink, Gyroscope, OmniDrive, MagneticGrapple, HoverEngine, CrawlerTrack }
    public enum R3DriveKind { Wheels, Tracks, Omni, Hover }
    public enum R3VehicleKind { Standard, Light, Heavy }
    [Serializable]
    public sealed class R3PartState
    {
        public Vector3Int Cell;
        public R3PartKind Kind;
        public float Health = 1;
        public string Id = Guid.NewGuid().ToString("N");
        public string Source = "Zakup / pojazd startowy";
        public R4WeaponKind Weapon;
        public float DeviceReadyAt;
        public R3PartState(Vector3Int cell, R3PartKind kind, float health = 1) { Cell = cell; Kind = kind; Health = health; }
        public R3PartState Copy() => new R3PartState(Cell, Kind, Health){Id=Id,Source=Source,Weapon=Weapon,DeviceReadyAt=DeviceReadyAt};
    }
    public sealed class R3VehicleDesign
    {
        public string Name, Description;
        public float Speed, TurnSpeed, CoreHp = 100;
        public readonly List<R3PartState> Parts = new List<R3PartState>();
    }
    [Serializable]
    public sealed class R3GarageEconomy
    {
        public int ArmorCost = 5, WheelCost = 15, GunCost = 30, MaxRepairCost = 3;
        public int Price(R3PartKind kind) => kind switch {
            R3PartKind.Suspension=>25, R3PartKind.Wheel=>WheelCost, R3PartKind.Gun=>GunCost,
            R3PartKind.Booster=>35, R3PartKind.JumpDrive=>45, R3PartKind.Blink=>60,
            R3PartKind.Gyroscope=>30, R3PartKind.OmniDrive=>40, R3PartKind.MagneticGrapple=>45,
            R3PartKind.HoverEngine=>35, R3PartKind.CrawlerTrack=>45, _=>ArmorCost };
    }
    public sealed class R3VehicleModel
    {
        public R3VehicleDesign Design { get; }
        public List<R3PartState> Parts { get; }
        private readonly List<R3PartState> inventory=new List<R3PartState>();
        public IReadOnlyList<R3PartState> Inventory => inventory;
        public int Scrap { get; private set; }
        public bool SuspensionEnabled = true;
        public RoboRougelike.Mechanics.MOV009.ActiveSuspensionSupportMode SuspensionMode = RoboRougelike.Mechanics.MOV009.ActiveSuspensionSupportMode.FullSupport;
        public float SuspensionHeightOffset;
        public R3DriveKind SelectedDrive;
        public bool GyroEnabled=true;
        public float BoosterThrustNewtons=3000, BoosterDurationSeconds=1;
        public float HoverHeightMeters=1;
        public float GrappleReadyAt;
        public R3GarageEconomy Economy { get; }
        public R3VehicleModel(R3VehicleDesign design, R3GarageEconomy economy = null)
        { Design = design; Parts = design.Parts.Select(p => {var part=p.Copy();part.Id=Guid.NewGuid().ToString("N");return part;}).ToList(); Economy = economy ?? new R3GarageEconomy(); }
        public static readonly Vector3Int[] Neighbours = { Vector3Int.left, Vector3Int.right, Vector3Int.up,
            Vector3Int.down, Vector3Int.forward, Vector3Int.back };
        public R3VehicleModel DeepCopy()
        {
            var design=new R3VehicleDesign{Name=Design.Name,Description=Design.Description,Speed=Design.Speed,TurnSpeed=Design.TurnSpeed,CoreHp=Design.CoreHp};
            design.Parts.AddRange(Design.Parts.Select(p=>p.Copy()));
            var copy=new R3VehicleModel(design,new R3GarageEconomy{ArmorCost=Economy.ArmorCost,WheelCost=Economy.WheelCost,GunCost=Economy.GunCost,MaxRepairCost=Economy.MaxRepairCost});
            copy.Parts.Clear();copy.Parts.AddRange(Parts.Select(p=>p.Copy()));copy.inventory.AddRange(inventory.Select(p=>p.Copy()));copy.Scrap=Scrap;
            copy.SuspensionEnabled=SuspensionEnabled;copy.SuspensionMode=SuspensionMode;copy.SuspensionHeightOffset=SuspensionHeightOffset;
            copy.SelectedDrive=SelectedDrive;copy.GyroEnabled=GyroEnabled;copy.BoosterThrustNewtons=BoosterThrustNewtons;
            copy.BoosterDurationSeconds=BoosterDurationSeconds;copy.GrappleReadyAt=GrappleReadyAt;
            copy.HoverHeightMeters=HoverHeightMeters;
            return copy;
        }
        public void Award(int amount) { Scrap = (int)Math.Min(100000L, Scrap + (long)Math.Max(0, amount)); }
        public R3PartState At(Vector3Int cell) => Parts.FirstOrDefault(p => p.Cell == cell);
        public int Price(R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic) => Math.Max(1,
            kind==R3PartKind.Gun&&weapon!=R4WeaponKind.Basic?R4WeaponDefinition.Get(weapon).Price:Economy.Price(kind));
        public bool Owns(string id) => Parts.Concat(inventory).Any(p=>p.Id==id);
        public string PlacementError(Vector3Int cell, R3PartKind kind, bool owned=false,R4WeaponKind weapon=R4WeaponKind.Basic)
        {
            if (kind == R3PartKind.Core) return "Rdzeń jest jedyny i nie można go kupić.";
            if (!Enum.IsDefined(typeof(R3PartKind),kind)) return "Nieznany typ części.";
            if (Math.Abs(cell.x) > 6 || Math.Abs(cell.z) > 7 || cell.y < 0 || cell.y > 5) return "Poza siatką montażową.";
            if (At(cell) != null) return "Komórka zajęta. Utraconą część odtwórz lub usuń jej plan.";
            if (Parts.Count >= 180) return "Limit 180 części.";
            if (kind == R3PartKind.Gun && Parts.Count(p => p.Kind == kind) >= 4) return "Limit 4 broni.";
            if (kind == R3PartKind.Wheel && Parts.Count(p => p.Kind == kind) >= 16) return "Limit 16 kół.";
            if (kind == R3PartKind.Suspension && Parts.Count(p => p.Kind == kind) >= 8) return "Limit 8 modułów zawieszenia.";
            int limit=kind switch {R3PartKind.Booster=>4,R3PartKind.JumpDrive=>2,R3PartKind.Blink=>1,
                R3PartKind.Gyroscope=>2,R3PartKind.MagneticGrapple=>2,_=>8};
            if(kind>=R3PartKind.Booster&&Parts.Count(p=>p.Kind==kind)>=limit)return "Limit modułów: "+limit+".";
            if(kind==R3PartKind.CrawlerTrack&&cell.x==0)return "Gąsienicę zamontuj po lewej lub prawej stronie.";
            if (!Neighbours.Any(n => At(cell + n)?.Health > 0)) return "Brak połączenia ze sprawną konstrukcją.";
            if (!owned && Scrap < Price(kind,weapon)) return "Za mało Złomu.";
            return null;
        }
        public bool Buy(Vector3Int cell, R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic)
        {
            if (PlacementError(cell, kind,false,weapon) != null) return false;
            Scrap -= Price(kind,weapon); Parts.Add(new R3PartState(cell, kind){Weapon=weapon}); return true;
        }
        public int RepairCost(R3PartState part)
        {
            if (part == null || part.Health >= 1f) return 0;
            // Lost modules require rebuilding at full purchase cost; damaged ones cost
            // a fraction of their value proportional to missing durability.
            return part.Health <= 0 ? Price(part.Kind,part.Weapon) : Math.Max(1,
                Mathf.CeilToInt((1 - part.Health) * Price(part.Kind,part.Weapon) * 0.35f));
        }
        public bool Repair(Vector3Int cell)
        {
            var part = At(cell); int price = RepairCost(part);
            if (part == null || price <= 0 || Scrap < price) return false;
            if (part.Health <= 0 && !Neighbours.Any(n => At(cell + n)?.Health > 0)) return false;
            Scrap -= price; part.Health = 1; return true;
        }
        public int RepairAffordable()
        {
            int count = 0; bool changed;
            do
            {
                changed = false;
                foreach (var part in Parts.OrderBy(p => p.Kind == R3PartKind.Core ? 0 : p.Kind == R3PartKind.Gun ? 1 : p.Kind == R3PartKind.Wheel ? 2 : 3))
                    if (Repair(part.Cell)) { count++; changed = true; }
            } while (changed);
            return count;
        }
        public int AffordableRepairCost
        {
            get {var preview=DeepCopy();preview.RepairAffordable();return Scrap-preview.Scrap;}
        }
        public bool Remove(Vector3Int cell)
        {
            var part = At(cell);
            if (part == null || part.Kind == R3PartKind.Core) return false;
            var rest = Parts.Where(p => p != part).ToList();
            if (!rest.Any(p => p.Kind == R3PartKind.Gun) ||
                !HasMobility(rest,SelectedDrive,false) ||
                (SelectedDrive!=R3DriveKind.Wheels&&CanDeploy&&!HasMobility(rest,SelectedDrive)) ||
                !Connected(rest) || !Connected(rest.Where(p => p.Health > 0))) return false;
            Parts.Remove(part); if(part.Health>0)inventory.Add(part); return true;
        }
        public bool ReceiveSalvage(IEnumerable<R3PartState> recovered)
        {
            if(recovered==null)return false;
            var batch=recovered.ToArray();
            if(batch.Any(p=>p==null||float.IsNaN(p.Health)||float.IsInfinity(p.Health)||p.Health<0||p.Health>1||
                !Enum.IsDefined(typeof(R3PartKind),p.Kind)||p.Kind==R3PartKind.Armor||p.Kind==R3PartKind.Core||string.IsNullOrWhiteSpace(p.Id)||Owns(p.Id))||
                batch.Select(p=>p.Id).Distinct().Count()!=batch.Length)return false;
            inventory.AddRange(batch.Select(p=>p.Copy()));return true;
        }
        public bool Mount(string id,Vector3Int cell)
        {
            var part=inventory.FirstOrDefault(p=>p.Id==id);
            if(part==null||part.Health<=0||PlacementError(cell,part.Kind,true,part.Weapon)!=null)return false;
            inventory.Remove(part);part.Cell=cell;Parts.Add(part);return true;
        }
        public bool RepairInventory(string id)
        {
            var part=inventory.FirstOrDefault(p=>p.Id==id);int cost=RepairCost(part);
            if(part==null||cost<=0||Scrap<cost)return false;
            Scrap-=cost;part.Health=1;return true;
        }
        public int DismantleValue(R3PartState part) => part==null?0:Mathf.FloorToInt(Price(part.Kind,part.Weapon)*0.25f*Mathf.Clamp01(part.Health));
        public bool Dismantle(string id)
        {
            var part=inventory.FirstOrDefault(p=>p.Id==id);if(part==null)return false;
            inventory.Remove(part);Award(DismantleValue(part));return true;
        }
        public static bool Connected(IEnumerable<R3PartState> parts)
        {
            var cells = parts.ToDictionary(p => p.Cell);
            var core = cells.Values.FirstOrDefault(p => p.Kind == R3PartKind.Core);
            if (core == null) return false;
            var visited = new HashSet<Vector3Int> { core.Cell }; var queue = new Queue<Vector3Int>(); queue.Enqueue(core.Cell);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var n in Neighbours)
                { var next = current + n; if (cells.ContainsKey(next) && visited.Add(next)) queue.Enqueue(next); }
            }
            return visited.Count == cells.Count;
        }
        public string DeployError
        {
            get
            {
                var alive = Parts.Where(p => p.Health > 0).ToList();
                if (!alive.Any(p => p.Kind == R3PartKind.Core)) return "Rdzeń zniszczony.";
                if (!Connected(alive)) return "Konstrukcja zawiera odłączone części.";
                if (!alive.Any(p => p.Kind == R3PartKind.Gun)) return "Brak sprawnej broni.";
                if(!HasMobility(alive,SelectedDrive))return SelectedDrive==R3DriveKind.Wheels?
                    "Potrzebne co najmniej jedno koło na każdej stronie.":"Brak sprawnego wybranego napędu (gąsienice wymagają obu stron).";
                return null;
            }
        }
        public bool CanDeploy => DeployError == null;
        public static bool HasMobility(IEnumerable<R3PartState> source,R3DriveKind drive,bool healthy=true)
        {
            var parts=source.Where(p=>!healthy||p.Health>0);
            if(drive==R3DriveKind.Wheels||drive==R3DriveKind.Tracks)
            {
                var kind=drive==R3DriveKind.Wheels?R3PartKind.Wheel:R3PartKind.CrawlerTrack;
                return parts.Any(p=>p.Kind==kind&&p.Cell.x<0)&&parts.Any(p=>p.Kind==kind&&p.Cell.x>0);
            }
            return drive==R3DriveKind.Omni?parts.Any(p=>p.Kind==R3PartKind.OmniDrive):
                drive==R3DriveKind.Hover&&parts.Any(p=>p.Kind==R3PartKind.HoverEngine);
        }
        public bool EmergencyRepair()
        {
            var plan=EmergencyRepairPlan();
            if(plan==null||plan.Count==0)return false;
            int price=plan.Sum(RepairCost);
            if(Scrap<price)return false;
            Scrap-=price;
            foreach(var part in plan)part.Health=1;
            return true;
        }
        public int EmergencyRepairCost => EmergencyRepairPlan()?.Sum(RepairCost)??0;
        private List<R3PartState> EmergencyRepairPlan()
        {
            if (CanDeploy || !Parts.Any(p => p.Kind == R3PartKind.Core && p.Health > 0)) return null;
            var byCell = Parts.ToDictionary(p => p.Cell); var core = Parts.Single(p => p.Kind == R3PartKind.Core);
            var previous = new Dictionary<Vector3Int, Vector3Int> { [core.Cell] = core.Cell };
            var costs = new Dictionary<Vector3Int,int> { [core.Cell]=0 };
            var pending = new List<Vector3Int>{core.Cell};
            while (pending.Count > 0)
            {
                var current=pending.OrderBy(cell=>costs[cell]).First();pending.Remove(current);
                foreach (var n in Neighbours)
                {
                    var next=current+n;if(!byCell.ContainsKey(next))continue;
                    int cost=costs[current]+(byCell[next].Health<=0?1:0);
                    if(costs.TryGetValue(next,out int old)&&old<=cost)continue;
                    costs[next]=cost;previous[next]=current;
                    if(!pending.Contains(next))pending.Add(next);
                }
            }
            Func<R3PartState,bool> reachable=p=>costs.ContainsKey(p.Cell);
            Func<R3PartState,int> distance=p=>Math.Abs(p.Cell.x-core.Cell.x)+Math.Abs(p.Cell.y-core.Cell.y)+Math.Abs(p.Cell.z-core.Cell.z);
            var required = new List<R3PartState>();
            if(!HasMobility(Parts,SelectedDrive,false))return null;
            var mobility=SelectedDrive==R3DriveKind.Tracks?R3PartKind.CrawlerTrack:SelectedDrive==R3DriveKind.Omni?R3PartKind.OmniDrive:
                SelectedDrive==R3DriveKind.Hover?R3PartKind.HoverEngine:R3PartKind.Wheel;
            var selectors=new List<Func<R3PartState,bool>>{p=>p.Kind==R3PartKind.Gun};
            if(SelectedDrive==R3DriveKind.Wheels||SelectedDrive==R3DriveKind.Tracks)
            {selectors.Add(p=>p.Kind==mobility&&p.Cell.x<0);selectors.Add(p=>p.Kind==mobility&&p.Cell.x>0);}
            else selectors.Add(p=>p.Kind==mobility);
            foreach(var selector in selectors)
            {
                var target=Parts.Where(p=>selector(p)&&reachable(p)).OrderBy(p=>costs[p.Cell]).ThenBy(distance).FirstOrDefault();
                if(target==null)return null;required.Add(target);
            }
            var repair=new HashSet<R3PartState>();
            foreach (var target in required)
                for (var cell = target.Cell; previous.ContainsKey(cell); cell = previous[cell])
                {
                    if (byCell[cell].Health <= 0) repair.Add(byCell[cell]);
                    if (cell == core.Cell) break;
                }
            // A disconnected live fragment also needs its bridge restored before deployment.
            foreach(var target in Parts.Where(p=>p.Health>0&&reachable(p)))
                for(var cell=target.Cell;cell!=core.Cell;cell=previous[cell])
                    if(byCell[cell].Health<=0)repair.Add(byCell[cell]);
            return repair.ToList();
        }
    }
}
