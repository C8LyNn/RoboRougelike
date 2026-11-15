using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R1;
using RoboRougelike.Mechanics.EMP;
using RoboRougelike.WeaponIntegration;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogEmpTierTests
{
    [Test] public void ThreeEmpEmittersFuseWithoutResettingCooldown()
    {
        var model = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Award(1000);
        for (int i = 0; i < 3; i++)
            Assert.That(model.PurchaseToInventory(R3PartKind.Gun, R4WeaponKind.EmpEmitter), Is.True);
        var emitters = model.Inventory.Where(p => p.Weapon == R4WeaponKind.EmpEmitter).ToArray();
        emitters[0].DeviceReadyAt = 5;
        emitters[1].DeviceReadyAt = 12;
        emitters[2].DeviceReadyAt = 8;
        Assert.That(model.FuseInventory(emitters.Select(p => p.Id)), Is.True);
        var upgraded = model.Inventory.Single();
        Assert.That(upgraded.Tier, Is.EqualTo(2));
        Assert.That(upgraded.Weapon, Is.EqualTo(R4WeaponKind.EmpEmitter));
        Assert.That(upgraded.DeviceReadyAt, Is.EqualTo(12));
    }
}

public sealed class BacklogEmpTierPhysicsTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
    }

    [UnityTest] public IEnumerator ContinuousWeaponScopesAffectOnlyTheirOwnDamageAndSurviveSave()
    {
        var cases=new[]{(R4WeaponKind.BeamCutter,"BeamWeapon","damage_beam_weapon"),
            (R4WeaponKind.RotarySaw,"SawWeapon","damage_saw_weapon"),
            (R4WeaponKind.EmpEmitter,"AuraWeapon","damage_aura_weapon")};
        foreach(var entry in cases)
        {
            Assert.That(System.Enum.TryParse(entry.Item2,out R1BonusScope scope),Is.True,"Missing scope "+entry.Item2);
            var reward=Resources.Load<R1RunDefinition>("R1/Run").Upgrades.Single(p=>p!=null&&p.Id==entry.Item3);
            Assert.That(reward.Scope,Is.EqualTo(scope));Assert.That(reward.Amount,Is.EqualTo(.3f));
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
            var gunState=model.Parts.First(p=>p.Kind==R3PartKind.Gun);gunState.Weapon=entry.Item1;
            var bonus=ScriptableObject.CreateInstance<R1UpgradeDefinition>();bonus.Id="test-"+entry.Item3;
            bonus.Effect=R1UpgradeEffect.Damage;bonus.Scope=scope;bonus.Amount=.3f;
            Assert.That(model.ReceiveBonuses(new[]{bonus}),Is.True);var id=model.Inventory.Single(p=>p.UpgradeId==bonus.Id).Id;
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct().First(c=>model.OwnedPlacementError(id,c,default)==null);
            Assert.That(model.Mount(id,cell),Is.True);
            var saved=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
            Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
            Assert.That(restored.Parts.Single(p=>p.Id==id).UpgradeScope,Is.EqualTo(scope));
            var paletteObject=new GameObject("Continuous scope palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
            for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
            var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
            vehicle.GetComponent<Rigidbody>().isKinematic=true;
            var gun=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==gunState.Id);
            var enemy=new GameObject("Continuous scope enemy");enemy.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().enabled=false;
            var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.SetParent(enemy.transform);
            target.transform.position=vehicle.transform.position+Vector3.forward*2;
            var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(500,false);Physics.SyncTransforms();
            var aura=gun.GetComponent<RRLDamageAuraBinding>();if(aura!=null)Assert.That(aura.Fire(),Is.True);
            void Hit()
            {
                if(entry.Item1==R4WeaponKind.BeamCutter)
                {
                    var beam=gun.GetComponent<RRLBeamBinding>();
                    var hit=new RoboRougelike.Mechanics.BeamCutter.BeamHitRequest(target.GetComponent<Collider>(),target.transform.position,Vector3.back,Vector3.forward,100,1,vehicle.GetComponent<Rigidbody>(),gun.gameObject);
                    beam.ApplyBeamHit(hit);
                }
                else if(entry.Item1==R4WeaponKind.RotarySaw)
                {
                    var saw=gun.GetComponent<RRLSawBinding>();
                    saw.ApplyDamage(new RoboRougelike.Weapons.RotarySaw.SawDamageEvent(hp.GetInstanceID(),100,target.transform.position,Vector3.up,target.GetComponent<Collider>(),saw.Module));
                }
                else typeof(RRLDamageAuraBinding).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(aura,null);
            }
            float baseline=entry.Item1==R4WeaponKind.EmpEmitter?RRLDamageAuraBinding.DamagePerSecond*Time.fixedDeltaTime:100;
            Hit();Assert.That(500-hp.CurrentHp,Is.EqualTo(baseline*1.3f).Within(.01f),entry.Item2);
            var effects=vehicle.GetComponent<R3MountedBonusEffects>();Assert.That(effects.AutomaticDamageMultiplier,Is.EqualTo(1));
            target.SetActive(false);vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id).gameObject.SetActive(false);yield return null;
            target.SetActive(true);hp.RestoreHealthRatio(1);Physics.SyncTransforms();Hit();
            Assert.That(500-hp.CurrentHp,Is.EqualTo(baseline).Within(.01f),"Disabled "+entry.Item2);
            Object.Destroy(vehicle.gameObject);Object.Destroy(enemy);Object.Destroy(paletteObject);Object.Destroy(material);Object.Destroy(bonus);yield return null;
        }
    }

    [UnityTest] public IEnumerator EnergyScopedBonusAffectsAuraAndBeamButNotKineticShots()
    {
        Assert.That(System.Enum.TryParse("EnergyDamage",out R1BonusScope scope),Is.True);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var gunState=model.Parts.First(p=>p.Kind==R3PartKind.Gun);gunState.Weapon=R4WeaponKind.EmpEmitter;
        var bonus=ScriptableObject.CreateInstance<R1UpgradeDefinition>();bonus.Id="energy-typed";bonus.Effect=R1UpgradeEffect.Damage;bonus.Scope=scope;bonus.Amount=.5f;
        var reward=Resources.Load<R1RunDefinition>("R1/Run").Upgrades.Single(p=>p!=null&&p.Id=="damage_energy");
        Assert.That(reward.Scope,Is.EqualTo(scope));Assert.That(reward.Amount,Is.EqualTo(.4f));
        Assert.That(model.ReceiveBonuses(new[]{bonus}),Is.True);var id=model.Inventory.Single(p=>p.UpgradeId==bonus.Id).Id;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct().First(c=>model.OwnedPlacementError(id,c,default)==null);
        Assert.That(model.Mount(id,cell),Is.True);
        var saved=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==id).UpgradeScope,Is.EqualTo(scope));
        Assert.That(R3DamagePropagation.TransferFraction(R3DamagePropagation.DamageKind.Energy),Is.EqualTo(.25f));
        var paletteObject=new GameObject("Energy scope palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var gun=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==gunState.Id);
        var enemy=new GameObject("enemy fixture");enemy.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().enabled=false;
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.SetParent(enemy.transform);
        target.transform.position=vehicle.transform.position+Vector3.forward*2;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(500,false);
        Physics.SyncTransforms();var aura=gun.GetComponent<RRLDamageAuraBinding>();Assert.That(aura.Fire(),Is.True);
        typeof(RRLDamageAuraBinding).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(aura,null);
        Assert.That(hp.CurrentHp,Is.EqualTo(500-RRLDamageAuraBinding.DamagePerSecond*1.5f*Time.fixedDeltaTime).Within(.001f));
        var effects=vehicle.GetComponent<R3MountedBonusEffects>();Assert.That(effects.KineticDamageMultiplier,Is.EqualTo(1));
        var beamPart=GameObject.CreatePrimitive(PrimitiveType.Cube);beamPart.transform.SetParent(vehicle.transform,false);
        beamPart.GetComponent<Collider>().enabled=false;beamPart.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Configure(100,false);
        var muzzle=new GameObject("Beam muzzle").transform;muzzle.SetParent(beamPart.transform,false);
        var beam=beamPart.AddComponent<RRLBeamBinding>();beam.Bind(new R3PartState(default,R3PartKind.Gun){Weapon=R4WeaponKind.BeamCutter},vehicle.transform,muzzle);
        hp.RestoreHealthRatio(1);
        var hit=new RoboRougelike.Mechanics.BeamCutter.BeamHitRequest(target.GetComponent<Collider>(),target.transform.position,Vector3.back,Vector3.forward,100,1,vehicle.GetComponent<Rigidbody>(),beam.gameObject);
        beam.ApplyBeamHit(hit);Assert.That(hp.CurrentHp,Is.EqualTo(350).Within(.01));
        vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id).gameObject.SetActive(false);yield return null;
        hp.RestoreHealthRatio(1);beam.ApplyBeamHit(hit);Assert.That(hp.CurrentHp,Is.EqualTo(400).Within(.01));
        Object.Destroy(vehicle.gameObject);Object.Destroy(enemy);Object.Destroy(paletteObject);Object.Destroy(material);Object.Destroy(bonus);yield return null;
    }

    [UnityTest] public IEnumerator ExtremeFiniteBeamAndSawDamageStillReachesTargets()
    {
        var owner=new GameObject("Continuous weapon owner");owner.transform.position=new Vector3(1000,1000,1000);
        var body=owner.AddComponent<Rigidbody>();body.isKinematic=true;
        GameObject Weapon(out Transform muzzle)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.transform.SetParent(owner.transform,false);
            part.GetComponent<Collider>().enabled=false;part.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Configure(100,false);
            muzzle=new GameObject("muzzle").transform;muzzle.SetParent(part.transform,false);return part;
        }
        GameObject Target(out RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable hp)
        {
            var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=owner.transform.position+Vector3.forward*4;
            hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(1000,false);return target;
        }
        var beam=Weapon(out var muzzle).AddComponent<RRLBeamBinding>();
        beam.Bind(new R3PartState(default,R3PartKind.Gun){Weapon=R4WeaponKind.BeamCutter,Tier=3},owner.transform,muzzle,float.MaxValue);
        var beamTarget=Target(out var beamHp);Physics.SyncTransforms();
        var hit=new RoboRougelike.Mechanics.BeamCutter.BeamHitRequest(beamTarget.GetComponent<Collider>(),beamTarget.transform.position,Vector3.back,Vector3.forward,100,1,body,beam.gameObject);
        beam.ApplyBeamHit(hit);
        beamTarget.SetActive(false);
        var protectedTarget=Target(out var protectedHp);
        var dome=new GameObject("hostile shield");dome.transform.position=owner.transform.position+Vector3.forward*2;
        dome.AddComponent<SphereCollider>().isTrigger=true;
        var shieldHp=dome.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();shieldHp.Configure(500,false);
        dome.AddComponent<R3ShieldDome>().Configure(2,1);Physics.SyncTransforms();
        var shieldedHit=new RoboRougelike.Mechanics.BeamCutter.BeamHitRequest(protectedTarget.GetComponent<Collider>(),protectedTarget.transform.position,Vector3.back,Vector3.forward,100,1,body,beam.gameObject);
        beam.ApplyBeamHit(shieldedHit);dome.SetActive(false);protectedTarget.SetActive(false);
        var saw=Weapon(out var blade).AddComponent<RRLSawBinding>();
        saw.Bind(new R3PartState(default,R3PartKind.Gun){Weapon=R4WeaponKind.RotarySaw,Tier=3},owner.transform,blade,float.MaxValue);
        var sawTarget=Target(out var sawHp);Physics.SyncTransforms();
        var sawHit=new RoboRougelike.Weapons.RotarySaw.SawDamageEvent(sawHp.GetInstanceID(),100,sawTarget.transform.position,Vector3.up,sawTarget.GetComponent<Collider>(),saw.Module);
        saw.ApplyDamage(sawHit);
        Assert.That(beamHp.IsDead&&sawHp.IsDead&&shieldHp.IsDead&&!protectedHp.IsDead,Is.True,
            $"Finite extreme damage: beam={beamHp.IsDead}, saw={sawHp.IsDead}, shield={shieldHp.IsDead}, protected={protectedHp.IsDead}.");
        Object.Destroy(beamTarget);Object.Destroy(sawTarget);Object.Destroy(protectedTarget);Object.Destroy(dome);Object.Destroy(owner);yield return null;
    }
    [UnityTest] public IEnumerator ExtremeFiniteAuraBonusStillDamagesEnemy()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=R4WeaponKind.EmpEmitter;state.Tier=3;
        var bonus=ScriptableObject.CreateInstance<R1UpgradeDefinition>();bonus.Id="aura-large";bonus.Effect=R1UpgradeEffect.Damage;bonus.Amount=float.MaxValue;
        Assert.That(model.ReceiveBonuses(new[]{bonus}),Is.True);string bonusId=model.Inventory.Single(p=>p.UpgradeId==bonus.Id).Id;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.OwnedPlacementError(bonusId,c,default)==null);
        Assert.That(model.Mount(bonusId,cell),Is.True);
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var gun=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
        var enemy=new GameObject("aura enemy fixture");enemy.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().enabled=false;
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.SetParent(enemy.transform);target.transform.position=vehicle.transform.position+Vector3.forward*2;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(500,false);
        Physics.SyncTransforms();var aura=gun.GetComponent<RRLDamageAuraBinding>();Assert.That(aura.Fire(),Is.True);
        typeof(RRLDamageAuraBinding).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(aura,null);
        Assert.That(hp.IsDead,Is.True,"A finite extreme damage modifier must not make the aura harmless.");
        Object.Destroy(vehicle.gameObject);Object.Destroy(enemy);Object.Destroy(paletteObject);Object.Destroy(material);Object.Destroy(bonus);yield return null;
    }

    [UnityTest] public IEnumerator DamageableWithoutRendererStillSettlesDamage()
    {
        var invisible=new GameObject("Niewidoczny element z HP");
        var hp=invisible.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(100,false);
        Assert.That(invisible.GetComponent<Renderer>(),Is.Null);
        Assert.DoesNotThrow(()=>hp.ApplyDamage(10,invisible.transform.position,Vector3.up));
        Assert.That(hp.CurrentHp,Is.EqualTo(90).Within(.001));
        bool died=false;hp.Died+=()=>died=true;
        Assert.DoesNotThrow(()=>hp.ApplyDamage(100,invisible.transform.position,Vector3.up));
        Assert.That(hp.IsDead,Is.True);Assert.That(died,Is.True,"Cosmetic feedback must not interrupt death callbacks.");
        Object.Destroy(invisible);yield return null;
    }
    [UnityTest] public IEnumerator MuzzleWithoutRendererStillProducesNoException()
    {
        var barrel=new GameObject("Lufa bez renderera");
        barrel.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Configure(100,false);
        Assert.DoesNotThrow(()=>RoboRougelike.R6.R6CombatFeedback.Muzzle(barrel.transform,R4WeaponKind.Basic));
        Object.Destroy(barrel);yield return null;
    }

    [UnityTest] public IEnumerator TierTwoAuraSpendsEnergyAndDamagesAnEnemyCollider()
    {
        var model = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state = model.Parts.First(p => p.Kind == R3PartKind.Gun);
        state.Weapon = R4WeaponKind.EmpEmitter; state.Tier = 2;
        var paletteObject = new GameObject("QA palette");
        var palette = paletteObject.AddComponent<R3VisualPalette>();
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for (int i = 0; i < palette.Parts.Length; i++) palette.Parts[i] = material;
        for (int i = 0; i < palette.Weapons.Length; i++) palette.Weapons[i] = material;
        var vehicle = R3VehicleAssembly.Build(model, new R1RunStats(), palette, new Vector3(0,20,0), Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic = true;
        var gun = vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p => p.Id == state.Id);
        var enemy = new GameObject("enemy fixture");
        enemy.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().enabled=false;
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.SetParent(enemy.transform);
        target.transform.position = vehicle.GetComponent<Rigidbody>().position + Vector3.forward * 2f;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(500,false);
        var bank = vehicle.GetComponent<VehicleEnergyBank>(); float before = bank.Current;
        Physics.SyncTransforms();
        var binding = gun.GetComponent<RRLDamageAuraBinding>();
        Assert.That(binding.Fire(), Is.True);
        typeof(RRLDamageAuraBinding).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(binding,null);
        Assert.That(bank.Current, Is.EqualTo(before-RRLDamageAuraBinding.EnergyPerSecond*1.2f*Time.fixedDeltaTime).Within(.001f));
        Assert.That(hp.CurrentHp, Is.EqualTo(500-RRLDamageAuraBinding.DamagePerSecond*2*Time.fixedDeltaTime).Within(.001f));
        Object.Destroy(vehicle.gameObject); Object.Destroy(enemy); Object.Destroy(paletteObject); Object.Destroy(material);
        yield return null;
    }
    [UnityTest] public IEnumerator AuraDamagesHostileDomeAndProtectsEnemyInside()=>AuraDomeScenario(false,true);
    [UnityTest] public IEnumerator AuraDamagesHostileDomeEvenWithoutTargetInside()=>AuraDomeScenario(false,false);
    [UnityTest] public IEnumerator FriendlyDomeDoesNotBlockAuraAgainstEnemy()=>AuraDomeScenario(true,true);
    [UnityTest] public IEnumerator AuraDoesNotDamageInnerDomeBehindHostileOuterDome()=>AuraDomeScenario(false,true,true);
    IEnumerator AuraDomeScenario(bool friendly,bool hasTarget,bool nested=false)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=R4WeaponKind.EmpEmitter;
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var gun=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
        var dome=GameObject.CreatePrimitive(PrimitiveType.Cube);dome.name="Osłona celu";dome.GetComponent<Collider>().enabled=false;
        dome.transform.position=vehicle.GetComponent<Rigidbody>().position+Vector3.forward*4;
        var sphere=dome.AddComponent<SphereCollider>();sphere.isTrigger=true;
        var shield=dome.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();shield.Configure(500,false);
        dome.AddComponent<R3ShieldDome>().Configure(friendly?1:2,2);
        GameObject inner=null;RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable innerHp=null;
        if(nested)
        {
            inner=GameObject.CreatePrimitive(PrimitiveType.Cube);inner.name="Wewnętrzna osłona";inner.GetComponent<Collider>().enabled=false;inner.transform.position=dome.transform.position;
            var innerSphere=inner.AddComponent<SphereCollider>();innerSphere.isTrigger=true;
            innerHp=inner.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();innerHp.Configure(500,false);
            inner.AddComponent<R3ShieldDome>().Configure(2,1);
        }
        GameObject enemy=null;RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable hp=null;
        if(hasTarget)
        {
            enemy=new GameObject("Osłonięty przeciwnik");enemy.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().enabled=false;
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.transform.SetParent(enemy.transform);part.transform.position=dome.transform.position;
            hp=part.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(500,false);
        }
        Physics.SyncTransforms();var aura=gun.GetComponent<RRLDamageAuraBinding>();Assert.That(aura.Fire(),Is.True);
        typeof(RRLDamageAuraBinding).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(aura,null);
        if(friendly){Assert.That(shield.CurrentHp,Is.EqualTo(500).Within(.001));Assert.That(hp.CurrentHp,Is.LessThan(500));}
        else {Assert.That(shield.CurrentHp,Is.LessThan(500));if(hp!=null)Assert.That(hp.CurrentHp,Is.EqualTo(500).Within(.001));}
        if(nested)Assert.That(innerHp.CurrentHp,Is.EqualTo(500).Within(.001));
        if(inner!=null)Object.Destroy(inner);if(enemy!=null)Object.Destroy(enemy);Object.Destroy(dome);Object.Destroy(vehicle.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);
        yield return null;
    }

    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }
}
