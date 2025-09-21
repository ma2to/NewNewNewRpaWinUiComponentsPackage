# AdvancedWinUiLogger Enterprise Architecture

## Overview

The AdvancedWinUiLogger component provides enterprise-grade logging configuration capabilities through a single entry point facade that accepts external logging systems and returns configured loggers. This implementation follows Clean Architecture principles with proper separation of concerns and dependency injection.

## Architecture Pattern: External Logger Configuration Facade

```
External Logger System (Serilog/NLog/etc.)
    ↓
AdvancedLoggerFacade (Single Entry Point)
    ↓ (Dependency Injection)
Application Services (LoggingService + FileManagementService)
    ↓ (Uses)
Infrastructure Layer (FileLoggerRepository + FileRotationService)
    ↓ (File Operations)
File System (Log Files, Rotation, Cleanup)
    ↓ (Returns Configured)
ILogger<T> / ILoggerFactory
    ↓ (Use in)
RpaWinUiComponentsPackage Components (DataGrid, etc.)
```

## Clean Architecture Implementation

### Facade Layer (Public API)
- **AdvancedLoggerFacade** - Single entry point with external logger integration
- **Types.cs** - Public DTOs and enums (LoggingMode, ConfigurationBuilder)
- **TypeExtensions.cs** - Mapping between public and internal types

### Application Layer (Services)
- **ILoggingService** - Core logging operations with session management and analytics
- **IFileManagementService** - File rotation, cleanup, and directory analysis
- **ILoggerPerformanceService** - Performance monitoring and metrics collection
- **ILoggerConfigurationService** - Configuration validation and management
- **ILoggerCreationService** - Logger factory creation strategies

### Infrastructure Layer (Persistence)
- **FileLoggerRepository** - File I/O operations, rotation, cleanup implementation
- **FileRotationService** - Advanced file rotation with compression and validation

### Core Layer (Domain)
- **Core.ValueObjects** - Immutable domain objects (LoggerConfiguration, RotationResult, etc.)
- **Core.Entities** - Domain entities (LoggerSession with lifecycle management)
- **Core.Interfaces** - Domain contracts (ILoggerRepository)

## Core Design Principles

### 1. Single Entry Point
- **Pattern**: Configuration Facade
- **Input**: External ILoggerFactory + Configuration
- **Output**: Configured ILogger instances
- **Benefits**: Centralized logging configuration, consistent behavior across components

### 2. External Logger Integration
- **Compatibility**: Serilog, NLog, Microsoft.Extensions.Logging
- **Null Safety**: Works without external logger (fallback to Console/Debug)
- **Strategy**: Configuration wrapper around external systems

### 3. Enterprise Configuration
- **Builder Pattern**: Fluent configuration interface
- **Presets**: Minimal, HighPerformance, Development configurations
- **Strategies**: Single, Bulk, AsyncBatch logging modes

## Usage Examples

### Basic Usage (Standalone)
```csharp
// Create facade without external logger (uses Console/Debug fallback)
var loggerFacade = new AdvancedLoggerFacade();

// Get configured logger for DataGrid component
var dataGridLogger = loggerFacade.ConfigureLogger<AdvancedDataGridFacade>();

// Use in DataGrid component with external logger
var dataGrid = new AdvancedDataGridFacade(dataGridLogger);
```

### External Logger Integration (Serilog)
```csharp
// Setup Serilog
var serilogLogger = new LoggerConfiguration()
    .WriteTo.File("logs/app.log")
    .WriteTo.Console()
    .CreateLogger();

var serilogFactory = new SerilogLoggerFactory(serilogLogger);

// Create facade with external logger factory
var loggerFacade = new AdvancedLoggerFacade(serilogFactory);

// Configure with custom settings using internal configuration
var config = AdvancedLoggerFacade.CreateHighPerformanceConfiguration("./logs", "enterprise");
var enterpriseLogger = loggerFacade.ConfigureLogger<MyEnterpriseComponent>(config);

// Use configured logger in component
var component = new MyEnterpriseComponent(enterpriseLogger);
```

### Integration with Existing Infrastructure
```csharp
// Use existing ILoggerFactory from DI container
ILoggerFactory existingFactory = serviceProvider.GetService<ILoggerFactory>();

// Create facade with existing factory
var loggerFacade = new AdvancedLoggerFacade(existingFactory);

// Configure for enterprise use with file management
var config = loggerFacade.CreateConfigurationBuilder()
    .SetLogDirectory("./enterprise-logs")
    .SetLoggingMode(LoggingMode.AsyncBatch)
    .EnableCompression(true)
    .Build();

// Get configured factory for use across components
var configuredFactory = loggerFacade.ConfigureLoggerFactory(config);
```

### Advanced Configuration with Builder Pattern
```csharp
var loggerFacade = new AdvancedLoggerFacade(externalLoggerFactory);

// Build custom configuration
var config = loggerFacade.CreateConfigurationBuilder()
    .SetLogDirectory("./enterprise-logs")
    .SetBaseFileName("advanced-app")
    .SetLoggingMode(LoggingMode.AsyncBatch)
    .SetMinimumLevel(LogLevel.Information)
    .EnableCompression(true)
    .SetMaxFileSize(100 * 1024 * 1024) // 100MB
    .EnablePerformanceCounters(true)
    .Build();

// Get configured logger factory
var configuredFactory = loggerFacade.ConfigureLoggerFactory(config);

// Create multiple component loggers
var dataGridLogger = configuredFactory.CreateLogger<AdvancedDataGridFacade>();
var reportLogger = configuredFactory.CreateLogger<ReportGenerator>();
```

## Configuration Options

