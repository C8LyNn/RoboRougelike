#!/usr/bin/env python3
"""B01-13 AIM-DELTA001: private side-by-side reviewer for verified AIM-OBS001 screenshots.

Does not run Unity, identify human participants, prove hits, or judge whether an aim change is better.
The generated HTML embeds ORIGINAL screenshot pixels; it MUST remain private.
"""
from __future__ import annotations
import argparse
import base64
import hashlib
import html
import importlib.util
import json
import os
from pathlib import Path
import re
import sys
import tempfile

PINNED_RECEIVER_SHA256 = '8e6cd46bc5f1229b54e565a1b74ea9387d27073d463f61268809f212a9e2ddf4'
CASES = ('CLOSE_STATIC','FAR_STATIC','OCCLUDED','ZOOM_MOVING')
IMAGE_MIME = {'.png':'image/png','.jpg':'image/jpeg','.jpeg':'image/jpeg','.webp':'image/webp'}
SHA40 = re.compile(r'[0-9a-f]{40}\Z')
SHA64 = re.compile(r'[0-9a-f]{64}\Z')
class DeltaError(ValueError): pass

def need(test, message):
    if not test: raise DeltaError(message)

def receiver_from(path:Path):
    need(path.is_file() and not path.is_symlink(),'Original receiver file is missing or linked')
    content=path.read_bytes()
    need(hashlib.sha256(content).hexdigest()==PINNED_RECEIVER_SHA256,'Original receiver SHA256 changed; unsafe unreviewed importer')
    spec=importlib.util.spec_from_file_location('b0113_immutable_aim_receiver',path)
    need(spec is not None and spec.loader is not None,'Receiver import unavailable')
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

def load_pair(before:Path, before_images:Path, after:Path, after_images:Path,
              before_game:str, before_zip:str, after_game:str, after_zip:str, receiver:Path):
    need(all(SHA40.fullmatch(x) for x in (before_game,after_game)),'Full lower-case game commit SHAs required')
    need(all(SHA64.fullmatch(x) for x in (before_zip,after_zip)),'Full lower-case Player ZIP SHAs required')
    base=receiver_from(receiver)
    # Deliberately use the existing independent original-source physical image-byte gate.
    base.verify(before,before_images,before_game,before_zip)
    base.verify(after,after_images,after_game,after_zip)
    old=base.load_json(before);new=base.load_json(after)
    need(old['mode']==new['mode'],'Different data modes must not be combined')
    need(old['mode'] in ('SYNTHETIC','OPERATOR_SOLO'),'Human-approved data must not be embedded in an HTML report')
    need(old['sid']!=new['sid'],'Before/after must have distinct session identifiers')
    need(before.resolve()!=after.resolve(),'The same receipt cannot be compared against itself')
    b={x['case']:x for x in old['frames']};a={x['case']:x for x in new['frames']}
    need(set(b)==set(a)==set(CASES),'Original four cases missing')
    pairs=[]
    for case in CASES:
        x,y=b[case],a[case]
        need(x['group']==y['group'] and x['input']==y['input'],f'{case}: group/input changed; incomparable trial')
        need((x['image']['width'],x['image']['height'])==(y['image']['width'],y['image']['height']),f'{case}: image sizes differ; pixel deltas prohibited')
        pairs.append((case,x,y,before_images/x['image']['filename'],after_images/y['image']['filename']))
    return {'mode':old['mode'],'before_session':old['sid'],'after_session':new['sid'],
        'before_game_sha':before_game,'after_game_sha':after_game,
        'before_zip_sha256':before_zip,'after_zip_sha256':after_zip,'pairs':pairs}

def marker(point,label,width,height):
    if point is None:return ''
    return f'<b class="mark {label}" style="left:{100*point["x"]/width:.8f}%;top:{100*point["y"]/height:.8f}%" title="{label} ({point["x"]},{point["y"]})" aria-label="{label} ({point["x"]},{point["y"]})">{label[0].upper()}</b>'

