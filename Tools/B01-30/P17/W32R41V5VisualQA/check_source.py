#!/usr/bin/env python3
"""Independent B01-30 SOURCE RED witness, never native Unity acceptance."""
import hashlib,json,pathlib,re,subprocess,sys
ROOT=pathlib.Path(__file__).resolve().parents[4]
BASE="ba92a52a9bb73fb63b17e94a3af4a199868ef92e"
EDITOR="Assets/B01_31/W30R41V5Adapter/Editor/B0131W30CreateR41V5AdapterCandidate.cs"
TEST="Assets/B01_30/W32R41V5VisualQA/Editor/R41V5VisualRigAcceptanceTests.cs"
OWN="Tools/B01-30/P17/W32R41V5VisualQA/"
WF=".github/workflows/b01-30-p17w32-r41v5-visual-qa.yml"
EXPECTED={WF,"Assets/B01_30.meta","Assets/B01_30/W32R41V5VisualQA.meta","Assets/B01_30/W32R41V5VisualQA/Editor.meta",TEST,TEST+".meta",
          "Assets/B01_30/W32R41V5VisualQA/Editor/RoboRougelike.B0130.W32.RealR41V5VisualQA.asmdef","Assets/B01_30/W32R41V5VisualQA/Editor/RoboRougelike.B0130.W32.RealR41V5VisualQA.asmdef.meta",OWN+"README.md",OWN+"check_source.py"}
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT)
def require(b,s):
 if not b:raise RuntimeError("B0130_W32_QA_SOURCE_RED "+s)
def problem(b):return b"copy.transform.SetParent(pitch, true);" in b
def main():
 git("merge-base","--is-ancestor",BASE,"HEAD")
 lines=git("diff","--name-status",BASE,"HEAD").decode().strip().splitlines()
 pairs=[l.split("\t",1) for l in lines]
 require(len(pairs)==10 and {p for status,p in pairs}==EXPECTED and
         all(status=="A" for status,p in pairs),"Only 10 additive B01-30 QA paths")
 original=git("show",BASE+":"+EDITOR)
 require(original==(ROOT/EDITOR).read_bytes(),"Original B01-31 source drift")
 require(problem(original),"P0 unexpectedly absent: re-review actual rig code")
 require(not problem(original.replace(b"copy.transform.SetParent(pitch, true);",
        b"copy.transform.SetParent(yaw, true);")),"Mutated source detector negative")
 test=(ROOT/TEST).read_bytes()
 require(test.count(b"[Test]")==2 and b"Assert.Inconclusive(" in test and
         b"Assert.IsFalse(visual.IsChildOf(pitch)" in test and
         b"Vector3.Distance(p0,visual.position)" in test,
         "Real FBX native 2-case negative tests")
 require(not (ROOT/"Assets/B01_31/W30R41V5Adapter/Incoming/R41_Turret_v5.fbx").exists(),
         "Never ship external private original FBX in public QA branch")
 guids={}
 for p in (ROOT/"Assets").rglob("*.meta"):
  match=re.search(r"(?m)^guid: ([a-f0-9]{32})$",p.read_text(errors="replace"))
  if match:
   guid=match.group(1)
   require(guid not in guids,"duplicate Unity GUID "+guid)
   guids[guid]=p.relative_to(ROOT).as_posix()
 meta=[p for p in EXPECTED if p.endswith(".meta")]
 require(len(meta)==5,"Own 5 native Unity meta files")
 for p in meta:
  content=(ROOT/p).read_text()
  match=re.search(r"(?m)^guid: ([a-f0-9]{32})$",content)
  require(match and guids.get(match.group(1))==p,"own GUID conflict "+p)
 out=ROOT/"build"/"B0130W32";out.mkdir(parents=True,exist_ok=True)
 receipt={"package":"B01-30-P17-W32-R41V5-VISUAL-QA-R1",
          "original_upstream_sha":BASE,"original_editor_sha256":hashlib.sha256(original).hexdigest(),
          "scope":"10_ADD_ONLY_PASS","globally_unique_unity_guids":len(guids),
          "negative_source_detection":"PASS","producer_visual_hierarchy":"P0_SOURCE_RED",
          "native_unity":"AUTHORED_NOT_RUN","real_fbx":"NOT_PRESENT","windows_player":"NOT_RUN",
          "integrated":False,"released":False}
 (out/"receipt.json").write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n")
 print(json.dumps(receipt,sort_keys=True))
 print("SOURCE_WITNESS_PASS_PRODUCT_VISUAL_P0_RED_NATIVE_UNITY_NOT_RUN")
if __name__=="__main__":
 try:main()
 except Exception as e:print(str(e),file=sys.stderr);sys.exit(1)
