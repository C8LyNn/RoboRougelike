"""Supplement the unchanged B01-60 recorder with all extracted payload byte hashes."""
import hashlib
import json
import pathlib
import zipfile

root=pathlib.Path(__file__).resolve().parents[2]
evidence=root/'Artifacts/Local/EXIT273-AB'
plan=json.loads((evidence/'plan.json').read_text(encoding='utf-8-sig'))
rows=[]
for label,build in plan['builds'].items():
    extracted=evidence/'extracted'/label
    files=0
    with zipfile.ZipFile(build['archive']) as archive:
        for member in archive.infolist():
            if member.is_dir():continue
            expected=hashlib.sha256(archive.read(member)).hexdigest()
            actual=hashlib.sha256((extracted/member.filename).read_bytes()).hexdigest()
            assert expected==actual,(label,member.filename)
            files+=1
    rows.append(dict(label=label,source=build['source_sha'],archiveSHA256=build['sha256'],filesVerified=files,assemblySHA256=hashlib.sha256((extracted/'RoboRougelike_Data/Managed/Assembly-CSharp.dll').read_bytes()).hexdigest(),unityPlayerSHA256=hashlib.sha256((extracted/'UnityPlayer.dll').read_bytes()).hexdigest()))
result=dict(status='EXTRACTED_PAYLOAD_BYTES_PASS_NOT_EXECUTION_PROOF',builds=rows)
(evidence/'payload-verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2))
