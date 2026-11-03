#!/usr/bin/env python3
"""B01-62 W32; exact original product source; strict independent QA only."""
import hashlib,subprocess,json,sys
from pathlib import Path
GAME="3bd9b159997924dcecb4e1cd1d41137896e5199e"
SAVE="a0282f2de0250d2a8fe421bbe182b4cb6fab2a11"
HERE="Tools/B01/B01-62/P17W32R3WorldCompatibility/"
FILENAMES={"Program.cs","R3FactoryCompileShim.cs",
            "OriginalR3VsFrozen556.csproj","README.md",
            "manifest.json","source_guard.py"}
WORKFLOW=".github/workflows/b01-62-w32-r3-world-compatibility.yml"
SOURCES={
 "Assets/R3/R3VehicleCatalog.cs":("HEAD","f76de5bbdcc1dbdb7ddb9fe9859c36c232fd8584"),
 "Assets/R3/R3VehicleModel.cs":("HEAD","1d0f378dc598e08b3d7c16975f419d84231511bb"),
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":(SAVE,"b52e511c370f5fca56c74029050844dc8cbb7f9a"),
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":(SAVE,"fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
 "Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs":(SAVE,"e6e2068e4a59d6240d8516ae0899541263a6d5c6")
}
def git(*args):return subprocess.check_output(["git",*args],stderr=subprocess.PIPE).decode().strip()
def run(outfile):
 if Path(outfile).exists():raise AssertionError("REFUSE_OVERWRITE_RECEIPT")
 expected={HERE+x for x in FILENAMES}|{WORKFLOW}
 diff=set(git("diff","--name-only",GAME,"HEAD").splitlines())
 if diff!=expected:raise AssertionError("QA_OWNERSHIP_SCOPE_MISMATCH:"+str(diff^expected))
 for path,(ref,sha) in SOURCES.items():
  if git("rev-parse",ref+":"+path)!=sha:raise AssertionError("CHANGED_ORIGINAL:"+path)
 originals=[
  (Path("Assets/R3/R3VehicleCatalog.cs").read_text(),"R3"),
  (Path(HERE+".source/DetachedSnapshotContract.cs").read_text(),"556"),
  (Path(HERE+".source/WholeGameEconomySlotV3.cs").read_text(),"896")]
 r3,world,store=[t for t,_ in originals]
 for s in ("design.Parts.Add(new R3PartState","int width = light ? 1 : heavy ? 3 : 2;",
           "int length = light ? 2 : heavy ? 4 : 3;"):
  if s not in r3:raise AssertionError("R3_FACTORY_PROVENANCE:"+s)
 for s in ("public const int Slots=2;","s.Parts.Length!=FrozenPartCatalog.Slots",
           "p.Id!=FrozenPartCatalog.Id(i)","p.MassKg!=FrozenPartCatalog.Mass(i)"):
  if s not in world:raise AssertionError("556_FIXED_CONTRACT_MISSING:"+s)
 for s in ("ColdRestore.TryStage(world","Semantic(frozenWorld,frozenEcon",
           "File.Replace(staging,authoritative,null)"):
  if s not in store:raise AssertionError("896_SAME_WRT_PROTOCOL_NOT_CHECKED:"+s)
 model=Path("Assets/R3/R3VehicleModel.cs").read_text()
 for s in ('Guid.NewGuid().ToString("N")','public int Ammo=-1;',
          'public float ThermalHeat;','public Vector3Int RotationQuarterTurns;'):
  if s not in model:raise AssertionError("R3_DYNAMIC_STATE_CHANGED:"+s)
 if "  pull_request:" not in Path(WORKFLOW).read_text():
  raise AssertionError("MISSING_W32_SCOPE_ONLY_TRIGGER")
 receipt={"game_head":GAME,"one_save_head":SAVE,
   "game_part_counts":{"Light":20,"Standard":83,"Heavy":139},
   "frozen_world_slots":2,
   "source_ownership":sorted(expected),
   "product_compatibility":"P0_RED_DEMONSTRATED",
   "writer_modified":False,"native_unity":"NOT_RUN",
   "proposal":"versioned one-writer dynamic world contract requires owner authorization"}
 raw=(json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode()
 Path(outfile).write_bytes(raw)
 print("B0162_W32_EXACT_ORIGINAL_GAME_AND_896_GIT_BLOBS=PASS")
 print("B0162_W32_QA_ONLY_SCOPE_7_PATHS=PASS")
 print("B0162_W32_RECEIPT_SHA256="+hashlib.sha256(raw).hexdigest())
if __name__=="__main__":run(sys.argv[1])
