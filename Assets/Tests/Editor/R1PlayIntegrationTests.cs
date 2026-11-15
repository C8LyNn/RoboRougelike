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

// Test-only navigation; production driving and aim are not altered.
internal static class R1PhysicalPilotRouting
{
    internal static Vector3 NextWaypoint(Vector3 start, Vector3 target, Bounds obstacle)
    {
        // Four corners outside the hall form a tiny visibility graph. The
        // vehicle receives only a waypoint and still moves via normal input.
        var safe=obstacle;safe.Expand(new Vector3(1f,0f,1f));
        const float clearance=2f;
        float left=safe.min.x-clearance,right=safe.max.x+clearance;
        float near=safe.min.z-clearance,far=safe.max.z+clearance;
        var nodes=new[]{new Vector3(start.x,0,start.z),new Vector3(target.x,0,target.z),
            new Vector3(left,0,near),new Vector3(left,0,far),new Vector3(right,0,near),new Vector3(right,0,far)};
        var distance=Enumerable.Repeat(float.PositiveInfinity,nodes.Length).ToArray();
        var previous=Enumerable.Repeat(-1,nodes.Length).ToArray();var visited=new bool[nodes.Length];
        distance[0]=0;
        for(int iteration=0;iteration<nodes.Length;iteration++)
        {
            int best=-1;
            for(int i=0;i<nodes.Length;i++)if(!visited[i]&&(best<0||distance[i]<distance[best]))best=i;
            if(best<0||float.IsPositiveInfinity(distance[best]))break;
            if(best==1)break;
            visited[best]=true;
            for(int next=0;next<nodes.Length;next++)
            {
                if(next==best||visited[next]||(next>=2&&Vector3.Distance(nodes[0],nodes[next])<1f)||Crosses(nodes[best],nodes[next],safe))continue;
                float candidate=distance[best]+Vector3.Distance(nodes[best],nodes[next]);
                if(candidate<distance[next]){distance[next]=candidate;previous[next]=best;}
            }
        }
        if(previous[1]<0)return target; // The vehicle or destination is already inside the obstacle.
        int waypoint=1;
        while(previous[waypoint]>0)waypoint=previous[waypoint];
        var result=nodes[waypoint];result.y=start.y;return result;
    }
    private static bool Crosses(Vector3 start,Vector3 end,Bounds bounds)
    {
        float enter=0,exit=1;
        if(!Clip(start.x,end.x-start.x,bounds.min.x,bounds.max.x,ref enter,ref exit)||
           !Clip(start.z,end.z-start.z,bounds.min.z,bounds.max.z,ref enter,ref exit))return false;
        return enter<.99999f&&exit>.00001f;
    }
    private static bool Clip(float origin,float delta,float min,float max,ref float enter,ref float exit)
    {
        if(Mathf.Abs(delta)<.00001f)return origin>=min&&origin<=max;
        float a=(min-origin)/delta,b=(max-origin)/delta;
        if(a>b){float swap=a;a=b;b=swap;}
        enter=Mathf.Max(enter,a);exit=Mathf.Min(exit,b);return enter<=exit;
    }
}

public sealed class R1PhysicalPilotRoutingTests
{
    [TestCase(-13.5f,0f,-13.2f,18f)]
    [TestCase(-13.2f,18f,-13.5f,0f)]
    [TestCase(-28f,7f,-8f,7f)]
    [TestCase(-8f,7f,-28f,7f)]
    public void PilotCanRouteAroundIndustrialHallWithoutCrossingIt(float sx,float sz,float tx,float tz)
    {
        var obstacle=new Bounds(new Vector3(-17,2.5f,7),new Vector3(10,5,9));
        var current=new Vector3(sx,2.5f,sz);var destination=new Vector3(tx,2.5f,tz);
        for(int i=0;i<4&&Vector3.Distance(current,destination)>.01f;i++)
        {
            var next=R1PhysicalPilotRouting.NextWaypoint(current,destination,obstacle);
            var delta=next-current;
            Assert.That(delta.magnitude,Is.GreaterThan(.01f),"The route must make progress.");
            Assert.That(obstacle.IntersectRay(new Ray(current,delta.normalized),out float hit)&&hit<delta.magnitude-.001f,Is.False,
                "The next leg must not pass through the actual hall.");
            current=next;
        }
        Assert.That(Vector3.Distance(current,destination),Is.LessThan(.01f),"The route must reach the opponent side.");
    }
}