def panel(frame,image_path):
    # The upstream receiver checked this file earlier, but files can change after
    # that check (editor/sync/replace). Bind the ACTUAL bytes embedded in the
    # private HTML to the independently verified receipt, not merely its path.
    need(image_path.is_file() and not image_path.is_symlink(),
         'IMAGE_CHANGED_AFTER_VERIFICATION: missing or linked screenshot')
    img=image_path.read_bytes()
    need(hashlib.sha256(img).hexdigest()==frame['image']['sha256'],
         'IMAGE_CHANGED_AFTER_VERIFICATION: embedded bytes differ from verified receipt')
    mime=IMAGE_MIME[image_path.suffix.lower()]
    w,h=frame['image']['width'],frame['image']['height']
    data=base64.b64encode(img).decode('ascii')
    marks=''.join(marker(frame['points'].get(k),k,w,h) for k in ('reticle','target','impact'))
    # No remote URL; image bytes and marker geometry never leave this private HTML.
    return (f'<div class="frame" style="--w:{w};--h:{h}"><img src="data:{mime};base64,{data}" width="{w}" height="{h}" alt="Zrzut {html.escape(frame["case"])}" />'
           f'<div class="marker-layer" aria-hidden="true">{marks}</div></div>')

def fmt_delta(x,y):
    if x is None or y is None:return '<span class="muted">brak porównywalnego pomiaru</span>'
    d=round(y-x,2)
    return f'{x:.2f} → {y:.2f} px <strong>Δ {d:+.2f} px</strong>'

def render(info):
    mode=info['mode']; parts=[]
    for index,(case,x,y,xb,ya) in enumerate(info['pairs']):
        metric_a=x['metrics'];metric_b=y['metrics']
        parts.append(f'''<section class="case" data-case="{case}" {'hidden' if index else ''}>
<h2>{case} · {x['group']} / {x['input']} · {x['image']['width']}×{x['image']['height']} px</h2>
<div class="two"><article><h3>PRZED · {info['before_session']}</h3>{panel(x,xb)}</article>
<article><h3>PO · {info['after_session']}</h3>{panel(y,ya)}</article></div>
<div class="metrics"><p>Celownik → cel: {fmt_delta(metric_a['reticle_target_px'],metric_b['reticle_target_px'])}</p>
<p>Uderzenie → cel: {fmt_delta(metric_a['impact_target_px'],metric_b['impact_target_px'])}</p>
<p>Wynik operatora: <strong>{x['outcome']}</strong> → <strong>{y['outcome']}</strong> (nie jest automatycznie rozstrzygany)</p>
</div></section>''')
    options=''.join(f'<option value="{c}">{c}</option>' for c in CASES)
    # Static source/template; no user notes, filenames, personal witness references, or arbitrary JSON included.
    return f'''<!doctype html><html lang="pl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline'; script-src 'unsafe-inline'; base-uri 'none'; form-action 'none'; connect-src 'none'" />
<title>AIM-DELTA001 | PRYWATNE porównanie przed/po</title><style>
:root{{font-family:system-ui,sans-serif;color-scheme:dark}}body{{margin:0;background:#111827;color:#eef4ff}}main{{max-width:1440px;margin:auto;padding:22px}}h1{{font-size:1.5rem}}h2{{font-size:1.18rem}}h3{{font-size:1rem}}p{{line-height:1.5;overflow-wrap:anywhere}}code{{overflow-wrap:anywhere;word-break:break-all}}.notice{{border:2px solid #d6aa61;padding:14px;background:#372916}}.muted{{color:#b9c6d7}}select,button{{font:inherit;padding:11px;color:#fff;background:#29394e;border:1px solid #94adcc;border-radius:8px}}button:focus-visible,select:focus-visible{{outline:3px solid #9bdcff}}.two{{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}}article{{min-width:0;border:1px solid #41536a;border-radius:12px;padding:12px;background:#1b293b}}.frame{{position:relative;max-width:100%;width:100%;line-height:0}}.frame img{{display:block;width:100%;height:auto}}.marker-layer{{position:absolute;inset:0;width:100%;height:100%;pointer-events:none}}.mark{{position:absolute;display:flex;justify-content:center;align-items:center;width:20px;height:20px;line-height:1;border:2px solid white;border-radius:50%;font-size:12px;color:#101820;transform:translate(-50%,-50%);background:#ffe482;box-shadow:0 0 2px #000}}.target{{background:#87f0d0}}.impact{{background:#ffa7bf}}.metrics{{background:#233348;margin-top:12px;border-radius:10px;padding:12px}}.case[hidden]{{display:none}}@media(max-width:760px){{.two{{grid-template-columns:1fr}}main{{padding:10px}}}}
</style></head><body><main><h1>AIM-DELTA001 — porównanie sesji</h1>
<p class="notice"><strong>PLIK PRYWATNY — ZAWIERA PEŁNE OBRAZY.</strong> To zestawienie dwóch zweryfikowanych strukturalnie eksportów AIM-OBS001. Różnica pikseli <strong>nie dowodzi poprawy celowania, trafienia ani testu z człowiekiem</strong>. Nie publikuj HTML, obrazów ani danych sesji.</p>
<p>Tryb: <strong>{mode}</strong> | Przed: <code>{info['before_session']}</code> | Po: <code>{info['after_session']}</code></p>
<p>Gra przed: <code>{info['before_game_sha']}</code> · po: <code>{info['after_game_sha']}</code></p>
<p><label for="scenario">Scenariusz: </label><select id="scenario">{options}</select> <label><input type="checkbox" id="marks" checked /> Pokaż punkty R/T/I</label></p>
{''.join(parts)}
<footer><p class="muted">Analizuj tylko porównywalne sceny. Zmiany dystansu, ułożenia celu, rozdzielczości, kamery i napędu mogą zmieniać odległości bez zmiany błędu gry. Wynik jest deklaracją operatora.</p></footer></main>
<script>const cases=[...document.querySelectorAll('.case')];document.getElementById('scenario').addEventListener('change',e=>cases.forEach(c=>c.hidden=c.dataset.case!==e.target.value));document.getElementById('marks').addEventListener('change',e=>document.querySelectorAll('.marker-layer').forEach(m=>m.hidden=!e.target.checked));</script></body></html>'''

