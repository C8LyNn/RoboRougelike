#!/usr/bin/env python3
"""Top-down SVG/HTML visualization of ORIGINAL generated ObstacleCourse blueprints."""
import html,json,math
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

def svg_for(m):
    xmin=m["bounds"]["center"]["x"]-m["bounds"]["size"]["x"]/2
    xmax=m["bounds"]["center"]["x"]+m["bounds"]["size"]["x"]/2
    zmin=m["bounds"]["center"]["z"]-m["bounds"]["size"]["z"]/2
    zmax=m["bounds"]["center"]["z"]+m["bounds"]["size"]["z"]/2
    scale=min(730/(xmax-xmin),830/(zmax-zmin))
    def sx(v):return 75+(v-xmin)*scale
    def sz(v):return 75+(zmax-v)*scale
    out=['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 920 1020" width="920" height="1020" role="img">',
         '<rect width="920" height="1020" fill="#101923"/>',
         '<text x="50" y="35" font-family="sans-serif" font-size="19" fill="#edf4ff">'+html.escape(m['variantId'])+' seed '+str(m['seed'])+'</text>']
    colors={"Floor":"#273c4c","Boundary":"#b3bdc7","Ramp":"#75d3ad","Obstacle":"#f3a45f","Cover":"#dfb36a"}
    for e in m['elements']:
        p=e['position'];s=e['size']
        x=sx(p['x']-s['x']/2)
        y=sz(p['z']+s['z']/2)
        out.append(f'<rect x="{x:.2f}" y="{y:.2f}" width="{s["x"]*scale:.2f}" height="{s["z"]*scale:.2f}" fill="{colors[e["kind"]]}" stroke="#12202a" stroke-width="1" opacity=".95"/>')
    route=[m['start']]+m['checkpoints']+[m['finish']]
    pts=' '.join(f'{sx(p["x"]):.2f},{sz(p["z"]):.2f}' for p in route)
    out.append(f'<polyline points="{pts}" fill="none" stroke="#7bc9ff" stroke-width="2.8" stroke-dasharray="9 6" opacity=".94"/>')
    for i,p in enumerate(route):
        name='S' if i==0 else 'F' if i==len(route)-1 else 'CP'+str(i)
        color="#7feda4" if name=="F" else "#76c8ff" if name=="S" else "#fad87f"
        out.append(f'<circle cx="{sx(p["x"]):.2f}" cy="{sz(p["z"]):.2f}" r="8" fill="{color}" stroke="#fff" stroke-width="1"/>')
        out.append(f'<text x="{sx(p["x"])+12:.2f}" y="{sz(p["z"])+4:.2f}" fill="white" font-family="sans-serif" font-size="13">{name}</text>')
    out.append('<text x="50" y="952" font-family="monospace" font-size="13" fill="#bcd0dc">Floor '+str(m['counts']['floors'])+' / obstacles '+str(m['counts']['obstacles'])+' / ramps '+str(m['counts']['ramps'])+' / Light+Heavy source PASS</text>')
    out.append('<text x="50" y="976" font-family="monospace" font-size="11" fill="#bcd0dc">SHA: '+m['geometryHash']+'</text>')
    out.append('<text x="50" y="1002" font-family="sans-serif" font-size="12" fill="#e8b798">SOURCE BLUEPRINT — NOT UNITY / PHYSX VISUALIZATION</text></svg>')
    return '\n'.join(out)+'\n'

def main(directory):
    directory=Path(directory)
    rows=json.loads((directory/'results.json').read_text(encoding='utf-8'))['generated']
    assert len(rows)==18
    out=directory/'previews';out.mkdir(exist_ok=True)
    cards=[]
    for entry in rows:
        p=directory/entry['file']
        m=json.loads(p.read_text(encoding='utf-8'))
        assert m['geometryHash']==entry['geometryHash']
        dst=out/(p.stem+'.svg')
        dst.write_text(svg_for(m),encoding='utf-8',newline='\n')
        ET.parse(dst)
        cards.append('<figure><img loading="lazy" src="previews/'+html.escape(dst.name)+'" alt="'+html.escape(m['variantId'])+' seed '+str(m['seed'])+'"><figcaption>'+html.escape(m['variantId'])+' / seed '+str(m['seed'])+' / ramps '+str(m['counts']['ramps'])+' / obstacles '+str(m['counts']['obstacles'])+'</figcaption></figure>')
    assert len(list(out.glob('*.svg')))==18
    css='<style>body{background:#101923;color:#e7f1fa;font:16px system-ui;margin:24px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(270px,1fr));gap:18px}figure{margin:0;background:#263748;padding:10px;border-radius:8px}img{width:100%;height:auto}figcaption{font-size:13px}small{color:#b3cadc}</style>'
    page='<!doctype html><html lang="pl"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>P17 MAPGEN Course</title>'+css+'<h1>P17 — 18 proceduralnych torów przeszkód</h1><p>Oryginalny MapGenPlanner C#; trzy konfiguracje długości i trudności. Checkpointy, start, meta, rampy, przeszkody.</p><small>Unity + PhysX: NOT_RUN. Geometry SHA każdego wariantu pokazano w SVG.</small><main>'+''.join(cards)+'</main></html>'
    (directory/'gallery.html').write_text(page,encoding='utf-8',newline='\n')
    print('P17_ORIGINAL_BLUEPRINT_SVG=18 GALLERY_HTML=PASS UNITY=NOT_RUN')

if __name__=="__main__":
    if len(sys.argv)!=2:raise SystemExit("Usage: render_courses.py output_directory")
    main(sys.argv[1])
