param([switch]$RunIntegration)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $csc)) { throw '.NET Framework C# compiler not found. Windows with .NET Framework 4.8 is required.' }
New-Item -ItemType Directory -Path 'build','build\tests' -Force | Out-Null
function Compile([string[]]$CompilerArgs) { & $csc /nologo /codepage:65001 /optimize+ @CompilerArgs; if ($LASTEXITCODE -ne 0) { throw 'C# compilation failed.' } }
function RunTest([string]$Exe, [string[]]$TestArgs = @()) { $options = @{FilePath=(Resolve-Path $Exe).Path; Wait=$true; PassThru=$true; NoNewWindow=$true}; if ($TestArgs.Count -gt 0) { $options.ArgumentList=$TestArgs }; $process=Start-Process @options; if ($process.ExitCode -ne 0) { throw "Test failed: $Exe (exit $($process.ExitCode))" } }
Compile @('/target:winexe','/out:build\PCInternetKontrolu.exe','/win32icon:assets\icon.ico','/win32manifest:src\app.manifest','/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:Microsoft.CSharp.dll','src\App.cs','src\Core.cs','src\Localization.cs','src\Models.cs','src\Manager.cs','src\WindowsBackend.cs','src\AssemblyInfo.cs')
Compile @('/out:build\tests\CoreTests.exe','src\Core.cs','src\Localization.cs','src\Models.cs','tests\CoreTests.cs')
Compile @('/out:build\tests\ManagerTests.exe','src\Core.cs','src\Localization.cs','src\Models.cs','src\Manager.cs','tests\ManagerTests.cs')
Compile @('/out:build\tests\WindowsBackendTests.exe','/r:Microsoft.CSharp.dll','src\Core.cs','src\Localization.cs','src\Models.cs','src\Manager.cs','src\WindowsBackend.cs','tests\WindowsBackendTests.cs')
Compile @('/out:build\tests\LanguageTests.exe','src\Core.cs','src\Localization.cs','src\Models.cs','src\Manager.cs','tests\LanguageTests.cs')
RunTest '.\build\tests\LanguageTests.exe'
RunTest '.\build\tests\CoreTests.exe'
RunTest '.\build\tests\ManagerTests.exe'
RunTest '.\build\tests\WindowsBackendTests.exe'
RunTest '.\build\PCInternetKontrolu.exe' @('--smoke')
$report = Join-Path $env:LOCALAPPDATA 'PCInternetKontrolu\ui-test.txt'
if (-not (Test-Path $report) -or -not ((Get-Content $report -Raw).StartsWith('PASS '))) { throw 'UI smoke report failed.' }
Copy-Item (Join-Path $env:LOCALAPPDATA 'PCInternetKontrolu\ui-preview.png') 'docs\screenshot.png' -Force
if ($RunIntegration) {
 Compile @('/out:build\tests\FirewallProbe.exe','tests\FirewallProbe.cs')
 Compile @('/out:build\tests\FirewallIntegrationTest.exe','/r:Microsoft.CSharp.dll','src\Core.cs','src\Localization.cs','src\Models.cs','src\Manager.cs','src\WindowsBackend.cs','tests\FirewallIntegrationTest.cs')
 RunTest '.\build\tests\FirewallIntegrationTest.exe'
}
Write-Output 'BUILD AND SAFE TESTS PASSED. Integration changes only the dedicated test probe when explicitly requested.'
