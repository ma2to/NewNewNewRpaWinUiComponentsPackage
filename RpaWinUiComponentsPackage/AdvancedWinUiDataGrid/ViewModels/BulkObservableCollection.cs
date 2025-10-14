using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

/// <summary>
/// Enhanced ObservableCollection that supports bulk operations with single notification.
/// CRITICAL: Prevents 10M individual CollectionChanged events when adding 10M items.
/// Instead fires one Reset event after all items are added.
/// This is essential for performance with large datasets (100k+ rows).
/// </summary>
/// <typeparam name="T">Type of items in the collection</typeparam>
public class BulkObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotification;

    /// <summary>
    /// Adds a range of items to the collection with a single CollectionChanged notification.
    /// PERFORMANCE: For 10M items, this changes from 10M events → 1 event = massive speedup.
    /// </summary>
    /// <param name="items">Items to add to the collection</param>
    /// <remarks>
    /// Implementation:
    /// 1. Suppress notifications during bulk add
    /// 2. Add all items to internal Items collection
    /// 3. Fire single Reset notification after all items added
    ///
    /// Why Reset instead of Add?
    /// - Reset tells UI "everything changed, rebuild your view"
    /// - Add would require tracking indices which is slow for bulk operations
    /// - Reset is the recommended approach for bulk operations per Microsoft docs
    /// </remarks>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        // Suppress individual notifications
        _suppressNotification = true;

        try
        {
            // Add all items to internal collection
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
        finally
        {
            // Always restore notifications even if exception occurs
            _suppressNotification = false;
        }

        // Fire single Reset notification for entire batch
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Clears all items and adds new range with a single notification.
    /// Equivalent to Clear() + AddRange() but more efficient.
    /// </summary>
    /// <param name="items">New items to replace existing collection</param>
    public void ReplaceAll(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        _suppressNotification = true;

        try
        {
            // Clear existing items
            Items.Clear();

            // Add new items
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
        finally
        {
            _suppressNotification = false;
        }

        // Single Reset notification
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Removes a range of items with a single notification.
    /// </summary>
    /// <param name="items">Items to remove from the collection</param>
    public void RemoveRange(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        _suppressNotification = true;

        try
        {
            foreach (var item in items)
            {
                Items.Remove(item);
            }
        }
        finally
        {
            _suppressNotification = false;
        }

        // Single Reset notification
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Removes item at specified index with granular Remove notification for 10M+ row performance.
    /// Unlike RemoveRange which fires Reset, this fires granular Remove action.
    /// CRITICAL: Used by InternalUIUpdateHandler for granular UI updates (avoids full rebuild).
    /// </summary>
    /// <param name="index">Zero-based index of item to remove</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of bounds</exception>
    /// <remarks>
    /// Why separate from RemoveRange?
    /// - RemoveAt fires granular Remove action → UI only removes ONE row visual
    /// - RemoveRange fires Reset action → UI rebuilds ENTIRE view
    /// For 10M rows, granular Remove is instant, Reset causes 30s+ freeze + OOM
    /// </remarks>
    public new void RemoveAt(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Index {index} is out of range. Collection has {Items.Count} items.");
        }

        // Get item before removal for event args
        var removedItem = Items[index];

        // Remove from internal collection
        Items.RemoveAt(index);

        // Fire granular Remove notification (NOT Reset!)
        // This allows DataGridCellsView to do incremental removal instead of full rebuild
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Remove,
            removedItem,
            index));
    }

    /// <summary>
    /// Overrides CollectionChanged notification to support suppression during bulk operations.
    /// When _suppressNotification is true, individual notifications are blocked.
    /// </summary>
    /// <param name="e">Collection changed event arguments</param>
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        // Only fire notification if not suppressed
        if (!_suppressNotification)
        {
            base.OnCollectionChanged(e);
        }
    }
}
