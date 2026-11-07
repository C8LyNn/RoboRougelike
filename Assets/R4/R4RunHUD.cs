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
        private int selectedBonus=-1;
        private R4SalvageLedger displayedLedger;
        private int lootPage,inventoryPage;
        private bool inventoryOpen;
        private string dismantleConfirmation;
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
            rewardInventoryOpen=false;rewardInventoryPage=0;rewardDismantleConfirmation=null;
        }
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
            if(keyboard.enterKey.wasPressedThisFrame)run.ConfirmRewards(lootSelection,selectedBonus>=0?new[]{selectedBonus}:System.Array.Empty<int>());
        }
#endif
        private void DrawSalvage(R1RunController run)
        {
            var ledger=run.Salvage;
            TrackRewardLedger(ledger);
            if(rewardInventoryOpen){DrawRewardInventory(run);return;}
            const int partsLimit=3;
            GUI.Label(new Rect(45,30,1190,60),run.LastActivityWasReactor?"ZWYCIĘSTWO • REAKTOR OBRONIONY":"ZWYCIĘSTWO • WYBÓR NAGRÓD",title);
            GUI.Label(new Rect(45,97,1190,50),
                $"DWA NIEZALEŻNE WYBORY: 1 z 3 modułów bonusowych + do 3 części z wraków. +{run.LastScrapReward} Z za walkę.",body);
            GUI.Label(new Rect(45,155,355,32),"MODUŁ BONUSOWY • 1 Z 3",heading);
            GUI.Label(new Rect(455,155,765,32),"CZĘŚCI PRZECIWNIKÓW • DO 3",heading);

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

            var items=ledger.Items.Where(item=>item.Selectable).ToArray();
            int pages=Mathf.Max(1,Mathf.CeilToInt(items.Length/6f));
            lootPage=Mathf.Clamp(lootPage,0,pages-1);
            var visible=items.Skip(lootPage*6).Take(6).ToArray();
            for(int i=0;i<visible.Length;i++)
            {
                var item=visible[i];
                float x=455+(i%2)*390,y=197+(i/2)*116;
                Panel(new Rect(x,y,375,104));
                var part=item.ToPart();
                var lootTitle=new GUIStyle(body){fontSize=17,wordWrap=true};
                var lootDetail=new GUIStyle(small){fontSize=13,wordWrap=true};
                GUI.Label(new Rect(x+10,y+5,252,26),R4WeaponDefinition.PartName(part),lootTitle);
                string type=item.Kind==R3PartKind.Gun?"Broń":R4WeaponDefinition.PartName(item.Kind);
                GUI.Label(new Rect(x+10,y+30,252,32),type+" • "+item.Source,lootDetail);
                string condition=item.Health<=0?"ZNISZCZONA":item.Health<1?"USZKODZONA":"NIENARUSZONA";
                GUI.Label(new Rect(x+10,y+64,255,37),
                    $"{condition} • HP: {item.Health*100:0}%\n{(item.Health<=0?"Odbudowa":"Naprawa")}: {run.VehicleModel.RepairCost(part)} Z",lootDetail);
                bool chosen=lootSelection.Contains(item.Id);
                GUI.enabled=chosen||lootSelection.Count<partsLimit;
                GUI.backgroundColor=chosen?new Color(.35f,.85f,.65f):Color.white;
                if(GUI.Button(new Rect(x+268,y+26,97,48),chosen?"Wybrano ✓":"Zatrzymaj",compactButton))
                {if(chosen)lootSelection.Remove(item.Id);else lootSelection.Add(item.Id);}
                GUI.enabled=true;GUI.backgroundColor=Color.white;
                string ammo=SalvageAmmoLabel(part);
                if(ammo!=null)GUI.Label(SalvageAmmoRect(x,y),ammo,lootDetail);
            }
            GUI.enabled=lootPage>0;
            if(GUI.Button(new Rect(465,556,90,35),"←",compactButton))lootPage=Mathf.Max(0,lootPage-1);
            GUI.Label(new Rect(562,559,185,32),$"Strona {lootPage+1}/{pages}",small);
            GUI.enabled=lootPage<pages-1;
            if(GUI.Button(new Rect(730,556,90,35),"→",compactButton))lootPage=Mathf.Min(pages-1,lootPage+1);
            GUI.enabled=true;

            if(GUI.Button(new Rect(850,556,375,35),$"Magazyn {run.VehicleModel.Inventory.Count}/{R3VehicleModel.InventoryCapacity} • zwolnij miejsce",compactButton))
                rewardInventoryOpen=true;
            bool room=run.VehicleModel.Inventory.Count+lootSelection.Count+1<=R3VehicleModel.InventoryCapacity;
            GUI.Label(new Rect(45,603,685,87),
                $"Moduł bonusowy: {(selectedBonus>=0?"1/1":"0/1 (WYMAGANY)")}  •  Części z wraków: {lootSelection.Count}/{partsLimit}\n" +
                $"Niewybrane części → +{ledger.PreviewScrap(run.VehicleModel,lootSelection)} Z.\n" +
                "Bonusy trafiają do magazynu i pozostają NIEAKTYWNE do montażu.",small);
            GUI.enabled=selectedBonus>=0&&room;
            if(GUI.Button(new Rect(805,610,420,68),"Zatwierdź oba wybory i kontynuuj",button))
                run.ConfirmRewards(lootSelection,selectedBonus>=0?new[]{selectedBonus}:System.Array.Empty<int>());
            GUI.enabled=true;
            if(!room)GUI.Label(new Rect(45,685,1180,30),"Brak miejsca na wybrane nagrody. Otwórz magazyn i rozmontuj posiadane części, aby zwolnić miejsce.",small);
        }
        private static Rect RewardInventoryDetailsRect(int index)=>new Rect(60,187+index*76,475,57);
        private static Rect RewardInventoryStatusRect(int index)=>new Rect(550,187+index*76,280,57);
        private static string RewardInventoryItemText(R3PartState item) =>
            $"{R4WeaponDefinition.PartName(item)} • HP {item.Health*100:0}%\nŹródło: {item.Source}";
        private static string RewardInventoryStatusText(R3VehicleModel model,R3PartState item)
        {
            string ammo=SalvageAmmoLabel(item);
            return $"{(item.Health<=0?"Odbudowa":"Naprawa")}: {model.RepairCost(item)} Z"+(ammo==null?"":"\n"+ammo);
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
            string name=kind==R3PartKind.BonusModule&&items.Length>0?R4WeaponDefinition.PartName(items[0]):R4WeaponDefinition.PartName(kind,weapon);
            GUI.Label(new Rect(970,70,280,48),$"{name}  × {items.Length}",body);
            stackScroll=GUI.BeginScrollView(new Rect(970,118,280,210),stackScroll,new Rect(0,0,255,Mathf.Max(210,items.Length*40)));
            for(int i=0;i<items.Length;i++)
            {
                var item=items[i];GUI.backgroundColor=garage.SelectedInventoryId==item.Id?new Color(.3f,.9f,.9f):Color.white;
                if(GUI.Button(new Rect(0,i*40,250,36),$"{R4WeaponDefinition.PartName(item)} • HP {item.Health*100:0}%",compactButton))
                {garage.ChooseInventory(item.Id);dismantleConfirmation=null;catalogOpen=false;}
            }
            GUI.EndScrollView();GUI.backgroundColor=Color.white;
            int price=model.Price(kind,weapon);
            GUI.enabled=kind!=R3PartKind.BonusModule&&model.Scrap>=price&&model.Inventory.Count<R3VehicleModel.InventoryCapacity;
            if(GUI.Button(new Rect(970,340,280,40),$"Kup do magazynu — {price} Z",compactButton))
            {
                if(run.GaragePurchase(kind,weapon))
                {var purchased=model.Inventory.Last();garage.ChooseInventory(purchased.Id);catalogOpen=false;garage.Message="Kupiono część. Kliknij pojazd, aby ją zamontować.";}
                else garage.Message="Nie udało się kupić części.";
            }
            GUI.enabled=true;
            var selected=items.FirstOrDefault(p=>p.Id==garage.SelectedInventoryId);
            if(selected==null)
            {GUI.Label(new Rect(970,400,280,100),"Wybierz egzemplarz z listy. Zielony podgląd oznacza poprawne miejsce montażu.",small);return;}
            GUI.Label(new Rect(970,395,280,70),$"{R4WeaponDefinition.PartName(selected)} • HP {selected.Health*100:0}%\nKliknij pojazd, aby zamontować.",small);
            int repairQuote=model.RepairCost(selected);
            GUI.enabled=repairQuote>0&&model.Scrap>=repairQuote;
            if(GUI.Button(new Rect(970,480,280,42),$"Napraw / odbuduj — {repairQuote} Z",compactButton))run.GarageRepairInventory(selected.Id);
            GUI.enabled=true;
            if(GUI.Button(new Rect(970,535,280,42),dismantleConfirmation==selected.Id?"Potwierdź rozmontowanie":$"Rozmontuj → {model.DismantleValue(selected)} Z",compactButton))
            {if(dismantleConfirmation==selected.Id){run.GarageDismantle(selected.Id);dismantleConfirmation=null;return;}dismantleConfirmation=selected.Id;}
            GUI.Label(new Rect(970,590,280,100),garage.PlacementIssue??"Zielony podgląd: kliknij, aby zamontować.",small);
        }
    }
}
