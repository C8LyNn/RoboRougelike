using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.MOV009Integration;
using RoboRougelike.Mechanics.MOV009;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        GUIStyle suspensionHelp;
        GUIStyle SuspensionHelp => suspensionHelp??(suspensionHelp=new GUIStyle(small){fontSize=12,wordWrap=true});
        void SuspensionModes(R1RunController run,float x,float y,float width)
        {
            var model=run.VehicleModel;string[] names={"OFF","Assist","Full"};
            for(int i=0;i<3;i++)
            {
                bool selected=i==0?!model.SuspensionEnabled:model.SuspensionEnabled&&model.SuspensionMode==(i==1?ActiveSuspensionSupportMode.AssistOnly:ActiveSuspensionSupportMode.FullSupport);
                GUI.backgroundColor=selected?new Color(.65f,.78f,.95f):Color.white;
                if(GUI.Button(new Rect(x+i*(width/3),y,width/3-4,34),names[i],compactButton))
                    run.ConfigureSuspension(i!=0,i==1?ActiveSuspensionSupportMode.AssistOnly:i==2?ActiveSuspensionSupportMode.FullSupport:model.SuspensionMode,model.SuspensionHeightOffset);
            }
            GUI.backgroundColor=Color.white;
        }
        void SuspensionHeight(R1RunController run,float x,float y,float width)
        {
            string[] labels={"− 0,1 m","0 m","+ 0,1 m"};
            for(int i=0;i<3;i++)if(GUI.Button(new Rect(x+i*(width/3),y,width/3-4,34),labels[i],compactButton))
                run.ConfigureSuspension(run.VehicleModel.SuspensionEnabled,run.VehicleModel.SuspensionMode,i==1?0:run.VehicleModel.SuspensionHeightOffset+(i==0?-.1f:.1f));
        }
        void DrawSuspensionGarage(R1RunController run)
        {
            var state=MOV009Presentation.Capture(run);
            GUI.Label(new Rect(30,353,280,35),$"{state.Status}\nWysokość: {state.Offset:+0.00;-0.00;0.00} m • sprawne: {state.OperationalCount}/{state.MountedCount}",SuspensionHelp);
            SuspensionModes(run,30,389,280);SuspensionHeight(run,30,425,280);
            string introduction=!string.IsNullOrEmpty(state.Protection)&&run.VehicleModel.SelectedDrive!=R3DriveKind.Hover?
                "Full ograniczony przez sprężyny kół.":"Podpiera kadłub na nierównościach.";
            bool placementError=run.Garage.Tool==RoboRougelike.R4_1.R41GarageTool.Mount&&run.Garage.HasTarget&&run.Garage.PlacementIssue!=null;
            if(!placementError)GUI.Label(new Rect(30,462,280,52),introduction+"\nOFF: wył. • Assist: słabsze • Full: pełne.\n+/−: docelowa wysokość (±0,35 m).\nWybierz Zawieszenie i kliknij zielone widmo.",SuspensionHelp);
        }
        void DrawSuspensionPause(R1RunController run)
        {
            if(run.Player==null||run.VehicleModel==null||run.MapEditing)return;
            var state=MOV009Presentation.Capture(run);
            string controls=state.CanConfigure?"Zmiany zadziałają po wznowieniu.":"Zmień ustawienia w garażu lub na poligonie.";
            GUI.Label(new Rect(230,470,820,58),$"Zawieszenie: {state.Mode} • {state.Status} • sprawne: {state.OperationalCount}/{state.MountedCount}\nOFF: wył. • Assist: słabsze • Full: pełne • wysokość {state.Offset:+0.00;-0.00;0.00} m (±0,35 m). {controls}",small);
            GUI.enabled=state.CanConfigure;
            SuspensionModes(run,230,530,300);SuspensionHeight(run,565,530,320);
            GUI.enabled=true;
        }
        void DrawSuspensionTelemetry(R1RunController run)
        {
            var adapter=run.Player?.GetComponent<MOV009VehicleAdapter>();if(adapter==null)return;
            var state=MOV009Presentation.Capture(run);Panel(new Rect(925,355,335,160));
            GUI.Label(new Rect(940,360,305,28),"ZAWIESZENIE",body);
            GUI.Label(new Rect(940,392,305,50),$"{state.Mode} • {state.Status}\nSprawne moduły: {state.OperationalCount}/{state.MountedCount}",small);
            GUI.Label(new Rect(940,444,305,35),$"Wysokość: {state.Offset:+0.00;-0.00;0.00} m • siła: {state.ForceNewtons:0} N",small);
            if(adapter.GroundingNotice!=null)GUI.Label(new Rect(940,479,305,34),adapter.GroundingNotice,SuspensionHelp);
            else if(!string.IsNullOrEmpty(state.Protection)&&run.VehicleModel.SelectedDrive!=R3DriveKind.Hover)
                GUI.Label(new Rect(940,479,305,34),"Full ograniczony przez sprężyny kół.",SuspensionHelp);
            else GUI.Label(new Rect(940,479,305,30),"Esc na poligonie — ustawienia.",SuspensionHelp);
        }
    }
}
