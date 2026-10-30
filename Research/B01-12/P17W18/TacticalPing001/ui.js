/* Browser-only visual adapter for immutable B01Ping reducer. */
(function () {
  'use strict';
  const engine = globalThis.B01Ping;
  let state = engine.initial(), type = 'attack', lastMessage = 'Gotowe.';
  const $=id=>document.getElementById(id);
  const labels={attack:'Atak',defend:'Obrona',repair:'Naprawa',move:'Ruch'};
  function dispatch(evt) {
    const out=engine.step(state,evt);
    if (out.ok) { state=out.state;lastMessage='Zastosowano '+evt.op; }
    else lastMessage='Odmowa: '+out.reason;
    render();return out;
  }
  function render() {
    $('clock').textContent='T+'+(state.clock/1000).toFixed(1)+' s';
    $('count').textContent=state.markers.length+' / '+engine.CAP;
    $('status').textContent=lastMessage;
    $('snapshot').textContent=engine.exportState(state);
    const map=$('arena'),list=$('list');map.replaceChildren();list.replaceChildren();
    for(const m of engine.rank(state)) {
      const b=document.createElement('button');b.className='marker '+m.type+(m.id===state.selected?' chosen':'');
      b.style.left=(m.point.x/10)+'%';b.style.top=(m.point.y/10)+'%';b.textContent=String(m.priority);
      b.setAttribute('aria-label',labels[m.type]+' '+m.id);b.dataset.id=m.id;
      b.addEventListener('click',e=>{e.stopPropagation();dispatch({op:'select',id:m.id});});map.append(b);
      const item=document.createElement('button');item.className='item'+(m.id===state.selected?' active':'');item.dataset.id=m.id;
      const text=document.createElement('span');text.className='description';text.textContent=labels[m.type]+' · '+m.id+' · P'+m.priority;
      const time=document.createElement('span');time.className='sub';time.textContent=((m.expires-state.clock)/1000).toFixed(1)+' s';
      item.append(text,time);item.addEventListener('click',()=>dispatch({op:'select',id:m.id}));list.append(item);
    }
    if(state.markers.length===0){const e=document.createElement('p');e.textContent='Brak aktywnych znaczników.';list.append(e);}
  }
  function add(x,y){const id='P'+(state.serial+1);return dispatch({op:'add',id,type,point:{x,y},priority:Number($('priority').value)});}
  $('arena').addEventListener('click',event=>{
    if(event.target!==$('arena'))return;
    const rect=$('arena').getBoundingClientRect();
    const x=Math.max(0,Math.min(999,Math.round(((event.clientX-rect.left)/rect.width)*999)));
    const y=Math.max(0,Math.min(999,Math.round(((event.clientY-rect.top)/rect.height)*999)));
    add(x,y);
  });
  $('arena').addEventListener('keydown',e=>{if(e.target===$('arena')&&(e.key==='Enter'||e.key===' ')){e.preventDefault();add(500,500);}});
  $('types').addEventListener('click',e=>{const b=e.target.closest('button[data-kind]');if(!b)return;type=b.dataset.kind;
    $('types').querySelectorAll('button').forEach(x=>x.classList.toggle('active',x===b));});
  $('tick').addEventListener('click',()=>dispatch({op:'tick',time:state.clock+2000}));
  $('remove').addEventListener('click',()=>dispatch({op:'remove',id:state.selected}));
  $('reset').addEventListener('click',()=>dispatch({op:'reset'}));
  render();
})();
