using UnityEngine;
using RoboRougelike.R6;
namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        GUIStyle aimCrossStyle,aimCaptionStyle,weaponMarkerStyle;
        void DrawTestRange(R1RunController run)
        {
            if(run.MapEditorTesting){DrawMapTest(run);return;}
            if(run.Transitioning){Panel(new Rect(0,0,1280,720));GUI.Label(new Rect(300,300,700,80),"Przygotowanie poligonu…",title);return;}
            if(run.Paused)
            {
                Panel(new Rect(190,115,900,550));
                GUI.Label(new Rect(230,145,820,65),run.RangeDisabled?"Pojazd testowy unieruchomiony":"POLIGON • PAUZA",title);
                GUI.Label(new Rect(230,225,820,50),"Testujesz kopię. Uszkodzenia, Złom i części z tej próby nie zmieniają konstrukcji w garażu.",body);
                var pausedRange=run.GetComponent<R6TestRange>();
                if(pausedRange?.TrainingTarget!=null)
                {
                    GUI.enabled=!run.RangeDisabled&&!pausedRange.TrainingTarget.IsDefeated;
                    if(GUI.Button(new Rect(230,280,820,36),pausedRange.TrainingFireRequested?"Wstrzymaj ostrzał celu":"Włącz ostrzał celu po wznowieniu",compactButton))
                        pausedRange.SetTrainingFire(!pausedRange.TrainingFireRequested);
                    GUI.enabled=true;
                }
                if(!run.RangeDisabled&&GUI.Button(new Rect(230,325,820,60),"Wznów  [Esc]",button))run.TogglePause();
                if(GUI.Button(new Rect(230,400,395,60),"Reset próby  [R]",button))run.ResetTestRange();
                if(GUI.Button(new Rect(650,400,400,60),"Powrót do garażu",button))run.ReturnFromTestRange();
                DrawSuspensionPause(run);
                DrawMovementPause(run);
                if(GUI.Button(new Rect(230,660,395,42),PauseCaption(run,1,"Powrót do menu głównego"),compactButton))run.RestartRun();
                if(GUI.Button(new Rect(650,660,400,42),PauseCaption(run,2,"Wyjdź z gry"),compactButton))run.RequestQuit();
                return;
            }
            Panel(new Rect(18,18,610,150));
            GUI.Label(new Rect(34,26,580,34),"POLIGON • kopia konstrukcji",heading);
            var drive=run.Player.GetComponent<R6VehicleDrive>();var bodyRb=run.Player.GetComponent<Rigidbody>();
            GUI.Label(new Rect(34,65,580,90),$"Prędkość: {bodyRb.linearVelocity.magnitude:0.0} m/s • Masa: {bodyRb.mass:0} kg\nKoła: {drive?.Wheels??0}/{run.Player.Modules.InitialWheelCount} • Broń: {run.Player.Modules.ActiveWeaponCount}\nRdzeń: {run.Player.Core?.CurrentHp??0:0} HP • HP ocalałych części: {run.Player.HealthFraction*100:0}%",small);
            var range=run.GetComponent<R6TestRange>();
            if(range?.TrainingTarget!=null)
            {
                Panel(new Rect(925,18,335,150));GUI.Label(new Rect(940,25,305,30),"CEL ELEKTRONICZNY",heading);
                GUI.Label(new Rect(940,62,305,38),range.TrainingFireRequested?"Ostrzał WŁĄCZONY":"Ostrzał WYŁĄCZONY",body);
                GUI.Label(new Rect(940,106,305,55),range.TrainingTarget.IsDefeated?"Cel zniszczony. R resetuje próbę.":"Robot na lewo przed startem.\nEsc — sterowanie ostrzałem celu.",small);
            }
            DrawAimReticle(run);
            Panel(new Rect(18,625,1244,78));
            DrawFeedback();
            GUI.Label(new Rect(34,635,1200,60),"WASD — napęd • Omni/Hover: A/D — bok, Z/C — obrót • LPM/PPM — ogień • R — reset • Esc — menu\nSkoki x≈−12 • Kotwa x≈−25, z≈7 • Slalom pośrodku • Tor MOV009 x≈12 • Cele na północy",small);
        }
        void DrawAimReticle(R1RunController run)
        {
            var feedback=run.Player.Controller.AimFeedback;
            DrawWeaponReticles(run.Player.Controller);
            if(aimCrossStyle==null)aimCrossStyle=new GUIStyle(heading){alignment=TextAnchor.MiddleCenter};
            if(aimCaptionStyle==null)aimCaptionStyle=new GUIStyle(small){alignment=TextAnchor.UpperLeft};
            aimCrossStyle.normal.textColor=feedback.Clear>0?new Color(.2f,.85f,.85f):new Color(1,.72f,.3f);
            GUI.Label(new Rect(624,344,32,32),"+",aimCrossStyle);
            Panel(new Rect(690,18,225,65));
            GUI.Label(new Rect(702,25,201,51),feedback.Caption,aimCaptionStyle);
        }
        void DrawWeaponReticles(RogueMachine.P12PipelineSafe.P12PipelineSafeTankController controller)
        {
            if(Event.current.type!=EventType.Repaint||Camera.main==null)return;
            if(weaponMarkerStyle==null)weaponMarkerStyle=new GUIStyle(small){fontSize=12,alignment=TextAnchor.MiddleCenter};
            var camera=Camera.main;
            var original=GUI.color;var markers=controller.WeaponReticles;
            for(int i=0;i<markers.Count;i++)
            {
                var marker=markers[i];var point=RoboRougelike.WeaponIntegration.RRLWeaponReticles.ToHudPoint(marker.Viewport,camera.pixelRect,new Vector2(Screen.width,Screen.height));
                float x=point.x,y=point.y;
                // Leave room for the outermost of four brackets and its number.
                x=Mathf.Clamp(x,46,1234);y=Mathf.Clamp(y,46,674);
                float radius=8+Mathf.Min(i,3)*4,stroke=2,length=5;
                GUI.color=marker.Disabled?new Color(.55f,.55f,.55f,.9f):
                    marker.Obstructed||marker.OutOfRange||marker.Offscreen?new Color(1,.7f,.25f,.95f):new Color(.35f,.95f,.95f,.9f);
                for(int xi=0;xi<2;xi++)for(int yi=0;yi<2;yi++)
                {
                    int sx=xi==0?-1:1,sy=yi==0?-1:1;
                    float cx=x+sx*radius,cy=y+sy*radius;
                    GUI.DrawTexture(new Rect(cx-(sx<0?0:length),cy-stroke*.5f,length,stroke),Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(cx-stroke*.5f,cy-(sy<0?0:length),stroke,length),Texture2D.whiteTexture);
                }
                float angle=(-45-i*90)*Mathf.Deg2Rad;
                GUI.Label(new Rect(x+Mathf.Cos(angle)*(radius+13)-10,y+Mathf.Sin(angle)*(radius+13)-10,20,20),(i+1).ToString(),weaponMarkerStyle);
            }
            GUI.color=original;
        }
        void DrawFeedback()
        {
            var run=R1RunController.Instance;
            if(run!=null)DrawMovementTelemetry(run);
            if(run!=null)DrawSuspensionTelemetry(run);
            if(run?.Player!=null)
            {
                Panel(new Rect(925,190,335,158));GUI.Label(new Rect(940,195,305,30),"UZBROJENIE",heading);
                var assembly=run.Player.GetComponent<RoboRougelike.R3.R3VehicleAssembly>();
                var a=run.VehicleModel.WeaponGroupA;var b=run.VehicleModel.WeaponGroupB;
                int countA=0,countB=0;
                foreach(var part in assembly.Parts)
                    if(part!=null&&part.Kind==RoboRougelike.R3.R3PartKind.Gun&&part.Operational&&part.transform.IsChildOf(assembly.transform))
                    {if(a==part.Weapon)countA++;if(b==part.Weapon)countB++;}
                GUI.Label(new Rect(940,228,305,24),$"A / LPM: {WeaponGroupName(a)} ({countA})",small);
                GUI.Label(new Rect(940,252,305,24),$"B / PPM: {WeaponGroupName(b)} ({countB})",small);
                int row=0;
                foreach(var gun in assembly.Parts)
                {
                    if(gun==null||gun.Kind!=RoboRougelike.R3.R3PartKind.Gun||!gun.Operational||!gun.transform.IsChildOf(run.Player.transform))continue;
                    if(row>=2)break; // Group counts stay bounded even on large builds.
                    float reload=Mathf.Max(0,gun.NextFireTime-Time.time);
                    var shotgun=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
                    var rail=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLRailgunBinding>();
                    var beam=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLBeamBinding>();
                    var rocket=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLRocketBinding>();
                    var saw=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLSawBinding>();
                    string status=saw!=null?($"{saw.Module.Motor.Rpm:0} RPM • {saw.Module.Motor.Heat:0}%"+(saw.Module.Motor.JamRemaining>0?" • blokada":"")):rocket!=null?($"{rocket.State.Ammo}/10 • "+(rocket.State.ReloadReadyAt>Time.time?"ładowanie":"T przeładuj")):beam!=null?($"temperatura {beam.Module.HeatFraction*100:0}%"+(beam.Module.IsOverheated?" • przegrzana":"")):rail!=null?($"{rail.State.Ammo}/12 • "+(rail.State.ReloadReadyAt>Time.time?"ładowanie":"T przeładuj")):shotgun!=null?($"{shotgun.State.Ammo}/12 • "+(shotgun.State.ReloadReadyAt>Time.time?"ładowanie":"T przeładuj")):
                        (reload>0?$"{reload:0.0} s":"gotowa");
                    var emp=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLEmpBinding>();
                    if(emp!=null)status=$"{emp.Module.CooldownRemaining:0.0}s • impuls {emp.LastReport.ReceiversAffected} modułów";
                    var disrupted=gun.GetComponent<RoboRougelike.Mechanics.EMP.EmpReceiver>();
                    if(disrupted!=null&&disrupted.IsDisrupted)status=$"EMP {disrupted.RemainingSeconds:0.0}s";
                    GUI.Label(new Rect(940,278+row*26,305,26),(row+1)+". "+RoboRougelike.R4.R4WeaponDefinition.Get(gun.Weapon).ShortName+" • "+status,small);row++;
                }
                var defenses=run.Player.GetComponentsInChildren<RoboRougelike.WeaponIntegration.RRLPointDefenseBinding>();
                var shields=run.Player.GetComponentsInChildren<RoboRougelike.WeaponIntegration.RRLShieldBinding>();
                int defenseRow=0;
                if(defenses.Length+shields.Length>0)
                {
                    Panel(new Rect(18,184,610,38+25*(defenses.Length+shields.Length)));GUI.Label(new Rect(33,188,580,25),"OBRONA",heading);
                    foreach(var shield in shields)
                    {GUI.Label(new Rect(33,219+defenseRow*25,580,25),$"DEF-001 • {shield.Generator.Charge:0}/100 • "+(shield.Generator.Operational?"ON":"OFF"),small);defenseRow++;}
                    foreach(var defense in defenses)
                    {GUI.Label(new Rect(33,219+defenseRow*25,580,25),$"DEF-002 • {defense.State.Ammo}/40 • "+(defense.Controller.IsOperational?"ON":"OFF")+" • T przeładuj",small);defenseRow++;}
                }
            }
            var feedback=Object.FindFirstObjectByType<R6CombatFeedback>();if(feedback==null)return;
            var lines=feedback.Notices();
            for(int i=0;i<lines.Length;i++)
            {Panel(new Rect(660,490+i*39,600,35));GUI.Label(new Rect(675,492+i*39,565,32),lines[i],small);}
        }
    }
}
