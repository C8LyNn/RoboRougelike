#!/usr/bin/env node
import {performance} from 'node:perf_hooks';
import {writeFileSync,existsSync} from 'node:fs';
import {makeScenario,bruteFirstHitPrevalidated,SpatialGrid,validateSameHits} from './broadphase.mjs';
const scenarios=[
  {name:'2-gun-light',robotParts:24,enemies:8,projectiles:64},
  {name:'6-gun-mid',robotParts:72,enemies:16,projectiles:256},
  {name:'12-gun-heavy',robotParts:144,enemies:32,projectiles:1024},
];
const args=process.argv.slice(2);
function value(flag,def){const at=args.indexOf(flag);return at<0?def:args[at+1];}
const seed=Number(value('--seed','31'));
const runs=Number(value('--runs','3'));
const output=value('--out',null);
if(!Number.isSafeInteger(seed)||seed<0||!Number.isInteger(runs)||runs<1||runs>10|| (args.includes('--out') && !output))throw Error('bad args');
const results=[];
for(const scenario of scenarios){
  const world=makeScenario({...scenario,seed});
  for(let repetition=1;repetition<=runs;repetition++) {
    // Validate targets identically before both algorithms; fair baseline checks pairs only.
    // A new validated index is timed separately below as part of the optimized total.
    new SpatialGrid(world.targets,4);
    let t=performance.now();const baselineCount={tested:0};
    const baseline=world.shots.map(shot=>bruteFirstHitPrevalidated(shot,world.targets,baselineCount));
    const bruteMs=performance.now()-t;
    t=performance.now();const grid=new SpatialGrid(world.targets,4);
    const gridBuildMs=performance.now()-t;
    const gridCount={tested:0,visitedCells:0};
    t=performance.now();const optimized=world.shots.map(shot=>grid.firstHit(shot,gridCount));
    const gridQueryMs=performance.now()-t;
    for(let i=0;i<baseline.length;i++) if(!validateSameHits(baseline[i],optimized[i]))
      throw new Error(`INCORRECT_OPTIMIZATION ${scenario.name} rep=${repetition} shot=${i}`);
    const record={case:scenario.name,seed,repetition,targets:world.targets.length,shots:world.shots.length,
      baselinePairChecks:baselineCount.tested,gridCandidateChecks:gridCount.tested,gridCellsVisited:gridCount.visitedCells,
      gridInserts:grid.inserts,gridBucketCount:grid.cells.size,bruteMs,gridBuildMs,gridQueryMs,
      hitMatches:baseline.length,comparison:'SYNTHETIC_JS_REFERENCE_NOT_UNITY_PHYSX'};
    results.push(record);
    console.log(`${record.case} rep=${repetition} exact=${record.hitMatches}/${record.shots} candidates=${record.gridCandidateChecks}/${record.baselinePairChecks} timeJS=${bruteMs.toFixed(2)}/${(gridBuildMs+gridQueryMs).toFixed(2)}ms`);
  }
}
const receipt={package_id:'B01-50-BROADPHASE-001/R1',engine:'Node.js',classification:'SYNTHETIC_2D_REFERENCE_BENCHMARK_NOT_GAME_PROFILING',seed,runs,scenarioCount:scenarios.length,results};
if(output){if(existsSync(output))throw Error('refuse overwrite');writeFileSync(output,JSON.stringify(receipt,null,2)+'\n');}
console.log('PASS: exact hit ID + earliest swept time parity; native Unity/PhysX NOT_RUN');
