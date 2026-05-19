# Matrix Multiplication Benchmark Suite

A .NET 8 console application for benchmarking parallel matrix multiplication algorithms using the thread pool.

## Overview

This solution implements and benchmarks four matrix multiplication algorithms:

1. **Sequential** - Baseline blocked algorithm (single-threaded)
2. **Striped** - Row/column block-striped decomposition
3. **Fox** - Fox's algorithm with broadcast phases
4. **Cannon** - Cannon's algorithm with initial skew and cyclic shifts

## Project Structure

```
MatrixMult/
├── MatrixMult.Core/          # Core data structures and utilities
│   ├── Matrix.cs            # Matrix data structure
│   ├── BlockHelper.cs       # Blocked multiplication utilities
│   └── Verification.cs      # Correctness verification
├── MatrixMult.Algorithms/   # Algorithm implementations
│   ├── IMatrixMultiplier.cs # Algorithm interface
│   ├── SequentialMultiplier.cs
│   ├── StripedMultiplier.cs
│   ├── FoxMultiplier.cs
│   └── CannonMultiplier.cs
├── MatrixMult.Bench/        # Benchmarking infrastructure
│   ├── BenchmarkStats.cs   # Statistical metrics
│   └── BenchmarkRunner.cs  # Benchmark execution
└── MatrixMult.App/          # CLI entry point
    └── Program.cs           # Argument parsing and orchestration
```

## Building

```bash
dotnet build MatrixMult.sln
```

## Running

### Basic Usage

Run all algorithms with default settings:
```bash
cd MatrixMult.App
dotnet run
```

### Command-Line Arguments

```
--n <size>           Matrix dimension (default: 500, must satisfy N^2 >= 250000)
--block <size>       Block size (default: 50)
--threads <count>    Max degree of parallelism (default: Environment.ProcessorCount)
--algo <name>        Algorithm: sequential|striped|fox|cannon|all (default: all)
--iterations <n>     Number of benchmark runs (default: 5)
--warmup <n>         Warmup runs (default: 1)
```

### Examples

Run only the striped algorithm with 4 threads:
```bash
dotnet run -- --n 500 --algo striped --threads 4
```

Run all algorithms with a larger matrix:
```bash
dotnet run -- --n 1000 --block 100 --algo all --iterations 10
```

Compare Fox and Cannon algorithms:
```bash
dotnet run -- --n 500 --algo fox --threads 8 --iterations 5
dotnet run -- --n 500 --algo cannon --threads 8 --iterations 5
```

## Algorithm Details

### Sequential Baseline

Standard blocked matrix multiplication using three nested loops over blocks. This serves as the correctness reference and performance baseline.

**Time Complexity**: O(N³)  
**Space Complexity**: O(N²)

### Striped Algorithm

Row/column block-striped decomposition. Each thread processes a set of row blocks from matrix A and corresponding column blocks from matrix B.

**Parallelization**: Row blocks distributed across threads  
**Communication**: None (shared memory)  
**Load Balancing**: Good for uniform block sizes

### Fox's Algorithm

Uses a p × p process grid where p = floor(√threads). In each phase:
1. Broadcast A blocks along rows
2. Perform local block multiplication
3. Shift B blocks cyclically

**Process Grid**: p × p where p = floor(√threads)  
**Phases**: p phases  
**Communication**: Broadcast per phase (simulated as O(log p) overhead)

### Cannon's Algorithm

Uses a p × p process grid where p = floor(√threads). Algorithm phases:
1. **Initial Skew**: Shift A blocks left by row index, B blocks up by column index
2. **p Phases**: 
   - Local block multiplication
   - Cyclic shift: A left by 1, B up by 1

**Process Grid**: p × p where p = floor(√threads)  
**Phases**: p phases (including initial skew)  
**Communication**: Initial skew + cyclic shifts per phase

## Performance Metrics

The benchmark outputs the following metrics:

