using System;
using System.Collections.Generic;
using RoboRougelike.R4;
using UnityEngine;

namespace RoboRougelike.R3
{
    public readonly struct R3SafeZoneOffer
    {
        public readonly int Index, Price;
        public readonly R3PartKind Kind;
        public readonly R4WeaponKind Weapon;
        public readonly bool Sold;
        public R3SafeZoneOffer(int index,R3PartKind kind,R4WeaponKind weapon,int price,bool sold)
        {Index=index;Kind=kind;Weapon=weapon;Price=price;Sold=sold;}
    }

    public sealed partial class R3VehicleModel
    {
        public int SafeVendorStage {get;private set;}
        public int SafeVendorSeed {get;private set;}
        public int SafeVendorSoldMask {get;private set;}
        public int SafeVendorRevision {get;private set;}
        public void PrepareSafeZoneVendor(int seed)
        {
            if(CreativeMode||SafeZoneLastCompleted<=0||SafeVendorStage==SafeZoneLastCompleted)return;
            SafeVendorStage=SafeZoneLastCompleted;SafeVendorSeed=seed;SafeVendorSoldMask=0;SafeVendorRevision=1;
        }
        public IReadOnlyList<R3SafeZoneOffer> SafeZoneVendorOffers()
        {
            if(CreativeMode||SafeVendorStage<=0||SafeVendorStage!=SafeZoneLastCompleted)return Array.Empty<R3SafeZoneOffer>();
            var random=new System.Random(SafeVendorSeed);
            var drive=new[]{R3PartKind.Wheel,R3PartKind.OmniDrive,R3PartKind.CrawlerTrack};
            var passive=new[]{R3PartKind.Battery,R3PartKind.EnergyRegenerator};
            var active=new[]{R3PartKind.RepairPulse,R3PartKind.Overdrive};
            var weapons=new[]{R4WeaponKind.Basic,R4WeaponKind.Automatic,R4WeaponKind.Heavy};
            var kinds=new[]{drive[random.Next(drive.Length)],R3PartKind.Gun,passive[random.Next(passive.Length)],active[random.Next(active.Length)]};
            var selectedWeapon=weapons[random.Next(weapons.Length)];
            // Revision 0 preserves offers already present in existing saves.
            if(SafeVendorRevision>=1&&random.Next(2)==0)kinds[2]=R3PartKind.Armor;
            var result=new List<R3SafeZoneOffer>();
            int count=Mathf.Clamp(Economy.SafeVendorOfferCount,0,kinds.Length);
            float factor=Economy.SafeVendorPriceMultiplier;
            if(float.IsNaN(factor)||float.IsInfinity(factor))factor=.5f;
            for(int i=0;i<count;i++)
            {
                var weapon=kinds[i]==R3PartKind.Gun?selectedWeapon:R4WeaponKind.Basic;
                int price=(int)Math.Min(int.MaxValue,Math.Max(1d,Math.Ceiling(BasePrice(kinds[i],weapon)*(double)Math.Max(0,factor))));
                result.Add(new R3SafeZoneOffer(i,kinds[i],weapon,price,(SafeVendorSoldMask&(1<<i))!=0));
            }
            return result;
        }
        public bool TryPurchaseSafeZoneOffer(int index)
        {
            var offers=SafeZoneVendorOffers();if(index<0||index>=offers.Count)return false;
            var offer=offers[index];int firstNewItem=inventory.Count;
            if(offer.Sold||!PurchaseToInventoryAtPrice(offer.Kind,offer.Weapon,R3WheelStyle.Offroad,R3ArmorShape.Cube,offer.Price))return false;
            if(offer.Kind==R3PartKind.Armor)
            {
                // Allocate at most the pack price across its actual physical
                // blocks; standard purchases and all other parts are untouched.
                int remaining=offer.Price;
                for(int i=firstNewItem;i<inventory.Count;i++)
                {
                    var part=inventory[i];int cap=Math.Min(remaining,DismantleFullValue(part));
                    part.HasDismantleCap=true;part.DismantleCap=cap;remaining-=cap;
                    part.Source="Pakiet sprzedawcy — ograniczony odzysk";
                }
            }
            SafeVendorSoldMask|=1<<index;return true;
        }
    }
}
