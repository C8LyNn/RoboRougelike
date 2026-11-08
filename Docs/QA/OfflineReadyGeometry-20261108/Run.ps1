$ErrorActionPreference='Stop'
$root=(Get-Location).Path
$refs=Get-Content 'Artifacts/Local/CSharpCompile-ReadyLayout-20261108/compile.rsp'
$unity='C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Data\Managed\UnityEngine'
$paths=@(Join-Path $unity 'UnityEngine.CoreModule.dll')
foreach($line in $refs){if($line -match '^/reference:"(.+Library\\ScriptAssemblies\\[^"\\]+\.dll)"$'){$paths+=$Matches[1]}}
foreach($path in $paths){try{[void][System.Reflection.Assembly]::LoadFrom($path)}catch{}}
$game=Join-Path $root 'Artifacts\Local\CSharpCompile-ReadyLayout-20261108\Assembly-CSharp.dll'
$assembly=[System.Reflection.Assembly]::LoadFrom($game)
$hud=$assembly.GetType('RoboRougelike.R1.R1RunHUD',$true)
$rectType=[Type]::GetType('UnityEngine.Rect, UnityEngine.CoreModule',$true)
$ctor=$rectType.GetConstructor([Type[]]@([float],[float],[float],[float]))
$overlaps=$rectType.GetMethod('Overlaps',[Type[]]@($rectType))
$flags=[System.Reflection.BindingFlags]::Static -bor [System.Reflection.BindingFlags]::NonPublic
function New-Rect([single]$x,[single]$y,[single]$w,[single]$h){$ctor.Invoke([object[]]@($x,$y,$w,$h))}
$messages=@()
foreach($name in @('ReadyIntroRect','ReadySaveMessageRect','ReadySeedWarningRect')){$messages+=@{name=$name;rect=$hud.GetMethod($name,$flags).Invoke($null,$null)}}
$controls=@((New-Rect 90 570 110 28),(New-Rect 200 568 230 28),(New-Rect 450 548 170 38),(New-Rect 640 548 300 38),(New-Rect 960 552 230 38),(New-Rect 90 600 500 65),(New-Rect 640 600 550 65))
$cards=@((New-Rect 80 205 360 280),(New-Rect 460 205 360 280),(New-Rect 840 205 360 280))
$results=@()
for($i=0;$i -lt $messages.Count;$i++){
 $m=$messages[$i].rect;$inside=$m.xMin -ge 0 -and $m.yMin -ge 0 -and $m.xMax -le 1280 -and $m.yMax -le 720
 $results+=@{name="$($messages[$i].name)-inside";status=if($inside){'PASS'}else{'FAIL'}}
 for($j=0;$j -lt $i;$j++){$ok=-not [bool]$overlaps.Invoke($m,[object[]]@($messages[$j].rect));$results+=@{name="$($messages[$i].name)-message-$j";status=if($ok){'PASS'}else{'FAIL'}}}
 for($j=0;$j -lt $controls.Count;$j++){$ok=-not [bool]$overlaps.Invoke($m,[object[]]@($controls[$j]));$results+=@{name="$($messages[$i].name)-control-$j";status=if($ok){'PASS'}else{'FAIL'}}}
 for($j=0;$j -lt $cards.Count;$j++){$ok=-not [bool]$overlaps.Invoke($m,[object[]]@($cards[$j]));$results+=@{name="$($messages[$i].name)-card-$j";status=if($ok){'PASS'}else{'FAIL'}}}
}
$failed=@($results|Where-Object{$_.status -ne 'PASS'}).Count
$report=@{compiled_source='38e7fe6e0144750b65b644ec1d69f35628acd85b';worktree_head=(git rev-parse HEAD);assembly_sha256=(Get-FileHash $game -Algorithm SHA256).Hash;environment='PowerShell/.NET; compiled HUD methods and Unity Rect; no Editor, Player, rendering or text measurement';passed=$results.Count-$failed;failed=$failed;cases=$results}
$report|ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 'Artifacts/Local/OfflineReadyGeometry-20261108/results.json'
"Actual HUD/Rect geometry: $($results.Count-$failed)/$($results.Count) PASS; failures: $failed"
if($failed -gt 0){$results|Where-Object{$_.status -ne 'PASS'}|Format-List;exit 1}