public class R1PlayIntegrationTests
{
    private Keyboard keyboard;
    private Mouse mouse;
    private InputSettings originalInputSettings;
    private InputSettings testInputSettings;
    private string pilotTechnologyPath;
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
        int expectedWheels=Run.VehicleModel.Parts.Count(p=>p.Kind==R3PartKind.Wheel&&p.Health>0);
        Assert.That(expectedWheels,Is.GreaterThan(0));
        Assert.That(Run.Player.Modules.ActiveWheelCount,Is.EqualTo(expectedWheels),"The chosen starter's physical wheels must match its model.");
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        if (originalInputSettings != null) InputSystem.settings = originalInputSettings;
        if (testInputSettings != null) Object.Destroy(testInputSettings);
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
        if (!string.IsNullOrEmpty(pilotTechnologyPath) && System.IO.File.Exists(pilotTechnologyPath)) System.IO.File.Delete(pilotTechnologyPath);
    }
    private void DefeatAllEnemies()
    {
        foreach (var enemy in Run.Encounter.Enemies)
            enemy.Core.ApplyDamage(100000, enemy.Core.transform.position, Vector3.forward);
    }

    // Use the same garage transactions as a player between physical fights.
    // An unmounted reward has no effect, and buying must use earned currency.
    private void PreparePhysicalPilotVehicle()
    {
        var model=Run.VehicleModel;
        Run.GarageRepairAffordable();
        if(!model.CanDeploy)Run.GarageEmergencyRepair();
        foreach(var bonus in model.Inventory.Where(p=>p.Kind==R3PartKind.BonusModule&&p.Health>0).ToArray())
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .OrderBy(c=>c.y).ThenBy(c=>Math.Abs(c.x)+Math.Abs(c.z));
            foreach(var cell in cells)
                if(model.OwnedPlacementError(bonus.Id,cell,default)==null&&Run.GarageMount(bonus.Id,cell))break;
        }
        // Spend earned currency on independent copies when mounting and the
        // construction budget permit it. Never grant currency or alter damage.
        while(model.Parts.Count(p=>p.Kind==R3PartKind.Gun&&p.Health>0)<6)
        {
            var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .Where(c=>model.PlacementError(c,R3PartKind.Gun,false)==null)
                .OrderByDescending(c=>c.y).ThenBy(c=>Math.Abs(c.x)+Math.Abs(c.z)).ToArray();
            bool bought=false;
            foreach(var cell in cells)if(Run.GarageBuyWeapon(cell,RoboRougelike.R4.R4WeaponKind.Basic)){bought=true;break;}
            if(!bought)break;
        }
        Debug.Log($"[R1_PHYSICAL_PILOT_GARAGE] guns={model.Parts.Count(p=>p.Kind==R3PartKind.Gun&&p.Health>0)} bonuses={model.Parts.Count(p=>p.Kind==R3PartKind.BonusModule&&p.Health>0)} scrap={model.Scrap}");
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
            if(wave==5){Assert.That(Run.Progress.Offers.Count,Is.EqualTo(3),$"Wave {wave+1}, state {Run.Progress.State}, completed {Run.Progress.CompletedEncounters}");Assert.That(Run.ConfirmRewards(Array.Empty<string>(),new[]{0}),Is.True);break;}
            Assert.That(Run.Progress.Offers.Count,Is.EqualTo(3),$"Wave {wave+1}, state {Run.Progress.State}, completed {Run.Progress.CompletedEncounters}");
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
        Assert.That(Run.Progress.Acquired.Count,Is.EqualTo(6));var old=Run;Run.RestartRun();
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
        int initialWheels=Run.Player.Modules.ActiveWheelCount;
        var wheelPart = wheel.GetComponent<P12PipelineSafeDamageable>();
        wheelPart.ApplyDamageResolvedTyped(wheelPart.CurrentHp/.9f+.01f,wheel.transform.position,Vector3.right,R3DamagePropagation.DamageKind.Piercing);
        yield return Simulate(0.15f);
        Assert.That(Run.Player.Modules.ActiveWheelCount, Is.EqualTo(initialWheels-1));
        var part = Run.Player.GetComponentsInChildren<P12_PipelineSafePlayerBlock>()
            .First(block => !block.IsCore).GetComponent<P12PipelineSafeDamageable>();
        part.ApplyDamage(20, part.transform.position, Vector3.forward);
        float originalMax=part.MaxHp,originalRatio=part.CurrentHp/part.MaxHp;
        var hull=Run.Definition.Upgrades.First(item=>item.Effect==R1UpgradeEffect.Hull&&item.Scope==R1BonusScope.Global&&item.Operation==R1ModifierOperation.Increased);
        Run.Progress.Stats.Apply(hull);
        Run.Player.Apply(Run.Progress.Stats);
        yield return Simulate(0.1f);
        Assert.That(wheelPart.IsDead, Is.True);
        Assert.That(Run.Player.Modules.ActiveWheelCount, Is.EqualTo(initialWheels-1));
        Assert.That(part.MaxHp, Is.EqualTo(originalMax*(1+hull.Amount)).Within(0.001));
        Assert.That(part.CurrentHp/part.MaxHp, Is.EqualTo(originalRatio).Within(0.001));
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
        var bonusId=Run.VehicleModel.Inventory.Single(p=>p.Kind==R3PartKind.BonusModule).Id;
        var bonusCell=Run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(cell=>Run.VehicleModel.OwnedPlacementError(bonusId,cell,Vector3Int.zero)==null);
        Assert.That(Run.GarageMount(bonusId,bonusCell),Is.True,Run.VehicleModel.OwnedPlacementError(bonusId,bonusCell,Vector3Int.zero));
        Run.StartNextEncounter();
        yield return Until(() => !Run.Transitioning, "Druga walka.");
        float upgradedDamage = Run.Player.Controller.EffectiveDamage;
        var wheel = Run.Player.GetComponentsInChildren<P12PipelineSafeFunctionalModule>()
            .First(module => module.ModuleType == P12FunctionalModuleType.Wheel);
        int initialWheels=Run.Player.Modules.ActiveWheelCount;string wheelId=wheel.GetComponent<R3MountedPart>().Id;
        var wheelHp=wheel.GetComponent<P12PipelineSafeDamageable>();
        wheelHp.ApplyDamageResolvedTyped(wheelHp.CurrentHp/.9f+.01f,wheel.transform.position,Vector3.right,R3DamagePropagation.DamageKind.Piercing);
        yield return Simulate(0.15f);
        Assert.That(Run.Player.Modules.ActiveWheelCount, Is.EqualTo(initialWheels-1));
        DefeatAllEnemies();
        yield return Until(() => Run.Progress.State == R1RunState.Salvage, "Druga nagroda.");
        var oldPlayer = Run.Player;
        Assert.That(Run.ConfirmRewards(Array.Empty<string>(),new[]{Run.Progress.Offers.ToList().FindIndex(item => item.Effect == R1UpgradeEffect.Hull)}),Is.True);
        Assert.That(Run.VehicleModel.Parts.Single(p=>p.Id==wheelId).Health,Is.Zero,"The safe zone must not revive a destroyed wheel.");
        Assert.That(Run.Player.Modules.ActiveWheelCount,Is.EqualTo(initialWheels-1));
        int scrapBefore=Run.VehicleModel.Scrap;
        Assert.That(Run.GarageRepairAffordable(),Is.True);
        Assert.That(Run.VehicleModel.Scrap,Is.LessThan(scrapBefore));
        Run.StartNextEncounter();
        yield return Until(() => !Run.Transitioning, "Naprawa i trzecia walka.");
        Assert.That(Run.Player, Is.Not.EqualTo(oldPlayer));
        Assert.That(Run.Player.Modules.ActiveWheelCount, Is.EqualTo(initialWheels));
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
        var coreBonus=Run.Definition.Upgrades.First(item=>item.Effect==R1UpgradeEffect.Core&&item.Scope==R1BonusScope.Global&&item.Operation==R1ModifierOperation.Increased);
        var reloadBonus=Run.Definition.Upgrades.First(item=>item.Effect==R1UpgradeEffect.Reload&&item.Scope==R1BonusScope.Global&&item.Operation==R1ModifierOperation.Increased);
        var mobilityBonus=Run.Definition.Upgrades.First(item=>item.Effect==R1UpgradeEffect.Mobility&&item.Scope==R1BonusScope.Global&&item.Operation==R1ModifierOperation.Increased);
        foreach(var bonus in new[]{coreBonus,reloadBonus,mobilityBonus})Run.Progress.Stats.Apply(bonus);
        Run.Player.Apply(Run.Progress.Stats);
        Assert.That(core.MaxHp, Is.EqualTo(coreHp*(1+coreBonus.Amount)).Within(0.001));
        Assert.That(tank.EffectiveFireInterval, Is.EqualTo(interval*(1-reloadBonus.Amount)).Within(0.001));
        Assert.That(tank.EffectiveForwardSpeed, Is.EqualTo(speed*(1+mobilityBonus.Amount)).Within(0.001));
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
        Assert.That(Run.Progress.State, Is.EqualTo(R1RunState.Garage), "RC2 prepares the canonical garage before activity choice.");
        Assert.That(Run.Player.Structure.CoreAlive, Is.True);
        Assert.That(Run.Encounter.AliveCount, Is.Zero, "Enter alone must not start an unchosen encounter.");
        var preparedModel = Run.VehicleModel;
        var preparedIds = preparedModel.Parts.Select(part => part.Id).ToArray();
        Assert.That(Run.StartNextEncounter(), Is.True);
        Assert.That(Run.ChooseRoute(0), Is.True);
        yield return Until(() => !Run.Transitioning, "Chosen encounter initializes after preparation.");
        Assert.That(Run.Progress.State, Is.EqualTo(R1RunState.Combat));
        Assert.That(Run.Player.Structure.CoreAlive, Is.True);
        Assert.That(Run.VehicleModel, Is.SameAs(preparedModel));
        Assert.That(Run.VehicleModel.Parts.Select(part => part.Id), Is.EqualTo(preparedIds));
    }

    [UnityTest]
    public IEnumerator KeyboardUpgradeChoiceWaitsForNewVehicleInitialization()
    {
        ConfigureVirtualInput();
        Run.Definition.Upgrades=new[]{Run.Definition.Upgrades.Single(x=>x.Id=="core"),Run.Definition.Upgrades.Single(x=>x.Id=="armor"),Run.Definition.Upgrades.Single(x=>x.Id=="damage")};
        DefeatAllEnemies();yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Reward screen",automaticRewards:false);
        int choice=Run.Progress.Offers.ToList().FindIndex(x=>x.Effect==R1UpgradeEffect.Core);
        var key=choice==0?Key.Digit1:choice==1?Key.Digit2:Key.Digit3;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Salvage),"Selection alone does not spend/confirm.");
        Assert.That(Run.Progress.Acquired,Is.Empty);
        // The unified reward screen also requires a basket when offers exist.
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q));yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));
        yield return Until(()=>Run.Progress.State==R1RunState.Garage,"Confirm unified keyboard rewards",5,automaticRewards:false);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        Assert.That(Run.Progress.Acquired.Count,Is.EqualTo(1));Assert.That(Run.Player.Modules.ActiveWheelCount,Is.EqualTo(Run.VehicleModel.Parts.Count(p=>p.Kind==R3PartKind.Wheel&&p.Health>0)));
        Assert.That(Run.StartNextEncounter(),Is.True);Assert.That(Run.ChooseRoute(0),Is.True);yield return Until(()=>!Run.Transitioning,"Combat initialization");
        Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Combat));
    }

    [UnityTest]
    public IEnumerator LosingPlayerWeaponEndsRunWhileCoreIsAlive()
    {
        var weapons = Run.Player.GetComponentsInChildren<P12PipelineSafeFunctionalModule>()
            .Where(module => module.ModuleType == P12FunctionalModuleType.Weapon).ToArray();
        Assert.That(weapons.Length,Is.EqualTo(2),"The standard starter has two real guns.");
        foreach(var weapon in weapons)
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
        foreach(var extra in enemy.AdditionalWeapons)
            extra.Block.ApplyDamage(10000,extra.Block.transform.position,Vector3.forward);
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
            Assert.That(Run.Player.Modules.ActiveWheelCount, Is.EqualTo(6));
        }
    }

    [UnityTest,Timeout(720000)]
    public IEnumerator SixEncountersCanBeWonWhileFiringRealPlayerWeapons()
    {
        // Enemy discovery during this long scenario must not modify the real profile.
        pilotTechnologyPath=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"robo-pilot-"+Guid.NewGuid().ToString("N")+".json");
        typeof(R1RunController).GetProperty("Technology").GetSetMethod(true).Invoke(Run,new object[]{new R1TechnologyProgress(pilotTechnologyPath)});
        ConfigureVirtualInput();
        var follow = Object.FindFirstObjectByType<P12PipelineSafeCamera>();
        follow.enabled = false;
        var camera = Camera.main;
        var shells=(System.Collections.Generic.HashSet<P17PhysicalShell>)typeof(P17PhysicalShell)
            .GetField("activeShells",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
        var rockets=(System.Collections.Generic.HashSet<RoboRougelike.Mechanics.Rockets.RocketProjectile>)
            typeof(RoboRougelike.Mechanics.Rockets.RocketProjectile)
                .GetField("activeFlights",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
        for (int stage = 0; stage < 6; stage++)
        {
            // The real pause/transition guard requires a release before the next press.
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left).WithButton(MouseButton.Right));
            yield return null;
            P12PipelineSafeEnemyTurret enemy = null;
            Rigidbody body = null;
            Vector3 previous = Vector3.zero, velocity = Vector3.zero;
            var arena=Object.FindFirstObjectByType<RoboRougelike.R5.R5Arena>();
            Bounds? sightObstacle=null;float nextObstacleScan=0;
            int detourFrames=0;
            var firedWeapons=new System.Collections.Generic.HashSet<string>();
            float deadline = Time.realtimeSinceStartup + 110;
            while (Run.Progress.State == R1RunState.Combat && Time.realtimeSinceStartup < deadline)
            {
                var playerRoot=Run.Player!=null?Run.Player.transform:null;
                if(playerRoot!=null)
                {
                    foreach(var shell in shells)
                        if(shell!=null&&shell.IsActiveProjectile&&shell.OwnerRoot==playerRoot&&shell.LaunchSequence!=0)
                        {
                            var origin=shell.GetComponent<RoboRougelike.R4.R4ProjectileOrigin>();
                            if(origin!=null&&!string.IsNullOrEmpty(origin.PartId))firedWeapons.Add(origin.PartId);
                        }
                    foreach(var rocket in rockets)
                        if(rocket!=null&&rocket.IsInitialized&&!rocket.IsResolved&&rocket.OwnerRoot==playerRoot)
                        {
                            var origin=rocket.GetComponent<RoboRougelike.R4.R4ProjectileOrigin>();
                            if(origin!=null&&!string.IsNullOrEmpty(origin.PartId))firedWeapons.Add(origin.PartId);
                        }
                }
                // Automated pilot uses WASD, camera aim and both weapon slots with live AI and physical rounds.
                if(enemy==null||enemy.IsDefeated)
                {
                    enemy=Run.Encounter.Enemies.Where(e=>e!=null&&!e.IsDefeated)
                        .OrderBy(e=>Vector3.Distance(e.transform.position,Run.Player.transform.position)).FirstOrDefault();
                    if(enemy!=null){body=enemy.GetComponent<Rigidbody>();previous=body.position;velocity=Vector3.zero;sightObstacle=null;nextObstacleScan=0;}
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
                    // The scripted camera does not run the game's collision
                    // avoidance. Keep its aim origin above the vehicle so an
                    // obstacle behind the player cannot falsely occlude a target.
                    camera.transform.position = Run.Player.transform.position + Vector3.up * 3;
                    camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position);

                    Vector3 away = Vector3.ProjectOnPlane(Run.Player.transform.position - position, Vector3.up);
                    float distance = away.magnitude;
                    Vector3 waypoint = distance < 16
                        ? position + away.normalized * 11 + Vector3.Cross(Vector3.up, away.normalized) * 7
                        : position;
                    // Only arena geometry between camera and target triggers a
                    // detour. Moving vehicles and the ground are not walls.
                    if(Time.time>=nextObstacleScan)
                    {
                        nextObstacleScan=Time.time+.5f;sightObstacle=null;
                        var aim=target-camera.transform.position;
                        if(arena!=null&&arena.Layout!=null&&aim.sqrMagnitude>.01f)
                            foreach(var hit in Physics.RaycastAll(camera.transform.position,aim.normalized,aim.magnitude,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                            {
                                if(hit.collider==null||hit.collider.transform.IsChildOf(Run.Player.transform))continue;
                                if(hit.collider.transform.IsChildOf(enemy.transform))break;
                                if(hit.collider.transform.IsChildOf(arena.Layout)){sightObstacle=hit.collider.bounds;break;}
                            }
                    }
                    if(sightObstacle.HasValue)
                    {waypoint=R1PhysicalPilotRouting.NextWaypoint(Run.Player.transform.position,position,sightObstacle.Value);detourFrames++;}
                    Vector3 direction = Vector3.ProjectOnPlane(waypoint - Run.Player.transform.position, Vector3.up);
                    float angle = Vector3.SignedAngle(Run.Player.transform.forward, direction, Vector3.up);
                    var keys = new System.Collections.Generic.List<Key>();
                    if (angle > 4) keys.Add(Key.D); else if (angle < -4) keys.Add(Key.A);
                    if (!sightObstacle.HasValue&&distance < 6) keys.Add(Key.S); else if (Mathf.Abs(angle) < 65) keys.Add(Key.W);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
                }
                else InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Debug.Log($"[R1_PHYSICAL_PILOT_STAGE] stage={stage+1} state={Run.Progress.State} detourFrames={detourFrames} firedWeapons={firedWeapons.Count}");
            if(Run.Progress.State==R1RunState.Salvage)Assert.That(RRLW2TestFlow.Claim(Run,Array.Empty<string>()),Is.True);
            if(Run.Progress.State!=R1RunState.Garage&&Run.Progress.State!=R1RunState.Won&&Run.Player!=null)
            {
                var assembly=Run.Player.GetComponent<R3VehicleAssembly>();
                var bank=Run.Player.GetComponent<VehicleEnergyBank>();
                var remaining=Run.Encounter.Enemies.Where(e=>e!=null&&!e.IsDefeated).ToArray();
                var guns=assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun)
                    .Select(p=>$"{p.Id}:{p.Weapon} hp={p.Damageable.CurrentHp:0.0} active={p.Operational} fired={firedWeapons.Contains(p.Id)} readyAt={p.NextFireTime:0.00}");
                var targets=remaining.Select(e=>$"{e.name} pos={e.transform.position} coreHp={e.Core.CurrentHp:0.0} distance={Vector3.Distance(e.transform.position,Run.Player.transform.position):0.0}");
                var hits=Physics.RaycastAll(camera.transform.position,camera.transform.forward,200,~0,QueryTriggerInteraction.Ignore)
                    .OrderBy(h=>h.distance).Take(8).Select(h=>$"{h.collider.name}@{h.distance:0.0}");
                Debug.LogWarning($"[R1_PHYSICAL_TRIAGE] stage={stage+1} seed={Run.Seed} state={Run.Progress.State} reason={Run.Progress.DefeatReason} player={Run.Player.transform.position} " +
                    $"energy={bank?.Current:0.0}/{bank?.Maximum:0.0} now={Time.time:0.00} guns=[{string.Join(",",guns)}] " +
                    $"alive=[{string.Join(";",targets)}] cameraRay=[{string.Join(",",hits)}]");
            }
            Assert.That(firedWeapons.Count,Is.GreaterThan(0),"The player must actually fire a weapon during each encounter.");
            Assert.That(Run.Progress.State, Is.EqualTo(stage == 5 ? R1RunState.Won : R1RunState.Garage),
                "Fizyczna broń powinna pozwalać wygrać pojedynek " + (stage + 1));
            if (stage == 5) break;

            PreparePhysicalPilotVehicle();
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
        int initialParts=structure.ConnectedBlockCount;
        Assert.That(initialParts, Is.EqualTo(23),"The current enemy has one additional physical weapon.");
        var parts = enemy.GetComponentsInChildren<P12_PipelineSafePlayerBlock>();
        Assert.That(parts.Single(part => part.IsCore).gameObject, Is.EqualTo(enemy.Core.gameObject));
        var corner = parts.Single(part => part.LogicalGridPosition == new Vector3Int(-1, 0, -1));
        foreach (var position in new[] { new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, -1), new Vector3Int(-1, -1, -1) })
        {
            var part = parts.Single(item => item.LogicalGridPosition == position);
            var hp=part.GetComponent<P12PipelineSafeDamageable>();
            hp.ApplyDamageResolvedTyped(hp.CurrentHp/.9f+.01f,part.transform.position,Vector3.up,R3DamagePropagation.DamageKind.Piercing);
        }
        structure.EvaluateStructure();
        Assert.That(corner.DetachedFromVehicle, Is.True);
        Assert.That(corner.transform.IsChildOf(enemy.transform), Is.False);
        Assert.That(corner.GetComponentInParent<Rigidbody>(), Is.Not.Null);
        Assert.That(structure.CoreAlive, Is.True);
        Assert.That(structure.ConnectedBlockCount, Is.EqualTo(17),"The severed corner and its newly added weapon must detach without disturbing the remaining structure.");
        yield return null;
    }
}
