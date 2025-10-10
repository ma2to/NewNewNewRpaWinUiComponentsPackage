# PowerShell script to update documentation and translate Slovak comments
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

# Slovak to English translation dictionary for comments
$translations = @{
    'Verejná implementácia' = 'Public implementation of'
    'Orchestruje všetky operácie komponentov cez interné služby' = 'Orchestrates all component operations through internal services'
    'Konštruktor' = 'Constructor for'
    'Inicializuje závislosti a získava operation logger cez DI' = 'Initializes dependencies and obtains operation logger via DI'
    'Získame operation logger cez DI, alebo použijeme null pattern' = 'Obtain operation logger via DI, or use null pattern'
    'Získame UI notification service \(dostupný ak je DispatcherQueue poskytnutý\)' = 'Obtain UI notification service (available if DispatcherQueue is provided)'
    'Získame GridViewModelAdapter \(dostupný ak je DispatcherQueue poskytnutý\)' = 'Obtain GridViewModelAdapter (available if DispatcherQueue is provided)'
    'Získame ThemeService \(vždy dostupný\)' = 'Obtain ThemeService (always available)'
    'Importuje dáta pomocou command pattern s LINQ optimalizáciou a validačným pipeline' = 'Imports data using command pattern with LINQ optimization and validation pipeline'
    'Začíname import operáciu - vytvoríme operation scope pre automatické tracking' = 'Starting import operation - create operation scope for automatic tracking'
    'Vytvoríme operation scope pre scoped services' = 'Create operation scope for scoped services'
    'Mapujeme public command na internal command' = 'Map public command to internal command'
    'Vykonáme interný import' = 'Execute internal import'
    'Mapujeme interný result na public PublicResult' = 'Map internal result to public result'
    'Automatický UI refresh v Interactive mode' = 'Automatic UI refresh in Interactive mode'
    'Exportuje dáta pomocou command pattern s komplexným filtrovaním' = 'Exports data using command pattern with comprehensive filtering'
    'Začíname export operáciu - vytvoríme operation scope pre automatické tracking' = 'Starting export operation - create operation scope for automatic tracking'
    'Vykonáme interný export' = 'Execute internal export'
    'Mapujeme interný result na public PublicResult \(s exportovanými dátami\)' = 'Map internal result to public result (with exported data)'
    'Validuje všetky neprázdne riadky s dávkovým, thread-safe spracovaním' = 'Validates all non-empty rows with batched, thread-safe processing'
    'Implementácia podľa dokumentácie.*' = 'Implementation according to documentation'
    'Obnoví výsledky validácie do UI \(no-op v headless režime\)' = 'Refreshes validation results to UI (no-op in headless mode)'
    'Používame' = 'Uses'
    'Nastavíme' = 'Sets'
    'Vytvoríme' = 'Creates'
    'Získame' = 'Obtains'
}

$stats = @{
    FilesProcessed = 0
    SlovakCommentsTranslated = 0
    FilesModified = @()
}

Get-ChildItem -Path $BasePath -Filter "*.cs" -Recurse | ForEach-Object {
    $relativePath = $_.FullName.Substring($BasePath.Length + 1)

    # Skip excluded files
    if ($excludedFiles -contains $relativePath) {
        return
    }

    $filePath = $_.FullName
    $content = Get-Content $filePath -Raw -Encoding UTF8
    $originalContent = $content
    $modified = $false

    # Translate Slovak comments to English
    foreach ($slovak in $translations.Keys) {
        $english = $translations[$slovak]
        if ($content -match $slovak) {
            $content = $content -replace $slovak, $english
            $modified = $true
            $stats.SlovakCommentsTranslated++
        }
    }

    # Save if modified
    if ($modified) {
        Set-Content -Path $filePath -Value $content -Encoding UTF8 -NoNewline
        $stats.FilesModified += $relativePath
    }

    $stats.FilesProcessed++
}

Write-Host "`n=== DOCUMENTATION UPDATE COMPLETE ===" -ForegroundColor Green
Write-Host "Files processed: $($stats.FilesProcessed)" -ForegroundColor Cyan
Write-Host "Slovak comments translated: $($stats.SlovakCommentsTranslated)" -ForegroundColor Cyan
Write-Host "Files modified: $($stats.FilesModified.Count)" -ForegroundColor Cyan

if ($stats.FilesModified.Count -gt 0) {
    Write-Host "`nModified files:" -ForegroundColor Yellow
    $stats.FilesModified | ForEach-Object { Write-Host "  - $_" }
}
