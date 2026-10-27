"""Aggregate completed trials without turning pilot failures into camera PASS."""
import json,pathlib,xml.etree.ElementTree as ET
from analyze_motion import analyze
r=pathlib.Path(__file__).resolve().parents[2];out=r/'Docs/Camera275';out.mkdir(parents=True,exist_ok=True)
rows=[]
for folder in sorted((r/'Artifacts/Local').glob('CAM275-Windows-*')):
    if not (folder/'process.json').exists():continue
    result=json.loads((folder/'camera-result.json').read_text(encoding='utf-8-sig'))
    process=json.loads((folder/'process.json').read_text(encoding='utf-8-sig'))
    metrics=analyze(folder)
    rows.append(dict(stage=folder.name,source=process['source'],process=process,result=result,motion=metrics))
(out/'motion-results.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
table=['| Pojazd | Limit / rzeczywiste FPS (mediana) | Przejazd | Dystans | Własny kadłub / świat | Dowód |','| --- | --- | --- | --- | --- | --- |']
for row in rows:
    if not row['stage'].startswith('CAM275-Windows-Final-'):continue
    s=row['result'];m=row['motion']
    table.append(f"| {s['vehicle']} | {s['fps']} / {m['medianFPS']:.2f} | {'PASS' if s['status']=='Passed' else 'FAIL — pilot nie ukończył trasy'} | {s['distance']:.1f} m | {s['ownRayHits']} / {s['worldOverlaps']} | [{row['stage']}](Evidence/{row['stage']}/camera-result.json) |")
(out/'WINDOWS_DRIVE_MATRIX.md').write_text('# Rzeczywiste przejazdy Windows\n\n'+ '\n'.join(table)+'\n\nKażda próba: rzeczywisty dynamiczny R3/R6, istniejący poligon, natywne stany WASD/myszy; bez teleportacji i zapisu prędkości. To automat wejścia, nie ręczna rozgrywka. FAIL przejazdu pozostaje FAIL nawet przy zerowych naruszeniach kamery. Limity rzeczywiście dotyczą renderowania Playera; deterministyczne kroki z testów konstrukcji są raportowane osobno.\n',encoding='utf-8')
final=list((r/'Artifacts/Local').glob('CAM275-Aim-Final-*/aim-result.json'))
if final:
    p=sorted(final)[-1];shots=json.loads(p.read_text(encoding='utf-8-sig'));table=['| Broń | 10 m | 30 m | 60 m | 100 m |','| --- | --- | --- | --- | --- |']
    for weapon in dict.fromkeys(s['weapon'] for s in shots['shots']):
        fields=[]
        for s in [s for s in shots['shots'] if s['weapon']==weapon]:
            fields.append(f"{s['status'].upper()}: {s['damage']:.2f} HP"+(' (poza zasięgiem)' if not s['expectedHit'] else ''))
        table.append('| '+weapon+' | '+' | '.join(fields)+' |')
    (out/'WINDOWS_SHOT_MATRIX.md').write_text('# Rzeczywiste pociski i HP w Windows Player\n\n'+ '\n'.join(table)+f'\n\nŹródło: `c61894a6389e43727db42a6dd58f153a496923ee`. [Pełne dane](Evidence/{p.parent.name}/aim-result.json). Fizyczne pociski / natywna wiązka i oryginalny system HP; cel nieruchomy, pojazd utrzymany w miejscu, geometria poligonu wyłączona w odizolowanym torze testowym. To nie dowód dynamicznej walki z AI ani trafiania w poruszający się cel. Obrót i LPM przechodzą przez rzeczywisty Input System i wspólny kontroler. Piła i EMP nie są bronią do strzelania na podanych dystansach.\n',encoding='utf-8')
print(json.dumps(dict(completedDriveTrials=len(rows),finalDriveTrials=sum(x['stage'].startswith('CAM275-Windows-Final-') for x in rows),finalDrivePassed=sum(x['stage'].startswith('CAM275-Windows-Final-') and x['result']['status']=='Passed' for x in rows)),indent=2))
