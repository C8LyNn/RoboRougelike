"""#1239 phase 0: canonical asset inventory, built from the REAL source files at intake (no hand-typed counts).
Reads the pinned checkouts of the source PR heads (R4_REF) + the A_Sleek packages in this branch and writes
inventory/canonical_inventory.json (+ .csv, INVENTORY.md). usage: R4_REF=<refs> python3 build_inventory.py"""
import os, sys, json, glob, hashlib, csv
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__)); PKG = os.path.dirname(HERE); MF = os.path.dirname(PKG)
sys.path.insert(0, os.path.join(MF, "R4BlocksRedesign1232", "scripts"))
import paths, glbio
STYLES = ["A", "B", "C", "D", "E"]
FREEZE = "2026-11-12"
SRC = {"blocks": dict(pr=963, branch="art/r4-building-blocks"), "weapons": dict(pr=939, branch="art/938-r4-weapon-models"), "movement": dict(pr=941, branch="art/940-r4-movement-models"),
       "wheels": dict(pr=1018, branch="art/r4-wheel-variants"), "connectors": dict(pr=1186, branch="art/connector-triforce-1182")}
PINS = {"pilot_blocks": dict(pr=1234, sha="5f4470d5cb148bcae358f6d09ec0f20e02590946"), "sleek_blocks": dict(pr=1237, sha="3002a94ed86dab0eff5b6cedb801edf9bc98b08d"),
        "sleek_modules": dict(pr=1238, sha="510a89ba3342470e6a81fef3c0bf4c8dd6ab535e"), "r41_automatic": dict(pr=1029, branch="feature/automatic-gun-r41-art", sha="46e83f87396300b32c90dd6d3aabfb3ae66e5f8e")}
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()
def rel(p): return p.replace("\\", "/").split("/Tools/")[-1] if "/Tools/" in p else p
def glb_facts(p):
    m = glbio.load(p); lo, hi = m.bounds(); nodes = {}
    for pr in m.prims: nodes[pr["node"]] = nodes.get(pr["node"], 0) + len(pr["idx"])
    emp = {k: [round(float(x), 4) for x in v[0][:3, 3]] for k, v in m.empties.items()}
    return dict(triangles=m.tri_count(), bounds_file_m=[[round(float(x), 4) for x in lo], [round(float(x), 4) for x in hi]], mesh_nodes=nodes, empties_file_m=emp,
                glb_animation_channels=sum(len(a["channels"]) for a in m.json.get("animations", [])), materials=sorted({pr["material"] for pr in m.prims}))
def item(cid, family, kind, src_lib, src_path, **kw):
    d = dict(CanonicalPartId=cid, family=family, gameplay_kind=kind, source=dict(library=src_lib, pr=kw.pop("pr"), commit=kw.pop("commit"), path="Tools/" + rel(src_path), sha256=sha(src_path)))
    d.update(glb_facts(src_path)); d.update(kw)
    d["styles"] = {s: dict(status="NOT_STARTED", asset=None) for s in STYLES}
    return d
