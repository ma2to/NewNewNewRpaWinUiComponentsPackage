using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.RowNumber.Interfaces;

/// <summary>
/// INTERNAL: Row number management service
/// ENTERPRISE: Professional row numbering with automatic regeneration
/// INTEGRATION: Triggered after Import/Add/Delete operations
/// </summary>
internal interface IRowNumberService
{
    /// <summary>
    /// CORE: Regenerate all row numbers with sequential ordering (1, 2, 3, ...)
    /// PERFORMANCE: Optimized bulk regeneration with minimal overhead
    /// SMART: Uses creation time as fallback ordering when RowNumbers are corrupted
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if regeneration succeeded</returns>
    Task<bool> RegenerateRowNumbersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// UTILITY: Get next available row number in sequence
    /// SMART: Automatic recovery from RowNumber inconsistencies
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next available row number (1-based)</returns>
    Task<int> GetNextRowNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// QUERY: Get current maximum row number
    /// FAST: O(1) cached lookup
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Maximum row number currently assigned</returns>
    Task<int> GetMaxRowNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// VALIDATION: Check if row numbers are valid and sequential
    /// DIAGNOSTIC: Detect gaps, duplicates, or corruption
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if row numbers are valid</returns>
    Task<bool> ValidateRowNumbersAsync(CancellationToken cancellationToken = default);
}
