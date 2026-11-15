using System.Collections.Generic;
using RoboRougelike.R1;
using RoboRougelike.PackageIntegration;
using RoboRougelike.WeaponIntegration;
using UnityEngine;

namespace RoboRougelike.R3
{
    // Provisional radar: detects live enemy vehicles through cover, never individual parts.
    public static class R3PassiveRadar
    {
        public static void Collect(R1RunController run,R3VehicleAssembly player,List<Vector3> contacts)
        {
            contacts.Clear();
            if(player==null||run==null)return;
            float range=player.PassiveDetectionRange(R3PartKind.RadarModule);
            if(range<=0)return;
            var origin=player.transform.position;int team=RRLHitRouter.Team(player.transform);
            var seen=new HashSet<int>();
            foreach(var enemy in RRLScanWorld.LiveEnemies(run))
            {
                if(enemy==null||enemy.IsDefeated||!enemy.gameObject.activeInHierarchy||!seen.Add(enemy.GetInstanceID()))continue;
                int other=RRLHitRouter.Team(enemy.transform);
                if(team!=0&&other==team)continue;
                var point=enemy.transform.position;
                double dx=(double)point.x-origin.x,dz=(double)point.z-origin.z;
                if(dx*dx+dz*dz<=(double)range*range)contacts.Add(point);
            }
        }
        public static Vector2 MapOffset(Vector3 origin,Vector3 forward,Vector3 target,float range)
        {
            if(float.IsNaN(range)||float.IsInfinity(range)||range<=0)return Vector2.zero;
            forward.y=0;if(forward.sqrMagnitude<.0001f)forward=Vector3.forward;forward.Normalize();
            Vector3 right=new Vector3(forward.z,0,-forward.x),delta=target-origin;
            return new Vector2(Vector3.Dot(delta,right)/range,-Vector3.Dot(delta,forward)/range);
        }
    }
}
