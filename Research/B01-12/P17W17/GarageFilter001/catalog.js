/* B01-12 GARAGE-FILTER-001: original illustrative static catalogue, not game balance data */
(function(root,factory){if(typeof module==='object'&&module.exports){module.exports=factory();}else{root.GarageCatalog=factory();}})(typeof globalThis!=='undefined'?globalThis:this,function(){
'use strict';
const records=[
{id:'wheel-01',name:'Opona terenowa',type:'drive',slots:2,mass:12,cost:80,energy:0,owned:true,fit:'both',tags:['terrain','rolling']},
{id:'wheel-02',name:'Koło lekkie',type:'drive',slots:1,mass:5,cost:35,energy:0,owned:true,fit:'light',tags:['speed','rolling']},
{id:'wheel-03',name:'Gąsienica ciężka',type:'drive',slots:3,mass:29,cost:160,energy:8,owned:false,fit:'heavy',tags:['terrain','armor']},
{id:'hover-01',name:'Silnik poduszkowy',type:'drive',slots:2,mass:16,cost:220,energy:32,owned:false,fit:'both',tags:['hover','speed']},
{id:'gun-01',name:'Działo kinetyczne',type:'weapon',slots:2,mass:24,cost:180,energy:14,owned:true,fit:'heavy',tags:['kinetic','ranged']},
{id:'gun-02',name:'Lekki karabin',type:'weapon',slots:1,mass:10,cost:115,energy:7,owned:true,fit:'light',tags:['kinetic','ranged']},
{id:'gun-03',name:'Emiter impulsów',type:'weapon',slots:2,mass:15,cost:235,energy:26,owned:false,fit:'both',tags:['energy','ranged']},
{id:'armor-01',name:'Pancerz stalowy',type:'armor',slots:2,mass:32,cost:75,energy:0,owned:true,fit:'heavy',tags:['armor','durable']},
{id:'armor-02',name:'Płyta kompozytowa',type:'armor',slots:1,mass:9,cost:95,energy:0,owned:false,fit:'light',tags:['armor','speed']},
{id:'power-01',name:'Mały reaktor',type:'power',slots:2,mass:14,cost:145,energy:60,owned:true,fit:'both',tags:['energy','utility']},
{id:'power-02',name:'Kondensator',type:'power',slots:1,mass:7,cost:90,energy:25,owned:false,fit:'both',tags:['energy','utility']},
{id:'util-01',name:'Moduł naprawczy',type:'utility',slots:2,mass:18,cost:190,energy:20,owned:false,fit:'both',tags:['repair','utility']}
];
return Object.freeze(records.map(x=>Object.freeze({...x,tags:Object.freeze([...x.tags])})));
});
