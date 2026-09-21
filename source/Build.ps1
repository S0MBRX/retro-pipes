$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$destination = Split-Path -Parent $PSScriptRoot
$source = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | Where-Object Name -ne 'MakeIcon.cs' | ForEach-Object FullName)
$icon = Join-Path $PSScriptRoot 'Pipes.ico'
$exe = Join-Path $destination 'RetroPipes.exe'
$notices = Join-Path $destination 'THIRD-PARTY-NOTICES.txt'
$wordmark = Join-Path $destination 'assets\Windows-XP-wordmark.png'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ "/out:$exe" "/win32icon:$icon" "/resource:$notices,RetroPipes.ThirdPartyNotices" "/resource:$wordmark,RetroPipes.WindowsXPWordmark" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.dll $source
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item -LiteralPath $exe -Destination (Join-Path $destination 'RetroPipes.scr') -Force
Write-Output "Built $exe and RetroPipes.scr"
