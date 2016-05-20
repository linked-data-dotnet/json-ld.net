param (
    [ValidateSet("debug", "release")]
    [string]$Configuration = 'release'
    )

$RepoRoot = $PSScriptRoot

$ArtifactsDir = Join-Path $RepoRoot 'artifacts'
$CLIRoot = Join-Path $RepoRoot 'cli'
$DotNetExe = Join-Path $CLIRoot 'dotnet.exe'
$NuGetExe = Join-Path $RepoRoot '.nuget\nuget.exe'

rm -r $ArtifactsDir -Force | Out-Null

New-Item -ItemType Directory -Force -Path $CLIRoot | Out-Null
New-Item -ItemType Directory -Force -Path $ArtifactsDir | Out-Null

if (-not (Test-Path $NuGetExe))
{
    wget https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile $NuGetExe
}

& $NuGetExe restore (Join-Path $RepoRoot '.nuget\packages.config') -SolutionDirectory $RepoRoot

# install dotnet CLI
$env:DOTNET_HOME=$CLIRoot

$installDotnet = Join-Path $CLIRoot "install.ps1"
$env:DOTNET_INSTALL_DIR=$NuGetClientRoot

New-Item -ItemType Directory -Force -Path $CLIRoot

wget https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/install.ps1 -OutFile cli/install.ps1

& cli/install.ps1 -Channel beta -i $CLIRoot -Version 1.0.0-preview1-002702

if (-not (Test-Path $DotNetExe)) {
    Write-Host "Unable to find dotnet.exe. The CLI install may have failed."
    Exit 1
}

# Display build info
& $DotNetExe --info


# download nuget packages
& $DotNetExe restore $RepoRoot

# Run tests
$TestDir = Join-Path $RepoRoot test\json-ld.net.tests
pushd $TestDir

# core clr

& $DotNetExe test --configuration $Configuration -f netcoreapp1.0

if (-not $?) {
    Error-Log "Tests failed!!!"
    Exit 1
}

# net46
& $DotNetExe build --configuration $Configuration -f net46 --runtime win7-x64

$xunit = Join-Path $RepoRoot packages\xunit.runner.console.2.1.0\tools\xunit.console.x86.exe

& $xunit bin\release\net46\win7-x64\json-ld.net.tests.dll -html (Join-Path $ArtifactsDir "testresults.html")

if (-not $?) {
    Write-Host "Tests failed!!!"
    Exit 1
}

popd

# Pack

Write-Host "Creating nupkg"

& $DotNetExe pack (Join-Path $RepoRoot src\json-ld.net) --configuration $Configuration --output $ArtifactsDir

Write-Host "Success!"