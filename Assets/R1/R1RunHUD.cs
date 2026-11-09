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
        private Texture2D buttonNormal,buttonHover,buttonPressed;
        private bool combatDetailsOpen;
        private int[] starterOffers;
        private readonly System.Random starterOfferRandom=new System.Random();
        private void Update()=>RefreshStarterOffers(R1RunController.Instance);
        private void RefreshStarterOffers(R1RunController run)
        {
            if(run==null||run.Progress==null)return;
            if(run.Progress.State!=R1RunState.Ready)starterOffers=null;
            else if(!run.MapEditing&&starterOffers==null)
            {
                starterOffers=R3VehicleCatalog.DrawOffers(starterOfferRandom);
            }
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
        private void DismissCatalog(){catalogOpen=false;catalogSearch=string.Empty;dismantleConfirmation=null;catalogFeedback=null;SyncQuickCategoryToCatalog();}
        internal bool ClosePartCatalog()
        {
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
        private void OnGUI()
        {
            var run = R1RunController.Instance;
            if (run == null) return;
            RefreshStarterOffers(run);
            if(run.Progress?.State==R1RunState.Ready&&!run.MapEditing&&starterOffers!=null&&
               !starterOffers.Any(offer=>(R3VehicleKind)(offer/4)==run.SelectedVehicle&&offer%4==run.SelectedVehicleVariant))
                run.SelectVehicle((R3VehicleKind)(starterOffers[0]/4),starterOffers[0]%4);
            Setup();
            bool sessionOverlay = run.Paused || run.QuitRequested;
            if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape)
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
                if(!sessionOverlay&&run.MapEditing)run.MapEditorPointer(Event.current,rawPointer);
                else if(!sessionOverlay&&!catalogOpen&&!garageOptionsOpen&&run.Progress?.State==R1RunState.Garage&&!run.Transitioning&&!run.Paused&&!run.QuitRequested)run.Garage.HandleGuiPointer(Event.current,rawPointer);
                else run.Garage?.CancelPointerGesture();
            }
            finally { GUI.matrix = previous; GUI.enabled = true; }
        }
        private void Draw(R1RunController run,bool portraitReady,bool portraitGarage,bool portraitRoute,bool portraitSalvage)
        {
            if (run.QuitRequested)
            { Panel(new Rect(0,0,1280,720)); GUI.Label(new Rect(350,300,650,80),"Zamykanie gry…",title); return; }
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
                Panel(new Rect(20,18,660,(combatDetailsOpen?180:125)+(run.PickupGraceRemaining>0?25:0)));
                GUI.Label(new Rect(36, 27, 630, 30), $"STARCIE {progress.EncounterIndex + 1}/{progress.TotalEncounters}   •   Wrogowie: {run.Encounter.AliveCount}   •   Złom: {run.VehicleModel.Scrap}", body);
                var player = run.Player;
                if (player != null)
                {
                    var core = player.Core;
                    float hp=core!=null?core.CurrentHp:0,maxHp=core!=null?core.MaxHp:0;
                    GUI.Label(new Rect(36,58,175,28),$"RDZEŃ: {hp:0}/{maxHp:0}",body);
                    DrawResourceBar(new Rect(215,65,110,12),hp,maxHp,new Color(.35f,.85f,.67f));
                    GUI.Label(new Rect(340,58,325,28),ActivityGoal(run),small);
                    var energy=player.GetComponent<VehicleEnergyBank>();
                    var repairPulse=player.GetComponent<R3RepairPulse>();
                    if(energy!=null)
                    {
                        GUI.Label(new Rect(36,90,175,25),$"Energia: {energy.Current:0}/{energy.Maximum:0}",small);
                        DrawResourceBar(new Rect(215,95,110,12),energy.Current,energy.Maximum,new Color(.25f,.65f,.95f));
                        if(repairPulse!=null)GUI.Label(new Rect(340,90,190,25),$"V — naprawa: {repairPulse.Status}",small);
                    }
                    if(GUI.Button(new Rect(535,88,130,30),combatDetailsOpen?"Mniej [I]":"Szczegóły [I]",compactButton))combatDetailsOpen=!combatDetailsOpen;
                    if(combatDetailsOpen)
                    {
                        int plannedGuns=run.VehicleModel.Parts.FindAll(p=>p.Kind==R3PartKind.Gun).Count;
                        GUI.Label(new Rect(36,120,630,26),$"Konstrukcja: {player.Structure.ConnectedBlockCount}/{player.Structure.InitialStructuralBlockCount} • Koła: {player.Modules.ActiveWheelCount}/{player.Modules.InitialWheelCount} • Bronie: {player.Modules.ActiveWeaponCount}/{plannedGuns}",small);
                        var physicsBody=player.GetComponent<Rigidbody>();
                        GUI.Label(new Rect(36,148,630,25),$"Masa: {physicsBody.mass:0} kg • Prędkość: {physicsBody.linearVelocity.magnitude:0.0} m/s • Esc — pauza",small);
                    }
                    if(run.PickupGraceRemaining>0)
                    {
                        GUI.Label(new Rect(36,combatDetailsOpen?174:121,630,22),$"Zbierz energię: {run.PickupGraceRemaining:0.0} s • Enter — nagrody",small);
                        GUI.Label(new Rect(640,210,285,55),"ZWYCIĘSTWO",title);
                    }
                }
                if (!run.Paused)
                {
                    Panel(new Rect(925,18,335,160));
                    if(!DrawReactorCombatStatus(run)&&!DrawFullActivityCombatStatus(run)){
                    GUI.Label(new Rect(940,28,305,48),run.CurrentEncounter.Title,heading);
                    int row=0;foreach(var enemy in run.Encounter.Enemies)
                    {
                        if(enemy==null||enemy.IsDefeated)continue;
                        GUI.Label(new Rect(940,80+row*24,305,25),$"{enemy.name}: rdzeń {enemy.Core.CurrentHp:0} HP",small);row++;
                    }
                    }
                    if(GUI.Button(new Rect(1130,670,125,35),"Pauza",compactButton))run.TogglePause();
                    DrawAimReticle(run);
                    GUI.Label(new Rect(24, 676, 900, 26),run.VehicleModel.SelectedDrive>=R3DriveKind.Omni?
                        "W/S — jazda • A/D — ruch boczny • Z/C — obrót • Mysz — celowanie • LPM/PPM — ogień":
                        "W/S — jazda • A/D — skręt • Mysz — celowanie • LPM/PPM — ogień • Kółko — przybliżenie",small);
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
                    int offer=starterOffers[i],variant=offer%4;var kind=(R3VehicleKind)(offer/4);var design=R3VehicleCatalog.Create(kind,variant);float x=80+i*380;
                    Panel(new Rect(x,205,360,280));
                    GUI.Label(new Rect(x+20,220,320,40),design.Name,heading);
                    GUI.Label(new Rect(x+20,270,320,90),design.Description,body);
                    float estimatedSpeed=R3VehicleAssembly.SpeedFor(design.Parts.Sum(R4WeaponDefinition.PartMass),design.Parts.Count(p=>p.Kind==R3PartKind.Wheel));
                    GUI.Label(new Rect(x+20,365,320,45),$"Części: {design.Parts.Count}  •  Limit napędu: {estimatedSpeed:0.0} m/s\nRdzeń: {design.CoreHp:0} HP",small);
                    if(GUI.Button(new Rect(x+20,420,320,48),run.SelectedVehicle==kind&&run.SelectedVehicleVariant==variant?"WYBRANY":"Wybierz",button))run.SelectVehicle(kind,variant);
                }
                GUI.Label(ReadyIntroRect(),"6 starć • 3 areny • elity • Oblężnik • nagrody: 1 bonus i do 3 części",small);
                GUI.Label(new Rect(90,570,110,28),"Seed:",body);
                seedText=GUI.TextField(new Rect(200,568,230,28),seedText,11);
                if(int.TryParse(seedText,out int seed))run.RequestedSeed=seed;
                DrawSeedWarning(ReadySeedWarningRect(),run.RequestedSeed);
                if(GUI.Button(new Rect(450,548,170,38),"Wczytaj wyprawę",compactButton))run.LoadRunCheckpoint();
                if(!string.IsNullOrEmpty(run.RunSaveMessage))GUI.Label(ReadySaveMessageRect(),run.RunSaveMessage,small);
                if (GUI.Button(new Rect(90,600,500,65), "Przygotuj pojazd  [Enter]", button)) run.PrepareUnifiedRun();
                if(GUI.Button(new Rect(640,600,550,65),"Garaż i poligon testowy",button))run.OpenSandboxGarage();
                if(GUI.Button(new Rect(640,548,300,38),"Edytor map",compactButton))run.OpenMapEditor();
                if(GUI.Button(new Rect(960,552,230,38),"Wyjdź z gry",button))run.RequestQuit();
                GUI.Label(new Rect(90,675,1100,30),"WASD — jazda / hamowanie • Mysz — celowanie • LPM/PPM — ogień • Esc — pauza",small);
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
                GUI.Label(new Rect(160, 235, 960, 70), won ? "Oblężnik pokonany. Wyprawa ukończona." : progress.DefeatReason, heading);
                GUI.Label(new Rect(160, 325, 960, 200), $"Walki: {progress.CompletedEncounters}/{progress.TotalEncounters}  •  Pojazd: {run.VehicleModel.Design.Name}\nPokonani: {run.DefeatedEnemies}  •  Zdobyty Złom: {run.EarnedScrap}  •  Saldo: {run.VehicleModel.Scrap}\nOdzyskane moduły: {run.RecoveredModules}  •  Części końcowe: {run.VehicleModel.Parts.FindAll(p=>p.Health>0).Count}\nZdobyte moduły bonusów: {progress.Acquired.Count}  •  Seed: {run.Seed}\nNowa próba przywróci wybrany pojazd, budżet startowy i brak bonusów.", body);
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
                int offer=starterOffers[i],variant=offer%4;var kind=(R3VehicleKind)(offer/4);var design=R3VehicleCatalog.Create(kind,variant);var card=PortraitVehicleCard(i);
                Panel(card);
                GUI.Label(new Rect(card.x+16,card.y+8,440,34),design.Name,heading);
                GUI.Label(new Rect(card.x+16,card.y+44,440,62),design.Description,body);
                float speed=R3VehicleAssembly.SpeedFor(design.Parts.Sum(R4WeaponDefinition.PartMass),design.Parts.Count(p=>p.Kind==R3PartKind.Wheel));
                GUI.Label(new Rect(card.x+16,card.y+112,430,42),$"Części: {design.Parts.Count} • Limit napędu: {speed:0.0} m/s\nRdzeń: {design.CoreHp:0} HP",small);
                if(GUI.Button(new Rect(card.x+475,card.y+96,225,52),run.SelectedVehicle==kind&&run.SelectedVehicleVariant==variant?"WYBRANY":"Wybierz",button))run.SelectVehicle(kind,variant);
            }
            GUI.Label(new Rect(24,704,75,30),"Seed:",body);
            seedText=GUI.TextField(new Rect(105,704,205,32),seedText,11);
            if(int.TryParse(seedText,out int seed))run.RequestedSeed=seed;
            DrawSeedWarning(new Rect(600,700,144,42),run.RequestedSeed);
            if(GUI.Button(new Rect(340,700,250,40),"Wczytaj wyprawę",compactButton))run.LoadRunCheckpoint();
            if(!string.IsNullOrEmpty(run.RunSaveMessage))GUI.Label(new Rect(24,744,720,34),run.RunSaveMessage,small);
            if(GUI.Button(PortraitActionRect(0),"Przygotuj pojazd  [Enter]",button))run.PrepareUnifiedRun();
            if(GUI.Button(PortraitActionRect(1),"Garaż i poligon testowy",button))run.OpenSandboxGarage();
            if(GUI.Button(PortraitActionRect(2),"Edytor map",compactButton))run.OpenMapEditor();
            if(GUI.Button(PortraitActionRect(3),"Wyjdź z gry",compactButton))run.RequestQuit();
            GUI.Label(new Rect(24,920,720,43),"6 starć • nagrody • garaż\nPo walce wybierz moduł i części z wraków.",small);
            GUI.Label(new Rect(24,975,720,40),"WASD — jazda • Mysz — celowanie • LPM/PPM — ogień • Esc — pauza",small);
        }

        private void DrawSessionPause(R1RunController run)
        {
            Panel(new Rect(0,0,1280,720));
            GUI.Label(new Rect(350,130,600,70),"Pauza",title);
            if(GUI.Button(new Rect(350,220,580,58),PauseCaption(run,0,"Wznów  [Esc]"),button))run.TogglePause();
            if(GUI.Button(new Rect(350,290,580,58),PauseCaption(run,1,run.MapReturnPending?"Potwierdź powrót":"Powrót do menu głównego"),button))run.RestartRun();
            if(GUI.Button(new Rect(350,360,580,58),PauseCaption(run,2,run.MapQuitPending?"Potwierdź wyjście":"Wyjdź z gry"),button))run.RequestQuitFromMenu();
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
            GUI.Label(new Rect(30,67,290,35),$"Złom: {model.Scrap}  •  Pola: {System.Linq.Enumerable.Sum(model.Parts,p=>System.Linq.Enumerable.Count(R3VehicleModel.OccupiedCells(p)))}/180",body);
            var garageEnergy=run.Player!=null?run.Player.GetComponent<VehicleEnergyBank>():null;
            if(garageEnergy!=null)GUI.Label(new Rect(30,91,290,20),$"Energia: {garageEnergy.Current:0}/{garageEnergy.Maximum:0}",small);
            DrawMovementCatalog(run);
            GUI.backgroundColor=Color.white;
            string[] tools={"Montuj","Zaznacz","Napraw","Zdejmij"};
            for(int i=0;i<4;i++)
            {
                GUI.backgroundColor=garage.Tool==(R41GarageTool)i?new Color(0.3f,0.9f,0.9f):Color.white;
                if(GUI.Button(new Rect(25+(i%2)*150,215+(i/2)*48,145,43),tools[i],compactButton))garage.SetTool((R41GarageTool)i);
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
                garage.Message=run.GarageRepair(garage.Cell)?"Przywrócono HP części.":"Najpierw odtwórz połączenie z konstrukcją.";
            GUI.enabled=selected!=null;
            if(GUI.Button(new Rect(970,334,280,48),"Zdejmij część",compactButton))
                garage.Message=run.GarageRemove(garage.Cell)?"Część wróciła do magazynu w obecnym stanie.":"Nie można usunąć rdzenia ani mostu; sprawdź także miejsce w magazynie.";
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
