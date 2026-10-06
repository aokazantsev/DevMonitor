param([Parameter(Mandatory = $true)][string]$Root)

$Output = Join-Path $Root 'installer' | Join-Path -ChildPath 'redist' | Join-Path -ChildPath 'PawnIO_setup.exe'

$ErrorActionPreference = 'Stop'
$url = 'https://github.com/namazso/PawnIO.Setup/releases/download/2.2.0/PawnIO_setup.exe'
$sha256 = '1f519a22e47187f70a1379a48ca604981c4fcf694f4e65b734aaa74a9fba3032'

if (Test-Path -LiteralPath $Output) {
    if ((Get-FileHash -LiteralPath $Output -Algorithm SHA256).Hash -eq $sha256) { "PawnIO setup: cached"; exit 0 }
    Remove-Item -LiteralPath $Output -Force
}
New-Item -ItemType Directory -Force -Path (Split-Path $Output) | Out-Null
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
(New-Object Net.WebClient).DownloadFile($url, $Output)
$actual = (Get-FileHash -LiteralPath $Output -Algorithm SHA256).Hash
if ($actual -ne $sha256) {
    Remove-Item -LiteralPath $Output -Force
    throw "PawnIO setup checksum mismatch: $actual"
}
"PawnIO setup: downloaded 2.2.0"
