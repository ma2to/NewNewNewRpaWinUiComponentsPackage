using System;
using System.Collections.Generic;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// CORE: Column definition for initialization with comprehensive configuration
/// ENTERPRISE: Professional column setup with validation, formatting, and behavior
/// CLEAN ARCHITECTURE: Core domain value object for column specification
/// </summary>
internal sealed record ColumnDefinition
{
    public string Name { get; }
    public string? DisplayName { get; }
    public Type DataType { get; }
    public bool IsVisible { get; }
    public bool IsReadOnly { get; }
    public bool IsSortable { get; }
    public bool IsFilterable { get; }
    public bool IsResizable { get; }
    public double? Width { get; }
    public double? MinWidth { get; }
    public double? MaxWidth { get; }
    public string? Format { get; }
    public object? DefaultValue { get; }
    public IReadOnlyList<IValidationRule>? ValidationRules { get; }
    public ValidationLogicalOperator ValidationOperator { get; }
    public ColumnValidationPolicy ValidationPolicy { get; }
    public ValidationEvaluationStrategy ValidationStrategy { get; }
    public Dictionary<string, object>? CustomProperties { get; }
    public bool IsRequired { get; }
    public string? Tooltip { get; }
    public string? PlaceholderText { get; }
    public SpecialColumnType SpecialType { get; }

    public ColumnDefinition(
        string name,
        Type dataType,
        string? displayName = null,
        bool isVisible = true,
        bool isReadOnly = false,
        bool isSortable = true,
        bool isFilterable = true,
        bool isResizable = true,
        double? width = null,
        double? minWidth = null,
        double? maxWidth = null,
        string? format = null,
        object? defaultValue = null,
        IReadOnlyList<IValidationRule>? validationRules = null,
        ValidationLogicalOperator validationOperator = ValidationLogicalOperator.And,
        ColumnValidationPolicy validationPolicy = ColumnValidationPolicy.ValidateAll,
        ValidationEvaluationStrategy validationStrategy = ValidationEvaluationStrategy.Sequential,
        Dictionary<string, object>? customProperties = null,
        bool isRequired = false,
        string? tooltip = null,
        string? placeholderText = null,
        SpecialColumnType specialType = SpecialColumnType.None)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        DisplayName = displayName ?? name;
        IsVisible = isVisible;
        IsReadOnly = isReadOnly;
        IsSortable = isSortable;
        IsFilterable = isFilterable;
        IsResizable = isResizable;
        Width = width;
        MinWidth = minWidth;
        MaxWidth = maxWidth;
        Format = format;
        DefaultValue = defaultValue;
        ValidationRules = validationRules;
        ValidationOperator = validationOperator;
        ValidationPolicy = validationPolicy;
        ValidationStrategy = validationStrategy;
        CustomProperties = customProperties;
        IsRequired = isRequired;
        Tooltip = tooltip;
        PlaceholderText = placeholderText;
        SpecialType = specialType;

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Column name cannot be null or empty", nameof(name));
    }

    #region Factory Methods

    /// <summary>Create a simple text column</summary>
    public static ColumnDefinition CreateText(string name, string? displayName = null, bool isRequired = false) =>
        new(name, typeof(string), displayName, isRequired: isRequired);

    /// <summary>Create a numeric column</summary>
    public static ColumnDefinition CreateNumber(string name, string? displayName = null, string? format = null, bool isRequired = false) =>
        new(name, typeof(decimal), displayName, format: format, isRequired: isRequired);

    /// <summary>Create a date column</summary>
    public static ColumnDefinition CreateDate(string name, string? displayName = null, string? format = null, bool isRequired = false) =>
        new(name, typeof(DateTime), displayName, format: format ?? "yyyy-MM-dd", isRequired: isRequired);

    /// <summary>Create a boolean column</summary>
    public static ColumnDefinition CreateBoolean(string name, string? displayName = null, bool defaultValue = false) =>
        new(name, typeof(bool), displayName, defaultValue: defaultValue);

    /// <summary>Create a read-only column</summary>
    public static ColumnDefinition CreateReadOnly(string name, Type dataType, string? displayName = null) =>
        new(name, dataType, displayName, isReadOnly: true);

    /// <summary>Create a hidden column</summary>
    public static ColumnDefinition CreateHidden(string name, Type dataType) =>
        new(name, dataType, isVisible: false);

    /// <summary>Create a column with validation rules</summary>
    public static ColumnDefinition CreateWithValidation(
        string name,
        Type dataType,
        IReadOnlyList<IValidationRule> validationRules,
        ValidationLogicalOperator validationOperator = ValidationLogicalOperator.And,
        string? displayName = null) =>
        new(name, dataType, displayName, validationRules: validationRules, validationOperator: validationOperator);

    #endregion
}

