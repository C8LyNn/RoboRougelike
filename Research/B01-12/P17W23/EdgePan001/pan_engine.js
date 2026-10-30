/* B01-12 EDGE-PAN-001/R1: independent deterministic viewport autopan engine. */
(function(root,factory){const api=factory();if(typeof module==='object'&&module.exports)module.exports=api;else root.EdgePan=api;})(typeof globalThis!=='undefined'?globalThis:this,function(){
'use strict';
const WIDTH=1440, HEIGHT=940, EDGE=72, MAX_SPEED=760, MAX_DT=50;
const has=(v,k)=>Object.prototype.hasOwnProperty.call(v,k);
const plain=v=>v!==null&&typeof v==='object'&&!Array.isArray(v)&&Object.getPrototypeOf(v)===Object.prototype;
const num=v=>typeof v==='number'&&Number.isFinite(v);
const keys=(v,k)=>plain(v)&&Object.keys(v).length===k.length&&k.every(x=>has(v,x));
function valid(s){return keys(s,['version','canvas','view','scroll','drag','revision'])&&s.version===1&&keys(s.canvas,['width','height'])&&keys(s.view,['width','height'])&&keys(s.scroll,['x','y'])&&keys(s.drag,['active','pointerId','x','y','enabled'])&&num(s.canvas.width)&&num(s.canvas.height)&&s.canvas.width>=320&&s.canvas.width<=8192&&s.canvas.height>=320&&s.canvas.height<=8192&&num(s.view.width)&&num(s.view.height)&&s.view.width>=160&&s.view.height>=160&&s.view.width<=s.canvas.width&&s.view.height<=s.canvas.height&&num(s.scroll.x)&&num(s.scroll.y)&&s.scroll.x>=0&&s.scroll.y>=0&&s.scroll.x<=s.canvas.width-s.view.width&&s.scroll.y<=s.canvas.height-s.view.height&&typeof s.drag.active==='boolean'&&typeof s.drag.enabled==='boolean'&&(s.drag.pointerId===null||Number.isSafeInteger(s.drag.pointerId)&&s.drag.pointerId>=0)&&num(s.drag.x)&&num(s.drag.y)&&(!s.drag.active||s.drag.pointerId!==null)&&(!s.drag.active||s.drag.x>=0&&s.drag.x<=s.view.width&&s.drag.y>=0&&s.drag.y<=s.view.height)&&Number.isSafeInteger(s.revision)&&s.revision>=0&&s.revision<=1000000;}
function initial(viewWidth=720,viewHeight=460){if(!num(viewWidth)||!num(viewHeight)||viewWidth<160||viewWidth>WIDTH||viewHeight<160||viewHeight>HEIGHT)throw Error('invalid view');return {version:1,canvas:{width:WIDTH,height:HEIGHT},view:{width:viewWidth,height:viewHeight},scroll:{x:0,y:0},drag:{active:false,pointerId:null,x:0,y:0,enabled:true},revision:0};}
function clone(s){return JSON.parse(JSON.stringify(s));}
function edgeVelocity(pos,size){if(pos<EDGE)return -MAX_SPEED*Math.pow((EDGE-pos)/EDGE,2);if(pos>size-EDGE)return MAX_SPEED*Math.pow((pos-(size-EDGE))/EDGE,2);return 0;}
function result(state,ok,reason='OK',delta={x:0,y:0}){return {ok,reason,state,delta};}
function apply(state,event){if(!valid(state)||!plain(event)||typeof event.type!=='string')return result(state,false,'SCHEMA');if(state.revision>=1000000)return result(state,false,'REVISION');let next=clone(state), d={x:0,y:0};
const equal=(names)=>keys(event,names);
switch(event.type){
case 'START': if(!equal(['type','pointerId','x','y'])||state.drag.active||!Number.isSafeInteger(event.pointerId)||event.pointerId<0||!num(event.x)||!num(event.y)||event.x<0||event.x>state.view.width||event.y<0||event.y>state.view.height)return result(state,false,'START');next.drag={...next.drag,active:true,pointerId:event.pointerId,x:event.x,y:event.y};break;
case 'MOVE': if(!equal(['type','pointerId','x','y'])||!state.drag.active||event.pointerId!==state.drag.pointerId||!num(event.x)||!num(event.y))return result(state,false,'POINTER');next.drag.x=Math.max(0,Math.min(state.view.width,event.x));next.drag.y=Math.max(0,Math.min(state.view.height,event.y));break;
case 'TICK': if(!equal(['type','dt'])||!num(event.dt)||event.dt<0||event.dt>MAX_DT)return result(state,false,'DT');if(state.drag.active&&state.drag.enabled){const dx=edgeVelocity(state.drag.x,state.view.width)*event.dt/1000,dy=edgeVelocity(state.drag.y,state.view.height)*event.dt/1000;next.scroll.x=Math.max(0,Math.min(state.canvas.width-state.view.width,state.scroll.x+dx));next.scroll.y=Math.max(0,Math.min(state.canvas.height-state.view.height,state.scroll.y+dy));d={x:next.scroll.x-state.scroll.x,y:next.scroll.y-state.scroll.y};}break;
case 'END': if(!equal(['type','pointerId'])||!state.drag.active||event.pointerId!==state.drag.pointerId)return result(state,false,'END');next.drag={...next.drag,active:false,pointerId:null};break;
case 'CANCEL': if(!equal(['type']))return result(state,false,'SCHEMA');next.drag={...next.drag,active:false,pointerId:null};break;
case 'ENABLE': if(!equal(['type','value'])||typeof event.value!=='boolean')return result(state,false,'ENABLE');next.drag.enabled=event.value;if(!event.value){next.drag.active=false;next.drag.pointerId=null;}break;
case 'VIEW': if(!equal(['type','width','height'])||state.drag.active||!num(event.width)||!num(event.height)||event.width<160||event.height<160||event.width>state.canvas.width||event.height>state.canvas.height)return result(state,false,'VIEW');next.view={width:event.width,height:event.height};next.scroll.x=Math.min(state.scroll.x,state.canvas.width-event.width);next.scroll.y=Math.min(state.scroll.y,state.canvas.height-event.height);break;
case 'SCROLL': if(!equal(['type','x','y'])||state.drag.active||!num(event.x)||!num(event.y)||event.x<0||event.y<0||event.x>state.canvas.width-state.view.width||event.y>state.canvas.height-state.view.height)return result(state,false,'SCROLL');next.scroll={x:event.x,y:event.y};break;
case 'RESET': if(!equal(['type']))return result(state,false,'SCHEMA');next=initial(state.view.width,state.view.height);next.drag.enabled=state.drag.enabled;break;
default:return result(state,false,'UNKNOWN');}
next.revision=state.revision+1;if(!valid(next))return result(state,false,'INVARIANT');return result(next,true,'OK',d);}
return Object.freeze({initial,apply,valid,edgeVelocity,config:Object.freeze({EDGE,MAX_SPEED,MAX_DT})});
});
