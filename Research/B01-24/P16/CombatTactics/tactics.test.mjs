import test from 'node:test';
import assert from 'node:assert/strict';
import {decide,geometry} from './tactics.mjs';
// B01-24 P16 runnable original-source tests. Run: node --test tactics.test.mjs
const fresh=()=>({self:{position:{x:0,z:0},radius:.5,drive:1,weapons:1},target:{position:{x:0,z:15}},allies:[],obstacles:[],config:{maxStep:1,preferredDistance:12}});
const state=(v={})=>({...fresh(),...v,self:{...fresh().self,...v.self},target:{...fresh().target,...v.target}});
const checks=[
 ['armed target approaches and fires',()=>{const d=decide(fresh());return d.mode==='ARMED_APPROACH'&&d.fireAllowed&&d.movement.z===1}],
 ['no gun but drive: ram mode',()=>{const d=decide(state({self:{weapons:0}}));return d.mode==='RAM_APPROACH'&&!d.fireAllowed&&d.step===1}],
 ['no drive but gun: stationary fire',()=>{const d=decide(state({self:{drive:0}}));return d.mode==='STATIONARY'&&d.fireAllowed&&d.step===0}],
 ['no drive/no gun: disabled',()=>{const d=decide(state({self:{drive:0,weapons:0}}));return d.mode==='DISABLED'&&!d.fireAllowed&&d.step===0}],
 ['partial drive scales actual suggestion',()=>decide(state({self:{drive:.4}})).step===.4],
 ['ally in target lane blocks shot',()=>{const d=decide(state({allies:[{position:{x:0,z:7},radius:1}]}));return d.blockedByAlly&&!d.fireAllowed&&d.fireReason==='ALLY_IN_LINE'}],
 ['ally past target cannot block shot',()=>decide(state({allies:[{position:{x:0,z:25},radius:1}]})).fireAllowed],
 ['near ally demands detour',()=>{const d=decide(state({allies:[{position:{x:0,z:1.6},radius:.6}]}));return d.choice>0&&d.step>0}],
 ['world cover observed but firing policy not invented',()=>{const d=decide(state({obstacles:[{minX:-1,maxX:1,minZ:6,maxZ:7}]}));return d.worldCover&&d.fireAllowed}],
 ['near obstacle demands detour',()=>{const d=decide(state({obstacles:[{minX:-.2,maxX:.2,minZ:1.6,maxZ:2.4}]}));return d.choice>0&&Math.abs(d.movement.x)>0}],
 ['surrounded unit stops rather than clipping',()=>{const d=decide(state({obstacles:[{minX:-2,maxX:2,minZ:-2,maxZ:2}]}));return d.choice<0&&d.step===0}],
 ['continuous swept test catches thin wall',()=>geometry.segmentBox({x:0,z:0},{x:0,z:10},{minX:-.5,maxX:.5,minZ:2,maxZ:2.001},.2)],
 ['tangent ally not counted as hit',()=>!geometry.segmentDisc({x:0,z:0},{x:0,z:10},{x:1,z:5},1)],
 ['real firing range clamp',()=>{const d=decide(state({config:{maxStep:1,preferredDistance:12,fireRange:10}}));return !d.fireAllowed&&d.fireReason==='OUT_OF_RANGE'}],
 ['zero-distance target generates finite output',()=>{const d=decide(state({target:{position:{x:0,z:0}}}));return !d.fireAllowed&&d.step===0&&Number.isFinite(d.movement.x)}],
 ['nonfinite drive is rejected',()=>{try{decide(state({self:{drive:NaN}}));return false}catch(e){return /equipment/.test(String(e))}}],
 ['deterministic repeat',()=>{const s=state({allies:[{position:{x:2,z:7},radius:1}],obstacles:[{minX:-.5,maxX:.5,minZ:1,maxZ:2}]});return JSON.stringify(decide(s))===JSON.stringify(decide(s))}],
 ['does not mutate caller input',()=>{const s=fresh(),before=JSON.stringify(s);decide(s);return before===JSON.stringify(s)}]
];
for(const [name,check] of checks)test(name,()=>assert.equal(check(),true,name));
