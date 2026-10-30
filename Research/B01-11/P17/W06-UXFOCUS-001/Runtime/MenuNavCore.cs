using System;
using System.Collections.Generic;

namespace B01.UXFocus
{
    // A pure navigation policy. Only Codex may map chosen actions to actual R1RunController calls.
    public enum NavScreen { VehicleSelect, RewardChoice, Results }
    public enum NavCommand { None, Left, Right, Up, Down, Confirm }
    public enum NavAction
    {
        None, Vehicle0, Vehicle1, Vehicle2, EditSeed, ResumeCheckpoint,
        PrepareVehicle, OpenSandbox, OpenMapEditor, Quit, Reward0, Reward1,
        Reward2, Restart
    }

    public sealed class MenuSnapshot
    {
        public NavScreen Screen;
        public int VehicleCount = 3;
        public int SelectedVehicleSlot;
        public int RewardCount = 3;
        public bool CheckpointAvailable;
        public bool MapEditorAvailable = true;
        public bool SandboxAvailable = true;
        public bool QuitAvailable = true;
        public bool IsTransitioning;
        public bool IsTextEditing;
    }

    public sealed class MenuSelection
    {
        public readonly string FocusId;
        public readonly NavAction Action;
        public readonly bool FocusChanged;
        public MenuSelection(string focusId, NavAction action, bool changed)
        { FocusId = focusId; Action = action; FocusChanged = changed; }
    }

    public sealed class FocusTarget
    {
        public readonly string Id;
        public readonly NavAction Action;
        public readonly float X, Y, Width, Height;
        internal FocusTarget(string id, NavAction action, float x, float y, float width, float height)
        {
            Id=id; Action=action; X=x; Y=y; Width=width; Height=height;
        }
        public float CenterX { get { return X + Width / 2; } }
        public float CenterY { get { return Y + Height / 2; } }
    }

    public static class MenuNavCore
    {
        // Rectangles copied from exact R1RunHUD.cs @ blob 049266e930fcb27062c0425dda248a970b681923.
        // No Unity dependency: the host renders/handles actual input and all game-state actions.
        static FocusTarget T(string id, NavAction action, float x,float y,float w,float h)
        { return new FocusTarget(id,action,x,y,w,h); }

        public static FocusTarget[] Targets(MenuSnapshot s)
        {
            Validate(s);
            var t=new List<FocusTarget>();
            if(s.IsTransitioning) return t.ToArray();

            if(s.Screen==NavScreen.VehicleSelect)
            {
                for(int i=0;i<s.VehicleCount;i++)
                    t.Add(T("ready/vehicle"+i,(NavAction)((int)NavAction.Vehicle0+i),
                        100+i*380,420,320,48));
                t.Add(T("ready/seed",NavAction.EditSeed,200,568,230,28));
                if(s.CheckpointAvailable)
                    t.Add(T("ready/resume",NavAction.ResumeCheckpoint,450,548,170,38));
                if(s.MapEditorAvailable)
                    t.Add(T("ready/editor",NavAction.OpenMapEditor,640,548,300,38));
                if(s.QuitAvailable)
                    t.Add(T("ready/quit",NavAction.Quit,960,552,230,38));
                t.Add(T("ready/prepare",NavAction.PrepareVehicle,90,600,500,65));
                if(s.SandboxAvailable)
                    t.Add(T("ready/sandbox",NavAction.OpenSandbox,640,600,550,65));
            }
            else if(s.Screen==NavScreen.RewardChoice)
            {
                for(int i=0;i<s.RewardCount;i++)
                    t.Add(T("reward/choice"+i,(NavAction)((int)NavAction.Reward0+i),
                        100+i*380,490,320,52));
            }
            else if(s.Screen==NavScreen.Results)
            {
                t.Add(T("results/restart",NavAction.Restart,160,535,500,70));
            }
            return t.ToArray();
        }

        public static MenuSelection Step(MenuSnapshot s, string currentFocusId, NavCommand command)
        {
            Validate(s);
            if(!Enum.IsDefined(typeof(NavCommand),command))
                throw new ArgumentException("Unknown navigation command","command");
            var targets=Targets(s);
            if(targets.Length==0) return new MenuSelection("",NavAction.None,
                !string.IsNullOrEmpty(currentFocusId));
            int current=-1;
            for(int i=0;i<targets.Length;i++)
                if(targets[i].Id==currentFocusId) {current=i;break;}

            // A stale focus from another screen must NOT trigger its old action via Confirm.
            if(current<0)
            {
                int first=(s.Screen==NavScreen.VehicleSelect)?s.SelectedVehicleSlot:0;
                // Validated VehicleCount / SelectedVehicleSlot ensure this is a valid button.
                return new MenuSelection(targets[first].Id,NavAction.None,targets[first].Id!=currentFocusId);
            }
            if(s.IsTextEditing)
                return new MenuSelection(targets[current].Id,NavAction.None,false);
            if(command==NavCommand.Confirm)
                return new MenuSelection(targets[current].Id,targets[current].Action,false);
            if(command==NavCommand.None)
                return new MenuSelection(targets[current].Id,NavAction.None,false);

            var here=targets[current];
            int best=-1;
            double score=double.PositiveInfinity;
            for(int i=0;i<targets.Length;i++)
            {
                if(i==current)continue;
                var dx=(double)targets[i].CenterX-here.CenterX;
                var dy=(double)targets[i].CenterY-here.CenterY;
                double primary=0,cross=0;
                if(command==NavCommand.Left){primary=-dx;cross=Math.Abs(dy);}
                else if(command==NavCommand.Right){primary=dx;cross=Math.Abs(dy);}
                else if(command==NavCommand.Up){primary=-dy;cross=Math.Abs(dx);}
                else if(command==NavCommand.Down){primary=dy;cross=Math.Abs(dx);}
                else throw new ArgumentException("Direction expected","command");

                // Reject diagonals where cross-axis movement dominates. A right arrow
                // must not jump down into Quit merely because Quit is slightly right.
                if(primary<=0 || cross>primary*1.5+25)continue;
                double candidate=primary+cross*3;
                if(candidate<score-0.000001)
                {score=candidate;best=i;}
                // Equal-score ties resolve by source target order for deterministic replays.
            }
            return best<0
                ?new MenuSelection(here.Id,NavAction.None,false)
                :new MenuSelection(targets[best].Id,NavAction.None,true);
        }

        static void Validate(MenuSnapshot s)
        {
            if(s==null) throw new ArgumentNullException("s");
            if(!Enum.IsDefined(typeof(NavScreen),s.Screen) ||
                s.VehicleCount<1 || s.VehicleCount>3 ||
                s.SelectedVehicleSlot<0 || s.SelectedVehicleSlot>=s.VehicleCount ||
                s.RewardCount<0 || s.RewardCount>3)
                throw new ArgumentException("Invalid authoritative menu snapshot","s");
        }
    }
}
