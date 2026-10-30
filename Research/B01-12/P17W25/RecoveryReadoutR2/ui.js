'use strict';
(function(){
 const data=RecoveryFixtures.list(),engine=RecoveryReadout;
 const el=id=>document.getElementById(id);let current=-1,view=null;
 const put=(id,value)=>{el(id).textContent=String(value)};
 function choose(index){
  if(!Number.isInteger(index)||index<0||index>=data.length)return false;
  const {label,...payload}=data[index];const next=engine.project(payload);if(!next.ok){put('status','Odmowa odczytu: '+next.code);return false;}
  current=index;view=next;
  for(const [i,btn] of [...el('scenarios').children].entries())btn.setAttribute('aria-pressed',String(i===current));
  put('identity','Run '+view.run+' · Generacja '+view.generation);
  el('status').replaceChildren();const heading=document.createElement('strong');heading.textContent=view.route.replaceAll('_',' ');el('status').append(heading,document.createTextNode(view.explanation));
  put('victory',view.victoryLabel);put('free',view.freeLabel);
  const p=el('parts');p.replaceChildren();for(const part of view.parts){const card=document.createElement('article');card.className='part';card.dataset.status=part.status;const name=document.createElement('h3');name.textContent=part.name;const chip=document.createElement('span');chip.className='chip';chip.textContent=part.status;const reason=document.createElement('p');reason.textContent=part.reason;const price=document.createElement('div');price.className='small';price.textContent='Kwota w danych demonstracyjnych (niepotwierdzona przez ECON): '+part.quote+' Scrap';card.append(chip,name,reason);if(part.status==='pending'||part.status==='destroyed')card.append(price);p.append(card)}
  el('exportPanel').hidden=true;put('json','');return true;
 }
 data.forEach((item,i)=>{const btn=document.createElement('button');btn.type='button';btn.className='scenario';btn.textContent=item.label;btn.setAttribute('aria-pressed','false');btn.addEventListener('click',()=>choose(i));el('scenarios').append(btn)});
 el('export').addEventListener('click',()=>{if(view){put('json',JSON.stringify(view,null,2));el('exportPanel').hidden=false}});
 el('hide').addEventListener('click',()=>{el('exportPanel').hidden=true;put('json','')});
 choose(0);
 window.__readoutDebug=()=>({current,route:view?.route,parts:view?.parts.length,exportOpen:!el('exportPanel').hidden});
})();
