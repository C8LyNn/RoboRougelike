using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RoboRougelike.WeaponIntegration;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        private readonly HashSet<string> lootSelection=new HashSet<string>();
        private IReadOnlyList<R4SalvageBasket> rewardBaskets=System.Array.Empty<R4SalvageBasket>();
        private int selectedBasket=-1;
        private int selectedBonus=-1;
        private R4SalvageLedger displayedLedger;
        private int lootPage,inventoryPage,basketPage;
        private bool inventoryOpen;
        private string dismantleConfirmation;
        private string catalogFeedback;
        private bool rewardInventoryOpen;
        private bool rewardMissedOpen;
        private int rewardMissedPage;
        private string rewardMissedFeedback;
        private bool rewardInventoryDamagedOnly;
        private int rewardInventoryPage;
        private string rewardDismantleConfirmation;
        private string rewardInventoryFeedback;
        private string rewardClaimFeedback;
        private int basketRerollCount;
        private static Rect RewardRerollRect()=>new Rect(455,556,370,35);
        private static Rect SalvageAmmoRect(float x,float y)=>new Rect(x+268,y+77,97,23);
        private static string SalvageAmmoLabel(R3PartState part)
        {
            int capacity=0;string unit=null;
            if(part.Kind==R3PartKind.PointDefense){capacity=40;unit="poc.";}
            else if(part.Kind==R3PartKind.Gun)
            {
                switch(part.Weapon)
                {
                    case R4WeaponKind.Shotgun:capacity=RRLShotgunBinding.MagazineSalvos;unit="salw";break;
                    case R4WeaponKind.Railgun:capacity=RRLRailgunBinding.Magazine;unit="nab.";break;
                    case R4WeaponKind.RocketLauncher:capacity=RRLRocketBinding.Magazine;unit="rak.";break;
                }
            }
            return capacity==0?null:$"{(part.Ammo<0?capacity:part.Ammo)}/{capacity} {unit}";
        }
        private void TrackRewardLedger(R4SalvageLedger ledger)
        {
            if(displayedLedger==ledger)return;
            displayedLedger=ledger;lootSelection.Clear();selectedBonus=-1;lootPage=0;basketPage=0;basketRerollCount=0;
            var run=R1RunController.Instance;
            rewardBaskets=ledger?.CreateBasketOffers((run?.Seed??0)^((run?.Progress?.EncounterIndex??0)*397),run?.Progress?.EncounterIndex??0,run?.VehicleModel?.RewardBasketOfferCount??3)
                ??System.Array.Empty<R4SalvageBasket>();
            selectedBasket=-1;
            rewardInventoryOpen=false;rewardMissedOpen=false;rewardMissedPage=0;rewardMissedFeedback=null;rewardInventoryDamagedOnly=false;rewardInventoryPage=0;rewardDismantleConfirmation=null;rewardInventoryFeedback=null;rewardClaimFeedback=null;
        }
        internal bool CloseRewardInventory()
        {
            if(rewardMissedOpen){rewardMissedOpen=false;rewardMissedFeedback=null;return true;}
            if(!rewardInventoryOpen)return false;
            rewardInventoryOpen=false;rewardDismantleConfirmation=null;rewardInventoryFeedback=null;return true;
        }
        private static R3PartState[] RewardInventoryItems(R3VehicleModel model,bool damagedOnly)=>
            damagedOnly?model.Inventory.Where(item=>item.Health<1).OrderBy(item=>item.Health).ToArray():model.Inventory.ToArray();
        private static int RewardInventoryMissingSlots(int stored,int loot)=>
            (int)System.Math.Min(int.MaxValue,System.Math.Max(0L,(long)stored+loot+1-R3VehicleModel.InventoryCapacity));
        private void ToggleRewardInventoryFilter()
        {rewardInventoryDamagedOnly=!rewardInventoryDamagedOnly;rewardInventoryPage=0;rewardDismantleConfirmation=null;rewardInventoryFeedback=null;}
        private void DrawRewardInventoryFilter(R3VehicleModel model,Rect rect)
        {
            int count=rewardInventoryDamagedOnly?model.Inventory.Count:model.Inventory.Count(item=>item.Health<1);
            if(GUI.Button(rect,rewardInventoryDamagedOnly?$"Pokaż wszystkie ({count})":$"Pokaż uszkodzone ({count})",compactButton))
                ToggleRewardInventoryFilter();
        }
        private void ChooseRewardBasket(int index)
        {
            if(index<0||index>=rewardBaskets.Count)return;
            selectedBasket=index;rewardClaimFeedback=null;lootSelection.Clear();
            foreach(var item in rewardBaskets[index].Items)lootSelection.Add(item.Id);
        }
        private void DrawBasketPager(Rect rect)
        {
            int pages=(int)System.Math.Max(1,((long)rewardBaskets.Count+2)/3);
            basketPage=Mathf.Clamp(basketPage,0,pages-1);
            if(pages<=1)return;
            bool enabled=GUI.enabled;
            GUI.enabled=enabled&&basketPage>0;
            if(GUI.Button(new Rect(rect.x,rect.y,54,rect.height),"‹",compactButton))basketPage--;
            GUI.enabled=enabled;
            GUI.Label(new Rect(rect.x+60,rect.y,rect.width-120,rect.height),$"Zestawy {basketPage+1}/{pages}",small);
            GUI.enabled=enabled&&basketPage<pages-1;
            if(GUI.Button(new Rect(rect.x+rect.width-54,rect.y,54,rect.height),"›",compactButton))basketPage++;
            GUI.enabled=enabled;
        }
        private static bool SameRewardBaskets(IReadOnlyList<R4SalvageBasket> a,IReadOnlyList<R4SalvageBasket> b)
        {
            if(a==null||b==null||a.Count!=b.Count)return false;
            var matched=new bool[b.Count];
            for(int i=0;i<a.Count;i++)
            {
                int found=-1;var ids=a[i].Items.Select(p=>p.Id).OrderBy(id=>id,System.StringComparer.Ordinal).ToArray();
                for(int j=0;j<b.Count;j++)
                    if(!matched[j]&&a[i].Archetype==b[j].Archetype&&ids.SequenceEqual(
                        b[j].Items.Select(p=>p.Id).OrderBy(id=>id,System.StringComparer.Ordinal)))
                    {found=j;break;}
                if(found<0)return false;matched[found]=true;
            }
            return true;
        }
        private bool RerollRewardBaskets(R1RunController run)
        {
            if(run==null||run.VehicleModel==null||run.Definition==null||run.Progress?.State!=R1RunState.Salvage||
                run.Salvage==null||run.Salvage!=displayedLedger||run.Salvage.Closed||rewardBaskets.Count==0)return false;
            int price=Mathf.Max(0,run.Definition.RewardBasketRerollCost);
            if(run.VehicleModel.Scrap<price){rewardClaimFeedback=$"Brakuje {price-run.VehicleModel.Scrap} Z na losowanie.";return false;}
            IReadOnlyList<R4SalvageBasket> next=null;
            int stage=run.Progress.EncounterIndex;
            for(int attempt=1;attempt<=16;attempt++)
            {
                int seed=unchecked((run.Seed^(stage*397))+((basketRerollCount*16+attempt)*7919));
                var candidate=run.Salvage.CreateBasketOffers(seed,stage,run.VehicleModel.RewardBasketOfferCount);
                if(candidate.Count>0&&!SameRewardBaskets(rewardBaskets,candidate)){next=candidate;break;}
            }
            if(next==null){rewardClaimFeedback="Brak innego zestawu do wylosowania.";return false;}
            if(!run.VehicleModel.TrySpendScrap(price))return false;
            basketRerollCount++;rewardBaskets=next;basketPage=0;selectedBasket=-1;lootSelection.Clear();
            rewardClaimFeedback=$"Nowe zestawy • koszt {price} Z. Wybierz zestaw.";return true;
        }
        private void DrawRerollRewardButton(R1RunController run,Rect rect)
        {
            int price=Mathf.Max(0,run.Definition.RewardBasketRerollCost);
            bool wasEnabled=GUI.enabled;
            GUI.enabled=wasEnabled&&rewardBaskets.Count>0&&run.VehicleModel.Scrap>=price;
            string caption=run.VehicleModel.Scrap<price?$"Brakuje {price-run.VehicleModel.Scrap} Z":$"Przelosuj zestawy • {price} Z";
            if(GUI.Button(rect,caption,compactButton))RerollRewardBaskets(run);
            GUI.enabled=wasEnabled;
        }
        private void SelectRewardBonus(int index){selectedBonus=index;rewardClaimFeedback=null;}
        private bool ConfirmSelectedRewards(R1RunController run)
        {
            bool accepted=run.ConfirmRewards(lootSelection,selectedBonus>=0?new[]{selectedBonus}:System.Array.Empty<int>());
            rewardClaimFeedback=accepted?null:"Nie udało się odebrać nagród. Spróbuj ponownie.";
            return accepted;
        }
        private static Color BasketColor(R4SalvageBasket basket)=>basket.Percent>=66?new Color(.48f,.9f,.55f):
            basket.Percent>=33?new Color(.45f,.82f,.95f):Color.white;
        private static string RewardBasketHeading(R4SalvageBasket basket)=>
            $"{R3EnemyProfiles.Name(basket.Archetype)} • Odzysk {basket.Percent}% ({basket.Items.Count}/{basket.AvailableParts})";
        private static string RewardBasketContents(IEnumerable<R4SalvageItem> items)=>string.Join(", ",
            items.Select(item=>item.ToPart()).GroupBy(part=>part.Kind==R3PartKind.BonusModule?
                "bonus:"+CatalogBonusKey(part)+":"+R3VehicleModel.NormalizedTier(part.Tier):"part:"+R4WeaponDefinition.PartName(part))
                .Select(group=>{
                    var part=group.First();string name=R4WeaponDefinition.PartName(part);
                    if(part.Kind==R3PartKind.BonusModule)name+=" • "+BonusInstanceText(part);
                    return group.Count()>1?$"{name} ×{group.Count()}":name;
                }));
        private static string RewardBasketContentsForModel(IEnumerable<R4SalvageItem> items,R3VehicleModel model)=>string.Join(", ",
            items.Select(item=>item.ToPart()).GroupBy(part=>part.Kind==R3PartKind.BonusModule?
                "bonus:"+CatalogBonusKey(part)+":"+R3VehicleModel.NormalizedTier(part.Tier):"part:"+R4WeaponDefinition.PartName(part))
                .Select(group=>{
                    var part=group.First();string name=R4WeaponDefinition.PartName(part);
                    if(part.Kind==R3PartKind.BonusModule)name+=" • "+BonusInstanceText(part);
                    long quantity=group.Sum(p=>(long)model.GrantQuantity(p.Kind,p.ArmorShape));
                    return quantity>1?$"{name} ×{quantity}":name;
                }));
        private int SelectedRewardPartCount(R1RunController run)=>run?.Salvage==null||run.VehicleModel==null?0:
            (int)System.Math.Min(int.MaxValue,run.Salvage.Items.Where(p=>lootSelection.Contains(p.Id))
                .Sum(p=>(long)run.VehicleModel.GrantQuantity(p.Kind,p.ToPart().ArmorShape)));
        private static int RewardShortcutIndex(bool first,bool second,bool third)=>first?0:second?1:third?2:-1;
        private static string RewardConfirmCaption(int basketCount)=>basketCount==0?
            "Odbierz bonus [Enter]":"Zatwierdź oba wybory [Enter]";
        private static string RewardActionCaption(int bonus,int bonusCount,int basket,int basketCount,int inventoryCount,int lootCount)
        {
            if(bonusCount<=0)return "Brak dostępnych bonusów";
            if(bonus<0||bonus>=bonusCount)return "Wybierz bonus";
            if(basketCount>0&&(basket<0||basket>=basketCount))return "Wybierz zestaw";
            int missing=RewardInventoryMissingSlots(inventoryCount,lootCount);
            if(missing>0)
            {
                int last=missing%10,lastTwo=missing%100;
                string word=missing==1?"miejsce":last>=2&&last<=4&&(lastTwo<12||lastTwo>14)?"miejsca":"miejsc";
                return $"Zwolnij {missing} {word} w magazynie";
            }
            return RewardConfirmCaption(basketCount);
        }
        private static string RewardIntro(int basketCount,int scrap)=>basketCount==0?
            $"Wybierz moduł bonusowy. Brak części do odzyskania. +{scrap} Z za walkę.":
            $"Wybierz moduł i jeden zestaw z wraków. +{scrap} Z za walkę.";
        private static bool RewardSelectionReady(int bonus,int bonusCount,int basket,int basketCount,int inventoryCount,int lootCount)
            =>bonus>=0&&bonus<bonusCount&&(basketCount==0||(basket>=0&&basket<basketCount))&&
              (long)inventoryCount+lootCount+1<=R3VehicleModel.InventoryCapacity;
