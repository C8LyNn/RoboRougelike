using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

public sealed class UI943PortraitRewardsLayoutTests
{
    static MethodInfo Method(string name)
    {var m=typeof(R1RunHUD).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);Assert.That(m,Is.Not.Null,name);return m;}
    static Rect RectOf(string name,params object[] args)=>(Rect)Method(name).Invoke(null,args);
    [TestCase(-1,3,-1,3,0,0,"Wybierz bonus")]
    [TestCase(0,3,-1,3,0,0,"Wybierz zestaw")]
    [TestCase(0,3,0,3,198,3,"Zwolnij 2 miejsca")]
    [TestCase(0,3,0,3,200,0,"Zwolnij 1 miejsce")]
    [TestCase(0,3,0,3,200,11,"Zwolnij 12 miejsc")]
    [TestCase(0,3,0,3,200,21,"Zwolnij 22 miejsca")]
    [TestCase(0,3,0,3,196,3,"Zatwierdź oba")]
    [TestCase(0,3,-1,0,0,0,"Odbierz bonus")]
    [TestCase(-1,0,-1,0,0,0,"Brak dostępnych bonusów")]
    public void RewardButtonExplainsMissingSelectionOrSpace(int bonus,int bonusCount,int basket,int basketCount,int inventory,int loot,string expected)
    {
        var caption=(string)Method("RewardActionCaption").Invoke(null,new object[]{bonus,bonusCount,basket,basketCount,inventory,loot});
        Assert.That(caption,Does.Contain(expected));
    }
    [Test] public void NoLootRequiresOnlyTheBonusAndExplainsIt()
    {
        var caption=(string)Method("RewardConfirmCaption").Invoke(null,new object[]{0});
        var intro=(string)Method("RewardIntro").Invoke(null,new object[]{0,25});
        Assert.That(caption,Does.Contain("bonus").And.Not.Contain("oba"));
        Assert.That(intro,Does.Contain("Brak części").And.Contain("25 Z"));
        Assert.That((bool)Method("RewardSelectionReady").Invoke(null,new object[]{0,3,-1,0,0,0}),Is.True);
        Assert.That((bool)Method("RewardSelectionReady").Invoke(null,new object[]{0,3,-1,1,0,0}),Is.False);
        Assert.That((string)Method("RewardConfirmCaption").Invoke(null,new object[]{1}),Does.Contain("oba"));
    }
    [Test] public void RewardContentsShowActualConfiguredBundleQuantity()
    {
        var model=new R3VehicleModel(new R3VehicleDesign());model.Economy.DriveGrantQuantity=3;
        var wheel=new R4SalvageItem(new R3PartState(Vector3Int.zero,R3PartKind.Wheel),1);
        var label=(string)Method("RewardBasketContentsForModel").Invoke(null,new object[]{new[]{wheel},model});
        Assert.That(label,Does.Contain("×3"));
    }
    [Test] public void RerollButtonsFitAlongsideInventoryAndBasketCards()
    {
        var portrait=RectOf("PortraitRewardRerollRect");var pager=RectOf("PortraitRewardPager");
        Assert.That(portrait.xMin,Is.GreaterThanOrEqualTo(pager.xMin));Assert.That(portrait.xMax,Is.LessThanOrEqualTo(pager.xMax));
        Assert.That(portrait.Overlaps(new Rect(pager.x+370,pager.y,350,44)),Is.False);
        for(int i=0;i<3;i++)Assert.That(portrait.Overlaps(RectOf("PortraitRewardBasketCard",i)),Is.False);
        var landscape=RectOf("RewardRerollRect");Assert.That(landscape.Overlaps(new Rect(850,556,375,35)),Is.False);
    }
    [Test] public void ReorderingExistingRewardBasketsIsNotANewOffer()
    {
        var ledger=new R4SalvageLedger(3);
        Assert.That(ledger.Record("a",new[]{new R4SalvageItem("gun","test",R3PartKind.Gun,R4WeaponKind.Basic,1)},R3EnemyArchetype.Scout),Is.True);
        Assert.That(ledger.Record("b",new[]{new R4SalvageItem("wheel","test",R3PartKind.Wheel,R4WeaponKind.Basic,1)},R3EnemyArchetype.Bruiser),Is.True);
        var offers=ledger.CreateBasketOffers(7,0);Assert.That(offers.Count,Is.EqualTo(3));
        var reordered=offers.Skip(1).Concat(offers.Take(1)).ToArray();
        Assert.That((bool)Method("SameRewardBaskets").Invoke(null,new object[]{offers,reordered}),Is.True);
    }
    [Test] public void DamagedRewardInventoryShowsWorstPartsFirstWithoutChangingInventory()
    {
        var model=new R3VehicleModel(new R3VehicleDesign());
        var parts=new[]{new R3PartState(Vector3Int.zero,R3PartKind.Wheel,1),
            new R3PartState(Vector3Int.zero,R3PartKind.Wheel,.5f),new R3PartState(Vector3Int.zero,R3PartKind.Wheel,.1f)};
        Assert.That(model.ReceiveSalvage(parts),Is.True);
        var before=model.Inventory.Select(part=>part.Id).ToArray();
        var filtered=(R3PartState[])Method("RewardInventoryItems").Invoke(null,new object[]{model,true});
        Assert.That(filtered.Select(part=>part.Id),Is.EqualTo(new[]{parts[2].Id,parts[1].Id}));
        var all=(R3PartState[])Method("RewardInventoryItems").Invoke(null,new object[]{model,false});
        Assert.That(all.Select(part=>part.Id),Is.EqualTo(before));
        Assert.That(model.Inventory.Select(part=>part.Id),Is.EqualTo(before));
    }
    [TestCase(195,3,0)] [TestCase(197,3,1)] [TestCase(200,0,1)]
    public void MissingRewardSpaceAccountsForSelectedLootAndBonus(int stored,int loot,int expected)
    {
        Assert.That((int)Method("RewardInventoryMissingSlots").Invoke(null,new object[]{stored,loot}),Is.EqualTo(expected));
    }
    [Test] public void RewardInventoryFilterFitsNextToPageControls()
    {
        var pager=RectOf("PortraitRewardInventoryPager");var filter=RectOf("PortraitRewardInventoryFilter");
        In(pager,filter,"Filtr magazynu");
        Assert.That(filter.Overlaps(new Rect(pager.x,pager.y,110,48)),Is.False);
        Assert.That(filter.Overlaps(new Rect(pager.x+310,pager.y,110,48)),Is.False);
    }
    [TestCase(false)] [TestCase(true)]
    public void DismantleConfirmationAndCancelFitInsideTheOriginalButton(bool portrait)
    {
        for(int i=0;i<5;i++)
        {
            var area=portrait?RectOf("PortraitRewardDismantleButton",i):new Rect(860,190+i*76,350,48);
            var confirm=RectOf("RewardDismantleChoiceRect",i,portrait,false);
            var cancel=RectOf("RewardDismantleChoiceRect",i,portrait,true);
            In(area,confirm,"Potwierdzenie");In(area,cancel,"Anulowanie");
            Assert.That(confirm.Overlaps(cancel),Is.False);
            Assert.That(cancel.height,Is.GreaterThanOrEqualTo(44));
        }
    }
    static void In(Rect outer,Rect inner,string name)
    {Assert.That(outer.Contains(inner.min)&&outer.Contains(inner.max-Vector2.one),Is.True,name);}
    [Test] public void BasketSummaryGroupsIdenticalPartsButKeepsDifferentTiersVisible()
    {
        var t1=new R3PartState(Vector3Int.zero,R3PartKind.Wheel,1);
        var t2=new R3PartState(Vector3Int.zero,R3PartKind.Wheel,1){Tier=2};
        var items=new[]{new R4SalvageItem(t1,1),new R4SalvageItem(t1.Copy(),.5f),new R4SalvageItem(t2,1)};
        var text=(string)Method("RewardBasketContents").Invoke(null,new object[]{items});
        Assert.That(text,Does.Contain("Koło terenowe ×2"));
        Assert.That(text,Does.Contain("Koło terenowe T2"));
    }
    [Test] public void BasketHeadingShowsTheActualRecoveryPercentageAlongsideCounts()
    {
        var ledger=new R4SalvageLedger(3);
        var parts=Enumerable.Range(0,8).Select(i=>new R4SalvageItem("part-"+i,"enemy",R3PartKind.Wheel,R4WeaponKind.Basic,1)).ToArray();
        Assert.That(ledger.Record("enemy",parts,R3EnemyArchetype.Scout),Is.True);
        var basket=ledger.CreateBasketOffers(17,3).First();
        var heading=(string)Method("RewardBasketHeading").Invoke(null,new object[]{basket});
        Assert.That(heading,Does.Contain($"{basket.Percent}%"));
        Assert.That(heading,Does.Contain($"{basket.Items.Count}/{basket.AvailableParts}"));
        Assert.That(heading,Does.Contain("Odzysk"));
    }
    [Test] public void BasketSummaryDoesNotMergeDifferentBonusEffectsWithTheSameTitle()
    {
        var damage=new R3PartState(Vector3Int.zero,R3PartKind.BonusModule,1){UpgradeId="same",UpgradeTitle="Moduł",UpgradeEffect=R1UpgradeEffect.Damage,UpgradeAmount=.2f};
        var speed=new R3PartState(Vector3Int.zero,R3PartKind.BonusModule,1){UpgradeId="same",UpgradeTitle="Moduł",UpgradeEffect=R1UpgradeEffect.Mobility,UpgradeAmount=.2f};
        var text=(string)Method("RewardBasketContents").Invoke(null,new object[]{new[]{new R4SalvageItem(damage,1),new R4SalvageItem(speed,1)}});
        Assert.That(text,Does.Contain("Obrażenia").And.Contain("Mobilność"));
        Assert.That(text,Does.Not.Contain("×2"));
    }
    [Test] public void InstanceDescriptionUsesActualTierStrength()
    {
        var item=new R3PartState(Vector3Int.zero,R3PartKind.BonusModule,1){Tier=2,UpgradeEffect=R1UpgradeEffect.Damage,UpgradeAmount=.2f};
        string Text()=>(string)Method("BonusInstanceText").Invoke(null,new object[]{item});
        Assert.That(Text(),Does.Contain("40%"));
        item.UpgradeEffect=R1UpgradeEffect.Mobility;Assert.That(Text(),Does.Contain("40%"));
        item.UpgradeEffect=R1UpgradeEffect.Hull;Assert.That(Text(),Does.Contain("40%"));
        item.UpgradeEffect=R1UpgradeEffect.Reload;Assert.That(Text(),Does.Contain("40%"));
        item.UpgradeEffect=R1UpgradeEffect.Repair;Assert.That(Text(),Does.Contain("5%"));
        item.Tier=3;Assert.That(Text(),Does.Contain("10%"),"Four repair contributions each provide 2.5% per second.");
    }
    [Test] public void ActualPortraitBasketCardsFitWithoutCoveringTheirControls()
    {
        var canvas=new Rect(0,0,768,1024);
        for(int i=0;i<3;i++)
        {
            var card=RectOf("PortraitRewardBasketCard",i);
            var info=RectOf("PortraitRewardBasketInfo",i);
            var button=RectOf("PortraitRewardBasketButton",i);
            In(canvas,card,"Zestaw "+i);In(card,info,"Opis zestawu "+i);In(card,button,"Przycisk zestawu "+i);
            Assert.That(info.Overlaps(button),Is.False);
            Assert.That(card.Overlaps(RectOf("PortraitRewardPager")),Is.False);
            if(i>0)Assert.That(card.Overlaps(RectOf("PortraitRewardBasketCard",i-1)),Is.False);
        }
    }
    [TestCase(768,1024,true)] [TestCase(1024,768,false)] [TestCase(1920,1080,false)]
    public void PortraitIsOnlyUsedForUnobscuredSalvage(int width,int height,bool expected)
    {
        var m=Method("UsePortraitSalvage");
        Assert.That((bool)m.Invoke(null,new object[]{width,height,true,false}),Is.EqualTo(expected));
        Assert.That((bool)m.Invoke(null,new object[]{width,height,false,false}),Is.False);
        Assert.That((bool)m.Invoke(null,new object[]{width,height,true,true}),Is.False,"Pauza, błąd, mapa i przejście zachowują swój układ.");
    }
    [Test] public void RewardCardsAndControlsFitWithoutOverlap()
    {
        var canvas=new Rect(0,0,768,1024);var areas=new List<Rect>();
        for(int i=0;i<3;i++)areas.Add(RectOf("PortraitRewardBonusCard",i));
        for(int i=0;i<6;i++)areas.Add(RectOf("PortraitRewardLootCard",i));
        areas.Add(RectOf("PortraitRewardPager"));areas.Add(RectOf("PortraitRewardSummary"));areas.Add(RectOf("PortraitRewardConfirm"));
        for(int i=0;i<areas.Count;i++)
        {In(canvas,areas[i],"Poza ekranem "+i);for(int j=0;j<i;j++)Assert.That(areas[i].Overlaps(areas[j]),Is.False,$"Obszary {i} i {j} nachodzą na siebie.");}
        Assert.That(RectOf("PortraitRewardConfirm").height,Is.GreaterThanOrEqualTo(44));
        for(int i=0;i<6;i++)
        {
            var card=RectOf("PortraitRewardLootCard",i);var details=RectOf("PortraitRewardLootDetails",i);var button=RectOf("PortraitRewardLootButton",i);var source=RectOf("PortraitRewardLootSource",i);
            In(card,details,"Dane łupu "+i);In(card,button,"Przycisk łupu "+i);In(card,source,"Źródło "+i);
            Assert.That(details.Overlaps(button),Is.False,"Przycisk nie zasłania HP, naprawy ani amunicji.");
            Assert.That(source.Overlaps(button)||source.Overlaps(details),Is.False,"Źródło nie zasłania pozostałych danych.");
            Assert.That(button.height,Is.GreaterThanOrEqualTo(44));
        }
    }
    [Test] public void InventoryCardsPagerAndReturnFitWithoutOverlap()
    {
        var canvas=new Rect(0,0,768,1024);var areas=new List<Rect>();
        for(int i=0;i<5;i++)areas.Add(RectOf("PortraitRewardInventoryCard",i));
        areas.Add(RectOf("PortraitRewardInventoryPager"));areas.Add(RectOf("PortraitRewardInventoryBack"));
        for(int i=0;i<areas.Count;i++)
        {In(canvas,areas[i],"Poza ekranem "+i);for(int j=0;j<i;j++)Assert.That(areas[i].Overlaps(areas[j]),Is.False,$"Obszary {i} i {j} nachodzą na siebie.");}
        for(int i=0;i<5;i++)
        {var card=RectOf("PortraitRewardInventoryCard",i);var text=RectOf("PortraitRewardInventoryDetails",i);var button=RectOf("PortraitRewardDismantleButton",i);In(card,text,"Dane "+i);In(card,button,"Demontaż "+i);Assert.That(text.Overlaps(button),Is.False);Assert.That(button.height,Is.GreaterThanOrEqualTo(44));}
    }
}
