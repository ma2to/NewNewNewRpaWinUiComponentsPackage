using System;
using System.Collections.Generic;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;

/// <summary>
/// PUBLIC API: Base interface for all validation rules
/// ENTERPRISE: Enables external validation rule implementations
/// </summary>
public interface IValidationRule
{
    /// <summary>Unique name for the validation rule</summary>
    string? RuleName { get; }

    /// <summary>Error message when validation fails</summary>
    string ErrorMessage { get; }

    /// <summary>Severity level of validation failure</summary>
    ValidationSeverity Severity { get; }

    /// <summary>Priority for rule execution (lower = higher priority)</summary>
    int? Priority { get; }

    /// <summary>Timeout for rule execution</summary>
    TimeSpan? Timeout { get; }

    /// <summary>Effective timeout with fallback to default</summary>
    TimeSpan EffectiveTimeout { get; }
}

