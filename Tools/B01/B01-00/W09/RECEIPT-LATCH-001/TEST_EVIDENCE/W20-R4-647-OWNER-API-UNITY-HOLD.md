# B01-00 / PASS17 W20 — ONE R4 owner freeze, source security HOLD, canonical API and safe Windows Unity

**Authority:** HQ [#668](https://github.com/C8LyNn/RoboRougelike/issues/668), user-approved NEW canonical API [#666](https://github.com/C8LyNn/RoboRougelike/issues/666), user-approved manually opted-in local Windows Unity [#667](https://github.com/C8LyNn/RoboRougelike/issues/667). Codex unavailable. This is B01-00's independently owned **source-only receipt validator and coordination runbook**, not an actual Save/Load game adapter or Unity runner.

## Sole frozen R4 owner decision and ORIGINAL sources

B01-23 [signed explicit winner comment on #647](https://github.com/C8LyNn/RoboRougelike/pull/647#issuecomment-6531727970) selects exactly [#647](https://github.com/C8LyNn/RoboRougelike/pull/647) **R4 immutable original SHA `09c1809b73324a6d31c13e5d0143de62ac1e3ca6`**. [#652](https://github.com/C8LyNn/RoboRougelike/pull/652) original `ae5dbfa5e9c4689d2bf80fcabf8438e9c673c419` **ALT_HOLD, never mix/install**. Older R3 #644 and R2 #604 remain ALT_HOLD. Original canonical game #510 `d94400d95d13db7f27e1533df05c9292d5380431`, Save schema #556 `33094f154c7b91119b36d58aab285e6e83c5de57`, R2 #596 `efb2b06eafedab5469b57f163152d96aca3c5915`, R3 #642 `22cec9888047994ae523cbe7bf1b0a483d64cec4`, all **FROZEN**.

| Original evidence | Selected #647 (only source approved for independent receiver) | #652 (ALT_HOLD) |
| --- | --- | --- |
| Original producer Git SHA | `09c1809b73324a6d31c13e5d0143de62ac1e3ca6` | `ae5dbfa5e9c4689d2bf80fcabf8438e9c673c419` |
| Actual dual-OS producer source CI | [#41110483164](https://github.com/C8LyNn/RoboRougelike/actions/runs/41110483164) success | [#41110778985](https://github.com/C8LyNn/RoboRougelike/actions/runs/41110778985) success |
| Original INNER Git ZIP, not outer Actions artifact | 105,099 B / 23 entries / SHA256 `9ae2919e9cfdea1bf75f785b1cdfc2d3465a6a90c7d18ff22def69cb53406d30` | 111,519 B / 25 entries / SHA256 `cf9412e2f34fadeb75e028387eb5869c4444c1b9e8a3b88e7c6f40bba7f37f9e` |
| Historical independent lower-layer security | B01-61 [#664](https://github.com/C8LyNn/RoboRougelike/pull/664) [CI #41112337448](https://github.com/C8LyNn/RoboRougelike/actions/runs/41112337448), 46/46 original C# core + **synthetic** Unity host assertions EACH OS PASS; B01-62 [#662](https://github.com/C8LyNn/RoboRougelike/pull/662) [CI #41112235790](https://github.com/C8LyNn/RoboRougelike/actions/runs/41112235790) 97/97 original Git ZIP+real disk-source each OS PASS | No equivalent accepted exact-#652 independent QA; original source OnDisable release risk |
| Legal source promotion | **SINGLE OWNER LINEAGE SELECTED ONLY; HOST SECURITY HOLD** | **ALT_HOLD / DO NOT INSTALL** |

## W20 independent public host security P0, and distinct receiver G0

**New relevant R4 #647 P0 witness [B01-61 #670](https://github.com/C8LyNn/RoboRougelike/pull/670)**: real original producer host+codec+ledger C# compiled with *test-only simulated UnityEngine MonoBehaviour lifecycle*, same-slot original OS FileShare.None lease. Calling **component-only Destroy(hostComponent)**, *not Destroy(root GameObject)* leaves the old Light world Rigidbody children alive while R4 `OnDestroy` releases the slot lease. A second original host can legally acquire the slot and restore its Heavy world, exposing two conflicting active physical world hierarchies and a Save from the second authority. First source [CI #41114875911](https://github.com/C8LyNn/RoboRougelike/actions/runs/41114875911) Ubuntu **17/17 witness assertions PASS**, meaning **SOURCE RELEASE RED**, not approved release. [Follow-up exact-head #41115043435](https://github.com/C8LyNn/RoboRougelike/actions/runs/41115043435) FAILED on its own QA-only parent-HEAD scope check `BASE_CHANGED` **before** full C# replay; must repair independently and rerun exact HEAD cross-OS. Do **not** claim final dual-OS #670 PASS or that genuine Unity/PhysX exhibited this bug. Independent historical #664 OnDisable GREEN remains valid **for that narrower scope** but does not certify component-only Destroy. B01-23 must author **NEW separate versioned #647 descendant** to safely retire owned physical world before owner lease can be released. Then B01-61/41 repeat exact new-SHA independent source and real native test.

B01-40 [#672](https://github.com/C8LyNn/RoboRougelike/pull/672) actually checked selected original #647 source ZIP/GUID/YAML/script **transport** and rejects #652 swapped files even when Unity serialized .meta GUIDs match (source exact SHA/bytes required). Independent original Windows and Ubuntu [Actions #41115235535](https://github.com/C8LyNn/RoboRougelike/actions/runs/41115235535) PASS physical safe scratch receiver and no foreign empty dirs/symlinks/dirty files; **FORMAL R4 G0 HOLD** while new public host security P0 persists. Old #596 accepted G0 SOURCE_ONLY never transfers to corrected new SHA. B01-40/62 sign off independently only on exact new owner-v5 source, never on combined R4s.

## Owner-APPROVED new API #666 (actual implementation delegated to B01-23/62/20)

**Development authorized, integration/release NOT.** New own-scoped versioned API, not patch frozen #510: quiescent main-thread capture of original private `Rig[]/AtomicDockLedger`; stable owner/run/part/pivot/parent, Rigidbody pose and velocities, detached/consumed IDs; staged private cold-world+ledger hydrate offstage; post-activation validation; single-slot crash-consistent disk Save+checkpoint journal with lost ACK recovery, CAS generations; fail-closed atomic publish/rollback/quarantine preserving old world; no reflection or private source hacks, no softlocks. Check real new PR@SHA before marking implemented; **B01-00's own offline gating does NOT implement that API**. Source QA/physical G0/native G1/real canonical integration are separate.

## Owner-APPROVED local Windows Unity #667 (actual runner owned by B01-60)

**The user has approved preparing a local, operator-mediated runner**, but ChatGPT does not have Windows terminal, SSH or license credentials. No PC accessed. B01-60/41/40 prepares an isolated one-command PowerShell runner using locally activated Unity **6000.3.14f1**, disposable noncanonical checkout, user confirmation, source SHA pins, exclusive per-test checkpoint directories. It must emit *actual* NUnit XML, Editor.log, Unity exit code, executed case IDs and file hashes; no credentials in chat, privilege, auto service, public self-hosted untrusted PR runner, foreign directory removal. Until operator runs and independently verifies artifacts, **NATIVE_UNITY_G1=NOT_RUN** (not PASS).

## Owner economy hypothesis and measured integration

Owner approved **temporary test hypothesis** one globally owned free rebuild per authenticated unique victory epoch, next repair in same epoch paid; owner service duplication and stale victory replay must be denied, single price tunable. B01-22/10/13 implement/version, run scenario simulations and actual playtests; original #252 frozen. **B01-00 has not implemented economy mechanic or performed human sessions**; true cost/retention/win outcomes and participant counts unavailable. No permanent balance decision yet.

Codex quota exhausted: **no external integration verdict, ACK, retries or elapsed time**, so these stay `NOT_MEASURED/null`. Maintain six distinct states (IMPLEMENTED/TESTED/QA_ACCEPTED/READY_FOR_INTEGRATOR/INTEGRATED/RELEASED). Source-only passing our receipt gate means only internally consistent HOLD; it cannot authenticate GitHub owners, production ZIP bytes or Unity activation.

## Executable new B01-00 W20 own-scoped package

`r4_w20_intake.py` validates owner-signed #647 winner and #652 ALT, frozen parent SHA, producer original ZIP SHA/count, both original prior independent QA source scopes, new #670 P0 witness, NEW API authorization **not implementation**, B01-60 manual runner **not run**, economy temporary hypothesis and absence of real Codex feedback. `r4_w20_pending.json` records external GitHub evidence; `test_r4_w20_intake.py` includes **90 actual positive and adversarial executable source cases**, so previous original W19 533 cases + 90 = **623**. It refuses fake G0/G1, unstaged canonical game API, fabricated player sessions and ACK, unauthorized SSH/manual run, auto merge, fake 0 regression, and overwrites foreign output.

```sh
python -m unittest discover -s Tools/B01/B01-00/W09/RECEIPT-LATCH-001 -p "test_*.py" -v
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/r4_w20_intake.py Tools/B01/B01-00/W09/RECEIPT-LATCH-001/r4_w20_pending.json
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/r4_w20_intake.py Tools/B01/B01-00/W09/RECEIPT-LATCH-001/r4_w20_pending.json --require-release
```
Valid receipt exit **0** means HOLD and outputs `HOLD_SELECTED_647_COMPONENT_DESTROY_P0_RED`. Strict release exits **3**, invalid forged statuses exit **2**. Any original GitHub Actions W20 dual-OS source PASS must be *inspected on exact source/manifest final HEAD*, not inferred from historical W19.

**Handoffs:** B01-23 owner source-only v5 correction + new canonical API, B01-61/41 independent exact new core/host+owner lease/ID/rollback source QA, B01-40/62 exact new-source physical original ZIP/foreign EMPTY safe G0, B01-60 real local Unity G1 runner/user manual result, B01-22/10/13 economy simulation, B01-00 future single source-only #79 handoff **only after all relevant gates pass**. No protected merges, no frozen-source/other owner edits, rollback only own unmerged B01-00 files and own disposable evidence.
