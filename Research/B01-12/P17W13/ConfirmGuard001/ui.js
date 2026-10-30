'use strict';
(function () {
  const engine=globalThis.ConfirmGuard;
  const $=(id)=>document.getElementById(id);
  let state=engine.initial();
  let activePointer=null;
  const clock=()=>Math.floor(performance.now());
  function dispatch(type,value='',at=clock()) {
    const result=engine.transition(state,{type,value,at});
    if (result.ok) {state=result.state;if(state.phase==='expired')activePointer=null;}
    else $('status').textContent='Odrzucono: '+result.reason+'. Stan bez zmian.';
    render(!result.ok);
    return result;
  }
  function render(rejected=false) {
    $('action').value=state.action;$('mode').value=state.mode;
    $('phase').textContent=state.phase.toUpperCase();
    $('serial').textContent='#'+state.serial;
    $('ttl').textContent=state.deadline!==null && ['armed','holding'].includes(state.phase) ? String(Math.max(0,state.deadline-state.clock))+' ms':'—';
    $('commits').textContent=state.committed;
    $('receipt').textContent=state.receipt?JSON.stringify(state.receipt,null,2):'Brak zatwierdzenia.';
    if(!rejected)$('status').textContent=state.message;
    const armed=state.phase==='armed';
    $('arm').disabled= !['idle','expired','committed'].includes(state.phase);
    $('hold').disabled=state.mode!=='hold'||(!armed&&state.phase!=='holding');
    $('confirm').disabled=state.mode!=='two-step'||!armed;
    $('action').disabled=state.phase==='armed'||state.phase==='holding';$('mode').disabled=$('action').disabled;
    const p=engine.progress(state);$('fill').style.width=p+'%';
    document.querySelector('.meter').setAttribute('aria-valuenow',String(p));
  }
  $('action').addEventListener('change',(e)=>dispatch('SET_ACTION',e.target.value));
  $('mode').addEventListener('change',(e)=>dispatch('SET_MODE',e.target.value));
  $('arm').addEventListener('click',()=>dispatch('ARM'));
  $('cancel').addEventListener('click',()=>{activePointer=null;dispatch('CANCEL');});
  $('reset').addEventListener('click',()=>{activePointer=null;dispatch('RESET');});
  $('confirm').addEventListener('click',()=>dispatch('CONFIRM'));
  const hold=$('hold');
  hold.addEventListener('pointerdown',(e)=>{
    if(e.button!==0||hold.disabled||activePointer!==null)return;
    const token='p'+e.pointerId;
    const result=dispatch('HOLD_START',token);
    if(result.ok){activePointer=token;try{hold.setPointerCapture(e.pointerId);}catch{}}
  });
  hold.addEventListener('pointerup',(e)=>{
    const token='p'+e.pointerId;
    if(activePointer!==token)return;
    activePointer=null;dispatch('HOLD_END',token);
  });
  hold.addEventListener('pointercancel',()=>{if(activePointer!==null){activePointer=null;dispatch('CANCEL');}});
  hold.addEventListener('lostpointercapture',()=>{if(activePointer!==null){activePointer=null;dispatch('CANCEL');}});
  hold.addEventListener('blur',()=>{if(activePointer==='keyboard'){activePointer=null;dispatch('CANCEL');}});
  hold.addEventListener('keydown',(e)=>{
    if(![' ','Enter'].includes(e.key))return;
    e.preventDefault();if(e.repeat)return;
    if(activePointer!==null)return;
    const result=dispatch('HOLD_START','keyboard');if(result.ok)activePointer='keyboard';
  });
  hold.addEventListener('keyup',(e)=>{
    if(![' ','Enter'].includes(e.key))return;e.preventDefault();
    if(activePointer!=='keyboard')return;activePointer=null;dispatch('HOLD_END','keyboard');
  });
  document.addEventListener('keydown',(e)=>{if(e.key==='Escape' && ['armed','holding'].includes(state.phase)){activePointer=null;dispatch('CANCEL');}});
  function poll(){if(state.phase==='armed'||state.phase==='holding'){dispatch('TICK');}requestAnimationFrame(poll);}
  render();requestAnimationFrame(poll);
  // Readonly public test interface, no fake time injected into normal UI interactions.
  Object.defineProperty(globalThis,'ConfirmGuardDemo',{value:Object.freeze({snapshot:()=>JSON.parse(JSON.stringify(state))}),writable:false});
})();
