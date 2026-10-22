using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
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
            GUI.Label(new Rect(45,30,1190,60),"ZWYCIĘSTWO • WYBÓR NAGRÓD",title);
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
                GUI.Label(new Rect(60,y+7,760,57),$"{R4WeaponDefinition.PartName(item)} • HP {item.Health*100:0}%\nŹródło: {item.Source}",small);
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
            var model=run.VehicleModel;var garage=run.Garage;var items=model.Inventory;
            int pages=Mathf.Max(1,Mathf.CeilToInt(items.Count/4f));inventoryPage=Mathf.Clamp(inventoryPage,0,pages-1);
            int row=0;
            foreach(var item in items.Skip(inventoryPage*4).Take(4).ToArray())
            {
                GUI.backgroundColor=garage.SelectedInventoryId==item.Id?new Color(0.3f,0.9f,0.9f):Color.white;
                if(GUI.Button(new Rect(970,75+row*66,280,60),$"{R4WeaponDefinition.PartName(item)}\nW magazynie • {(item.Kind==R3PartKind.BonusModule?"1×2×2 • ":"")}HP {item.Health*100:0}%",compactButton))
                {garage.ChooseInventory(item.Id);dismantleConfirmation=null;}row++;
            }
            GUI.backgroundColor=Color.white;
            if(GUI.Button(new Rect(970,345,70,32),"←",compactButton))inventoryPage=Mathf.Max(0,inventoryPage-1);
            GUI.Label(new Rect(1050,347,130,30),$"{inventoryPage+1}/{pages}",small);
            if(GUI.Button(new Rect(1180,345,70,32),"→",compactButton))inventoryPage=Mathf.Min(pages-1,inventoryPage+1);
            var selected=items.FirstOrDefault(p=>p.Id==garage.SelectedInventoryId);
            if(selected==null)
            {GUI.Label(new Rect(970,390,280,240),"Wybierz część z magazynu. Bonusy 1×2×2 działają wyłącznie po montażu i zajmują cztery sąsiednie pola. Zniszczone moduły napraw przed montażem.\n\nZdjęte moduły wracają do magazynu.",body);return;}
            var profile=R4WeaponDefinition.Get(selected.Weapon);
            GUI.Label(new Rect(970,390,280,66),selected.Kind==R3PartKind.BonusModule?
                $"Moduł 1×2×2 • HP {selected.Health*100:0}%\n{BonusText(selected)} (po montażu)":
                selected.Kind==R3PartKind.Gun?
                $"{profile.Damage*(run.ActiveStats?.Damage??1f):0} obrażeń • co {profile.Interval*(run.ActiveStats?.FireInterval??1f):0.00} s\n{profile.Speed:0} m/s • masa {profile.Mass:0} • HP {selected.Health*100:0}%":$"{R4WeaponDefinition.PartName(selected)} • masa {R4WeaponDefinition.PartMass(selected)}\nHP {selected.Health*100:0}%",small);
            string error=garage.PlacementIssue;
            GUI.enabled=garage.HasTarget&&error==null&&selected.Health>0;
            if(GUI.Button(new Rect(970,465,280,42),"Zamontuj — bez opłaty",compactButton))
            {garage.Message=run.GarageMount(selected.Id,garage.Cell,garage.SelectedRotation)?"Moduł zamontowany z zachowanym HP.":"Montaż nieudany.";return;}
            GUI.enabled=model.RepairCost(selected)>0&&model.Scrap>=model.RepairCost(selected);
            if(GUI.Button(new Rect(970,515,280,42),$"{(selected.Health<=0?"Odbuduj":"Napraw")} — {model.RepairCost(selected)} Z",compactButton))run.GarageRepairInventory(selected.Id);
            GUI.enabled=true;
            if(GUI.Button(new Rect(970,565,280,42),dismantleConfirmation==selected.Id?"Potwierdź rozmontowanie":$"Rozmontuj → {model.DismantleValue(selected)} Z",compactButton))
            {if(dismantleConfirmation==selected.Id){run.GarageDismantle(selected.Id);dismantleConfirmation=null;return;}dismantleConfirmation=selected.Id;}
            GUI.Label(new Rect(970,618,280,77),error??$"Wybrano {garage.Cell}. Zielony podgląd.\nŹródło: {selected.Source}",small);
        }
    }
}
