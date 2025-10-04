# Replace internal types with public types in IAdvancedDataGridFacade.cs

$file = "D:\www\RB0120APP\NewRpaWinUiComponentsPackage\RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Api\IAdvancedDataGridFacade.cs"
$content = Get-Content $file -Raw

# Replace Result types
$content = $content -replace 'Task<Result<bool>>', 'Task<PublicResult<bool>>'
$content = $content -replace 'Task<Result>', 'Task<PublicResult>'
$content = $content -replace '([^a-zA-Z])Result([^a-zA-Z<])', '$1PublicResult$2'

# Replace Column types
$content = $content -replace 'IReadOnlyList<ColumnDefinition>', 'IReadOnlyList<PublicColumnDefinition>'
$content = $content -replace '([^a-zA-Z])ColumnDefinition([^a-zA-Z])', '$1PublicColumnDefinition$2'

# Replace Filter types
$content = $content -replace '([^a-zA-Z])FilterOperator([^a-zA-Z])', '$1PublicFilterOperator$2'

# Replace Sort types
$content = $content -replace '([^a-zA-Z])SortDirection([^a-zA-Z])', '$1PublicSortDirection$2'

# Replace AutoRowHeight types
$content = $content -replace 'AutoRowHeightConfiguration', 'PublicAutoRowHeightConfiguration'
$content = $content -replace 'Task<AutoRowHeightResult>', 'Task<PublicAutoRowHeightResult>'
$content = $content -replace 'Task<IReadOnlyList<RowHeightCalculationResult>>', 'Task<IReadOnlyList<PublicRowHeightCalculationResult>>'
$content = $content -replace 'Task<RowHeightCalculationResult>', 'Task<PublicRowHeightCalculationResult>'
$content = $content -replace 'RowHeightCalculationOptions', 'PublicRowHeightCalculationOptions'
$content = $content -replace 'Task<TextMeasurementResult>', 'Task<PublicTextMeasurementResult>'
$content = $content -replace 'IProgress<BatchCalculationProgress>', 'IProgress<PublicBatchCalculationProgress>'
$content = $content -replace '([^a-zA-Z])AutoRowHeightStatistics([^a-zA-Z])', '$1PublicAutoRowHeightStatistics$2'
$content = $content -replace '([^a-zA-Z])CacheStatistics([^a-zA-Z])', '$1PublicCacheStatistics$2'

Set-Content $file $content -NoNewline
Write-Host "Updated IAdvancedDataGridFacade.cs with public types"
