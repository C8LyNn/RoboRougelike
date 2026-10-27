#!/usr/bin/env python3
"""Read-only original-source fetch; no host, ECON or CHL source modifications."""
import hashlib
import json
import pathlib
import time
import urllib.request

HERE = pathlib.Path(__file__).resolve().parent
DEST = HERE / "_pinned"
PIN = "88b8eed60345c49481648833e1fde6dc1a400258"
SOURCES = {
    "Assets/R1/R1RunController.cs": "c6d5f3efe4949da68a5c48ec38ef5028af8b8410",
    "Assets/R3/R3VehicleModel.cs": "cdd75fff4923a382b16c430dac1452320eda88f6",
}
CHL_PIN = "23d40eb21a7c0db97238516c9e9ec6e708f54998"
CHL_SOURCES = {
    "Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003CourseCore.cs": "fa8a12ae71addccc7acee587702bff041bc74d33",
    "Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003CourseController.cs": "3c1a7a85c781aecc8ecb43862ff9f000bbce557b",
    "Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003Gate.cs": "2b9d887b098981578ad150d66feb024389645544",
    "Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003VehicleMotor.cs": "f0417d5f0de1eb63d8579932d5dad707b0f06980",
    "Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003ContactMonitor.cs": "f0eaae5b84dcb1b118f175251f71215d635be5a9",
    "Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003FinishArbiter.cs": "0cb572a40a4120158c46ab36e312afee5c22de8d",
    "Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003TrialMetrics.cs": "b5050e3921092a2192d5928f86aec6ca0f208ab6",
    "Assets/RoboRougelikeChallenges/CHL003/Demo/CHL003DemoBootstrap.cs": "c74455dda6aa6c5210ebcce73330cf296b3597bd",
    "Assets/RoboRougelikeChallenges/CHL003/Tests/EditMode/CHL003FinishAndTelemetryEditModeTests.cs": "44ff2df5b906ab82aed6ac8c7002788188beaaea",
    "Assets/RoboRougelikeChallenges/CHL003/Tests/PlayMode/CHL003DemoAcceptancePlayModeTests.cs": "ac126bbe00479790fcd7bc3480faec5c0c4b7e88",
    "Assets/RoboRougelikeChallenges/CHL003/Tests/EditMode/CHL003CoreEditModeTests.cs": "f361800e81f76cd1060d9aaeaca554893e60c2b9",
    "Assets/RoboRougelikeChallenges/CHL003/Tests/PlayMode/CHL003PhysicsPlayModeTests.cs": "c979f474dab98ff7a475d1c8021a10e55d8fe2ce",
    "Assets/RoboRougelikeChallenges/CHL003/Runtime/B01.CHL003.Runtime.asmdef": "28cf327188cc7f17888e889ce096cc38b7f7e534",
    "Assets/RoboRougelikeChallenges/CHL003/Tests/EditMode/B01.CHL003.EditModeTests.asmdef": "740fd4fc6f478149fff6f6ff934204ceb41198b1",
    "Assets/RoboRougelikeChallenges/CHL003/Tests/PlayMode/B01.CHL003.PlayModeTests.asmdef": "29f79be3a6103f2861a98188dc6eda83a04bf5ec",
    "Assets/RoboRougelikeChallenges/CHL003/Demo/CHL003_Demo.unity": "2852e6f6a7738bf18d8ec3fdda3626b097549339",
}
def blob_id(raw):
    return hashlib.sha1(b"blob " + str(len(raw)).encode("ascii") + b"\0" + raw).hexdigest()

def main():
    receipts=[]
    chl_receipts=[]
    for path, expected in list(SOURCES.items()) + list(CHL_SOURCES.items()):
        source_pin = CHL_PIN if path in CHL_SOURCES else PIN
        url="https://raw.githubusercontent.com/C8LyNn/RoboRougelike/"+source_pin+"/"+path
        err=None
        for trial in range(3):
            try:
                with urllib.request.urlopen(url,timeout=25) as fp:
                    raw=fp.read()
                err=None
                break
            except Exception as ex:
                err=ex
                time.sleep(1)
        if err:
            raise RuntimeError("ACTUAL_FROZEN_HOST_SOURCE_FETCH_FAILED "+path) from err
        actual=blob_id(raw)
        if actual!=expected:
            raise RuntimeError("ACTUAL_HOST_SOURCE_BLOB_MISMATCH "+path+" "+actual)
        file=DEST/path
        file.parent.mkdir(parents=True,exist_ok=True)
        file.write_bytes(raw)
        item={"path":path,"git_blob":actual,
          "sha256":hashlib.sha256(raw).hexdigest(),"bytes":len(raw)}
        (chl_receipts if path in CHL_SOURCES else receipts).append(item)
    (DEST/"receipt.json").write_text(json.dumps({"commit":PIN,"chl_commit":CHL_PIN,
       "actual_pinned_host_files":receipts,"actual_pinned_chl_files":chl_receipts,
       "native_unity":"NOT_RUN"},indent=2)+"\n",encoding="utf-8")
    print("ACTUAL_PINNED_CODEX_CSHARP_VERIFIED",len(receipts),"ORIGINAL_GIT_BLOBS")
    print("ACTUAL_PINNED_CHL285_SOURCE_VERIFIED",len(chl_receipts),"ORIGINAL_GIT_BLOBS",CHL_PIN)

if __name__=="__main__":
    main()
