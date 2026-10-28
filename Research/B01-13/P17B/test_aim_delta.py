#!/usr/bin/env python3
"""Real original receiver + real Chromium AIM-DELTA001 positive/negative gates (synthetic images only)."""
from __future__ import annotations
import json, tempfile, hashlib, subprocess, sys, importlib.util
from pathlib import Path
from PIL import Image, ImageDraw
from playwright.sync_api import sync_playwright

HERE=Path(__file__).resolve().parent
SOURCE=HERE/'aim_delta.py'
# The frozen parent PR #349 is a required immutable dependency, not copied into P17B.
RECEIVER=HERE.parent/'P17'/'verify_aim_receipt.py'
OLD_GAME='a'*40; NEW_GAME='b'*40; OLD_ZIP='c'*64; NEW_ZIP='d'*64
CASES=('CLOSE_STATIC','FAR_STATIC','OCCLUDED','ZOOM_MOVING')


def make_receipt(root:Path,sid:str,mode:str,game:str,zipsha:str,shift:int):
    root.mkdir(parents=True,exist_ok=True)
    frames=[]
    for n,case in enumerate(CASES):
        name=case+'.png'; p=root/name
        image=Image.new('RGB',(640,360),(20+n*18,43,67))
        draw=ImageDraw.Draw(image);draw.text((10,10),'SYNTHETIC '+case+sid,fill='white')
        draw.rectangle((80,45,555,300),outline='yellow',width=3);image.save(p)
        r={'x':200+shift,'y':160};t={'x':300,'y':160};impact={'x':310+shift,'y':170} if n==2 else None
        from math import hypot
        frames.append({'case':case,'group':'A' if n%2==0 else 'B','input':'LMB' if n%2==0 else 'RMB',
            'outcome':'UNVERIFIED',
            'image':{'filename':name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'width':640,'height':360},
            'points':{'reticle':r,'target':t,'impact':impact},
            'metrics':{'reticle_target_px':round(hypot(r['x']-t['x'],r['y']-t['y']),2),
                       'impact_target_px':round(hypot(impact['x']-t['x'],impact['y']-t['y']),2) if impact else None},
            'note':'SYNTHETIC only <script>alert(1)</script>'})
    obj={'schema':'B01-13/P17/AIM-OBS001/v1','mode':mode,'sid':sid,'game_sha':game,
         'player_zip_sha256':zipsha,'consent_witness':None,'frames':frames}
    file=root/'receipt.json';file.write_text(json.dumps(obj,ensure_ascii=False),encoding='utf-8');return file


