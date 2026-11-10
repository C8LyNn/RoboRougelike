using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RoboRougelike.R4;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.MOVSeries;
using RoboRougelike.R4_1;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        int settingsTab;
        Vector2 catalogScroll,stackScroll;
        bool catalogOpen;
        string catalogSearch=string.Empty;
        R3PartKind? catalogGroup;
        R4WeaponKind catalogWeapon;
        string catalogBonusId;
        const string SlickWheelGroup="wheel:slick";
        const string LargeWheelGroup="wheel:large";
        const string MonsterWheelGroup="wheel:monster";
        const string SmallWheelGroup="wheel:small";
        const string BalloonWheelGroup="wheel:balloon";
        const string SpikedWheelGroup="wheel:spiked";
        const string TwinWheelGroup="wheel:twin";
        const string SteerWheelGroup="wheel:steer-road";
        const string LargeSteerWheelGroup="wheel:steer-offroad";
        const string CasterWheelGroup="wheel:caster";
        const string SwingWheelGroup="wheel:swing";
        const string WedgeArmorGroup="armor:wedge";
        const string HalfArmorGroup="armor:half";
        const string PyramidArmorGroup="armor:pyramid";
        const string RoundArmorGroup="armor:round";
        const string HalfWedgeArmorGroup="armor:half-wedge";
        const string DomeArmorGroup="armor:dome";
        static R3WheelStyle CatalogWheelStyle(string group)=>group==SlickWheelGroup?R3WheelStyle.Slick:
            group==LargeWheelGroup?R3WheelStyle.LargeOffroad:group==MonsterWheelGroup?R3WheelStyle.Monster:
            group==SmallWheelGroup?R3WheelStyle.SmallFork:group==BalloonWheelGroup?R3WheelStyle.BalloonFork:group==SpikedWheelGroup?R3WheelStyle.SpikedFork:group==TwinWheelGroup?R3WheelStyle.TwinRoad:group==SteerWheelGroup?R3WheelStyle.SteerRoad:group==LargeSteerWheelGroup?R3WheelStyle.SteerOffroad:group==CasterWheelGroup?R3WheelStyle.CasterSlick:group==SwingWheelGroup?R3WheelStyle.SwingOffroad:R3WheelStyle.Offroad;
        static R3ArmorShape CatalogArmorShape(string group)=>group switch {
            WedgeArmorGroup=>R3ArmorShape.Wedge,HalfArmorGroup=>R3ArmorShape.HalfBlock,
            PyramidArmorGroup=>R3ArmorShape.Pyramid,RoundArmorGroup=>R3ArmorShape.RoundEdge,
            HalfWedgeArmorGroup=>R3ArmorShape.HalfWedge,DomeArmorGroup=>R3ArmorShape.Dome,_=>R3ArmorShape.Cube};
        GUIStyle garageTabStyle;
        static readonly R3PartKind[] shopKinds={R3PartKind.Armor,R3PartKind.Wheel,R3PartKind.Gun,R3PartKind.Suspension,
            R3PartKind.Booster,R3PartKind.JumpDrive,R3PartKind.Blink,R3PartKind.Gyroscope,
            R3PartKind.OmniDrive,R3PartKind.MagneticGrapple,R3PartKind.HoverEngine,R3PartKind.CrawlerTrack,R3PartKind.DirectionalShield,R3PartKind.PointDefense,R3PartKind.StructuralScanner,R3PartKind.ScannerPulse,R3PartKind.IndependentSteering,R3PartKind.Battery,R3PartKind.RepairPulse,R3PartKind.DrivePower,R3PartKind.EnergyRegenerator};
        static readonly string[] shopNames={"Pancerz","Koło","Broń","Zawieszenie","Booster","Skok","Blink","Żyroskop","Omni","Zaczep","Hover","Gąsienica","Tarcza","Obrona pkt.","Skan struktury","Impuls skanera","Skręt osi","Bateria","Impuls naprawczy","Silnik kół","Regenerator energii"};
        static readonly string[] driveNames={"Koła","Gąsienice","Omni","Hover"};
        readonly struct CatalogEntry
        {
            public readonly R3PartKind Kind;
            public readonly R4WeaponKind Weapon;
            public readonly string Group,Name;
            public CatalogEntry(R3PartKind kind,R4WeaponKind weapon,string group,string name)
            {Kind=kind;Weapon=weapon;Group=group;Name=name;}
        }
        static string NormalizeCatalogSearch(string text)
        {
            var normalized=(text??string.Empty).Trim().ToLowerInvariant().Replace('ł','l').Normalize(NormalizationForm.FormD);
            var result=new StringBuilder(normalized.Length);
            foreach(char ch in normalized)
                if(CharUnicodeInfo.GetUnicodeCategory(ch)!=UnicodeCategory.NonSpacingMark)result.Append(ch);
            return result.ToString();
        }
        static bool CatalogNameMatches(string name,string query)=>
            NormalizeCatalogSearch(name).Contains(NormalizeCatalogSearch(query));
        // This UI-only key keeps distinct effects and strengths separate while
        // leaving tiers and HP in the same stack of physical instances.
        static string CatalogBonusKey(R3PartState part)
        {
            string id=part.UpgradeId??string.Empty;
            float amount=part.UpgradeAmount==0?0:part.UpgradeAmount;
            return id.Length+":"+id+":"+(int)part.UpgradeEffect+":"+amount.ToString("R",CultureInfo.InvariantCulture);
        }
        static List<CatalogEntry> BuildCatalogEntries(R3VehicleModel model)
        {
            var result=new List<CatalogEntry>();
            void Add(R3PartKind kind,string group,string name)=>result.Add(new CatalogEntry(kind,R4WeaponKind.Basic,group,name));
            for(int i=0;i<shopKinds.Length;i++)
            {
                var kind=shopKinds[i];if(kind==R3PartKind.Gun)continue;
                Add(kind,null,kind==R3PartKind.Wheel?"Koło terenowe":shopNames[i]);
                if(kind==R3PartKind.Armor)
                {
                    Add(kind,WedgeArmorGroup,"Pancerz • klin");Add(kind,HalfArmorGroup,"Pancerz • półblok");
                    Add(kind,PyramidArmorGroup,"Pancerz • piramida");Add(kind,RoundArmorGroup,"Pancerz • zaokrąglenie");
                    Add(kind,HalfWedgeArmorGroup,"Pancerz • półklin");Add(kind,DomeArmorGroup,"Pancerz • kopuła");
                }
                if(kind==R3PartKind.Wheel)
                {
                    Add(kind,SlickWheelGroup,"Koło szosowe");Add(kind,LargeWheelGroup,"Duże koło terenowe");
                    Add(kind,MonsterWheelGroup,"Koło monster XL");Add(kind,SmallWheelGroup,"Małe koło drogowe");
                    Add(kind,BalloonWheelGroup,"Koło balonowe");Add(kind,SpikedWheelGroup,"Koło z kolcami");
                    Add(kind,TwinWheelGroup,"Koło bliźniacze");Add(kind,SteerWheelGroup,"Koło skrętne drogowe");
                    Add(kind,LargeSteerWheelGroup,"Duże koło skrętne");Add(kind,CasterWheelGroup,"Kółko samonastawne");
                    Add(kind,SwingWheelGroup,"Koło na wahaczu");
                }
            }
            for(int i=0;i<R4WeaponDefinition.Count;i++)
            {var weapon=(R4WeaponKind)i;result.Add(new CatalogEntry(R3PartKind.Gun,weapon,null,R4WeaponDefinition.Get(weapon).Name));}
            foreach(var item in model.Inventory.Where(p=>p.Kind==R3PartKind.BonusModule).GroupBy(CatalogBonusKey).Select(g=>g.First()))
                result.Add(new CatalogEntry(R3PartKind.BonusModule,R4WeaponKind.Basic,CatalogBonusKey(item),CatalogBonusGroupName(item)+" • "+BonusText(item)));
            return result;
        }
        void DrawCatalogSearch(R1RunController run,Rect label,Rect field,Rect clear)
        {
            GUI.Label(label,"Szukaj:",small);
            string next=GUI.TextField(field,catalogSearch,64);
            if(GUI.Button(clear,"×",compactButton))next=string.Empty;
            ApplyCatalogSearch(run,next);
        }
        void ApplyCatalogSearch(R1RunController run,string next)
        {
            if(next==catalogSearch)return;
            catalogSearch=next;catalogScroll=Vector2.zero;stackScroll=Vector2.zero;
            // A hidden old group must never remain the target of a purchase.
            catalogGroup=null;catalogBonusId=null;dismantleConfirmation=null;catalogFeedback=null;
            run.Garage.SelectedInventoryId=null;run.Garage.UseInventory=true;run.Garage.SetTool(R41GarageTool.Mount);
        }
        void DrawMovementCatalog(R1RunController run)
        {
            if(GUI.Button(new Rect(25,108,295,99),"KATALOG CZĘŚCI\nWybierz stos, kup lub zamontuj",compactButton))catalogOpen=true;
        }
        void DrawFullCatalog(R1RunController run)
        {
            Panel(new Rect(0,0,1280,720));
            GUI.Label(new Rect(25,20,870,48),"KATALOG CZĘŚCI • MAGAZYN",heading);
            GUI.Label(new Rect(25,60,490,25),$"Złom: {run.VehicleModel.Scrap} • wybierz grupę i egzemplarz",small);
            DrawCatalogSearch(run,new Rect(530,61,70,25),new Rect(600,60,255,28),new Rect(860,60,65,28));
            if(GUI.Button(new Rect(1080,20,175,40),"Wróć do garażu",compactButton)){DismissCatalog();return;}
            var model=run.VehicleModel;
            var entries=BuildCatalogEntries(model).Where(entry=>CatalogNameMatches(entry.Name,catalogSearch)).ToArray();
            catalogScroll=GUI.BeginScrollView(new Rect(25,98,900,570),catalogScroll,new Rect(0,0,875,Mathf.Max(570,Mathf.CeilToInt(entries.Length/3f)*55)));
            for(int row=0;row<entries.Length;row++)
            {var entry=entries[row];DrawCatalogRow(run,entry.Kind,entry.Weapon,entry.Group,entry.Name,row);}
            if(entries.Length==0)GUI.Label(new Rect(0,5,800,35),"Brak części pasujących do wyszukiwania.",small);
            GUI.EndScrollView();GUI.backgroundColor=Color.white;
            DrawInventory(run);
            if(!string.IsNullOrEmpty(catalogFeedback))GUI.Label(new Rect(25,675,900,30),catalogFeedback,small);
        }
        bool InCatalogGroup(R3PartState item,R3PartKind kind,R4WeaponKind weapon,string bonus) =>
            item.Kind==kind&&(kind!=R3PartKind.Gun||item.Weapon==weapon)&&(kind!=R3PartKind.BonusModule||CatalogBonusKey(item)==bonus)&&
            (kind!=R3PartKind.Wheel||item.WheelStyle==CatalogWheelStyle(bonus))&&
            (kind!=R3PartKind.Armor||item.ArmorShape==CatalogArmorShape(bonus));
        static string CatalogBonusGroupName(R3PartState part) =>
            string.IsNullOrWhiteSpace(part.UpgradeTitle)?"Moduł bonusu":part.UpgradeTitle;
        static string CatalogStackCaption(string name,int count,bool usable,bool unlocked) =>
            $"{name} × {count}"+(count>0&&!usable?" • napraw":!unlocked?" • blokada":"");
        string CatalogGroupCaption(R1RunController run,R3PartKind kind,R4WeaponKind weapon,string bonus,string name)
        {
            int count=0;bool usable=false;
            foreach(var item in run.VehicleModel.Inventory)
                if(InCatalogGroup(item,kind,weapon,bonus)){count++;if(item.Health>0)usable=true;}
            return CatalogStackCaption(name,count,usable,run.CanBuyTechnology(kind,weapon));
        }
        void DrawCatalogRow(R1RunController run,R3PartKind kind,R4WeaponKind weapon,string bonus,string name,int row)
        {
            bool chosen=catalogGroup==kind&&(kind!=R3PartKind.Gun||catalogWeapon==weapon)&&
                (kind!=R3PartKind.BonusModule&&kind!=R3PartKind.Wheel&&kind!=R3PartKind.Armor||catalogBonusId==bonus);
            GUI.backgroundColor=chosen?new Color(.3f,.9f,.9f):Color.white;
            if(GUI.Button(new Rect((row%3)*290,(row/3)*55,280,49),CatalogGroupCaption(run,kind,weapon,bonus,name),compactButton))
            {
                catalogGroup=kind;catalogWeapon=weapon;catalogBonusId=bonus;stackScroll=Vector2.zero;dismantleConfirmation=null;catalogFeedback=null;
                var garage=run.Garage;garage.SelectedPart=kind;garage.SelectedWeapon=weapon;
                garage.SelectedInventoryId=null;garage.UseInventory=true;garage.SetTool(R41GarageTool.Mount);
                inventoryOpen=true;
            }
        }
        void DrawDriveButtons(R1RunController run,float x,float y,float width,bool row=false)
        {
            for(int i=0;i<4;i++)
            {
                var mode=(R3DriveKind)i;GUI.enabled=R3VehicleModel.HasMobility(run.VehicleModel.Parts,mode);
                GUI.backgroundColor=run.VehicleModel.SelectedDrive==mode?new Color(.3f,.9f,.9f):Color.white;
                var rect=row?new Rect(x+i*width/4,y,width/4-4,30):new Rect(x+i%2*width/2,y+i/2*35,width/2-4,31);
                if(GUI.Button(rect,driveNames[i],compactButton))run.ConfigureDrive(mode);
            }
            GUI.enabled=true;GUI.backgroundColor=Color.white;
        }
        void ChangeMovement(R1RunController run,float power,float duration,bool gyro,float height)=>run.ConfigureMovement(power,duration,gyro,height);
        void DrawMovementGarage(R1RunController run)
        {
            var m=run.VehicleModel;string[] tabs={"Napęd","Moduł","Zawieszenie"};
            if(garageTabStyle==null)garageTabStyle=new GUIStyle(compactButton){fontSize=13};
            for(int i=0;i<3;i++)
            {
                GUI.backgroundColor=settingsTab==i?new Color(.3f,.9f,.9f):Color.white;
                if(GUI.Button(new Rect(30+i*94,320,90,29),tabs[i],garageTabStyle))settingsTab=i;
            }
            GUI.backgroundColor=Color.white;
            if(settingsTab==0)
            {
                GUI.Label(new Rect(30,355,280,25),m.SelectedDrive==R3DriveKind.Hover?"Hover • zawieszenie wyłączone":"Jeden aktywny napęd • wybierz:",small);
                DrawDriveButtons(run,30,383,280);
                GUI.Label(new Rect(30,455,280,95),"Aby odblokować napęd, zamontuj sprawne części:\nKoła lub gąsienice: po jednej z każdej strony.\nOmni lub Hover: co najmniej jeden moduł.",small);
                return;
            }
            if(settingsTab==2){DrawSuspensionGarage(run);return;}
            var selected=run.Garage.SelectedPart;
            if(selected==R3PartKind.Booster)
            {
                GUI.Label(new Rect(30,355,280,25),$"{m.BoosterThrustNewtons:0} N • {m.BoosterDurationSeconds:0.0} s • Shift",small);
                if(GUI.Button(new Rect(30,384,135,31),"Siła − 500 N",compactButton))ChangeMovement(run,m.BoosterThrustNewtons-500,m.BoosterDurationSeconds,m.GyroEnabled,m.HoverHeightMeters);
                if(GUI.Button(new Rect(175,384,135,31),"Siła + 500 N",compactButton))ChangeMovement(run,m.BoosterThrustNewtons+500,m.BoosterDurationSeconds,m.GyroEnabled,m.HoverHeightMeters);
                if(GUI.Button(new Rect(30,420,135,31),"Czas − 0,2 s",compactButton))ChangeMovement(run,m.BoosterThrustNewtons,m.BoosterDurationSeconds-.2f,m.GyroEnabled,m.HoverHeightMeters);
                if(GUI.Button(new Rect(175,420,135,31),"Czas + 0,2 s",compactButton))ChangeMovement(run,m.BoosterThrustNewtons,m.BoosterDurationSeconds+.2f,m.GyroEnabled,m.HoverHeightMeters);
            }
            else if(selected==R3PartKind.HoverEngine)
            {
                GUI.Label(new Rect(30,355,280,55),$"Wysokość Hover: {m.HoverHeightMeters:0.0} m\nRozmieść 4 silniki na rogach.",small);
                if(GUI.Button(new Rect(30,420,135,31),"Niżej − 0,1 m",compactButton))ChangeMovement(run,m.BoosterThrustNewtons,m.BoosterDurationSeconds,m.GyroEnabled,m.HoverHeightMeters-.1f);
                if(GUI.Button(new Rect(175,420,135,31),"Wyżej + 0,1 m",compactButton))ChangeMovement(run,m.BoosterThrustNewtons,m.BoosterDurationSeconds,m.GyroEnabled,m.HoverHeightMeters+.1f);
            }
            else if(selected==R3PartKind.Battery)
                GUI.Label(new Rect(30,355,280,95),"Bateria: T1 +100, T2 +200, T3 +400 maks. energii.\nDziała po zamontowaniu.\nPole ładowania przyspiesza regenerację.",small);
            else if(selected==R3PartKind.RepairPulse)
                GUI.Label(new Rect(30,355,280,100),"V — impuls naprawczy\nDziała 5 s • odnowienie 15 s\nWyższy tier leczy szybciej.\nNie odbudowuje zniszczonych.",small);
            else if(selected==R3PartKind.DrivePower)
                GUI.Label(new Rect(30,355,280,100),$"Silnik kół wspomaga\nprzyspieszanie i skręcanie.\nRezerwa: {VehicleEnergyState.PassiveReservation:0} energii na moduł.\nPotrzebuje kół na podłożu.",small);
            else if(selected==R3PartKind.EnergyRegenerator)
                GUI.Label(new Rect(30,355,280,100),$"Regenerator T1/T2/T3: +{VehicleEnergyState.RegenerationPerModule*R3VehicleModel.TierEffectUnits(1):0}/+{VehicleEnergyState.RegenerationPerModule*R3VehicleModel.TierEffectUnits(2):0}/+{VehicleEnergyState.RegenerationPerModule*R3VehicleModel.TierEffectUnits(3):0} energii/s.\nNa postoju regeneracja ×4.\nRezerwa: {VehicleEnergyState.PassiveReservation:0} energii na moduł.",small);
            else if(selected==R3PartKind.PointDefense)
                GUI.Label(new Rect(30,355,280,100),"Obrona 1×1×1 • automat 24 m\n40 nabojów • T przeładuj (3 s)\nChroni przed pociskami i rakietami.\nWymaga wolnej linii strzału.",small);
            else if(selected==R3PartKind.DirectionalShield)
            {
                GUI.Label(new Rect(30,355,280,100),"Tarcza 1×1×1 • ładuje się sama\n100 osłony • 120° • zasięg 9 m\nRegeneracja 12/s po 3 s.\nKierunek zmienisz obrotem XYZ.",small);
            }
            else if(selected==R3PartKind.IndependentSteering)
                GUI.Label(new Rect(30,355,280,100),"M — zmień tryb podczas jazdy\nPrzód / tył / przeciwnie / krab.\nWymaga 4 prostych kół narożnych.\nInny układ: standardowy skręt R6.",small);
            else if(selected==R3PartKind.Gyroscope)
            {
                GUI.Label(new Rect(30,355,280,55),"Żyroskop prostuje przechył.\nG przełącza go podczas jazdy.",small);
                if(GUI.Button(new Rect(30,420,280,31),m.GyroEnabled?"Żyroskop: ON":"Żyroskop: OFF",compactButton))ChangeMovement(run,m.BoosterThrustNewtons,m.BoosterDurationSeconds,!m.GyroEnabled,m.HoverHeightMeters);
            }
            else
            {
                string hint=selected==R3PartKind.JumpDrive?"Spacja — skok z podłoża.\nKilka modułów zwiększa impuls.":selected==R3PartKind.Blink?"Q — Blink do przodu (8 m).\nDroga i lądowanie muszą być wolne.":selected==R3PartKind.MagneticGrapple?"E — zaczep w punkt celowania.\nE ponownie — zwolnij linę.":"Kliknij ścianę bloku, aby montować.\nZielony ghost: miejsce poprawne.\nOmni i gąsienice montuj nisko.";
                GUI.Label(new Rect(30,355,280,92),hint,small);
            }
        }
        void DrawMovementPause(R1RunController run)
        {
            if(run.Player?.GetComponent<MOVSeriesVehicleAdapter>()==null)return;
            GUI.Label(new Rect(230,575,820,25),"Napęd po wznowieniu • Hover wyłącza siły MOV009",small);
            DrawDriveButtons(run,230,603,820,true);
        }
        static string AbilityStatus(MOVSeriesVehicleAdapter a,R3PartKind kind)
        {
            float wait=a.ReadyIn(kind);return float.IsPositiveInfinity(wait)?"niedostępny":wait>.05f?$"{wait:0.0} s":"gotowy";
        }
        readonly List<string> compactMovementRows=new List<string>(5);
        static string CompactMovementPair(string first,string second)=>string.IsNullOrEmpty(first)?second:
            string.IsNullOrEmpty(second)?first:first+" • "+second;
        static Rect CompactMovementPanelRect(int rows)
        {
            float top=615-Mathf.Clamp(rows,1,5)*24-10;
            return new Rect(18,top,610,615-top);
        }
        void RebuildMovementRows(R1RunController run,MOVSeriesVehicleAdapter a,string steeringStatus)
        {
            var m=run.VehicleModel;compactMovementRows.Clear();
            compactMovementRows.Add($"NAPĘD: {driveNames[(int)m.SelectedDrive]} • {(a.HasActiveDrive?"sprawny":"niedostępny")}");
            bool Installed(R3PartKind kind)=>m.Parts.Any(p=>p.Kind==kind);
            void AddPair(string first,string second)
            {string line=CompactMovementPair(first,second);if(!string.IsNullOrEmpty(line))compactMovementRows.Add(line);}
            AddPair(Installed(R3PartKind.Booster)?$"Shift Booster: {(a.Boosting?"aktywny":AbilityStatus(a,R3PartKind.Booster))}":null,
                Installed(R3PartKind.JumpDrive)?$"Spacja skok: {AbilityStatus(a,R3PartKind.JumpDrive)}":null);
            string grapple=Installed(R3PartKind.MagneticGrapple)?a.OperationalCount(R3PartKind.MagneticGrapple)==0?"niedostępny":
                a.GrappleAttached?"E zwalnia":m.GrappleReadyAt>Time.time?$"{m.GrappleReadyAt-Time.time:0.0} s":"gotowy":null;
            AddPair(Installed(R3PartKind.Blink)?$"Q Blink: {AbilityStatus(a,R3PartKind.Blink)}":null,
                grapple!=null?$"E zaczep: {grapple}":null);
            AddPair(Installed(R3PartKind.Gyroscope)?$"G żyroskop: {(a.OperationalCount(R3PartKind.Gyroscope)==0?"niedostępny":m.GyroEnabled?"ON":"OFF")}":null,
                m.SelectedDrive==R3DriveKind.Hover?$"Hover: {m.HoverHeightMeters:0.0} m":null);
            string detail=steeringStatus??a.Feedback;
            if(!string.IsNullOrEmpty(detail))compactMovementRows.Add(detail);
        }
        void DrawMovementTelemetry(R1RunController run)
        {
            if(run.Paused||run.Transitioning)return;
            var a=run.Player?.GetComponent<MOVSeriesVehicleAdapter>();
            var steering=run.Player?.GetComponentInChildren<RoboRougelike.PackageIntegration.RRLIndependentSteeringBinding>();
            if(a==null)
            {
                if(steering!=null)
                {Panel(new Rect(18,565,610,50));GUI.Label(new Rect(33,577,580,28),steering.Status,small);}
                return;
            }
            RebuildMovementRows(run,a,steering!=null?steering.Status:null);
            var panel=CompactMovementPanelRect(compactMovementRows.Count);Panel(panel);
            for(int i=0;i<compactMovementRows.Count;i++)GUI.Label(new Rect(33,panel.y+5+i*24,580,23),compactMovementRows[i],small);
        }
    }
}
