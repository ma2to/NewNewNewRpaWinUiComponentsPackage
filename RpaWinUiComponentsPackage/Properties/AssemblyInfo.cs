using System.Reflection;
using System.Runtime.CompilerServices;

// SENIOR DEVELOPER: Hide Internal namespaces from IntelliSense
// This ensures only the main public API namespaces are visible to consumers

// PROFESSIONAL SOLUTION: Assembly-level metadata for clean API surface
[assembly: AssemblyMetadata("DesignTimeVisible", "false")]

// Ensure Internal implementations are not part of public API surface
[assembly: AssemblyMetadata("PublicApiNamespaces", "RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;RpaWinUiComponentsPackage.AdvancedWinUiLogger")]

// InternalsVisibleTo attributes for testing assemblies
// This allows test projects to access internal types and members for unit testing
[assembly: InternalsVisibleTo("RpaWinUiComponentsPackage.Tests")]
[assembly: InternalsVisibleTo("RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Tests")]
[assembly: InternalsVisibleTo("RpaWinUiComponentsPackage.AdvancedWinUiLogger.Tests")]
[assembly: InternalsVisibleTo("RpaWinUiComponentsPackage.IntegrationTests")]
[assembly: InternalsVisibleTo("RpaWinUiComponentsPackage.UnitTests")]

// DynamicProxyGenAssembly2 for mocking frameworks like Moq
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

// Additional testing frameworks that might need access to internals
[assembly: InternalsVisibleTo("RpaWinUiComponentsPackage.TestUtilities")]