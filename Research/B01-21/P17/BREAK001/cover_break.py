#!/usr/bin/env python3
"""B01-21 MAP-BREAK-001: reversible single-cover destruction source-world states.

Data-only what-if world mechanic, not actual Unity destructible GameObjects.
Uses immutable actual MAPGEN001 C# authored geometry + B01-21 cover-source
LOS and source Heavy route checks. Python stdlib, deterministic artifacts.
"""
from __future__ import annotations
import argparse
import copy
import hashlib
import json
from pathlib import Path
import sys

HERE=Path(__file__).resolve().parent
P17=HERE.parent
sys.path.insert(0,str(P17/"COVER001"))
sys.path.insert(0,str(P17))
import cover_field as cover
import map_routes as routes

OUT=HERE/"generated"
PACKAGE="B01-21-MAP-BREAK-001"
ORIGINAL_COVER_SOURCE_SHA="454cd190639ba6ce3600ceed5f4fbd97fff298c2"
MAP_IDS=tuple(cover.INPUT_IDS)
PROFILES=("Light","Heavy")

def stable_json(obj):
    return json.dumps(obj,indent=2,sort_keys=True,ensure_ascii=False)+"\n"

def version_hash(original_hash, active_covers):
    # A content hash for the overlay state ONLY, NEVER a fake MAPGEN geometryHash.
    return hashlib.sha256((original_hash+":MAP-BREAK-001:"+",".join(str(x) for x in active_covers)).encode("ascii")).hexdigest()

def removed_plan(original, cover_ordinal):
    d=copy.deepcopy(original)
    indices=[i for i,e in enumerate(d["elements"]) if e["kind"]=="Cover"]
    if not 0<=cover_ordinal<len(indices):
        raise ValueError("INVALID_COVER_ORDINAL")
    source_idx=indices[cover_ordinal]
    removed=d["elements"].pop(source_idx)
    if removed["kind"]!="Cover":
        raise ValueError("SOURCE_COVER_ITEM_CHANGED")
    return d,source_idx

def baseline(original, layout_id):
    original_covers=cover.covers_in(original)
    pre=cover.original_sightline_field(original,layout_id)
    return {
      "schema":"B01-21-P17-MAP-BREAK-001-v1",
      "packageId":PACKAGE,
      "arenaId":layout_id,
      "originalInputBlueprintGitBlob":routes.SOURCE_BLOBS[layout_id],
      "originalMAPGENSourceGameSHA":routes.SOURCE_GAME,
      "originalGeometryHash":original["geometryHash"],
      "originalSeed":original["seed"],
      "worldUnits":"metres",
      "localXZ":True,
      "staticEyeHeightYMetres":cover.EYE_Y,
      "sourceEnemyDescriptors":original["enemySpawnDescriptors"],
      "originalFloorBoundariesAndCovers":[{
           "sourceElementIndex":i,
           "kind":e["kind"],"position":e["position"],"size":e["size"],
       } for i,e in enumerate(original["elements"]) if e["kind"] in ("Cover","Boundary")],
      "originalCoverCount":len(original_covers),
      "intactBaseline":{
           "stateHash":version_hash(original["geometryHash"],range(len(original_covers))),
           "visibilityGrid":pre["visibilityGrid"],
           "visibilityHistogram":pre["visibleEnemyCountHistogram"],
           "originalShelterAnchors":pre["shelterAnchors"],
           "clearSampleCells":len(pre["visibilityGrid"]),
       },
      "coverDestructionCases":[],
      "runtimeDisclaimer":{
           "maxDestroyedSimultaneously":1,
           "canRestoreToPristine":True,
           "precomputedStaticSourceOnly":True,
           "realPhysicsRaycasts":"NOT_RUN",
           "liveCoverDestructionScript":False,
           "nativeVehicleRoute":"NOT_RUN",
           "actualCombat":"NOT_RUN",
           "inGame":False,
       },
    }

