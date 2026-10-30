(function(){'use strict';
 const API=window.RunRecap; const $=id=>document.getElementById(id);
 let current=API.sample(),report=API.derive(current),selected=null;
 const status=(m,err=false)=>{const n=$('status');n.textContent=m;n.style.color=err?'#ffc183':'#82cfff';};
 const time=t=>(t/1000).toFixed(2)+' s';
 const node=(tag,className,text)=>{const n=document.createElement(tag);if(className)n.className=className;if(text!==undefined)n.textContent=String(text);return n;};
 function render(){report=API.derive(current);$('stat-result').textContent=report.outcome==='victory'?'Wygrana':'Porażka';$('stat-damage').textContent=report.totalDamage;$('stat-healed').textContent=report.totalRestored;$('stat-lost').textContent=report.lostModules+'/'+report.totalModules;renderEvents();renderModules();}
 function renderEvents(){const list=$('events');list.replaceChildren();const events=API.filter(report,{type:$('type').value,source:$('source').value,query:$('query').value});if(!events.some(e=>e.id===selected))selected=events[0]?.id||null;
 for(const e of events){const b=node('button','event'+(e.type==='detach'?' detached':'')+(e.id===selected?' selected':''));b.type='button';b.dataset.eventId=e.id;b.setAttribute('aria-pressed',String(e.id===selected));const clock=node('span','clock',time(e.t));const details=node('span');details.append(node('strong','',e.description),node('small','',API.TYPE_LABEL[e.type]+' · '+API.SOURCE_LABEL[e.source]));b.append(clock,details);b.addEventListener('click',()=>{selected=e.id;renderEvents();});list.append(b);}
 $('count').textContent=`Widoczne: ${events.length} z ${report.timeline.length} zdarzeń`;
 const found=events.find(e=>e.id===selected);$('selection').textContent=found?`${time(found.t)} — ${found.description}. Pozostałe HP modułu: ${found.hpAfter}.`:'Brak wyników. Zmień filtry.';$('cause').textContent=found?.cause||'W tym zdarzeniu nie określono osobnej przyczyny strukturalnej.';
 }
 function renderModules(){const container=$('modules');container.replaceChildren();for(const m of report.modules){const outer=node('div','module'+(m.detached?' lost':''));const header=node('div');header.append(node('strong','',m.name),node('span','',m.detached?'ODŁĄCZONY':`${m.hp}/${m.max} HP`));const bar=node('div','bar');const fill=node('span');fill.style.width=(m.hp/m.max*100)+'%';bar.append(fill);outer.append(header,bar);container.append(outer);}}
 for(const id of ['type','source','query'])$(id).addEventListener(id==='query'?'input':'change',renderEvents);
 $('export').addEventListener('click',()=>{$('json').value=JSON.stringify(current,null,2);status('Wyeksportowano zweryfikowany raport lokalnie.');});
 $('load').addEventListener('click',()=>{try{const candidate=API.parse($('json').value);const next=API.derive(candidate);current=candidate;report=next;selected=null;render();status('Import zakończony atomowo: '+candidate.runId);}catch(err){status('Import odrzucony: '+err.message+'; raport bez zmian.',true);}});
 $('reset').addEventListener('click',()=>{current=API.sample();selected=null;$('type').value='all';$('source').value='all';$('query').value='';$('json').value='';render();status('Przywrócono pierwotny raport demonstracyjny.');});
 render();status('Wczytano zweryfikowane zdarzenia demonstracyjne.');
})();
