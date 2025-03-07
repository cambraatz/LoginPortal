# define the path to the source directory and the output zip file
$sourceDirectory = "C:\Users\camer\Desktop\Apps\LoginPortal\LoginPortal.Server\bin\Release\net7.0\publish\wwwroot"
$outputZipFile = "C:\Users\camer\Desktop\Apps\LoginPortal\LoginPortal.Server\bin\Release\net7.0\linux-x64\wwwroot.zip"

# define the FileSystemWatcher to monitor the directory for changes
$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = $sourceDirectory
$watcher.Filter = "*.*"
$watcher.NotifyFilter = [System.IO.NotifyFilters]::LastWrite

# define the action to take when a change is detected
$action = {
    # wait for the file to be fully written (optional, for large files)
    Start-Sleep -Seconds 1

    # compress the directory into a zip file
    Write-Host "Directory updated, creating zip file..."

    # remove the old zip if it exists
    if (Test-Path $using:outputZipFile) {
        Remove-Item $using:outputZipFile -Force
    }

    # create a new zip file from the source directory
    Compress-Archive -Path $using:sourceDirectory -DestinationPath $using:outputZipFile

    Write-Host "Zip file created at: $using:outputZipFile"
}

# attach the event handler to the FileSystemWatcher
$watcher.Add_Changed($action)

# start watching for changes
$watcher.EnableRaisingEvents = $true

# keep the script running to monitor changes
Write-Host "Monitoring directory for changes. Press [Ctrl] + [C] to stop..."
while ($true) {
    Start-Sleep -Seconds 10
}
