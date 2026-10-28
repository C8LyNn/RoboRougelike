// B01-50 PERFVIEW-001 R1: offline inspection of original PERFSCALING-001 live receipts.
// All displayed metrics are Unity Time.unscaledDeltaTime, NOT PresentMon displayed FPS.
// No network, no storage, no access to game runtime, no engine acceptance inference.
export const FRAME_FIELDS = ['frame_index', 'elapsed_ms', 'delta_ms'];
export const OBJECT_FIELDS = ['elapsed_ms', 'scene', 'active_parts', 'active_projectiles', 'active_enemies', 'rigidbodies', 'scan_ms'];
export const CLASSIFICATION = 'GAME_FRAME_TIME_REVIEW_REQUIRED_NOT_DISPLAY_PRESENTMON_OR_NATIVE_PERF_PASS';
const SHA40 = /^[0-9a-f]{40}$/i, SHA64 = /^[0-9a-f]{64}$/i;

function assert(ok, message) { if (!ok) throw new Error(message); }
function finiteNumber(value, label, positive = false) {
  assert(typeof value === 'string' || typeof value === 'number', `${label}: unsupported type`);
  assert(value !== '' && value !== null, `${label}: empty`);
  const n = Number(value);
  assert(Number.isFinite(n) && (positive ? n > 0 : n >= 0), `${label}: nonfinite or negative`);
  return n;
}
function uintString(value, label) {
  assert(typeof value === 'string' && /^(0|[1-9][0-9]*)$/.test(value), `${label}: invalid count`);
  const n = Number(value);
  assert(Number.isSafeInteger(n), `${label}: overflow`);
  return n;
}
export function parseCsv(source, header, maxRows = 30000) {
  assert(typeof source === 'string' && source.length <= 6_000_000, 'CSV: missing or >6MB');
  if (source.charCodeAt(0) === 0xFEFF) source = source.slice(1);
  let rows = [], field = '', cells = [], quoted = false, closed = false;
  const pushCell = () => { cells.push(field); field = ''; closed = false; };
  const pushRow = () => {
    pushCell();
    assert(cells.length === header.length, 'CSV: missing/extra cells');
    rows.push(cells); cells = [];
    assert(rows.length <= maxRows + 1, 'CSV: too many rows');
  };
  for (let i = 0; i < source.length; i++) {
    const c = source[i];
    if (quoted) {
      if (c === '"') { if (source[i + 1] === '"') {field += '"'; i++;} else {quoted = false; closed = true;} }
      else field += c;
    } else if (c === '"') { assert(!closed && field === '', 'CSV: malformed quote'); quoted = true; }
    else if (c === ',') pushCell();
    else if (c === '\r' || c === '\n') {
      if (c === '\r' && source[i + 1] === '\n') i++;
      pushRow();
    } else { assert(!closed, 'CSV: characters after quote'); field += c; }
  }
  assert(!quoted, 'CSV: unclosed quote');
  if (field.length || cells.length) pushRow();
  assert(rows.length >= 2, 'CSV: missing data');
  assert(rows[0].every((c, i) => c === header[i]), 'CSV: unexpected/duplicate header');
  return rows.slice(1).map(row => Object.fromEntries(header.map((key, i) => [key, row[i]])));
}
export function quantile(vals, p) {
  assert(vals.length > 0, 'quantile: empty');
  const a = [...vals].sort((x, y) => x - y), v = (a.length - 1) * p;
  const lo = Math.floor(v), hi = Math.ceil(v);
  return a[lo] + (a[hi] - a[lo]) * (v - lo);
}
function mean(arr) { return arr.reduce((a, b) => a + b, 0) / arr.length; }

