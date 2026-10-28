#!/usr/bin/env python3
"""Strict input validator and frame-time summarizer. Never upgrades synthetic data to runtime PASS."""
import argparse, csv, json, math, statistics
from pathlib import Path

COLS=('scenario_id','run_id','frame_index','time_ms','frame_ms','cpu_main_ms',
      'cpu_render_ms','gpu_ms','gc_alloc_bytes','managed_heap_bytes','active_guns',
      'active_projectiles','active_vfx','active_colliders','active_renderers')
OPTIONAL_FLOAT=('cpu_main_ms','cpu_render_ms','gpu_ms')
OPTIONAL_INT=('gc_alloc_bytes','managed_heap_bytes','active_guns','active_projectiles',
              'active_vfx','active_colliders','active_renderers')
PINNED='7299dcc5fd63ac042a0ffcd97b69d02fb8744649'
PINNED_ZIP='ec3193bb3ac24398c496ea65ad12aa653321db160a4dc74e8f618e3cc7ad6d1d'

def finite(raw,field,positive=False):
    try: value=float(raw)
    except (TypeError,ValueError): raise ValueError(f'{field}: non-numeric')
    if not math.isfinite(value) or (value<=0 if positive else value<0):
        raise ValueError(f'{field}: invalid value')
    return value

def require(condition,message):
    if not condition:raise ValueError(message)

def quantile(sorted_vals,q):
    require(bool(sorted_vals),'percentile needs frames')
    rank=(len(sorted_vals)-1)*q
    a,b=math.floor(rank),math.ceil(rank)
    return sorted_vals[a]+(sorted_vals[b]-sorted_vals[a])*(rank-a)

def read_csv(path):
    with Path(path).open(newline='',encoding='utf-8-sig') as f:
        reader=csv.DictReader(f)
        require(reader.fieldnames is not None,'missing header')
        require(tuple(reader.fieldnames)==COLS,'unexpected/incorrect CSV columns')
        out=[]
        for line,row in enumerate(reader,start=2):
            require(None not in row,'extra CSV fields')
            require(all(v is not None for v in row.values()),f'row {line}: missing field')
            require(row['scenario_id']!='' and row['run_id']!='',f'row {line}: missing run ID')
            try:idx=int(row['frame_index'])
            except ValueError:raise ValueError(f'row {line}: invalid frame index')
            require(idx>=0,f'row {line}: negative frame index')
            values={k:finite(row[k],k,positive=(k=='frame_ms')) for k in ('time_ms','frame_ms')}
            values.update({k:(None if row[k]=='' else finite(row[k],k)) for k in OPTIONAL_FLOAT})
            for k in OPTIONAL_INT:
                if row[k]=='':values[k]=None
                else:
                    try:v=int(row[k])
                    except ValueError:raise ValueError(f'row {line}: {k} not integer')
                    require(v>=0,f'row {line}: negative {k}')
                    values[k]=v
            values.update({'frame_index':idx,'scenario_id':row['scenario_id'],'run_id':row['run_id']})
            out.append(values)
        require(len(out)>=2,'too few frames')
        expect_id,expect_run=out[0]['scenario_id'],out[0]['run_id']
        for i,v in enumerate(out):
            require(v['scenario_id']==expect_id and v['run_id']==expect_run,'mixed run or scenario')
            if i:
                prev=out[i-1]
                require(v['frame_index']==prev['frame_index']+1,'frame gaps or duplicates')
                require(v['time_ms']>prev['time_ms'],'non-monotonic capture time')
        return out

