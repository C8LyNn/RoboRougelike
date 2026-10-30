/* B01-12 BOXSELECT UI adapter; self-contained offline demo, no persistence/network. */
(()=>{'use strict';
const E=window.BoxSelect;if(!E)throw Error('BOX_ENGINE_MISSING');
let state=E.initial();const el=id=>document.getElementById(id),board=el('board'),drag=el('drag'),nodes=new Map();
const show=(msg,bad=false)=>{const a=el('status');a.textContent=msg;a.classList.toggle('error',bad);};
const mode=()=>el('mode').value;
function act(event){const r=E.step(state,event);if(r.ok){state=r.state;render();show(r.reason==='NO_CHANGE'?'Brak zmian.':'Zapisano operację '+event.type+' · rewizja '+state.rev+'.');}else show('Odrzucono: '+r.reason+'. Stan pozostaje bez zmian.',true);return r;}
function render(){el('version').textContent='REV '+state.rev;el('stats').textContent=state.selected.length+' z '+state.parts.filter(p=>!p.locked).length+' dostępnych części wybranych';
  for(const p of state.parts){const n=nodes.get(p.id);n.style.left=p.x+'px';n.style.top=p.y+'px';n.style.width=p.w+'px';n.style.height=p.h+'px';n.setAttribute('aria-pressed',String(state.selected.includes(p.id)));}
  const ul=el('selected');ul.replaceChildren();const chosen=state.parts.filter(p=>state.selected.includes(p.id));if(!chosen.length){const li=document.createElement('li');li.textContent='Brak wybranych elementów';ul.append(li);}else for(const p of chosen){const li=document.createElement('li');li.textContent=p.label+' · '+p.id;ul.append(li);}
  el('undo').disabled=!state.undo.length;el('redo').disabled=!state.redo.length;
  for(const name of ['up','down','left','right'])el(name).disabled=!state.selected.length;
}
function mount(){for(const p of state.parts){const n=document.createElement('button');n.className='part';n.type='button';n.textContent=p.label;n.dataset.kind=p.kind;n.dataset.id=p.id;n.disabled=p.locked;n.setAttribute('aria-label',p.label+(p.locked?' — zablokowane':''));n.setAttribute('aria-pressed','false');n.title=p.locked?'Core: nie można zaznaczyć':'Kliknij albo Enter, aby zaznaczyć';
    n.addEventListener('pointerdown',ev=>ev.stopPropagation());n.addEventListener('click',()=>{act({type:'SELECT',ids:[p.id],mode:mode()});n.focus();});board.append(n);nodes.set(p.id,n);}
  render();}
function local(ev){const r=board.getBoundingClientRect();return {x:Math.max(0,Math.min(799,Math.floor(ev.clientX-r.left-1))),y:Math.max(0,Math.min(499,Math.floor(ev.clientY-r.top-1)))};}
let start=null, pointerId=null;
board.addEventListener('pointerdown',ev=>{if(ev.target!==board||ev.button!==0)return;start=local(ev);pointerId=ev.pointerId;board.setPointerCapture(ev.pointerId);drag.style.display='block';drag.style.left=start.x+'px';drag.style.top=start.y+'px';drag.style.width='0px';drag.style.height='0px';});
board.addEventListener('pointermove',ev=>{if(!start||ev.pointerId!==pointerId)return;const p=local(ev);drag.style.left=Math.min(start.x,p.x)+'px';drag.style.top=Math.min(start.y,p.y)+'px';drag.style.width=Math.abs(start.x-p.x)+'px';drag.style.height=Math.abs(start.y-p.y)+'px';});
board.addEventListener('pointerup',ev=>{if(!start||ev.pointerId!==pointerId)return;const p=local(ev),sx=start.x,sy=start.y;start=null;pointerId=null;drag.style.display='none';if(Math.abs(sx-p.x)<4&&Math.abs(sy-p.y)<4){if(mode()==='replace')act({type:'SELECT',ids:[],mode:'replace'});return;}const x=Math.min(sx,p.x),y=Math.min(sy,p.y);act({type:'RECT',x,y,w:Math.abs(sx-p.x)+1,h:Math.abs(sy-p.y)+1,mode:mode(),contain:el('contain').value});});
board.addEventListener('pointercancel',()=>{start=null;pointerId=null;drag.style.display='none';});
const mv={up:[0,-20],down:[0,20],left:[-20,0],right:[20,0]};for(const [id,[dx,dy]] of Object.entries(mv))el(id).addEventListener('click',()=>act({type:'MOVE',dx,dy}));
el('clear').addEventListener('click',()=>act({type:'SELECT',ids:[],mode:'replace'}));
el('select-mobility').addEventListener('click',()=>act({type:'SELECT',ids:state.parts.filter(p=>p.kind==='mobility').map(p=>p.id),mode:mode()}));
el('undo').addEventListener('click',()=>act({type:'UNDO'}));el('redo').addEventListener('click',()=>act({type:'REDO'}));el('reset').addEventListener('click',()=>act({type:'RESET'}));
el('export').addEventListener('click',()=>{try{const data=E.snapshot(state);show('Eksport (bez zapisu): '+data.slice(0,180)+(data.length>180?'…':''));}catch(e){show('Odmowa eksportu — niespójny stan',true);}});
document.addEventListener('keydown',ev=>{if(ev.ctrlKey&&!ev.altKey&&!ev.shiftKey&&ev.key.toLowerCase()==='z'){ev.preventDefault();act({type:'UNDO'});}else if(ev.ctrlKey&&!ev.altKey&&!ev.shiftKey&&ev.key.toLowerCase()==='y'){ev.preventDefault();act({type:'REDO'});}else if(ev.altKey&&!ev.ctrlKey&&['ArrowUp','ArrowDown','ArrowLeft','ArrowRight'].includes(ev.key)){ev.preventDefault();const [dx,dy]=mv[ev.key.slice(5).toLowerCase()];act({type:'MOVE',dx,dy});}});
mount();
})();
