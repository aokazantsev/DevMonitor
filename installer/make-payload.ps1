param(
    [Parameter(Mandatory = $true)][string]$Root,
    [Parameter(Mandatory = $true)][string]$Output
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path -LiteralPath $Root).Path.TrimEnd([char]92)
$staging = Join-Path ([IO.Path]::GetTempPath()) ("DevMonitorPayload-" + [Guid]::NewGuid().ToString('N'))

$excludedDirectories = @(
    (Join-Path $Root 'data'),
    (Join-Path $Root 'installer\obj'),
    (Join-Path $Root 'dist'),
    (Join-Path $Root '.git')
)
$excludedFiles = @('settings.txt', 'position.txt', '*.old.exe', '.gitignore', '.gitattributes')

& robocopy $Root $staging /E /NFL /NDL /NJH /NJS /NP /XD $excludedDirectories /XF $excludedFiles | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed with code $LASTEXITCODE" }

foreach ($leftover in @('data', 'settings.txt', 'position.txt')) {
    if (Test-Path (Join-Path $staging $leftover)) { throw "payload still contains $leftover" }
}

New-Item -ItemType Directory -Force -Path (Split-Path $Output) | Out-Null
if (Test-Path $Output) { Remove-Item $Output -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($staging, $Output, [IO.Compression.CompressionLevel]::Optimal, $false)
Remove-Item $staging -Recurse -Force

$archive = [IO.Compression.ZipFile]::OpenRead($Output)
try {
    "payload: {0} files, {1:N1} MB" -f $archive.Entries.Count, ((Get-Item $Output).Length / 1MB)
} finally {
    $archive.Dispose()
}
