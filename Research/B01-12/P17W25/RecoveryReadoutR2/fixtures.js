'use strict';
(function(root,factory){const api=factory();if(typeof module==='object'&&module.exports)module.exports=api;root.RecoveryFixtures=api;})(typeof globalThis!=='undefined'?globalThis:this,function(){
 const part=(id,name,status,destroyedAt=0,readyAfter=0,quote=65)=>({id,name,status,purchasePrice:100,rebuildQuote:quote,destroyedAt,readyAfter});
 const examples=[
 {label:'Sprawny z uszkodzonym modułem',run:'demo-run-01',generation:8,coreAlive:true,canDeploy:true,hasNextChallenge:true,loanerUsed:false,loanerEligible:false,victoryId:'win-07',freeAvailable:false,parts:[part('core','Rdzeń','ok'),part('wheel','Koło','damaged'),part('turret','Wieżyczka','pending',7,9)]},
 {label:'Zablokowany: robot zastępczy dostępny',run:'demo-run-02',generation:9,coreAlive:true,canDeploy:false,hasNextChallenge:true,loanerUsed:false,loanerEligible:true,victoryId:'win-08',freeAvailable:true,parts:[part('core','Rdzeń','ok'),part('drive','Napęd','pending',8,10),part('radar','Radar','damaged')]},
 {label:'Zniszczony rdzeń — bez zastępstwa',run:'demo-run-03',generation:4,coreAlive:false,canDeploy:false,hasNextChallenge:true,loanerUsed:false,loanerEligible:false,victoryId:null,freeAvailable:false,parts:[part('core','Rdzeń','destroyed',3,5),part('wheel','Koło','ok')]},
 {label:'Jednorazowa możliwość wykorzystana',run:'demo-run-04',generation:12,coreAlive:true,canDeploy:false,hasNextChallenge:true,loanerUsed:true,loanerEligible:false,victoryId:'win-11',freeAvailable:false,parts:[part('core','Rdzeń','ok'),part('drive','Napęd','pending',11,13)]},
 {label:'Brak następnego wyzwania',run:'demo-run-05',generation:7,coreAlive:true,canDeploy:false,hasNextChallenge:false,loanerUsed:false,loanerEligible:false,victoryId:'win-06',freeAvailable:true,parts:[part('core','Rdzeń','ok'),part('weapon','Broń','pending',6,8)]}
 ];
 const list=()=>JSON.parse(JSON.stringify(examples));return Object.freeze({list});
});
