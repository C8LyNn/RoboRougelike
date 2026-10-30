/* B01-12-FOCUSNAV-001/R1. Pure deterministic focus-state reducer; no DOM, storage, Unity or network. */
(function (root, factory) {
  if (typeof module === 'object' && module.exports) module.exports = factory();
  else root.B01FocusNav = factory();
})(typeof self !== 'undefined' ? self : this, function () {
  'use strict';
  const panels = Object.freeze({
    garage: Object.freeze([
      {id:'build', label:'Zbuduj', x:0, y:0}, {id:'rotate',label:'Obróć moduł',x:1,y:0},
      {id:'repair',label:'Napraw',x:2,y:0}, {id:'inventory',label:'Magazyn',x:0,y:1},
      {id:'paint',label:'Kolor',x:1,y:1}, {id:'launch',label:'Rozpocznij walkę',x:2,y:1}]),
    run: Object.freeze([
      {id:'run-stats',label:'Statystyki',x:0,y:0}, {id:'run-map',label:'Mapa',x:1,y:0},
      {id:'run-loadout',label:'Uzbrojenie',x:2,y:0}, {id:'run-pause',label:'Pauza',x:0,y:1},
      {id:'run-retry',label:'Ponów',x:1,y:1}, {id:'run-exit',label:'Wyjdź',x:2,y:1}]),
    reward: Object.freeze([
      {id:'reward-armor',label:'Pancerz',x:0,y:0}, {id:'reward-speed',label:'Prędkość',x:1,y:0},
      {id:'reward-power',label:'Obrażenia',x:2,y:0}, {id:'reward-info',label:'Opis ulepszeń',x:0,y:1},
      {id:'reward-reroll',label:'Przelosuj',x:1,y:1}, {id:'reward-accept',label:'Zatwierdź',x:2,y:1}])
  });
  const dialog = Object.freeze([
    {id:'confirm',label:'Potwierdź',x:0,y:0}, {id:'cancel',label:'Anuluj',x:1,y:0}]);
  const directions = Object.freeze({ArrowUp:[0,-1],ArrowDown:[0,1],ArrowLeft:[-1,0],ArrowRight:[1,0]});
  const ids = new Set(Object.values(panels).flat().map(n=>n.id));
  const special = new Set(dialog.map(n=>n.id));

  class FocusRouter {
    constructor(){
      this.panel='garage';this.focus='build';this.disabled=new Set();
      this.memory={garage:'build',run:'run-stats',reward:'reward-armor'};
      this.modal=null;this.lastEvent='Gotowe';this.sequence=0;
    }
    nodes(){return this.modal ? dialog : panels[this.panel];}
    enabledNodes(){return this.nodes().filter(n=>this.modal||!this.disabled.has(n.id));}
    _valid(id){return this.enabledNodes().some(n=>n.id===id);}
    _first(){return this.enabledNodes()[0]?.id||null;}
    _event(message){this.lastEvent=message;this.sequence+=1;return message;}
    switchPanel(name){
      if(!Object.hasOwn(panels,name)) return {ok:false,reason:'unknown-panel'};
      if(this.modal) return {ok:false,reason:'modal-trap'};
      this.memory[this.panel]=this.focus;
      this.panel=name;const next=this.memory[name];this.focus=this._valid(next)?next:this._first();
      this._event('Ekran: '+name);return {ok:true,focus:this.focus};
    }
    setEnabled(id,enabled){
      if(!ids.has(id))return {ok:false,reason:'unknown-control'};
      if(typeof enabled!=='boolean')return {ok:false,reason:'invalid-enabled'};
      const current=this.disabled.has(id);
      if(enabled===!current)return {ok:true,unchanged:true};
      if(!enabled){
        const owner=Object.keys(panels).find(p=>panels[p].some(n=>n.id===id));
        const remaining=panels[owner].filter(n=>n.id!==id&&!this.disabled.has(n.id));
        if(!remaining.length)return {ok:false,reason:'last-focusable'};
        this.disabled.add(id);
        if(this.panel===owner&&!this.modal&&this.focus===id)this.focus=remaining[0].id;
        if(this.memory[owner]===id)this.memory[owner]=remaining[0].id;
      }else this.disabled.delete(id);
      this._event((enabled?'Włączono: ':'Wyłączono: ')+id);
      return {ok:true,focus:this.focus};
    }
    move(key){
      if(!directions[key])return {ok:false,reason:'invalid-direction'};
      const v=directions[key],current=this.enabledNodes().find(n=>n.id===this.focus);
      if(!current){this.focus=this._first();return {ok:!!this.focus,focus:this.focus};}
      let candidates=this.enabledNodes().filter(n=>n.id!==current.id).map(n=>{
        const dx=n.x-current.x,dy=n.y-current.y,primary=dx*v[0]+dy*v[1],cross=Math.abs(dx*v[1]-dy*v[0]);
        return {n,primary,cross};
      }).filter(n=>n.primary>0).sort((a,b)=>{
        const as=a.primary+3*a.cross,bs=b.primary+3*b.cross;
        return as-bs||a.cross-b.cross||a.primary-b.primary||a.n.id.localeCompare(b.n.id);
      });
      if(!candidates.length)return {ok:false,reason:'edge',focus:this.focus};
      this.focus=candidates[0].n.id;
      if(!this.modal)this.memory[this.panel]=this.focus;
      this._event('Fokus: '+this.focus);
      return {ok:true,focus:this.focus};
    }
    focusControl(id){
      if(!this._valid(id))return {ok:false,reason:this.modal?'modal-trap':'disabled-or-unknown'};
      this.focus=id;if(!this.modal)this.memory[this.panel]=id;
      this._event('Fokus: '+id);return {ok:true,focus:id};
    }
    openDialog(){
      if(this.modal)return {ok:false,reason:'already-modal'};
      this.modal={returnTo:this.focus,panel:this.panel};this.focus='confirm';
      this._event('Otwarto potwierdzenie');return {ok:true,focus:this.focus};
    }
    closeDialog(accepted=false){
      if(!this.modal)return {ok:false,reason:'no-modal'};
      const previous=this.modal;this.modal=null;this.panel=previous.panel;
      this.focus=this._valid(previous.returnTo)?previous.returnTo:this._first();
      this.memory[this.panel]=this.focus;
      this._event(accepted?'Zatwierdzono':'Anulowano');return {ok:true,focus:this.focus,accepted:!!accepted};
    }
    activate(id=this.focus){
      if(!this._valid(id))return {ok:false,reason:this.modal?'modal-trap':'disabled-or-unknown'};
      this.focus=id;
      if(this.modal)return this.closeDialog(id==='confirm');
      this.memory[this.panel]=id;return this.openDialog();
    }
    snapshot(){return {
      version:1,panel:this.panel,focus:this.focus,modal:!!this.modal,
      disabled:Array.from(this.disabled).sort(),memory:{...this.memory},
      sequence:this.sequence,lastEvent:this.lastEvent
    };}
    invariant(){
      if(!this._valid(this.focus))return false;
      if(this.modal&&(!special.has(this.focus)||this.modal.panel!==this.panel))return false;
      return Object.keys(panels).every(p=>panels[p].some(n=>!this.disabled.has(n.id)));
    }
  }
  return {FocusRouter,panels,dialog,directions};
});
