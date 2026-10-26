using UnityEngine;
using RoboRougelike.MapSystemR4;
namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        MapR4Mode roadSelection;
        static string ActivityGoal(R1RunController run)=>run.CheckpointRace!=null?"Zalicz bramki i metę":
            run.RoadChallenge!=null?run.RoadChallenge.Mode==MapR4Mode.TimeAttack?"Checkpoint → meta":
            run.RoadChallenge.Mode==MapR4Mode.Escape?"Dotrzyj do wyjścia":run.RoadChallenge.Mode==MapR4Mode.Survival?"Przetrwaj fale":"Pokonaj Oblężnika":
            run.ReactorActive?"Chroń reaktor":"Zniszcz rdzenie przeciwników";
        void DrawFullActivityGarageActions(R1RunController run)
        {
            if(run.SandboxWorkshop||run.ReactorPrepared)return;
            GUI.enabled=run.CanChooseCheckpointRace;
            if(GUI.Button(new Rect(355,124,280,38),"Wyścig • checkpointy",compactButton))run.ChooseCheckpointRace();
            GUI.enabled=run.CanChooseRoadChallenge;
            if(GUI.Button(new Rect(650,124,235,38),R1RunController.RoadModeName(roadSelection),compactButton))run.ChooseRoadChallenge(roadSelection);
            if(GUI.Button(new Rect(890,124,40,38),"›",compactButton))roadSelection=(MapR4Mode)(((int)roadSelection+1)%4);
            GUI.enabled=true;
        }
        bool DrawFullActivityCombatStatus(R1RunController run)
        {
            if(run.CheckpointRace==null&&run.RoadChallenge==null)return false;
            GUI.Label(new Rect(940,28,305,48),run.CurrentEncounter.Title,heading);
            string text=run.CheckpointRace!=null?$"Bramka {run.CheckpointRace.NextCheckpoint+1}/{run.CheckpointRace.CheckpointCount}\nCzas {run.CheckpointRace.ElapsedSeconds:0.0} / 180 s":
                $"{run.CurrentEncounter.Rule}\nCzas {run.RoadChallenge.ElapsedSeconds:0.0} s";
            GUI.Label(new Rect(940,80,305,90),text,small);return true;
        }
    }
}
