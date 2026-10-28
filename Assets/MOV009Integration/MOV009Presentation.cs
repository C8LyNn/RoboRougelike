using System.Linq;
using RoboRougelike.Mechanics.MOV009;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.MOV009Integration
{
    // Read-only presentation of the existing adapter and its current physics diagnostics.
    public static class MOV009Presentation
    {
        public struct Snapshot
        {
            public string Mode,Status,Protection;
            public int MountedCount,OperationalCount;
            public float Offset,ForceNewtons;
            public bool Applying,CanConfigure;
        }
        public static Snapshot Capture(R1RunController run)
        {
            var result=new Snapshot{Mode="OFF",Status="Brak pojazdu."};
            var model=run?.VehicleModel;
            if(model==null||run.Progress==null)return result;
            result.Mode=!model.SuspensionEnabled?"OFF":model.SuspensionMode==ActiveSuspensionSupportMode.AssistOnly?"Assist":"Full";
            result.Offset=model.SuspensionHeightOffset;
            result.MountedCount=model.Parts.Count(p=>p.Kind==R3PartKind.Suspension);
            result.CanConfigure=!run.Transitioning&&(run.Progress.State==R1RunState.Garage||run.Progress.State==R1RunState.TestDrive);
            var adapter=run.Player!=null?run.Player.GetComponent<MOV009VehicleAdapter>():null;
            var controller=adapter!=null?adapter.Controller:null;
            result.OperationalCount=adapter!=null?adapter.OperationalSupports:0;
            result.Protection=adapter!=null?adapter.ProtectionReason:null;
            if(result.MountedCount==0){result.Status="Brak modułu — zamontuj zawieszenie.";return result;}
            if(!model.SuspensionEnabled){result.Status="Wyłączone — wybierz Assist lub Full.";return result;}
            if(model.SelectedDrive==R3DriveKind.Hover){result.Status="Nieaktywne — wybrano napęd Hover.";return result;}
            if(controller==null||result.OperationalCount==0){result.Status="Brak sprawnych modułów zawieszenia.";return result;}
            if(!controller.isActiveAndEnabled||!controller.SuspensionEnabled){result.Status="Nieaktywne — sprawdź napęd i moduły.";return result;}
            bool simulationRunning=!run.Paused&&!run.Transitioning&&Time.timeScale>0&&
                (run.Progress.State==R1RunState.Combat||run.Progress.State==R1RunState.TestDrive)&&
                run.Player!=null&&!run.Player.GetComponent<Rigidbody>().isKinematic;
            result.Applying=simulationRunning&&controller.LastTotalLiftNewtons>.01f;
            result.ForceNewtons=simulationRunning?controller.LastTotalLiftNewtons:0;
            result.Status=result.Applying?"Pracuje — podpiera pojazd.":simulationRunning?
                "Gotowe — teraz nie podaje siły.":"Gotowe — zadziała po ruszeniu.";
            return result;
        }
    }
}
