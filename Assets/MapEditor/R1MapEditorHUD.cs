using UnityEngine;
using RoboRougelike.MapEditor;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        static readonly string[] mapNames={"Podłoże","Ściana","Rampa","Przeszkoda","Start pojazdu","Meta","Platforma","Filar","Niska bariera","Schody"};
        void DrawMapEditor(R1RunController run)
        {
            Panel(new Rect(10,10,305,660));
            GUI.Label(new Rect(25,18,280,40),"EDYTOR MAP",heading);
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
            run.EditorName=GUI.TextField(new Rect(95,472,200,30),run.EditorName,64);
            run.EditorTextFocused=GUI.GetNameOfFocusedControl()=="map-name";
            if(GUI.Button(new Rect(25,510,130,35),"Zapisz",compactButton)){run.SaveEditedMap();GUI.FocusControl(null);}
            if(GUI.Button(new Rect(165,510,130,35),"Wczytaj",compactButton)){run.LoadEditedMap();GUI.FocusControl(null);}
            if(GUI.Button(new Rect(25,550,270,40),"Testuj mapę",button)){GUI.FocusControl(null);run.TestEditedMap();}
            if(GUI.Button(new Rect(25,595,270,35),"Dostosuj pojazd w garażu",compactButton))run.MapEditorGarage();
            if(GUI.Button(new Rect(25,635,270,28),"Powrót do menu",compactButton))run.RestartRun();
            Panel(new Rect(340,615,920,88));
            GUI.Label(new Rect(355,624,890,65),"LPM — postaw / usuń • PPM — obrót widoku • kółko — zoom • WASD — przesuwanie\nZielone widmo — można postawić • czerwone — pole zajęte / poza mapą • V — obrót\n"+run.EditorMessage,small);
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
                GUI.Label(new Rect(230,220,820,48),"Jazda próbna używa kopii pojazdu; mapa i konstrukcja w garażu pozostają zachowane.",body);
                if(!run.RangeDisabled&&GUI.Button(new Rect(230,285,820,55),PauseCaption(run,0,"Wznów [Esc]"),button))run.TogglePause();
                if(GUI.Button(new Rect(230,355,820,55),PauseCaption(run,1,"Wróć do edytora map"),button))run.ReturnFromEditedMapTest();
                if(GUI.Button(new Rect(230,425,395,55),PauseCaption(run,2,"Powrót do menu"),button))run.RestartRun();
                if(GUI.Button(new Rect(650,425,400,55),PauseCaption(run,3,"Wyjdź z gry"),button))run.RequestQuit();return;
            }
            Panel(new Rect(18,18,560,78));GUI.Label(new Rect(32,27,535,55),run.EditorWorld.Finished?"META OSIĄGNIĘTA • Esc — wróć do edytora":"TEST MAPY • Esc — pauza / powrót do edytora",small);
            DrawAimReticle(run);
            GUI.Label(new Rect(25,677,1000,25),"WASD — jazda • Mysz — widok i celowanie • LPM — ogień • Esc — pauza",small);
        }
    }
}
