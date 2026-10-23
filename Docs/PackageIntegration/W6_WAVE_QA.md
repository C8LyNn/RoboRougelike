# W6 wave closure QA

**Decision: BLOCKED pending fresh full regression, Windows build and native gameplay.** No W6 release ZIP has been published yet. Frozen W5, main and develop remain unchanged.

Source: `e9ba555e6fab1798c679fe90fa4a8d0e2ad5c8de`, following EMP checkpoint `a63f8192fa5c223f47002b71c60e2b51eb49a8c3`. Drafts #21–28 are stacked per mechanic. Earlier frozen shield/PDS checkpoints depend on the shared mass/lifecycle/salvage fixes in #28 when used in the aggregate. Source kit integrity remains18/18 PASS; no Unity/package upgrade or old GUID/scene/prefab changes.

| Gate | Result | Evidence under Artifacts/Local |
|---|---|---|
| Initial full EditMode | 750/755,5 failed,native2; repaired below | 20261023T160904Z-9e90709ff77347beb6a8d57f113f75f4 |
| Affected campaign/garage/W6 regression | 55/55 PASS,native0 | W6-regression-fixes-45af3a535e0643b2beefb28a039d3b62 |
| Expired equipment resources/identity/repair and range transition | 3/3 PASS,native0 | W6-expired-resources-green-cd576b69b6554e18b8b82d84dae5e662 |
| Repository/GUID/config validator | 1141 files,0 errors | Source checkpoint validation |
| Python CI helpers | 16/16 PASS, workspace-local tempfile | W6-python-unit.log |
| Fresh full Edit/Play, Windows retail and QA | RUNNING | 20261023T165120Z-17d174630cbe4f3a8d43b151257c2886 |
| Native 720p/1080p, performance, complete extraction/launch | NOT RUN | Pending successful build |

The initial full run found two expired-weapon exceptions, two old loot-count assumptions and a reward-offer assertion. The offer case passed unchanged in isolation and in the whole affected class after the salvage correction; no reward algorithm was modified. The linear expiry test observed the real MissingReferenceException before the guard. A second RED observed shotgun ammo11 becoming−1 after its view expired; retained canonical R3 references fix resource loss and missing shield/PDS salvage. Both normal/elite expired guns and wheels remain repairable, and spent devices keep identity/resources through paid repair and copy. Test counts now include actual defense loot. No HP assertions, active AI, mandatory bonus or six-stage victory criteria were removed.

The affected regression includes six projectile-driven victories with active AI using virtual aim/fire input:227.382 seconds. Other reward/repair/EMP fixtures use explicit APIs. All are Editor automation and are separate from future native Windows player-input evidence. Filter Emp50 includes35 original EMP EditMode cases,10 integration cases and5 incidental Empty matches; it is not50 unique EMP-only cases.

Known W5 UnityPlayer C0000005 associated with UI Automation client lifetime remains unresolved; normal human Alt+F4 was previously confirmed. This wave has not yet run a fresh shutdown control and makes no fix claim. Cloud Unity CI still requires its existing activation secret; local licensed Unity is used here.

Remaining wave closure: fresh full suites/build, native mounting/use/removal and six-stage UI flow,720p/1080p performance, retail ZIP/extraction/launch, exact hash/size, then advance only the private aggregate and continue W7.
