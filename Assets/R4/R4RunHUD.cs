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
        private readonly HashSet<int> bonusSelection=new HashSet<int>();
        private R4SalvageLedger displayedLedger;
        private int lootPage,inventoryPage;
        private bool inventoryOpen;
        private string dismantleConfirmation;
        private void TrackRewardLedger(R4SalvageLedger ledger)
        {
            if(displayedLedger==ledger)return;
            displayedLedger=ledger;lootSelection.Clear();bonusSelection.Clear();lootPage=0;
        }
#if ENABLE_INPUT_SYSTEM
        public void ReadRewardInput(Keyboard keyboard)
        {
            var run=R1RunController.Instance;
            if(run?.Progress?.State!=R1RunState.Salvage||run.Transitioning||run.Salvage==null)return;
            TrackRewardLedger(run.Salvage);
            int index=keyboard.digit1Key.wasPressedThisFrame?0:keyboard.digit2Key.wasPressedThisFrame?1:keyboard.digit3Key.wasPressedThisFrame?2:-1;
            if(index>=0&&index<run.Progress.Offers.Count)
            {
                if(bonusSelection.Contains(index))bonusSelection.Remove(index);
                else if(bonusSelection.Count+lootSelection.Count<3)bonusSelection.Add(index);
            }
            if(keyboard.enterKey.wasPressedThisFrame)run.ConfirmRewards(lootSelection,bonusSelection);
        }
