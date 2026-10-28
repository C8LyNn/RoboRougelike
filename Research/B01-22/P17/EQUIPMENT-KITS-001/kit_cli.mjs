import {readFileSync} from "node:fs";
import {fileURLToPath} from "node:url";
import {resolve} from "node:path";
import {validateCatalog,quoteKit} from "./kit_core.mjs";
const catalog=JSON.parse(readFileSync(new URL("./catalog.json",import.meta.url),"utf8"));
if(process.argv[1]&&resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 try{const command=process.argv[2]??"validate";
 if(command==="validate")console.log(JSON.stringify(validateCatalog(catalog)));
 else if(command==="list"){validateCatalog(catalog);console.log(JSON.stringify(catalog.kits.map(k=>({id:k.id,title:k.title,role:k.role,price:quoteKit(catalog,k.id,0).price})),null,2))}
 else if(command==="quote")console.log(JSON.stringify(quoteKit(catalog,process.argv[3],Number(process.argv[4]),process.argv[5]===undefined?0:Number(process.argv[5])),null,2));
 else throw Error("UNKNOWN_COMMAND")
 }catch(e){console.error(String(e));process.exitCode=2}
}
