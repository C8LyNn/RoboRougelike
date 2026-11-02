#!/usr/bin/env python3
from pathlib import Path
import json,subprocess
r=Path(__file__).resolve().parents[3]
m=json.loads(Path(__file__).with_name('manifest.json').read_text())
def g(*args):return subprocess.check_output(['git',*args],cwd=r,text=True).strip()
assert m['base']=='b4df10a7ad7e6c758f8331013f6b216b3a3689a3'
for p,sha in m['blobs'].items():assert g('rev-parse','HEAD:'+p)==sha,p
for p,sha in m['frozen'].items():assert g('rev-parse','HEAD:'+p)==sha,p
base=m['base']
if subprocess.run(['git','cat-file','-e',base+'^{commit}'],cwd=r,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode:
 subprocess.run(['git','fetch','--no-tags','--depth=1','origin',base],cwd=r,check=True)
files=set(g('diff','--name-only',base,'HEAD').splitlines())
assert files==set(m['paths']) and len(files)==7
assert all(x.startswith('Tools/B01/ECONCAMPAIGNW27/') or x=='.github/workflows/b01-22-p17w27-econ-campaign.yml' for x in files)
assert m['product_gate']=='P0_SOFTLOCK_RED' and m['unity_g1']=='NOT_RUN'
print('W27_MANIFEST_PASS 7 additive-only 6 blob-pinned + original V7 exact SHA unchanged; Unity G1 NOT_RUN')