#endif
        private void DrawSalvage(R1RunController run)
        {
            var ledger=run.Salvage;
            TrackRewardLedger(ledger);
            const int limit=3;
            int selected=lootSelection.Count+bonusSelection.Count;
            GUI.Label(new Rect(45,30,1190,60),"ZWYCIĘSTWO • WYBÓR NAGRÓD",title);
            GUI.Label(new Rect(45,97,1190,50),
                $"Zatrzymaj maksymalnie {limit} rzeczy: dowolne bonusy lub części (bez pancerza i rdzeni). +{run.LastScrapReward} Z za walkę.",body);
            GUI.Label(new Rect(45,155,355,32),"BONUSY",heading);
            GUI.Label(new Rect(455,155,765,32),"CZĘŚCI PRZECIWNIKÓW",heading);

            var offers=run.Progress.Offers.ToArray();
            for(int i=0;i<offers.Length;i++)
            {
                var upgrade=offers[i];float y=197+i*116;
                Panel(new Rect(45,y,370,104));
                GUI.Label(new Rect(55,y+8,243,29),upgrade.Title,body);
                GUI.Label(new Rect(55,y+40,243,57),upgrade.Description,small);
                bool chosen=bonusSelection.Contains(i);
                GUI.enabled=chosen||selected<limit;
                GUI.backgroundColor=chosen?new Color(.35f,.85f,.65f):Color.white;
                if(GUI.Button(new Rect(308,y+25,97,50),chosen?"Wybrano ✓":"Wybierz",compactButton))
                {if(chosen)bonusSelection.Remove(i);else bonusSelection.Add(i);selected=lootSelection.Count+bonusSelection.Count;}
                GUI.enabled=true;GUI.backgroundColor=Color.white;
            }
            if(offers.Length==0)
                GUI.Label(new Rect(55,205,345,80),"Ostatnie starcie: tylko odzysk części.",small);

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
                GUI.enabled=chosen||selected<limit;
                GUI.backgroundColor=chosen?new Color(.35f,.85f,.65f):Color.white;
                if(GUI.Button(new Rect(x+268,y+26,97,48),chosen?"Wybrano ✓":"Zatrzymaj",compactButton))
                {if(chosen)lootSelection.Remove(item.Id);else lootSelection.Add(item.Id);selected=lootSelection.Count+bonusSelection.Count;}
                GUI.enabled=true;GUI.backgroundColor=Color.white;
            }
            GUI.enabled=lootPage>0;
            if(GUI.Button(new Rect(465,556,90,35),"←",compactButton))lootPage=Mathf.Max(0,lootPage-1);
            GUI.Label(new Rect(562,559,185,32),$"Strona {lootPage+1}/{pages}",small);
            GUI.enabled=lootPage<pages-1;
            if(GUI.Button(new Rect(730,556,90,35),"→",compactButton))lootPage=Mathf.Min(pages-1,lootPage+1);
            GUI.enabled=true;

            GUI.Label(new Rect(45,603,685,87),
                $"Wybrano {selected}/{limit}  •  Bonusy: {bonusSelection.Count}  •  Części: {lootSelection.Count}\n" +
                $"Niewybrane części → +{ledger.PreviewScrap(run.VehicleModel,lootSelection)} Z.\n" +
                "Zniszczone części wymagają odbudowy; uszkodzone tańszej naprawy.",small);
            if(GUI.Button(new Rect(805,610,420,68),"Zatwierdź nagrody [Enter]",button))
                run.ConfirmRewards(lootSelection,bonusSelection);
        }
        private void DrawInventory(R1RunController run)
        {
            var model=run.VehicleModel;var garage=run.Garage;var items=model.Inventory;
            int pages=Mathf.Max(1,Mathf.CeilToInt(items.Count/4f));inventoryPage=Mathf.Clamp(inventoryPage,0,pages-1);
            int row=0;
            foreach(var item in items.Skip(inventoryPage*4).Take(4).ToArray())
            {
                GUI.backgroundColor=garage.SelectedInventoryId==item.Id?new Color(0.3f,0.9f,0.9f):Color.white;
                if(GUI.Button(new Rect(970,75+row*66,280,60),$"{R4WeaponDefinition.PartName(item)}\nW magazynie • HP {item.Health*100:0}%",compactButton))
                {garage.ChooseInventory(item.Id);dismantleConfirmation=null;}row++;
            }
            GUI.backgroundColor=Color.white;
            if(GUI.Button(new Rect(970,345,70,32),"←",compactButton))inventoryPage=Mathf.Max(0,inventoryPage-1);
            GUI.Label(new Rect(1050,347,130,30),$"{inventoryPage+1}/{pages}",small);
            if(GUI.Button(new Rect(1180,345,70,32),"→",compactButton))inventoryPage=Mathf.Min(pages-1,inventoryPage+1);
            var selected=items.FirstOrDefault(p=>p.Id==garage.SelectedInventoryId);
            if(selected==null)
            {GUI.Label(new Rect(970,390,280,240),"Wybierz część z magazynu. Zniszczone moduły napraw przed montażem. Montaż sprawnych i uszkodzonych części jest bezpłatny.\n\nZdjęte moduły wracają do magazynu.",body);return;}
            var profile=R4WeaponDefinition.Get(selected.Weapon);
            GUI.Label(new Rect(970,390,280,66),selected.Kind==R3PartKind.Gun?
                $"{profile.Damage*run.Progress.Stats.Damage:0} obrażeń • co {profile.Interval*run.Progress.Stats.FireInterval:0.00} s\n{profile.Speed:0} m/s • masa {profile.Mass:0} • HP {selected.Health*100:0}%":$"{R4WeaponDefinition.PartName(selected)} • masa {R4WeaponDefinition.PartMass(selected)}\nHP {selected.Health*100:0}%",small);
            string error=model.PlacementError(garage.Cell,selected.Kind,true,selected.Weapon);
            GUI.enabled=garage.HasTarget&&error==null&&selected.Health>0;
            if(GUI.Button(new Rect(970,465,280,42),"Zamontuj — bez opłaty",compactButton))
            {garage.Message=run.GarageMount(selected.Id,garage.Cell)?"Moduł zamontowany z zachowanym HP.":"Montaż nieudany.";return;}
            GUI.enabled=model.RepairCost(selected)>0&&model.Scrap>=model.RepairCost(selected);
            if(GUI.Button(new Rect(970,515,280,42),$"{(selected.Health<=0?"Odbuduj":"Napraw")} — {model.RepairCost(selected)} Z",compactButton))run.GarageRepairInventory(selected.Id);
            GUI.enabled=true;
            if(GUI.Button(new Rect(970,565,280,42),dismantleConfirmation==selected.Id?"Potwierdź rozmontowanie":$"Rozmontuj → {model.DismantleValue(selected)} Z",compactButton))
            {if(dismantleConfirmation==selected.Id){run.GarageDismantle(selected.Id);dismantleConfirmation=null;return;}dismantleConfirmation=selected.Id;}
            GUI.Label(new Rect(970,618,280,77),error??$"Wybrano {garage.Cell}. Zielony podgląd.\nŹródło: {selected.Source}",small);
        }
    }
}
