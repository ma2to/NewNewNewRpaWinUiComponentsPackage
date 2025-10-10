namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Defines when validation is automatically triggered during data operations.
/// Controls the automation behavior of the validation system.
/// </summary>
public enum ValidationAutomationMode
{
    /// <summary>
    /// Validation runs automatically on data changes (import, paste, edit, row operations).
    /// User can also call validation entry point methods manually.
    /// This is the DEFAULT mode for maximum data integrity.
    ///
    /// Automatic triggers:
    /// - ImportAsync: batch validation after import (if EnableBatchValidation = true)
    /// - PasteAsync: batch validation after paste (if EnableBatchValidation = true)
    /// - UpdateCellAsync: real-time validation on cell change (if EnableRealTimeValidation = true)
    /// - UpdateRowAsync: real-time validation on row change (if EnableRealTimeValidation = true)
    /// - SmartAddRowsAsync: batch validation after adding rows (if EnableBatchValidation = true)
    /// - SmartDeleteRowsAsync: batch validation after deleting rows (if EnableBatchValidation = true)
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Validation runs ONLY when calling entry point methods explicitly.
    /// No automatic validation on import/paste/edit/row operations.
    /// Use this mode when you want full manual control over validation timing.
    ///
    /// Manual triggers (only way to validate):
    /// - ValidateAllAsync(): validates all non-empty rows
    /// - ValidateRowAsync(): validates specific row
    /// - ValidateCellAsync(): validates specific cell
    ///
    /// Note: EnableBatchValidation and EnableRealTimeValidation are ignored in this mode.
    /// </summary>
    Manual = 1
}
