# deploy.ps1

# ensure script runs from the project root
param (
    [string]$ProjectPath = (Get-Location).Path
)

# define paths
$publishPath = "$ProjectPath\LoginPortal.Server\bin\Release\net7.0\publish"
$linuxPath = "$ProjectPath\LoginPortal.Server\bin\Release\net7.0\linux-x64"
$clientPath = "$ProjectPath\loginportal.client"

Write-Host "Starting deployment script..."

# build and publish the application to default directory
Write-Host "Publishing .NET app..."
Set-Location $ProjectPath
dotnet clean
dotnet publish -c Release -r linux-x64 --self-contained=false -o ./bin/Release/net7.0/publish

# run the vite build process for the client app
Write-Host "Building client app using Vite..."
Set-Location $clientPath
# npm install
npm run build

# generate linux-x64 directory if one does not exist
if (!(Test-Path $linuxPath)) {
    New-Item -ItemType Directory -Path $linuxPath | Out-Null
}

# copy publish directory to linux-x64 directory
Write-Host "Copying files to linux-x64 directory..."
Copy-Item -Path "$publishPath\*" -Destination $linuxPath -Force -Exclude "wwwroot"

# compress the wwwroot directory
if (Test-Path "$publishPath\wwwroot") {
    Write-Host "Compressing wwwroot directory..."
    if (Test-Path "$linuxPath\wwwroot.zip") {
        Write-Host "Removing existing wwwroot.zip file..."
        Remove-Item "$linuxPath\wwwroot.zip" -Force
    }
    Compress-Archive -Path "$publishPath\wwwroot" -DestinationPath "$linuxPath\wwwroot.zip" -Force
    Write-Host "Compression completed successfully: $linuxPath\wwwroot.zip"
} else {
    Write-Host "wwwroot directory not found at $publishPath\wwwroot"
}

Set-Location ..

Write-Host "Deployment script completed successfully!"
