using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunController
    {
        public string BlueprintApplyMessage {get;private set;}
        public bool StartApplyBlueprint(R3Blueprint document)
        {
            if(Transitioning||Paused||QuitRequested||Progress?.State!=R1RunState.Garage||Player==null||VehicleModel==null)return false;
            try
            {
                // Copy the description so no UI caller can change it mid-transaction.
                var snapshot=JsonUtility.FromJson<R3Blueprint>(JsonUtility.ToJson(document));
                var plan=R3BlueprintLibrary.Plan(snapshot,VehicleModel);
                if(plan.Preview.MissingCount>0)
                {BlueprintApplyMessage=$"Brakuje {plan.Preview.MissingCount} części. Napraw lub zdobądź je przed montażem.";return false;}
                Transitioning=true;BlueprintApplyMessage="Sprawdzanie projektu i montażu…";
                StartCoroutine(ApplyBlueprintTransaction(snapshot,plan));return true;
            }
            catch(Exception error){BlueprintApplyMessage="Nie można odtworzyć projektu: "+error.Message;return false;}
        }
        R1VehicleState BuildBlueprintStage(R3VehicleModel model,Vector3 position,Quaternion rotation)
        {
            var result=R3VehicleAssembly.Build(model,Progress.Stats,palette,position,rotation);
            foreach(var renderer in result.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            Physics.SyncTransforms();return result;
        }
        static bool ApplyBlueprintSlots(R3VehicleModel model,R3Blueprint document)
        {
            for(int i=0;i<4;i++)if(document.weapons[i]>=0&&!model.AssignWeaponSlot((R4WeaponKind)document.weapons[i],i))return false;
            for(int i=0;i<4;i++)if(document.active[i]>=0&&!model.AssignActiveSlot((R3PartKind)document.active[i],i))return false;
            for(int i=0;i<4;i++)if(document.deployables[i]>=0&&!model.AssignDeployableSlot((R3PartKind)document.deployables[i],i))return false;
            return model.GetWeaponSlots().Select(x=>x.HasValue?(int)x.Value:-1).SequenceEqual(document.weapons)&&
                model.GetActiveSlots().Select(x=>x.HasValue?(int)x.Value:-1).SequenceEqual(document.active)&&
                model.GetDeployableSlots().Select(x=>x.HasValue?(int)x.Value:-1).SequenceEqual(document.deployables);
        }
        IEnumerator ApplyBlueprintTransaction(R3Blueprint document,R3BlueprintPlan plan)
        {
            var original=Player;var oldModel=VehicleModel;R3VehicleModel working=null;
            var position=original.transform.position;var rotation=original.transform.rotation;
            float energy=oldModel.Energy.Current;var charges=oldModel.Charges.Snapshot();
            R1VehicleState stage=null;bool committed=false;
            try
            {
                BlueprintApplyMessage="Nie udało się odtworzyć projektu; obecny pojazd pozostaje bez zmian.";
                working=oldModel.DeepCopy();
                original.gameObject.SetActive(false);
                stage=BuildBlueprintStage(working,position,rotation);yield return null;
                var keep=new HashSet<string>(plan.Placements.Where(p=>p.AlreadyPlaced).Select(p=>p.Id),StringComparer.Ordinal);
                while(working.Parts.Any(p=>p.Kind!=R3PartKind.Core&&!keep.Contains(p.Id)))
                {
                    bool removed=false;
                    foreach(var part in working.Parts.Where(p=>p.Kind!=R3PartKind.Core&&!keep.Contains(p.Id)).ToArray())
                        if(working.Remove(part.Cell)){removed=true;break;}
                    if(!removed){BlueprintApplyMessage="Nie można bezpiecznie zdjąć części lub brakuje miejsca w magazynie.";yield break;}
                    stage.gameObject.SetActive(false);Destroy(stage.gameObject);stage=BuildBlueprintStage(working,position,rotation);yield return null;
                }
                var pending=plan.Placements.Where(p=>!p.AlreadyPlaced).ToList();
                while(pending.Count>0)
                {
                    bool mounted=false;string reason=null;
                    foreach(var place in pending.ToArray())
                    {
                        var part=working.Inventory.FirstOrDefault(p=>p.Id==place.Id);
                        var item=document.parts.FirstOrDefault(p=>p.cell+working.Parts.Single(x=>x.Kind==R3PartKind.Core).Cell==place.Cell);
                        if(part==null||item==null||!item.Matches(part)){reason="Część zmieniła swój wariant podczas zdejmowania.";continue;}
                        reason=working.OwnedPlacementError(place.Id,place.Cell,place.Rotation);
                        if(reason!=null||!working.Mount(place.Id,place.Cell,place.Rotation))continue;
                        var mountedPart=working.Parts.First(p=>p.Id==place.Id);
                        if(mountedPart.ContactMask!=item.contacts)
                        {BlueprintApplyMessage="Nie można odtworzyć zapisanych punktów mocowania; pojazd pozostaje bez zmian.";yield break;}
                        pending.Remove(place);mounted=true;break;
                    }
                    if(!mounted){BlueprintApplyMessage="Nie można zamontować projektu: "+(reason??"brak zgodnych połączeń.");yield break;}
                    stage.gameObject.SetActive(false);Destroy(stage.gameObject);stage=BuildBlueprintStage(working,position,rotation);yield return null;
                }
                if(!ApplyBlueprintSlots(working,document)){BlueprintApplyMessage="Nie można odtworzyć przypisań slotów.";yield break;}
                // Temporary staging may shrink capacity; do not spend stored resources.
                working.RefreshEnergyLayout();working.Energy.Restore(energy);working.Charges.Restore(charges);
                stage.gameObject.SetActive(false);Destroy(stage.gameObject);stage=null;
                var replacement=R3VehicleAssembly.Build(working,Progress.Stats,palette,position,rotation);
                try
                {
                    VehicleModel=working;Player=replacement;
                    followCamera.Configure(Player.transform,Player.CameraPivot);
                    R3BuildValidation.SetGarageSockets(Player.GetComponent<R3VehicleAssembly>(),true);Garage.RefreshPreview();
                    committed=true;BlueprintApplyMessage="Odtworzono projekt z posiadanych części.";Destroy(original.gameObject);
                }
                finally
                {
                    if(!committed)
                    {
                        replacement.gameObject.SetActive(false);Destroy(replacement.gameObject);
                        VehicleModel=oldModel;Player=original;followCamera.Configure(original.transform,original.CameraPivot);
                    }
                }
            }
            finally
            {
                if(stage!=null){stage.gameObject.SetActive(false);Destroy(stage.gameObject);}
                if(!committed&&original!=null){original.gameObject.SetActive(true);Physics.SyncTransforms();}
                Transitioning=false;if(!QuitRequested&&Garage!=null)Garage.Message=BlueprintApplyMessage;
            }
        }
    }
}
