using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;
using Object = UnityEngine.Object;

public class R1PlayIntegrationTests
{
    private Keyboard keyboard;
    private Mouse mouse;
    private InputSettings originalInputSettings;
    private InputSettings testInputSettings;
    private R1RunController Run => R1RunController.Instance;

    private static IEnumerator Until(Func<bool> condition, string description, float timeout = 15, bool automaticRewards = true)
    {
        if(R1RunController.Instance?.Progress?.State==R1RunState.Route)Assert.That(R1RunController.Instance.ChooseRoute(0),Is.True);
        float deadline = Time.realtimeSinceStartup + timeout;
        while (Time.realtimeSinceStartup < deadline)
        {
            // R4 adds a mandatory loot step. Legacy scenarios explicitly dismantle every offer.
            var run=R1RunController.Instance;
            if(condition())break;
            if(automaticRewards&&run?.Progress?.State==R1RunState.Salvage)Assert.That(RRLW2TestFlow.Claim(run,Array.Empty<string>()),Is.True);
            if(condition())break;yield return null;
        }
        Assert.That(condition(), Is.True, description);
    }
    private static IEnumerator Simulate(float seconds)
    {
        float until = Time.time + seconds;
        yield return Until(() => Time.time >= until, "Symulacja powinna postępować.", seconds + 10);
    }
    private void ConfigureVirtualInput()
    {
        originalInputSettings = InputSystem.settings;
        testInputSettings = Object.Instantiate(originalInputSettings);
        InputSystem.settings = testInputSettings;
        testInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        testInputSettings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();
    }
    [UnitySetUp]
    public IEnumerator EnterRealScene()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return Until(() => R1RunController.Instance != null && R1RunController.Instance.Progress != null,
            "Bootstrap powinien podłączyć wyprawę do istniejącej sceny.");
        Assert.That(Run.Error, Is.Null);
        Assert.That(Run.Progress.State, Is.EqualTo(R1RunState.Ready));
        Assert.That(Run.StartRun(), Is.True);
        yield return Until(() => !Run.Transitioning && Run.Player != null, "Pojazd powinien być gotowy.");
        Assert.That(Run.Player.Modules.ActiveWheelCount, Is.EqualTo(12));
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        if (originalInputSettings != null) InputSystem.settings = originalInputSettings;
        if (testInputSettings != null) Object.Destroy(testInputSettings);
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }
    private void DefeatAllEnemies()
    {
        foreach (var enemy in Run.Encounter.Enemies)
            enemy.Core.ApplyDamage(100000, enemy.Core.transform.position, Vector3.forward);
    }

    [UnityTest]
    public IEnumerator SixRealEncountersRewardsFinaleAndRestart()
    {
        float baseDamage=Run.Player.Controller.EffectiveDamage;
        for(int wave=0;wave<6;wave++)
        {
            Assert.That(Run.Encounter.AliveCount,Is.EqualTo(Run.CurrentEncounter.SpawnCount));
            Assert.That(Run.Encounter.Enemies[0].Core.MaxHp,Is.InRange(60,160));
            DefeatAllEnemies();
            yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Unified rewards",automaticRewards:false);
            if(wave==5){Assert.That(Run.ConfirmRewards(Array.Empty<string>(),Array.Empty<int>()),Is.True);break;}
            Assert.That(Run.Progress.Offers.Count,Is.EqualTo(3));
            Assert.That(Run.Progress.Offers.Select(x=>x.Id).Distinct().Count(),Is.EqualTo(3));
            Assert.That(Time.timeScale,Is.Zero);
            Assert.That(Run.ConfirmRewards(Array.Empty<string>(),new[]{0}),Is.True);
            Assert.That(Run.ConfirmRewards(Array.Empty<string>(),new[]{1}),Is.False,"No replay.");
            Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Garage));
            Assert.That(Run.StartNextEncounter(),Is.True);yield return Until(()=>!Run.Transitioning,"Next fight");
            Assert.That(Run.Player.Controller.EffectiveDamage,Is.EqualTo(baseDamage*Run.Progress.Stats.Damage).Within(.001));
            Assert.That(Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None),Is.Empty);
        }
        Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Won));Assert.That(Run.Progress.CompletedEncounters,Is.EqualTo(6));
        Assert.That(Run.Progress.Acquired.Count,Is.EqualTo(5));var old=Run;Run.RestartRun();
        yield return Until(()=>Run!=null&&Run!=old&&Run.Progress!=null,"Restart");
        Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Ready));Assert.That(Run.Progress.Acquired,Is.Empty);
    }

    [UnityTest]
    public IEnumerator KeyboardMovementMouseFireAndPauseUseRealController()
    {
        Run.Encounter.SetCombatActive(false);
        // Batch Editor tests have no focused Game View. Route virtual input to the game buffer.
        ConfigureVirtualInput();
        Vector3 before = Run.Player.transform.position;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        InputSystem.Update();
        yield return Simulate(0.8f);
        Assert.That(Vector3.Distance(before, Run.Player.transform.position), Is.GreaterThan(0.5f));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        yield return Until(() => Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None)
            .Any(shell => shell.name == "P17_PlayerPhysicalShell"), "LPM powinien wystrzelić fizyczny pocisk.");
        InputSystem.QueueStateEvent(mouse, new MouseState());
        Run.TogglePause();
        R1EncounterManager.ClearTransientObjects();
        yield return null;
        before = Run.Player.transform.position;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        float deadline = Time.realtimeSinceStartup + 0.4f;
        while (Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Run.Player.transform.position, Is.EqualTo(before));
        Assert.That(Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None), Is.Empty);
        Assert.That(Run.Player.Controller.GameplayInputEnabled, Is.False);
        Run.TogglePause();
        Assert.That(Time.timeScale, Is.EqualTo(1));
        Assert.That(Run.Player.Controller.GameplayInputEnabled, Is.True);
    }

    [UnityTest]
    public IEnumerator UpgradeDoesNotReviveDestroyedWheelAndPreservesDamageRatio()
    {
        Run.Encounter.SetCombatActive(false);
        var wheel = Run.Player.GetComponentsInChildren<P12PipelineSafeFunctionalModule>()
            .First(module => module.ModuleType == P12FunctionalModuleType.Wheel);
        var wheelPart = wheel.GetComponent<P12PipelineSafeDamageable>();
        wheelPart.ApplyDamage(10000, wheel.transform.position, Vector3.right);
        yield return Simulate(0.15f);
        Assert.That(Run.Player.Modules.ActiveWheelCount, Is.EqualTo(11));
        var part = Run.Player.GetComponentsInChildren<P12_PipelineSafePlayerBlock>()
            .First(block => !block.IsCore).GetComponent<P12PipelineSafeDamageable>();
        part.ApplyDamage(20, part.transform.position, Vector3.forward);
        Run.Progress.Stats.Apply(Run.Definition.Upgrades.First(item => item.Effect == R1UpgradeEffect.Hull));
        Run.Player.Apply(Run.Progress.Stats);
        yield return Simulate(0.1f);
        Assert.That(wheelPart.IsDead, Is.True);
        Assert.That(Run.Player.Modules.ActiveWheelCount, Is.EqualTo(11));
        Assert.That(part.MaxHp, Is.EqualTo(120).Within(0.001));
        Assert.That(part.CurrentHp, Is.EqualTo(96).Within(0.001));
    }

    [UnityTest]
    public IEnumerator PaidGarageRestoresConstructionAndKeepsEarnedDamageBonus()
    {
        var damage = Run.Definition.Upgrades.First(item => item.Effect == R1UpgradeEffect.Damage);
        var core = Run.Definition.Upgrades.First(item => item.Effect == R1UpgradeEffect.Core);
        var hull = Run.Definition.Upgrades.First(item => item.Effect == R1UpgradeEffect.Hull);
        Run.Definition.Upgrades = new[] { damage, core, hull };
        DefeatAllEnemies();
        yield return Until(() => Run.Progress.State == R1RunState.Salvage, "Pierwsza nagroda.");
        Run.ConfirmRewards(Array.Empty<string>(),new[]{Run.Progress.Offers.ToList().FindIndex(item => item.Effect == R1UpgradeEffect.Damage)});
        Run.StartNextEncounter();
        yield return Until(() => !Run.Transitioning, "Druga walka.");
        float upgradedDamage = Run.Player.Controller.EffectiveDamage;
        var wheel = Run.Player.GetComponentsInChildren<P12PipelineSafeFunctionalModule>()
            .First(module => module.ModuleType == P12FunctionalModuleType.Wheel);
        wheel.GetComponent<P12PipelineSafeDamageable>().ApplyDamage(10000, wheel.transform.position, Vector3.right);
        yield return Simulate(0.15f);
        Assert.That(Run.Player.Modules.ActiveWheelCount, Is.EqualTo(11));
        DefeatAllEnemies();
        yield return Until(() => Run.Progress.State == R1RunState.Salvage, "Druga nagroda.");
        var oldPlayer = Run.Player;
        Run.ConfirmRewards(Array.Empty<string>(),new[]{Run.Progress.Offers.ToList().FindIndex(item => item.Effect == R1UpgradeEffect.Hull)});
        Assert.That(Run.Player,Is.EqualTo(oldPlayer),"Nagroda sama nie odbudowuje konstrukcji.");
        Assert.That(Run.Player.Modules.ActiveWheelCount,Is.EqualTo(11));
        int scrapBefore=Run.VehicleModel.Scrap;
        Assert.That(Run.GarageRepairAffordable(),Is.True);
        Assert.That(Run.VehicleModel.Scrap,Is.LessThan(scrapBefore));
        Run.StartNextEncounter();
        yield return Until(() => !Run.Transitioning, "Naprawa i trzecia walka.");
        Assert.That(Run.Player, Is.Not.EqualTo(oldPlayer));
        Assert.That(Run.Player.Modules.ActiveWheelCount, Is.EqualTo(12));
        Assert.That(Run.Player.Modules.WeaponOnline, Is.True);
        Assert.That(Run.Player.Structure.ConnectedBlockCount, Is.EqualTo(Run.VehicleModel.Parts.Count));
        Assert.That(Run.Player.Controller.EffectiveDamage, Is.EqualTo(upgradedDamage));
        Assert.That(Run.Player.GetComponentsInChildren<P12PipelineSafeDamageable>()
            .All(part => !part.IsDead && Mathf.Approximately(part.CurrentHp, part.MaxHp)), Is.True);
    }

    [UnityTest]
    public IEnumerator DestroyedPlayerCoreEndsRunAndRestartClearsState()
    {
        var core = Run.Player.GetComponentsInChildren<P12_PipelineSafePlayerBlock>().Single(block => block.IsCore);
        core.GetComponent<P12PipelineSafeDamageable>().ApplyDamage(10000, core.transform.position, Vector3.forward);
        yield return Until(() => Run.Progress.State == R1RunState.Lost, "Utrata rdzenia powinna zakończyć wyprawę.");
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(Run.Player.Controller.GameplayInputEnabled, Is.False);
        var old = Run; Run.RestartRun();
        yield return Until(() => Run != null && Run != old && Run.Progress != null, "Restart po porażce.");
        Assert.That(Run.Progress.CompletedEncounters, Is.Zero);
        Assert.That(Run.Progress.Acquired, Is.Empty);
    }

    [UnityTest]
    public IEnumerator CoreProtectionReloadAndMobilityReachActualVehicleComponents()
    {
        Run.Encounter.SetCombatActive(false);
        var tank = Run.Player.Controller;
        var core = Run.Player.GetComponentsInChildren<P12_PipelineSafePlayerBlock>()
            .Single(block => block.IsCore).GetComponent<P12PipelineSafeDamageable>();
        float coreHp = core.MaxHp, interval = tank.EffectiveFireInterval, speed = tank.EffectiveForwardSpeed;
        foreach (var effect in new[] { R1UpgradeEffect.Core, R1UpgradeEffect.Reload, R1UpgradeEffect.Mobility })
            Run.Progress.Stats.Apply(Run.Definition.Upgrades.Single(item => item.Effect == effect));
        Run.Player.Apply(Run.Progress.Stats);
        Assert.That(core.MaxHp, Is.EqualTo(coreHp * 1.5f).Within(0.001));
        Assert.That(tank.EffectiveFireInterval, Is.EqualTo(interval * 0.85f).Within(0.001));
        Assert.That(tank.EffectiveForwardSpeed, Is.EqualTo(speed * 1.15f).Within(0.001));
        yield return null;
    }

    [UnityTest]
    public IEnumerator KeyboardEnterStartsRunWithoutPrematureDefeat()
    {
        var old = Run; Run.RestartRun();
        yield return Until(() => Run != null && Run != old && Run.Progress != null, "Nowy ekran startowy.");
        ConfigureVirtualInput();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
        yield return Until(() => Run.Progress.State != R1RunState.Ready, "Enter powinien rozpocząć wyprawę.");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return Until(() => !Run.Transitioning, "Inicjalizacja po klawiszu Enter.");
        Assert.That(Run.Progress.State, Is.EqualTo(R1RunState.Combat));
        Assert.That(Run.Player.Structure.CoreAlive, Is.True);
    }

    [UnityTest]
    public IEnumerator KeyboardUpgradeChoiceWaitsForNewVehicleInitialization()
    {
        ConfigureVirtualInput();
        Run.Definition.Upgrades=new[]{Run.Definition.Upgrades.Single(x=>x.Effect==R1UpgradeEffect.Core),Run.Definition.Upgrades.Single(x=>x.Effect==R1UpgradeEffect.Hull),Run.Definition.Upgrades.Single(x=>x.Effect==R1UpgradeEffect.Damage)};
        DefeatAllEnemies();yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Reward screen",automaticRewards:false);
        int choice=Run.Progress.Offers.ToList().FindIndex(x=>x.Effect==R1UpgradeEffect.Core);
        var key=choice==0?Key.Digit1:choice==1?Key.Digit2:Key.Digit3;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Salvage),"Selection alone does not spend/confirm.");
        Assert.That(Run.Progress.Acquired,Is.Empty);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));
        yield return Until(()=>Run.Progress.State==R1RunState.Garage,"Confirm unified keyboard rewards",5,automaticRewards:false);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        Assert.That(Run.Progress.Acquired.Count,Is.EqualTo(1));Assert.That(Run.Player.Modules.ActiveWheelCount,Is.EqualTo(12));
        Assert.That(Run.StartNextEncounter(),Is.True);yield return Until(()=>!Run.Transitioning,"Combat initialization");
        Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Combat));
    }

    [UnityTest]
    public IEnumerator LosingPlayerWeaponEndsRunWhileCoreIsAlive()
    {
        var weapon = Run.Player.GetComponentsInChildren<P12PipelineSafeFunctionalModule>()
            .Single(module => module.ModuleType == P12FunctionalModuleType.Weapon);
        weapon.GetComponent<P12PipelineSafeDamageable>().ApplyDamage(10000, weapon.transform.position, Vector3.right);
        yield return Until(() => Run.Progress.State == R1RunState.Lost, "Utrata broni powinna zakończyć próbę.");
        Assert.That(Run.Player.Structure.CoreAlive, Is.True);
        Assert.That(Run.Player.Modules.WeaponOnline, Is.False);
    }

    [UnityTest]
    public IEnumerator LosingEveryWheelEndsRunWithoutWaitingForCoreDestruction()
    {
        foreach (var wheel in Run.Player.GetComponentsInChildren<P12PipelineSafeFunctionalModule>()
            .Where(module => module.ModuleType == P12FunctionalModuleType.Wheel))
            wheel.GetComponent<P12PipelineSafeDamageable>().ApplyDamage(10000, wheel.transform.position, Vector3.right);
        yield return Until(() => Run.Progress.State == R1RunState.Lost, "Bez mobilności walka nie powinna utknąć.");
        Assert.That(Run.Player.Structure.CoreAlive, Is.True);
        Assert.That(Run.Player.Modules.ActiveWheelCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator DestroyingEnemyBarrelStopsFireButOnlyCoreDestructionWins()
    {
        var enemy = Run.Encounter.Enemies.First();
        foreach(var escort in Run.Encounter.Enemies.Where(e=>e!=enemy))escort.SetCombatActive(false);
        // The original scene blueprint has a central turret block, barrel block, then muzzle.
        var barrel = enemy.transform.Find("Enemy_Yaw").GetChild(1).GetComponent<P12PipelineSafeDamageable>();
        Assert.That(barrel, Is.Not.Null);
        barrel.ApplyDamage(10000, barrel.transform.position, Vector3.forward);
        R1EncounterManager.ClearTransientObjects();
        yield return Simulate(2);
        Assert.That(Run.Progress.State, Is.EqualTo(R1RunState.Combat), "Rozbrojenie nie zastępuje zniszczenia rdzenia.");
        Assert.That(enemy.Core.IsDead, Is.False);
        Assert.That(enemy.IsDefeated, Is.False);
        Assert.That(Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None)
            .Any(shell => shell.name == "P17_EnemyPhysicalShell"), Is.False);
        DefeatAllEnemies();
        yield return Until(() => Run.Progress.State == R1RunState.Garage, "Zniszczenie rdzenia kończy starcie.");
        Assert.That(Run.Encounter.Enemies, Is.Empty, "Pozostałości przeciwnika sprzątamy już na ekranie nagród.");
    }

    [UnityTest]
    public IEnumerator EnemyMovesFiresAndStopsDuringPause()
    {
        var enemy = Run.Encounter.Enemies.First();
        var before = enemy.transform.position;
        yield return Until(() => Vector3.Distance(before, enemy.transform.position) > 1,
            "Przeciwnik ma aktywnie poruszać się po arenie.");
        yield return Until(() => Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None)
            .Any(shell => shell.name == "P17_EnemyPhysicalShell"), "Przeciwnik ma strzelać fizycznymi pociskami.");
        Run.TogglePause();
        before = enemy.transform.position;
        float until = Time.realtimeSinceStartup + 0.3f;
        while (Time.realtimeSinceStartup < until) yield return null;
        Assert.That(enemy.transform.position, Is.EqualTo(before));
        Run.TogglePause();
        Assert.That(Run.Progress.State, Is.EqualTo(R1RunState.Combat));
    }

    [UnityTest]
    public IEnumerator CoreLossCanEndEachStageAndFreshAttemptHasNoBonuses()
    {
        for (int stage = 0; stage < 6; stage++)
        {
            for (int victory = 0; victory < stage; victory++)
            {
                DefeatAllEnemies();
                yield return Until(() => Run.Progress.State == R1RunState.Garage, "Nagroda przed kolejnym etapem.");

                Run.GarageRepairAffordable();
                if(!Run.VehicleModel.CanDeploy)Run.GarageEmergencyRepair();
                Run.StartNextEncounter();
                yield return Until(() => !Run.Transitioning, "Gotowość kolejnej walki.");
            }
            var core = Run.Player.GetComponentsInChildren<P12_PipelineSafePlayerBlock>().Single(block => block.IsCore);
            core.GetComponent<P12PipelineSafeDamageable>().ApplyDamage(10000, core.transform.position, Vector3.up);
            yield return Until(() => Run.Progress.State == R1RunState.Lost, "Porażka na każdym etapie.");
            Assert.That(Run.Progress.EncounterIndex, Is.EqualTo(stage));
            var old = Run; Run.RestartRun();
            yield return Until(() => Run != null && Run != old && Run.Progress != null, "Czysty restart po porażce.");
            Assert.That(Run.Progress.Acquired, Is.Empty);
            Run.StartRun();
            yield return Until(() => !Run.Transitioning, "Start nowej próby.");
            Assert.That(Run.Progress.Stats.Damage, Is.EqualTo(1));
            Assert.That(Run.Player.Modules.ActiveWheelCount, Is.EqualTo(12));
        }
    }

    [UnityTest,Timeout(720000)]
    public IEnumerator SixEncountersCanBeWonWithActualPlayerGunAndPhysicalHits()
    {
        ConfigureVirtualInput();
        var follow = Object.FindFirstObjectByType<P12PipelineSafeCamera>();
        follow.enabled = false;
        var camera = Camera.main;
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        for (int stage = 0; stage < 6; stage++)
        {
            P12PipelineSafeEnemyTurret enemy = null;
            Rigidbody body = null;
            Vector3 previous = Vector3.zero, velocity = Vector3.zero;
            float deadline = Time.realtimeSinceStartup + 110;
            while (Run.Progress.State == R1RunState.Combat && Time.realtimeSinceStartup < deadline)
            {
                // Test pilot controls only camera aim and LMB. No damage API, HP edits or disabled AI.
                if(enemy==null||enemy.IsDefeated)
                {
                    enemy=Run.Encounter.Enemies.Where(e=>e!=null&&!e.IsDefeated)
                        .OrderBy(e=>Vector3.Distance(e.transform.position,Run.Player.transform.position)).FirstOrDefault();
                    if(enemy!=null){body=enemy.GetComponent<Rigidbody>();previous=body.position;velocity=Vector3.zero;}
                }
                if (enemy != null && enemy.Core != null)
                {
                    Vector3 position = body.position;
                    if (Time.deltaTime > 0) velocity = Vector3.Lerp(velocity,
                        (position - previous) / Time.deltaTime, 0.25f);
                    previous = position;
                    Vector3 muzzle = Run.Player.transform.Find("Turret_Yaw/Cannon_Pitch/Muzzle").position;
                    float flight = Vector3.Distance(muzzle, enemy.Core.transform.position) / 34;
                    Vector3 target = enemy.Core.transform.position + velocity * flight;
                    camera.transform.position = Run.Player.transform.position + new Vector3(0, 3, -6);
                    camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position);
                }
                yield return null;
            }
            if(Run.Progress.State==R1RunState.Salvage)Assert.That(RRLW2TestFlow.Claim(Run,Array.Empty<string>()),Is.True);
            Assert.That(Run.Progress.State, Is.EqualTo(stage == 5 ? R1RunState.Won : R1RunState.Garage),
                "Fizyczna broń powinna pozwalać wygrać pojedynek " + (stage + 1));
            if (stage == 5) break;

            Run.GarageRepairAffordable();
            if(!Run.VehicleModel.CanDeploy)Run.GarageEmergencyRepair();
            Run.StartNextEncounter();
            yield return Until(() => !Run.Transitioning, "Następny pojedynek po prawdziwym trafieniu.");
            follow.enabled=false;
        }
        InputSystem.QueueStateEvent(mouse, new MouseState());
        Assert.That(Run.Progress.CompletedEncounters, Is.EqualTo(6));
    }

    [UnityTest]
    public IEnumerator ActualEnemyUsesDesignatedCoreAndDetachesSeveredBaseCorner()
    {
        Run.Encounter.SetCombatActive(false);
        var enemy = Run.Encounter.Enemies.First();
        var structure = enemy.GetComponent<P12PipelineSafeVehicleStructure>();
        Assert.That(structure.ConnectedBlockCount, Is.EqualTo(22));
        var parts = enemy.GetComponentsInChildren<P12_PipelineSafePlayerBlock>();
        Assert.That(parts.Single(part => part.IsCore).gameObject, Is.EqualTo(enemy.Core.gameObject));
        var corner = parts.Single(part => part.LogicalGridPosition == new Vector3Int(-1, 0, -1));
        foreach (var position in new[] { new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, -1), new Vector3Int(-1, -1, -1) })
        {
            var part = parts.Single(item => item.LogicalGridPosition == position);
            part.GetComponent<P12PipelineSafeDamageable>().ApplyDamage(1000, part.transform.position, Vector3.up);
        }
        structure.EvaluateStructure();
        Assert.That(corner.DetachedFromVehicle, Is.True);
        Assert.That(corner.transform.IsChildOf(enemy.transform), Is.False);
        Assert.That(corner.GetComponentInParent<Rigidbody>(), Is.Not.Null);
        Assert.That(structure.CoreAlive, Is.True);
        Assert.That(structure.ConnectedBlockCount, Is.EqualTo(17));
        yield return null;
    }
}
