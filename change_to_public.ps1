# Change internal types to public in Common/Models
$files = Get-ChildItem -Path "D:\www\RB0120APP\NewRpaWinUiComponentsPackage\RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Common\Models\*.cs" -File

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace 'internal class', 'public class'
    $content = $content -replace 'internal record', 'public record'
    $content = $content -replace 'internal enum', 'public enum'
    Set-Content $file.FullName $content -NoNewline
    Write-Host "Updated: $($file.Name)"
}

# Change internal enums to public in Common root
$enumFiles = Get-ChildItem -Path "D:\www\RB0120APP\NewRpaWinUiComponentsPackage\RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Common\*.cs" -File

foreach ($file in $enumFiles) {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace 'internal enum', 'public enum'
    $content = $content -replace 'internal sealed record', 'public sealed record'
    Set-Content $file.FullName $content -NoNewline
    Write-Host "Updated: $($file.Name)"
}

Write-Host "Done!"
