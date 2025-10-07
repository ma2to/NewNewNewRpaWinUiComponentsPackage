@echo off
REM Batch script to run DataGrid Performance Tests
REM Simple wrapper for users who prefer CMD over PowerShell

echo ================================================================
echo    DataGrid Performance Test Runner
echo ================================================================
echo.

REM Check if project file exists
if not exist "PerformanceTests.csproj" (
    echo ERROR: PerformanceTests.csproj not found!
    echo Please ensure you're running this script from the project root directory.
    pause
    exit /b 1
)

echo [1/4] Cleaning previous builds...
if exist "bin" rmdir /s /q "bin" 2>nul
if exist "obj" rmdir /s /q "obj" 2>nul

echo [2/4] Restoring dependencies...
dotnet restore PerformanceTests.csproj
if errorlevel 1 (
    echo ERROR: Failed to restore dependencies!
    pause
    exit /b 1
)

echo [3/4] Building performance tests (Release mode)...
dotnet build PerformanceTests.csproj --configuration Release --no-restore
if errorlevel 1 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo Build successful! Starting performance tests...
echo.
echo ================================================================
echo.

echo [4/4] Running performance tests...
dotnet run --project PerformanceTests.csproj --configuration Release --no-build

echo.
echo ================================================================
echo.

REM Check if results were generated
if exist "PERFORMANCE_RESULTS_*.txt" (
    echo * Performance tests completed successfully!
    echo.
    echo Results saved to:
    dir /b /o-d "PERFORMANCE_RESULTS_*.*" 2>nul | findstr /r "PERFORMANCE_RESULTS_.*\.txt$ PERFORMANCE_RESULTS_.*\.csv$"
    echo.
) else (
    echo WARNING: Tests completed but no result files were found.
    echo Check console output above for errors.
    echo.
)

pause
