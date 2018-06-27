##$templateRootDirectory = "C:\Source\_SdkProjectTemplate"
$templateRootDirectory = $PSScriptRoot
$replaceRootDirectory = "C:\Source\"

# =================== CONSTANTS ========================

$deleteDirectoryName = "delete"
$createDirectoryName = "create"
$replaceDirectoryName = "replace"

# =================== INITIALIZATION ========================

Write-Host "Searching for template files in '$($templateRootDirectory)'"

$deleteTemplateFiles = Get-ChildItem "$($templateRootDirectory)\$($deleteDirectoryName)\" -Recurse -File
$createTemplateFiles = Get-ChildItem "$($templateRootDirectory)\$($createDirectoryName)\" -Recurse -File
$replaceTemplateFiles = Get-ChildItem "$($templateRootDirectory)\$($replaceDirectoryName)\" -Recurse -File

Write-Host "Found '$($deleteTemplateFiles.Count)' delete templates";
Write-Host "Found '$($createTemplateFiles.Count)' create templates";
Write-Host "Found '$($replaceTemplateFiles.Count)' replace templates";

Write-Host "Searching for repositories in '$($replaceRootDirectory)'"

$gitRepositories = Get-ChildItem "$($replaceRootDirectory)\" -Filter ".git" -Hidden -Recurse -Directory -Depth 2

Write-Host "Found '$($gitRepositories.Count)' potential repositories"

foreach ($potentialGitRepository in $gitRepositories)
{
    if ($potentialGitRepository -eq $templateRootDirectory)
    {
        Write-Host "Skipping '$($potentialGitRepository)' since it's the current template repository"
        continue
    }

    $gitRepositoryDirectory = $potentialGitRepository.Parent.FullName;

    Write-Host "Updating files for repository '$($gitRepositoryDirectory)'"

    # Delete
    foreach ($fileToDelete in $deleteTemplateFiles)
    {
        $relativeFileName = $fileToDelete.FullName.Replace("$($templateRootDirectory)\$($deleteDirectoryName)", "")
        $targetFileName = Join-Path $gitRepositoryDirectory $relativeFileName
        
        # Write-Host "$($fileToDelete.FullName) => $($targetFileName))"

        # Only delete if exists
        if (![System.IO.File]::Exists($targetFileName))
        {
            continue
        }

        Write-Host "Deleting '$($targetFileName)'"

        Write-Host "[DELETING IS DISABLED, UNCOMMENT THE NEXT LINE IN THE SCRIPT TO ENABLE DELETING, USE AT YOUR OWN RISK]"
        # Delete-Item -Path $fileToCreate.FullName -Force
    }

    # Create
    foreach ($fileToCreate in $createTemplateFiles)
    {
        $relativeFileName = $fileToCreate.FullName.Replace("$($templateRootDirectory)\$($createDirectoryName)", "")
        $targetFileName = Join-Path $gitRepositoryDirectory $relativeFileName
        $targetDirectoryName = [System.IO.Path]::GetDirectoryName($targetFileName)

        # Write-Host "$($fileToCreate.FullName) => $($targetFileName))"

        # Only create, not overwrite
        if ([System.IO.File]::Exists($targetFileName))
        {
            continue
        }

        # Write-Host "Creating '$($targetFileName)'"

        if (![System.IO.Directory]::Exists($targetDirectoryName))
        {
            Write-Host "Creating directory '$($targetDirectoryName)'"

            New-Item $targetDirectoryName -ItemType directory
        }

        Copy-Item -Path $fileToCreate.FullName -Destination $targetFileName -Force
    }

    # Replace
    foreach ($fileToReplace in $replaceTemplateFiles)
    {
        $relativeFileName = $fileToReplace.FullName.Replace("$($templateRootDirectory)\$($replaceDirectoryName)", "")
        $targetFileName = Join-Path $gitRepositoryDirectory $relativeFileName

        # Write-Host "$($fileToReplace.FullName) => $($targetFileName))"

        # Only overwrite, not create
        if (![System.IO.File]::Exists($targetFileName))
        {
            continue
        }

        # Write-Host "Replacing $($targetFileName)"

        Copy-Item -Path $fileToReplace.FullName -Destination $targetFileName -force
    }
}

Write-Host "Done updating repositories"