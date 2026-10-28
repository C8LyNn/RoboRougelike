// B01-10 W17 CHL-003 six-trial measurement intake. Pure data review, NEVER PhysX proof.
import {readFileSync} from 'node:fs';
import {pathToFileURL} from 'node:url';

export const SOURCE_PIN = '30c0355a62764eb37798347612564ee51f3b36e1';
export const COLUMNS = ['profile','attempt','massKg','widthM','lengthM',
  'elapsedSec','state','physicsSamples','collisionEntries','contactPoints',
  'barrierPoints','rampPoints','bridgePoints','groundPoints','falls',
  'checkpoints','completions','peakSpeedMps','travelMeters','minHeightM','maxHeightM'];
export const METRICS = ['elapsedSec','peakSpeedMps','rampContactsPerMeter',
  'bridgeContactsPerMeter','barrierContactsPerMeter'];

function csvRows(text) {
  if(typeof text!=='string') throw Error('CSV_TEXT_REQUIRED');
  const lines=text.replace(/\r/g,'').trim().split('\n').filter(Boolean);
  if(lines[0]!==COLUMNS.join(',')) throw Error('CHL003_SUMMARY_HEADER_MISMATCH');
  if(lines.length===1) return [];
  return lines.slice(1).map((line,index)=>{
    if(line.includes('"')||line.includes('\t')) throw Error('NONCANONICAL_CSV_ROW_'+index);
    const values=line.split(',');
    if(values.length!==COLUMNS.length) throw Error('ROW_FIELD_COUNT_'+index);
    const item={};
    COLUMNS.forEach((col,i)=>{
      if(['profile','state'].includes(col)) item[col]=values[i];
      else {
        if(values[i].trim()==='')throw Error('EMPTY_METRIC_'+col+'_'+index);
        item[col]=Number(values[i]);
        if(!Number.isFinite(item[col]))throw Error('NONFINITE_'+col+'_'+index);
      }
    });
    return item;
  });
}
const positive=['massKg','widthM','lengthM','elapsedSec','physicsSamples','peakSpeedMps','travelMeters'];
const nonnegative=['collisionEntries','contactPoints','barrierPoints','rampPoints','bridgePoints',
 'groundPoints','falls','checkpoints','completions'];
const integers=['attempt','physicsSamples','collisionEntries','contactPoints','barrierPoints',
 'rampPoints','bridgePoints','groundPoints','falls','checkpoints','completions'];