#if ENABLE_INPUT_SYSTEM
        public void ReadRewardInput(Keyboard keyboard)
        {
            var run=R1RunController.Instance;
            if(run?.Progress?.State!=R1RunState.Salvage||run.Transitioning||run.Salvage==null)return;
            TrackRewardLedger(run.Salvage);
            if(rewardInventoryOpen||rewardMissedOpen)return;
            int index=keyboard.digit1Key.wasPressedThisFrame?0:keyboard.digit2Key.wasPressedThisFrame?1:keyboard.digit3Key.wasPressedThisFrame?2:-1;
            if(index>=0&&index<run.Progress.Offers.Count)
            {
                SelectRewardBonus(index);
            }
            int basket=RewardShortcutIndex(keyboard.qKey.wasPressedThisFrame,keyboard.wKey.wasPressedThisFrame,keyboard.eKey.wasPressedThisFrame);
            if(basket>=0&&basketPage*3+basket<rewardBaskets.Count)ChooseRewardBasket(basketPage*3+basket);
            if(keyboard.enterKey.wasPressedThisFrame&&RewardSelectionReady(selectedBonus,run.Progress.Offers.Count,selectedBasket,rewardBaskets.Count,run.VehicleModel.Inventory.Count,SelectedRewardPartCount(run)))ConfirmSelectedRewards(run);
        }
