using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4_1;
using RoboRougelike.MapSystemR4;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        int portraitGarageTab;
        string activitySaveFeedback;
        static string ActivitySaveResult(bool saved,string previous,string current)=>saved?
            (string.IsNullOrEmpty(current)?"Zapisano wyprawę.":current):
            (!string.IsNullOrEmpty(current)&&(current!=previous||current.StartsWith("Nie zapisano wyprawy:",System.StringComparison.Ordinal))?
                current:"Nie udało się zapisać wyprawy.");
        static string ActivityReadinessHint(bool canDeploy,string reason,R3DriveKind drive)=>!canDeploy?
            "Start zablokowany: "+(string.IsNullOrWhiteSpace(reason)?"Sprawdź konstrukcję.":reason):
            drive!=R3DriveKind.Wheels?"Wyścig, tryby drogowe i generowana arena wymagają kół.":null;
        string ActivityContextHint(R1RunController run)
        {
            var model=run.VehicleModel;
            if(!model.CanDeploy)
                return ActivityReadinessHint(false,model.CriticalReadinessMessage??model.DeployError,model.SelectedDrive);
            if(run.Progress.EncounterIndex>=run.Progress.TotalEncounters-1)
                return "Finał: dodatkowe aktywności są niedostępne. Wybierz następną trasę.";
            string hint=ActivityReadinessHint(true,null,model.SelectedDrive);
            var offers=run.RouteOffers;
            if(offers.Length>0&&offers[0].SpawnCount>3)
                hint=(hint==null?"":hint+"\n")+"Areny dodatkowe obsługują do 3 przeciwników na tej trasie.";
            return hint;
        }
        static string ActivityLaunchResult(bool started,string detail)=>started?null:
            string.IsNullOrWhiteSpace(detail)?"Nie udało się uruchomić aktywności.":detail;
        void AttemptGarageActivity(R1RunController run,System.Func<bool> start)
        {
            activitySaveFeedback=null;
            var garage=run.Garage;
            if(garage!=null)garage.Message=null;
            bool started=start();
            activitySaveFeedback=ActivityLaunchResult(started,garage!=null?garage.Message:null);
            if(!started&&garage!=null)garage.Message=activitySaveFeedback;
        }

        void DrawPortraitGarage(R1RunController run)
        {
            var opening=R3GarageView.PortraitOpening;
            var color=GUI.color;GUI.color=new Color(.04f,.06f,.08f,1);
            GUI.DrawTexture(new Rect(0,0,768,opening.yMin),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0,opening.yMin,opening.xMin,opening.height),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(opening.xMax,opening.yMin,768-opening.xMax,opening.height),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0,opening.yMax,768,1024-opening.yMax),Texture2D.whiteTexture);
            GUI.color=color;
            var garage=run.Garage;var model=run.VehicleModel;var selected=model.At(garage.Cell);
            GUI.Label(new Rect(24,12,185,40),"GARAŻ",heading);
            var energy=run.Player!=null?run.Player.GetComponent<VehicleEnergyBank>():null;
            GUI.Label(new Rect(210,14,540,52),$"{model.Design.Name} • Złom: {model.Scrap} • Punkty: {model.ConstructionBudgetUsed}/{model.ConstructionBudgetLimit}"+
                (energy!=null?$"\nEnergia: {energy.Current:0}/{energy.Maximum:0}":""),small);
            string[] tools={"Montuj","Zaznacz","Napraw","Zdejmij"};
            for(int i=0;i<tools.Length;i++)
            {
                GUI.backgroundColor=garage.Tool==(R41GarageTool)i?new Color(.3f,.9f,.9f):Color.white;
                if(GUI.Button(new Rect(24+i*183,505,171,44),tools[i],compactButton))garage.SelectTool((R41GarageTool)i);
            }
            GUI.backgroundColor=Color.white;
            string[] tabs={"CZĘŚCI I WARSZTAT","NAPĘD I MODUŁY","AKTYWNOŚCI"};
            for(int i=0;i<tabs.Length;i++)
            {
                GUI.backgroundColor=portraitGarageTab==i?new Color(.3f,.9f,.9f):Color.white;
                if(GUI.Button(new Rect(24+i*244,558,232,42),tabs[i],compactButton))
                {if(portraitGarageTab!=i)activitySaveFeedback=null;portraitGarageTab=i;}
            }
            GUI.backgroundColor=Color.white;
            if(portraitGarageTab==0)DrawPortraitWorkshop(run,selected);
            else if(portraitGarageTab==1)
            {
                // Reuse the actual movement and suspension controls, including all module-specific options.
                var matrix=GUI.matrix;
                try{GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(-18,178,0),Quaternion.identity,Vector3.one*1.35f);DrawMovementGarage(run);}
                finally{GUI.matrix=matrix;}
                GUI.Label(new Rect(430,620,300,180),"Wybierz moduł w katalogu, aby zobaczyć jego ustawienia.\n\nZmiana napędu wymaga zamontowanych, sprawnych części.",small);
                if(GUI.Button(new Rect(430,805,300,44),"Otwórz katalog części",compactButton))catalogOpen=true;
            }
            else DrawPortraitActivities(run);
            GUI.Label(new Rect(24,906,720,48),model.CanDeploy?garage.Message:
                "Możesz budować dalej; aktywność niedostępna. " +(model.CriticalReadinessMessage??model.DeployError),small);
            GUI.enabled=run.MapEditorSession||model.CanDeploy;
            if(run.MapEditorSession){if(GUI.Button(new Rect(24,960,350,44),"Wróć do edytora map",compactButton))run.ReturnToMapEditorFromGarage();}
            else if(GUI.Button(new Rect(24,960,350,44),"Testuj na poligonie",compactButton))run.EnterTestRange();
            GUI.enabled=run.SandboxWorkshop||model.CanDeploy;
            if(GUI.Button(new Rect(394,960,350,44),run.ReactorPrepared?"Broń reaktora":run.SandboxWorkshop?"Menu główne":"Następna trasa",compactButton))
            {if(run.ReactorPrepared)run.ChooseReactorDefense();else if(run.SandboxWorkshop)run.RestartRun();else run.StartNextEncounter();}
            GUI.enabled=true;
        }

        void DrawPortraitWorkshop(R1RunController run,R3PartState selected)
        {
            var garage=run.Garage;var model=run.VehicleModel;
            if(GUI.Button(new Rect(24,612,350,48),"KATALOG CZĘŚCI I MAGAZYN",compactButton))catalogOpen=true;
            GUI.Label(new Rect(24,667,350,48),garage.Tool==R41GarageTool.Mount?garage.PlacementIssue??"Najedź na pojazd i kliknij zielone widmo.":"Wybierz część na pojeździe.",small);
            GUI.enabled=garage.Tool==R41GarageTool.Mount;
            if(GUI.Button(new Rect(24,718,110,38),"Q / E  Y",compactButton))garage.RotateSelected(1,1);
            if(GUI.Button(new Rect(141,718,110,38),"R / F  X",compactButton))garage.RotateSelected(0,1);
            if(GUI.Button(new Rect(258,718,116,38),"Z / X  Z",compactButton))garage.RotateSelected(2,1);
            GUI.enabled=true;
            GUI.Label(new Rect(24,762,350,30),$"Obrót X {garage.SelectedRotation.x*90}° · Y {garage.SelectedRotation.y*90}° · Z {garage.SelectedRotation.z*90}°",small);
            if(GUI.Button(new Rect(24,798,82,40),"Obrót ←",compactButton))garage.Orbit(-25);
            if(GUI.Button(new Rect(111,798,82,40),"Obrót →",compactButton))garage.Orbit(25);
            if(GUI.Button(new Rect(198,798,82,40),"Widok ↑",compactButton))garage.Tilt(10);
            if(GUI.Button(new Rect(285,798,89,40),"Widok ↓",compactButton))garage.Tilt(-10);
            if(GUI.Button(new Rect(24,846,110,40),"Zoom +",compactButton))garage.Zoom(-1);
            if(GUI.Button(new Rect(141,846,110,40),"Zoom −",compactButton))garage.Zoom(1);
            if(GUI.Button(new Rect(258,846,116,40),"Dopasuj",compactButton))garage.FrameVehicle();
            GUI.Label(new Rect(394,612,350,76),$"Komórka: {garage.Cell} • {PartName(selected?.Kind??garage.SelectedPart)}\n"+(selected==null?"Wybierz część.":$"HP: {selected.Health*100:0}% • naprawa {model.RepairCost(selected)} Z"),small);
            GUI.enabled=selected!=null&&model.RepairCost(selected)>0&&model.Scrap>=model.RepairCost(selected);
            if(GUI.Button(new Rect(394,686,350,42),"Napraw wybraną część",compactButton))garage.RepairPartAt(garage.Cell);
            GUI.enabled=selected!=null;
            if(GUI.Button(new Rect(394,734,350,42),"Zdejmij część",compactButton))garage.RemovePartAt(garage.Cell);
            int bulk=model.AffordableRepairCost;GUI.enabled=bulk>0;
            if(GUI.Button(new Rect(394,782,350,42),$"Napraw możliwe — {bulk} Z",compactButton))garage.Message=run.GarageRepairAffordable()?"Naprawiono dostępne części.":"Nie można naprawić części.";
            int emergency=model.EmergencyRepairCost;GUI.enabled=emergency>0&&model.Scrap>=emergency;
            if(GUI.Button(new Rect(394,826,350,38),$"Odbudowa awaryjna — {emergency} Z",compactButton))garage.Message=run.GarageEmergencyRepair()?"Odbudowano niezbędne moduły.":"Nie można odbudować części.";
            GUI.enabled=true;
            GUI.Label(new Rect(394,868,250,36),"A / LPM: "+WeaponGroupName(model.WeaponGroupA)+"\nB / PPM: "+WeaponGroupName(model.WeaponGroupB),small);
            if(GUI.Button(new Rect(650,875,94,28),"Zamień",compactButton))model.SwapWeaponGroups();
        }

        void DrawPortraitActivities(R1RunController run)
        {
            if(run.SandboxWorkshop){GUI.Label(new Rect(24,620,700,50),"Warsztat poligonu • wybierz Menu główne poniżej, aby wrócić.",body);return;}
            if(run.ReactorPrepared){GUI.Label(new Rect(24,620,700,50),"Obrona reaktora: przygotuj pojazd do trzech fal.",body);return;}
            GUI.enabled=run.CanChooseGeneratedArena;
            if(GUI.Button(new Rect(24,620,350,50),"Generowana arena • seed "+run.RequestedSeed,compactButton))AttemptGarageActivity(run,run.ChooseGeneratedArena);
            GUI.enabled=run.CanChooseReactorDefense;
            if(GUI.Button(new Rect(394,620,350,50),"Obrona reaktora",compactButton))AttemptGarageActivity(run,run.ChooseReactorDefense);
            GUI.enabled=run.CanChooseCheckpointRace;
            if(GUI.Button(new Rect(24,680,350,50),"Wyścig • checkpointy",compactButton))AttemptGarageActivity(run,run.ChooseCheckpointRace);
            GUI.enabled=run.CanChooseRoadChallenge;
            if(GUI.Button(new Rect(394,680,275,50),R1RunController.RoadModeName(roadSelection),compactButton))AttemptGarageActivity(run,()=>run.ChooseRoadChallenge(roadSelection));
            GUI.enabled=true;
            if(GUI.Button(new Rect(678,680,66,50),"›",compactButton)){roadSelection=(MapR4Mode)(((int)roadSelection+1)%4);activitySaveFeedback=null;}
            GUI.enabled=run.CanChooseLD02Arena;
            if(GUI.Button(new Rect(24,740,350,50),"Arena LD02 • walka",compactButton))AttemptGarageActivity(run,run.ChooseLD02Arena);
            GUI.enabled=true;
            if(!run.MapEditorSession&&GUI.Button(new Rect(394,740,350,50),"Zapisz wyprawę",compactButton))
            {
                string previous=run.RunSaveMessage;bool saved=run.SaveRunCheckpoint();
                activitySaveFeedback=ActivitySaveResult(saved,previous,run.RunSaveMessage);
            }
            if(!string.IsNullOrEmpty(activitySaveFeedback))GUI.Label(new Rect(24,800,720,70),activitySaveFeedback,small);
            string hint=ActivityContextHint(run);
            if(hint!=null)GUI.Label(new Rect(24,875,720,50),hint,small);
        }
    }
}
