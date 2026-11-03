#!/usr/bin/env python3
"""B01-12 W32 exact original-game source QA (not a native Unity test)."""
import argparse, hashlib, json, re, subprocess, sys
from pathlib import Path
PARENT="3bd9b159997924dcecb4e1cd1d41137896e5199e"
OLD="bfbc544ef6722ebd60ed86ed65ba45a441726069"
BASE="Tools/B01/B01-12/P17W30GarageSpatial"
RUNTIME="Assets/B01UserExperience/B0112GarageSpatialW28/B0112GarageSpatialOverlay.cs"
TESTS="Assets/Tests/Editor/B0112GarageSpatialW28Tests.cs"
HOST={
 "Assets/R3/R3BuildValidation.cs":"996a2de80039103538700020cca38bc0ede6076a",
 "Assets/R3/R3GarageView.cs":"1976a19516905be63259109ec1af0a92ef52d6f4",
 "Assets/R3/R3RestorationValidation.cs":"0d31d7201957d36b637a9be58574ca6c4c5d1dbf",
 "Assets/R3/R3VehicleModel.cs":"1d0f378dc598e08b3d7c16975f419d84231511bb",
 "Assets/RoboRougelikeMechanics/WPN005RotarySaw/Runtime/RotarySawConfiguration.cs":"a3f05958a0549f2a8d5a971463f099792936169c",
}
def blob(b):return hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
def main():
 p=argparse.ArgumentParser()
 p.add_argument("--root",type=Path)
 p.add_argument("--overlay-copy",type=Path)
 p.add_argument("--tests-copy",type=Path)
 a=p.parse_args()
 root=(a.root or Path(__file__).resolve().parents[4]).resolve()
 m=json.loads((root/BASE/"manifest.json").read_text())
 if m.get("package_id")!="B01-12-W30-LIVE-GARAGE-SAW-GHOST-001/R3" or m.get("parent_sha")!=PARENT:
  print("FAIL_CLOSED invalid manifest/parent");return 2
 expected={RUNTIME,TESTS,BASE+"/source_gate.py",BASE+"/README.md",".github/workflows/b01-12-w30-garage-ghost.yml"}
 if set(m["files"])!=expected:
  print("FAIL_CLOSED wrong file scope");return 2
 for name,pin in m["files"].items():
  path=(a.overlay_copy if name==RUNTIME and a.overlay_copy else
        a.tests_copy if name==TESTS and a.tests_copy else root/name)
  b=path.read_bytes()
  if len(b)!=pin["bytes"] or blob(b)!=pin["git_blob_sha1"]:
   print("FAIL_CLOSED source mismatch:",name);return 2
 print("MANIFEST 5/5 EXACT Git blobs+byte sizes")
 old=subprocess.run(["git","show",PARENT+":"+RUNTIME],cwd=root,capture_output=True)
 if old.returncode or blob(old.stdout)!=OLD:
  print("FAIL_CLOSED untrusted #877 baseline");return 2
 s=(root/RUNTIME).read_text(encoding="utf-8");t=(root/TESTS).read_text(encoding="utf-8")
 before=old.stdout.decode("utf-8")
 view=(root/"Assets/R3/R3GarageView.cs").read_text(encoding="utf-8-sig")
 config=(root/"Assets/RoboRougelikeMechanics/WPN005RotarySaw/Runtime/RotarySawConfiguration.cs").read_text()
 results=[]
 def ok(name,condition):
  results.append(bool(condition));print(("PASS " if condition else "FAIL ")+name)
 ok("R1 original bug: mounted-only rotary saw lookup",
    "root.GetComponent<R3MountedPart>()?.Weapon==R4WeaponKind.RotarySaw" in before
    and "AddFunctionalEnvelope(source.Preview,source.MountKind)" in before)
 ghost=view[view.index("private GameObject CreateGhost("):view.index("private static void AddPortMarkers(")]
 ok("original R3 ghost has no R3MountedPart", "AddComponent<R3MountedPart>" not in ghost and "R41PartGeometry.Payload" in ghost)
 ok("new saw-only discriminator", "kind==R3PartKind.Gun&&weapon==R4WeaponKind.RotarySaw" in s)
 ok("installed + ghost call carry authoritative weapon type",
    "AddFunctionalEnvelope(p.transform,p.Kind,p.Weapon)" in s
    and "AddFunctionalEnvelope(source.Preview,source.MountKind,source.MountWeapon)" in s)
 ok("orange hint emitted for actual saw without mounted part",
    "if(NeedsSawDiscZone(kind,weapon))" in s and "var hint=SawDiscHint(muzzle)" in s
    and "new Vector3(.28f,1.8f,1.8f)" in s)
 ok("orange hint matches original WPN005 default envelope",
    "bladeRadius = 0.9f" in config and "bladeHalfThickness = 0.14f" in config)
 ok("R3 authority: EMP radial exemption and distinct contact Saw disc",
    "if(candidate.Weapon==R4WeaponKind.EmpEmitter)return null;" in
      (root/"Assets/R3/R3BuildValidation.cs").read_text()
    and "if(candidate.Weapon==R4WeaponKind.RotarySaw)" in
      (root/"Assets/R3/R3BuildValidation.cs").read_text())
 ok("W32 UI: non-projectile EMP and Saw do not inherit fake directional purple line",
    "NeedsDirectionalMuzzleZone(R3PartKind kind,R4WeaponKind weapon)" in s
    and "weapon!=R4WeaponKind.EmpEmitter" in s
    and "weapon!=R4WeaponKind.RotarySaw" in s
    and "if(NeedsDirectionalMuzzleZone(kind,weapon))" in s
    and "if(weapon==R4WeaponKind.EmpEmitter)" in s
    and "Vector3.one*.55f" in s
    and all(k in t for k in ["ActualEmpGhostShowsSourceMarkerButNeverDirectionalShotLine",
                            "ActualSawGhostDrawsOnlyOrangeDiscWhileBasicGunKeepsTwoPurpleHints",
                            "ProjectileGunOnlyOwnsDirectionalMuzzleVisual"]))
 ok("reused lists for 3 previously allocating traversals",
    all(k in s for k in ["GetComponentsInChildren(true,rendererScratch)",
      "GetComponentsInChildren(true,transformScratch)",
      "GetComponentsInChildren(true,socketScratch)"]))
 ok("Editor temporary mesh cleanup", "else DestroyImmediate(primitive)" in s)
 ok("host owns permission and overlay cannot spend or write",
    "reason=source.PlacementIssue" in s and "=> hasTarget&&placementIssue==null" in s
    and not re.search(r"\.(GarageBuy|GarageMount|GarageRemove|GarageBuyWeapon|Save|Commit|Spend|Award)\s*\(",s))
 ok("real Unity NUnit test calls production envelope on unmounted ghost",
    t.count("[Test]")==15 and "ActualFunctionalEnvelopeCreatesOrangeZoneForUninstalledSawGhost" in t
    and 'GetMethod("AddFunctionalEnvelope",flags)' in t)
 restoration=(root/"Assets/R3/R3RestorationValidation.cs").read_text(encoding="utf-8")
 validation=(root/"Assets/R3/R3BuildValidation.cs").read_text(encoding="utf-8")
 ok("W31 restoration shares live functional source gate without new B01-12 authority",
    "FunctionalWeaponPlacementIssue(prospective[index],probe)" in restoration
    and "ExistingMountedWeaponPlacementIssue(probe,colliders,replacedIds)" in restoration
    and "FunctionalWeaponPlacementIssue(candidate,probe)" in validation
    and "ExistingMountedWeaponPlacementIssue(probe,candidateColliders)" in validation)
 ok("current #877 host runtime untouched",all(blob((root/f).read_bytes())==pin for f,pin in HOST.items()))
 print(f"W32 SOURCE_GUARD {sum(results)}/{len(results)}; UNITY_G1=NOT_RUN; PLAYER=NOT_RUN")
 return 0 if all(results) else 1
if __name__=="__main__":sys.exit(main())
