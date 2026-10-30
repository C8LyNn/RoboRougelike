#!/usr/bin/env python3
"""Static Unity text-serialized .unity/.prefab performance inventory.

NO FPS/PhysX runtime claims. Read-only YAML object boundaries, not a full YAML interpreter.
Never edits or imports Unity source assets.
"""
from __future__ import annotations

import argparse
from collections import Counter
import csv
import io
import hashlib
import json
from pathlib import Path
import re
import sys

MAX_BYTES = 40_000_000
HEADER = re.compile(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\s*$')
FIELD = re.compile(r'^  ([a-zA-Z_][\w]*):\s*(.*?)\s*$')
COMP = re.compile(r'^  - component: \{fileID: (-?\d+)\}\s*$')
GAME_REF = re.compile(r'\{fileID: (-?\d+)\}')
GUID = re.compile(r'\bguid: ([0-9a-fA-F]{32})\b')
CLASSES = {
    1: 'GameObject', 4: 'Transform', 20: 'Camera', 23: 'MeshRenderer',
    33: 'MeshFilter', 54: 'Rigidbody', 64: 'MeshCollider',
    65: 'BoxCollider', 66: 'SphereCollider', 95: 'Animator',
    108: 'Light', 114: 'MonoBehaviour', 120: 'LineRenderer',
    136: 'CapsuleCollider', 198: 'ParticleSystem',
    199: 'ParticleSystemRenderer', 212: 'SpriteRenderer',
}
# Advisory counts, NOT FPS targets or Unity performance acceptance.
DEFAULT_BUDGETS = {
    'GameObject': 300, 'MonoBehaviour': 256, 'MeshRenderer': 150,
    'BoxCollider': 128, 'MeshCollider': 30, 'Rigidbody': 128,
    'ParticleSystem': 64, 'Light': 32,
}

class AuditError(ValueError):
    pass

def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1 << 20), b''):
            digest.update(chunk)
    return digest.hexdigest()

def _parse_document(kind: int, block: list[str]) -> dict:
    result = {'kind': kind, 'fields': {}, 'components': []}
    for line in block:
        match = FIELD.match(line)
        if match:
            result['fields'][match[1]] = match[2]
        comp = COMP.match(line)
        if comp:
            result['components'].append(int(comp[1]))
    return result

def scan(path: Path, limits: dict[str, int] | None = None) -> dict:
    if path.suffix.lower() not in {'.unity', '.prefab'}:
        raise AuditError('only Unity text .unity and .prefab are accepted')
    if not path.is_file() or path.is_symlink():
        raise AuditError('source must be a regular existing file, not a symlink')
    size = path.stat().st_size
    if size > MAX_BYTES:
        raise AuditError(f'source exceeds {MAX_BYTES} bytes; select individual Unity files')
    try:
        data = path.read_text(encoding='utf-8-sig')
    except UnicodeError as exc:
        raise AuditError('Unity YAML must be UTF-8 text') from exc
    if not data.startswith('%YAML 1.1') or '%TAG !u!' not in data[:150]:
        raise AuditError('not text-serialized Unity YAML (binary/unsupported format)')
    objects: dict[int, dict] = {}
    kind = None
    local_id = None
    body = []
    for line in data.splitlines():
        m = HEADER.match(line)
        if m:
            if local_id is not None:
                objects[local_id] = _parse_document(kind, body)
            kind = int(m[1]); local_id = int(m[2]); body = []
            if local_id in objects or local_id == 0:
                raise AuditError('duplicate/invalid local YAML fileID')
        elif line.startswith('--- !u!'):
            raise AuditError('unsupported malformed Unity YAML object header')
        elif local_id is not None:
            body.append(line)
    if local_id is None:
        raise AuditError('no Unity serialized objects found')
    if local_id in objects:
        raise AuditError('duplicate Unity YAML fileID')
    objects[local_id] = _parse_document(kind, body)
    counts = Counter(CLASSES.get(d['kind'], f'Class_{d["kind"]}') for d in objects.values())
    if not counts['GameObject']:
        raise AuditError('scene/prefab contains no GameObject')
    go = {i: d for i,d in objects.items() if d['kind'] == 1}
    problems = []
    roots = {}
    script_counts = Counter()
    for ident, doc in sorted(objects.items()):
        fields=doc['fields']
        kind=doc['kind']
        if kind == 1:
            for compid in doc['components']:
                if compid not in objects:
                    problems.append({'type':'missing_local_component','id':ident,'component_id':compid})
            name=fields.get('m_Name','').strip('"').strip("'")
            roots[ident] = name
        elif kind in (54,64,65,66,136,23,33,95,108,114,198,199,212):
            ref=GAME_REF.search(fields.get('m_GameObject',''))
            if ref and int(ref[1]) not in go:
                problems.append({'type':'missing_game_object','id':ident,'game_object':int(ref[1])})
        if kind == 114:
            script=GUID.search(fields.get('m_Script',''))
            if script:
                script_counts[script[1].lower()] += 1
            else:
                problems.append({'type':'missing_script_guid','id':ident})
        if kind == 64 and fields.get('m_Convex','').strip() in ('0','false','False'):
            ref=GAME_REF.search(fields.get('m_GameObject',''))
            if ref:
                gid=int(ref[1]); same_go=go.get(gid,{}).get('components',[])
                if any(objects.get(component,{}).get('kind') == 54 for component in same_go):
                    problems.append({'type':'nonconvex_meshcollider_with_rigidbody_same_go','id':ident,'game_object':gid})
    budget = dict(DEFAULT_BUDGETS if limits is None else limits)
    for key,val in budget.items():
        if key not in CLASSES.values() or type(val) is not int or val < 0 or val > 1_000_000:
            raise AuditError('invalid component budget '+str(key))
    exceed = [{'type':'advisory_budget_exceeded','component':key,'count':counts.get(key,0),'budget':limit}
              for key,limit in sorted(budget.items()) if counts.get(key,0)>limit]
    all_issues = sorted(problems+exceed,key=lambda p:json.dumps(p,sort_keys=True))
    return {
        'schema':'B01-50-SCENECOST-001/R1',
        'classification':'STATIC_YAML_COMPONENT_INVENTORY_NOT_FPS_OR_NATIVE_PERFORMANCE',
        'source':path.name,'source_sha256':sha256(path),'source_bytes':size,
        'document_count':len(objects),
        'counts':dict(sorted(counts.items())),
        'budgets':dict(sorted(budget.items())),
        'unique_script_guids':len(script_counts),
        'top_script_guids':[{'guid':k,'count':v} for k,v in sorted(script_counts.items(),key=lambda kv:(-kv[1],kv[0]))[:12]],
        'game_object_names':dict(sorted(roots.items())),
        'issues':all_issues,
        'issue_count':len(all_issues),
        'limitations':'Static YAML heuristics and configurable review budgets only; no mesh triangles, draw calls, CPU/GPU cost, dynamic spawns, variants, PhysX runtime, actual FPS or native Unity test.',
    }

