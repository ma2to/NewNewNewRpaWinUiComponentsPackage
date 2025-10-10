# Comprehensive documentation update script
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

# Translation pairs (Slovak -> English)
$translations = @(
    @('Verejná implementácia', 'Public implementation of'),
    @('Orchestruje všetky operácie komponentov cez interné služby', 'Orchestrates all component operations through internal services'),
    @('Konštruktor', 'Constructor for'),
    @('Inicializuje závislosti a získava operation logger cez DI', 'Initializes dependencies and obtains operation logger via DI'),
    @('Získame operation logger cez DI, alebo použijeme null pattern', 'Obtain operation logger via DI, or use null pattern'),
    @('Získame UI notification service \(dostupný ak je DispatcherQueue poskytnutý\)', 'Obtain UI notification service (available if DispatcherQueue is provided)'),
    @('Získame GridViewModelAdapter \(dostupný ak je DispatcherQueue poskytnutý\)', 'Obtain GridViewModelAdapter (available if DispatcherQueue is provided)'),
    @('Získame ThemeService \(vždy dostupný\)', 'Obtain ThemeService (always available)'),
    @('Importuje dáta pomocou command pattern s LINQ optimalizáciou a validačným pipeline', 'Imports data using command pattern with LINQ optimization and validation pipeline'),
    @('Začíname import operáciu - vytvoríme operation scope pre automatické tracking', 'Start import operation - create operation scope for automatic tracking'),
    @('Vytvoríme operation scope pre scoped services', 'Create operation scope for scoped services'),
    @('Mapujeme public command na internal command', 'Map public command to internal command'),
    @('Vykonáme interný import', 'Execute internal import'),
    @('Mapujeme interný result na public PublicResult', 'Map internal result to public result'),
    @('Mapujeme interný result na public PublicResult \(s exportovanými dátami\)', 'Map internal result to public result (with exported data)'),
    @('Automatický UI refresh v Interactive mode', 'Automatic UI refresh in Interactive mode'),
    @('Exportuje dáta pomocou command pattern s komplexným filtrovaním', 'Exports data using command pattern with comprehensive filtering'),
    @('Začíname export operáciu - vytvoríme operation scope pre automatické tracking', 'Start export operation - create operation scope for automatic tracking'),
    @('Vykonáme interný export', 'Execute internal export'),
    @('Validuje všetky neprázdne riadky s dávkovým, thread-safe spracovaním', 'Validates all non-empty rows with batched, thread-safe processing'),
    @('Implementácia podľa dokumentácie: AreAllNonEmptyRowsValidAsync s dávkovým, thread-safe, stream supportom', 'Implementation according to documentation: AreAllNonEmptyRowsValidAsync with batched, thread-safe, stream support'),
    @('Obnoví výsledky validácie do UI \(no-op v headless režime\)', 'Refreshes validation results to UI (no-op in headless mode)'),
    @('získava', 'obtains'),
    @('získame', 'obtain'),
    @('získa', 'obtains'),
    @('vytvoríme', 'create'),
    @('vykonáme', 'execute'),
    @('mapujeme', 'map'),
    @('začíname', 'start'),
    @('všetky', 'all'),
    @('všetko', 'everything'),
    @('všetkých', 'all'),
    @('dáta', 'data'),
    @('pomocou', 'using'),
    @('operáciu', 'operation'),
    @('operácie', 'operations'),
    @('služby', 'services'),
    @('alebo', 'or'),
    @('použijeme', 'use'),
    @('ak je', 'if'),
    @('dostupný', 'available'),
    @('vždy', 'always'),
    @('pre ', 'for '),
    @('cez ', 'via '),
    @('interné ', 'internal '),
    @('interný ', 'internal '),
    @('závislosti', 'dependencies')
)

$stats = @{
    TotalFiles = 0
    FilesModified = 0
    TranslationsMade = 0
    FilesWithTranslations = @()
}

Write-Host "Processing files in: $BasePath" -ForegroundColor Cyan
Write-Host ""

Get-ChildItem -Path $BasePath -Filter "*.cs" -Recurse | ForEach-Object {
    $relativePath = $_.FullName.Substring($BasePath.Length + 1)

    # Skip excluded files
    if ($excludedFiles -contains $relativePath) {
        return
    }

    $filePath = $_.FullName
    $content = [System.IO.File]::ReadAllText($filePath, [System.Text.Encoding]::UTF8)
    $originalContent = $content
    $modified = $false
    $fileTranslations = 0

    # Apply all translations
    foreach ($pair in $translations) {
        $slovak = $pair[0]
        $english = $pair[1]

        if ($content -cmatch $slovak) {
            $content = $content -creplace $slovak, $english
            $modified = $true
            $fileTranslations++
        }
    }

    # Save if modified
    if ($modified) {
        [System.IO.File]::WriteAllText($filePath, $content, [System.Text.Encoding]::UTF8)
        $stats.FilesModified++
        $stats.TranslationsMade += $fileTranslations
        $stats.FilesWithTranslations += $relativePath
        Write-Host "[TRANSLATED] $relativePath ($fileTranslations changes)" -ForegroundColor Green
    }

    $stats.TotalFiles++
}

Write-Host ""
Write-Host "=== DOCUMENTATION UPDATE COMPLETE ===" -ForegroundColor Green
Write-Host "Total files processed: $($stats.TotalFiles)" -ForegroundColor Cyan
Write-Host "Files modified: $($stats.FilesModified)" -ForegroundColor Yellow
Write-Host "Total translations made: $($stats.TranslationsMade)" -ForegroundColor Yellow
Write-Host ""

if ($stats.FilesModified -gt 0) {
    Write-Host "Modified files:" -ForegroundColor Cyan
    $stats.FilesWithTranslations | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
}
