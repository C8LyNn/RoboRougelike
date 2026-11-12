using System.Linq;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using UnityEngine;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        // Coordinates are on the caller's 768 x 1024 portrait canvas.
        private static Rect PortraitCatalogGroupsViewport()=>new Rect(24,138,346,790);
        private static Rect PortraitCatalogItemsViewport()=>new Rect(390,175,354,550);
        private static Rect PortraitCatalogBulkRepairRect()=>new Rect(390,735,354,50);
        private static Rect PortraitCatalogBuyRect()=>new Rect(390,805,354,52);
        private static Rect PortraitCatalogBackRect()=>new Rect(560,20,184,52);
        private static Rect PortraitCatalogGroupRow(int index)=>new Rect(0,index*56,320,50);
        private static Rect PortraitCatalogItemRow(int index)=>new Rect(0,index*108,330,102);

        private void DrawPortraitCatalog(R1RunController run)
        {
            var before=GUI.color;GUI.color=new Color(.04f,.06f,.08f,1);
            GUI.DrawTexture(new Rect(0,0,768,1024),Texture2D.whiteTexture);GUI.color=before;
            GUI.Label(new Rect(24,20,520,40),"KATALOG CZĘŚCI • MAGAZYN",heading);
            GUI.Label(new Rect(24,80,720,26),run.VehicleModel.CreativeMode?$"Darmowe części • Magazyn: {run.VehicleModel.Inventory.Count}":$"Złom: {run.VehicleModel.Scrap} • Magazyn: {run.VehicleModel.Inventory.Count}/{R3VehicleModel.InventoryCapacity}",body);
            if(GUI.Button(PortraitCatalogBackRect(),"Wróć do garażu",compactButton)){DismissCatalog();return;}
            DrawCatalogSearch(run,new Rect(24,110,70,25),new Rect(94,108,222,28),new Rect(322,108,48,28));
            GUI.Label(new Rect(390,110,354,55),"Egzemplarze w magazynie",body);

            var model=run.VehicleModel;
            var entries=BuildCatalogEntries(model).Where(entry=>CatalogNameMatches(entry.Name,catalogSearch)).ToArray();
            var groups=PortraitCatalogGroupsViewport();
            catalogScroll=GUI.BeginScrollView(groups,catalogScroll,new Rect(0,0,320,Mathf.Max(groups.height,entries.Length*56)));
            for(int row=0;row<entries.Length;row++)
            {var entry=entries[row];DrawPortraitCatalogGroup(run,entry.Kind,entry.Weapon,entry.Group,entry.Name,row);}
            if(entries.Length==0)GUI.Label(new Rect(0,5,310,60),"Brak części pasujących do wyszukiwania.",small);
            GUI.EndScrollView();GUI.backgroundColor=Color.white;

            if(!catalogGroup.HasValue)
            {
                GUI.Label(new Rect(390,175,354,100),"Wybierz grupę, aby zobaczyć jej egzemplarze.",body);
                return;
            }
            var selectedKind=catalogGroup.Value;var selectedWeapon=catalogWeapon;
            var items=model.Inventory.Where(p=>InCatalogGroup(p,selectedKind,selectedWeapon,catalogBonusId)).ToArray();
            string name=selectedKind==R3PartKind.Armor?R4WeaponDefinition.ArmorName(CatalogArmorShape(catalogBonusId)):
                selectedKind==R3PartKind.Wheel?R4WeaponDefinition.WheelName(CatalogWheelStyle(catalogBonusId)):
                selectedKind==R3PartKind.BonusModule&&items.Length>0?CatalogBonusGroupName(items[0]):R4WeaponDefinition.PartName(selectedKind,selectedWeapon);
            GUI.Label(new Rect(390,145,354,28),$"{name} × {items.Length}",small);
            var viewport=PortraitCatalogItemsViewport();
            stackScroll=GUI.BeginScrollView(viewport,stackScroll,new Rect(0,0,330,Mathf.Max(viewport.height,items.Length*108)));
            for(int i=0;i<items.Length;i++)
            {
                var item=items[i];var rect=PortraitCatalogItemRow(i);
                GUI.Label(new Rect(rect.x,rect.y,230,28),CatalogItemCaption(item,false),small);
                if(GUI.Button(new Rect(rect.x+234,rect.y,96,28),"Akcje",compactButton))SelectCatalogDetails(run,item.Id);
                GUI.backgroundColor=run.Garage.SelectedInventoryId==item.Id?new Color(.3f,.9f,.9f):Color.white;
                if(GUI.Button(new Rect(rect.x,rect.y+31,102,60),item.Health<=0?"Szczegóły\nwraku":"Wybierz\ndo montażu",compactButton))PortraitCatalogSelectItem(run,item.Id);
                GUI.backgroundColor=Color.white;
                int repair=model.RepairCost(item);GUI.enabled=(repair>0||(model.CreativeMode&&item.Health<1))&&model.Scrap>=repair;
                if(GUI.Button(new Rect(rect.x+108,rect.y+31,102,60),model.CreativeMode&&item.Health<1?"Napraw 0 Z":GarageRepairCaption(item,repair,model.Scrap),compactButton))PortraitCatalogRepair(run,item.Id);
                GUI.enabled=true;
                if(GUI.Button(new Rect(rect.x+216,rect.y+31,114,60),dismantleConfirmation==item.Id?"Potwierdź\nrozmontowanie":$"Rozmontuj\n→ {model.PreviewAward(model.DismantleValue(item))} Z",compactButton))PortraitCatalogDismantle(run,item.Id);
            }
            if(items.Length==0)GUI.Label(new Rect(0,5,330,100),CatalogEmptyStackHint(model.CreativeMode,selectedKind,run.CanBuyTechnology(selectedKind,selectedWeapon)),small);
            GUI.EndScrollView();
            DrawCatalogBulkRepair(run,items,PortraitCatalogBulkRepairRect());
            int price=model.Price(selectedKind,selectedWeapon);
            bool unlocked=run.CanBuyTechnology(selectedKind,selectedWeapon);
            GUI.enabled=unlocked&&(model.CreativeMode||selectedKind!=R3PartKind.BonusModule)&&model.Scrap>=price&&model.Inventory.Count<model.AvailableInventoryCapacity;
            var buyRect=PortraitCatalogBuyRect();
            if(GUI.Button(new Rect(buyRect.x,buyRect.y,172,buyRect.height),model.CreativeMode?"Dodaj za darmo":GaragePurchaseCaption(selectedKind,unlocked,price,model.Scrap,model.Inventory.Count>=model.AvailableInventoryCapacity),compactButton))PortraitCatalogPurchase(run,selectedKind,selectedWeapon);
            DrawBuyFive(run,selectedKind,selectedWeapon,new Rect(buyRect.x+180,buyRect.y,174,buyRect.height));
            GUI.enabled=true;
            var selected=items.FirstOrDefault(p=>p.Id==run.Garage.SelectedInventoryId);
            if(selected!=null&&R3VehicleModel.SupportsFusion(selected.Kind,selected.Weapon))DrawFusionButton(run,selected,new Rect(390,875,354,52));
            else if(items.Length>0)GUI.Label(new Rect(390,875,354,82),"Wybierz egzemplarz, aby wrócić do garażu i zamontować go na pojeździe.",small);
            if(!string.IsNullOrEmpty(catalogFeedback))GUI.Label(new Rect(24,940,720,52),catalogFeedback,small);
        }

        private void DrawPortraitCatalogGroup(R1RunController run,R3PartKind kind,R4WeaponKind weapon,string bonus,string name,int row)
        {
            bool chosen=catalogGroup==kind&&(kind!=R3PartKind.Gun||catalogWeapon==weapon)&&
                (kind!=R3PartKind.BonusModule&&kind!=R3PartKind.Wheel&&kind!=R3PartKind.Armor||catalogBonusId==bonus);
            GUI.backgroundColor=chosen?new Color(.3f,.9f,.9f):Color.white;
            if(GUI.Button(PortraitCatalogGroupRow(row),CatalogGroupCaption(run,kind,weapon,bonus,name),compactButton))PortraitCatalogSelectGroup(run,kind,weapon,bonus);
        }

        private void PortraitCatalogSelectGroup(R1RunController run,R3PartKind kind,R4WeaponKind weapon,string bonus)
        {
            catalogGroup=kind;catalogWeapon=weapon;catalogBonusId=bonus;stackScroll=Vector2.zero;dismantleConfirmation=null;catalogFeedback=null;
            var garage=run.Garage;garage.SelectedPart=kind;garage.SelectedWeapon=weapon;
            garage.SelectedInventoryId=null;garage.UseInventory=true;garage.SetTool(R41GarageTool.Mount);inventoryOpen=true;
        }
        private void PortraitCatalogSelectItem(R1RunController run,string id)=>SelectCatalogItem(run,id);
        private void PortraitCatalogPurchase(R1RunController run,R3PartKind kind,R4WeaponKind weapon)
        {
            dismantleConfirmation=null;
            if(AddSelectedCatalogPart(run,kind,weapon))
            {
                var purchased=run.VehicleModel.Inventory.Last();run.Garage.ChooseInventory(purchased.Id);DismissCatalog();
                run.Garage.Message=run.CreativeWorkshop?"Dodano część za darmo. Kliknij pojazd, aby ją zamontować.":"Kupiono część. Kliknij pojazd, aby ją zamontować.";
            }
            else catalogFeedback=run.Garage.Message=PurchaseFailure(run,kind,weapon);
        }
        private void PortraitCatalogRepair(R1RunController run,string id)
        {RepairCatalogItem(run,id);}
        private void PortraitCatalogDismantle(R1RunController run,string id)
        {DismantleCatalogItem(run,id);}
    }
}