def run():
    checks=[]
    def check(k,c):
        checks.append({'case':k,'pass':bool(c)})
        if not c:raise AssertionError(k)
    with tempfile.TemporaryDirectory() as td:
        t=Path(td);old=t/'before';new=t/'after';out=t/'PRIVATE.html'
        br=make_receipt(old,'S01','SYNTHETIC',OLD_GAME,OLD_ZIP,0)
        ar=make_receipt(new,'S02','SYNTHETIC',NEW_GAME,NEW_ZIP,20)
        def argv(outfile=out, before=br,after=ar,receiver=RECEIVER,extra=(),zipafter=NEW_ZIP):
            return [sys.executable,str(SOURCE),'--before',str(before),'--before-images',str(old),
                '--before-game-sha',OLD_GAME,'--before-zip-sha256',OLD_ZIP,
                '--after',str(after),'--after-images',str(new),'--after-game-sha',NEW_GAME,
                '--after-zip-sha256',zipafter,'--receiver',str(receiver),'--output',str(outfile),*extra]
        r=subprocess.run(argv(extra=('--confirm-private-embedded-images',)),capture_output=True,text=True)
        check('cli_verified_original_receiver',r.returncode==0)
        check('cli_structural_status',json.loads(r.stdout)['status']=='STRUCTURAL_COMPARE_ONLY_NOT_GAMEPLAY_PROOF')
        check('output_was_written',out.exists())
        check('private_output_0600',out.stat().st_mode&0o777==0o600)
        generated=out.read_text()
        check('four_real_image_pairs',generated.count('data:image/png;base64,')==8)
        check('original_private_images_embedded',generated.count('SYNTHETIC')>=1)
        check('both_sha_printed',OLD_GAME in generated and NEW_GAME in generated)
        check('delta_positive_numeric', 'Δ +20.00 px' not in generated and 'Δ -20.00 px' in generated)
        check('no_unsafe_note_in_html','<script>alert(1)</script>' not in generated)
        check('privacy_warning_rendered','PRYWATNY' in generated)
        check('network_blocking_CSP', "connect-src 'none'" in generated)
        check('no_auto_gameplay_pass', 'NOT_GAMEPLAY_PROOF' not in generated and 'nie dowodzi poprawy' in generated)
        with sync_playwright() as pw:
            browser=pw.chromium.launch(executable_path='/usr/bin/chromium',headless=True)
            page=browser.new_page(viewport={'width':1280,'height':900})
            errors=[];requests=[]
            page.on('pageerror',lambda e:errors.append(str(e)))
            page.on('request',lambda req:requests.append(req.url) if req.url.startswith('http') else None)
            page.set_content(generated)
            page.wait_for_function('document.querySelector(".frame img").complete')
            check('chromium_renders_images',page.locator('.frame img').first.evaluate('(e)=>e.naturalWidth')==640)
            check('first_case_shows',page.locator('[data-case="CLOSE_STATIC"]').is_visible())
            check('other_cases_hidden',not page.locator('[data-case="FAR_STATIC"]').is_visible())
            check('eight_original_image_objects',page.locator('.frame img').count()==8)
            check('before_after_dimensions',page.locator('.frame img').first.evaluate('(e)=>e.naturalHeight')==360)
            check('marker_default_visible',page.locator('.marker-layer').first.is_visible())
            check('marker_R_T_exist',page.locator('[data-case="CLOSE_STATIC"] .mark').count()==4)
            check('impact_case_shows_three_each',page.locator('[data-case="OCCLUDED"] .mark').count()==6)
            page.select_option('#scenario','OCCLUDED')
            check('scenario_switch_visually',page.locator('[data-case="OCCLUDED"]').is_visible())
            check('scenario_first_hidden_after_switch',not page.locator('[data-case="CLOSE_STATIC"]').is_visible())
            page.uncheck('#marks')
            check('annotations_toggle_off',not page.locator('[data-case="OCCLUDED"] .marker-layer').first.is_visible())
            page.check('#marks')
            check('annotations_toggle_on',page.locator('[data-case="OCCLUDED"] .marker-layer').first.is_visible())
            page.select_option('#scenario','CLOSE_STATIC')
            for width in (1280,720,320):
                page.set_viewport_size({'width':width,'height':900})
                check(f'{width}_no_horizontal_scroll',page.evaluate('document.documentElement.scrollWidth<=window.innerWidth'))
                def mark_error():
                    return page.evaluate('''() => {const img=document.querySelector('.case:not([hidden]) .frame img'); const m=document.querySelector('.case:not([hidden]) .mark.reticle'); const a=img.getBoundingClientRect(),b=m.getBoundingClientRect();return Math.max(Math.abs(b.x+b.width/2-(a.x+(200/640)*a.width)),Math.abs(b.y+b.height/2-(a.y+(160/360)*a.height)));}''')
                check(f'{width}_original_image_marker_alignment',mark_error()<3)
            check('no_JS_runtime_errors',not errors)
            check('no_external_requests',not requests)
            browser.close()
        # Negative cases are actual subprocess invocations that must fail closed.
        def reject(name,cmd):
            x=subprocess.run(cmd,capture_output=True,text=True)
            check(name,x.returncode==2 and 'FAIL_CLOSED' in x.stderr)
        reject('repeat_write_rejected',argv(extra=('--confirm-private-embedded-images',)))
        reject('missing_privacy_confirmation_rejected',argv(outfile=t/'no_ack.html'))
        reject('wrong_after_zip_rejected',argv(outfile=t/'bad_zip.html',zipafter='f'*64,extra=('--confirm-private-embedded-images',)))
        reject('same_session_rejected',argv(outfile=t/'same.html',after=br,extra=('--confirm-private-embedded-images',)))
        bad_mode=new/'human.json';bad=json.loads(ar.read_text());bad['mode']='HUMAN_APPROVED';bad['consent_witness']={'unverified':True,'hq':'HQREF123','consent':'CONSENT123','host':'HOST123','facilitator':'FACILITATOR123'};bad_mode.write_text(json.dumps(bad))
        reject('human_embed_refused',argv(outfile=t/'human.html',after=bad_mode,extra=('--confirm-private-embedded-images',)))
        bad_group=new/'wrong_group.json';bad=json.loads(ar.read_text());bad['frames'][0]['group']='B';bad['frames'][0]['input']='RMB';bad_group.write_text(json.dumps(bad))
        reject('mismatched_AB_groups_rejected',argv(outfile=t/'group.html',after=bad_group,extra=('--confirm-private-embedded-images',)))
        bad_dims=new/'wrong_size.json';bad=json.loads(ar.read_text());bad['frames'][0]['image']['width']=720;bad_dims.write_text(json.dumps(bad))
        reject('altered_image_dimensions_rejected',argv(outfile=t/'size.html',after=bad_dims,extra=('--confirm-private-embedded-images',)))
        # Build a second ORIGINAL receiver under a deliberately changed filename/body.
        forged=t/'forged_receiver.py';forged.write_bytes(RECEIVER.read_bytes()+b'\n# altered\n')
        reject('untrusted_receiver_SHA_rejected',argv(outfile=t/'tamper.html',receiver=forged,extra=('--confirm-private-embedded-images',)))
        check('no_partial_output_after_reject',not (t/'tamper.html').exists())
        # WAVE02 original-source TOCTOU negative: mutate PHYSICAL PNG bytes only
        # AFTER both original-image gates have passed but BEFORE embedding HTML.
        # Prior R1 emitted the changed PNG unverified (reproduced SOURCE_RED).
        spec=importlib.util.spec_from_file_location('aim_delta_r2_original',SOURCE)
        module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        info=module.load_pair(br,old,ar,new,OLD_GAME,OLD_ZIP,NEW_GAME,NEW_ZIP,RECEIVER)
        check('original_gate_accepts_unmodified_image_pair',len(info['pairs'])==4)
        from PIL import ImageDraw as Draw
        from PIL import Image as PILImage
        before_png=old/'CLOSE_STATIC.png';original_before=before_png.read_bytes()
        with PILImage.open(before_png) as pic:
            modified=pic.copy()
        Draw.Draw(modified).rectangle((16,16,60,60),fill=(255,18,36))
        modified.save(before_png)
        check('test_mutation_changes_physical_before_sha',hashlib.sha256(before_png.read_bytes()).hexdigest()!=hashlib.sha256(original_before).hexdigest())
        try:
            module.render(info);rejected_before=False
        except module.DeltaError as e:
            rejected_before='IMAGE_CHANGED_AFTER_VERIFICATION' in str(e)
        check('post_gate_before_image_mutation_rejected',rejected_before)
        before_png.write_bytes(original_before)
        info=module.load_pair(br,old,ar,new,OLD_GAME,OLD_ZIP,NEW_GAME,NEW_ZIP,RECEIVER)
        after_png=new/'FAR_STATIC.png';original_after=after_png.read_bytes()
        with PILImage.open(after_png) as pic:
            changed=pic.copy()
        Draw.Draw(changed).rectangle((22,22,70,70),fill=(12,255,48))
        changed.save(after_png)
        try:
            module.render(info);rejected_after=False
        except module.DeltaError as e:
            rejected_after='IMAGE_CHANGED_AFTER_VERIFICATION' in str(e)
        check('post_gate_after_image_mutation_rejected',rejected_after)
        after_png.write_bytes(original_after)
        # A file replaced with a symlink after gate must also fail closed.
        info=module.load_pair(br,old,ar,new,OLD_GAME,OLD_ZIP,NEW_GAME,NEW_ZIP,RECEIVER)
        before_png.unlink()
        before_png.symlink_to(old/'FAR_STATIC.png')
        try:
            module.render(info);rejected_symlink=False
        except module.DeltaError as e:
            rejected_symlink='IMAGE_CHANGED_AFTER_VERIFICATION' in str(e)
        check('post_gate_symlink_substitution_rejected',rejected_symlink)
        before_png.unlink();before_png.write_bytes(original_before)
        check('restored_unmodified_png_render_passes',module.render(module.load_pair(br,old,ar,new,OLD_GAME,OLD_ZIP,NEW_GAME,NEW_ZIP,RECEIVER)).count('data:image/png;base64,')==8)
    report={'package':'AIM-DELTA001/R2','source':'aim_delta.py','original_dependency':'AIM-OBS001/R1 GitHub Draft #349 verifier SHA256 8e6cd46bc5f1229b54e565a1b74ea9387d27073d463f61268809f212a9e2ddf4','environment':'real Chromium Linux Playwright / Python Pillow, synthetically generated original bytes','human_count':0,'unity':'NOT_RUN','tests':checks,'pass':sum(x['pass'] for x in checks),'fail':sum(not x['pass'] for x in checks)}
    (HERE/'QA_delta.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print('AIM-DELTA001',report['pass'],'/',len(checks),'PASS; human=0 Unity=NOT_RUN');return report['fail']==0

if __name__=='__main__':sys.exit(0 if run() else 1)
