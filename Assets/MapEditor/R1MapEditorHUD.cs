using UnityEngine;
using RoboRougelike.MapEditor;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        static readonly string[] mapNames={"Podłoże","Ściana","Rampa","Przeszkoda","Start pojazdu","Meta"};
        void DrawMapEditor(R1RunController run)
        {
            Panel(new Rect(10,10,305,660));
            GUI.Label(new Rect(25,18,280,40),"EDYTOR MAP",heading);
            GUI.Label(new Rect(25,58,280,30),"Wybierz element:",small);
            for(int i=0;i<mapNames.Length;i++)
            {
                int index=i;bool selected=!run.EditorRemove&&(int)run.EditorKind==index;
                if(GUI.Button(new Rect(25,90+i*42,270,36),(selected?"●  ":"")+mapNames[i],compactButton))
                {run.EditorKind=(MapItemKind)index;run.EditorRemove=false;GUI.FocusControl(null);}
            }
            if(GUI.Button(new Rect(25,350,130,35),"Obróć 90° [V]",compactButton)){run.EditorRotation=(run.EditorRotation+1)%4;GUI.FocusControl(null);}
            if(GUI.Button(new Rect(165,350,130,35),run.EditorRemove?"Usuwanie ✓":"Usuń",compactButton)){run.EditorRemove=!run.EditorRemove;GUI.FocusControl(null);}
            GUI.enabled=run.EditorDraft.CanUndo;if(GUI.Button(new Rect(25,392,130,35),"Cofnij",compactButton))run.ChangeEditedMap(true);
            GUI.enabled=run.EditorDraft.CanRedo;if(GUI.Button(new Rect(165,392,130,35),"Ponów",compactButton))run.ChangeEditedMap(false);GUI.enabled=true;
            GUI.Label(new Rect(25,435,270,25),"Nazwa mapy:",small);GUI.SetNextControlName("map-name");
            run.EditorName=GUI.TextField(new Rect(25,460,270,30),run.EditorName,64);
            run.EditorTextFocused=GUI.GetNameOfFocusedControl()=="map-name";
            if(GUI.Button(new Rect(25,498,130,35),"Zapisz",compactButton)){run.SaveEditedMap();GUI.FocusControl(null);}
            if(GUI.Button(new Rect(165,498,130,35),"Wczytaj",compactButton)){run.LoadEditedMap();GUI.FocusControl(null);}
            if(GUI.Button(new Rect(25,540,270,42),"Testuj mapę",button)){GUI.FocusControl(null);run.TestEditedMap();}
            if(GUI.Button(new Rect(25,588,270,35),"Dostosuj pojazd w garażu",compactButton))run.MapEditorGarage();
            if(GUI.Button(new Rect(25,630,270,30),"Powrót do menu",compactButton))run.RestartRun();
            Panel(new Rect(340,615,920,88));
            GUI.Label(new Rect(355,624,890,65),"LPM — postaw / usuń • PPM — obrót widoku • kółko — zoom • WASD — przesuwanie\nV — obrót elementu • Ctrl+Z / Ctrl+Y — cofnij / ponów\n"+run.EditorMessage,small);
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
