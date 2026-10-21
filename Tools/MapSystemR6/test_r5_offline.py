import unittest
import json
import math
from pathlib import Path
r=Path(__file__).resolve().parents[1]
m=json.loads((r/'Assets/RoboRougelike/MapSystemR4/Samples/Resources/MAP_R4_quarry_03.json').read_text())
nodes={n['id']:n for n in m['nodes']}
edges={e['id']:e for e in m['edges']}
class MapR5OfflineTests(unittest.TestCase):
    def test_reference_topology(self):
        self.assertEqual(len(m['nodes']),7)
        self.assertEqual(len(m['edges']),8)
        self.assertEqual(len(m['states']),3)
    def test_unique_ids(self):
        for group in ['nodes','edges','anchors','states']:
            keys=[x['id'] for x in m[group]]
            self.assertEqual(len(keys),len(set(keys)))
    def test_edges_reference_nodes(self):
        for e in m['edges']:
            self.assertIn(e['fromNode'],nodes)
            self.assertIn(e['toNode'],nodes)
    def test_bridge_collapsed_but_not_lower(self):
        s=next(s for s in m['states'] if s['id']=='bridge_collapsed')
        self.assertIn('bridge_exit',s['disabledEdgeIds'])
        self.assertNotIn('lower_exit',s['disabledEdgeIds'])
    def test_all_routes_closed(self):
        s=next(s for s in m['states'] if s['id']=='all_eastern_routes_closed')
        self.assertEqual(set(s['disabledEdgeIds']),{'bridge_exit','lower_exit','arena_exit'})
    def test_curve_present(self):
        self.assertTrue(edges['junction_upper']['useCurve'])
        self.assertTrue(edges['junction_lower']['useCurve'])
        self.assertTrue(edges['bridge_exit']['useCurve'])
    def test_road_resolution(self):
        for e in m['edges']:
            self.assertTrue(2<=e['roadSegments']<=256)
            self.assertGreater(e['roadThickness'],0)
    def test_slope_metadata_recalibrated(self):
        self.assertEqual(edges['junction_lower']['declaredSlopeDeg'],0)
        self.assertLess(edges['junction_upper']['declaredSlopeDeg'],6)
        self.assertEqual(edges['upper_bridge']['declaredSlopeDeg'],0)
    def test_valid_map_id(self):
        self.assertEqual(m['mapId'],'quarry_03')
        self.assertEqual(m['version'],6)
    def test_anchor_references(self):
        for a in m['anchors']:self.assertIn(a['nodeId'],nodes)
    def test_curve_controls(self):
        for e in m['edges']:
            if e['useCurve']:
                self.assertNotEqual(e['controlA'],e['controlB'])
    def test_no_r4_original_metadata_claim(self):
        original=json.loads((r/'R3_Reference/Examples/quarry_03.map.json').read_text())
        by_id={e['id']:e for e in original['edges']}
        self.assertNotEqual(edges['junction_upper']['declaredSlopeDeg'],by_id['junction_upper']['slope_deg'])
if __name__=='__main__':unittest.main(verbosity=2)