function dataIssues(rows){
 const errors=[],seen=new Set(),counts={LIGHT:0,HEAVY:0};
 for(const r of rows){
   if(!(r.profile in counts))errors.push('UNKNOWN_PROFILE');
   else counts[r.profile]++;
   if(!Number.isInteger(r.attempt)||r.attempt<1)errors.push('BAD_ATTEMPT');
   for(const n of positive)if(!(r[n]>0))errors.push('NONPOSITIVE_'+n);
   for(const n of nonnegative)if(r[n]<0)errors.push('NEGATIVE_'+n);
   for(const n of integers)if(!Number.isInteger(r[n]))errors.push('NONINTEGER_'+n);
   const key=r.profile+'/'+r.attempt;
   if(seen.has(key))errors.push('DUPLICATE_ATTEMPT_'+key);
   seen.add(key);
 }
 if(rows.length!==6||counts.LIGHT!==3||counts.HEAVY!==3)errors.push('REQUIRE_EXACT_3_EACH');
 for(const type of ['LIGHT','HEAVY']){
   const selected=rows.filter(r=>r.profile===type);
   if(selected.length===3 && new Set(selected.map(x=>x.massKg)).size!==1)
     errors.push('MASS_CHANGED_BETWEEN_TRIALS_'+type);
   if(selected.length===3 && new Set(selected.map(x=>x.widthM)).size!==1)
     errors.push('WIDTH_CHANGED_BETWEEN_TRIALS_'+type);
 }
 return [...new Set(errors)];
}
const median=arr=>{const a=arr.slice().sort((x,y)=>x-y);return a[1];};
function metricRows(rows,key){
 const values=rows.map(r=> {
  if(key==='rampContactsPerMeter')return r.rampPoints/r.travelMeters;
  if(key==='bridgeContactsPerMeter')return r.bridgePoints/r.travelMeters;
  if(key==='barrierContactsPerMeter')return r.barrierPoints/r.travelMeters;
  return r[key];
 });
 return {values,median:median(values),min:Math.min(...values),max:Math.max(...values),
  range:Math.max(...values)-Math.min(...values)};
}
function discrimination(rows){
 const groups={LIGHT:rows.filter(r=>r.profile==='LIGHT'),
  HEAVY:rows.filter(r=>r.profile==='HEAVY')};
 const comparisons=METRICS.map(name=>{
  const l=metricRows(groups.LIGHT,name),h=metricRows(groups.HEAVY,name);
  const gap=Math.abs(l.median-h.median);
  const separated=l.min>h.max || h.min>l.max;
  const beyondWithinSpread=gap>Math.max(l.range,h.range);
  return {name,light:l,heavy:h,betweenMedianGap:gap,
    separatedFromWithinRigVariation:separated&&beyondWithinSpread};
 });
 return {comparisons,discriminatingMetrics:comparisons.filter(x=>x.separatedFromWithinRigVariation).map(x=>x.name)};
}
export function review(summaryCsv){
 let rows;
 try{ rows=csvRows(summaryCsv); }
 catch(e){return{status:'INVALID_INPUT',reason:e.message,nativeUnity:'NOT_RUN_BY_THIS_TOOL',g2:'HOLD'};}
 if(rows.length===0)return{status:'NOT_RUN',reason:'NO_NATIVE_TRIAL_ROWS',trials:0,nativeUnity:'NOT_RUN',g2:'HOLD'};
 const issues=dataIssues(rows);
 if(issues.length)return{status:'INVALID_INPUT',reason:issues.join('|'),trials:rows.length,nativeUnity:'NOT_VERIFIED',g2:'HOLD'};
 const fails=[];
 for(const r of rows){
  const id=r.profile+'/'+r.attempt;
  if(r.state!=='Completed')fails.push(id+':NOT_COMPLETED');
  if(r.checkpoints!==4)fails.push(id+':CHECKPOINT_COUNT_'+r.checkpoints);
  if(r.completions!==1)fails.push(id+':COMPLETIONS_'+r.completions);
  if(r.falls!==0)fails.push(id+':FALL');
  if(r.rampPoints===0)fails.push(id+':NO_RAMP_CONTACT');
  if(r.bridgePoints===0)fails.push(id+':NO_BRIDGE_CONTACT');
  if(r.physicsSamples<=4)fails.push(id+':INSUFFICIENT_SAMPLES');
  if(r.travelMeters<=35)fails.push(id+':INSUFFICIENT_DISTANCE');
 }
 const score=discrimination(rows);
 return {status:fails.length?'DESIGN_ACCEPTANCE_FAIL':'DATA_READY_NATIVE_EVIDENCE_PENDING',
  trials:rows.length,perProfile:{LIGHT:3,HEAVY:3},acceptanceErrors:fails,
  scorecard:score,physicalDiscrimination:fails.length?'UNASSESSED':
    score.discriminatingMetrics.length?'CANDIDATE_REPRODUCIBLE_DIFFERENCE':'INCONCLUSIVE_NO_STABLE_DIFFERENCE',
  evidenceRequired:['Unity Editor/Player.log source+binary SHA','Unity Test XML / process exit',
    'exact source scene GUID','control protocol fixed across rigs','Physics50Hz and renderFPS',
    'native wheel-ground and airborne input/velocity/force traces','B01-60 independent review'],
  nativeUnity:'NOT_VERIFIED_BY_CSV',g2:'HOLD'};
}
if(process.argv[1] && import.meta.url===pathToFileURL(process.argv[1]).href){
 const file=process.argv[2];
 const txt=file?readFileSync(file,'utf8'):COLUMNS.join(',')+'\n';
 const receipt=review(txt);
 console.log(JSON.stringify(receipt,null,2));
 if(receipt.status==='INVALID_INPUT'||receipt.status==='DESIGN_ACCEPTANCE_FAIL')
  process.exitCode=1;
}
