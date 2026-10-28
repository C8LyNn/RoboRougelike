// Inlined with core.mjs and real JSON blueprints by build_viewer.py.
// This is a 2D arcade preview, NOT Unity Rigidbody/PhysX or playable game integration.
const maps=JSON.parse(document.getElementById("maps-data").textContent);
const byKey=new Map(maps.map(m=>[m.variantId+"@"+m.seed,m]));
const course=document.getElementById("course");
const profile=document.getElementById("profile");
const auto=document.getElementById("auto");
const pause=document.getElementById("pause");
const reset=document.getElementById("reset");
const status=document.getElementById("status");
const caption=document.getElementById("caption");
const ctx=document.getElementById("arena").getContext("2d");
const keys=new Set();
for(const m of maps){
 const op=document.createElement("option");
 op.value=m.variantId+"@"+m.seed;
 op.textContent=m.variantId.replace("P17-COURSE-","")+" — seed "+m.seed;
 course.append(op);
}
let selected,player;
function restart(){
 selected=byKey.get(course.value);
 player=createState(selected,profile.value);
 // Reset the fixed-step accumulator: no carryover from the previous course.
 acc=0;
 last=performance.now();
 pause.textContent="Pauza";
 status.textContent="START";
}
course.addEventListener("change",restart);
profile.addEventListener("change",restart);
reset.addEventListener("click",restart);
pause.addEventListener("click",()=>{
 player.paused=!player.paused;
 pause.textContent=player.paused?"Wznów":"Pauza";
});
window.addEventListener("keydown",e=>{
 if(["ArrowUp","ArrowDown","ArrowLeft","ArrowRight"," "].includes(e.key))e.preventDefault();
 if(e.key==="r"||e.key==="R"){restart();return;}
 if(e.key===" "){if(!e.repeat)pause.click();return;}
 keys.add(e.key.toLowerCase());
});
window.addEventListener("keyup",e=>keys.delete(e.key.toLowerCase()));
window.addEventListener("blur",()=>keys.clear());
function manual(){
 let x=0,z=0;
 if(keys.has("a")||keys.has("arrowleft"))x--;
 if(keys.has("d")||keys.has("arrowright"))x++;
 if(keys.has("w")||keys.has("arrowup"))z++;
 if(keys.has("s")||keys.has("arrowdown"))z--;
 return{x,z};
}
const can=ctx.canvas;
function draw(){
 const bounds=selected.bounds;
 const minX=bounds.center.x-bounds.size.x/2;
 const maxZ=bounds.center.z+bounds.size.z/2;
 const scale=Math.min((can.width-70)/bounds.size.x,(can.height-90)/bounds.size.z);
 const tx=x=>can.width/2+(x-bounds.center.x)*scale;
 const tz=z=>can.height/2+(bounds.center.z-z)*scale;
 ctx.fillStyle="#0a1621";
 ctx.fillRect(0,0,can.width,can.height);
 // coordinate grid drawn BEFORE the original source geometry
 ctx.strokeStyle="#203648";ctx.lineWidth=1;
 for(let x=Math.ceil(minX/4)*4;x<minX+bounds.size.x;x+=4){
  ctx.beginPath();ctx.moveTo(tx(x),0);ctx.lineTo(tx(x),can.height);ctx.stroke();
 }
 for(let z=Math.floor(maxZ/4)*4;z>maxZ-bounds.size.z;z-=4){
  ctx.beginPath();ctx.moveTo(0,tz(z));ctx.lineTo(can.width,tz(z));ctx.stroke();
 }
 const palette={Floor:"#2c4354",Boundary:"#879cab",Ramp:"#629c80",Obstacle:"#bf8052",Cover:"#c4a06a"};
 for(const e of selected.elements){
  ctx.fillStyle=palette[e.kind]||"#df9d9d";
  ctx.fillRect(tx(e.position.x-e.size.x/2),tz(e.position.z+e.size.z/2),
      e.size.x*scale,e.size.z*scale);
 }
 const waypoints=[selected.start,...selected.checkpoints,selected.finish];
 ctx.beginPath();ctx.setLineDash([5,4]);ctx.strokeStyle="#8fdbff";ctx.lineWidth=2;
 for(let i=0;i<waypoints.length;i++){
  const w=waypoints[i];i===0?ctx.moveTo(tx(w.x),tz(w.z)):ctx.lineTo(tx(w.x),tz(w.z));
 }
 ctx.stroke();ctx.setLineDash([]);
 for(let i=0;i<waypoints.length;i++){
  const p=waypoints[i],done=i>0&&i<=player.cp;
  ctx.beginPath();ctx.arc(tx(p.x),tz(p.z),i===0?5:7,0,Math.PI*2);
  ctx.fillStyle=done?"#81e49f":i===0?"#69c9ff":"#edcf82";ctx.fill();
  ctx.font="12px system-ui";ctx.fillStyle="#f0f6fa";
  ctx.fillText(i===0?"S":i===waypoints.length-1?"F":"CP"+i,tx(p.x)+11,tz(p.z)-7);
 }
 const p=PROFILES[player.profileName];
 ctx.fillStyle="#f4f8fb";ctx.strokeStyle="#66cefa";ctx.lineWidth=2;
 ctx.fillRect(tx(player.x-p.width/2),tz(player.z+p.length/2),
              p.width*scale,p.length*scale);
 ctx.strokeRect(tx(player.x-p.width/2),tz(player.z+p.length/2),
              p.width*scale,p.length*scale);
 ctx.font="14px system-ui";ctx.fillStyle="#eff4ff";
 ctx.fillText("N",tx(player.x)-5,tz(player.z+p.length/2)-10);
}
let last=performance.now(),acc=0;
function frame(now){
 const dt=Math.min(.1,Math.max(0,(now-last)/1000));last=now;acc+=dt;
 let frames=0;
 while(acc>=.05&&frames<3){
  player=tick(selected,player,auto.checked?assist(selected,player):manual(),.05);
  acc-=.05;frames++;
 }
 caption.textContent="Profil "+player.profileName+" · geometry "+selected.geometryHash.slice(0,16)+"… · 2D source-only";
 status.textContent=(player.done?"META!":player.paused?"PAUZA":"CHECKPOINT "+player.cp+"/"+(selected.checkpoints.length+1))+
  " · "+player.elapsed.toFixed(1)+" s · kolizje "+player.collisions+
  " · pozycja X "+player.x.toFixed(1)+" / Z "+player.z.toFixed(1);
 draw();
 requestAnimationFrame(frame);
}
restart();
requestAnimationFrame(frame);
