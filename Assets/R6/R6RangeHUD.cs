using UnityEngine;
using RoboRougelike.R6;
namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        void DrawTestRange(R1RunController run)
        {
            if(run.Transitioning){Panel(new Rect(0,0,1280,720));GUI.Label(new Rect(300,300,700,80),"Przygotowanie poligonu…",title);return;}
            if(run.Paused)
            {
                Panel(new Rect(190,115,900,550));
                GUI.Label(new Rect(230,145,820,65),run.RangeDisabled?"Pojazd testowy unieruchomiony":"POLIGON • PAUZA",title);
                GUI.Label(new Rect(230,225,820,85),"Testujesz kopię. Uszkodzenia, Złom i części z tej próby nie zmieniają konstrukcji w garażu.",body);
                if(!run.RangeDisabled&&GUI.Button(new Rect(230,325,820,60),"Wznów  [Esc]",button))run.TogglePause();
                if(GUI.Button(new Rect(230,400,395,60),"Reset próby  [R]",button))run.ResetTestRange();
                if(GUI.Button(new Rect(650,400,400,60),"Powrót do garażu",button))run.ReturnFromTestRange();
                DrawSuspensionPause(run);
                DrawMovementPause(run);
                return;
            }
            Panel(new Rect(18,18,610,150));
            GUI.Label(new Rect(34,26,580,34),"POLIGON • kopia konstrukcji",heading);
            var drive=run.Player.GetComponent<R6VehicleDrive>();var bodyRb=run.Player.GetComponent<Rigidbody>();
            GUI.Label(new Rect(34,65,580,90),$"Prędkość: {bodyRb.linearVelocity.magnitude:0.0} m/s • Masa: {bodyRb.mass:0} kg\nKoła: {drive?.Wheels??0}/{run.Player.Modules.InitialWheelCount} • Broń: {run.Player.Modules.ActiveWeaponCount}\nRdzeń: {run.Player.Core?.CurrentHp??0:0} HP • HP ocalałych części: {run.Player.HealthFraction*100:0}%",small);
            GUI.Label(new Rect(629,341,32,38),"+",heading);
            Panel(new Rect(18,625,1244,78));
            DrawFeedback();
            GUI.Label(new Rect(34,635,1200,60),"WASD — napęd • Omni/Hover: A/D — bok, Z/C — obrót • LPM — ogień • R — reset • Esc — menu\nSkoki x≈−12 • Kotwa x≈−25, z≈7 • Slalom pośrodku • Tor MOV009 x≈12 • Cele na północy",small);
        }
        void DrawFeedback()
        {
            var run=R1RunController.Instance;
            if(run!=null)DrawMovementTelemetry(run);
            if(run!=null)DrawSuspensionTelemetry(run);
            if(run?.Player!=null)
            {
                Panel(new Rect(925,190,335,158));GUI.Label(new Rect(940,195,305,30),"UZBROJENIE",heading);
                int row=0;
                foreach(var gun in run.Player.GetComponent<RoboRougelike.R3.R3VehicleAssembly>().Parts)
                {
                    if(gun==null||gun.Kind!=RoboRougelike.R3.R3PartKind.Gun||!gun.Operational||!gun.transform.IsChildOf(run.Player.transform))continue;
                    float reload=Mathf.Max(0,gun.NextFireTime-Time.time);
                    var shotgun=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
                    string status=shotgun!=null?($"{shotgun.State.Ammo}/12 • "+(shotgun.State.ReloadReadyAt>Time.time?"ładowanie":"T przeładuj")):
                        (reload>0?$"{reload:0.0} s":"gotowa");
                    GUI.Label(new Rect(940,232+row*26,305,26),RoboRougelike.R4.R4WeaponDefinition.Get(gun.Weapon).Name+" • "+status,small);row++;
                }
            }
            var feedback=Object.FindFirstObjectByType<R6CombatFeedback>();if(feedback==null)return;
            var lines=feedback.Notices();
            for(int i=0;i<lines.Length;i++)
            {Panel(new Rect(660,490+i*39,600,35));GUI.Label(new Rect(675,492+i*39,565,32),lines[i],small);}
        }
    }
}
