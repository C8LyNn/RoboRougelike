#!/usr/bin/env python3
"""Run original standalone B01-13 AIM-OBS001 HTML through real Chromium/Playwright.
All scene images & session values are SYNTHETIC. No Unity or humans.
"""
from pathlib import Path
from hashlib import sha256,sha1
from playwright.sync_api import sync_playwright
from PIL import Image,ImageDraw
from verify_aim_receipt import verify,Invalid
import tempfile,json,sys

HERE=Path(__file__).resolve().parent
APP=HERE/'aim_observer.html'
SHA=sha1(b'blob '+str(len(APP.read_bytes())).encode()+b'\0'+APP.read_bytes()).hexdigest()
TEST_SHA='682dc0c5c13615577334375ffb6c27be5cdebf4a'
ZIP_SHA='ab'*32

def run():
    checked=[]
    def check(name,cond):
        checked.append({'case':name,'pass':bool(cond)})
        if not cond: raise AssertionError(name)
    with tempfile.TemporaryDirectory() as tmp, sync_playwright() as pw:
        folder=Path(tmp)
        scenes=['CLOSE_STATIC','FAR_STATIC','OCCLUDED','ZOOM_MOVING']
        originals={}
        for i,s in enumerate(scenes):
            p=folder/(s+'.png')
            img=Image.new('RGB',(640,360),(18+i*13,44+i*8,60+i*10))
            d=ImageDraw.Draw(img)
            d.rectangle((100,60,550,300),outline='white',width=3)
            d.text((15,15),'SYNTHETIC QA IMAGE '+s,fill='white')
            d.ellipse((285,160,335,210),outline='yellow',width=3)
            img.save(p)
            originals[s]=p
        browser=pw.chromium.launch(headless=True,executable_path='/usr/bin/chromium')
        page=browser.new_page(viewport={'width':1280,'height':720},accept_downloads=True)
        errors=[];outgoing=[]
        page.on('pageerror',lambda e:errors.append(str(e)))
        page.on('request',lambda r:outgoing.append(r.url) if r.url.startswith('http') and '127.0.0.1' not in r.url else None)
        page.set_content(APP.read_text(encoding='utf-8'))
        check('real_original_git_blob_40',len(SHA)==40)
        check('initial_empty_session',page.locator('#export').is_disabled())
        check('schema_declared',page.evaluate('window.AIMOBS.SCHEMA')=='B01-13/P17/AIM-OBS001/v1')
        check('four_cases_declared',page.evaluate('window.AIMOBS.CASES.length')==4)
        page.fill('#zip',ZIP_SHA)
        page.fill('#sid','S17')
        # Cannot save without image or valid full sha.
        check('no_image_save_blocked',page.locator('#store').is_disabled())
        page.fill('#zip','xyz')
        page.locator('#imagefile').set_input_files(str(originals['CLOSE_STATIC']))
        page.wait_for_function('document.getElementById("shot").complete && window.AIMOBS && !!document.getElementById("shot").naturalWidth')
        check('image_loaded',page.locator('#shot').is_visible())
        check('image_size_native',page.locator('#measurement').inner_text().find('640×360')>=0)
        def mark(which,x,y):
            page.locator('#tool'+which).click()
            page.locator('#shot').click(position={'x':x,'y':y},force=True)
        mark('Reticle',250,150);mark('Target',275,150)
        check('two_points_allow_save',page.locator('#store').is_enabled())
        page.screenshot(path=str(HERE/'QA_synthetic_preview.png'),full_page=True)
        page.click('#store')
        check('wrong_zip_rejected',len(page.evaluate('window.AIMOBS.records'))==0)
        check('wrong_zip_error', 'SHA' in page.locator('#message').inner_text())
        page.fill('#zip',ZIP_SHA)
        page.click('#store')
        check('first_frame_saved',len(page.evaluate('window.AIMOBS.records'))==1)
        check('native_coordinates',0<=page.evaluate('window.AIMOBS.records[0].points.reticle.x')<=640)
        check('click_mapping',page.evaluate('window.AIMOBS.records[0].metrics.reticle_target_px')>0)
        check('image_sha256_correct',page.evaluate('window.AIMOBS.records[0].image.sha256')==sha256(originals['CLOSE_STATIC'].read_bytes()).hexdigest())
        check('no_screenshot_bytes_in_record',not any(k in str(page.evaluate('window.AIMOBS.records[0]')) for k in ['data:image','base64,']))
        check('lock_zip_after_first_frame',page.locator('#zip').is_disabled())
        # Repeat the first scenario, require actual negative.
        page.locator('#imagefile').set_input_files(str(originals['CLOSE_STATIC']))
        mark('Reticle',250,150);mark('Target',275,150)
        page.click('#store')
        check('duplicate_case_denied',len(page.evaluate('window.AIMOBS.records'))==1)
        check('duplicate_error_visible','Duplikat' in page.locator('#message').inner_text())
        for idx,name in enumerate(scenes[1:],1):
            page.select_option('#case',name)
            page.select_option('#group','B' if idx%2 else 'A')
            page.locator('#imagefile').set_input_files(str(originals[name]))
            page.wait_for_function('document.getElementById("shot").naturalWidth === 640')
            mark('Reticle',250,150);mark('Target',275,150)
            if idx==2:mark('Impact',300,180)
            page.select_option('#outcome', 'BLOCKED' if idx==2 else 'UNVERIFIED')
            page.click('#store')
            check('frame_'+name,len(page.evaluate('window.AIMOBS.records'))==idx+1)
        check('export_unlocked',page.locator('#export').is_enabled())
        check('input_A_is_LMB',page.evaluate('window.AIMOBS.records[0].input')=='LMB')
        check('input_B_is_RMB',page.evaluate('window.AIMOBS.records[1].input')=='RMB')
        check('impact_point_optional',page.evaluate('window.AIMOBS.records[1].points.impact') is None)
        check('impact_distance_measured',page.evaluate('window.AIMOBS.records[2].metrics.impact_target_px') is not None)
        check('outcome_not_inferred',page.evaluate('window.AIMOBS.records[0].outcome')=='UNVERIFIED')
        with page.expect_download() as ev: page.click('#export')
        dl=ev.value
        output=folder/dl.suggested_filename;dl.save_as(output)
        data=json.loads(output.read_text())
        check('export_filename',dl.suggested_filename=='AIM_OBS001_SYNTHETIC_S17.json')
        check('export_schema',data['schema']=='B01-13/P17/AIM-OBS001/v1')
        check('export_exact_game_sha',data['game_sha']==TEST_SHA)
        check('export_exact_zip_sha',data['player_zip_sha256']==ZIP_SHA)
        check('export_frames_four',len(data['frames'])==4)
        check('export_no_image_pixels',not any(v in output.read_text() for v in ['data:image','base64,','SYNTHETIC QA IMAGE']))
        check('export_image_hash',data['frames'][3]['image']['sha256']==sha256(originals['ZOOM_MOVING'].read_bytes()).hexdigest())
        check('receiver_real_images',verify(output,folder,TEST_SHA,ZIP_SHA)['cases']==4)
        def fails(name,mutate):
            original=json.loads(output.read_text())
            mutate(original)
            candidate=folder/'bad.json';candidate.write_text(json.dumps(original),encoding='utf-8')
            try:verify(candidate,folder,TEST_SHA,ZIP_SHA)
            except Invalid:check(name,True)
            else:check(name,False)
        fails('receiver_fake_game_sha_rejected',lambda x:x.update(game_sha='0'*40))
        fails('receiver_wrong_fire_group_rejected',lambda x:x['frames'][1].update(input='LMB'))
        fails('receiver_fake_image_sha_rejected',lambda x:x['frames'][0]['image'].update(sha256='0'*64))
        fails('receiver_fake_dimensions_rejected',lambda x:x['frames'][0]['image'].update(width=999))
        fails('receiver_traversal_rejected',lambda x:x['frames'][0]['image'].update(filename='../other.png'))
        fails('receiver_wrong_geometry_rejected',lambda x:x['frames'][0]['metrics'].update(reticle_target_px=777))
        fails('receiver_duplicate_case_rejected',lambda x:x['frames'][1].update(case=x['frames'][0]['case']))
        fails('receiver_forged_human_ref_rejected',lambda x:x.update(mode='HUMAN_APPROVED'))
        check('export_no_human_consent_claim',data['consent_witness'] is None)
        check('mode_no_human',data['mode']=='SYNTHETIC')
        # Withdrawal AFTER export is not magic remote purge. Local state must be wiped and locked.
        page.on('dialog',lambda d:d.accept())
        page.click('#withdraw')
        check('withdraw_in_memory_purges',page.evaluate('window.AIMOBS.records.length')==0)
        check('withdraw_tombstone_client',page.evaluate('window.AIMOBS.withdrawn') is True)
        check('withdraw_export_disabled',page.locator('#export').is_disabled())
        check('download_copy_retained_explicitly',output.is_file())
        page.click('#reset')
        check('reset_new_session',not page.evaluate('window.AIMOBS.withdrawn'))
        check('reset_unlock_build',page.locator('#zip').is_enabled())
        # HUMAN_APPROVED mode is blocked without external operator attestation refs.
        page.select_option('#mode','HUMAN_APPROVED')
        check('human_refs_visible',page.locator('#humanRefs').is_visible())
        page.locator('#imagefile').set_input_files(str(originals['CLOSE_STATIC']))
        mark('Reticle',250,150);mark('Target',275,150)
        page.click('#store')
        check('human_missing_refs_denied',len(page.evaluate('window.AIMOBS.records'))==0)
        check('human_missing_refs_error','referencji' in page.locator('#message').inner_text())
        pre_resize=page.locator('#shot').bounding_box()
        expected_point=page.evaluate('window.AIMOBS.points.reticle')
        page.set_viewport_size({'width':720,'height':720})
        check('720_no_overflow',page.evaluate('document.documentElement.scrollWidth <= window.innerWidth'))
        page.set_viewport_size({'width':320,'height':720})
        check('320_no_overflow',page.evaluate('document.documentElement.scrollWidth <= window.innerWidth'))
        page.wait_for_timeout(120)
        check('320_marker_matches_scaled_image',page.evaluate("""(p)=>{const shot=document.getElementById('shot'),marker=document.querySelector('.mark.reticle');if(!marker)return false;const b=shot.getBoundingClientRect(),m=marker.getBoundingClientRect();return Math.abs((m.left+m.width/2)-(b.left+p.x/640*b.width))<4&&Math.abs((m.top+m.height/2)-(b.top+p.y/360*b.height))<4}""",expected_point))
        check('no_js_errors',not errors)
        check('no_external_network',not outgoing)
        browser.close()
    report={'package':'AIM-OBS001/R1','original_html_git_blob':SHA,'environment':'real Linux Chromium / Playwright','human_participants':0,'unity_native':'NOT_RUN','tests':checked,'pass':sum(x['pass'] for x in checked),'fail':sum(not x['pass'] for x in checked)}
    (HERE/'QA_browser.json').write_text(json.dumps(report,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print('B01-13 AIM-OBS001: real Chromium '+str(report['pass'])+'/'+str(len(checked))+' PASS; 0 humans; Unity NOT_RUN; HTML Git blob '+SHA)
    return report['fail']==0
if __name__=='__main__': sys.exit(0 if run() else 1)