- **Avg Time (ms)**: Average execution time across iterations
- **Min/Max Time (ms)**: Minimum and maximum execution times
- **P95/P99 Time (ms)**: 95th and 99th percentile latencies
- **Throughput (GFlops)**: Floating-point operations per second (2×N³ operations / time)
- **Speedup**: Ratio of sequential time to parallel time
- **Efficiency**: Speedup divided by number of threads (ideal = 100%)
- **Correctness**: PASS/FAIL with maximum absolute error

## Block Size and Thread Grid

### Block Size

The block size determines the granularity of blocked multiplication. Larger blocks:
- Reduce overhead from loop control
- Improve cache locality
- May reduce parallelism opportunities

Smaller blocks:
- Increase parallelism opportunities
- May increase overhead
- Better load balancing for irregular workloads

**Recommendation**: Block size should be chosen such that blocks fit in L2/L3 cache. Typical values: 32-128 for matrices of size 500-2000.

### Thread Grid (Fox/Cannon)

For Fox and Cannon algorithms, the number of logical workers is p² where p = floor(√threads).

Examples:
- `--threads 4` → p = 2 → 4 workers (2×2 grid)
- `--threads 8` → p = 2 → 4 workers (2×2 grid)
- `--threads 9` → p = 3 → 9 workers (3×3 grid)
- `--threads 16` → p = 4 → 16 workers (4×4 grid)

**Note**: If threads is not a perfect square, the algorithm uses p² workers where p = floor(√threads). For example, with 8 threads, only 4 workers are used (2×2 grid).

## Mapping Results to Petri Net Parameters

The benchmark results can be mapped to Petri net model parameters for performance analysis:

### Transition Firing Times

- **Compute Time**: Average compute time per phase/block → transition firing time for compute transitions
- **Communication Overhead**: Average communication time → transition firing time for communication transitions

### Place Capacities

- **Thread Count**: Maximum degree of parallelism → token capacity of resource places
- **Block Count**: Number of blocks → initial marking of data places

### Throughput Analysis

- **GFlops**: System throughput → transition throughput in Petri net
- **Efficiency**: Resource utilization → token utilization in resource places

### Example Mapping

For a Fox algorithm run with N=500, block=50, threads=4:

```
Process Grid: 2×2 (p=2)
Phases: 2
Blocks per dimension: 10

Petri Net Parameters:
- Compute transition: ~X ms (from compute time / phases)
- Broadcast transition: ~Y ms (from comm overhead / phases)
- Resource place capacity: 4 (threads)
- Data place initial marking: 100 (total blocks)
- Throughput: Z GFlops
```

## Implementation Notes

### Communication Simulation

Fox and Cannon algorithms simulate communication overhead:
- **Fox**: Broadcast overhead modeled as O(log p) steps per phase
- **Cannon**: Skew and shift overhead modeled as O(p) steps

In real MPI implementations, these would be actual network operations. The simulation uses `Thread.SpinWait()` to model latency.

### Timing Breakdown

Each algorithm returns:
- **ComputeTime**: Pure computation time (excluding communication simulation)
- **CommunicationOverhead**: Simulated communication time
- **TotalTime**: End-to-end execution time

### Correctness Validation

Results are validated against the sequential baseline using maximum absolute error. Default threshold: 1e-10.

## Performance Tips

1. **Warmup Runs**: Use `--warmup 2` or more to ensure JIT compilation and cache warming
2. **Multiple Iterations**: Use `--iterations 10` or more for stable statistics
3. **Block Size Tuning**: Experiment with block sizes (32, 50, 64, 100, 128) to find optimal cache usage
4. **Thread Count**: Match thread count to CPU cores for best performance
5. **Matrix Size**: Larger matrices (N ≥ 1000) show better parallel speedup

## Limitations

- Communication overhead is simulated, not real network latency
- Algorithms assume square matrices
- Block size must divide matrix size evenly for optimal performance (handled with bounds checking)
- Process grid for Fox/Cannon is limited to p² workers where p = floor(√threads)

## Future Enhancements

- Support for rectangular matrices
- Real MPI implementation for distributed memory
- Additional algorithms (e.g., SUMMA)
- GPU acceleration support
- Detailed profiling with ETW/PerfView integration


