using System.Linq;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        private static bool UsePortraitSalvage(int width,int height,bool salvage,bool overlay)
            =>width>0&&height>width&&salvage&&!overlay;
        private static Rect PortraitRewardBonusCard(int index)=>new Rect(24,132+index*104,720,96);
        private static Rect PortraitRewardLootCard(int index)=>new Rect(24+(index%2)*366,480+(index/2)*103,354,98);
        private static Rect PortraitRewardLootDetails(int index)
        {var card=PortraitRewardLootCard(index);return new Rect(card.x+10,card.y+3,230,67);}
        private static Rect PortraitRewardLootSource(int index)
        {var card=PortraitRewardLootCard(index);return new Rect(card.x+10,card.y+75,334,20);}
        private static Rect PortraitRewardLootButton(int index)
        {var card=PortraitRewardLootCard(index);return new Rect(card.x+248,card.y+15,96,52);}
        private static Rect PortraitRewardPager()=>new Rect(24,792,720,44);
        private static Rect PortraitRewardSummary()=>new Rect(24,840,720,72);
        private static Rect PortraitRewardConfirm()=>new Rect(24,930,720,64);
        private static Rect PortraitRewardInventoryCard(int index)=>new Rect(24,170+index*120,720,108);
        private static Rect PortraitRewardInventoryDetails(int index)
        {var card=PortraitRewardInventoryCard(index);return new Rect(card.x+12,card.y+5,450,98);}
        private static Rect PortraitRewardDismantleButton(int index)
        {var card=PortraitRewardInventoryCard(index);return new Rect(card.x+490,card.y+23,215,60);}
        private static Rect PortraitRewardInventoryPager()=>new Rect(24,800,720,52);
        private static Rect PortraitRewardInventoryBack()=>new Rect(24,930,720,64);

        private void DrawPortraitSalvage(R1RunController run)
        {
            var ledger=run.Salvage;TrackRewardLedger(ledger);
            if(rewardInventoryOpen){DrawPortraitRewardInventory(run);return;}
            GUI.Label(new Rect(24,20,720,42),"WYBÓR NAGRÓD",title);
            GUI.Label(new Rect(24,68,720,28),$"{(run.LastActivityWasReactor?"Reaktor obroniony":"Zwycięstwo")} • +{run.LastScrapReward} Z za walkę",body);
            GUI.Label(new Rect(24,100,720,28),"MODUŁ BONUSOWY • WYBIERZ 1 Z 3",heading);
            var offers=run.Progress.Offers.ToArray();
            for(int i=0;i<offers.Length;i++)
            {
                var card=PortraitRewardBonusCard(i);var upgrade=offers[i];Panel(card);
                GUI.Label(new Rect(card.x+12,card.y+5,530,24),upgrade.Title,body);
                GUI.Label(new Rect(card.x+12,card.y+30,530,60),upgrade.Description,small);
                bool chosen=selectedBonus==i;GUI.backgroundColor=chosen?new Color(.35f,.85f,.65f):Color.white;
                if(GUI.Button(new Rect(card.x+555,card.y+22,150,52),chosen?"Wybrano ✓":"Wybierz",compactButton))PortraitRewardSelectBonus(i);
                GUI.backgroundColor=Color.white;
            }
            if(offers.Length==0)GUI.Label(new Rect(36,145,680,50),"Brak propozycji modułów bonusowych.",small);
            GUI.Label(new Rect(24,442,720,30),"CZĘŚCI Z WRAKÓW • WYBIERZ DO 3",heading);
            var items=ledger.Items.Where(item=>item.Selectable).ToArray();
            int pages=Mathf.Max(1,Mathf.CeilToInt(items.Length/6f));lootPage=Mathf.Clamp(lootPage,0,pages-1);
            var visible=items.Skip(lootPage*6).Take(6).ToArray();
            for(int i=0;i<visible.Length;i++)
            {
                var item=visible[i];var part=item.ToPart();var card=PortraitRewardLootCard(i);var detail=PortraitRewardLootDetails(i);Panel(card);
                GUI.Label(new Rect(detail.x,card.y+3,detail.width,22),R4WeaponDefinition.PartName(part),small);
                string condition=item.Health<=0?"ZNISZCZONA":item.Health<1?"USZKODZONA":"NIENARUSZONA";
                GUI.Label(new Rect(detail.x,card.y+26,detail.width,19),$"{condition} • HP {item.Health*100:0}%",small);
                string ammo=SalvageAmmoLabel(part);
                GUI.Label(new Rect(detail.x,card.y+48,detail.width,20),$"{(item.Health<=0?"Odbudowa":"Naprawa")}: {run.VehicleModel.RepairCost(part)} Z{(ammo==null?"":" • "+ammo)}",small);
                GUI.Label(PortraitRewardLootSource(i),item.Source,small);
                bool chosen=lootSelection.Contains(item.Id);GUI.enabled=chosen||lootSelection.Count<3;
                GUI.backgroundColor=chosen?new Color(.35f,.85f,.65f):Color.white;
                if(GUI.Button(PortraitRewardLootButton(i),chosen?"Wybrano ✓":"Zatrzymaj",compactButton))PortraitRewardToggleLoot(item);
                GUI.enabled=true;GUI.backgroundColor=Color.white;
            }
            if(items.Length==0)GUI.Label(new Rect(36,495,680,50),"Brak części do odzyskania.",small);
            var pager=PortraitRewardPager();
            GUI.enabled=lootPage>0;
            if(GUI.Button(new Rect(pager.x,pager.y,100,44),"←",compactButton))PortraitRewardMoveLootPage(-1,items.Length);
            GUI.Label(new Rect(pager.x+112,pager.y+10,135,27),$"Strona {lootPage+1}/{pages}",small);
            GUI.enabled=lootPage<pages-1;
            if(GUI.Button(new Rect(pager.x+250,pager.y,100,44),"→",compactButton))PortraitRewardMoveLootPage(1,items.Length);
            GUI.enabled=true;
            if(GUI.Button(new Rect(pager.x+370,pager.y,350,44),$"Magazyn {run.VehicleModel.Inventory.Count}/{R3VehicleModel.InventoryCapacity}",compactButton))rewardInventoryOpen=true;
            bool room=run.VehicleModel.Inventory.Count+lootSelection.Count+1<=R3VehicleModel.InventoryCapacity;
            GUI.Label(PortraitRewardSummary(),
                $"Bonus: {(selectedBonus>=0?"1/1":"0/1 (WYMAGANY)")} • Części: {lootSelection.Count}/3\n"+
                $"Niewybrane części → +{ledger.PreviewScrap(run.VehicleModel,lootSelection)} Z. Bonus jest nieaktywny do montażu.\n"+
                (room?"":"Brak miejsca na nagrody. Otwórz magazyn i rozmontuj część."),small);
            GUI.enabled=selectedBonus>=0&&room;
            if(GUI.Button(PortraitRewardConfirm(),"Zatwierdź oba wybory i kontynuuj",button))PortraitRewardConfirm(run);
            GUI.enabled=true;
        }

        private void DrawPortraitRewardInventory(R1RunController run)
        {
            var model=run.VehicleModel;
            GUI.Label(new Rect(24,20,720,42),"MAGAZYN NAGRÓD",title);
            GUI.Label(new Rect(24,70,720,84),$"Zajęte: {model.Inventory.Count}/{R3VehicleModel.InventoryCapacity} • Potrzeba {lootSelection.Count+1} wolnych miejsc.\nRozmontowanie usuwa część i wypłaca złom. Wymaga drugiego kliknięcia.",body);
            var items=model.Inventory.ToArray();int pages=Mathf.Max(1,Mathf.CeilToInt(items.Length/5f));
            rewardInventoryPage=Mathf.Clamp(rewardInventoryPage,0,pages-1);
            var visible=items.Skip(rewardInventoryPage*5).Take(5).ToArray();
            for(int i=0;i<visible.Length;i++)
            {
                var item=visible[i];var card=PortraitRewardInventoryCard(i);var detail=PortraitRewardInventoryDetails(i);Panel(card);
                GUI.Label(new Rect(detail.x,card.y+5,detail.width,23),R4WeaponDefinition.PartName(item),body);
                GUI.Label(new Rect(detail.x,card.y+30,detail.width,28),$"Źródło: {item.Source}",small);
                GUI.Label(new Rect(detail.x,card.y+59,detail.width,20),$"HP {item.Health*100:0}%",small);
                string ammo=SalvageAmmoLabel(item);
                GUI.Label(new Rect(detail.x,card.y+80,detail.width,21),$"{(item.Health<=0?"Odbudowa":"Naprawa")}: {model.RepairCost(item)} Z{(ammo==null?"":" • "+ammo)}",small);
                if(GUI.Button(PortraitRewardDismantleButton(i),rewardDismantleConfirmation==item.Id?"Potwierdź rozmontowanie":$"Rozmontuj → {model.DismantleValue(item)} Z",compactButton))
                {
                    bool confirming=rewardDismantleConfirmation==item.Id;
                    PortraitRewardDismantle(run,item.Id);
                    if(confirming)return;
                }
            }
            var pager=PortraitRewardInventoryPager();
            GUI.enabled=rewardInventoryPage>0;
            if(GUI.Button(new Rect(pager.x,pager.y,110,48),"←",compactButton)){rewardInventoryPage--;rewardDismantleConfirmation=null;}
            GUI.Label(new Rect(pager.x+125,pager.y+11,180,27),$"Strona {rewardInventoryPage+1}/{pages}",small);
            GUI.enabled=rewardInventoryPage<pages-1;
            if(GUI.Button(new Rect(pager.x+310,pager.y,110,48),"→",compactButton)){rewardInventoryPage++;rewardDismantleConfirmation=null;}
            GUI.enabled=true;
            if(GUI.Button(PortraitRewardInventoryBack(),"Wróć do wybranych nagród",button))
            {rewardInventoryOpen=false;rewardDismantleConfirmation=null;}
        }

        private void PortraitRewardSelectBonus(int index)=>selectedBonus=index;
        private void PortraitRewardMoveLootPage(int step,int itemCount)
        {lootPage=Mathf.Clamp(lootPage+step,0,Mathf.Max(1,Mathf.CeilToInt(itemCount/6f))-1);}
        private void PortraitRewardToggleLoot(R4SalvageItem item)
        {
            if(lootSelection.Contains(item.Id))lootSelection.Remove(item.Id);
            else if(item.Selectable&&lootSelection.Count<3)lootSelection.Add(item.Id);
        }
        private bool PortraitRewardConfirm(R1RunController run)=>run.ConfirmRewards(lootSelection,selectedBonus>=0?new[]{selectedBonus}:System.Array.Empty<int>());
        private bool PortraitRewardDismantle(R1RunController run,string id)
        {
            if(rewardDismantleConfirmation!=id){rewardDismantleConfirmation=id;return false;}
            bool changed=run.DismantleRewardInventoryItem(id);rewardDismantleConfirmation=null;return changed;
        }
    }
}