def write_private(path:Path,content:str):
    need(path.parent.is_dir() and not path.parent.is_symlink(),'Output directory missing or symlink')
    need(not path.exists(),'Refuse overwrite of previous evidence')
    fd,tmp=tempfile.mkstemp(prefix='.aim-delta-',suffix='.tmp',dir=path.parent)
    try:
        os.fchmod(fd,0o600)
        with os.fdopen(fd,'w',encoding='utf-8') as stream:
            stream.write(content);stream.flush();os.fsync(stream.fileno())
        need(not path.exists(),'Output already exists')
        os.replace(tmp,path)
    finally:
        if os.path.exists(tmp):os.unlink(tmp)

def main(argv=None):
    p=argparse.ArgumentParser(description=__doc__)
    for s in ('before','after'):
        p.add_argument('--'+s,type=Path,required=True)
        p.add_argument('--'+s+'-images',type=Path,required=True)
        p.add_argument('--'+s+'-game-sha',required=True)
        p.add_argument('--'+s+'-zip-sha256',required=True)
    p.add_argument('--receiver',type=Path,required=True,help='Exact original AIM-OBS001/R1 verifier')
    p.add_argument('--output',type=Path,required=True,help='Private local HTML, must not exist')
    p.add_argument('--confirm-private-embedded-images',action='store_true')
    a=p.parse_args(argv)
    try:
        need(a.confirm_private_embedded_images,'Explicit privacy confirmation required')
        info=load_pair(a.before,a.before_images,a.after,a.after_images,a.before_game_sha,a.before_zip_sha256,a.after_game_sha,a.after_zip_sha256,a.receiver)
        result=render(info)
        write_private(a.output,result)
        print(json.dumps({'status':'STRUCTURAL_COMPARE_ONLY_NOT_GAMEPLAY_PROOF','mode':info['mode'],'cases':len(info['pairs']),'private_html_sha256':hashlib.sha256(result.encode()).hexdigest(),'native_unity':'NOT_RUN','human_participants':'UNVERIFIED','output':str(a.output)},sort_keys=True));return 0
    except (DeltaError,ValueError,OSError,Exception) as e:
        # Receiver's Invalid exception is external, fail closed on any receiver error.
        print('FAIL_CLOSED: '+str(e),file=sys.stderr);return 2
if __name__=='__main__':raise SystemExit(main())
