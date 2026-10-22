using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RogueMachine.P12PipelineSafe;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class R4CombatTests
{
    private R1RunController Run=>R1RunController.Instance;
    private Keyboard keyboard;
    private Mouse mouse;
    private InputSettings original,testSettings;
    private void VirtualInput()
    {
        original=InputSystem.settings;testSettings=UnityEngine.Object.Instantiate(original);InputSystem.settings=testSettings;
        testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        testSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
    }
    private static IEnumerator Until(Func<bool> condition,string message,float timeout=15)
    {if(R1RunController.Instance?.Progress?.State==R1RunState.Route)Assert.That(R1RunController.Instance.ChooseRoute(0),Is.True);float end=Time.realtimeSinceStartup+timeout;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Assert.That(condition(),Is.True,message);}
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        yield return Until(()=>Run?.Progress!=null,"Bootstrap");Run.StartRun();yield return Until(()=>!Run.Transitioning,"Starcie");
        Run.Encounter.SetCombatActive(false);
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
        if(original!=null)InputSystem.settings=original;if(testSettings!=null)UnityEngine.Object.Destroy(testSettings);
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
    }
    [UnityTest] public IEnumerator CoreDeathSnapshotsActualSurvivingWeaponBeforeWreckCleanup()
    {
        var enemy=Run.Encounter.Enemies.First();
        string sourcePrefix=enemy.name+" /";
        var yaw=enemy.transform.Find("Enemy_Yaw");
        var gun=yaw.GetComponentsInChildren<P12PipelineSafeDamageable>().OrderByDescending(p=>p.transform.localPosition.sqrMagnitude).First();
        gun.ApplyDamage(gun.MaxHp*0.35f,gun.transform.position,Vector3.forward);
        enemy.Core.ApplyDamage(10000,enemy.Core.transform.position,Vector3.forward);RRLW2TestFlow.DefeatAll(Run);
        yield return Until(()=>Run.Progress.State!=R1RunState.Combat,"Zwycięstwo");
        Assert.That(Run.Progress.State.ToString(),Is.EqualTo("Salvage"),"Przed ulepszeniem wymagany jest rzeczywisty wybór łupów.");
        var property=typeof(R1RunController).GetProperty("Salvage");Assert.That(property,Is.Not.Null);
        var ledger=(R4SalvageLedger)property.GetValue(Run);
        var recovered=ledger.Items.Single(p=>p.Kind==R3PartKind.Gun&&p.Source.StartsWith(sourcePrefix));
        Assert.That(recovered.Health,Is.EqualTo(0.65f).Within(0.01));
        Assert.That(recovered.Weapon,Is.EqualTo(R4WeaponKind.Automatic));
        Assert.That(Run.Encounter.Enemies,Is.Empty);Assert.That(ledger.Items.Count,Is.EqualTo(10));
    }
    [UnityTest] public IEnumerator DestroyedGunCanBeRebuiltAndDuplicateRewardIsRejected()
    {
        var enemy=Run.Encounter.Enemies.First();enemy.WeaponBlock.ApplyDamage(10000,enemy.WeaponBlock.transform.position,Vector3.up);
        enemy.Core.ApplyDamage(10000,enemy.Core.transform.position,Vector3.up);RRLW2TestFlow.DefeatAll(Run);
        yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Łupy");
        var gun=Run.Salvage.Items.First(p=>p.Kind==R3PartKind.Gun);Assert.That(gun.Recoverable,Is.False);
        int before=Run.VehicleModel.Scrap;Assert.That(RRLW2TestFlow.Claim(Run,new[]{gun.Id}),Is.True);
        Assert.That(Run.VehicleModel.Inventory.Single(p=>p.Id==gun.Id).Health,Is.Zero);
        int after=Run.VehicleModel.Scrap;Assert.That(after,Is.GreaterThan(before));Assert.That(Run.GarageMount(gun.Id,new Vector3Int(1,2,0)),Is.False);
        Assert.That(Run.GarageRepairInventory(gun.Id),Is.True);Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(after-36));
        after=Run.VehicleModel.Scrap;
        Assert.That(RRLW2TestFlow.Claim(Run,Array.Empty<string>()),Is.False);
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(after));
    }
    [UnityTest] public IEnumerator RecoveredGunPhysicallyHitsNextEnemyForEveryStarterAndSurvivesWholeRun()
    {
        for(int chassis=0;chassis<3;chassis++)
        {
            if(chassis>0)
            {
                var previous=Run;Run.RestartRun();yield return Until(()=>Run!=null&&Run!=previous&&Run.Progress!=null,"Nowy wzorzec");
                Run.SelectVehicle((R3VehicleKind)chassis);Run.StartRun();yield return Until(()=>!Run.Transitioning,"Start");Run.Encounter.SetCombatActive(false);
            }
            var source=Run.Encounter.Enemies.First();source.WeaponBlock.ApplyDamage(source.WeaponBlock.MaxHp*0.2f,source.WeaponBlock.transform.position,Vector3.up);
            source.Core.ApplyDamage(10000,source.Core.transform.position,Vector3.up);RRLW2TestFlow.DefeatAll(Run);
            yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Odzysk");
            var loot=Run.Salvage.Items.First(p=>p.Kind==R3PartKind.Gun);Assert.That(loot.Health,Is.EqualTo(0.8f).Within(0.01));
            Assert.That(RRLW2TestFlow.Claim(Run,new[]{loot.Id}),Is.True);
            int funds=Run.VehicleModel.Scrap;int y=chassis==1?1:2;var cell=new Vector3Int(1,y,0);
            Assert.That(Run.GarageMount(loot.Id,cell),Is.True);Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
            var mounted=Run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==loot.Id);
            Assert.That(mounted.Damageable.CurrentHp/mounted.Damageable.MaxHp,Is.EqualTo(0.8f).Within(0.01));
            Assert.That(Run.StartNextEncounter(),Is.True);yield return Until(()=>!Run.Transitioning,"Następna walka");
            Run.Encounter.SetCombatActive(false);R1EncounterManager.ClearTransientObjects();
            var enemy=Run.Encounter.Enemies.First();var target=enemy.GetComponentsInChildren<P12PipelineSafeDamageable>()
                .Where(p=>p.transform.parent.name=="Base").OrderBy(p=>p.transform.position.z).ThenBy(p=>Mathf.Abs(p.transform.position.x-enemy.transform.position.x)).First();
            Run.Player.MoveTo(new Vector3(enemy.transform.position.x,0.7f,enemy.transform.position.z-7),Quaternion.identity);
            mounted=Run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==loot.Id);
            mounted.GunPivot.rotation=Quaternion.LookRotation(target.transform.position-mounted.GunPivot.position);
            Physics.SyncTransforms();float hp=target.CurrentHp;
            Run.Player.Controller.FireFrom(mounted.Muzzle,mounted.Weapon,mounted.Id);
            var projectile=UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Single(p=>p.PartId==loot.Id);
            Assert.That(projectile.GetComponent<Rigidbody>().linearVelocity.magnitude,Is.EqualTo(42).Within(0.01));
            yield return Until(()=>target==null||target.CurrentHp<hp,"Pocisk odzyskanej broni musi trafić rzeczywisty blok kolejnego wroga");
            Assert.That(target.CurrentHp,Is.EqualTo(hp-12*Run.Progress.Stats.Damage).Within(0.1));
            foreach(var foe in Run.Encounter.Enemies)foe.Core.ApplyDamage(10000,foe.Core.transform.position,Vector3.up);
            yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Drugie łupy");RRLW2TestFlow.Claim(Run,Array.Empty<string>());
            Assert.That(Run.VehicleModel.At(cell).Id,Is.EqualTo(loot.Id));Assert.That(Run.StartNextEncounter(),Is.True);
            yield return Until(()=>!Run.Transitioning,"Finał");Assert.That(Run.Encounter.AliveCount,Is.EqualTo(4));
            foreach(var foe in Run.Encounter.Enemies)foe.Core.ApplyDamage(10000,foe.Core.transform.position,Vector3.up);
            yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Finałowe łupy");RRLW2TestFlow.Claim(Run,Array.Empty<string>());
            while(Run.Progress.State==R1RunState.Garage)
            {
                Run.StartNextEncounter();yield return Until(()=>!Run.Transitioning,"Dalszy etap R5");Run.Encounter.SetCombatActive(false);
                foreach(var foe in Run.Encounter.Enemies)foe.Core.ApplyDamage(10000,foe.Core.transform.position,Vector3.up);
                yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Dalsze łupy R5");RRLW2TestFlow.Claim(Run,Array.Empty<string>());
                Assert.That(Run.VehicleModel.At(cell).Id,Is.EqualTo(loot.Id));
            }
            Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Won));Assert.That(Run.VehicleModel.At(cell).Id,Is.EqualTo(loot.Id));
        }
        var old=Run;Run.RestartRun();yield return Until(()=>Run!=null&&Run!=old&&Run.Progress!=null,"Czysty restart");
        Run.StartRun();yield return Until(()=>!Run.Transitioning,"Nowa wyprawa");
        Assert.That(Run.VehicleModel.Inventory,Is.Empty);Assert.That(Run.VehicleModel.Scrap,Is.Zero);Assert.That(Run.Progress.Acquired,Is.Empty);
        Assert.That(Run.Salvage.Items,Is.Empty);
    }
    [UnityTest] public IEnumerator EscapeEventsPauseResumeAndGarageBlocksCombatInput()
    {
        VirtualInput();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return Until(()=>Run.Paused,"Esc pauzuje");
        Assert.That(Time.timeScale,Is.Zero);Assert.That(Run.Player.Controller.GameplayInputEnabled,Is.False);
        Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.None));Assert.That(Cursor.visible,Is.True);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return Until(()=>!Run.Paused,"Esc wznawia");
        Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(Run.Player.Controller.GameplayInputEnabled,Is.True);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        var enemy=Run.Encounter.Enemies.First();enemy.Core.ApplyDamage(10000,enemy.Core.transform.position,Vector3.up);RRLW2TestFlow.DefeatAll(Run);
        yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Łupy");RRLW2TestFlow.Claim(Run,Array.Empty<string>());
        R1EncounterManager.ClearTransientObjects();var position=Run.Player.transform.position;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));
        float end=Time.realtimeSinceStartup+0.25f;yield return Until(()=>Time.realtimeSinceStartup>=end,"Garaż");
        Assert.That(Vector3.Distance(Run.Player.transform.position,position),Is.LessThan(0.0001f),"Brak jazdy w garażu z tolerancją 0,1 mm na interpolację Rigidbody.");
        Assert.That(UnityEngine.Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None),Is.Empty);
        Assert.That(Cursor.visible,Is.True);Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.None));
        var cell=Run.Garage.Cell;Run.Garage.Select(new Vector3Int(0,3,1));Run.Garage.Orbit(25);Run.Garage.Tilt(10);yield return null;
        Assert.That(Run.Garage.Cell,Is.Not.EqualTo(cell));
    }
    [UnityTest] public IEnumerator AllWeaponsHaveIndependentCadenceUpgradesAndPhysicalDamage()
    {
        var enemy=Run.Encounter.Enemies.First();enemy.Core.ApplyDamage(10000,enemy.Core.transform.position,Vector3.up);RRLW2TestFlow.DefeatAll(Run);
        yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Łupy");RRLW2TestFlow.Claim(Run,Array.Empty<string>());
        Run.VehicleModel.Award(100);Assert.That(Run.GarageBuyWeapon(new Vector3Int(1,2,0),R4WeaponKind.Automatic),Is.True);
        Assert.That(Run.GarageBuyWeapon(new Vector3Int(-1,2,0),R4WeaponKind.Heavy),Is.True);
        var damage=Run.Definition.Upgrades.Single(p=>p.Effect==R1UpgradeEffect.Damage);
        var reload=Run.Definition.Upgrades.Single(p=>p.Effect==R1UpgradeEffect.Reload);
        Assert.That(Run.VehicleModel.ReceiveBonuses(new[]{damage,reload}),Is.True);
        string damageId=Run.VehicleModel.Inventory.Last(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeId==damage.Id).Id;
        string reloadId=Run.VehicleModel.Inventory.Last(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeId==reload.Id).Id;
        Assert.That(Run.GarageMount(damageId,new Vector3Int(1,2,1)),Is.True);
        Assert.That(Run.GarageMount(reloadId,new Vector3Int(-2,2,1)),Is.True);
        Assert.That(Run.Progress.Stats.Damage,Is.EqualTo(1));Assert.That(Run.ActiveStats.Damage,Is.GreaterThan(1));
        Run.StartNextEncounter();yield return Until(()=>!Run.Transitioning,"Strzelanie");Run.Encounter.SetCombatActive(false);
        var assembly=Run.Player.GetComponent<R3VehicleAssembly>();
        foreach(var part in assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun))part.GunPivot.rotation=Quaternion.Euler(-30,180,0);
        R1EncounterManager.ClearTransientObjects();assembly.FireGuns();
        Assert.That(UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Length,Is.EqualTo(3));
        float end=Time.time+0.17f;yield return Until(()=>Time.time>=end,"Szybkostrzelność");assembly.FireGuns();
        var shots=UnityEngine.Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None);
        Assert.That(shots.Count(p=>p.Weapon==R4WeaponKind.Automatic),Is.EqualTo(2));
        Assert.That(shots.Count(p=>p.Weapon==R4WeaponKind.Basic),Is.EqualTo(1));Assert.That(shots.Count(p=>p.Weapon==R4WeaponKind.Heavy),Is.EqualTo(1));
        foreach(var part in assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun))
        {
            R1EncounterManager.ClearTransientObjects();var fixture=GameObject.CreatePrimitive(PrimitiveType.Cube);fixture.transform.position=part.Muzzle.position+Vector3.up*5;
            var hp=fixture.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
            part.GunPivot.rotation=Quaternion.LookRotation(fixture.transform.position-part.GunPivot.position);Physics.SyncTransforms();
            Run.Player.Controller.FireFrom(part.Muzzle,part.Weapon,part.Id);
            yield return Until(()=>hp.CurrentHp<1000,"Każda broń musi zadawać obrażenia fizycznym pociskiem");
            Assert.That(hp.CurrentHp,Is.EqualTo(1000-R4WeaponDefinition.Get(part.Weapon).Damage*Run.ActiveStats.Damage).Within(0.1));
            UnityEngine.Object.Destroy(fixture);
        }
    }
    [UnityTest] public IEnumerator EachEnemySnapshotSurvivesItsWreckExpiringBeforeLastCoreDies()
    {
        var first=Run.Encounter.Enemies.First();first.Core.ApplyDamage(10000,first.Core.transform.position,Vector3.up);RRLW2TestFlow.DefeatAll(Run);
        yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Pierwsze łupy");RRLW2TestFlow.Claim(Run,Array.Empty<string>());
        Run.StartNextEncounter();yield return Until(()=>!Run.Transitioning,"Dwóch przeciwników");Run.Encounter.SetCombatActive(false);
        var enemy=Run.Encounter.Enemies[0];var yaw=enemy.transform.Find("Enemy_Yaw");
        enemy.Core.ApplyDamage(10000,enemy.Core.transform.position,Vector3.up);
        yield return Until(()=>!enemy.enabled,"Wyłączenie AI");
        Assert.That(Run.Salvage.Items.Count,Is.EqualTo(7));var saved=Run.Salvage.Items.First(p=>p.Kind==R3PartKind.Gun);
        Assert.That(saved.Weapon,Is.EqualTo(R4WeaponKind.Heavy));Assert.That(saved.Health,Is.EqualTo(1));
        yield return Until(()=>yaw==null,"Wrak musi faktycznie wygasnąć",15);
        Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Combat));Assert.That(Run.Salvage.Items.Count,Is.EqualTo(7));
        RRLW2TestFlow.DefeatAll(Run);
        yield return Until(()=>Run.Progress.State==R1RunState.Salvage,"Końcowe łupy");Assert.That(Run.Salvage.Items.Count,Is.EqualTo(17));
        Assert.That(Run.Salvage.Items.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(17));
        Assert.That(RRLW2TestFlow.Claim(Run,new[]{saved.Id}),Is.True);Assert.That(Run.VehicleModel.Inventory.Single(p=>p.Id==saved.Id).Weapon,Is.EqualTo(R4WeaponKind.Heavy));
    }
}
