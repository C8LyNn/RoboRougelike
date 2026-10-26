// CAMERA001 R1 independent deterministic math surrogate; mirrors framing equations.
// Node.js: node Tools/B01/CAMERA001/geometry_probe.js
// Does NOT compile or execute Unity/C#; native QA is separate.

function rayBox(origin,dir,centre,extents){
  let near=-Infinity,far=Infinity;for(let i=0;i<3;i++){
    const l=centre[i]-extents[i],u=centre[i]+extents[i];
    if(Math.abs(dir[i])<1e-10){if(origin[i]<l||origin[i]>u)return false;continue}
    let t1=(l-origin[i])/dir[i],t2=(u-origin[i])/dir[i];
    if(t1>t2){const t=t1;t1=t2;t2=t;}
    near=Math.max(near,t1);far=Math.min(far,t2);if(near>far)return false;
  }return far>=Math.max(0,near);
}

function solve(m,yawDeg,pitchDeg,requested,aspect){
 const yaw=yawDeg*Math.PI/180,pitch=pitchDeg*Math.PI/180;
 const forward=[Math.sin(yaw),0,Math.cos(yaw)];
 const behind=[-Math.sin(yaw)*Math.cos(pitch),Math.sin(pitch),-Math.cos(yaw)*Math.cos(pitch)];
 const e=m.extents,c=m.center,forwardRadius=Math.abs(forward[0])*e[0]+Math.abs(forward[2])*e[2],
 horizontalRadius=Math.abs(forward[2])*e[0]+Math.abs(forward[0])*e[2];
 const k=Math.tan(62*Math.PI/180*.5*.78);
 const fit=Math.max(3,(e[1]+.65)/k,(horizontalRadius+.65)/(k*aspect));
 const dist=Math.max(3,Math.min(18,Math.max(requested,fit)));
 const neg=Math.max(0,-behind[1]*dist);
 const aim=[c[0]+forward[0]*(forwardRadius+1.75),c[1]+e[1]+.65+neg,c[2]+forward[2]*(forwardRadius+1.75)];
 const camera=aim.map((v,i)=>v+behind[i]*dist);
 const direction=behind.map(x=>-x);
 return{ownOcclusion:rayBox(camera,direction,c,e),aim,camera,dist,fit,saturated:fit>18};
}

function run(){
 const body=[{center:[0,1.4,0],extents:[1.05,2,1.6]},{center:[0,3,0],extents:[2.25,5,2.9]}];
 let sweep=0,bad=0;
 for(const hull of body)for(const aspect of[16/9,4/3,21/9])
 for(const pitch of[-8,-2,0,18,35,55,70])
 for(let step=0;step<20;step++){
  const y=18*step,r=solve(hull,y,pitch,3+step*.5,aspect);sweep++;
  if(r.ownOcclusion || r.camera[1]<hull.center[1]+hull.extents[1]+.60 || r.dist<3 ||r.dist>18)bad++;
 }
 const small=solve({center:[0,1,0],extents:[1,1,1.5]},15,18,3,16/9);
 const large=solve({center:[0,3,0],extents:[2.5,5.5,3.5]},15,18,3,16/9);
 const enormous=solve({center:[0,10,0],extents:[10,15,15]},180,0,3,1);
 const ratioA=solve({center:[9,1,19],extents:[1,2.5,2]},210,20,8,1280/720);
 const ratioB=solve({center:[9,1,19],extents:[1,2.5,2]},210,20,8,1920/1080);
 const checks={sweep_840_exact:sweep===840,sweep_840_clear:bad===0,large_requires_more_fit:large.fit>small.fit,saturated_explicit:enormous.saturated&&enormous.dist===18,resolution_invariant:ratioA.camera.every((x,i)=>Math.abs(x-ratioB.camera[i])<1e-9)};
 const pass=Object.values(checks).every(Boolean);
 return {method:"JS_OFFLINE_ANALYTIC_PORT_NOT_UNITY",checks,sweep,bad,pass,unity_compile:"NOT_RUN",native_test:"NOT_RUN"};
}

const result = run();

console.log(JSON.stringify(result, null, 2));

if (!result.pass) process.exitCode = 1;