def validate_meta(meta,rows):
    require(meta.get('source_sha')==PINNED,'unverified source SHA')
    require(meta.get('player_zip_sha256')==PINNED_ZIP,'unverified release ZIP SHA')
    require(meta.get('scenario_id')==rows[0]['scenario_id'],'scenario mismatch')
    require(meta.get('run_id')==rows[0]['run_id'],'run mismatch')
    require(meta.get('origin') in ('SYNTHETIC','WINDOWS_PLAYER_CAPTURE'),'invalid origin')
    origin=meta['origin']
    if origin=='WINDOWS_PLAYER_CAPTURE':
        require(meta.get('authorization_url'),'real run: required HQ authorization evidence')
        require(meta.get('host') and meta.get('cpu') and meta.get('gpu') and meta.get('driver'),
                'real run: missing hardware/driver')
        require(meta.get('graphics_api'),'real run: missing renderer API')
        require(meta.get('mode') in ('RELEASE','DEVELOPMENT'),'real run: specify build mode')
        require(meta.get('width') in (1280,1920) and meta.get('height') in (720,1080),
                'real run: unsupported resolution')
        require((meta['width'],meta['height']) in ((1280,720),(1920,1080)),
                'real run: mismatched width/height')
        require(meta.get('warmup_seconds',0)>=30,'real run: missing warmup')
        require(meta.get('capture_seconds',0)>=120,'real run: insufficient scheduled duration')
        require(meta.get('tool') in ('PRESENTMON','UNITY_PROFILER','APP_TELEMETRY'),
                'real run: evidence acquisition tool missing')
        if meta['tool']=='PRESENTMON':
            # PASS13 PresentMon adapter stores the *END* of each captured display
            # interval. The complete capture includes the first interval, so
            # last_time - first_time silently drops one real frame. Its cumulative
            # times are rounded to 6 decimals by the adapter.
            cumulative=0.0
            for row in rows:
                cumulative+=row['frame_ms']
                require(abs(row['time_ms']-cumulative)<=0.05,
                        'real run: PresentMon cumulative display timestamps mismatch')
            require(cumulative>=120000-0.001,
                    'real run: captured duration <120 s')
        else:
            # For a non-PresentMon source the timestamp defines captured span.
            # Do not invent display-interval completeness for other profilers.
            require(rows[-1]['time_ms']-rows[0]['time_ms']>=120000,
                    'real run: captured duration <120 s')
        require(meta.get('repetition') in (1,2,3),'real run: missing repetition index')
    return origin

def summary(rows,meta):
    origin=validate_meta(meta,rows)
    fs=sorted(v['frame_ms'] for v in rows)
    slowest=fs[-max(1,math.ceil(len(fs)*0.01)):]
    nonzero=[x for x in rows if x['gc_alloc_bytes'] is not None]
    gpu=[x['gpu_ms'] for x in rows if x['gpu_ms'] is not None]
    cpum=[x['cpu_main_ms'] for x in rows if x['cpu_main_ms'] is not None]
    cpur=[x['cpu_render_ms'] for x in rows if x['cpu_render_ms'] is not None]
    def nullable_mean(values):return round(statistics.mean(values),4) if values else None
    return {
      'classification':('SYNTHETIC_TEST_ONLY' if origin=='SYNTHETIC' else 'WINDOWS_CAPTURE_REVIEW_REQUIRED'),
      'runtime_pass':False, # FPS/quality threshold must be approved + independently evaluated.
      'scenario_id':meta['scenario_id'],'run_id':meta['run_id'],'source_sha':meta['source_sha'],
      'sample_count':len(fs),
      'observed_duration_s':round((sum(v['frame_ms'] for v in rows) if origin=='WINDOWS_PLAYER_CAPTURE' and meta.get('tool')=='PRESENTMON' else rows[-1]['time_ms']-rows[0]['time_ms'])/1000,4),
      'duration_basis':('PRESENTMON_CUMULATIVE_DISPLAY_INTERVALS' if origin=='WINDOWS_PLAYER_CAPTURE' and meta.get('tool')=='PRESENTMON' else 'END_MINUS_FIRST_TIMESTAMP'),
      'mean_fps':round(1000/statistics.mean(fs),4),
      'one_percent_low_fps':round(1000/statistics.mean(slowest),4),
      'p50_frame_ms':round(quantile(fs,.50),4),
      'p95_frame_ms':round(quantile(fs,.95),4),
      'p99_frame_ms':round(quantile(fs,.99),4),
      'frames_over_33_3ms':sum(v>33.3 for v in fs),
      'frames_over_50ms':sum(v>50 for v in fs),
      'mean_cpu_main_ms':nullable_mean(cpum),
      'mean_cpu_render_ms':nullable_mean(cpur),
      'mean_gpu_ms':nullable_mean(gpu),
      'total_observed_gc_alloc_bytes':sum(x['gc_alloc_bytes'] for x in nonzero) if len(nonzero)==len(rows) else None,
      'gc_allocation_sample_coverage':round(len(nonzero)/len(rows),4),
      'gpu_sample_coverage':round(len(gpu)/len(rows),4),
      'note':'Unsupported/absent metrics are null, never imputed zero. 1% low is inverse mean of slowest ceil(1%) frame times.'
    }

def main():
    p=argparse.ArgumentParser()
    p.add_argument('--csv',required=True)
    p.add_argument('--metadata',required=True)
    p.add_argument('--output',required=True)
    a=p.parse_args()
    meta=json.loads(Path(a.metadata).read_text(encoding='utf-8'))
    result=summary(read_csv(a.csv),meta)
    Path(a.output).write_text(json.dumps(result,indent=2,sort_keys=True)+'\n',encoding='utf-8')
    print(json.dumps({'classification':result['classification'],'n':result['sample_count'],'output':a.output}))
if __name__=='__main__':main()
