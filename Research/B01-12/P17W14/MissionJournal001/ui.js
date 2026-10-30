(function(){'use strict';
const J=globalThis.MissionJournal;let state=J.initial(),selected=null,serial=0;
const $=id=>document.getElementById(id);
const msg=t=>{$('status').textContent=t;};
function apply(result){if(result.ok){state=result.state;msg(result.reason==='COMPLETED'?'Cel ukończony.':result.reason==='PIN_UPDATED'?'Przypięcie zmienione.':'Misja zresetowana.');render();return true;}msg('Odmowa: '+result.reason);return false;}
function select(id){selected=id;render();}
function render(){
 const all=J.view(state), filter=$('filter').value;
 const shown=all.filter(t=>filter==='all'||t.status===filter);
 const list=$('list');list.replaceChildren();
 for(const task of shown){
  const item=document.createElement('article');item.className='item';
  const info=document.createElement('div');info.style.minWidth='0';
  const name=document.createElement('strong');name.textContent=task.label;
  const details=document.createElement('small');details.textContent=task.group+' • '+task.status+(task.pinned?' • przypięte':'');
  details.className=task.status;
  info.append(name,details);
  const buttons=document.createElement('div');buttons.className='actions';
  const inspect=document.createElement('button');inspect.type='button';inspect.textContent='Szczegóły';inspect.setAttribute('aria-label','Szczegóły: '+task.label);inspect.onclick=()=>select(task.id);
  const pin=document.createElement('button');pin.type='button';pin.textContent=task.pinned?'Odepnij':'Przypnij';pin.setAttribute('aria-pressed',String(task.pinned));pin.onclick=()=>apply(J.transition(state,{type:'PIN',id:task.id,receipt:null}));
  const complete=document.createElement('button');complete.type='button';complete.textContent='Ukończ';complete.disabled=task.status!=='available';complete.onclick=()=>{serial++;apply(J.transition(state,{type:'COMPLETE',id:task.id,receipt:'ui_'+serial}));};
  buttons.append(inspect,pin,complete);item.append(info,buttons);list.append(item);
 }
 $('progress').value=state.done.length;$('counter').textContent=state.done.length+' / '+J.TASKS.length+' ukończonych • rev '+state.revision;
 const obj=all.find(t=>t.id===selected);
 $('selected').textContent=obj?obj.label+' — '+obj.status:'Wybierz cel z listy.';
 $('dependency').textContent=obj?'Wymaga: '+(obj.deps.length?obj.deps.join(', '):'brak'):'';
 const history=$('history');history.replaceChildren();
 for(let i=0;i<state.done.length;i++){const line=document.createElement('div');line.textContent=(i+1)+'. '+state.done[i]+' ['+state.receipts[i]+']';history.append(line);}
}
$('filter').addEventListener('change',render);
$('reset').addEventListener('click',()=>{serial=0;selected=null;apply(J.transition(state,{type:'RESET',id:null,receipt:null}));});
$('export').addEventListener('click',()=>{$('json').value=J.exportState(state);msg('Wyeksportowano aktualny stan.');});
$('import').addEventListener('click',()=>{const r=J.importState($('json').value);if(r.ok){state=r.state;serial=Math.max(serial,state.revision);msg('Poprawny import.');render();}else msg('Import odrzucony: '+r.reason);});
globalThis.MissionJournalDemo={snapshot:()=>JSON.parse(JSON.stringify(state)),select:id=>select(id)};
render();
})();
