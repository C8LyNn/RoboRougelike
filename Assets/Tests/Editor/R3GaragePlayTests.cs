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

    [UnityTest] public IEnumerator DisarmedEnemyRamsAlongArenaBoundaryWithGroundedWheels()
    {
        var enemy = Run.Encounter.Enemies.First();
        int wheels = enemy.ActiveWheelCount;
        Assert.That(wheels, Is.GreaterThan(0));
        var weapon = enemy.WeaponBlock;
        Assert.That(weapon, Is.Not.Null);
        // Isolate movement with intact wheels; a large hit now correctly
        // propagates through connections and can destroy those wheels too.
        weapon.gameObject.SetActive(false);
        foreach (var gun in enemy.AdditionalWeapons)
            if (gun.Block != null && !gun.Block.IsDead) gun.Block.gameObject.SetActive(false);
        yield return null;
        Assert.That(enemy.IsDefeated, Is.False);
        Assert.That(enemy.WeaponOnline, Is.False);
        Assert.That(enemy.ActiveWheelCount, Is.EqualTo(wheels));
        var body = enemy.GetComponent<Rigidbody>();
        var drive = enemy.GetComponent<RoboRougelike.R6.R6VehicleDrive>();
        body.position = new Vector3(37.99f, 0, 0);
        Run.Player.MoveTo(new Vector3(60, 0.7f, 0), Quaternion.identity);
        Physics.SyncTransforms();
        Vector3 before = body.position;
        float end = Time.time + 3.2f;
        int grounded = 0;
        while (Time.time < end)
        {
            yield return MOVSeriesDeviceTests.FixedStep();
            if (drive.GroundedWheels > 0) grounded++;
            Assert.That(body.position.x, Is.LessThanOrEqualTo(38.01f), "Przeciwnik przekroczył granicę areny.");
        }
        Assert.That(grounded, Is.GreaterThan(0), "Ruch wymaga kontaktu kół z podłożem.");
        Assert.That(Mathf.Abs(body.position.z - before.z), Is.GreaterThan(0.3f), "Rozbrojony przeciwnik powinien poruszać się wzdłuż granicy.");
        Assert.That(Mathf.Abs(body.position.x), Is.LessThanOrEqualTo(38.01f));
    }

    [UnityTest] public IEnumerator DisarmedEnemyRetreatsWhenBothBoundaryTangentsAreBlocked()
    {
        var enemy = Run.Encounter.Enemies.First();
        enemy.SetCombatActive(false);
        var weapon = enemy.WeaponBlock;
        Assert.That(weapon, Is.Not.Null);
        weapon.gameObject.SetActive(false);
        foreach (var gun in enemy.AdditionalWeapons)
            if (gun.Block != null && !gun.Block.IsDead) gun.Block.gameObject.SetActive(false);
        yield return null;
        Assert.That(enemy.WeaponOnline, Is.False);
        var body = enemy.GetComponent<Rigidbody>();
        var drive = enemy.GetComponent<RoboRougelike.R6.R6VehicleDrive>();
        // Isolate a measured lane from the arena's own obstacles, with a real
        // floor, rigidbody, wheels and colliders on both sides.
        var center = new Vector3(1000, 0, 1000);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = center + Vector3.down * .5f;
        floor.transform.localScale = new Vector3(40, 1, 40);
        enemy.ConfigureMovementBounds(new Bounds(center, new Vector3(10, 100, 10)));
        body.position = center + new Vector3(4.99f, 0, 0); body.rotation = Quaternion.identity;
        body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
        var target = Run.Player.GetComponent<Rigidbody>(); target.isKinematic = true;
        target.position = center + new Vector3(10, .7f, 0); target.rotation = Quaternion.identity;
        Physics.SyncTransforms(); body.WakeUp();
        yield return Until(() => { drive.RefreshMass(); return drive.GroundedWheels > 0; }, "Koła przeciwnika muszą dotykać podłoża.", 5);
        var walls=new System.Collections.Generic.List<Collider>();
        foreach (float offset in new[] { -1.05f, 1.05f })
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name=offset<0?"Boundary wall south":"Boundary wall north";
            wall.transform.position = center + new Vector3(5, 1.5f, offset);
            wall.transform.localScale = new Vector3(5, 3, .2f);
            walls.Add(wall.GetComponent<Collider>());
        }
        Physics.SyncTransforms();
        var probe=enemy.GetType().GetMethod("CanMove",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(probe,Is.Not.Null);
        Assert.That((bool)probe.Invoke(enemy,new object[]{Vector3.left,.2f,true}),Is.True,
            "The robot must be allowed to probe away along the corridor, not treat side walls as a rear wall.");
        Assert.That((bool)probe.Invoke(enemy,new object[]{Vector3.forward,.2f,true}),Is.False,
            "A probe directed into a real side wall must remain blocked.");
        body.rotation=Quaternion.Euler(0,-7,0);Physics.SyncTransforms();
        Assert.That((bool)probe.Invoke(enemy,new object[]{Vector3.left,.2f,true}),Is.True,
            "A slightly rotated probe overlapping side walls must still allow movement parallel to them.");
        Assert.That((bool)probe.Invoke(enemy,new object[]{Vector3.forward,.2f,true}),Is.False,
            "Rotating the probe must not allow driving into a wall.");
        body.rotation=Quaternion.identity;Physics.SyncTransforms();
        var before = body.position;
        enemy.SetCombatActive(true);
        float end = Time.time + 2;
        int grounded = 0;float nextTrace=Time.time;
        while (Time.time < end)
        {
            yield return MOVSeriesDeviceTests.FixedStep();
            if (drive.GroundedWheels > 0) grounded++;
            if(Time.time>=nextTrace)
            {
                nextTrace=Time.time+.3f;
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var tangent=(Vector3)enemy.GetType().GetField("obstacleTangent",flags).GetValue(enemy);
                var until=(float)enemy.GetType().GetField("navigationUntil",flags).GetValue(enemy);
                var names=new System.Collections.Generic.HashSet<string>();
                foreach(var collider in body.GetComponentsInChildren<Collider>())
                {
                    if(!collider.enabled||collider.isTrigger||collider.attachedRigidbody!=body)continue;
                    foreach(var wall in walls)
                        if(Physics.ComputePenetration(collider,collider.transform.position,collider.transform.rotation,wall,wall.transform.position,wall.transform.rotation,out _,out _))names.Add(wall.name);
                }
                var surfaces=string.Join(",",names);
                Debug.Log($"Boundary retreat trace t={Time.time:F2} pos={body.position} vel={body.linearVelocity} yaw={body.rotation.eulerAngles.y:F1} spin={body.angularVelocity.y:F2} grounded={drive.GroundedWheels} tangent={tangent} nav={until-Time.time:F2} dead={enemy.IsDefeated} contacts={surfaces}");
            }
        }
        Assert.That(grounded, Is.GreaterThan(0));
        Assert.That(body.position.x - before.x, Is.LessThan(-.3f), "Przy zablokowanych stycznych przeciwnik powinien wycofać się do środka areny.");
    }

    [UnityTest] public IEnumerator RotatedEnemyObstacleSweepTracksItsActualOrientation()
    {
        var enemy=Run.Encounter.Enemies.First();enemy.SetCombatActive(false);
        var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var extent=(Vector3)enemy.GetType().GetField("movementHalfExtents",flags).GetValue(enemy);
        Assert.That(extent.x,Is.GreaterThan(extent.z+.1f),"The fixture needs a non-square enemy footprint.");
        var origin=new Vector3(1000,2,1000);body.position=origin;body.rotation=Quaternion.Euler(0,90,0);
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Obstacle beside rotated enemy";
        wall.transform.localScale=new Vector3(.1f,1,.1f);
        wall.transform.position=origin+new Vector3((extent.x+extent.z)*.5f,.75f,extent.z+1);
        Physics.SyncTransforms();yield return null;
        var canMove=enemy.GetType().GetMethod("CanMove",flags);Assert.That(canMove,Is.Not.Null);
        bool Sweep()=>(bool)canMove.Invoke(enemy,new object[]{Vector3.forward,2f,false});
        Assert.That(Sweep(),Is.True,"An obstacle outside the rotated vehicle's sweep must not block it.");
        wall.transform.position=origin+new Vector3(0,.75f,extent.x+1);
        Physics.SyncTransforms();
        Assert.That(Sweep(),Is.False,"A wall in front of the rotated vehicle must still block movement.");
        UnityEngine.Object.Destroy(wall);
    }

    [UnityTest] public IEnumerator RotatedClearanceProbeAllowsParallelEscapeButStillBlocksWalls()
    {
        var enemy=Run.Encounter.Enemies.First();enemy.SetCombatActive(false);
        var body=enemy.GetComponent<Rigidbody>();body.isKinematic=true;
        var origin=new Vector3(1000,2,1000);body.position=origin;body.rotation=Quaternion.Euler(0,-7,0);
        var walls=new System.Collections.Generic.List<GameObject>();
        try
        {
            foreach(float z in new[]{-1.05f,1.05f})
            {
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);walls.Add(wall);
                wall.transform.position=origin+new Vector3(0,.75f,z);wall.transform.localScale=new Vector3(5,3,.2f);
            }
            Physics.SyncTransforms();
            var method=enemy.GetType().GetMethod("CanMove",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            bool Clear(Vector3 direction)=>(bool)method.Invoke(enemy,new object[]{direction,.2f,true});
            Assert.That(Clear(Vector3.left),Is.True,"Side walls overlapping the rotated clearance box cannot block parallel escape.");
            Assert.That(Clear(Vector3.forward),Is.False,"The robot cannot drive into a side wall.");
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);walls.Add(blocker);
            blocker.transform.position=origin+new Vector3(-1.1f,.75f,0);blocker.transform.localScale=new Vector3(.2f,3,4);
            Physics.SyncTransforms();
            Assert.That(Clear(Vector3.left),Is.False,"A wall actually ahead must remain blocking, including initial probe overlap.");
        }
        finally{foreach(var wall in walls)UnityEngine.Object.Destroy(wall);}
        yield return null;
    }

    [UnityTest] public IEnumerator TrappedUnarmedEnemyUsesDelayedBlastWithoutSlidingThroughWalls()
    {
        var enemy=Run.Encounter.Enemies.First();enemy.SetCombatActive(false);
        if(enemy.WeaponBlock!=null)enemy.WeaponBlock.ApplyDamage(100000,enemy.WeaponBlock.transform.position,Vector3.up);
        foreach(var gun in enemy.AdditionalWeapons)if(gun.Block!=null&&!gun.Block.IsDead)gun.Block.ApplyDamage(100000,gun.Block.transform.position,Vector3.up);
        yield return null;Assert.That(enemy.WeaponOnline,Is.False);
        var center=new Vector3(1000,0,1000);var body=enemy.GetComponent<Rigidbody>();var drive=enemy.GetComponent<RoboRougelike.R6.R6VehicleDrive>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=center+Vector3.down*.5f;floor.transform.localScale=new Vector3(40,1,40);
        enemy.ConfigureMovementBounds(new Bounds(center,new Vector3(10,100,10)));body.position=center+new Vector3(4.99f,0,0);body.rotation=Quaternion.identity;
        body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
        var target=Run.Player.GetComponent<Rigidbody>();target.isKinematic=true;target.position=center+new Vector3(10,.7f,0);Physics.SyncTransforms();body.WakeUp();
        yield return Until(()=>{drive.RefreshMass();return drive.GroundedWheels>0;},"Ground contact is required.",5);
        foreach(float offset in new[]{-1.05f,1.05f})
        {var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=center+new Vector3(5,1.5f,offset);wall.transform.localScale=new Vector3(5,3,.2f);}
        Physics.SyncTransforms();enemy.SetCombatActive(true);float started=Time.time,deadline=Time.realtimeSinceStartup+15;
        while(Time.time-started<4.6f&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.False,"Do not skip the five-second warning.");
        while(!enemy.IsDefeated&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.IsDefeated,Is.True,"An unarmed enemy trapped near the player must self-destruct.");
    }
    [UnityTest] public IEnumerator EnemyMovesTangentiallyAlongInnerBoundaryWithGroundedWheels()
    {
        var enemy = Run.Encounter.Enemies.First();
        var body = enemy.GetComponent<Rigidbody>();
        var drive = enemy.GetComponent<RoboRougelike.R6.R6VehicleDrive>();
        Assert.That(drive, Is.Not.Null);
        // Put the logical boundary inside the physical arena: the player can exert
        // outward pressure without being teleported through the arena wall.
        enemy.ConfigureMovementBounds(new Bounds(Vector3.zero, new Vector3(48, 100, 76)));
        enemy.enabled = false;
        body.position = new Vector3(23.99f, 0, -22);
        body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
        Run.Player.MoveTo(new Vector3(34, 0.7f, -14), Quaternion.identity);
        var playerBody = Run.Player.GetComponent<Rigidbody>();
        playerBody.linearVelocity = Vector3.zero; playerBody.angularVelocity = Vector3.zero;
        Physics.SyncTransforms();
        yield return Until(() => { drive.RefreshMass(); return drive.GroundedWheels > 0; }, "Koła przeciwnika muszą mieć rzeczywisty kontakt z podłożem.", 5);
        Vector3 before = body.position;
        enemy.enabled = true;
        float end = Time.time + 1.5f;
        int groundedSamples = 0, samples = 0;
        while (Time.time < end)
        {
            yield return MOVSeriesDeviceTests.FixedStep();
            samples++;
            if (drive.GroundedWheels > 0) groundedSamples++;
            Assert.That(body.position.x, Is.LessThanOrEqualTo(24.01f), "Przeciwnik przekroczył granicę logiczną.");
            Assert.That(body.position.x, Is.GreaterThanOrEqualTo(23f), "Przeciwnik powinien pozostać blisko granicy.");
            Assert.That(Mathf.Abs(body.position.z), Is.LessThanOrEqualTo(38.01f));
        }
        Assert.That(groundedSamples, Is.GreaterThan(0), "Ruch wymaga rzeczywistego kontaktu kół podczas próby.");
        Assert.That(samples, Is.GreaterThan(0));
        Assert.That(Vector3.Distance(before, body.position), Is.GreaterThan(0.3f));
        Assert.That(Mathf.Abs(body.position.z - before.z), Is.GreaterThan(0.3f), "Przeciwnik powinien poruszyć się stycznie do granicy.");
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
            Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(R3VehicleCatalog.StartingScrap((R3VehicleKind)i)));
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
            float coreDamage=10*(1-R3DamagePropagation.TransferFraction(R3DamagePropagation.DamageKind.Kinetic));
            Assert.That(core.CurrentHp,Is.EqualTo(core.MaxHp-coreDamage).Within(0.01));
        }
        Assert.That(counts[1],Is.LessThan(counts[0]));Assert.That(counts[2],Is.GreaterThan(counts[0]));
        Assert.That(masses[1],Is.LessThan(masses[0]));Assert.That(masses[2],Is.GreaterThan(masses[0]));
        Assert.That(speeds[1],Is.GreaterThan(speeds[0]));Assert.That(speeds[2],Is.LessThan(speeds[0]));
    }

    [UnityTest] public IEnumerator PaidRepairsNewModulesIndependentEnemiesAndCleanRestart()
    {
        Run.Encounter.SetCombatActive(false);
        int startingScrap=Run.VehicleModel.Scrap,startingParts=Run.VehicleModel.Parts.Count;
        var assembly=Run.Player.GetComponent<R3VehicleAssembly>();
        var profiles=new System.Collections.Generic.HashSet<R3EnemyArchetype>{Run.Encounter.Enemies[0].Archetype};
        var wheel=assembly.Parts.First(p=>p.Kind==R3PartKind.Wheel);var wheelCell=wheel.Cell;
        wheel.Damageable.ApplyDamage(wheel.Damageable.MaxHp*2,wheel.transform.position,Vector3.right);
        Run.Player.Core.ApplyDamage(10,Run.Player.Core.transform.position,Vector3.forward);
        var armor=assembly.Parts.Where(p=>p.Kind==R3PartKind.Armor&&!p.Damageable.IsDead)
            .OrderByDescending(p=>(p.transform.position-wheel.transform.position).sqrMagnitude).First();var armorCell=armor.Cell;
        armor.Damageable.ApplyDamage(25,armor.transform.position,Vector3.forward);
        float armorHealth=armor.Damageable.CurrentHp/armor.Damageable.MaxHp;
        Assert.That(armorHealth,Is.InRange(.001f,.999f),"The fixture needs a damaged, surviving armor part.");
        KillEnemies();yield return Until(()=>Run.Progress.State==R1RunState.Garage,"Pierwsze zwycięstwo.");

        int reward=startingScrap+Run.LastScrapReward+Run.Salvage.DismantledScrap;
        Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Garage));Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(reward));
        Assert.That(Run.VehicleModel.At(wheelCell).Health,Is.Zero);
        Assert.That(Run.VehicleModel.At(armorCell).Health,Is.EqualTo(armorHealth).Within(0.01));
        int coreQuote=Run.VehicleModel.RepairCost(Run.VehicleModel.At(Vector3Int.zero));
        Assert.That(coreQuote,Is.GreaterThan(0));Assert.That(Run.GarageRepair(Vector3Int.zero),Is.True);
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(reward-coreQuote));
        int wheelQuote=Run.VehicleModel.RepairCost(Run.VehicleModel.At(wheelCell));
        Assert.That(wheelQuote,Is.GreaterThan(0));Assert.That(Run.GarageRepair(wheelCell),Is.True);
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(reward-coreQuote-wheelQuote));
        int wheelsBefore=Run.Player.Modules.ActiveWheelCount;
        Assert.That(wheelsBefore,Is.GreaterThan(1));
        int armorQuote=Run.VehicleModel.RepairCost(Run.VehicleModel.At(armorCell));
        Assert.That(armorQuote,Is.GreaterThan(0));Assert.That(Run.GarageRepair(armorCell),Is.True);
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(reward-coreQuote-wheelQuote-armorQuote));
        Assert.That(Run.GarageBuy(new Vector3Int(0,2,-2),R3PartKind.Armor),Is.True,Run.VehicleModel.PlacementError(new Vector3Int(0,2,-2),R3PartKind.Armor));
        var directions=new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right,Vector3Int.forward,Vector3Int.back};
        Vector3Int[] CandidateCells()=>Run.VehicleModel.Parts.SelectMany(part=>directions.Select(direction=>part.Cell+direction)).Distinct().ToArray();
        var gunPosition=CandidateCells().Where(cell=>Run.VehicleModel.PlacementError(cell,R3PartKind.Gun)==null)
            .Select(cell=>(Vector3Int?)cell).FirstOrDefault();
        Assert.That(gunPosition.HasValue,Is.True,"A valid gun socket with physical clearance must exist.");
        var extraGunCell=gunPosition.Value;
        Assert.That(Run.GarageBuy(extraGunCell,R3PartKind.Gun),Is.True,Run.VehicleModel.PlacementError(extraGunCell,R3PartKind.Gun));
        var wheelPosition=CandidateCells().SelectMany(cell=>new[]{0,1,2,3}.Select(y=>new{cell,rotation=new Vector3Int(0,y,0)}))
            .FirstOrDefault(candidate=>Run.VehicleModel.PlacementError(candidate.cell,R3PartKind.Wheel,false,default,candidate.rotation)==null);
        Assert.That(wheelPosition,Is.Not.Null,"A side-facing wheel with a valid physical socket must exist.");
        Assert.That(Run.GarageBuy(wheelPosition.cell,R3PartKind.Wheel,wheelPosition.rotation),Is.True);
        Assert.That(Run.Player.Modules.ActiveWheelCount,Is.EqualTo(wheelsBefore+1));Assert.That(Run.Player.Modules.ActiveWeaponCount,Is.EqualTo(3));
        Assert.That(Run.GarageBuy(new Vector3Int(0,3,-2),R3PartKind.Armor),Is.True);
        Assert.That(Run.GarageRemove(new Vector3Int(0,2,-2)),Is.False,"Usunięcie mostu jest blokowane.");
        Assert.That(Run.GarageRemove(new Vector3Int(0,3,-2)),Is.True);
        Assert.That(Run.GarageRemove(Vector3Int.zero),Is.False);
        var blinkPosition=CandidateCells().Where(cell=>Run.VehicleModel.PlacementError(cell,R3PartKind.Blink)==null)
            .Select(cell=>(Vector3Int?)cell).FirstOrDefault();
        Assert.That(blinkPosition.HasValue,Is.True,"The insufficient-funds check needs a valid empty socket first.");
        int beforePurchase=Run.VehicleModel.Scrap;
        Assert.That(Run.GaragePurchase(R3PartKind.Battery),Is.True);
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(beforePurchase-Run.VehicleModel.Price(R3PartKind.Battery)));
        while(Run.VehicleModel.Scrap>=Run.VehicleModel.Price(R3PartKind.Blink))
            Assert.That(Run.GaragePurchase(R3PartKind.Battery),Is.True,"Buy real inventory parts until the blink is unaffordable.");
        int funds=Run.VehicleModel.Scrap;
        Assert.That(Run.GarageBuy(extraGunCell,R3PartKind.Gun),Is.False);
        var blinkCell=blinkPosition.Value;
        Assert.That(funds,Is.LessThan(Run.VehicleModel.Price(R3PartKind.Blink)));
        Assert.That(Run.VehicleModel.PlacementError(blinkCell,R3PartKind.Blink),Does.Contain("Za mało Złomu"));
        Assert.That(Run.GarageBuy(blinkCell,R3PartKind.Blink),Is.False,"Brak środków.");
        Assert.That(Run.GarageBuy(new Vector3Int(6,5,7),R3PartKind.Armor),Is.False);
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(funds));
        assembly=Run.Player.GetComponent<R3VehicleAssembly>();R1EncounterManager.ClearTransientObjects();
        foreach(var gun in assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun))gun.GunPivot.rotation=Quaternion.Euler(-70,0,0);
        Physics.SyncTransforms();
        Assert.That(assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun).All(assembly.ClearProjectilePath),Is.True,"Każde działo musi mieć wolny tor strzału.");
        float energyBefore=Run.Player.GetComponent<VehicleEnergyBank>().Current;
        assembly.FireGuns();Assert.That(UnityEngine.Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Length,Is.EqualTo(3));
        Assert.That(Run.Player.GetComponent<VehicleEnergyBank>().Current,Is.EqualTo(energyBefore).Within(.01f),"Podstawowe działa nie zużywają energii.");
        var extraGun=assembly.Parts.Single(p=>p.Cell==extraGunCell);
        extraGun.Damageable.ApplyDamage(extraGun.Damageable.MaxHp*2,extraGun.transform.position,Vector3.forward);assembly.Capture();
        Assert.That(Run.Player.Modules.ActiveWeaponCount,Is.EqualTo(2));
        Assert.That(Run.StartNextEncounter(),Is.True);yield return Until(()=>!Run.Transitioning,"Druga walka.");
        Assert.That(Run.Player.Modules.ActiveWeaponCount,Is.EqualTo(2));Assert.That(Run.VehicleModel.Parts.Count,Is.EqualTo(startingParts+3));
        Assert.That(Run.Encounter.AliveCount,Is.EqualTo(3));
        var positions=Run.Encounter.Enemies.Select(e=>e.transform.position).ToArray();
        float moveUntil=Time.time+0.9f;yield return Until(()=>Time.time>=moveUntil,"Ruch dwóch AI.");
        for(int i=0;i<3;i++){Assert.That(Vector3.Distance(positions[i],Run.Encounter.Enemies[i].transform.position),Is.GreaterThan(0.1f));profiles.Add(Run.Encounter.Enemies[i].Archetype);}
        Run.Encounter.SetCombatActive(false);
        var first=Run.Encounter.Enemies[0];first.Core.ApplyDamage(10000,first.Core.transform.position,Vector3.up);
        float end=Time.time+0.2f;yield return Until(()=>Time.time>=end,"Niezależna śmierć jednego AI.");
        Assert.That(Run.Encounter.AliveCount,Is.EqualTo(2));Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Combat));
        KillEnemies();yield return Until(()=>Run.Progress.State==R1RunState.Garage,"Drugie zwycięstwo.");
        var wreck=Run.VehicleModel.At(extraGunCell);
        var neighbour=typeof(R3VehicleModel).GetMethod("HasAliveExternalNeighbour",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That((bool)neighbour.Invoke(Run.VehicleModel,new object[]{wreck.Cell,wreck.Kind,wreck.RotationQuarterTurns,wreck.ContactMask,wreck}),Is.False,
            "This hit also destroyed the support, so rebuilding the gun alone must fail.");
        int fundsBeforeRepairs=Run.VehicleModel.Scrap;
        Assert.That(Run.GarageRepair(extraGunCell),Is.False,"A gun cannot be restored without an intact connection.");
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(fundsBeforeRepairs));
        int rebuildQuote=Run.VehicleModel.AffordableRepairCost;
        Assert.That(rebuildQuote,Is.GreaterThan(0));Assert.That(Run.GarageRepairAffordable(),Is.True);
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(fundsBeforeRepairs-rebuildQuote));
        Assert.That(Run.VehicleModel.At(extraGunCell).Health,Is.EqualTo(1),"Repair must reconnect the support and the gun.");
        Assert.That(Run.Player.Modules.ActiveWeaponCount,Is.EqualTo(3));
        Assert.That(Run.StartNextEncounter(),Is.True);yield return Until(()=>!Run.Transitioning,"Trzecia walka.");
        Assert.That(Run.Encounter.AliveCount,Is.EqualTo(4));
        positions=Run.Encounter.Enemies.Select(e=>e.transform.position).ToArray();
        moveUntil=Time.time+0.9f;yield return Until(()=>Time.time>=moveUntil,"Ruch trzech AI.");
        for(int i=0;i<4;i++){Assert.That(Vector3.Distance(positions[i],Run.Encounter.Enemies[i].transform.position),Is.GreaterThan(0.1f));profiles.Add(Run.Encounter.Enemies[i].Archetype);}
        Assert.That(profiles.Count,Is.EqualTo(4));
        Assert.That(Run.Player.Modules.ActiveWheelCount,Is.EqualTo(wheelsBefore+1));Assert.That(Run.Progress.Acquired.Count,Is.EqualTo(2));
        KillEnemies();yield return Until(()=>Run.Progress.State==R1RunState.Garage,"Trzecie starcie.");
        while(Run.Progress.State==R1RunState.Garage)
        {
            Run.StartNextEncounter();yield return Until(()=>!Run.Transitioning,"Dalszy etap R5.");
            Run.Encounter.SetCombatActive(false);KillEnemies();yield return Until(()=>Run.Progress.State==R1RunState.Garage||Run.Progress.State==R1RunState.Won,"Dalsza nagroda R5.");
        }
        Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Won));
        var previous=Run;Run.RestartRun();yield return Until(()=>Run!=null&&Run!=previous&&Run.Progress!=null,"Nowa próba.");
        Run.StartRun();yield return Until(()=>!Run.Transitioning,"Nienaruszony wzorzec.");
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(R3VehicleCatalog.StartingScrap(Run.SelectedVehicle)));
        Assert.That(Run.VehicleModel.Parts.Count,Is.EqualTo(R3VehicleCatalog.Create(Run.SelectedVehicle,Run.SelectedVehicleVariant).Parts.Count));
        Assert.That(Run.Progress.Acquired,Is.Empty);
    }

    [UnityTest] public IEnumerator TurretSupportLossDropsEntireRemainingWeapon()
    {
        var enemy=Run.Encounter.Enemies.First();var yaw=enemy.transform.Find("Enemy_Yaw");
        yaw.GetChild(0).GetComponent<P12PipelineSafeDamageable>().ApplyDamage(1000,yaw.position,Vector3.up);
        yield return Until(()=>!yaw.IsChildOf(enemy.transform),"Odłączenie głównej wieżyczki.");
        yield return null;
        Assert.That(yaw.IsChildOf(enemy.transform),Is.False);Assert.That(yaw.GetComponent<Rigidbody>(),Is.Not.Null);
        Assert.That(enemy.IsDefeated,Is.False);Assert.That(Run.Progress.State,Is.EqualTo(R1RunState.Combat));
        Assert.That(enemy.AdditionalWeapons,Is.Not.Empty);Assert.That(enemy.WeaponOnline,Is.True,"Dodatkowa broń nadal działa po utracie wieżyczki.");
        foreach(var gun in enemy.AdditionalWeapons)gun.Block.ApplyDamage(1000,gun.Block.transform.position,Vector3.up);
        yield return Until(()=>!enemy.WeaponOnline,"Rozbrojenie wszystkich broni.");
    }

    [UnityTest] public IEnumerator EmergencyRebuildPaysForEssentialModulesWithoutFullReset()
    {
        KillEnemies();yield return Until(()=>Run.Progress.State==R1RunState.Garage,"Nagroda.");
        var assembly=Run.Player.GetComponent<R3VehicleAssembly>();
        foreach(var part in assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun||p.Kind==R3PartKind.Wheel))
            part.Damageable.ApplyDamage(10000,part.transform.position,Vector3.up);
        assembly.Capture();int scrap=Run.VehicleModel.Scrap;
        int quoted=Run.VehicleModel.EmergencyRepairCost;Assert.That(quoted,Is.GreaterThan(0));
        Assert.That(Run.StartNextEncounter(),Is.False);
        Assert.That(Run.GarageEmergencyRepair(),Is.True);
        Assert.That(Run.VehicleModel.Scrap,Is.EqualTo(scrap-quoted));
        Assert.That(Run.Player.Modules.ActiveWheelCount,Is.EqualTo(2));Assert.That(Run.Player.Modules.ActiveWeaponCount,Is.EqualTo(1));
        Assert.That(Run.VehicleModel.Parts.Count(p=>p.Kind==R3PartKind.Wheel&&p.Health<=0),Is.EqualTo(4));
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
