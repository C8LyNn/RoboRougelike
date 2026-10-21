#!/usr/bin/env python3
"""Independent offline reproduction of R5 cubic/smoothed grade formulas, graph and basic bounds.
Does not compile C#, instantiate Unity meshes or verify driving physics.
"""
from pathlib import Path
import json, math, sys
from collections import deque
root=Path(__file__).resolve().parents[1]
p=root/'Assets/RoboRougelike/MapSystemR4/Samples/Resources/MAP_R4_quarry_03.json'
m=json.loads(p.read_text(encoding='utf-8'))
nodes={n['id']:n['position'] for n in m['nodes']}
errors=[]

def point(e,t):
    a=nodes[e['fromNode']];b=nodes[e['toNode']];u=1-t
    if e['useCurve']:
        x=u**3*a['x']+3*u*u*t*e['controlA']['x']+3*u*t*t*e['controlB']['x']+t**3*b['x']
        z=u**3*a['z']+3*u*u*t*e['controlA']['z']+3*u*t*t*e['controlB']['z']+t**3*b['z']
    else:
        x=a['x']+(b['x']-a['x'])*t
        z=a['z']+(b['z']-a['z'])*t
    smooth=t*t*(3-2*t)
    y=a['y']+(b['y']-a['y'])*smooth
    return x,y,z

def slope(e):
    before=point(e,0)
    result=0
    for i in range(1,65):
        after=point(e,i/64)
        d=tuple(after[j]-before[j] for j in range(3))
        horizontal=math.hypot(d[0],d[2])
        if horizontal<0.0001: return 90.
        result=max(result,math.degrees(math.atan2(abs(d[1]),horizontal)))
        before=after
    return result

def radius(e):
    if not e['useCurve']: return math.inf
    before=point(e,0);mid=point(e,1/64)
    result=math.inf
    for i in range(2,65):
        after=point(e,i/64)
        p0=(before[0],before[2]);p1=(mid[0],mid[2]);p2=(after[0],after[2]);
        ab=math.dist(p0,p1);bc=math.dist(p1,p2);ca=math.dist(p2,p0)
        double_area=abs((p1[0]-p0[0])*(p2[1]-p0[1])-(p1[1]-p0[1])*(p2[0]-p0[0]))
        if double_area>1e-6 and ab*bc*ca>1e-6:
            result=min(result,ab*bc*ca/(2*double_area))
        before,mid=mid,after
    return result

def route(state_id,width=2.6,turn=4.5,slope_deg=16,from_node='start',to_node='exit'):
    state=next(s for s in m['states'] if s['id']==state_id)
    disabled=set(state['disabledEdgeIds'])
    adjacency={n:[] for n in nodes}
    for e in m['edges']:
        if e['id'] in disabled:continue
        if e['width']<width+2*.35 or e['availableTurnRadius']<turn or radius(e)<turn or slope(e)>slope_deg:continue
        a=e['fromNode'];b=e['toNode'];adjacency[a].append(b);adjacency[b].append(a)
    q=deque([from_node]);seen={from_node}
    while q:
        node=q.popleft()
        if node==to_node:return True
        for nbr in adjacency[node]:
            if nbr not in seen:seen.add(nbr);q.append(nbr)
    return False

if len(nodes)!=7 or len(m['edges'])!=8: errors.append('Unexpected reference topology')
for e in m['edges']:
    a=nodes[e['fromNode']];b=nodes[e['toNode']]
    if e['roadSegments']<2 or e['roadThickness']<=0:errors.append('Invalid tessellation '+e['id'])
    if math.dist(point(e,0),(a['x'],a['y'],a['z']))>1e-5 or math.dist(point(e,1),(b['x'],b['y'],b['z']))>1e-5:errors.append('Endpoint mismatch '+e['id'])
    sl=slope(e);rad=radius(e)
    if sl>e['declaredSlopeDeg']+0.15:errors.append(f"Slope too high {e['id']} {sl:.2f}>{e['declaredSlopeDeg']}")
    if rad<4.5:errors.append(f"Radius below standard profile {e['id']} radius={rad:.2f}")
    # End tangent in Y should be zero (smoothstep endpoint slope).
    if abs(point(e,0.0001)[1]-a['y'])>0.00001 or abs(point(e,0.9999)[1]-b['y'])>0.00001:
        errors.append('Grade endpoint discontinuity '+e['id'])
    print(f"  {e['id']}: maxSlope={sl:.2f} deg declared={e['declaredSlopeDeg']:.1f}; minRadius={rad if math.isfinite(rad) else float('inf'):.1f} m")
if not route('intact'):errors.append('No valid standard intact route')
if not route('bridge_collapsed'):errors.append('No valid bridge collapsed route')
if route('all_eastern_routes_closed'):errors.append('Softlock not rejected')
if route('intact',width=8):errors.append('Oversized vehicle unexpectedly passed')
if not route('intact',from_node='start',to_node='upper_ramp'):errors.append('Upper checkpoint unreachable')
print(f"R5 offline geometry: {len(m['edges'])} roads, {len(errors)} errors")
for e in errors:print('ERROR:',e)
print('RESULT:', 'FAIL' if errors else 'PASS — data/path calculations only; Unity/PhysX NOT TESTED')
sys.exit(1 if errors else 0)
