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
            GUI.Label(new Rect(24,20,720,42),"ZWYCIĘSTWO",title);
            GUI.Label(new Rect(24,68,720,28),$"{(run.LastActivityWasReactor?"Reaktor obroniony":$"Starcie {run.Progress.CompletedEncounters}/{run.Progress.TotalEncounters}")} • +{run.LastScrapReward} Z"+
                (string.IsNullOrEmpty(run.TechnologyMessage)?" • wybierz nagrody":run.Technology?.Error!=null?" • problem zapisu technologii":" • nowa technologia"),body);
            GUI.Label(new Rect(24,100,720,28),"BONUS • [1 / 2 / 3]",heading);
            var offers=run.Progress.Offers.ToArray();
            for(int i=0;i<offers.Length;i++)
            {
                var card=PortraitRewardBonusCard(i);var upgrade=offers[i];Panel(card);
                GUI.Label(new Rect(card.x+12,card.y+5,530,24),upgrade.Title,body);
                GUI.Label(new Rect(card.x+12,card.y+30,530,60),upgrade.Description,small);
                bool chosen=selectedBonus==i;GUI.backgroundColor=chosen?new Color(.35f,.85f,.65f):Color.white;
                if(GUI.Button(new Rect(card.x+555,card.y+22,150,52),chosen?"Wybrano ✓":$"Wybierz [{i+1}]",compactButton))PortraitRewardSelectBonus(i);
                GUI.backgroundColor=Color.white;
            }
            if(offers.Length==0)GUI.Label(new Rect(36,145,680,50),"Brak propozycji modułów bonusowych.",small);
            GUI.Label(new Rect(24,442,720,30),"ZESTAW • [Q / W / E]",heading);
            for(int i=0;i<rewardBaskets.Count;i++)
            {
                var basket=rewardBaskets[i];var card=new Rect(24,480+i*103,720,98);Panel(card);
                GUI.contentColor=BasketColor(basket);
                GUI.Label(new Rect(card.x+10,card.y+5,510,24),$"{R3EnemyProfiles.Name(basket.Archetype)} • {basket.Items.Count}/{basket.AvailableParts} części",small);
                GUI.contentColor=Color.white;
                GUI.Label(new Rect(card.x+10,card.y+31,510,58),string.Join(", ",basket.Items.Select(p=>R4WeaponDefinition.PartName(p.ToPart())))+"\nOdbiór: 100% HP",small);
                bool chosen=selectedBasket==i;
                GUI.backgroundColor=chosen?new Color(.35f,.85f,.65f):Color.white;
                string key=i<3?"QWE"[i].ToString():null;
                if(GUI.Button(new Rect(card.x+555,card.y+23,150,52),chosen?"Wybrano ✓":key!=null?$"Wybierz [{key}]":"Wybierz",compactButton))ChooseRewardBasket(i);
                GUI.backgroundColor=Color.white;
            }
            if(rewardBaskets.Count==0)GUI.Label(new Rect(36,495,680,50),"Brak części do odzyskania.",small);
            var pager=PortraitRewardPager();
            if(GUI.Button(new Rect(pager.x+370,pager.y,350,44),$"Magazyn {run.VehicleModel.Inventory.Count}/{R3VehicleModel.InventoryCapacity}",compactButton))rewardInventoryOpen=true;
            bool room=run.VehicleModel.Inventory.Count+lootSelection.Count+1<=R3VehicleModel.InventoryCapacity;
            GUI.Label(PortraitRewardSummary(),
                $"Bonus: {(selectedBonus>=0?"wybrany":"wymagany")} • Zestaw: {(selectedBasket>=0?lootSelection.Count+" części":rewardBaskets.Count==0?"brak łupów":"wymagany")}\n"+
                $"Niewybrane części → +{run.VehicleModel.PreviewAward(ledger.PreviewScrap(run.VehicleModel,lootSelection))} Z. Bonus jest nieaktywny do montażu.\n"+
                (!room?"Brak miejsca na nagrody. Otwórz magazyn i rozmontuj część.":rewardClaimFeedback??""),small);
            GUI.enabled=RewardSelectionReady(selectedBonus,offers.Length,selectedBasket,rewardBaskets.Count,run.VehicleModel.Inventory.Count,lootSelection.Count);
            if(GUI.Button(PortraitRewardConfirm(),"Zatwierdź oba wybory [Enter]",button))PortraitRewardConfirm(run);
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
                GUI.Label(new Rect(detail.x,card.y+80,detail.width,21),RewardInventoryRepairText(model,item)+(ammo==null?"":" • "+ammo),small);
                if(GUI.Button(PortraitRewardDismantleButton(i),rewardDismantleConfirmation==item.Id?"Potwierdź rozmontowanie":$"Rozmontuj → {model.PreviewAward(model.DismantleValue(item))} Z",compactButton))
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
            if(!string.IsNullOrEmpty(rewardInventoryFeedback))
                GUI.Label(new Rect(24,865,720,52),rewardInventoryFeedback,small);
            if(GUI.Button(PortraitRewardInventoryBack(),"Wróć do nagród [Esc]",button))
                CloseRewardInventory();
        }

        private void PortraitRewardSelectBonus(int index)=>SelectRewardBonus(index);
        private void PortraitRewardMoveLootPage(int step,int itemCount)
        {lootPage=Mathf.Clamp(lootPage+step,0,Mathf.Max(1,Mathf.CeilToInt(itemCount/6f))-1);}
        private void PortraitRewardToggleLoot(R4SalvageItem item)
        {
            if(lootSelection.Contains(item.Id))lootSelection.Remove(item.Id);
            else if(item.Selectable&&lootSelection.Count<3)lootSelection.Add(item.Id);
        }
        private bool PortraitRewardConfirm(R1RunController run)=>ConfirmSelectedRewards(run);
        private bool PortraitRewardDismantle(R1RunController run,string id)
        {
            if(rewardDismantleConfirmation!=id){rewardDismantleConfirmation=id;rewardInventoryFeedback=null;return false;}
            int before=run.VehicleModel.Scrap;
            bool changed=run.DismantleRewardInventoryItem(id);rewardDismantleConfirmation=null;
            rewardInventoryFeedback=RewardDismantleFeedback(changed,run.VehicleModel.Scrap-before);
            return changed;
        }
        private static string RewardDismantleFeedback(bool changed,int scrap)
            =>changed?$"Rozmontowano część: +{scrap} Z.":"Nie udało się rozmontować części.";
    }
}
