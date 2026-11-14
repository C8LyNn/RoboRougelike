using System.Linq;
using UnityEngine;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        string controlAwaiting,controlFeedback;
        int controlCaptureFrame;
        Vector2 controlScroll;
        internal bool PauseControlsOpen {get;private set;}
        internal bool MainControlsOpen {get;private set;}
        void OpenControls(){garageOptionsTab=5;controlAwaiting=null;controlFeedback=null;controlScroll=Vector2.zero;}
        internal void OpenMainControls(){MainControlsOpen=true;controlAwaiting=null;controlFeedback=null;controlScroll=Vector2.zero;}
        internal bool CloseMainControls(){if(!MainControlsOpen)return false;MainControlsOpen=false;controlAwaiting=null;return true;}
        internal void OpenPauseControls(){PauseControlsOpen=true;controlAwaiting=null;controlFeedback=null;controlScroll=Vector2.zero;}
        internal bool ClosePauseControls(){if(!PauseControlsOpen)return false;PauseControlsOpen=false;controlAwaiting=null;return true;}
        void DrawPauseControls()
        {
            Panel(new Rect(190,70,900,590));
            GUI.Label(new Rect(230,90,820,50),"STEROWANIE",heading);
            DrawControls(new Rect(230,145,820,490));
        }
        void DrawMainControls(bool portrait)
        {
            if(portrait){Panel(new Rect(12,50,744,900));GUI.Label(new Rect(24,70,720,50),"STEROWANIE",heading);DrawControls(new Rect(24,130,720,790));}
            else{Panel(new Rect(190,70,900,590));GUI.Label(new Rect(230,90,820,50),"STEROWANIE",heading);DrawControls(new Rect(230,145,820,490));}
        }
        void DrawControls(Rect area)
        {
            var profile=R1ControlProfile.Current;var input=Event.current;
            if(controlAwaiting!=null&&Time.frameCount>controlCaptureFrame&&
               (input.type==EventType.KeyDown||input.type==EventType.MouseDown))
            {
                if(input.type==EventType.KeyDown&&input.keyCode==KeyCode.Escape)
                {controlAwaiting=null;controlFeedback="Anulowano zmianę.";input.Use();}
                else
                {
                    string value=R1ControlProfile.FromEvent(input);
                    if(value!=null)
                    {
                        profile.TrySaveBinding(controlAwaiting,value,out controlFeedback);
                        controlAwaiting=null;input.Use();
                    }
                }
            }
            GUI.Label(new Rect(area.x,area.y,area.width,26),"Kliknij akcję i naciśnij nowy klawisz lub przycisk myszy.",small);
            if(!string.IsNullOrEmpty(profile.Error))GUI.Label(new Rect(area.x,area.y+24,area.width,35),profile.Error,small);
            float top=area.y+60,height=area.height-150;
            controlScroll=GUI.BeginScrollView(new Rect(area.x,top,area.width,height),controlScroll,
                new Rect(0,0,area.width-20,R1ControlProfile.Actions.Length*42));
            try
            {
                for(int i=0;i<R1ControlProfile.Actions.Length;i++)
                {
                    string action=R1ControlProfile.Actions[i];float y=i*42;
                    GUI.Label(new Rect(0,y+7,area.width-230,30),R1ControlProfile.ActionName(action),small);
                    if(GUI.Button(new Rect(area.width-220,y,180,36),controlAwaiting==action?"Naciśnij...":R1ControlProfile.Display(profile.Binding(action)),compactButton))
                    {controlAwaiting=action;controlCaptureFrame=Time.frameCount;controlFeedback=null;}
                }
            }
            finally{GUI.EndScrollView();}
            if(GUI.Button(new Rect(area.x,area.y+area.height-80,230,38),"Domyślne",compactButton))
            {
                // Keep the previous live profile if saving fails.
                var old=R1ControlProfile.Actions.Select(profile.Binding).ToArray();profile.ResetDefaults();
                try{profile.Save();controlFeedback="Przywrócono domyślne klawisze.";}
                catch(System.Exception e){profile.RestoreValues(old);controlFeedback="Nie zapisano ustawień: "+e.Message;}
            }
            if(GUI.Button(new Rect(area.x+245,area.y+area.height-80,200,38),"Wróć",compactButton))
            {controlAwaiting=null;if(PauseControlsOpen)ClosePauseControls();else if(MainControlsOpen)CloseMainControls();else garageOptionsTab=0;}
            if(!string.IsNullOrEmpty(controlFeedback))GUI.Label(new Rect(area.x,area.y+area.height-40,area.width,38),controlFeedback,small);
        }
    }
}