def scenario(original, b, remove_ordinal):
    basec=cover.covers_in(original)
    if b["originalCoverCount"]!=len(basec):
        raise ValueError("STALE_ORIGINAL_COVER_COUNT")
    altered,source_index=removed_plan(original,remove_ordinal)
    if len(cover.covers_in(altered))!=len(basec)-1:
        raise ValueError("DESTROY_MUST_REMOVE_EXACTLY_ONE_SOURCE_COVER")
    original_cover=basec[remove_ordinal]
    intact=b["intactBaseline"]
    current_covers=cover.covers_in(altered)
    updated_grid=cover.sample_exposure(altered,current_covers)
    new_coords={(p["x"],p["z"]):p for p in updated_grid}
    before={ (p["x"],p["z"]):p for p in intact["visibilityGrid"] }
    if not set(before).issubset(new_coords):
        raise ValueError("REMOVING_COVER_REDUCED_SOURCE_CLEAR_FLOOR")
    changed=[]
    for pt,p in sorted(before.items()):
        after=new_coords[pt]
        if after["visibleEnemyCount"]<p["visibleEnemyCount"]:
            raise ValueError("DESTRUCTION_NON_MONOTONIC_SOURCE_LOS")
        if after["visibleEnemyCount"]>p["visibleEnemyCount"]:
            changed.append({
              "x":p["x"],"z":p["z"],
              "visibleBefore":p["visibleEnemyCount"],
              "visibleAfter":after["visibleEnemyCount"],
              "newlyVisibleEnemyDescriptors":sorted(set(after["visibleEnemyDescriptors"])-set(p["visibleEnemyDescriptors"])),
            })
    new_points=[new_coords[x] for x in sorted(set(new_coords)-set(before))]
    destroyed=set([remove_ordinal])
    remaining=[x for x in range(len(basec)) if x not in destroyed]
    old_anchors=intact["originalShelterAnchors"]
    invalidated=[]
    kept=[]
    for anchor in old_anchors:
        oid=anchor["coverSourceElementOrdinal"]
        if oid==remove_ordinal:
            invalidated.append(anchor["anchorId"])
            continue
        pos=anchor["anchorLocalXZ"]
        point=(pos["x"],pos["z"])
        if not routes.free_on_original_geometry(altered,point,routes.PROFILES["Heavy"]):
            invalidated.append(anchor["anchorId"])
            continue
        sight=cover.sight(altered,point,current_covers)
        # The retained source cover still must directly shield from a real
        # enemy descriptor, never silently mark an uncovered anchor as sheltered.
        owner_cover=basec[oid]
        owner_occludes=[i for i,e in enumerate(original["enemySpawnDescriptors"])
             if cover.clip_original_aabb((e["x"],e["z"]),point,owner_cover)]
        if not owner_occludes:
            invalidated.append(anchor["anchorId"])
            continue
        kept.append(anchor["anchorId"])
    if len(kept)+len(invalidated)!=len(old_anchors):
        raise ValueError("ANCHOR_INTEGRITY_LOST")
    path_counts={}
    for pname in PROFILES:
        profile=routes.PROFILES[pname]
        p=routes.shortest_to(altered,routes.START,routes.FINISH,profile)
        if p[0]!=routes.START or p[-1]!=routes.FINISH:
            raise ValueError("SOFTLOCK_SOURCE_ROUTE_AFTER_COVER_REMOVAL")
        path_counts[pname]=len(p)-1
    return {
       "scenarioId":"DESTROY_COVER_%02d"%(remove_ordinal+1),
       "operation":"REMOVE_ONE_COVER_SOURCE_BOX",
       "destroyedCoverOrdinal":remove_ordinal,
       "destroyedSourceElementIndex":source_index,
       "destroyedBox":original_cover,
       "remainingSourceCoverOrdinals":remaining,
       "overlayStateHash":version_hash(original["geometryHash"],remaining),
       "sourceClearGridCellsBefore":len(before),
       "sourceClearGridCellsAfter":len(updated_grid),
       "newlyClearCells":new_points,
       "priorClearCellsWithIncreasedEnemyVisibility":changed,
       "additionalVisibleEnemyPairCount":sum(x["visibleAfter"]-x["visibleBefore"] for x in changed),
       "originalShelterAnchorsInvalidated":invalidated,
       "originalShelterAnchorsRetained":kept,
       "originalSourceStartFinishGridStepsByHistoricalProfile":path_counts,
       "realWheelSweptAndReal3DRaycast":"NOT_RUN",
    }

