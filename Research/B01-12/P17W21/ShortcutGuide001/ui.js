(function(){'use strict';
const API=window.ShortcutGuide;let state=API.initial();
const query=document.getElementById('query'),results=document.getElementById('results'),details=document.getElementById('details'),count=document.getElementById('count'),status=document.getElementById('status'),panel=document.getElementById('panel'),toggle=document.getElementById('toggle');
function node(tag,text,className){const e=document.createElement(tag);if(text!==undefined)e.textContent=text;if(className)e.className=className;return e;}
function update(){const found=API.getCommands(state.scope,state.query);if(!found.ok){status.textContent=found.error;return;}
 panel.hidden=!state.open;toggle.setAttribute('aria-expanded',String(state.open));toggle.textContent=state.open?'Ukryj przewodnik (?)':'Pokaz przewodnik (?)';
 for(const button of document.querySelectorAll('[data-scope]'))button.setAttribute('aria-pressed',String(button.dataset.scope===state.scope));
 if(query.value!==state.query)query.value=state.query;
 results.replaceChildren();count.textContent=found.commands.length+' polecen / '+API.COMMANDS.length+' w katalogu';
 if(found.commands.length===0)results.appendChild(node('div','Brak wynikow. Sprobuj innej frazy.','empty'));
 for(const c of found.commands){const button=node('button',undefined,'item');button.type='button';button.dataset.id=c.id;button.setAttribute('aria-pressed',String(state.selected===c.id));const left=node('span');left.appendChild(node('strong',c.title));left.appendChild(node('small',c.category+' · '+(c.scope==='global'?'Wszystkie tryby':c.scope)));button.appendChild(left);button.appendChild(node('kbd',c.key));button.addEventListener('click',()=>dispatch('SELECT',c.id));results.appendChild(button);}
 details.replaceChildren();const selected=found.commands.find(c=>c.id===state.selected);
 details.appendChild(node('div',selected?'SZCZEGOLY AKCJI':'PODPOWIEDZ','caps'));
 details.appendChild(node('h2',selected?selected.title:'Wybierz polecenie'));
 if(selected){details.appendChild(node('kbd',selected.key));details.appendChild(node('p',selected.description));details.appendChild(node('p','Dostepnosc: '+(selected.scope==='global'?'wszystkie konteksty':selected.scope)));}
 else details.appendChild(node('p','Wybierz polecenie z listy. Opis jest wyłącznie demonstracyjny.'));
 status.textContent='Kontekst: '+state.scope+' · rewizja '+state.revision+' · '+(state.open?'przewodnik widoczny':'przewodnik ukryty');
}
function dispatch(type,value){const next=API.reduce(state,{type,value});if(next.ok){state=next.state;update();}else{status.textContent='Odrzucono: '+next.error;}}
for(const button of document.querySelectorAll('[data-scope]'))button.addEventListener('click',()=>dispatch('SCOPE',button.dataset.scope));
query.addEventListener('input',()=>dispatch('QUERY',query.value));toggle.addEventListener('click',()=>dispatch('TOGGLE',null));
document.addEventListener('keydown',e=>{if(e.key==='Escape'&&state.open){e.preventDefault();dispatch('CLOSE',null);toggle.focus();return;}if(e.key==='?'&&!e.repeat&&!(e.target instanceof HTMLInputElement)&&!(e.target instanceof HTMLTextAreaElement)){e.preventDefault();dispatch('TOGGLE',null);}});
update();window.__guideDemo={getState:()=>({...state}),getCount:()=>API.getCommands(state.scope,state.query).commands.length};
})();
