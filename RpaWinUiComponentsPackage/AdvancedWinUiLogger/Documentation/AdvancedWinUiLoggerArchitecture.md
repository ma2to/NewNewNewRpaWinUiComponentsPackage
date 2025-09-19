# AdvancedWinUiLogger - Enterprise Logging Component

## Overview

AdvancedWinUiLogger je vysoko výkonný, enterprise-grade logging komponent navrhnutý podľa Clean Architecture princípov. Poskytuje komplexnú funkcionalitu pre file-based logging s automatickou rotáciou, správou súborov a pokročilými možnosťami konfigurácie.

## Architektúra

### Clean Architecture Implementation

```
┌─────────────────┐
│   Presentation  │ ← API Layer (AdvancedWinUiLogger.cs)
├─────────────────┤
│   Application   │ ← Use Cases, Interfaces, Services
├─────────────────┤
│ Infrastructure  │ ← File Operations, Persistence
├─────────────────┤
│      Core       │ ← Entities, Value Objects, Interfaces
└─────────────────┘
```

### 1. Core Layer (Domain)

**Zodpovednosti:**
- Definuje základné business entity a value objects
- Obsahuje core interfaces bez závislostí na externe knižnice
- Implementuje business logic a domain rules

**Kľúčové komponenty:**
- `LoggerSession` - Aggregate root pre logging session
- `LogEntry` - Immutable value object pre log záznamy
- `LoggerConfiguration` - Value object pre konfiguráciu
- `Result<T>` - Functional error handling monad
- `LogFileInfo` - File metadata value object

### 2. Application Layer

**Zodpovednosti:**
- Orchestrácia business operácií
- Use cases implementácia
- Application services a interfaces

**Kľúčové komponenty:**
- `ILoggerService` - Hlavný application service interface
- `WriteLogEntryUseCase` - Use case pre zápis log entries
- `RotateLogFileUseCase` - Use case pre file rotáciu
- `LoggerApi` - Public API facade

### 3. Infrastructure Layer

**Zodpovednosti:**
- File system operácie
- External dependencies implementation
- Persistence mechanizmy

**Kľúčové komponenty:**
- `FileLoggerRepository` - File-based persistence
- `FileRotationService` - File rotácia a cleanup
- `ConfigurationValidator` - Konfigurácia validácia

### 4. Presentation Layer (API)

**Zodpovednosti:**
- Public API pre external consuming
- Unified entry point
- API contracts a DTOs

## Kľúčové Funkcionality

### 1. **High-Performance Logging**
- Asynchronný batch writing
- Optimalizované memory management
- Thread-safe operácie
- Konfigurovateľné buffer sizes

### 2. **Automatic File Rotation**
- Size-based rotation
- Time-based rotation
- Configurable retention policies
- Automatic cleanup of old files

### 3. **Enterprise Configuration**
- Environment-specific defaults
- Runtime configuration updates
- Comprehensive validation
- Production-ready templates

### 4. **Monitoring & Diagnostics**
- Real-time performance metrics
- Health checking capabilities
- Session status tracking
- Telemetry collection

### 5. **Error Handling**
- Functional Result monad pattern
- Comprehensive error reporting
- Graceful degradation
- Automatic retry mechanisms

## API Usage Examples

### Basic Setup

```csharp
using RpaWinUiComponentsPackage.AdvancedWinUiLogger;

// Simple logger setup
var loggerResult = AdvancedWinUiLogger.CreateSimpleLogger(@"C:\Logs", "MyApp");
if (loggerResult.IsSuccess)
{
    var logger = loggerResult.Value;
    logger.LogInformation("Application started");
}
```

### Production Setup

```csharp
// High-performance logger for production
var productionLogger = AdvancedWinUiLogger.CreateProductionLogger(@"C:\Logs\Production", "MyEnterpriseApp");

// Environment-specific logger
var envLogger = AdvancedWinUiLogger.CreateEnvironmentLogger("Production", @"C:\Logs", "MyApp");
```

### Custom Configuration

```csharp
// Custom configuration
var configResult = AdvancedWinUiLogger.CreateHighPerformanceConfiguration(@"C:\Logs", "CustomApp");
if (configResult.IsSuccess)
{
    var config = configResult.Value.WithMinLogLevel(LogLevel.Warning);
    var customLogger = AdvancedWinUiLogger.CreateCustomLogger(config);
}
```

### File Management

```csharp
// Manual file rotation
await AdvancedWinUiLogger.RotateLogFileAsync(@"C:\Logs\current.log");

// Cleanup old files
var deletedCount = await AdvancedWinUiLogger.CleanupOldLogsAsync(
    @"C:\Logs",
    maxAgeDays: 30,
    maxFileCount: 100);

// Get log files information
var logFilesResult = await AdvancedWinUiLogger.GetLogFilesAsync(@"C:\Logs");
```

### Health Monitoring

```csharp
// Health check
var healthResult = await AdvancedWinUiLogger.CheckHealthAsync();
if (healthResult.IsSuccess && healthResult.Value)
{
    Console.WriteLine("Logger is healthy");
}

// Performance metrics
var metricsResult = AdvancedWinUiLogger.GetPerformanceMetrics();
if (metricsResult.IsSuccess)
{
    var metrics = metricsResult.Value;
    // Process metrics data
}
```

## Configuration Options

### LoggerConfiguration Properties

