# Update AdvancedDataGridFacade implementation signatures to match interface

$file = "D:\www\RB0120APP\NewRpaWinUiComponentsPackage\RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Api\AdvancedDataGridFacade.cs"
$content = Get-Content $file -Raw

# Add using for mappings at top
if ($content -notmatch 'using.*Api\.Internal') {
    $content = $content -replace '(using System\.Data;)', "`$1`nusing RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Internal;"
}

# Replace Result types in method signatures (only public methods)
$content = $content -replace '(\s+public\s+Task<)Result<bool>', '$1PublicResult<bool>'
$content = $content -replace '(\s+public\s+Task<)Result>', '$1PublicResult>'
$content = $content -replace '(\s+public\s+)Result\s', '$1PublicResult '

# Replace Column types
$content = $content -replace '(\s+public\s+IReadOnlyList<)ColumnDefinition>', '$1PublicColumnDefinition>'
$content = $content -replace '(\s+public\s+bool\s+\w+\()ColumnDefinition\s', '$1PublicColumnDefinition '

# Replace Filter types
$content = $content -replace '(\s+public\s+Task<int>\s+\w+\([^,]+,\s*)FilterOperator\s', '$1PublicFilterOperator '

# Replace Sort types
$content = $content -replace '(\s+public\s+Task<\w+>\s+\w+\([^,]+,\s*)SortDirection([,\)])', '$1PublicSortDirection$2'

# Replace AutoRowHeight types
$content = $content -replace '(\s+public\s+Task<)AutoRowHeightResult>(\s+\w+\()AutoRowHeightConfiguration', '$1PublicAutoRowHeightResult>$2PublicAutoRowHeightConfiguration'
$content = $content -replace '(\s+public\s+Task<IReadOnlyList<)RowHeightCalculationResult>>', '$1PublicRowHeightCalculationResult>>'
$content = $content -replace '(\s+public\s+Task<)RowHeightCalculationResult>', '$1PublicRowHeightCalculationResult>'
$content = $content -replace '(\w+\([^,]+,\s*)RowHeightCalculationOptions\?', '$1PublicRowHeightCalculationOptions?'
$content = $content -replace '(\s+public\s+Task<)TextMeasurementResult>', '$1PublicTextMeasurementResult>'
$content = $content -replace 'IProgress<BatchCalculationProgress>', 'IProgress<PublicBatchCalculationProgress>'
$content = $content -replace '(\s+public\s+)AutoRowHeightStatistics\s', '$1PublicAutoRowHeightStatistics '
$content = $content -replace '(\s+public\s+)CacheStatistics\s', '$1PublicCacheStatistics '

Set-Content $file $content -NoNewline
Write-Host "Updated AdvancedDataGridFacade.cs signatures"
