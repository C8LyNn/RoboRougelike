using UnityEngine;
using RoboRougelike.R5;
namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        private string seedText="2026";
        private void DrawRoute(R1RunController run)
        {
            GUI.Label(new Rect(80,45,1120,60),"WYBIERZ TRASĘ",title);
            GUI.Label(new Rect(80,108,1120,40),$"Etap {run.Progress.EncounterIndex+1}/6 • Seed {run.Seed} • Złom {run.VehicleModel.Scrap}",heading);
            for(int i=0;i<6;i++)
            {
                GUI.color=i<run.Progress.CompletedEncounters?new Color(.08f,.3f,.22f):i==run.Progress.EncounterIndex?new Color(.04f,.32f,.38f):new Color(.12f,.15f,.19f);
                GUI.DrawTexture(new Rect(85+i*185,165,160,44),Texture2D.whiteTexture);GUI.color=Color.white;
                var nodeStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
                GUI.Label(new Rect(85+i*185,165,160,44),i==5?"6 • OBLĘŻNIK":$"{i+1} • {(i<run.Progress.CompletedEncounters?"ukończone":i==run.Progress.EncounterIndex?"teraz":"dalej")}",nodeStyle);
            }
            GUI.color=Color.white;
            var offers=run.RouteOffers;
            for(int i=0;i<offers.Length;i++)
            {
                var item=offers[i];float x=80+i*580;
                Panel(new Rect(x,245,550,350));
                GUI.Label(new Rect(x+20,265,510,48),item.Title,heading);
                GUI.Label(new Rect(x+20,320,510,180),$"{R5RunPlan.ArenaName(item.Arena)}\nRyzyko: {item.Risk} • Zagrożenie {R5RunPlan.Threat(item):0.0}\nWrogowie: {item.SpawnCount} • Złom: {item.ScrapReward}\nŁupy: {item.Loot}\n{item.Rule}",body);
                if(GUI.Button(new Rect(x+20,520,510,55),"Wyrusz — "+item.Title,button)){run.ChooseRoute(i);break;}
            }
            GUI.Label(new Rect(80,630,1100,55),"Łupy zależą od stanu modułów. Strzelaj w rdzeń albo odcinaj uzbrojenie. Wybór jest ostateczny dla tego etapu.",small);
        }
    }
}