### LoggingMode Strategies

#### Single Mode
- **Use Case**: Simple applications, debugging
- **Behavior**: Each log entry written immediately
- **Performance**: Lower throughput, immediate persistence

#### Bulk Mode
- **Use Case**: Moderate throughput applications
- **Behavior**: Batches log entries for efficient writing
- **Performance**: Balanced throughput and latency

#### AsyncBatch Mode
- **Use Case**: High-performance enterprise applications
- **Behavior**: Asynchronous batching with background processing
- **Performance**: Maximum throughput, eventual consistency

### Preset Configurations

#### Minimal Configuration
```csharp
var config = AdvancedLoggerFacade.CreateMinimalConfiguration("./logs", "app");
// - LoggingMode: Single
// - MinimumLevel: Information
// - Compression: Disabled
// - PerformanceCounters: Disabled
```

#### High Performance Configuration
```csharp
var config = AdvancedLoggerFacade.CreateHighPerformanceConfiguration("./logs", "enterprise");
// - LoggingMode: AsyncBatch
// - MaxFileSize: 50MB
// - Compression: Enabled
// - PerformanceCounters: Enabled
```

#### Development Configuration
```csharp
var config = AdvancedLoggerFacade.CreateDevelopmentConfiguration("./logs", "dev");
// - LoggingMode: Single
// - MinimumLevel: Debug
// - StructuredLogging: Enabled
```

## Integration with Components

### DataGrid Component Integration
```csharp
// Configure logger for DataGrid
var loggerFacade = new AdvancedLoggerFacade(serilogFactory);
var dataGridLogger = loggerFacade.ConfigureLogger<AdvancedDataGridFacade>();

// Pass to DataGrid component
var dataGrid = new AdvancedDataGridFacade(dataGridLogger);

// DataGrid will use configured logger for:
// - Import/Export operations (Information level)
// - Performance monitoring (Warning level)
// - Error conditions (Error level)
```

### Cross-Component Consistency
```csharp
// Single configuration for all components
var enterpriseConfig = AdvancedLoggerFacade.CreateHighPerformanceConfiguration("./logs", "enterprise");
var loggerFactory = loggerFacade.ConfigureLoggerFactory(enterpriseConfig);

// All components use same configuration
var dataGridLogger = loggerFactory.CreateLogger<AdvancedDataGridFacade>();
var reportLogger = loggerFactory.CreateLogger<ReportGenerator>();
var importLogger = loggerFactory.CreateLogger<DataImporter>();
```

## Enterprise Benefits

### 1. File Management System
- **Automatic Rotation**: Log files rotated when size limits exceeded
- **Intelligent Cleanup**: Age-based and count-based file cleanup policies
- **Compression Support**: Optional compression for archived log files
- **Directory Analytics**: Comprehensive directory statistics and analysis

### 2. Logging Capabilities
- **Dual Persistence**: Writes to external logger AND internal file repository
- **Session Management**: Organized logging with session boundaries and lifecycle
- **Batch Processing**: High-performance batch logging for enterprise scenarios
- **Search & Analytics**: Advanced log searching and statistical analysis

### 3. Architecture Benefits
- **Clean Architecture**: Proper separation with Facade → Application → Infrastructure layers
- **Dependency Injection**: Full DI support with service registration and lifecycle management
- **Type Safety**: Comprehensive mapping between public facade and internal types
- **Error Resilience**: Graceful degradation with fallback mechanisms

### 4. External Integration
- **Provider Agnostic**: Works with Serilog, NLog, Microsoft.Extensions.Logging
- **Null Safety**: Operates standalone with Console/Debug fallback
- **Configuration Bridge**: Maps internal configurations to external logger systems
- **Factory Pattern**: Returns properly configured ILoggerFactory instances

## Best Practices

### 1. Service Architecture & Dependencies
- **Dependency Injection**: FileLoggerRepository and Application Services properly registered
- **Service Registration Order**: Infrastructure → Application Services → Facade
- **Repository Pattern**: FileLoggerRepository handles all file I/O operations
- **Service Lifecycle**: Singleton services with proper resource management

### 2. File Management Operations
- **Size-Based Rotation**: Automatic rotation when MaxFileSizeBytes exceeded
- **Cleanup Policies**: Age-based (MaxFileAgeDays) and count-based (MaxLogFiles) cleanup
- **Directory Operations**: Comprehensive file discovery and directory analysis
- **Error Handling**: Graceful handling of file access and permission issues

### 3. Application Services Implementation
- **LoggingService**: Dual writing (external logger + internal repository)
- **FileManagementService**: File rotation, cleanup, directory analysis
- **Session Management**: Proper session lifecycle with resource cleanup
- **Performance Optimization**: Efficient batch processing and async operations

### 3. Performance Optimization
- **LoggingMode Selection**:
  - Single: Development and debugging
  - Bulk: Moderate throughput applications
  - AsyncBatch: High-performance enterprise scenarios
- **Service Provider Caching**: Build service provider once and cache when possible
- **Lazy Initialization**: Services are created only when needed

### 4. Integration Patterns
- **Component Integration**: Pass configured ILogger<T> to component constructors
- **Cross-Component Consistency**: Use single LoggerFactory for all components
- **Error Handling**: Leverage Result<T> pattern for error propagation
- **Monitoring**: Use ILoggerPerformanceService for health checks and metrics

### 5. Testing Strategy
- **Service Mocking**: Mock ILoggingService and IFileManagementService interfaces
- **Configuration Testing**: Test configuration validation and mapping
- **Integration Testing**: Test with real external logger factories
- **Performance Testing**: Validate LoggingMode strategies under load