#!/usr/bin/env python3
"""Independent adversarial tests of the original Git tree GUID/path receiver.
These are *synthetic collision controls*, separate from the real Codex tree scan.
"""
import importlib.util
from pathlib import Path
import unittest

SPEC=importlib.util.spec_from_file_location("original_guid_gate",Path(__file__).parent/"bracing_codex_preflight.py")
gate=importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(gate)

BASE="Assets/PARTSIZE001/Bracing/"
G0="11111111111111111111111111111111"
G1="22222222222222222222222222222222"
def meta(guid):return ("fileFormatVersion: 2\nguid: "+guid+"\nDefaultImporter:\n").encode()

def fixtures():
    own={
        "Assets/PARTSIZE001/Bracing.meta":meta("a"*32),
        BASE+"P17.meta":meta("b"*32),
    }
    for i,name in enumerate(("Bulwark","Flanker","Skirmisher","StructuralBracingKits")):
        p=BASE+"P17/P17_"+name+".json"
        own[p]=b'{"real_file_fixture":true}\n'
        own[p+".meta"]=meta(f"{i+1:032x}")
    game={"Assets/Game/Original.prefab.meta":meta(G0),
          "Assets/Game/Vehicle.cs.meta":meta(G1),
          "Assets/Game/Vehicle.cs":b""}
    return game,own

class CollisionAdversarialTests(unittest.TestCase):
    def assertReject(self,change,needle):
        game,own=fixtures()
        change(game,own)
        with self.assertRaisesRegex(gate.PreflightError,needle):
            gate.collisions(game,own)

    def test_01_valid_original_six_guid_four_json(self):
        game,own=fixtures()
        x=gate.collisions(game,own)
        self.assertEqual(x["newUniqueGuids"],6)
        self.assertEqual(x["pathCollisions"],0)
        self.assertEqual(x["newJsonFiles"],4)

    def test_02_original_guid_collision_must_fail(self):
        self.assertReject(lambda game,own:game.__setitem__("Assets/X/Danger.prefab.meta",own[BASE+"P17/P17_Flanker.json.meta"]),
                          "EXISTING_CODEX_UNITY_GUID_COLLISION")

    def test_03_exact_asset_path_collision_must_fail(self):
        self.assertReject(lambda game,own:game.__setitem__(BASE+"P17/P17_Flanker.json",b""),
                          "EXISTING_CODEX_ASSET_PATH_COLLISION")

    def test_04_casefold_only_asset_path_collision_must_fail(self):
        self.assertReject(lambda game,own:game.__setitem__("assets/partsize001/bracing/p17/p17_flanker.json",b""),
                          "EXISTING_CODEX_ASSET_PATH_COLLISION|INVALID_GAME_TREE_SCOPE")

    def test_05_original_game_duplicate_guid_must_fail(self):
        self.assertReject(lambda game,own:game.__setitem__("Assets/Game/Forged.prefab.meta",meta(G0)),
                          "DUPLICATE_EXISTING_GAME_GUID")

    def test_06_bracing_own_duplicate_guid_must_fail(self):
        self.assertReject(lambda game,own:own.__setitem__(BASE+"P17/P17_Bulwark.json.meta",
                           own[BASE+"P17/P17_Flanker.json.meta"]),"DUPLICATE_BRACING_GUID")

    def test_07_malformed_original_codex_meta_cannot_claim_zero_conflict(self):
        self.assertReject(lambda game,own:game.__setitem__("Assets/Game/Vehicle.cs.meta",b"bad"),
                          "INVALID_OR_MULTIPLE_META_GUID")

    def test_08_duplicate_meta_guid_line_rejected(self):
        self.assertReject(lambda game,own:own.__setitem__(BASE+"P17.meta",meta("b"*32)+b"guid: "+b"b"*32+b"\n"),
                          "INVALID_OR_MULTIPLE_META_GUID")

    def test_09_orphaned_original_json_asset_rejected(self):
        self.assertReject(lambda game,own:own.pop(BASE+"P17/P17_Flanker.json.meta"),
                          "WRONG_OWN_CONTENT_COUNTS")

    def test_10_extraneous_bracing_own_file_rejected(self):
        self.assertReject(lambda game,own:own.__setitem__(BASE+"P17/Unexpected.exe",b"surprise"),
                          "WRONG_OWN_CONTENT_COUNTS|BRACING")

    def test_11_original_codex_existing_paths_mixed_case_duplicate(self):
        self.assertReject(lambda game,own:game.__setitem__("Assets/Game/vehicle.cs",b""),
                          "CASE_INSENSITIVE_DUPLICATE_EXISTING_PATH")

    def test_12_codex_game_collision_with_folder_meta(self):
        self.assertReject(lambda game,own:game.__setitem__("Assets/PARTSIZE001/Bracing.meta",meta("9"*32)),
                          "EXISTING_CODEX_ASSET_PATH_COLLISION")

    def test_13_original_bracing_asset_scope_escape_rejected(self):
        self.assertReject(lambda game,own:own.__setitem__("Assets/Elsewhere/foreign.asset",b""),
                          "WRONG_OWN_CONTENT_COUNTS|OUT_OF_SCOPE_CONTENT_PATH")

    def test_14_missing_original_bracing_folder_meta_rejected(self):
        self.assertReject(lambda game,own:own.pop("Assets/PARTSIZE001/Bracing.meta"),
                          "WRONG_OWN_CONTENT_COUNTS")

    def test_15_negative_manifest_sha_constant_is_full_git_hex(self):
        self.assertEqual(len(gate.BRACING_MANIFEST_GIT_BLOB),40)
        self.assertTrue(gate.SHA_RE.fullmatch(gate.CODEX))
        self.assertTrue(gate.SHA_RE.fullmatch(gate.BRACING))

    def test_16_original_bracing_guid_normalizes_case(self):
        self.assertEqual(gate.parse_guid(meta("AB"*16),"dummy"),"ab"*16)

if __name__=="__main__":
    unittest.main(verbosity=2)
