#!/usr/bin/env python3
"""Execute original byte-pinned W28 R1 vs W29 R2 source-contract witnesses.
This is NOT Unity compilation, scene rendering, or native test execution.
"""
from __future__ import annotations
import argparse
import hashlib
import re
import subprocess
import sys
import zipfile
from pathlib import Path

PARENT='c267bebbd65337fa41db65185448da670f783d39'
OLD_SHA256='d03325a7fe8594c785e275cb570c9dbb2dd92dd269b562ad91aa6454885ccbe8'
PATH='Assets/B01UserExperience/B0112GarageSpatialW28/B0112GarageSpatialOverlay.cs'
TEST_PATH='Assets/Tests/Editor/B0112GarageSpatialW28Tests.cs'

def main() -> int:
    p=argparse.ArgumentParser()
    p.add_argument('--root',default=None)
    p.add_argument('--r1-zip',default=None)
    args=p.parse_args()
    root=Path(args.root).resolve() if args.root else Path(__file__).resolve().parents[4]
    if args.r1_zip:
        with zipfile.ZipFile(args.r1_zip) as archive: original=archive.read(PATH)
    else:
        result=subprocess.run(['git','show',f'{PARENT}:{PATH}'],cwd=root,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
        if result.returncode!=0: print('FAIL R1 unavailable:',result.stderr.decode(errors='replace'));return 2
        original=result.stdout
    if hashlib.sha256(original).hexdigest()!=OLD_SHA256:
        print('FAIL_CLOSED frozen R1 bytes mismatch');return 2
    before=original.decode('utf-8')
    after=(root/PATH).read_text(encoding='utf-8')
    tests=(root/TEST_PATH).read_text(encoding='utf-8')
    scenarios=[
      ('R1 used delayed Destroy in Editor Awake', '            Destroy(primitive);' in before),
      ('R2 uses DestroyImmediate in EditMode', 'if(Application.isPlaying)Destroy(primitive);else DestroyImmediate(primitive);' in after),
      ('R1 dropped candidate on 256-slot saturation', 'if(boxes.Count>=MaxBoxes)return;' in before and before.index('AddFunctionalEnvelope(p.transform,p.Kind)')<before.index('var tint=CandidateTint')),
      ('R2 reserves slots for 2x2 candidate', all(x in after for x in ['ReservedCandidateBoxes=4','BackgroundBudget=MaxBoxes-ReservedCandidateBoxes','candidate ? MaxBoxes : BackgroundBudget','candidate:true'])),
      ('R1 hid all zones when no hovered target', '!source.HasTarget||source.Tool!=R41GarageTool.Mount' in before),
      ('R2 preserves fixed zones but no phantom candidate', '!source.HasTarget||source.Tool!=R41GarageTool.Mount' not in after and 'if(source.HasTarget)' in after),
      ('R1 enumerated child component arrays per readout', 'GetComponentsInChildren<Renderer>(true)' in before and 'GetComponentsInChildren<Transform>(true)' in before),
      ('R2 reuses component list buffers', 'GetComponentsInChildren(true,rendererScratch)' in after and 'GetComponentsInChildren(true,transformScratch)' in after),
      ('R2 maintains existing host permission', 'source.PlacementIssue' in after and '=> hasTarget&&placementIssue==null' in after),
      ('R2 authored native Unity regression tests', tests.count('[Test]')==8 and 'EditorInstantiationDoesNotLeaveTemporaryCubeBehind' in tests and 'LargeRigReserveKeepsAllWideCandidateCellsAvailable' in tests),
    ]
    count=0
    for title,yes in scenarios:
        print(('PASS' if yes else 'FAIL'),title)
        count+=bool(yes)
    # Exercises the configured reservation semantics as a SOURCE model, not a C# or Unity run.
    max_boxes=int(re.search(r'public const int MaxBoxes=(\d+)',after).group(1))
    reserve=int(re.search(r'public const int ReservedCandidateBoxes=(\d+)',after).group(1))
    total=0
    for _ in range(300):
        if total<max_boxes-reserve:total+=1
    for _ in range(reserve):
        if total<max_boxes:total+=1
    good=total==256
    print(('PASS' if good else 'FAIL'),'SOURCE MODEL 300 background + 4 candidate => 256 bounded boxes')
    count+=bool(good)
    print(f'W29 SOURCE REGRESSION {count}/{len(scenarios)+1} PASS; UNITY_G1=NOT_RUN')
    return 0 if count==len(scenarios)+1 else 1

if __name__=='__main__':sys.exit(main())
