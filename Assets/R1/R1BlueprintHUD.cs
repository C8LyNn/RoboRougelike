using System;
using System.Linq;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunHUD
    {
        R3BlueprintLibrary blueprintLibrary;
        R3BlueprintPreview blueprintPreview;
        R3Blueprint blueprintSelected;
        int blueprintOverwriteSlot=-1;
        bool[] blueprintExists;
        string blueprintFeedback;
        bool blueprintApplyPending;
        Vector2 blueprintMissingScroll;

        void OpenBlueprintLibrary()
        {
            OpenSimpleGarageOptions(4);
            try
            {
                blueprintLibrary??=new R3BlueprintLibrary(R3BlueprintLibrary.DefaultDirectory);
                blueprintExists=Enumerable.Range(0,blueprintLibrary.SlotCount).Select(blueprintLibrary.Exists).ToArray();
                blueprintPreview=null;blueprintSelected=null;blueprintOverwriteSlot=-1;blueprintFeedback=null;blueprintApplyPending=false;blueprintMissingScroll=Vector2.zero;
            }
            catch(Exception error){blueprintFeedback="Nie można otworzyć biblioteki: "+error.Message;}
        }
        void SaveBlueprintSlot(R1RunController run,int slot)
        {
            if(blueprintLibrary==null||blueprintExists==null)return;
            if(blueprintExists[slot]&&blueprintOverwriteSlot!=slot)
            {blueprintOverwriteSlot=slot;blueprintFeedback="Kliknij ponownie, aby nadpisać ten projekt.";return;}
            try
            {
                var document=R3BlueprintLibrary.Capture(run.VehicleModel,"Projekt "+(slot+1));
                blueprintLibrary.Save(slot,document);blueprintExists[slot]=true;blueprintOverwriteSlot=-1;
                blueprintSelected=document;blueprintPreview=R3BlueprintLibrary.Preview(document,run.VehicleModel);
                blueprintFeedback="Zapisano układ i sloty. Ekwipunek oraz złom pozostają w tej wyprawie.";
            }
            catch(Exception error){blueprintFeedback="Nie zapisano projektu: "+error.Message;}
        }
        void PreviewBlueprintSlot(R1RunController run,int slot)
        {
            if(blueprintLibrary==null||blueprintExists==null||!blueprintExists[slot])return;
            try
            {
                var document=blueprintLibrary.Load(slot);var preview=R3BlueprintLibrary.Preview(document,run.VehicleModel);
                blueprintSelected=document;blueprintPreview=preview;blueprintOverwriteSlot=-1;blueprintFeedback=null;blueprintMissingScroll=Vector2.zero;
            }
            catch(Exception error){blueprintFeedback="Nie odczytano projektu: "+error.Message;}
        }
        void DrawBlueprintLibrary(R1RunController run,Rect area)
        {
            if(blueprintApplyPending&&!run.Transitioning)
            {
                blueprintApplyPending=false;blueprintFeedback=run.BlueprintApplyMessage;
                if(blueprintSelected!=null)blueprintPreview=R3BlueprintLibrary.Preview(blueprintSelected,run.VehicleModel);
            }
            GUI.Label(new Rect(area.x,area.y,area.width,35),"Projekty zapisują układ i sloty, bez przenoszenia części ani złomu.",small);
            if(blueprintExists==null){GUI.Label(new Rect(area.x,area.y+42,area.width,60),blueprintFeedback??"Biblioteka jest niedostępna.",small);return;}
            float gap=area.width/4;
            for(int i=0;i<blueprintExists.Length;i++)
            {
                float x=area.x+i*gap,w=gap-8;
                GUI.Label(new Rect(x,area.y+42,w,27),"Projekt "+(i+1)+(blueprintExists[i]?" • zapisany":" • pusty"),small);
                if(GUI.Button(new Rect(x,area.y+70,w,38),blueprintOverwriteSlot==i?"Potwierdź zapis":"Zapisz obecny",compactButton))SaveBlueprintSlot(run,i);
                GUI.enabled=blueprintExists[i];
                if(GUI.Button(new Rect(x,area.y+112,w,38),"Sprawdź części",compactButton))PreviewBlueprintSlot(run,i);
                GUI.enabled=true;
            }
            if(!string.IsNullOrEmpty(blueprintFeedback))GUI.Label(new Rect(area.x,area.y+160,area.width,48),blueprintFeedback,small);
            if(blueprintSelected!=null&&blueprintPreview!=null)
            {
                var summary=$"{blueprintSelected.title}: dostępne {blueprintPreview.AvailableCount}/{blueprintPreview.RequiredCount}";
                var missing=blueprintPreview.Missing.GroupBy(x=>x).Select(g=>g.Key+" ×"+g.Count()).ToArray();
                summary+=missing.Length>0?"\n"+string.Join("\n",missing):"\nWszystkie potrzebne części są dostępne.";
                float height=Mathf.Max(130,(missing.Length+2)*24);
                blueprintMissingScroll=GUI.BeginScrollView(new Rect(area.x,area.y+210,area.width,130),blueprintMissingScroll,new Rect(0,0,area.width-20,height));
                try{GUI.Label(new Rect(0,0,area.width-24,height),summary,small);}finally{GUI.EndScrollView();}
            }
            GUI.enabled=blueprintSelected!=null&&blueprintPreview!=null&&blueprintPreview.MissingCount==0&&!run.Transitioning;
            if(GUI.Button(new Rect(area.x,area.y+345,270,42),"Odtwórz z posiadanych",compactButton))
            {
                blueprintApplyPending=run.StartApplyBlueprint(blueprintSelected);
                blueprintFeedback=run.BlueprintApplyMessage;
            }
            GUI.enabled=true;
            GUI.Label(new Rect(area.x+280,area.y+345,area.width-280,45),"Globalne klawisze pozostają bez zmian.",small);
        }
    }
}
