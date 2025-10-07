# PowerShell script to run DataGrid Performance Tests
# Automatically builds and executes the performance test suite

Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   DataGrid Performance Test Runner                          ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Configuration
$projectFile = "PerformanceTests.csproj"
$outputDir = "bin\Release\net8.0-windows10.0.19041.0\win-x64"

# Check if project file exists
if (-not (Test-Path $projectFile)) {
    Write-Host "ERROR: $projectFile not found!" -ForegroundColor Red
    Write-Host "Please ensure you're running this script from the project root directory." -ForegroundColor Yellow
    exit 1
}

# Clean previous build
Write-Host "[1/4] Cleaning previous builds..." -ForegroundColor Yellow
Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue

# Restore dependencies
Write-Host "[2/4] Restoring dependencies..." -ForegroundColor Yellow
dotnet restore $projectFile
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to restore dependencies!" -ForegroundColor Red
    exit 1
}

# Build the project
Write-Host "[3/4] Building performance tests (Release mode)..." -ForegroundColor Yellow
dotnet build $projectFile --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Build successful! Starting performance tests..." -ForegroundColor Green
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Run the tests
Write-Host "[4/4] Running performance tests..." -ForegroundColor Yellow
dotnet run --project $projectFile --configuration Release --no-build

# Check if results were generated
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$resultFiles = Get-ChildItem -Path "." -Filter "PERFORMANCE_RESULTS_*.txt" -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($resultFiles) {
    Write-Host "✓ Performance tests completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Results saved to:" -ForegroundColor Cyan
    Get-ChildItem -Path "." -Filter "PERFORMANCE_RESULTS_*.*" -File | Sort-Object LastWriteTime -Descending | Select-Object -First 2 | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor White
    }
    Write-Host ""

    # Ask if user wants to view the results
    $response = Read-Host "Would you like to view the text report now? (y/n)"
    if ($response -eq 'y' -or $response -eq 'Y') {
        $latestTxt = Get-ChildItem -Path "." -Filter "PERFORMANCE_RESULTS_*.txt" -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($latestTxt) {
            Get-Content $latestTxt.FullName | Out-Host
        }
    }
} else {
    Write-Host "⚠ Tests completed but no result files were found." -ForegroundColor Yellow
    Write-Host "Check console output above for errors." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
