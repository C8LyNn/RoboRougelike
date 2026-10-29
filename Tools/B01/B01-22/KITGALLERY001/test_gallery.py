import copy
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
    return dict(schema="b01-22-unity-gallery-v1",source_sha="0d212a706c1e633b773d2e03b7b40c28bb63521b",source_package="B01-22-EQUIPMENT-KITS-001",reference_only=True,offers=offers)
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
if __name__=="__main__":unittest.main()
