/* Interactive lab deliberately never writes game state, browser storage, or network. */
(function(){'use strict';
const $=id=>document.getElementById(id);const L=globalThis.B01Localize.make(globalThis.B01Catalog);
const state={locale:'pl',screen:'garage',quantity:2,scrap:40,weapon:'Railgun',module:'Wheel',key:'R',renderCount:0};
const trans=(key,params={})=>L.translate(state.locale,key,params);
const node=(tag,text,className)=>{const e=document.createElement(tag);e.textContent=text;if(className)e.className=className;return e};
function update(){document.documentElement.lang=state.locale==='pseudo'?'en':state.locale;
 $('title').textContent=trans('app.title');$('subtitle').textContent=trans('app.subtitle');$('localeLabel').textContent=trans('control.language');
 $('quantityLabel').textContent=trans('control.quantity');$('modeLabel').textContent=trans('control.motion');$('refresh').textContent=trans('control.button');
 $('garageTab').textContent=trans('nav.garage');$('combatTab').textContent=trans('nav.combat');
 $('garageTab').setAttribute('aria-pressed',String(state.screen==='garage'));
 $('combatTab').setAttribute('aria-pressed',String(state.screen==='combat'));
 const p=$('screen');p.replaceChildren();const garage=state.screen==='garage';const title=trans(garage?'garage.title':'combat.title');
 p.append(node('h2',title));p.append(node('div',trans(garage?'garage.parts':'combat.ammo',{count:state.quantity}),'stat'));
 p.append(node('p',garage?trans('garage.scrap',{scrap:state.scrap}):trans('combat.weapon',{weapon:trans('weapon.railgun')})));
 p.append(node('p',garage?trans('garage.warning'):trans('combat.reload',{key:state.key}),garage?'warning':'muted'));
 const btn=node('button',garage?trans('garage.mount',{module:trans('part.wheel')}):trans('status.ready'));btn.type='button';btn.id='actionButton';btn.addEventListener('click',()=>{state.renderCount++;update();});p.append(btn);
 const visible=[...p.querySelectorAll('h2,div,p,button')].map(n=>n.textContent);const max=Math.max(0,...visible.map(x=>x.length));
 $('auditTitle').textContent=trans('audit.title');$('auditFormat').textContent=trans('audit.format',{count:L.keys.length});
 $('auditLong').textContent=trans('audit.long',{count:max});$('auditOverflow').textContent=trans('audit.overflow',{phase:document.documentElement.scrollWidth>innerWidth?'YES':'NO'});
 $('snapshot').textContent=JSON.stringify(state,null,2);
}
$('locale').addEventListener('change',e=>{if(!globalThis.B01Localize.LOCALES.includes(e.target.value))return;state.locale=e.target.value;update();$('notice').textContent=trans(state.locale==='pseudo'?'notice.pseudo':'notice.changed')});
$('quantity').addEventListener('change',e=>{const n=Number(e.target.value);if(!Number.isSafeInteger(n)||n<0||n>99){e.target.value=String(state.quantity);return;}state.quantity=n;update()});
for(const screen of ['garage','combat'])$(screen+'Tab').addEventListener('click',()=>{state.screen=screen;update()});
$('refresh').addEventListener('click',()=>update());update();
})();