def derive_plan(name_or_plan):
    # The regression suite also passes a deliberately mutated in-memory
    # blueprint; never silently reload and discard a negative test mutation.
    if isinstance(name_or_plan,dict):
        d=name_or_plan
        name=d.get("id")
    else:
        name=name_or_plan
        d=routes.load_original(name)
    if name not in MAP_IDS or d.get("id")!=name:
        raise ValueError("UNKNOWN_OR_MISMATCHED_SOURCE_ARENA")
    b=baseline(d,name)
    allcases=[scenario(d,b,i) for i in range(b["originalCoverCount"])]
    b["coverDestructionCases"]=allcases
    if len({p["overlayStateHash"] for p in allcases})!=len(allcases)+0:
        raise ValueError("NONUNIQUE_STATE_HASH")
    if len(allcases)==0 or not any(p["additionalVisibleEnemyPairCount"]>0 for p in allcases):
        raise ValueError("NO_OBSERVABLE_DESTRUCTION_EFFECT")
    if not any(p["originalShelterAnchorsInvalidated"] for p in allcases):
        raise ValueError("NO_INVALIDATION_OF_SOURCE_COVER_ANCHOR")
    return b

def viewer_html(all_data):
    # Read-only independent interactive prototype. One cover destroyed at a time;
    # the UI consumes exact precomputed source data, not synthetic game screenshots.
    payload=json.dumps(all_data,sort_keys=True,separators=(",",":"),ensure_ascii=False).replace("<","\\u003c")
    page=r'''<!doctype html><html lang="pl"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>B01-21 MAP-BREAK-001 — Destruction sandbox</title>
<style>
:root{color-scheme:dark;font-family:system-ui,sans-serif;background:#0e1924;color:#edf5fb}
*{box-sizing:border-box}body{margin:0}header,main,footer{max-width:1370px;margin:auto;padding:18px 25px}
h1{font-size:clamp(1.5rem,4vw,2.2rem);margin:.3rem 0}p{color:#b6ccd9}
.toolbar{display:flex;align-items:center;gap:12px;flex-wrap:wrap;margin:15px 0}
button,select{background:#263d4e;border:1px solid #6a8795;border-radius:8px;padding:10px;color:white;font-size:1rem;cursor:pointer}
button:focus-visible,select:focus-visible{outline:3px solid #fbd18a}
button:hover{background:#335368}
.two{display:grid;grid-template-columns:1fr 1fr;gap:15px}
.panel{background:#1b2c39;border:1px solid #405d6b;padding:12px;border-radius:12px;min-width:0}
canvas{display:block;width:100%;aspect-ratio:1/1;background:#334650;border-radius:5px}
h2{margin:4px 0 12px;font-size:1rem}
.stats{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:10px;margin:15px 0}
.stat{padding:12px;background:#1e3443;border-radius:8px}.stat strong{display:block;font-size:1.3rem}
small{color:#a9bfcc}#status{margin-top:10px;color:#f1cc93}
label{font-weight:650}output{font-family:ui-monospace,monospace}
@media(max-width:780px){.two{grid-template-columns:1fr}.stats{grid-template-columns:1fr 1fr}header,main,footer{padding:14px}}
</style></head><body>
<header><small>B01-21 / PASS17 / WAVE02 · source geometry only</small>
<h1>Niszczalne osłony — symulacja zmian mapy</h1>
<p>Kliknij osłonę na mapie lub wybierz ją z listy. Porównaj stan przed i po usunięciu bryły, widoczność trzech źródłowych znaczników przeciwnika oraz utratę punktów schronienia. Przywrócenie odtwarza identyczny stan.</p>
</header><main>
<div class="toolbar">
<label for="arena">Arena</label>
<select id="arena"><option value="AR16_01_OPEN_FLANK">Otwarta · 6 osłon</option><option value="AR16_02_SPLIT_DECISION">Podzielona · 11 osłon</option><option value="AR16_03_DENSE_PRESSURE">Gęsta · 16 osłon</option></select>
<label for="cover">Osłona do zniszczenia</label><select id="cover"></select>
<button type="button" id="destroy">Usuń wybraną osłonę</button>
<button type="button" id="restore">Przywróć wszystkie osłony</button>
</div>
<div class="two">
<section class="panel"><h2>Przed — oryginalna arena MAPGEN001</h2><canvas id="before" width="576" height="576" role="img" aria-label="Źródłowa arena z oryginalnymi bryłami osłon"></canvas></section>
<section class="panel"><h2>Po — pojedynczy wariant zniszczenia</h2><canvas id="after" width="576" height="576" role="img" aria-label="Arena po zniszczeniu osłony"></canvas></section>
</div>
<div class="stats" aria-live="polite"><div class="stat">Nowo widoczne relacje wróg–próbka<strong id="visibility">0</strong></div><div class="stat">Nowe przejezdne próbki źródłowe<strong id="clear">0</strong></div><div class="stat">Unieważnione punkty schronienia<strong id="anchors">0</strong></div></div>
<p id="status">Stan pierwotny · nic nie usunięto</p>
<p><small>Statyczne źródłowe AABB i wysokość oka 1,25 m. Historyczny obrys Light/Heavy. Bez rzeczywistego Physics.Raycast, dynamicznych colliderów, sterowania wrogiem i testów PhysX. Nie jest to build Unity ani zmiana kodu gry.</small></p>
<output id="selftest" data-status="NOT_RUN" hidden></output>
</main><footer><small>Źródła: oryginalne blueprints MAPGEN, przypięte hashe geometrii i odtwarzalne stany wyliczone w Pythonie. Nie należy wykorzystywać ich jako gwarancji przejazdu pojazdu.</small></footer>
<script id="data" type="application/json">__DATA__</script>
<script>
"use strict";
const DATA=JSON.parse(document.getElementById("data").textContent);
const arenaSelect=document.getElementById("arena"),coverSelect=document.getElementById("cover");
const removed={value:null};
const getArena=()=>DATA[arenaSelect.value];
function draw(canvas,arena,caseData){
 const ctx=canvas.getContext("2d");if(!ctx)throw new Error("NO_CANVAS_CONTEXT");
 ctx.clearRect(0,0,576,576);ctx.fillStyle="#354752";ctx.fillRect(0,0,576,576);
 const x=v=>(v+32)*9,z=v=>(32-v)*9;
 const original=arena.intactBaseline.visibilityGrid;
 const delta=new Map((caseData?.priorClearCellsWithIncreasedEnemyVisibility||[]).map(p=>[p.x+","+p.z,p]));
 const newClear=(caseData?.newlyClearCells||[]);
 const grid=(caseData?[...original,...newClear]:original);
 for(const p of grid){
   const patch=delta.get(p.x+","+p.z);
   const exposed=patch?patch.visibleAfter:p.visibleEnemyCount;
   ctx.fillStyle=["#56d5b0","#a1caa3","#f4b96d","#f17875"][exposed];
   ctx.globalAlpha=.8;ctx.fillRect(x(p.x)-5,z(p.z)-5,10,10);ctx.globalAlpha=1;
 }
 const boxes=arena.originalFloorBoundariesAndCovers.filter(e=>e.kind!=="Cover"||
  !caseData||e.sourceElementIndex!==caseData.destroyedSourceElementIndex);
 for(const e of boxes){
   ctx.fillStyle=e.kind==="Boundary"?"#d4a984":"#b17db5";
   ctx.fillRect(x(e.position.x)-e.size.x*4.5,z(e.position.z)-e.size.z*4.5,e.size.x*9,e.size.z*9);
 }
 if(caseData){
   const e=arena.originalFloorBoundariesAndCovers.find(e=>e.sourceElementIndex===caseData.destroyedSourceElementIndex);
   ctx.strokeStyle="#fff2ae";ctx.lineWidth=3;ctx.setLineDash([6,4]);
   ctx.strokeRect(x(e.position.x)-e.size.x*4.5,z(e.position.z)-e.size.z*4.5,e.size.x*9,e.size.z*9);ctx.setLineDash([]);
 }
 for(const s of arena.sourceEnemyDescriptors){
   ctx.beginPath();ctx.arc(x(s.x),z(s.z),7,0,2*Math.PI);
   ctx.fillStyle="#f35f80";ctx.fill();ctx.lineWidth=2;ctx.strokeStyle="#fff";ctx.stroke();
 }
 ctx.beginPath();ctx.arc(x(0),z(-25),7,0,2*Math.PI);ctx.fillStyle="#4beac0";ctx.fill();
 ctx.beginPath();ctx.arc(x(0),z(25),7,0,2*Math.PI);ctx.fillStyle="#ffe77c";ctx.fill();
}
function render(){
 const arena=getArena();const damaged=removed.value===null?null:arena.coverDestructionCases[removed.value];
 draw(document.getElementById("before"),arena,null);
 draw(document.getElementById("after"),arena,damaged);
 document.getElementById("visibility").textContent=String(damaged?.additionalVisibleEnemyPairCount||0);
 document.getElementById("clear").textContent=String(damaged?.newlyClearCells.length||0);
 document.getElementById("anchors").textContent=String(damaged?.originalShelterAnchorsInvalidated.length||0);
 document.getElementById("status").textContent=damaged?
  "Zniszczono "+damaged.scenarioId+" · source state "+damaged.overlayStateHash.substring(0,12):
  "Stan pierwotny · nic nie usunięto";
 document.getElementById("status").dataset.removed=damaged?String(damaged.destroyedCoverOrdinal):"NONE";
}
function arenaChanged(){
 removed.value=null;const d=getArena();coverSelect.replaceChildren();
 d.coverDestructionCases.forEach((item,i)=>{
  const option=document.createElement("option");option.value=String(i);option.textContent="Cover "+String(i+1).padStart(2,"0");
  coverSelect.append(option);
 });
 render();
}
function destroy(){removed.value=Number(coverSelect.value);if(!Number.isInteger(removed.value)||
 removed.value<0||removed.value>=getArena().coverDestructionCases.length)throw new Error("INVALID_COVER_SELECTION");render();}
function restore(){removed.value=null;render();}
arenaSelect.addEventListener("change",arenaChanged);
document.getElementById("destroy").addEventListener("click",destroy);
document.getElementById("restore").addEventListener("click",restore);
document.getElementById("after").addEventListener("click",event=>{
 const bound=event.target.getBoundingClientRect();
 const cx=(event.clientX-bound.left)/bound.width*576;
 const cy=(event.clientY-bound.top)/bound.height*576;
 const items=getArena().originalFloorBoundariesAndCovers.filter(x=>x.kind==="Cover");
 const found=items.findIndex(e=>Math.abs(cx-(e.position.x+32)*9)<=e.size.x*4.5
    &&Math.abs(cy-(32-e.position.z)*9)<=e.size.z*4.5);
 if(found>=0){coverSelect.value=String(found);destroy();}
});
arenaChanged();
function sourceSelfTest(){
 let count=0;const errors=[];
 for(const name of Object.keys(DATA)){
   arenaSelect.value=name;arenaChanged();
   const d=getArena();
   if(coverSelect.options.length!==d.originalCoverCount)errors.push(name+"/COUNT");
   d.coverDestructionCases.forEach((c,i)=>{
     coverSelect.value=String(i);destroy();count++;
     if(document.getElementById("status").dataset.removed!==String(i)||
        Number(document.getElementById("visibility").textContent)!==c.additionalVisibleEnemyPairCount||
        Number(document.getElementById("clear").textContent)!==c.newlyClearCells.length||
        Number(document.getElementById("anchors").textContent)!==c.originalShelterAnchorsInvalidated.length)
          errors.push(name+"/"+i+"/EVENT");
     restore();
     if(document.getElementById("status").dataset.removed!=="NONE")errors.push(name+"/"+i+"/RESTORE");
   });
 }
 arenaSelect.value="AR16_02_SPLIT_DECISION";arenaChanged();
 const out=document.getElementById("selftest");
 out.dataset.status=errors.length?"FAIL":"PASS";
 out.textContent=errors.length?errors.join(","):"PASSED "+count+" COVER DESTRUCTION AND RESTORE OPERATIONS ON REAL ORIGINAL MAP DATA";
}
if(new URLSearchParams(location.search).get("selftest")==="1")
window.addEventListener("load",sourceSelfTest,{once:true});
</script></body></html>'''
    return page.replace("__DATA__",payload)+"\n"

