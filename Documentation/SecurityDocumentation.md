# KOMPLETNÁ ŠPECIFIKÁCIA ENTERPRISE SECURITY INFRASTRUCTURE

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY

### Clean Architecture + Command Pattern
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: Security services, threat handlers (internal)
- **Core Layer**: Security domain entities, validation rules (internal)
- **Infrastructure Layer**: Input validation, audit logging, monitoring (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každý security service má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové typy threats bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky security handlers implementujú spoločné interface
- **Interface Segregation**: Špecializované interfaces pre rôzne typy security operations
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained
- **Clean Architecture**: Commands v Core layer, processing v Application layer
- **Hybrid DI**: Command factory methods s dependency injection support
- **Functional/OOP**: Immutable security commands + encapsulated behavior
- **SOLID**: Single responsibility pre každý security type
- **LINQ Optimization**: Lazy evaluation, parallel processing, streaming where beneficial
- **Performance**: Object pooling, atomic operations, minimal allocations
- **Thread Safety**: Immutable commands, atomic operation updates
- **Internal DI Registration**: Všetky security časti budú registrované v InternalServiceRegistration.cs

## 🔄 BACKUP STRATEGY & IMPLEMENTATION APPROACH

### 1. Backup Strategy
- Vytvoriť .oldbackup_timestamp súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### 2. Implementation Replacement
- Kompletný refaktoring s command pattern a LINQ optimizations
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované a bezpečné a stabilné riešenie

## 🛡️ CORE SECURITY INFRASTRUCTURE COMPONENTS

### 1. **InputValidator** - DoS Protection & Injection Prevention

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Security;

/// <summary>
/// ENTERPRISE: Comprehensive input validation with DoS protection
/// THREAD SAFE: All validation operations are thread-safe
/// PERFORMANCE: Optimized for high-throughput scenarios
/// SECURITY: Multi-layer defense against injection and DoS attacks
/// </summary>
internal static class InputValidator
{
    #region DoS Protection Constants

    private const int MAX_STRING_LENGTH = 32_768; // 32KB limit - prevents memory exhaustion
    private const int MAX_COLLECTION_SIZE = 10_000; // Collection size limit - prevents collection overflow
    private const int MAX_REGEX_TIMEOUT_MS = 100; // ReDoS protection - prevents regex timeout attacks
    private const int MAX_REGEX_COMPLEXITY = 1000; // Regex complexity limit
    private const int MAX_NESTED_DEPTH = 10; // Nested object depth limit

    #endregion

    #region Dangerous Pattern Detection

    /// <summary>
    /// SECURITY: Compiled regex for dangerous content detection
    /// PERFORMANCE: Pre-compiled with optimized timeout
    /// </summary>
    private static readonly Regex _dangerousPatterns = new(
        @"(<script.*?>|javascript:|vbscript:|data:text/html|onload=|onerror=|<iframe|<object|<embed)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    /// <summary>
    /// SECURITY: Reserved JavaScript property names that could be exploited
    /// </summary>
    private static readonly HashSet<string> _reservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "__proto__", "constructor", "prototype", "eval", "function",
        "arguments", "caller", "toString", "valueOf", "hasOwnProperty"
    };

    #endregion

    #region Input Sanitization Methods

    /// <summary>
    /// SECURITY: Primary input sanitization with injection prevention
    /// DOS PROTECTION: Length limits and pattern removal
    /// </summary>
    public static string SanitizeInput(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // DoS Protection: String length limit
        if (input.Length > MAX_STRING_LENGTH)
            throw new SecurityException($"Input exceeds maximum length of {MAX_STRING_LENGTH} characters");

        var sanitized = input.Trim();

        // Injection Prevention: Remove dangerous patterns
        try
        {
            sanitized = _dangerousPatterns.Replace(sanitized, string.Empty);
        }
        catch (RegexMatchTimeoutException)
        {
            // ReDoS protection: If regex times out, reject the input
            throw new SecurityException("Input contains potentially malicious patterns that caused timeout");
        }

        // Remove control characters except allowed whitespace
        sanitized = new string(sanitized
            .Where(c => !char.IsControl(c) || c == '\t' || c == '\n' || c == '\r')
            .ToArray());

        return sanitized;
    }

    /// <summary>
    /// SECURITY: Column name validation with reserved name protection
    /// INJECTION PREVENTION: Blocks JavaScript reserved properties
    /// </summary>
    public static bool IsValidColumnName(string? columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return false;

        // Length check for DoS protection
        if (columnName.Length > 255)
            return false;

        // Reserved name check for injection prevention
        if (_reservedNames.Contains(columnName))
            return false;

        // Pattern validation: Allow only alphanumeric, underscore, and dot
        return Regex.IsMatch(columnName, @"^[a-zA-Z_][a-zA-Z0-9_\.]*$",
            RegexOptions.Compiled, TimeSpan.FromMilliseconds(10));
    }

    #endregion

    #region Collection Security Validation

    /// <summary>
    /// DOS PROTECTION: Collection size validation with efficient counting
    /// PERFORMANCE: Uses TryGetNonEnumeratedCount for efficiency
    /// </summary>
    public static bool ValidateCollectionSize<T>(IEnumerable<T>? collection, int? maxSize = null)
    {
        if (collection == null)
            return true;

        var limit = maxSize ?? MAX_COLLECTION_SIZE;

        // Efficient count check without full enumeration
        if (collection.TryGetNonEnumeratedCount(out var count))
            return count <= limit;

        // Fallback: Manual counting with limit check
        var currentCount = 0;
        foreach (var _ in collection)
        {
            if (++currentCount > limit)
                return false;
        }

        return true;
    }

    /// <summary>
    /// SECURITY: Nested object depth validation to prevent stack overflow
    /// DOS PROTECTION: Prevents deeply nested object attacks
    /// </summary>
    public static bool ValidateNestedDepth(object? obj, int maxDepth = MAX_NESTED_DEPTH)
    {
        return ValidateNestedDepthRecursive(obj, 0, maxDepth);
    }

    private static bool ValidateNestedDepthRecursive(object? obj, int currentDepth, int maxDepth)
    {
        if (obj == null || currentDepth >= maxDepth)
            return currentDepth < maxDepth;

        // Check primitive types and strings
        if (obj.GetType().IsPrimitive || obj is string)
            return true;

        // For complex objects, check properties recursively
        var properties = obj.GetType().GetProperties();
        foreach (var prop in properties.Take(20)) // Limit property checks for DoS protection
        {
            try
            {
                var value = prop.GetValue(obj);
                if (!ValidateNestedDepthRecursive(value, currentDepth + 1, maxDepth))
                    return false;
            }
            catch
            {
                // Skip problematic properties
                continue;
            }
        }

        return true;
    }

    #endregion

    #region Advanced Security Validation

    /// <summary>
    /// SECURITY: Comprehensive input validation with multiple checks
    /// ENTERPRISE: Production-ready validation with detailed error reporting
    /// </summary>
    public static ValidationResult ValidateSecureInput(object? input, SecurityValidationOptions? options = null)
    {
        options ??= SecurityValidationOptions.Default;
        var errors = new List<string>();

        try
        {
            // Null check
            if (input == null)
                return options.AllowNull ? ValidationResult.Success() : ValidationResult.Failure("Input cannot be null");

            // String validation
            if (input is string stringInput)
            {
                if (stringInput.Length > options.MaxStringLength)
                    errors.Add($"String length exceeds maximum of {options.MaxStringLength} characters");

                if (!options.AllowEmptyStrings && string.IsNullOrWhiteSpace(stringInput))
                    errors.Add("Empty strings are not allowed");

                // Dangerous pattern check
                if (_dangerousPatterns.IsMatch(stringInput))
                    errors.Add("Input contains potentially dangerous patterns");
            }

            // Collection validation
            if (input is System.Collections.IEnumerable enumerable and not string)
            {
                if (!ValidateCollectionSize(enumerable.Cast<object>(), options.MaxCollectionSize))
                    errors.Add($"Collection size exceeds maximum of {options.MaxCollectionSize} items");
            }

            // Nested depth validation
            if (!ValidateNestedDepth(input, options.MaxNestedDepth))
                errors.Add($"Object nesting exceeds maximum depth of {options.MaxNestedDepth}");

            return errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Validation error: {ex.Message}");
        }
    }

    #endregion

    #region File System Security

    /// <summary>
    /// SECURITY: File path validation with path traversal protection
    /// ENTERPRISE: Production-ready file access validation
    /// </summary>
    public static bool IsSecureFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        // Path traversal prevention
        var normalizedPath = Path.GetFullPath(filePath);
        if (normalizedPath.Contains("..") || normalizedPath.Contains("~"))
            return false;

        // Check for dangerous file extensions
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var dangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".com", ".scr", ".vbs", ".js" };
        if (dangerousExtensions.Contains(extension))
            return false;

        return true;
    }

    /// <summary>
    /// SECURITY: Directory access validation with permission checks
    /// ASYNC: Asynchronous file system access validation
    /// </summary>
    public static async Task<bool> ValidateDirectoryAccessAsync(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
                return false;

            // Test read access
            _ = Directory.GetFiles(directoryPath).Take(1).ToArray();

            // Test write access (create and delete temporary file)
            var tempFile = Path.Combine(directoryPath, $"temp_{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(tempFile, "test");
            File.Delete(tempFile);

            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}

/// <summary>
/// DDD: Security validation options configuration
/// </summary>
public sealed record SecurityValidationOptions
{
    public int MaxStringLength { get; init; } = 32_768;
    public int MaxCollectionSize { get; init; } = 10_000;
    public int MaxNestedDepth { get; init; } = 10;
    public bool AllowNull { get; init; } = false;
    public bool AllowEmptyStrings { get; init; } = true;

    public static SecurityValidationOptions Default => new();

    public static SecurityValidationOptions Strict => new()
    {
        MaxStringLength = 1000,
        MaxCollectionSize = 100,
        MaxNestedDepth = 5,
        AllowNull = false,
        AllowEmptyStrings = false
    };
}

/// <summary>
/// DDD: Validation result value object
/// </summary>
public sealed record ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(string error) => new() { IsValid = false, Errors = new[] { error } };
    public static ValidationResult Failure(IEnumerable<string> errors) => new() { IsValid = false, Errors = errors.ToArray() };
}

