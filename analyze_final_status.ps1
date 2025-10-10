# Final documentation status analysis
param(
    [string]$BasePath = "D:\www\RB0120APP\NewRpaWinUiComponentsPackage\RpaWinUiComponentsPackage\AdvancedWinUiDataGrid"
)

$excludedFiles = @(
    "UIControls\CellControl.cs",
    "UIControls\DataGridCellsView.cs",
    "UIControls\HeadersRowView.cs",
    "UIControls\FilterRowView.cs",
    "UIControls\SearchPanelView.cs",
    "ViewModels\DataGridViewModel.cs",
    "ViewModels\CellViewModel.cs",
    "ViewModels\ThemeManager.cs",
    "UIControls\AdvancedDataGridControl.cs",
    "UIControls\AdvancedDataGridFacadeUI.cs"
)

$stats = @{
    TotalFiles = 0
    FilesWithXmlDocs = 0
    FilesWithLogging = 0
    FilesWithoutXmlDocs = @()
    FilesWithoutLogging = @()
    PublicClassesCount = 0
    PublicInterfacesCount = 0
    PublicMethodsCount = 0
    XmlDocumentedClasses = 0
    XmlDocumentedMethods = 0
    LogStatementsCount = 0
}

Write-Host "Analyzing documentation status..." -ForegroundColor Cyan
Write-Host ""

Get-ChildItem -Path $BasePath -Filter "*.cs" -Recurse | ForEach-Object {
    $relativePath = $_.FullName.Substring($BasePath.Length + 1)

    # Skip excluded files
    if ($excludedFiles -contains $relativePath) {
        return
    }

    $stats.TotalFiles++
    $content = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)

    # Count public classes/interfaces
    $publicClasses = ([regex]::Matches($content, 'public\s+(class|sealed\s+class)\s+\w+')).Count
    $publicInterfaces = ([regex]::Matches($content, 'public\s+interface\s+\w+')).Count
    $publicMethods = ([regex]::Matches($content, 'public\s+.*\s+\w+\s*\(')).Count

    $stats.PublicClassesCount += $publicClasses
    $stats.PublicInterfacesCount += $publicInterfaces
    $stats.PublicMethodsCount += $publicMethods

    # Check for XML documentation
    $hasXmlDocs = $content -match '///\s*<summary>'
    if ($hasXmlDocs) {
        $stats.FilesWithXmlDocs++

        # Count documented elements
        $xmlDocs = ([regex]::Matches($content, '///\s*<summary>')).Count
        $stats.XmlDocumentedClasses += $xmlDocs
        $stats.XmlDocumentedMethods += $xmlDocs
    } else {
        if ($publicClasses -gt 0 -or $publicInterfaces -gt 0 -or $publicMethods -gt 0) {
            $stats.FilesWithoutXmlDocs += $relativePath
        }
    }

    # Check for logging
    $hasLogging = $content -match 'ILogger|_logger|LogInformation|LogWarning|LogError'
    if ($hasLogging) {
        $stats.FilesWithLogging++
        $logStatements = ([regex]::Matches($content, '\.(LogInformation|LogWarning|LogError|LogDebug)\(')).Count
        $stats.LogStatementsCount += $logStatements
    } else {
        if ($publicMethods -gt 5) {  # Only flag files with significant methods
            $stats.FilesWithoutLogging += $relativePath
        }
    }
}

Write-Host "=== FINAL DOCUMENTATION STATUS ===" -ForegroundColor Green
Write-Host ""
Write-Host "FILES SUMMARY:" -ForegroundColor Cyan
Write-Host "  Total files analyzed: $($stats.TotalFiles)" -ForegroundColor White
Write-Host "  Files with XML documentation: $($stats.FilesWithXmlDocs)" -ForegroundColor Green
Write-Host "  Files with logging: $($stats.FilesWithLogging)" -ForegroundColor Green
Write-Host ""
Write-Host "CODE ELEMENTS:" -ForegroundColor Cyan
Write-Host "  Public classes: $($stats.PublicClassesCount)" -ForegroundColor White
Write-Host "  Public interfaces: $($stats.PublicInterfacesCount)" -ForegroundColor White
Write-Host "  Public methods: $($stats.PublicMethodsCount)" -ForegroundColor White
Write-Host "  XML documented elements: $($stats.XmlDocumentedClasses)" -ForegroundColor Green
Write-Host ""
Write-Host "LOGGING:" -ForegroundColor Cyan
Write-Host "  Total log statements: $($stats.LogStatementsCount)" -ForegroundColor Green
Write-Host ""

$xmlCoverage = [Math]::Round(($stats.FilesWithXmlDocs / $stats.TotalFiles) * 100, 2)
$loggingCoverage = [Math]::Round(($stats.FilesWithLogging / $stats.TotalFiles) * 100, 2)

Write-Host "COVERAGE:" -ForegroundColor Cyan
Write-Host "  XML Documentation: $xmlCoverage%" -ForegroundColor $(if ($xmlCoverage -ge 80) { "Green" } elseif ($xmlCoverage -ge 50) { "Yellow" } else { "Red" })
Write-Host "  Logging: $loggingCoverage%" -ForegroundColor $(if ($loggingCoverage -ge 80) { "Green" } elseif ($loggingCoverage -ge 50) { "Yellow" } else { "Red" })
Write-Host ""

if ($stats.FilesWithoutXmlDocs.Count -gt 0 -and $stats.FilesWithoutXmlDocs.Count -le 20) {
    Write-Host "Files potentially needing XML documentation:" -ForegroundColor Yellow
    $stats.FilesWithoutXmlDocs | Select-Object -First 20 | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }
    if ($stats.FilesWithoutXmlDocs.Count -gt 20) {
        Write-Host "  ... and $($stats.FilesWithoutXmlDocs.Count - 20) more" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "=== ANALYSIS COMPLETE ===" -ForegroundColor Green
