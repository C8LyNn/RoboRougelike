using System.Linq;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.Mechanics.StructuralScan;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RoboRougelike.PackageIntegration
{
    public sealed class RRLScannerControls : MonoBehaviour
    {
        IRRLScanDevice[] scanners;
        GUIStyle label;
        void Start()=>Cache();
        void Cache()=>scanners=GetComponentsInChildren<MonoBehaviour>().OfType<IRRLScanDevice>().ToArray();
        bool PlayerScope=>R1RunController.Instance?.Player?.gameObject==gameObject&&RRLScanWorld.InEncounter(R1RunController.Instance);
        public bool Activate(RRLScanKind kind)
        {
            if(!PlayerScope||R1RunController.Instance.Paused)return false;if(scanners==null)Cache();
            foreach(var scanner in scanners)
                if(scanner is MonoBehaviour component&&component!=null&&scanner.Kind==kind&&scanner.TryActivate())return true;
            return false;
        }
        public bool HandleGuiInput(Event input)
        {
            if(input==null||input.type!=EventType.KeyDown||input.alt||input.control||input.command)return false;
            return (input.keyCode==KeyCode.K||input.character=='k'||input.character=='K')&&Activate(RRLScanKind.Structural);
        }
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current?.kKey.wasPressedThisFrame==true)Activate(RRLScanKind.Structural);
#endif
        }
        static string PartLabel(StructuralPartType type)=>type==StructuralPartType.Core?"Rdzeń":
            type==StructuralPartType.Weapon?"Broń":type==StructuralPartType.Wheel?"Koło":"Pancerz";
        void OnGUI()
        {
            if(HandleGuiInput(Event.current))Event.current.Use();
            if(!PlayerScope||R1RunController.Instance.Paused)return;if(scanners==null)Cache();
            if(label==null)label=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};
            var previous=GUI.matrix;float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,Vector3.one*scale);
            try
            {
                float top=188;
                foreach(var scanner in scanners.OfType<RRLStructuralScannerBinding>())
                {
                    if(scanner==null||scanner.Device==null||scanner.Scanner==null)continue;
                    var source=scanner.Scanner;int rows=Mathf.Min(6,source.Contacts.Count);
                    GUI.Box(new Rect(20,top,410,58+rows*22),GUIContent.none);
                    string readiness=!scanner.Device.HardwareAvailable?"Moduł niesprawny":
                        scanner.CooldownLeft>0?$"Odnowienie {scanner.CooldownLeft:0.0} s":"Gotowy • wymaga widocznego celu";
                    GUI.Label(new Rect(30,top+5,390,46),$"K • Skan struktury • 60 m / 85°\n{readiness}",label);
                    int row=0;foreach(var contact in source.Contacts.Take(6))
                        GUI.Label(new Rect(30,top+52+22*row++,390,22),$"{PartLabel(contact.Type)} • HP {contact.HealthFraction*100:0}% • {(contact.Functional?"sprawny":"niesprawny")} • odczyt {source.DisplayLeft:0.0}s",label);
                    top+=66+rows*22;
                }
            }
            finally{GUI.matrix=previous;}
        }
    }
}