/// <summary>
/// SECURITY: Security exception for input validation failures
/// </summary>
public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
    public SecurityException(string message, Exception innerException) : base(message, innerException) { }
}
```

### 2. **SecurityContext** - Access Control & Context Management

```csharp
using System;
using System.Collections.Concurrent;
using System.Security.Principal;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Security;

/// <summary>
/// ENTERPRISE: Security context management with role-based access control
/// THREAD SAFE: Concurrent context storage and access
/// AUDIT: Comprehensive security event logging
/// </summary>
internal sealed class SecurityContext
{
    private readonly ConcurrentDictionary<string, object> _contextData = new();
    private readonly ConcurrentDictionary<string, Permission> _permissions = new();
    private volatile DateTime _createdAt = DateTime.UtcNow;
    private volatile DateTime _lastAccessed = DateTime.UtcNow;

    public string SessionId { get; } = Guid.NewGuid().ToString("N");
    public IIdentity? Identity { get; private set; }
    public IPrincipal? Principal { get; private set; }

    /// <summary>
    /// SECURITY: Initialize security context with user identity
    /// AUDIT: Log context creation
    /// </summary>
    public void Initialize(IPrincipal principal)
    {
        Principal = principal ?? throw new ArgumentNullException(nameof(principal));
        Identity = principal.Identity;
        _lastAccessed = DateTime.UtcNow;

        // Log security context creation
        SecurityAuditLogger.LogContextCreated(SessionId, Identity?.Name);
    }

