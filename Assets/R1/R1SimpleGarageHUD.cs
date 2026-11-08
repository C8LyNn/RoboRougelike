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
        int quickPartCategory;
        GUIStyle quickCategoryButton;
        Vector2 quickPartsScroll;
        static bool QuickCategoryIncludes(int category,R3PartKind kind)=>category==0?kind==R3PartKind.Armor:
            category==1?kind==R3PartKind.Wheel:category==2?kind==R3PartKind.Gun:
            category==3&&kind!=R3PartKind.Armor&&kind!=R3PartKind.Wheel&&kind!=R3PartKind.Gun;

        static string GaragePurchaseCaption(R3PartKind kind,bool unlocked,int price,int scrap,bool full,bool toInventory=false)=>
            kind==R3PartKind.BonusModule?"Tylko z nagród":
            !unlocked?"Pokonaj wroga z tą bronią":full?"Magazyn pełny":
            scrap<price?$"Brakuje {price-scrap} Z":toInventory?$"Kup do magazynu — {price} Z":$"Kup — {price} Z";

        static string GarageRepairCaption(R3PartState part,int price,int scrap)=>
            part.Health>=1?"Część sprawna":price<=0?"Nie można naprawić":
            scrap<price?$"Brakuje {price-scrap} Z":$"Napraw — {price} Z";

        static string GarageInspectionSummary(R3VehicleModel model,R3PartState part)
        {
            int quote=model.RepairCost(part);
            string repair=part.Health>=1?"sprawna":quote>0?$"naprawa: {quote} Z":"naprawa niedostępna";
            return $"{R4WeaponDefinition.PartName(part)} • HP {part.Health*100:0}%\n{repair}";
        }

        static string GarageWheelStats(R3PartState part)=>
            $"{R4WeaponDefinition.WheelName(part.WheelStyle)} T{R3VehicleModel.NormalizedTier(part.Tier)} • {R4WeaponDefinition.PartMass(part):0.#} kg\n"+
            $"Siła {100*R3VehicleModel.WheelTraction(part.WheelStyle)*R3VehicleModel.TierWheelTraction(part.Tier):0}% przy 100% HP";

        static string GarageWheelSelection(R3PartState part)
        {
            string hint=part.WheelStyle==R3WheelStyle.SwingOffroad?"Oś podąża za nierównościami.":
                part.WheelStyle==R3WheelStyle.CasterSlick?"Koło obraca się zgodnie z ruchem.":
                part.WheelStyle==R3WheelStyle.SteerOffroad||part.WheelStyle==R3WheelStyle.SteerRoad?"Zwrotnica wymaga modułu skrętu osi.":
                part.WheelStyle==R3WheelStyle.SpikedFork?"Kolce są wizualne.":
                part.WheelStyle==R3WheelStyle.Monster||part.WheelStyle==R3WheelStyle.LargeOffroad?"Potrzebuje więcej miejsca.":
                R3VehicleModel.HasForkMount(part.WheelStyle)?"Mocowanie od góry.":"Kliknij pojazd, aby zamontować.";
            return GarageWheelStats(part)+". "+hint+(R3VehicleModel.HasForkMount(part.WheelStyle)?" U: widok od spodu.":"");
        }

        string SimpleGarageFeedback(R1RunController run)
        {
            var model=run.VehicleModel;
            string message=run.Garage.Message??string.Empty;
            if(!model.CanDeploy)
                message+="\nStart zablokowany: "+(model.CriticalReadinessMessage??model.DeployError);
            if(run.Technology?.Error!=null)message+="\nBłąd zapisu lub odczytu technologii.";
            return message.TrimStart('\n');
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
            float maxBonus=(RoboRougelike.R6.R6VehicleDrive.MotorMultiplier(100)-1)*100;
            float firstMotor=(RoboRougelike.R6.R6VehicleDrive.MotorMultiplier(1)-1)*100;
            return $"Masa: {body.mass:0} kg • Sprawne koła: {drive.Wheels}\n"+
                $"Siła silników kół: +{bonus:0}% (maks. +{maxBonus:0}%)\n"+
                $"Większa masa utrudnia jazdę. Sprawny silnik T1: +{firstMotor:0}% siły i −{VehicleEnergyState.PassiveReservation:0} maks. energii. Koła muszą dotykać podłoża.";
        }

        // One click chooses an owned instance; buying remains a separate, explicit action.
        void SelectQuickPart(R1RunController run,R3PartKind kind,R4WeaponKind weapon,string bonus)
        {
            catalogGroup=kind;catalogWeapon=weapon;catalogBonusId=bonus;
            var garage=run.Garage;
            garage.SelectedPart=kind;garage.SelectedWeapon=weapon;garage.UseInventory=true;
            var part=run.VehicleModel.Inventory.Where(p=>InCatalogGroup(p,kind,weapon,bonus)&&p.Health>0)
                .OrderByDescending(p=>p.Health).FirstOrDefault();
            if(part!=null){garage.ChooseInventory(part.Id);garage.Message=kind==R3PartKind.Battery?
                $"Bateria T{R3VehicleModel.NormalizedTier(part.Tier)}: +{VehicleEnergyState.BatteryCapacity*R3VehicleModel.TierEffectUnits(part.Tier):0} maks. energii. Kliknij, aby zamontować.":
                kind==R3PartKind.RepairPulse?
                $"Impuls T{R3VehicleModel.NormalizedTier(part.Tier)}: V, {R3RepairPulse.EnergyCost*R3VehicleModel.TierActivationCost(part.Tier):0} energii, 5 s. Kliknij, aby zamontować.":
                kind==R3PartKind.DrivePower?$"Silnik T{R3VehicleModel.NormalizedTier(part.Tier)} wspomaga koła; rezerwuje {VehicleEnergyState.PassiveReservation+R3VehicleModel.TierPassiveExtra(part.Tier):0} energii.":
                kind==R3PartKind.EnergyRegenerator?$"Regenerator T{R3VehicleModel.NormalizedTier(part.Tier)}: +{VehicleEnergyState.RegenerationPerModule*R3VehicleModel.TierEffectUnits(part.Tier):0} energii/s, rezerwa {VehicleEnergyState.PassiveReservation+R3VehicleModel.TierPassiveExtra(part.Tier):0}.":
                kind==R3PartKind.Gun&&weapon==R4WeaponKind.RocketLauncher?"Rakiety ładują się automatycznie po opróżnieniu. Kliknij, aby zamontować.":
                kind==R3PartKind.Armor&&part.ArmorShape==R3ArmorShape.Wedge?"Klin: montaż od spodu lub od tylnej płaskiej ściany. Obracaj Q/E, R/F, Z/X.":
                kind==R3PartKind.Wheel?GarageWheelSelection(part):"Kliknij pojazd, aby zamontować część.";}
            else {garage.SelectedInventoryId=null;garage.SetTool(R41GarageTool.Mount);
                garage.Message=kind==R3PartKind.BonusModule?"Brak sprawnego bonusu. Otwórz egzemplarze i napraw część.":
                    run.VehicleModel.Inventory.Any(p=>InCatalogGroup(p,kind,weapon,bonus))?"Brak sprawnej części. Otwórz egzemplarze i napraw ją.":
                    !run.CanBuyTechnology(kind,weapon)?"Zablokowane: pokonaj przeciwnika z tą bronią, aby odkryć technologię.":kind==R3PartKind.Battery?
                    $"Bateria T1: +{VehicleEnergyState.BatteryCapacity:0} maks. energii. Kup część lub otwórz szczegóły.":kind==R3PartKind.RepairPulse?
                    "Impuls naprawczy: V, 15 energii, 5 s. Kup część lub otwórz szczegóły.":
                    kind==R3PartKind.DrivePower?"Silnik wspomaga koła; zajmuje 25 maks. energii. Kup część lub otwórz szczegóły.":
                    kind==R3PartKind.EnergyRegenerator?"Regenerator: +5 energii/s, −25 maks. energii. Kup część lub otwórz szczegóły.":
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
                    kind==R3PartKind.Wheel&&CatalogWheelStyle(bonus)==R3WheelStyle.Monster?"Koło monster XL: 24 kg, +50% siły przyczepności. Kup część.":
                    kind==R3PartKind.Wheel&&CatalogWheelStyle(bonus)==R3WheelStyle.LargeOffroad?"Duże koło: 18 kg, +25% siły przyczepności. Kup część.":"Brak sprawnej części w magazynie. Kup nową lub otwórz szczegóły.";}
        }

        void DrawQuickPartRow(R1RunController run,R3PartKind kind,R4WeaponKind weapon,string bonus,string name,int row,float width)
        {
            int count=run.VehicleModel.Inventory.Count(p=>InCatalogGroup(p,kind,weapon,bonus));
            bool selected=catalogGroup==kind&&(kind!=R3PartKind.Gun||catalogWeapon==weapon)&&
                (kind!=R3PartKind.BonusModule&&kind!=R3PartKind.Wheel&&kind!=R3PartKind.Armor||catalogBonusId==bonus);
            compactButton.normal.background=selected?buttonPressed:buttonNormal;GUI.backgroundColor=Color.white;
            bool locked=!run.CanBuyTechnology(kind,weapon);
            if(GUI.Button(new Rect(0,row*38,width,34),$"{name}  × {count}"+(locked?" • blokada":""),compactButton))
                SelectQuickPart(run,kind,weapon,bonus);
            compactButton.normal.background=buttonNormal;
        }

        void DrawQuickParts(R1RunController run,Rect area,float rowWidth)
        {
            var model=run.VehicleModel;
            var bonuses=model.Inventory.Where(p=>p.Kind==R3PartKind.BonusModule).Select(p=>p.UpgradeId).Distinct().ToArray();
            string[] categories={"Bloki","Koła","Broń","Moduły"};
            if(quickCategoryButton==null)quickCategoryButton=new GUIStyle(compactButton){fontSize=13,wordWrap=false,padding=new RectOffset(1,1,2,2)};
            float categoryWidth=(area.width-12)/4;
            for(int i=0;i<categories.Length;i++)
            {
                quickCategoryButton.normal.background=quickPartCategory==i?buttonPressed:buttonNormal;
                if(GUI.Button(new Rect(area.x+i*(categoryWidth+4),area.y,categoryWidth,34),categories[i],quickCategoryButton))
                {quickPartCategory=i;quickPartsScroll=Vector2.zero;}
            }
            GUI.backgroundColor=Color.white;
            var listArea=new Rect(area.x,area.y+40,area.width,area.height-40);
            int count=quickPartCategory==0?7:quickPartCategory==1?12:quickPartCategory==2?R4WeaponDefinition.Count:shopKinds.Length-3+bonuses.Length;
            quickPartsScroll=GUI.BeginScrollView(listArea,quickPartsScroll,new Rect(0,0,rowWidth,Mathf.Max(listArea.height,count*38)));
            int row=0;
            for(int i=0;i<shopKinds.Length;i++)
            {var kind=shopKinds[i];if(kind==R3PartKind.Gun||!QuickCategoryIncludes(quickPartCategory,kind))continue;
                DrawQuickPartRow(run,kind,R4WeaponKind.Basic,null,kind==R3PartKind.Wheel?"Koło terenowe":shopNames[i],row++,rowWidth);
                if(kind==R3PartKind.Armor)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,WedgeArmorGroup,"Pancerz • klin",row++,rowWidth);
                if(kind==R3PartKind.Armor)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,HalfArmorGroup,"Pancerz • półblok",row++,rowWidth);
                if(kind==R3PartKind.Armor)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,PyramidArmorGroup,"Pancerz • piramida",row++,rowWidth);
                if(kind==R3PartKind.Armor)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,RoundArmorGroup,"Pancerz • zaokrąglenie",row++,rowWidth);
                if(kind==R3PartKind.Armor)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,HalfWedgeArmorGroup,"Pancerz • półklin",row++,rowWidth);
                if(kind==R3PartKind.Armor)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,DomeArmorGroup,"Pancerz • kopuła",row++,rowWidth);
                if(kind==R3PartKind.Wheel)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,SlickWheelGroup,"Koło szosowe",row++,rowWidth);
                if(kind==R3PartKind.Wheel)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,LargeWheelGroup,"Duże koło terenowe",row++,rowWidth);
                if(kind==R3PartKind.Wheel)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,MonsterWheelGroup,"Koło monster XL",row++,rowWidth);
                if(kind==R3PartKind.Wheel)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,SmallWheelGroup,"Małe koło drogowe",row++,rowWidth);
                if(kind==R3PartKind.Wheel)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,BalloonWheelGroup,"Koło balonowe",row++,rowWidth);
                if(kind==R3PartKind.Wheel)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,SpikedWheelGroup,"Koło z kolcami",row++,rowWidth);
                if(kind==R3PartKind.Wheel)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,TwinWheelGroup,"Koło bliźniacze",row++,rowWidth);
                if(kind==R3PartKind.Wheel)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,SteerWheelGroup,"Koło skrętne drogowe",row++,rowWidth);
                if(kind==R3PartKind.Wheel)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,LargeSteerWheelGroup,"Duże koło skrętne",row++,rowWidth);
                if(kind==R3PartKind.Wheel)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,CasterWheelGroup,"Kółko samonastawne",row++,rowWidth);
                if(kind==R3PartKind.Wheel)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,SwingWheelGroup,"Koło na wahaczu",row++,rowWidth);}
            if(QuickCategoryIncludes(quickPartCategory,R3PartKind.Gun))for(int i=0;i<R4WeaponDefinition.Count;i++)
            {var weapon=(R4WeaponKind)i;DrawQuickPartRow(run,R3PartKind.Gun,weapon,null,R4WeaponDefinition.Get(weapon).Name,row++,rowWidth);}
            if(QuickCategoryIncludes(quickPartCategory,R3PartKind.BonusModule))foreach(var bonus in bonuses)
            {var item=model.Inventory.First(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeId==bonus);
                DrawQuickPartRow(run,R3PartKind.BonusModule,R4WeaponKind.Basic,bonus,R4WeaponDefinition.PartName(item),row++,rowWidth);}
            GUI.EndScrollView();GUI.backgroundColor=Color.white;
        }

        void BuyQuickPart(R1RunController run)
        {
            if(!catalogGroup.HasValue)return;
            if(run.GaragePurchase(catalogGroup.Value,catalogWeapon,catalogGroup.Value==R3PartKind.Wheel?CatalogWheelStyle(catalogBonusId):R3WheelStyle.Offroad,
                catalogGroup.Value==R3PartKind.Armor?CatalogArmorShape(catalogBonusId):R3ArmorShape.Cube))
            {var item=run.VehicleModel.Inventory.Last();run.Garage.ChooseInventory(item.Id);run.Garage.Message="Kupiono część. Kliknij pojazd, aby ją zamontować.";}
            else run.Garage.Message="Nie udało się kupić części.";
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
            GUI.Label(new Rect(14,43,245,25),$"Złom: {model.Scrap} • {model.Design.Name}",small);
            string[] labels={"Montuj","Zaznacz","Napraw","Zdejmij"};
            for(int i=0;i<labels.Length;i++)
            {
                compactButton.normal.background=garage.Tool==(R41GarageTool)i?buttonPressed:buttonNormal;GUI.backgroundColor=Color.white;
                if(GUI.Button(new Rect(284+i*116,15,108,42),labels[i],compactButton))garage.SetTool((R41GarageTool)i);
                compactButton.normal.background=buttonNormal;
            }
            GUI.backgroundColor=Color.white;
            if(GUI.Button(new Rect(760,15,145,42),"Ustawienia",compactButton)){garageOptionsOpen=true;garageOptionsTab=0;}
            if(GUI.Button(new Rect(913,15,145,42),"Aktywności",compactButton)){garageOptionsOpen=true;garageOptionsTab=1;}
            if(GUI.Button(new Rect(1066,15,195,42),"Dopasuj widok",compactButton))garage.FrameVehicle();
            GUI.Label(new Rect(12,79,240,24),"CZĘŚCI • kliknij, aby wybrać",small);
            DrawQuickParts(run,new Rect(12,105,246,397),225);
            var owned=model.Inventory.FirstOrDefault(p=>p.Id==garage.SelectedInventoryId);
            var inspected=garage.Tool!=R41GarageTool.Mount&&garage.HasTarget?model.At(garage.Cell):null;
            GUI.Label(new Rect(12,511,245,43),inspected!=null?GarageInspectionSummary(model,inspected):owned!=null?$"Wybrano: {R4WeaponDefinition.PartName(owned)} • HP {owned.Health*100:0}%":
                catalogGroup.HasValue?"Wybierz egzemplarz lub kup część.":"Wybierz część z listy.",small);
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
                GUI.enabled=!garageOptionsOpen&&unlocked&&kind!=R3PartKind.BonusModule&&model.Scrap>=price&&model.Inventory.Count<R3VehicleModel.InventoryCapacity;
                if(GUI.Button(new Rect(12,560,245,42),GaragePurchaseCaption(kind,unlocked,price,model.Scrap,model.Inventory.Count>=R3VehicleModel.InventoryCapacity),compactButton))BuyQuickPart(run);
                GUI.enabled=!garageOptionsOpen;
            }
            if(GUI.Button(new Rect(12,610,245,42),"Egzemplarze i naprawy",compactButton))
            {inventoryOpen=true;catalogOpen=true;stackScroll=Vector2.zero;}
            GUI.Label(new Rect(12,655,245,62),"PPM/kółko: widok • U: spód\nShift/Ctrl + LPM: zdejmij/napraw\nQ/E, R/F, Z/X: obrót części",small);
            string garageFeedback=SimpleGarageFeedback(run);
            GUI.Label(new Rect(286,655,570,58),garageFeedback,small);
            GUI.enabled=!garageOptionsOpen&&(run.MapEditorSession||model.CanDeploy);
            if(run.MapEditorSession){if(GUI.Button(new Rect(870,663,185,43),"Wróć do edytora",compactButton))run.ReturnToMapEditorFromGarage();}
            else if(GUI.Button(new Rect(870,663,185,43),"Testuj pojazd",compactButton))run.EnterTestRange();
            GUI.enabled=!garageOptionsOpen&&(run.SandboxWorkshop||model.CanDeploy);
            if(GUI.Button(new Rect(1066,663,195,43),run.ReactorPrepared?"Broń reaktora":run.SandboxWorkshop?"Menu główne":"Następna trasa",compactButton))
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
            GUI.Label(new Rect(210,18,530,35),$"{model.Design.Name} • Złom: {model.Scrap}",body);
            string[] labels={"Montuj","Zaznacz","Napraw","Zdejmij"};
            for(int i=0;i<labels.Length;i++)
            {
                compactButton.normal.background=garage.Tool==(R41GarageTool)i?buttonPressed:buttonNormal;GUI.backgroundColor=Color.white;
                if(GUI.Button(new Rect(24+i*183,584,171,42),labels[i],compactButton))garage.SetTool((R41GarageTool)i);
                compactButton.normal.background=buttonNormal;
            }
            GUI.backgroundColor=Color.white;
            GUI.Label(new Rect(24,632,420,24),"CZĘŚCI • kliknij, aby wybrać",small);
            DrawQuickParts(run,new Rect(24,658,414,220),392);
            var owned=model.Inventory.FirstOrDefault(p=>p.Id==garage.SelectedInventoryId);
            var inspected=garage.Tool!=R41GarageTool.Mount&&garage.HasTarget?model.At(garage.Cell):null;
            GUI.Label(new Rect(458,637,286,50),inspected!=null?GarageInspectionSummary(model,inspected):owned!=null?$"{R4WeaponDefinition.PartName(owned)} • HP {owned.Health*100:0}%":
                "Wybierz część z listy.",small);
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
                GUI.enabled=!garageOptionsOpen&&unlocked&&kind!=R3PartKind.BonusModule&&model.Scrap>=price&&model.Inventory.Count<R3VehicleModel.InventoryCapacity;
                if(GUI.Button(new Rect(458,690,286,40),GaragePurchaseCaption(kind,unlocked,price,model.Scrap,model.Inventory.Count>=R3VehicleModel.InventoryCapacity),compactButton))BuyQuickPart(run);
                GUI.enabled=!garageOptionsOpen;
            }
            if(GUI.Button(new Rect(458,737,286,40),"Egzemplarze i naprawy",compactButton))
            {inventoryOpen=true;catalogOpen=true;stackScroll=Vector2.zero;}
            if(GUI.Button(new Rect(458,784,138,40),"Ustawienia",compactButton)){garageOptionsOpen=true;garageOptionsTab=0;}
            if(GUI.Button(new Rect(606,784,138,40),"Aktywności",compactButton)){garageOptionsOpen=true;garageOptionsTab=1;}
            if(GUI.Button(new Rect(458,831,286,40),"Dopasuj widok",compactButton))garage.FrameVehicle();
            string garageFeedback=SimpleGarageFeedback(run);
            GUI.Label(new Rect(24,884,720,55),garageFeedback+
                "\nShift + LPM: zdejmij • Ctrl + LPM: napraw",small);
            GUI.enabled=!garageOptionsOpen&&(run.MapEditorSession||model.CanDeploy);
            if(run.MapEditorSession){if(GUI.Button(new Rect(24,960,350,44),"Wróć do edytora",compactButton))run.ReturnToMapEditorFromGarage();}
            else if(GUI.Button(new Rect(24,960,350,44),"Testuj pojazd",compactButton))run.EnterTestRange();
            GUI.enabled=!garageOptionsOpen&&(run.SandboxWorkshop||model.CanDeploy);
            if(GUI.Button(new Rect(394,960,350,44),run.ReactorPrepared?"Broń reaktora":run.SandboxWorkshop?"Menu główne":"Następna trasa",compactButton))
            {if(run.ReactorPrepared)run.ChooseReactorDefense();else if(run.SandboxWorkshop)run.RestartRun();else run.StartNextEncounter();}
            GUI.enabled=true;
            if(garageOptionsOpen)DrawSimplePortraitOptions(run);
        }

        void DrawSimplePortraitOptions(R1RunController run)
        {
            Panel(new Rect(12,90,744,800));
            GUI.Label(new Rect(24,105,540,40),garageOptionsTab==0?"USTAWIENIA POJAZDU":"WYBÓR AKTYWNOŚCI",heading);
            if(GUI.Button(new Rect(606,105,138,40),"Zamknij",compactButton)){garageOptionsOpen=false;return;}
            if(garageOptionsTab==0)
            {
                var matrix=GUI.matrix;
                try{GUI.matrix=matrix*Matrix4x4.Translate(new Vector3(-6,-140,0));DrawMovementGarage(run);}
                finally{GUI.matrix=matrix;}
                var model=run.VehicleModel;
                GUI.Label(new Rect(350,180,380,70),"A / LPM: "+WeaponGroupName(model.WeaponGroupA)+"\nB / PPM: "+WeaponGroupName(model.WeaponGroupB),small);
                if(GUI.Button(new Rect(350,255,380,44),"Zamień grupy broni",compactButton))model.SwapWeaponGroups();
                int repair=model.AffordableRepairCost;GUI.enabled=repair>0;
                if(GUI.Button(new Rect(350,309,380,44),$"Napraw możliwe — {repair} Z",compactButton))
                    run.Garage.Message=run.GarageRepairAffordable()?"Naprawiono dostępne części.":"Brak środków lub części do naprawy.";
                int emergency=model.EmergencyRepairCost;GUI.enabled=emergency>0&&model.Scrap>=emergency;
                if(GUI.Button(new Rect(350,363,380,48),$"Odbudowa awaryjna — {emergency} Z",compactButton))
                    run.Garage.Message=run.GarageEmergencyRepair()?"Odbudowano niezbędne moduły.":"Nie można odbudować części.";
                GUI.enabled=true;
                GUI.Label(new Rect(350,425,380,135),GarageDriveSummary(run),small);
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
            GUI.Label(new Rect(300,105,570,40),garageOptionsTab==0?"USTAWIENIA POJAZDU":"WYBÓR AKTYWNOŚCI",heading);
            if(GUI.Button(new Rect(925,105,135,40),"Zamknij",compactButton)){garageOptionsOpen=false;return;}
            if(garageOptionsTab==0)
            {
                var matrix=GUI.matrix;
                try{GUI.matrix=matrix*Matrix4x4.Translate(new Vector3(310,-140,0));DrawMovementGarage(run);}
                finally{GUI.matrix=matrix;}
                var model=run.VehicleModel;
                GUI.Label(new Rect(680,190,350,70),"A / LPM: "+WeaponGroupName(model.WeaponGroupA)+"\nB / PPM: "+WeaponGroupName(model.WeaponGroupB),small);
                if(GUI.Button(new Rect(680,270,300,40),"Zamień grupy broni",compactButton))model.SwapWeaponGroups();
                int repair=model.AffordableRepairCost;GUI.enabled=repair>0;
                if(GUI.Button(new Rect(680,325,300,40),$"Napraw możliwe — {repair} Z",compactButton))
                    run.Garage.Message=run.GarageRepairAffordable()?"Naprawiono dostępne części.":"Brak środków lub części do naprawy.";
                int emergency=model.EmergencyRepairCost;GUI.enabled=emergency>0&&model.Scrap>=emergency;
                if(GUI.Button(new Rect(680,375,300,46),$"Odbudowa awaryjna — {emergency} Z",compactButton))
                    run.Garage.Message=run.GarageEmergencyRepair()?"Odbudowano niezbędne moduły.":"Nie można odbudować części.";
                GUI.enabled=true;
                GUI.Label(new Rect(680,435,350,145),GarageDriveSummary(run),small);
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
