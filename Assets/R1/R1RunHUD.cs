using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD : MonoBehaviour
    {
        private GUIStyle title, heading, body, small, button, compactButton;
        private int weaponPage;
        private readonly Color panel = new Color(0.04f, 0.06f, 0.08f, 0.94f);
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
        }
        private void Panel(Rect rect)
        {
            var before = GUI.color; GUI.color = panel; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = before;
        }
        private void OnGUI()
        {
            var run = R1RunController.Instance;
            if (run == null) return;
            Setup();
            if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape&&(run.Progress?.State==R1RunState.Combat||run.Progress?.State==R1RunState.TestDrive))
            {run.HandleEscape();Event.current.Use();}
            var rawPointer=Event.current.mousePosition;
            var previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                (Screen.height - 720 * scale) / 2), Quaternion.identity, Vector3.one * scale);
            try
            {
                Draw(run);
                if(run.Progress?.State==R1RunState.Garage&&!run.Transitioning)run.Garage.HandleGuiPointer(Event.current,rawPointer);
            }
            finally { GUI.matrix = previous; GUI.enabled = true; }
        }
        private void Draw(R1RunController run)
        {
            if (run.Error != null)
            {
                Panel(new Rect(240, 190, 800, 340));
                GUI.Label(new Rect(280, 230, 720, 60), "Nie udało się rozpocząć wyprawy", heading);
                GUI.Label(new Rect(280, 310, 720, 100), run.Error, body); return;
            }
            if (run.Progress == null) return;
            var progress = run.Progress;
            if(progress.State==R1RunState.TestDrive){DrawTestRange(run);return;}
            if(progress.State==R1RunState.Garage&&!run.Transitioning){DrawGarage(run);return;}
            if (progress.State == R1RunState.Combat && !run.Transitioning)
            {
                Panel(new Rect(20, 18, 660, 158));
                GUI.Label(new Rect(36, 27, 630, 30), $"STARCIE {progress.EncounterIndex + 1}/{progress.TotalEncounters}   •   Wrogowie: {run.Encounter.AliveCount}   •   Złom: {run.VehicleModel.Scrap}", body);
                var player = run.Player;
                if (player != null)
                {
                    var core = player.Core;
                    GUI.Label(new Rect(36, 58, 630, 28), $"RDZEŃ: {(core != null ? core.CurrentHp : 0):0}/{(core != null ? core.MaxHp : 0):0} HP   •   {ActivityGoal(run)}", body);
                    int plannedGuns=run.VehicleModel.Parts.FindAll(p=>p.Kind==R3PartKind.Gun).Count;
                    GUI.Label(new Rect(36, 93, 630, 26), $"Konstrukcja: {player.Structure.ConnectedBlockCount}/{player.Structure.InitialStructuralBlockCount}    Koła: {player.Modules.ActiveWheelCount}/{player.Modules.InitialWheelCount}    Bronie: {player.Modules.ActiveWeaponCount}/{plannedGuns}", small);
                    var physicsBody=player.GetComponent<Rigidbody>();
                    GUI.Label(new Rect(36, 126, 630, 25), $"Masa: {physicsBody.mass:0} kg • Prędkość: {physicsBody.linearVelocity.magnitude:0.0} m/s • Esc — pauza", small);
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
                    GUI.Label(new Rect(629, 341, 32, 38), "+", heading);
                    GUI.Label(new Rect(24, 676, 900, 26),run.VehicleModel.SelectedDrive>=R3DriveKind.Omni?
                        "W/S — jazda • A/D — ruch boczny • Z/C — obrót • Mysz — celowanie • LPM — ogień":
                        "W/S — jazda • A/D — skręt • Mysz — celowanie • LPM — ogień • Kółko — przybliżenie",small);
                    DrawFeedback();
                    return;
                }
            }
            Panel(new Rect(0, 0, 1280, 720));
            if (run.Transitioning)
            { GUI.Label(new Rect(360, 310, 600, 80), "Przygotowanie starcia…", title); return; }
            if (run.Paused)
            {
                GUI.Label(new Rect(350, 210, 600, 80), "Pauza", title);
                if(run.ReactorActive&&GUI.Button(new Rect(350,280,580,38),"Zakończ próbę obrony i wróć do menu",compactButton))run.RestartRun();
                if (GUI.Button(new Rect(350, 330, 580, 64), "Wznów walkę  [Esc]", button)) run.TogglePause();
                DrawMovementPause(run);DrawSuspensionPause(run);
                return;
            }
            if(progress.State==R1RunState.Salvage){DrawSalvage(run);return;}
            if(progress.State==R1RunState.Route){DrawRoute(run);return;}
            if (progress.State == R1RunState.Ready)
            {
                GUI.Label(new Rect(90, 55, 1100, 60), "ROBOROGUELIKE • PEŁNA INTEGRACJA", title);
                GUI.Label(new Rect(90, 125, 1100, 45), "Wybierz pojazd do Gauntletu", heading);
                GUI.Label(new Rect(90,173,1100,28),"Jeden pojazd, wspólny garaż i nagrody. Aktywność wybierzesz po przygotowaniu.",small);
                for(int i=0;i<3;i++)
                {
                    var kind=(R3VehicleKind)i;var design=R3VehicleCatalog.Create(kind);float x=80+i*380;
                    Panel(new Rect(x,205,360,280));
                    GUI.Label(new Rect(x+20,220,320,40),design.Name,heading);
                    GUI.Label(new Rect(x+20,270,320,90),design.Description,body);
                    GUI.Label(new Rect(x+20,365,320,45),$"Części: {design.Parts.Count}  •  Jazda: {design.Speed:0.0} m/s\nRdzeń: {design.CoreHp:0} HP",small);
                    if(GUI.Button(new Rect(x+20,420,320,48),run.SelectedVehicle==kind?"WYBRANY":"Wybierz",button))run.SelectVehicle(kind);
                }
                GUI.Label(new Rect(90,505,900,62),"6 starć • 3 areny • elity • Oblężnik. Wybieraj ryzyko i zachowuj sprawne moduły. Po walce: jeden moduł z trzech + do 3 części z wraków → garaż.",body);
                GUI.Label(new Rect(90,570,110,28),"Seed:",body);
                seedText=GUI.TextField(new Rect(200,568,230,28),seedText,11);
                if(int.TryParse(seedText,out int seed))run.RequestedSeed=seed;
                if (GUI.Button(new Rect(90,600,500,65), "Przygotuj pojazd  [Enter]", button)) run.PrepareUnifiedRun();
                if(GUI.Button(new Rect(640,600,550,65),"Garaż i poligon testowy",button))run.OpenSandboxGarage();
                GUI.Label(new Rect(640,554,550,38),"Trasy • obrona reaktora • generowana arena",small);
                GUI.Label(new Rect(90,675,1100,30),"WASD — jazda / hamowanie • Mysz — celowanie • LPM — ogień • Esc — pauza",small);
            }
            else if (progress.State == R1RunState.UpgradeChoice)
            {
                GUI.Label(new Rect(90, 90, 1120, 65), $"Zwycięstwo!  +{run.LastScrapReward} Złomu", title);
                GUI.Label(new Rect(90, 170, 1120, 55), "Wybierz moduł 1×2×2 i zamontuj go w garażu, aby aktywować bonus", heading);
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
                GUI.Label(new Rect(160, 130, 1050, 80), won ? "Gauntlet ukończony" : "Porażka", title);
                GUI.Label(new Rect(160, 235, 960, 70), won ? "Oblężnik pokonany. Wyprawa ukończona." : progress.DefeatReason, heading);
                GUI.Label(new Rect(160, 325, 960, 200), $"Walki: {progress.CompletedEncounters}/{progress.TotalEncounters}  •  Pojazd: {run.VehicleModel.Design.Name}\nPokonani: {run.DefeatedEnemies}  •  Zdobyty Złom: {run.EarnedScrap}  •  Saldo: {run.VehicleModel.Scrap}\nOdzyskane moduły: {run.RecoveredModules}  •  Części końcowe: {run.VehicleModel.Parts.FindAll(p=>p.Health>0).Count}\nZdobyte moduły bonusów: {progress.Acquired.Count}  •  Seed: {run.Seed}\nNowa próba przywróci bazowy pojazd, 0 Złomu i brak bonusów.", body);
                if (GUI.Button(new Rect(160, 535, 500, 70), won ? "Zagraj ponownie  [Enter]" : "Spróbuj ponownie  [Enter]", button)) run.RestartRun();
            }
        }

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
            GUI.Label(new Rect(30,453,280,60),garage.Tool==R41GarageTool.Mount?
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
            if(GUI.Button(new Rect(970,25,280,38),inventoryOpen?"← Warsztat":$"Magazyn ({model.Inventory.Count}) →",compactButton))
            {inventoryOpen=!inventoryOpen;garage.UseInventory=inventoryOpen;if(!inventoryOpen)garage.SelectedInventoryId=null;garage.RefreshMarker();}
            if(inventoryOpen)DrawInventory(run);
            else {
            GUI.Label(new Rect(970,70,280,65),$"Komórka: {garage.Cell}\n{PartName(selected?.Kind??garage.SelectedPart)} {(selected==null?"— nowa część":"")}",body);
            GUI.Label(new Rect(970,140,280,65),selected==null?$"Montaż: {model.Price(garage.SelectedPart,garage.SelectedWeapon)} Złomu":
                $"{(selected.Health<=0?"Utracony plan":selected.Health<0.999f?"Zamontowana • uszkodzona":"Zamontowana • sprawna")} • HP: {selected.Health*100:0}%\n{(selected.Health<=0?"Odtworzenie":"Naprawa")}: {model.RepairCost(selected)} Złomu",small);
            string error=garage.PlacementIssue;
            GUI.enabled=garage.Tool==R41GarageTool.Mount&&error==null;
            if(GUI.Button(new Rect(970,216,280,48),"Zamontuj w miejscu ghosta",compactButton))garage.ClickTarget();
            GUI.enabled=selected!=null&&model.RepairCost(selected)>0&&model.Scrap>=model.RepairCost(selected);
            if(GUI.Button(new Rect(970,275,280,48),selected!=null&&selected.Health<=0?"Odtwórz utraconą część":"Napraw wybraną część",button))
                garage.Message=run.GarageRepair(garage.Cell)?"Przywrócono HP części.":"Najpierw odtwórz połączenie z konstrukcją.";
            GUI.enabled=selected!=null;
            if(GUI.Button(new Rect(970,334,280,48),"Zdejmij / usuń utracony plan",compactButton))
                garage.Message=run.GarageRemove(garage.Cell)?"Żywa część wraca do magazynu; utracony plan usunięty.":"Nie można usunąć rdzenia, mostu lub ostatniego niezbędnego modułu.";
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
            int weaponPages=Mathf.Max(1,(R4WeaponDefinition.Count+2)/3);weaponPage=Mathf.Clamp(weaponPage,0,weaponPages-1);
            for(int slot=0;slot<3;slot++)
            {
                int w=weaponPage*3+slot;if(w>=R4WeaponDefinition.Count)break;
                string caption=w==0?"Działo":w==1?"Automat":w==2?"Ciężka":R4WeaponDefinition.Get((R4WeaponKind)w).ShortName;
                if(GUI.Button(new Rect(970+slot*94,535,90,40),caption,compactButton))
                {garage.SelectedWeapon=(R4WeaponKind)w;garage.SelectedPart=R3PartKind.Gun;partsPage=0;garage.SelectedInventoryId=null;garage.UseInventory=false;garage.SetTool(R41GarageTool.Mount);}
            }
            if(GUI.Button(new Rect(970,580,50,26),"‹",compactButton))weaponPage=(weaponPage+weaponPages-1)%weaponPages;
            GUI.Label(new Rect(1028,582,155,25),$"Broń {weaponPage+1}/{weaponPages}",small);
            if(GUI.Button(new Rect(1200,580,50,26),"›",compactButton))weaponPage=(weaponPage+1)%weaponPages;
            GUI.Label(new Rect(970,615,280,48),selected==null?$"{R4WeaponDefinition.Get(garage.SelectedWeapon).Name}\n{error??"Miejsce poprawne."}":
                $"{R4WeaponDefinition.PartName(selected)}\nDemontaż po zdjęciu: {model.DismantleValue(selected)} Z",small);
            }
            GUI.Label(new Rect(350,25,580,60),run.SandboxWorkshop?$"{model.Design.Name} • Warsztat poligonu":$"{model.Design.Name}  •  Następna walka {run.Progress.EncounterIndex+1}/{run.Progress.TotalEncounters}",heading);
            DrawReactorGarageAction(run);
            if(!run.ReactorPrepared&&!run.SandboxWorkshop){GUI.enabled=run.CanChooseGeneratedArena;
                if(GUI.Button(new Rect(355,80,280,38),"Generowana arena • seed "+run.RequestedSeed,compactButton))run.ChooseGeneratedArena();GUI.enabled=true;}
            DrawFullActivityGarageActions(run);
            if(GUI.Button(new Rect(355,538,125,44),"Obrót ←",compactButton))garage.Orbit(-25);
            if(GUI.Button(new Rect(485,538,125,44),"Obrót →",compactButton))garage.Orbit(25);
            if(GUI.Button(new Rect(615,538,125,44),"Widok ↑",compactButton))garage.Tilt(10);
            if(GUI.Button(new Rect(745,538,125,44),"Widok ↓",compactButton))garage.Tilt(-10);
            Panel(new Rect(345,590,585,115));
            GUI.Label(new Rect(360,600,555,55),garage.Message,small);
            GUI.enabled=model.CanDeploy;
            if(GUI.Button(new Rect(360,654,270,40),"Testuj na poligonie",compactButton))run.EnterTestRange();
            GUI.enabled=run.SandboxWorkshop||model.CanDeploy;
            if(GUI.Button(new Rect(645,654,270,40),run.ReactorPrepared?"Broń reaktora":run.SandboxWorkshop?"Menu główne":"Następna trasa",compactButton))
            {if(run.ReactorPrepared)run.ChooseReactorDefense();else if(run.SandboxWorkshop)run.RestartRun();else run.StartNextEncounter();}
            GUI.enabled=true;
            if(!model.CanDeploy)GUI.Label(new Rect(970,650,280,55),model.DeployError,small);
        }
        private static string PartName(R3PartKind kind) => kind==R3PartKind.Gun?"Broń":R4WeaponDefinition.PartName(kind);
    }
}
