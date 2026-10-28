// B01-24 PASS15 continued: original source API shape gate, NOT a C# compiler or Unity test.
// Usage: node Research/B01-24/PASS15/verify_variant_api.mjs [repo-checkout-root]
// Requires Node >=18, original files in the B01-24 QA branch, and exact Git blobs.
// Do not repin SHA without an explicit new Codex source/review.
import {readFileSync} from "node:fs";
import {resolve} from "node:path";
import {createHash} from "node:crypto";

const fileSpec = {
  enemy: ["Assets/Pack/P12_PipelineSafeEnemyTurret.cs","8b0cfacbba967ca1a3e3e514e2cd1b49b805946e"],
  fixture: ["Assets/Tests/Editor/B0124VariantTargetReacquisitionEditModeTests.cs","0dcd7609e59568db145869dd3eb8c0a7e2f19de7"],
  damage: ["Assets/Pack/P12_PipelineSafeDamageable.cs","1fd0e6ef2bfc46d089c9b7706b5eff2c956829df"],
  block: ["Assets/Pack/P12_PipelineSafePlayerBlock.cs","e0e1df01c25a36aa370862933dfd4bc30630791d"]
};
const root = resolve(process.argv[2] || ".");
const data = {};
function gitBlobSha(bytes) {
  const header = Buffer.from("blob " + bytes.length + "\0","utf8");
  return createHash("sha1").update(header).update(bytes).digest("hex");
}
for (const [key, [path, expected]] of Object.entries(fileSpec)) {
  const bytes = readFileSync(resolve(root,path));
  const actual = gitBlobSha(bytes);
  if (actual !== expected) {
    process.stderr.write("SOURCE_DRIFT " + key + " expected=" + expected + " actual=" + actual + "\n");
    process.exit(2);
  }
  data[key] = bytes.toString("utf8");
}

