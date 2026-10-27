using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        private readonly HashSet<string> lootSelection=new HashSet<string>();
        private R4SalvageLedger displayedLedger;
        private int lootPage,inventoryPage;
        private bool inventoryOpen;
        private string dismantleConfirmation;
        private void DrawSalvage(R1RunController run)
        {
            var ledger=run.Salvage;
            if(displayedLedger!=ledger){displayedLedger=ledger;lootSelection.Clear();lootPage=0;}
            GUI.Label(new Rect(70,40,1140,60),"ZWYCIĘSTWO • ODZYSK",title);
            GUI.Label(new Rect(70,108,1140,55),$"Zachowaj do {ledger.Limit} modułów. Stan pochodzi z pokonanych pojazdów. +{run.LastScrapReward} Z za walkę.",body);
            var items=ledger.Items;int pages=Mathf.Max(1,Mathf.CeilToInt(items.Count/6f));lootPage=Mathf.Clamp(lootPage,0,pages-1);
            foreach(var item in items.Skip(lootPage*6).Take(6).ToArray())
            {
                int index=items.ToList().IndexOf(item)-lootPage*6;float x=70+(index%2)*580,y=185+(index/2)*120;
                Panel(new Rect(x,y,560,108));var part=item.ToPart();
                GUI.Label(new Rect(x+15,y+8,360,28),R4WeaponDefinition.PartName(part),heading);
                GUI.Label(new Rect(x+15,y+40,360,56),$"{item.Source}\nHP: {item.Health*100:0}% • {(item.Recoverable?"ocalała część":"zniszczona — tylko 1 Z")}",small);
                bool chosen=lootSelection.Contains(item.Id);GUI.enabled=item.Recoverable&&(chosen||lootSelection.Count<ledger.Limit);
                if(GUI.Button(new Rect(x+390,y+22,155,62),chosen?"ZACHOWAJ ✓":item.Recoverable?"Zachowaj":"Zniszczona",compactButton))
                {if(chosen)lootSelection.Remove(item.Id);else lootSelection.Add(item.Id);}
                GUI.enabled=true;
            }
            if(GUI.Button(new Rect(70,557,110,36),"←",compactButton))lootPage=Mathf.Max(0,lootPage-1);
            GUI.Label(new Rect(200,561,200,30),$"Strona {lootPage+1}/{pages}",body);
            if(GUI.Button(new Rect(420,557,110,36),"→",compactButton))lootPage=Mathf.Min(pages-1,lootPage+1);
            GUI.Label(new Rect(70,608,650,70),$"Wybrano: {lootSelection.Count}/{ledger.Limit}. Pozostałe → +{ledger.PreviewScrap(run.VehicleModel,lootSelection)} Z.\nDemontaż: 25% ceny × HP (w dół). Wrak modułu: 1 Z. Odbiór jednorazowy.",small);
            if(GUI.Button(new Rect(770,608,440,65),"Odbierz i kontynuuj",button))run.ConfirmSalvage(lootSelection);
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
            {GUI.Label(new Rect(970,390,280,240),"Wybierz część z magazynu, najedź na powierzchnię pojazdu i kliknij zielony ghost. Montaż jest bezpłatny.\n\nŻywe części zdjęte z pojazdu wracają tutaj.",body);return;}
            var profile=R4WeaponDefinition.Get(selected.Weapon);
            GUI.Label(new Rect(970,390,280,66),selected.Kind==R3PartKind.Gun?
                $"{profile.Damage*run.Progress.Stats.Damage:0} obrażeń • co {profile.Interval*run.Progress.Stats.FireInterval:0.00} s\n{profile.Speed:0} m/s • masa {profile.Mass:0} • HP {selected.Health*100:0}%":$"{R4WeaponDefinition.PartName(selected)} • masa {R4WeaponDefinition.PartMass(selected)}\nHP {selected.Health*100:0}%",small);
            string error=model.PlacementError(garage.Cell,selected.Kind,true,selected.Weapon);
            GUI.enabled=garage.HasTarget&&error==null;
            if(GUI.Button(new Rect(970,465,280,42),"Zamontuj — bez opłaty",compactButton))
            {garage.Message=run.GarageMount(selected.Id,garage.Cell)?"Moduł zamontowany z zachowanym HP.":"Montaż nieudany.";return;}
            GUI.enabled=model.RepairCost(selected)>0&&model.Scrap>=model.RepairCost(selected);
            if(GUI.Button(new Rect(970,515,280,42),$"Napraw — {model.RepairCost(selected)} Z",compactButton))run.GarageRepairInventory(selected.Id);
            GUI.enabled=true;
            if(GUI.Button(new Rect(970,565,280,42),dismantleConfirmation==selected.Id?"Potwierdź rozmontowanie":$"Rozmontuj → {model.DismantleValue(selected)} Z",compactButton))
            {if(dismantleConfirmation==selected.Id){run.GarageDismantle(selected.Id);dismantleConfirmation=null;return;}dismantleConfirmation=selected.Id;}
            GUI.Label(new Rect(970,618,280,77),error??$"Wybrano {garage.Cell}. Zielony podgląd.\nŹródło: {selected.Source}",small);
        }
    }
}
