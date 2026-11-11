"""step 6: per-model manifests, INDEX.json, MODULE_FAMILY.md tables, SHA256SUMS.txt, CHANGED_FILES.txt. usage: python3 make_manifests.py"""
import json, os, hashlib, subprocess
import mkit as K, paths_m as P, modules as MD
from concepts import CONCEPT, NAME_PL, FUNC_PL
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()
GT = {"A": "A. Podstawowe", "B": "B. Ruch i sterowanie", "C": "C. Bonusy", "D": "D. Walka / energia / obrona", "E": "E. Informacja / ekonomia / pasywne", "F": "F. Wyrzutniki (wersje montowane na robocie)", "T": "Pułapki rozstawiane w świecie gry"}
ZONE_ROLE = {"R4P_Base": "duze panele - glowny kolor malowania (jak w klockach A_Sleek)", "R4P_Secondary": "rama / fazy / obudowy - drugi kolor malowania (jak w klockach A_Sleek)",
             "R4P_Trim": "ciemne elementy techniczne i scianki paneli (jak w klockach A_Sleek)", "R4P_Metal": "PROPOSAL: gole elementy mechaniczne (tloczyska, lufy, osie) - domyslnie Steel_Light",
             "R4P_Glow": "PROPOSAL: akcent emisyjny, kolor per modul (czytelnosc funkcji)"}
