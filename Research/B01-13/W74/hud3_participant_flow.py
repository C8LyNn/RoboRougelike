#!/usr/bin/env python3
"""B01-13 PASS15 continuation: source-pinned genuine Chromium HUD3 session/withdrawal receiver.

This exercises B01-12's *unaltered original HTML*, not a JS mirror. Data are synthetic;
no actual human participation, Unity, or production-game controls are claimed.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
from playwright.sync_api import sync_playwright

OLD_HUD_BLOB = '1caee60184a918d1690a67e363acfc8b213bfe9b'


def git_blob(data: bytes) -> str:
    return hashlib.sha1(f'blob {len(data)}\0'.encode('ascii') + data).hexdigest()


def assert_true(value: bool, msg: str) -> None:
    if not value:
        raise AssertionError(msg)


class EphemeralOperator:
    """No storage, no consent claim; sample IDs do not identify real people."""
    def __init__(self):
        self.rows: list[dict] = []
        self.withdrawn: set[str] = set()

    def observe(self, pid: str, variant: str, source_hash: str, observation: str):
        assert_true(pid not in self.withdrawn, 'withdrawn ID reused')
        self.rows.append(dict(kind='SYNTHETIC', pid=pid, variant=variant,
                              source_blob=source_hash, observation=observation))

    def withdraw(self, pid: str):
        self.withdrawn.add(pid)
        self.rows = [r for r in self.rows if r['pid'] != pid]


def run(html: Path, expected_blob: str, out: Path) -> dict:
    original = html.read_bytes()
    actual_blob = git_blob(original)
    assert_true(actual_blob == expected_blob, f'HTML Git blob mismatch! Expected {expected_blob}, got {actual_blob}')
    out.mkdir(parents=True, exist_ok=True)
    results = {'role': 'B01-13', 'data_mode': 'SYNTHETIC_ONLY', 'ui_owner': 'B01-12',
               'tested_html_git_blob': actual_blob, 'matches_immutable_original_P14': actual_blob == OLD_HUD_BLOB, 'git_blob_pin_verified': True,
               'native_unity': 'NOT_RUN', 'real_participants': 0,
               'checks': [], 'producer_findings': []}
    checks = results['checks']
    problems = results['producer_findings']
    def check(name: str, ok: bool, severity: str = 'P1', detail: str = ''):
        entry = dict(id=name, status='PASS' if ok else 'FAIL', detail=detail)
        checks.append(entry)
        if not ok: problems.append(dict(id=name, owner='B01-12', severity=severity, detail=detail))

    tracker = EphemeralOperator()
    with sync_playwright() as pw:
        browser = pw.chromium.launch(executable_path='/usr/bin/chromium', headless=True, args=['--no-sandbox'])
        for width in (320, 720, 1280):
            ctx = browser.new_context(viewport={'width': width, 'height': 900}, accept_downloads=False)
            net = []
            page = ctx.new_page()
            page.on('request', lambda req: net.append(req.url))
            page.set_content(original.decode('utf-8'), wait_until='load')
            page.locator('#fixture').select_option('F1')
            # Independent synthetic A/B conditions, no human data and no replay from previously modified fixture.
            a_groups = None
            for variant in ('A', 'B'):
                page.locator('#fixture').select_option('F0')
                page.locator('#fixture').select_option('F1')  # explicit fixture reset
                page.locator(f'#mode{variant}').click()
                groups = page.locator('#groups').inner_text()
                if variant == 'A': a_groups = groups
                else: check(f'{width}_A_B_same_group_alerts', groups == a_groups,
                            detail='A/B treatment must not alter identical 6-gun stimulus')
                page.locator('#participant').check()
                check(f'{width}_{variant}_moderator_hidden', not page.locator('#moderator').is_visible())
                check(f'{width}_{variant}_participant_focus', page.evaluate("document.activeElement.id") == 'hudRegion')
                exposure = page.locator('#coverage').inner_text().strip()
                visible = page.locator('#coverage').is_visible()
                # Includes DOM-visible and therefore accessibility-exposed treatment assignment.
                clean_label = not (visible and exposure.startswith(f'{variant}:'))
                check(f'{width}_{variant}_participant_blind', clean_label,
                      detail=f'Participant HUD has exposed treatment label: {exposure!r}')
                check(f'{width}_{variant}_no_horizontal_scroll', page.evaluate('document.documentElement.scrollWidth<=innerWidth'))
                if width == 720: page.screenshot(path=str(out / f'HUD3_{variant}_F1_{width}_SYNTHETIC.png'), full_page=True)
                if variant == 'B':
                    button = page.locator('#expand')
                    check(f'{width}_B_expand_available', button.is_visible())
                    button.click()
                    check(f'{width}_B_expanded', button.get_attribute('aria-expanded') == 'true' and page.locator('#overflow').is_visible())
                    page.keyboard.press('Escape')
                    check(f'{width}_B_escape_clears_participant', not page.locator('body').evaluate("e=>e.classList.contains('participant')"))
                    page.locator('#participant').check()
                    check(f'{width}_B_reentry_collapses', button.get_attribute('aria-expanded') == 'false' and not page.locator('#overflow').is_visible())
                page.keyboard.press('Escape')
                check(f'{width}_{variant}_exit_restores_moderator', page.locator('#moderator').is_visible())
            # F0 has zero overflow. Help must not tell a test participant to expand absent content.
            page.locator('#fixture').select_option('F0')
            page.locator('#modeA').click()
            page.locator('#participant').check()
            help_text = page.locator('#participantHelp').inner_text().lower()
            missing_expand = not page.locator('#expand').is_visible()
            check(f'{width}_A_F0_help_truthful', not (missing_expand and ('rozwin' in help_text)),
                  detail='Participant help promises expansion but F0 A has no expansion control')
            page.keyboard.press('Escape')
            # Leave a changed session state, then re-enter *without reinitializing the same fixture*.
            page.locator('#fixture').select_option('F1')
            page.locator('#modeB').click()
            page.locator('#participant').check()
            baseline = page.locator('#groups').inner_text()
            page.keyboard.press('Escape')
            page.locator('#resolveA').click()  # facilitator demo of alert recovery between sessions
            page.locator('#participant').check()
            next_session = page.locator('#groups').inner_text()
            check(f'{width}_cross_session_fixture_resets_automatically', next_session == baseline,
                  detail='Entering new participant mode after moderator action retains mutated weapon state; reset fixture before next person')
            page.keyboard.press('Escape')
            check(f'{width}_no_network', net == [], detail=f'Network requests: {net[:2]}')
            check(f'{width}_no_web_storage', page.evaluate("(()=>{try{return localStorage.length===0 && sessionStorage.length===0}catch(e){return e.name==='SecurityError'}})()"))
            ctx.close()
        # NEW safe synthetic session integration: isolated context per participant, explicit withdrawal, no storage.
        ctx = browser.new_context(viewport={'width': 1280, 'height': 900})
        page = ctx.new_page(); page.set_content(original.decode('utf-8'))
        page.locator('#fixture').select_option('F1'); page.locator('#modeA').click(); page.locator('#participant').check()
        tracker.observe('P01', 'A', actual_blob, 'SYNTHETIC sample 1')
        assert_true(len(tracker.rows) == 1, 'P01 expected in volatile memory')
        tracker.withdraw('P01')
        check('synthetic_withdraw_removes_prior_record', not tracker.rows)
        ctx.close()  # independent browser context: no fixture or storage carry-over
        ctx = browser.new_context(viewport={'width': 1280, 'height': 900})
        page = ctx.new_page(); page.set_content(original.decode('utf-8'))
        page.locator('#fixture').select_option('F1'); page.locator('#modeB').click(); page.locator('#participant').check()
        tracker.observe('P02', 'B', actual_blob, 'SYNTHETIC sample 2')
        check('synthetic_withdraw_blocks_reentry', 'P01' in tracker.withdrawn and all(r['pid'] != 'P01' for r in tracker.rows))
        try:
            tracker.observe('P01', 'A', actual_blob, 'must be blocked')
            check('synthetic_withdraw_reimport_rejected', False)
        except AssertionError:
            check('synthetic_withdraw_reimport_rejected', True)
        check('fresh_browser_context_no_previous_session', page.locator('#groups').inner_text().count('PRZEGRZANIE') == 1 and page.locator('#groups').inner_text().count('ZACIĘCIE') == 1)
        ctx.close(); browser.close()
    results['checks_total'] = len(checks)
    results['checks_pass'] = sum(c['status'] == 'PASS' for c in checks)
    results['checks_fail'] = len(problems)
    results['research_ui_contract_pass'] = len(problems) == 0
    (out / 'HUD3_B01-13_RECEIVER_SYNTHETIC.json').write_text(json.dumps(results, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    return results


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--html', type=Path, required=True)
    p.add_argument('--expected-git-blob', default=OLD_HUD_BLOB)
    p.add_argument('--out', type=Path, required=True)
    p.add_argument('--accept-known-red', action='store_true', help='Diagnostic only: exit 0 in diagnostic mode with explicit producer FAILs; never assert human approval')
    args=p.parse_args()
    try:
        r=run(args.html, args.expected_git_blob, args.out)
        print(json.dumps({k:v for k,v in r.items() if k not in ('checks','producer_findings')}, indent=2, ensure_ascii=False))
        for x in r['producer_findings']:
            print('PRODUCER_RED', x['id'], x['detail'])
        return 0 if (r['research_ui_contract_pass'] or args.accept_known_red) else 3
    except Exception as e:
        print(f'HARD_FAIL: {type(e).__name__}: {e}')
        return 2

if __name__=='__main__':
    raise SystemExit(main())
