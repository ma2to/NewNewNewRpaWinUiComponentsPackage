# Revert all public types back to internal in Common/Models and Features

# Common/Models
$files = Get-ChildItem -Path "D:\www\RB0120APP\NewRpaWinUiComponentsPackage\RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Common\Models\*.cs" -File
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace 'public class', 'internal class'
    $content = $content -replace 'public record', 'internal record'
    $content = $content -replace 'public enum', 'internal enum'
    Set-Content $file.FullName $content -NoNewline
    Write-Host "Reverted: $($file.Name)"
}

# Common root enums
$enumFiles = Get-ChildItem -Path "D:\www\RB0120APP\NewRpaWinUiComponentsPackage\RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Common\*.cs" -File
foreach ($file in $enumFiles) {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace 'public enum', 'internal enum'
    $content = $content -replace 'public sealed record', 'internal sealed record'
    Set-Content $file.FullName $content -NoNewline
    Write-Host "Reverted: $($file.Name)"
}

# AutoRowHeight types
$file = "D:\www\RB0120APP\NewRpaWinUiComponentsPackage\RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Features\AutoRowHeight\Interfaces\IAutoRowHeightService.cs"
$content = Get-Content $file -Raw
$content = $content -replace 'public record AutoRowHeightConfiguration', 'internal record AutoRowHeightConfiguration'
$content = $content -replace 'public record AutoRowHeightResult', 'internal record AutoRowHeightResult'
$content = $content -replace 'public record RowHeightCalculationResult', 'internal record RowHeightCalculationResult'
$content = $content -replace 'public record TextMeasurementResult', 'internal record TextMeasurementResult'
$content = $content -replace 'public record RowHeightCalculationOptions', 'internal record RowHeightCalculationOptions'
$content = $content -replace 'public record BatchCalculationProgress', 'internal record BatchCalculationProgress'
$content = $content -replace 'public record AutoRowHeightStatistics', 'internal record AutoRowHeightStatistics'
$content = $content -replace 'public record CacheStatistics', 'internal record CacheStatistics'
Set-Content $file $content -NoNewline
Write-Host "Reverted: AutoRowHeight types"

Write-Host "All reverted to internal!"