def render_all():
    data={}
    outputs={}
    for name in MAP_IDS:
        d=derive_plan(name)
        data[name]=d
        outputs[name+".break.json"]=stable_json(d)
    outputs["cover_break_viewer.html"]=viewer_html(data)
    outputs["manifest.json"]=stable_json({
       "schema":"B01-21-MAP-BREAK-001-MANIFEST-v1",
       "sourceCover001Commit":ORIGINAL_COVER_SOURCE_SHA,
       "sourceBlueprintGitBlobs":routes.SOURCE_BLOBS,
       "originalMAPGENGameSHA":routes.SOURCE_GAME,
       "arenas":[{
           "arenaId":name,"sourceCoverCount":d["originalCoverCount"],
           "stateCount":len(d["coverDestructionCases"]),
           "totalNewlyVisiblePairsAcrossSingleCoverWhatIfs":sum(s["additionalVisibleEnemyPairCount"] for s in d["coverDestructionCases"]),
           "statesWithVisibilityIncrease":sum(s["additionalVisibleEnemyPairCount"]>0 for s in d["coverDestructionCases"]),
           "json":name+".break.json",
       } for name,d in data.items()],
       "generatedFiles":sorted(outputs),
       "testScope":"SOURCE_ONLY",
       "nativeUnity":"NOT_RUN",
       "playerBuild":"NOT_RUN",
       "integrated":False
    })
    return outputs

