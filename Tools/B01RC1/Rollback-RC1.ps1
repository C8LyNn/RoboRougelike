param([Parameter(Mandatory)][string]$Target,[string]$Repo=(Get-Location).Path)
$ErrorActionPreference='Stop'
$Repo=(Resolve-Path -LiteralPath $Repo).Path
$Target=[IO.Path]::GetFullPath($Target)
if(Test-Path -LiteralPath $Target){throw 'Rollback target must be a new directory; nothing will be overwritten'}
$source=(git -C $Repo rev-parse HEAD).Trim()
$branch='rollback/b01-rc1-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
git -C $Repo merge-base --is-ancestor 8ac3a814b692a380033323b188747fe106314054 $source
if($LASTEXITCODE -ne 0){throw 'Not the RC1 integration lineage'}
git -C $Repo worktree add -b $branch $Target $source
if($LASTEXITCODE -ne 0){throw 'Cannot create isolated rollback checkout'}
foreach($pin in @('86eb7bb77e6bfc7787f781ea1ccda547b8a4f643','ebadd88320520dee6506839abd7f66a3f60ba66b','8ac3a814b692a380033323b188747fe106314054','bc46c8ad7a3ea32c7099b390bdde46b641e95588','5cf2fd81e1b163f5b5463201584180f24df220f3')){
 git -C $Target revert --no-edit $pin
 if($LASTEXITCODE -ne 0){throw 'Rollback conflict: retained in NEW checkout only; original checkout untouched'}
}
$diff=git -C $Target diff --name-only 4c9236a4cdc27814ea2e7679872ec4a524150cc8 -- Assets Packages ProjectSettings
if($LASTEXITCODE -ne 0 -or $diff){throw 'Rollback source differs from BASE authoritative game files'}
$tree=(git -C $Target rev-parse 'HEAD^{tree}').Trim();$baseTree=(git -C $Target rev-parse '4c9236a4cdc27814ea2e7679872ec4a524150cc8^{tree}').Trim()
@{status='PASS_SOURCE_ROLLBACK_NOT_A_NEW_NATIVE_RUN';source=$source;target=$Target;branch=$branch;rollbackHead=(git -C $Target rev-parse HEAD).Trim();gameFilesByteIdenticalToBase=$true;entireTreeIdenticalToBase=($tree -eq $baseTree);note='Original integration checkout and BASE/experimental ZIPs untouched; no shared branch merge, reset, force-push or branch deletion'} | ConvertTo-Json
