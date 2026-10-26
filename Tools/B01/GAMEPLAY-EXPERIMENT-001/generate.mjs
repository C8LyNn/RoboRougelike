import {writeFileSync} from 'node:fs';
// Synthetic stimuli only; not measured gameplay. Build tradeoff distributions engineered to be non-dominating.
let rng=0xB01B01;const random=()=>{rng=(Math.imul(rng,1664525)+1013904223)>>>0;return rng/4294967296};
const builds=['Allrounder','HeavyGunner','FastRunner'];const challenges=['defense','race','boss'];
const success={Allrounder:[.7,.65,.6],HeavyGunner:[.88,.36,.8],FastRunner:[.48,.91,.42]};
const rows=[];for(let b=0;b<3;b++)for(let ch=0;ch<3;ch++)for(let seed=0;seed<100;seed++){
 const finished=random()<success[builds[b]][ch];const wheelDamage=seed%13===0?1:0;
 rows.push({eventId:`SYN-${builds[b]}-${challenges[ch]}-${seed}`,origin:'synthetic_fixture',buildId:builds[b],challenge:challenges[ch],seed,
  finished,seconds:Math.round(50+random()*110),repairScrap:Math.round((b===1?35:b===2?15:25)+random()*20),
  lootedNonArmor:Math.floor(random()*4),lootedArmor:0,wheelDamage,
  steeringBeforeDegSec:42+Math.round(random()*9),steeringAfterDegSec:wheelDamage?22+Math.round(random()*9):43+Math.round(random()*9),
  partsChangedAfterRun:seed%3===0?1:0,garageSeconds:Math.round(50+random()*160),firstMeaningfulActionSeconds:2+Math.round(random()*22)});
 }
writeFileSync(new URL('./SYNTHETIC_FIXTURES.json',import.meta.url),JSON.stringify(rows,null,2)+'\n');console.log('Generated',rows.length,'synthetic events; never real player data');
