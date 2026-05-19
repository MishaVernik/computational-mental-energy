using MatrixMult.Algorithms;
using MatrixMult.Bench;
using MatrixMult.Core;

namespace MatrixMult.App;

class Program
{
    static int Main(string[] args)
    {
        // Check if running in table benchmark mode
        if (args.Length > 0 && args[0] == "--table")
        {
            return BenchmarkTableRunner.Run(args.Skip(1).ToArray());
        }
        
        return RunStandardBenchmark(args);
    }
    
    static int RunStandardBenchmark(string[] args)
    {
        var config = ParseArguments(args);
        
        if (config == null)
        {
            PrintUsage();
            return 1;
        }

        Console.WriteLine("=== Matrix Multiplication Benchmark ===");
        Console.WriteLine($"Matrix Size: {config.N} x {config.N}");
        Console.WriteLine($"Block Size: {config.BlockSize}");
        Console.WriteLine($"Max Threads: {config.Threads}");
        Console.WriteLine($"Algorithm: {config.Algorithm}");
        Console.WriteLine($"Iterations: {config.Iterations}");
        Console.WriteLine($"Warmup Runs: {config.Warmup}");
        Console.WriteLine();

        // Generate matrices
        Console.WriteLine("Generating matrices...");
        var a = new Matrix(config.N);
        var b = new Matrix(config.N);
        a.FillRandom();
        b.FillRandom();

        // Compute reference result using sequential algorithm
        Console.WriteLine("Computing reference result (sequential)...");
        var sequentialMultiplier = new SequentialMultiplier();
        var reference = new Matrix(config.N);
        reference.FillZero();
        sequentialMultiplier.Multiply(a, b, reference, config.BlockSize, 1);

        var runner = new BenchmarkRunner(
            a, b, reference,
            config.BlockSize, config.Threads);

        // Run sequential baseline
        BenchmarkStats? sequentialStats = null;
        if (config.Algorithm == "all" || config.Algorithm == "sequential")
        {
            Console.WriteLine("\n[Sequential Baseline]");
            sequentialStats = runner.RunBenchmark(
                new SequentialMultiplier(), config.Iterations, config.Warmup);
            // Sequential has no speedup/efficiency (it's the baseline)
            sequentialStats.Speedup = 1.0;
            sequentialStats.Efficiency = 1.0 / config.Threads;
            sequentialStats.Print();
        }

        // Run requested algorithm(s)
        if (config.Algorithm == "all" || config.Algorithm == "striped")
        {
            Console.WriteLine("\n[Striped Algorithm]");
            var stats = runner.RunBenchmark(
                new StripedMultiplier(), config.Iterations, config.Warmup);
            if (sequentialStats != null)
                runner.CalculateRelativeStats(sequentialStats, stats);
            stats.Print();
        }

        if (config.Algorithm == "all" || config.Algorithm == "fox")
        {
            Console.WriteLine("\n[Fox Algorithm]");
            var stats = runner.RunBenchmark(
                new FoxMultiplier(), config.Iterations, config.Warmup);
            if (sequentialStats != null)
                runner.CalculateRelativeStats(sequentialStats, stats);
            stats.Print();
        }

        if (config.Algorithm == "all" || config.Algorithm == "cannon")
        {
            Console.WriteLine("\n[Cannon Algorithm]");
            var stats = runner.RunBenchmark(
                new CannonMultiplier(), config.Iterations, config.Warmup);
            if (sequentialStats != null)
                runner.CalculateRelativeStats(sequentialStats, stats);
            stats.Print();
        }

        return 0;
    }
    
    static Config? ParseArguments(string[] args)
    {
        var config = new Config();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--n" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], out int n) || n <= 0)
                    {
                        Console.WriteLine("Error: --n must be a positive integer");
                        return null;
                    }
                    if (n * n < 250000)
                    {
                        Console.WriteLine($"Warning: N^2 = {n * n} < 250000. Using N=500 instead.");
                        config.N = 500;
                    }
                    else
                    {
                        config.N = n;
                    }
                    break;

                case "--block" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], out int block) || block <= 0)
                    {
                        Console.WriteLine("Error: --block must be a positive integer");
                        return null;
                    }
                    config.BlockSize = block;
                    break;

                case "--threads" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], out int threads) || threads <= 0)
                    {
                        Console.WriteLine("Error: --threads must be a positive integer");
                        return null;
                    }
                    config.Threads = threads;
                    break;

                case "--algo" when i + 1 < args.Length:
                    var algo = args[++i].ToLowerInvariant();
                    if (algo != "sequential" && algo != "striped" && algo != "fox" && algo != "cannon" && algo != "all")
                    {
                        Console.WriteLine("Error: --algo must be one of: sequential, striped, fox, cannon, all");
                        return null;
                    }
                    config.Algorithm = algo;
                    break;

                case "--iterations" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], out int iterations) || iterations <= 0)
                    {
                        Console.WriteLine("Error: --iterations must be a positive integer");
                        return null;
                    }
                    config.Iterations = iterations;
                    break;

                case "--warmup" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], out int warmup) || warmup < 0)
                    {
                        Console.WriteLine("Error: --warmup must be a non-negative integer");
                        return null;
                    }
                    config.Warmup = warmup;
                    break;

                default:
                    Console.WriteLine($"Unknown argument: {args[i]}");
                    return null;
            }
        }

        return config;
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage: MatrixMult.App [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --n <size>           Matrix dimension (default: 500, must satisfy N^2 >= 250000)");
        Console.WriteLine("  --block <size>      Block size (default: 50)");
        Console.WriteLine("  --threads <count>   Max degree of parallelism (default: Environment.ProcessorCount)");
        Console.WriteLine("  --algo <name>       Algorithm: sequential|striped|fox|cannon|all (default: all)");
        Console.WriteLine("  --iterations <n>    Number of benchmark runs (default: 5)");
        Console.WriteLine("  --warmup <n>        Warmup runs (default: 1)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run -- --n 500 --algo striped --threads 4");
        Console.WriteLine("  dotnet run -- --n 1000 --block 100 --algo all --iterations 10");
    }

    class Config
    {
        public int N { get; set; } = 500;
        public int BlockSize { get; set; } = 50;
        public int Threads { get; set; } = Environment.ProcessorCount;
        public string Algorithm { get; set; } = "all";
        public int Iterations { get; set; } = 5;
        public int Warmup { get; set; } = 1;
    }
}
