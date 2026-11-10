using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;

public sealed class BacklogBonusTierTests
{
    [Test] public void IdenticalRewardModulesFuseAndPreserveTheirEffect()
    {
        var model = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var reward = ScriptableObject.CreateInstance<R1UpgradeDefinition>();
        reward.Id = "damage"; reward.Effect = R1UpgradeEffect.Damage; reward.Amount = .2f;
        Assert.That(model.ReceiveBonuses(new[] {reward, reward, reward}), Is.True);
        var ids = model.Inventory.Select(p => p.Id).ToArray();
        Assert.That(model.FuseInventory(ids), Is.True);
        var result = model.Inventory.Single();
        Assert.That(result.Tier, Is.EqualTo(2));
        Assert.That(result.UpgradeId, Is.EqualTo("damage"));
        Assert.That(result.UpgradeEffect, Is.EqualTo(R1UpgradeEffect.Damage));
        Assert.That(result.UpgradeAmount, Is.EqualTo(.2f));
        var checkpoint = model.MakeCheckpoint(0);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint, R3VehicleKind.Light, model.Economy, 0, out var restored, out var error), Is.True, error);
        Assert.That(restored.Inventory.Single().Tier, Is.EqualTo(2));
        Assert.That(restored.Inventory.Single().UpgradeId, Is.EqualTo("damage"));
        Object.DestroyImmediate(reward);
    }

    [Test] public void DifferentRewardEffectsCannotBeCombined()
    {
        var model = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var damage = ScriptableObject.CreateInstance<R1UpgradeDefinition>();
        var mobility = ScriptableObject.CreateInstance<R1UpgradeDefinition>();
        damage.Id = "damage"; damage.Effect = R1UpgradeEffect.Damage; damage.Amount = .2f;
        mobility.Id = "mobility"; mobility.Effect = R1UpgradeEffect.Mobility; mobility.Amount = .2f;
        Assert.That(model.ReceiveBonuses(new[] {damage, damage, mobility}), Is.True);
        var ids = model.Inventory.Select(p => p.Id).ToArray();
        Assert.That(model.FuseInventory(ids), Is.False);
        Assert.That(model.Inventory.Select(p => p.Id), Is.EqualTo(ids));
        model.Inventory[2].UpgradeId = "damage";
        model.Inventory[2].UpgradeEffect = R1UpgradeEffect.Damage;
        model.Inventory[2].UpgradeAmount = .4f;
        Assert.That(model.FuseInventory(ids), Is.False, "Same ID with different strength must not be combined.");
        Object.DestroyImmediate(damage); Object.DestroyImmediate(mobility);
    }
}

public sealed class BacklogBonusTierGameplayTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        for (int i = 0; i < 300 && R1RunController.Instance?.Progress == null; i++) yield return null;
    }
    [UnityTest] public IEnumerator MountedTierTwoDamageRewardWorksAndStopsWhenDestroyed()
    {
        var run = R1RunController.Instance;
        Assert.That(run.SelectVehicle(R3VehicleKind.Light) && run.OpenSandboxGarage(), Is.True);
        var reward = ScriptableObject.CreateInstance<R1UpgradeDefinition>();
        reward.Id = "damage"; reward.Effect = R1UpgradeEffect.Damage; reward.Amount = .2f;
        var model = run.VehicleModel;
        Assert.That(model.ReceiveBonuses(new[] {reward, reward, reward}), Is.True);
        Assert.That(run.GarageFuse(model.Inventory.Select(p => p.Id).ToArray()), Is.True);
        var state = model.Inventory.Single();
        var cells = model.Parts.SelectMany(p => R3VehicleModel.Neighbours.Select(d => p.Cell + d)).Distinct();
        var cell = cells.First(c => model.OwnedPlacementError(state.Id, c, default) == null);
        Assert.That(run.GarageMount(state.Id, cell), Is.True);
        yield return null;
        var effects = run.Player.GetComponent<R3MountedBonusEffects>();
        Assert.That(effects.EffectiveStats.Damage, Is.EqualTo(1.44f).Within(.001f));
        var part = run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p => p.Id == state.Id);
        part.Damageable.ApplyDamage(100000, part.transform.position, Vector3.up);
        yield return null;
        Assert.That(effects.EffectiveStats.Damage, Is.EqualTo(1f).Within(.001f));
        Object.Destroy(reward);
    }
    [UnityTest] public IEnumerator LiveMountedBonusUpdatesWhenItsPropertiesChange()
    {
        var run=R1RunController.Instance;
        Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
        var reward=ScriptableObject.CreateInstance<R1UpgradeDefinition>();
        reward.Id="damage";reward.Effect=R1UpgradeEffect.Damage;reward.Amount=.2f;
        var model=run.VehicleModel;Assert.That(model.ReceiveBonuses(new[]{reward}),Is.True);
        var state=model.Inventory.Single();
        var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        var cell=cells.First(c=>model.OwnedPlacementError(state.Id,c,default)==null);
        Assert.That(run.GarageMount(state.Id,cell),Is.True);yield return null;
        var effects=run.Player.GetComponent<R3MountedBonusEffects>();
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.2f).Within(.001f));
        part.Tier=2;yield return null;
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.44f).Within(.001f),"The live tier is part of the effect, even if the part ID is unchanged.");
        part.UpgradeAmount=.3f;yield return null;
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1.69f).Within(.001f));
        part.UpgradeEffect=R1UpgradeEffect.Mobility;yield return null;
        Assert.That(effects.EffectiveStats.Damage,Is.EqualTo(1f).Within(.001f));
        Assert.That(effects.EffectiveStats.Mobility,Is.EqualTo(1.69f).Within(.001f));
        Object.Destroy(reward);
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }
}
