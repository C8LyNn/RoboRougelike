"""R3 to R5 topological parity; unlike R4, geometry/slope declarations intentionally change."""
from pathlib import Path
import json
root=Path(__file__).resolve().parents[1]
a=json.loads((root/'R3_Reference/Examples/quarry_03.map.json').read_text(encoding='utf-8'))
b=json.loads((root/'Assets/RoboRougelike/MapSystemR4/Samples/Resources/MAP_R4_quarry_03.json').read_text(encoding='utf-8'))
assert a['map_id']==b['mapId']
assert {n['id'] for n in a['nodes']}=={n['id'] for n in b['nodes']}
assert {e['id'] for e in a['edges']}=={e['id'] for e in b['edges']}
assert {a['id'] for a in a['anchors']}=={x['id'] for x in b['anchors']}
assert {k:set(v['disabled_edges']) for k,v in a['states'].items()}=={s['id']:set(s['disabledEdgeIds']) for s in b['states']}
for x,y in zip(a['nodes'],b['nodes']):assert dict(zip('xyz',x['position']))==y['position']
for x,y in zip(a['edges'],b['edges']):
 assert x['from']==y['fromNode'] and x['to']==y['toNode']
 assert x['min_clear_width_m']==y['width']
 assert x['min_clear_height_m']==y['clearHeight']
 assert x['available_turn_radius_m']==y['availableTurnRadius']
print('PASS — R3/R5 nodes, anchors counts, edge endpoints, widths, heights, states. Slopes/curves intentionally recalibrated.')
