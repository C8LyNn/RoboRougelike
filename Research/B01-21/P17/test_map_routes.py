#!/usr/bin/env python3
"""PASS17 real generated original MAPGEN map route geometry tests. stdlib only."""
import copy
import unittest
import map_routes as nav


class MapRoutesP17Tests(unittest.TestCase):
    def test_source_three_original_p16_mapgen_blobs_are_pinned(self):
        self.assertEqual(len(nav.SOURCE_BLOBS),3)
        for name in nav.SOURCE_BLOBS:
            d=nav.load_original(name)
            self.assertEqual(len(d["geometryHash"]),64)
            self.assertEqual(d["schema"],"B01-21-P16-ACTUAL-MAPGEN001-BLUEPRINT-v1")

    def test_original_source_route_3_by_2_by_3(self):
        for name in nav.SOURCE_BLOBS:
            d=nav.load_original(name)
            result=nav.overlay(d,name)
            self.assertEqual(len(result["routes"]),6)
            for profile_name in nav.PROFILES:
                routes=[x for x in result["routes"] if x["referenceProfile"]==profile_name]
                self.assertEqual([x["routeId"] for x in routes],list(nav.LANES))
                for route in routes:
                    cells=[(p["x"],p["z"]) for p in route["gridPath"]]
                    self.assertEqual(cells[0],nav.START)
                    self.assertEqual(cells[-1],nav.FINISH)
                    self.assertTrue(all(nav.free_on_original_geometry(d,p,nav.PROFILES[profile_name])
                                        for p in cells))
                    self.assertEqual(route["gridStepsAt1m"],len(cells)-1)
                    self.assertTrue(all(abs(a[0]-b[0])+abs(a[1]-b[1])==1
                                        for a,b in zip(cells,cells[1:])))
                    self.assertFalse(route["sweptTurnAndPhysxCertificate"])

    def test_alternatives_have_real_lateral_routes_and_nonzero_detours(self):
        for name in nav.SOURCE_BLOBS:
            d=nav.load_original(name)
            for profile in nav.PROFILES:
                three=[nav.route(d,lane,profile) for lane in nav.LANES]
                cells=[[ (p["x"],p["z"]) for p in v["gridPath"]] for v in three]
                self.assertTrue(all(x==0 for x,_ in cells[0]))
                self.assertIn((-3,0),cells[1])
                self.assertIn((3,0),cells[2])
                self.assertGreater(three[1]["gridStepsAt1m"],three[0]["gridStepsAt1m"])
                self.assertGreater(three[2]["gridStepsAt1m"],three[0]["gridStepsAt1m"])

    def test_negative_source_blob_mutation_is_rejected(self):
        import tempfile
        from pathlib import Path
        src=(nav.SOURCE/"AR16_01_OPEN_FLANK.blueprint.json").read_bytes()
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)/"input.json"
            p.write_bytes(src+b" ")
            with self.assertRaisesRegex(ValueError,"SOURCE_GIT_BLOB_CHANGED"):
                nav.load_original("AR16_01_OPEN_FLANK",p)

    def test_negative_unknown_obstacle_kind_fails_closed(self):
        d=copy.deepcopy(nav.load_original("AR16_01_OPEN_FLANK"))
        d["elements"].append({"kind":"UnknownSurprise",
                              "position":{"x":0,"z":0},"size":{"x":1,"z":1},
                              "pitchDegrees":0})
        with self.assertRaisesRegex(ValueError,"UNSUPPORTED_SOLID_KIND"):
            nav.route(d,"MAIN","Heavy")

    def test_negative_center_terrain_blocker_fails_closed(self):
        d=copy.deepcopy(nav.load_original("AR16_01_OPEN_FLANK"))
        d["elements"].append({"kind":"Cover","position":{"x":0,"z":0},
                              "size":{"x":20,"z":20},"pitchDegrees":0})
        with self.assertRaises(ValueError):
            nav.route(d,"MAIN","Heavy")

    def test_negative_missing_floor_and_unreachable_anchor(self):
        d=copy.deepcopy(nav.load_original("AR16_02_SPLIT_DECISION"))
        d["elements"]=[e for e in d["elements"] if e["kind"]!="Floor"]
        with self.assertRaisesRegex(ValueError,"START_BLOCKED"):
            nav.route(d,"MAIN","Light")

    def test_generated_is_stable_with_three_distinct_original_hashes(self):
        a=nav.render_all()
        b=nav.render_all()
        self.assertEqual(a,b)
        self.assertEqual(len(a),7)
        self.assertEqual(sum(x.endswith(".routes.svg") for x in a),3)
        self.assertEqual(sum(x.endswith(".routes.json") for x in a),3)
        import json
        m=json.loads(a["manifest.json"])
        self.assertEqual(m["numberOfDistinctProfileLaneRoutes"],18)
        self.assertEqual([p["coverCount"] for p in m["layouts"]],[6,11,16])
        self.assertEqual(len({p["geometryHash"] for p in m["layouts"]}),3)


if __name__=="__main__":
    unittest.main()
