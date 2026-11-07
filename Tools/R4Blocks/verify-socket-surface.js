'use strict';
const fs=require('node:fs');
const path=require('node:path');
const {buildBundle,verifyBundleAgainstSource}=require('./export-unity-colliders.js');

const dot=(a,b)=>a[0]*b[0]+a[1]*b[1]+a[2]*b[2];
const sub=(a,b)=>a.map((n,i)=>n-b[i]);
const cross=(a,b)=>[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]];
function sampleSocketSurface(socket,pieces) {
  const {cell,face,offset,half}=socket;
  if(!Array.isArray(cell)||cell.length!==3||cell.some(n=>!Number.isInteger(n))||
     !Array.isArray(face)||face.length!==3||face.some(n=>!Number.isInteger(n))||face.reduce((s,n)=>s+Math.abs(n),0)!==1)
    throw new Error('Invalid socket cell or face');
  if(!Array.isArray(offset)||offset.length!==2||offset.some(n=>!Number.isFinite(n))||
     !Array.isArray(half)||half.length!==2||half.some(n=>!Number.isFinite(n)||n<=0))throw new Error('Invalid socket patch');
  const u=face[0]!==0?[0,1,0]:[1,0,0],v=cross(face,u);
  const points=[];
  for(const a of [-1,0,1])for(const b of [-1,0,1])points.push(cell.map((n,i)=>
    n*.55+face[i]*.26+u[i]*(offset[0]+a*half[0])*.55+v[i]*(offset[1]+b*half[1])*.55));
  const triangles=[];
  for(const piece of pieces) {
    if(!Array.isArray(piece.positions)||piece.positions.length%3||!Array.isArray(piece.indices)||piece.indices.length%3)
      throw new Error('Invalid mesh arrays');
    for(let i=0;i<piece.indices.length;i+=3) {
      const indices=piece.indices.slice(i,i+3);
      if(indices.some(n=>!Number.isInteger(n)||n<0||n*3+2>=piece.positions.length))throw new Error('Invalid mesh index');
      const triangle=indices.map(n=>piece.positions.slice(n*3,n*3+3));
      const plane=cell.map((n,j)=>n*.55+face[j]*.26);
      if(triangle.every(point=>Math.abs(dot(sub(point,plane),face))<=.0001))triangles.push(triangle);
    }
  }
  function contains(point,triangle) {
    const [a,b,c]=triangle,ab=sub(b,a),ac=sub(c,a),ap=sub(point,a);
    const d00=dot(ab,ab),d01=dot(ab,ac),d11=dot(ac,ac),d20=dot(ap,ab),d21=dot(ap,ac);
    const denom=d00*d11-d01*d01;if(Math.abs(denom)<1e-16)return false;
    const x=(d11*d20-d01*d21)/denom,y=(d00*d21-d01*d20)/denom;
    return x>=-1e-5 && y>=-1e-5 && x+y<=1+1e-5;
  }
  const uncovered=points.filter(point=>!triangles.some(t=>contains(point,t)));
  function segmentDistance(point,a,b) {
    const ab=sub(b,a),denom=dot(ab,ab);
    const t=denom>0?Math.max(0,Math.min(1,dot(sub(point,a),ab)/denom)):0;
    return Math.hypot(...sub(point,a.map((n,i)=>n+ab[i]*t)));
  }
  const examples=uncovered.map(point=>({point,nearestTriangleEdgeMeters:triangles.length?
    Math.min(...triangles.flatMap(t=>[segmentDistance(point,t[0],t[1]),segmentDistance(point,t[1],t[2]),segmentDistance(point,t[2],t[0])])):null}));
  return {covered:uncovered.length===0,uncoveredSamples:uncovered.length,totalSamples:points.length,examples};
}

function verifySockets(root,bundle) {
  verifyBundleAgainstSource(bundle,root);
  const catalog=JSON.parse(fs.readFileSync(path.join(root,'integration','R4_BLOCKS_UNITY.json'),'utf8'));
  const shapes=new Map(bundle.shapes.map(shape=>[shape.id,shape]));
  const failures=[];let count=0;
  for(const block of catalog.blocks)for(let i=0;i<block.sockets.length;i++) {
    const result=sampleSocketSurface(block.sockets[i],shapes.get(block.id).pieces);count++;
    if(!result.covered)failures.push({id:block.id,socket:i,kind:block.sockets[i].kind,...result});
  }
  return {count,failures};
}

if(require.main===module) {
  try {
    if(process.argv.length!==3)throw new Error('Usage: node verify-socket-surface.js PATH_TO_R4Blocks');
    const result=verifySockets(process.argv[2],buildBundle(process.argv[2]));
    console.log(`Socket samples: ${result.count-result.failures.length}/${result.count} patches covered; ${result.failures.length} patches have uncovered samples.`);
    for(const item of result.failures) {
      const example=item.examples[0],point=example.point.map(n=>n.toFixed(4)).join(',');
      const gap=example.nearestTriangleEdgeMeters==null?'no coplanar face':`${(example.nearestTriangleEdgeMeters*1000).toFixed(3)} mm`;
      console.log(`${item.id} socket ${item.socket} (${item.kind}): ${item.uncoveredSamples}/${item.totalSamples} uncovered; example=(${point}) m, nearest edge=${gap}`);
    }
    if(result.failures.length)process.exitCode=1;
  } catch(error) {console.error(error.message);process.exitCode=1;}
}
module.exports={sampleSocketSurface,verifySockets};