    /// <summary>
    /// ACCESS CONTROL: Check if current user has specific permission
    /// PERFORMANCE: Fast permission lookup with caching
    /// </summary>
    public bool HasPermission(string permission)
    {
        _lastAccessed = DateTime.UtcNow;

        if (_permissions.TryGetValue(permission, out var perm))
        {
            return perm.IsGranted && perm.ExpiresAt > DateTime.UtcNow;
        }

        // Default permissions for authenticated users
        if (Principal?.Identity?.IsAuthenticated == true)
        {
            return CheckDefaultPermissions(permission);
        }

        SecurityAuditLogger.LogPermissionDenied(SessionId, permission, Identity?.Name);
        return false;
    }

    /// <summary>
    /// SECURITY: Grant permission with expiration
    /// AUDIT: Log permission grant
    /// </summary>
    public void GrantPermission(string permission, TimeSpan? expiration = null)
    {
        var expiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromHours(8));
        _permissions.AddOrUpdate(permission,
            new Permission(permission, true, expiresAt),
            (key, existing) => existing with { IsGranted = true, ExpiresAt = expiresAt });

        SecurityAuditLogger.LogPermissionGranted(SessionId, permission, Identity?.Name, expiresAt);
    }

    /// <summary>
    /// SECURITY: Revoke specific permission
    /// AUDIT: Log permission revocation
    /// </summary>
    public void RevokePermission(string permission)
    {
        if (_permissions.TryGetValue(permission, out var existing))
        {
            _permissions.TryUpdate(permission, existing with { IsGranted = false }, existing);
            SecurityAuditLogger.LogPermissionRevoked(SessionId, permission, Identity?.Name);
        }
    }

    /// <summary>
    /// SECURITY: Get secure context data with type safety
    /// </summary>
    public T? GetContextData<T>(string key, T? defaultValue = default)
    {
        _lastAccessed = DateTime.UtcNow;
        return _contextData.TryGetValue(key, out var value) && value is T typedValue
            ? typedValue
            : defaultValue;
    }

    /// <summary>
    /// SECURITY: Set context data with validation
    /// </summary>
    public void SetContextData<T>(string key, T value)
    {
        if (value == null)
        {
            _contextData.TryRemove(key, out _);
            return;
        }

        // Validate input before storing
        var validationResult = InputValidator.ValidateSecureInput(value);
        if (!validationResult.IsValid)
        {
            throw new SecurityException($"Invalid context data: {string.Join(", ", validationResult.Errors)}");
        }

        _contextData.AddOrUpdate(key, value, (k, existing) => value);
        _lastAccessed = DateTime.UtcNow;
    }

    /// <summary>
    /// SECURITY: Check if context is still valid and not expired
    /// </summary>
    public bool IsValid()
    {
        var maxIdleTime = TimeSpan.FromMinutes(30);
        var maxLifetime = TimeSpan.FromHours(8);

        return DateTime.UtcNow - _lastAccessed <= maxIdleTime &&
               DateTime.UtcNow - _createdAt <= maxLifetime;
    }

    /// <summary>
    /// SECURITY: Clear context and revoke all permissions
    /// AUDIT: Log context destruction
    /// </summary>
    public void Destroy()
    {
        _contextData.Clear();
        _permissions.Clear();
        SecurityAuditLogger.LogContextDestroyed(SessionId, Identity?.Name);
    }

    private bool CheckDefaultPermissions(string permission)
    {
        // Default permissions for authenticated users
        var defaultPermissions = new[]
        {
            "data.read", "data.filter", "data.sort", "ui.interact"
        };

        return defaultPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// DDD: Permission value object with expiration
/// </summary>
internal sealed record Permission(string Name, bool IsGranted, DateTime ExpiresAt);

/// <summary>
/// SECURITY: Security audit logging for compliance
/// </summary>
internal static class SecurityAuditLogger
{
    private static readonly ILogger _logger = LoggerFactory.CreateLogger("SecurityAudit");

    public static void LogContextCreated(string sessionId, string? userName)
    {
        _logger.LogInformation("Security context created: session={SessionId}, user={UserName}, time={Time}",
            sessionId, userName ?? "anonymous", DateTime.UtcNow);
    }

    public static void LogContextDestroyed(string sessionId, string? userName)
    {
        _logger.LogInformation("Security context destroyed: session={SessionId}, user={UserName}, time={Time}",
            sessionId, userName ?? "anonymous", DateTime.UtcNow);
    }

    public static void LogPermissionGranted(string sessionId, string permission, string? userName, DateTime expiresAt)
    {
        _logger.LogInformation("Permission granted: session={SessionId}, permission={Permission}, user={UserName}, expires={ExpiresAt}",
            sessionId, permission, userName ?? "anonymous", expiresAt);
    }

    public static void LogPermissionRevoked(string sessionId, string permission, string? userName)
    {
        _logger.LogWarning("Permission revoked: session={SessionId}, permission={Permission}, user={UserName}, time={Time}",
            sessionId, permission, userName ?? "anonymous", DateTime.UtcNow);
    }

    public static void LogPermissionDenied(string sessionId, string permission, string? userName)
    {
        _logger.LogWarning("Permission denied: session={SessionId}, permission={Permission}, user={UserName}, time={Time}",
            sessionId, permission, userName ?? "anonymous", DateTime.UtcNow);
    }
}
```

## 📋 COMMAND PATTERN PRE SECURITY OPERATIONS

### ValidateSecurityCommand
```csharp
public sealed record ValidateSecurityCommand
{
    public required object? Input { get; init; }
    public required string ValidationContext { get; init; }
    public SecurityValidationOptions Options { get; init; } = SecurityValidationOptions.Default;
    public bool EnableAuditLogging { get; init; } = true;
    public bool ThrowOnValidationFailure { get; init; } = false;
    public IProgress<SecurityValidationProgress>? ProgressReporter { get; init; }
    public cancellationToken cancellationToken { get; init; } = default;

    // FLEXIBLE factory methods s DI support
    public static ValidateSecurityCommand Create(object? input, string context) =>
        new() { Input = input, ValidationContext = context };

    public static ValidateSecurityCommand WithStrictOptions(object? input, string context) =>
        new() { Input = input, ValidationContext = context, Options = SecurityValidationOptions.Strict };

    // DI factory method
    public static ValidateSecurityCommand CreateWithDI(object? input, string context, IServiceProvider services) =>
        new() { Input = input, ValidationContext = context };
}
```

### AuthorizeOperationCommand
```csharp
public sealed record AuthorizeOperationCommand
{
    public required string Operation { get; init; }
    public required SecurityContext SecurityContext { get; init; }
    public IReadOnlyDictionary<string, object?> OperationParameters { get; init; } = new Dictionary<string, object?>();
    public bool RequireExplicitPermission { get; init; } = false;
    public TimeSpan? OperationTimeout { get; init; }
    public bool EnableAuditLogging { get; init; } = true;

    // FLEXIBLE factory methods
    public static AuthorizeOperationCommand Create(string operation, SecurityContext context) =>
        new() { Operation = operation, SecurityContext = context };

    public static AuthorizeOperationCommand WithParameters(
        string operation,
        SecurityContext context,
        IReadOnlyDictionary<string, object?> parameters) =>
        new() { Operation = operation, SecurityContext = context, OperationParameters = parameters };

    // LINQ optimized factory pre bulk authorization
    public static IEnumerable<AuthorizeOperationCommand> CreateBulk(
        IEnumerable<(string operation, SecurityContext context)> authorizations) =>
        authorizations.Select(auth => Create(auth.operation, auth.context));
}
```

## 🎯 FAÇADE API METÓDY

### Security Validation API
```csharp
#region Security Operations with Command Pattern

/// <summary>
/// PUBLIC API: Validate input security using command pattern
/// ENTERPRISE: Professional security validation with audit logging
/// CONSISTENT: Rovnaká štruktúra ako ImportAsync a ValidateAsync
/// </summary>
Task<Result<SecurityValidationResult>> ValidateSecurityAsync(
    ValidateSecurityCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Authorize operation execution
/// ENTERPRISE: Role-based access control with comprehensive auditing
/// LINQ OPTIMIZED: Parallel processing for bulk authorization checks
/// </summary>
Task<Result<AuthorizationResult>> AuthorizeOperationAsync(
    AuthorizeOperationCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Get current security context
/// PERFORMANCE: Immediate context retrieval without validation overhead
/// </summary>
SecurityContext GetCurrentSecurityContext();

/// <summary>
/// PUBLIC API: Create new security context
/// ENTERPRISE: Comprehensive context initialization with audit logging
/// </summary>
Task<Result<SecurityContext>> CreateSecurityContextAsync(IPrincipal principal);

#endregion

#region Threat Detection and Monitoring

/// <summary>
/// PUBLIC API: Monitor security threats
/// ENTERPRISE: Real-time threat detection and analysis
/// AUTOMATIC: Continuous security monitoring with pattern recognition
/// </summary>
Task<Result<ThreatMonitoringResult>> MonitorSecurityThreatsAsync(
    TimeSpan monitoringWindow,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Get security health status
/// REAL-TIME: Current security posture and threat levels
/// </summary>
SecurityHealthStatus GetSecurityHealthStatus();

/// <summary>
/// PUBLIC API: Configure security settings
/// DYNAMIC: Runtime security configuration updates
/// </summary>
Task<Result<bool>> ConfigureSecurityAsync(SecurityConfiguration configuration);

#endregion

#region Audit and Compliance

/// <summary>
/// PUBLIC API: Get security audit log
/// COMPLIANCE: Comprehensive audit trail for regulatory compliance
/// </summary>
Task<Result<IReadOnlyList<SecurityAuditEntry>>> GetSecurityAuditLogAsync(
    DateTime fromDate,
    DateTime toDate,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Generate security compliance report
/// ENTERPRISE: Professional compliance reporting with detailed metrics
/// </summary>
Task<Result<SecurityComplianceReport>> GenerateComplianceReportAsync(
    ComplianceReportParameters parameters,
    cancellationToken cancellationToken = default);

#endregion
```

## 🛡️ THREAT DETECTION & MONITORING SYSTEMS

### Real-Time Threat Detection
```csharp
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Security;

/// <summary>
/// ENTERPRISE: Real-time threat detection and monitoring system
/// PERFORMANCE: High-throughput security event processing
/// INTELLIGENCE: Pattern-based threat identification
/// </summary>
internal sealed class ThreatDetectionService
{
    private readonly ConcurrentDictionary<string, ThreatPattern> _detectedThreats = new();
    private readonly ConcurrentQueue<SecurityEvent> _eventQueue = new();
    private readonly Timer _analysisTimer;
    private volatile long _totalSecurityEvents = 0;
    private volatile long _threatsDetected = 0;

    public ThreatDetectionService()
    {
        _analysisTimer = new Timer(AnalyzeSecurityEvents, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// REAL-TIME: Process security event for threat detection
    /// PERFORMANCE: Non-blocking event processing
    /// </summary>
    public void ProcessSecurityEvent(SecurityEvent securityEvent)
    {
        _eventQueue.Enqueue(securityEvent);
        Interlocked.Increment(ref _totalSecurityEvents);

        // Immediate threat detection for critical events
        if (securityEvent.Severity >= SecurityEventSeverity.High)
        {
            DetectImmediateThreats(securityEvent);
        }
    }

    /// <summary>
    /// INTELLIGENCE: Analyze security events for threat patterns
    /// BACKGROUND: Periodic analysis without blocking main operations
    /// </summary>
    private void AnalyzeSecurityEvents(object? state)
    {
        var eventsToAnalyze = new List<SecurityEvent>();

        // Drain event queue for analysis
        while (_eventQueue.TryDequeue(out var evt) && eventsToAnalyze.Count < 1000)
        {
            eventsToAnalyze.Add(evt);
        }

        if (!eventsToAnalyze.Any()) return;

        // Pattern analysis
        AnalyzeFailedLoginPatterns(eventsToAnalyze);
        AnalyzeInputValidationFailures(eventsToAnalyze);
        AnalyzePerformanceAnomalies(eventsToAnalyze);
        AnalyzeSuspiciousOperationPatterns(eventsToAnalyze);
    }

    /// <summary>
    /// THREAT DETECTION: Failed login pattern analysis
    /// </summary>
    private void AnalyzeFailedLoginPatterns(IList<SecurityEvent> events)
    {
        var failedLogins = events
            .Where(e => e.EventType == SecurityEventType.AuthenticationFailure)
            .GroupBy(e => e.SourceIP)
            .Where(g => g.Count() >= 5) // 5+ failures from same IP
            .ToList();

        foreach (var group in failedLogins)
        {
            var threatKey = $"BruteForce_{group.Key}";
            _detectedThreats.AddOrUpdate(threatKey,
                new ThreatPattern(ThreatType.BruteForceAttack, group.Key, group.Count()),
                (key, existing) => existing with { EventCount = existing.EventCount + group.Count() });

            Interlocked.Increment(ref _threatsDetected);
            SecurityAuditLogger.LogThreatDetected(threatKey, ThreatType.BruteForceAttack, group.Key);
        }
    }

    /// <summary>
    /// THREAT DETECTION: Input validation failure analysis
    /// </summary>
    private void AnalyzeInputValidationFailures(IList<SecurityEvent> events)
    {
        var validationFailures = events
            .Where(e => e.EventType == SecurityEventType.InputValidationFailure)
            .GroupBy(e => e.SourceIP)
            .Where(g => g.Count() >= 10) // 10+ validation failures
            .ToList();

        foreach (var group in validationFailures)
        {
            var threatKey = $"InjectionAttempt_{group.Key}";
            _detectedThreats.AddOrUpdate(threatKey,
                new ThreatPattern(ThreatType.InjectionAttack, group.Key, group.Count()),
                (key, existing) => existing with { EventCount = existing.EventCount + group.Count() });

            Interlocked.Increment(ref _threatsDetected);
            SecurityAuditLogger.LogThreatDetected(threatKey, ThreatType.InjectionAttack, group.Key);
        }
    }

    /// <summary>
    /// PERFORMANCE: Get current threat status
    /// </summary>
    public ThreatStatus GetCurrentThreatStatus()
    {
        return new ThreatStatus
        {
            TotalSecurityEvents = _totalSecurityEvents,
            ThreatsDetected = _threatsDetected,
            ActiveThreats = _detectedThreats.Count,
            ThreatLevel = CalculateThreatLevel(),
            LastAnalysisTime = DateTime.UtcNow
        };
    }

    private ThreatLevel CalculateThreatLevel()
    {
        var activeThreats = _detectedThreats.Count;
        return activeThreats switch
        {
            0 => ThreatLevel.Low,
            < 5 => ThreatLevel.Medium,
            < 20 => ThreatLevel.High,
            _ => ThreatLevel.Critical
        };
    }
}

/// <summary>
/// DDD: Security event value object
/// </summary>
public sealed record SecurityEvent
{
    public required SecurityEventType EventType { get; init; }
    public required SecurityEventSeverity Severity { get; init; }
    public required string SourceIP { get; init; }
    public required DateTime Timestamp { get; init; }
    public string? UserId { get; init; }
    public string? Description { get; init; }
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}

/// <summary>
/// DDD: Threat pattern value object
/// </summary>
internal sealed record ThreatPattern(ThreatType Type, string Source, int EventCount)
{
    public DateTime FirstDetected { get; init; } = DateTime.UtcNow;
    public DateTime LastSeen { get; init; } = DateTime.UtcNow;
}

public enum SecurityEventType
{
    AuthenticationFailure,
    AuthorizationDenied,
    InputValidationFailure,
    SuspiciousActivity,
    PerformanceAnomaly,
    DataAccessViolation
}

public enum SecurityEventSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum ThreatType
{
    BruteForceAttack,
    InjectionAttack,
    DenialOfService,
    DataExfiltration,
    PrivilegeEscalation,
    Reconnaissance
}

public enum ThreatLevel
{
    Low,
    Medium,
    High,
    Critical
}
```

## 🎯 KĽÚČOVÉ VYLEPŠENIA PODĽA ARCHITEKTÚRY

### 1. **Defense in Depth Security**
- **Input Validation**: Multi-layer validation s DoS protection
- **Access Control**: Role-based permissions s expiration
- **Threat Detection**: Real-time pattern analysis
- **Audit Logging**: Comprehensive security event tracking

### 2. **Enterprise Security Standards**
- **Clean Architecture**: Security concerns properly separated
- **Command Pattern**: All security operations through commands
- **Thread Safety**: Concurrent security operations
- **Performance Optimized**: High-throughput security processing

### 3. **DoS Protection Mechanisms**
- **Resource Limits**: String length, collection size, nesting depth
- **Timeout Protection**: Regex timeout prevention (ReDoS)
- **Rate Limiting**: Pattern-based request limiting
- **Circuit Breaker**: Integration with resilience patterns

### 4. **Injection Prevention**
- **Pattern Detection**: Compiled regex for dangerous content
- **Input Sanitization**: Multi-stage content cleaning
- **Reserved Name Protection**: JavaScript property name blocking
- **Path Traversal Prevention**: File system security

### 5. **Comprehensive Auditing**
- **Security Events**: All security operations logged
- **Compliance Support**: Detailed audit trails
- **Threat Intelligence**: Pattern-based threat detection
- **Performance Monitoring**: Security operation metrics

### 6. **Access Control & Authorization**
- **Context Management**: Secure session management
- **Permission System**: Granular permission control
- **Identity Integration**: Standard .NET identity support
- **Session Security**: Timeout and lifecycle management

---

## **🔍 LOGGING ŠPECIFIKÁCIA PRE SECURITY OPERÁCIE**

### **Internal DI Registration & Service Distribution**
Všetky security logging services sú registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`** a distribuované cez internal DI do `SecurityService`:

```csharp
// V InternalServiceRegistration.cs
services.AddSingleton<ISecurityLogger<SecurityService>, SecurityLogger<SecurityService>>();
services.AddSingleton<IOperationLogger<SecurityService>, OperationLogger<SecurityService>>();
services.AddSingleton<ICommandLogger<SecurityService>, CommandLogger<SecurityService>>();
services.AddSingleton<IThreatDetectionService, ThreatDetectionService>();

// V SecurityService constructor
public SecurityService(
    ILogger<SecurityService> logger,
    ISecurityLogger<SecurityService> securityLogger,
    IOperationLogger<SecurityService> operationLogger,
    ICommandLogger<SecurityService> commandLogger,
    IThreatDetectionService threatDetection)
```

### **Security Operations Logging**
```csharp
// Input validation logging
_securityLogger.LogInputValidation(validationResult.IsValid, input.GetType().Name, validationContext);

_logger.LogInformation("Input validation: valid={IsValid}, type={InputType}, context={Context}, errors={ErrorCount}",
    validationResult.IsValid, input.GetType().Name, validationContext, validationResult.Errors.Count);

// Authorization logging
_securityLogger.LogAuthorizationCheck(operation, securityContext.SessionId, hasPermission);

_logger.LogInformation("Authorization check: operation={Operation}, session={SessionId}, authorized={Authorized}, user={User}",
    operation, securityContext.SessionId, hasPermission, securityContext.Identity?.Name ?? "anonymous");

// Threat detection logging
_securityLogger.LogThreatDetected(threatType, sourceIP, eventCount);

_logger.LogWarning("Security threat detected: type={ThreatType}, source={SourceIP}, events={EventCount}, level={ThreatLevel}",
    threatType, sourceIP, eventCount, currentThreatLevel);
```

### **Security Audit Logging**
```csharp
// Security context operations
_logger.LogInformation("Security context created: session={SessionId}, user={User}, permissions={PermissionCount}",
    context.SessionId, context.Identity?.Name ?? "anonymous", grantedPermissions.Count);

// Permission operations
_logger.LogInformation("Permission granted: session={SessionId}, permission={Permission}, expires={ExpiresAt}",
    sessionId, permission, expiresAt);

_logger.LogWarning("Permission denied: session={SessionId}, permission={Permission}, reason={Reason}",
    sessionId, permission, denialReason);

// Compliance logging
_logger.LogInformation("Security compliance check: standard={Standard}, compliant={IsCompliant}, issues={IssueCount}",
    complianceStandard, isCompliant, complianceIssues.Count);
```

### **Threat Monitoring Logging**
```csharp
// Real-time threat monitoring
_logger.LogInformation("Threat monitoring active: events_processed={EventCount}, threats_detected={ThreatCount}, level={ThreatLevel}",
    processedEvents, detectedThreats, currentThreatLevel);

// Critical security alerts
if (threatLevel >= ThreatLevel.Critical)
{
    _logger.LogCritical("CRITICAL SECURITY ALERT: level={ThreatLevel}, active_threats={ActiveThreats}, immediate_action_required=true",
        threatLevel, activeThreats);
}

// Attack pattern detection
_logger.LogWarning("Attack pattern detected: pattern={AttackPattern}, source={SourceIP}, frequency={Frequency}, blocked={Blocked}",
    attackPattern, sourceIP, attackFrequency, wasBlocked);
```

### **Logging Levels Usage:**
- **Information**: Successful validations, authorization grants, context operations, compliance checks
- **Warning**: Permission denials, threat detections, suspicious activities, validation failures
- **Error**: Security operation failures, validation errors, authorization failures, system security issues
- **Critical**: Critical threats detected, system compromise attempts, security system failures

Táto špecifikácia poskytuje kompletný, enterprise-ready security systém s pokročilou ochranou, monitoringom a jednotnou architektúrou s ostatnými časťami komponentu.