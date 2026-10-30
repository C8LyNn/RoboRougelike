import copy
import hashlib
import re

def semantic_fingerprint(gallery):
    fields=[gallery["schema"],gallery["source_sha"],gallery["source_package"],
            "1" if gallery["reference_only"] else "0",len(gallery["offers"])]
    for offer in gallery["offers"]:
        fields.extend([offer["id"],offer["title"],offer["role"],offer["description"],
                       offer["price_scrap"],offer["pieces"],len(offer["parts"])])
        for part in offer["parts"]:
            fields.extend([part["kind"],part.get("weapon",""),part["count"],part["unit_scrap"]])
    canonical="".join(str(len(str(v).encode("utf-16-le"))//2)+":"+str(v) for v in fields)
    return hashlib.sha256(canonical.encode("utf-8")).hexdigest()
import json
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
DATA=ROOT/"Assets/B01KitGallery001/Data/KitGallery.json"
FIXTURE=Path(__file__).parent/"original_source_fixture.json"
def transform(source):
    offers=[]
    for k in source["kits"]:
        parts=[dict(kind=p["kind"],weapon=p.get("weapon",""),count=p["count"],unit_scrap=(source["weapon_prices"][p["weapon"]] if p["kind"]=="Gun" else source["part_prices"][p["kind"]])) for p in k["parts"]]
        offers.append(dict(id=k["id"],title=k["title"],role=k["role"],description=k["description"],parts=parts,price_scrap=sum(p["unit_scrap"]*p["count"] for p in parts),pieces=sum(p["count"] for p in parts)))
    return dict(schema="b01-22-unity-gallery-v1",source_sha="b1d4e341d846c9397e236809a1d7e4c4fff9c935",source_package="B01-22-EQUIPMENT-KITS-001",reference_only=True,offers=offers)
def validate(fixture,gallery):
    if gallery!=transform(fixture): raise ValueError("ORIGINAL_SOURCE_MISMATCH")
    return True
class GalleryTests(unittest.TestCase):
    def setUp(self):
        self.catalog=json.loads(FIXTURE.read_text(encoding="utf8"))
        self.gallery=json.loads(DATA.read_text(encoding="utf8"))
    def test_exact_original_content(self):self.assertTrue(validate(self.catalog,self.gallery))
    def test_all_10_kits(self):self.assertEqual(len(self.gallery["offers"]),10)
    def test_10_distinct_ids(self):self.assertEqual(len(set(k["id"] for k in self.gallery["offers"])),10)
    def test_ek08_price(self):self.assertEqual(self.gallery["offers"][7]["price_scrap"],235)
    def test_reference_only(self):self.assertTrue(self.gallery["reference_only"])
    def test_all_count_legal(self):self.assertTrue(all(p["count"]>0 for k in self.gallery["offers"] for p in k["parts"]))
    def test_non_host_asset(self):self.assertNotIn("Assets/R3/",str(DATA))
    def test_wrong_sha_rejected(self):
        x=copy.deepcopy(self.gallery);x["source_sha"]="tampered"
        with self.assertRaises(ValueError):validate(self.catalog,x)
    def test_wrong_price_rejected(self):
        x=copy.deepcopy(self.gallery);x["offers"][0]["price_scrap"]+=1
        with self.assertRaises(ValueError):validate(self.catalog,x)
    def test_unknown_part_rejected(self):
        x=copy.deepcopy(self.gallery);x["offers"][0]["parts"][0]["kind"]="Unknown"
        with self.assertRaises(ValueError):validate(self.catalog,x)
    def test_missing_kit_rejected(self):
        x=copy.deepcopy(self.gallery);x["offers"].pop()
        with self.assertRaises(ValueError):validate(self.catalog,x)
    def test_new_codex_provenance(self):
        self.assertEqual(self.catalog["version"],"1.0.3")
        self.assertEqual(self.catalog["provenance"]["codex_sha"],"203a0805251ed28293888e43f2d49f0df63767c6")
    def test_raw_bom_reference_prices(self):
        self.assertEqual([x["price_scrap"] for x in self.gallery["offers"]],
                         [110,205,146,175,155,160,195,235,165,176])
    def test_90_distinct_kit_pairs(self):
        self.assertEqual(sum(a["id"]!=b["id"] for a in self.gallery["offers"] for b in self.gallery["offers"]),90)
    def test_ek08_vs_ek01_delta(self):
        self.assertEqual(self.gallery["offers"][7]["price_scrap"]-self.gallery["offers"][0]["price_scrap"],125)
    def test_snapshot_digest_matches_compiled_csharp_constant(self):
        source=(ROOT/"Assets/B01KitGallery001/Editor/KitGalleryWindow.cs").read_text(encoding="utf-8")
        signature=re.search(r'FrozenCatalogDigest = "([0-9a-f]{64})"',source)
        self.assertIsNotNone(signature)
        self.assertEqual(semantic_fingerprint(self.gallery),signature.group(1))
    def test_coherently_forged_price_has_different_snapshot_digest(self):
        forged=copy.deepcopy(self.gallery)
        forged["offers"][0]["parts"][0]["unit_scrap"]+=3
        forged["offers"][0]["price_scrap"]+=6
        self.assertEqual(forged["offers"][0]["price_scrap"],sum(x["count"]*x["unit_scrap"] for x in forged["offers"][0]["parts"]))
        self.assertNotEqual(semantic_fingerprint(forged),semantic_fingerprint(self.gallery))
if __name__=="__main__":unittest.main()
