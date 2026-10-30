/* B01-12 TOOLTIP-ANCHOR-001 browser adapter: no game integration, no persistence. */
(()=>{'use strict';
const E=globalThis.B01TooltipAnchor;
if(!E)throw Error('Missing B01TooltipAnchor engine');
const $=id=>document.getElementById(id);
const records=[
{id:'hp',title:'Punkty wytrzymałości',detail:'Stan HP elementu. Utrata punktów może spowodować jego odłączenie.',x:9,y:10,tag:'HP / 420'},
{id:'mass',title:'Masa pojazdu',detail:'Wartość poglądowa. Masa wpływa na przyspieszanie, stabilność i hamowanie.',x:41,y:10,tag:'890 kg'},
{id:'energy',title:'Energia',detail:'Rezerwa na napęd i wyposażenie. Podgląd nie modyfikuje ekonomii.',x:73,y:10,tag:'75 / 100'},
{id:'heat',title:'Temperatura',detail:'Ilustracyjne obciążenie termiczne podsystemu.',x:9,y:39,tag:'62 °C'},
{id:'speed',title:'Prędkość',detail:'Szybkość w aktualnym układzie napędowym.',x:73,y:39,tag:'42 km/h'},
{id:'repair',title:'Naprawa',detail:'Liczba części wymagających obsługi po walce.',x:9,y:70,tag:'3 części'},
{id:'armor',title:'Pancerz',detail:'Warstwa ochronna modułu. Wartości wyłącznie demonstracyjne.',x:41,y:70,tag:'18 mm'},
{id:'weapon',title:'Uzbrojenie',detail:'Podgląd statystyk broni bez zmiany aktywnej konfiguracji.',x:73,y:70,tag:'2 sloty'}];
const byId=new Map(records.map(r=>[r.id,r]));
let state=E.EMPTY;
const stage=$('stage'), tip=$('tooltip'), nodes=$('modules');
const setpct=(node,rect,viewport)=>{node.style.left=(rect.x/viewport.width*100)+'%';node.style.top=(rect.y/viewport.height*100)+'%';node.style.width=(rect.width/viewport.width*100)+'%';node.style.height=(rect.height/viewport.height*100)+'%';};
function viewport(){const [width,height]=$('view').value.split('x').map(Number);return {width,height};}
function moduleRect(r,v){const w=v.width*.18,h=v.height*.15;return {x:v.width*r.x/100,y:v.height*r.y/100,width:w,height:h};}
function dispatch(event){const r=E.reduce(state,event);if(!r.ok){$('status').textContent='Odrzucono zdarzenie: '+r.code;return false;}state=r.state;render();return true;}
function render(){const v=viewport();stage.style.aspectRatio=v.width+' / '+v.height;$('viewport-label').textContent=`${v.width} × ${v.height}`;
 const pad={top:Math.min(v.height*.06,30),bottom:Math.min(v.height*.06,30),left:Math.min(v.width*.045,28),right:Math.min(v.width*.045,28)};
 setpct($('safe'),{x:pad.left,y:pad.top,width:v.width-pad.left-pad.right,height:v.height-pad.top-pad.bottom},v);
 const exc={x:v.width*.37,y:v.height*.37,width:v.width*.25,height:v.height*.26};
 $('exclusion').hidden=$('obstacle').value==='off';if(!$('exclusion').hidden)setpct($('exclusion'),exc,v);
 for(const r of records){const button=$('mod-'+r.id);setpct(button,moduleRect(r,v),v);button.setAttribute('aria-pressed',String(state.open&&state.id===r.id&&state.pinned));}
 tip.setAttribute('aria-hidden','true');$('pin').disabled=!state.open;
 if(!state.open){$('status').textContent='Wybierz element, aby wyświetlić podpowiedź.';$('diagnostic').textContent='Tryb: brak aktywnej podpowiedzi';return;}
 const rec=byId.get(state.id);const anchor=moduleRect(rec,v);const dimensions={width:Math.min(v.width*.30,230),height:Math.min(v.height*.20,120)};
 const solved=E.solve({viewport:v,anchor,tooltip:dimensions,preferred:$('preferred').value,gap:Math.min(14, v.width*.018),insets:pad,exclusions:$('obstacle').value==='on'?[exc]:[]});
 if(!solved.ok){$('status').textContent='Podpowiedź ukryta — '+solved.code;$('diagnostic').textContent='Fail-closed: '+solved.code;return;}
 setpct(tip,solved.rect,v);$('tip-title').textContent=rec.title;$('tip-body').textContent=rec.detail;$('tip-side').textContent='STRONA: '+solved.side.toUpperCase()+(state.pinned?' · PRZYPIĘTA':'');tip.setAttribute('aria-hidden','false');$('status').textContent=rec.title+(state.pinned?' — przypięta':' — podgląd');$('diagnostic').textContent=`Strona ${solved.side}, pozycja (${Math.round(solved.rect.x)}, ${Math.round(solved.rect.y)}), rewizja ${state.revision}`;
}
for(const rec of records){const b=document.createElement('button');b.type='button';b.className='module';b.id='mod-'+rec.id;b.dataset.key=rec.id;b.setAttribute('aria-describedby','tooltip');b.setAttribute('aria-pressed','false');const title=document.createElement('strong'),value=document.createElement('small');title.textContent=rec.title;value.textContent=rec.tag;b.append(title,value);b.addEventListener('pointerenter',()=>dispatch({type:'SHOW',id:rec.id}));b.addEventListener('pointerleave',()=>dispatch({type:'HIDE'}));b.addEventListener('focus',()=>dispatch({type:'SHOW',id:rec.id}));b.addEventListener('blur',()=>dispatch({type:'HIDE'}));b.addEventListener('click',()=>dispatch({type:'PIN',id:rec.id}));nodes.appendChild(b);}
$('view').addEventListener('change',render);$('preferred').addEventListener('change',render);$('obstacle').addEventListener('change',render);$('close').addEventListener('click',()=>dispatch({type:'ESCAPE'}));$('pin').addEventListener('click',()=>{if(state.open)dispatch({type:'PIN',id:state.id});});document.addEventListener('keydown',e=>{if(e.key==='Escape')dispatch({type:'ESCAPE'});});render();
globalThis.B01TooltipDemo=Object.freeze({getState:()=>state, viewport, render});
})();
