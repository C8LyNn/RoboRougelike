using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRougelike.R4;
using RoboRougelike.R1;

namespace RoboRougelike.R3
{
    public enum R3PartKind { Armor, Wheel, Gun, Core, Suspension, Booster, JumpDrive, Blink, Gyroscope, OmniDrive, MagneticGrapple, HoverEngine, CrawlerTrack, BonusModule, DirectionalShield, PointDefense, StructuralScanner, ScannerPulse, IndependentSteering }
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
        // A bonus is a physical, single inventory item occupying a 1x2x2 grid footprint.
        public string UpgradeId, UpgradeTitle, UpgradeDescription;
        public R1UpgradeEffect UpgradeEffect;
        public float UpgradeAmount;
        // Rotation in 90-degree steps around local X/Y/Z; mask encodes six
        // optional face-centre mounting contacts in local orientation.
        public Vector3Int RotationQuarterTurns;
        public int ContactMask=R3MountingGrid.AllFaces;
        public RoboRougelike.Mechanics.IndependentSteering.SteeringMode SteeringMode;
        public float DeviceReadyAt;
        // Persistent resources belong to the owned part, including during garage rebuilds.
        public int Ammo=-1;
        public float ReloadReadyAt;
        public float DisruptionRemaining;
        public float ThermalHeat;
        public bool ThermalLocked;
        public float DefenseCharge=-1,DefenseRechargeAge=float.PositiveInfinity;
        public R3PartState(Vector3Int cell, R3PartKind kind, float health = 1) { Cell = cell; Kind = kind; Health = health; }
        public R3PartState Copy() => new R3PartState(Cell, Kind, Health){Id=Id,Source=Source,Weapon=Weapon,SteeringMode=SteeringMode,DeviceReadyAt=DeviceReadyAt,Ammo=Ammo,ReloadReadyAt=ReloadReadyAt,ThermalHeat=ThermalHeat,ThermalLocked=ThermalLocked,DisruptionRemaining=DisruptionRemaining,DefenseCharge=DefenseCharge,DefenseRechargeAge=DefenseRechargeAge,
            UpgradeId=UpgradeId,UpgradeTitle=UpgradeTitle,UpgradeDescription=UpgradeDescription,
            UpgradeEffect=UpgradeEffect,UpgradeAmount=UpgradeAmount,
            RotationQuarterTurns=RotationQuarterTurns,ContactMask=ContactMask};
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
            R3PartKind.HoverEngine=>35, R3PartKind.CrawlerTrack=>45,
            R3PartKind.BonusModule=>60,R3PartKind.DirectionalShield=>60,R3PartKind.PointDefense=>70,R3PartKind.StructuralScanner=>40,R3PartKind.ScannerPulse=>30,R3PartKind.IndependentSteering=>45, _=>ArmorCost };
    }
    public sealed class R3VehicleModel
    {
        public const int InventoryCapacity=200;
        public R3VehicleDesign Design { get; }
        public List<R3PartState> Parts { get; }
        private readonly List<R3PartState> inventory=new List<R3PartState>();
        public IReadOnlyList<R3PartState> Inventory => inventory;
        public int Scrap { get; private set; }
        private Func<R3PartState,string> installationGate;
        internal void SetInstallationGate(Func<R3PartState,string> gate) { installationGate=gate; }
        private Func<IReadOnlyList<R3PartState>,bool> restorationGate;
        internal void SetRestorationGate(Func<IReadOnlyList<R3PartState>,bool> gate){restorationGate=gate;}
        readonly struct FactoryWheelPose {
            public readonly Vector3Int Cell,Rotation;public readonly int Contacts;
            public FactoryWheelPose(R3PartState p){Cell=p.Cell;Rotation=p.RotationQuarterTurns;Contacts=p.ContactMask;}
        }
        readonly Dictionary<string,FactoryWheelPose> factoryWheels=new Dictionary<string,FactoryWheelPose>(StringComparer.Ordinal);
        internal bool IsFactoryWheel(R3PartState part)=>part!=null&&part.Kind==R3PartKind.Wheel&&
            factoryWheels.TryGetValue(part.Id,out var pose)&&pose.Cell==part.Cell&&pose.Rotation==part.RotationQuarterTurns&&pose.Contacts==part.ContactMask;
        public bool SuspensionEnabled = true;
        public RoboRougelike.Mechanics.MOV009.ActiveSuspensionSupportMode SuspensionMode = RoboRougelike.Mechanics.MOV009.ActiveSuspensionSupportMode.FullSupport;
        public float SuspensionHeightOffset;
        public R3DriveKind SelectedDrive;
        public bool GyroEnabled=true;
        public float BoosterThrustNewtons=3000, BoosterDurationSeconds=1;
        public float HoverHeightMeters=1;
        public float GrappleReadyAt;
        // Assignments follow mounted types, including damaged plans that can be repaired.
        R4WeaponKind? weaponGroupA, weaponGroupB;
        public R4WeaponKind? WeaponGroupA { get { SyncWeaponGroups(); return weaponGroupA; } }
        public R4WeaponKind? WeaponGroupB { get { SyncWeaponGroups(); return weaponGroupB; } }
        public void SyncWeaponGroups()
        {
            var types=Parts.Where(p=>p.Kind==R3PartKind.Gun).Select(p=>p.Weapon).Distinct().Take(2).ToList();
            if(weaponGroupA.HasValue&&!types.Contains(weaponGroupA.Value))weaponGroupA=null;
            if(weaponGroupB.HasValue&&!types.Contains(weaponGroupB.Value))weaponGroupB=null;
            foreach(var kind in types)
            {
                if(weaponGroupA==kind||weaponGroupB==kind)continue;
                if(!weaponGroupA.HasValue)weaponGroupA=kind;
                else if(!weaponGroupB.HasValue)weaponGroupB=kind;
            }
        }
        public void SwapWeaponGroups()
        { SyncWeaponGroups(); var old=weaponGroupA; weaponGroupA=weaponGroupB; weaponGroupB=old; }
        public R3GarageEconomy Economy { get; }
        public R3VehicleModel(R3VehicleDesign design, R3GarageEconomy economy = null)
        { Design = design; Parts = design.Parts.Select(p => {var part=p.Copy();part.Id=Guid.NewGuid().ToString("N");return part;}).ToList(); Economy = economy ?? new R3GarageEconomy();
            foreach(var part in Parts.Where(p=>p.Kind==R3PartKind.Wheel))factoryWheels.Add(part.Id,new FactoryWheelPose(part)); }
        public static readonly Vector3Int[] Neighbours = { Vector3Int.left, Vector3Int.right, Vector3Int.up,
            Vector3Int.down, Vector3Int.forward, Vector3Int.back };
        public R3VehicleModel DeepCopy()
        {
            var design=new R3VehicleDesign{Name=Design.Name,Description=Design.Description,Speed=Design.Speed,TurnSpeed=Design.TurnSpeed,CoreHp=Design.CoreHp};
            design.Parts.AddRange(Design.Parts.Select(p=>p.Copy()));
            var copy=new R3VehicleModel(design,new R3GarageEconomy{ArmorCost=Economy.ArmorCost,WheelCost=Economy.WheelCost,GunCost=Economy.GunCost,MaxRepairCost=Economy.MaxRepairCost});
            copy.Parts.Clear();copy.Parts.AddRange(Parts.Select(p=>p.Copy()));copy.inventory.AddRange(inventory.Select(p=>p.Copy()));copy.Scrap=Scrap;
            copy.factoryWheels.Clear();foreach(var item in factoryWheels)copy.factoryWheels.Add(item.Key,item.Value);
            copy.SuspensionEnabled=SuspensionEnabled;copy.SuspensionMode=SuspensionMode;copy.SuspensionHeightOffset=SuspensionHeightOffset;
            copy.SelectedDrive=SelectedDrive;copy.GyroEnabled=GyroEnabled;copy.BoosterThrustNewtons=BoosterThrustNewtons;
            copy.BoosterDurationSeconds=BoosterDurationSeconds;copy.GrappleReadyAt=GrappleReadyAt;
            copy.HoverHeightMeters=HoverHeightMeters;
            SyncWeaponGroups();copy.weaponGroupA=weaponGroupA;copy.weaponGroupB=weaponGroupB;
            return copy;
        }
        public void Award(int amount) { Scrap = (int)Math.Min(100000L, Scrap + (long)Math.Max(0, amount)); }
        // All parts have a voxel footprint. The bonus is one logical item
        // occupying 2x2x1 cells; 90-degree rotations allow vertical placement.
        public static IEnumerable<Vector3Int> OccupiedCells(Vector3Int anchor,R3PartKind kind) =>
            OccupiedCells(anchor,kind,default);
        public static bool UsesWideFootprint(R3PartKind kind) => kind==R3PartKind.BonusModule||kind==R3PartKind.DirectionalShield||kind==R3PartKind.PointDefense||kind==R3PartKind.StructuralScanner||kind==R3PartKind.ScannerPulse||kind==R3PartKind.IndependentSteering;
        public static IEnumerable<Vector3Int> OccupiedCells(Vector3Int anchor,R3PartKind kind,Vector3Int turns)
        {
            yield return anchor;
            if(!UsesWideFootprint(kind))yield break;
            yield return anchor+R3MountingGrid.Rotate(Vector3Int.right,turns);
            yield return anchor+R3MountingGrid.Rotate(Vector3Int.forward,turns);
            yield return anchor+R3MountingGrid.Rotate(Vector3Int.right+Vector3Int.forward,turns);
        }
        public static IEnumerable<Vector3Int> OccupiedCells(R3PartState part) =>
            OccupiedCells(part.Cell,part.Kind,part.RotationQuarterTurns);
        public R3PartState At(Vector3Int cell) => Parts.FirstOrDefault(p=>OccupiedCells(p).Contains(cell));
        private bool HasAliveExternalNeighbour(Vector3Int anchor,R3PartKind kind,
            Vector3Int turns=default,int contacts=R3MountingGrid.AllFaces)
        {
            var footprint=new HashSet<Vector3Int>(OccupiedCells(anchor,kind,turns));
            int worldPorts=R3MountingGrid.WorldPortMask(turns,contacts);
            foreach(var cell in footprint)
                foreach(var direction in Neighbours)
                {
                    if(footprint.Contains(cell+direction)||
                       (worldPorts&R3MountingGrid.FaceBit(direction))==0)continue;
                    var neighbour=At(cell+direction);
                    if(neighbour!=null&&neighbour.Health>0&&
                       R3MountingGrid.PortOnFace(neighbour,-direction))return true;
                }
            return false;
        }
        public int Price(R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic) => Math.Max(1,
            kind==R3PartKind.Gun&&weapon!=R4WeaponKind.Basic?R4WeaponDefinition.Get(weapon).Price:Economy.Price(kind));
        public bool Owns(string id) => Parts.Concat(inventory).Any(p=>p.Id==id);
        // Mounted wreck plans reserve their type too: rebuilding them must not
        // turn a legal two-type design into a three-type vehicle. Inventory
        // parts remain unrestricted until the player actually mounts them.
        const int MaxWeaponTypes=2;
        const string WeaponTypeLimitError="Maksymalnie 2 różne typy broni na pojeździe.";
        HashSet<R4WeaponKind> MountedWeaponTypes() => new HashSet<R4WeaponKind>(
            Parts.Where(p=>p.Kind==R3PartKind.Gun).Select(p=>p.Weapon));
        bool InvalidWeaponTypes => MountedWeaponTypes().Count>MaxWeaponTypes;
        public string PlacementError(Vector3Int cell, R3PartKind kind, bool owned=false,
            R4WeaponKind weapon=R4WeaponKind.Basic,Vector3Int rotation=default,
            int contactMask=R3MountingGrid.AllFaces)
            => CheckPlacement(cell,kind,owned,weapon,rotation,contactMask,null);
        public string OwnedPlacementError(string id,Vector3Int cell,Vector3Int rotation)
        {
            var part=inventory.FirstOrDefault(p=>p.Id==id);
            return part==null?"Brak części w magazynie.":part.Health<=0?"Odtwórz zniszczoną część przed montażem.":
                CheckPlacement(cell,part.Kind,true,part.Weapon,rotation,part.ContactMask,part.Id);
        }
        string CheckPlacement(Vector3Int cell,R3PartKind kind,bool owned,R4WeaponKind weapon,Vector3Int rotation,int contactMask,string ownedId)
        {
            if (kind == R3PartKind.Core) return "Rdzeń jest jedyny i nie można go kupić.";
            if (!Enum.IsDefined(typeof(R3PartKind),kind)) return "Nieznany typ części.";
            if(kind==R3PartKind.Gun&&!Enum.IsDefined(typeof(R4WeaponKind),weapon))return "Nieznany rodzaj broni.";
            var footprint=OccupiedCells(cell,kind,rotation).ToArray();
            if(footprint.Any(c=>Math.Abs(c.x)>6||Math.Abs(c.z)>7||c.y<0||c.y>5))return "Moduł wykracza poza siatkę montażową.";
            if(footprint.Any(c=>At(c)!=null))return "Zajęte pole obrysu 1×2×2. Utraconą część odtwórz lub usuń jej plan.";
            if(Parts.Sum(p=>OccupiedCells(p).Count())+footprint.Length>180)return "Limit 180 pól części.";
            if(kind==R3PartKind.BonusModule&&!owned)return "Moduły bonusów zdobywa się wyłącznie jako nagrody.";
            if(kind==R3PartKind.Gun)
            {
                var types=MountedWeaponTypes();
                if(types.Count>MaxWeaponTypes||(types.Count==MaxWeaponTypes&&!types.Contains(weapon)))return WeaponTypeLimitError;
            }
            if (kind == R3PartKind.Wheel && Parts.Count(p => p.Kind == kind) >= 16) return "Limit 16 kół.";
            if (kind == R3PartKind.Suspension && Parts.Count(p => p.Kind == kind) >= 8) return "Limit 8 modułów zawieszenia.";
            int limit=kind switch {R3PartKind.Booster=>4,R3PartKind.JumpDrive=>2,R3PartKind.Blink=>1,
                R3PartKind.Gyroscope=>2,R3PartKind.MagneticGrapple=>2,R3PartKind.DirectionalShield=>2,R3PartKind.PointDefense=>2,R3PartKind.StructuralScanner=>1,R3PartKind.ScannerPulse=>1,R3PartKind.IndependentSteering=>1,_=>8};
            if(kind>=R3PartKind.Booster&&Parts.Count(p=>p.Kind==kind)>=limit)return "Limit modułów: "+limit+".";
            if(kind==R3PartKind.CrawlerTrack&&cell.x==0)return "Gąsienicę zamontuj po lewej lub prawej stronie.";
            if (!HasAliveExternalNeighbour(cell,kind,rotation,contactMask))
                return "Brak zgodnych punktów mocowania ze sprawną konstrukcją.";
            if (!owned && Scrap < Price(kind,weapon)) return "Za mało Złomu.";
            if(installationGate==null)return null;
            var candidate=new R3PartState(cell,kind){Weapon=weapon,RotationQuarterTurns=rotation,ContactMask=contactMask};
            if(ownedId!=null)candidate.Id=ownedId;
            return installationGate(candidate);
        }
        public bool Buy(Vector3Int cell,R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic,
            Vector3Int rotation=default)
        {
            rotation=R3MountingGrid.Normalize(rotation);
            if(kind==R3PartKind.BonusModule||PlacementError(cell,kind,false,weapon,rotation)!=null)return false;
            Scrap-=Price(kind,weapon);
            Parts.Add(new R3PartState(cell,kind){Weapon=weapon,RotationQuarterTurns=rotation});
            return true;
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
            if(part.Kind==R3PartKind.Gun&&InvalidWeaponTypes)return false;
            if (part.Health<=0&&!HasAliveExternalNeighbour(part.Cell,part.Kind,
                part.RotationQuarterTurns,part.ContactMask))return false;
            if(part.Health<=0&&restorationGate!=null&&!restorationGate(new[]{part}))return false;
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
            if(inventory.Count+batch.Length>InventoryCapacity)return false;
            if(batch.Any(p=>p==null||float.IsNaN(p.Health)||float.IsInfinity(p.Health)||p.Health<0||p.Health>1||
                !Enum.IsDefined(typeof(R3PartKind),p.Kind)||p.Kind==R3PartKind.Armor||p.Kind==R3PartKind.Core||
                (p.Kind==R3PartKind.BonusModule&&(string.IsNullOrWhiteSpace(p.UpgradeId)||
                    !Enum.IsDefined(typeof(R1UpgradeEffect),p.UpgradeEffect)||float.IsNaN(p.UpgradeAmount)||
                    float.IsInfinity(p.UpgradeAmount)||p.UpgradeAmount<0||p.UpgradeAmount>1))||
                string.IsNullOrWhiteSpace(p.Id)||Owns(p.Id))||
                batch.Select(p=>p.Id).Distinct().Count()!=batch.Length)return false;
            inventory.AddRange(batch.Select(p=>p.Copy()));return true;
        }
        public static R3PartState CreateBonusReward(R1UpgradeDefinition bonus)
        {
            if(bonus==null||string.IsNullOrWhiteSpace(bonus.Id)||
                !Enum.IsDefined(typeof(R1UpgradeEffect),bonus.Effect)||
                float.IsNaN(bonus.Amount)||float.IsInfinity(bonus.Amount))return null;
            return new R3PartState(default,R3PartKind.BonusModule){
                Id=Guid.NewGuid().ToString("N"),Source="Bonus za zwycięstwo",
                UpgradeId=bonus.Id,UpgradeTitle=string.IsNullOrWhiteSpace(bonus.Title)?bonus.Id:bonus.Title,
                UpgradeDescription=bonus.Description,UpgradeEffect=bonus.Effect,
                UpgradeAmount=Mathf.Clamp01(bonus.Amount)};
        }
        public bool ReceiveBonuses(IEnumerable<R1UpgradeDefinition> rewards)
        {
            if(rewards==null)return false;
            var parts=rewards.Select(CreateBonusReward).ToArray();
            return ReceiveSalvage(parts);
        }
        public bool Mount(string id,Vector3Int cell)
        {
            var stored=inventory.FirstOrDefault(p=>p.Id==id);
            return stored!=null&&Mount(id,cell,stored.RotationQuarterTurns);
        }
        public bool Mount(string id,Vector3Int cell,Vector3Int rotation)
        {
            var part=inventory.FirstOrDefault(p=>p.Id==id);
            rotation=R3MountingGrid.Normalize(rotation);
            if(part==null||part.Health<=0||
                OwnedPlacementError(part.Id,cell,rotation)!=null)return false;
            inventory.Remove(part);part.Cell=cell;part.RotationQuarterTurns=rotation;
            Parts.Add(part);return true;
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
            var cells=new Dictionary<Vector3Int,R3PartState>();
            foreach(var part in parts)
                foreach(var cell in OccupiedCells(part))
                    if(!cells.TryAdd(cell,part))return false;
            var core = cells.Values.FirstOrDefault(p => p.Kind == R3PartKind.Core);
            if (core == null) return false;
            var visited = new HashSet<Vector3Int> { core.Cell }; var queue = new Queue<Vector3Int>(); queue.Enqueue(core.Cell);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var n in Neighbours)
                { var next=current+n;
                    if(cells.TryGetValue(next,out var other)&&!visited.Contains(next)&&
                        R3MountingGrid.Compatible(cells[current],other,n))
                    {visited.Add(next);queue.Enqueue(next);}
                }
            }
            return visited.Count == cells.Count;
        }
        public string DeployError
        {
            get
            {
                if(InvalidWeaponTypes)return WeaponTypeLimitError;
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
            if(InvalidWeaponTypes)return false;
            var plan=EmergencyRepairPlan();
            if(plan==null||plan.Count==0)return false;
            int price=plan.Sum(RepairCost);
            if(Scrap<price)return false;
            if(restorationGate!=null&&!restorationGate(plan))return false;
            Scrap-=price;
            foreach(var part in plan)part.Health=1;
            return true;
        }
        public int EmergencyRepairCost => EmergencyRepairPlan()?.Sum(RepairCost)??0;
        private List<R3PartState> EmergencyRepairPlan()
        {
            if (InvalidWeaponTypes || CanDeploy || !Parts.Any(p => p.Kind == R3PartKind.Core && p.Health > 0)) return null;
            var byCell=Parts.SelectMany(p=>OccupiedCells(p).Select(cell=>new{cell,part=p})).ToDictionary(x=>x.cell,x=>x.part); var core = Parts.Single(p => p.Kind == R3PartKind.Core);
            var previous = new Dictionary<Vector3Int, Vector3Int> { [core.Cell] = core.Cell };
            var costs = new Dictionary<Vector3Int,int> { [core.Cell]=0 };
            var pending = new List<Vector3Int>{core.Cell};
            while (pending.Count > 0)
            {
                var current=pending.OrderBy(cell=>costs[cell]).First();pending.Remove(current);
                foreach (var n in Neighbours)
                {
                    var next=current+n;
                    if(!byCell.ContainsKey(next)||
                        !R3MountingGrid.Compatible(byCell[current],byCell[next],n))continue;
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
