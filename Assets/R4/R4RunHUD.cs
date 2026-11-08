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
        private int lootPage,inventoryPage;
        private bool inventoryOpen;
        private string dismantleConfirmation;
        private string catalogFeedback;
        private bool rewardInventoryOpen;
        private int rewardInventoryPage;
        private string rewardDismantleConfirmation;
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
            displayedLedger=ledger;lootSelection.Clear();selectedBonus=-1;lootPage=0;
            var run=R1RunController.Instance;
            rewardBaskets=ledger?.CreateBasketOffers((run?.Seed??0)^((run?.Progress?.EncounterIndex??0)*397),run?.Progress?.EncounterIndex??0)
                ??System.Array.Empty<R4SalvageBasket>();
            selectedBasket=-1;
            rewardInventoryOpen=false;rewardInventoryPage=0;rewardDismantleConfirmation=null;
        }
        private void ChooseRewardBasket(int index)
        {
            if(index<0||index>=rewardBaskets.Count)return;
            selectedBasket=index;lootSelection.Clear();
            foreach(var item in rewardBaskets[index].Items)lootSelection.Add(item.Id);
        }
        private static Color BasketColor(R4SalvageBasket basket)=>basket.Percent>=66?new Color(.48f,.9f,.55f):
            basket.Percent>=33?new Color(.45f,.82f,.95f):Color.white;
#if ENABLE_INPUT_SYSTEM
        public void ReadRewardInput(Keyboard keyboard)
        {
            var run=R1RunController.Instance;
            if(run?.Progress?.State!=R1RunState.Salvage||run.Transitioning||run.Salvage==null)return;
            TrackRewardLedger(run.Salvage);
            if(rewardInventoryOpen)return;
            int index=keyboard.digit1Key.wasPressedThisFrame?0:keyboard.digit2Key.wasPressedThisFrame?1:keyboard.digit3Key.wasPressedThisFrame?2:-1;
            if(index>=0&&index<run.Progress.Offers.Count)
            {
                selectedBonus=index;
            }
            if(keyboard.enterKey.wasPressedThisFrame&&(selectedBasket>=0||rewardBaskets.Count==0))run.ConfirmRewards(lootSelection,selectedBonus>=0?new[]{selectedBonus}:System.Array.Empty<int>());
        }
