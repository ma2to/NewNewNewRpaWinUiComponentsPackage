# PowerShell script to analyze C# files for documentation status
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
    FilesWithSlovakComments = @()
    FilesNeedingXmlDocs = @()
    FilesNeedingLogging = @()
}

Get-ChildItem -Path $BasePath -Filter "*.cs" -Recurse | ForEach-Object {
    $relativePath = $_.FullName.Substring($BasePath.Length + 1)

    # Skip excluded files
    if ($excludedFiles -contains $relativePath) {
        return
    }

    $stats.TotalFiles++
    $content = Get-Content $_.FullName -Raw -Encoding UTF8

    # Check for Slovak comments/docs
    $slovakPatterns = @(
        'Verejn[áý]',
        'Implement[áa]cia',
        'Konštruktor',
        'Získame',
        'Inicializuje',
        'Vykonáme',
        'Mapujeme',
        'Začíname',
        'Vytvoríme',
        'Používame',
        'Nastavíme'
    )

    foreach ($pattern in $slovakPatterns) {
        if ($content -match $pattern) {
            $stats.FilesWithSlovakComments += $_.FullName
            break
        }
    }

    # Check if file needs XML docs (looking for public members without ///)
    if ($content -match 'public\s+(class|interface|enum|struct)\s+\w+' -and
        $content -notmatch '/// <summary>') {
        $stats.FilesNeedingXmlDocs += $_.FullName
    }

    # Check if file needs logging (has public methods but no ILogger)
    if ($content -match 'public\s+.*Task<.*>\s+\w+\(' -and
        $content -notmatch 'ILogger') {
        $stats.FilesNeedingLogging += $_.FullName
    }
}

Write-Host "`n=== DOCUMENTATION ANALYSIS ===" -ForegroundColor Cyan
Write-Host "Total files to process: $($stats.TotalFiles)" -ForegroundColor Yellow
Write-Host "`nFiles with Slovak comments: $($stats.FilesWithSlovakComments.Count)" -ForegroundColor Yellow
Write-Host "Files needing XML docs: $($stats.FilesNeedingXmlDocs.Count)" -ForegroundColor Yellow
Write-Host "Files potentially needing logging: $($stats.FilesNeedingLogging.Count)" -ForegroundColor Yellow

# Output file lists for processing
$stats.FilesWithSlovakComments | Out-File "$BasePath\..\_slovak_files.txt" -Encoding UTF8
$stats.FilesNeedingXmlDocs | Out-File "$BasePath\..\_files_need_xml.txt" -Encoding UTF8
$stats.FilesNeedingLogging | Out-File "$BasePath\..\_files_need_logging.txt" -Encoding UTF8

Write-Host "`nFile lists saved to:" -ForegroundColor Green
Write-Host "  - _slovak_files.txt"
Write-Host "  - _files_need_xml.txt"
Write-Host "  - _files_need_logging.txt"
