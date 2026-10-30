/* B01-12 GARAGE-FILTER-001 UI adapter, no state persistence */
(function(){'use strict';
const $=id=>document.getElementById(id);
const query=$('query'),status=$('status'),cards=$('cards'),count=$('count'),facets=$('facets'),json=$('json'),copy=$('copy');
const labels={drive:'Napęd',weapon:'Broń',armor:'Pancerz',power:'Energia',utility:'Narzędzia'};
let last=null,exported='';
function el(tag,cls,value){const n=document.createElement(tag);if(cls)n.className=cls;if(value!==undefined)n.textContent=String(value);return n;}
function render(){
 const result=GarageFilter.search(GarageCatalog,query.value);
 last=result;exported='';json.textContent='—';copy.disabled=true;cards.replaceChildren();facets.replaceChildren();
 if(!result.ok){status.textContent='Odrzucono zapytanie: '+result.code;status.className='status error';count.textContent='0 wyników — błąd';return;}
 status.className='status';status.textContent='Filtry zastosowane bezpiecznie';count.textContent=result.count+' części';
 if(result.count===0)cards.append(el('div','empty','Nie znaleziono elementów — zmień filtry.'));
 for(const x of result.items){
   const card=el('article','part');card.dataset.id=x.id;
   card.append(el('h3','',x.name),el('div','id',x.id+' · '+labels[x.type]));
   const facts=el('div','facts');for(const [k,v] of [['Masa',x.mass],['Koszt',x.cost],['Energia',x.energy],['Sloty',x.slots],['Własność',x.owned?'Tak':'Nie'],['Pasuje',x.fit]])facts.append(el('span','',k+': '+v));card.append(facts);
   const tags=el('div','tags');for(const t of x.tags)tags.append(el('span','tag',t));card.append(tags);cards.append(card);
 }
 for(const key of ['drive','weapon','armor','power','utility']){
   const line=el('div','facet');line.append(el('span','',labels[key]),el('strong','',result.facets[key]));facets.append(line);
 }
}
$('search').addEventListener('click',render);query.addEventListener('keydown',e=>{if(e.key==='Enter'){e.preventDefault();render();}});
$('reset').addEventListener('click',()=>{query.value='';render();query.focus();});
for(const preset of document.querySelectorAll('.preset'))preset.addEventListener('click',()=>{query.value=preset.dataset.query;render();});
$('export').addEventListener('click',()=>{const out=GarageFilter.exportSelection(last);if(!out.ok){status.textContent='Eksport odrzucony: '+out.code;status.className='status error';return;}exported=out.json;json.textContent=exported;copy.disabled=false;status.textContent='Gotowy eksport identyfikatorów';});
copy.addEventListener('click',async()=>{if(!exported)return;try{await navigator.clipboard.writeText(exported);status.textContent='Skopiowano JSON';}catch(_){status.textContent='Brak uprawnienia do schowka — JSON jest dostępny poniżej';}});
render();
})();
