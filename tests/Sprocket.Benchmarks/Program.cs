using BenchmarkDotNet.Running;

namespace Sprocket.Benchmarks;

/// <summary>Runs the benchmarks the arguments pick, as BenchmarkDotNet's switcher reads them: <c>--filter *Parse*</c>, for one.</summary>
static class Program
{
    static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
