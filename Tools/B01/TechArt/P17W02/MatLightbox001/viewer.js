"use strict";
/* MAT-LIGHTBOX-001, B01-30: original-source PBR maps on an independent WebGL2 shaderball.
No gameplay or native Unity G1 claims. No external graphics/network/library dependency. */
(() => {
const canvas=document.querySelector("#viewer"), status=document.querySelector("#status"),
  mode=document.querySelector("#renderState");
const controls={family:document.querySelector("#family"),light:document.querySelector("#light"),
  normal:document.querySelector("#normalStrength"),uv:document.querySelector("#uvScale"),
  exposure:document.querySelector("#exposure")};
const gl=canvas.getContext("webgl2",{antialias:true,alpha:false,preserveDrawingBuffer:true});
const state={ready:false,profile:"tire_rubber",loaded:[],error:null,frames:0,yaw:0.15,pitch:-0.13};
window.lightboxQA=state;
if(!gl){state.error="WEBGL2_UNAVAILABLE";status.textContent=state.error;mode.textContent="BLOCKED: WEBGL2";return;}
const vs=[
"#version 300 es",
"precision highp float;",
"layout(location=0) in vec3 position;",
"layout(location=1) in vec3 normal;",
"layout(location=2) in vec2 uv;",
"layout(location=3) in vec3 tangent;",
"uniform float yaw,pitch,aspect;",
"out vec2 fUV;",
"out vec3 fNormal,fTangent,fViewPos;",
"void main(){",
" float cy=cos(yaw),sy=sin(yaw),cx=cos(pitch),sx=sin(pitch);",
" mat3 ry=mat3(cy,0.,-sy, 0.,1.,0., sy,0.,cy);",
" mat3 rx=mat3(1.,0.,0., 0.,cx,sx, 0.,-sx,cx);",
" mat3 rot=rx*ry;",
" vec3 p=rot*position;",
" fNormal=normalize(rot*normal);",
" fTangent=normalize(rot*tangent);",
" fUV=uv;",
" fViewPos=p;",
" gl_Position=vec4(p.x*.78/aspect,p.y*.78,p.z*.09,1.);",
"}"
].join("\n");
const fs=[
"#version 300 es",
"precision highp float;",
"in vec2 fUV;",
"in vec3 fNormal,fTangent,fViewPos;",
"uniform sampler2D texBase,texNormal,texMetal,texAO;",
"uniform float lightAngle,normalStrength,uvScale,exposure;",
"out vec4 finalColor;",
"void main(){",
" vec2 uv=fUV*uvScale;",
" vec3 srgb=texture(texBase,uv).rgb;",
" vec3 base=pow(max(srgb,vec3(.001)),vec3(2.2));",
" vec3 nm=texture(texNormal,uv).rgb*2.-1.;",
" nm.xy*=normalStrength;",
" nm=normalize(nm);",
" vec3 n=normalize(fNormal);",
" vec3 t=normalize(fTangent-n*dot(n,fTangent));",
" vec3 bb=normalize(cross(n,t));",
" n=normalize(t*nm.x+bb*nm.y+n*nm.z);",
" vec4 packed=texture(texMetal,uv);",
" float metal=clamp(packed.r,0.,1.);",
" float smooth=clamp(packed.a,0.,1.);",
" float rough=max(.075,1.-smooth);",
" float ao=texture(texAO,uv).g;",
" float phi=radians(lightAngle);",
" vec3 l=normalize(vec3(sin(phi),.52,cos(phi)));",
" vec3 viewDir=normalize(vec3(0.,0.,2.5)-fViewPos);",
" vec3 halfDir=normalize(l+viewDir);",
" float ndl=max(dot(n,l),0.);",
" float ndh=max(dot(n,halfDir),.0001);",
" float vdh=max(dot(viewDir,halfDir),0.);",
" vec3 F0=mix(vec3(.04),base,metal);",
" vec3 F=F0+(1.-F0)*pow(1.-vdh,5.);",
" float shininess=mix(12.,175.,(1.-rough)*(1.-rough));",
" float spec=pow(ndh,shininess)*(shininess+2.)/8.;",
" vec3 color=base*(.17*ao+.94*ndl*(1.-metal))+F*spec*ndl*1.5;",
" color*=exposure;",
" color=color/(vec3(1.)+color);",
" color=pow(color,vec3(1./2.2));",
" finalColor=vec4(color,1.);",
"}"
].join("\n");
function makeShader(type,text){
 const sh=gl.createShader(type);gl.shaderSource(sh,text);gl.compileShader(sh);
 if(!gl.getShaderParameter(sh,gl.COMPILE_STATUS))throw Error("GLSL_COMPILE:"+gl.getShaderInfoLog(sh));
 return sh;
}
function buildProgram(){
 const p=gl.createProgram();
 gl.attachShader(p,makeShader(gl.VERTEX_SHADER,vs));gl.attachShader(p,makeShader(gl.FRAGMENT_SHADER,fs));
 gl.linkProgram(p);
 if(!gl.getProgramParameter(p,gl.LINK_STATUS))throw Error("GLSL_LINK:"+gl.getProgramInfoLog(p));
 return p;
}
function makeSphere(){
 const data=[],indices=[],rings=48,seg=80;
 for(let y=0;y<=rings;y++)for(let x=0;x<=seg;x++){
  const theta=Math.PI*y/rings,phi=x*2*Math.PI/seg;
  const sn=Math.sin(theta),cs=Math.cos(theta),sp=Math.sin(phi),cp=Math.cos(phi);
  const px=sn*cp,py=cs,pz=sn*sp;
  data.push(px,py,pz,px,py,pz,x/seg,1-y/rings,-sp,0,cp);
 }
 for(let y=0;y<rings;y++)for(let x=0;x<seg;x++){
  const q=y*(seg+1)+x;
  indices.push(q,q+seg+1,q+1,q+1,q+seg+1,q+seg+2);
 }
 const vao=gl.createVertexArray();gl.bindVertexArray(vao);
 const v=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,v);
 gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(data),gl.STATIC_DRAW);
 for(const [index,n,offset] of [[0,3,0],[1,3,3],[2,2,6],[3,3,8]]){
  gl.vertexAttribPointer(index,n,gl.FLOAT,false,44,offset*4);gl.enableVertexAttribArray(index);
 }
 const element=gl.createBuffer();gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,element);
 gl.bufferData(gl.ELEMENT_ARRAY_BUFFER,new Uint16Array(indices),gl.STATIC_DRAW);
 gl.bindVertexArray(null);
 return {vao:vao,count:indices.length};
}
let program,mesh;
const families=["tire_rubber","brushed_arm_alloy"],
 maps=["basecolor","normal","metallic_smoothness","occlusion"], textures={};
