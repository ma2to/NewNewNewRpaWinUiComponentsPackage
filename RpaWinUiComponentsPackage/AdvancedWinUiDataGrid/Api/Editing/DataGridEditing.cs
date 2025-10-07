using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CellEdit.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Editing;

/// <summary>
/// Internal implementation of DataGrid editing operations.
/// Delegates to internal editing service and provides mapping between public and internal models.
/// </summary>
internal sealed class DataGridEditing : IDataGridEditing
{
    private readonly ILogger<DataGridEditing>? _logger;
    private readonly ICellEditService _cellEditService;

    public DataGridEditing(
        ICellEditService cellEditService,
        ILogger<DataGridEditing>? logger = null)
    {
        _cellEditService = cellEditService ?? throw new ArgumentNullException(nameof(cellEditService));
        _logger = logger;
    }

    public async Task<PublicResult> BeginEditAsync(int rowIndex, string columnName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Beginning edit for cell [{RowIndex}, {ColumnName}] via Editing module", rowIndex, columnName);

            var internalResult = await _cellEditService.BeginEditAsync(rowIndex, columnName, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "BeginEdit failed in Editing module");
            throw;
        }
    }

    public async Task<PublicResult> CommitEditAsync(object? newValue, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Committing edit via Editing module with value: {Value}", newValue);

            // Note: newValue parameter is ignored here because UpdateCellAsync already set the value
            // This parameter exists for backward compatibility in the public API
            var internalResult = await _cellEditService.CommitEditAsync(cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CommitEdit failed in Editing module");
            throw;
        }
    }

    public async Task<PublicResult> CancelEditAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Canceling edit via Editing module");

            var internalResult = await _cellEditService.CancelEditAsync(cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CancelEdit failed in Editing module");
            throw;
        }
    }

    public async Task<PublicResult> UpdateCellAsync(int rowIndex, string columnName, object? newValue, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Updating cell [{RowIndex}, {ColumnName}] via Editing module", rowIndex, columnName);

            var internalResult = await _cellEditService.UpdateCellAsync(rowIndex, columnName, newValue, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UpdateCell failed in Editing module");
            throw;
        }
    }

    public bool IsEditing()
    {
        try
        {
            return _cellEditService.IsEditing();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "IsEditing check failed in Editing module");
            throw;
        }
    }

    public PublicCellPosition? GetCurrentEditPosition()
    {
        try
        {
            var internalPosition = _cellEditService.GetCurrentEditPosition();
            if (internalPosition == null)
                return null;

            return new PublicCellPosition
            {
                RowIndex = internalPosition.Value.rowIndex,
                ColumnName = internalPosition.Value.columnName
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetCurrentEditPosition failed in Editing module");
            throw;
        }
    }

    public PublicResult SetEditingEnabled(bool enabled)
    {
        try
        {
            _logger?.LogInformation("Setting editing enabled to {Enabled} via Editing module", enabled);

            _cellEditService.SetEditingEnabled(enabled);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetEditingEnabled failed in Editing module");
            throw;
        }
    }

    public bool IsEditingEnabled()
    {
        try
        {
            return _cellEditService.IsEditingEnabled();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "IsEditingEnabled check failed in Editing module");
            throw;
        }
    }
}
