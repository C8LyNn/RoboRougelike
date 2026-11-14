using UnityEngine;
using System.Linq;
using RoboRougelike.R6;
using RoboRougelike.R3;
namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        GUIStyle aimCrossStyle,aimCaptionStyle,weaponMarkerStyle;
        int weaponStatusPage;
        bool creativeEnemyMenu;
        string creativeEnemyMessage;
        internal bool CreativeEnemyMenuOpen=>creativeEnemyMenu;
        internal bool CloseCreativeEnemyMenu()
        {if(!creativeEnemyMenu)return false;creativeEnemyMenu=false;return true;}
        void DrawCreativeEnemyMenu(R1RunController run)
        {
            Panel(new Rect(190,115,900,550));
            GUI.Label(new Rect(230,145,820,60),"POLIGON • PRZECIWNICY",title);
            GUI.Label(new Rect(230,220,820,45),"Wybierz przeciwnika. Pojawi się w pobliżu; zacznie działać po wznowieniu.",small);
            var types=new[]{R3EnemyArchetype.Scout,R3EnemyArchetype.Bruiser,R3EnemyArchetype.Gunner,R3EnemyArchetype.Flanker};
            string[] names={"Zwiadowca","Taranujący","Strzelec","Osaczający"};
            for(int i=0;i<types.Length;i++)
                if(GUI.Button(new Rect(230+(i%2)*420,280+(i/2)*65,395,52),names[i],button))
                    creativeEnemyMessage=run.SpawnCreativeEnemy(types[i])?names[i]+" dodany.":"Nie udało się dodać przeciwnika. Sprawdź wolne miejsce.";
            var range=run.GetComponent<R6TestRange>();
            if(GUI.Button(new Rect(230,430,395,52),"Usuń przeciwników",button))
            {run.ClearCreativeEnemies();creativeEnemyMessage="Usunięto przeciwników.";}
            if(GUI.Button(new Rect(650,430,400,52),"Wróć",button))creativeEnemyMenu=false;
            GUI.Label(new Rect(230,505,820,55),$"Przeciwnicy: {range?.CreativeEnemyCount??0}"+
                (string.IsNullOrEmpty(creativeEnemyMessage)?"":" • "+creativeEnemyMessage),small);
        }
        private static string ManualReloadAction(int ammo,int capacity,float reloadReadyAt,float now,bool available)
            =>!available?"niedostępne":reloadReadyAt>now?"ładowanie":ammo>=capacity?"pełny":R1ControlProfile.Display(R1ControlProfile.Current.Binding("Reload"))+" przeładuj";
        private static string ManualReloadWeaponStatus(int ammo,int capacity,float reloadReadyAt,float now,bool available)
            =>$"{ammo}/{capacity} • {ManualReloadAction(ammo,capacity,reloadReadyAt,now,available)}";
        private static string RocketWeaponStatus(int ammo,float reloadReadyAt,float readyAt,float now)
        {
            string state=ammo<=0||reloadReadyAt>now?"ładowanie":readyAt>now?$"{readyAt-now:0.0} s":"gotowa";
            return $"{ammo}/{RoboRougelike.WeaponIntegration.RRLRocketBinding.Magazine} • {state}";
        }
        private static int WeaponStatusPageCount(int count)=>Mathf.Max(1,(Mathf.Max(0,count)+1)/2);
        private static int WeaponStatusPageStart(int count,int page)=>Mathf.Clamp(page,0,WeaponStatusPageCount(count)-1)*2;
        static string RangeControlsHint()
        {
            var profile=R1ControlProfile.Current;
            string Names(params string[] actions)=>string.Join("/",actions.Select(a=>R1ControlProfile.Display(profile.Binding(a))));
            return Names("MoveForward","MoveBackward","TurnLeft","TurnRight")+" — napęd • "+
                Names("SteeringLeft","SteeringRight")+" — osie • "+Names("Weapon1","Weapon2","Weapon3","Weapon4")+" — broń • "+
                Names("Active1","Active2","Active3","Active4")+" — moduły • "+Names("Deploy1","Deploy2","Deploy3","Deploy4")+" — urządzenia • "+
                Names("ResetRange")+" — reset • Esc — pauza";
        }
        private void AdvanceWeaponStatusPage(int count)
        {int pages=WeaponStatusPageCount(count);weaponStatusPage=(Mathf.Clamp(weaponStatusPage,0,pages-1)+1)%pages;}
        void DrawTestRange(R1RunController run)
        {
            if(run.MapEditorTesting){DrawMapTest(run);return;}
            if(run.Transitioning){Panel(new Rect(0,0,1280,720));GUI.Label(new Rect(300,300,700,80),"Przygotowanie poligonu…",title);return;}
            if(run.Paused)
            {
                if(run.CreativeWorkshop&&creativeEnemyMenu&&!run.RangeDisabled){DrawCreativeEnemyMenu(run);return;}
                Panel(new Rect(190,115,900,550));
                GUI.Label(new Rect(230,145,560,65),run.RangeDisabled?"Pojazd testowy unieruchomiony":"POLIGON • PAUZA",title);
                if(GUI.Button(new Rect(810,150,240,45),PauseCaption(run,5,"Sterowanie"),compactButton))OpenPauseControls();
                GUI.Label(new Rect(230,225,820,50),"Testujesz kopię — uszkodzenia i złom nie zmieniają garażu.\nGłówne akcje: ↑ / ↓ i Enter • pad: krzyżak, A, B / Start",small);
                var pausedRange=run.GetComponent<R6TestRange>();
                if(run.CreativeWorkshop&&!run.RangeDisabled&&GUI.Button(new Rect(230,280,820,36),"Przeciwnicy — dodaj lub usuń",compactButton))
                {creativeEnemyMenu=true;creativeEnemyMessage=null;}
                if(pausedRange?.TrainingTarget!=null)
                {
                    GUI.enabled=!run.RangeDisabled&&!pausedRange.TrainingTarget.IsDefeated;
                    if(GUI.Button(new Rect(230,280,820,36),pausedRange.TrainingFireRequested?"Wstrzymaj ostrzał celu":"Włącz ostrzał celu po wznowieniu",compactButton))
                        pausedRange.SetTrainingFire(!pausedRange.TrainingFireRequested);
                    GUI.enabled=true;
                }
                if(!run.RangeDisabled&&GUI.Button(new Rect(230,325,820,60),PauseCaption(run,0,"Wznów  [Esc]"),button))run.TogglePause();
                if(GUI.Button(new Rect(230,400,395,60),PauseCaption(run,1,$"Reset próby  [{R1ControlProfile.Display(R1ControlProfile.Current.Binding("ResetRange"))}]"),button))run.ResetTestRange();
                if(GUI.Button(new Rect(650,400,400,60),PauseCaption(run,2,"Powrót do garażu"),button))run.ReturnFromTestRange();
                DrawSuspensionPause(run);
                DrawMovementPause(run);
                if(GUI.Button(new Rect(230,660,395,42),PauseCaption(run,3,"Powrót do menu głównego"),compactButton))run.RestartRun();
                if(GUI.Button(new Rect(650,660,400,42),PauseCaption(run,4,"Wyjdź z gry"),compactButton))run.RequestQuitFromMenu();
                TryRangeMouseReset(run);
                return;
            }
            creativeEnemyMenu=false;
            Panel(new Rect(18,18,610,175));
            GUI.Label(new Rect(34,26,580,34),run.CreativeWorkshop?"POLIGON KREATYWNY • kopia konstrukcji":"POLIGON • kopia konstrukcji",heading);
            var drive=run.Player.GetComponent<R6VehicleDrive>();var bodyRb=run.Player.GetComponent<Rigidbody>();
            GUI.Label(new Rect(34,65,580,90),$"Prędkość: {bodyRb.linearVelocity.magnitude:0.0} m/s • Masa: {bodyRb.mass:0} kg\nKoła: {drive?.Wheels??0}/{run.Player.Modules.InitialWheelCount} • Broń: {run.Player.Modules.ActiveWeaponCount}\nRdzeń: {run.Player.Core?.CurrentHp??0:0} HP • HP ocalałych części: {run.Player.HealthFraction*100:0}%",small);
            var energy=run.Player.GetComponent<VehicleEnergyBank>();
            if(energy!=null)
            {
                string charges="";var devices=run.Player.GetComponent<R3EmpDevices>();
                if(devices!=null)
                {
                    var bank=run.VehicleModel.Charges;
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.TargetedEmp))charges+=$" • EMP cel. {bank.Current(R3EmpDevices.TargetPool)}/{bank.Maximum(R3EmpDevices.TargetPool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.AreaEmp))charges+=$" • EMP obsz. {bank.Current(R3EmpDevices.AreaPool)}/{bank.Maximum(R3EmpDevices.AreaPool)}";
                }
                if(run.Player.GetComponent<R3DeployableService>()!=null)
                {
                    var bank=run.VehicleModel.Charges;
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.RampDeployer))charges+=$" • Rampa {bank.Current(R3DeployableService.RampPool)}/{bank.Maximum(R3DeployableService.RampPool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.WallDeployer))charges+=$" • Murek {bank.Current(R3DeployableService.WallPool)}/{bank.Maximum(R3DeployableService.WallPool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.JumpPadDeployer))charges+=$" • Skok {bank.Current(R3DeployableService.JumpPadPool)}/{bank.Maximum(R3DeployableService.JumpPadPool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.BlastTrapDeployer))charges+=$" • Mina {bank.Current(R3DeployableService.BlastTrapPool)}/{bank.Maximum(R3DeployableService.BlastTrapPool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.EmpTrapDeployer))charges+=$" • Mina EMP {bank.Current(R3DeployableService.EmpTrapPool)}/{bank.Maximum(R3DeployableService.EmpTrapPool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.RepelTrapDeployer))charges+=$" • Mina odrz. {bank.Current(R3DeployableService.RepelTrapPool)}/{bank.Maximum(R3DeployableService.RepelTrapPool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.DamageTrapDeployer))charges+=$" • Pole {bank.Current(R3DeployableService.DamageTrapPool)}/{bank.Maximum(R3DeployableService.DamageTrapPool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.SlowTrapDeployer))charges+=$" • Spowoln. {bank.Current(R3DeployableService.SlowTrapPool)}/{bank.Maximum(R3DeployableService.SlowTrapPool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.ImmobilizeTrapDeployer))charges+=$" • Blokada {bank.Current(R3DeployableService.ImmobilizeTrapPool)}/{bank.Maximum(R3DeployableService.ImmobilizeTrapPool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.FlyingDroneGenerator))charges+=$" • Dron {bank.Current(R3DeployableService.FlyingDronePool)}/{bank.Maximum(R3DeployableService.FlyingDronePool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.GroundDroneGenerator))charges+=$" • Dron kołowy {bank.Current(R3DeployableService.GroundDronePool)}/{bank.Maximum(R3DeployableService.GroundDronePool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.OffensiveTurretGenerator))charges+=$" • Wieżyczka {bank.Current(R3DeployableService.OffensiveTurretPool)}/{bank.Maximum(R3DeployableService.OffensiveTurretPool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.HealingTurretGenerator))charges+=$" • Naprawa {bank.Current(R3DeployableService.HealingTurretPool)}/{bank.Maximum(R3DeployableService.HealingTurretPool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.SupportTurretGenerator))charges+=$" • Wsparcie {bank.Current(R3DeployableService.SupportTurretPool)}/{bank.Maximum(R3DeployableService.SupportTurretPool)}";
                    if(run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.ShieldTurretGenerator))charges+=$" • Kopuła {bank.Current(R3DeployableService.ShieldTurretPool)}/{bank.Maximum(R3DeployableService.ShieldTurretPool)}";
                }
                GUI.Label(new Rect(34,151,580,22),$"Energia: {energy.Current:0}/{energy.Maximum:0}{charges}",small);
            }
            var range=run.GetComponent<R6TestRange>();
            if(range?.TrainingTarget!=null)
            {
                Panel(new Rect(925,18,335,150));GUI.Label(new Rect(940,25,305,30),"CEL ELEKTRONICZNY",heading);
                GUI.Label(new Rect(940,62,305,38),range.TrainingFireRequested?"Ostrzał WŁĄCZONY":"Ostrzał WYŁĄCZONY",body);
                GUI.Label(new Rect(940,106,305,55),range.TrainingTarget.IsDefeated?"Cel zniszczony.":"Robot na lewo przed startem.\nEsc — sterowanie ostrzałem celu.",small);
            }
            DrawAimReticle(run);
            Panel(new Rect(18,625,1244,78));
            DrawFeedback();
            GUI.enabled=run.EmergencyRescueCooldownLeft<=0;
            if(GUI.Button(new Rect(1080,635,165,45),run.EmergencyRescueCooldownLeft>0?$"Ratunek {Mathf.CeilToInt(run.EmergencyRescueCooldownLeft)}s":"Ratunek",compactButton))run.TryEmergencyRescue();
            GUI.enabled=true;
            if(run.EmergencyRescueNotice!=null||run.EmergencyRescueActive)GUI.Label(new Rect(665,602,570,25),
                run.EmergencyRescueNotice??"Ratunek: niewrażliwość; atak wstrzymany.",small);
            GUI.Label(new Rect(34,635,1200,60),RangeControlsHint()+
                (run.CreativeWorkshop?" i przeciwnicy":"")+
                "\nSkoki x≈−12 • Kotwa x≈−25, z≈7 • Slalom pośrodku • Zawieszenie: porównaj OFF/Assist/Full na torze x≈12 • Cele na północy",small);
            TryRangeMouseReset(run);
        }
        static void TryRangeMouseReset(R1RunController run)
        {
            var input=Event.current;
            if(input!=null&&input.type==EventType.MouseDown&&R1ControlProfile.Current.MatchesEvent("ResetRange",input)&&run.ResetTestRange())input.Use();
        }
        private static Rect AimStatusPanelRect(float textHeight)
        {
            if(float.IsNaN(textHeight)||float.IsInfinity(textHeight))textHeight=0;
            return new Rect(690,18,225,Mathf.Clamp(textHeight+14,42,65));
        }
        private static bool AimStatusNeedsAttention(int guns,bool hasSurface,int clear)=>
            guns<=0||(hasSurface&&clear<guns);

        void DrawAimReticle(R1RunController run)
        {
            var feedback=run.Player.Controller.AimFeedback;
            DrawWeaponReticles(run.Player.Controller);
            if(aimCrossStyle==null)aimCrossStyle=new GUIStyle(body){fontSize=20,fontStyle=FontStyle.Normal,alignment=TextAnchor.MiddleCenter};
            if(aimCaptionStyle==null)aimCaptionStyle=new GUIStyle(small){alignment=TextAnchor.UpperLeft};
            aimCrossStyle.normal.textColor=feedback.Clear>0?new Color(.2f,.85f,.85f):new Color(1,.72f,.3f);
            GUI.Label(new Rect(624,344,32,32),"+",aimCrossStyle);
            bool warning=AimStatusNeedsAttention(feedback.Guns,feedback.HasSurface,feedback.Clear);
            string caption=feedback.Caption;
            var area=AimStatusPanelRect(aimCaptionStyle.CalcHeight(new GUIContent(caption),201));
            CombatPanel(area,warning?new Color(1f,.68f,.28f):new Color(.28f,.76f,.75f));
            aimCaptionStyle.normal.textColor=warning?new Color(1f,.82f,.55f):new Color(.91f,.94f,.97f);
            GUI.Label(new Rect(area.x+12,area.y+7,201,area.height-14),caption,aimCaptionStyle);
        }
        void DrawWeaponReticles(RogueMachine.P12PipelineSafe.P12PipelineSafeTankController controller)
        {
            if(Event.current.type!=EventType.Repaint||Camera.main==null)return;
            if(weaponMarkerStyle==null)weaponMarkerStyle=new GUIStyle(small){fontSize=12,alignment=TextAnchor.MiddleCenter};
            var camera=Camera.main;
            var original=GUI.color;var markers=controller.WeaponReticles;
            for(int i=0;i<markers.Count;i++)
            {
                var marker=markers[i];var point=RoboRougelike.WeaponIntegration.RRLWeaponReticles.MarkerHudPoint(marker.Viewport,camera.pixelRect,new Vector2(Screen.width,Screen.height),marker.Offscreen);
                float x=point.x,y=point.y;

                float radius=8+Mathf.Min(i,3)*4,stroke=1,length=5;
                GUI.color=marker.Disabled?new Color(.55f,.55f,.55f,.9f):
                    marker.Obstructed||marker.OutOfRange||marker.Offscreen?new Color(1,.7f,.25f,.95f):new Color(.35f,.95f,.95f,.9f);
                for(int xi=0;xi<2;xi++)for(int yi=0;yi<2;yi++)
                {
                    int sx=xi==0?-1:1,sy=yi==0?-1:1;
                    float cx=x+sx*radius,cy=y+sy*radius;
                    GUI.DrawTexture(new Rect(cx-(sx<0?0:length),cy-stroke*.5f,length,stroke),Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(cx-stroke*.5f,cy-(sy<0?0:length),stroke,length),Texture2D.whiteTexture);
                }
                var offset=RoboRougelike.WeaponIntegration.RRLWeaponReticles.LabelOffset(i);
                var label=RoboRougelike.WeaponIntegration.RRLWeaponReticles.ClampHudToViewport(new Vector2(x,y)+offset,camera.pixelRect,new Vector2(Screen.width,Screen.height),10);
                GUI.Label(new Rect(label.x-10,label.y-10,20,20),(i+1).ToString(),weaponMarkerStyle);
            }
            GUI.color=original;
        }
        private const int DefensePageSize=9;
        private int defenseStatusPage;
        private static int DefensePageCount(int count)=>Mathf.Max(1,(Mathf.Max(0,count)+DefensePageSize-1)/DefensePageSize);
        private static int DefensePageStart(int count,int page)=>Mathf.Clamp(page,0,DefensePageCount(count)-1)*DefensePageSize;
        private void UpdateDefensePaging(Event evt,int count)
        {
            int pages=DefensePageCount(count);defenseStatusPage=Mathf.Clamp(defenseStatusPage,0,pages-1);
            if(pages>1&&evt!=null&&evt.type==EventType.KeyDown&&evt.keyCode==KeyCode.P)
            {defenseStatusPage=(defenseStatusPage+1)%pages;evt.Use();}
        }
        private static string[] DefensePageRows(RoboRougelike.WeaponIntegration.RRLShieldBinding[] shields,
            RoboRougelike.WeaponIntegration.RRLPointDefenseBinding[] defenses,int page)
        {
            int start=DefensePageStart(shields.Length+defenses.Length,page),index=0;
            var rows=new System.Collections.Generic.List<string>();
            foreach(var shield in shields)
            {
                if(index>=start&&rows.Count<DefensePageSize)
                    rows.Add($"DEF-001 • {shield.Generator.Charge:0}/100 • "+(shield.Generator.Operational?"ON":"OFF"));
                index++;
            }
            foreach(var defense in defenses)
            {
                if(index>=start&&rows.Count<DefensePageSize)
                    rows.Add($"DEF-002 • {defense.State.Ammo}/40 • "+(defense.Controller.IsOperational?"ON":"OFF")+" • "+ManualReloadAction(defense.State.Ammo,40,defense.State.ReloadReadyAt,Time.time,defense.Device.Available));
                index++;
            }
            return rows.ToArray();
        }
        private static Rect DefensePanelRect(int count) => new Rect(18,206,610,44+25*Mathf.Clamp(count,0,DefensePageSize));
        private static Rect DefenseTitleRect() => new Rect(33,210,580,30);
        private static Rect DefenseRowRect(int index) => new Rect(33,246+index*25,580,25);
        private static Rect DamageNoticePanelRect(int index) => new Rect(925,516+index*36,335,35);
        private static Rect CombatWeaponPanelRect(int count)=>new Rect(925,190,335,94+26*Mathf.Clamp(count,0,2));
        void DrawFeedback()
        {
            var run=R1RunController.Instance;
            if(run!=null)DrawMovementTelemetry(run);
            if(run!=null)DrawSuspensionTelemetry(run);
            if(run?.Player!=null)
            {
                var assembly=run.Player.GetComponent<RoboRougelike.R3.R3VehicleAssembly>();
                var slots=run.VehicleModel.GetWeaponSlots();var a=slots[0];var b=slots[1];
                var counts=new int[4];int totalGuns=0;
                foreach(var part in assembly.Parts)
                    if(part!=null&&part.Kind==RoboRougelike.R3.R3PartKind.Gun&&part.Operational&&part.transform.IsChildOf(assembly.transform))
                    {totalGuns++;for(int i=0;i<4;i++)if(R3VehicleModel.MatchesWeaponSlot(part.Weapon,i,slots,run.CreativeWorkshop))counts[i]++;}
                int extraRows=slots[2].HasValue||slots[3].HasValue?48:0;
                var weaponPanel=CombatWeaponPanelRect(totalGuns);weaponPanel.height+=extraRows;
                CombatPanel(weaponPanel,new Color(.28f,.76f,.75f));
                GUI.Label(new Rect(940,195,305,30),"UZBROJENIE",heading);
                int pages=WeaponStatusPageCount(totalGuns);
                weaponStatusPage=Mathf.Clamp(weaponStatusPage,0,pages-1);
                if(pages>1)
                {
                    if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Tab)
                    {AdvanceWeaponStatusPage(totalGuns);Event.current.Use();}
                    GUI.Label(new Rect(1112,199,130,25),$"Tab: {weaponStatusPage+1}/{pages}",small);
                }
                GUI.Label(new Rect(940,228,305,24),$"A / LPM: {WeaponGroupName(a)} ({counts[0]})",small);
                string extra=run.CreativeWorkshop&&run.VehicleModel.Parts.Where(p=>p.Kind==R3PartKind.Gun).Select(p=>p.Weapon).Distinct().Count()>4?" + inne":"";
                GUI.Label(new Rect(940,252,305,24),$"B / PPM: {WeaponGroupName(b)}{extra} ({counts[1]})",small);
                if(extraRows>0)
                {
                    GUI.Label(new Rect(940,276,305,24),$"C / Q: {WeaponGroupName(slots[2])} ({counts[2]})",small);
                    GUI.Label(new Rect(940,300,305,24),$"D / E: {WeaponGroupName(slots[3])} ({counts[3]})",small);
                }
                int row=0,index=0,start=WeaponStatusPageStart(totalGuns,weaponStatusPage);
                foreach(var gun in assembly.Parts)
                {
                    if(gun==null||gun.Kind!=RoboRougelike.R3.R3PartKind.Gun||!gun.Operational||!gun.transform.IsChildOf(run.Player.transform))continue;
                    if(index++<start)continue;
                    if(row>=2)break;
                    float reload=Mathf.Max(0,gun.NextFireTime-Time.time);
                    var shotgun=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
                    var rail=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLRailgunBinding>();
                    var beam=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLBeamBinding>();
                    var rocket=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLRocketBinding>();
                    var saw=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLSawBinding>();
                    string status=saw!=null?($"{saw.Module.Motor.Rpm:0} RPM • {saw.Module.Motor.Heat:0}%"+(saw.Module.Motor.JamRemaining>0?" • blokada":"")):rocket!=null?RocketWeaponStatus(rocket.State.Ammo,rocket.State.ReloadReadyAt,rocket.State.DeviceReadyAt,Time.time):beam!=null?($"temperatura {beam.Module.HeatFraction*100:0}%"+(beam.Module.IsOverheated?" • przegrzana":"")):rail!=null?ManualReloadWeaponStatus(rail.State.Ammo,RoboRougelike.WeaponIntegration.RRLRailgunBinding.Magazine,rail.State.ReloadReadyAt,Time.time,rail.Device.Available):shotgun!=null?ManualReloadWeaponStatus(shotgun.State.Ammo,RoboRougelike.WeaponIntegration.RRLShotgunBinding.MagazineSalvos,shotgun.State.ReloadReadyAt,Time.time,shotgun.Available):
                        (reload>0?$"{reload:0.0} s":"gotowa");
                    var aura=gun.GetComponent<RoboRougelike.WeaponIntegration.RRLDamageAuraBinding>();
                    if(aura!=null)status=aura.IsActive?"aura włączona":"aura wyłączona";
                    var disrupted=gun.GetComponent<RoboRougelike.Mechanics.EMP.EmpReceiver>();
                    if(disrupted!=null&&disrupted.IsDisrupted)status=$"EMP {disrupted.RemainingSeconds:0.0}s";
                    GUI.Label(new Rect(940,278+extraRows+row*26,305,26),(start+row+1)+". "+RoboRougelike.R4.R4WeaponDefinition.Get(gun.Weapon).ShortName+" • "+status,small);row++;
                }
                var defenses=run.Player.GetComponentsInChildren<RoboRougelike.WeaponIntegration.RRLPointDefenseBinding>();
                var shields=run.Player.GetComponentsInChildren<RoboRougelike.WeaponIntegration.RRLShieldBinding>();
                int defenseCount=defenses.Length+shields.Length;
                UpdateDefensePaging(Event.current,defenseCount);
                if(defenseCount>0)
                {
                    var defensePanel=DefensePanelRect(defenseCount);
                    Panel(defensePanel);GUI.Label(DefenseTitleRect(),"OBRONA",heading);
                    if(DefensePageCount(defenseCount)>1)
                        GUI.Label(new Rect(435,211,175,25),$"P: zmień {defenseStatusPage+1}/{DefensePageCount(defenseCount)}",small);
                    var rows=DefensePageRows(shields,defenses,defenseStatusPage);
                    for(int defenseRow=0;defenseRow<rows.Length;defenseRow++)GUI.Label(DefenseRowRect(defenseRow),rows[defenseRow],small);
                }
            }
            var feedback=Object.FindFirstObjectByType<R6CombatFeedback>();if(feedback==null)return;
            var lines=feedback.Notices();
            for(int i=0;i<lines.Length;i++)
            {var notice=DamageNoticePanelRect(i);Panel(notice);GUI.Label(new Rect(notice.x+15,notice.y+1,305,33),lines[i],small);}
        }
    }
}
