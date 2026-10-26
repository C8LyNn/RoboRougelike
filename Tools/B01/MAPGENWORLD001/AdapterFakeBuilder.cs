// TEST-ONLY simulated source builder. Never include in real Assets/Unity assembly or ZIP.
using B01.MAPGEN001;
using UnityEngine;
namespace B01.MAPGEN001 {
    public enum FakeWorldFault {
        None, StartOutsideWorldBounds, FinishOutsideWorldBounds,
        CheckpointOutsideWorldBounds, SpawnOutsideWorldBounds,
        MissingCheckpoint, WrongSeed, WrongVersion, NonFiniteFinish
    }
    public sealed class MapGenBuilder : MonoBehaviour {
        public MapResult Current { get; private set; }
        public bool FailNext;
        public bool CorruptNextHash;
        public FakeWorldFault NextFault;
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
            switch(NextFault) {
                case FakeWorldFault.StartOutsideWorldBounds:
                    result.Start=new Vector3(5000f,1.25f,0f);break;
                case FakeWorldFault.FinishOutsideWorldBounds:
                    result.Finish=new Vector3(0f,1.25f,5000f);break;
                case FakeWorldFault.CheckpointOutsideWorldBounds:
                    if(result.CheckpointDescriptors.Length>0)
                        result.CheckpointDescriptors[0]=new Vector3(5000f,1.25f,5000f);
                    break;
                case FakeWorldFault.SpawnOutsideWorldBounds:
                    if(result.Spawns.Length>0)
                        result.Spawns[0]=new Vector3(-5000f,1.25f,0f);
                    break;
                case FakeWorldFault.MissingCheckpoint:
                    result.CheckpointDescriptors=System.Array.Empty<Vector3>();break;
                case FakeWorldFault.WrongSeed:result.Seed=seed+1;break;
                case FakeWorldFault.WrongVersion:result.GenerationVersion+=1;break;
                case FakeWorldFault.NonFiniteFinish:
                    result.Finish=new Vector3(float.NaN,1.25f,0f);break;
            }
            NextFault=FakeWorldFault.None;
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
