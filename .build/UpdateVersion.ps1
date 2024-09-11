<#
.SYNOPSIS
    Script to updating project version.
.DESCRIPTION
    Script will update version for all csharp projects.
.PARAMETER mode
    Specify a value for the version
.EXAMPLE
    UpdateVersion.ps1 "1.2.3.4"
#>

[cmdletbinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$version
)

$projectFiles = Get-ChildItem -Path $PSScriptRoot/../*.csproj -Recurse -Force

foreach ($file in $projectFiles) {
    Write-Host "Found project file:" $file.Name

    $xml = [xml](Get-Content $file)
    [bool]$updated = $false

    $xml.GetElementsByTagName("AssemblyVersion") | ForEach-Object{
        Write-Host "Updating AssemblyVersion to:" $version
        $_."#text" = $version

        $updated = $true
    }

    $xml.GetElementsByTagName("FileVersion") | ForEach-Object{
        Write-Host "Updating FileVersion to:" $version
        $_."#text" = $version

        $updated = $true
    }

    if ($updated) {
        Write-Host "Project file saved"
        $xml.Save($file.FullName)
    } else {
        Write-Host "Neither 'AssemblyVersion' nor 'FileVersion' properties were found in the project file."
    }
}