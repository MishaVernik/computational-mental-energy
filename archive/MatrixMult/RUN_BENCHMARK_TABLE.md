# Benchmark Table Runner

This tool generates a comprehensive performance table by testing different matrix sizes and thread counts.

## Usage

```bash
cd MatrixMult.App
dotnet run -- --table [options]
```

## Options

- `--sizes <list>` - Comma-separated matrix sizes (default: 500,1000,2000,3000,5000,10000)
- `--threads <list>` - Comma-separated thread counts (default: auto-detected powers of 2 up to max CPU cores)
- `--iterations <n>` - Number of benchmark runs per configuration (default: 3)
- `--warmup <n>` - Warmup runs before benchmarking (default: 1)
- `--algo <name>` - Algorithm to test: sequential|striped|fox|cannon|all (default: all)
- `--output <file>` - Output CSV file path (default: benchmark-results.csv)

## Examples

### Quick test with small matrices
```bash
dotnet run -- --table --sizes "500,1000" --threads "1,2,4" --iterations 1 --warmup 0
```

### Full benchmark suite
```bash
dotnet run -- --table --sizes "500,1000,2000,3000,5000,10000" --iterations 3
```

### Test specific algorithm
```bash
dotnet run -- --table --sizes "500,1000,2000" --algo striped --iterations 5
```

### Custom thread counts
```bash
dotnet run -- --table --sizes "1000,2000" --threads "1,4,8,16,22" --iterations 3
```

## Output

The benchmark generates a CSV file (`benchmark-results.csv`) with the following columns:

- **MatrixSize** - Matrix dimension (N x N)
- **Threads** - Number of threads used
- **Algorithm** - Algorithm name (Sequential, Striped, Fox, Cannon)
- **AvgTimeMs** - Average execution time in milliseconds
- **MinTimeMs** - Minimum execution time
- **MaxTimeMs** - Maximum execution time
- **P95TimeMs** - 95th percentile latency
- **P99TimeMs** - 99th percentile latency
- **ThroughputGFlops** - Throughput in GigaFlops
- **Speedup** - Speedup relative to sequential baseline
- **Efficiency** - Efficiency (speedup / threads)
- **Correctness** - PASS/FAIL
- **MaxError** - Maximum absolute error vs reference

## Viewing Results

### PowerShell
```powershell
Import-Csv benchmark-results.csv | Format-Table -AutoSize
```

### Filter by algorithm
```powershell
Import-Csv benchmark-results.csv | Where-Object { $_.Algorithm -eq "Striped" } | Format-Table MatrixSize,Threads,AvgTimeMs,Speedup -AutoSize
```

### Export to Excel
```powershell
Import-Csv benchmark-results.csv | Export-Excel -Path "results.xlsx"
```

## Default Configuration

- **Matrix Sizes**: 500, 1000, 2000, 3000, 5000, 10000
- **Thread Counts**: Auto-detected based on CPU cores (typically: 1, 2, 4, 8, 16, 22)
- **Iterations**: 3 runs per configuration
- **Warmup**: 1 warmup run per configuration
- **Algorithm**: All algorithms (Sequential, Striped, Fox, Cannon)

## Performance Notes

- Larger matrices show better parallel speedup
- Optimal thread count depends on matrix size and algorithm
- Fox and Cannon algorithms have communication overhead simulation
- Results may vary based on system load and CPU architecture