/// <summary>
/// CORE: Comprehensive initialization configuration
/// ENTERPRISE: Professional setup with all component aspects
/// CLEAN ARCHITECTURE: Core configuration value object
/// </summary>
internal sealed record InitializationConfiguration
{
    public IReadOnlyList<ColumnDefinition> Columns { get; }
    public ColorConfiguration? ColorTheme { get; }
    public PerformanceConfiguration? Performance { get; }
    public ValidationConfiguration? ValidationConfig { get; }
    public GridBehaviorConfiguration? Behavior { get; }
    public Dictionary<string, object>? CustomSettings { get; }
    public bool IsHeadlessMode { get; }
    public string? ConfigurationName { get; }

    public InitializationConfiguration(
        IReadOnlyList<ColumnDefinition> columns,
        ColorConfiguration? colorTheme = null,
        PerformanceConfiguration? performance = null,
        ValidationConfiguration? validationConfig = null,
        GridBehaviorConfiguration? behavior = null,
        Dictionary<string, object>? customSettings = null,
        bool isHeadlessMode = false,
        string? configurationName = null)
    {
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        ColorTheme = colorTheme;
        Performance = performance;
        ValidationConfig = validationConfig;
        Behavior = behavior;
        CustomSettings = customSettings;
        IsHeadlessMode = isHeadlessMode;
        ConfigurationName = configurationName;

        if (!columns.Any())
            throw new ArgumentException("At least one column must be defined", nameof(columns));
    }
}

/// <summary>
/// CORE: Grid behavior configuration for smart features
/// ENTERPRISE: Professional behavior control with automation
/// CLEAN ARCHITECTURE: Core behavior value object
/// </summary>
internal sealed record GridBehaviorConfiguration
{
    public bool EnableSmartDelete { get; }
    public bool EnableSmartExpand { get; }
    public bool EnableAutoSave { get; }
    public bool EnableInlineEditing { get; }
    public bool EnableBulkOperations { get; }
    public bool EnableKeyboardNavigation { get; }
    public bool EnableRowSelection { get; }
    public bool EnableMultiSelect { get; }
    public bool EnableColumnReordering { get; }
    public bool EnableExport { get; }
    public TimeSpan AutoSaveInterval { get; }
    public int MaxRowsForSmartOperations { get; }
    public Dictionary<string, object>? CustomBehaviors { get; }

    public GridBehaviorConfiguration(
        bool enableSmartDelete = true,
        bool enableSmartExpand = true,
        bool enableAutoSave = false,
        bool enableInlineEditing = true,
        bool enableBulkOperations = true,
        bool enableKeyboardNavigation = true,
        bool enableRowSelection = true,
        bool enableMultiSelect = false,
        bool enableColumnReordering = true,
        bool enableExport = true,
        TimeSpan? autoSaveInterval = null,
        int maxRowsForSmartOperations = 10000,
        Dictionary<string, object>? customBehaviors = null)
    {
        EnableSmartDelete = enableSmartDelete;
        EnableSmartExpand = enableSmartExpand;
        EnableAutoSave = enableAutoSave;
        EnableInlineEditing = enableInlineEditing;
        EnableBulkOperations = enableBulkOperations;
        EnableKeyboardNavigation = enableKeyboardNavigation;
        EnableRowSelection = enableRowSelection;
        EnableMultiSelect = enableMultiSelect;
        EnableColumnReordering = enableColumnReordering;
        EnableExport = enableExport;
        AutoSaveInterval = autoSaveInterval ?? TimeSpan.FromMinutes(5);
        MaxRowsForSmartOperations = maxRowsForSmartOperations;
        CustomBehaviors = customBehaviors;

        if (maxRowsForSmartOperations <= 0)
            throw new ArgumentException("Max rows for smart operations must be positive", nameof(maxRowsForSmartOperations));
    }

    #region Factory Methods

    /// <summary>Create configuration for UI mode with full features</summary>
    public static GridBehaviorConfiguration CreateForUI() =>
        new(enableSmartDelete: true, enableSmartExpand: true, enableInlineEditing: true, enableBulkOperations: true);

    /// <summary>Create configuration for headless mode with minimal UI features</summary>
    public static GridBehaviorConfiguration CreateForHeadless() =>
        new(enableSmartDelete: true, enableSmartExpand: true, enableInlineEditing: false, enableBulkOperations: true,
            enableKeyboardNavigation: false, enableColumnReordering: false);

    /// <summary>Create read-only configuration</summary>
    public static GridBehaviorConfiguration CreateReadOnly() =>
        new(enableSmartDelete: false, enableSmartExpand: true, enableInlineEditing: false, enableBulkOperations: false,
            enableRowSelection: true, enableMultiSelect: true);

    #endregion
}