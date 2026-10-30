/* B01-12 STRUCTURE-IMPACT-001 — original offline, deterministic, read-only structure preview. */
'use strict';
(function (root, factory) {
  const api = factory();
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  if (root) root.StructureImpact = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  const idPattern = /^[a-z][a-z0-9_-]{0,23}$/;
  const kinds = new Set(['core','wheel','armor','weapon','beam','battery','sensor']);
  const exact = (o, keys) => o && typeof o === 'object' && !Array.isArray(o) && Object.keys(o).sort().join('|') === keys.slice().sort().join('|');
  const error = code => Object.freeze({ok:false,code,removed:null,detached:[],retained:[],components:[],baselineDisconnected:[]});
  function validate(graph) {
    if (!exact(graph,['version','coreId','nodes','edges']) || graph.version !== 1 || !Array.isArray(graph.nodes) || !Array.isArray(graph.edges) || graph.nodes.length < 2 || graph.nodes.length > 32 || graph.edges.length > 70) return 'GRAPH_SCHEMA';
    if (typeof graph.coreId !== 'string' || !idPattern.test(graph.coreId)) return 'CORE_ID';
    const seen = new Set();
    for (const n of graph.nodes) {
      if (!exact(n,['id','label','kind','x','y']) || typeof n.id !== 'string' || !idPattern.test(n.id) || seen.has(n.id) || typeof n.label !== 'string' || n.label.length < 1 || n.label.length > 32 || !kinds.has(n.kind) || !Number.isInteger(n.x) || n.x < 4 || n.x > 96 || !Number.isInteger(n.y) || n.y < 4 || n.y > 96) return 'NODE_INVALID';
      seen.add(n.id);
    }
    const core = graph.nodes.find(n=>n.id===graph.coreId);
    if (!core || core.kind!=='core') return 'CORE_MISSING';
    if (graph.nodes.filter(n=>n.kind==='core').length!==1) return 'MULTIPLE_CORES';
    const keys = new Set();
    for (const e of graph.edges) {
      if (!Array.isArray(e) || e.length!==2 || typeof e[0]!=='string' || typeof e[1]!=='string' || e[0]===e[1] || !seen.has(e[0]) || !seen.has(e[1])) return 'EDGE_INVALID';
      const key = [e[0],e[1]].sort().join('|');
      if (keys.has(key)) return 'EDGE_DUPLICATE';
      keys.add(key);
    }
    return null;
  }
  function components(graph, removedId) {
    const adjacent = new Map(graph.nodes.filter(n=>n.id!==removedId).map(n=>[n.id,[]]));
    for (const [a,b] of graph.edges) if (adjacent.has(a) && adjacent.has(b)) {adjacent.get(a).push(b);adjacent.get(b).push(a);}
    const seen=new Set(), result=[];
    for (const id of [...adjacent.keys()].sort()) {
      if (seen.has(id)) continue;
      const queue=[id], comp=[];seen.add(id);
      for(let i=0;i<queue.length;i++) {const cur=queue[i];comp.push(cur);for(const neighbor of adjacent.get(cur).slice().sort()) if(!seen.has(neighbor)){seen.add(neighbor);queue.push(neighbor);}}
      result.push(comp.sort());
    }
    return result.sort((a,b)=>a[0].localeCompare(b[0]));
  }
  function preview(graph, removeId) {
    const invalid=validate(graph);if(invalid) return error(invalid);
    if(typeof removeId !== 'string' || !idPattern.test(removeId) || !graph.nodes.some(n=>n.id===removeId)) return error('TARGET_UNKNOWN');
    if(removeId === graph.coreId) return error('CORE_PROTECTED');
    const before=components(graph,null);
    const activeBefore=new Set((before.find(c=>c.includes(graph.coreId))||[]));
    const baselineDisconnected=graph.nodes.map(n=>n.id).filter(id=>!activeBefore.has(id)).sort();
    const after=components(graph,removeId);
    const activeAfter=new Set(after.find(c=>c.includes(graph.coreId))||[]);
    const detached=graph.nodes.map(n=>n.id).filter(id=>id!==removeId && activeBefore.has(id) && !activeAfter.has(id)).sort();
    const retained=graph.nodes.map(n=>n.id).filter(id=>id!==removeId && activeAfter.has(id)).sort();
    return {ok:true,code:'OK',removed:removeId,detached,retained,components:after,baselineDisconnected};
  }
  function scan(graph){
    const invalid=validate(graph);if(invalid)return error(invalid);
    const rows=graph.nodes.filter(n=>n.id!==graph.coreId).map(n=>{
      const p=preview(graph,n.id);
      return {id:n.id,label:n.label,kind:n.kind,detachedCount:p.detached.length,detached:p.detached};
    });
    rows.sort((a,b)=>b.detachedCount-a.detachedCount || a.id.localeCompare(b.id));
    return {ok:true,code:'OK',rows};
  }
  return Object.freeze({validate,preview,scan});
});