#endif
        private void DrawSalvage(R1RunController run)
        {
            var ledger=run.Salvage;
            TrackRewardLedger(ledger);
            if(rewardInventoryOpen){DrawRewardInventory(run);return;}
            GUI.Label(new Rect(45,30,1190,60),run.LastActivityWasReactor?"ZWYCIĘSTWO • REAKTOR OBRONIONY":"ZWYCIĘSTWO • WYBÓR NAGRÓD",title);
            GUI.Label(new Rect(45,97,1190,50),
                $"Wybierz moduł i jeden zestaw z wraków. +{run.LastScrapReward} Z za walkę."+
                (string.IsNullOrEmpty(run.TechnologyMessage)?"":"\n"+run.TechnologyMessage),body);
            GUI.Label(new Rect(45,155,355,32),"MODUŁ BONUSOWY • 1 Z 3",heading);
            GUI.Label(new Rect(455,155,765,32),"ODZYSK • WYBIERZ 1 ZESTAW",heading);

            var offers=run.Progress.Offers.ToArray();
            for(int i=0;i<offers.Length;i++)
            {
                var upgrade=offers[i];float y=197+i*116;
                Panel(new Rect(45,y,370,104));
                GUI.Label(new Rect(55,y+8,243,29),upgrade.Title,body);
                GUI.Label(new Rect(55,y+40,243,57),upgrade.Description,small);
                bool chosen=selectedBonus==i;
                GUI.backgroundColor=chosen?new Color(.35f,.85f,.65f):Color.white;
                if(GUI.Button(new Rect(308,y+25,97,50),chosen?"Wybrano ✓":"Wybierz",compactButton))
                    selectedBonus=i;
                GUI.enabled=true;GUI.backgroundColor=Color.white;
            }
            if(offers.Length==0)
                GUI.Label(new Rect(55,205,345,80),"Brak propozycji modułów bonusowych.",small);

            for(int i=0;i<rewardBaskets.Count;i++)
            {
                var basket=rewardBaskets[i];float x=455,y=197+i*116;
                Panel(new Rect(x,y,765,104));
                GUI.contentColor=BasketColor(basket);
                GUI.Label(new Rect(x+10,y+6,520,28),$"{R3EnemyProfiles.Name(basket.Archetype)} • {basket.Items.Count}/{basket.AvailableParts} części",body);
                GUI.contentColor=Color.white;
                GUI.Label(new Rect(x+10,y+39,535,54),string.Join(", ",basket.Items.Select(p=>R4WeaponDefinition.PartName(p.ToPart())))+"\nWszystkie części sprawne (100% HP)",small);
                bool chosen=selectedBasket==i;
                GUI.backgroundColor=chosen?new Color(.35f,.85f,.65f):Color.white;
                if(GUI.Button(new Rect(x+570,y+25,180,50),chosen?"Wybrano ✓":"Wybierz",compactButton))ChooseRewardBasket(i);
                GUI.backgroundColor=Color.white;
            }
            if(rewardBaskets.Count==0)GUI.Label(new Rect(465,205,735,60),"Brak części do odzyskania.",small);

            if(GUI.Button(new Rect(850,556,375,35),$"Magazyn {run.VehicleModel.Inventory.Count}/{R3VehicleModel.InventoryCapacity} • zwolnij miejsce",compactButton))
                rewardInventoryOpen=true;
            bool room=run.VehicleModel.Inventory.Count+lootSelection.Count+1<=R3VehicleModel.InventoryCapacity;
            GUI.Label(new Rect(45,603,685,87),
                $"Moduł: {(selectedBonus>=0?"wybrany":"wymagany")}  •  Zestaw: {(selectedBasket>=0?lootSelection.Count+" części":rewardBaskets.Count==0?"brak łupów":"wymagany")}\n" +
                $"Niewybrane części → +{ledger.PreviewScrap(run.VehicleModel,lootSelection)} Z.\n" +
                "Bonusy trafiają do magazynu i pozostają NIEAKTYWNE do montażu.",small);
            GUI.enabled=selectedBonus>=0&&room&&(selectedBasket>=0||rewardBaskets.Count==0);
            if(GUI.Button(new Rect(805,610,420,68),"Zatwierdź oba wybory i kontynuuj",button))
                run.ConfirmRewards(lootSelection,selectedBonus>=0?new[]{selectedBonus}:System.Array.Empty<int>());
            GUI.enabled=true;
            if(!room)GUI.Label(new Rect(45,685,1180,30),"Brak miejsca na wybrane nagrody. Otwórz magazyn i rozmontuj posiadane części, aby zwolnić miejsce.",small);
        }
        private static Rect RewardInventoryDetailsRect(int index)=>new Rect(60,187+index*76,475,57);
        private static Rect RewardInventoryStatusRect(int index)=>new Rect(550,187+index*76,280,57);
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
            GUI.Label(new Rect(45,99,1180,62),$"Zajęte: {model.Inventory.Count}/{R3VehicleModel.InventoryCapacity}. Nagrody wymagają {lootSelection.Count+1} wolnych miejsc.\nRozmontowanie usuwa wybraną posiadaną część i wypłaca złom. Wymaga drugiego kliknięcia.",body);
            var items=model.Inventory.ToArray();
            int pages=Mathf.Max(1,Mathf.CeilToInt(items.Length/5f));
            rewardInventoryPage=Mathf.Clamp(rewardInventoryPage,0,pages-1);
            var visible=items.Skip(rewardInventoryPage*5).Take(5).ToArray();
            for(int i=0;i<visible.Length;i++)
            {
                var item=visible[i];float y=180+i*76;
                Panel(new Rect(45,y,1180,68));
                GUI.Label(RewardInventoryDetailsRect(i),RewardInventoryItemText(item),small);
                GUI.Label(RewardInventoryStatusRect(i),RewardInventoryStatusText(model,item),small);
                if(GUI.Button(new Rect(860,y+10,350,48),rewardDismantleConfirmation==item.Id?"Potwierdź rozmontowanie":$"Rozmontuj → {model.DismantleValue(item)} Z",compactButton))
                {
                    if(rewardDismantleConfirmation==item.Id){run.DismantleRewardInventoryItem(item.Id);rewardDismantleConfirmation=null;return;}
                    rewardDismantleConfirmation=item.Id;
                }
            }
            GUI.enabled=rewardInventoryPage>0;
            if(GUI.Button(new Rect(45,580,90,40),"←",compactButton)){rewardInventoryPage--;rewardDismantleConfirmation=null;}
            GUI.Label(new Rect(155,585,250,35),$"Strona {rewardInventoryPage+1}/{pages}",small);
            GUI.enabled=rewardInventoryPage<pages-1;
            if(GUI.Button(new Rect(420,580,90,40),"→",compactButton)){rewardInventoryPage++;rewardDismantleConfirmation=null;}
            GUI.enabled=true;
            if(GUI.Button(new Rect(805,630,420,60),"Wróć do wybranych nagród",button))
            {rewardInventoryOpen=false;rewardDismantleConfirmation=null;}
        }
        private static string BonusText(R3PartState item)
        {
            if(item.UpgradeEffect==R1UpgradeEffect.Repair)return "Regeneracja 2,5% HP/s";
            string name=item.UpgradeEffect switch {
                R1UpgradeEffect.Hull=>"Wytrzymałość",R1UpgradeEffect.Damage=>"Obrażenia",
                R1UpgradeEffect.Reload=>"Czas przeładowania",R1UpgradeEffect.Mobility=>"Mobilność",
                R1UpgradeEffect.Core=>"Rdzeń",_=>"Bonus"
            };
            return name+" "+(item.UpgradeEffect==R1UpgradeEffect.Reload?"−":"+")+
                (item.UpgradeAmount*100).ToString("0")+"%";
        }
        private void DrawInventory(R1RunController run)
        {
            var model=run.VehicleModel;var garage=run.Garage;
            if(!catalogGroup.HasValue)
            {GUI.Label(new Rect(970,85,280,120),"Wybierz stos części z listy po lewej. Zobaczysz tu każdy egzemplarz i jego HP.",small);return;}
            var kind=catalogGroup.Value;var weapon=catalogWeapon;
            var items=model.Inventory.Where(p=>InCatalogGroup(p,kind,weapon,catalogBonusId)).ToArray();
            string name=kind==R3PartKind.Armor?R4WeaponDefinition.ArmorName(CatalogArmorShape(catalogBonusId)):
                kind==R3PartKind.Wheel?R4WeaponDefinition.WheelName(CatalogWheelStyle(catalogBonusId)):
                kind==R3PartKind.BonusModule&&items.Length>0?CatalogBonusGroupName(items[0]):R4WeaponDefinition.PartName(kind,weapon);
            GUI.Label(new Rect(970,70,280,48),$"{name}  × {items.Length}\nHP — montaż • Akcje — łączenie i demontaż",small);
            stackScroll=GUI.BeginScrollView(new Rect(970,118,280,210),stackScroll,new Rect(0,0,255,Mathf.Max(210,items.Length*40)));
            for(int i=0;i<items.Length;i++)
            {
                var item=items[i];GUI.backgroundColor=garage.SelectedInventoryId==item.Id?new Color(.3f,.9f,.9f):Color.white;
                if(GUI.Button(new Rect(0,i*40,100,36),$"T{R3VehicleModel.NormalizedTier(item.Tier)} • {item.Health*100:0}%",compactButton))
                    SelectCatalogItem(run,item.Id);
                GUI.backgroundColor=Color.white;
                if(GUI.Button(new Rect(104,i*40,55,36),"Akcje",compactButton))SelectCatalogDetails(run,item.Id);
                int itemRepair=model.RepairCost(item);GUI.enabled=itemRepair>0&&model.Scrap>=itemRepair;
                if(GUI.Button(new Rect(163,i*40,87,36),GarageRepairCaption(item,itemRepair,model.Scrap),compactButton))
                    RepairCatalogItem(run,item.Id);
                GUI.enabled=true;
            }
            GUI.EndScrollView();GUI.backgroundColor=Color.white;
            int price=model.Price(kind,weapon);
            bool unlocked=run.CanBuyTechnology(kind,weapon);
            GUI.enabled=unlocked&&kind!=R3PartKind.BonusModule&&model.Scrap>=price&&model.Inventory.Count<R3VehicleModel.InventoryCapacity;
            if(GUI.Button(new Rect(970,340,280,40),GaragePurchaseCaption(kind,unlocked,price,model.Scrap,model.Inventory.Count>=R3VehicleModel.InventoryCapacity,true),compactButton))
            {
                dismantleConfirmation=null;
                if(run.GaragePurchase(kind,weapon,kind==R3PartKind.Wheel?CatalogWheelStyle(catalogBonusId):R3WheelStyle.Offroad,
                    kind==R3PartKind.Armor?CatalogArmorShape(catalogBonusId):R3ArmorShape.Cube))
                {var purchased=model.Inventory.Last();garage.ChooseInventory(purchased.Id);DismissCatalog();garage.Message="Kupiono część. Kliknij pojazd, aby ją zamontować.";}
                else garage.Message="Nie udało się kupić części.";
            }
            GUI.enabled=true;
            var selected=items.FirstOrDefault(p=>p.Id==garage.SelectedInventoryId);
            if(selected==null)
            {GUI.Label(new Rect(970,400,280,100),"Wybierz egzemplarz z listy. Zielony podgląd oznacza poprawne miejsce montażu.",small);return;}
            GUI.Label(new Rect(970,395,280,70),$"{R4WeaponDefinition.PartName(selected)} • HP {selected.Health*100:0}%\n"+
                (selected.Health<=0?"Napraw w wierszu przed montażem.":"Kliknij HP w wierszu, aby montować."),small);
            if(GUI.Button(new Rect(970,480,280,42),dismantleConfirmation==selected.Id?"Potwierdź rozmontowanie":$"Rozmontuj → {model.DismantleValue(selected)} Z",compactButton))
            {if(DismantleCatalogItem(run,selected.Id))return;}
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
            if(item.Health<=0){run.Garage.Message="Zniszczoną część napraw przed montażem.";return;}
            DismissCatalog();
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
                $"3×T{tier} → T{tier+1} • HP "+string.Join("/",group.Select(p=>(p.Health*100).ToString("0")))+"%";
            GUI.enabled=available;
            if(GUI.Button(rect,label,compactButton))
            {dismantleConfirmation=null;catalogFeedback=run.GarageFuse(group.Select(p=>p.Id))?run.Garage.Message:"Nie udało się połączyć części.";}
            GUI.enabled=true;
        }
        private void RepairCatalogItem(R1RunController run,string id)
        {
            dismantleConfirmation=null;
            var item=run.VehicleModel.Inventory.FirstOrDefault(p=>p.Id==id);
            int quote=run.VehicleModel.RepairCost(item);
            catalogFeedback=run.GarageRepairInventory(id)?$"Naprawiono część • koszt {quote} Z.":"Nie udało się naprawić części.";
        }
        private bool DismantleCatalogItem(R1RunController run,string id)
        {
            if(dismantleConfirmation!=id){dismantleConfirmation=id;catalogFeedback="Kliknij ponownie, aby rozmontować wybraną część.";return false;}
            dismantleConfirmation=null;
            var item=run.VehicleModel.Inventory.FirstOrDefault(p=>p.Id==id);
            int quote=run.VehicleModel.DismantleValue(item);
            bool done=run.GarageDismantle(id);
            catalogFeedback=done?$"Rozmontowano część • +{quote} Z.":"Nie udało się rozmontować części.";
            return done;
        }
    }
}
