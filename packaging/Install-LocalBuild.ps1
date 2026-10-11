#requires -Version 7.2

<#
.SYNOPSIS
Publishes this working tree and installs it for the current user.

.DESCRIPTION
Publishes the application, the CLI and SchedulerManager as self-contained Release
builds, merges them into one folder, and replaces the installed copy with it.

The new copy is built completely before the installed one is touched, so a failed
build leaves the installation as it was. The destination is replaced, not copied
over: files that a newer build no longer produces would otherwise stay behind.
Settings are stored elsewhere (%LocalAppData%\<company>\<product>) and are kept.

A copy that is running from the destination is asked to exit first and is started
again afterwards.

.EXAMPLE
packaging\Install-LocalBuild.ps1
#>
[CmdletBinding()]
param(
	[Parameter()]
	[string] $Destination = (Join-Path $env:LOCALAPPDATA "Programs\SylphyHornPlus"),

	[Parameter()]
	[ValidateSet("win-x64", "win-x86", "win-arm64")]
	[string] $RuntimeIdentifier = "win-x64",

	# Leave the application stopped after installing, even if it was running.
	[Parameter()]
	[switch] $NoStart
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$targetFramework = "net10.0-windows10.0.26100.0"
$applicationName = "SylphyHorn.exe"
$cliName = "sylphyhorn-cli.exe"
$projects = [ordered]@{
	"application" = "source/SylphyHorn/SylphyHorn.csproj"
	"cli"         = "source/SylphyHorn.Cli/SylphyHorn.Cli.csproj"
	"scheduler"   = "source/SylphyHorn.SchedulerManager/SylphyHorn.SchedulerManager.csproj"
}

$destinationPath = [System.IO.Path]::GetFullPath($Destination)
$destinationParent = Split-Path -Parent $destinationPath
$incomingPath = "$destinationPath.new"
$previousPath = "$destinationPath.old"

# Only ever replace a folder that is empty, absent, or an earlier installation.
if ((Test-Path -LiteralPath $destinationPath) -and
	(Get-ChildItem -LiteralPath $destinationPath -Force | Select-Object -First 1) -and
	-not (Test-Path -LiteralPath (Join-Path $destinationPath $applicationName) -PathType Leaf)) {
	throw "Refusing to replace '$destinationPath': it is not empty and does not contain $applicationName."
}

$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("SylphyHornPlus-install-" + [System.Guid]::NewGuid().ToString("N"))
try {
	# 1. Publish each project outside the repository, so its bin and obj folders are not touched.
	foreach ($name in $projects.Keys) {
		Write-Host "Publishing $name..."
		$output = & dotnet publish (Join-Path $repositoryRoot $projects[$name]) `
			-c Release `
			-f $targetFramework `
			-r $RuntimeIdentifier `
			--self-contained true `
			-p:Platform=AnyCPU `
			-p:RunSylphyHornPostBuild=false `
			"--artifacts-path=$(Join-Path $stagingRoot 'artifacts')" `
			"-p:PublishDir=$(Join-Path $stagingRoot $name)\" `
			-v q -nologo 2>&1
		if ($LASTEXITCODE -ne 0) {
			$output | Write-Host
			throw "Publishing $name failed with exit code $LASTEXITCODE."
		}
	}

	# 2. Merge the three outputs. They share the runtime's files, which must be identical.
	$merged = Join-Path $stagingRoot "merged"
	New-Item -ItemType Directory -Path $merged | Out-Null
	$hashes = @{}
	foreach ($name in $projects.Keys) {
		$source = Join-Path $stagingRoot $name
		foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -File) {
			$relative = $file.FullName.Substring($source.Length + 1)
			$hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
			if ($hashes.ContainsKey($relative)) {
				if ($hashes[$relative] -ne $hash) {
					throw "The published outputs disagree about '$relative'; they cannot share one folder."
				}
				continue
			}
			$hashes[$relative] = $hash
			$target = Join-Path $merged $relative
			New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
			Copy-Item -LiteralPath $file.FullName -Destination $target
		}
	}
	Write-Host "Built $($hashes.Count) files."

	# 3. Stop a copy that is running from the destination. A copy running from anywhere
	#    else (a Debug build, for instance) is left alone, and the new one is not started.
	$running = @(Get-Process -Name ([System.IO.Path]::GetFileNameWithoutExtension($applicationName)) -ErrorAction SilentlyContinue)
	$installed = @($running | Where-Object { $_.Path -and [System.IO.Path]::GetFullPath($_.Path).StartsWith($destinationPath + "\", [System.StringComparison]::OrdinalIgnoreCase) })
	$elsewhere = @($running | Where-Object { $_ -notin $installed })
	$wasRunning = $installed.Count -gt 0
	if ($wasRunning) {
		Write-Host "Stopping the installed copy..."
		$cli = Join-Path $destinationPath $cliName
		if (Test-Path -LiteralPath $cli -PathType Leaf) {
			& $cli exit | Out-Null
		}
		foreach ($process in $installed) {
			if (-not $process.WaitForExit(15000)) {
				throw "The installed copy (process $($process.Id)) did not exit. Close it and run this script again."
			}
		}
	}

	# 4. Swap the folders. The old copy is kept until the new one is in place.
	New-Item -ItemType Directory -Path $destinationParent -Force | Out-Null
	foreach ($leftover in @($incomingPath, $previousPath)) {
		if (Test-Path -LiteralPath $leftover) {
			Remove-Item -LiteralPath $leftover -Recurse -Force
		}
	}
	Copy-Item -LiteralPath $merged -Destination $incomingPath -Recurse
	$hadPrevious = Test-Path -LiteralPath $destinationPath
	if ($hadPrevious) {
		Rename-Item -LiteralPath $destinationPath -NewName (Split-Path -Leaf $previousPath)
	}
	try {
		Rename-Item -LiteralPath $incomingPath -NewName (Split-Path -Leaf $destinationPath)
	}
	catch {
		if ($hadPrevious) {
			Rename-Item -LiteralPath $previousPath -NewName (Split-Path -Leaf $destinationPath)
		}
		throw
	}
	if ($hadPrevious) {
		Remove-Item -LiteralPath $previousPath -Recurse -Force
	}
	Write-Host "Installed to $destinationPath"

	# 5. Start it again if it was running before.
	if ($NoStart) {
		Write-Host "Not started (-NoStart)."
	}
	elseif ($elsewhere.Count -gt 0) {
		Write-Warning "Another copy is running from $($elsewhere[0].Path). The installed copy was not started."
	}
	elseif ($wasRunning) {
		Start-Process -FilePath (Join-Path $destinationPath $applicationName) -WorkingDirectory $destinationPath
		Write-Host "Started."
	}
	else {
		Write-Host "Not started, because it was not running before."
	}
}
finally {
	if (Test-Path -LiteralPath $stagingRoot) {
		Remove-Item -LiteralPath $stagingRoot -Recurse -Force -ErrorAction SilentlyContinue
	}
}