#endif
        private void DrawSalvage(R1RunController run)
        {
            var ledger=run.Salvage;
            TrackRewardLedger(ledger);
            if(rewardMissedOpen){DrawMissedLoot(run,false);return;}
            if(rewardInventoryOpen){DrawRewardInventory(run);return;}
            GUI.Label(new Rect(45,30,1190,60),run.LastActivityWasReactor?"ZWYCIĘSTWO • REAKTOR OBRONIONY":"ZWYCIĘSTWO • WYBÓR NAGRÓD",title);
            GUI.Label(new Rect(45,97,1190,50),
                RewardIntro(rewardBaskets.Count,run.LastScrapReward)+
                (string.IsNullOrEmpty(run.TechnologyMessage)?"":"\n"+run.TechnologyMessage),body);
            GUI.Label(new Rect(45,155,355,32),"BONUS • [1 / 2 / 3]",heading);
            GUI.Label(new Rect(455,155,420,32),"ZESTAW • [Q / W / E]",heading);
            DrawBasketPager(new Rect(875,155,345,32));

            var offers=run.Progress.Offers.ToArray();
            for(int i=0;i<offers.Length;i++)
            {
                var upgrade=offers[i];float y=197+i*116;
                Panel(new Rect(45,y,370,104));
                GUI.Label(new Rect(55,y+8,243,29),upgrade.Title,body);
                GUI.Label(new Rect(55,y+40,243,57),upgrade.Description,small);
                bool chosen=selectedBonus==i;
                GUI.backgroundColor=chosen?new Color(.35f,.85f,.65f):Color.white;
                if(GUI.Button(new Rect(308,y+25,97,50),chosen?"Wybrano ✓":$"Wybierz [{i+1}]",compactButton))
                    SelectRewardBonus(i);
                GUI.enabled=true;GUI.backgroundColor=Color.white;
            }
            if(offers.Length==0)
                GUI.Label(new Rect(55,205,345,80),"Brak propozycji modułów bonusowych.",small);

            for(int i=0;i<3&&basketPage*3+i<rewardBaskets.Count;i++)
            {
                int basketIndex=basketPage*3+i;
                var basket=rewardBaskets[basketIndex];float x=455,y=197+i*116;
                Panel(new Rect(x,y,765,104));
                GUI.contentColor=BasketColor(basket);
                GUI.Label(new Rect(x+10,y+6,520,28),RewardBasketHeading(basket),body);
                GUI.contentColor=Color.white;
                GUI.Label(new Rect(x+10,y+39,535,54),RewardBasketContentsForModel(basket.Items,run.VehicleModel)+"\nWszystkie części sprawne (100% HP)",small);
                bool chosen=selectedBasket==basketIndex;
                GUI.backgroundColor=chosen?new Color(.35f,.85f,.65f):Color.white;
                string key=i<3?"QWE"[i].ToString():null;
                if(GUI.Button(new Rect(x+570,y+25,180,50),chosen?"Wybrano ✓":key!=null?$"Wybierz [{key}]":"Wybierz",compactButton))ChooseRewardBasket(basketIndex);
                GUI.backgroundColor=Color.white;
            }
            if(rewardBaskets.Count==0)GUI.Label(new Rect(465,205,735,60),"Brak części do odzyskania.",small);

            DrawRerollRewardButton(run,RewardRerollRect());
            DrawMissedLootButton(run,new Rect(45,556,370,35));
            if(GUI.Button(new Rect(850,556,375,35),$"Magazyn {run.VehicleModel.Inventory.Count}/{R3VehicleModel.InventoryCapacity} • zwolnij miejsce",compactButton))
                rewardInventoryOpen=true;
            int rewardPartCount=SelectedRewardPartCount(run);
            int missing=RewardInventoryMissingSlots(run.VehicleModel.Inventory.Count,rewardPartCount);
            bool room=missing==0;
            GUI.Label(new Rect(45,603,685,87),
                $"Moduł: {(selectedBonus>=0?"wybrany":"wymagany")}  •  Zestaw: {(selectedBasket>=0?rewardPartCount+" części":rewardBaskets.Count==0?"brak łupów":"wymagany")}\n" +
                $"Niewybrane części → +{run.VehicleModel.PreviewAward(ledger.PreviewScrap(run.VehicleModel,lootSelection))} Z.\n" +
                "Bonusy trafiają do magazynu i pozostają NIEAKTYWNE do montażu.",small);
            GUI.enabled=RewardSelectionReady(selectedBonus,offers.Length,selectedBasket,rewardBaskets.Count,run.VehicleModel.Inventory.Count,rewardPartCount);
            if(GUI.Button(new Rect(805,610,420,68),RewardActionCaption(selectedBonus,offers.Length,selectedBasket,rewardBaskets.Count,run.VehicleModel.Inventory.Count,rewardPartCount),button))
                ConfirmSelectedRewards(run);
            GUI.enabled=true;
            if(!room||!string.IsNullOrEmpty(rewardClaimFeedback))GUI.Label(new Rect(45,685,1180,30),!room?$"Brakuje {missing} {(missing==1?"miejsca":"miejsc")}. Otwórz magazyn, aby je zwolnić.":rewardClaimFeedback,small);
        }
        private void DrawMissedLootButton(R1RunController run,Rect rect)
        {
            int count=run.Salvage.MissedPhysicalItems.Count();
            if(count>0&&GUI.Button(rect,$"Pominięte łupy ({count})",compactButton))
            {rewardMissedOpen=true;rewardMissedPage=0;rewardMissedFeedback=null;}
        }
        private void DrawMissedLoot(R1RunController run,bool portrait)
        {
            var model=run.VehicleModel;var items=run.Salvage.MissedPhysicalItems.ToArray();
            int pages=Mathf.Max(1,Mathf.CeilToInt(items.Length/5f));rewardMissedPage=Mathf.Clamp(rewardMissedPage,0,pages-1);
            float left=portrait?24:45,width=portrait?720:1180,top=portrait?170:180,height=portrait?108:68,step=portrait?120:76;
            GUI.Label(new Rect(left,20,width,45),"POMINIĘTE ŁUPY",title);
            GUI.Label(new Rect(left,72,width,76),$"Odzyskaj część za {model.Economy.MissedLootRecoveryCost} Z • masz {model.Scrap} Z.\nStan HP pozostaje taki jak na polu walki.",small);
            if(items.Length==0)GUI.Label(new Rect(left,top,width,45),"Brak pominiętych części.",body);
            var visible=items.Skip(rewardMissedPage*5).Take(5).ToArray();
            for(int i=0;i<visible.Length;i++)
            {
                var item=visible[i];var part=item.ToPart();int quantity=model.GrantQuantity(part.Kind,part.ArmorShape);
                float y=top+i*step;Panel(new Rect(left,y,width,height));
                float buttonWidth=portrait?190:250;
                GUI.Label(new Rect(left+12,y+5,width-buttonWidth-30,height-10),$"{R4WeaponDefinition.PartName(part)} × {quantity} • HP {part.Health*100:0}%\n{part.Source}",small);
                bool hasRoom=(long)model.Inventory.Count+quantity<=model.AvailableInventoryCapacity;
                int price=model.Economy.MissedLootRecoveryCost;bool affordable=price>0&&model.Scrap>=price;
                bool enabled=GUI.enabled;GUI.enabled=enabled&&hasRoom&&affordable;
                string caption=!hasRoom?"Brak miejsca":!affordable?"Brak środków":$"Odzyskaj • {price} Z";
                if(GUI.Button(new Rect(left+width-buttonWidth-12,y+(height-48)/2,buttonWidth,48),caption,compactButton))
                {
                    bool accepted=run.TryRecoverMissedLoot(item.Id);
                    rewardMissedFeedback=accepted?"Część trafiła do magazynu.":"Nie udało się odzyskać części.";
                    if(accepted)return;
                }
                GUI.enabled=enabled;
            }
            float pagerY=portrait?800:580;bool previous=GUI.enabled;
            GUI.enabled=previous&&rewardMissedPage>0;if(GUI.Button(new Rect(left,pagerY,90,40),"←",compactButton))rewardMissedPage--;
            GUI.enabled=previous;GUI.Label(new Rect(left+105,pagerY+5,220,32),$"Strona {rewardMissedPage+1}/{pages}",small);
            GUI.enabled=previous&&rewardMissedPage<pages-1;if(GUI.Button(new Rect(left+320,pagerY,90,40),"→",compactButton))rewardMissedPage++;
            GUI.enabled=previous;
            if(!string.IsNullOrEmpty(rewardMissedFeedback))GUI.Label(new Rect(left,portrait?865:625,portrait?720:700,35),rewardMissedFeedback,small);
            if(GUI.Button(portrait?new Rect(24,930,720,64):new Rect(805,630,420,60),"Wróć do nagród [Esc]",button))CloseRewardInventory();
        }
        private static Rect RewardInventoryDetailsRect(int index)=>new Rect(60,187+index*76,475,57);
        private static Rect RewardInventoryStatusRect(int index)=>new Rect(550,187+index*76,280,57);
        private static Rect RewardDismantleChoiceRect(int index,bool portrait,bool cancel)
        {
            var area=portrait?PortraitRewardDismantleButton(index):new Rect(860,190+index*76,350,48);
            float confirmWidth=portrait?130:212;float gap=portrait?5:8;
            return cancel?new Rect(area.x+confirmWidth+gap,area.y,area.width-confirmWidth-gap,area.height):
                new Rect(area.x,area.y,confirmWidth,area.height);
        }
        private static string RewardInventoryItemText(R3PartState item) =>
            $"{R4WeaponDefinition.PartName(item)} • HP {item.Health*100:0}%\nŹródło: {item.Source}";
        private static string RewardInventoryRepairText(R3VehicleModel model,R3PartState item)
        {
            if(item.Health>=1f)return "Naprawa: niepotrzebna";
            int price=model.RepairCost(item);
            return price>0?$"{(item.Health<=0?"Odbudowa":"Naprawa")}: {price} Z":"Naprawa: niedostępna";
        }
        private static string RewardInventoryStatusText(R3VehicleModel model,R3PartState item)
        {
            string ammo=SalvageAmmoLabel(item);
            return RewardInventoryRepairText(model,item)+(ammo==null?"":"\n"+ammo);
        }
        private void DrawRewardInventory(R1RunController run)
        {
            var model=run.VehicleModel;
            GUI.Label(new Rect(45,30,1190,60),"MAGAZYN • MIEJSCE NA NAGRODY",title);
            int rewardPartCount=SelectedRewardPartCount(run);
            int missing=RewardInventoryMissingSlots(model.Inventory.Count,rewardPartCount);
            GUI.Label(new Rect(45,99,790,62),$"Zajęte: {model.Inventory.Count}/{R3VehicleModel.InventoryCapacity} • Nagrody: {(long)rewardPartCount+1} miejsc.\n"+
                (missing>0?$"Brakuje {missing} {(missing==1?"miejsca":"miejsc")}. ":"Obecny wybór się mieści. ")+"Rozmontowanie wymaga potwierdzenia.",body);
            DrawRewardInventoryFilter(model,new Rect(860,110,350,48));
            var items=RewardInventoryItems(model,rewardInventoryDamagedOnly);
            int pages=Mathf.Max(1,Mathf.CeilToInt(items.Length/5f));
            rewardInventoryPage=Mathf.Clamp(rewardInventoryPage,0,pages-1);
            var visible=items.Skip(rewardInventoryPage*5).Take(5).ToArray();
            if(items.Length==0)GUI.Label(new Rect(60,190,850,48),rewardInventoryDamagedOnly?"Brak uszkodzonych części.":"Magazyn jest pusty.",body);
            for(int i=0;i<visible.Length;i++)
            {
                var item=visible[i];float y=180+i*76;
                Panel(new Rect(45,y,1180,68));
                GUI.Label(RewardInventoryDetailsRect(i),RewardInventoryItemText(item),small);
                GUI.Label(RewardInventoryStatusRect(i),RewardInventoryStatusText(model,item),small);
                if(rewardDismantleConfirmation==item.Id)
                {
                    if(GUI.Button(RewardDismantleChoiceRect(i,false,false),"Potwierdź",compactButton))
                    {PortraitRewardDismantle(run,item.Id);return;}
                    if(GUI.Button(RewardDismantleChoiceRect(i,false,true),"Anuluj",compactButton))CancelRewardDismantle();
                }
                else if(GUI.Button(new Rect(860,y+10,350,48),$"Rozmontuj → {model.PreviewAward(model.DismantleValue(item))} Z",compactButton))
                    PortraitRewardDismantle(run,item.Id);
            }
            GUI.enabled=rewardInventoryPage>0;
            if(GUI.Button(new Rect(45,580,90,40),"←",compactButton)){rewardInventoryPage--;rewardDismantleConfirmation=null;}
            GUI.Label(new Rect(155,585,250,35),$"Strona {rewardInventoryPage+1}/{pages}",small);
            GUI.enabled=rewardInventoryPage<pages-1;
            if(GUI.Button(new Rect(420,580,90,40),"→",compactButton)){rewardInventoryPage++;rewardDismantleConfirmation=null;}
            GUI.enabled=true;
            if(!string.IsNullOrEmpty(rewardInventoryFeedback))
                GUI.Label(new Rect(530,580,680,40),rewardInventoryFeedback,small);
            if(GUI.Button(new Rect(805,630,420,60),"Wróć do nagród [Esc]",button))
                CloseRewardInventory();
        }
        private static string BonusText(R3PartState item)
        {
            if(item.UpgradeEffect==R1UpgradeEffect.Repair)return "Regeneracja 2,5% HP/s";
            if(item.UpgradeEffect==R1UpgradeEffect.HybridBlastRounds)return $"Amunicja działa: {Mathf.Min(1,item.UpgradeAmount)*100:0.#}% wybuchowa";
            string name=item.UpgradeEffect switch {
                R1UpgradeEffect.Hull=>"Wytrzymałość",R1UpgradeEffect.Damage=>DamageBonusName(item),
                R1UpgradeEffect.Reload=>"Czas przeładowania",R1UpgradeEffect.Mobility=>"Mobilność",
                R1UpgradeEffect.Core=>"Rdzeń",R1UpgradeEffect.EnergyCapacity=>"Pojemność energii",R1UpgradeEffect.EnergyRegeneration=>"Regeneracja energii",R1UpgradeEffect.ShieldDurability=>"Wytrzymałość kopuły",_=>"Bonus"
            };
            if(item.UpgradeOperation!=R1ModifierOperation.Increased)return OtherBonusOperationText(item,name,1);
            return name+" "+(item.UpgradeEffect==R1UpgradeEffect.Reload?"−":"+")+
                (item.UpgradeAmount*100).ToString("0")+"%";
        }
        private static string DamageBonusName(R3PartState item)=>item.UpgradeScope switch {
            R1BonusScope.BasicCannon=>"Obrażenia działa podstawowego",R1BonusScope.Drones=>"Obrażenia dronów",
            R1BonusScope.Turrets=>"Obrażenia wieżyczek",R1BonusScope.Traps=>"Obrażenia pułapek",
            R1BonusScope.BlastDamage=>"Obrażenia wybuchowe",R1BonusScope.PiercingDamage=>"Obrażenia przebijające",
            R1BonusScope.KineticDamage=>"Obrażenia kinetyczne",_=>"Obrażenia"};
        private static string OtherBonusOperationText(R3PartState item,string name,int units)
        {
            bool inverse=item.UpgradeEffect==R1UpgradeEffect.Reload;
            if(item.UpgradeOperation==R1ModifierOperation.Reduced)
                return $"{name} {(inverse?"+":"−")}{item.UpgradeAmount*units*100:0.#}%";
            bool more=item.UpgradeOperation==R1ModifierOperation.More;
            double sign=(more?1d:-1d)*(inverse?-1d:1d);
            double factor=System.Math.Pow(System.Math.Max(0d,1d+sign*item.UpgradeAmount),units);
            return $"{name} ×{factor:0.##} • osobny mnożnik";
        }
        private static string BonusInstanceText(R3PartState item)
        {
            int units=R3VehicleModel.TierEffectUnits(item.Tier);
            if(item.UpgradeEffect==R1UpgradeEffect.Repair)
                return $"Regeneracja {units*.025f*100:0.#}% HP/s";
            if(item.UpgradeEffect==R1UpgradeEffect.HybridBlastRounds)
                return $"Działo: do {Mathf.Min(1,item.UpgradeAmount*units)*100:0.#}% składowej wybuchowej";
            string name;float percent;bool reduction=false;
            switch(item.UpgradeEffect)
            {
                case R1UpgradeEffect.Hull:name="Wytrzymałość";percent=item.UpgradeAmount*units*100;break;
                case R1UpgradeEffect.Damage:name=DamageBonusName(item);percent=item.UpgradeAmount*units*100;break;
                case R1UpgradeEffect.Reload:name="Czas przeładowania";percent=Mathf.Min(100,item.UpgradeAmount*units*100);reduction=true;break;
                case R1UpgradeEffect.Mobility:name="Mobilność";percent=item.UpgradeAmount*units*100;break;
                case R1UpgradeEffect.Core:name="Rdzeń";percent=item.UpgradeAmount*units*100;break;
                case R1UpgradeEffect.EnergyCapacity:name="Pojemność energii";percent=item.UpgradeAmount*units*100;break;
                case R1UpgradeEffect.EnergyRegeneration:name="Regeneracja energii";percent=item.UpgradeAmount*units*100;break;
                case R1UpgradeEffect.ShieldDurability:name="Wytrzymałość kopuły";percent=item.UpgradeAmount*units*100;break;
                default:return "Nieznany bonus";
            }
            if(item.UpgradeOperation!=R1ModifierOperation.Increased)return OtherBonusOperationText(item,name,units);
            return $"{name} do {(reduction?"−":"+")}{percent:0.#}%";
        }
        private void DrawInventory(R1RunController run)
        {
            var model=run.VehicleModel;var garage=run.Garage;
            if(!catalogGroup.HasValue)
            {GUI.Label(new Rect(970,85,280,120),"Wybierz stos części z listy po lewej. Zobaczysz tu każdy egzemplarz i jego HP.",small);return;}
            var kind=catalogGroup.Value;var weapon=catalogWeapon;
            var items=CatalogItemsForGroup(model,kind,weapon,catalogBonusId);
            string name=kind==R3PartKind.Armor?R4WeaponDefinition.ArmorName(CatalogArmorShape(catalogBonusId)):
                kind==R3PartKind.Wheel?R4WeaponDefinition.WheelName(CatalogWheelStyle(catalogBonusId)):
                kind==R3PartKind.BonusModule&&items.Length>0?CatalogBonusGroupName(items[0]):R4WeaponDefinition.PartName(kind,weapon);
            GUI.Label(new Rect(970,70,280,48),$"{name}  × {items.Length}\nHP — montaż lub naprawa • Akcje — szczegóły",small);
            stackScroll=GUI.BeginScrollView(new Rect(970,118,280,168),stackScroll,new Rect(0,0,255,Mathf.Max(168,items.Length*40)));
            for(int i=0;i<items.Length;i++)
            {
                var item=items[i];int itemRepair=model.RepairCost(item);
                bool wreck=item.Health<=0;
                GUI.backgroundColor=garage.SelectedInventoryId==item.Id?new Color(.3f,.9f,.9f):Color.white;
                GUI.enabled=!wreck||R3VehicleModel.ValidEconomicPart(item)&&(model.CreativeMode||itemRepair>0&&model.Scrap>=itemRepair);
                if(GUI.Button(new Rect(0,i*40,100,36),CatalogMountCaption(item,model.CreativeMode,itemRepair,model.Scrap,false),compactButton))
                {if(wreck)RepairAndSelectCatalogItem(run,item.Id);else SelectCatalogItem(run,item.Id);}
                GUI.enabled=true;
                GUI.backgroundColor=Color.white;
                if(GUI.Button(new Rect(104,i*40,55,36),"Akcje",compactButton))SelectCatalogDetails(run,item.Id);
                GUI.enabled=(itemRepair>0||(model.CreativeMode&&item.Health<1))&&model.Scrap>=itemRepair;
                if(GUI.Button(new Rect(163,i*40,87,36),model.CreativeMode&&item.Health<1?"Napraw 0 Z":GarageRepairCaption(item,itemRepair,model.Scrap),compactButton))
                    RepairCatalogItem(run,item.Id);
                GUI.enabled=true;
            }
            if(items.Length==0)GUI.Label(new Rect(0,5,255,100),CatalogEmptyStackHint(model.CreativeMode,kind,run.CanBuyTechnology(kind,weapon)),small);
            GUI.EndScrollView();GUI.backgroundColor=Color.white;
            DrawCatalogBulkRepair(run,items,new Rect(970,295,280,36));
            int price=model.Price(kind,weapon);
            bool unlocked=run.CanBuyTechnology(kind,weapon);
            GUI.enabled=unlocked&&(model.CreativeMode||kind!=R3PartKind.BonusModule)&&model.Scrap>=price&&(long)model.Inventory.Count+CatalogPurchaseQuantity(model,kind)<=model.AvailableInventoryCapacity;
            if(GUI.Button(new Rect(970,340,136,40),CatalogPurchaseCaption(model,kind,unlocked,price),compactButton))
            {
                dismantleConfirmation=null;
                if(AddSelectedCatalogPart(run,kind,weapon))
                {var purchased=model.Inventory.Last();garage.ChooseInventory(purchased.Id);DismissCatalog();garage.Message=run.CreativeWorkshop?"Dodano część za darmo. Kliknij pojazd, aby ją zamontować.":$"Kupiono {CatalogPurchaseQuantity(model,kind)} szt. Kliknij pojazd, aby zamontować część.";}
                else catalogFeedback=garage.Message=PurchaseFailure(run,kind,weapon);
            }
            DrawBuyFive(run,kind,weapon,new Rect(1112,340,138,40));
            GUI.enabled=true;
            var selected=items.FirstOrDefault(p=>p.Id==garage.SelectedInventoryId);
            if(selected==null)
            {if(items.Length>0)GUI.Label(new Rect(970,400,280,100),"Wybierz egzemplarz z listy. Zielony podgląd oznacza poprawne miejsce montażu.",small);return;}
            GUI.Label(new Rect(970,395,280,70),$"{R4WeaponDefinition.PartName(selected)} • HP {selected.Health*100:0}%\n"+
                (selected.Kind==R3PartKind.BonusModule?BonusInstanceText(selected)+"\n":"")+
                (selected.Health<=0?"Napraw w wierszu przed montażem.":"Kliknij HP w wierszu, aby montować."),small);
            if(dismantleConfirmation==selected.Id)
            {
                if(GUI.Button(new Rect(970,480,136,42),"Potwierdź",compactButton))
                {if(DismantleCatalogItem(run,selected.Id))return;}
                if(GUI.Button(new Rect(1112,480,138,42),"Anuluj",compactButton))CancelCatalogDismantle();
            }
            else if(GUI.Button(new Rect(970,480,280,42),$"Rozmontuj → {model.PreviewAward(model.DismantleValue(selected))} Z",compactButton))
                DismantleCatalogItem(run,selected.Id);
            DrawFusionButton(run,selected,new Rect(970,535,280,42));
            GUI.Label(new Rect(970,590,280,52),"Po wyborze do montażu katalog się zamknie i pojawi się widmo.",small);
        }
        private void SelectCatalogDetails(R1RunController run,string id)
        {
            var item=run.VehicleModel.Inventory.FirstOrDefault(p=>p.Id==id);
            if(item==null)return;
            run.Garage.ChooseInventory(id);dismantleConfirmation=null;catalogFeedback=null;
        }
        private void SelectCatalogItem(R1RunController run,string id)
        {
            var item=run.VehicleModel.Inventory.FirstOrDefault(p=>p.Id==id);
            if(item==null)return;
            SelectCatalogDetails(run,id);
            if(item.Health<=0){catalogFeedback=run.Garage.Message="Zniszczoną część napraw przed montażem.";return;}
            DismissCatalog();
        }
        private void RepairAndSelectCatalogItem(R1RunController run,string id)
        {
            var item=run.VehicleModel.Inventory.FirstOrDefault(p=>p.Id==id);
            if(item==null)return;
            if(item.Health>0){SelectCatalogItem(run,id);return;}
            int price=run.VehicleModel.RepairCost(item);
            if(!run.GarageRepairInventory(id))
            {
                catalogFeedback=price>run.VehicleModel.Scrap?$"Brakuje {price-run.VehicleModel.Scrap} Z na naprawę.":"Nie udało się naprawić części.";
                return;
            }
            SelectCatalogItem(run,id);
            run.Garage.Message=run.CreativeWorkshop?"Naprawiono za darmo. Wybierz miejsce montażu.":$"Naprawiono za {price} Z. Wybierz miejsce montażu.";
        }
        private void DrawFusionButton(R1RunController run,R3PartState selected,Rect rect)
        {
            if(selected==null||!R3VehicleModel.SupportsFusion(selected.Kind,selected.Weapon))return;
            int tier=R3VehicleModel.NormalizedTier(selected.Tier);
            var others=run.VehicleModel.Inventory.Where(p=>p.Id!=selected.Id&&
                R3VehicleModel.SameFusionType(selected,p)).OrderBy(p=>p.Health).Take(2).ToArray();
            var group=new[]{selected}.Concat(others).ToArray();
            bool available=tier<3&&group.Length==3;
            string label=tier>=3?"Maksymalny poziom T3":!available?$"Połącz T{tier} — potrzeba 3":
                FusionPreviewLabel(tier,group);
            GUI.enabled=available;
            if(GUI.Button(rect,label,compactButton))
            {dismantleConfirmation=null;catalogFeedback=run.GarageFuse(group.Select(p=>p.Id))?run.Garage.Message:"Nie udało się połączyć części.";}
            GUI.enabled=true;
        }
        private static string FusionPreviewLabel(int tier,R3PartState[] group)
        {
            var inputs=string.Join(" / ",group.Select(p=>(p.Health*100).ToString("0")+"%"));
            return $"3×T{tier} → T{tier+1} • HP {group.Average(p=>p.Health)*100:0}%\nSkładniki: {inputs}";
        }
        private void RepairCatalogItem(R1RunController run,string id)
        {
            dismantleConfirmation=null;
            var item=run.VehicleModel.Inventory.FirstOrDefault(p=>p.Id==id);
            int quote=run.VehicleModel.RepairCost(item);
            catalogFeedback=run.GarageRepairInventory(id)?$"Naprawiono część • koszt {quote} Z.":"Nie udało się naprawić części.";
        }
        private static R3PartState[] CatalogRepairPlan(R3VehicleModel model,R3PartState[] items,out int eligible,out int total)
        {
            var candidates=items.Where(item=>item!=null).Select(item=>new{item,cost=model.RepairCost(item)})
                .Where(entry=>entry.cost>0||(model.CreativeMode&&entry.item.Health<1&&R3VehicleModel.ValidEconomicPart(entry.item))).OrderBy(entry=>entry.cost).ThenBy(entry=>entry.item.Id,System.StringComparer.Ordinal).ToArray();
            eligible=candidates.Length;total=0;
            var selected=new System.Collections.Generic.List<R3PartState>();
            foreach(var entry in candidates)
            {
                if(entry.cost>model.Scrap-total)break;
                selected.Add(entry.item);total+=entry.cost;
            }
            return selected.ToArray();
        }
        private static string CatalogRepairLabel(R3VehicleModel model,R3PartState[] items,int planned,int eligible,int total)
        {
            if(eligible==0)return "Brak części do naprawy";
            if(planned==0)
            {
                int cheapest=items.Where(item=>item!=null).Select(model.RepairCost).Where(cost=>cost>0).DefaultIfEmpty(0).Min();
                return cheapest>model.Scrap?$"Brakuje {cheapest-model.Scrap} Z":"Naprawa niedostępna";
            }
            if(model.CreativeMode)return $"Napraw {planned} • za darmo";
            return $"Napraw {planned}/{eligible} • {total} Z"+(planned<eligible?" • najtaniej":"");
        }
        private static string CatalogMountCaption(R3PartState item,bool creative,int repair,int scrap,bool portrait)
        {
            if(item==null)return "Brak części";
            if(item.Health>0)return portrait?"Wybierz\ndo montażu":CatalogItemCaption(item,true);
            if(!R3VehicleModel.ValidEconomicPart(item)||(!creative&&repair<=0))return "Naprawa niedostępna";
            if(!creative&&scrap<repair)return $"Brakuje {repair-scrap} Z";
            return $"Napraw +\nmontuj: {(creative?0:repair)} Z";
        }
        private static string QuickStackRepairCaption(R3PartState[] items,int planned,int eligible,int total)
        {
            if(!items.Any(item=>item!=null))return "Stos: pusty";
            if(eligible==0)return items.Any(item=>item!=null&&item.Health<1)?"Stos: niedostępny":"Stos: sprawny";
            return planned==0?"Stos: brak środków":$"Stos {planned}/{eligible} • {total} Z";
        }
        private void DrawCatalogBulkRepair(R1RunController run,R3PartState[] items,Rect rect,bool garageQuick=false,bool compact=false)
        {
            var plan=CatalogRepairPlan(run.VehicleModel,items,out int eligible,out int total);
            bool previouslyEnabled=GUI.enabled;GUI.enabled=previouslyEnabled&&plan.Length>0;
            string label=CatalogRepairLabel(run.VehicleModel,items,plan.Length,eligible,total);
            if(compact)label=QuickStackRepairCaption(items,plan.Length,eligible,total);
            else if(garageQuick)label="Magazyn: "+label;
            if(GUI.Button(rect,label,compactButton))RepairCatalogStack(run,items);
            GUI.enabled=previouslyEnabled;
        }
        private void RepairCatalogStack(R1RunController run,R3PartState[] items)
        {
            dismantleConfirmation=null;
            var plan=CatalogRepairPlan(run.VehicleModel,items,out _,out _);
            int before=run.VehicleModel.Scrap;
            int repaired=run.GarageRepairInventoryBatch(plan.Select(item=>item.Id));
            int spent=before-run.VehicleModel.Scrap;
            catalogFeedback=repaired>0?$"Naprawiono {repaired} części • koszt {spent} Z.":"Nie udało się naprawić części.";
            if(!catalogOpen&&run.Garage!=null)
            {
                if(repaired>0&&catalogGroup.HasValue&&run.Garage.Tool==R41GarageTool.Mount&&
                   string.IsNullOrEmpty(run.Garage.SelectedInventoryId))
                {
                    var ready=plan.FirstOrDefault(part=>part.Health>=1&&
                        InCatalogGroup(part,catalogGroup.Value,catalogWeapon,catalogBonusId)&&
                        run.VehicleModel.Inventory.Any(owned=>owned.Id==part.Id));
                    if(ready!=null)
                    {run.Garage.ChooseInventory(ready.Id);catalogFeedback+=" Wybierz miejsce montażu.";}
                }
                run.Garage.ShowActionMessage(catalogFeedback);
            }
        }
        private void CancelCatalogDismantle()
        {dismantleConfirmation=null;catalogFeedback="Anulowano rozmontowanie.";}
        private bool DismantleCatalogItem(R1RunController run,string id)
        {
            if(dismantleConfirmation!=id)
            {
                var item=run.VehicleModel.Inventory.FirstOrDefault(part=>part.Id==id);
                if(item==null){catalogFeedback="Nie znaleziono części w magazynie.";return false;}
                dismantleConfirmation=id;
                catalogFeedback=$"{R4WeaponDefinition.PartName(item)} → {run.VehicleModel.PreviewAward(run.VehicleModel.DismantleValue(item))} Z. Kliknij ponownie, aby rozmontować.";
                return false;
            }
            dismantleConfirmation=null;
            int before=run.VehicleModel.Scrap;
            bool done=run.GarageDismantle(id);
            int credited=run.VehicleModel.Scrap-before;
            catalogFeedback=done?$"Rozmontowano część • +{credited} Z.":"Nie udało się rozmontować części.";
            return done;
        }
    }
}
