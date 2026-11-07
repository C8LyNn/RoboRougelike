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
        private static Rect PortraitCatalogItemsViewport()=>new Rect(390,175,354,610);
        private static Rect PortraitCatalogBuyRect()=>new Rect(390,805,354,52);
        private static Rect PortraitCatalogBackRect()=>new Rect(560,20,184,52);
        private static Rect PortraitCatalogGroupRow(int index)=>new Rect(0,index*56,320,50);
        private static Rect PortraitCatalogItemRow(int index)=>new Rect(0,index*108,330,102);

        private void DrawPortraitCatalog(R1RunController run)
        {
            var before=GUI.color;GUI.color=new Color(.04f,.06f,.08f,1);
            GUI.DrawTexture(new Rect(0,0,768,1024),Texture2D.whiteTexture);GUI.color=before;
            GUI.Label(new Rect(24,20,520,40),"KATALOG CZĘŚCI • MAGAZYN",heading);
            GUI.Label(new Rect(24,80,720,26),$"Złom: {run.VehicleModel.Scrap} • wybierz grupę i egzemplarz według HP",body);
            if(GUI.Button(PortraitCatalogBackRect(),"Wróć do garażu",compactButton)){catalogOpen=false;return;}
            GUI.Label(new Rect(24,110,346,25),"Grupy części",body);
            GUI.Label(new Rect(390,110,354,55),"Egzemplarze w magazynie",body);

            var model=run.VehicleModel;
            var bonuses=model.Inventory.Where(p=>p.Kind==R3PartKind.BonusModule).Select(p=>p.UpgradeId).Distinct().ToArray();
            int count=shopKinds.Length-1+R4WeaponDefinition.Count+bonuses.Length;
            var groups=PortraitCatalogGroupsViewport();
            catalogScroll=GUI.BeginScrollView(groups,catalogScroll,new Rect(0,0,320,Mathf.Max(groups.height,count*56)));
            int row=0;
            for(int i=0;i<shopKinds.Length;i++)
            {
                var kind=shopKinds[i];if(kind==R3PartKind.Gun)continue;
                DrawPortraitCatalogGroup(run,kind,R4WeaponKind.Basic,null,shopNames[i],row++);
            }
            for(int i=0;i<R4WeaponDefinition.Count;i++)
            {
                var weapon=(R4WeaponKind)i;
                DrawPortraitCatalogGroup(run,R3PartKind.Gun,weapon,null,R4WeaponDefinition.Get(weapon).Name,row++);
            }
            foreach(var bonus in bonuses)
            {
                var item=model.Inventory.First(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeId==bonus);
                DrawPortraitCatalogGroup(run,R3PartKind.BonusModule,R4WeaponKind.Basic,bonus,R4WeaponDefinition.PartName(item),row++);
            }
            GUI.EndScrollView();GUI.backgroundColor=Color.white;

            if(!catalogGroup.HasValue)
            {
                GUI.Label(new Rect(390,175,354,100),"Wybierz grupę, aby zobaczyć jej egzemplarze.",body);
                return;
            }
            var selectedKind=catalogGroup.Value;var selectedWeapon=catalogWeapon;
            var items=model.Inventory.Where(p=>InCatalogGroup(p,selectedKind,selectedWeapon,catalogBonusId)).ToArray();
            string name=selectedKind==R3PartKind.BonusModule&&items.Length>0?R4WeaponDefinition.PartName(items[0]):R4WeaponDefinition.PartName(selectedKind,selectedWeapon);
            GUI.Label(new Rect(390,145,354,28),$"{name} × {items.Length}",small);
            var viewport=PortraitCatalogItemsViewport();
            stackScroll=GUI.BeginScrollView(viewport,stackScroll,new Rect(0,0,330,Mathf.Max(viewport.height,items.Length*108)));
            for(int i=0;i<items.Length;i++)
            {
                var item=items[i];var rect=PortraitCatalogItemRow(i);
                GUI.Label(new Rect(rect.x,rect.y,rect.width,28),$"{R4WeaponDefinition.PartName(item)} • HP {item.Health*100:0}%",small);
                GUI.backgroundColor=run.Garage.SelectedInventoryId==item.Id?new Color(.3f,.9f,.9f):Color.white;
                if(GUI.Button(new Rect(rect.x,rect.y+31,102,60),"Wybierz\ndo montażu",compactButton))PortraitCatalogSelectItem(run,item.Id);
                GUI.backgroundColor=Color.white;
                int repair=model.RepairCost(item);GUI.enabled=repair>0&&model.Scrap>=repair;
                if(GUI.Button(new Rect(rect.x+108,rect.y+31,102,60),$"Napraw\n{repair} Z",compactButton))PortraitCatalogRepair(run,item.Id);
                GUI.enabled=true;
                if(GUI.Button(new Rect(rect.x+216,rect.y+31,114,60),dismantleConfirmation==item.Id?"Potwierdź\nrozmontowanie":$"Rozmontuj\n→ {model.DismantleValue(item)} Z",compactButton))PortraitCatalogDismantle(run,item.Id);
            }
            GUI.EndScrollView();
            int price=model.Price(selectedKind,selectedWeapon);
            bool unlocked=run.CanBuyTechnology(selectedKind,selectedWeapon);
            GUI.enabled=unlocked&&selectedKind!=R3PartKind.BonusModule&&model.Scrap>=price&&model.Inventory.Count<R3VehicleModel.InventoryCapacity;
            if(GUI.Button(PortraitCatalogBuyRect(),unlocked?$"Kup do magazynu — {price} Z":"Pokonaj wroga z tą bronią",compactButton))PortraitCatalogPurchase(run,selectedKind,selectedWeapon);
            GUI.enabled=true;
            var selected=items.FirstOrDefault(p=>p.Id==run.Garage.SelectedInventoryId);
            if(selected!=null&&R3VehicleModel.SupportsFusion(selected.Kind,selected.Weapon))DrawFusionButton(run,selected,new Rect(390,875,354,52));
            else GUI.Label(new Rect(390,875,354,82),"Wybierz egzemplarz, aby wrócić do garażu i zamontować go na pojeździe.",small);
        }

        private void DrawPortraitCatalogGroup(R1RunController run,R3PartKind kind,R4WeaponKind weapon,string bonus,string name,int row)
        {
            int count=run.VehicleModel.Inventory.Count(p=>InCatalogGroup(p,kind,weapon,bonus));
            bool chosen=catalogGroup==kind&&(kind!=R3PartKind.Gun||catalogWeapon==weapon)&&(kind!=R3PartKind.BonusModule||catalogBonusId==bonus);
            GUI.backgroundColor=chosen?new Color(.3f,.9f,.9f):Color.white;
            if(GUI.Button(PortraitCatalogGroupRow(row),$"{name} × {count}"+(run.CanBuyTechnology(kind,weapon)?"":" • blokada"),compactButton))PortraitCatalogSelectGroup(run,kind,weapon,bonus);
        }

        private void PortraitCatalogSelectGroup(R1RunController run,R3PartKind kind,R4WeaponKind weapon,string bonus)
        {
            catalogGroup=kind;catalogWeapon=weapon;catalogBonusId=bonus;stackScroll=Vector2.zero;
            var garage=run.Garage;garage.SelectedPart=kind;garage.SelectedWeapon=weapon;
            garage.SelectedInventoryId=null;garage.UseInventory=true;garage.SetTool(R41GarageTool.Mount);inventoryOpen=true;
        }
        private void PortraitCatalogSelectItem(R1RunController run,string id)
        {run.Garage.ChooseInventory(id);dismantleConfirmation=null;catalogOpen=false;}
        private void PortraitCatalogPurchase(R1RunController run,R3PartKind kind,R4WeaponKind weapon)
        {
            if(run.GaragePurchase(kind,weapon))
            {
                var purchased=run.VehicleModel.Inventory.Last();run.Garage.ChooseInventory(purchased.Id);catalogOpen=false;
                run.Garage.Message="Kupiono część. Kliknij pojazd, aby ją zamontować.";
            }
            else run.Garage.Message="Nie udało się kupić części.";
        }
        private void PortraitCatalogRepair(R1RunController run,string id)=>run.GarageRepairInventory(id);
        private void PortraitCatalogDismantle(R1RunController run,string id)
        {
            if(dismantleConfirmation==id){run.GarageDismantle(id);dismantleConfirmation=null;}
            else dismantleConfirmation=id;
        }
    }
}
