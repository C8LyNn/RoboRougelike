# BUILD R1.3 — native receiver gate BLOCKED

Producer B01-23, PR256, exact be54b4a33360cbd1923ae5dacbc92dff8eaf586b.

Full physical ZIP138,799B, SHA256bbd604f497539451bee3ed54094132fd830e68901d11ba6fa7c5943babbebc75:55sourcefiles+manifest, all source/GUID/meta checks pass. Native Unity6000.3.14f1 compile0, original18Edit+6Play PASS, including W9-01..04. These original fixes are acknowledged.

Additional receiver test at snapshot702cb3b2eeba1661f8ca16f6ff75de76ba4ea5e4, evidence Artifacts/Local/RC2-UnifiedGREEN-BuildSafety-20261026T105808Z:2genuine BUILD FAIL,1positive control PASS. Separate unified-menu input fixture also failed because unfocused EditMode input was not pumped; this is not a BUILD defect and is corrected separately.

Exact reproduction: BuildSocketGeometry.Touches, adjacent one-cell Core/Armor at anchors(0,0,0)/(0,1,0), opposing Structural up/down sockets, same centres, support HalfSize(.48,.48). Candidate HalfSize(.02,.02), valid definitions. Actual nested intersection4*.02*.02=.0016cells², below source minimum.01cells²; expected false. Native actual true because aw+bw-du / ah+bh-dv overestimate contained intervals. Larger positive control HalfSize(.2,.2) is correctly admitted but reports.139876m² instead of its actual.048400m². Genuine source defect is in interval intersection calculation, not PhysX or an assertion adjustment.

Reproduction C# retained in Tools/B01RC2/Blocked/BuildRC2SafetyTests.cs. Original XML/logs/native exit2 retained. Do not weaken area assertions or silently use Check() as installation authority. Source fix belongs to B01-23. Complete package removed from final Assets and no BUILD activity/prefab marker/alternate mount authority admitted. Existing R3 garage/XYZ/1x2x2/HP/socket mechanics stay active and unchanged.

Future retest: fix true interval intersection for nested/unequal/offset patches, run both negative/positive receiver cases, original24native cases, then actual exact-collider registry, genuine full-broadphase ComputePenetration, rotated R3 root/XYZ/wide ownership before gameplay admission. Formal G2 unchanged.
