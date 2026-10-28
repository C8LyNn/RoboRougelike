// B01-25-CLEARANCELADDER-001 R1. Original MapWorld-source-based XZ AABB proxy.
// Does not execute Unity C#, actual PhysX/vehicle suspension, or real collider bounds.
export function analyzeLadder(doc,manifest){
  const tests=[],errors=[],assert=(name,ok)=>{tests.push({name,pass:!!ok});if(!ok)errors.push(name)};
  const items=Array.isArray(doc?.items)?doc.items:[];
  const key=(x,z)=>x+","+z;
  const dims=manifest?.arenaCells||[],W=dims[0],H=dims[1],gates=manifest?.gates||[];
  assert("MAPDOCUMENT_V2_LOCAL",doc?.version===2&&doc?.globalFloor===false);
  assert("DIMENSIONS_18X16_GRID4",W===18&&H===16&&manifest?.cellMeters===4);
  assert("SOURCE_ITEM_CAP",items.length<=4000&&items.filter(i=>i?.kind===0).length<=2000&&items.filter(i=>i?.kind!==0).length<=2000);
  assert("VALID_ORIGINAL_ITEM_LIMITS",items.every(i=>i&&Number.isInteger(i.kind)&&i.kind>=0&&i.kind<=9&&Number.isInteger(i.x)&&Number.isInteger(i.z)&&Math.abs(i.x)<=50&&Math.abs(i.z)<=50&&Number.isInteger(i.rotation)&&i.rotation>=0&&i.rotation<=3&&["scaleX","scaleY","scaleZ"].every(k=>Number.isInteger(i[k])&&i[k]>=1&&i[k]<=4)));
  const ground=items.filter(i=>i.kind===0),objects=items.filter(i=>i.kind!==0),solid=objects.filter(i=>i.kind!==4&&i.kind!==5);
  const kinds=items.reduce((s,i)=>(s[i.kind]=(s[i.kind]||0)+1,s),{});
  assert("EXACT_NATIVE_CONTENT_KINDS",items.length===415&&Object.entries(manifest.expectedKinds).every(([k,v])=>kinds[k]===v));
  assert("FULL_LOCAL_FLOOR_NO_DUPLICATES",ground.length===W*H&&new Set(ground.map(i=>key(i.x,i.z))).size===W*H);
  assert("NON_GROUND_UNIQUE_FOOTPRINT",objects.length===new Set(objects.map(i=>key(i.x,i.z))).size&&objects.every(i=>i.scaleX===1&&i.scaleZ===1));
  assert("SPAWN_FINISH_UNIQUE",kinds[4]===1&&kinds[5]===1);
  assert("SPAWN_FINISH_POSITION_STAGING",items.some(i=>i.kind===4&&i.x===9&&i.z===2)&&items.some(i=>i.kind===5&&i.x===9&&i.z===13));
  const floor=new Set(ground.map(i=>key(i.x,i.z))),occupied=new Set(solid.map(i=>key(i.x,i.z)));
  assert("MARKERS_ON_FREE_GROUND",floor.has("9,2")&&floor.has("9,13")&&!occupied.has("9,2")&&!occupied.has("9,13"));
  assert("FOUR_SOURCE_SOLID_CORNERS",[[0,0],[W-1,0],[0,H-1],[W-1,H-1]].every(([x,z])=>items.some(i=>i.kind===6&&i.x===x&&i.z===z&&i.rotation===0&&i.scaleY===4)));
  assert("PERIMETER_ORIGINAL_WALLS",items.filter(i=>i.kind===1&&(i.x===0||i.x===W-1||i.z===0||i.z===H-1)).length===2*(W-2)+2*(H-2));
  const bbox=i=>{
    const x=i.x*4,z=i.z*4;
    if(i.kind===1)return {kind:1,x0:x-(i.rotation%2?.25:2),x1:x+(i.rotation%2?.25:2),z0:z-(i.rotation%2?2:.25),z1:z+(i.rotation%2?2:.25)};
    if(i.kind===6)return {kind:6,x0:x-2,x1:x+2,z0:z-2,z1:z+2};
    if(i.kind===7)return {kind:7,x0:x-.75,x1:x+.75,z0:z-.75,z1:z+.75};
    if(i.kind===8)return {kind:8,x0:x-2,x1:x+2,z0:z-.25,z1:z+.25};
    return null;
  };
  const boxes=objects.map(bbox).filter(Boolean);
  const dist=(x,z,b)=>Math.hypot(Math.max(b.x0-x,0,x-b.x1),Math.max(b.z0-z,0,z-b.z1));
  const intersect=(A,B,r)=>{
    let lo=0,hi=1;
    for(const [k,mn,mx]of [["x","x0","x1"],["z","z0","z1"]]){
      const d=B[k]-A[k];
      if(Math.abs(d)<1e-8){if(A[k]<r[mn]||A[k]>r[mx])return false}
      else{const a=(r[mn]-A[k])/d,b=(r[mx]-A[k])/d;lo=Math.max(lo,Math.min(a,b));hi=Math.min(hi,Math.max(a,b));if(lo>hi)return false}
    }
    return true;
  };
  const bx=(W-1)*4,bz=(H-1)*4;
  const raySamples=[["W",1,bz/2,-1,bz/2,1],["E",bx-1,bz/2,bx+1,bz/2,1],["S",bx/2,1,bx/2,-1,1],["N",bx/2,bz-1,bx/2,bz+1,1],["SW",1,1,1,-1,6],["SE",bx-1,1,bx-1,-1,6],["NW",1,bz-1,1,bz+1,6],["NE",bx-1,bz-1,bx-1,bz+1,6]];
  for(const [label,x,z,X,Z,kind]of raySamples)assert("SOURCE_COLLIDER_RAY_"+label,boxes.some(b=>b.kind===kind&&intersect({x,z},{x:X,z:Z},b)));
  const measured=[];
  assert("GATE_FOUR_PROGRESSIVE",gates.length===4&&gates.map(g=>g.z).join(",")==="3,6,9,12"&&gates.map(g=>g.openCells).join(",")==="7,5,4,3");
  for(const g of gates){
    const row=objects.filter(i=>i.kind===1&&i.z===g.z&&i.x>=1&&i.x<W-1);
    const correct=row.length===16-g.openCells&&row.every(i=>i.x<g.opening[0]||i.x>g.opening[1]);
    assert(g.id+"_WALL_PATTERN",correct);
    assert(g.id+"_OPEN_CELLS",g.opening[1]-g.opening[0]+1===g.openCells&&Array.from({length:g.openCells},(_,i)=>g.opening[0]+i).every(x=>floor.has(key(x,g.z))&&!occupied.has(key(x,g.z))));
    const horizontal=boxes.filter(b=>b.kind===1&&b.z0<=4*g.z&&b.z1>=4*g.z);
    const left=Math.max(...horizontal.filter(b=>b.x1<36).map(b=>b.x1));
    const right=Math.min(...horizontal.filter(b=>b.x0>36).map(b=>b.x0));
    const gap=right-left,clearance=Math.min(36-left,right-36);
    measured.push({id:g.id,z:g.z,openCells:g.openCells,sourceOpeningMeters:gap,centerClearanceMeters:clearance});
    assert(g.id+"_PHYSICAL_OPENING_WIDTH",Number.isFinite(gap)&&Math.abs(gap-g.physicalOpeningMeters)<1e-8);
    assert(g.id+"_SOURCE_CENTER_MARGIN",Number.isFinite(clearance)&&Math.abs(clearance-g.centerClearanceMeters)<1e-8);
  }
  const path=manifest?.referencePath||[];
  assert("REFERENCE_ELEVEN_MOVES",path.length===12&&manifest.referenceSteps===11);
  assert("REFERENCE_START_TO_FINISH",key(...path[0])==="9,2"&&key(...path.at(-1))==="9,13");
  assert("REFERENCE_4_NEIGHBOR",path.every(([x,z],i)=>i===0||Math.abs(x-path[i-1][0])+Math.abs(z-path[i-1][1])===1));
  assert("REFERENCE_FREE_CELLS",path.every(([x,z])=>floor.has(key(x,z))&&!occupied.has(key(x,z))));
  let clearance=Infinity;
  for(let i=1;i<path.length;i++){
    const [a,b]=[path[i-1],path[i]];
    for(let t=0;t<=16;t++){
      const x=(a[0]+(b[0]-a[0])*t/16)*4,z=(a[1]+(b[1]-a[1])*t/16)*4;
      for(const r of boxes)clearance=Math.min(clearance,dist(x,z,r));
    }
  }
  clearance=Number.isFinite(clearance)?Math.round(clearance*1000)/1000:null;
  assert("SOURCE_SWEPT_CENTERLINE_SIX_METER_MARGIN",clearance!==null&&clearance>=6-1e-8&&Math.abs(clearance-manifest.sourceAabbMinDistanceMeters)<1e-8);
  const radiusCases=[{radius:2.5,expect:true},{radius:5.5,expect:true},{radius:6.5,expect:false}];
  const sweep=radius=>({radius,gateResults:measured.map(g=>({id:g.id,pass:radius<=g.centerClearanceMeters,marginMeters:Math.round((g.centerClearanceMeters-radius)*1000)/1000})),globalPass:clearance!==null&&radius<=clearance});
  for(const rc of radiusCases)assert("RADIUS_PROXY_"+rc.radius,sweep(rc.radius).globalPass===rc.expect);
  return {ok:errors.length===0,failures:errors,checks:tests,stats:{items:items.length,measurements:measured,referenceSteps:path.length-1,minSourceClearanceMeters:clearance,radiusCases:radiusCases.map(c=>sweep(c.radius))}};
}