def main():
    tests = json.load(open(os.path.join(P.PKG, "tests", "module_tests.json")))["modules"]; index = []; rows = {g: [] for g in GT}
    for n, d in MD.ALL.items():
        i = ("R4D_" if d["group"] == "T" else "R4X_") + n; od = P.mdir(i); qa = json.load(open(os.path.join(od, i + "_QA.json"))); t = tests[i]
        hier = {i + "_ROOT": None}; roles = {i + "_ROOT": "root (origin = " + ("ground contact centre" if qa["world_object"] else "cell centre") + ")", i + "_Body": "static body"}
        hier[i + "_Body"] = i + "_ROOT"
        for nn, nd in qa["movable_nodes"].items(): hier[f"{i}_{nn}"] = i + "_ROOT"; roles[f"{i}_{nn}"] = f"movable part (PROPOSAL), pivot {nd['pivot']} m (Blender axes), preview motion {nd['anim']}"
        for p in qa["ports_proposal"]: hier[f"Port_{p['dir']}_000" + ("" if p["kind"] == "full" else "_part")] = i + "_ROOT"
        if not qa["world_object"]: hier["Socket_Mount"] = i + "_ROOT"; roles["Socket_Mount"] = f"PROPOSAL: primary mount face = {qa['mount_proposal']}, +Z of the empty points out of the module"
        for e in qa["function_empties"]: hier[e] = i + "_ROOT"; roles[e] = "PROPOSAL: effect / deploy origin, +Z = direction"
        man = dict(id=i, catalog_number=d["id"] if d["group"] != "T" else None, catalog_group=GT[d["group"]], name=n, name_pl=NAME_PL[n], function_pl=FUNC_PL[n], silhouette_concept_pl=CONCEPT[n],
                   kind="world object (deployed trap)" if qa["world_object"] else "robot-mounted module 1x1x1", units="meter 1:1", axes="Blender: X right, -Y forward, Z up; file / Unity: +Z forward, +Y up",
                   cell="logical 1x1x1 on the 0.55 m grid; geometry inside the 0.52 m block envelope" if not qa["world_object"] else "footprint inside one 0.55 m cell, origin on the ground, height <= 0.30 m",
                   hierarchy=hier, node_roles=roles, mount_proposal=None if qa["world_object"] else qa["mount_proposal"], contacts_proposal=qa["ports_proposal"], contact_count=qa["contact_count"],
                   contact_mask_local_proposal=qa["contact_mask_local_proposal"], paint_zones={z: ZONE_ROLE[z] for z in qa["materials"]}, glow_rgb=qa["glow_rgb"],
                   triangles=dict(lod0=qa["triangles"], lod1=qa["triangles_lod1"]), files=sorted(qa["files"]), status=qa["status"], unity="UNITY_NOT_RUN",
                   proposals=["contact faces / port set", "Socket_Mount face", "paint zones R4P_Metal and R4P_Glow", "movable nodes and the preview animation clip", "function empties (FX_*, Deploy_Out)", "naming prefix " + i[:4]],
                   gameplay="Art asset only. No gameplay value, timing, range or cost is defined or changed by this file.", notes=qa["notes"])
        json.dump(man, open(os.path.join(od, i + "_model_manifest.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
        ap = os.path.join(od, i + "_animation_manifest.json")
        if qa["animation"]:
            json.dump(dict(id=i, fps=24, frame_range=[1, 49], status="PROPOSAL - preview clip, visual only", note="One baked take in FBX / GLB. Frame 1 = rest pose (the QA pose). The clip loops seamlessly.",
                           clips=[dict(name="Active", start=1, end=49, loop=True, desc="; ".join(f"{nn}: {nd['anim']['type']} about / along {nd['anim']['axis']}" + (f" by {nd['anim']['amount']}" if 'amount' in nd['anim'] else " (full turns)") for nn, nd in qa["movable_nodes"].items() if nd.get("anim")))],
                           animated_nodes=[f"{i}_{nn}" for nn in qa["animation"]["nodes"]], sweep=qa["animation_sweep"], gameplay="Animation is visual only: it adds no gameplay capability or timing authority."),
                      open(ap, "w"), indent=1)
        elif os.path.exists(ap): os.remove(ap)
        ports = ", ".join(p["dir"] + ("" if p["kind"] == "full" else "*") for p in qa["ports_proposal"]) or "-"
        rows[d["group"]].append(f"| {('T%d' % (d['id'] - 100)) if d['group'] == 'T' else d['id']} | `{i}` | {NAME_PL[n]} | {CONCEPT[n]} | {ports} | {qa['triangles']} / {qa['triangles_lod1']} | {', '.join(sorted(qa['movable_nodes'])) or '-'} | {qa['status']} / {'PASS' if t['PASS'] else 'FAIL'} |")
        index.append(dict(id=i, number=d["id"], group=d["group"], wave=d["wave"], name=n, name_pl=NAME_PL[n], status=qa["status"], tests="PASS" if t["PASS"] else "FAIL", triangles=qa["triangles"], triangles_lod1=qa["triangles_lod1"],
                          contacts=[p["dir"] + ":" + p["kind"] for p in qa["ports_proposal"]], mount=qa["mount_proposal"], animated=bool(qa["animation"]), world_object=qa["world_object"],
                          dir=os.path.relpath(od, P.PKG).replace(os.sep, "/"), closest_silhouette=t["M6"]["closest"], closest_silhouette_iou=t["M6"]["min_view_iou"]))
    json.dump(dict(library="R4 Sleek module library 1x1x1", count=len(index), modules=sum(1 for x in index if not x["world_object"]), world_traps=sum(1 for x in index if x["world_object"]),
                   blender_pass=sum(1 for x in index if x["status"] == "BLENDER_PASS"), tests_pass=sum(1 for x in index if x["tests"] == "PASS"), unity="UNITY_NOT_RUN", windows_player="WINDOWS_PLAYER_NOT_RUN", items=index),
              open(os.path.join(P.PKG, "INDEX.json"), "w"), indent=1)
    with open(os.path.join(P.PKG, "CATALOG_TABLES.md"), "w", encoding="utf-8") as f:
        f.write("# Katalog modeli (generowany z rekordów QA)\n\nKontakty: nazwa ściany = pełny kontakt, `*` = kontakt częściowy. Wszystkie kontakty i mocowania to **proposal**.\n")
        for g, title in GT.items():
            f.write(f"\n## {title}\n\n| nr | plik | nazwa | sylwetka | kontakty | tris LOD0 / LOD1 | części ruchome | Blender / testy GLB |\n|---|---|---|---|---|---|---|---|\n" + "\n".join(rows[g]) + "\n")
    files = []
    for root, _, fs in os.walk(P.PKG):
        for fn in fs:
            fp = os.path.join(root, fn); rel = os.path.relpath(fp, P.PKG).replace(os.sep, "/")
            if "__pycache__" in rel or rel in ("SHA256SUMS.txt", "CHANGED_FILES.txt") or rel.startswith("preview/_stage"): continue
            files.append(rel)
    files.sort()
    with open(os.path.join(P.PKG, "SHA256SUMS.txt"), "w") as f:
        for rel in files: f.write(f"{sha(os.path.join(P.PKG, rel))}  {rel}\n")
    with open(os.path.join(P.PKG, "CHANGED_FILES.txt"), "w") as f: f.write("\n".join("Tools/ModelFactory/R4ModulesSleek1x1/" + r for r in files + ["SHA256SUMS.txt", "CHANGED_FILES.txt"]) + "\n")
    print("manifests", len(index), "files", len(files) + 2, "blender_pass", sum(1 for x in index if x["status"] == "BLENDER_PASS"), "tests", sum(1 for x in index if x["tests"] == "PASS"))
if __name__ == "__main__": main()
