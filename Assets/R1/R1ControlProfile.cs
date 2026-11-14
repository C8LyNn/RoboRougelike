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
            "Deploy1","Deploy2","Deploy3","Deploy4","Gyro","Reload","SteeringLeft","SteeringRight"};
        static readonly string[] Defaults={"W","S","A","D","MouseLeft","MouseRight","Q","E",
            "Digit1","Digit2","Digit3","Digit4","Z","X","C","V","G","T","Comma","Period"};
        readonly Dictionary<string,string> bindings=new Dictionary<string,string>();
        readonly string path;
        bool unreadable;
        public string Error { get; private set; }
        public static string DefaultPath=>Path.Combine(Application.persistentDataPath,"Settings","controls.json");
        public R1ControlProfile(string storagePath)
        {
            if(string.IsNullOrWhiteSpace(storagePath))throw new ArgumentException("Brak ścieżki ustawień.");
            path=storagePath;ResetDefaults();if(!File.Exists(path))return;
            try
            {
                var data=JsonUtility.FromJson<R1ControlData>(R1RunCheckpointStorage.ReadPayload(path));
                if(data==null||data.version!=1||data.actions==null||data.bindings==null||data.actions.Length!=Actions.Length||
                    data.bindings.Length!=Actions.Length||data.actions.Distinct().Count()!=Actions.Length||
                    data.actions.Any(a=>!Actions.Contains(a))||data.bindings.Any(b=>!ValidBinding(b))||
                    data.bindings.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Actions.Length)
                    throw new InvalidDataException("Niepoprawne przypisania sterowania.");
                for(int i=0;i<data.actions.Length;i++)bindings[data.actions[i]]=data.bindings[i];
            }
            catch(Exception e){unreadable=true;Error="Nie odczytano ustawień; plik pozostawiono bez zmian: "+e.Message;}
        }
        public void ResetDefaults(){for(int i=0;i<Actions.Length;i++)bindings[Actions[i]]=Defaults[i];}
        public string Binding(string action)=>bindings.TryGetValue(action,out var binding)?binding:null;
        public string Conflict(string action,string binding)=>bindings.FirstOrDefault(x=>x.Key!=action&&
            string.Equals(x.Value,binding,StringComparison.OrdinalIgnoreCase)).Key;
        public static bool ValidBinding(string binding)
        {
            if(binding=="MouseLeft"||binding=="MouseRight"||binding=="MouseMiddle"||binding=="MouseForward"||binding=="MouseBack")return true;
            return Enum.TryParse(binding,false,out Key key)&&key!=Key.None&&key!=Key.Escape&&
                Enum.IsDefined(typeof(Key),key)&&key!=Key.LeftAlt&&key!=Key.RightAlt&&key!=Key.LeftMeta&&key!=Key.RightMeta;
        }
        public bool TryBind(string action,string binding)
        {
            if(!bindings.ContainsKey(action)||!ValidBinding(binding)||Conflict(action,binding)!=null)return false;
            bindings[action]=binding;return true;
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
