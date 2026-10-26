import unittest, json, math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
M=json.loads((ROOT/'Assets/RoboRougelike/MapSystemR4/Samples/Resources/MAP_R4_quarry_03.json').read_text())
N={n['id']:n for n in M['nodes']}; E={e['id']:e for e in M['edges']}

def v(p):return [p['x'],p['y'],p['z']]
def sub(a,b):return [a[i]-b[i] for i in range(3)]
def add(a,b):return [a[i]+b[i] for i in range(3)]
def mul(a,k):return [x*k for x in a]
def norm(a):
    d=math.hypot(a[0],a[2]);return [a[0]/d,0,a[2]/d] if d>1e-9 else [0,0,0]
def dist(a,b):return math.dist(a,b)
def evaluate(edge,t):
    a=v(N[edge['fromNode']]['position']);b=v(N[edge['toNode']]['position'])
    if edge.get('useCurve'):
        c=v(edge['controlA']);d=v(edge['controlB']);u=1-t
        p=add(add(mul(a,u**3),mul(c,3*u*u*t)),add(mul(d,3*u*t*t),mul(b,t**3)))
    else:p=add(mul(a,1-t),mul(b,t))
    p[1]=a[1]+(b[1]-a[1])*(t*t*(3-2*t))
    return p

def tangent(edge,t):
    a=v(N[edge['fromNode']]['position']);b=v(N[edge['toNode']]['position'])
    if edge.get('useCurve'):
        c=v(edge['controlA']);d=v(edge['controlB']);u=1-t
        delta=add(add(mul(sub(c,a),3*u*u),mul(sub(d,c),6*u*t)),mul(sub(b,d),3*t*t))
    else:delta=sub(b,a)
    return norm(delta)
def length(edge):return sum(dist(evaluate(edge,(i-1)/64),evaluate(edge,i/64)) for i in range(1,65))

def plan(in_id,out_id,node_id,pad=36,vehicle_radius=.1):
    incoming,outgoing=E[in_id],E[out_id]
    assert node_id in (incoming['fromNode'],incoming['toNode']) and node_id in (outgoing['fromNode'],outgoing['toNode'])
    frac_in=max(.03,min(.38,pad*.43/length(incoming)))
    frac_out=max(.03,min(.38,pad*.43/length(outgoing)))
    in_f=incoming['toNode']==node_id;out_f=outgoing['fromNode']==node_id
    ti=1-frac_in if in_f else frac_in;to=frac_out if out_f else 1-frac_out
    p0,p3=evaluate(incoming,ti),evaluate(outgoing,to)
    hi=mul(tangent(incoming,ti),1 if in_f else -1)
    ho=mul(tangent(outgoing,to),1 if out_f else -1)
    h=max(.5,min(dist(p0,p3)*.48,pad*.65))
    c1=add(p0,mul(hi,h));c2=sub(p3,mul(ho,h));c1[1]=p0[1];c2[1]=p3[1]
    def curve(t):
        u=1-t
        p=add(add(mul(p0,u**3),mul(c1,3*u*u*t)),add(mul(c2,3*u*t*t),mul(p3,t**3)))
        p[1]=p0[1]+(p3[1]-p0[1])*(t*t*(3-2*t))
        return p
    points=[curve(i/96) for i in range(97)]
    radii=[]
    for a,b,c in zip(points,points[1:],points[2:]):
        ab=math.hypot(a[0]-b[0],a[2]-b[2]);bc=math.hypot(b[0]-c[0],b[2]-c[2]);ca=math.hypot(c[0]-a[0],c[2]-a[2]);area2=abs((b[0]-a[0])*(c[2]-a[2])-(b[2]-a[2])*(c[0]-a[0]))
        if area2>1e-6 and ab*bc*ca>1e-6:radii.append(ab*bc*ca/(2*area2))
    minimum=min(radii) if radii else math.inf
    maximum_slope=max(math.degrees(math.atan2(abs(b[1]-a[1]),max(1e-9,math.hypot(b[0]-a[0],b[2]-a[2])))) for a,b in zip(points,points[1:]))
    return dict(minimum_radius=minimum,max_slope_deg=maximum_slope,valid=minimum+.05>=vehicle_radius,points=points)

class OfflineJunctionTests(unittest.TestCase):
    def test_existing_geometry_preserved(self):
        self.assertEqual((len(N),len(E)),(7,8))
    def test_wide_synthetic_ninety_degree_turn(self):
        backupN,backupE=N.copy(),E.copy()
        try:
            N.clear();E.clear()
            N.update({"w":{"position":{"x":-60,"y":0,"z":0}},"c":{"position":{"x":0,"y":0,"z":0}},"n":{"position":{"x":0,"y":0,"z":60}}})
            E.update({"west":{"id":"west","fromNode":"w","toNode":"c","useCurve":False},"north":{"id":"north","fromNode":"c","toNode":"n","useCurve":False}})
            result=plan('west','north','c',pad=36,vehicle_radius=.1)
            self.assertTrue(result['valid']);self.assertGreater(result['minimum_radius'],0)
            self.assertEqual(result['max_slope_deg'],0)
        finally:N.clear();N.update(backupN);E.clear();E.update(backupE)
    def test_extreme_radius_blocks_same_turn(self):
        p=plan('start_junction','junction_lower','junction',pad=12,vehicle_radius=1000)
        self.assertFalse(p['valid'])
    def test_zero_degree_straight_link(self):
        p=plan('start_junction','junction_upper','junction',pad=12,vehicle_radius=.1)
        self.assertGreater(p['minimum_radius'],0)
    def test_reproducible_route(self):
        x=plan('start_junction','junction_lower','junction',pad=12)
        y=plan('start_junction','junction_lower','junction',pad=12)
        self.assertEqual(x['minimum_radius'],y['minimum_radius'])
        self.assertEqual(x['points'],y['points'])

if __name__=='__main__':unittest.main(verbosity=2)
