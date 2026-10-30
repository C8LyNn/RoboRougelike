#!/usr/bin/env python3
"""Independent negative tamper, original source pin and temporal-route regression."""
import copy
import json
import os
from pathlib import Path
import unittest

from receiver import (
    MAPS, Reject, authored_inputs, edge_safe, find_route, git_blob, inspect,
    validate_level,
)

class OriginalOwnerTemporalTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.original, cls.maps=authored_inputs(os.environ.get("B01_REPO","."))
        cls.levels={x["levelId"]:x for x in cls.original["levels"]}

    def check_mutated(self,mutator,reason):
        p=copy.deepcopy(self.original)
        maps=dict(self.maps)
        mutator(p,maps)
        with self.assertRaisesRegex(Reject,reason):
            inspect(p,maps,require_pins=False)

    def test_01_original_two_real_maps_zero_damage_temporal_routes(self):
        result=inspect(self.original,self.maps,require_pins=True)
        self.assertEqual(set(result["maps"]),set(MAPS))
        for key in MAPS:
            x=result["maps"][key]
            self.assertEqual(x["pulse_hits"],0)
            self.assertEqual(x["time_seconds"],len(x["cells_with_phase_tick"])-1)
            self.assertEqual(x["moves"]+x["wait_seconds"],x["time_seconds"])
            self.assertGreater(x["time_seconds"],0)
            self.assertGreater(x["states_explored"],10)
            self.assertEqual(x["cells_with_phase_tick"][0][:2],x["start"])
            self.assertEqual(x["cells_with_phase_tick"][-1][:2],x["finish"])
        self.assertEqual(result["maps"]["CLEARANCE_PULSE_CROSSING"]["time_seconds"],12)
        self.assertEqual(result["maps"]["SLALOM_PULSE_CROSSING"]["time_seconds"],44)

    def test_02_replay_receipt_exactly_deterministic(self):
        a=inspect(self.original,self.maps)
        b=inspect(self.original,self.maps)
        self.assertEqual(json.dumps(a,sort_keys=True),json.dumps(b,sort_keys=True))

    def test_03_git_original_map_blob_matches_real_physical_bytes(self):
        for key,info in MAPS.items():
            self.assertEqual(git_blob(self.maps[key]),info[2])

    def test_04_reject_modified_original_map_bytes(self):
        raw=dict(self.maps)
        key="SLALOM_PULSE_CROSSING"
        raw[key]=raw[key]+b"\n"
        with self.assertRaisesRegex(Reject,"ORIGINAL_MAP_BYTES_DRIFT"):
            inspect(self.original,raw,require_pins=True)

    def test_05_reject_duplicate_hazard_ids(self):
        self.check_mutated(lambda p,m:p["levels"][0]["zones"][1].update(
            id=p["levels"][0]["zones"][0]["id"]),"DUPLICATE_OR_INVALID_ZONE_ID")

    def test_06_reject_off_grid_hazard_world_position(self):
        self.check_mutated(lambda p,m:p["levels"][0]["zones"][0].update(
            world=[29,32]),"CELL_TO_WORLD_DISAGREEMENT")

    def test_07_reject_occupied_hazard_cell(self):
        self.check_mutated(lambda p,m:p["levels"][0]["zones"][0].update(
            cell=[7,9],world=[28,36]),"ZONE_CENTER_NOT_CLEAR")

    def test_08_reject_authorized_longer_active_time(self):
        self.check_mutated(lambda p,m:p["levels"][0]["zones"][0].update(
            active=3),"INVALID_ZONE_ACTIVE")

    def test_09_reject_staggered_both_active_same_offset(self):
        self.check_mutated(lambda p,m:p["levels"][0]["zones"][1].update(
            offset=0),"NO_ALTERNATING_SAFE_LANE")

    def test_10_reject_mismatched_fixed_profile_map(self):
        self.check_mutated(lambda p,m:p["levels"][0].update(
            levelId="UNKNOWN_LEVEL"),"LEVEL_SET_DRIFT")

    def test_11_reject_trapped_spawn_even_when_map_item_count_unchanged(self):
        def trap(p,maps):
            key="SLALOM_PULSE_CROSSING"
            doc=json.loads(maps[key])
            sx,sz=next((x["x"],x["z"]) for x in doc["items"] if x["kind"]==4)
            for x in doc["items"]:
                if x["kind"]==0 and abs(x["x"]-sx)+abs(x["z"]-sz)==1:
                    x["kind"]=1
            maps[key]=(json.dumps(doc,sort_keys=True)+"\n").encode()
        self.check_mutated(trap,"NO_ZERO_PULSE_TIMED_ROUTE")

    def test_12_reject_unmodeled_multicell_module_geometry(self):
        def mutate(p,maps):
            key="SLALOM_PULSE_CROSSING"
            doc=json.loads(maps[key])
            next(x for x in doc["items"] if x["kind"]==1)["scaleX"]=2
            maps[key]=(json.dumps(doc,sort_keys=True)+"\n").encode()
        self.check_mutated(mutate,"UNSUPPORTED_MULTICELL_FOOTPRINT")

    def test_13_reject_unknown_original_map_kind(self):
        def mutate(p,maps):
            key="CLEARANCE_PULSE_CROSSING"
            doc=json.loads(maps[key])
            doc["items"][0]["kind"]=99
            maps[key]=(json.dumps(doc,sort_keys=True)+"\n").encode()
        self.check_mutated(mutate,"UNKNOWN_KIND")

    def test_14_reject_wrong_original_grid_meter_contract(self):
        self.check_mutated(lambda p,m:p.update(gridMeters=5),"INVALID_PROFILE_CONTRACT")

    def test_15_original_hazard_phases_cover_real_ticks_with_safe_alternative(self):
        for level in self.original["levels"]:
            a,b=level["zones"]
            for tick in range(0,24*8):
                from receiver import active
                self.assertFalse(active(a,tick) and active(b,tick))

    def test_16_reject_one_hit_due_to_wrongly_shifted_source_map_hash(self):
        key="CLEARANCE_PULSE_CROSSING"
        v=copy.deepcopy(self.original)
        v["levels"][0]["mapGitBlob"]="0"*40
        with self.assertRaisesRegex(Reject,"ORIGINAL_MAP_PROVENANCE_DRIFT"):
            inspect(v,self.maps,require_pins=True)

if __name__=="__main__":
    unittest.main()
