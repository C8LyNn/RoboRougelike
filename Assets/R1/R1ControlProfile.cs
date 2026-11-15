using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoboRougelike.R1
{
    [Serializable] sealed class R1ControlData { public int version=1; public string[] actions,bindings; }

    // The profile is independent of vehicle ownership and survives run restarts.
    public sealed class R1ControlProfile
    {
        public static readonly string[] Actions={"MoveForward","MoveBackward","TurnLeft","TurnRight",
            "Weapon1","Weapon2","Weapon3","Weapon4","Active1","Active2","Active3","Active4",
            "Deploy1","Deploy2","Deploy3","Deploy4","Gyro","Reload","SteeringLeft","SteeringRight","SteeringMode","CombatDetails","ResetRange","WeaponStatusPage","DefenseStatusPage","GarageUnderside","GarageFrame","GarageSearch","GarageBlocks","GarageWheels","GarageWeapons","GarageModules","GarageToolMount","GarageToolInspect","GarageToolRepair","GarageToolRemove","GarageRotateYLeft","GarageRotateYRight","GarageRotateXPositive","GarageRotateXNegative","GarageRotateZNegative","GarageRotateZPositive","Rescue","Interact","Pause","SkipPickup","RewardBonus1","RewardBonus2","RewardBonus3","RewardBasket1","RewardBasket2","RewardBasket3","RewardConfirm","MenuConfirm","PausePrevious","PauseNext","PauseConfirm"};
        static readonly string[] Defaults={"W","S","A","D","MouseLeft","MouseRight","Q","E",
            "Digit1","Digit2","Digit3","Digit4","Z","X","C","V","G","T","Comma","Period","M","I","R","Tab","P","U","Home","Slash","Digit5","Digit6","Digit7","Digit8","Digit1","Digit2","Digit3","Digit4","Q","E","R","F","Z","X","H","F","Escape","Enter","Digit1","Digit2","Digit3","Q","W","E","Enter","Enter","UpArrow","DownArrow","Enter"};
        const int OriginalActionCount=20;
        readonly Dictionary<string,string> bindings=new Dictionary<string,string>();
        readonly string path;
        bool unreadable;
        public string Error { get; private set; }
        static R1ControlProfile current;
        public static R1ControlProfile Current=>current??=new R1ControlProfile(DefaultPath);
        public static string DefaultPath=>Application.isBatchMode?
            Path.Combine(Path.GetTempPath(),"RRL-controls-QA-"+System.Diagnostics.Process.GetCurrentProcess().Id,"controls.json"):
            Path.Combine(Application.persistentDataPath,"Settings","controls.json");
        public R1ControlProfile(string storagePath)
        {
            if(string.IsNullOrWhiteSpace(storagePath))throw new ArgumentException("Brak ścieżki ustawień.");
            path=storagePath;ResetDefaults();if(!File.Exists(path))return;
            try
            {
                var data=JsonUtility.FromJson<R1ControlData>(R1RunCheckpointStorage.ReadPayload(path));
                if(data==null||data.version!=1||data.actions==null||data.bindings==null||
                    data.actions.Length<OriginalActionCount||data.actions.Length>Actions.Length||
                    data.bindings.Length!=data.actions.Length||data.actions.Distinct().Count()!=data.actions.Length||
                    data.actions.Any(a=>!Actions.Contains(a))||
                    Actions.Take(OriginalActionCount).Any(a=>!data.actions.Contains(a)))
                    throw new InvalidDataException("Niepoprawne przypisania sterowania.");
                var merged=Actions.Select(a=>{int at=Array.IndexOf(data.actions,a);return at<0?null:data.bindings[at];}).ToArray();
                var missing=Actions.Select(a=>!data.actions.Contains(a)).ToArray();FillMissing(merged,missing);
                if(!ValidSnapshot(merged))throw new InvalidDataException("Niepoprawne przypisania sterowania.");
                for(int i=0;i<Actions.Length;i++)bindings[Actions[i]]=merged[i];
            }
            catch(Exception e){unreadable=true;Error="Nie odczytano ustawień; plik pozostawiono bez zmian: "+e.Message;}
        }
        public void ResetDefaults(){for(int i=0;i<Actions.Length;i++)bindings[Actions[i]]=Defaults[i];}
        internal void RestoreValues(string[] values){var full=NormalizeSnapshot(values);for(int i=0;i<Actions.Length;i++)bindings[Actions[i]]=full[i];}
        public string Binding(string action)=>bindings.TryGetValue(action,out var binding)?binding:null;
        public string[] Snapshot()=>Actions.Select(Binding).ToArray();
        static string[] NormalizeSnapshot(string[] values)
        {
            if(values==null||values.Length<OriginalActionCount||values.Length>=Actions.Length)return values;
            var full=new string[Actions.Length];Array.Copy(values,full,values.Length);
            var missing=Enumerable.Range(0,Actions.Length).Select(i=>i>=values.Length).ToArray();
            FillMissing(full,missing);return full;
        }
        static void FillMissing(string[] values,bool[] missing)
        {
            var used=new HashSet<string>(values.Where((v,i)=>!missing[i]&&v!=null&&!MenuAction(Actions[i])&&!PauseNavigation(Actions[i])),StringComparer.OrdinalIgnoreCase);
            var garageUsed=new HashSet<string>(values.Where((v,i)=>!missing[i]&&v!=null&&GarageAction(Actions[i])),StringComparer.OrdinalIgnoreCase);
            var gameplayUsed=new HashSet<string>(values.Where((v,i)=>!missing[i]&&v!=null&&!GarageAction(Actions[i])&&!RewardAction(Actions[i])&&!MenuAction(Actions[i])&&!PauseNavigation(Actions[i])),StringComparer.OrdinalIgnoreCase);
            var rewardUsed=new HashSet<string>(values.Where((v,i)=>!missing[i]&&v!=null&&RewardAction(Actions[i])),StringComparer.OrdinalIgnoreCase);
            var menuUsed=new HashSet<string>(values.Where((v,i)=>!missing[i]&&v!=null&&(MenuAction(Actions[i])||Actions[i]=="Pause")),StringComparer.OrdinalIgnoreCase);
            var pauseUsed=new HashSet<string>(values.Where((v,i)=>!missing[i]&&v!=null&&(PauseNavigation(Actions[i])||Actions[i]=="Pause"||Actions[i]=="ResetRange")),StringComparer.OrdinalIgnoreCase);
            for(int i=0;i<values.Length;i++)
            {
                if(!missing[i])continue;
                var candidates=new[]{Defaults[i],"F5","F6","F7","F9","F11","F12"}.Concat(Enum.GetNames(typeof(Key)));
                string chosen=candidates.FirstOrDefault(key=>AllowedActionBinding(Actions[i],key)&&!GarageModifierReserved(Actions[i],key)&&
                    !(PauseNavigation(Actions[i])?pauseUsed:MenuAction(Actions[i])?menuUsed:RewardAction(Actions[i])?rewardUsed:GarageToolAction(Actions[i])||GarageRotationAction(Actions[i])?garageUsed:Actions[i]=="Interact"||Actions[i]=="Pause"||Actions[i]=="SkipPickup"?gameplayUsed:used).Contains(key)&&
                    !((Actions[i]=="Pause"||Actions[i]=="ResetRange")&&pauseUsed.Contains(key))&&!(Actions[i]=="Pause"&&menuUsed.Contains(key)));
                values[i]=chosen;if(chosen!=null){if(PauseNavigation(Actions[i]))pauseUsed.Add(chosen);else if(MenuAction(Actions[i]))menuUsed.Add(chosen);else{used.Add(chosen);if(RewardAction(Actions[i]))rewardUsed.Add(chosen);else if(GarageAction(Actions[i]))garageUsed.Add(chosen);else gameplayUsed.Add(chosen);if(Actions[i]=="Pause"||Actions[i]=="ResetRange")pauseUsed.Add(chosen);if(Actions[i]=="Pause")menuUsed.Add(chosen);}}
            }
        }
        public static bool ValidSnapshot(string[] values)
        {
            var full=NormalizeSnapshot(values);
            if(full==null||full.Length!=Actions.Length||!full.Select((value,i)=>AllowedActionBinding(Actions[i],value)&&!GarageMouseReserved(Actions[i],value)).All(valid=>valid))return false;
            for(int i=0;i<full.Length;i++)for(int j=0;j<i;j++)
                if(SameContext(Actions[i],Actions[j])&&string.Equals(full[i],full[j],StringComparison.OrdinalIgnoreCase))return false;
            return true;
        }
        public bool TryRestoreSnapshot(string[] values,out string message)
        {
            if(!ValidSnapshot(values)){message="Zapisane przypisania są niepoprawne.";return false;}
            var previous=Snapshot();RestoreValues(values);
            try{Save();message="Przywrócono przypisania klawiszy.";return true;}
            catch(Exception error){RestoreValues(previous);message="Nie przywrócono klawiszy: "+error.Message;return false;}
        }
        // Older profiles could assign pause and a reward action to the same
        // input. Keep loading those profiles, but prevent new conflicts and
        // make the existing one visible until the player resolves it.
        static bool PauseRewardConflict(string left,string right)=>
            left=="Pause"&&RewardAction(right)||right=="Pause"&&RewardAction(left);
        public string ExistingConflictNotice
        {
            get
            {
                var pause=Binding("Pause");
                var reward=Actions.FirstOrDefault(action=>RewardAction(action)&&
                    string.Equals(Binding(action),pause,StringComparison.OrdinalIgnoreCase));
                return reward==null?null:"Konflikt: Pauza / "+ActionName(reward)+". Zmień jedno przypisanie.";
            }
        }
        public string Conflict(string action,string binding)=>bindings.FirstOrDefault(x=>x.Key!=action&&(SameContext(x.Key,action)||PauseRewardConflict(x.Key,action))&&
            string.Equals(x.Value,binding,StringComparison.OrdinalIgnoreCase)).Key;
        public static bool ValidBinding(string binding)
        {
            if(binding=="MouseLeft"||binding=="MouseRight"||binding=="MouseMiddle"||binding=="MouseForward"||binding=="MouseBack")return true;
            return Enum.TryParse(binding,false,out Key key)&&key!=Key.None&&key!=Key.Escape&&
                Enum.IsDefined(typeof(Key),key)&&key!=Key.LeftAlt&&key!=Key.RightAlt&&key!=Key.LeftMeta&&key!=Key.RightMeta;
        }
        // Fixed session/UI shortcuts remain reserved until they too are configurable.
        public static bool Reserved(string binding)=>new[]{"Escape","Enter","NumpadEnter",
            "UpArrow","DownArrow","LeftArrow","RightArrow","F4","F8","F10"}.Contains(binding);
        static bool AllowedActionBinding(string action,string binding)=>action=="Pause"&&binding=="Escape"||
            action=="SkipPickup"&&binding=="Enter"||(RewardAction(action)||MenuAction(action))&&(binding=="Enter"||binding=="NumpadEnter")||
            PauseNavigation(action)&&PauseNavigationReserved(binding)||ValidBinding(binding)&&!Reserved(binding);
        // These buttons already place, remove/orbit and pick parts in the garage.
        static bool GarageCategoryAction(string action)=>action=="GarageBlocks"||action=="GarageWheels"||action=="GarageWeapons"||action=="GarageModules";
        static bool GarageToolAction(string action)=>action=="GarageToolMount"||action=="GarageToolInspect"||action=="GarageToolRepair"||action=="GarageToolRemove";
        static bool GarageRotationAction(string action)=>action=="GarageRotateYLeft"||action=="GarageRotateYRight"||action=="GarageRotateXPositive"||action=="GarageRotateXNegative"||action=="GarageRotateZNegative"||action=="GarageRotateZPositive";
        static bool GarageAction(string action)=>action=="GarageUnderside"||action=="GarageFrame"||action=="GarageSearch"||GarageCategoryAction(action)||GarageToolAction(action)||GarageRotationAction(action);
        static bool RewardAction(string action)=>action=="RewardBonus1"||action=="RewardBonus2"||action=="RewardBonus3"||
            action=="RewardBasket1"||action=="RewardBasket2"||action=="RewardBasket3"||action=="RewardConfirm";
        static bool MenuAction(string action)=>action=="MenuConfirm";
        static bool PauseNavigation(string action)=>action=="PausePrevious"||action=="PauseNext"||action=="PauseConfirm";
        static bool PauseNavigationReserved(string binding)=>binding=="Enter"||binding=="NumpadEnter"||binding=="UpArrow"||binding=="DownArrow"||binding=="LeftArrow"||binding=="RightArrow";
        static int Context(string action)=>PauseNavigation(action)?4:MenuAction(action)?3:RewardAction(action)?2:GarageAction(action)?1:0;
        // Pause is also handled on main and result screens, so it cannot
        // share their confirmation binding even though movement can.
        static bool SameContext(string left,string right)=>Context(left)==Context(right)||
            MenuAction(left)&&right=="Pause"||MenuAction(right)&&left=="Pause"||
            PauseNavigation(left)&&(right=="Pause"||right=="ResetRange")||PauseNavigation(right)&&(left=="Pause"||left=="ResetRange");
        static bool GarageMouseReserved(string action,string binding)=>GarageAction(action)&&
            (binding=="MouseLeft"||binding=="MouseRight"||binding=="MouseMiddle");
        // Shift rotates/removes and Ctrl modifies repair clicks in the garage.
        static bool GarageModifierReserved(string action,string binding)=>GarageAction(action)&&
            (binding=="LeftShift"||binding=="RightShift"||binding=="LeftCtrl"||binding=="RightCtrl");
        public static string Display(string binding)=>binding=="Escape"?"Esc":binding=="MouseLeft"?"LPM":binding=="MouseRight"?"PPM":
            binding=="MouseMiddle"?"ŚPM":binding=="MouseForward"?"Mysz 4":binding=="MouseBack"?"Mysz 5":
            binding=="UpArrow"?"↑":binding=="DownArrow"?"↓":binding=="LeftArrow"?"←":binding=="RightArrow"?"→":
            binding=="Comma"?",":binding=="Period"?".":binding=="Slash"?"/":binding=="NumpadDivide"?"Num /":
            binding!=null&&binding.StartsWith("Numpad",StringComparison.Ordinal)?"Num "+binding.Substring(6):
            binding!=null&&binding.StartsWith("Digit",StringComparison.Ordinal)?binding.Substring(5):binding??"—";
        public static bool IsBindingMouseDown(Event e)=>e!=null&&(e.type==EventType.MouseDown||
            (e.type==EventType.Ignore&&e.rawType==EventType.MouseDown&&(e.button==3||e.button==4)));
        public static bool IsBindingInput(Event e)=>e!=null&&(e.type==EventType.KeyDown||IsBindingMouseDown(e));
        public static string FromEvent(Event e)
        {
            if(e==null)return null;
            if(IsBindingMouseDown(e))return e.button==0?"MouseLeft":e.button==1?"MouseRight":
                e.button==2?"MouseMiddle":e.button==3?"MouseBack":e.button==4?"MouseForward":null;
            bool controlKey=e.keyCode==KeyCode.LeftControl||e.keyCode==KeyCode.RightControl;
            if(e.type!=EventType.KeyDown||e.alt||e.command||e.control&&!controlKey)return null;
            string name=e.keyCode.ToString();
            if(name.StartsWith("Alpha",StringComparison.Ordinal))return "Digit"+name.Substring(5);
            if(name.StartsWith("Keypad",StringComparison.Ordinal))return "Numpad"+name.Substring(6);
            if(name=="Return")return "Enter";
            if(name=="BackQuote")return "Backquote";
            if(name=="LeftControl")return "LeftCtrl";
            if(name=="RightControl")return "RightCtrl";
            if(name=="Numlock")return "NumLock";
            if(name=="Menu")return "ContextMenu";
            return name;
        }
        public bool MatchesEvent(string action,Event e)=>string.Equals(Binding(action),FromEvent(e),StringComparison.Ordinal);
        public bool TryBind(string action,string binding)
        {
            if(!bindings.ContainsKey(action)||!AllowedActionBinding(action,binding)||GarageMouseReserved(action,binding)||GarageModifierReserved(action,binding)||Conflict(action,binding)!=null)return false;
            bindings[action]=binding;return true;
        }
        public bool TrySaveBinding(string action,string binding,out string message)
        {
            var old=Binding(action);string conflict=Conflict(action,binding);
            if(GarageMouseReserved(action,binding)){message="Ten przycisk wykonuje już podstawową akcję w garażu.";return false;}
            if(GarageModifierReserved(action,binding)){message="Shift i Ctrl służą do działań na częściach w garażu.";return false;}
            if(Reserved(binding)&&!(action=="Pause"&&binding=="Escape")&&!(action=="SkipPickup"&&binding=="Enter")&&!((RewardAction(action)||MenuAction(action))&&(binding=="Enter"||binding=="NumpadEnter"))&&!(PauseNavigation(action)&&PauseNavigationReserved(binding))){message="Ten klawisz jest używany przez inne sterowanie.";return false;}
            if(conflict!=null){message="Ten przycisk jest już używany: "+ActionName(conflict)+".";return false;}
            if(!TryBind(action,binding)){message="Nie można przypisać tego przycisku.";return false;}
            try{Save();message="Zapisano przypisanie.";return true;}
            catch(Exception e){bindings[action]=old;message="Nie zapisano ustawień: "+e.Message;return false;}
        }
        public static string ActionName(string action)
        {
            switch(action){case "MoveForward":return "Jazda do przodu";case "MoveBackward":return "Jazda do tyłu";
                case "TurnLeft":return "Skręt w lewo";case "TurnRight":return "Skręt w prawo";
                case "Gyro":return "Żyroskop";case "Reload":return "Przeładowanie";
                case "SteeringLeft":return "Obrót w lewo";case "SteeringRight":return "Obrót w prawo";case "SteeringMode":return "Tryb skrętu";case "CombatDetails":return "Szczegóły w walce";case "ResetRange":return "Reset poligonu";
                case "WeaponStatusPage":return "Następna strona uzbrojenia";case "DefenseStatusPage":return "Następna strona obrony";
                case "GarageUnderside":return "Garaż: widok od spodu";case "GarageFrame":return "Garaż: dopasuj widok";
                case "GarageSearch":return "Garaż: szukaj części";
                case "GarageBlocks":return "Garaż: bloki";case "GarageWheels":return "Garaż: koła";
                case "GarageWeapons":return "Garaż: broń";case "GarageModules":return "Garaż: moduły";
                case "GarageToolMount":return "Garaż: montuj";case "GarageToolInspect":return "Garaż: zaznacz";
                case "GarageToolRepair":return "Garaż: napraw";case "GarageToolRemove":return "Garaż: zdejmij";
                case "GarageRotateYLeft":return "Garaż: obrót Y w lewo";case "GarageRotateYRight":return "Garaż: obrót Y w prawo";
                case "GarageRotateXPositive":return "Garaż: obrót X dodatni";case "GarageRotateXNegative":return "Garaż: obrót X ujemny";
                case "GarageRotateZNegative":return "Garaż: obrót Z ujemny";case "GarageRotateZPositive":return "Garaż: obrót Z dodatni";
                case "Rescue":return "Ratunek";case "Interact":return "Interakcja";case "Pause":return "Pauza";case "SkipPickup":return "Pomiń zbieranie po walce";}
            if(action=="RewardConfirm")return "Nagrody: zatwierdź";
            if(action=="MenuConfirm")return "Menu: rozpocznij / wróć";
            if(action=="PausePrevious")return "Pauza: poprzednia opcja";
            if(action=="PauseNext")return "Pauza: następna opcja";
            if(action=="PauseConfirm")return "Pauza: zatwierdź";
            if(action.StartsWith("RewardBonus"))return "Nagrody: bonus "+action.Substring(11);
            if(action.StartsWith("RewardBasket"))return "Nagrody: zestaw "+action.Substring(12);
            if(action.StartsWith("Weapon"))return "Broń "+action.Substring(6);
            if(action.StartsWith("Active"))return "Moduł "+action.Substring(6);
            if(action.StartsWith("Deploy"))return "Urządzenie "+action.Substring(6);
            return action;
        }
        public void Save()
        {
            if(unreadable)throw new InvalidOperationException("Nie można nadpisać nieczytelnego pliku ustawień.");
            R1RunCheckpointStorage.WritePayload(path,JsonUtility.ToJson(new R1ControlData{
                actions=Actions.ToArray(),bindings=Actions.Select(a=>bindings[a]).ToArray()}));
            Error=null;
        }
        public bool Pressed(string action,bool down=false)
        {
            var value=Binding(action);var mouse=Mouse.current;
            UnityEngine.InputSystem.Controls.ButtonControl button=null;
            if(value=="MouseLeft")button=mouse?.leftButton;
            else if(value=="MouseRight")button=mouse?.rightButton;
            else if(value=="MouseMiddle")button=mouse?.middleButton;
            else if(value=="MouseForward")button=mouse?.forwardButton;
            else if(value=="MouseBack")button=mouse?.backButton;
            else if(Enum.TryParse(value,false,out Key key)&&key!=Key.None&&Keyboard.current!=null)button=Keyboard.current[key];
            return button!=null&&(down?button.wasPressedThisFrame:button.isPressed);
        }
    }
}
