using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class R3GaragePlayTests
{
    private R1RunController Run => R1RunController.Instance;
    private Keyboard keyboard;
    private Mouse mouse;
    private InputSettings savedSettings,testSettings;
    private static IEnumerator Until(Func<bool> condition, string message, float timeout = 15, bool automaticRewards = true)
    {
        if(R1RunController.Instance?.Progress?.State==R1RunState.Route)Assert.That(R1RunController.Instance.ChooseRoute(0),Is.True);
        float deadline = Time.realtimeSinceStartup + timeout;
        while (Time.realtimeSinceStartup < deadline)
        {
            var run=R1RunController.Instance;
            if(condition())break;
            if(automaticRewards&&run?.Progress?.State==R1RunState.Salvage)Assert.That(RRLW2TestFlow.Claim(run,Array.Empty<string>()),Is.True);
            if(condition())break;yield return null;
        }
        Assert.That(condition(), Is.True, message);
    }
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return Until(() => Run != null && Run.Progress != null, "Bootstrap.");
        Run.StartRun(); yield return Until(() => !Run.Transitioning, "Start walki.");
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
        if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
        if(savedSettings!=null)InputSystem.settings=savedSettings;
        if(testSettings!=null)UnityEngine.Object.Destroy(testSettings);
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }
    private void VirtualInput()
    {
        savedSettings=InputSystem.settings;testSettings=UnityEngine.Object.Instantiate(savedSettings);InputSystem.settings=testSettings;
        testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        testSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
    }
    private void KillEnemies()
    { foreach(var enemy in Run.Encounter.Enemies)if(enemy!=null&&!enemy.IsDefeated)enemy.Core.ApplyDamage(10000,enemy.Core.transform.position,Vector3.up); }
    [UnityTest] public IEnumerator RewardOpensGarageWithoutFreeRepair()
    {
        Run.Encounter.SetCombatActive(false);
        var part = Run.Player.GetComponentsInChildren<P12_PipelineSafePlayerBlock>().First(p => !p.IsCore)
            .GetComponent<P12PipelineSafeDamageable>();
        part.ApplyDamage(20, part.transform.position, Vector3.forward);
        foreach (var enemy in Run.Encounter.Enemies) enemy.Core.ApplyDamage(10000, enemy.Core.transform.position, Vector3.up);
        yield return Until(() => Run.Progress.State == R1RunState.Garage, "Nagroda.");

        yield return Until(() => !Run.Transitioning, "Przejście do garażu.");
        Assert.That(Run.Progress.State.ToString(), Is.EqualTo("Garage"));
        Assert.That(Run.Player.Controller.GameplayInputEnabled, Is.False);
        Assert.That(Run.Player.GetComponentsInChildren<P12PipelineSafeDamageable>()
            .Any(p => !p.IsDead && p.CurrentHp < p.MaxHp * 0.99f), Is.True, "Ulepszenie nie może naprawić uszkodzeń.");
    }
    [UnityTest] public IEnumerator EnemyKeepsMovingAlongArenaBoundary()
    {
        var enemy = Run.Encounter.Enemies.First();
        var body = enemy.GetComponent<Rigidbody>(); body.position = new Vector3(37.99f, 0, 0);
        Run.Player.MoveTo(new Vector3(60, 0.7f, 0), Quaternion.identity);
        Physics.SyncTransforms();
        Vector3 before = body.position;
        float end = Time.time + 1.5f;
        yield return Until(() => Time.time >= end, "Symulacja granicy.");
        Assert.That(Vector3.Distance(before, body.position), Is.GreaterThan(0.3f));
        Assert.That(Mathf.Abs(body.position.x), Is.LessThanOrEqualTo(38.01f));
    }

    [UnityTest] public IEnumerator AllThreeVehiclesMoveFireTakeDamageAndReset()
    {
        VirtualInput();
        var counts=new int[3];var masses=new float[3];var speeds=new float[3];
        for(int i=0;i<3;i++)
        {
            if(i>0)
            {
                var old=Run;Run.RestartRun();yield return Until(()=>Run!=null&&Run!=old&&Run.Progress!=null,"Wybór kolejnego pojazdu.");
                Assert.That(Run.SelectVehicle((R3VehicleKind)i),Is.True);Run.StartRun();
                yield return Until(()=>!Run.Transitioning,"Inicjalizacja innego wzorca.");
            }
            Run.Encounter.SetCombatActive(false);
            counts[i]=Run.VehicleModel.Parts.Count;masses[i]=Run.Player.GetComponent<Rigidbody>().mass;
            speeds[i]=Run.Player.Controller.EffectiveForwardSpeed;
            Assert.That(Run.Player.Structure.CoreAlive,Is.True);Assert.That(Run.Player.Modules.WeaponOnline,Is.True);
            Assert.That(Run.VehicleModel.Scrap,Is.Zero);
            var before=Run.Player.transform.position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
            float until=Time.time+0.7f;yield return Until(()=>Time.time>=until,"Jazda pojazdem.");
            Assert.That(Vector3.Distance(before,Run.Player.transform.position),Is.GreaterThan(0.4f));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));
            yield return Until(()=>UnityEngine.Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None)
                .Any(s=>s.name=="P17_PlayerPhysicalShell"),"Każdy pojazd strzela po LPM.");
            InputSystem.QueueStateEvent(mouse,new MouseState());
            var core=Run.Player.Core;core.ApplyDamage(10,core.transform.position,Vector3.forward);
            Assert.That(core.CurrentHp,Is.EqualTo(core.MaxHp-10).Within(0.01));
        }
        Assert.That(counts[1],Is.LessThan(counts[0]));Assert.That(counts[2],Is.GreaterThan(counts[0]));
        Assert.That(masses[1],Is.LessThan(masses[0]));Assert.That(masses[2],Is.GreaterThan(masses[0]));
        Assert.That(speeds[1],Is.GreaterThan(speeds[0]));Assert.That(speeds[2],Is.LessThan(speeds[0]));
    }

    [UnityTest] public IEnumerator PaidRepairsNewModulesIndependentEnemiesAndCleanRestart()
    {
        Run.Encounter.SetCombatActive(false);
        var assembly=Run.Player.GetComponent<R3VehicleAssembly>();
        var profiles=new System.Collections.Generic.HashSet<R3EnemyArchetype>{Run.Encounter.Enemies[0].Archetype};
        var wheel=assembly.Parts.First(p=>p.Kind==R3PartKind.Wheel);var wheelCell=wheel.Cell;
        wheel.Damageable.ApplyDamage(1000,wheel.transform.position,Vector3.right);
        Run.Player.Core.ApplyDamage(10,Run.Player.Core.transform.position,Vector3.forward);
        var armor=assembly.Parts.First(p=>p.Kind==R3PartKind.Armor);var armorCell=armor.Cell;
        armor.Damageable.ApplyDamage(25,armor.transform.position,Vector3.forward);
        KillEnemies();yield return Until(()=>Run.Progress.State==R1RunState.Garage,"Pierwsze zwycięstwo.");

        int reward=80+Run.Salvage.DismantledScrap;
        Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(reward));
        Assert.That(Run.VehicleModel.At(wheelCell).Health,Is.Zero);
        Assert.That(Run.VehicleModel.At(armorCell).Health,Is.EqualTo(0.75f).Within(0.01));
        Assert.That(Run.GarageRepair(Vector3Int.zero),Is.True);
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(reward-1));
        Assert.That(Run.GarageRepair(wheelCell),Is.True);Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(reward-16));
        Assert.That(Run.Player.Modules.ActiveWheelCount,Is.EqualTo(12));
        Assert.That(Run.GarageRepair(armorCell),Is.True);Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(reward-17));
        Assert.That(Run.GarageBuy(new Vector3Int(0,2,1),R3PartKind.Armor),Is.True,Run.VehicleModel.PlacementError(new Vector3Int(0,2,1),R3PartKind.Armor));
        Assert.That(Run.GarageBuy(new Vector3Int(1,2,0),R3PartKind.Gun),Is.True);
        Assert.That(Run.GarageBuy(new Vector3Int(4,0,0),R3PartKind.Wheel),Is.True);
        Assert.That(Run.Player.Modules.ActiveWheelCount,Is.EqualTo(13));Assert.That(Run.Player.Modules.ActiveWeaponCount,Is.EqualTo(2));
        Assert.That(Run.GarageBuy(new Vector3Int(0,3,1),R3PartKind.Armor),Is.True);
        Assert.That(Run.GarageRemove(new Vector3Int(0,2,1)),Is.False,"Usunięcie mostu jest blokowane.");
        Assert.That(Run.GarageRemove(new Vector3Int(0,3,1)),Is.True);
        Assert.That(Run.GarageRemove(Vector3Int.zero),Is.False);
        int funds=Run.VehicleModel.Scrap;
        Assert.That(Run.GarageBuy(new Vector3Int(1,2,0),R3PartKind.Gun),Is.False);
        Assert.That(Run.GarageBuy(new Vector3Int(-1,2,0),R3PartKind.Blink),Is.False,"Brak środków.");
        Assert.That(Run.GarageBuy(new Vector3Int(6,5,7),R3PartKind.Armor),Is.False);
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        assembly=Run.Player.GetComponent<R3VehicleAssembly>();R1EncounterManager.ClearTransientObjects();
        assembly.FireGuns();Assert.That(UnityEngine.Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Length,Is.EqualTo(2));
        var extraGun=assembly.Parts.Single(p=>p.Cell==new Vector3Int(1,2,0));
        extraGun.Damageable.ApplyDamage(1000,extraGun.transform.position,Vector3.forward);assembly.Capture();
        Assert.That(Run.Player.Modules.ActiveWeaponCount,Is.EqualTo(1));
        Assert.That(Run.StartNextEncounter(),Is.True);yield return Until(()=>!Run.Transitioning,"Druga walka.");
        Assert.That(Run.Player.Modules.ActiveWeaponCount,Is.EqualTo(1));Assert.That(Run.VehicleModel.Parts.Count,Is.EqualTo(86));
        Assert.That(Run.Encounter.AliveCount,Is.EqualTo(3));
        var positions=Run.Encounter.Enemies.Select(e=>e.transform.position).ToArray();
        float moveUntil=Time.time+0.9f;yield return Until(()=>Time.time>=moveUntil,"Ruch dwóch AI.");
        for(int i=0;i<3;i++){Assert.That(Vector3.Distance(positions[i],Run.Encounter.Enemies[i].transform.position),Is.GreaterThan(0.1f));profiles.Add(Run.Encounter.Enemies[i].Archetype);}
        Run.Encounter.SetCombatActive(false);
        var first=Run.Encounter.Enemies[0];first.Core.ApplyDamage(10000,first.Core.transform.position,Vector3.up);
        float end=Time.time+0.2f;yield return Until(()=>Time.time>=end,"Niezależna śmierć jednego AI.");
        Assert.That(Run.Encounter.AliveCount,Is.EqualTo(2));Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Combat));
        KillEnemies();yield return Until(()=>Run.Progress.State==R1RunState.Garage,"Drugie zwycięstwo.");
        Assert.That(Run.GarageRepair(new Vector3Int(1,2,0)),Is.True);
        Assert.That(Run.Player.Modules.ActiveWeaponCount,Is.EqualTo(2));
        Assert.That(Run.StartNextEncounter(),Is.True);yield return Until(()=>!Run.Transitioning,"Trzecia walka.");
        Assert.That(Run.Encounter.AliveCount,Is.EqualTo(4));
        positions=Run.Encounter.Enemies.Select(e=>e.transform.position).ToArray();
        moveUntil=Time.time+0.9f;yield return Until(()=>Time.time>=moveUntil,"Ruch trzech AI.");
        for(int i=0;i<4;i++){Assert.That(Vector3.Distance(positions[i],Run.Encounter.Enemies[i].transform.position),Is.GreaterThan(0.1f));profiles.Add(Run.Encounter.Enemies[i].Archetype);}
        Assert.That(profiles.Count,Is.EqualTo(4));
        Assert.That(Run.Player.Modules.ActiveWheelCount,Is.EqualTo(13));Assert.That(Run.Progress.Acquired.Count,Is.EqualTo(2));
        KillEnemies();yield return Until(()=>Run.Progress.State==R1RunState.Garage,"Trzecie starcie.");
        while(Run.Progress.State==R1RunState.Garage)
        {
            Run.StartNextEncounter();yield return Until(()=>!Run.Transitioning,"Dalszy etap R5.");
            Run.Encounter.SetCombatActive(false);KillEnemies();yield return Until(()=>Run.Progress.State==R1RunState.Garage||Run.Progress.State==R1RunState.Won,"Dalsza nagroda R5.");
        }
        Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Won));
        var previous=Run;Run.RestartRun();yield return Until(()=>Run!=null&&Run!=previous&&Run.Progress!=null,"Nowa próba.");
        Run.StartRun();yield return Until(()=>!Run.Transitioning,"Nienaruszony wzorzec.");
        Assert.That(Run.VehicleModel.Scrap,Is.Zero);Assert.That(Run.VehicleModel.Parts.Count,Is.EqualTo(83));Assert.That(Run.Progress.Acquired,Is.Empty);
    }

    [UnityTest] public IEnumerator TurretSupportLossDropsEntireRemainingWeapon()
    {
        var enemy=Run.Encounter.Enemies.First();var yaw=enemy.transform.Find("Enemy_Yaw");
        yaw.GetChild(0).GetComponent<P12PipelineSafeDamageable>().ApplyDamage(1000,yaw.position,Vector3.up);
        yield return Until(()=>!enemy.WeaponOnline,"Rozbrojenie.");
        yield return null;
        Assert.That(yaw.IsChildOf(enemy.transform),Is.False);Assert.That(yaw.GetComponent<Rigidbody>(),Is.Not.Null);
        Assert.That(enemy.IsDefeated,Is.False);Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Combat));
    }

    [UnityTest] public IEnumerator EmergencyRebuildPaysForEssentialModulesWithoutFullReset()
    {
        KillEnemies();yield return Until(()=>Run.Progress.State==R1RunState.Garage,"Nagroda.");
        var assembly=Run.Player.GetComponent<R3VehicleAssembly>();
        foreach(var part in assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun||p.Kind==R3PartKind.Wheel))
            part.Damageable.ApplyDamage(10000,part.transform.position,Vector3.up);
        assembly.Capture();int scrap=Run.VehicleModel.Scrap;
        Assert.That(Run.StartNextEncounter(),Is.False);
        Assert.That(Run.GarageEmergencyRepair(),Is.True);
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(scrap-60));
        Assert.That(Run.Player.Modules.ActiveWheelCount,Is.EqualTo(2));Assert.That(Run.Player.Modules.ActiveWeaponCount,Is.EqualTo(1));
        Assert.That(Run.VehicleModel.Parts.Count(p=>p.Kind==R3PartKind.Wheel&&p.Health<=0),Is.EqualTo(10));
        foreach(var part in Run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.Gun||p.Kind==R3PartKind.Wheel))
            Assert.That(part.Damageable.CurrentHp/part.Damageable.MaxHp,Is.EqualTo(1f).Within(0.001));
        Assert.That(Run.StartNextEncounter(),Is.True);yield return Until(()=>!Run.Transitioning,"Walka po pomocy awaryjnej.");
        Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Combat));
    }

    [UnityTest] public IEnumerator ActivePhysicalProjectilesAreBounded()
    {
        Run.TogglePause();
        for(int i=0;i<120;i++)new GameObject("Pocisk testowy "+i).AddComponent<P17PhysicalShell>();
        for(int i=0;i<80;i++)new GameObject("Szczątki testowe "+i).AddComponent<R3Debris>();
        yield return null;yield return null;
        Assert.That(UnityEngine.Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Length,Is.LessThanOrEqualTo(96));
        Assert.That(UnityEngine.Object.FindObjectsByType<R3Debris>(FindObjectsSortMode.None).Length,Is.LessThanOrEqualTo(70));
    }

    [UnityTest] public IEnumerator CoreDeathDropsWeaponAndCleansItWhileAnotherEnemySurvives()
    {
        KillEnemies();yield return Until(()=>Run.Progress.State==R1RunState.Garage,"Pierwsza nagroda.");
        Run.StartNextEncounter();yield return Until(()=>!Run.Transitioning,"Walka z dwoma przeciwnikami.");
        Run.Encounter.SetCombatActive(false);
        var dead=Run.Encounter.Enemies[0];var yaw=dead.transform.Find("Enemy_Yaw");
        dead.Core.ApplyDamage(10000,dead.Core.transform.position,Vector3.up);
        yield return null;yield return null;
        Assert.That(yaw.IsChildOf(dead.transform),Is.False,"Śmierć rdzenia musi odłączyć całą sprawną wieżyczkę.");
        Assert.That(yaw.GetComponent<Rigidbody>(),Is.Not.Null);
        Assert.That(Run.Encounter.AliveCount,Is.EqualTo(2));Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Combat));
        yield return Until(()=>yaw==null,"Szczątki wieżyczki znikają w trwającej walce.",15);
        Assert.That(dead.IsDefeated,Is.True);Assert.That(dead.enabled,Is.False);
        Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Combat));
    }
}
