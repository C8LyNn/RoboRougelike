using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;

public sealed class BacklogRewardBasketTests
{
    [Test]
    public void RecordsEnemyArchetypeAlongsideOriginalPhysicalParts()
    {
        var ledger = new R4SalvageLedger(3);
        var record = typeof(R4SalvageLedger).GetMethod("Record", new[]
        {
            typeof(string), typeof(IEnumerable<R4SalvageItem>), typeof(R3EnemyArchetype)
        });
        Assert.That(record, Is.Not.Null, "Reward baskets need the actual defeated enemy archetype.");
        var item = new R4SalvageItem(Guid.NewGuid().ToString("N"), "enemy", R3PartKind.Wheel, R4WeaponKind.Basic, .5f);
        Assert.That((bool)record.Invoke(ledger, new object[] { "enemy/1", new[] { item }, R3EnemyArchetype.Scout }), Is.True);
        Assert.That(ledger.Items.Single().Id, Is.EqualTo(item.Id));
    }

    [Test]
    public void OffersComeOnlyFromRealDefeatedRobotsAndRespectExistingLimit()
    {
        var ledger=new R4SalvageLedger(3);
        var scouts=Enumerable.Range(0,8).Select(i=>new R4SalvageItem(Guid.NewGuid().ToString("N"),"scout",R3PartKind.Wheel,R4WeaponKind.Basic,.3f)).ToArray();
        var gunners=Enumerable.Range(0,4).Select(i=>new R4SalvageItem(Guid.NewGuid().ToString("N"),"gunner",R3PartKind.Gun,R4WeaponKind.Automatic,0)).ToArray();
        Assert.That(ledger.Record("scout",scouts,R3EnemyArchetype.Scout),Is.True);
        Assert.That(ledger.Record("gunner",gunners,R3EnemyArchetype.Gunner),Is.True);
        var baskets=ledger.CreateBasketOffers(17,3);
        Assert.That(baskets.Count,Is.EqualTo(3));
        foreach(var basket in baskets)
        {
            var source=basket.Archetype==R3EnemyArchetype.Scout?scouts:gunners;
            Assert.That(basket.Items.Count,Is.InRange(1,3));
            Assert.That(basket.Items.Select(i=>i.Id).Distinct().Count(),Is.EqualTo(basket.Items.Count));
            Assert.That(basket.Items.All(i=>source.Contains(i)),Is.True);
            Assert.That(basket.AvailableParts,Is.EqualTo(source.Length));
            Assert.That(basket.Percent,Is.EqualTo(basket.Items.Count*100/source.Length));
        }
        var again=ledger.CreateBasketOffers(17,3);
        Assert.That(again.SelectMany(b=>b.Items).Select(i=>i.Id),Is.EqualTo(baskets.SelectMany(b=>b.Items).Select(i=>i.Id)));
    }

    [Test]
    public void EmptyOrArmorOnlyEnemiesNeverProduceMisleadingOffers()
    {
        var ledger=new R4SalvageLedger(3);
        var armor=new R4SalvageItem(Guid.NewGuid().ToString("N"),"enemy",R3PartKind.Armor,R4WeaponKind.Basic,1);
        Assert.That(ledger.Record("enemy",new[]{armor},R3EnemyArchetype.Bruiser),Is.True);
        Assert.That(ledger.CreateBasketOffers(1,0),Is.Empty);
    }

    [Test]
    public void FailedDuplicateRecordCannotIntroduceForeignBasketSource()
    {
        var ledger=new R4SalvageLedger(3);
        var part=new R4SalvageItem(Guid.NewGuid().ToString("N"),"enemy",R3PartKind.Wheel,R4WeaponKind.Basic,1);
        Assert.That(ledger.Record("one",new[]{part},R3EnemyArchetype.Scout),Is.True);
        Assert.That(ledger.Record("two",new[]{part},R3EnemyArchetype.Gunner),Is.False);
        Assert.That(ledger.CreateBasketOffers(4,0).All(b=>b.Archetype==R3EnemyArchetype.Scout),Is.True);
    }
}
