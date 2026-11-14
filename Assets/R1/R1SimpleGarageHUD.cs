using System.Linq;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using UnityEngine;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        bool garageOptionsOpen;
        int garageOptionsTab;
        string garageOptionsFeedback;
        int quickPartCategory;
        bool quickOwnedOnly;
        const string QuickSearchControlName="GarageQuickSearch";
        bool quickSearchEditing,quickSearchReleasePending;
        string quickPartSearch=string.Empty;
        GUIStyle quickCategoryButton,garageShortcutLabel,garagePrimaryButton;
        Vector2 quickPartsScroll;
        R3VehicleModel repairQuoteModel;
        float nextRepairQuoteTime;
        int repairQuote;
        static Rect QuickPartsArea(bool portrait)=>portrait?new Rect(24,658,414,178):new Rect(12,105,246,355);
        static Rect QuickRepairArea(bool portrait)=>portrait?new Rect(24,840,414,38):new Rect(12,465,245,38);
        static Rect GarageNoticeRect(bool portrait)=>portrait?new Rect(24,878,720,74):new Rect(280,650,575,64);
        void DrawGarageNotice(bool portrait,string message,bool blocked)
        {
            var rect=GarageNoticeRect(portrait);var before=GUI.color;
            GUI.color=new Color(.075f,.12f,.15f,.97f);GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=blocked?new Color(1f,.65f,.31f):new Color(.28f,.82f,.80f);
            GUI.DrawTexture(new Rect(rect.x,rect.y,3,rect.height),Texture2D.whiteTexture);
            GUI.color=before;
            GUI.Label(new Rect(rect.x+10,rect.y+4,rect.width-16,rect.height-8),message,small);
        }
        GUIStyle GaragePrimaryButton()
        {
            if(garagePrimaryButton==null)
            {
                garagePrimaryButton=new GUIStyle(compactButton);
                garagePrimaryButton.normal.background=buttonPressed;
            }
            return garagePrimaryButton;
        }
        void OpenSimpleGarageOptions(int tab){garageOptionsTab=tab;garageOptionsFeedback=null;activitySaveFeedback=null;garageOptionsOpen=true;repairQuoteModel=null;}
        int SimpleGarageRepairQuote(R3VehicleModel model)
        {
            // The physical quote may construct temporary collision probes.
            // Do not repeat that work for every IMGUI event and rendered frame.
            if(repairQuoteModel!=model||Time.unscaledTime>=nextRepairQuoteTime)
            {repairQuote=model.AffordableRepairCost;repairQuoteModel=model;nextRepairQuoteTime=Time.unscaledTime+.25f;}
            return repairQuote;
        }
        void RepairFromGarageOptions(R1RunController run,bool emergency)
        {
            int before=run.VehicleModel.Scrap;
            bool done=emergency?run.GarageEmergencyRepair():run.GarageRepairAffordable();
            repairQuoteModel=null;
            string action=emergency?"Odbudowano niezbędne moduły.":"Naprawiono dostępne części.";
            garageOptionsFeedback=done?$"{action} Koszt: {before-run.VehicleModel.Scrap} Z.":
                emergency?"Nie można odbudować części.":"Brak środków lub części do naprawy.";
            run.Garage.Message=garageOptionsFeedback;
        }
        static bool QuickCategoryIncludes(int category,R3PartKind kind)=>category==0?kind==R3PartKind.Armor:
            category==1?kind==R3PartKind.Wheel:category==2?kind==R3PartKind.Gun:
            category==3&&kind!=R3PartKind.Armor&&kind!=R3PartKind.Wheel&&kind!=R3PartKind.Gun;

        static int QuickCategoryFor(R3PartKind kind)=>kind==R3PartKind.Armor?0:
            kind==R3PartKind.Wheel?1:kind==R3PartKind.Gun?2:3;

        void SelectQuickCategory(R1RunController run,int category)
        {
            if(category<0||category>3||category==quickPartCategory)return;
            quickPartCategory=category;quickPartsScroll=Vector2.zero;
            catalogGroup=null;catalogBonusId=null;dismantleConfirmation=null;catalogFeedback=null;
            var garage=run?.Garage;
            if(garage==null)return;
            garage.SelectedInventoryId=null;garage.UseInventory=true;
            if(garage.Tool==R41GarageTool.Mount)
            {garage.SetTool(R41GarageTool.Mount);garage.Message="Wybierz część z listy.";}
        }

        void BrowseQuickCategory(R1RunController run,int category)
        {
            if(category<0||category>3)return;
            ApplyQuickSearch(run,string.Empty);
            SelectQuickCategory(run,category);
        }

        void SyncQuickCategoryToCatalog()
        {
            if(!catalogGroup.HasValue)return;
            var model=R1RunController.Instance?.VehicleModel;
            if(quickOwnedOnly&&model!=null&&!model.Inventory.Any(part=>
                InCatalogGroup(part,catalogGroup.Value,catalogWeapon,catalogBonusId)))quickOwnedOnly=false;
            int category=QuickCategoryFor(catalogGroup.Value);
            if(category!=quickPartCategory){quickPartCategory=category;quickPartsScroll=Vector2.zero;}
            if(model!=null&&!BuildFilteredQuickCatalogEntries(model,category,quickOwnedOnly,quickPartSearch).Any(QuickEntrySelected))
                quickPartSearch=string.Empty;
        }

        bool CatalogEntryOwned(R3VehicleModel model,CatalogEntry entry)=>
            model.Inventory.Any(part=>InCatalogGroup(part,entry.Kind,entry.Weapon,entry.Group));

        System.Collections.Generic.List<CatalogEntry> BuildQuickCatalogEntries(R3VehicleModel model,int category,bool ownedOnly)=>
            BuildCatalogEntries(model).Where(entry=>QuickCategoryIncludes(category,entry.Kind)&&
                (!ownedOnly||CatalogEntryOwned(model,entry))).ToList();

        System.Collections.Generic.List<CatalogEntry> BuildFilteredQuickCatalogEntries(R3VehicleModel model,int category,bool ownedOnly,string search)=>
            string.IsNullOrWhiteSpace(search)?BuildQuickCatalogEntries(model,category,ownedOnly):
            BuildCatalogEntries(model).Where(entry=>(!ownedOnly||CatalogEntryOwned(model,entry))&&CatalogNameMatches(entry.Name,search)).ToList();

        bool QuickEntrySelected(CatalogEntry entry)=>catalogGroup==entry.Kind&&
            (entry.Kind!=R3PartKind.Gun||catalogWeapon==entry.Weapon)&&
            (entry.Kind!=R3PartKind.BonusModule&&entry.Kind!=R3PartKind.Wheel&&entry.Kind!=R3PartKind.Armor||catalogBonusId==entry.Group);

        void ApplyQuickSearch(R1RunController run,string next)
        {
            next=next??string.Empty;if(next==quickPartSearch)return;
            quickPartSearch=next;quickPartsScroll=Vector2.zero;
            if(!catalogGroup.HasValue||BuildFilteredQuickCatalogEntries(run.VehicleModel,quickPartCategory,quickOwnedOnly,next).Any(QuickEntrySelected))return;
            catalogGroup=null;catalogBonusId=null;dismantleConfirmation=null;catalogFeedback=null;
            var garage=run.Garage;garage.SelectedInventoryId=null;garage.UseInventory=true;
            if(garage.Tool==R41GarageTool.Mount)
            {garage.SetTool(R41GarageTool.Mount);garage.Message="Wybierz część z listy.";}
        }

        static bool GarageSearchConsumesKey(EventType type,string focused)=>
            (type==EventType.KeyDown||type==EventType.KeyUp)&&focused==QuickSearchControlName;
        internal bool CloseQuickGarageSearch(R1RunController run)
        {
            if(!quickSearchEditing||run==null||run.Paused||run.QuitRequested||run.MapEditing||run.Transitioning||
                run.Progress?.State!=R1RunState.Garage||catalogOpen||garageOptionsOpen||run.Error!=null)return false;
            quickSearchEditing=false;quickSearchReleasePending=true;return true;
        }
        static bool GarageSearchExitKey(EventType type,KeyCode key,string focused,bool active)=>
            active&&focused==QuickSearchControlName&&type==EventType.KeyDown&&
            (key==KeyCode.Escape||key==KeyCode.Return||key==KeyCode.KeypadEnter);
        static int? QuickCategoryShortcut(KeyCode key,bool modified)
        {
            if(modified)return null;
            switch(key)
            {
                case KeyCode.Alpha5:case KeyCode.Keypad5:return 0;
                case KeyCode.Alpha6:case KeyCode.Keypad6:return 1;
                case KeyCode.Alpha7:case KeyCode.Keypad7:return 2;
                case KeyCode.Alpha8:case KeyCode.Keypad8:return 3;
                default:return null;
            }
        }
        bool HandleQuickGarageKey(R1RunController run,Event current,bool textFocused)
        {
            if(textFocused||current.type!=EventType.KeyDown)return false;
            bool modified=current.shift||current.control||current.alt||current.command;
            var category=QuickCategoryShortcut(current.keyCode,modified);
            if(category.HasValue)
            {
                ApplyQuickSearch(run,string.Empty);SelectQuickCategory(run,category.Value);
                current.Use();return true;
            }
            if(!modified&&(current.keyCode==KeyCode.Slash||current.keyCode==KeyCode.KeypadDivide))
            {GUI.FocusControl(QuickSearchControlName);current.Use();return true;}
            return false;
        }

        static void QuickSearchRects(bool portrait,out Rect input,out Rect clear)
        {
            var area=portrait?new Rect(24,628,190,27):new Rect(12,78,112,25);
            input=new Rect(area.x,area.y,area.width-29,area.height);
            clear=new Rect(area.xMax-25,area.y,25,area.height);
        }

        void DrawQuickSearch(R1RunController run,bool portrait)
        {
            QuickSearchRects(portrait,out var rect,out var clear);
            GUI.SetNextControlName(QuickSearchControlName);
            string next=GUI.TextField(rect,quickPartSearch,64);
            if(string.IsNullOrEmpty(next)&&GUI.GetNameOfFocusedControl()!=QuickSearchControlName)
            {var previous=GUI.color;GUI.color=new Color(.62f,.7f,.75f,1);GUI.Label(new Rect(rect.x+5,rect.y,rect.width-10,rect.height),"Szukaj /",small);GUI.color=previous;}
            if(GUI.Button(clear,"×",compactButton))next=string.Empty;
            ApplyQuickSearch(run,next);
        }

        void ToggleQuickOwnedOnly(R1RunController run)
        {
            quickOwnedOnly=!quickOwnedOnly;quickPartsScroll=Vector2.zero;
            ClearMissingOwnedSelection(run);
        }

        void ClearMissingOwnedSelection(R1RunController run)
        {
            if(!quickOwnedOnly||!catalogGroup.HasValue||run.VehicleModel.Inventory.Any(part=>
                InCatalogGroup(part,catalogGroup.Value,catalogWeapon,catalogBonusId)))return;
            catalogGroup=null;catalogBonusId=null;dismantleConfirmation=null;catalogFeedback=null;
            var garage=run.Garage;garage.SelectedInventoryId=null;garage.UseInventory=true;
            if(garage.Tool==R41GarageTool.Mount)
            {garage.SetTool(R41GarageTool.Mount);garage.Message="Wybierz posiadaną część albo pokaż wszystkie części w filtrze.";}
        }

        void DrawQuickOwnedFilter(R1RunController run,Rect rect)
        {
            if(GUI.Button(rect,quickOwnedOnly?"Filtr: posiadane":"Filtr: wszystkie",compactButton))ToggleQuickOwnedOnly(run);
        }
        void DrawQuickStackRepair(R1RunController run,Rect rect)
        {
            if(!catalogGroup.HasValue)
            {
                bool previouslyEnabled=GUI.enabled;GUI.enabled=false;
                GUI.Button(rect,"Wybierz grupę części do naprawy",compactButton);
                GUI.enabled=previouslyEnabled;return;
            }
            var items=run.VehicleModel.Inventory.Where(part=>
                InCatalogGroup(part,catalogGroup.Value,catalogWeapon,catalogBonusId)).ToArray();
            var selected=run.Garage.Tool==R41GarageTool.Mount?
                items.FirstOrDefault(part=>part.Id==run.Garage.SelectedInventoryId&&part.Health<1):null;
            if(selected==null){DrawCatalogBulkRepair(run,items,rect,true);return;}
            var model=run.VehicleModel;int price=model.RepairCost(selected);
            CatalogRepairPlan(model,items,out int eligible,out _);
            bool split=eligible>1;float gap=5,width=split?(rect.width-gap)/2:rect.width;
            bool previous=GUI.enabled;
            GUI.enabled=previous&&R3VehicleModel.ValidEconomicPart(selected)&&
                (model.CreativeMode||price>0&&model.Scrap>=price);
            string label=!R3VehicleModel.ValidEconomicPart(selected)?"Naprawa niedostępna":
                model.CreativeMode?"Napraw 1 • 0 Z":price<=0?"Naprawa niedostępna":
                model.Scrap<price?$"Brakuje {price-model.Scrap} Z":$"Napraw 1 • {price} Z";
            if(GUI.Button(new Rect(rect.x,rect.y,width,rect.height),label,compactButton))RepairQuickSelected(run);
            GUI.enabled=previous;
            if(split)DrawCatalogBulkRepair(run,items,new Rect(rect.x+width+gap,rect.y,width,rect.height),true,true);
        }

        bool RepairQuickSelected(R1RunController run)
        {
            if(run?.Garage==null||run.VehicleModel==null||!catalogGroup.HasValue||run.Garage.Tool!=R41GarageTool.Mount)return false;
            var item=run.VehicleModel.Inventory.FirstOrDefault(part=>part.Id==run.Garage.SelectedInventoryId&&
                InCatalogGroup(part,catalogGroup.Value,catalogWeapon,catalogBonusId));
            if(item==null||item.Health>=1)return false;
            int before=run.VehicleModel.Scrap;
            bool repaired=run.GarageRepairInventory(item.Id);
            run.Garage.ShowActionMessage(repaired?$"Naprawiono wybraną część • koszt {before-run.VehicleModel.Scrap} Z.":"Nie udało się naprawić wybranej części.");
            return repaired;
        }

        static string GaragePurchaseCaption(R3PartKind kind,bool unlocked,int price,int scrap,bool full,bool toInventory=false)=>
            kind==R3PartKind.BonusModule?"Tylko z nagród":
            !unlocked?"Pokonaj wroga z tą bronią":full?"Magazyn pełny":
            price==0?"Dodaj za darmo":scrap<price?$"Brakuje {price-scrap} Z":toInventory?$"Kup do magazynu — {price} Z":$"Kup — {price} Z";

        static string QuickPurchaseCaption(bool creative,R3PartKind kind,bool unlocked,int price,int scrap,bool full)
        {
            if(!unlocked)return "Pokonaj wroga z tą bronią";
            if(full)return "Magazyn pełny";
            if(creative)return "Dodaj i wybierz\n0 Z";
            if(kind==R3PartKind.BonusModule||price<=0||scrap<price)
                return GaragePurchaseCaption(kind,unlocked,price,scrap,full);
            return $"Kup i wybierz\n{price} Z";
        }
        int CatalogPurchaseQuantity(R3VehicleModel model,R3PartKind kind)=>model.PurchaseQuantity(kind,
            kind==R3PartKind.Armor?CatalogArmorShape(catalogBonusId):R3ArmorShape.Cube);
        string CatalogPurchaseCaption(R3VehicleModel model,R3PartKind kind,bool unlocked,int price)
        {
            int quantity=CatalogPurchaseQuantity(model,kind);
            bool full=(long)model.Inventory.Count+quantity>model.AvailableInventoryCapacity;
            if(quantity>1&&unlocked&&!full&&price>0&&model.Scrap>=price)return $"Kup ×{quantity}\n{price} Z";
            return QuickPurchaseCaption(model.CreativeMode,kind,unlocked,price,model.Scrap,full);
        }

        static bool CanBuyFive(R3PartKind kind,bool unlocked,int price,int scrap,int inventoryCount)=>
            kind!=R3PartKind.BonusModule&&unlocked&&price>0&&scrap>=0&&inventoryCount>=0&&
            inventoryCount<=R3VehicleModel.InventoryCapacity-5&&(long)price*5<=scrap;

        static string BuyFiveCaption(R3PartKind kind,bool unlocked,int price,int scrap,int inventoryCount)
        {
            if(kind==R3PartKind.BonusModule)return "Tylko nagrody";
            if(!unlocked)return "Blokada";
            if(price<=0)return "Niedostępne";
            if(inventoryCount>R3VehicleModel.InventoryCapacity-5)return "Brak 5 miejsc";
            long total=(long)price*5;
            return total>scrap?$"Brakuje {total-scrap} Z":$"Kup 5 • {total} Z";
        }

        static string BasicPurchaseFailure(bool garageReady,R3PartKind kind,bool unlocked,int price,int scrap,int inventoryCount)
        {
            if(!garageReady)return "Garaż jest chwilowo niedostępny.";
            if(kind==R3PartKind.BonusModule)return "Moduły bonusowe pochodzą z nagród.";
            if(!unlocked)return "Technologia nie została odblokowana.";
            if(inventoryCount>=R3VehicleModel.InventoryCapacity)return "Magazyn jest pełny.";
            if(price<=0)return "Ta część jest niedostępna.";
            if(scrap<price)return $"Brakuje {price-scrap} Z.";
            return null;
        }

        string PurchaseFailure(R1RunController run,R3PartKind kind,R4WeaponKind weapon)
        {
            var model=run.VehicleModel;
            string basic=model.CreativeMode?null:BasicPurchaseFailure(!run.Transitioning&&run.Progress?.State==R1RunState.Garage,
                kind,run.CanBuyTechnology(kind,weapon),model.Price(kind,weapon),model.Scrap,model.Inventory.Count);
            if(basic!=null)return basic;
            int quantity=CatalogPurchaseQuantity(model,kind);
            if((long)model.Inventory.Count+quantity>model.AvailableInventoryCapacity)return $"Brak miejsca na pakiet {quantity} części.";
            if(kind==R3PartKind.Wheel)
            {
                var style=CatalogWheelStyle(catalogBonusId);
                if((R3VehicleModel.HasSizedSideWheel(style)&&!R4WheelArt.SizedWheelAvailable(style))||
                   (R3VehicleModel.HasForkMount(style)&&!R4WheelArt.ForkWheelAvailable(style)))
                    return "Model tego koła jest niedostępny.";
            }
            if(kind==R3PartKind.Armor)
            {
                var shape=CatalogArmorShape(catalogBonusId);
                if(shape!=R3ArmorShape.Cube&&!R41PartGeometry.ArmorShapeAvailable(shape))
                    return "Model tego pancerza jest niedostępny.";
            }
            return "Nie udało się kupić części.";
        }

        void DrawBuyFive(R1RunController run,R3PartKind kind,R4WeaponKind weapon,Rect rect)
        {
            var model=run.VehicleModel;int price=model.Price(kind,weapon);bool unlocked=run.CanBuyTechnology(kind,weapon);
            int quantity=CatalogPurchaseQuantity(model,kind);
            if(quantity>1){GUI.Label(rect,$"Pakiet: {quantity} szt.",small);return;}
            bool previouslyEnabled=GUI.enabled;
            GUI.enabled=previouslyEnabled&&((model.CreativeMode&&unlocked)||CanBuyFive(kind,unlocked,price,model.Scrap,model.Inventory.Count));
            if(GUI.Button(rect,model.CreativeMode?"Dodaj 5 za darmo":BuyFiveCaption(kind,unlocked,price,model.Scrap,model.Inventory.Count),compactButton))
                BuyFiveParts(run,kind,weapon);
            GUI.enabled=previouslyEnabled;
        }

        void BuyFiveParts(R1RunController run,R3PartKind kind,R4WeaponKind weapon)
        {
            var model=run.VehicleModel;int price=model.Price(kind,weapon);
            bool catalogWasOpen=catalogOpen;
            dismantleConfirmation=null;
            if(!(model.CreativeMode&&run.CanBuyTechnology(kind,weapon))&&!CanBuyFive(kind,run.CanBuyTechnology(kind,weapon),price,model.Scrap,model.Inventory.Count))
            {catalogFeedback=run.Garage.Message="Nie kupiono pięciu: "+BuyFiveCaption(kind,run.CanBuyTechnology(kind,weapon),price,model.Scrap,model.Inventory.Count)+".";return;}
            int before=model.Scrap,bought=0;
            for(int i=0;i<5;i++)
            {
                if(!AddSelectedCatalogPart(run,kind,weapon))break;
                bought++;
            }
            if(bought>0)run.Garage.ChooseInventory(model.Inventory.Last().Id);
            string message=model.CreativeMode&&bought==5?"Dodano 5 części za darmo. Wybierz miejsce montażu.":
                model.CreativeMode?$"Dodano {bought}/5 części za darmo. "+PurchaseFailure(run,kind,weapon):
                bought==5?$"Kupiono 5 części • koszt {before-model.Scrap} Z. Wybierz miejsce montażu.":
                $"Kupiono {bought}/5 • {before-model.Scrap} Z. "+PurchaseFailure(run,kind,weapon);
            if(catalogWasOpen&&bought==5)DismissCatalog();
            if(catalogOpen){run.Garage.Message=message;catalogFeedback=message;}
            else run.Garage.ShowActionMessage(message);
        }

        static string GarageRepairCaption(R3PartState part,int price,int scrap)=>
            part.Health>=1?"Część sprawna":price<=0?"Nie można naprawić":
            scrap<price?$"Brakuje {price-scrap} Z":$"Napraw — {price} Z";

        static string GarageBulkRepairCaption(R3VehicleModel model,int affordable)
        {
            if(model.CreativeMode&&model.Parts.Any(p=>p.Health<1))return "Napraw części — za darmo";
            if(affordable>0)return $"Napraw możliwe — {affordable} Z";
            if(!model.Parts.Any(p=>p.Health<1))return "Części pojazdu sprawne";
            int cheapest=model.Parts.Where(p=>p.Health<1).Select(model.RepairCost)
                .Where(price=>price>0).DefaultIfEmpty(int.MaxValue).Min();
            return cheapest!=int.MaxValue&&model.Scrap<cheapest?
                "Brak złomu na naprawę":"Naprawa niedostępna";
        }

        static string GarageEmergencyRepairCaption(int cost,int scrap,bool canDeploy)=>
            cost<=0?(canDeploy?"Odbudowa niepotrzebna":"Odbudowa niedostępna"):
            scrap<cost?$"Brakuje {cost-scrap} Z na odbudowę":$"Odbudowa awaryjna — {cost} Z";

        static string GarageWeaponGroups(R3VehicleModel model)
        {
            var slots=model.GetWeaponSlots();
            bool extra=model.CreativeMode&&model.Parts.Where(p=>p.Kind==R3PartKind.Gun).Select(p=>p.Weapon).Distinct().Count()>4;
            return "LPM: "+WeaponGroupName(slots[0])+"  •  PPM: "+WeaponGroupName(slots[1])+
                "\nQ: "+WeaponGroupName(slots[2])+"  •  E: "+WeaponGroupName(slots[3])+(extra?" + pozostałe":"");
        }
        void DrawSlotAssignments(R3VehicleModel model,float x,float y,float width)
        {
            var weapons=model.GetWeaponSlots();var active=model.GetActiveSlots();var deployables=model.GetDeployableSlots();
            bool showDeployables=deployables.Any(p=>p.HasValue);
            float col=(width-(showDeployables?24:12))/(showDeployables?3:2);
            var keys=new[]{"LPM","PPM","Q","E"};
            var deployKeys=new[]{"Z","X","C","V"};
            GUI.Label(new Rect(x,y,col,30),"BROŃ",heading);
            GUI.Label(new Rect(x+col+12,y,col,30),"MODUŁY AKTYWNE",heading);
            if(showDeployables)GUI.Label(new Rect(x+2*(col+12),y,col,30),"URZĄDZENIA",heading);
            for(int i=0;i<4;i++)
            {
                float row=y+40+i*50;
                GUI.enabled=weapons[i].HasValue;
                if(GUI.Button(new Rect(x,row,col,44),keys[i]+": "+WeaponGroupName(weapons[i]),compactButton))
                    model.AssignWeaponSlot(weapons[i].Value,(i+1)%4);
                GUI.enabled=active[i].HasValue;
                if(GUI.Button(new Rect(x+col+12,row,col,44),(i+1)+": "+(active[i].HasValue?R4WeaponDefinition.PartName(active[i].Value):"—"),compactButton))
                    model.AssignActiveSlot(active[i].Value,(i+1)%4);
                if(showDeployables)
                {
                    GUI.enabled=deployables[i].HasValue;
                    if(GUI.Button(new Rect(x+2*(col+12),row,col,44),deployKeys[i]+": "+(deployables[i].HasValue?R4WeaponDefinition.PartName(deployables[i].Value):"—"),compactButton))
                        model.AssignDeployableSlot(deployables[i].Value,(i+1)%4);
                }
            }
            GUI.enabled=true;
            if(model.CreativeMode)
            {
                var extraWeapon=model.Parts.Where(p=>p.Kind==R3PartKind.Gun).Select(p=>p.Weapon).Distinct()
                    .Where(kind=>!weapons.Contains(kind)).Select(kind=>(R4WeaponKind?)kind).FirstOrDefault();
                var extraActive=model.Parts.Where(p=>R3VehicleModel.IsActiveSlotPart(p.Kind)).Select(p=>p.Kind).Distinct()
                    .Where(kind=>!active.Contains(kind)).Select(kind=>(R3PartKind?)kind).FirstOrDefault();
                var extraDeployable=model.Parts.Where(p=>R3VehicleModel.IsDeployableSlotPart(p.Kind)).Select(p=>p.Kind).Distinct()
                    .Where(kind=>!deployables.Contains(kind)).Select(kind=>(R3PartKind?)kind).FirstOrDefault();
                if(extraWeapon.HasValue&&GUI.Button(new Rect(x,y+245,col,44),"Slot E: "+WeaponGroupName(extraWeapon),compactButton))
                    model.AssignWeaponSlot(extraWeapon.Value,3);
                if(extraActive.HasValue&&GUI.Button(new Rect(x+col+12,y+245,col,44),"Slot 4: "+R4WeaponDefinition.PartName(extraActive.Value),compactButton))
                    model.AssignActiveSlot(extraActive.Value,3);
                if(showDeployables&&extraDeployable.HasValue&&GUI.Button(new Rect(x+2*(col+12),y+245,col,44),"Slot V: "+R4WeaponDefinition.PartName(extraDeployable.Value),compactButton))
                    model.AssignDeployableSlot(extraDeployable.Value,3);
            }
            GUI.Label(new Rect(x,y+296,width,42),"Kliknij rodzaj, aby przenieść go do kolejnego slotu. Zajęte sloty zamienią się miejscami.",small);
        }

        static string GarageInspectionSummary(R3VehicleModel model,R3PartState part)
        {
            int quote=model.RepairCost(part);
            string repair=part.Health>=1?"sprawna":model.CreativeMode?"naprawa za darmo":quote>0?$"naprawa: {quote} Z":"naprawa niedostępna";
            return $"{R4WeaponDefinition.PartName(part)} • HP {part.Health*100:0}%\n{repair}";
        }

        string GarageSelectionSummary(R1RunController run,R3PartState inspected,R3PartState owned)
        {
            var model=run.VehicleModel;
            if(inspected!=null)return GarageInspectionSummary(model,inspected);
            if(owned!=null)return $"{R4WeaponDefinition.PartName(owned)} • HP {owned.Health*100:0}%"+
                (owned.Health<=0?"\nNajpierw napraw część.":"");
            if(!catalogGroup.HasValue)return "Wybierz część z listy.";
            var entry=BuildCatalogEntries(model).FirstOrDefault(QuickEntrySelected);
            string name=entry.Name;
            if(name==null&&catalogGroup.Value==R3PartKind.BonusModule)
            {
                var mounted=model.Parts.FirstOrDefault(part=>part.Kind==R3PartKind.BonusModule&&CatalogBonusKey(part)==catalogBonusId);
                if(mounted!=null)name=CatalogBonusGroupName(mounted);
            }
            name=name??"Wybrana część";
            var items=CatalogItemsForGroup(model,catalogGroup.Value,catalogWeapon,catalogBonusId);
            if(items.Length>0)return items.Any(item=>item.Health>0)?$"{name}\nWybierz egzemplarz w magazynie.":
                $"{name}\nWszystkie zniszczone — napraw poniżej.";
            if(catalogGroup.Value==R3PartKind.BonusModule)return $"{name}\nZdobądź kolejny bonus w nagrodach.";
            if(model.CreativeMode)return $"{name}\nCzęść jest obecnie niedostępna.";
            if(!run.CanBuyTechnology(catalogGroup.Value,catalogWeapon))return $"{name}\nNajpierw odblokuj technologię.";
            return $"{name}\nKup część, aby ją zamontować.";
        }

        static string CatalogItemCaption(R3PartState part,bool compact)
        {
            string caption=$"T{R3VehicleModel.NormalizedTier(part.Tier)} • {(compact?"":"HP ")}{part.Health*100:0}%";
            if(part.Kind==R3PartKind.Wheel&&R3VehicleModel.HasSizedSideWheel(part.WheelStyle))
                caption+=(compact?"\n":" • ")+(part.FootprintRevision==1?"3×3":"1 pole");
            if(part.Kind==R3PartKind.Gun&&R3VehicleModel.SupportsGunScale2(part.Weapon))
                caption+=(compact?"\n":" • ")+(part.FootprintRevision==2?"×2":"×1");
            return caption;
        }

        static string GarageWheelStats(R3PartState part)=>
            $"{R4WeaponDefinition.WheelName(part.WheelStyle)} T{R3VehicleModel.NormalizedTier(part.Tier)} • {R4WeaponDefinition.PartMass(part):0.#} kg\n"+
            $"Siła {100*R3VehicleModel.WheelTraction(part.WheelStyle)*R3VehicleModel.TierWheelTraction(part.Tier):0}% przy 100% HP"+
            (R3VehicleModel.HasSizedSideWheel(part.WheelStyle)?(part.FootprintRevision==1?" • obrys 3×3":" • obrys 1 pole"):"");

        static string GarageWheelSelection(R3PartState part)
        {
            string hint=part.WheelStyle==R3WheelStyle.SwingOffroad?"Oś podąża za nierównościami.":
                part.WheelStyle==R3WheelStyle.CasterSlick?"Koło obraca się zgodnie z ruchem.":
                part.WheelStyle==R3WheelStyle.SteerOffroad||part.WheelStyle==R3WheelStyle.SteerRoad?"Zwrotnica wymaga modułu skrętu osi.":
                part.WheelStyle==R3WheelStyle.SpikedFork?"Kolce są wizualne.":
                (part.WheelStyle==R3WheelStyle.Monster||part.WheelStyle==R3WheelStyle.LargeOffroad)&&part.FootprintRevision==1?"Potrzebuje więcej miejsca.":
                R3VehicleModel.HasForkMount(part.WheelStyle)?"Mocowanie od góry.":"Kliknij pojazd, aby zamontować.";
            return GarageWheelStats(part)+". "+hint+(R3VehicleModel.HasForkMount(part.WheelStyle)?" U: widok od spodu.":"");
        }

        string SimpleGarageFeedback(R1RunController run)
        {
            var model=run.VehicleModel;
            if(run.MapExitPending)return run.EditorMessage;
            string message=run.Garage.Message??string.Empty;
            // The visible ghost can move onto a blocked cell after a previous
            // successful purchase. Explain that current placement before the
            // player clicks, rather than leaving the old purchase message.
            if(!run.Garage.HasRecentActionMessage&&run.Garage.Tool==R41GarageTool.Mount&&run.Garage.HasTarget&&run.Garage.HasMountSelection)
            {
                string placement=run.Garage.PlacementIssue;
                message=placement??"Można zamontować • LPM — zamontuj część.";
            }
            string reason=model.DeployError;
            if(reason!=null)
            {
                string detail=model.CriticalReadinessMessage;
                // Add the combined missing-gun/missing-drive detail only when
                // it describes the actual first deployment failure. Otherwise
                // disconnected parts or the weapon-type limit could be hidden.
                if(!string.IsNullOrEmpty(detail)&&detail.StartsWith(reason.TrimEnd('.'),System.StringComparison.Ordinal))reason=detail;
                message+="\nStart zablokowany: "+reason;
            }
            else if(model.CreativeMode&&!R3VehicleModel.HasMobility(model.Parts,model.SelectedDrive))
                message+="\nPoligon bez napędu: pojazd może nie jechać.";
            if(run.Technology?.Error!=null)message+="\nBłąd zapisu lub odczytu technologii.";
            return message.TrimStart('\n');
        }

        static string GarageWheelPowerAdvice(float units)
        {
            float firstMotor=(RoboRougelike.R6.R6VehicleDrive.MotorMultiplier(1)-1)*100;
            return $"Podjazd: dołóż silnik lub koła albo zmniejsz masę. T1: +{firstMotor:0}% siły, −{VehicleEnergyState.PassiveReservation:0} maks. energii. Koła muszą dotykać podłoża.";
        }

        static string GarageDriveSummary(R1RunController run)
        {
            if(run?.VehicleModel==null||run.Player==null)return "Dane napędu są niedostępne.";
            var body=run.Player.GetComponent<Rigidbody>();
            if(body==null)return "Dane napędu są niedostępne.";
            if(run.VehicleModel.SelectedDrive!=R3DriveKind.Wheels)
            {
                string name=run.VehicleModel.SelectedDrive==R3DriveKind.Tracks?"Gąsienice":
                    run.VehicleModel.SelectedDrive==R3DriveKind.Omni?"Omni":
                    run.VehicleModel.SelectedDrive==R3DriveKind.Hover?"Hover":"Nieznany";
                return $"Masa: {body.mass:0} kg • Napęd: {name}\nSilniki kół wspomagają wyłącznie napęd kołowy.";
            }
            var drive=run.Player.GetComponent<RoboRougelike.R6.R6VehicleDrive>();
            if(drive==null)return "Dane napędu są niedostępne.";
            float bonus=(RoboRougelike.R6.R6VehicleDrive.MotorMultiplier(drive.ActivePowerUnits)-1)*100;
            return $"Masa: {body.mass:0} kg • Sprawne koła: {drive.Wheels}\n"+
                $"Siła silników kół: +{bonus:0}%\n"+
                GarageWheelPowerAdvice(drive.ActivePowerUnits);
        }

        // Expedition purchases stay explicit; creative selection can supply a free item.
        public bool SelectMountedPartForBuilding(R1RunController run,R3PartState mounted)
        {
            if(run?.Garage==null||run.VehicleModel==null||mounted==null||mounted.Health<=0)return false;
            var entry=BuildCatalogEntries(run.VehicleModel).FirstOrDefault(candidate=>
                InCatalogGroup(mounted,candidate.Kind,candidate.Weapon,candidate.Group));
            if(entry.Name==null){run.Garage.Message=mounted.Kind==R3PartKind.BonusModule?
                "Brak kopii tego bonusu w magazynie.":"Tego elementu nie można wybrać z katalogu.";return false;}
            quickPartCategory=QuickCategoryFor(entry.Kind);quickPartsScroll=Vector2.zero;
            if(!CatalogNameMatches(entry.Name,quickPartSearch))quickPartSearch=string.Empty;
            if(quickOwnedOnly&&!CatalogEntryOwned(run.VehicleModel,entry))quickOwnedOnly=false;
            catalogGroup=entry.Kind;catalogWeapon=entry.Weapon;catalogBonusId=entry.Group;
            var garage=run.Garage;garage.SelectedPart=entry.Kind;garage.SelectedWeapon=entry.Weapon;garage.UseInventory=true;
            var items=CatalogItemsForGroup(run.VehicleModel,entry.Kind,entry.Weapon,entry.Group);
            var matching=items.FirstOrDefault(p=>p.Health>0&&R3VehicleModel.SameMountVariant(p,mounted));
            if(matching==null&&run.CreativeWorkshop)run.AddCreativeEquivalent(mounted,out matching);
            if(matching==null)
            {
                var wreck=items.FirstOrDefault(p=>p.Health<=0&&R3VehicleModel.SameMountVariant(p,mounted));
                if(wreck!=null)
                {
                    garage.ChooseInventory(wreck.Id);garage.CopyMountedRotation(mounted);
                    garage.Message=$"Część T{R3VehicleModel.NormalizedTier(wreck.Tier)} jest zniszczona. Użyj naprawy poniżej.";
                    return true;
                }
            }
            if(matching==null)
            {
                garage.SelectedInventoryId=null;garage.SetTool(R41GarageTool.Mount);
                int tier=R3VehicleModel.NormalizedTier(mounted.Tier);
                garage.Message=items.Any(p=>p.Health<=0&&R3VehicleModel.SameMountVariant(p,mounted))?
                    $"Brak sprawnej części T{tier}. Użyj naprawy poniżej lub wybierz egzemplarz w magazynie.":
                    $"Brak części T{tier} o tych punktach mocowania lub w tym wariancie. Wybierz inny egzemplarz w magazynie.";
                return false;
            }
            garage.ChooseInventory(matching.Id);
            run.Garage.CopyMountedRotation(mounted);
            run.Garage.Message=$"Wybrano część T{R3VehicleModel.NormalizedTier(matching.Tier)}. Kliknij miejsce montażu.";
            return true;
        }

        void SelectQuickPart(R1RunController run,R3PartKind kind,R4WeaponKind weapon,string bonus)
        {
            quickPartCategory=QuickCategoryFor(kind);
            catalogGroup=kind;catalogWeapon=weapon;catalogBonusId=bonus;
            var garage=run.Garage;
            garage.SelectedPart=kind;garage.SelectedWeapon=weapon;garage.UseInventory=true;
            var part=CatalogItemsForGroup(run.VehicleModel,kind,weapon,bonus).FirstOrDefault(p=>p.Health>0);
            if(part==null&&run.CreativeWorkshop&&AddSelectedCatalogPart(run,kind,weapon))
                part=run.VehicleModel.Inventory.LastOrDefault();
            if(part==null)
            {
                var wreck=CatalogItemsForGroup(run.VehicleModel,kind,weapon,bonus).FirstOrDefault(p=>p.Health<=0);
                if(wreck!=null)
                {
                    garage.ChooseInventory(wreck.Id);
                    garage.Message="Część jest zniszczona. Użyj naprawy poniżej, aby ją zamontować.";
                    return;
                }
            }
            if(part!=null){garage.ChooseInventory(part.Id);garage.Message=kind==R3PartKind.BonusModule?
                $"Moduł T{R3VehicleModel.NormalizedTier(part.Tier)}: {BonusInstanceText(part)}. Kliknij, aby zamontować.":kind==R3PartKind.Battery?
                $"Bateria T{R3VehicleModel.NormalizedTier(part.Tier)}: +{VehicleEnergyState.BatteryCapacity*R3VehicleModel.TierEffectUnits(part.Tier):0} maks. energii. Kliknij, aby zamontować.":
                kind==R3PartKind.RepairPulse?
                $"Impuls T{R3VehicleModel.NormalizedTier(part.Tier)}: slot aktywny 1–4, {R3RepairPulse.EnergyCost*R3VehicleModel.TierActivationCost(part.Tier):0} energii, {R3RepairPulse.Duration:0} s. Kliknij, aby zamontować.":
                kind==R3PartKind.DrivePower?$"Silnik T{R3VehicleModel.NormalizedTier(part.Tier)} wspomaga koła; rezerwuje {VehicleEnergyState.PassiveReservation+R3VehicleModel.TierPassiveExtra(part.Tier):0} energii.":
                kind==R3PartKind.EnergyRegenerator?$"Regenerator T{R3VehicleModel.NormalizedTier(part.Tier)}: +{VehicleEnergyState.RegenerationPerModule*R3VehicleModel.TierEffectUnits(part.Tier):0} energii/s, rezerwa {VehicleEnergyState.PassiveReservation+R3VehicleModel.TierPassiveExtra(part.Tier):0}.":
                kind==R3PartKind.SalvageOptimizer?$"Odzysk T{R3VehicleModel.NormalizedTier(part.Tier)}: +{run.VehicleModel.Economy.SalvageOptimizerBonus*R3VehicleModel.TierEffectUnits(part.Tier)*100:0}% złomu i do +{run.VehicleModel.Economy.SalvageOptimizerExtraOffersPerUnit*R3VehicleModel.TierEffectUnits(part.Tier)} ofert. Rezerwa {VehicleEnergyState.PassiveReservation:0} energii.":
                kind==R3PartKind.Overdrive?$"Overdrive T{R3VehicleModel.NormalizedTier(part.Tier)}: +{run.VehicleModel.Economy.OverdriveBonusPerUnit*R3VehicleModel.TierEffectUnits(part.Tier)*100:0}% obrażeń na {run.VehicleModel.Economy.OverdriveDuration:0} s. Slot aktywny 1–4.":
                kind==R3PartKind.RapidFire?$"Rapid Fire T{R3VehicleModel.NormalizedTier(part.Tier)}: −{run.VehicleModel.Economy.RapidFireReductionPerUnit*R3VehicleModel.TierEffectUnits(part.Tier)*100:0}% czasu między strzałami na {run.VehicleModel.Economy.RapidFireDuration:0} s. Slot 1–4.":
                kind==R3PartKind.DoubleShot?$"Double Shot T{R3VehicleModel.NormalizedTier(part.Tier)}: +{run.VehicleModel.Economy.DoubleShotChancePerUnit*R3VehicleModel.TierEffectUnits(part.Tier)*100:0}% szansy. Działa na działa, strzelbę, railgun i rakiety. Rezerwa {VehicleEnergyState.PassiveReservation:0} energii. Limit: 96 pocisków / 32 rakiety na mapie.":
                kind==R3PartKind.DoubleShotOverdrive?$"Double Shot Overdrive T{R3VehicleModel.NormalizedTier(part.Tier)}: +{run.VehicleModel.Economy.DoubleShotOverdriveExtraPerUnit*R3VehicleModel.TierEffectUnits(part.Tier)} na każdy pocisk przez {run.VehicleModel.Economy.DoubleShotOverdriveDuration:0} s. Limit: 96 pocisków / 32 rakiety. Slot 1–4.":
                kind==R3PartKind.TargetedEmp?$"EMP celowany: zakłóca elektronikę wskazanego wroga do {R3EmpDevices.TargetRange:0} m. Zużywa 1 ładunek; bazowo {run.VehicleModel.Economy.DeviceBaseCharges}. Bez obrażeń. Slot 1–4.":
                kind==R3PartKind.AreaEmp?$"EMP obszarowy: zakłóca elektronikę wrogów wokół pojazdu. Zużywa 1 ładunek; bazowo {run.VehicleModel.Economy.DeviceBaseCharges}. Bez obrażeń. Slot 1–4.":
                kind==R3PartKind.ChargeCapacity?$"Pojemność: +{run.VehicleModel.Economy.CapacityChargesPerUnit*R3VehicleModel.TierEffectUnits(part.Tier)} ładunków dla każdego urządzenia. Rezerwa {VehicleEnergyState.PassiveReservation:0} energii.":
                kind==R3PartKind.ChargeRegenerator?$"Regenerator: +{run.VehicleModel.Economy.ChargesPerSecondPerRegenerator*R3VehicleModel.TierEffectUnits(part.Tier):0.##} ładunku/s na urządzenie. Rezerwa {VehicleEnergyState.PassiveReservation:0} energii.":
                kind==R3PartKind.RechargePulse?$"Impuls: +{run.VehicleModel.Economy.RechargePulseChargesPerUnit*R3VehicleModel.TierEffectUnits(part.Tier)} ładunków każdemu sprawnemu urządzeniu. Slot 1–4.":
                kind==R3PartKind.RampDeployer?$"Wyrzutnik ramp: stawia rampę za jadącym pojazdem. Slot Z/X/C/V; 1 ładunek i {run.VehicleModel.Economy.RampEnergyPerCopy*R3VehicleModel.TierActivationCost(part.Tier):0} energii. Więcej kopii poszerza rampę.":
                kind==R3PartKind.WallDeployer?$"Wyrzutnik murków: stawia osłonę za jadącym pojazdem. Slot Z/X/C/V; 1 ładunek i {run.VehicleModel.Economy.WallEnergyPerCopy*R3VehicleModel.TierActivationCost(part.Tier):0} energii.":
                kind==R3PartKind.JumpPadDeployer?$"Wyrzutnik jump padów: stawia wyrzutnię skoku za jadącym pojazdem. Slot Z/X/C/V; 1 ładunek i {run.VehicleModel.Economy.JumpPadEnergyPerCopy*R3VehicleModel.TierActivationCost(part.Tier):0} energii.":
                kind==R3PartKind.BlastTrapDeployer?$"Pułapka wybuchowa: zostaje za pojazdem i atakuje wrogów w pobliżu. 1 ładunek, {run.VehicleModel.Economy.TrapEnergyPerCopy*R3VehicleModel.TierActivationCost(part.Tier):0} energii; slot Z/X/C/V.":
                kind==R3PartKind.EmpTrapDeployer?$"Pułapka EMP: zostaje za pojazdem i zakłóca elektronikę pobliskich wrogów. Bez obrażeń HP; slot Z/X/C/V.":
                kind==R3PartKind.RepelTrapDeployer?$"Pułapka odrzucająca: odpycha pobliskich wrogów bez obrażeń. Slot Z/X/C/V; 1 ładunek i {run.VehicleModel.Economy.TrapEnergyPerCopy*R3VehicleModel.TierActivationCost(part.Tier):0} energii.":
                kind==R3PartKind.DamageTrapDeployer?$"Pułapka obrażeń: tworzy pole raniące wrogów przez krótki czas. Slot Z/X/C/V; 1 ładunek i {run.VehicleModel.Economy.TrapEnergyPerCopy*R3VehicleModel.TierActivationCost(part.Tier):0} energii.":
                kind==R3PartKind.Gun&&weapon==R4WeaponKind.Basic?"Działo podstawowe strzela bez zużycia energii. Kliknij, aby zamontować.":
                kind==R3PartKind.Gun&&weapon==R4WeaponKind.RocketLauncher?"Rakiety ładują się automatycznie po opróżnieniu. Kliknij, aby zamontować.":
                kind==R3PartKind.Armor&&part.ArmorShape==R3ArmorShape.Wedge?"Klin: montaż od spodu lub od tylnej płaskiej ściany. Obracaj Q/E, R/F, Z/X.":
                kind==R3PartKind.Wheel?GarageWheelSelection(part):"Kliknij pojazd, aby zamontować część.";}
            else {garage.SelectedInventoryId=null;garage.SetTool(R41GarageTool.Mount);
                garage.Message=run.CreativeWorkshop?"Ta część jest obecnie niedostępna w poligonie.":
                    kind==R3PartKind.BonusModule?"Brak sprawnego bonusu. Użyj naprawy poniżej.":
                    run.VehicleModel.Inventory.Any(p=>InCatalogGroup(p,kind,weapon,bonus))?"Brak sprawnej części. Użyj naprawy poniżej.":
                    !run.CanBuyTechnology(kind,weapon)?"Zablokowane: pokonaj przeciwnika z tą bronią, aby odkryć technologię.":kind==R3PartKind.Battery?
                    $"Bateria T1: +{VehicleEnergyState.BatteryCapacity:0} maks. energii. Kup część lub otwórz szczegóły.":kind==R3PartKind.RepairPulse?
                    $"Impuls naprawczy: slot aktywny 1–4, {R3RepairPulse.EnergyCost:0} energii, {R3RepairPulse.Duration:0} s. Kup część lub otwórz szczegóły.":
                    kind==R3PartKind.DrivePower?$"Silnik wspomaga koła; zajmuje {VehicleEnergyState.PassiveReservation:0} maks. energii. Kup część lub otwórz szczegóły.":
                    kind==R3PartKind.EnergyRegenerator?$"Regenerator: +{VehicleEnergyState.RegenerationPerModule:0} energii/s, −{VehicleEnergyState.PassiveReservation:0} maks. energii. Kup część lub otwórz szczegóły.":
                    kind==R3PartKind.SalvageOptimizer?$"Odzysk: +{run.VehicleModel.Economy.SalvageOptimizerBonus*100:0}% złomu i do +{run.VehicleModel.Economy.SalvageOptimizerExtraOffersPerUnit} ofert. Rezerwa {VehicleEnergyState.PassiveReservation:0} energii.":
                    kind==R3PartKind.Overdrive?$"Overdrive: +{run.VehicleModel.Economy.OverdriveBonusPerUnit*100:0}% obrażeń na {run.VehicleModel.Economy.OverdriveDuration:0} s za {run.VehicleModel.Economy.OverdriveEnergyPerCopy:0} energii. Slot aktywny 1–4.":
                    kind==R3PartKind.RapidFire?$"Rapid Fire: −{run.VehicleModel.Economy.RapidFireReductionPerUnit*100:0}% czasu między strzałami na {run.VehicleModel.Economy.RapidFireDuration:0} s za {run.VehicleModel.Economy.RapidFireEnergyPerCopy:0} energii. Slot 1–4.":
                    kind==R3PartKind.DoubleShot?$"Double Shot: +{run.VehicleModel.Economy.DoubleShotChancePerUnit*100:0}% szansy. Działa na działa, strzelbę, railgun i rakiety. Rezerwa {VehicleEnergyState.PassiveReservation:0} energii. Limit: 96 pocisków / 32 rakiety na mapie.":
                    kind==R3PartKind.DoubleShotOverdrive?$"Double Shot Overdrive: +{run.VehicleModel.Economy.DoubleShotOverdriveExtraPerUnit} na każdy pocisk przez {run.VehicleModel.Economy.DoubleShotOverdriveDuration:0} s za {run.VehicleModel.Economy.DoubleShotOverdriveEnergyPerCopy:0} energii. Limit: 96 pocisków / 32 rakiety. Slot 1–4.":
                    kind==R3PartKind.TargetedEmp?$"EMP celowany: zakłóca wroga do {R3EmpDevices.TargetRange:0} m; 1 ładunek i {R3EmpDevices.EnergyPerCopy:0} energii. Bazowo {run.VehicleModel.Economy.DeviceBaseCharges} ładunki; slot 1–4.":
                    kind==R3PartKind.AreaEmp?$"EMP obszarowy: zakłóca wrogów w promieniu {R3EmpDevices.AreaRadius:0} m; 1 ładunek i {R3EmpDevices.EnergyPerCopy:0} energii. Bazowo {run.VehicleModel.Economy.DeviceBaseCharges} ładunki; slot 1–4.":
                    kind==R3PartKind.ChargeCapacity?$"Pojemność: +{run.VehicleModel.Economy.CapacityChargesPerUnit} ładunek na urządzenie. Rezerwa {VehicleEnergyState.PassiveReservation:0} energii.":
                    kind==R3PartKind.ChargeRegenerator?$"Regenerator: +{run.VehicleModel.Economy.ChargesPerSecondPerRegenerator:0.##} ładunku/s na urządzenie. Rezerwa {VehicleEnergyState.PassiveReservation:0} energii.":
                    kind==R3PartKind.RechargePulse?$"Impuls: +{run.VehicleModel.Economy.RechargePulseChargesPerUnit} ładunek każdemu sprawnemu urządzeniu za {run.VehicleModel.Economy.RechargePulseEnergyPerCopy:0} energii. Slot 1–4.":
                    kind==R3PartKind.RampDeployer?$"Wyrzutnik ramp: stawia rampę za jadącym pojazdem; 1 ładunek i {run.VehicleModel.Economy.RampEnergyPerCopy:0} energii. Slot Z/X/C/V.":
                    kind==R3PartKind.WallDeployer?$"Wyrzutnik murków: stawia osłonę za jadącym pojazdem; 1 ładunek i {run.VehicleModel.Economy.WallEnergyPerCopy:0} energii. Slot Z/X/C/V.":
                    kind==R3PartKind.JumpPadDeployer?$"Wyrzutnik jump padów: stawia wyrzutnię skoku za jadącym pojazdem; 1 ładunek i {run.VehicleModel.Economy.JumpPadEnergyPerCopy:0} energii. Slot Z/X/C/V.":
                    kind==R3PartKind.BlastTrapDeployer?$"Pułapka wybuchowa: zostaje za pojazdem; 1 ładunek i {run.VehicleModel.Economy.TrapEnergyPerCopy:0} energii. Slot Z/X/C/V.":
                    kind==R3PartKind.EmpTrapDeployer?$"Pułapka EMP: zakłóca wrogów bez obrażeń HP; 1 ładunek i {run.VehicleModel.Economy.TrapEnergyPerCopy:0} energii. Slot Z/X/C/V.":
                    kind==R3PartKind.RepelTrapDeployer?$"Pułapka odrzucająca: odpycha wrogów bez obrażeń HP; 1 ładunek i {run.VehicleModel.Economy.TrapEnergyPerCopy:0} energii. Slot Z/X/C/V.":
                    kind==R3PartKind.DamageTrapDeployer?$"Pułapka obrażeń: tworzy pole raniące wrogów przez krótki czas; 1 ładunek i {run.VehicleModel.Economy.TrapEnergyPerCopy:0} energii. Slot Z/X/C/V.":
                    kind==R3PartKind.Gun&&weapon==R4WeaponKind.Basic?"Działo podstawowe strzela bez zużycia energii. Kup część.":
                    kind==R3PartKind.Gun&&weapon==R4WeaponKind.RocketLauncher?"Rakiety ładują się automatycznie po opróżnieniu. Kup część.":
                    kind==R3PartKind.Armor&&CatalogArmorShape(bonus)==R3ArmorShape.Wedge?"Klin 1×1×1: mocowanie od spodu i z tyłu. Kup część.":
                    kind==R3PartKind.Wheel&&CatalogWheelStyle(bonus)==R3WheelStyle.SwingOffroad?"Koło na wahaczu: 12 kg. Mocowanie od góry.":
                    kind==R3PartKind.Wheel&&CatalogWheelStyle(bonus)==R3WheelStyle.CasterSlick?"Kółko samonastawne: 4 kg, połowa napędu. Mocowanie od góry.":
                    kind==R3PartKind.Wheel&&CatalogWheelStyle(bonus)==R3WheelStyle.SteerOffroad?"Duże skrętne: 16 kg, +15% napędu. Zwrotnica z modułem skrętu osi.":
                    kind==R3PartKind.Wheel&&CatalogWheelStyle(bonus)==R3WheelStyle.SteerRoad?"Koło skrętne: 12 kg, mocowanie od góry. Zwrotnica wymaga modułu skrętu osi.":
                    kind==R3PartKind.Wheel&&CatalogWheelStyle(bonus)==R3WheelStyle.TwinRoad?"Koło bliźniacze: 12 kg, mocowanie od góry, dwie opony.":
                    kind==R3PartKind.Wheel&&CatalogWheelStyle(bonus)==R3WheelStyle.SpikedFork?"Koło z kolcami: 12 kg, mocowanie od góry. Kolce są wizualne.":
                    kind==R3PartKind.Wheel&&CatalogWheelStyle(bonus)==R3WheelStyle.BalloonFork?"Koło balonowe: 16 kg, +15% siły przyczepności. Mocowanie od góry.":
                    kind==R3PartKind.Wheel&&CatalogWheelStyle(bonus)==R3WheelStyle.SmallFork?"Małe koło: 6 kg, −35% siły przyczepności. Mocowanie od góry.":
                    kind==R3PartKind.Wheel&&CatalogWheelStyle(bonus)==R3WheelStyle.Monster?$"Koło monster: 24 kg, +50% przyczepności; obrys {(R3VehicleModel.UseEnlargedEquipment?"3×3":"1 pole")}. Kup część.":
                    kind==R3PartKind.Wheel&&CatalogWheelStyle(bonus)==R3WheelStyle.LargeOffroad?$"Koło terenowe: 18 kg, +25% przyczepności; obrys {(R3VehicleModel.UseEnlargedEquipment?"3×3":"1 pole")}. Kup część.":"Brak sprawnej części w magazynie. Kup nową lub otwórz szczegóły.";}
            if(part!=null&&run.CreativeWorkshop)garage.Message+=" • za darmo.";
        }

        void DrawQuickPartRow(R1RunController run,R3PartKind kind,R4WeaponKind weapon,string bonus,string name,int row,float width)
        {
            bool selected=catalogGroup==kind&&(kind!=R3PartKind.Gun||catalogWeapon==weapon)&&
                (kind!=R3PartKind.BonusModule&&kind!=R3PartKind.Wheel&&kind!=R3PartKind.Armor||catalogBonusId==bonus);
            compactButton.normal.background=selected?buttonPressed:buttonNormal;GUI.backgroundColor=Color.white;
            if(GUI.Button(new Rect(0,row*38,width,34),CatalogGroupCaption(run,kind,weapon,bonus,name),compactButton))
                SelectQuickPart(run,kind,weapon,bonus);
            if(selected)
            {
                var color=GUI.color;GUI.color=new Color(.42f,.94f,.90f);
                GUI.DrawTexture(new Rect(1,row*38+4,3,26),Texture2D.whiteTexture);GUI.color=color;
            }
            compactButton.normal.background=buttonNormal;
        }

        void DrawQuickParts(R1RunController run,Rect area,float rowWidth)
        {
            var model=run.VehicleModel;
            ClearMissingOwnedSelection(run);

            string[] categories={"5 Bloki","6 Koła","7 Broń","8 Moduły"};
            if(quickCategoryButton==null)quickCategoryButton=new GUIStyle(compactButton){fontSize=13,wordWrap=false,padding=new RectOffset(1,1,2,2)};
            float categoryWidth=(area.width-12)/4;
            bool searching=!string.IsNullOrWhiteSpace(quickPartSearch);
            for(int i=0;i<categories.Length;i++)
            {
                quickCategoryButton.normal.background=!searching&&quickPartCategory==i?buttonPressed:buttonNormal;
                if(GUI.Button(new Rect(area.x+i*(categoryWidth+4),area.y,categoryWidth,34),categories[i],quickCategoryButton))
                {
                    BrowseQuickCategory(run,i);
                    GUI.FocusControl(null);GUIUtility.keyboardControl=0;
                }
            }
            GUI.backgroundColor=Color.white;
            var listArea=new Rect(area.x,area.y+40,area.width,area.height-40);
            var entries=BuildFilteredQuickCatalogEntries(model,quickPartCategory,quickOwnedOnly,quickPartSearch);
            float contentHeight=entries.Count==0?145:entries.Count*38;
            quickPartsScroll=GUI.BeginScrollView(listArea,quickPartsScroll,new Rect(0,0,rowWidth,Mathf.Max(listArea.height,contentHeight)));
            for(int row=0;row<entries.Count;row++)
            {var entry=entries[row];DrawQuickPartRow(run,entry.Kind,entry.Weapon,entry.Group,entry.Name,row,rowWidth);}
            if(entries.Count==0)
            {
                GUI.Label(new Rect(0,5,rowWidth,50),searching?"Brak wyników wyszukiwania.":
                    quickOwnedOnly?"Brak posiadanych części w tej kategorii.":"Brak części w tej kategorii.",small);
                float y=55;
                if(searching&&GUI.Button(new Rect(0,y,rowWidth,34),"Wyczyść wyszukiwanie",compactButton))
                {ApplyQuickSearch(run,string.Empty);GUI.FocusControl(null);GUIUtility.keyboardControl=0;}
                if(searching)y+=38;
                if(quickOwnedOnly&&GUI.Button(new Rect(0,y,rowWidth,34),"Pokaż wszystkie części",compactButton))
                    ToggleQuickOwnedOnly(run);
            }
            GUI.EndScrollView();GUI.backgroundColor=Color.white;
        }

        void BuyQuickPart(R1RunController run)
        {
            if(!catalogGroup.HasValue)return;
            int before=run.VehicleModel.Scrap;
            if(AddSelectedCatalogPart(run,catalogGroup.Value,catalogWeapon))
            {
                var item=run.VehicleModel.Inventory.Last();run.Garage.ChooseInventory(item.Id);
                run.Garage.ShowActionMessage(run.CreativeWorkshop?"Dodano część za darmo. Kliknij pojazd, aby ją zamontować.":
                    $"Kupiono {CatalogPurchaseQuantity(run.VehicleModel,catalogGroup.Value)} szt. • koszt {before-run.VehicleModel.Scrap} Z. Kliknij pojazd, aby zamontować część.");
            }
            else run.Garage.ShowActionMessage(PurchaseFailure(run,catalogGroup.Value,catalogWeapon));
        }

        bool AddSelectedCatalogPart(R1RunController run,R3PartKind kind,R4WeaponKind weapon)
        {
            if(kind==R3PartKind.BonusModule&&run.CreativeWorkshop)
            {
                foreach(var bonus in run.Definition.Upgrades)
                {
                    var sample=R3VehicleModel.CreateBonusReward(bonus);
                    if(sample!=null&&CatalogBonusKey(sample)==catalogBonusId)return run.AddCreativeBonus(bonus.Id);
                }
                return false;
            }
            return run.GaragePurchase(kind,weapon,kind==R3PartKind.Wheel?CatalogWheelStyle(catalogBonusId):R3WheelStyle.Offroad,
                kind==R3PartKind.Armor?CatalogArmorShape(catalogBonusId):R3ArmorShape.Cube);
        }

        void DrawSimpleGarage(R1RunController run)
        {
            var viewport=R3GarageView.Viewport;var old=GUI.color;GUI.color=new Color(.04f,.06f,.08f,1);
            float left=viewport.xMin*1280,right=viewport.xMax*1280,top=(1-viewport.yMax)*720,bottom=(1-viewport.yMin)*720;
            GUI.DrawTexture(new Rect(0,0,left,720),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(right,0,1280-right,720),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(left,0,right-left,top),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(left,bottom,right-left,720-bottom),Texture2D.whiteTexture);
            GUI.color=old;
            var garage=run.Garage;var model=run.VehicleModel;
            GUI.enabled=!garageOptionsOpen;
            Panel(new Rect(0,0,1280,76));Panel(new Rect(0,76,270,644));
            GUI.Label(new Rect(14,10,150,34),"GARAŻ",heading);
            GUI.Label(new Rect(14,40,245,36),model.CreativeMode?$"Darmowe części • Energia: {model.Energy.Maximum:0}":$"Złom: {model.Scrap} • {model.ConstructionBudgetUsed}/{model.ConstructionBudgetLimit} pkt\n{model.ConstructionSizeLabel}: {model.PhysicalPartCount} części • Energia: {model.Energy.Maximum:0}",small);
            string[] labels={"1 Montuj","2 Zaznacz","3 Napraw","4 Zdejmij"};
            for(int i=0;i<labels.Length;i++)
            {
                compactButton.normal.background=garage.Tool==(R41GarageTool)i?buttonPressed:buttonNormal;GUI.backgroundColor=Color.white;
                if(GUI.Button(new Rect(284+i*116,15,108,42),labels[i],compactButton))garage.SelectTool((R41GarageTool)i);
                compactButton.normal.background=buttonNormal;
            }
            GUI.backgroundColor=Color.white;
            if(GUI.Button(new Rect(760,15,145,42),"Ustawienia",compactButton))OpenSimpleGarageOptions(0);
            if(GUI.Button(new Rect(913,15,145,42),"Aktywności",compactButton))OpenSimpleGarageOptions(1);
            if(GUI.Button(new Rect(1066,15,195,42),"Dopasuj widok",compactButton))garage.FrameVehicle();
            DrawQuickSearch(run,false);
            DrawQuickOwnedFilter(run,new Rect(130,78,127,25));
            DrawQuickParts(run,QuickPartsArea(false),225);
            DrawQuickStackRepair(run,QuickRepairArea(false));
            var owned=model.Inventory.FirstOrDefault(p=>p.Id==garage.SelectedInventoryId);
            var inspected=garage.Tool!=R41GarageTool.Mount&&garage.HasTarget?model.At(garage.Cell):null;
            GUI.Label(new Rect(12,511,245,43),GarageSelectionSummary(run,inspected,owned),small);
            var inspectedWheel=garage.Tool==R41GarageTool.Inspect?inspected:null;
            if(inspectedWheel!=null&&inspectedWheel.Kind==R3PartKind.Wheel&&!R3VehicleModel.CanSwapWheelAppearance(inspectedWheel.WheelStyle))
                GUI.Label(new Rect(12,560,245,42),GarageWheelStats(inspectedWheel),small);
            else if(inspectedWheel!=null&&inspectedWheel.Kind==R3PartKind.Wheel)
            {
                GUI.enabled=!garageOptionsOpen&&inspectedWheel.Health>0;
                string next=inspectedWheel.WheelStyle==R3WheelStyle.Offroad?"szosowe":"terenowe";
                if(GUI.Button(new Rect(12,560,245,42),"Zmień na: "+next,compactButton))
                {
                    if(run.GarageToggleWheelStyle(inspectedWheel.Cell))
                        garage.Message="Wzór koła: "+(inspectedWheel.WheelStyle==R3WheelStyle.Slick?"szosowe":"terenowe")+". Właściwości jezdne są takie same.";
                }
                GUI.enabled=!garageOptionsOpen;
            }
            else if(catalogGroup.HasValue)
            {
                var kind=catalogGroup.Value;int price=model.Price(kind,catalogWeapon);
                bool unlocked=run.CanBuyTechnology(kind,catalogWeapon);
                GUI.enabled=!garageOptionsOpen&&unlocked&&(model.CreativeMode||kind!=R3PartKind.BonusModule)&&model.Scrap>=price&&(long)model.Inventory.Count+CatalogPurchaseQuantity(model,kind)<=model.AvailableInventoryCapacity;
                if(GUI.Button(new Rect(12,560,120,42),CatalogPurchaseCaption(model,kind,unlocked,price),compactButton))BuyQuickPart(run);
                DrawBuyFive(run,kind,catalogWeapon,new Rect(137,560,120,42));
                GUI.enabled=!garageOptionsOpen;
            }
            if(GUI.Button(new Rect(12,605,245,42),model.CreativeMode?$"Magazyn {model.Inventory.Count} • naprawy":$"Magazyn {model.Inventory.Count}/{R3VehicleModel.InventoryCapacity} • naprawy",compactButton))
            {inventoryOpen=true;catalogOpen=true;stackScroll=Vector2.zero;}
            if(garageShortcutLabel==null)garageShortcutLabel=new GUIStyle(small){fontSize=14,wordWrap=false};
            GUI.Label(new Rect(12,649,245,70),"PPM: zdejmij • ŚPM: wybierz typ\nPPM przeciągnij: widok • U: spód\nQ/E R/F Z/X: obrót • Shift: część\nCtrl+LPM: napraw • Home: widok",garageShortcutLabel);
            string garageFeedback=SimpleGarageFeedback(run);
            DrawGarageNotice(false,garageFeedback,!run.MapEditorSession&&garageFeedback.Contains("Start zablokowany:"));
            GUI.enabled=!garageOptionsOpen&&(run.MapEditorSession||model.CanDeploy);
            if(run.MapEditorSession){if(GUI.Button(new Rect(870,663,185,43),"Wróć do edytora",compactButton))run.ReturnToMapEditorFromGarage();}
            else if(GUI.Button(new Rect(870,663,185,43),"Testuj pojazd",run.SandboxWorkshop?GaragePrimaryButton():compactButton))run.EnterTestRange();
            GUI.enabled=!garageOptionsOpen&&(run.SandboxWorkshop||model.CanDeploy);
            if(GUI.Button(new Rect(1066,663,195,43),run.ReactorPrepared?"Broń reaktora":run.SandboxWorkshop?(run.MapReturnPending?"Potwierdź powrót":"Menu główne"):"Następna trasa",run.SandboxWorkshop?compactButton:GaragePrimaryButton()))
            {if(run.ReactorPrepared)run.ChooseReactorDefense();else if(run.SandboxWorkshop)run.RestartRun();else run.StartNextEncounter();}
            GUI.enabled=true;
            if(garageOptionsOpen)DrawSimpleGarageOptions(run);
        }

        void DrawSimplePortraitGarage(R1RunController run)
        {
            var opening=R3GarageView.PortraitOpening;var color=GUI.color;GUI.color=new Color(.04f,.06f,.08f,1);
            GUI.DrawTexture(new Rect(0,0,768,opening.yMin),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0,opening.yMin,opening.xMin,opening.height),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(opening.xMax,opening.yMin,768-opening.xMax,opening.height),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0,opening.yMax,768,1024-opening.yMax),Texture2D.whiteTexture);GUI.color=color;
            var garage=run.Garage;var model=run.VehicleModel;GUI.enabled=!garageOptionsOpen;
            GUI.Label(new Rect(24,12,185,40),"GARAŻ",heading);
            GUI.Label(new Rect(210,18,530,35),model.CreativeMode?$"{model.Design.Name} • Darmowe części • Energia: {model.Energy.Maximum:0}":$"{model.Design.Name} • {model.Scrap} Z • {model.ConstructionBudgetUsed}/{model.ConstructionBudgetLimit} pkt • Energia: {model.Energy.Maximum:0}",body);
            string[] labels={"1 Montuj","2 Zaznacz","3 Napraw","4 Zdejmij"};
            for(int i=0;i<labels.Length;i++)
            {
                compactButton.normal.background=garage.Tool==(R41GarageTool)i?buttonPressed:buttonNormal;GUI.backgroundColor=Color.white;
                if(GUI.Button(new Rect(24+i*183,584,171,42),labels[i],compactButton))garage.SelectTool((R41GarageTool)i);
                compactButton.normal.background=buttonNormal;
            }
            GUI.backgroundColor=Color.white;
            DrawQuickSearch(run,true);
            DrawQuickOwnedFilter(run,new Rect(240,628,190,27));
            DrawQuickParts(run,QuickPartsArea(true),392);
            DrawQuickStackRepair(run,QuickRepairArea(true));
            var owned=model.Inventory.FirstOrDefault(p=>p.Id==garage.SelectedInventoryId);
            var inspected=garage.Tool!=R41GarageTool.Mount&&garage.HasTarget?model.At(garage.Cell):null;
            GUI.Label(new Rect(458,637,286,50),GarageSelectionSummary(run,inspected,owned),small);
            var inspectedWheel=garage.Tool==R41GarageTool.Inspect?inspected:null;
            if(inspectedWheel!=null&&inspectedWheel.Kind==R3PartKind.Wheel&&!R3VehicleModel.CanSwapWheelAppearance(inspectedWheel.WheelStyle))
                GUI.Label(new Rect(458,690,286,40),GarageWheelStats(inspectedWheel),small);
            else if(inspectedWheel!=null&&inspectedWheel.Kind==R3PartKind.Wheel)
            {
                GUI.enabled=!garageOptionsOpen&&inspectedWheel.Health>0;
                string next=inspectedWheel.WheelStyle==R3WheelStyle.Offroad?"szosowe":"terenowe";
                if(GUI.Button(new Rect(458,690,286,40),"Zmień na: "+next,compactButton))
                {
                    if(run.GarageToggleWheelStyle(inspectedWheel.Cell))
                        garage.Message="Wzór koła: "+(inspectedWheel.WheelStyle==R3WheelStyle.Slick?"szosowe":"terenowe")+". Właściwości jezdne są takie same.";
                }
                GUI.enabled=!garageOptionsOpen;
            }
            else if(catalogGroup.HasValue)
            {
                var kind=catalogGroup.Value;int price=model.Price(kind,catalogWeapon);
                bool unlocked=run.CanBuyTechnology(kind,catalogWeapon);
                GUI.enabled=!garageOptionsOpen&&unlocked&&(model.CreativeMode||kind!=R3PartKind.BonusModule)&&model.Scrap>=price&&(long)model.Inventory.Count+CatalogPurchaseQuantity(model,kind)<=model.AvailableInventoryCapacity;
                if(GUI.Button(new Rect(458,690,140,40),CatalogPurchaseCaption(model,kind,unlocked,price),compactButton))BuyQuickPart(run);
                DrawBuyFive(run,kind,catalogWeapon,new Rect(604,690,140,40));
                GUI.enabled=!garageOptionsOpen;
            }
            if(GUI.Button(new Rect(458,737,286,40),model.CreativeMode?$"Magazyn {model.Inventory.Count} • naprawy":$"Magazyn {model.Inventory.Count}/{R3VehicleModel.InventoryCapacity} • naprawy",compactButton))
            {inventoryOpen=true;catalogOpen=true;stackScroll=Vector2.zero;}
            if(GUI.Button(new Rect(458,784,138,40),"Ustawienia",compactButton))OpenSimpleGarageOptions(0);
            if(GUI.Button(new Rect(606,784,138,40),"Aktywności",compactButton))OpenSimpleGarageOptions(1);
            if(GUI.Button(new Rect(458,831,286,40),"Dopasuj widok",compactButton))garage.FrameVehicle();
            string garageFeedback=SimpleGarageFeedback(run);
            DrawGarageNotice(true,garageFeedback+
                "\nPPM: zdejmij • ŚPM: wybierz typ • przeciągnij PPM: obrót widoku"+
                "\nQ/E R/F Z/X: obrót • Shift: część • Ctrl+LPM: napraw • Home: widok",!run.MapEditorSession&&garageFeedback.Contains("Start zablokowany:"));
            GUI.enabled=!garageOptionsOpen&&(run.MapEditorSession||model.CanDeploy);
            if(run.MapEditorSession){if(GUI.Button(new Rect(24,960,350,44),"Wróć do edytora",compactButton))run.ReturnToMapEditorFromGarage();}
            else if(GUI.Button(new Rect(24,960,350,44),"Testuj pojazd",run.SandboxWorkshop?GaragePrimaryButton():compactButton))run.EnterTestRange();
            GUI.enabled=!garageOptionsOpen&&(run.SandboxWorkshop||model.CanDeploy);
            if(GUI.Button(new Rect(394,960,350,44),run.ReactorPrepared?"Broń reaktora":run.SandboxWorkshop?(run.MapReturnPending?"Potwierdź powrót":"Menu główne"):"Następna trasa",run.SandboxWorkshop?compactButton:GaragePrimaryButton()))
            {if(run.ReactorPrepared)run.ChooseReactorDefense();else if(run.SandboxWorkshop)run.RestartRun();else run.StartNextEncounter();}
            GUI.enabled=true;
            if(garageOptionsOpen)DrawSimplePortraitOptions(run);
        }

        void DrawSimplePortraitOptions(R1RunController run)
        {
            Panel(new Rect(12,90,744,800));
            GUI.Label(new Rect(24,105,540,40),garageOptionsTab==0?"USTAWIENIA POJAZDU":garageOptionsTab==2?"SLOTY":"WYBÓR AKTYWNOŚCI",heading);
            if(GUI.Button(new Rect(606,105,138,40),"Zamknij",compactButton)){garageOptionsOpen=false;return;}
            if(garageOptionsTab==0)
            {
                var matrix=GUI.matrix;
                try{GUI.matrix=matrix*Matrix4x4.Translate(new Vector3(-6,-140,0));DrawMovementGarage(run);}
                finally{GUI.matrix=matrix;}
                var model=run.VehicleModel;
                GUI.Label(new Rect(350,180,380,70),GarageWeaponGroups(model),small);
                if(GUI.Button(new Rect(350,255,380,44),"Konfiguruj sloty",compactButton))garageOptionsTab=2;
                int repair=SimpleGarageRepairQuote(model);GUI.enabled=repair>0||(model.CreativeMode&&model.Parts.Any(p=>p.Health<1));
                if(GUI.Button(new Rect(350,309,380,44),GarageBulkRepairCaption(model,repair),compactButton))
                    RepairFromGarageOptions(run,false);
                int emergency=model.EmergencyRepairCost;GUI.enabled=emergency>0&&model.Scrap>=emergency;
                if(GUI.Button(new Rect(350,363,380,48),GarageEmergencyRepairCaption(emergency,model.Scrap,model.CanDeploy),compactButton))
                    RepairFromGarageOptions(run,true);
                GUI.enabled=true;
                GUI.Label(new Rect(350,425,380,135),GarageDriveSummary(run),small);
                if(!string.IsNullOrEmpty(garageOptionsFeedback))GUI.Label(new Rect(350,575,380,60),garageOptionsFeedback,small);
            }
            else if(garageOptionsTab==2)
            {
                DrawSlotAssignments(run.VehicleModel,24,165,720);
                if(GUI.Button(new Rect(24,510,250,44),"Wróć do ustawień",compactButton))garageOptionsTab=0;
            }
            else
            {
                var matrix=GUI.matrix;
                try{GUI.matrix=matrix*Matrix4x4.Translate(new Vector3(0,-430,0));DrawPortraitActivities(run);}
                finally{GUI.matrix=matrix;}
            }
        }

        void DrawSimpleGarageOptions(R1RunController run)
        {
            Panel(new Rect(280,90,800,545));
            GUI.Label(new Rect(300,105,570,40),garageOptionsTab==0?"USTAWIENIA POJAZDU":garageOptionsTab==2?"SLOTY":"WYBÓR AKTYWNOŚCI",heading);
            if(GUI.Button(new Rect(925,105,135,40),"Zamknij",compactButton)){garageOptionsOpen=false;return;}
            if(garageOptionsTab==0)
            {
                var matrix=GUI.matrix;
                try{GUI.matrix=matrix*Matrix4x4.Translate(new Vector3(310,-140,0));DrawMovementGarage(run);}
                finally{GUI.matrix=matrix;}
                var model=run.VehicleModel;
                GUI.Label(new Rect(680,190,350,70),GarageWeaponGroups(model),small);
                if(GUI.Button(new Rect(680,270,300,40),"Konfiguruj sloty",compactButton))garageOptionsTab=2;
                int repair=SimpleGarageRepairQuote(model);GUI.enabled=repair>0||(model.CreativeMode&&model.Parts.Any(p=>p.Health<1));
                if(GUI.Button(new Rect(680,325,300,40),GarageBulkRepairCaption(model,repair),compactButton))
                    RepairFromGarageOptions(run,false);
                int emergency=model.EmergencyRepairCost;GUI.enabled=emergency>0&&model.Scrap>=emergency;
                if(GUI.Button(new Rect(680,375,300,46),GarageEmergencyRepairCaption(emergency,model.Scrap,model.CanDeploy),compactButton))
                    RepairFromGarageOptions(run,true);
                GUI.enabled=true;
                GUI.Label(new Rect(680,435,350,145),GarageDriveSummary(run),small);
                if(!string.IsNullOrEmpty(garageOptionsFeedback))GUI.Label(new Rect(680,585,350,40),garageOptionsFeedback,small);
            }
            else if(garageOptionsTab==2)
            {
                DrawSlotAssignments(run.VehicleModel,300,165,740);
                if(GUI.Button(new Rect(300,505,250,44),"Wróć do ustawień",compactButton))garageOptionsTab=0;
            }
            else
            {
                var matrix=GUI.matrix;
                try{GUI.matrix=matrix*Matrix4x4.Translate(new Vector3(280,-460,0));DrawPortraitActivities(run);}
                finally{GUI.matrix=matrix;}
            }
        }
    }
}
