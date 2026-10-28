import test from "node:test";
import assert from "node:assert/strict";
import {readFileSync} from "node:fs";
import {validateCatalog,quoteKit} from "./kit_core.mjs";
const original=JSON.parse(readFileSync(new URL("./catalog.json",import.meta.url),"utf8")),copy=()=>structuredClone(original);
test("all kit schemas",()=>assert.deepEqual(validateCatalog(original),{valid:true,kits:10}));
for(const [id,cost] of Object.entries({EK01:110,EK02:205,EK03:146,EK04:175,EK05:155,EK06:160,EK07:195,EK08:235,EK09:165,EK10:176}))test(id+" exact price",()=>assert.equal(quoteKit(original,id,1000).price,cost));
test("no wallet or host mutation",()=>{const old=JSON.stringify(original);assert.equal(quoteKit(original,"EK08",20).eligible,false);assert.equal(JSON.stringify(original),old)});
test("capacity limit",()=>assert.equal(quoteKit(original,"EK01",999,198).eligible,false));
test("capacity edge",()=>assert.equal(quoteKit(original,"EK01",110,195).eligible,true));
test("negative wallet",()=>assert.throws(()=>quoteKit(original,"EK01",-1),/INVALID_WALLET_OR_CAPACITY/));
test("fraction wallet",()=>assert.throws(()=>quoteKit(original,"EK01",1.5),/INVALID_WALLET_OR_CAPACITY/));
test("unknown kit",()=>assert.throws(()=>quoteKit(original,"EK00",999),/UNKNOWN_KIT/));
const mutations=[
["duplicate kit","DUPLICATE_OR_INVALID_ID",c=>c.kits.push(c.kits[0])],
["unknown part","UNKNOWN_OR_CORE_KIND",c=>c.kits[0].parts[0].kind="FuturePart"],
["invalid weapon","UNKNOWN_WEAPON",c=>c.kits[0].parts[0]={kind:"Gun",weapon:"Future",count:1}],
["third weapon","THIRD_WEAPON_FAMILY",c=>c.kits[0].parts=[{kind:"Gun",weapon:"Basic",count:1},{kind:"Gun",weapon:"Heavy",count:1},{kind:"Gun",weapon:"Railgun",count:1}]],
["price drift","PRICE_DRIFT",c=>c.weapon_prices.Basic=1],
["core","UNKNOWN_OR_CORE_KIND",c=>c.kits[0].parts[0].kind="Core"],
["weapon on wheel","NON_GUN_WEAPON",c=>c.kits[0].parts[0].weapon="Basic"],
["duplicate row","DUPLICATED_ITEM_ROW",c=>c.kits[0].parts.push({kind:"Wheel",count:1})],
["too many items","KIT_TOO_LARGE",c=>{c.kits[0].parts[0].count=8;c.kits[0].parts[1].count=8}]
];
for(const [name,error,mutate] of mutations)test(name,()=>{const c=copy();mutate(c);assert.throws(()=>validateCatalog(c),new RegExp(error))});
