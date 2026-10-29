// Data-only offline helper; NOT a Unity transaction or canonical game component.
export function validateCatalog(c){
 if(!c||c.schema!=="b01-equipment-kits-v1"||c.currency!=="scrap"||c.no_mounted_positions!==true)throw Error("SCHEMA");
 if(!c.provenance||!c.provenance.r3_blob||!c.provenance.r4_blob)throw Error("SOURCE_PIN");
 const p={Wheel:15,Suspension:25,Booster:35,JumpDrive:45,Blink:60,Gyroscope:30,OmniDrive:40,MagneticGrapple:45,HoverEngine:35,CrawlerTrack:45,BonusModule:60,DirectionalShield:60,PointDefense:70,StructuralScanner:40,ScannerPulse:30,IndependentSteering:45,Armor:5};
 const w={Basic:30,Automatic:36,Heavy:55,Shotgun:45,Railgun:65,BeamCutter:55,RocketLauncher:60,RotarySaw:50,EmpEmitter:65};
 if(JSON.stringify(c.part_prices)!==JSON.stringify(p)||JSON.stringify(c.weapon_prices)!==JSON.stringify(w))throw Error("PRICE_DRIFT");
 if(!Array.isArray(c.kits)||!c.kits.length)throw Error("NO_KITS");
 const ids=new Set();
 for(const kit of c.kits){
  if(!kit||typeof kit.id!=="string"||!/^EK[0-9]{2}$/.test(kit.id)||ids.has(kit.id))throw Error("DUPLICATE_OR_INVALID_ID");ids.add(kit.id);
  if(typeof kit.title!=="string"||!kit.title.trim()||typeof kit.role!=="string"||!kit.role.trim()||typeof kit.description!=="string"||!kit.description.trim())throw Error("KIT_FIELDS");
  if(!Array.isArray(kit.parts)||!kit.parts.length||kit.parts.length>10)throw Error("PART_COUNT");
  let n=0;const rows=new Set(),weapons=new Set();
  for(const item of kit.parts){
   if(!item||typeof item.kind!=="string"||!Number.isSafeInteger(item.count)||item.count<1||item.count>8)throw Error("PART_SCHEMA");
   if(item.kind==="Core"||(!Object.hasOwn(p,item.kind)&&item.kind!=="Gun"))throw Error("UNKNOWN_OR_CORE_KIND");
   if(item.kind==="Gun"){if(typeof item.weapon!=="string"||!Object.hasOwn(w,item.weapon))throw Error("UNKNOWN_WEAPON");weapons.add(item.weapon)}
   else if(Object.hasOwn(item,"weapon"))throw Error("NON_GUN_WEAPON");
   const key=item.kind+":"+(item.weapon??"");if(rows.has(key))throw Error("DUPLICATED_ITEM_ROW");rows.add(key);n+=item.count;
  }
  if(weapons.size>2)throw Error("THIRD_WEAPON_FAMILY");
  if(n>12)throw Error("KIT_TOO_LARGE");
 }
 return {valid:true,kits:c.kits.length};
}
export function quoteKit(c,id,wallet,inventoryUsed=0){
 validateCatalog(c);
 if(!Number.isSafeInteger(wallet)||wallet<0||wallet>100000||!Number.isSafeInteger(inventoryUsed)||inventoryUsed<0||inventoryUsed>200)throw Error("INVALID_WALLET_OR_CAPACITY");
 const kit=c.kits.find(k=>k.id===id);if(!kit)throw Error("UNKNOWN_KIT");
 const lines=kit.parts.map(p=>({kind:p.kind,...(p.kind==="Gun"?{weapon:p.weapon}:{}),count:p.count,unit_price:p.kind==="Gun"?c.weapon_prices[p.weapon]:c.part_prices[p.kind],line_total:p.count*(p.kind==="Gun"?c.weapon_prices[p.weapon]:c.part_prices[p.kind])}));
 const price=lines.reduce((s,x)=>s+x.line_total,0),items=lines.reduce((s,x)=>s+x.count,0);
 return {kit_id:id,price,items,lines,affordable:wallet>=price,capacity_ok:inventoryUsed+items<=200,eligible:wallet>=price&&inventoryUsed+items<=200,wallet_after_preview:wallet>=price?wallet-price:null,mutates_host:false};
}