function loadImage(family,map){
 const path="./textures/"+family+"/"+family+"_"+map+".png";
 return new Promise((resolve,reject)=>{
  const im=new Image();
  im.onload=()=>im.naturalWidth===512&&im.naturalHeight===512?resolve(im):reject(Error("WRONG_SIZE:"+path));
  im.onerror=()=>reject(Error("SOURCE_PNG_MISSING:"+path));
  im.src=path;
 });
}
function upload(img){
 const tex=gl.createTexture();gl.bindTexture(gl.TEXTURE_2D,tex);
 gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,true);
 gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA8,gl.RGBA,gl.UNSIGNED_BYTE,img);
 gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.REPEAT);
 gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.REPEAT);
 gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR_MIPMAP_LINEAR);
 gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);
 gl.generateMipmap(gl.TEXTURE_2D);
 return tex;
}
function render(){
 if(!state.ready)return;
 const rect=canvas.getBoundingClientRect();
 const w=Math.max(256,Math.round(rect.width*devicePixelRatio)),h=Math.max(256,Math.round(rect.height*devicePixelRatio));
 if(canvas.width!==w||canvas.height!==h){canvas.width=w;canvas.height=h;}
 gl.viewport(0,0,w,h);gl.clearColor(.022,.046,.079,1);
 gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);
 gl.enable(gl.DEPTH_TEST);gl.depthFunc(gl.LEQUAL);gl.useProgram(program);
 gl.bindVertexArray(mesh.vao);
 for(let i=0;i<4;i++){
  gl.activeTexture(gl.TEXTURE0+i);gl.bindTexture(gl.TEXTURE_2D,textures[state.profile][maps[i]]);
  gl.uniform1i(gl.getUniformLocation(program,["texBase","texNormal","texMetal","texAO"][i]),i);
 }
 const set=(name,value)=>gl.uniform1f(gl.getUniformLocation(program,name),value);
 set("yaw",state.yaw);set("pitch",state.pitch);set("aspect",w/h);
 set("lightAngle",+controls.light.value);set("normalStrength",+controls.normal.value);
 set("uvScale",+controls.uv.value);set("exposure",+controls.exposure.value);
 gl.drawElements(gl.TRIANGLES,mesh.count,gl.UNSIGNED_SHORT,0);
 if(gl.getError()!==gl.NO_ERROR)throw Error("WEBGL_DRAW_ERROR");
 state.frames++;
 mode.textContent="WEBGL2 DRAW PASS · "+state.frames+" frames · 8 original PNGs";
 document.querySelector("#lightValue").textContent=controls.light.value+"°";
 document.querySelector("#normalValue").textContent=(+controls.normal.value).toFixed(1);
 document.querySelector("#uvValue").textContent=(+controls.uv.value).toFixed(1)+"×";
 document.querySelector("#exposureValue").textContent=(+controls.exposure.value).toFixed(1);
}
function safeRender(){
 try{render();}catch(e){state.error=String(e.message);status.textContent=state.error;mode.textContent="WEBGL ERROR";throw e;}
}
async function init(){
 try{
  program=buildProgram();mesh=makeSphere();
  for(const family of families){
   textures[family]={};
   const imgs=await Promise.all(maps.map(m=>loadImage(family,m)));
   for(let i=0;i<4;i++){textures[family][maps[i]]=upload(imgs[i]);state.loaded.push(family+"/"+maps[i]);}
  }
  if(state.loaded.length!==8)throw Error("MISSING_MAPS");
  state.ready=true;status.textContent="8 oryginalnych tekstur · WebGL2";
  safeRender();document.querySelector("#reset").disabled=false;
 }catch(e){state.error=String(e.message);status.textContent=state.error;mode.textContent="BLOCKED: "+state.error;}
}
controls.family.addEventListener("change",()=>{state.profile=controls.family.value;safeRender();});
for(const k of ["light","normal","uv","exposure"])controls[k].addEventListener("input",safeRender);
document.querySelector("#reset").addEventListener("click",()=>{
 state.yaw=.15;state.pitch=-.13;controls.light.value=35;controls.normal.value=1;
 controls.uv.value=2;controls.exposure.value=1;safeRender();
});
let dragging=false,oldX=0,oldY=0;
canvas.addEventListener("pointerdown",e=>{dragging=true;oldX=e.clientX;oldY=e.clientY;canvas.setPointerCapture(e.pointerId);});
canvas.addEventListener("pointermove",e=>{
 if(!dragging)return;state.yaw+=(e.clientX-oldX)*.009;
 state.pitch=Math.max(-1.35,Math.min(1.35,state.pitch+(e.clientY-oldY)*.009));
 oldX=e.clientX;oldY=e.clientY;safeRender();
});
canvas.addEventListener("pointerup",()=>dragging=false);
window.addEventListener("resize",()=>{if(state.ready)safeRender();});
document.querySelector("#reset").disabled=true;
init();
})();