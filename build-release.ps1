# SimControlCentre v1.3.0 Release Build Script

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SimControlCentre v1.3.0 Release Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
Remove-Item -Recurse -Force "SimControlCentre\bin\Release" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "Installers" -ErrorAction SilentlyContinue

# Create output directory
New-Item -ItemType Directory -Force -Path "Installers" | Out-Null

Write-Host "? Cleaned" -ForegroundColor Green
Write-Host ""

# Build plugins first
Write-Host "Building plugins..." -ForegroundColor Yellow
dotnet build SimControlCentre.Plugins.GoXLR/SimControlCentre.Plugins.GoXLR.csproj -c Release --verbosity quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "? GoXLR plugin built" -ForegroundColor Green
} else {
    Write-Host "? Plugin build failed!" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Build 1: Framework-Dependent (requires .NET 8)
Write-Host "Building framework-dependent version..." -ForegroundColor Yellow
dotnet publish SimControlCentre/SimControlCentre.csproj `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -o "SimControlCentre\bin\Release\Publish" `
    --verbosity quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Framework-dependent build complete" -ForegroundColor Green
    
    # Copy plugins to publish folder
    Write-Host "Copying plugins to publish folder..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path "SimControlCentre\bin\Release\Publish\Plugins" | Out-Null
    Copy-Item "SimControlCentre.Plugins.GoXLR\bin\Release\net8.0-windows\SimControlCentre.Plugins.GoXLR.dll" `
        -Destination "SimControlCentre\bin\Release\Publish\Plugins\" -Force
    Write-Host "? Plugins copied" -ForegroundColor Green
} else {
    Write-Host "? Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host ""


# Build 2: Self-Contained (includes .NET 8)
Write-Host "Building self-contained version..." -ForegroundColor Yellow
dotnet publish SimControlCentre/SimControlCentre.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --verbosity quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Self-contained build complete" -ForegroundColor Green
    
    # Copy plugins to self-contained publish folder
    Write-Host "Copying plugins to self-contained folder..." -ForegroundColor Yellow
    $selfContainedPath = "SimControlCentre\bin\Release\net8.0-windows\win-x64\publish"
    New-Item -ItemType Directory -Force -Path "$selfContainedPath\Plugins" | Out-Null
    Copy-Item "SimControlCentre.Plugins.GoXLR\bin\Release\net8.0-windows\SimControlCentre.Plugins.GoXLR.dll" `
        -Destination "$selfContainedPath\Plugins\" -Force
    Write-Host "? Plugins copied" -ForegroundColor Green
} else {
    Write-Host "? Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host ""


# Check for Inno Setup
Write-Host "Checking for Inno Setup..." -ForegroundColor Yellow
$innoPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

if (Test-Path $innoPath) {
    Write-Host "? Inno Setup found" -ForegroundColor Green
    Write-Host ""
    
    # Build installer 1: Framework-dependent
    Write-Host "Building framework-dependent installer..." -ForegroundColor Yellow
    & $innoPath "installer.iss" /Q
    
    if ($LASTEXITCODE -eq 0) {
        $installerPath = Get-Item "Installers\SimControlCentre-Setup-v*.exe" | Select-Object -First 1
        $size = [math]::Round($installerPath.Length / 1MB, 2)
        Write-Host "? Installer created: $($installerPath.Name) ($size MB)" -ForegroundColor Green
    } else {
        Write-Host "? Installer build failed!" -ForegroundColor Red
    }
    Write-Host ""
    
    # Build installer 2: Self-contained
    Write-Host "Building self-contained installer..." -ForegroundColor Yellow
    & $innoPath "installer-standalone.iss" /Q
    
    if ($LASTEXITCODE -eq 0) {
        $installerPath = Get-Item "Installers\SimControlCentre-Setup-Standalone-v*.exe" | Select-Object -First 1
        $size = [math]::Round($installerPath.Length / 1MB, 2)
        Write-Host "? Installer created: $($installerPath.Name) ($size MB)" -ForegroundColor Green
    } else {
        Write-Host "? Installer build failed!" -ForegroundColor Red
    }
} else {
    Write-Host "? Inno Setup not found at: $innoPath" -ForegroundColor Yellow
    Write-Host "  Download from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "  Installers will not be created, but builds are ready in:" -ForegroundColor Yellow
    Write-Host "    - SimControlCentre\bin\Release\Publish (framework-dependent)" -ForegroundColor Yellow
    Write-Host "    - SimControlCentre\bin\Release\net8.0-windows\win-x64\publish (self-contained)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output files in: .\Installers\" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Test both installers on a clean VM" -ForegroundColor White
Write-Host "  2. Create GitHub Release (v1.0.0)" -ForegroundColor White
Write-Host "  3. Upload installers to release" -ForegroundColor White
Write-Host "  4. Write release notes" -ForegroundColor White
Write-Host ""
