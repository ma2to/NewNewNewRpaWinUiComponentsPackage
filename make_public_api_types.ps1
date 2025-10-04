# Change all internal types to public that are exposed via public Facade API

# AutoRowHeight types
$file = "D:\www\RB0120APP\NewRpaWinUiComponentsPackage\RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Features\AutoRowHeight\Interfaces\IAutoRowHeightService.cs"
$content = Get-Content $file -Raw
$content = $content -replace 'internal record AutoRowHeightConfiguration', 'public record AutoRowHeightConfiguration'
$content = $content -replace 'internal record AutoRowHeightResult', 'public record AutoRowHeightResult'
$content = $content -replace 'internal record RowHeightCalculationResult', 'public record RowHeightCalculationResult'
$content = $content -replace 'internal record TextMeasurementResult', 'public record TextMeasurementResult'
$content = $content -replace 'internal record RowHeightCalculationOptions', 'public record RowHeightCalculationOptions'
$content = $content -replace 'internal record BatchCalculationProgress', 'public record BatchCalculationProgress'
$content = $content -replace 'internal record AutoRowHeightStatistics', 'public record AutoRowHeightStatistics'
$content = $content -replace 'internal record CacheStatistics', 'public record CacheStatistics'
Set-Content $file $content -NoNewline
Write-Host "Updated: AutoRowHeight types"

Write-Host "Done!"
