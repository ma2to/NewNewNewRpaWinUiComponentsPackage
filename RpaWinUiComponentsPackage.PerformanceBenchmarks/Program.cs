using BenchmarkDotNet.Running;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks;

class Program
{
    static void Main(string[] args)
    {
        // Run all benchmarks
        // Note: Only Headless and Readonly modes work without UI dispatcher
        // Interactive mode requires STA thread and UI message pump
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
