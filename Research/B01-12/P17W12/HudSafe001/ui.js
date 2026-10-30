(function(){'use strict';
const $=id=>document.getElementById(id);
const presets={mobile:[320,720],tablet:[768,1024],desktop:[1280,720],ultra:[1920,1080]};
const BASE=[['HP',125,52,9,'tl'],['CORE',112,52,9,'tr'],['AMMO_A',116,52,8,'bl'],['AMMO_B',116,52,8,'br'],['OBJECTIVE',184,54,6,'tc'],['MINIMAP',118,114,5,'br']];
function widgets(mode,width){const scale=width<400?.85:1;const a=BASE.map(([id,w,h,priority,anchor])=>({id,w:Math.round(w*scale),h:Math.round(h*scale),priority,anchor}));
if(mode==='crowded')for(let n=0;n<6;n++)a.push({id:'EXTRA_'+n,w:Math.round(112*scale),h:Math.round(60*scale),priority:3,anchor:['tl','tr','bl','br','tc','bc'][n]});
if(mode==='impossible')return Array.from({length:12},(_,i)=>({id:'BLOCK_'+i,w:500,h:300,priority:9,anchor:['tl','tr','bl','br','tc','bc'][i%6]}));
return a;}
function config(){const [width,height]=presets[$('preset').value];const big=$('insets').value==='large';const inset=big?32:12;let exclusions=[];
const cutout=$('cutout').value;
if(cutout==='notch')exclusions=[{x:Math.floor(width/2)-Math.min(width*.25,150),y:0,w:Math.min(width*.5,300),h:46}];
if(cutout==='center')exclusions=[{x:Math.floor(width*.35),y:Math.floor(height*.35),w:Math.floor(width*.3),h:Math.floor(height*.3)}];
return {width,height,insets:{left:inset,right:inset,top:inset,bottom:inset},exclusions,widgets:widgets($('density').value,width)};}
function rectNode(cls,r,parent,text){const d=document.createElement('div');d.className=cls;d.style.left=r.x+'px';d.style.top=r.y+'px';d.style.width=r.w+'px';d.style.height=r.h+'px';if(text)d.textContent=text;parent.appendChild(d);return d;}
function render(){const c=config(),res=HUDSafe.solve(c),screen=$('screen'),layers=$('layers');screen.style.width=c.width+'px';screen.style.height=c.height+'px';layers.replaceChildren();$('status').dataset.ok=String(res.ok);$('status').textContent=res.ok?'GOTOWE: wszystkie panele rozmieszczone bez kolizji':'ZABLOKOWANO: '+res.reason+' — brak częściowego układu';
const area={x:c.insets.left+8,y:c.insets.top+8,w:c.width-c.insets.left-c.insets.right-16,h:c.height-c.insets.top-c.insets.bottom-16};rectNode('safe',area,layers);
for(const e of c.exclusions)rectNode('excluded',e,layers);
for(const p of res.placements){const d=rectNode('widget'+(p.priority>=9?' high':''),p,layers,p.id);d.setAttribute('role','img');d.setAttribute('aria-label',p.id+', priorytet '+p.priority);}
const badOverlap=res.placements.some((p,i)=>res.placements.slice(i+1).some(q=>HUDSafe.overlap(p,q))||c.exclusions.some(q=>HUDSafe.overlap(p,q)));
$('metrics').textContent='Widok: '+c.width+'×'+c.height+'\nPanele: '+res.placements.length+'/'+c.widgets.length+'\nPowód: '+res.reason+'\nZajęta powierzchnia: '+res.occupied+' px²';
$('checks').textContent='Kolizje: '+(badOverlap?'RED':'BRAK')+'\nUnieważnianie częściowego stanu: '+(!res.ok&&res.placements.length===0?'PASS':'—')+'\nDeterministyczny wynik: '+(HUDSafe.snapshot(c)===HUDSafe.snapshot(c)?'PASS':'RED');
window.__hudResult={config:c,result:res,badOverlap};}
for(const id of ['preset','cutout','insets','density'])$(id).addEventListener('change',render);$('recalc').addEventListener('click',render);render();
})();
