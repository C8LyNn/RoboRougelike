"""Reject empty/failed Unity NUnit XML even when an action exits successfully."""
import argparse, json, xml.etree.ElementTree as ET
from pathlib import Path

def validate(path: Path, exit_code: int = 0) -> dict:
    if exit_code != 0:
        raise ValueError(f'Unity process/action exit code {exit_code}')
    root=ET.parse(path).getroot()
    cases=root.findall('.//test-case')
    if root.tag!='test-run' or root.get('result')!='Passed' or not cases:
        raise ValueError('Expected a Passed test-run with executed test cases')
    counts={s:sum(c.get('result')==s for c in cases) for s in ('Passed','Failed','Skipped','Inconclusive')}
    if counts['Passed'] != len(cases):
        raise ValueError(f'Every case must execute and pass: {counts}')
    for field,expected in [('total',len(cases)),('passed',counts['Passed']),('failed',0),('skipped',0),('inconclusive',0)]:
        if field in root.attrib and int(root.get(field))!=expected:
            raise ValueError(f'XML aggregate {field} does not match executed cases')
    return {'total':len(cases),'passed':counts['Passed'],'failed':0,'skipped':0}

if __name__=='__main__':
    p=argparse.ArgumentParser(); p.add_argument('xml',type=Path); p.add_argument('--exit-code',type=int,default=0)
    args=p.parse_args(); print(json.dumps(validate(args.xml,args.exit_code)))
