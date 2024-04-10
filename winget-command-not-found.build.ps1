#
# To build, make sure you've installed InvokeBuild
#   Install-Module -Repository PowerShellGallery -Name InvokeBuild -RequiredVersion 3.1.0
#
# Then:
#   Invoke-Build
#
# Or:
#   Invoke-Build -Task ZipRelease
#
# Or:
#   Invoke-Build -Configuration Debug
#
# etc.
#

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = (property Configuration Release),

    [ValidateSet("net8.0")]
    [string]$Framework,

    [switch]$CheckHelpContent
)

Import-Module "$PSScriptRoot/tools/helper.psm1"

# Final bits to release go here
$targetDir = "bin/$Configuration/winget-command-not-found"

if (-not $Framework)
{
    $Framework = "net8.0"
}

Write-Verbose "Building for '$Framework'" -Verbose

function ConvertTo-CRLF([string] $text) {
    $text.Replace("`r`n","`n").Replace("`n","`r`n")
}

$binaryModuleParams = @{
    Inputs  = { Get-ChildItem src/*.cs, src/winget-command-not-found.csproj }
    Outputs = "src/bin/$Configuration/$Framework/winget-command-not-found.dll"
}

<#
Synopsis: Build main binary module
#>
task BuildMainModule @binaryModuleParams {
    exec { dotnet publish -f $Framework -c $Configuration winget-command-not-found }
}

<#
Synopsis: Copy all of the files that belong in the module to one place in the layout for installation
#>
task LayoutModule BuildMainModule, {
    if (-not (Test-Path $targetDir -PathType Container)) {
        New-Item $targetDir -ItemType Directory -Force > $null
    }

    $extraFiles = @('License.txt')

    foreach ($file in $extraFiles) {
        # ensure files have \r\n line endings as the signing tool only uses those endings to avoid mixed endings
        $content = Get-Content -Path $file -Raw
        Set-Content -Path (Join-Path $targetDir (Split-Path $file -Leaf)) -Value (ConvertTo-CRLF $content) -Force
    }

    $binPath = "winget-command-not-found/bin/$Configuration/$Framework/publish"
    Copy-Item $binPath/winget-command-not-found.dll $targetDir
    Copy-Item $binPath/ValidateOS.psm1 $targetDir

    if ($Configuration -eq 'Debug') {
        Copy-Item $binPath/*.pdb $targetDir
    }

    # Copy module manifest, but fix the version to match what we've specified in the binary module.
    $moduleManifestContent = ConvertTo-CRLF (Get-Content -Path 'src/winget-command-not-found.psd1' -Raw)
    $versionInfo = (Get-ChildItem -Path $targetDir/winget-command-not-found.dll).VersionInfo
    $version = $versionInfo.FileVersion
    $semVer = $versionInfo.ProductVersion

    # dotnet build may add the Git commit hash to the 'ProductVersion' attribute with this format: +<commit-hash>.
    if ($semVer -match "(.*)-([^\+]*)(?:\+.*)?") {
        # Make sure versions match
        if ($matches[1] -ne $version) { throw "AssemblyFileVersion mismatch with AssemblyInformationalVersion" }
        $prerelease = $matches[2]

        # Put the prerelease tag in private data, along with the project URI.
        $privateDataSection = "PrivateData = @{ PSData = @{ Prerelease = '$prerelease'; ProjectUri = 'https://github.com/Microsoft/winget-command-not-found' } }"
    } else {
        # Put the project URI in private data.
        $privateDataSection = "PrivateData = @{ PSData = @{ ProjectUri = 'https://github.com/Microsoft/winget-command-not-found' } }"
    }

    $moduleManifestContent = [regex]::Replace($moduleManifestContent, "}", "${privateDataSection}$([System.Environment]::Newline)}")
    $moduleManifestContent = [regex]::Replace($moduleManifestContent, "ModuleVersion = '.*'", "ModuleVersion = '$version'")
    $moduleManifestContent | Set-Content -Path $targetDir/winget-command-not-found.psd1

    # Make sure we don't ship any read-only files
    foreach ($file in (Get-ChildItem -Recurse -File $targetDir)) {
        $file.IsReadOnly = $false
    }
}

<#
Synopsis: Zip up the binary for release.
#>
task ZipRelease LayoutModule, {
    Compress-Archive -Force -LiteralPath $targetDir -DestinationPath "bin/$Configuration/winget-command-not-found.zip"
}

<#
Synopsis: Install newly built winget-command-not-found
#>
task Install LayoutModule, {

    function Install($InstallDir) {
        if (!(Test-Path -Path $InstallDir))
        {
            New-Item -ItemType Directory -Force $InstallDir
        }

        try
        {
            if (Test-Path -Path $InstallDir\winget-command-not-found)
            {
                Remove-Item -Recurse -Force $InstallDir\winget-command-not-found -ErrorAction Stop
            }
            Copy-Item -Recurse $targetDir $InstallDir
        }
        catch
        {
            Write-Error -Message "Can't install, module is probably in use."
        }
    }

    Install "$HOME\Documents\PowerShell\Modules"
}

<#
Synopsis: Publish to PSGallery
#>
task Publish -If ($Configuration -eq 'Release') {

    $binDir = "$PSScriptRoot/bin/Release/winget-command-not-found"

    # Check signatures before publishing
    Get-ChildItem -Recurse $binDir -Include "*.dll","*.ps*1" | Get-AuthenticodeSignature | ForEach-Object {
        if ($_.Status -ne 'Valid') {
            throw "$($_.Path) is not signed"
        }
        if ($_.SignerCertificate.Subject -notmatch 'CN=Microsoft Corporation.*') {
            throw "$($_.Path) is not signed with a Microsoft signature"
        }
    }

    # Check newlines in signed files before publishing
    Get-ChildItem -Recurse $binDir -Include "*.ps*1" | Get-AuthenticodeSignature | ForEach-Object {
        $lines = (Get-Content $_.Path | Measure-Object).Count
        $fileBytes = [System.IO.File]::ReadAllBytes($_.Path)
        $toMatch = ($fileBytes | ForEach-Object { "{0:X2}" -f $_ }) -join ';'
        $crlf = ([regex]::Matches($toMatch, ";0D;0A") | Measure-Object).Count

        if ($lines -ne $crlf) {
            throw "$($_.Path) appears to have mixed newlines"
        }
    }

    $manifest = Import-PowerShellDataFile $binDir/winget-command-not-found.psd1

    $version = $manifest.ModuleVersion
    if ($null -ne $manifest.PrivateData)
    {
        $psdata = $manifest.PrivateData['PSData']
        if ($null -ne $psdata)
        {
            $prerelease = $psdata['Prerelease']
            if ($null -ne $prerelease)
            {
                $version = $version + '-' + $prerelease
            }
        }
    }

    $yes = Read-Host "Publish version $version (y/n)"

    if ($yes -ne 'y') { throw "Publish aborted" }

    $nugetApiKey = Read-Host -AsSecureString "Nuget api key for PSGallery"

    $publishParams = @{
        Path = $binDir
        NuGetApiKey = [PSCredential]::new("user", $nugetApiKey).GetNetworkCredential().Password
        Repository = "PSGallery"
        ReleaseNotes = (Get-Content -Raw $binDir/Changes.txt)
        ProjectUri = 'https://github.com/Microsoft/winget-command-not-found'
    }

    Publish-Module @publishParams
}

<#
Synopsis: Remove temporary items.
#>
task Clean {
    git clean -fdx
}

<#
Synopsis: Default build rule - build and create module layout
#>
task . LayoutModule
