using System.Linq;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.Mechanics.StructuralScan;
using RoboRougelike.Mechanics.SEN001;
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
            if(input.keyCode==KeyCode.K||input.character=='k'||input.character=='K')return Activate(RRLScanKind.Structural);
            return (input.keyCode==KeyCode.L||input.character=='l'||input.character=='L')&&Activate(RRLScanKind.Pulse);
        }
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current?.kKey.wasPressedThisFrame==true)Activate(RRLScanKind.Structural);
            if(Keyboard.current?.lKey.wasPressedThisFrame==true)Activate(RRLScanKind.Pulse);
#endif
        }
        static string PartLabel(StructuralPartType type)=>type==StructuralPartType.Core?"Rdzeń":
            type==StructuralPartType.Weapon?"Broń":type==StructuralPartType.Wheel?"Koło":"Pancerz";
        static string SignalLabel(ScannerSignalKind kind)=>kind==ScannerSignalKind.Enemy?"Wróg":
            kind==ScannerSignalKind.Salvage?"Wrak":kind==ScannerSignalKind.Hazard?"Zagrożenie":"Cel";
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
                    var source=scanner.Scanner;int rows=Mathf.Min(3,source.Contacts.Count);
                    GUI.Box(new Rect(20,top,410,58+rows*22),GUIContent.none);
                    string readiness=!scanner.Device.HardwareAvailable?"Moduł niesprawny":
                        scanner.CooldownLeft>0?$"Odnowienie {scanner.CooldownLeft:0.0} s":"Gotowy • wymaga widocznego celu";
                    GUI.Label(new Rect(30,top+5,390,46),$"K • Skan struktury • 60 m / 85° • {source.Contacts.Count} części\n{readiness}",label);
                    int row=0;foreach(var contact in source.Contacts.Take(3))
                        GUI.Label(new Rect(30,top+52+22*row++,390,22),$"{PartLabel(contact.Type)} • HP {contact.HealthFraction*100:0}% • {(contact.Functional?"sprawny":"niesprawny")} • odczyt {source.DisplayLeft:0.0}s",label);
                    top+=66+rows*22;
                }
                foreach(var scanner in scanners.OfType<RRLPulseScannerBinding>())
                {
                    if(scanner==null||scanner.Device==null||scanner.Pulse==null)continue;
                    var source=scanner.Pulse;
                    var visible=source.Revealed.Where(signal=>source.IsRevealed(signal)).ToArray();
                    var signals=visible.Take(3).ToArray();
                    GUI.Box(new Rect(20,top,410,58+signals.Length*22),GUIContent.none);
                    string readiness=!scanner.Device.HardwareAvailable?"Moduł niesprawny":
                        scanner.CooldownLeft>0?$"Odnowienie {scanner.CooldownLeft:0.0} s":"Gotowy • sygnały także przez ściany";
                    GUI.Label(new Rect(30,top+5,390,46),$"L • Impuls skanera • 20 m / 3 s • {visible.Length} sygnałów\n{readiness}",label);
                    int row=0;foreach(var signal in signals)
                    {
                        var offset=signal.transform.position-transform.position;
                        float bearing=Vector3.SignedAngle(transform.forward,Vector3.ProjectOnPlane(offset,Vector3.up),Vector3.up);
                        string direction=Mathf.Abs(bearing)>135?"z tyłu":Mathf.Abs(bearing)<45?"z przodu":bearing>0?"po prawej":"po lewej";
                        GUI.Label(new Rect(30,top+52+22*row++,390,22),$"{SignalLabel(signal.SignalKind)} • {offset.magnitude:0} m • {direction}",label);
                    }
                    top+=66+signals.Length*22;
                }
            }
            finally{GUI.matrix=previous;}
        }
    }
}
