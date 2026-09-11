param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('run_script', 'recompile', 'recompile_status', 'get_console_logs', 'list_open_scenes', 'capture_game_view')]
    [string]$Command,
    [string]$ParametersJson = '{}'
)

$ErrorActionPreference = 'Stop'
$ctjProject = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$ctjConnection = Get-Content -Raw -LiteralPath (Join-Path $ctjProject 'Library/Pipeline/.unity-pipeline-port') | ConvertFrom-Json
if ([IO.Path]::GetFullPath($ctjConnection.projectPath) -ne $ctjProject) {
    throw 'Unity connection does not match this project.'
}
$ctjHeaders = @{ Authorization = 'Bearer ' + $ctjConnection.evalToken }
$ctjBody = @{ command = $Command; parameters = ($ParametersJson | ConvertFrom-Json) } | ConvertTo-Json -Depth 12
$ctjResult = Invoke-RestMethod -Uri ('http://127.0.0.1:' + $ctjConnection.port + '/api/exec') -Method Post -Headers $ctjHeaders -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($ctjBody)) -TimeoutSec 60
$ctjResult | ConvertTo-Json -Depth 15
if ($ctjResult.success -eq $false -or $ctjResult.result.success -eq $false) { exit 1 }
