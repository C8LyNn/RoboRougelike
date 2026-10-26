import unittest
from collections import deque
from test_r6_junctions import M,N,E,plan

def check_explicit_path(edgeids,state="bridge_collapsed",width=2.6,minrad=4.5):
    blocked=set(next(s for s in M['states'] if s['id']==state)['disabledEdgeIds'])
    at='start';previous=None
    for edgeid in edgeids:
        if edgeid in blocked:return False
        edge=E[edgeid]
        if at not in (edge['fromNode'],edge['toNode']):return False
        if edge['width'] < width + .7 or edge['availableTurnRadius']<minrad:return False
        if previous is not None:
            turn=plan(previous,edgeid,at,pad=N[at]['padWidth'],vehicle_radius=minrad)
            if not turn['valid'] or turn['max_slope_deg']>16:return False
        at=edge['toNode'] if at==edge['fromNode'] else edge['fromNode']
        previous=edgeid
    return at=='exit'

class MapR6RouteOfflineTests(unittest.TestCase):
    def test_version_increment_and_pad_fix(self):
        self.assertEqual(M['version'],6)
        self.assertEqual(N['lower_road']['padWidth'],18)
    def test_original_lower_turn_was_too_tight(self):
        self.assertLess(plan('junction_lower','lower_exit','lower_road',pad=12)['minimum_radius'],4.5)
    def test_revised_lower_turn_exceeds_radius(self):
        self.assertGreater(plan('junction_lower','lower_exit','lower_road',pad=N['lower_road']['padWidth'])['minimum_radius'],4.5)
    def test_escape_lower_route_possible_geometrically(self):
        self.assertTrue(check_explicit_path(['start_junction','junction_lower','lower_exit']))
    def test_bridge_route_disabled_after_collapse(self):
        self.assertFalse(check_explicit_path(['start_junction','junction_upper','upper_bridge','bridge_exit']))
    def test_all_exit_routes_closed(self):
        self.assertFalse(check_explicit_path(['start_junction','junction_lower','lower_exit'],'all_eastern_routes_closed'))
    def test_tight_turn_vehicle_fails(self):
        self.assertFalse(check_explicit_path(['start_junction','junction_lower','lower_exit'],minrad=20))
    def test_missing_turn_connection_rejected(self):
        self.assertFalse(check_explicit_path(['start_junction','lower_exit']))
    def test_reference_pad_footprint_not_physically_proven(self):
        self.assertEqual(len(M['nodes']),7)
        self.assertEqual(len(M['edges']),8)

if __name__=='__main__':unittest.main(verbosity=2)
