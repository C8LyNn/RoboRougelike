using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

public sealed class BacklogSafeVendorTests
{
    [Test] public void DiscountedDrivePacksCannotProfitWhenTheirConfiguredSizeIncreases()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Economy.DriveGrantQuantity=4;Assert.That(model.TryEnterSafeZone(2),Is.True);
        model.PrepareSafeZoneVendor(917);model.Award(1000);var offer=model.SafeZoneVendorOffers()[0];
        Assert.That(model.TryPurchaseSafeZoneOffer(offer.Index),Is.True);
        var pack=model.Inventory.ToArray();Assert.That(pack.Length,Is.EqualTo(4));
        Assert.That(pack.Sum(p=>model.DismantleValue(p)),Is.LessThanOrEqualTo(offer.Price));
        Assert.That(model.FuseInventory(pack.Take(3).Select(p=>p.Id)),Is.True);
        Assert.That(model.Inventory.Sum(p=>model.DismantleValue(p)),Is.LessThanOrEqualTo(offer.Price));
        var saved=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.Inventory.Sum(p=>restored.DismantleValue(p)),Is.LessThanOrEqualTo(offer.Price));
    }
    [Test] public void VendorDoesNotCapARegularDrivePackWhenResaleIsAlreadySafe()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        Assert.That(model.TryEnterSafeZone(2),Is.True);model.PrepareSafeZoneVendor(917);model.Award(1000);
        var offer=model.SafeZoneVendorOffers()[0];Assert.That(model.TryPurchaseSafeZoneOffer(offer.Index),Is.True);
        Assert.That(model.Inventory.Count,Is.EqualTo(2));Assert.That(model.Inventory.All(p=>!p.HasDismantleCap),Is.True);
        Assert.That(model.Inventory.Sum(p=>model.DismantleValue(p)),Is.LessThanOrEqualTo(offer.Price));
    }
    [Test] public void LargerDiscountAlsoCapsAProfitableDrivePack()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Economy.SafeVendorPriceMultiplier=.1f;
        Assert.That(model.TryEnterSafeZone(2),Is.True);model.PrepareSafeZoneVendor(917);model.Award(1000);
        var offer=model.SafeZoneVendorOffers()[0];Assert.That(model.TryPurchaseSafeZoneOffer(offer.Index),Is.True);
        Assert.That(model.Inventory.Count,Is.EqualTo(2));Assert.That(model.Inventory.Sum(p=>model.DismantleValue(p)),Is.LessThanOrEqualTo(offer.Price));
    }
    [Test] public void ConfiguredOffersBeyondFourRemainIndependentThroughSaveAndRestock()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        Assert.That(model.TryEnterSafeZone(2),Is.True);model.PrepareSafeZoneVendor(321);model.Award(1000);
        var original=model.SafeZoneVendorOffers().Select(o=>(o.Kind,o.Weapon,o.Price)).ToArray();
        model.Economy.SafeVendorOfferCount=40;var expanded=model.SafeZoneVendorOffers();
        Assert.That(expanded.Count,Is.EqualTo(40));Assert.That(expanded.Take(4).Select(o=>(o.Kind,o.Weapon,o.Price)),Is.EqualTo(original));
        Assert.That(model.TryPurchaseSafeZoneOffer(0),Is.True);Assert.That(model.TryPurchaseSafeZoneOffer(32),Is.True);
        Assert.That(model.TryPurchaseSafeZoneOffer(33),Is.True);Assert.That(model.SafeZoneVendorOffers()[0].Sold,Is.True);
        Assert.That(model.SafeZoneVendorOffers()[1].Sold,Is.False);Assert.That(model.SafeZoneVendorOffers()[32].Sold,Is.True);
        Assert.That(model.SafeZoneVendorOffers()[33].Sold,Is.True);Assert.That(model.SafeZoneVendorOffers()[34].Sold,Is.False);
        var saved=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(0)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.SafeZoneVendorOffers()[32].Sold,Is.True);Assert.That(restored.SafeZoneVendorOffers()[33].Sold,Is.True);
        Assert.That(restored.TryPurchaseSafeZoneOffer(32),Is.False);var copy=restored.DeepCopy();
        Assert.That(copy.SafeZoneVendorOffers()[33].Sold,Is.True);
        Assert.That(copy.TryEnterSafeZone(4),Is.True);copy.PrepareSafeZoneVendor(44);
        Assert.That(copy.SafeZoneVendorOffers()[32].Sold,Is.False);Assert.That(copy.SafeZoneVendorOffers()[0].Sold,Is.False);
    }
    [Test] public void HugeConfiguredOfferCountCanBeIndexedWithoutGeneratingEveryOffer()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        Assert.That(model.TryEnterSafeZone(2),Is.True);model.PrepareSafeZoneVendor(22);
        model.Economy.SafeVendorOfferCount=int.MaxValue;var offers=model.SafeZoneVendorOffers();
        Assert.That(offers.Count,Is.EqualTo(int.MaxValue));
        var last=offers[int.MaxValue-1];Assert.That(last.Index,Is.EqualTo(int.MaxValue-1));
        Assert.That(offers[int.MaxValue-1].Kind,Is.EqualTo(last.Kind));
    }
    [Test] public void VendorCheckpointAcceptsOldDataAndRejectsInvalidExtraSoldIndices()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        Assert.That(model.TryEnterSafeZone(2),Is.True);model.PrepareSafeZoneVendor(5);model.Award(1000);
        model.Economy.SafeVendorOfferCount=40;Assert.That(model.TryPurchaseSafeZoneOffer(0),Is.True);
        Assert.That(model.TryPurchaseSafeZoneOffer(32),Is.True);
        var saved=model.MakeCheckpoint(0);saved.safeVendorExtraSold=null;
        var old=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(saved));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(old,R3VehicleKind.Light,model.Economy,0,out var restored,out var error),Is.True,error);
        Assert.That(restored.SafeZoneVendorOffers()[0].Sold,Is.True);Assert.That(restored.SafeZoneVendorOffers()[32].Sold,Is.False);
        saved.safeVendorExtraSold=new[]{32,32};
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,0,out _,out _),Is.False);
        saved.safeVendorExtraSold=new[]{1};
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,0,out _,out _),Is.False);
    }
    static R3VehicleModel ModelWithArmorOffer()
    {
        for(int seed=0;seed<100;seed++)
        {
            var candidate=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
            candidate.TryEnterSafeZone(2);candidate.PrepareSafeZoneVendor(seed);
            if(candidate.SafeZoneVendorOffers().Any(o=>o.Kind==R3PartKind.Armor))return candidate;
        }
        Assert.Fail("The vendor must offer armor in some visits.");return null;
    }

    [Test] public void DiscountedArmorPackCannotProfitAndCapSurvivesSaveAndMixedFusion()
    {
        var model=ModelWithArmorOffer();model.Award(1000);
        var offer=model.SafeZoneVendorOffers().Single(o=>o.Kind==R3PartKind.Armor);
        int start=model.Inventory.Count;Assert.That(model.TryPurchaseSafeZoneOffer(offer.Index),Is.True);
        var discounted=model.Inventory.Skip(start).ToArray();
        Assert.That(discounted.Length,Is.EqualTo(5));
        Assert.That(discounted.Sum(p=>model.DismantleValue(p)),Is.LessThanOrEqualTo(offer.Price));
        var mounting=discounted[2];int mountValue=model.DismantleValue(mounting);
        var cells=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(n=>p.Cell+n)).Distinct().ToArray();
        var available=cells.Where(c=>model.OwnedPlacementError(mounting.Id,c,default)==null).ToArray();
        Assert.That(available,Is.Not.Empty);Assert.That(model.Mount(mounting.Id,available[0]),Is.True);
        Assert.That(model.Remove(available[0]),Is.True);
        Assert.That(model.DismantleValue(model.Inventory.Single(p=>p.Id==mounting.Id)),Is.EqualTo(mountValue));
        Assert.That(model.PurchaseToInventory(R3PartKind.Armor),Is.True);
        var ordinary=model.Inventory.Skip(start+5).First();
        Assert.That(model.DismantleValue(ordinary),Is.EqualTo(1),"Ordinary purchases retain the original dismantle value");
        int ceiling=model.DismantleValue(discounted[0])+model.DismantleValue(discounted[1])+model.DismantleValue(ordinary);
        Assert.That(model.FuseInventory(new[]{discounted[0].Id,discounted[1].Id,ordinary.Id}),Is.True);
        var fused=model.Inventory.Single(p=>p.Kind==R3PartKind.Armor&&p.Tier==2);
        Assert.That(model.DismantleValue(fused),Is.LessThanOrEqualTo(ceiling));
        string id=fused.Id;
        var saved=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(100)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,200,out var restored,out var error),Is.True,error);
        var restoredFused=restored.Inventory.Single(p=>p.Id==id);
        Assert.That(restored.DismantleValue(restoredFused),Is.LessThanOrEqualTo(ceiling));
        Assert.That(restored.Inventory.Where(p=>discounted.Skip(2).Any(d=>d.Id==p.Id)).Sum(p=>restored.DismantleValue(p))+
            restored.DismantleValue(restoredFused),Is.LessThanOrEqualTo(offer.Price+model.DismantleValue(ordinary)));
    }

    [Test] public void ExistingVendorRevisionKeepsItsOriginalOffers()
    {
        var model=ModelWithArmorOffer();var saved=model.MakeCheckpoint(100);saved.safeVendorRevision=0;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(saved,R3VehicleKind.Light,model.Economy,200,out var legacy,out var error),Is.True,error);
        Assert.That(legacy.SafeZoneVendorOffers().Any(o=>o.Kind==R3PartKind.Armor),Is.False);
        var copy=legacy.DeepCopy();Assert.That(copy.SafeZoneVendorOffers().Select(o=>o.Kind),Is.EqualTo(legacy.SafeZoneVendorOffers().Select(o=>o.Kind)));
        var again=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(copy.MakeCheckpoint(200)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(again,R3VehicleKind.Light,copy.Economy,300,out var reloaded,out error),Is.True,error);
        Assert.That(reloaded.SafeZoneVendorOffers().Select(o=>o.Kind),Is.EqualTo(legacy.SafeZoneVendorOffers().Select(o=>o.Kind)));
    }
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
