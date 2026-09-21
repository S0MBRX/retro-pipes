$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$destination = Split-Path -Parent $PSScriptRoot
$source = Join-Path $PSScriptRoot 'Pipes.cs'
$icon = Join-Path $PSScriptRoot 'Pipes.ico'
$exe = Join-Path $destination 'RetroPipes.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ "/out:$exe" "/win32icon:$icon" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.dll $source
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item -LiteralPath $exe -Destination (Join-Path $destination 'RetroPipes.scr') -Force
Write-Output "Built $exe and RetroPipes.scr"
