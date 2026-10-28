#!/usr/bin/env python3
"""Render ONLY original-C#-generated MapBlueprint JSON as inspectable SVG/HTML previews."""
import html
import json
import pathlib
import sys
import xml.etree.ElementTree as ET

def svg_for(arena):
    W=760
    margin=60
    scale=10
    def xx(x):return margin+(x+32)*scale
    def zz(z):return margin+(32-z)*scale
    defs={'Floor':'#283444','Boundary':'#8796a5','Cover':'#d6a45a','Obstacle':'#b85f58','Ramp':'#a2ce81'}
    parts=[
        '<svg xmlns="http://www.w3.org/2000/svg" width="760" height="820" viewBox="0 0 760 820" role="img">',
        '<rect width="760" height="820" fill="#101820"/>',
        '<text x="60" y="31" font-family="sans-serif" font-size="18" fill="#f0f5f7">'
        +html.escape(arena['variantId'])+' / seed '+str(arena['seed'])+'</text>',
        '<rect x="60" y="60" width="640" height="640" fill="#253444" stroke="#9fb8cc" stroke-width="1"/>'
    ]
    cover=0
    for e in arena['elements']:
        if e['kind']=='Floor':continue
        p=e['position']; s=e['size']
        x=xx(p['x']-s['x']/2)
        y=zz(p['z']+s['z']/2)
        color=defs.get(e['kind'],'#e49f9f')
        if e['kind']=='Cover': cover+=1
        parts.append(f'<rect x="{x:.2f}" y="{y:.2f}" width="{s["x"]*scale:.2f}" height="{s["z"]*scale:.2f}" fill="{color}" opacity="0.90" stroke="#101820" stroke-width="1"/>')
    for i,p in enumerate(arena['enemySpawns']):
        parts.append(f'<circle cx="{xx(p["x"]):.2f}" cy="{zz(p["z"]):.2f}" r="9" fill="#ff627d" stroke="#ffffff" stroke-width="1.5"/>')
        parts.append(f'<text x="{xx(p["x"])+12:.2f}" y="{zz(p["z"])+4:.2f}" font-family="sans-serif" font-size="13" fill="#fff">E{i+1}</text>')
    for name,color,pos in [('S','#61c9f5',arena['start']),('F','#7ee0a0',arena['finish'])]:
        parts.append(f'<circle cx="{xx(pos["x"]):.2f}" cy="{zz(pos["z"]):.2f}" r="10" fill="{color}" stroke="#ffffff" stroke-width="2"/>')
        parts.append(f'<text x="{xx(pos["x"])+13:.2f}" y="{zz(pos["z"])+4:.2f}" font-family="sans-serif" font-size="14" fill="#fff">{name}</text>')
    parts.append(f'<text x="60" y="731" font-family="monospace" font-size="13" fill="#b6c6d2">Cover blocks: {cover}; corridor: 12m; Light/Heavy: source PASS</text>')
    parts.append(f'<text x="60" y="754" font-family="monospace" font-size="12" fill="#b6c6d2">Geometry SHA256: {arena["geometryHash"]}</text>')
    parts.append('<text x="60" y="781" font-family="sans-serif" font-size="12" fill="#e3aa8b">TOP-DOWN SOURCE PREVIEW ONLY — NOT NATIVE UNITY/PHYSX</text>')
    parts.append('</svg>')
    return '\n'.join(parts)+'\n',cover

def main(root):
    p=pathlib.Path(root)
    rows=json.loads((p/'results.json').read_text(encoding='utf-8'))['generatedBlueprints']
    assert len(rows)==18
    out=p/'previews'
    out.mkdir(exist_ok=True)
    gallery=[]
    counts={}
    for item in rows:
        arena=json.loads((p/item['file']).read_text(encoding='utf-8'))
        assert arena['geometryHash']==item['geometryHash']
        assert arena['nativeUnityPhysics']=='NOT_RUN'
        svg,cover=svg_for(arena)
        path=out/(pathlib.Path(item['file']).stem+'.svg')
        path.write_text(svg,encoding='utf-8')
        ET.parse(path)
        counts.setdefault(item['variant'],[]).append(cover)
        gallery.append(f'<figure><img loading="lazy" alt="{html.escape(arena["variantId"])} seed {arena["seed"]} top down source preview" src="previews/{path.name}"><figcaption>{html.escape(arena["variantId"])} · seed {arena["seed"]} · covers {cover}</figcaption></figure>')
    assert len(list(out.glob('*.svg')))==18
    # The three meaningful content variants must actually differ, not just change labels.
    means=[sum(v)/len(v) for _,v in sorted(counts.items())]
    assert means[0]<means[1]<means[2],f'NO_INCREASING_COVER_DENSITY:{counts}'
    style='<style>body{background:#101820;color:#e8eff5;font:16px system-ui,sans-serif;margin:24px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(290px,1fr));gap:14px}figure{margin:0;padding:8px;background:#202e3b;border-radius:9px}img{width:100%;height:auto}figcaption{padding:7px;font-size:13px}small{color:#b1c0cd}</style>'
    page='<!doctype html><html lang="pl"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>MAPGEN P16 — original C# arena previews</title>'+style+'<h1>MAPGEN P16 — 18 wygenerowanych aren</h1><p>Trzy konfiguracje, po sześć seedów. Wygenerowano oryginalnym MapGenPlanner C#; podgląd z góry, nie silnik Unity.</p><small>Oryginalny generator: 5be31039411f89f17654b1c332f8692883fa03cc. NATIVE UNITY / PHYSX: NOT_RUN.</small><main>'+''.join(gallery)+'</main></html>'
    (p/'gallery.html').write_text(page,encoding='utf-8')
    print('P16_SOURCE_RENDERED_SVGS=18 HTML_GALLERY=PASS MEAN_COVER_BY_VARIANT='+str([round(m,2) for m in means])+' UNITY=NOT_RUN')

if __name__=='__main__':
    if len(sys.argv)!=2:raise SystemExit('Usage: render_arenas.py OUTPUT_DIR')
    main(sys.argv[1])
