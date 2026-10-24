using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.MOV009Integration;
using RoboRougelike.Mechanics.MOV009;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        void SuspensionModes(R1RunController run,float x,float y,float width)
        {
            var model=run.VehicleModel;string[] names={"OFF","Assist","Full"};
            for(int i=0;i<3;i++)
            {
                bool active=i==0?!model.SuspensionEnabled:model.SuspensionEnabled&&model.SuspensionMode==(i==1?ActiveSuspensionSupportMode.AssistOnly:ActiveSuspensionSupportMode.FullSupport);
                GUI.backgroundColor=active?new Color(.5f,.9f,.85f):Color.white;
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
            GUI.Label(new Rect(30,355,280,27),$"{(run.VehicleModel.SelectedDrive==R3DriveKind.Hover?"Hover: MOV009 OFF":"Montuj nisko")} • {run.VehicleModel.SuspensionHeightOffset:+0.00;-0.00;0.00} m",small);
            SuspensionModes(run,30,384,280);SuspensionHeight(run,30,420,280);
        }
        void DrawSuspensionPause(R1RunController run)
        {
            if(run.Player?.GetComponent<MOV009VehicleAdapter>()==null)return;
            GUI.Label(new Rect(230,480,820,45),$"Zawieszenie • offset: {run.VehicleModel.SuspensionHeightOffset:+0.00;-0.00;0.00} m • regulacja zadziała po wznowieniu",small);
            SuspensionModes(run,230,530,300);SuspensionHeight(run,565,530,320);
        }
        void DrawSuspensionTelemetry(R1RunController run)
        {
            var adapter=run.Player?.GetComponent<MOV009VehicleAdapter>();if(adapter==null)return;
            var controller=adapter.Controller;Panel(new Rect(925,355,335,160));
            GUI.Label(new Rect(940,360,305,28),"AKTYWNE ZAWIESZENIE",body);
            string mode=!controller.SuspensionEnabled?"OFF":controller.SupportMode.ToString();
            GUI.Label(new Rect(940,392,305,82),$"{mode} • sprawne podpory: {adapter.OperationalSupports}\nOffset: {controller.CurrentRideHeightOffsetMeters:+0.00;-0.00;0.00} m • siła: {controller.LastTotalLiftNewtons:0} N\nEsc na poligonie — regulacja",small);
            if(adapter.GroundingNotice!=null)GUI.Label(new Rect(940,463,305,48),adapter.GroundingNotice,small);
        }
    }
}
