import json, math
from pathlib import Path
from test_r6_junctions import M,N,E,plan
root=Path(__file__).resolve().parents[1]
turns=[('start_junction','junction_lower','junction'),('start_junction','junction_upper','junction'),('start_junction','junction_arena','junction'),('junction_lower','lower_exit','lower_road'),('junction_upper','upper_bridge','upper_ramp'),('lower_exit','start_junction','junction')]
results=[]
for a,b,node in turns:
    if node not in (E[a]['fromNode'],E[a]['toNode']) or node not in (E[b]['fromNode'],E[b]['toNode']):
        results.append({'incoming':a,'outgoing':b,'junction':node,'status':'INVALID_CONNECTIVITY'});continue
    p=plan(a,b,node,pad=N[node]['padWidth'],vehicle_radius=4.5)
    results.append({'incoming':a,'outgoing':b,'junction':node,'radiusMeters':round(p['minimum_radius'],2),'slopeDeg':round(p['max_slope_deg'],2),'status':'CANDIDATE' if p['valid'] and p['max_slope_deg']<=16 else 'NEEDS_REDESIGN'})
result={'note':'Offline geometric candidate only; no Unity driving evidence','mapId':M['mapId'],'vehicleMinTurnRadius':4.5,'results':results}
path=root/'Docs/R6_REFERENCE_JUNCTION_REPORT.json'
path.write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n')
print(json.dumps(result,indent=2,ensure_ascii=False))
