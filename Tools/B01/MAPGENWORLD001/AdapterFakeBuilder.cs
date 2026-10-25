// TEST-ONLY simulated source builder. Never include in real Assets/Unity assembly or ZIP.
using B01.MAPGEN001;
using UnityEngine;
namespace B01.MAPGEN001 {
    public sealed class MapGenBuilder : MonoBehaviour {
        public MapResult Current { get; private set; }
        public bool FailNext;
        public bool CorruptNextHash;
        public MapResult Generate(MapRecipe recipe,int seed,MapGenConfig config,MapVehicleProfile vehicle) {
            if(FailNext){FailNext=false;throw new System.InvalidOperationException("FAKE_BUILD_FAILURE");}
            var plan=MapGenPlanner.Plan(recipe,seed,config,vehicle);
            var result=new MapResult {
                Recipe=recipe,Seed=seed,GenerationVersion=plan.GenerationVersion,
                GeometryHash=plan.GeometryHash,Start=plan.Start,Finish=plan.Finish,
                Spawns=plan.EnemySpawns.ToArray(),CheckpointDescriptors=plan.Checkpoints.ToArray(),
                GeneratedRoot=new GameObject(),Bounds=plan.Bounds,Diagnostics=plan.Diagnostics
            };
            if(CorruptNextHash){CorruptNextHash=false;result.GeometryHash="CORRUPTED_FAKE_OUTPUT";}
            Clear();
            Current=result;
            return result;
        }
        public void Clear() {
            if(Current==null)return;
            Current.GeneratedRoot.activeInHierarchy=false;
            Current=null;
        }
    }
}
