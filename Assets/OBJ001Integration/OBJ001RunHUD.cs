using UnityEngine;
namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        bool DrawReactorCombatStatus(R1RunController run)
        {
            if(!run.ReactorActive)return false;
            GUI.Label(new Rect(940,28,305,40),"OBRONA REAKTORA",heading);
            GUI.Label(new Rect(940,70,305,27),$"Reaktor: {run.ReactorHp:0}/{run.ReactorMaxHp:0} HP",body);
            GUI.Label(new Rect(940,101,305,27),$"Fala {run.DefenseWave}/{run.DefenseWaveCount} • Wrogowie: {run.Encounter.AliveCount}",body);
            GUI.Label(new Rect(940,132,305,29),run.DefenseIntermission>0?$"Następna fala za {run.DefenseIntermission:0.0} s":"Przetrwaj wszystkie trzy fale",small);return true;
        }
        void DrawReactorGarageAction(R1RunController run)
        {
            if(run.ReactorPrepared)GUI.Label(new Rect(355,78,560,40),"Przygotuj pojazd • 3 fale • Reaktor 600 HP",body);
            else if(run.CanChooseReactorDefense&&GUI.Button(new Rect(650,80,265,38),"Zamiast trasy: obrona reaktora",compactButton))run.ChooseReactorDefense();
        }
    }
}
