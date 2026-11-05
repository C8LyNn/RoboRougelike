using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class UnitCellEnergyTests
{
    private static object NewEnergy()
    {
        var type = typeof(R3VehicleModel).Assembly.GetType("RoboRougelike.R3.VehicleEnergyState");
        Assert.That(type, Is.Not.Null, "The shared energy state must exist for both player and enemies.");
        return Activator.CreateInstance(type);
    }
    private static float Number(object state, string property) =>
        (float)state.GetType().GetProperty(property).GetValue(state);
    private static object Call(object state, string method, params object[] args) =>
        state.GetType().GetMethod(method).Invoke(state, args);
    private static Vector3Int? FindPlacement(R3VehicleModel model,R3PartKind kind)
    {
        for(int y=0;y<=5;y++)for(int x=-6;x<=6;x++)for(int z=-7;z<=7;z++)
        {
            var cell=new Vector3Int(x,y,z);
            if(model.PlacementError(cell,kind)==null)return cell;
        }
        return null;
    }

    [Test]
    public void CoreBatteriesAndPassiveReservationChangeCapacity()
    {
        var energy = NewEnergy();
        Call(energy, "Refresh", 0, 0, true);
        Assert.That(Number(energy, "Maximum"), Is.EqualTo(500));
        Call(energy, "Refresh", 2, 1, true);
        Assert.That(Number(energy, "Maximum"), Is.EqualTo(675));
        Call(energy, "Refresh", 0, 1, true);
        Assert.That(Number(energy, "Maximum"), Is.EqualTo(475));
        Call(energy, "Refresh", 0, 0, false);
        Assert.That(Number(energy, "Maximum"), Is.Zero);
    }

    [Test]
    public void BatteryIsARealPurchasableOneCellPart()
    {
        Assert.That(Enum.TryParse("Battery", out R3PartKind battery), Is.True);
        var model = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Award(1000);
        var cell = Enumerable.Range(-5, 11)
            .SelectMany(x => Enumerable.Range(-5, 11).Select(z => new Vector3Int(x, 1, z)))
            .First(p => model.PlacementError(p, battery) == null);
        Assert.That(model.Buy(cell, battery), Is.True);
        Assert.That(model.At(cell).Kind, Is.EqualTo(battery));
        Assert.That(R4WeaponDefinition.PartName(model.At(cell)), Does.Contain("Bateria"));
        Assert.That(R3VehicleModel.OccupiedCells(model.At(cell)).Count(), Is.EqualTo(1));
    }

    [Test]
    public void CheckpointPreservesEnergyAndOlderCheckpointStartsCharged()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        Assert.That(model.Energy.TrySpend(120),Is.True);
        var saved=model.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,new R3GarageEconomy(),0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Energy.Current,Is.EqualTo(380));
        saved.energyVersion=0;saved.energy=0;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,new R3GarageEconomy(),0,out restored,out error),Is.True,error);
        Assert.That(restored.Energy.Current,Is.EqualTo(500));
    }

    [UnityTest]
    public IEnumerator RealGarageVehicleHasOneSharedEnergyBank()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline = Time.realtimeSinceStartup + 15f;
        while (R1RunController.Instance?.Progress == null && Time.realtimeSinceStartup < deadline)
            yield return null;
        var run = R1RunController.Instance;
        Assert.That(run, Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Light), Is.True);
        Assert.That(run.OpenSandboxGarage(), Is.True);
        yield return null;
        var bankType = typeof(R3VehicleModel).Assembly.GetType("RoboRougelike.R3.VehicleEnergyBank");
        Assert.That(bankType, Is.Not.Null, "A real vehicle needs the shared energy bank.");
        var bank = run.Player.GetComponent(bankType);
        Assert.That(bank, Is.Not.Null);
        Assert.That((float)bankType.GetProperty("Maximum").GetValue(bank), Is.EqualTo(500));
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator ChargingFieldChargesAnyVehicleInsideFasterThanOutside()
    {
        yield return new EnterPlayMode();
        Time.timeScale=1f;
        var inside = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var outside = GameObject.CreatePrimitive(PrimitiveType.Cube);
        outside.transform.position = new Vector3(20, 0, 0);
        var insideHp = inside.AddComponent<P12PipelineSafeDamageable>(); insideHp.Configure(100, true);
        var outsideHp = outside.AddComponent<P12PipelineSafeDamageable>(); outsideHp.Configure(100, true);
        var insideBank = inside.AddComponent<VehicleEnergyBank>(); insideBank.ConfigureEnemy(insideHp);
        var outsideBank = outside.AddComponent<VehicleEnergyBank>(); outsideBank.ConfigureEnemy(outsideHp);
        Assert.That(insideBank.TrySpend(100), Is.True);
        Assert.That(outsideBank.TrySpend(100), Is.True);
        var fieldType = typeof(R3VehicleModel).Assembly.GetType("RoboRougelike.R3.EnergyChargingField");
        Assert.That(fieldType, Is.Not.Null, "A real charging field must exist in the game.");
        var field = new GameObject("energy test field"); field.AddComponent(fieldType);
        Physics.SyncTransforms();
        float start = Time.fixedTime,deadline=Time.realtimeSinceStartup+10f;
        while (Time.fixedTime - start < .5f&&Time.realtimeSinceStartup<deadline) yield return null;
        Assert.That(Time.fixedTime-start,Is.GreaterThanOrEqualTo(.5f),"The test requires real advancing physics.");
        Assert.That(insideBank.Current, Is.GreaterThan(outsideBank.Current + 5f));
        UnityEngine.Object.Destroy(field); UnityEngine.Object.Destroy(inside); UnityEngine.Object.Destroy(outside);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator RealBatteryAndPassiveModuleUpdateCapacityAfterDestruction()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;
        Assert.That(run,Is.Not.Null);Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);run.VehicleModel.Award(1000);
        var batteryCell=FindPlacement(run.VehicleModel,R3PartKind.Battery);
        Assert.That(batteryCell.HasValue,Is.True);Assert.That(run.GarageBuy(batteryCell.Value,R3PartKind.Battery),Is.True);
        var bank=run.Player.GetComponent<VehicleEnergyBank>();Assert.That(bank.Maximum,Is.EqualTo(600));
        bank.Recharge(1000);Assert.That(bank.Current,Is.EqualTo(600));
        var gyroCell=FindPlacement(run.VehicleModel,R3PartKind.Gyroscope);
        Assert.That(gyroCell.HasValue,Is.True);Assert.That(run.GarageBuy(gyroCell.Value,R3PartKind.Gyroscope),Is.True);
        bank=run.Player.GetComponent<VehicleEnergyBank>();Assert.That(bank.Maximum,Is.EqualTo(575));
        R3MountedPart battery=null;
        foreach(var part in run.Player.GetComponent<R3VehicleAssembly>().Parts)
            if(part.Kind==R3PartKind.Battery){battery=part;break;}
        Assert.That(battery,Is.Not.Null);
        battery.Damageable.ApplyDamage(100000,battery.transform.position,Vector3.zero);
        bank.Refresh();Assert.That(bank.Maximum,Is.EqualTo(475));Assert.That(bank.Current,Is.LessThanOrEqualTo(475));
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator RealBasicGunSpendsEnergyAndCannotFireWhenEmpty()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;
        Assert.That(run,Is.Not.Null);Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();
        var bank=run.Player.GetComponent<VehicleEnergyBank>();
        R3MountedPart gun=null;foreach(var part in assembly.Parts)if(part.Kind==R3PartKind.Gun){gun=part;break;}
        Assert.That(gun,Is.Not.Null);
        gun.GunPivot.rotation=Quaternion.LookRotation(Vector3.up,Vector3.forward);Physics.SyncTransforms();
        Assert.That(assembly.ClearProjectilePath(gun),Is.True,"The actual muzzle must be unobstructed.");
        float before=bank.Current;assembly.FireGuns();
        Assert.That(gun.NextFireTime,Is.GreaterThan(Time.time));
        Assert.That(bank.Current,Is.EqualTo(before-VehicleEnergyCosts.BasicShot).Within(.01f));
        Assert.That(bank.TrySpend(bank.Current),Is.True);gun.NextFireTime=0;
        assembly.FireGuns();
        Assert.That(gun.NextFireTime,Is.Zero,"An empty bank must not fire or start cooldown.");
        Assert.That(bank.Current,Is.Zero);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator ProjectileWeaponsAndEmpShareTheRealVehicleBank()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);Assert.That(run.OpenSandboxGarage(),Is.True);
        var palette=run.GetComponent<R3VisualPalette>();Assert.That(palette,Is.Not.Null);
        var kinds=new[]{R4WeaponKind.Basic,R4WeaponKind.Automatic,R4WeaponKind.Heavy,R4WeaponKind.Shotgun,
            R4WeaponKind.Railgun,R4WeaponKind.RocketLauncher,R4WeaponKind.EmpEmitter};
        for(int i=0;i<kinds.Length;i++)
        {
            var design=R3VehicleCatalog.Create(R3VehicleKind.Light);
            foreach(var p in design.Parts)if(p.Kind==R3PartKind.Gun){p.Weapon=kinds[i];break;}
            var model=new R3VehicleModel(design);
            var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(500+i*20,30,100),Quaternion.identity);
            vehicle.GetComponent<Rigidbody>().isKinematic=true;yield return null;
            var assembly=vehicle.GetComponent<R3VehicleAssembly>();var bank=vehicle.GetComponent<VehicleEnergyBank>();
            R3MountedPart gun=null;foreach(var part in assembly.Parts)if(part.Kind==R3PartKind.Gun){gun=part;break;}
            Assert.That(gun,Is.Not.Null,kinds[i].ToString());
            gun.GunPivot.rotation=Quaternion.LookRotation(Vector3.up,Vector3.forward);Physics.SyncTransforms();
            if(kinds[i]!=R4WeaponKind.EmpEmitter)Assert.That(assembly.ClearProjectilePath(gun),Is.True,kinds[i]+" muzzle must be clear");
            var gunState=model.Parts.First(p=>p.Kind==R3PartKind.Gun);int ammo=gunState.Ammo;
            Assert.That(bank.TrySpend(bank.Current),Is.True);assembly.FireGroup(false);
            Assert.That(gun.NextFireTime,Is.Zero,kinds[i]+" must not fire when empty");
            Assert.That(gunState.Ammo,Is.EqualTo(ammo),kinds[i]+" must not spend ammo when empty");
            bank.Recharge(1000);float before=bank.Current;assembly.FireGroup(false);
            Assert.That(bank.Current,Is.LessThan(before),kinds[i]+" must spend energy when used");
            Assert.That(gun.NextFireTime,Is.GreaterThan(Time.time),kinds[i]+" must actually fire");
            UnityEngine.Object.Destroy(vehicle.gameObject);R1EncounterManager.ClearTransientObjects();yield return null;
        }
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator ContinuousWeaponsDrainTheSameBankWhileOperating()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();Time.timeScale=1f;
        float deadline=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);Assert.That(run.OpenSandboxGarage(),Is.True);
        // The garage pauses the whole game; this isolated physics probe needs real fixed steps.
        Time.timeScale=1f;
        var palette=run.GetComponent<R3VisualPalette>();
        var kinds=new[]{R4WeaponKind.BeamCutter,R4WeaponKind.RotarySaw};
        for(int i=0;i<kinds.Length;i++)
        {
            var design=R3VehicleCatalog.Create(R3VehicleKind.Light);
            foreach(var p in design.Parts)if(p.Kind==R3PartKind.Gun){p.Weapon=kinds[i];break;}
            var vehicle=R3VehicleAssembly.Build(new R3VehicleModel(design),new R1RunStats(),palette,new Vector3(600+i*20,30,100),Quaternion.identity);
            vehicle.GetComponent<Rigidbody>().isKinematic=true;yield return null;
            var assembly=vehicle.GetComponent<R3VehicleAssembly>();var bank=vehicle.GetComponent<VehicleEnergyBank>();
            R3MountedPart gun=null;foreach(var p in assembly.Parts)if(p.Kind==R3PartKind.Gun){gun=p;break;}
            Assert.That(gun,Is.Not.Null);gun.GunPivot.rotation=Quaternion.LookRotation(Vector3.up,Vector3.forward);Physics.SyncTransforms();
            Assert.That(assembly.ClearProjectilePath(gun),Is.True,kinds[i]+" must have a clear path");
            bank.Recharge(1000);float before=bank.Current;
            assembly.SetGroupTriggers(true,false);
            float until=Time.fixedTime+.6f;deadline=Time.realtimeSinceStartup+10;
            while(Time.fixedTime<until&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(Time.fixedTime,Is.GreaterThanOrEqualTo(until));
            Assert.That(bank.Current,Is.LessThan(before-1f),kinds[i]+" must consume more than passive regeneration while held");
            assembly.SetGroupTriggers(false,false);UnityEngine.Object.Destroy(vehicle.gameObject);yield return null;
        }
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator RealEnemyBatteryRaisesAndLosesCapacityWhenDestroyed()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run,Is.Not.Null);
        Assert.That(run.StartRun(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);
        P12PipelineSafeEnemyTurret enemy=null;VehicleEnergyBattery battery=null;
        foreach(var item in run.Encounter.Enemies)
        {
            var candidate=item.GetComponentInChildren<VehicleEnergyBattery>();
            if(candidate!=null){enemy=item;battery=candidate;break;}
        }
        Assert.That(enemy,Is.Not.Null,"An enemy must actually carry a battery.");
        var bank=enemy.GetComponent<VehicleEnergyBank>();Assert.That(bank,Is.Not.Null);
        Assert.That(bank.Maximum,Is.EqualTo(600));bank.Recharge(1000);
        Assert.That(bank.Current,Is.EqualTo(600));
        var hp=battery.GetComponent<P12PipelineSafeDamageable>();
        hp.ApplyDamage(100000,battery.transform.position,Vector3.zero);
        bank.Refresh();Assert.That(bank.Maximum,Is.EqualTo(500));
        Assert.That(bank.Current,Is.LessThanOrEqualTo(500));
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator ActualEncounterCreatesAChargingFieldOnGround()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+15;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run,Is.Not.Null);
        Assert.That(run.StartRun(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        while((run.Transitioning||UnityEngine.Object.FindFirstObjectByType<EnergyChargingField>()==null)&&Time.realtimeSinceStartup<deadline)yield return null;
        var field=UnityEngine.Object.FindFirstObjectByType<EnergyChargingField>();
        Assert.That(field,Is.Not.Null,"The live arena must expose a charging field.");
        Assert.That(field.GetComponentsInChildren<Collider>().Any(c=>c.enabled),Is.False,"A field must not obstruct vehicles.");
        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator CleanupPlayMode()
    {
        Time.timeScale=1f;
        if(Application.isPlaying)yield return new ExitPlayMode();
    }

    [Test]
    public void SpendRegenerateAndRechargeRespectCurrentCapacity()
    {
        var energy = NewEnergy();
        Call(energy, "Refresh", 1, 0, true);
        Call(energy, "Fill");
        Assert.That((bool)Call(energy, "TrySpend", 550f), Is.True);
        Assert.That((bool)Call(energy, "TrySpend", 51f), Is.False);
        Assert.That(Number(energy, "Current"), Is.EqualTo(50));
        Call(energy, "Advance", 2f);
        Assert.That(Number(energy, "Current"), Is.EqualTo(60));
        Call(energy, "Recharge", 1000f);
        Assert.That(Number(energy, "Current"), Is.EqualTo(600));
        Call(energy, "Refresh", 0, 1, true);
        Assert.That(Number(energy, "Current"), Is.EqualTo(475));
        Assert.That((bool)Call(energy, "TrySpend", float.NaN), Is.False);
    }

    [Test]
    public void EveryPartOccupiesOneCellAtEveryQuarterTurn()
    {
        var origin = new Vector3Int(2, 3, -4);
        foreach (R3PartKind kind in Enum.GetValues(typeof(R3PartKind)))
        for (int x = 0; x < 4; x++)
        for (int y = 0; y < 4; y++)
        for (int z = 0; z < 4; z++)
        {
            var cells = R3VehicleModel.OccupiedCells(origin, kind, new Vector3Int(x, y, z)).ToArray();
            Assert.That(cells, Is.EqualTo(new[] { origin }), $"{kind} rotation {x},{y},{z}");
        }
    }

    [Test]
    public void AllModulesUseTheOrdinarySingleCellPhysicalGeometry()
    {
        foreach (R3PartKind kind in Enum.GetValues(typeof(R3PartKind)))
            Assert.That(R3VehicleModel.UsesWideFootprint(kind), Is.False, kind.ToString());
    }
}
