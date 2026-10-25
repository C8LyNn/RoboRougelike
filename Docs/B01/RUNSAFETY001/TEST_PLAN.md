# RUNSAFETY001 required test plan
## Author-defined EditMode NUnit (NOT EXECUTED)
Zero HP last weapon, last drive/both and disconnected bridge; 0 scrap and only one free order; paid quote less than buy; repeated paid attempts do not debit twice; pending cannot be mounted; duplicate part IDs fail closed; invalid cost; 12x/20x Garage re-entry without resetting free slot; defeat does not advance win serial, keeps queue; next real win completes exactly queued earlier order, no auto-mount; second win permits next free slot; repeated/incorrect victory events blocked; normal CanDeploy doesn't spawn loaner; no alive Core, unrelated blockers, exhausted challenge and contradictory CanDeploy don't bypass. Check real Core loss still terminates per R1.
## Authored PlayMode UnityTest (NOT EXECUTED)
Original 0HP gun/wheel remain unchanged while Rigidbody primitive loaner temporarily exists on physical floor; one real event demo marks victory, lease object destroyed and original ID unchanged. Defeat preserves queue and prevents replayed defeat→win. Healthy robot doesn't get lease. Record Unity 6000.3.14f1 EditMode/PlayMode TEST RESULTS XML, Editor.log, exit code, test count, exact SHA.
## Extra QA / negative before G2
Restore normal R1 defeat/real run-end via real Codex target ONLY after approved integration; last core loss, zero funds, both side wheels, no gun, hull disconnect, out-of-challenges, idle UI, pause, retry, duplicate handlers, save reload, seed change, counterfeit victory event, zero damage 0HP queue, no paid purchase after zero wallet; partial HP instant repair unaffected by ECON.
## Commands (local licensed Windows operator)
`Unity.exe -batchmode -nographics -projectPath <DISPOSABLE_CHECKOUT> -runTests -testPlatform EditMode -testResults run_safety_edit.xml -logFile run_safety_edit.log -quit`
`Unity.exe -batchmode -nographics -projectPath <DISPOSABLE_CHECKOUT> -runTests -testPlatform PlayMode -testResults run_safety_play.xml -logFile run_safety_play.log -quit`
Document ACTUAL exits + NUnit XML; no inference from authored test code.
## QA roles
B01-60 independent exact-SHA tests and adversarial exploit review; B01-40 import/GUID/SHA/target ACK. If failed B01-20 performs minimal isolated R1.1 and retest new SHA. If no Unity available mark NOT_RUN/BLOCKED. No G2/receiver integration until POLICY HQ GO and independent PASS.