def budget_json(path: Path | None) -> dict | None:
    if path is None:
        return None
    raw = json.loads(path.read_text(encoding='utf-8'))
    if not isinstance(raw,dict):
        raise AuditError('budgets must be JSON object')
    return raw

def write_new(path: Path, payload: str) -> None:
    path.parent.mkdir(parents=False,exist_ok=True)  # parent must exist; no arbitrary path creation
    with path.open('x',encoding='utf-8',newline='') as output:
        output.write(payload)

def main(argv=None):
    parser=argparse.ArgumentParser(description='Read-only Unity YAML scene/prefab cost inventory. Never native FPS pass.')
    parser.add_argument('files',nargs='+',type=Path)
    parser.add_argument('--budgets',type=Path)
    parser.add_argument('--out',type=Path)
    parser.add_argument('--csv',type=Path,help='New CSV summary; never overwrites')
    parser.add_argument('--strict',action='store_true',help='Nonzero when advisory issues exceed configured review budgets')
    a=parser.parse_args(argv)
    try:
        destinations=[p for p in (a.out,a.csv) if p is not None]
        if len({p.resolve() for p in destinations}) != len(destinations):
            raise AuditError('JSON and CSV output paths cannot alias')
        originals={p.resolve() for p in a.files}
        for destination in destinations:
            if not destination.parent.is_dir() or destination.exists() or destination.is_symlink():
                raise AuditError('refuse missing parent or overwriting output')
            if destination.resolve() in originals:
                raise AuditError('refuse modifying input source')
        limits=budget_json(a.budgets)
        reports=[scan(p,limits) for p in a.files]
        result={'schema':'B01-50-SCENECOST-001/R1','files':reports,
                'advisory_issues':sum(r['issue_count'] for r in reports),
                'native_unity':'NOT_RUN','measured_fps':'NOT_MEASURED'}
        result_text=json.dumps(result,indent=2,ensure_ascii=False,sort_keys=True)+'\n'
        if a.csv:
            output=io.StringIO(newline='')
            writer=csv.writer(output,lineterminator='\n')
            writer.writerow(['source','component','count','advisory_budget','over_review_budget'])
            for report in reports:
                for component in sorted(set(report['counts'])|set(report['budgets'])):
                    count=report['counts'].get(component,0)
                    limit=report['budgets'].get(component)
                    writer.writerow([report['source'],component,count,'' if limit is None else limit,limit is not None and count>limit])
            csv_payload=output.getvalue()
        if a.out:
            write_new(a.out,result_text)
        else:
            print(result_text,end='')
        if a.csv:
            write_new(a.csv,csv_payload)
        return 4 if a.strict and result['advisory_issues'] else 0
    except (AuditError, OSError, UnicodeError, ValueError) as exc:
        print(f'SCENECOST_REJECTED: {exc}',file=sys.stderr)
        return 2

if __name__=='__main__':
    raise SystemExit(main())
