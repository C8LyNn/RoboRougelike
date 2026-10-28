import {analyzeCapture,compare,makeSyntheticCapture} from './core.mjs';
const doc=id=>document.getElementById(id);
const captures={A:null,B:null};
let synthetic=false;
const num=(x,precision=1)=>Number.isFinite(x)?x.toFixed(precision):'—';
function state(id,txt,okay){const el=doc(`state${id}`);el.textContent=txt;el.className='file-state '+(okay===true?'good':okay===false?'bad':'');}
function color(id){return id==='A'?'#96eccb':'#a9abff';}
async function loadFiles(id,list){
  const names=['receipt.json','frames.csv','objects.csv'];
  try{
    const map=new Map([...list].map(file=>[file.name,file]));
    if(map.size!==3||names.some(n=>!map.has(n)))throw new Error('Wskaż dokładnie trzy pliki: receipt.json, frames.csv, objects.csv');
    for(const n of names)if(map.get(n).size>6_000_000)throw new Error('Plik za duży: '+n);
    const [rec,frame,obj]=await Promise.all(names.map(n=>map.get(n).text()));
    captures[id]=analyzeCapture(rec,frame,obj);synthetic=false;
    state(id,`Zaimportowano ${captures[id].frames} klatek · ${captures[id].scans} skanów`,true);
    doc('error').textContent='';render();
  }catch(e){captures[id]=null;state(id,e.message,false);doc('error').textContent='Błąd '+id+': '+e.message;render();}
}
doc('filesA').addEventListener('change',e=>loadFiles('A',e.target.files));
doc('filesB').addEventListener('change',e=>loadFiles('B',e.target.files));
doc('demo').addEventListener('click',()=>{
  captures.A=analyzeCapture(...makeSyntheticCapture({scenario:'DEMO_SYNTHETIC_A',slow:0,shots:0}));
  captures.B=analyzeCapture(...makeSyntheticCapture({scenario:'DEMO_SYNTHETIC_B',slow:1.8,shots:64}));
  synthetic=true;state('A','DEMO — dane syntetyczne (nie pomiar)',true);
  state('B','DEMO — dane syntetyczne (nie pomiar)',true);
  doc('error').textContent='';render();
});
doc('reset').addEventListener('click',()=>{captures.A=captures.B=null;synthetic=false;doc('filesA').value='';doc('filesB').value='';state('A','Nie zaimportowano');state('B','Nie zaimportowano');doc('error').textContent='';render();});
function createMetric(title,val,description){const m=document.createElement('div');m.className='metric';const h=document.createElement('div');h.className='name';h.textContent=title;const v=document.createElement('div');v.className='value';v.textContent=val;const d=document.createElement('div');d.className='secondary-value';d.textContent=description;m.append(h,v,d);return m;}
function chart(canvas,as,bs,field,unit){
  const ctx=canvas.getContext('2d');const w=canvas.width,h=canvas.height;
  ctx.clearRect(0,0,w,h);const left=51,right=20,top=20,bottom=43;
  const values=[...as,...bs].map(r=>r?.[field]).filter(Number.isFinite);const topVal=Math.max(1,...values)*1.18;
  ctx.lineWidth=1;ctx.font='13px system-ui';ctx.fillStyle='#91a7b9';
  for(let i=0;i<=4;i++){
    const y=top+(h-top-bottom)*(i/4);ctx.strokeStyle='#2b3847';ctx.beginPath();ctx.moveTo(left,y);ctx.lineTo(w-right,y);ctx.stroke();ctx.fillText(num(topVal*(1-i/4),1),5,y+4);
  }
  for(let i=0;i<12;i+=2){let x=left+(w-left-right)*(i/11);ctx.fillText(`${i*10}s`,x-10,h-13);}
  for(const [arr,id] of [[as,'A'],[bs,'B']]){
    ctx.strokeStyle=color(id);ctx.lineWidth=3;ctx.beginPath();let started=false;
    arr.forEach((r,i)=>{const val=r?.[field];if(!Number.isFinite(val)){started=false;return;}const x=left+(w-left-right)*(i/11),y=top+(h-top-bottom)*(1-val/topVal);if(started)ctx.lineTo(x,y);else ctx.moveTo(x,y);started=true;});ctx.stroke();
  }
  ctx.fillStyle='#778d9f';ctx.fillText(unit,w-right-40,12);
}
function appendRow(table, a,b,i){let tr=document.createElement('tr');let items=[`${i*10}–${(i+1)*10}s`,num(a?.p95),num(b?.p95),`${num(a?.parts,0)} / ${num(b?.parts,0)}`,`${num(a?.shots,0)} / ${num(b?.shots,0)}`,`${num(a?.enemies,0)} / ${num(b?.enemies,0)}`];for(const t of items){const td=document.createElement('td');td.textContent=t;tr.append(td);}table.append(tr);}
function render(){
 const a=captures.A,b=captures.B;doc('analysis').hidden=!(a||b);if(!a&&!b)return;
 doc('provenance').textContent=synthetic?'DEMO — WYŁĄCZNIE SYNTETYCZNE':'PLIKI NIEZWERYFIKOWANE NATYWNIE';doc('provenance').className='pill'+(synthetic?' synthetic':'');
 const metrics=doc('metrics');metrics.replaceChildren();
 for(const [label,session] of [['A',a],['B',b]])if(session){
   metrics.append(createMetric(`${label} · P95 Update`,num(session.p95,2)+' ms',session.scenario));
   metrics.append(createMetric(`${label} · P99 Update`,num(session.p99,2)+' ms','nie DisplayChange'));
   metrics.append(createMetric(`${label} · Części`,num(session.meanParts,1),'średnia z ok. 1 Hz'));
   metrics.append(createMetric(`${label} · Obiekty pocisków`,num(session.meanShots,1),`wrogowie ${num(session.meanEnemies,1)}`));
 }
 const c=doc('comparison');c.replaceChildren();c.className='comparison';
 let heading=document.createElement('b'),desc=document.createElement('div');
 if(!a||!b){heading.textContent='Wczytaj drugą sesję, aby porównać.';desc.textContent='Panel działa również w trybie pojedynczej sesji.';}
 else {const result=compare(a,b);
   heading.textContent=result.comparable?'METADANE ZGODNE — PORÓWNANIE EKSPLORACYJNE':'WARUNKI RÓŻNIĄ SIĘ — NIE WYCIĄGAJ WNIOSKÓW';
   desc.textContent=result.comparable?`ΔP95 Update ${result.p95DeltaMs>=0?'+':''}${num(result.p95DeltaMs,2)} ms; ΔP99 ${num(result.p99DeltaMs,2)} ms. Bez kontroli sceny/sterowania nie oznacza to skutku zwiększenia liczby obiektów.`:`Różne: ${result.mismatches.join(', ')}. Traktuj wyniki tylko jako osobne obserwacje.`;
   if(!result.comparable)c.classList.add('warn');
 }
 c.append(heading,desc);
 const empty=Array.from({length:12},()=>null);const at=a?.timeline||empty,bt=b?.timeline||empty;
 chart(doc('frameChart'),at,bt,'p95','ms');chart(doc('countChart'),at,bt,'parts','szt.');
 const table=doc('timeline');table.replaceChildren();for(let i=0;i<12;i++)appendRow(table,at[i],bt[i],i);
}
render();
