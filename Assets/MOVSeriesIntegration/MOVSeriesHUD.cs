using System;
using System.Linq;
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
        R3PartKind? catalogGroup;
        R4WeaponKind catalogWeapon;
        string catalogBonusId;
        GUIStyle garageTabStyle;
        static readonly R3PartKind[] shopKinds={R3PartKind.Armor,R3PartKind.Wheel,R3PartKind.Gun,R3PartKind.Suspension,
            R3PartKind.Booster,R3PartKind.JumpDrive,R3PartKind.Blink,R3PartKind.Gyroscope,
            R3PartKind.OmniDrive,R3PartKind.MagneticGrapple,R3PartKind.HoverEngine,R3PartKind.CrawlerTrack,R3PartKind.DirectionalShield,R3PartKind.PointDefense,R3PartKind.StructuralScanner,R3PartKind.ScannerPulse,R3PartKind.IndependentSteering,R3PartKind.Battery};
        static readonly string[] shopNames={"Pancerz","Koło","Broń","Zawieszenie","Booster","Skok","Blink","Żyroskop","Omni","Zaczep","Hover","Gąsienica","Tarcza","Obrona pkt.","Skan struktury","Impuls skanera","Skręt osi","Bateria +100"};
        static readonly string[] driveNames={"Koła","Gąsienice","Omni","Hover"};
        void DrawMovementCatalog(R1RunController run)
        {
            if(GUI.Button(new Rect(25,108,295,99),"KATALOG CZĘŚCI\nWybierz stos, kup lub zamontuj",compactButton))catalogOpen=true;
        }
        void DrawFullCatalog(R1RunController run)
        {
            Panel(new Rect(0,0,1280,720));
            GUI.Label(new Rect(25,20,870,48),"KATALOG CZĘŚCI • MAGAZYN",heading);
            GUI.Label(new Rect(25,60,870,25),$"Złom: {run.VehicleModel.Scrap}  •  wybierz stos, potem egzemplarz według HP",small);
            if(GUI.Button(new Rect(1080,20,175,40),"Wróć do garażu",compactButton)){catalogOpen=false;return;}
            var model=run.VehicleModel;
            var bonuses=model.Inventory.Where(p=>p.Kind==R3PartKind.BonusModule).Select(p=>p.UpgradeId).Distinct().ToArray();
            int rows=shopKinds.Length-1+R4WeaponDefinition.Count+bonuses.Length;
            catalogScroll=GUI.BeginScrollView(new Rect(25,98,900,570),catalogScroll,new Rect(0,0,875,Mathf.Max(570,Mathf.CeilToInt(rows/3f)*55)));
            int row=0;
            for(int i=0;i<shopKinds.Length;i++)
            {
                var kind=shopKinds[i];if(kind==R3PartKind.Gun)continue;
                DrawCatalogRow(run,kind,R4WeaponKind.Basic,null,shopNames[i],row++);
            }
            for(int i=0;i<R4WeaponDefinition.Count;i++)
            {var weapon=(R4WeaponKind)i;DrawCatalogRow(run,R3PartKind.Gun,weapon,null,R4WeaponDefinition.Get(weapon).Name,row++);}
            foreach(var bonus in bonuses)
            {var item=model.Inventory.First(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeId==bonus);DrawCatalogRow(run,R3PartKind.BonusModule,R4WeaponKind.Basic,bonus,R4WeaponDefinition.PartName(item),row++);}
            GUI.EndScrollView();GUI.backgroundColor=Color.white;
            DrawInventory(run);
        }
        bool InCatalogGroup(R3PartState item,R3PartKind kind,R4WeaponKind weapon,string bonus) =>
            item.Kind==kind&&(kind!=R3PartKind.Gun||item.Weapon==weapon)&&(kind!=R3PartKind.BonusModule||item.UpgradeId==bonus);
        void DrawCatalogRow(R1RunController run,R3PartKind kind,R4WeaponKind weapon,string bonus,string name,int row)
        {
            int count=run.VehicleModel.Inventory.Count(p=>InCatalogGroup(p,kind,weapon,bonus));
            bool chosen=catalogGroup==kind&&(kind!=R3PartKind.Gun||catalogWeapon==weapon)&&(kind!=R3PartKind.BonusModule||catalogBonusId==bonus);
            GUI.backgroundColor=chosen?new Color(.3f,.9f,.9f):Color.white;
            if(GUI.Button(new Rect((row%3)*290,(row/3)*55,280,49),$"{name}  × {count}",compactButton))
            {
                catalogGroup=kind;catalogWeapon=weapon;catalogBonusId=bonus;stackScroll=Vector2.zero;
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
                GUI.Label(new Rect(30,355,280,25),m.SelectedDrive==R3DriveKind.Hover?"Hover • podparcie MOV009: OFF":"Jeden aktywny napęd • wybierz:",small);DrawDriveButtons(run,30,383,280);return;
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
                GUI.Label(new Rect(30,355,280,95),"Bateria: +100 maks. energii.\nDziała po zamontowaniu.\nEnergia odnawia się powoli;\nturkusowe pole ładuje szybciej.",small);
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
            float wait=a.ReadyIn(kind);return float.IsPositiveInfinity(wait)?"brak":wait>.05f?$"{wait:0.0} s":"gotowy";
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
            Panel(new Rect(18,490,610,125));var m=run.VehicleModel;
            GUI.Label(new Rect(33,495,580,23),$"NAPĘD: {driveNames[(int)m.SelectedDrive]}",small);
            GUI.Label(new Rect(33,519,580,23),$"Shift Booster: {(a.Boosting?"aktywny":AbilityStatus(a,R3PartKind.Booster))} • Spacja skok: {AbilityStatus(a,R3PartKind.JumpDrive)}",small);
            string grapple=a.OperationalCount(R3PartKind.MagneticGrapple)==0?"brak":a.GrappleAttached?"E zwalnia":m.GrappleReadyAt>Time.time?$"{m.GrappleReadyAt-Time.time:0.0} s":"gotowy";
            GUI.Label(new Rect(33,543,580,23),$"Q Blink: {AbilityStatus(a,R3PartKind.Blink)} • E zaczep: {grapple}",small);
            GUI.Label(new Rect(33,567,580,23),$"G żyroskop: {(a.OperationalCount(R3PartKind.Gyroscope)==0?"brak":m.GyroEnabled?"ON":"OFF")} • Hover: {m.HoverHeightMeters:0.0} m • {(a.HasActiveDrive?"napęd sprawny":"utrata napędu")}",small);
            GUI.Label(new Rect(33,590,580,23),steering!=null?steering.Status:a.Feedback??"Regulacja w garażu • Esc — pauza / zmiana napędu",small);
        }
    }
}
