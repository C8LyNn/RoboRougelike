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
            if(kind==RRLScanKind.Pulse)return ActivatePulseGroup();
            if(kind==RRLScanKind.Structural)return ActivateStructuralGroup();
            foreach(var scanner in scanners)
                if(scanner is MonoBehaviour component&&component!=null&&scanner.Kind==kind&&scanner.TryActivate())return true;
            return false;
        }
        bool ActivateStructuralGroup()
        {
            var model=R1RunController.Instance.VehicleModel;
            var group=scanners.OfType<RRLStructuralScannerBinding>().Where(s=>s!=null&&s.Device!=null&&s.Device.Available&&s.Scanner!=null).ToArray();
            if(group.Length==0||model.StructuralScannerReadyAt>Time.time||
               model.Parts.Any(p=>p.Kind==RoboRougelike.R3.R3PartKind.StructuralScanner&&p.DeviceReadyAt>Time.time)||
               group.Any(s=>!s.Scanner.IsReady))return false;
            int units=group.Sum(s=>RoboRougelike.R3.R3VehicleModel.TierEffectUnits(s.Device.State.Tier));
            float cost=group.Sum(s=>RoboRougelike.R3.R3VehicleModel.TierActivationCost(s.Device.State.Tier));
            var config=model.Economy;
            if(float.IsNaN(config.StructuralScanRange)||float.IsInfinity(config.StructuralScanRange)||config.StructuralScanRange<=0||
               float.IsNaN(config.StructuralScanEnergy)||float.IsInfinity(config.StructuralScanEnergy)||config.StructuralScanEnergy<0||
               float.IsNaN(config.StructuralScanCooldown)||float.IsInfinity(config.StructuralScanCooldown)||config.StructuralScanCooldown<0)return false;
            float totalEnergy=config.StructuralScanEnergy*cost;
            if(units<=0||float.IsNaN(totalEnergy)||float.IsInfinity(totalEnergy))return false;
            var source=group.FirstOrDefault(s=>s.ActivateCombined(units,cost,config));if(source==null)return false;
            model.StructuralScannerReadyAt=Time.time+source.Scanner.CooldownLeft;
            foreach(var state in model.Parts.Where(p=>p.Kind==RoboRougelike.R3.R3PartKind.StructuralScanner))state.DeviceReadyAt=model.StructuralScannerReadyAt;
            foreach(var scanner in group.Where(s=>s!=source))scanner.RestoreCombinedCooldown(source.Scanner.CooldownLeft);
            return true;
        }
        bool ActivatePulseGroup()
        {
            var model=R1RunController.Instance.VehicleModel;
            var group=scanners.OfType<RRLPulseScannerBinding>().Where(s=>s!=null&&s.Device!=null&&s.Device.Available&&s.Pulse!=null).ToArray();
            if(group.Length==0||model.ScannerPulseReadyAt>Time.time||
               model.Parts.Any(p=>p.Kind==RoboRougelike.R3.R3PartKind.ScannerPulse&&p.DeviceReadyAt>Time.time)||
               group.Any(s=>!s.Pulse.Ready))return false;
            int units=group.Sum(s=>RoboRougelike.R3.R3VehicleModel.TierEffectUnits(s.Device.State.Tier));
            float cost=group.Sum(s=>RoboRougelike.R3.R3VehicleModel.TierActivationCost(s.Device.State.Tier));
            if(!group[0].ActivateCombined(units,cost))return false;
            model.ScannerPulseReadyAt=Time.time+RRLPulseScannerBinding.CooldownSeconds;
            foreach(var state in model.Parts.Where(p=>p.Kind==RoboRougelike.R3.R3PartKind.ScannerPulse))state.DeviceReadyAt=model.ScannerPulseReadyAt;
            foreach(var scanner in group.Skip(1))scanner.Pulse.RestoreCooldown(RRLPulseScannerBinding.CooldownSeconds);
            return true;
        }
        public bool HandleGuiInput(Event input)
        {
            if(input==null||input.type!=EventType.KeyDown||input.alt||input.control||input.command)return false;
            int key=(int)input.keyCode;
            int index=key>=(int)KeyCode.Alpha1&&key<=(int)KeyCode.Alpha4?key-(int)KeyCode.Alpha1:-1;
            var slots=R1RunController.Instance?.VehicleModel?.GetActiveSlots();
            if(index<0||slots==null)return false;
            return slots[index]==RoboRougelike.R3.R3PartKind.StructuralScanner?Activate(RRLScanKind.Structural):
                slots[index]==RoboRougelike.R3.R3PartKind.ScannerPulse&&Activate(RRLScanKind.Pulse);
        }
        static string PartLabel(StructuralPartType type)=>type==StructuralPartType.Core?"Rdzeń":
            type==StructuralPartType.Weapon?"Broń":type==StructuralPartType.Wheel?"Koło":"Pancerz";
        static string SignalLabel(ScannerSignalKind kind)=>kind==ScannerSignalKind.Enemy?"Wróg":
            kind==ScannerSignalKind.Salvage?"Wrak":kind==ScannerSignalKind.Hazard?"Zagrożenie":"Cel";
        void OnGUI()
        {
            if(!PlayerScope||R1RunController.Instance.Paused)return;if(scanners==null)Cache();
            if(label==null)label=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};
            var previous=GUI.matrix;float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,Vector3.one*scale);
            try
            {
                float top=188;
                var structural=scanners.OfType<RRLStructuralScannerBinding>().Where(s=>s!=null&&s.Device!=null&&s.Scanner!=null).ToArray();
                foreach(var scanner in structural.OrderByDescending(s=>s.Scanner.Contacts.Count).ThenByDescending(s=>s.Device.HardwareAvailable).Take(1))
                {
                    if(scanner==null||scanner.Device==null||scanner.Scanner==null)continue;
                    var source=scanner.Scanner;int rows=Mathf.Min(3,source.Contacts.Count);
                    GUI.Box(new Rect(20,top,410,58+rows*22),GUIContent.none);
                    int active=structural.Count(s=>s.Device.Available);
                    int units=structural.Where(s=>s.Device.Available).Sum(s=>RoboRougelike.R3.R3VehicleModel.TierEffectUnits(s.Device.State.Tier));
                    float cooldown=Mathf.Max(scanner.CooldownLeft,R1RunController.Instance.VehicleModel.StructuralScannerReadyAt-Time.time);
                    string readiness=active==0?"Moduły niesprawne":
                        cooldown>0?$"Odnowienie {cooldown:0.0} s":"Gotowy • wymaga widocznego celu";
                    GUI.Label(new Rect(30,top+5,390,46),$"{R1RunController.Instance.VehicleModel.ActiveSlotLabel(RoboRougelike.R3.R3PartKind.StructuralScanner)} • Skan struktury • {StructuralScanSettings.CombinedRange(R1RunController.Instance.VehicleModel.Economy.StructuralScanRange,units):0} m / 85° • {source.Contacts.Count} części\n{readiness}",label);
                    int row=0;foreach(var contact in source.Contacts.Take(3))
                        GUI.Label(new Rect(30,top+52+22*row++,390,22),$"{PartLabel(contact.Type)} • HP {contact.HealthFraction*100:0}% • {(contact.Functional?"sprawny":"niesprawny")} • odczyt {source.DisplayLeft:0.0}s",label);
                    top+=66+rows*22;
                }
                var pulses=scanners.OfType<RRLPulseScannerBinding>().Where(s=>s!=null&&s.Device!=null&&s.Pulse!=null).ToArray();
                foreach(var scanner in pulses.OrderByDescending(s=>s.Pulse.Revealed.Count).ThenByDescending(s=>s.Device.HardwareAvailable).Take(1))
                {
                    if(scanner==null||scanner.Device==null||scanner.Pulse==null)continue;
                    var source=scanner.Pulse;
                    var visible=source.Revealed.Where(signal=>source.IsRevealed(signal)).ToArray();
                    var signals=visible.Take(3).ToArray();
                    GUI.Box(new Rect(20,top,410,58+signals.Length*22),GUIContent.none);
                    int active=pulses.Count(s=>s.Device.Available);
                    int units=pulses.Where(s=>s.Device.Available).Sum(s=>RoboRougelike.R3.R3VehicleModel.TierEffectUnits(s.Device.State.Tier));
                    float cooldown=Mathf.Max(scanner.CooldownLeft,R1RunController.Instance.VehicleModel.ScannerPulseReadyAt-Time.time);
                    string readiness=active==0?"Moduły niesprawne":
                        cooldown>0?$"Odnowienie {cooldown:0.0} s":"Gotowy • sygnały także przez ściany";
                    GUI.Label(new Rect(30,top+5,390,46),$"{R1RunController.Instance.VehicleModel.ActiveSlotLabel(RoboRougelike.R3.R3PartKind.ScannerPulse)} • Impuls skanera • {RRLPulseScannerBinding.BaseRadius*units:0} m / 3 s • {visible.Length} sygnałów\n{readiness}",label);
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
