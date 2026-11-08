$ErrorActionPreference='Stop'
$root=(Get-Location).Path
$refs=Get-Content 'Artifacts/Local/CSharpCompile-ActivitySave-20261108/compile.rsp'
$unity='C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Data\Managed\UnityEngine'
$paths=@(Join-Path $unity 'UnityEngine.CoreModule.dll')
foreach($line in $refs){if($line -match '^/reference:"(.+Library\\ScriptAssemblies\\[^"\\]+\.dll)"$'){$paths+=$Matches[1]}}
foreach($path in $paths){try{[void][System.Reflection.Assembly]::LoadFrom($path)}catch{}}
$game=Join-Path $root 'Artifacts\Local\CSharpCompile-ActivitySave-20261108\Assembly-CSharp.dll'
$assembly=[System.Reflection.Assembly]::LoadFrom($game)
$hud=$assembly.GetType('RoboRougelike.R1.R1RunHUD',$true)
$flags=[System.Reflection.BindingFlags]::Static -bor [System.Reflection.BindingFlags]::NonPublic
$method=$hud.GetMethod('ActivitySaveResult',$flags)
if($null -eq $method){throw 'Missing method ActivitySaveResult'}
$cases=@(
 @{name='save-success';arguments=@($true,$null,'Zapisano wyprawę w garażu.');expected='Zapisano wyprawę w garażu.'},
 @{name='save-new-error';arguments=@($false,'stary','Nie zapisano wyprawy: brak miejsca.');expected='Nie zapisano wyprawy: brak miejsca.'},
 @{name='save-stale-success';arguments=@($false,'Zapisano wyprawę.','Zapisano wyprawę.');expected='Nie udało się zapisać wyprawy.'},
 @{name='save-no-message';arguments=@($false,$null,$null);expected='Nie udało się zapisać wyprawy.'},
 @{name='save-repeated-error';arguments=@($false,'Błąd','Błąd');expected='Nie udało się zapisać wyprawy.'}
)
$results=@()
foreach($case in $cases){
 $actual=$method.Invoke($null,[object[]]$case.arguments);$ok=[object]::Equals($actual,$case.expected)
 $results+=@{name=$case.name;status=if($ok){'PASS'}else{'FAIL'};actual=$actual;expected=$case.expected}
}
$failures=@($results|Where-Object{$_.status -ne 'PASS'}).Count
$report=@{compiled_source='61eb6a0da775d021c3421db1c52d9b11dcaeace8';worktree_head=(git rev-parse HEAD);assembly_sha256=(Get-FileHash $game -Algorithm SHA256).Hash;environment='PowerShell/.NET; actual compiled game assembly; no Unity Editor, Player or save I/O';cases=$results;passed=$results.Count-$failures;failed=$failures}
$report|ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 'Artifacts/Local/OfflineActivitySave-20261108/results.json'
"Actual compiled feedback method: $($results.Count-$failures)/$($results.Count) PASS; failures: $failures"
if($failures -gt 0){$results|Where-Object{$_.status -ne 'PASS'}|Format-List;exit 1}
