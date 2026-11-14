using UnityEngine;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        void DrawSafeZone(R1RunController run)
        {
            if(run.SafeZoneTrading)
            {
                Panel(new Rect(190,75,900,570));
                GUI.Label(new Rect(230,95,820,55),"SPRZEDAWCA",heading);
                DrawSafeVendor(run,new Rect(230,155,820,390));
                if(GUI.Button(new Rect(230,570,250,48),"Wróć do strefy",compactButton))run.CloseSafeZoneTrade();
                return;
            }
            Panel(new Rect(20,18,630,110));
            GUI.Label(new Rect(36,28,590,30),"BEZPIECZNA STREFA",heading);
            GUI.Label(new Rect(36,65,590,45),run.NearSafeZoneVendor?
                "Jesteś przy sprzedawcy. Możesz sprawdzić oferty.":"Podjedź do sprzedawcy, aby zobaczyć oferty.",small);
            if(run.NearSafeZoneVendor&&GUI.Button(new Rect(20,140,250,48),"Porozmawiaj",compactButton))
            {garageOptionsFeedback=null;vendorOfferPage=0;run.OpenSafeZoneTrade();}
            if(GUI.Button(new Rect(20,196,250,48),"Wróć do garażu",compactButton))run.ReturnFromSafeZone();
            if(GUI.Button(new Rect(20,252,250,48),"Zapisz wyprawę",compactButton))run.SaveRunCheckpoint();
            if(!string.IsNullOrEmpty(run.RunSaveMessage))GUI.Label(new Rect(20,305,510,55),run.RunSaveMessage,small);
            GUI.enabled=run.EmergencyRescueCooldownLeft<=0;
            if(GUI.Button(new Rect(20,365,250,48),"Ratunek",compactButton))run.TryEmergencyRescue();
            GUI.enabled=true;
        }
    }
}