const verify = function verify({enemy,fixture,damage,block}) {
 const e=enemy.replace(/\r\n/g,"\n"),f=fixture.replace(/\r\n/g,"\n"),d=damage.replace(/\r\n/g,"\n"),b=block.replace(/\r\n/g,"\n");
 const checks=[
 ["C01 canonical enemy class",/public class P12PipelineSafeEnemyTurret\s*:\s*MonoBehaviour/.test(e)],
 ["C02 actual Configure six-parameter public signature",/public void Configure\(\s*Transform player,\s*Transform yaw,\s*Transform muzzleTransform,\s*Transform root,\s*P12PipelineSafeDamageable coreBlock,\s*Material tracer\)/.test(e)],
 ["C03 SetCombatActive public API",/public void SetCombatActive\(bool active\)/.test(e)],
 ["C04 VariantGun remains public nested API",/public sealed class VariantGun\s*\{/.test(e)],
 ["C05 VariantGun health fields",/public P12PipelineSafeDamageable Block,Support;/.test(e)],
 ["C06 VariantGun Transform fields",/public Transform Pivot,Muzzle;/.test(e)],
 ["C07 VariantGun cooldown field",/public float NextShot;/.test(e)],
 ["C08 VariantGun Online(Transform) API",/public bool Online\(Transform owner\)/.test(e)],
 ["C09 original private variantGuns list",/private readonly List<VariantGun> variantGuns\s*=\s*new List<VariantGun>\(\);/.test(e)],
 ["C10 original exposed VariantWeapons collection",/public IReadOnlyList<VariantGun> VariantWeapons\s*=>\s*variantGuns\.AsReadOnly\(\);/.test(e)],
 ["C11 original private SelectTargetCollider signature",/private Collider SelectTargetCollider\(\)/.test(e)],
 ["C12 original private Update signature",/private void Update\(\)/.test(e)],
 ["C13 actual Update variant branch dispatch",/if\s*\(variantGuns\.Count>0\)\s*\{\s*UpdateVariantWeapons\(\);return;\s*\}/.test(e)],
 ["C14 actual variant path retains separate cached invalidation",/if\s*\(currentTarget==null\|\|!currentTarget\.enabled\|\|Time\.time>=nextTargetTime\)\s*\{\s*currentTarget=SelectTargetCollider\(\);/.test(e)],
 ["C15 actual configured damageable API",/public void Configure\(float hp,\s*bool shouldDetach\)/.test(d)],
 ["C16 actual player block Component API",/public class P12_PipelineSafePlayerBlock\s*:\s*MonoBehaviour/.test(b)],
 ["C17 new fixture constructs original VariantGun",/new P12PipelineSafeEnemyTurret\.VariantGun\s*\{/.test(f)],
 ["C18 new fixture uses typed private List field",/List<P12PipelineSafeEnemyTurret\.VariantGun>\)\s*PrivateField\("variantGuns"\)/.test(f)],
 ["C19 new fixture selects original target",/PrivateMethod\("SelectTargetCollider"\)\.Invoke\(enemy,\s*null\)/.test(f)],
 ["C20 new fixture invokes actual original Update",/PrivateMethod\("Update"\)\.Invoke\(enemy,\s*null\)/.test(f)],
 ["C21 new fixture asserts actual alive and active target",/Assert\.That\(selected,\s*Is\.SameAs\(playerTarget\)/.test(f)],
 ["C22 new fixture explicitly deactivates an already selected target",/playerTarget\.gameObject\.SetActive\(false\)/.test(f)],
 ["C23 new fixture tests cache eviction",/Assert\.That\(cached,\s*Is\.Null,/.test(f)],
 ["C24 new fixture controls timer before invoking Update",/PrivateField\("nextTargetTime"\)\.SetValue\(enemy,\s*float\.PositiveInfinity\)/.test(f)],
 ["C25 new fixture gates shell emission",/PrivateField\("maxRange"\)\.SetValue\(enemy,\s*0f\)/.test(f)],
 ["C26 two independently named NUnit test cases",/void ActiveCachedTarget_RemainsSelectableDuringVariantCombatUpdate_Positive\(\)/.test(f)&&/void InactiveCachedTarget_IsDroppedDuringVariantCombatUpdate_Negative\(\)/.test(f)&&(f.match(/\[Test\]/g)||[]).length===2],
 ["C27 active positive control asserts target unchanged",/Assert\.That\(cached,\s*Is\.SameAs\(playerTarget\)/.test(f)],
 ["C28 negative control asserts Collider stays enabled",/Assert\.That\(playerTarget\.enabled,\s*Is\.True\)/.test(f)],
 ];
 return checks.map(([name,ok])=>({name,ok}));
};

const original = verify(data);
const mutations = [
  ["M01 nonpublic VariantGun", "enemy", "public sealed class VariantGun", "internal sealed class VariantGun"],
  ["M02 changed Configure argument type", "enemy", "P12PipelineSafeDamageable coreBlock,", "Component coreBlock,"],
  ["M03 missing variant Update branch", "enemy", "if(variantGuns.Count>0){UpdateVariantWeapons();return;}", "if(variantGuns.Count>0){return;}"],
  ["M04 changed variant target-cache condition", "enemy", "currentTarget==null||!currentTarget.enabled||Time.time>=nextTargetTime", "currentTarget==null||Time.time>=nextTargetTime"],
  ["M05 missing inactive-player transition", "fixture", "playerTarget.gameObject.SetActive(false);", "playerTarget.gameObject.SetActive(true);"],
  ["M06 missing negative assertion", "fixture", "Assert.That(cached, Is.Null,", "Assert.That(cached, Is.Not.Null,"],
  ["M07 changed original damageable Configure API", "damage", "public void Configure(float hp, bool shouldDetach)", "public void Configure(float hp, int shouldDetach)"],
  ["M08 original player block no longer Component", "block", "public class P12_PipelineSafePlayerBlock : MonoBehaviour", "public class P12_PipelineSafePlayerBlock : System.Object"]
];
const negatives = mutations.map(([name,key,from,to])=>{
  if (!data[key].includes(from)) return {name,pass:false,error:"mutation input not present"};
  const altered={...data,[key]:data[key].replace(from,to)};
  const fails=verify(altered).filter(check=>!check.ok).map(check=>check.name);
  return {name,pass:fails.length>0,caught:fails};
});
const failedOriginal=original.filter(x=>!x.ok);
const failedMutation=negatives.filter(x=>!x.pass);
console.log(JSON.stringify({
  kind:"ORIGINAL_CSHARP_API_SOURCE_SHAPE_NOT_COMPILATION",
  canonicalCodexCommit:"231005c6d0351945a8ee6684f3856af118f0e94f",
  original:{pass:original.length-failedOriginal.length,total:original.length,failed:failedOriginal},
  negativeMutations:{pass:negatives.length-failedMutation.length,total:negatives.length,failed:failedMutation},
  nativeUnity:"NOT_RUN",
  csharpCompiler:"NOT_RUN"
},null,2));
if(failedOriginal.length||failedMutation.length)process.exit(1);
