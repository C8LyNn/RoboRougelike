#!/usr/bin/env python3
"""B01-13 P17 AIM-OBS001: strict independent offline screenshot -> JSON receiver.
Only proves provenance/structure. Never proves real gameplay, consent or physical hit.
"""
import argparse, hashlib, json, math, re, sys
from pathlib import Path
from PIL import Image

SCHEMA='B01-13/P17/AIM-OBS001/v1'
CASES={'CLOSE_STATIC','FAR_STATIC','OCCLUDED','ZOOM_MOVING'}
OUTCOMES={'HIT','BLOCKED','MISS','NO_SHOT','UNVERIFIED'}
SHA40=re.compile(r'[0-9a-f]{40}\Z');SHA64=re.compile(r'[0-9a-f]{64}\Z')
FILE=re.compile(r'[A-Za-z0-9_.-]{1,80}\.(png|jpg|jpeg|webp)\Z',re.I)
REF=re.compile(r'[A-Za-z0-9._-]{5,100}\Z')

class Invalid(ValueError):pass

def need(test,reason):
    if not test: raise Invalid(reason)

def load_json(p):
    need(p.is_file() and not p.is_symlink() and p.stat().st_size<60000,'missing/oversized/linked JSON receipt')
    try: data=json.loads(p.read_text(encoding='utf-8'))
    except (OSError,UnicodeError,ValueError) as e: raise Invalid('bad JSON: '+str(e))
    need(isinstance(data,dict),'root must be object')
    return data

def valid_point(p,width,height):
    return isinstance(p,dict) and set(p)=={'x','y'} and all(type(p[k])==int for k in ['x','y']) and 0<=p['x']<=width and 0<=p['y']<=height

def dist(a,b):
    if a is None or b is None:return None
    return round(math.hypot(a['x']-b['x'],a['y']-b['y'])+1e-11,2)

def verify(receipt:Path,image_dir:Path,expected_game_sha:str,expected_zip_sha256:str):
    need(image_dir.is_dir() and not image_dir.is_symlink(),'image directory missing/unsafe')
    obj=load_json(receipt)
    need(obj.get('schema')==SCHEMA,'schema mismatch')
    mode=obj.get('mode');need(mode in {'SYNTHETIC','OPERATOR_SOLO','HUMAN_APPROVED'},'mode mismatch')
    need(isinstance(obj.get('sid'),str) and re.fullmatch(r'S\d{2,4}',obj['sid']),'SID mismatch')
    game=obj.get('game_sha');zipsha=obj.get('player_zip_sha256')
    need(type(game)==str and SHA40.fullmatch(game) and game==expected_game_sha,'game SHA mismatch')
    need(type(zipsha)==str and SHA64.fullmatch(zipsha) and zipsha==expected_zip_sha256,'Player ZIP SHA mismatch')
    witness=obj.get('consent_witness')
    if mode=='HUMAN_APPROVED':
        need(isinstance(witness,dict) and witness.get('unverified') is True,'HUMAN refs must be marked unverified')
        need(all(isinstance(witness.get(k),str) and REF.fullmatch(witness[k]) for k in ('hq','consent','host','facilitator')),'missing HUMAN attestation refs')
    else:need(witness is None,'no HUMAN attestation allowed in operator/synthetic mode')
    frames=obj.get('frames');need(type(frames)==list and len(frames)==4,'exact four frames required')
    got_cases=set();got_groups=set();hashes=[]
    for i,f in enumerate(frames):
        need(type(f)==dict,f'frame {i}: not an object')
        case=f.get('case');need(case in CASES and case not in got_cases,f'frame {i}: duplicate/unknown scenario');got_cases.add(case)
        group=f.get('group');need(group in {'A','B'},f'frame {i}: invalid group');got_groups.add(group)
        need(f.get('input')==('LMB' if group=='A' else 'RMB'),f'frame {i}: wrong firing input')
        need(f.get('outcome') in OUTCOMES,f'frame {i}: unknown outcome')
        img=f.get('image');need(isinstance(img,dict),f'frame {i}: no image metadata')
        name=img.get('filename');need(type(name)==str and FILE.fullmatch(name) and name not in {'.','..'},f'frame {i}: unsafe image filename')
        w,h=img.get('width'),img.get('height')
        need(type(w)==int and type(h)==int and 10<=w<=8192 and 10<=h<=8192,f'frame {i}: invalid dimensions')
        hashval=img.get('sha256');need(type(hashval)==str and SHA64.fullmatch(hashval),f'frame {i}: bad image SHA')
        p=image_dir/name;need(p.is_file() and not p.is_symlink(),f'frame {i}: missing/nonregular image')
        need(p.stat().st_size<=8*1024*1024,f'frame {i}: image oversized')
        real_hash=hashlib.sha256(p.read_bytes()).hexdigest()
        need(real_hash==hashval,f'frame {i}: IMAGE_BYTES_SHA_MISMATCH')
        try:
            with Image.open(p) as screenshot:
                need(screenshot.format in {'PNG','JPEG','WEBP'},f'frame {i}: bad image encoding')
                need(screenshot.size==(w,h),f'frame {i}: image dimensions mismatch')
        except (OSError,ValueError) as e:raise Invalid(f'frame {i}: invalid bitmap: {e}')
        pts=f.get('points');need(isinstance(pts,dict) and set(pts)=={'reticle','target','impact'},f'frame {i}: points missing')
        need(valid_point(pts['reticle'],w,h) and valid_point(pts['target'],w,h),f'frame {i}: invalid mandatory marker')
        need(pts['impact'] is None or valid_point(pts['impact'],w,h),f'frame {i}: invalid optional marker')
        m=f.get('metrics');need(isinstance(m,dict),f'frame {i}: metrics missing')
        for key,value in (('reticle_target_px',dist(pts['reticle'],pts['target'])),('impact_target_px',dist(pts['impact'],pts['target']))):
            need(type(m.get(key)) in (int,float) if value is not None else m.get(key) is None,f'frame {i}: {key} missing/type')
            need(m.get(key)==value,f'frame {i}: {key} spoofed computation')
        note=f.get('note');need(isinstance(note,str) and len(note)<=300,f'frame {i}: note invalid')
        hashes.append(real_hash)
    need(got_cases==CASES and got_groups=={'A','B'},'missing conditions/groups')
    return {'status':'STRUCTURAL_AND_IMAGE_BYTES_PASS_NOT_GAMEPLAY_PROOF','game_sha':game,'zip_sha256':zipsha,'mode':mode,'cases':len(got_cases),'groups':sorted(got_groups),'images_sha256':hashes,'unity':'NOT_RUN_BY_THIS_TOOL','human_consent_proven':False}

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('receipt',type=Path);p.add_argument('--image-dir',type=Path,required=True)
    p.add_argument('--game-sha',required=True);p.add_argument('--zip-sha256',required=True)
    a=p.parse_args()
    try:
        need(bool(SHA40.fullmatch(a.game_sha)) and bool(SHA64.fullmatch(a.zip_sha256)),'invalid CLI game or ZIP SHA')
        output=verify(a.receipt,a.image_dir,a.game_sha,a.zip_sha256)
        print(json.dumps(output,ensure_ascii=False,sort_keys=True));return 0
    except (Invalid,ValueError,OSError) as e:
        print('FAIL_CLOSED: '+str(e),file=sys.stderr);return 2
if __name__=='__main__':sys.exit(main())
