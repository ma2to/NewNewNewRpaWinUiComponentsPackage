using BenchmarkDotNet.Running;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks;

class Program
{
    static void Main(string[] args)
    {
        // Run all benchmarks
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
