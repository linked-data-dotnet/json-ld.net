nuget restore .\.nuget\packages.config -o packages

$config = $env:CONFIGURATION
if(!$config) {
    $config = "Debug"
}
$xunitDir = dir $PsScriptRoot\packages\xunit.runners.* | select -first 1
$xunit = Join-Path $xunitDir.FullName "tools\xunit.console.exe"
$asm = Convert-Path "$PsScriptRoot\tests\JsonLD.Test\bin\$config\JsonLD.Test.dll"

$testStartedRegex = [regex]"^##teamcity\[testStarted name='(?<name>[^']*)' flowId='(?<flowId>[^']*)'\]$"
$testFinishedRegex = [regex]"^##teamcity\[testFinished name='(?<name>[^']*)' duration='(?<duration>[^']*)' flowId='(?<flowId>[^']*)'\]$"

& $xunit $asm -appveyor