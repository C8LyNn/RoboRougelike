using System.Linq;
using UnityEngine;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.WeaponIntegration
{
    public static class RRLEmpVehicleAdapter
    {
        public static void Configure(Transform owner)
        {
            var assembly=owner.GetComponent<R3VehicleAssembly>();
            var enemy=owner.GetComponent<P12PipelineSafeEnemyTurret>();
            foreach(var hp in owner.GetComponentsInChildren<P12PipelineSafeDamageable>())
            {
                var part=hp.GetComponent<R3MountedPart>();R3PartState state=null;
                bool electronic;
                if(part!=null&&assembly!=null)
                {electronic=part.Kind!=R3PartKind.Armor&&part.Kind!=R3PartKind.Core&&part.Kind!=R3PartKind.BonusModule;state=assembly.Model.Parts.First(p=>p.Id==part.Id);}
                else electronic=hp.GetComponent<RRLDeviceBinding>()!=null||RoboRougelike.R6.R6VehicleDrive.IsWheel(hp)||
                    (enemy!=null&&(hp==enemy.WeaponBlock||enemy.VariantWeapons.Any(g=>g.Block==hp)));
                if(!electronic||hp.GetComponent<RRLEmpPartState>()!=null)continue;
                hp.gameObject.AddComponent<RRLEmpPartState>().Bind(owner,state);
            }
            owner.GetComponent<RoboRougelike.R6.R6VehicleDrive>()?.RefreshParts();
        }
    }
}
