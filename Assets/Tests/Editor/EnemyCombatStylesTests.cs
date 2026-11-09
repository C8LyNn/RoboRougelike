using NUnit.Framework;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;

public sealed class EnemyCombatStylesTests
{
    [Test]
    public void ArmedRangedEnemiesDoNotRamJustBecausePlayerIsClose()
    {
        Assert.That(P12PipelineSafeEnemyTurret.ShouldRam(true, R3EnemyArchetype.Gunner, 3, 1), Is.False);
        Assert.That(P12PipelineSafeEnemyTurret.ShouldRam(true, R3EnemyArchetype.Flanker, 3, 1), Is.False);
    }

    [Test]
    public void BruiserHasAttackAndRecoveryPhasesWhileUnarmedEnemiesCanStillRam()
    {
        Assert.That(P12PipelineSafeEnemyTurret.ShouldRam(true, R3EnemyArchetype.Bruiser, 5, 1), Is.True);
        Assert.That(P12PipelineSafeEnemyTurret.ShouldRam(true, R3EnemyArchetype.Bruiser, 5, 8), Is.False);
        Assert.That(P12PipelineSafeEnemyTurret.ShouldRam(false, R3EnemyArchetype.Gunner, 5, 8), Is.True);
    }

    [Test]
    public void ExtendedContactStartsRetreatAndRetreatContinuesAfterSeparation()
    {
        var recovery = new EnemyContactRecovery();
        Assert.That(recovery.Advance(true, 1f), Is.False);
        Assert.That(recovery.Advance(true, .3f), Is.True);
        Assert.That(recovery.Advance(false, 1f), Is.True);
        Assert.That(recovery.Advance(false, 1.3f), Is.False);
    }

    [Test]
    public void BriefContactsDoNotAccumulateAndResetClearsRetreat()
    {
        var recovery = new EnemyContactRecovery();
        recovery.Advance(true, 1f);
        recovery.Advance(false, .1f);
        Assert.That(recovery.Advance(true, 1f), Is.False);
        Assert.That(recovery.Advance(true, .3f), Is.True);
        recovery.Reset();
        Assert.That(recovery.Recovering, Is.False);
    }
}
