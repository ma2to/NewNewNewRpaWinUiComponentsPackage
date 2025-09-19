using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;

/// <summary>
/// APPLICATION: Service interface for validation operations
/// SINGLE RESPONSIBILITY: Validation orchestration and rule management
/// </summary>
internal interface IValidationService
{
    /// <summary>Add single cell validation rule</summary>
    Result AddValidationRule(ISingleCellValidationRule rule);

    /// <summary>Add cross-column validation rule</summary>
    Result AddValidationRule(ICrossColumnValidationRule rule);

    /// <summary>Add cross-row validation rule</summary>
    Result AddValidationRule(ICrossRowValidationRule rule);

    /// <summary>Add conditional validation rule</summary>
    Result AddValidationRule(IConditionalValidationRule rule);

    /// <summary>Add complex validation rule</summary>
    Result AddValidationRule(IComplexValidationRule rule);

    /// <summary>Remove validation rules by column names</summary>
    Result RemoveValidationRulesByColumns(params string[] columnNames);

    /// <summary>Remove validation rule by name</summary>
    Result RemoveValidationRuleByName(string ruleName);

    /// <summary>Clear all validation rules</summary>
    Result ClearAllValidationRules();

    /// <summary>Validate single cell value</summary>
    Task<Result<ValidationResult>> ValidateCellAsync(string columnName, object? value,
        IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default);

    /// <summary>Validate entire row</summary>
    Task<Result<IReadOnlyList<ValidationResult>>> ValidateRowAsync(int rowIndex,
        IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default);

    /// <summary>Validate entire dataset</summary>
    Task<Result<IReadOnlyList<ValidationResult>>> ValidateDatasetAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset, CancellationToken cancellationToken = default);

    /// <summary>Check if all non-empty rows are valid</summary>
    Task<Result<bool>> AreAllNonEmptyRowsValidAsync(bool onlyFiltered = false, CancellationToken cancellationToken = default);
}