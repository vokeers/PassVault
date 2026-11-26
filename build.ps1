$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDir = Join-Path $root 'src\PassVault'
$distDir = Join-Path $root 'dist\PassVault'
$releaseDir = Join-Path $root 'release'
$zipPath = Join-Path $releaseDir 'PassVault-1.3.0-win-x64-portable.zip'

$rootFull = [System.IO.Path]::GetFullPath($root)
$distFull = [System.IO.Path]::GetFullPath($distDir)
if (-not $distFull.StartsWith($rootFull + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "构建目录不在工作区内：$distFull"
}

$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) {
    $csc = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $csc)) {
    throw '找不到 .NET Framework C# 编译器 csc.exe。'
}

if (Test-Path -LiteralPath $distDir) {
    Remove-Item -LiteralPath $distDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $distDir | Out-Null
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

$sourceFiles = Get-ChildItem -LiteralPath $sourceDir -Filter '*.cs' | ForEach-Object { $_.FullName }
$exePath = Join-Path $distDir 'PassVault.exe'
$iconPath = Join-Path $sourceDir 'PassVault.ico'
$manifestPath = Join-Path $sourceDir 'app.manifest'

$cscArgs = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/debug-',
    '/codepage:65001',
    ('/out:' + $exePath),
    ('/win32icon:' + $iconPath),
    ('/win32manifest:' + $manifestPath),
    '/r:System.dll',
    '/r:System.Core.dll',
    '/r:System.Windows.Forms.dll',
    '/r:System.Drawing.dll',
    '/r:System.Runtime.Serialization.dll',
    '/r:System.Security.dll',
    '/r:System.Xml.dll'
) + $sourceFiles

& $csc $cscArgs
if ($LASTEXITCODE -ne 0) {
    throw "编译失败，退出码：$LASTEXITCODE"
}

Copy-Item -LiteralPath (Join-Path $root '使用说明.txt') -Destination (Join-Path $distDir '使用说明.txt') -Force
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination (Join-Path $distDir 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $root 'TEST-REPORT.md') -Destination (Join-Path $distDir 'TEST-REPORT.md') -Force
$docsSource = Join-Path $root 'docs'
$docsTarget = Join-Path $distDir 'docs'
if (Test-Path -LiteralPath $docsSource) {
    Copy-Item -LiteralPath $docsSource -Destination $docsTarget -Recurse -Force
}

$resultPath = Join-Path $distDir 'selftest-result.txt'
if (Test-Path -LiteralPath $resultPath) {
    Remove-Item -LiteralPath $resultPath -Force
}
$selfTest = Start-Process -FilePath $exePath -ArgumentList '--selftest' -WorkingDirectory $distDir -Wait -PassThru -WindowStyle Hidden
if ($selfTest.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $resultPath) {
        Get-Content -LiteralPath $resultPath -Encoding UTF8 | Write-Host
    }
    throw "自测失败，退出码：$($selfTest.ExitCode)"
}
$resultText = Get-Content -LiteralPath $resultPath -Raw -Encoding UTF8
if ($resultText -notmatch 'PASS ALL') {
    throw '自测未通过：缺少 PASS ALL。'
}
Remove-Item -LiteralPath $resultPath -Force

Compress-Archive -Path (Join-Path $distDir '*') -DestinationPath $zipPath -CompressionLevel Optimal -Force

$exeInfo = Get-Item -LiteralPath $exePath
$zipInfo = Get-Item -LiteralPath $zipPath
Write-Host ("编译完成：{0} ({1:N0} bytes)" -f $exeInfo.FullName, $exeInfo.Length)
Write-Host ("便携包：{0} ({1:N0} bytes)" -f $zipInfo.FullName, $zipInfo.Length)
Write-Host '自测结果：PASS'