| Property | Description | Default | Range |
|----------|-------------|---------|--------|
| `LogDirectory` | Directory for log files | Required | Valid path |
| `BaseFileName` | Base name for log files | "application" | 1-100 chars |
| `MaxFileSizeMB` | Max file size before rotation | 10 MB | 1-1024 MB |
| `MaxLogFiles` | Maximum files to retain | 10 | 1-1000 |
| `MinLogLevel` | Minimum log level | Information | Trace-Critical |
| `BufferSize` | Entries buffer size | 1000 | 10-50000 |
| `FlushInterval` | Auto-flush interval | 5 seconds | 1-300 seconds |
| `EnableAutoRotation` | Enable automatic rotation | true | true/false |
| `EnableBackgroundLogging` | Enable async logging | true | true/false |
| `EnablePerformanceMonitoring` | Enable metrics collection | false | true/false |

### Environment-Specific Defaults

#### Development
- `MinLogLevel`: Trace
- `BufferSize`: 100
- `FlushInterval`: 1 second
- `MaxFileSizeMB`: 5
- `EnablePerformanceMonitoring`: true

#### Production
- `MinLogLevel`: Warning
- `BufferSize`: 5000
- `FlushInterval`: 5 seconds
- `MaxFileSizeMB`: 50
- `EnablePerformanceMonitoring`: true

#### Staging
- `MinLogLevel`: Information
- `BufferSize`: 1000
- `FlushInterval`: 5 seconds
- `MaxFileSizeMB`: 10
- `EnablePerformanceMonitoring`: true

## Performance Characteristics

### Throughput
- **Development**: 1,000-5,000 entries/second
- **Production**: 10,000-50,000 entries/second
- **High-Performance**: 50,000+ entries/second

### Memory Usage
- **Base overhead**: ~1 MB
- **Per buffer entry**: ~150 bytes
- **Maximum recommended**: 500 MB

### File Operations
- **Rotation time**: < 100ms (typical)
- **Flush time**: < 50ms (typical)
- **Cleanup time**: Varies by file count

## SOLID Principles Implementation

### Single Responsibility Principle (SRP)
- Každá trieda má jedinú zodpovednosť
- Use cases sú špecializované na konkrétne operácie
- Value objects sú immutable a focused

### Open/Closed Principle (OCP)
- Interfaces umožňujú extension bez modification
- Strategy pattern pre rôzne types of operations
- Configuration-driven behavior

### Liskov Substitution Principle (LSP)
- Interface implementations sú fully substitutable
- Consistent contracts across implementations

### Interface Segregation Principle (ISP)
- Malé, špecializované interfaces
- Clients depend only na methods they use

### Dependency Inversion Principle (DIP)
- High-level modules nezávisia na low-level modules
- Both depend na abstractions
- Dependency injection ready

## Enterprise Features

### 1. **Session Management**
- Multiple concurrent sessions
- Session lifecycle tracking
- Resource management
- Automatic cleanup

### 2. **File Lifecycle Management**
- Automatic rotation based na size/time
- Intelligent cleanup policies
- Archive support
- Compression options

### 3. **Monitoring Integration**
- Health check endpoints
- Performance metrics export
- Status reporting
- Alerting support

### 4. **Security Considerations**
- Directory permission validation
- Safe file operations
- Input sanitization
- Error message sanitization

## Testing Strategy

### Unit Tests
- Core business logic testing
- Value object behavior
- Error condition handling
- Edge case coverage

### Integration Tests
- File system operations
- Configuration validation
- End-to-end scenarios
- Performance testing

### Test Categories
- **Core Entity Tests**: LoggerSession, LogEntry behavior
- **Value Object Tests**: Immutability, validation
- **Use Case Tests**: Business workflow testing
- **Infrastructure Tests**: File operations, persistence

## Migration Guide

### From Legacy Logging
1. Identify current logging patterns
2. Create appropriate configuration
3. Replace logger instances
4. Update log statements if needed
5. Test thoroughly

### Configuration Migration
```csharp
// Legacy setup
var oldLogger = new FileLogger("app.log");

// New setup
var newLogger = AdvancedWinUiLogger.CreateSimpleLogger(@"C:\Logs", "app");
```

## Best Practices

### 1. **Configuration**
- Use environment-specific configurations
- Validate configurations at startup
- Monitor configuration changes

### 2. **Performance**
- Use appropriate buffer sizes
- Enable background logging v production
- Monitor memory usage

### 3. **Error Handling**
- Always check Result.IsSuccess
- Log configuration errors
- Implement fallback logging

### 4. **Maintenance**
- Regular log cleanup
- Monitor disk space
- Set up health checks

## Troubleshooting

### Common Issues

#### "Directory not accessible"
- Check directory permissions
- Verify directory exists
- Check disk space

#### "File in use"
- Multiple logger instances
- Antivirus interference
- Check file locks

#### "Poor performance"
- Increase buffer size
- Enable background logging
- Check disk I/O

### Diagnostics

```csharp
// Check health
var health = await AdvancedWinUiLogger.CheckHealthAsync();

// Get metrics
var metrics = AdvancedWinUiLogger.GetPerformanceMetrics();

// Validate configuration
var validation = AdvancedWinUiLogger.ValidateProductionConfiguration(config);
```

## Version Information

- **Component Version**: 1.0.0
- **Configuration Schema**: 1.0
- **Supported Frameworks**: .NET 8.0+
- **Platform**: Windows (WinUI 3)

## Support a Maintenance

Pre support a reporting issues:
1. Check documentation a troubleshooting guide
2. Review configuration setup
3. Collect diagnostic information
4. Report issues s detailed reproduction steps

---

*Táto dokumentácia pokrýva kompletnú architektúru a použitie AdvancedWinUiLogger komponenta. Pre špecifické implementačné detaily, pozrite si source code a inline dokumentáciu.*