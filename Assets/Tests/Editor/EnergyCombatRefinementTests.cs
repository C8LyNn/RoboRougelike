using System;
using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;
using RoboRougelike.R1;

public sealed class EnergyCombatRefinementTests
{
    [Test] public void ChargingFieldChoosesSurfaceNearVehicleHeightRegardlessOfRaycastOrder()
    {
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);var upper=GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            var origin=new Vector3(12000,0,12000);
            floor.transform.position=origin+Vector3.down*.5f;floor.transform.localScale=new Vector3(8,1,8);
            upper.transform.position=origin+Vector3.up*4;upper.transform.localScale=new Vector3(2,1,2);
            Physics.SyncTransforms();
            var hits=Physics.RaycastAll(origin+Vector3.up*12,Vector3.down,20,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider==floor.GetComponent<Collider>()||h.collider==upper.GetComponent<Collider>())
                .OrderByDescending(h=>h.point.y).ToArray();
            Assert.That(hits.Length,Is.EqualTo(2));Assert.That(hits[0].collider,Is.EqualTo(upper.GetComponent<Collider>()));
            var method=typeof(R1EnergyFieldDirector).GetMethod("TrySelectFieldSurface",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public);
            Assert.That(method,Is.Not.Null,"Surface selection must not depend on Physics.RaycastAll ordering.");
            var args=new object[]{hits,origin.y+.7f,default(RaycastHit)};
            Assert.That((bool)method.Invoke(null,args),Is.True);Assert.That(((RaycastHit)args[2]).collider,Is.EqualTo(floor.GetComponent<Collider>()));
            Array.Reverse(hits);args[0]=hits;Assert.That((bool)method.Invoke(null,args),Is.True);
            Assert.That(((RaycastHit)args[2]).collider,Is.EqualTo(floor.GetComponent<Collider>()));
        }
        finally{UnityEngine.Object.DestroyImmediate(floor);UnityEngine.Object.DestroyImmediate(upper);}
    }
    static System.Collections.Generic.List<string> InvokeEnemyGunAndCollectLogs(object enemy,System.Reflection.MethodInfo update,object list)
    {
        var messages=new System.Collections.Generic.List<string>();
        Application.LogCallback capture=(condition,stack,logType)=>messages.Add(condition);
        Application.logMessageReceived+=capture;
        try{update.Invoke(enemy,new[]{list,"extra-gun/"});}
        finally{Application.logMessageReceived-=capture;}
        return messages;
    }
    static void Invoke(VehicleEnergyState state,string name,params object[] args)
    {
        var method=typeof(VehicleEnergyState).GetMethod(name);
        Assert.That(method,Is.Not.Null,$"Missing {name}");
        method.Invoke(state,args);
    }
    [Test] public void StartsFullIncludingInstalledBatteries()
    {
        var design=new R3VehicleDesign();
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        design.Parts.Add(new R3PartState(Vector3Int.up,R3PartKind.Battery));
        var model=new R3VehicleModel(design);
        Assert.That(model.Energy.Maximum,Is.EqualTo(600));
        Assert.That(model.Energy.Current,Is.EqualTo(600));
    }
    [Test] public void InitialDeploymentFillsNewBatteryButLaterDeploymentsDoNotRefill()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(100);
        var cell=Enumerable.Range(-6,13).SelectMany(x=>Enumerable.Range(-7,15).Select(z=>new Vector3Int(x,1,z)))
            .First(c=>model.PlacementError(c,R3PartKind.Battery)==null);
        Assert.That(model.Buy(cell,R3PartKind.Battery),Is.True);
        var method=typeof(R3VehicleModel).GetMethod("PrepareInitialEnergy");Assert.That(method,Is.Not.Null);
        method.Invoke(model,null);Assert.That(model.Energy.Maximum,Is.EqualTo(600));Assert.That(model.Energy.Current,Is.EqualTo(600));
        model.Energy.TrySpend(50);method.Invoke(model,null);Assert.That(model.Energy.Current,Is.EqualTo(550));
    }    [Test] public void MovingAndStationaryRegenerationUsesTenAndFortyPerSecond()
    {
        var energy=new VehicleEnergyState();energy.TrySpend(500);
        Invoke(energy,"AdvanceWithMotion",1f,false);Assert.That(energy.Current,Is.EqualTo(10).Within(.01f));
        Invoke(energy,"AdvanceWithMotion",1f,true);Assert.That(energy.Current,Is.EqualTo(50).Within(.01f));
    }
    [Test] public void ChargingFieldImmediatelyRestoresHalfMissingAndAddsFivePercentForTenSeconds()
    {
        var energy=new VehicleEnergyState();energy.TrySpend(500);
        Invoke(energy,"ApplyFieldCharge");Assert.That(energy.Current,Is.EqualTo(250).Within(.01f));
        Invoke(energy,"AdvanceWithMotion",1f,false);Assert.That(energy.Current,Is.EqualTo(285).Within(.01f));
        energy.TrySpend(285);Invoke(energy,"AdvanceWithMotion",10f,false);
        Assert.That(energy.Current,Is.EqualTo(325).Within(.01f));
        Invoke(energy,"AdvanceWithMotion",1f,false);Assert.That(energy.Current,Is.EqualTo(335).Within(.01f));
    }
    [Test] public void PickupsStackAndEachExpiresAfterFiveSeconds()
    {
        var energy=new VehicleEnergyState();energy.Refresh(5,0,true);energy.TrySpend(500);
        Invoke(energy,"ApplyEnergyPickup");Invoke(energy,"ApplyEnergyPickup");
        Invoke(energy,"AdvanceWithMotion",1f,false);Assert.That(energy.Current,Is.EqualTo(210).Within(.01f));
        energy.TrySpend(210);Invoke(energy,"AdvanceWithMotion",5f,false);Assert.That(energy.Current,Is.EqualTo(850).Within(.01f));
        Invoke(energy,"AdvanceWithMotion",1f,false);Assert.That(energy.Current,Is.EqualTo(860).Within(.01f));
    }
    [Test] public void ActiveCostsAreHalvedAndPassiveReservationRemainsTwentyFive()
    {
        Assert.That(VehicleEnergyCosts.BasicShot,Is.Zero);
        Assert.That(VehicleEnergyCosts.ConventionalShot(R4WeaponKind.Automatic),Is.EqualTo(2.5f));
        Assert.That(VehicleEnergyCosts.ConventionalShot(R4WeaponKind.Heavy),Is.EqualTo(2.5f));
        Assert.That(VehicleEnergyCosts.Shotgun,Is.EqualTo(5f));
        Assert.That(VehicleEnergyCosts.BoosterActivation,Is.EqualTo(7.5f));
        Assert.That(VehicleEnergyState.PassiveReservation,Is.EqualTo(25f));
    }
    [Test] public void PurchasingAddsIndividualPartsToInventoryBeforeMounting()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(100);
        int before=model.Scrap,price=model.Price(R3PartKind.Wheel);
        Assert.That(model.PurchaseToInventory(R3PartKind.Wheel),Is.True);
        Assert.That(model.PurchaseToInventory(R3PartKind.Wheel),Is.True);
        var items=model.Inventory.Where(p=>p.Kind==R3PartKind.Wheel).ToArray();
        Assert.That(items.Length,Is.EqualTo(2));Assert.That(items[0].Id,Is.Not.EqualTo(items[1].Id));
        Assert.That(items.All(p=>p.Health==1),Is.True);Assert.That(model.Scrap,Is.EqualTo(before-2*price));
        Assert.That(model.Parts.Count(p=>p.Kind==R3PartKind.Wheel),Is.EqualTo(4));
    }
    [Test] public void ArmedEnemiesPeriodicallyTryToRamWithoutDiscardingTheirWeapon()
    {
        var method=typeof(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret).GetMethod("ShouldRam");
        Assert.That(method,Is.Not.Null,"Mobile armed enemies need a periodic charge.");
        bool Decide(bool armed,R3EnemyArchetype kind,float distance,float time)=>(bool)method.Invoke(null,new object[]{armed,kind,distance,time});
        Assert.That(Decide(true,R3EnemyArchetype.Scout,15,1),Is.True);
        Assert.That(Decide(true,R3EnemyArchetype.Scout,15,6),Is.False);
        Assert.That(Decide(true,R3EnemyArchetype.Gunner,6,6),Is.True);
        Assert.That(Decide(false,R3EnemyArchetype.Gunner,15,6),Is.True);
    }
    [Test] public void SelfDestructionRequiresFiveSecondsDisabledAndNearbyPlayer()
    {
        var type=typeof(R3VehicleModel).Assembly.GetType("RoboRougelike.R1.EnemySelfDestructTimer");
        Assert.That(type,Is.Not.Null,"A timed disabled-enemy policy must exist.");
        var timer=Activator.CreateInstance(type);var step=type.GetMethod("Advance");Assert.That(step,Is.Not.Null);
        bool Step(bool disabled,bool nearby,float seconds)=>(bool)step.Invoke(timer,new object[]{disabled,nearby,seconds});
        Assert.That(Step(true,true,4.99f),Is.False);Assert.That(Step(true,false,.02f),Is.False);
        Assert.That(Step(true,true,.01f),Is.True);
        timer=Activator.CreateInstance(type);Assert.That(Step(true,true,4),Is.False);
        Assert.That(Step(false,true,1),Is.False);Assert.That(Step(true,true,1),Is.False);
    }
    [Test] public void StartingVehiclesGetTheirDifferencesFromRealParts()
    {
        var light=R3VehicleCatalog.Create(R3VehicleKind.Light);
        var medium=R3VehicleCatalog.Create(R3VehicleKind.Standard);
        var heavy=R3VehicleCatalog.Create(R3VehicleKind.Heavy);
        int Wheels(R3VehicleDesign d)=>d.Parts.Count(p=>p.Kind==R3PartKind.Wheel);
        int Guns(R3VehicleDesign d)=>d.Parts.Count(p=>p.Kind==R3PartKind.Gun);
        float Capacity(R3VehicleDesign d)=>new R3VehicleModel(d).Energy.Maximum;
        Assert.That(Wheels(light),Is.LessThan(Wheels(medium)));
        Assert.That(Wheels(medium),Is.LessThan(Wheels(heavy)));
        Assert.That(Guns(light),Is.LessThan(Guns(heavy)));
        Assert.That(Capacity(light),Is.LessThan(Capacity(medium)));
        Assert.That(Capacity(medium),Is.LessThan(Capacity(heavy)));
        Assert.That(heavy.Parts.Count(p=>p.Kind==R3PartKind.Battery),Is.EqualTo(3));
        Assert.That(heavy.Parts.Count(p=>p.Kind==R3PartKind.Armor),Is.LessThan(100));
        Assert.That(light.Parts.Where(p=>p.Kind==R3PartKind.Wheel).Max(p=>p.Cell.z),Is.EqualTo(2));
        Assert.That(light.Parts.Where(p=>p.Kind==R3PartKind.Wheel).Min(p=>p.Cell.z),Is.EqualTo(-2));
        Assert.That(light.Speed,Is.EqualTo(medium.Speed));Assert.That(medium.Speed,Is.EqualTo(heavy.Speed));
        Assert.That(light.CoreHp,Is.EqualTo(heavy.CoreHp));
    }
    [UnityTest] public IEnumerator StandardStarterCanFireBothRealGunsWithoutHittingItsOwnConstruction()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var paletteObject=new GameObject("starter gun QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        yield return null;
        var assembly=rig.GetComponent<R3VehicleAssembly>();
        var guns=assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun).ToArray();
        Assert.That(guns.Length,Is.EqualTo(2),"The standard starter carries two physical guns.");
        R1EncounterManager.ClearTransientObjects();Physics.SyncTransforms();assembly.FireGuns();
        Assert.That(guns.All(g=>g.NextFireTime>0),Is.True,"Both guns must have unobstructed firing lanes.");
        var shots=UnityEngine.Object.FindObjectsByType<RoboRougelike.R4.R4ProjectileOrigin>(FindObjectsSortMode.None);
        Assert.That(shots.Select(s=>s.PartId).Distinct().Count(),Is.EqualTo(2),"Both owned guns must spawn their own physical shot.");
        UnityEngine.Object.Destroy(rig.gameObject);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator EscapeClosesCatalogBeforeOpeningPause()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var hud=run.GetComponent<R1RunHUD>();var field=typeof(R1RunHUD).GetField("catalogOpen",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(field,Is.Not.Null);field.SetValue(hud,true);run.HandleEscape();
        Assert.That((bool)field.GetValue(hud),Is.False);Assert.That(run.Paused,Is.False,"Closing catalog must not pause gameplay.");
        yield return null;run.HandleEscape();Assert.That(run.Paused,Is.True,"A second escape opens the existing pause menu.");
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator PurchasedInventoryInstanceMountsWithoutLosingItsHealthOrChargingTwice()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run.SelectVehicle(R3VehicleKind.Light),Is.True);
        Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;int original=model.Scrap,price=model.Price(R3PartKind.Wheel);
        Assert.That(run,Is.Not.Null);Assert.That(model,Is.Not.Null);Assert.That(run.Progress,Is.Not.Null);
        bool purchased=run.GaragePurchase(R3PartKind.Wheel);
        Debug.Log("GARAGE-TEST after purchase="+purchased+" count="+model.Inventory.Count);
        Assert.That(purchased,Is.True,"Purchase through real garage");
        Assert.That(model.Inventory,Is.Not.Null);Assert.That(model.Inventory.Count,Is.GreaterThan(0));
        var snapshot=model.Inventory.ToArray();
        var part=snapshot[0];
        Assert.That(part,Is.Not.Null);part.Health=.55f;
        Assert.That(model.Scrap,Is.EqualTo(original-price));
        Vector3Int? destination=null;
        for(int y=0;y<5&&!destination.HasValue;y++)for(int x=-6;x<=6&&!destination.HasValue;x++)for(int z=-7;z<=7;z++)
        {var cell=new Vector3Int(x,y,z);if(model.OwnedPlacementError(part.Id,cell,Vector3Int.zero)==null){destination=cell;break;}}
        Assert.That(destination.HasValue,Is.True,"A valid physical placement must exist.");
        Assert.That(run.Garage,Is.Not.Null);run.Garage.ChooseInventory(part.Id);run.Garage.Select(destination.Value);
        Assert.That(run.Garage.ClickTarget(),Is.True,run.Garage.Message);
        var mounted=model.At(destination.Value);Assert.That(mounted,Is.Not.Null,"Purchased instance must exist at target after mounting.");Assert.That(mounted.Id,Is.EqualTo(part.Id));Assert.That(mounted.Health,Is.EqualTo(.55f));
        Assert.That(model.Scrap,Is.EqualTo(original-price));bool retained=false;foreach(var stored in model.Inventory)if(stored.Id==part.Id)retained=true;Assert.That(retained,Is.False);
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator DisabledRealEnemyExplodesOnlyAfterDelayNearPlayer()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run.StartRun(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        var enemy=run.Encounter.Enemies.First();foreach(var other in run.Encounter.Enemies.Skip(1))other.SetCombatActive(false);
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var primary=(RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable)enemy.GetType().GetField("weaponBlock",flags).GetValue(enemy);
        Assert.That(primary,Is.Not.Null);primary.ApplyDamage(100000,primary.transform.position,Vector3.zero);
        foreach(var gun in enemy.AdditionalWeapons)gun.Block.ApplyDamage(100000,gun.Block.transform.position,Vector3.zero);
        var wheels=(IEnumerable)enemy.GetType().GetField("driveWheels",flags).GetValue(enemy);
        foreach(RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable wheel in wheels)wheel.ApplyDamage(100000,wheel.transform.position,Vector3.zero);
        Assert.That(enemy.WeaponOnline,Is.False);Assert.That(enemy.ActiveWheelCount,Is.Zero);
        var body=run.Player.GetComponent<Rigidbody>();body.isKinematic=true;body.position=enemy.transform.position+Vector3.back*3;
        run.Player.transform.position=body.position;Physics.SyncTransforms();
        float hp=run.Player.Core.CurrentHp;float until=Time.time+4f;
        while(Time.time<until)yield return null;
        Assert.That(enemy.IsDefeated,Is.False,"Must wait at least five seconds.");
        deadline=Time.realtimeSinceStartup+15;
        while(!enemy.IsDefeated&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.True);Assert.That(run.Player.Core.CurrentHp,Is.LessThan(hp));
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator FinalEnemyDropsRemainCollectibleBeforeRewards()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run.StartRun(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        foreach(var enemy in run.Encounter.Enemies)enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat),"Player needs a short chance to collect the drops.");
        var type=typeof(VehicleEnergyState).Assembly.GetType("RoboRougelike.R3.EnergyPickup");
        Assert.That(UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Length,Is.GreaterThan(0));
        deadline=Time.realtimeSinceStartup+25;
        while(run.Progress.State==R1RunState.Combat&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage),"Rewards must still open after the collection window.");
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator ExistingBattleArenaHasMultipleLargePhysicalRamps()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run.StartRun(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        var ramps=run.Arena.Layout.GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith("Rampa terenu")).ToArray();
        Assert.That(ramps.Length,Is.GreaterThanOrEqualTo(4));
        Assert.That(ramps.All(c=>c.enabled&&!c.isTrigger&&c.transform.lossyScale.z>=10),Is.True);
        Assert.That(ramps.All(c=>Vector3.Angle(c.transform.up,Vector3.up)>=15),Is.True);
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator NormalEncounterEnemiesCarryAnAdditionalRealWeapon()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run.StartRun(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        var enemy=run.Encounter.Enemies.First();
        var property=enemy.GetType().GetProperty("AdditionalWeapons");Assert.That(property,Is.Not.Null);
        var guns=((IEnumerable)property.GetValue(enemy)).Cast<object>().ToArray();Assert.That(guns.Length,Is.GreaterThanOrEqualTo(1));
        var gun=guns[0];var block=(RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable)gun.GetType().GetField("Block").GetValue(gun);
        Assert.That(block,Is.Not.Null);Assert.That(block.transform.IsChildOf(enemy.transform),Is.True);
        Assert.That(block.IsDead,Is.False);Assert.That(enemy.WeaponOnline,Is.True);
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator AdditionalEnemyWeaponActuallyFiresPhysicalProjectile()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run.StartRun(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        var enemy=run.Encounter.Enemies.First();foreach(var other in run.Encounter.Enemies.Skip(1))other.SetCombatActive(false);
        Assert.That(enemy.AdditionalWeapons.Count,Is.GreaterThan(0));var gun=enemy.AdditionalWeapons[0];float first=gun.NextShot;
        var body=run.Player.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position=enemy.transform.position+Vector3.back*8+Vector3.up;run.Player.transform.position=body.position;Physics.SyncTransforms();
        deadline=Time.realtimeSinceStartup+18;
        while(gun.NextShot<=first&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(gun.NextShot,Is.GreaterThan(first),"The added weapon must actually fire, not just exist visually.");
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator AdditionalEnemyGunDoesNotFireAtAnUndefinedZeroDistanceDirection()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        run.Encounter.SetCombatActive(false);var enemy=run.Encounter.Enemies.First();
        Assert.That(enemy.AdditionalWeapons.Count,Is.GreaterThan(0));var gun=enemy.AdditionalWeapons[0];
        Assert.That(gun.Online(enemy.transform),Is.True);
        var target=run.Player.Core.GetComponent<Collider>();Assert.That(target,Is.Not.Null);
        var body=run.Player.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position+=gun.Pivot.position-target.bounds.center;Physics.SyncTransforms();
        Assert.That(Vector3.Distance(gun.Pivot.position,target.bounds.center),Is.LessThan(.0001f));
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var type=typeof(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret);
        var targetField=type.GetField("currentTarget",flags);Assert.That(targetField,Is.Not.Null);targetField.SetValue(enemy,target);
        var clockField=type.GetField("nextTargetTime",flags);Assert.That(clockField,Is.Not.Null);clockField.SetValue(enemy,Time.time+10f);
        var listField=type.GetField("additionalGuns",flags);Assert.That(listField,Is.Not.Null);var list=listField.GetValue(enemy);
        var update=type.GetMethod("UpdateVariantWeapons",flags);Assert.That(update,Is.Not.Null);
        gun.NextShot=0;var energy=enemy.GetComponent<VehicleEnergyBank>();Assert.That(energy,Is.Not.Null);float before=energy.Current;
        var messages=InvokeEnemyGunAndCollectLogs(enemy,update,list);
        Assert.That(messages.Any(m=>m!=null&&m.Contains("Look rotation viewing vector is zero")),Is.False,
            "Overlapping vehicles must not emit a zero-direction warning each frame.");
        Assert.That(gun.NextShot,Is.Zero,"An undefined direction must not spend a shot or start cooldown.");
        Assert.That(energy.Current,Is.EqualTo(before));
    }
    [UnityTest] public IEnumerator PrimaryEnemyGunDoesNotSpendAZeroDirectionShot()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        run.Encounter.SetCombatActive(false);var enemy=run.Encounter.Enemies.First();
        foreach(var extra in enemy.AdditionalWeapons)extra.Block.transform.SetParent(null,true);
        Assert.That(enemy.WeaponOnline,Is.True,"The primary gun remains mounted and operational.");
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var type=typeof(RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret);
        var muzzle=(Transform)type.GetField("muzzle",flags).GetValue(enemy);Assert.That(muzzle,Is.Not.Null);
        var target=run.Player.Core.GetComponent<Collider>();var body=run.Player.GetComponent<Rigidbody>();body.isKinematic=true;
        body.position+=muzzle.position-target.bounds.center;Physics.SyncTransforms();
        Assert.That(Vector3.Distance(muzzle.position,target.bounds.center),Is.LessThan(.0001f));
        type.GetField("currentTarget",flags).SetValue(enemy,target);type.GetField("nextTargetTime",flags).SetValue(enemy,Time.time+10f);
        type.GetField("nextFireTime",flags).SetValue(enemy,0f);type.GetField("turnSpeed",flags).SetValue(enemy,0f);
        enemy.SetCombatActive(true);type.GetMethod("Update",flags).Invoke(enemy,null);
        Assert.That((float)type.GetField("nextFireTime",flags).GetValue(enemy),Is.Zero,
            "An undefined muzzle-to-target direction must not consume a shot or cooldown.");
    }
    [UnityTest] public IEnumerator DefeatedEnemyDropsCollectibleThatGivesTimedPlayerEnergy()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;
        Assert.That(run.StartRun(),Is.True);Assert.That(run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Encounter.Enemies.Count,Is.GreaterThan(1));
        var bank=run.Player.GetComponent<VehicleEnergyBank>();bank.TrySpend(bank.Current);
        var enemy=run.Encounter.Enemies[0];enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
        yield return new WaitForFixedUpdate();
        var type=typeof(VehicleEnergyState).Assembly.GetType("RoboRougelike.R3.EnergyPickup");
        Assert.That(type,Is.Not.Null,"A physical collectible must exist.");
        var found=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None);
        Assert.That(found.Length,Is.GreaterThan(0),"A defeated enemy must drop energy.");
        var pickup=(Component)found[0];pickup.transform.position=run.Player.Core.transform.position;
        Physics.SyncTransforms();float before=bank.Current;
        deadline=Time.realtimeSinceStartup+3;
        while(pickup!=null&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(pickup==null,Is.True,"Pickup disappears after collection.");
        float fixedStart=Time.fixedTime;deadline=Time.realtimeSinceStartup+10;
        while(Time.fixedTime-fixedStart<.4f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(Time.fixedTime-fixedStart,Is.GreaterThanOrEqualTo(.4f));
        Assert.That(bank.Current,Is.GreaterThan(before+10),"Collected energy grants timed regeneration.");
        yield return new ExitPlayMode();
    }
    [UnityTest] public IEnumerator EnergyPickupFindsPlayerInCrowdedCollisionArea()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;
        Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        AssertCrowdedPickup(run);
    }

    static void AssertCrowdedPickup(R1RunController run)
    {
        var position=run.Player.Core.transform.position;
        var obstacles=new GameObject("Crowded pickup fixture");
        var cube=new GameObject("Crowded energy cube");
        try
        {
            for(int i=0;i<300;i++)
            {
                var obstacle=new GameObject("Obstacle "+i);obstacle.transform.SetParent(obstacles.transform);
                obstacle.transform.position=position;obstacle.AddComponent<BoxCollider>().size=Vector3.one*.1f;
            }
            cube.transform.position=position;var pickup=cube.AddComponent<EnergyPickup>();
            Physics.SyncTransforms();
            var hits=new Collider[128];int count=Physics.OverlapSphereNonAlloc(position,1.1f,hits,~0,QueryTriggerInteraction.Ignore);
            Assert.That(count,Is.EqualTo(hits.Length),"Fixture must overflow the pickup buffer.");
            Assert.That(hits.Any(hit=>hit.GetComponentInParent<R3VehicleAssembly>()?.gameObject==run.Player.gameObject),Is.False,
                "The first buffer must not already include the player.");
            typeof(EnergyPickup).GetMethod("FixedUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(pickup,null);
            Assert.That(cube.activeSelf,Is.False,"A player touching the pickup must collect it despite other nearby colliders.");
        }
        finally {UnityEngine.Object.DestroyImmediate(obstacles);if(cube!=null)UnityEngine.Object.DestroyImmediate(cube);}
    }

    [UnityTest] public IEnumerator ChargingFieldFindsPlayerInCrowdedCollisionArea()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        var run=R1RunController.Instance;
        Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        AssertCrowdedField(run);
    }

    static void AssertCrowdedField(R1RunController run)
    {
        var position=run.Player.Core.transform.position;
        var obstacles=new GameObject("Crowded field fixture");
        var fieldObject=new GameObject("Crowded field");
        try
        {
            for(int i=0;i<1200;i++)
            {
                var obstacle=new GameObject("Obstacle "+i);obstacle.transform.SetParent(obstacles.transform);
                obstacle.transform.position=position;obstacle.AddComponent<BoxCollider>().size=Vector3.one*.1f;
            }
            fieldObject.transform.position=position-Vector3.up;
            var field=fieldObject.AddComponent<EnergyChargingField>();var bank=run.Player.GetComponent<VehicleEnergyBank>();
            bank.TrySpend(bank.Current);Physics.SyncTransforms();
            var hits=new Collider[1024];int count=Physics.OverlapBoxNonAlloc(position,new Vector3(2.5f,1.5f,2.5f),hits,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
            Assert.That(count,Is.EqualTo(hits.Length),"Fixture must overflow the field buffer.");
            Assert.That(hits.Any(hit=>hit.GetComponentInParent<VehicleEnergyBank>()==bank),Is.False,
                "The first buffer must not already include the player.");
            typeof(EnergyChargingField).GetMethod("FixedUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(field,null);
            Assert.That(bank.Current,Is.EqualTo(bank.Maximum*.5f).Within(.001f),"The field must charge the player even when crowded.");
        }
        finally {UnityEngine.Object.DestroyImmediate(obstacles);UnityEngine.Object.DestroyImmediate(fieldObject);}
    }

    [UnityTest] public IEnumerator CrowdedEnergyQueriesHaveBoundedSteadyStateAllocations()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var obstacles=new GameObject("Energy allocation fixture");obstacles.transform.position=new Vector3(14000,2,14000);
        var cube=new GameObject("Energy allocation pickup");cube.transform.position=obstacles.transform.position;
        var fieldObject=new GameObject("Energy allocation field");fieldObject.transform.position=obstacles.transform.position-Vector3.up;
        try
        {
            for(int i=0;i<1200;i++)obstacles.AddComponent<BoxCollider>().size=Vector3.one*.1f;
            var pickup=cube.AddComponent<EnergyPickup>();var field=fieldObject.AddComponent<EnergyChargingField>();
            pickup.enabled=false;field.enabled=false;Physics.SyncTransforms();
            Assert.That(Physics.OverlapSphere(cube.transform.position,1.1f,~0,QueryTriggerInteraction.Ignore).Length,Is.GreaterThan(1024));
            var pickupTick=(Action)Delegate.CreateDelegate(typeof(Action),pickup,typeof(EnergyPickup).GetMethod("FixedUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance));
            var fieldTick=(Action)Delegate.CreateDelegate(typeof(Action),field,typeof(EnergyChargingField).GetMethod("FixedUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance));
            for(int i=0;i<3;i++){pickupTick();fieldTick();}
            using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",16))
            {
                Assert.That(recorder.Valid,Is.True);yield return null;yield return null;
                long idle=recorder.LastValue;
                for(int i=0;i<50;i++){pickupTick();fieldTick();}
                yield return null;long sample=recorder.LastValue;
                var control=new byte[1024*1024];control[0]=1;yield return null;long controlBytes=recorder.LastValue;GC.KeepAlive(control);
                Debug.Log($"[ENERGY_QUERY_ALLOC] queries=50 colliders=1200 idle={idle} sample={sample} control={controlBytes}");
                Assert.That(controlBytes,Is.GreaterThanOrEqualTo(1024*1024),"The profiler must detect a known allocation.");
                Assert.That(sample-idle,Is.LessThan(20000),"Steady crowded queries must avoid a full allocation every tick.");
            }
        }
        finally{UnityEngine.Object.Destroy(obstacles);UnityEngine.Object.Destroy(cube);UnityEngine.Object.Destroy(fieldObject);}
    }

    [UnityTearDown] public IEnumerator Cleanup()
    {if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
