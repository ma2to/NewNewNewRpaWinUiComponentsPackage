using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Validation;

/// <summary>
/// Internal implementation of DataGrid validation operations.
/// Delegates to internal Validation service.
/// </summary>
internal sealed class DataGridValidation : IDataGridValidation
{
    private readonly ILogger<DataGridValidation>? _logger;
    private readonly IValidationService _validationService;

    public DataGridValidation(
        IValidationService validationService,
        ILogger<DataGridValidation>? logger = null)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger;
    }

    public async Task<PublicResult<bool>> ValidateAllAsync(bool onlyFiltered = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Validating all rows via Validation module (onlyFiltered: {OnlyFiltered})", onlyFiltered);
            var result = await _validationService.ValidateAllAsync(onlyFiltered, cancellationToken);
            return result.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ValidateAll failed in Validation module");
            throw;
        }
    }

    public async Task<Api.Models.PublicValidationResultWithStatistics> ValidateAllWithStatisticsAsync(bool onlyFiltered = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Validating all rows with statistics via Validation module (onlyFiltered: {OnlyFiltered})", onlyFiltered);
            var result = await _validationService.ValidateAllWithStatisticsAsync(onlyFiltered, cancellationToken);
            // Return directly since it's already the public type
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ValidateAllWithStatistics failed in Validation module");
            throw;
        }
    }

    public async Task<bool> AreAllNonEmptyRowsValidAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Checking if all non-empty rows are valid via Validation module");
            var result = await _validationService.ValidateAllAsync(onlyFiltered: false, cancellationToken);
            return result.IsSuccess && result.Value;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AreAllNonEmptyRowsValid check failed in Validation module");
            throw;
        }
    }

    public void RefreshValidationResultsToUI()
    {
        try
        {
            _logger?.LogInformation("Refreshing validation results to UI via Validation module");
            _validationService.RefreshValidationResultsToUI();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RefreshValidationResultsToUI failed in Validation module");
            throw;
        }
    }

    public async Task<PublicResult> AddValidationRuleAsync(IValidationRule rule)
    {
        try
        {
            _logger?.LogInformation("Adding validation rule '{RuleName}' via Validation module", rule?.RuleName);
            var result = await _validationService.AddValidationRuleAsync(rule);
            return result.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AddValidationRule failed in Validation module");
            throw;
        }
    }

    public async Task<PublicResult> RemoveValidationRulesAsync(params string[] columnNames)
    {
        try
        {
            _logger?.LogInformation("Removing validation rules for columns: {Columns} via Validation module", string.Join(", ", columnNames));
            var result = await _validationService.RemoveValidationRulesAsync(columnNames);
            return result.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RemoveValidationRules failed in Validation module");
            throw;
        }
    }

    public async Task<PublicResult> RemoveValidationRuleAsync(string ruleName)
    {
        try
        {
            _logger?.LogInformation("Removing validation rule '{RuleName}' via Validation module", ruleName);
            var result = await _validationService.RemoveValidationRuleAsync(ruleName);
            return result.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RemoveValidationRule failed in Validation module");
            throw;
        }
    }

    public async Task<PublicResult> ClearAllValidationRulesAsync()
    {
        try
        {
            _logger?.LogInformation("Clearing all validation rules via Validation module");
            var result = await _validationService.ClearAllValidationRulesAsync();
            return result.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ClearAllValidationRules failed in Validation module");
            throw;
        }
    }

    public string GetValidationAlerts(int rowIndex)
    {
        try
        {
            return _validationService.GetValidationAlerts(rowIndex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetValidationAlerts failed in Validation module for row {RowIndex}", rowIndex);
            throw;
        }
    }

    public bool HasValidationErrors(int rowIndex)
    {
        try
        {
            return _validationService.HasValidationErrors(rowIndex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "HasValidationErrors check failed in Validation module for row {RowIndex}", rowIndex);
            throw;
        }
    }
}
