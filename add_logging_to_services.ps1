# PowerShell script to add comprehensive logging to all services

$services = @(
    "RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Features\Export\Services\ExportService.cs",
    "RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Features\Validation\Services\ValidationService.cs",
    "RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Features\Filter\Services\FilterService.cs",
    "RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Features\CopyPaste\Services\CopyPasteService.cs",
    "RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Features\Selection\Services\SelectionService.cs",
    "RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Features\Column\Services\ColumnService.cs",
    "RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Features\AutoRowHeight\Services\AutoRowHeightService.cs",
    "RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Features\RowNumber\Services\RowNumberService.cs",
    "RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Features\Sort\Services\SortService.cs"
)

$usingStatements = @"
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;
"@

Write-Host "Adding logging using statements to services..." -ForegroundColor Cyan

foreach ($service in $services) {
    $serviceName = Split-Path $service -Leaf
    Write-Host "  Processing: $serviceName" -ForegroundColor Yellow

    $content = Get-Content $service -Raw

    # Check if already has logging using statements
    if ($content -notmatch "Infrastructure\.Logging\.Interfaces") {
        # Find last using statement and add ours
        $content = $content -replace "(using [^;]+;)(\r?\n\r?\nnamespace)", "`$1`r`n$usingStatements`$2"

        Set-Content -Path $service -Value $content -NoNewline
        Write-Host "    Added using statements" -ForegroundColor Green
    } else {
        Write-Host "    Already has using statements" -ForegroundColor Gray
    }
}

Write-Host "`nDone! All services have logging using statements." -ForegroundColor Green
Write-Host "Next step: Manually add field and constructor parameter to each service" -ForegroundColor Cyan
