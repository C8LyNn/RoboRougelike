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
            "Deploy1","Deploy2","Deploy3","Deploy4","Gyro","Reload","SteeringLeft","SteeringRight","SteeringMode","CombatDetails","ResetRange","WeaponStatusPage","DefenseStatusPage"};
        static readonly string[] Defaults={"W","S","A","D","MouseLeft","MouseRight","Q","E",
            "Digit1","Digit2","Digit3","Digit4","Z","X","C","V","G","T","Comma","Period","M","I","R","Tab","P"};
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
            var used=new HashSet<string>(values.Where((v,i)=>!missing[i]&&v!=null),StringComparer.OrdinalIgnoreCase);
            for(int i=0;i<values.Length;i++)
            {
                if(!missing[i])continue;
                var candidates=new[]{Defaults[i],"F5","F6","F7","F9","F11","F12"}.Concat(Enum.GetNames(typeof(Key)));
                string chosen=candidates.FirstOrDefault(key=>ValidBinding(key)&&!Reserved(key)&&!used.Contains(key));
                values[i]=chosen;if(chosen!=null)used.Add(chosen);
            }
        }
        public static bool ValidSnapshot(string[] values)
        {
            var full=NormalizeSnapshot(values);
            return full!=null&&full.Length==Actions.Length&&full.All(x=>ValidBinding(x)&&!Reserved(x))&&
                full.Distinct(StringComparer.OrdinalIgnoreCase).Count()==Actions.Length;
        }
        public bool TryRestoreSnapshot(string[] values,out string message)
        {
            if(!ValidSnapshot(values)){message="Zapisane przypisania są niepoprawne.";return false;}
            var previous=Snapshot();RestoreValues(values);
            try{Save();message="Przywrócono przypisania klawiszy.";return true;}
            catch(Exception error){RestoreValues(previous);message="Nie przywrócono klawiszy: "+error.Message;return false;}
        }
        public string Conflict(string action,string binding)=>bindings.FirstOrDefault(x=>x.Key!=action&&
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
        public static string Display(string binding)=>binding=="MouseLeft"?"LPM":binding=="MouseRight"?"PPM":
            binding=="MouseMiddle"?"ŚPM":binding=="MouseForward"?"Mysz 4":binding=="MouseBack"?"Mysz 5":
            binding=="Comma"?",":binding=="Period"?".":
            binding!=null&&binding.StartsWith("Digit",StringComparison.Ordinal)?binding.Substring(5):binding??"—";
        public static string FromEvent(Event e)
        {
            if(e==null)return null;
            if(e.type==EventType.MouseDown)return e.button==0?"MouseLeft":e.button==1?"MouseRight":
                e.button==2?"MouseMiddle":e.button==3?"MouseBack":e.button==4?"MouseForward":null;
            if(e.type!=EventType.KeyDown||e.alt||e.control||e.command)return null;
            string name=e.keyCode.ToString();
            if(name.StartsWith("Alpha",StringComparison.Ordinal))return "Digit"+name.Substring(5);
            if(name=="Return")return "Enter";
            return name;
        }
        public bool MatchesEvent(string action,Event e)=>string.Equals(Binding(action),FromEvent(e),StringComparison.Ordinal);
        public bool TryBind(string action,string binding)
        {
            if(!bindings.ContainsKey(action)||!ValidBinding(binding)||Reserved(binding)||Conflict(action,binding)!=null)return false;
            bindings[action]=binding;return true;
        }
        public bool TrySaveBinding(string action,string binding,out string message)
        {
            var old=Binding(action);string conflict=Conflict(action,binding);
            if(Reserved(binding)){message="Ten klawisz jest używany przez inne sterowanie.";return false;}
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
                case "WeaponStatusPage":return "Następna strona uzbrojenia";case "DefenseStatusPage":return "Następna strona obrony";}
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
