using UnityEngine;
using RoboRougelike.MapEditor;
using RoboRougelike.R3;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        static readonly string[] mapNames={"Podłoże","Ściana","Rampa","Przeszkoda","Start pojazdu","Meta","Platforma","Filar","Niska bariera","Schody"};
        MapDraft mapReadinessDraft;
        R3VehicleModel mapReadinessVehicle;
        float nextMapReadinessRefresh;
        string mapReadinessError;
        bool mapBrowserOpen;
        string[] savedMapNames=System.Array.Empty<string>();
        string mapBrowserError;
        Vector2 savedMapsScroll;
        void OpenMapBrowser(R1RunController run)
        {
            mapBrowserOpen=true;savedMapsScroll=Vector2.zero;run.EditorTextFocused=true;
            run.EditorWorld?.HideGhost();RefreshSavedMapNames();
        }
        void RefreshSavedMapNames()
        {
            try{savedMapNames=MapStorage.ListSavedNames(MapStorage.DirectoryPath);mapBrowserError=null;}
            catch(System.Exception error){savedMapNames=System.Array.Empty<string>();mapBrowserError="Nie można odczytać listy map: "+error.Message;}
        }
        internal bool CloseMapBrowser(R1RunController run)
        {if(!mapBrowserOpen)return false;mapBrowserOpen=false;run.EditorTextFocused=false;return true;}
        void LoadMapFromBrowser(R1RunController run,string name)
        {
            if(!mapBrowserOpen||System.Array.IndexOf(savedMapNames,name)<0)return;
            run.EditorName=name;run.CancelMapOverwrite();run.LoadEditedMap();CloseMapBrowser(run);
        }
        void DrawMapBrowser(R1RunController run)
        {
            run.EditorTextFocused=true;
            Panel(new Rect(0,0,1280,720));Panel(new Rect(270,55,740,610));
            GUI.Label(new Rect(300,75,680,45),"ZAPISANE MAPY",heading);
            GUI.Label(new Rect(300,122,680,35),"Wybierz mapę, aby ją wczytać.",small);
            var viewport=new Rect(300,165,680,390);
            savedMapsScroll=GUI.BeginScrollView(viewport,savedMapsScroll,new Rect(0,0,650,Mathf.Max(viewport.height,savedMapNames.Length*48)));
            for(int i=0;i<savedMapNames.Length;i++)
                if(GUI.Button(new Rect(0,i*48,650,42),savedMapNames[i],compactButton))
                {LoadMapFromBrowser(run,savedMapNames[i]);break;}
            if(savedMapNames.Length==0)GUI.Label(new Rect(0,5,640,60),mapBrowserError??"Nie masz jeszcze zapisanych map.",small);
            GUI.EndScrollView();
            if(GUI.Button(new Rect(300,585,325,52),"Odśwież listę",compactButton)){savedMapsScroll=Vector2.zero;RefreshSavedMapNames();}
            if(GUI.Button(new Rect(650,585,330,52),"Wróć do edytora",button))CloseMapBrowser(run);
        }
        void DrawMapTestReadiness(R1RunController run)
        {
            // Checking floor coverage can visit thousands of cells. Refresh at
            // a bounded rate, while the actual test rechecks synchronously.
            if(mapReadinessDraft!=run.EditorDraft||mapReadinessVehicle!=run.VehicleModel||Time.realtimeSinceStartup>=nextMapReadinessRefresh)
            {
                mapReadinessDraft=run.EditorDraft;mapReadinessVehicle=run.VehicleModel;
                mapReadinessError=run.MapTestReadiness();nextMapReadinessRefresh=Time.realtimeSinceStartup+.5f;
            }
            var before=GUI.color;GUI.color=mapReadinessError==null?new Color(.55f,.96f,.72f):new Color(1f,.75f,.42f);
            GUI.Label(new Rect(355,619,890,22),"Test mapy: "+(mapReadinessError??"mapa i pojazd gotowe."),small);
            GUI.color=before;
        }
        void DrawMapEditor(R1RunController run)
        {
            if(mapBrowserOpen){DrawMapBrowser(run);return;}
            Panel(new Rect(10,10,305,660));
            GUI.Label(new Rect(25,18,150,40),"EDYTOR MAP",heading);
            if(GUI.Button(new Rect(175,20,120,34),"Moje mapy",compactButton)){GUI.FocusControl(null);OpenMapBrowser(run);return;}
            GUI.Label(new Rect(25,58,280,30),"Wybierz element:",small);
            for(int i=0;i<mapNames.Length;i++)
            {
                int index=i;bool selected=!run.EditorRemove&&!run.EditorSelect&&(int)run.EditorKind==index;
                if(GUI.Button(new Rect(25+(i%2)*138,85+(i/2)*38,132,33),(selected?"●  ":"")+mapNames[i],compactButton))
                {run.EditorKind=(MapItemKind)index;run.SetMapTool(false,false);GUI.FocusControl(null);}
            }
            GUI.Label(new Rect(25,282,270,22),"Skala elementu:",small);
            MapScalePicker(run,25,"X",0,run.EditorScaleX);MapScalePicker(run,115,"Y",1,run.EditorScaleY);MapScalePicker(run,205,"Z",2,run.EditorScaleZ);
            if(GUI.Button(new Rect(25,350,84,35),"Obróć [V]",compactButton)){run.RotateMapItem();GUI.FocusControl(null);}
            if(GUI.Button(new Rect(115,350,84,35),run.EditorRemove?"Usuń ✓":"Usuń",compactButton)){run.SetMapTool(false,!run.EditorRemove);GUI.FocusControl(null);}
            if(GUI.Button(new Rect(205,350,90,35),run.EditorSelect?"Wybór ✓":"Zaznacz",compactButton)){run.SetMapTool(!run.EditorSelect,false);GUI.FocusControl(null);}
            if(GUI.Button(new Rect(25,390,270,35),run.EditorDraft.GlobalFloor?"Podłoga całej mapy: WŁ":"Podłoga całej mapy: WYŁ",compactButton))run.ToggleMapFloor();
            GUI.enabled=run.EditorDraft.CanUndo;if(GUI.Button(new Rect(25,430,130,35),"Cofnij",compactButton))run.ChangeEditedMap(true);
            GUI.enabled=run.EditorDraft.CanRedo;if(GUI.Button(new Rect(165,430,130,35),"Ponów",compactButton))run.ChangeEditedMap(false);GUI.enabled=true;
            GUI.Label(new Rect(25,475,70,25),"Nazwa:",small);GUI.SetNextControlName("map-name");
            string previousName=run.EditorName;run.EditorName=GUI.TextField(new Rect(95,472,200,30),run.EditorName,64);
            if(run.EditorName!=previousName)run.CancelMapOverwrite();
            run.EditorTextFocused=GUI.GetNameOfFocusedControl()=="map-name";
            if(GUI.Button(new Rect(25,510,130,35),run.MapOverwritePending?"Potwierdź zapis":"Zapisz",compactButton)){run.SaveEditedMap();GUI.FocusControl(null);}
            if(GUI.Button(new Rect(165,510,130,35),run.MapLoadPending?"Potwierdź":"Wczytaj",compactButton)){run.LoadEditedMap();GUI.FocusControl(null);}
            if(GUI.Button(new Rect(25,550,270,40),"Testuj mapę",button)){GUI.FocusControl(null);run.TestEditedMap();}
            if(GUI.Button(new Rect(25,595,270,35),"Dostosuj pojazd w garażu",compactButton))run.MapEditorGarage();
            if(GUI.Button(new Rect(25,635,270,28),run.MapReturnPending?"Potwierdź powrót":"Powrót do menu",compactButton))run.ReturnFromMapEditorToMenu();
            Panel(new Rect(340,615,920,88));
            DrawMapTestReadiness(run);
            if(run.MapOverwritePending||run.MapLoadPending||run.MapExitPending)
            {
                GUI.Label(new Rect(355,644,660,52),run.EditorMessage,small);
                if(GUI.Button(new Rect(1040,647,205,44),"Anuluj",compactButton)){run.CancelMapPendingAction();GUI.FocusControl(null);}
            }
            else GUI.Label(new Rect(355,641,890,59),"LPM — postaw / usuń • PPM — obrót widoku • kółko — zoom • WASD — przesuwanie\nZielone widmo — można postawić • czerwone — pole zajęte / poza mapą • V — obrót\n"+run.EditorMessage,small);
        }
        void MapScalePicker(R1RunController run,float x,string axis,int index,int value)
        {
            GUI.Label(new Rect(x,305,85,22),axis+"  ×"+value,small);
            if(GUI.Button(new Rect(x,325,38,25),"−",compactButton)){run.ChangeMapScale(index,-1);GUI.FocusControl(null);}
            if(GUI.Button(new Rect(x+42,325,38,25),"+",compactButton)){run.ChangeMapScale(index,1);GUI.FocusControl(null);}
        }
        void DrawMapTest(R1RunController run)
        {
            if(run.Transitioning){Panel(new Rect(0,0,1280,720));GUI.Label(new Rect(300,300,700,80),"Przygotowanie mapy…",title);return;}
            if(run.Paused)
            {
                Panel(new Rect(190,115,900,520));GUI.Label(new Rect(230,145,820,65),run.RangeDisabled?"Pojazd unieruchomiony":"TEST MAPY • PAUZA",title);
                GUI.Label(new Rect(230,220,820,48),run.MapExitPending?run.EditorMessage:"Jazda próbna używa kopii pojazdu; mapa i konstrukcja w garażu pozostają zachowane.",body);
                if(!run.RangeDisabled&&GUI.Button(new Rect(230,285,820,55),PauseCaption(run,0,"Wznów [Esc]"),button))run.TogglePause();
                if(GUI.Button(new Rect(230,355,820,55),PauseCaption(run,1,"Wróć do edytora map"),button))run.ReturnFromEditedMapTest();
                if(GUI.Button(new Rect(230,425,395,55),PauseCaption(run,2,run.MapReturnPending?"Potwierdź powrót":"Powrót do menu"),button))run.RestartRun();
                if(GUI.Button(new Rect(650,425,400,55),PauseCaption(run,3,run.MapQuitPending?"Potwierdź wyjście":"Wyjdź z gry"),button))run.RequestQuitFromMenu();return;
            }
            Panel(new Rect(18,18,560,78));GUI.Label(new Rect(32,27,535,55),run.EditorWorld.Finished?"META OSIĄGNIĘTA • Esc — wróć do edytora":"TEST MAPY • Esc — pauza / powrót do edytora",small);
            DrawAimReticle(run);
            GUI.Label(new Rect(25,677,1000,25),"WASD — jazda • Mysz — widok i celowanie • LPM — ogień • Esc — pauza",small);
        }
    }
}