export function analyzeCapture(receiptText, frameText, objectText) {
  assert(typeof receiptText === 'string' && receiptText.length < 50000, 'receipt: missing or >50KB');
  let receipt;
  try { receipt = JSON.parse(receiptText); } catch { throw new Error('receipt: invalid JSON'); }
  assert(receipt && !Array.isArray(receipt) && typeof receipt === 'object', 'receipt: invalid object');
  assert(receipt.classification === CLASSIFICATION, 'receipt: unsupported capture classification');
  assert(receipt.unity_version === '6000.3.14f1', 'receipt: unsupported Unity version');
  assert(receipt.capture_complete === true && finiteNumber(receipt.observed_capture_seconds, 'capture seconds') >= 120, 'receipt: incomplete capture');
  assert(SHA40.test(receipt.game_sha), 'receipt: invalid game SHA');
  assert(SHA64.test(receipt.build_zip_sha256), 'receipt: invalid build SHA256');
  assert(typeof receipt.scenario_id === 'string' && receipt.scenario_id.length > 0 && receipt.scenario_id.length <= 128, 'receipt: scenario');
  for (const p of ['cpu', 'gpu', 'os']) assert(typeof receipt[p] === 'string' && receipt[p].trim(), `receipt: missing ${p}`);
  assert(receipt.profiler_main_render_gpu_gc === 'NOT_MEASURED', 'receipt: false profiler counters');
  const rawFrames = parseCsv(frameText, FRAME_FIELDS), rawObjects = parseCsv(objectText, OBJECT_FIELDS, 300);
  assert(rawFrames.length === receipt.frame_rows && rawFrames.length >= 120, 'frames: receipt row count mismatch');
  assert(rawObjects.length === receipt.object_scans && rawObjects.length >= 90, 'objects: receipt scan count mismatch');
  let last = -1;
  const frames = rawFrames.map((row, idx) => {
    assert(uintString(row.frame_index, 'frame index') === idx, 'frames: non-sequential frame index');
    const t = finiteNumber(row.elapsed_ms, 'frame elapsed'), dt = finiteNumber(row.delta_ms, 'frame delta', true);
    assert(t > last && t < 120000, 'frames: nonmonotonic/out-of-range time'); last = t;
    return {t, dt};
  });
  last = -1;
  const objects = rawObjects.map(row => {
    const t = finiteNumber(row.elapsed_ms, 'scan elapsed'), scan = finiteNumber(row.scan_ms, 'scan duration');
    assert(t > last && t < 120000, 'objects: nonmonotonic/out-of-range time'); last = t;
    assert(row.scene && row.scene.length <= 256, 'objects: missing scene');
    return {t, scene:row.scene, parts:uintString(row.active_parts,'parts'),
      shots:uintString(row.active_projectiles,'projectiles'),
      enemies:uintString(row.active_enemies,'enemies'),
      rigidbodies:uintString(row.rigidbodies,'rigidbodies'), scan};
  });
  const deltas = frames.map(f=>f.dt);
  const timeline = Array.from({length: 12}, (_, i) => {
    const lo = i * 10000, hi = (i + 1) * 10000;
    const fs = frames.filter(f => f.t >= lo && f.t < hi).map(f=>f.dt);
    const os = objects.filter(o => o.t >= lo && o.t < hi);
    return {from: i * 10, to: (i + 1) * 10, count:fs.length,
      p95:fs.length ? quantile(fs,.95) : null,
      parts:os.length ? mean(os.map(o=>o.parts)) : null,
      shots:os.length ? mean(os.map(o=>o.shots)) : null,
      enemies:os.length ? mean(os.map(o=>o.enemies)) : null,
      scans:os.length};
  });
  return {
    scenario:receipt.scenario_id, gameSha:receipt.game_sha, buildSha:receipt.build_zip_sha256,
    unity:receipt.unity_version, os:receipt.os, cpu:receipt.cpu, gpu:receipt.gpu,
    scenes:[...new Set(objects.map(o=>o.scene))], frames:frames.length, scans:objects.length,
    captureSeconds:receipt.observed_capture_seconds,
    p50:quantile(deltas,.5), p95:quantile(deltas,.95), p99:quantile(deltas,.99),
    updateFpsEstimate:1000/mean(deltas),
    over33:deltas.filter(x=>x>33.3).length,
    maxScanMs:Math.max(...objects.map(x=>x.scan)),
    meanParts:mean(objects.map(x=>x.parts)), meanShots:mean(objects.map(x=>x.shots)),
    meanEnemies:mean(objects.map(x=>x.enemies)), timeline,
    classification:'FILE_STRUCTURE_VALIDATED_ONLY_NATIVE_PROVENANCE_NOT_VERIFIED',
    metric:'UNITY_UNSCALED_DELTA_NOT_DISPLAYED_FRAME_TIME',
    profiler:'CPU_MAIN_RENDER_GPU_GC_NOT_MEASURED'
  };
}
export function compare(a, b) {
  assert(a && b, 'compare: need two sessions');
  const keys = ['gameSha', 'buildSha', 'unity', 'os', 'cpu', 'gpu'];
  const changed = keys.filter(k => a[k] !== b[k]);
  if (a.scenes.join('|') !== b.scenes.join('|')) changed.push('scenes');
  return {comparable:changed.length === 0, mismatches:changed,
    // Only exploratory relative differences. These are NOT performance conclusions.
    p95DeltaMs:b.p95-a.p95, p99DeltaMs:b.p99-a.p99,
    partDelta:b.meanParts-a.meanParts, shotDelta:b.meanShots-a.meanShots,
    enemyDelta:b.meanEnemies-a.meanEnemies,
    status:changed.length ? 'CONTEXT_DIFFERS_NO_PERFORMANCE_INFERENCE' : 'MATCHED_METADATA_ONLY_NOT_CONTROLLED_AB_EXPERIMENT'};
}
export function makeSyntheticCapture({scenario='SYNTHETIC_A',slow=0,shots=0}={}) {
  const frames = [FRAME_FIELDS.join(',')], objects=[OBJECT_FIELDS.join(',')];
  const count=7200;
  for(let i=0; i<count; i++){
    const t=(i*120000/count), dt=16.4+slow+((i%90)===0?18:0);
    frames.push(`${i},${t.toFixed(6)},${dt.toFixed(3)}`);
  }
  for(let i=0;i<120;i++) objects.push(`${i*1000},Arena,${24+(i>60?48:0)},${shots+(i>50?32:0)},${i>25?8:0},${32+(i>60?48:0)},0.03`);
  const receipt={classification:CLASSIFICATION, game_sha:'a'.repeat(40),
    build_zip_sha256:'b'.repeat(64),unity_version:'6000.3.14f1',
    scenario_id:scenario,capture_complete:true,observed_capture_seconds:120.01,
    profiler_main_render_gpu_gc:'NOT_MEASURED',cpu:'SYNTHETIC ONLY',gpu:'SYNTHETIC ONLY',
    os:'SYNTHETIC ONLY',frame_rows:count,object_scans:120};
  return [JSON.stringify(receipt),frames.join('\n')+'\n',objects.join('\n')+'\n'];
}
