using UnityEngine;
using RoboRougelike.R5;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        static bool UsePortraitRoute(int width,int height,bool route,bool overlay)=>width>0&&height>width&&route&&!overlay;
        static Rect PortraitRouteCard(int index)=>new Rect(24,215+index*300,720,280);
        static Rect PortraitRouteButton(int index)=>new Rect(44,429+index*300,680,48);
        static Rect PortraitRouteGarageButton()=>new Rect(24,938,720,54);
        void DrawPortraitRoute(R1RunController run)
        {
            GUI.Label(new Rect(24,20,720,50),"WYBIERZ TRASĘ",title);
            GUI.Label(new Rect(24,78,720,36),$"Etap {run.Progress.EncounterIndex+1}/6 • Seed {run.Seed} • Złom {run.VehicleModel.Scrap}",heading);
            var nodeStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
            for(int i=0;i<6;i++)
            {
                GUI.color=i<run.Progress.CompletedEncounters?new Color(.08f,.3f,.22f):i==run.Progress.EncounterIndex?new Color(.04f,.32f,.38f):new Color(.12f,.15f,.19f);
                var node=new Rect(24+i*120,132,110,54);GUI.DrawTexture(node,Texture2D.whiteTexture);GUI.color=Color.white;
                GUI.Label(node,RouteStageCaption(i,run.Progress.CompletedEncounters,run.Progress.EncounterIndex,true),nodeStyle);
            }
            var offers=run.RouteOffers;
            for(int i=0;i<offers.Length;i++)
            {
                var item=offers[i];var card=PortraitRouteCard(i);Panel(card);
                GUI.Label(new Rect(card.x+20,card.y+15,680,38),item.Title,heading);
                GUI.Label(new Rect(card.x+20,card.y+55,680,146),$"{R5RunPlan.ArenaName(item.Arena)}\nRyzyko: {item.Risk} • Zagrożenie {R5RunPlan.Threat(item):0.0}\nWrogowie: {item.SpawnCount} • {RouteRewardText(run.VehicleModel,item.ScrapReward)}\nŁupy: {item.Loot}\n{item.Rule}",body);
                if(GUI.Button(PortraitRouteButton(i),"Wyrusz — "+item.Title,button)){run.ChooseRoute(i);break;}
            }
            GUI.Label(new Rect(24,840,720,90),"Odebrane części są sprawne; złom za niewybrane zależy od stanu wraków. Wybór jest ostateczny dla tego etapu.",small);
            if(GUI.Button(PortraitRouteGarageButton(),"Wróć do garażu",button))run.ReturnToGarageFromRoute();
        }
    }
}
