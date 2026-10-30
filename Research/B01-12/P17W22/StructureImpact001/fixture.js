/* Original illustrative connectivity graph, not canonical Unity/PhysX topology. */
'use strict';
(function(root,factory){const data=factory();if(typeof module!=='undefined'&&module.exports)module.exports=data;if(root)root.StructureFixture=data;})(typeof globalThis!=='undefined'?globalThis:this,function(){
 return {
 version:1,coreId:'core',nodes:[
 {id:'core',label:'Rdzeń',kind:'core',x:50,y:48},
 {id:'spine_l',label:'Belka lewa',kind:'beam',x:34,y:48},
 {id:'spine_r',label:'Belka prawa',kind:'beam',x:66,y:48},
 {id:'crossbar',label:'Poprzeczka',kind:'beam',x:50,y:28},
 {id:'front_l',label:'Pancerz przedni L',kind:'armor',x:34,y:28},
 {id:'front_r',label:'Pancerz przedni P',kind:'armor',x:66,y:28},
 {id:'wheel_fl',label:'Koło przód L',kind:'wheel',x:19,y:28},
 {id:'wheel_fr',label:'Koło przód P',kind:'wheel',x:81,y:28},
 {id:'rear_l',label:'Belka tylna L',kind:'beam',x:34,y:69},
 {id:'rear_r',label:'Belka tylna P',kind:'beam',x:66,y:69},
 {id:'wheel_rl',label:'Koło tył L',kind:'wheel',x:19,y:73},
 {id:'wheel_rr',label:'Koło tył P',kind:'wheel',x:81,y:73},
 {id:'gun',label:'Wieżyczka',kind:'weapon',x:50,y:9},
 {id:'cell',label:'Akumulator',kind:'battery',x:50,y:88},
 {id:'sensor',label:'Sensor',kind:'sensor',x:9,y:48}
 ],edges:[
 ['core','spine_l'],['core','spine_r'],['core','crossbar'],['spine_l','front_l'],['spine_r','front_r'],
 ['front_l','crossbar'],['front_r','crossbar'],['front_l','wheel_fl'],['front_r','wheel_fr'],
 ['spine_l','rear_l'],['spine_r','rear_r'],['rear_l','rear_r'],['rear_l','wheel_rl'],['rear_r','wheel_rr'],
 ['crossbar','gun'],['core','cell'],['spine_l','sensor']
 ]};
});
