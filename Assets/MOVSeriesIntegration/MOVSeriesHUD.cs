using System;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.MOVSeries;
using RoboRougelike.R4_1;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        int partsPage,settingsTab;
        GUIStyle garageTabStyle;
        static readonly R3PartKind[] shopKinds={R3PartKind.Armor,R3PartKind.Wheel,R3PartKind.Gun,R3PartKind.Suspension,
            R3PartKind.Booster,R3PartKind.JumpDrive,R3PartKind.Blink,R3PartKind.Gyroscope,
            R3PartKind.OmniDrive,R3PartKind.MagneticGrapple,R3PartKind.HoverEngine,R3PartKind.CrawlerTrack,R3PartKind.DirectionalShield,R3PartKind.PointDefense,R3PartKind.StructuralScanner,R3PartKind.ScannerPulse,R3PartKind.IndependentSteering};
        static readonly string[] shopNames={"Pancerz","Koło","Broń","Zawieszenie","Booster","Skok","Blink","Żyroskop","Omni","Zaczep","Hover","Gąsienica","Tarcza","Obrona pkt.","Skan struktury","Impuls skanera","Skręt osi"};
        static readonly string[] driveNames={"Koła","Gąsienice","Omni","Hover"};
        void DrawMovementCatalog(R1RunController run)
        {
            var garage=run.Garage;
            int pages=Mathf.Max(1,(shopKinds.Length+3)/4);partsPage=Mathf.Clamp(partsPage,0,pages-1);
            if(GUI.Button(new Rect(25,108,40,23),"‹",compactButton))partsPage=(partsPage+pages-1)%pages;
            GUI.Label(new Rect(70,108,205,23),$"Części {partsPage+1}/{pages} • wybierz moduł",small);
            if(GUI.Button(new Rect(280,108,40,23),"›",compactButton))partsPage=(partsPage+1)%pages;
            for(int i=0;i<4;i++)
            {
                int index=partsPage*4+i;if(index>=shopKinds.Length)break;var kind=shopKinds[index];
                GUI.backgroundColor=garage.SelectedPart==kind?new Color(.3f,.9f,.9f):Color.white;
                if(GUI.Button(new Rect(25+i%2*150,137+i/2*36,145,33),$"{shopNames[index]} • {run.VehicleModel.Price(kind,garage.SelectedWeapon)} Z",compactButton))
                {
                    garage.SelectedPart=kind;garage.SelectedInventoryId=null;garage.UseInventory=false;inventoryOpen=false;garage.SetTool(R41GarageTool.Mount);
                    settingsTab=kind==R3PartKind.Suspension?2:kind==R3PartKind.OmniDrive||kind==R3PartKind.CrawlerTrack?0:1;
                }
            }
            GUI.backgroundColor=Color.white;
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
            else if(selected==R3PartKind.PointDefense)
                GUI.Label(new Rect(30,355,280,100),"Obrona 1×2×2 • automat 24 m\n40 nabojów • T przeładuj (3 s)\nChroni przed pociskami i rakietami.\nWymaga wolnej linii strzału.",small);
            else if(selected==R3PartKind.DirectionalShield)
            {
                GUI.Label(new Rect(30,355,280,100),"Tarcza 1×2×2 • ładuje się sama\n100 osłony • 120° • zasięg 9 m\nRegeneracja 12/s po 3 s.\nKierunek zmienisz obrotem XYZ.",small);
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
            GUI.Label(new Rect(33,495,580,23),$"NAPĘD: {driveNames[(int)m.SelectedDrive]} • WASD ruch • Z/C obrót Omni/Hover",small);
            GUI.Label(new Rect(33,519,580,23),$"Shift Booster: {(a.Boosting?"aktywny":AbilityStatus(a,R3PartKind.Booster))} • Spacja skok: {AbilityStatus(a,R3PartKind.JumpDrive)}",small);
            string grapple=a.OperationalCount(R3PartKind.MagneticGrapple)==0?"brak":a.GrappleAttached?"E zwalnia":m.GrappleReadyAt>Time.time?$"{m.GrappleReadyAt-Time.time:0.0} s":"gotowy";
            GUI.Label(new Rect(33,543,580,23),$"Q Blink: {AbilityStatus(a,R3PartKind.Blink)} • E zaczep: {grapple}",small);
            GUI.Label(new Rect(33,567,580,23),$"G żyroskop: {(a.OperationalCount(R3PartKind.Gyroscope)==0?"brak":m.GyroEnabled?"ON":"OFF")} • Hover: {m.HoverHeightMeters:0.0} m • {(a.HasActiveDrive?"napęd sprawny":"utrata napędu")}",small);
            GUI.Label(new Rect(33,590,580,23),steering!=null?steering.Status:a.Feedback??"Regulacja w garażu • Esc — pauza / zmiana napędu",small);
        }
    }
}
