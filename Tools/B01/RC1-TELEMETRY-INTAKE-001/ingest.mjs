// B01-10 W13 P0 native RC1 intake. Zero external deps. ORIGINAL files immutable by Git blob SHA-1.
import {createHash} from 'node:crypto';
import {readFileSync,writeFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
export function gitBlobSha(bytes){return createHash('sha1').update('blob '+bytes.length+'\\0').update(bytes).digest('hex');}
function insist(ok,reason){if(!ok)throw new Error(reason);}
function finite(v){return typeof v==='number'&&Number.isFinite(v);}
const FIELDS=['phase','time','x','y','z','speed','contacts','wheels','aliveEnemies'];
function parseTrace(text){
 const lines=text.replace(/^\uFEFF/,'').trim().split(/\r?\n/);
 insist(lines.length>=3,'TRACE_TOO_SHORT');
 insist(lines[0]===FIELDS.join(','),'TRACE_HEADER_CHANGED');
 const values=[];
 for(let i=1;i<lines.length;i++){
  const cells=lines[i].split(',');
  insist(cells.length===FIELDS.length,'TRACE_FIELDS_'+i);
  const row=Object.fromEntries(FIELDS.map((k,j)=>[k,Number(cells[j])]));
  insist(Object.values(row).every(finite),'TRACE_NONFINITE_'+i);
  insist(['phase','contacts','wheels','aliveEnemies'].every(k=>Number.isSafeInteger(row[k])&&row[k]>=0),'TRACE_COUNTER_INVALID_'+i);
  insist(row.speed>=0,'NEGATIVE_SPEED_'+i);
  insist(i===1||row.time>values[i-2].time,'TRACE_NON_MONOTONIC_'+i);
  values.push(row);
 }
 return values;
}
export function ingest({csvBytes,resultBytes,manifest,origin='native_api'}){
 insist(origin==='native_api','ORIGIN_ESCALATION_DENIED');
 insist(manifest.sourceRepo==='C8LyNn/RoboRougelike'&&manifest.sourceGitCommit==='5c29e8e28e7b34fedf0c992fca49451828b02314','UNTRUSTED_SOURCE_COMMIT');
 const src=manifest.sources;
 insist(Array.isArray(src)&&src.length===2,'SOURCE_MANIFEST_INVALID');
 for(const [idx,bytes] of [[0,csvBytes],[1,resultBytes]]){
  insist(Buffer.isBuffer(bytes),'BYTES_REQUIRED');
  insist(typeof src[idx].blobSha1==='string'&&src[idx].blobSha1.length===40&&gitBlobSha(bytes)===src[idx].blobSha1,'SOURCE_BLOB_SHA_MISMATCH_'+idx);
 }
 insist(src[0].path==='Docs/B01_RC1/FinalMapgen1080/drive.csv'&&src[1].path==='Docs/B01_RC1/FinalMapgen1080/rc1-native-result.json','SOURCE_PATH_MISMATCH');
 const rows=parseTrace(csvBytes.toString('utf8')),report=JSON.parse(resultBytes.toString('utf8'));
 insist(report.status==='Passed'&&typeof report.method==='string'&&/API-controlled/i.test(report.method)&&/NOT human gameplay/i.test(report.method),'NON_API_OR_UNVERIFIED_METHOD');
 insist(report.errors?.length===0,'NATIVE_RUNTIME_ERRORS');
 const checks=report.checks;
 insist(Array.isArray(checks)&&checks.length>=10&&checks.every(c=>c.passed===true),'NATIVE_OBSERVATION_FAILED');
 for(const keyword of ['Real R6 grounded force drive','Real hostile and player projectile damage','Controlled win uses existing salvage']){
  insist(checks.some(x=>x.name===keyword),'MISSING_REQUIRED_NATIVE_CHECK');
 }
 insist(rows.length===manifest.expected.sampleRows,'TRACE_ROW_COUNT_MISMATCH');
 insist(rows[0].phase===manifest.expected.firstPhase&&rows.every(r=>r.phase===rows[0].phase),'TRACE_PHASE_CHANGED');
 insist(report.contactSamples===manifest.expected.summaryContactSamples&&report.supportedSamples===manifest.expected.summarySupportedSamples,'SUMMARY_CONTACT_COUNT_CHANGED');
 insist(finite(report.distance)&&report.distance>0&&finite(report.measuredEnvelope?.x),'SUMMARY_PHYSICAL_INVALID');
 insist(Math.abs(report.measuredEnvelope.x-manifest.expected.measuredRigWidthM)<0.0001,'UNEXPECTED_MEASURED_RIG');
 const coords=(a,b)=>Math.hypot(a.x-b.x,a.y-b.y,a.z-b.z);
 const chord=coords(rows[0],rows.at(-1));
 const distanceTrace=rows.slice(1).reduce((s,r,i)=>s+coords(rows[i],r),0);
 insist(distanceTrace>=chord-0.03,'TRACE_PATH_BELOW_CHORD');
 insist(distanceTrace<=report.distance+0.10,'TRACE_EXCEEDS_NATIVE_DISTANCE');
 insist(rows.every(r=>r.contacts<=r.wheels&&r.contacts>=0),'INVALID_WHEEL_CONTACT_AUTHORITY');
 const maxSpeed=Math.max(...rows.map(r=>r.speed));
 const counts={total:rows.length,supported:rows.filter(r=>r.contacts>0).length};
 const warnings=['SAMPLE_DRIVE_CSV_NOT_FULL_NATIVE_CONTACT_SERIES','CONTROLLED_WIN_NOT_UNASSISTED','NO_PER_BUILD_SEED_MATRIX','NO_HUMAN_INPUT_PLAYTEST','NO_RECOVERY_ECONOMY_CHOICE_METRICS'];
 return{
  contract:'B01-RC1-TELEMETRY-INTAKE-001-R1',state:'NATIVE_API_SAMPLE_VALIDATED_NOT_HUMAN_PLAYTEST',
  provenance:{repo:manifest.sourceRepo,sourceGitCommit:manifest.sourceGitCommit,sourcePR:manifest.sourcePR,driveGitBlobSha1:src[0].blobSha1,resultGitBlobSha1:src[1].blobSha1},
  method:'Codex native Windows x64 API-controlled R3/R6 physical test (cited external evidence)',origin:'native_api',
  verifiedFields:{rigWidthM:report.measuredEnvelope.x,rigHeightM:report.measuredEnvelope.y,rigLengthM:report.measuredEnvelope.z,physicsContactSamples:report.contactSamples,physicsSupportedSamples:report.supportedSamples,reportedTravelDistanceM:report.distance,actualPlayerHpLost:report.playerHpLost,actualEnemyHpLost:report.enemyHpLost,originalCheckCount:checks.length},
  traceMetrics:{traceRows:counts.total,traceAllRowsWithWheelContact:counts.supported===counts.total,traceStartSeconds:rows[0].time,traceEndSeconds:rows.at(-1).time,traceDurationSeconds:Number((rows.at(-1).time-rows[0].time).toFixed(3)),traceIntegratedDistanceM:Number(distanceTrace.toFixed(4)),traceDisplacementM:Number(chord.toFixed(4)),peakSampleSpeedMps:maxSpeed,peakContacts:Math.max(...rows.map(r=>r.contacts)),maxWheelCount:Math.max(...rows.map(r=>r.wheels))},
  gameplayExperiment001:{automaticConversionAllowed:false,requiredMissing:['seed-attributed player-run terminal event','human-vs-API signed input provenance','final repair/salvage choice','garage before/after timing','build identity/budget','real 3x3x100 paired seed matrix'],eventRowsGenerated:0},
  qualityWarnings:warnings,independentB0160Unity:'NOT_RUN',humanPlaytest:'NOT_RUN',G2:'HOLD'
 };
}
const directory=new URL('.',import.meta.url);
if(process.argv[1]===fileURLToPath(import.meta.url)){
 const args=process.argv.slice(2),manifest=JSON.parse(readFileSync(args[0]||new URL('manifest.json',directory),'utf8'));
 const drive=readFileSync(args[1]||new URL('samples/drive.csv',directory)),result=readFileSync(args[2]||new URL('samples/rc1-native-result.json',directory));
 const output=ingest({csvBytes:drive,resultBytes:result,manifest,origin:process.env.B01_TELEMETRY_ORIGIN||'native_api'});
 if(args[3]==='--output'&&args[4])writeFileSync(args[4],JSON.stringify(output,null,2)+'\n');
 console.log(JSON.stringify(output,null,2));
}
