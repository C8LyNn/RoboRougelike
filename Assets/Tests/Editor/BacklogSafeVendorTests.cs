using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

public sealed class BacklogSafeVendorTests
{
    [Test] public void OffersUseBasePricesAndPreserveSalesAcrossCheckpoints()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        Assert.That(model.TryEnterSafeZone(2),Is.True);
        model.PrepareSafeZoneVendor(917);
        var offers=model.SafeZoneVendorOffers();
        Assert.That(offers.Count,Is.EqualTo(4));
        Assert.That(offers.Select(o=>o.Kind).Distinct().Count(),Is.EqualTo(4));
        var drive=offers[0];
        Assert.That(drive.Price,Is.EqualTo(Mathf.CeilToInt(model.Economy.Price(drive.Kind)*.5f)));
        int oldScrap=model.Scrap;int oldCount=model.Inventory.Count;
        model.Award(1000);oldScrap=model.Scrap;
        Assert.That(model.TryPurchaseSafeZoneOffer(drive.Index),Is.True);
        Assert.That(model.Scrap,Is.EqualTo(oldScrap-drive.Price));
        var purchased=model.Inventory.Skip(oldCount).ToArray();
        Assert.That(purchased.Length,Is.EqualTo(model.GrantQuantity(drive.Kind)));
        Assert.That(purchased.Sum(p=>model.DismantleValue(p)),Is.LessThanOrEqualTo(drive.Price),"Vendor packs must not create scrap on dismantle");
        Assert.That(model.TryPurchaseSafeZoneOffer(drive.Index),Is.False);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(100)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,model.Economy,200,out var restored,out var error),Is.True,error);
        Assert.That(restored.SafeZoneVendorOffers().Select(o=>o.Kind),Is.EqualTo(offers.Select(o=>o.Kind)));
        Assert.That(restored.SafeZoneVendorOffers().Select(o=>o.Weapon),Is.EqualTo(offers.Select(o=>o.Weapon)));
        Assert.That(restored.SafeZoneVendorOffers()[drive.Index].Sold,Is.True);
        Assert.That(restored.TryPurchaseSafeZoneOffer(drive.Index),Is.False);
    }

    [Test] public void FullInventoryDoesNotChargeOrConsumeAnOffer()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        Assert.That(model.TryEnterSafeZone(2),Is.True);model.PrepareSafeZoneVendor(2);model.Award(1000);
        var filler=Enumerable.Range(0,model.AvailableInventoryCapacity-model.Inventory.Count)
            .Select(_=>new R3PartState(default,R3PartKind.Battery)).ToArray();
        Assert.That(model.ReceiveSalvage(filler),Is.True);int before=model.Scrap;
        Assert.That(model.TryPurchaseSafeZoneOffer(0),Is.False);
        Assert.That(model.Scrap,Is.EqualTo(before));Assert.That(model.SafeZoneVendorOffers()[0].Sold,Is.False);
    }

    [Test] public void AllDefaultOfferVariantsCannotProfitByImmediateDismantling()
    {
        for(int seed=0;seed<50;seed++)
        {
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
            Assert.That(model.TryEnterSafeZone(2),Is.True);model.PrepareSafeZoneVendor(seed);model.Award(1000);
            foreach(var offer in model.SafeZoneVendorOffers())
            {
                int count=model.Inventory.Count;
                Assert.That(model.TryPurchaseSafeZoneOffer(offer.Index),Is.True);
                int resale=model.Inventory.Skip(count).Sum(part=>model.DismantleValue(part));
                Assert.That(resale,Is.LessThanOrEqualTo(offer.Price),$"seed {seed}, {offer.Kind}");
            }
        }
    }

    [Test] public void InsufficientFundsDoNotConsumeTheOfferAndNextSafeZoneRestocks()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        Assert.That(model.TryEnterSafeZone(2),Is.True);model.PrepareSafeZoneVendor(10);
        Assert.That(model.TrySpendScrap(model.Scrap),Is.True);
        Assert.That(model.TryPurchaseSafeZoneOffer(0),Is.False);
        Assert.That(model.SafeZoneVendorOffers()[0].Sold,Is.False);
        model.Award(1000);Assert.That(model.TryPurchaseSafeZoneOffer(0),Is.True);
        model.PrepareSafeZoneVendor(42);
        Assert.That(model.SafeZoneVendorOffers()[0].Sold,Is.True,"Reopening cannot restock the same visit");
        var clone=model.DeepCopy();Assert.That(clone.SafeZoneVendorOffers()[0].Sold,Is.True);
        Assert.That(model.TryEnterSafeZone(4),Is.True);model.PrepareSafeZoneVendor(42);
        Assert.That(model.SafeZoneVendorOffers()[0].Sold,Is.False);
        Assert.That(model.SafeVendorStage,Is.EqualTo(4));
    }
}
