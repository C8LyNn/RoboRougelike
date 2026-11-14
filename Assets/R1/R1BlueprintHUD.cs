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
        int blueprintPage;
        bool[] blueprintExists;
        string blueprintFeedback;
        bool blueprintApplyPending;
        bool blueprintRestoreControls;
        Vector2 blueprintMissingScroll;

        void OpenBlueprintLibrary()
        {
            OpenSimpleGarageOptions(4);
            try
            {
                int slots=Mathf.Clamp(R1RunController.Instance?.VehicleModel?.Economy?.BlueprintSlotCount??R3BlueprintLibrary.DefaultSlots,1,R3BlueprintLibrary.MaximumSlots);
                if(blueprintLibrary==null||blueprintLibrary.SlotCount!=slots)
                    blueprintLibrary=new R3BlueprintLibrary(blueprintLibrary?.RootDirectory??R3BlueprintLibrary.DefaultDirectory,slots);
                blueprintExists=Enumerable.Range(0,blueprintLibrary.SlotCount).Select(blueprintLibrary.Exists).ToArray();
                blueprintPage=0;blueprintPreview=null;blueprintSelected=null;blueprintOverwriteSlot=-1;blueprintFeedback=null;blueprintApplyPending=false;blueprintRestoreControls=false;blueprintMissingScroll=Vector2.zero;
            }
            catch(Exception error){blueprintFeedback="Nie można otworzyć biblioteki: "+error.Message;}
        }
        void ChangeBlueprintPage(int delta)
        {
            if(blueprintExists==null||blueprintExists.Length==0)return;
            int last=(blueprintExists.Length-1)/4;
            blueprintPage=(int)Math.Min(last,Math.Max(0L,(long)blueprintPage+delta));
            blueprintOverwriteSlot=-1;
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
                blueprintSelected=document;blueprintPreview=preview;blueprintOverwriteSlot=-1;blueprintFeedback=null;blueprintRestoreControls=false;blueprintMissingScroll=Vector2.zero;
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
            if(blueprintExists==null){GUI.Label(new Rect(area.x,area.y+42,area.width,60),blueprintFeedback??"Biblioteka jest niedostępna.",small);return;}
            bool pages=blueprintExists.Length>4;
            GUI.Label(new Rect(area.x,area.y,pages?area.width-185:area.width,35),"Projekty zapisują układ i sloty, bez przenoszenia części ani złomu.",small);
            if(pages)
            {
                int pageCount=(blueprintExists.Length+3)/4;bool enabled=GUI.enabled;
                GUI.enabled=enabled&&blueprintPage>0;
                if(GUI.Button(new Rect(area.x+area.width-180,area.y,42,32),"‹",compactButton))ChangeBlueprintPage(-1);
                GUI.enabled=enabled;GUI.Label(new Rect(area.x+area.width-130,area.y+3,75,28),$"{blueprintPage+1}/{pageCount}",small);
                GUI.enabled=enabled&&blueprintPage+1<pageCount;
                if(GUI.Button(new Rect(area.x+area.width-48,area.y,42,32),"›",compactButton))ChangeBlueprintPage(1);
                GUI.enabled=enabled;
            }
            float gap=area.width/4;
            int first=blueprintPage*4;
            for(int i=first;i<Math.Min(first+4,blueprintExists.Length);i++)
            {
                float x=area.x+(i-first)*gap,w=gap-8;
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
                blueprintApplyPending=blueprintRestoreControls?run.StartApplyBlueprintWithControls(blueprintSelected):run.StartApplyBlueprint(blueprintSelected);
                blueprintFeedback=run.BlueprintApplyMessage;
            }
            GUI.enabled=true;
            GUI.enabled=blueprintSelected?.globalBindings!=null&&!run.Transitioning;
            blueprintRestoreControls=GUI.Toggle(new Rect(area.x+280,area.y+345,area.width-280,45),blueprintRestoreControls,"Przywróć również przypisania klawiszy");
            GUI.enabled=true;
        }
    }
}
