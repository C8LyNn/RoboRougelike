#!/usr/bin/env python3
"""B01-21 MAP-BREAK-001 tests: original frozen MAPGEN geometry + destructive what-if states."""
import copy
import json
import tempfile
from pathlib import Path
import unittest
import cover_break as world
import cover_field as cover
import map_routes as routes


class CoverBreakTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.originals={name:routes.load_original(name) for name in world.MAP_IDS}
        cls.datasets={name:world.derive_plan(d) for name,d in cls.originals.items()}

    def test_original_git_pinned_three_map_inputs(self):
        self.assertEqual(len(self.originals),3)
        for name,orig in self.originals.items():
            self.assertEqual(orig["id"],name)
            self.assertEqual(len(orig["geometryHash"]),64)
            self.assertEqual(self.datasets[name]["originalInputBlueprintGitBlob"],routes.SOURCE_BLOBS[name])
            self.assertEqual(self.datasets[name]["originalGeometryHash"],orig["geometryHash"])

    def test_all_33_real_single_cover_state_events_exist(self):
        count=[len(self.datasets[n]["coverDestructionCases"]) for n in world.MAP_IDS]
        self.assertEqual(count,[6,11,16])
        self.assertEqual(sum(count),33)
        for name in world.MAP_IDS:
            p=self.datasets[name]
            self.assertEqual(len(set(x["overlayStateHash"] for x in p["coverDestructionCases"])),len(p["coverDestructionCases"]))
            self.assertEqual(len({p["intactBaseline"]["stateHash"],*[x["overlayStateHash"] for x in p["coverDestructionCases"]]}),len(p["coverDestructionCases"])+1)

    def test_removal_mutates_exact_one_original_cover_never_original_input(self):
        for name,d in self.originals.items():
            original=json.dumps(d,sort_keys=True)
            src=[x for x in d["elements"] if x["kind"]=="Cover"]
            for i in range(len(src)):
                altered,source_idx=world.removed_plan(d,i)
                self.assertEqual(len([x for x in altered["elements"] if x["kind"]=="Cover"]),len(src)-1)
                self.assertEqual(len(altered["elements"]),len(d["elements"])-1)
                self.assertEqual(d["elements"][source_idx],src[i])
            self.assertEqual(json.dumps(d,sort_keys=True),original)

    def test_source_visibility_cannot_decrease_after_destruction(self):
        for name,p in self.datasets.items():
            for s in p["coverDestructionCases"]:
                self.assertGreaterEqual(s["sourceClearGridCellsAfter"],s["sourceClearGridCellsBefore"])
                for v in s["priorClearCellsWithIncreasedEnemyVisibility"]:
                    self.assertGreater(v["visibleAfter"],v["visibleBefore"])
                    self.assertGreater(len(v["newlyVisibleEnemyDescriptors"]),0)
                self.assertEqual(s["additionalVisibleEnemyPairCount"],
                    sum(v["visibleAfter"]-v["visibleBefore"] for v in s["priorClearCellsWithIncreasedEnemyVisibility"]))

    def test_actual_precomputed_damage_has_nonzero_visibility_effect(self):
        for name,p in self.datasets.items():
            self.assertGreater(sum(s["additionalVisibleEnemyPairCount"] for s in p["coverDestructionCases"]),0)
            self.assertGreater(sum(s["additionalVisibleEnemyPairCount"]>0 for s in p["coverDestructionCases"]),0)
            self.assertGreater(sum(len(s["originalShelterAnchorsInvalidated"]) for s in p["coverDestructionCases"]),0)

    def test_no_source_main_route_softlock_after_each_real_cover_removal(self):
        for name,p in self.datasets.items():
            for s in p["coverDestructionCases"]:
                actual,_=world.removed_plan(self.originals[name],s["destroyedCoverOrdinal"])
                for pname in world.PROFILES:
                    path=routes.shortest_to(actual,routes.START,routes.FINISH,routes.PROFILES[pname])
                    self.assertEqual(path[0],routes.START)
                    self.assertEqual(path[-1],routes.FINISH)
                    self.assertEqual(s["originalSourceStartFinishGridStepsByHistoricalProfile"][pname],len(path)-1)
                    self.assertGreaterEqual(len(path)-1,50)

    def test_original_anchor_references_never_orphan_on_damage(self):
        for name,p in self.datasets.items():
            intact={q["anchorId"] for q in p["intactBaseline"]["originalShelterAnchors"]}
            for s in p["coverDestructionCases"]:
                lost=s["originalShelterAnchorsInvalidated"]
                retained=s["originalShelterAnchorsRetained"]
                self.assertFalse(set(lost)&set(retained))
                self.assertEqual(set(lost)|set(retained),intact)
                destroyed=s["destroyedCoverOrdinal"]
                for q in p["intactBaseline"]["originalShelterAnchors"]:
                    if q["coverSourceElementOrdinal"]==destroyed:
                        self.assertIn(q["anchorId"],lost)

    def test_invalid_cover_ordinal_is_fail_closed(self):
        d=self.originals[world.MAP_IDS[0]]
        with self.assertRaisesRegex(ValueError,"INVALID_COVER_ORDINAL"):
            world.removed_plan(d,-1)
        with self.assertRaisesRegex(ValueError,"INVALID_COVER_ORDINAL"):
            world.removed_plan(d,len(cover.covers_in(d)))

    def test_unknown_source_kind_negative(self):
        d=copy.deepcopy(self.originals[world.MAP_IDS[0]])
        d["elements"].append({"kind":"UnrecognizedDynamicTile"})
        with self.assertRaisesRegex(ValueError,"UNSUPPORTED_MAP_SOLID"):
            world.derive_plan(d)

    def test_original_input_blob_mutation_fails_before_build(self):
        import tempfile
        raw=(routes.SOURCE/(world.MAP_IDS[0]+".blueprint.json")).read_bytes()
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)/"mutated.json"
            p.write_bytes(raw+b" ")
            with self.assertRaisesRegex(ValueError,"SOURCE_GIT_BLOB_CHANGED"):
                routes.load_original(world.MAP_IDS[0],p)

    def test_all_outputs_are_deterministic_and_viewer_has_live_controls(self):
        first=world.render_all()
        second=world.render_all()
        self.assertEqual(first,second)
        self.assertEqual(len(first),5)
        manifest=json.loads(first["manifest.json"])
        self.assertEqual(len(manifest["arenas"]),3)
        self.assertEqual([x["stateCount"] for x in manifest["arenas"]],[6,11,16])
        h=first["cover_break_viewer.html"]
        self.assertIn('id="destroy"',h)
        self.assertIn('id="restore"',h)
        self.assertIn('id="before"',h)
        self.assertIn('id="after"',h)
        self.assertIn('sourceSelfTest()',h)
        for name in world.MAP_IDS:
            self.assertIn(name,h)
            d=json.loads(first[name+".break.json"])
            self.assertEqual(d["arenaId"],name)
            self.assertEqual(d["runtimeDisclaimer"]["realPhysicsRaycasts"],"NOT_RUN")

    def test_restoring_produces_identical_pristine_overlay_hash(self):
        for name,p in self.datasets.items():
            total=p["originalCoverCount"]
            pristine=world.version_hash(p["originalGeometryHash"],range(total))
            self.assertEqual(p["intactBaseline"]["stateHash"],pristine)
            for state in p["coverDestructionCases"]:
                self.assertNotEqual(state["overlayStateHash"],pristine)
            # original immutable geometry never rewritten / no save or runtime mutation.
            self.assertEqual(p["originalGeometryHash"],self.originals[name]["geometryHash"])


if __name__=="__main__":
    unittest.main()
