$ErrorActionPreference='Stop'
$root=(Get-Location).Path
$refs=Get-Content 'Artifacts/Local/CSharpCompile-RewardEscape-20261108/compile.rsp'
$unity='C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Data\Managed\UnityEngine'
$paths=@(Join-Path $unity 'UnityEngine.CoreModule.dll')
foreach($line in $refs){if($line -match '^/reference:"(.+Library\\ScriptAssemblies\\[^"\\]+\.dll)"$'){$paths+=$Matches[1]}}
foreach($path in $paths){try{[void][System.Reflection.Assembly]::LoadFrom($path)}catch{}}
$game=Join-Path $root 'Artifacts\Local\CSharpCompile-RewardEscape-20261108\Assembly-CSharp.dll'
$assembly=[System.Reflection.Assembly]::LoadFrom($game)
$hud=$assembly.GetType('RoboRougelike.R1.R1RunHUD',$true)
$flags=[System.Reflection.BindingFlags]::Static -bor [System.Reflection.BindingFlags]::NonPublic
$cases=@(
 @{name='seed-empty';method='SeedInputWarning';arguments=@('',2026);expected="Błędny seed`nUżyty: 2026"},
 @{name='seed-nonnumeric';method='SeedInputWarning';arguments=@('abc',7);expected="Błędny seed`nUżyty: 7"},
 @{name='seed-overflow';method='SeedInputWarning';arguments=@('2147483648',-4);expected="Błędny seed`nUżyty: -4"},
 @{name='seed-min';method='SeedInputWarning';arguments=@('-2147483648',2026);expected=$null},
 @{name='seed-valid';method='SeedInputWarning';arguments=@('42',2026);expected=$null},
 @{name='shortcut-none';method='RewardShortcutIndex';arguments=@($false,$false,$false);expected=-1},
 @{name='shortcut-q';method='RewardShortcutIndex';arguments=@($true,$false,$false);expected=0},
 @{name='shortcut-w';method='RewardShortcutIndex';arguments=@($false,$true,$false);expected=1},
 @{name='shortcut-e';method='RewardShortcutIndex';arguments=@($false,$false,$true);expected=2},
 @{name='stack-wreck';method='CatalogStackCaption';arguments=@('Koło',5,$false,$true);expected='Koło × 5 • napraw'},
 @{name='stack-usable';method='CatalogStackCaption';arguments=@('Koło',5,$true,$true);expected='Koło × 5'},
 @{name='stack-locked';method='CatalogStackCaption';arguments=@('Koło',0,$false,$false);expected='Koło × 0 • blokada'},
 @{name='stack-empty';method='CatalogStackCaption';arguments=@('Koło',0,$false,$true);expected='Koło × 0'}
)
$results=@()
foreach($case in $cases){
 $method=$hud.GetMethod($case.method,$flags)
 if($null -eq $method){throw "Missing method: $($case.method)"}
 $actual=$method.Invoke($null,[object[]]$case.arguments)
 $ok=[object]::Equals($actual,$case.expected)
 $results+=@{name=$case.name;status=if($ok){'PASS'}else{'FAIL'};actual=$actual;expected=$case.expected}
}
$partType=$assembly.GetType('RoboRougelike.R3.R3PartState',$true)
$kindType=$assembly.GetType('RoboRougelike.R3.R3PartKind',$true)
$vectorType=[Type]::GetType('UnityEngine.Vector3Int, UnityEngine.CoreModule',$true)
$ctor=$partType.GetConstructor([Type[]]@($vectorType,$kindType,[float]))
$bonus=$ctor.Invoke([object[]]@([Activator]::CreateInstance($vectorType),[Enum]::Parse($kindType,'BonusModule'),[float]1))
$tierField=$partType.GetField('Tier');$titleField=$partType.GetField('UpgradeTitle')
$bonusMethod=$hud.GetMethod('CatalogBonusGroupName',$flags)
foreach($tier in @(2,3)){
 $tierField.SetValue($bonus,$tier);$titleField.SetValue($bonus,'Osłona')
 $actual=$bonusMethod.Invoke($null,[object[]]@($bonus));$ok=$actual -ceq 'Osłona'
 $results+=@{name="bonus-group-T$tier";status=if($ok){'PASS'}else{'FAIL'};actual=$actual;expected='Osłona'}
}
$titleField.SetValue($bonus,$null);$actual=$bonusMethod.Invoke($null,[object[]]@($bonus));$ok=$actual -ceq 'Moduł bonusu'
$results+=@{name='bonus-group-untitled';status=if($ok){'PASS'}else{'FAIL'};actual=$actual;expected='Moduł bonusu'}
$failures=@($results|Where-Object{$_.status -ne 'PASS'}).Count
$report=@{compiled_source='fa8fb66b56832f0558e393d115b34a4b8b897f66';worktree_head=(git rev-parse HEAD);assembly_sha256=(Get-FileHash $game -Algorithm SHA256).Hash;environment='PowerShell/.NET; actual compiled game assembly; no Unity Editor or Player';cases=$results;passed=$results.Count-$failures;failed=$failures}
$report|ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 'Artifacts/Local/OfflineHudPure-20261108/results.json'
"Actual compiled methods: $($results.Count-$failures)/$($results.Count) PASS; failures: $failures"
if($failures -gt 0){$results|Where-Object{$_.status -ne 'PASS'}|Format-List;exit 1}
