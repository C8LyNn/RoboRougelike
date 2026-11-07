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
        Vector2 quickPartsScroll;

        // One click chooses an owned instance; buying remains a separate, explicit action.
        void SelectQuickPart(R1RunController run,R3PartKind kind,R4WeaponKind weapon,string bonus)
        {
            catalogGroup=kind;catalogWeapon=weapon;catalogBonusId=bonus;
            var garage=run.Garage;
            garage.SelectedPart=kind;garage.SelectedWeapon=weapon;garage.UseInventory=true;
            var part=run.VehicleModel.Inventory.Where(p=>InCatalogGroup(p,kind,weapon,bonus)&&p.Health>0)
                .OrderByDescending(p=>p.Health).FirstOrDefault();
            if(part!=null){garage.ChooseInventory(part.Id);garage.Message=kind==R3PartKind.RepairPulse?
                "Kliknij pojazd, aby zamontować. Podczas jazdy V naprawia żywe części: 15 energii, 5 s.":
                kind==R3PartKind.DrivePower?"Silnik wspomaga koła i zajmuje 25 maks. energii. Kliknij pojazd, aby zamontować.":
                kind==R3PartKind.EnergyRegenerator?"Regenerator: +5 energii/s, −25 maks. energii. Kliknij pojazd, aby zamontować.":"Kliknij pojazd, aby zamontować część.";}
            else {garage.SelectedInventoryId=null;garage.SetTool(R41GarageTool.Mount);
                garage.Message=!run.CanBuyTechnology(kind,weapon)?"Zablokowane: pokonaj przeciwnika z tą bronią, aby odkryć technologię.":kind==R3PartKind.RepairPulse?
                    "Impuls naprawczy: V, 15 energii, 5 s. Kup część lub otwórz szczegóły.":
                    kind==R3PartKind.DrivePower?"Silnik wspomaga koła; zajmuje 25 maks. energii. Kup część lub otwórz szczegóły.":
                    kind==R3PartKind.EnergyRegenerator?"Regenerator: +5 energii/s, −25 maks. energii. Kup część lub otwórz szczegóły.":"Brak sprawnej części w magazynie. Kup nową lub otwórz szczegóły.";}
        }

        void DrawQuickPartRow(R1RunController run,R3PartKind kind,R4WeaponKind weapon,string bonus,string name,int row,float width)
        {
            int count=run.VehicleModel.Inventory.Count(p=>InCatalogGroup(p,kind,weapon,bonus));
            bool selected=catalogGroup==kind&&(kind!=R3PartKind.Gun||catalogWeapon==weapon)&&
                (kind!=R3PartKind.BonusModule||catalogBonusId==bonus);
            GUI.backgroundColor=selected?new Color(.3f,.9f,.9f):Color.white;
            bool locked=!run.CanBuyTechnology(kind,weapon);
            if(GUI.Button(new Rect(0,row*38,width,34),$"{name}  × {count}"+(locked?" • blokada":""),compactButton))
                SelectQuickPart(run,kind,weapon,bonus);
        }

        void DrawQuickParts(R1RunController run,Rect area,float rowWidth)
        {
            var model=run.VehicleModel;
            var bonuses=model.Inventory.Where(p=>p.Kind==R3PartKind.BonusModule).Select(p=>p.UpgradeId).Distinct().ToArray();
            int count=shopKinds.Length-1+R4WeaponDefinition.Count+bonuses.Length;
            quickPartsScroll=GUI.BeginScrollView(area,quickPartsScroll,new Rect(0,0,rowWidth,Mathf.Max(area.height,count*38)));
            int row=0;
            for(int i=0;i<shopKinds.Length;i++)
            {var kind=shopKinds[i];if(kind!=R3PartKind.Gun)DrawQuickPartRow(run,kind,R4WeaponKind.Basic,null,shopNames[i],row++,rowWidth);}
            for(int i=0;i<R4WeaponDefinition.Count;i++)
            {var weapon=(R4WeaponKind)i;DrawQuickPartRow(run,R3PartKind.Gun,weapon,null,R4WeaponDefinition.Get(weapon).Name,row++,rowWidth);}
            foreach(var bonus in bonuses)
            {var item=model.Inventory.First(p=>p.Kind==R3PartKind.BonusModule&&p.UpgradeId==bonus);
                DrawQuickPartRow(run,R3PartKind.BonusModule,R4WeaponKind.Basic,bonus,R4WeaponDefinition.PartName(item),row++,rowWidth);}
            GUI.EndScrollView();GUI.backgroundColor=Color.white;
        }

        void BuyQuickPart(R1RunController run)
        {
            if(!catalogGroup.HasValue)return;
            if(run.GaragePurchase(catalogGroup.Value,catalogWeapon))
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
                GUI.backgroundColor=garage.Tool==(R41GarageTool)i?new Color(.3f,.9f,.9f):Color.white;
                if(GUI.Button(new Rect(284+i*116,15,108,42),labels[i],compactButton))garage.SetTool((R41GarageTool)i);
            }
            GUI.backgroundColor=Color.white;
            if(GUI.Button(new Rect(760,15,145,42),"Ustawienia",compactButton)){garageOptionsOpen=true;garageOptionsTab=0;}
            if(GUI.Button(new Rect(913,15,145,42),"Aktywności",compactButton)){garageOptionsOpen=true;garageOptionsTab=1;}
            if(GUI.Button(new Rect(1066,15,195,42),"Dopasuj widok",compactButton))garage.FrameVehicle();
            GUI.Label(new Rect(12,79,240,24),"CZĘŚCI • kliknij, aby wybrać",small);
            DrawQuickParts(run,new Rect(12,105,246,397),225);
            var owned=model.Inventory.FirstOrDefault(p=>p.Id==garage.SelectedInventoryId);
            GUI.Label(new Rect(12,511,245,43),owned!=null?$"Wybrano: {R4WeaponDefinition.PartName(owned)} • HP {owned.Health*100:0}%":
                catalogGroup.HasValue?"Wybierz egzemplarz lub kup część.":"Wybierz część z listy.",small);
            if(catalogGroup.HasValue)
            {
                var kind=catalogGroup.Value;int price=model.Price(kind,catalogWeapon);
                bool unlocked=run.CanBuyTechnology(kind,catalogWeapon);
                GUI.enabled=!garageOptionsOpen&&unlocked&&kind!=R3PartKind.BonusModule&&model.Scrap>=price&&model.Inventory.Count<R3VehicleModel.InventoryCapacity;
                if(GUI.Button(new Rect(12,560,245,42),unlocked?$"Kup — {price} Z":"Pokonaj wroga z tą bronią",compactButton))BuyQuickPart(run);
                GUI.enabled=!garageOptionsOpen;
            }
            if(GUI.Button(new Rect(12,610,245,42),"Egzemplarze i naprawy",compactButton))
            {inventoryOpen=true;catalogOpen=true;stackScroll=Vector2.zero;}
            GUI.Label(new Rect(12,655,245,62),"PPM: widok • kółko: zoom\nShift + LPM: zdejmij • Ctrl + LPM: napraw\nQ/E, R/F, Z/X: obrót części",small);
            string garageFeedback=model.CanDeploy?garage.Message:
                "Możesz budować dalej. " +(model.CriticalReadinessMessage??model.DeployError);
            if(run.Technology?.Error!=null)garageFeedback+="\nBłąd zapisu lub odczytu technologii.";
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
                GUI.backgroundColor=garage.Tool==(R41GarageTool)i?new Color(.3f,.9f,.9f):Color.white;
                if(GUI.Button(new Rect(24+i*183,584,171,42),labels[i],compactButton))garage.SetTool((R41GarageTool)i);
            }
            GUI.backgroundColor=Color.white;
            GUI.Label(new Rect(24,632,420,24),"CZĘŚCI • kliknij, aby wybrać",small);
            DrawQuickParts(run,new Rect(24,658,414,220),392);
            var owned=model.Inventory.FirstOrDefault(p=>p.Id==garage.SelectedInventoryId);
            GUI.Label(new Rect(458,637,286,50),owned!=null?$"{R4WeaponDefinition.PartName(owned)} • HP {owned.Health*100:0}%":
                "Wybierz część z listy.",small);
            if(catalogGroup.HasValue)
            {
                var kind=catalogGroup.Value;int price=model.Price(kind,catalogWeapon);
                bool unlocked=run.CanBuyTechnology(kind,catalogWeapon);
                GUI.enabled=!garageOptionsOpen&&unlocked&&kind!=R3PartKind.BonusModule&&model.Scrap>=price&&model.Inventory.Count<R3VehicleModel.InventoryCapacity;
                if(GUI.Button(new Rect(458,690,286,40),unlocked?$"Kup — {price} Z":"Pokonaj wroga z tą bronią",compactButton))BuyQuickPart(run);
                GUI.enabled=!garageOptionsOpen;
            }
            if(GUI.Button(new Rect(458,737,286,40),"Egzemplarze i naprawy",compactButton))
            {inventoryOpen=true;catalogOpen=true;stackScroll=Vector2.zero;}
            if(GUI.Button(new Rect(458,784,138,40),"Ustawienia",compactButton)){garageOptionsOpen=true;garageOptionsTab=0;}
            if(GUI.Button(new Rect(606,784,138,40),"Aktywności",compactButton)){garageOptionsOpen=true;garageOptionsTab=1;}
            if(GUI.Button(new Rect(458,831,286,40),"Dopasuj widok",compactButton))garage.FrameVehicle();
            string garageFeedback=model.CanDeploy?garage.Message:
                "Możesz budować dalej. "+(model.CriticalReadinessMessage??model.DeployError);
            if(run.Technology?.Error!=null)garageFeedback+=" • błąd zapisu lub odczytu technologii";
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
