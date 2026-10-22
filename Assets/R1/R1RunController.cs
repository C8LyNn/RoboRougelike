using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R5;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RoboRougelike.R1
{
    [DefaultExecutionOrder(1000)]
    public sealed partial class R1RunController : MonoBehaviour
    {
        public static R1RunController Instance { get; private set; }
        public R1RunProgress Progress { get; private set; }
        public R1VehicleState Player { get; private set; }
        public R1EncounterManager Encounter { get; private set; }
        public R1RunDefinition Definition { get; private set; }
        public bool Transitioning { get; private set; }
        public bool Paused { get; private set; }
        public string Error { get; private set; }
        public R3VehicleModel VehicleModel { get; private set; }
        public R1RunStats ActiveStats => Player?.GetComponent<R3MountedBonusEffects>()?.EffectiveStats??Progress?.Stats;
        public R3VehicleKind SelectedVehicle { get; private set; }
        public R3GarageView Garage { get; private set; }
        public int LastScrapReward { get; private set; }
        public R4SalvageLedger Salvage { get; private set; }
        public R5RunPlan Plan { get; private set; }
        public int RequestedSeed = 2026;
        public int Seed => Plan?.Seed ?? RequestedSeed;
        public R1EncounterDefinition[] RouteOffers => Plan?.Offers(Progress.EncounterIndex) ?? System.Array.Empty<R1EncounterDefinition>();
        public R1EncounterDefinition CurrentEncounter { get; private set; }
        public int DefeatedEnemies { get; private set; }
        public int EarnedScrap { get; private set; }
        public int RecoveredModules { get; private set; }
        public R5Arena Arena { get; private set; }
        private string runId;
        private R3VisualPalette palette;
        private P12PipelineSafeEnemyTurret enemyTemplate;
        private P12PipelineSafeCamera followCamera;
        private Vector3 playerSpawn;
        private Quaternion playerRotation;
        private int lastEscapeFrame=-1;

        private void Awake() { Instance = this; }

        private void Start()
        {
            var sourceDefinition = Resources.Load<R1RunDefinition>("R1/Run");
            Definition = sourceDefinition != null ? Instantiate(sourceDefinition) : null;
            gameObject.AddComponent<R1RunHUD>();
            try
            {
                var catalog=Resources.Load<TextAsset>("R5/Encounters");
                if(Definition==null||catalog==null)throw new System.ArgumentException("Brak konfiguracji wyprawy.");
                Definition.RouteStages=JsonUtility.FromJson<R5Catalog>(catalog.text)?.Stages;
                R5RunPlan.Validate(Definition.RouteStages);
                Definition.Encounters=Definition.RouteStages.Select(stage=>stage.Options[0]).ToArray();
            }
            catch(System.Exception problem)
            {Error="Niepoprawny katalog starć: "+problem.Message;Time.timeScale=0;return;}
            if (Definition == null || Definition.Encounters == null || Definition.Encounters.Length < 3 ||
                Definition.Encounters.Any(item => item == null) || Definition.Upgrades == null ||
                Definition.Upgrades.Where(item => item != null && !string.IsNullOrWhiteSpace(item.Id))
                    .Select(item => item.Id).Distinct().Count() < 3)
            { Error = "Brak poprawnych danych wyprawy R1."; Time.timeScale = 0; return; }
            Progress = new R1RunProgress(Definition.Encounters.Length,true);
            var sourcePlayer = FindFirstObjectByType<P12PipelineSafeTankController>();
            var sourceEnemy = FindFirstObjectByType<P12PipelineSafeEnemyTurret>();
            followCamera = FindFirstObjectByType<P12PipelineSafeCamera>();
            if (sourcePlayer == null || sourceEnemy == null || followCamera == null || followCamera.Pivot == null)
            { Error = "Scena nie zawiera kompletnego pojazdu, kamery lub przeciwnika."; Time.timeScale = 0; return; }
            Encounter = gameObject.AddComponent<R1EncounterManager>();
            var rig = sourcePlayer.gameObject.AddComponent<R1VehicleState>();
            rig.Bind(sourcePlayer, followCamera.Pivot);
            palette=gameObject.AddComponent<R3VisualPalette>();
            palette.Initialize(sourcePlayer.GetComponentInChildren<Renderer>().sharedMaterial);
            Arena=gameObject.AddComponent<R5Arena>();Arena.Initialize(sourcePlayer.GetComponentInChildren<Renderer>().sharedMaterial);
            Garage=gameObject.AddComponent<R3GarageView>(); Garage.Initialize(this,Camera.main,followCamera,palette);
            playerSpawn = sourcePlayer.transform.position; playerRotation = sourcePlayer.transform.rotation;
            var templates = new GameObject("Szablony pojazdów");
            templates.transform.SetParent(transform); templates.SetActive(false);
            enemyTemplate = Instantiate(sourceEnemy, templates.transform);
            enemyTemplate.gameObject.SetActive(false);
            sourcePlayer.gameObject.SetActive(false); sourceEnemy.gameObject.SetActive(false);
            Destroy(sourcePlayer.gameObject); Destroy(sourceEnemy.gameObject);
            SetSimulation(false);
            Debug.Log("[R5] Gotowość: sześć etapów, trasy, garaż i odzysk.");
        }

        public bool StartRun()
        {
            if (Error != null || Transitioning || Progress == null) return false;
            R5RunPlan candidate;
            try{candidate=new R5RunPlan(Definition.RouteStages,RequestedSeed);}
            catch(System.ArgumentException problem){Error=problem.Message;return false;}
            if(!Progress.Start())return false;
            VehicleModel=new R3VehicleModel(R3VehicleCatalog.Create(SelectedVehicle),Definition.GaragePrices);
            runId=System.Guid.NewGuid().ToString("N");
            Plan=candidate;
            DefeatedEnemies=EarnedScrap=RecoveredModules=LastScrapReward=0;
            SetSimulation(false);
            Debug.Log("[R5] Seed="+Seed+"; pojazd="+SelectedVehicle);
            return true;
        }

        public bool ChooseRoute(int index)
        {
            if(Transitioning||Progress?.State!=R1RunState.Route||index<0||index>=RouteOffers.Length)return false;
            var choice=RouteOffers[index];
            if(!Progress.ChooseRoute())return false;
            CurrentEncounter=choice;Definition.Encounters[Progress.EncounterIndex]=choice;
            StartCoroutine(BeginEncounter());return true;
        }

        public bool ChooseUpgrade(int index)
        {
            if (Transitioning || Progress == null) return false;
            if(Progress.State!=R1RunState.UpgradeChoice)return false;
            Player.GetComponent<R3VehicleAssembly>().Capture();
            var upgrade = Progress.ChooseUpgrade(index);
            if (upgrade == null) return false;
            if(!VehicleModel.ReceiveBonuses(new[]{upgrade}))
            {Debug.LogError("[R1] Nie udało się umieścić modułu bonusu w magazynie.");return false;}
            Debug.Log("[R1] Zdobyto fizyczny moduł bonusu (nieaktywny): " + upgrade.Title);
            Garage.Open();
            return true;
        }

        public bool SelectVehicle(R3VehicleKind kind)
        {
            if(Progress==null||Progress.State!=R1RunState.Ready||!System.Enum.IsDefined(typeof(R3VehicleKind),kind))return false;
            SelectedVehicle=kind;return true;
        }
        public bool StartNextEncounter()
        {
            if(SandboxWorkshop)return false;
            if(Transitioning||VehicleModel==null||!VehicleModel.CanDeploy||!Progress.LeaveGarage())return false;
            Garage.Close();SetSimulation(false);return true;
        }
        public bool GarageBuy(Vector3Int cell,R3PartKind kind) => EditGarage(()=>VehicleModel.Buy(cell,kind));
        public bool GarageBuyWeapon(Vector3Int cell,R4WeaponKind kind) => EditGarage(()=>VehicleModel.Buy(cell,R3PartKind.Gun,kind));
        public bool GarageMount(string id,Vector3Int cell) => EditGarage(()=>VehicleModel.Mount(id,cell));
        public bool GarageRepairInventory(string id) => EditGarage(()=>VehicleModel.RepairInventory(id));
        public bool GarageDismantle(string id)
        {
            if(VehicleModel==null)return false;
            int before=VehicleModel.Scrap;bool changed=EditGarage(()=>VehicleModel.Dismantle(id));
            if(changed)EarnedScrap+=VehicleModel.Scrap-before;return changed;
        }
        public bool ConfirmSalvage(System.Collections.Generic.IEnumerable<string> selected)
        {
            if(Transitioning||Progress?.State!=R1RunState.Salvage||Salvage==null)return false;
            var ids=selected?.ToArray();
            if(!Salvage.Claim(VehicleModel,ids))return false;
            RecoveredModules+=ids.Length;EarnedScrap+=Salvage.DismantledScrap;
            Progress.CompleteSalvage();Debug.Log("[R4] Odzysk rozliczony: magazyn="+VehicleModel.Inventory.Count+", Złom +"+Salvage.DismantledScrap);return true;
        }
        public bool ConfirmRewards(System.Collections.Generic.IEnumerable<string> selectedParts,
            System.Collections.Generic.IEnumerable<int> selectedBonuses)
        {
            if(Transitioning||Progress?.State!=R1RunState.Salvage||Salvage==null||VehicleModel==null)return false;
            var ids=selectedParts?.ToArray();var bonuses=selectedBonuses?.ToArray();
            if(ids==null||bonuses==null||ids.Length>3||
                ids.Distinct().Count()!=ids.Length||!Progress.CanCompleteUnifiedRewards(bonuses))return false;
            // Two separate reward pools: one bonus and up to three salvaged parts.
            // Reject before settling salvage so that inventory never receives partial rewards.
            if(VehicleModel.Inventory.Count+ids.Length+bonuses.Length>200)return false;
            var selectedUpgrades=bonuses.Select(index=>Progress.Offers[index]).ToArray();
            if(ids.Any(id=>!Salvage.Items.Any(item=>item.Id==id&&item.Selectable)))return false;
            if(!Salvage.Claim(VehicleModel,ids))return false;
            if(!VehicleModel.ReceiveBonuses(selectedUpgrades))
            {Debug.LogError("[R4] Niespójny magazyn fizycznych modułów bonusów.");return false;}
            RecoveredModules+=ids.Length;EarnedScrap+=Salvage.DismantledScrap;
            if(!Progress.CompleteUnifiedRewards(bonuses))
            {Debug.LogError("[R4] Niespójny stan nagród po zatwierdzeniu odzysku.");return false;}
            if(Progress.State==R1RunState.Garage)
            {
                // No stat bonus is applied here. Only a mounted, live 1x2x2 module works.
                Garage.Open();
            }
            Debug.Log("[R4] Nagrody: bonusy="+bonuses.Length+"; moduły="+ids.Length+
                "; złom z demontażu="+Salvage.DismantledScrap);
            return true;
        }
        public bool GarageRepair(Vector3Int cell) => EditGarage(()=>VehicleModel.Repair(cell));
        public bool GarageRemove(Vector3Int cell) => EditGarage(()=>VehicleModel.Remove(cell));
        public bool GarageRepairAffordable() => EditGarage(()=>VehicleModel.RepairAffordable()>0);
        public bool GarageEmergencyRepair() => EditGarage(()=>VehicleModel.EmergencyRepair());
        private bool EditGarage(System.Func<bool> action)
        {
            if(Transitioning||Progress?.State!=R1RunState.Garage||!action())return false;
            RebuildGarageVehicle();return true;
        }
        private void RebuildGarageVehicle()
        {
            // Garage edits must preserve the exact world frame used by the cursor and ghost.
            var position=Player!=null?Player.transform.position:playerSpawn;
            var rotation=Player!=null?Player.transform.rotation:playerRotation;
            if(Player!=null){Player.gameObject.SetActive(false);Destroy(Player.gameObject);}
            R1EncounterManager.ClearTransientObjects();
            Player=R3VehicleAssembly.Build(VehicleModel,Progress.Stats,palette,position,rotation);
            followCamera.Configure(Player.transform,Player.CameraPivot);
            Garage.RefreshPreview();
        }

        private IEnumerator BeginEncounter()
        {
            Transitioning = true; Paused = false; SetSimulation(false);
            Encounter.Clear();
            Arena.Build(CurrentEncounter.Arena);playerSpawn=Arena.PlayerSpawn;
            if(Player==null)Player=R3VehicleAssembly.Build(VehicleModel,Progress.Stats,palette,playerSpawn,playerRotation);
            else Player.MoveTo(playerSpawn,playerRotation);
            Player.Controller.SetGameplayInputEnabled(false);
            followCamera.Configure(Player.transform, Player.CameraPivot);
            // Start() builds the structure and initializes cloned module references while time is paused.
            yield return null;
            yield return null;
            // Preserve only modifiers from currently mounted, operational bonus modules.
            Player.Apply(ActiveStats??Progress.Stats);
            Salvage=new R4SalvageLedger(3);
            Encounter.Spawn(enemyTemplate, Player.transform, Definition.Encounters[Progress.EncounterIndex],Salvage,runId+"/encounter/"+Progress.EncounterIndex);
            yield return null;
            Transitioning = false; SetSimulation(true);
            Debug.Log("[R1] Starcie " + (Progress.EncounterIndex + 1) + ": przeciwników=" + Encounter.AliveCount);
        }

        private void Update()
        {
            if (Error != null || Progress == null || Transitioning) return;
            ReadMenuInput();
            if(Progress.State==R1RunState.TestDrive){if(!Transitioning)UpdateRange();return;}
            // Menu actions can begin a coroutine before the new vehicle has run Start().
            if (Transitioning || Progress.State != R1RunState.Combat || Paused) return;
            string defeat = Player == null ? "Pojazd został zniszczony." : Player.DefeatReason;
            // Resolve player loss first when both sides die in the same physics step.
            if (defeat != null)
            {
                DefeatedEnemies+=Encounter.Enemies.Count(enemy=>enemy!=null&&enemy.IsDefeated);
                Player?.GetComponent<R3VehicleAssembly>().Capture();
                Progress.Lose(defeat); SetSimulation(false);
                Debug.Log("[R1] Porażka: " + defeat);
            }
            else if (Encounter.Cleared)
            {
                Player.GetComponent<R3VehicleAssembly>().Capture();
                LastScrapReward=Definition.Encounters[Progress.EncounterIndex].ScrapReward;
                VehicleModel.Award(LastScrapReward);
                EarnedScrap+=LastScrapReward;DefeatedEnemies+=Encounter.Enemies.Count;
                Progress.WinEncounter(Definition.Upgrades, Plan.UpgradeRandom(Progress.EncounterIndex));
                Progress.OpenSalvage();
                SetSimulation(false);
                Encounter.Clear();
                Debug.Log("[R3] Wygrano starcie " + Progress.CompletedEncounters + "; Złom +"+LastScrapReward+"; stan=" + Progress.State);
            }
        }

        private void ReadMenuInput()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (Progress.State == R1RunState.Ready && keyboard.enterKey.wasPressedThisFrame) StartRun();
            else if (Progress.State == R1RunState.UpgradeChoice)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) ChooseUpgrade(0);
                else if (keyboard.digit2Key.wasPressedThisFrame) ChooseUpgrade(1);
                else if (keyboard.digit3Key.wasPressedThisFrame) ChooseUpgrade(2);
            }
            else if ((Progress.State == R1RunState.Combat||Progress.State==R1RunState.TestDrive) && keyboard.escapeKey.wasPressedThisFrame) HandleEscape();
            else if(Progress.State==R1RunState.TestDrive&&keyboard.rKey.wasPressedThisFrame)ResetTestRange();
            else if ((Progress.State == R1RunState.Won || Progress.State == R1RunState.Lost) &&
                keyboard.enterKey.wasPressedThisFrame) RestartRun();
#endif
        }

        public void TogglePause()
        {
            if (Transitioning || Progress == null || (Progress.State != R1RunState.Combat&&Progress.State!=R1RunState.TestDrive)||RangeDisabled) return;
            Paused = !Paused; SetSimulation(!Paused);
        }
        public void HandleEscape()
        {
            if(lastEscapeFrame==Time.frameCount)return;
            lastEscapeFrame=Time.frameCount;TogglePause();
        }

        private void SetSimulation(bool active)
        {
            Time.timeScale = active ? 1 : 0;
            if (Player != null) Player.Controller.SetGameplayInputEnabled(active);
            if (followCamera != null) followCamera.SetGameplayInputEnabled(active);
            if (Encounter != null) Encounter.SetCombatActive(active);
        }

        public void RestartRun()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(gameObject.scene.path);
        }

        private void OnDestroy()
        {
            if (Definition != null) Destroy(Definition);
            if (Instance == this) { Instance = null; Time.timeScale = 1; }
        }
    }
}
