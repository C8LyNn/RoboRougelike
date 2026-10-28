#!/usr/bin/env python3
"""Fail-closed adapter for raw console PresentMon 2.x displayed-frame intervals.
Never labels a CSV as game PASS. Requires original raw CSV, single PID/swapchain and 120s coverage;
CPU main/render, GC and Unity GPU fields remain unpopulated because PresentMon is not Unity Profiler.
"""
import argparse,csv,hashlib,json,math,pathlib,io

FIELDS=['scenario_id','run_id','frame_index','time_ms','frame_ms','cpu_main_ms','cpu_render_ms','gpu_ms','gc_alloc_bytes','managed_heap_bytes','active_guns','active_projectiles','active_vfx','active_colliders','active_renderers']

def convert_bytes(raw,scenario,run,minimum_ms=120000):
    try: s=raw.decode('utf-8-sig')
    except UnicodeError as e: raise ValueError('NOT_UTF8_CSV') from e
    reader=csv.DictReader(io.StringIO(s))
    required={'Application','ProcessID','SwapChainAddress','MsBetweenDisplayChange'}
    if not reader.fieldnames or not required.issubset(reader.fieldnames): raise ValueError('MISSING_RAW_PRESENTMON_COLUMNS')
    if len(reader.fieldnames) != len(set(reader.fieldnames)): raise ValueError('DUPLICATE_RAW_COLUMNS')
    pid=set();swap=set();out=[];elapsed=0.0;ignored=0
    for row in reader:
        # DictReader otherwise hides trailing undeclared cells under key None,
        # and may silently return missing declared cells as None. Neither is
        # acceptable for raw evidence intake. Legitimate extra *named* raw
        # PresentMon columns remain supported.
        if None in row or any(value is None for value in row.values()):
            raise ValueError('MALFORMED_RAW_ROW')
        if row.get('Application','').strip().lower()!='roborougelike.exe': raise ValueError('FOREIGN_PROCESS')
        pid.add(row['ProcessID'].strip());swap.add(row['SwapChainAddress'].strip())
        duration=row['MsBetweenDisplayChange'].strip()
        if duration.upper() in ('NA','N/A',''):
            ignored+=1;continue
        try: ms=float(duration)
        except ValueError as e:raise ValueError('INVALID_DISPLAY_INTERVAL') from e
        if not math.isfinite(ms) or ms<=0:raise ValueError('INVALID_DISPLAY_INTERVAL')
        elapsed+=ms
        out.append([scenario,run,len(out),round(elapsed,6),ms]+['']*10)
    if not pid or len(pid)!=1 or '' in pid:raise ValueError('MIXED_OR_MISSING_PID')
    if len(swap)!=1 or '' in swap:raise ValueError('MIXED_OR_MISSING_SWAPCHAIN')
    if not out: raise ValueError('NO_DISPLAYED_FRAMES')
    if elapsed<minimum_ms:raise ValueError('INSUFFICIENT_DISPLAY_DURATION')
    return out, {'classification':'ORIGINAL_PRESENTMON_CSV_STRUCTURAL_REVIEW_REQUIRED_NOT_UNITY','raw_sha256':hashlib.sha256(raw).hexdigest(), 'process_id':next(iter(pid)), 'swapchain':next(iter(swap)),'raw_rows':len(out)+ignored,'displayed_rows':len(out),'ignored_unavailable_display_interval_rows':ignored,'displayed_coverage_ms':round(elapsed,6),'frame_time_definition':'MsBetweenDisplayChange: display-to-display, not MsBetweenPresents','source_cpu_main_render_gc_memory':'NOT_MEASURED','cpu_gpu_presentmon_fields':'ORIGINAL_RAW_RETAINED_NOT_MAPPED_TO_UNITY'}

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--raw',required=True);ap.add_argument('--scenario',required=True);ap.add_argument('--run',required=True);ap.add_argument('--out',required=True);a=ap.parse_args()
    rows,receipt=convert_bytes(pathlib.Path(a.raw).read_bytes(),a.scenario,a.run)
    target=pathlib.Path(a.out);target.parent.mkdir(parents=True,exist_ok=True)
    with target.open('w',newline='',encoding='utf-8') as fp:
        writer=csv.writer(fp);writer.writerow(FIELDS);writer.writerows(rows)
    target.with_suffix('.receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
    print('STRUCTURAL_REVIEW_REQUIRED_NATIVE_PROFILING_NOT_CONFIRMED')
if __name__=='__main__': main()
