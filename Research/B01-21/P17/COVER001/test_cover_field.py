#!/usr/bin/env python3
"""Tests of B01-21 P17 COVER001 original 3-map visibility/cover data."""
import copy
import json
import tempfile
from pathlib import Path
import unittest

import cover_field as cover
import map_routes as nav


class SourceMapCoverTests(unittest.TestCase):
    def test_original_input_three_immutable_git_blobs(self):
        self.assertEqual(len(cover.INPUT_IDS),3)
        for name in cover.INPUT_IDS:
            plan=nav.load_original(name)
            self.assertEqual(plan["schema"],"B01-21-P16-ACTUAL-MAPGEN001-BLUEPRINT-v1")
            self.assertEqual(len(plan["enemySpawnDescriptors"]),3)

    def test_real_source_sightlines_are_cover_dependent(self):
        for name in cover.INPUT_IDS:
            plan=nav.load_original(name)
            field=cover.original_sightline_field(plan,name)
            self.assertEqual(field["sourceMapGenGeometryHash"],plan["geometryHash"])
            self.assertEqual(field["coverElementCount"],len(cover.covers_in(plan)))
            self.assertGreater(len(field["visibilityGrid"]),0)
            self.assertGreater(len(field["shelterAnchors"]),1)
            self.assertEqual(sum(field["visibleEnemyCountHistogram"].values()),len(field["visibilityGrid"]))
            self.assertEqual(sorted(field["visibleEnemyCountHistogram"]),list(map(str,range(4))))
            self.assertGreater(field["visibleEnemyCountHistogram"]["3"],0)
            self.assertFalse(field["historicalHeavyEnvelope"]["currentActualRigMeasured"])
            self.assertEqual(field["limitations"]["nativeUnityRaycast"],"NOT_RUN")

    def test_cover_anchor_is_owned_occlusion_and_original_source_heavy_reachable(self):
        for name in cover.INPUT_IDS:
            plan=nav.load_original(name)
            original=cover.covers_in(plan)
            anchors=cover.find_shelter_anchors(plan,original)
            ids=set()
            cells=set()
            for a in anchors:
                cell=(a["anchorLocalXZ"]["x"],a["anchorLocalXZ"]["z"])
                self.assertNotIn(a["anchorId"],ids)
                ids.add(a["anchorId"])
                self.assertNotIn(cell,cells)
                cells.add(cell)
                self.assertTrue(cover.heavy_free(plan,cell))
                owner=a["coverSourceElementOrdinal"]
                self.assertEqual(a["sourceCoverPosition"],original[owner]["position"])
                self.assertGreaterEqual(len(a["directOcclusionByThisCover"]),1)
                visibility=cover.sight(plan,cell,original)
                self.assertEqual(a["occludedEnemyDescriptors"],visibility["occludedEnemyDescriptors"])
                self.assertEqual(a["visibleEnemyDescriptors"],visibility["visibleEnemyDescriptors"])
                for i in a["directOcclusionByThisCover"]:
                    self.assertIn(owner,visibility["blockingCoverIndicesByEnemy"][i])
                p=nav.shortest_to(plan,nav.START,cell,nav.PROFILES["Heavy"])
                self.assertEqual(len(p)-1,a["historicalHeavyReachabilityGridStepsFromStart"])

    def test_visibility_field_uses_current_original_geometry_not_nominal_density(self):
        counts=[]
        for name in cover.INPUT_IDS:
            plan=nav.load_original(name)
            counts.append(len(cover.covers_in(plan)))
        self.assertEqual(counts,[6,11,16])

    def test_negative_z_box_at_eye_height_occludes(self):
        box={"kind":"Cover","position":{"x":0,"y":1.5,"z":0},
             "size":{"x":2,"y":3,"z":2},"pitchDegrees":0}
        self.assertTrue(cover.clip_original_aabb((0,10),(0,-10),box,eye_y=1.25))
        self.assertFalse(cover.clip_original_aabb((4,10),(4,-10),box,eye_y=1.25))
        self.assertFalse(cover.clip_original_aabb((0,10),(0,7),box,eye_y=1.25))

    def test_negative_too_short_box_does_not_hide_eye(self):
        box={"kind":"Cover","position":{"x":0,"y":0.25,"z":0},
             "size":{"x":2,"y":0.5,"z":2},"pitchDegrees":0}
        self.assertFalse(cover.clip_original_aabb((0,10),(0,-10),box,eye_y=1.25))
        self.assertTrue(cover.clip_original_aabb((0,10),(0,-10),box,eye_y=0.25))

    def test_negative_unknown_solid_kind_rejects(self):
        plan=copy.deepcopy(nav.load_original(cover.INPUT_IDS[0]))
        plan["elements"].append({"kind":"FutureSlopedWorld","pitchDegrees":0})
        with self.assertRaisesRegex(ValueError,"UNSUPPORTED_MAP_SOLID"):
            cover.original_sightline_field(plan,cover.INPUT_IDS[0])

    def test_negative_rotated_cover_rejects(self):
        plan=copy.deepcopy(nav.load_original(cover.INPUT_IDS[0]))
        next(x for x in plan["elements"] if x["kind"]=="Cover")["pitchDegrees"]=90
        with self.assertRaisesRegex(ValueError,"UNSUPPORTED_MAP_ROTATION"):
            cover.original_sightline_field(plan,cover.INPUT_IDS[0])

    def test_negative_spawn_descriptor_count_rejects(self):
        plan=copy.deepcopy(nav.load_original(cover.INPUT_IDS[0]))
        plan["enemySpawnDescriptors"].pop()
        with self.assertRaisesRegex(ValueError,"ENEMY_SPAWN_DESCRIPTOR_CONTRACT_CHANGED"):
            cover.original_sightline_field(plan,cover.INPUT_IDS[0])

    def test_negative_missing_floor_rejects(self):
        plan=copy.deepcopy(nav.load_original(cover.INPUT_IDS[0]))
        plan["elements"]=[e for e in plan["elements"] if e["kind"]!="Floor"]
        with self.assertRaisesRegex(ValueError,"NO_SOURCE_CLEAR_EXPOSURE_GRID"):
            cover.original_sightline_field(plan,cover.INPUT_IDS[0])

    def test_repeated_generation_deterministic_and_source_derived_svg(self):
        a=cover.render_all()
        b=cover.render_all()
        self.assertEqual(a,b)
        self.assertEqual(len(a),7)
        self.assertEqual(sum(k.endswith(".cover.json") for k in a),3)
        self.assertEqual(sum(k.endswith(".cover.svg") for k in a),3)
        for name in cover.INPUT_IDS:
            data=json.loads(a[name+".cover.json"])
            svg=a[name+".cover.svg"]
            self.assertEqual(svg.count('data-source-cover-anchor='),len(data["shelterAnchors"]))
            self.assertEqual(svg.count('data-enemy-descriptor='),3)
            self.assertEqual(svg.count('data-source-solid='),data["coverElementCount"]+4)
            self.assertTrue(svg.endswith("</svg>\n"))
        manifest=json.loads(a["manifest.json"])
        self.assertEqual(manifest["packageId"],cover.PACKAGE)
        self.assertFalse(manifest["inGame"])

    def test_fail_closed_real_blueprint_mutation(self):
        raw=(nav.SOURCE/(cover.INPUT_IDS[0]+".blueprint.json")).read_bytes()
        with tempfile.TemporaryDirectory() as directory:
            p=Path(directory)/"mutant.json"
            p.write_bytes(raw+b" ")
            with self.assertRaisesRegex(ValueError,"SOURCE_GIT_BLOB_CHANGED"):
                nav.load_original(cover.INPUT_IDS[0],p)


if __name__=="__main__":
    unittest.main()