def main():
    items = []
    # ---- 30 building blocks: interface = PR #963 (approved), Sleek A art = #1234 pilot (5) + #1237 rollout (25)
    for qf in sorted(glob.glob(paths.ref_file("blocks", "R4B_*", "*_QA.json"))):
        q = json.load(open(qf)); kind = q["id"][4:]; g = paths.ref_glb("blocks", q["id"])
        a_dir = os.path.join(MF, "R4BlocksRedesign1232", "A_Sleek", q["id"]); origin = "pilot #1234"
        if not os.path.isdir(a_dir): a_dir = os.path.join(MF, "R4BlocksSleek1235", "A_Sleek", q["id"]); origin = "rollout #1237"
        it = item(q["id"], "block", "R4 building block / " + kind, "blocks", g, pr=963, commit=paths.REF_SHA["blocks"], occupied_cells=q["cells"], ports=q["ports"], contact_mask_local_anchor=q["contact_mask_local_anchor"],
                  sockets=["Socket_Mount"], collider="approved %s_colliders.glb (unchanged)" % q["id"], animation_semantics="static part: no source clips; atlas adds cosmetic Hit / Damaged / Recover on the visual body only",
                  source_clips=[], paint_zones=["R4P_Base", "R4P_Secondary", "R4P_Trim"], attribute_status="APPROVED interface (PR #963)", live_integration="not in main; FBX intake copy exists in PR #1029 branch (Assets/Art/R4Intake/Blocks)",
                  existing_style_A=dict(origin=origin, dir="Tools/" + rel(a_dir), glb_sha256=sha(os.path.join(a_dir, q["id"] + ".glb"))), overlaps=[])
        if os.path.isdir(os.path.join(MF, "R4BlocksRedesign1232", "B_Industrial", q["id"])): it["existing_style_B_pilot"] = "Tools/ModelFactory/R4BlocksRedesign1232/B_Industrial/" + q["id"] + " (pilot #1234, milder than the #1239 brief)"
        items.append(it)
    # ---- 20 connectors
    for mf in sorted(glob.glob(paths.ref_file("connectors", "R4C_*", "*_connector_manifest.json"))):
        c = json.load(open(mf)); q = json.load(open(mf.replace("_connector_manifest", "_QA"))); g = paths.ref_glb("connectors", c["id"])
        items.append(item(c["id"], "connector", "R4 connector / " + c["family"], "connectors", g, pr=1186, commit=paths.REF_SHA["connectors"], occupied_cells=c["occupied_cells"], ports=q["ports"], contact_mask_local_anchor=c.get("contact_mask_anchor"),
                          sockets=sorted(c.get("sockets", {})) if isinstance(c.get("sockets"), dict) else c.get("sockets"), nodes=c["nodes"], collider="approved %s_colliders.glb" % c["id"], connection_rule=c.get("connection_rule"),
                          animation_semantics="static part: no source clips; atlas adds cosmetic Hit / Damaged / Recover", source_clips=[], paint_zones="R41 palette slots in source; atlas maps to R4P_Base / Secondary / Trim (+Metal PROPOSAL)",
                          attribute_status="source PR #1186 open; connector contract marked proposal=%s in its manifest" % json.dumps(c.get("proposal"))[:80], live_integration="not in main", overlaps=[]))
    # ---- weapons, movement, wheels (R41 standard, animated)
    for lib, fam in (("weapons", "weapon"), ("movement", "movement"), ("wheels", "wheel")):
        for mf in sorted(glob.glob(paths.ref_file(lib, "R4*", "*_model_manifest.json"))):
            m = json.load(open(mf)); q = json.load(open(mf.replace("_model_manifest", "_QA"))); am = mf.replace("_model_manifest", "_animation_manifest"); a = json.load(open(am)) if os.path.exists(am) else None
            g = paths.ref_glb(lib, m["id"]); sw = q.get("sweep", {}); cells = sw.get("static_cells") or (sw.get("rest_pose", {}) or {}).get("cells") or [[0, 0, 0]]
            items.append(item(m["id"], fam, m["kind"], lib, g, pr=SRC[lib]["pr"], commit=paths.REF_SHA[lib], game_name=m.get("game_name"), occupied_cells=cells, mount_cell=sw.get("mount_cell", [0, 0, 0]), ports="mount socket only (R4 port, see sockets)",
                              sockets=[k for k in q.get("contract", {}) if k != m["id"] + "_ROOT"], contract_file_axes=q.get("contract"), hierarchy=m["hierarchy"], muzzles=m.get("muzzles", []),
                              collider="approved %s_colliders.glb" % m["id"], sweep=dict((k, sw[k]) for k in sw if k in ("aim_sweep", "static_bounds_file", "housing_bounds_file", "mount_cell")),
                              animation_semantics="baked master timeline, clips by frame range (fps %s)" % (a["fps"] if a else "-"), source_clips=[dict(name=c["name"], start=c["start"], end=c["end"], loop=c.get("loop")) for c in a["clips"]] if a else [],
                              animated_nodes=a.get("animated_nodes") if a else [], paint_zones="R41 palette slots in source (TeamColor_Red = team tint); atlas maps to R4P_Base / Secondary / Trim (+Metal, Glow PROPOSAL)",
                              attribute_status="source PR #%d open (status %s)" % (SRC[lib]["pr"], q.get("status")), live_integration="not in main" + ("; FBX intake copy exists in PR #1029 branch (Assets/Art/R4Intake)" if lib != "wheels" else ""), overlaps=[]))
    # ---- R41 Automatic: artist FBX only, on the integration branch of PR #1029
    items.append(dict(CanonicalPartId="R41_Automatic", family="weapon", gameplay_kind="R4WeaponKind.Automatic (R41 turret v6.1)", source=dict(library="r41_automatic", pr=1029, commit=PINS["r41_automatic"]["sha"],
                      path="Assets/Art/R4Intake/Weapons/R41Automatic/R41_Turret_v6.1.fbx", sha256="03c60486a6af5890f1106bc090d70bb3c09a67f8278e02c2942cab8e77845d6a (as recorded in Docs/R41_AUTOMATIC_ART_QA.md)"),
                      attribute_status="artist-delivered FBX, integrated only on feature/automatic-gun-r41-art (PR #1029, open). No .blend / GLB / manifests in the repository.", live_integration="PR #1029 (open), not in main",
                      note="Source is accessible (FBX). Clips, sockets and muzzle must be read from the FBX in Blender before production; until then the interface fields are UNKNOWN, not guessed.",
                      occupied_cells=None, sockets=None, source_clips=None, overlaps=[], styles={s: dict(status="NOT_STARTED", asset=None) for s in STYLES}))
    # ---- 61 modules + 6 world traps (#1238, Sleek A; interface = PROPOSAL)
    idx = json.load(open(os.path.join(MF, "R4ModulesSleek1x1", "INDEX.json")))
    for x in idx["items"]:
        d = os.path.join(MF, "R4ModulesSleek1x1", x["dir"]); q = json.load(open(os.path.join(d, x["id"] + "_QA.json"))); g = os.path.join(d, x["id"] + ".glb")
        fam = "world_trap" if x["world_object"] else ("deployer" if x["group"] == "F" or x["name"] in ("DeployableShieldGenerator", "DeployableAmplifier") else "module")
        sem = {"world_trap": "atlas clips Idle / Arm / Triggered / Recovery", "deployer": "atlas clips Idle / Deploy (prepare-release-reset, event Deploy_Out) / Hit", "module": "atlas clips Idle / Activate / Hit"}[fam]
        items.append(item(x["id"], fam, "catalogue #%s %s" % (x["number"], x["name"]), "sleek_modules", g, pr=1238, commit=PINS["sleek_modules"]["sha"], game_name=q["name_pl"], occupied_cells=[[0, 0, 0]] if not x["world_object"] else "footprint of one cell, on the ground",
                          ports=q["ports_proposal"], contact_mask_local_anchor=q["contact_mask_local_proposal"], sockets=(["Socket_Mount"] if not x["world_object"] else []) + q["function_empties"], mount=q["mount_proposal"],
                          collider="none authored (owner decision pending)", animation_semantics=sem + "; source has only the preview clip 'Active' (PROPOSAL)", source_clips=[dict(name="Active", start=1, end=49, loop=True)] if q["animation"] else [],
                          animated_nodes=sorted(q["movable_nodes"]), paint_zones=q["materials"], attribute_status="PROPOSAL: contacts, mount, R4P_Metal / R4P_Glow, movable nodes, clip (PR #1238 open)", live_integration="not in main",
                          existing_style_A=dict(origin="#1238", dir="Tools/ModelFactory/R4ModulesSleek1x1/" + x["dir"], glb_sha256=sha(g)), overlaps=[]))
    # ---- overlaps (same gameplay kind authored twice): keep both identities, flag for the owner
    by = {i["CanonicalPartId"]: i for i in items}
    for n in ("Blink", "Booster", "Gyroscope", "HoverEngine", "IndependentSteering", "JumpDrive", "MagneticGrapple", "Suspension"):
        a, b = "R4M_" + n, "R4X_" + n
        by[a]["overlaps"].append(dict(with_id=b, reason="same gameplay kind; R4M = approved-pipeline animated model (#941), R4X = 1x1x1 module (#1238, proposal)", resolution="both kept as separate canonical visuals; which one ships is an OWNER_DECISION"))
        by[b]["overlaps"].append(dict(with_id=a, reason="see " + a, resolution="OWNER_DECISION"))
    by["R4M_Wheel"]["overlaps"].append(dict(with_id="R4M_Wheel* (12 wheel variants, #1018)", reason="generic wheel of #941 vs 12 sized variants", resolution="13 separate identities kept"))
    legacy = []
    for ln in open(os.path.join(PKG, "inventory", "legacy_branch_scan.tsv")):
        b, s, n = ln.rstrip("\n").split("\t")
        if int(n) > 0 and "automatic-gun" not in b: legacy.append(dict(branch=b, head=s, model_files=int(n)))
    fam = {}
    for i in items: fam[i["family"]] = fam.get(i["family"], 0) + 1
    out = dict(issue=1239, scope_freeze=FREEZE, method="generated by scripts/build_inventory.py from the source files; main @5f06af8 contains no model files (0 fbx/glb/blend/obj)", source_pins=dict(SRC=SRC, REF_SHA=paths.REF_SHA, **PINS),
               count=len(items), by_family=fam, styles=STYLES, target_variants=len(items)*5, items=items,
               legacy_branch_sources=dict(status="OUT_OF_FROZEN_CATALOGUE - OWNER_DECISION", note="Older single-asset feature branches (rrl-model M01-M04, rrl-env) that contain OBJ/GLB experiments. They are not R4-standard parts, have no port / socket manifests "
                                          "and are not referenced by the families named in #1239. Listed so nothing is hidden; not scheduled for 5-style production unless the owner adds them.", branches=legacy,
                                          scan="145 candidate branches scanned (legacy_branch_scan.tsv); 1171 remote heads exist, branches without art-like names were not opened"),
               BLOCKED_SOURCE=[], NEW_ASSET_DELTA=[])
    json.dump(out, open(os.path.join(PKG, "inventory", "canonical_inventory.json"), "w"), indent=1)
    with open(os.path.join(PKG, "inventory", "canonical_inventory.csv"), "w", newline="") as f:
        w = csv.writer(f); w.writerow(["CanonicalPartId", "family", "gameplay_kind", "source_pr", "source_commit", "source_path", "source_sha256", "triangles", "occupied_cells", "source_clips", "attribute_status", "overlaps"] + ["style_" + s for s in STYLES])
        for i in items: w.writerow([i["CanonicalPartId"], i["family"], i["gameplay_kind"], i["source"]["pr"], i["source"]["commit"][:9], i["source"]["path"], i["source"]["sha256"][:16], i.get("triangles"), json.dumps(i.get("occupied_cells")),
                                    ";".join(c["name"] for c in (i.get("source_clips") or [])), i["attribute_status"], ";".join(o["with_id"] for o in i["overlaps"])] + [i["styles"][s]["status"] for s in STYLES])
    print(len(items), fam, "legacy branches", len(legacy))
if __name__ == "__main__": main()