def main():
    cli=argparse.ArgumentParser(description=__doc__)
    cli.add_argument("--check",action="store_true",help="verify exact git-committed artifacts before overwriting")
    cli.add_argument("--emit-base64",action="store_true",help="for verified first-run git blob handoff")
    args=cli.parse_args()
    generated=render_all()
    OUT.mkdir(parents=True,exist_ok=True)
    import base64
    for name,text in sorted(generated.items()):
        p=OUT/name;raw=text.encode("utf-8")
        if args.check:
            if not p.exists() or p.read_bytes()!=raw:
                raise ValueError("COMMITTED_BREAK_STATE_DRIFT:"+name)
            print("MAP_BREAK_P17_BYTE_IDENTICAL",name)
        else:
            p.write_bytes(raw);print("MAP_BREAK_P17_GENERATED",name)
        if args.emit_base64:
            print("MAP_BREAK_P17_FILE_B64 | "+name+" | "+base64.b64encode(raw).decode("ascii"))
    print("MAP_BREAK_P17_SOURCE_PASS",len(MAP_IDS),"maps",sum(len(derive_plan(i)["coverDestructionCases"]) for i in MAP_IDS),"single-box cases; NATIVE_UNITY_NOT_RUN")
    return 0

if __name__=="__main__":
    raise SystemExit(main())
