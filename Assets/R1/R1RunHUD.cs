using UnityEngine;
using System.Linq;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD : MonoBehaviour
    {
        private GUIStyle title, heading, body, small, button, compactButton;
        private static int CombatAbilityExtraHeight(int count)=>Mathf.Max(0,count-1)*22;
        private static Rect CombatAbilityStatusRect(int row)=>new Rect(340,90+22*row,190,22);
        private Texture2D buttonNormal,buttonHover,buttonPressed;
        private bool combatDetailsOpen;
        static string GameplayControlsHint()
        {
            var profile=R1ControlProfile.Current;
            string Names(params string[] actions)=>string.Join("/",actions.Select(a=>R1ControlProfile.Display(profile.Binding(a))));
            return Names("MoveForward","MoveBackward","TurnLeft","TurnRight")+" — ruch • Mysz — celowanie • "+
                Names("Weapon1","Weapon2","Weapon3","Weapon4")+" — broń • "+Names("Active1","Active2","Active3","Active4")+" — moduły • Esc — pauza";
        }
        private int[] starterOffers;
        private R3VehicleDesign[] starterDesigns;
        private string[] starterDescriptions,starterSummaries;
        private readonly System.Random starterOfferRandom=new System.Random();
        private void Update()=>RefreshStarterOffers(R1RunController.Instance);
        private void RefreshStarterOffers(R1RunController run)
        {
            if(run==null||run.Progress==null)return;
            if(run.Progress.State!=R1RunState.Ready)
            {starterOffers=null;starterDesigns=null;starterDescriptions=null;starterSummaries=null;}
            else if(!run.MapEditing&&starterOffers==null)
            {
                starterOffers=R3VehicleCatalog.DrawOffers(starterOfferRandom);
                starterDesigns=starterOffers.Select(offer=>R3VehicleCatalog.Create((R3VehicleKind)(offer/4),offer%4)).ToArray();
                starterDescriptions=starterDesigns.Select(StarterVehicleDescription).ToArray();
                starterSummaries=starterDesigns.Select(StarterVehicleSummary).ToArray();
            }
            if(run.Progress.State==R1RunState.Ready&&!run.MapEditing&&starterOffers!=null&&starterOffers.Length>0&&
               !starterOffers.Any(offer=>(R3VehicleKind)(offer/4)==run.SelectedVehicle&&offer%4==run.SelectedVehicleVariant))
                run.SelectVehicle((R3VehicleKind)(starterOffers[0]/4),starterOffers[0]%4);
        }

        private string StarterActionCaption(R1RunController run)
        {
            int selected=(int)run.SelectedVehicle*4+run.SelectedVehicleVariant;
            int index=starterOffers==null?-1:System.Array.IndexOf(starterOffers,selected);
            string name=index>=0&&starterDesigns!=null&&index<starterDesigns.Length?starterDesigns[index]?.Name:null;
            return string.IsNullOrEmpty(name)?"Przygotuj pojazd  [Enter]":$"Przygotuj: {name}  [Enter]";
        }

        private readonly Color panel = new Color(0.04f, 0.06f, 0.08f, 0.94f);
        private static bool UsePortraitReady(int width,int height,bool ready,bool overlay)
            => width>0&&height>width&&ready&&!overlay;
        private static bool UsePortraitGarage(int width,int height,bool garage,bool mapEditing,bool overlay)
            =>width>0&&height>width&&garage&&!mapEditing&&!overlay;
        private static Rect PortraitVehicleCard(int index)=>new Rect(24,150+index*180,720,166);
        private static Rect PortraitActionRect(int index)=>new Rect(24+(index%2)*370,index<2?790:855,350,index<2?54:48);
        private static Rect ReadyIntroRect()=>new Rect(90,490,880,22);
        private static Rect ReadySaveMessageRect()=>new Rect(90,515,880,30);
        private static Rect ReadySeedWarningRect()=>new Rect(990,505,200,42);
        private static string StarterVehicleSummary(R3VehicleDesign design)
        {
            int wheels=design.Parts.Count(p=>p.Kind==R3PartKind.Wheel);
            int weapons=design.Parts.Count(p=>p.Kind==R3PartKind.Gun);
            float energy=new R3VehicleModel(design).Energy.Maximum;
            float speed=R3VehicleAssembly.SpeedFor(design.Parts.Sum(R4WeaponDefinition.PartMass),wheels);
            return $"Koła: {wheels} • Broń: {weapons} • Energia: {energy:0}\nLimit napędu: {speed:0.0} m/s • Rdzeń: {design.CoreHp:0} HP";
        }
        private static string StarterVehicleDescription(R3VehicleDesign design)
        {
            var weapons=design.Parts.Where(p=>p.Kind==R3PartKind.Gun)
                .Select(p=>R4WeaponDefinition.Get(p.Weapon).ShortName).Distinct();
            string names=string.Join(" / ",weapons);
            return design.Description+"\n"+(names.Length>0?names:"Bez broni");
        }
        private void DismissCatalog(){catalogOpen=false;catalogSearch=string.Empty;dismantleConfirmation=null;catalogFeedback=null;SyncQuickCategoryToCatalog();}
        internal bool ClosePartCatalog()
        {
            if(catalogOpen&&dismantleConfirmation!=null){CancelCatalogDismantle();return true;}
            if(catalogOpen){DismissCatalog();return true;}
            if(garageOptionsOpen){garageOptionsOpen=false;return true;}
            return false;
        }
        private void Setup()
        {
            if (title != null) return;
            body = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            body.normal.textColor = new Color(0.91f, 0.94f, 0.97f);
            small = new GUIStyle(body) { fontSize = 15 };
            title = new GUIStyle(body) { fontSize = 36, fontStyle = FontStyle.Bold };
            heading = new GUIStyle(body) { fontSize = 24, fontStyle = FontStyle.Bold };
            heading.normal.textColor = new Color(0.45f, 0.88f, 0.88f);
            button = new GUIStyle(GUI.skin.button) { fontSize = 20, wordWrap = true };
            compactButton = new GUIStyle(GUI.skin.button) { fontSize = 15, wordWrap = true };
            buttonNormal=ButtonTile(new Color(.075f,.12f,.145f),new Color(.22f,.39f,.43f));
            buttonHover=ButtonTile(new Color(.11f,.23f,.26f),new Color(.36f,.77f,.78f));
            buttonPressed=ButtonTile(new Color(.08f,.29f,.30f),new Color(.50f,.93f,.90f));
            StyleButton(button);StyleButton(compactButton);
        }
        private static Texture2D ButtonTile(Color fill,Color edge)
        {
            var texture=new Texture2D(3,3,TextureFormat.RGBA32,false){name="RRL HUD button",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Point};
            var pixels=new Color[9];for(int y=0;y<3;y++)for(int x=0;x<3;x++)pixels[y*3+x]=(x==0||x==2||y==0||y==2)?edge:fill;
            texture.SetPixels(pixels);texture.Apply(false,true);return texture;
        }
        private void StyleButton(GUIStyle style)
        {
            style.border=new RectOffset(1,1,1,1);
            style.normal.background=buttonNormal;style.hover.background=buttonHover;style.active.background=buttonPressed;
            style.focused.background=buttonHover;style.onNormal.background=buttonPressed;style.onHover.background=buttonHover;style.onActive.background=buttonPressed;
            var text=new Color(.91f,.96f,.97f);style.normal.textColor=text;style.hover.textColor=Color.white;
            style.active.textColor=Color.white;style.focused.textColor=Color.white;style.onNormal.textColor=text;style.onHover.textColor=Color.white;style.onActive.textColor=Color.white;
        }
        private void OnDestroy()
        {
            if(buttonNormal!=null)Destroy(buttonNormal);
            if(buttonHover!=null)Destroy(buttonHover);
            if(buttonPressed!=null)Destroy(buttonPressed);
        }
        private void UpdateCombatDetails(Event input)
        {
            if(input!=null&&input.type==EventType.KeyDown&&input.keyCode==KeyCode.I)
            {combatDetailsOpen=!combatDetailsOpen;input.Use();}
        }
        private static float BarFraction(float current,float maximum)
        {
            if(float.IsNaN(current)||float.IsInfinity(current)||float.IsNaN(maximum)||float.IsInfinity(maximum)||maximum<=0)return 0;
            return Mathf.Clamp01(current/maximum);
        }
        private static bool CombatResourceLow(float current,float maximum,float threshold)=>
            !float.IsNaN(current)&&!float.IsInfinity(current)&&!float.IsNaN(maximum)&&!float.IsInfinity(maximum)&&
            maximum>0&&current<=maximum*threshold;
        private static string CombatResourceWarning(float hp,float maxHp,float energy,float maxEnergy)
        {
            if(CombatResourceLow(hp,maxHp,.25f))return "Niski stan rdzenia — unikaj trafień.";
            if(CombatResourceLow(energy,maxEnergy,.2f))return "Niska energia — postój przyspiesza jej regenerację.";
            return null;
        }
        private static string CombatEquipmentWarning(R3DriveKind drive,int initialWheels,int survivingWheels,bool weaponsInactive)
        {
            if(drive==R3DriveKind.Wheels&&initialWheels>1&&survivingWheels==1)
                return "Ostatnie koło — jego utrata oznacza porażkę.";
            if(weaponsInactive)return "Uzbrojenie nieaktywne — sprawdź stan broni.";
            return null;
        }
        private static Rect EnemyStatusLabelRect(int row)=>new Rect(940,80+row*24,305,18);
        private static Rect EnemyStatusBarRect(int row)=>new Rect(940,99+row*24,305,4);
        private static Rect CombatEnemyPanelRect(int visibleRows,bool special,bool defeated)
        {
            int rows=Mathf.Clamp(visibleRows,0,4);
            float height=special?160:rows>0?64+24*rows:defeated?112:62;
            return new Rect(925,18,335,height);
        }
        private static string EnemyCoreStatus(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret enemy)
        {
            if(enemy==null||enemy.IsDefeated||enemy.Core==null)return null;
            return $"{enemy.name} • rdzeń {enemy.Core.CurrentHp:0}/{enemy.Core.MaxHp:0} HP";
        }
        private static string RunEndOutcome(bool won,string reason)=>won?"Wyprawa ukończona.":
            string.IsNullOrWhiteSpace(reason)?"Pojazd został zniszczony.":reason;
        private static string RunEndRecovery(int recovered)=>$"Odzyskane części: {recovered}";
        private static string RunEndNewRunNote()=>
            "W menu wybierzesz jedną z nowych wylosowanych konstrukcji. Nowa wyprawa ma własny budżet startowy; łupy i złom z tej próby nie przechodzą.";
        private static string SeedInputWarning(string text,int lastValid)=>
            int.TryParse(text,out _)?null:$"Błędny seed\nUżyty: {lastValid}";
        private void DrawSeedWarning(Rect area,int lastValid)
        {
            string warning=SeedInputWarning(seedText,lastValid);if(warning==null)return;
            var before=GUI.color;GUI.color=new Color(1f,.72f,.35f);GUI.Label(area,warning,small);GUI.color=before;
        }
        private static void DrawResourceBar(Rect rect,float current,float maximum,Color color)
        {
            var before=GUI.color;GUI.color=new Color(.11f,.17f,.19f,1);GUI.DrawTexture(rect,Texture2D.whiteTexture);
            var inner=new Rect(rect.x+1,rect.y+1,Mathf.Max(0,rect.width-2)*BarFraction(current,maximum),Mathf.Max(0,rect.height-2));
            GUI.color=color;GUI.DrawTexture(inner,Texture2D.whiteTexture);GUI.color=before;
        }
        private void Panel(Rect rect)
        {
            var before = GUI.color; GUI.color = panel; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = before;
        }
        private void CombatPanel(Rect rect,Color accent)
        {
            Panel(rect);
            var before=GUI.color;GUI.color=accent;
            GUI.DrawTexture(new Rect(rect.x,rect.y,3,rect.height),Texture2D.whiteTexture);GUI.color=before;
        }
        private void OnGUI()
        {
            var run = R1RunController.Instance;
            if (run == null) return;
            RefreshStarterOffers(run);
            Setup();
            bool sessionOverlay = run.Paused || run.QuitRequested;
            if(quickSearchReleasePending)
            {GUI.FocusControl(null);GUIUtility.keyboardControl=0;quickSearchReleasePending=false;}
            string focused=GUI.GetNameOfFocusedControl();
            bool garageSearchKey=GarageSearchConsumesKey(Event.current.type,focused);
            bool quickSearchActive=run.Progress?.State==R1RunState.Garage&&!run.MapEditing&&!run.Transitioning&&
                !sessionOverlay&&!catalogOpen&&!garageOptionsOpen&&run.Error==null;
            if(GarageSearchExitKey(Event.current.type,Event.current.keyCode,focused,quickSearchActive))
            {
                if(Event.current.keyCode==KeyCode.Escape){quickSearchEditing=true;run.HandleEscape();}
                GUI.FocusControl(null);GUIUtility.keyboardControl=0;quickSearchEditing=false;quickSearchReleasePending=false;Event.current.Use();
            }
            else if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape)
            {run.HandleEscape();Event.current.Use();}
            var rawPointer=Event.current.mousePosition;
            var previous = GUI.matrix;
            bool portraitReady=UsePortraitReady(Screen.width,Screen.height,run.Progress?.State==R1RunState.Ready&&!run.MapEditing,
                sessionOverlay||run.Error!=null||run.Transitioning);
            bool portraitGarage=UsePortraitGarage(Screen.width,Screen.height,run.Progress?.State==R1RunState.Garage,
                run.MapEditing,run.Transitioning||sessionOverlay||run.Error!=null);
            bool portraitRoute=UsePortraitRoute(Screen.width,Screen.height,run.Progress?.State==R1RunState.Route,
                run.Transitioning||sessionOverlay||run.Error!=null||run.MapEditing);
            bool portraitSalvage=UsePortraitSalvage(Screen.width,Screen.height,run.Progress?.State==R1RunState.Salvage,
                run.Transitioning||sessionOverlay||run.Error!=null||run.MapEditing);
            float canvasWidth=portraitReady||portraitGarage||portraitRoute||portraitSalvage?768:1280,canvasHeight=portraitReady||portraitGarage||portraitRoute||portraitSalvage?1024:720;
            float scale = Mathf.Min(Screen.width/canvasWidth,Screen.height/canvasHeight);
            float offsetX=(Screen.width-canvasWidth*scale)/2,offsetY=(Screen.height-canvasHeight*scale)/2;
            if((run.Progress?.State==R1RunState.Garage||run.Progress?.State==R1RunState.Route||run.Progress?.State==R1RunState.Salvage)&&!run.Transitioning)
            {
                // Cover letterbox margins so the scene cannot show through outside
                // the garage, route or rewards interface at non-matching aspect ratios.
                GUI.matrix=Matrix4x4.identity;
                var color=GUI.color;GUI.color=new Color(.04f,.06f,.08f,1);
                if(offsetX>0){GUI.DrawTexture(new Rect(0,0,offsetX,Screen.height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(Screen.width-offsetX,0,offsetX,Screen.height),Texture2D.whiteTexture);}
                if(offsetY>0){GUI.DrawTexture(new Rect(0,0,Screen.width,offsetY),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,Screen.height-offsetY,Screen.width,offsetY),Texture2D.whiteTexture);}
                GUI.color=color;
            }
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX,offsetY), Quaternion.identity, Vector3.one * scale);
            try
            {
                Draw(run,portraitReady,portraitGarage,portraitRoute,portraitSalvage);
                if(run.Paused)LightStylePrototype.Instance?.DrawPausePanel();
                if(quickSearchActive&&!garageSearchKey&&!catalogOpen&&!garageOptionsOpen)
                    HandleQuickGarageKey(run,Event.current,false);
                quickSearchEditing=quickSearchActive&&!run.Paused&&!catalogOpen&&!garageOptionsOpen&&
                    GUI.GetNameOfFocusedControl()==QuickSearchControlName;
                if(!sessionOverlay&&run.MapEditing){if(!mapBrowserOpen)run.MapEditorPointer(Event.current,rawPointer);}
                else if(!garageSearchKey&&!sessionOverlay&&!catalogOpen&&!garageOptionsOpen&&run.Progress?.State==R1RunState.Garage&&!run.Transitioning&&!run.Paused&&!run.QuitRequested)run.Garage.HandleGuiPointer(Event.current,rawPointer);
                else run.Garage?.CancelPointerGesture();
            }
            finally { GUI.matrix = previous; GUI.enabled = true; }
        }
        private void Draw(R1RunController run,bool portraitReady,bool portraitGarage,bool portraitRoute,bool portraitSalvage)
        {
            if (run.QuitRequested)
            { Panel(new Rect(0,0,1280,720)); GUI.Label(new Rect(350,300,650,80),"Zamykanie gry…",title); return; }
            if(run.Paused&&PauseControlsOpen){DrawPauseControls();return;}
            if (run.Paused && run.Progress?.State != R1RunState.TestDrive)
            { DrawSessionPause(run); return; }
            if (run.Error != null)
            {
                Panel(new Rect(240, 190, 800, 340));
                GUI.Label(new Rect(280, 230, 720, 60), "Nie udało się rozpocząć wyprawy", heading);
                GUI.Label(new Rect(280, 310, 720, 100), run.MapQuitPending?run.EditorMessage:run.Error, body);
                if(GUI.Button(new Rect(280,440,720,55),run.MapQuitPending?"Potwierdź wyjście":"Wyjdź z gry",button))run.RequestQuitFromMenu(); return;
            }
            if (run.Progress == null) return;
            var progress = run.Progress;
            if(run.MapEditing&&!run.Paused){DrawMapEditor(run);return;}
            if(progress.State==R1RunState.TestDrive){DrawTestRange(run);return;}
            if(progress.State==R1RunState.Garage&&!run.Transitioning){if(catalogOpen){if(portraitGarage)DrawPortraitCatalog(run);else DrawFullCatalog(run);}else if(portraitGarage)DrawSimplePortraitGarage(run);else DrawSimpleGarage(run);return;}
            DismissCatalog();garageOptionsOpen=false;
            if (progress.State == R1RunState.Combat && !run.Transitioning)
            {
                UpdateCombatDetails(Event.current);
                var player = run.Player;
                var core=player!=null?player.Core:null;
                float hp=core!=null?core.CurrentHp:0,maxHp=core!=null?core.MaxHp:0;
                var energy=player!=null?player.GetComponent<VehicleEnergyBank>():null;
                float currentEnergy=energy!=null?energy.Current:0,maxEnergy=energy!=null?energy.Maximum:0;
                bool lowCore=CombatResourceLow(hp,maxHp,.25f),lowEnergy=CombatResourceLow(currentEnergy,maxEnergy,.2f);
                string resourceWarning=CombatResourceWarning(hp,maxHp,currentEnergy,maxEnergy);
                var modules=player!=null?player.Modules:null;
                var repairPulse=player!=null?player.GetComponent<R3RepairPulse>():null;
                var offensive=player!=null?player.GetComponent<R3OffensiveDevices>():null;
                bool hasOverdrive=offensive!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.Overdrive);
                bool hasRapidFire=offensive!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.RapidFire);
                bool hasDoubleBoost=offensive!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.DoubleShotOverdrive);
                var emp=player!=null?player.GetComponent<R3EmpDevices>():null;
                bool hasTargetEmp=emp!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.TargetedEmp);
                bool hasAreaEmp=emp!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.AreaEmp);
                var recharge=player!=null?player.GetComponent<R3RechargePulse>():null;
                var deploy=player!=null?player.GetComponent<R3DeployableService>():null;
                bool hasRamp=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.RampDeployer);
                bool hasWall=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.WallDeployer);
                bool hasJumpPad=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.JumpPadDeployer);
                bool hasBlastTrap=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.BlastTrapDeployer);
                bool hasEmpTrap=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.EmpTrapDeployer);
                bool hasRepelTrap=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.RepelTrapDeployer);
                bool hasDamageTrap=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.DamageTrapDeployer);
                bool hasSlowTrap=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.SlowTrapDeployer);
                bool hasImmobilizeTrap=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.ImmobilizeTrapDeployer);
                bool hasFlyingDrone=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.FlyingDroneGenerator);
                bool hasGroundDrone=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.GroundDroneGenerator);
                bool hasOffensiveTurret=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.OffensiveTurretGenerator);
                bool hasHealingTurret=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.HealingTurretGenerator);
                bool hasSupportTurret=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.SupportTurretGenerator);
                bool hasShieldTurret=deploy!=null&&run.VehicleModel.Parts.Any(p=>p.Kind==R3PartKind.ShieldTurretGenerator);
                int extraStatusHeight=energy!=null?CombatAbilityExtraHeight((repairPulse!=null?1:0)+(hasOverdrive?1:0)+(hasRapidFire?1:0)+(hasDoubleBoost?1:0)+(hasTargetEmp?1:0)+(hasAreaEmp?1:0)+(recharge!=null?1:0)+(hasRamp?1:0)+(hasWall?1:0)+(hasJumpPad?1:0)+(hasBlastTrap?1:0)+(hasEmpTrap?1:0)+(hasRepelTrap?1:0)+(hasDamageTrap?1:0)+(hasSlowTrap?1:0)+(hasImmobilizeTrap?1:0)+(hasFlyingDrone?1:0)+(hasGroundDrone?1:0)+(hasOffensiveTurret?1:0)+(hasHealingTurret?1:0)+(hasSupportTurret?1:0)+(hasShieldTurret?1:0)):0;
                if(!lowCore&&modules!=null&&run.VehicleModel!=null)
                {
                    var drive=run.VehicleModel.SelectedDrive;
                    int surviving=drive==R3DriveKind.Wheels&&modules.InitialWheelCount>1&&modules.ActiveWheelCount<=1?
                        modules.SurvivingWheelCount:-1;
                    bool weaponsInactive=modules.ActiveWeaponCount==0&&modules.HasSurvivingWeapon;
                    resourceWarning=CombatEquipmentWarning(drive,modules.InitialWheelCount,surviving,weaponsInactive)??resourceWarning;
                }
                CombatPanel(new Rect(20,18,660,(combatDetailsOpen?180:125)+extraStatusHeight+(resourceWarning!=null?25:0)+(run.PickupGraceRemaining>0?25:0)),
                    lowCore?new Color(1f,.42f,.32f):new Color(.28f,.76f,.75f));
                GUI.Label(new Rect(36, 27, 630, 30), $"STARCIE {progress.EncounterIndex + 1}/{progress.TotalEncounters}   •   Wrogowie: {run.Encounter.AliveCount}   •   Złom: {run.VehicleModel.Scrap}", body);
                if (player != null)
                {
                    var beforeText=GUI.contentColor;
                    if(lowCore)GUI.contentColor=new Color(1f,.52f,.42f);
                    GUI.Label(new Rect(36,58,175,28),$"RDZEŃ{(lowCore?"!":":")} {hp:0}/{maxHp:0}",body);
                    GUI.contentColor=beforeText;
                    DrawResourceBar(new Rect(215,65,110,12),hp,maxHp,lowCore?new Color(1f,.35f,.25f):new Color(.35f,.85f,.67f));
                    GUI.Label(new Rect(340,58,325,28),ActivityGoal(run),small);
                    if(energy!=null)
                    {
                        if(lowEnergy)GUI.contentColor=new Color(1f,.78f,.38f);
                        GUI.Label(new Rect(36,90,175,25),$"Energia{(lowEnergy?"!":":")} {currentEnergy:0}/{maxEnergy:0}",small);
                        GUI.contentColor=beforeText;
                        DrawResourceBar(new Rect(215,95,110,12),currentEnergy,maxEnergy,lowEnergy?new Color(1f,.68f,.24f):new Color(.25f,.65f,.95f));
                        int statusRow=0;
                        if(repairPulse!=null)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.ActiveSlotLabel(R3PartKind.RepairPulse)} naprawa: {repairPulse.Status}",small);
                        if(hasOverdrive)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.ActiveSlotLabel(R3PartKind.Overdrive)} Overdrive: {offensive.OverdriveStatus}",small);
                        if(hasRapidFire)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.ActiveSlotLabel(R3PartKind.RapidFire)} Rapid: {offensive.RapidFireStatus}",small);
                        if(hasDoubleBoost)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.ActiveSlotLabel(R3PartKind.DoubleShotOverdrive)} Double: {offensive.DoubleShotOverdriveStatus}",small);
                        if(hasTargetEmp)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.ActiveSlotLabel(R3PartKind.TargetedEmp)} EMP cel.: {emp.Status(R3PartKind.TargetedEmp)}",small);
                        if(hasAreaEmp)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.ActiveSlotLabel(R3PartKind.AreaEmp)} EMP obsz.: {emp.Status(R3PartKind.AreaEmp)}",small);
                        if(recharge!=null)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.ActiveSlotLabel(R3PartKind.RechargePulse)} ładunki: {recharge.Status}",small);
                        if(hasRamp)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.RampDeployer)} rampa: {deploy.StatusFor(R3PartKind.RampDeployer)}",small);
                        if(hasWall)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.WallDeployer)} murek: {deploy.StatusFor(R3PartKind.WallDeployer)}",small);
                        if(hasJumpPad)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.JumpPadDeployer)} jump pad: {deploy.StatusFor(R3PartKind.JumpPadDeployer)}",small);
                        if(hasBlastTrap)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.BlastTrapDeployer)} pułapka: {deploy.StatusFor(R3PartKind.BlastTrapDeployer)}",small);
                        if(hasEmpTrap)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.EmpTrapDeployer)} pułapka EMP: {deploy.StatusFor(R3PartKind.EmpTrapDeployer)}",small);
                        if(hasRepelTrap)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.RepelTrapDeployer)} odrzucanie: {deploy.StatusFor(R3PartKind.RepelTrapDeployer)}",small);
                        if(hasDamageTrap)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.DamageTrapDeployer)} pole obrażeń: {deploy.StatusFor(R3PartKind.DamageTrapDeployer)}",small);
                        if(hasSlowTrap)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.SlowTrapDeployer)} spowolnienie: {deploy.StatusFor(R3PartKind.SlowTrapDeployer)}",small);
                        if(hasImmobilizeTrap)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.ImmobilizeTrapDeployer)} blokada napędu: {deploy.StatusFor(R3PartKind.ImmobilizeTrapDeployer)}",small);
                        if(hasFlyingDrone)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.FlyingDroneGenerator)} dron: {deploy.StatusFor(R3PartKind.FlyingDroneGenerator)}",small);
                        if(hasGroundDrone)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.GroundDroneGenerator)} dron naziemny: {deploy.StatusFor(R3PartKind.GroundDroneGenerator)}",small);
                        if(hasOffensiveTurret)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.OffensiveTurretGenerator)} wieżyczka: {deploy.StatusFor(R3PartKind.OffensiveTurretGenerator)}",small);
                        if(hasHealingTurret)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.HealingTurretGenerator)} naprawa: {deploy.StatusFor(R3PartKind.HealingTurretGenerator)}",small);
                        if(hasSupportTurret)GUI.Label(CombatAbilityStatusRect(statusRow++),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.SupportTurretGenerator)} wsparcie: {deploy.StatusFor(R3PartKind.SupportTurretGenerator)}",small);
                        if(hasShieldTurret)GUI.Label(CombatAbilityStatusRect(statusRow),$"{run.VehicleModel.DeployableSlotLabel(R3PartKind.ShieldTurretGenerator)} kopuła: {deploy.StatusFor(R3PartKind.ShieldTurretGenerator)}",small);
                    }
                    if(GUI.Button(new Rect(535,88,130,30),combatDetailsOpen?"Mniej [I]":"Szczegóły [I]",compactButton))combatDetailsOpen=!combatDetailsOpen;
                    if(combatDetailsOpen)
                    {
                        int plannedGuns=run.VehicleModel.Parts.FindAll(p=>p.Kind==R3PartKind.Gun).Count;
                        GUI.Label(new Rect(36,120+extraStatusHeight,630,26),$"Konstrukcja: {player.Structure.ConnectedBlockCount}/{player.Structure.InitialStructuralBlockCount} • Koła: {player.Modules.ActiveWheelCount}/{player.Modules.InitialWheelCount} • Bronie: {player.Modules.ActiveWeaponCount}/{plannedGuns}",small);
                        var physicsBody=player.GetComponent<Rigidbody>();
                        GUI.Label(new Rect(36,148+extraStatusHeight,630,25),$"Masa: {physicsBody.mass:0} kg • Prędkość: {physicsBody.linearVelocity.magnitude:0.0} m/s • Esc — pauza",small);
                    }
                    float noticeY=(combatDetailsOpen?174:121)+extraStatusHeight;
                    if(resourceWarning!=null)
                    {var before=GUI.contentColor;GUI.contentColor=lowCore?new Color(1f,.62f,.52f):new Color(1f,.8f,.48f);
                     GUI.Label(new Rect(36,noticeY,630,22),resourceWarning,small);GUI.contentColor=before;noticeY+=25;}
                    if(run.PickupGraceRemaining>0)
                    {
                        GUI.Label(new Rect(36,noticeY,630,22),$"Zbierz łupy i energię: {run.PickupGraceRemaining:0.0} s • Enter — nagrody",small);
                        GUI.Label(new Rect(640,210,285,55),"ZWYCIĘSTWO",title);
                    }
                }
                if (!run.Paused)
                {
                    bool special=run.ReactorActive||run.CheckpointRace!=null||run.RoadChallenge!=null;
                    int visibleEnemies=0;
                    if(!special)foreach(var enemy in run.Encounter.Enemies)
                        if(enemy!=null&&!enemy.IsDefeated&&enemy.Core!=null)visibleEnemies++;
                    bool enemiesDefeated=visibleEnemies==0&&run.Encounter.Enemies.Count>0;
                    CombatPanel(CombatEnemyPanelRect(visibleEnemies,special,enemiesDefeated),new Color(.28f,.76f,.75f));
                    if(!DrawReactorCombatStatus(run)&&!DrawFullActivityCombatStatus(run)){
                    GUI.Label(new Rect(940,28,305,48),run.CurrentEncounter.Title,heading);
                    int row=0;foreach(var enemy in run.Encounter.Enemies)
                    {
                        string status=EnemyCoreStatus(enemy);if(status==null)continue;
                        if(row>=4)break;
                        GUI.Label(EnemyStatusLabelRect(row),status,small);
                        DrawResourceBar(EnemyStatusBarRect(row),enemy.Core.CurrentHp,enemy.Core.MaxHp,new Color(.95f,.53f,.28f));row++;
                    }
                    if(row==0&&run.Encounter.Enemies.Count>0)GUI.Label(new Rect(940,80,305,50),"Wszyscy przeciwnicy pokonani.",small);
                    }
                    if(GUI.Button(new Rect(1130,670,125,35),"Pauza",compactButton))run.TogglePause();
                    GUI.enabled=run.EmergencyRescueCooldownLeft<=0;
                    if(GUI.Button(new Rect(990,670,130,35),run.EmergencyRescueCooldownLeft>0?$"Ratunek {Mathf.CeilToInt(run.EmergencyRescueCooldownLeft)}s":"Ratunek",compactButton))run.TryEmergencyRescue();
                    GUI.enabled=true;
                    if(run.EmergencyRescueNotice!=null||run.EmergencyRescueActive)GUI.Label(new Rect(660,637,450,30),
                        run.EmergencyRescueNotice??"Ratunek: 50% otrzymywanych obrażeń; atak wstrzymany.",small);
                    DrawAimReticle(run);
                    GUI.Label(new Rect(24, 676, 900, 26),GameplayControlsHint(),small);
                    DrawFeedback();
                    return;
                }
            }
            Panel(new Rect(0,0,portraitReady||portraitRoute||portraitSalvage?768:1280,portraitReady||portraitRoute||portraitSalvage?1024:720));
            if (run.Transitioning)
            { GUI.Label(new Rect(360, 310, 600, 80), "Przygotowanie starcia…", title); return; }
            if(progress.State==R1RunState.Salvage){if(portraitSalvage)DrawPortraitSalvage(run);else DrawSalvage(run);return;}
            if(progress.State==R1RunState.Route){if(portraitRoute)DrawPortraitRoute(run);else DrawRoute(run);return;}
            if (progress.State == R1RunState.Ready)
            {
                if(portraitReady){DrawPortraitReady(run);return;}
                GUI.Label(new Rect(90, 55, 1100, 60), "ROBOROGUELIKE • PEŁNA INTEGRACJA", title);
                GUI.Label(new Rect(90, 125, 1100, 45), "Wybierz jeden z trzech wylosowanych pojazdów", heading);
                GUI.Label(new Rect(90,173,1100,28),"Jeden pojazd, wspólny garaż i nagrody. Aktywność wybierzesz po przygotowaniu.",small);
                for(int i=0;i<3;i++)
                {
                    int offer=starterOffers[i],variant=offer%4;var kind=(R3VehicleKind)(offer/4);var design=starterDesigns[i];float x=80+i*380;
                    Panel(new Rect(x,205,360,280));
                    GUI.Label(new Rect(x+20,220,320,40),design.Name,heading);
                    GUI.Label(new Rect(x+20,270,320,90),starterDescriptions[i],body);
                    GUI.Label(new Rect(x+20,365,320,45),starterSummaries[i],small);
                    if(GUI.Button(new Rect(x+20,420,320,48),run.SelectedVehicle==kind&&run.SelectedVehicleVariant==variant?"WYBRANY":"Wybierz",button))run.SelectVehicle(kind,variant);
                }
                GUI.Label(ReadyIntroRect(),"6 starć • 3 areny • elity • Oblężnik • nagrody: 1 bonus i do 3 części",small);
                GUI.Label(new Rect(90,570,110,28),"Seed:",body);
                seedText=GUI.TextField(new Rect(200,568,230,28),seedText,11);
                if(int.TryParse(seedText,out int seed))run.RequestedSeed=seed;
                DrawSeedWarning(ReadySeedWarningRect(),run.RequestedSeed);
                if(GUI.Button(new Rect(450,548,170,38),"Wczytaj wyprawę",compactButton))run.LoadRunCheckpoint();
                if(!string.IsNullOrEmpty(run.RunSaveMessage))GUI.Label(ReadySaveMessageRect(),run.RunSaveMessage,small);
                if (GUI.Button(new Rect(90,600,500,65), StarterActionCaption(run), button)) run.PrepareUnifiedRun();
                if(GUI.Button(new Rect(640,600,550,65),"Poligon kreatywny — buduj od rdzenia",button))run.OpenCreativeGarage();
                if(GUI.Button(new Rect(640,548,300,38),"Edytor map",compactButton))run.OpenMapEditor();
                if(GUI.Button(new Rect(960,552,230,38),"Wyjdź z gry",button))run.RequestQuit();
                GUI.Label(new Rect(90,675,1100,30),GameplayControlsHint(),small);
            }
            else if (progress.State == R1RunState.UpgradeChoice)
            {
                GUI.Label(new Rect(90, 90, 1120, 65), $"Zwycięstwo!  +{run.LastScrapReward} Złomu", title);
                GUI.Label(new Rect(90, 170, 1120, 55), "Wybierz moduł 1×1×1 i zamontuj go w garażu, aby aktywować bonus", heading);
                // Keep rendering a stable snapshot: a click clears Offers immediately.
                var offers = new R1UpgradeDefinition[progress.Offers.Count];
                for (int i = 0; i < offers.Length; i++) offers[i] = progress.Offers[i];
                for (int i = 0; i < offers.Length; i++)
                {
                    float x = 80 + i * 380;
                    Panel(new Rect(x, 270, 360, 290));
                    GUI.Label(new Rect(x + 20, 292, 320, 60), offers[i].Title, heading);
                    GUI.Label(new Rect(x + 20, 365, 320, 110), offers[i].Description, body);
                    if (GUI.Button(new Rect(x + 20, 490, 320, 52), $"Wybierz  [{i + 1}]", button))
                    { run.ChooseUpgrade(i); break; }
                }
                GUI.Label(new Rect(90, 610, 1100, 60), $"Złom: {run.VehicleModel.Scrap}   •   Kolejne starcie: {progress.EncounterIndex + 2}/{progress.TotalEncounters}   •   Naprawy i rozbudowa w garażu", body);
            }
            else
            {
                bool won = progress.State == R1RunState.Won;
                GUI.Label(new Rect(160, 130, 1050, 80), won ? "ZWYCIĘSTWO" : "PORAŻKA", title);
                GUI.Label(new Rect(160, 235, 960, 70), RunEndOutcome(won,progress.DefeatReason), heading);
                GUI.Label(new Rect(160, 325, 960, 200), $"Walki: {progress.CompletedEncounters}/{progress.TotalEncounters}  •  Pojazd: {run.VehicleModel.Design.Name}\nPokonani: {run.DefeatedEnemies}  •  Zdobyty Złom: {run.EarnedScrap}  •  Saldo: {run.VehicleModel.Scrap}\n{RunEndRecovery(run.RecoveredModules)}  •  Części końcowe: {run.VehicleModel.Parts.FindAll(p=>p.Health>0).Count}\nZdobyte moduły bonusów: {progress.Acquired.Count}  •  Seed: {run.Seed}\n{RunEndNewRunNote()}", body);
                if (GUI.Button(new Rect(160, 535, 500, 70), "Wróć do menu  [Enter]", button)) run.RestartRun();
            }
        }

        private void DrawPortraitReady(R1RunController run)
        {
            GUI.Label(new Rect(24,20,720,48),"ROBOROGUELIKE",title);
            GUI.Label(new Rect(24,74,720,38),"Wybierz jeden z trzech pojazdów",heading);
            GUI.Label(new Rect(24,113,720,28),"Jeden pojazd, wspólny garaż i nagrody.",small);
            for(int i=0;i<3;i++)
            {
                int offer=starterOffers[i],variant=offer%4;var kind=(R3VehicleKind)(offer/4);var design=starterDesigns[i];var card=PortraitVehicleCard(i);
                Panel(card);
                GUI.Label(new Rect(card.x+16,card.y+8,440,34),design.Name,heading);
                GUI.Label(new Rect(card.x+16,card.y+44,440,62),starterDescriptions[i],body);
                GUI.Label(new Rect(card.x+16,card.y+112,430,42),starterSummaries[i],small);
                if(GUI.Button(new Rect(card.x+475,card.y+96,225,52),run.SelectedVehicle==kind&&run.SelectedVehicleVariant==variant?"WYBRANY":"Wybierz",button))run.SelectVehicle(kind,variant);
            }
            GUI.Label(new Rect(24,704,75,30),"Seed:",body);
            seedText=GUI.TextField(new Rect(105,704,205,32),seedText,11);
            if(int.TryParse(seedText,out int seed))run.RequestedSeed=seed;
            DrawSeedWarning(new Rect(600,700,144,42),run.RequestedSeed);
            if(GUI.Button(new Rect(340,700,250,40),"Wczytaj wyprawę",compactButton))run.LoadRunCheckpoint();
            if(!string.IsNullOrEmpty(run.RunSaveMessage))GUI.Label(new Rect(24,744,720,34),run.RunSaveMessage,small);
            if(GUI.Button(PortraitActionRect(0),StarterActionCaption(run),button))run.PrepareUnifiedRun();
            if(GUI.Button(PortraitActionRect(1),"Poligon kreatywny — buduj od rdzenia",button))run.OpenCreativeGarage();
            if(GUI.Button(PortraitActionRect(2),"Edytor map",compactButton))run.OpenMapEditor();
            if(GUI.Button(PortraitActionRect(3),"Wyjdź z gry",compactButton))run.RequestQuit();
            GUI.Label(new Rect(24,920,720,43),"6 starć • nagrody • garaż\nPo walce wybierz moduł i części z wraków.",small);
            GUI.Label(new Rect(24,975,720,40),GameplayControlsHint(),small);
        }

        private void DrawSessionPause(R1RunController run)
        {
            Panel(new Rect(0,0,1280,720));
            GUI.Label(new Rect(350,130,600,70),"Pauza",title);
            if(GUI.Button(new Rect(350,220,580,58),PauseCaption(run,0,"Wznów  [Esc]"),button))run.TogglePause();
            if(GUI.Button(new Rect(350,290,580,58),PauseCaption(run,1,run.MapReturnPending?"Potwierdź powrót":"Powrót do menu głównego"),button))run.RestartRun();
            if(GUI.Button(new Rect(350,360,580,58),PauseCaption(run,2,run.MapQuitPending?"Potwierdź wyjście":"Wyjdź z gry"),button))run.RequestQuitFromMenu();
            if(GUI.Button(new Rect(950,220,240,58),PauseCaption(run,3,"Sterowanie"),compactButton))OpenPauseControls();
            GUI.Label(new Rect(350,430,580,60),run.MapExitPending?run.EditorMessage:"↑ / ↓ i Enter • pad: krzyżak, A, B / Start",small);
            DrawMovementPause(run);DrawSuspensionPause(run);
        }
        private static string PauseCaption(R1RunController run,int index,string label)
            => (run.PauseSelection==index?"›  ":"")+label;

        private void DrawGarage(R1RunController run)
        {
            // A partial camera viewport does not clear the surrounding backbuffer.
            // Cover every outside pixel opaquely so earlier menu text cannot persist in the gaps.
            var viewport=R3GarageView.Viewport;var before=GUI.color;GUI.color=new Color(0.04f,0.06f,0.08f,1);
            float left=viewport.xMin*1280,right=viewport.xMax*1280,top=(1-viewport.yMax)*720,bottom=(1-viewport.yMin)*720;
            GUI.DrawTexture(new Rect(0,0,left,720),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(right,0,1280-right,720),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(left,0,right-left,top),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(left,bottom,right-left,720-bottom),Texture2D.whiteTexture);
            GUI.color=before;
            var garage=run.Garage;var model=run.VehicleModel;var selected=model.At(garage.Cell);
            Panel(new Rect(10,10,320,700));Panel(new Rect(950,10,320,700));
            GUI.Label(new Rect(30,25,290,40),"GARAŻ",heading);
            GUI.Label(new Rect(30,67,290,35),$"Złom: {model.Scrap} • Punkty: {model.ConstructionBudgetUsed}/{model.ConstructionBudgetLimit}",body);
            var garageEnergy=run.Player!=null?run.Player.GetComponent<VehicleEnergyBank>():null;
            if(garageEnergy!=null)GUI.Label(new Rect(30,91,290,20),$"Energia: {garageEnergy.Current:0}/{garageEnergy.Maximum:0}",small);
            DrawMovementCatalog(run);
            GUI.backgroundColor=Color.white;
            string[] tools={"Montuj","Zaznacz","Napraw","Zdejmij"};
            for(int i=0;i<4;i++)
            {
                GUI.backgroundColor=garage.Tool==(R41GarageTool)i?new Color(0.3f,0.9f,0.9f):Color.white;
                if(GUI.Button(new Rect(25+(i%2)*150,215+(i/2)*48,145,43),tools[i],compactButton))garage.SelectTool((R41GarageTool)i);
            }
            GUI.backgroundColor=Color.white;
            DrawMovementGarage(run);
            if(settingsTab!=2||(garage.Tool==R41GarageTool.Mount&&garage.HasTarget&&garage.PlacementIssue!=null))GUI.Label(new Rect(30,453,280,60),garage.Tool==R41GarageTool.Mount?
                garage.PlacementIssue??"Punkty mocowania zgodne. Kliknij, aby zamontować.":
                "Wybierz część konstrukcji: jej stan zobaczysz w warsztacie.",small);
            GUI.enabled=garage.Tool==R41GarageTool.Mount;
            if(GUI.Button(new Rect(28,514,91,27),"Q / E  Y",compactButton))garage.RotateSelected(1,1);
            if(GUI.Button(new Rect(123,514,91,27),"R / F  X",compactButton))garage.RotateSelected(0,1);
            if(GUI.Button(new Rect(218,514,91,27),"Z / X  Z",compactButton))garage.RotateSelected(2,1);
            GUI.enabled=true;
            GUI.Label(new Rect(30,545,285,27),$"Obrót X {garage.SelectedRotation.x*90}° · Y {garage.SelectedRotation.y*90}° · Z {garage.SelectedRotation.z*90}°",small);
            if(GUI.Button(new Rect(30,580,130,40),"Zoom +",compactButton))garage.Zoom(-1);
            if(GUI.Button(new Rect(175,580,130,40),"Zoom −",compactButton))garage.Zoom(1);
            if(GUI.Button(new Rect(30,630,275,40),"Dopasuj widok",compactButton))garage.FrameVehicle();
            GUI.Label(new Rect(30,676,280,25),"PPM — kamera • Q/E R/F Z/X — część",small);
            if(GUI.Button(new Rect(970,25,280,38),inventoryOpen?"← Stan pojazdu":"Wybrany stos części →",compactButton))
            {inventoryOpen=!inventoryOpen;garage.RefreshMarker();}
            if(inventoryOpen)DrawInventory(run);
            else {
            GUI.Label(new Rect(970,70,280,65),$"Komórka: {garage.Cell}\n{PartName(selected?.Kind??garage.SelectedPart)} {(selected==null?"— nowa część":"")}",body);
            GUI.Label(new Rect(970,140,280,65),selected==null?(garage.SelectedInventoryId!=null?"Wybrano egzemplarz z magazynu.":"Wybierz egzemplarz z katalogu części."):
                $"{(selected.Health<=0?"Utracony plan":selected.Health<0.999f?"Zamontowana • uszkodzona":"Zamontowana • sprawna")} • HP: {selected.Health*100:0}%\n{(selected.Health<=0?"Odtworzenie":"Naprawa")}: {model.RepairCost(selected)} Złomu",small);
            string error=garage.PlacementIssue;
            GUI.enabled=garage.Tool==R41GarageTool.Mount&&error==null;
            GUI.Label(new Rect(970,216,280,48),"Montaż: wybierz egzemplarz i kliknij pojazd.",small);
            GUI.enabled=selected!=null&&model.RepairCost(selected)>0&&model.Scrap>=model.RepairCost(selected);
            if(GUI.Button(new Rect(970,275,280,48),selected!=null&&selected.Health<=0?"Odtwórz utraconą część":"Napraw wybraną część",button))
                garage.RepairPartAt(garage.Cell);
            GUI.enabled=selected!=null;
            if(GUI.Button(new Rect(970,334,280,48),"Zdejmij część",compactButton))
                garage.RemovePartAt(garage.Cell);
            GUI.enabled=true;
            int bulkCost=model.AffordableRepairCost;
            GUI.enabled=bulkCost>0;
            if(GUI.Button(new Rect(970,405,280,48),$"Napraw możliwe — {bulkCost} Z",button))
                garage.Message=run.GarageRepairAffordable()?"Naprawiono dostępne części; sprawdź pozostały Złom.":"Brak środków, połączenia lub uszkodzeń.";
            int emergencyCost=model.EmergencyRepairCost;
            GUI.enabled=emergencyCost>0&&model.Scrap>=emergencyCost;
            if(GUI.Button(new Rect(970,465,280,56),$"Odbudowa awaryjna — {emergencyCost} Z\nniezbędne moduły: 100% HP",compactButton))
                garage.Message=run.GarageEmergencyRepair()?"Odbudowano niezbędną broń, napęd i połączenia; koszt pobrano raz.":"Brak środków lub poprawnego planu napędu / rdzenia.";
            GUI.enabled=true;

            }
            GUI.Label(new Rect(350,25,580,60),run.SandboxWorkshop?$"{model.Design.Name} • Warsztat poligonu":$"{model.Design.Name}  •  Następna walka {run.Progress.EncounterIndex+1}/{run.Progress.TotalEncounters}",heading);
            DrawReactorGarageAction(run);
            if(!run.ReactorPrepared&&!run.SandboxWorkshop){GUI.enabled=run.CanChooseGeneratedArena;
                if(GUI.Button(new Rect(355,80,280,38),"Generowana arena • seed "+run.RequestedSeed,compactButton))run.ChooseGeneratedArena();GUI.enabled=true;}
            if(!run.ReactorPrepared&&!run.SandboxWorkshop){GUI.enabled=run.CanChooseLD02Arena;
                if(GUI.Button(new Rect(355,168,280,38),"Arena LD02 • walka",compactButton))run.ChooseLD02Arena();GUI.enabled=true;}
            if(!run.ReactorPrepared&&!run.SandboxWorkshop&&!run.MapEditorSession)
                if(GUI.Button(new Rect(650,168,265,38),"Zapisz wyprawę",compactButton))run.SaveRunCheckpoint();
            DrawFullActivityGarageActions(run);
            Panel(R3GarageView.LegendOverlay);
            GUI.Label(new Rect(360,422,540,22),"Czerwone linie: zajęte pola  •  Niebieski: obrys części",small);
            Panel(R3GarageView.WeaponGroupsOverlay);
            GUI.Label(new Rect(363,458,370,24),"A / LPM: "+WeaponGroupName(model.WeaponGroupA),small);
            GUI.Label(new Rect(363,485,370,24),"B / PPM: "+WeaponGroupName(model.WeaponGroupB),small);
            if(GUI.Button(new Rect(740,465,155,48),"Zamień A / B",compactButton))model.SwapWeaponGroups();
            if(GUI.Button(new Rect(355,538,125,44),"Obrót ←",compactButton))garage.Orbit(-25);
            if(GUI.Button(new Rect(485,538,125,44),"Obrót →",compactButton))garage.Orbit(25);
            if(GUI.Button(new Rect(615,538,125,44),"Widok ↑",compactButton))garage.Tilt(10);
            if(GUI.Button(new Rect(745,538,125,44),"Widok ↓",compactButton))garage.Tilt(-10);
            Panel(new Rect(345,590,585,115));
            GUI.Label(new Rect(360,600,555,55),model.CanDeploy?garage.Message:
                "Możesz budować dalej; aktywność niedostępna.\n"+(model.CriticalReadinessMessage??model.DeployError),small);
            GUI.enabled=run.MapEditorSession||model.CanDeploy;
            if(run.MapEditorSession){if(GUI.Button(new Rect(360,654,270,40),"Wróć do edytora map",compactButton))run.ReturnToMapEditorFromGarage();}
            else if(GUI.Button(new Rect(360,654,270,40),"Testuj na poligonie",compactButton))run.EnterTestRange();
            GUI.enabled=run.SandboxWorkshop||model.CanDeploy;
            if(GUI.Button(new Rect(645,654,270,40),run.ReactorPrepared?"Broń reaktora":run.SandboxWorkshop?"Menu główne":"Następna trasa",compactButton))
            {if(run.ReactorPrepared)run.ChooseReactorDefense();else if(run.SandboxWorkshop)run.RestartRun();else run.StartNextEncounter();}
            GUI.enabled=true;
            if(!model.CanDeploy)GUI.Label(new Rect(970,650,280,55),model.DeployError,small);
        }
        private static string WeaponGroupName(R4WeaponKind? kind) => kind.HasValue?R4WeaponDefinition.Get(kind.Value).ShortName:"—";
        private static string PartName(R3PartKind kind) => kind==R3PartKind.Gun?"Broń":R4WeaponDefinition.PartName(kind);
    }
}
