# Benchmark script to generate performance table
# Tests different matrix sizes and thread counts

param(
    [int[]]$MatrixSizes = @(500, 1000, 2000, 3000, 5000, 10000),
    [int[]]$ThreadCounts = @(),
    [int]$Iterations = 3,
    [int]$Warmup = 1,
    [string]$Algorithm = "all",
    [string]$OutputFile = "benchmark-results.csv"
)

# Get available threads if not specified
if ($ThreadCounts.Count -eq 0) {
    $maxThreads = [System.Environment]::ProcessorCount
    $ThreadCounts = @(1, 2, 4, 8, 16)
    # Add powers of 2 up to maxThreads
    $power = 1
    while ($power * 2 -le $maxThreads) {
        $power = $power * 2
        if ($power -gt 16 -and $ThreadCounts -notcontains $power) {
            $ThreadCounts += $power
        }
    }
    # Add maxThreads if not already included
    if ($ThreadCounts -notcontains $maxThreads) {
        $ThreadCounts += $maxThreads
    }
    $ThreadCounts = $ThreadCounts | Sort-Object -Unique
}

Write-Host "=== Matrix Multiplication Benchmark Table ===" -ForegroundColor Green
Write-Host "Matrix Sizes: $($MatrixSizes -join ', ')" -ForegroundColor Cyan
Write-Host "Thread Counts: $($ThreadCounts -join ', ')" -ForegroundColor Cyan
Write-Host "Iterations: $Iterations" -ForegroundColor Cyan
Write-Host "Algorithm: $Algorithm" -ForegroundColor Cyan
Write-Host ""

# Create CSV header
$csvHeader = "MatrixSize,Threads,Algorithm,AvgTimeMs,MinTimeMs,MaxTimeMs,P95TimeMs,P99TimeMs,ThroughputGFlops,Speedup,Efficiency,Correctness,MaxError"
$csvHeader | Out-File -FilePath $OutputFile -Encoding UTF8

$totalRuns = $MatrixSizes.Count * $ThreadCounts.Count
$currentRun = 0

foreach ($size in $MatrixSizes) {
    Write-Host "`n--- Testing Matrix Size: ${size}x${size} ---" -ForegroundColor Yellow
    
    foreach ($threads in $ThreadCounts) {
        $currentRun++
        Write-Host "[$currentRun/$totalRuns] Size: ${size}x${size}, Threads: $threads" -ForegroundColor Cyan
        
        try {
            # Run benchmark
            $output = dotnet run --project MatrixMult.App -- --n $size --threads $threads --algo $Algorithm --iterations $Iterations --warmup $Warmup 2>&1
            
            # Parse output for each algorithm
            $algorithms = @("Sequential", "Striped", "Fox", "Cannon")
            if ($Algorithm -ne "all") {
                $algorithms = @($Algorithm)
            }
            
            foreach ($algo in $algorithms) {
                $algoSection = $output | Select-String -Pattern "\[$algo" -Context 0, 15
                if ($algoSection) {
                    $lines = $algoSection.Line + ($algoSection.Context.PostContext -join "`n")
                    
                    # Extract metrics using regex
                    $avgTime = if ($lines -match "Avg Time:\s+([\d.]+)") { $matches[1] } else { "" }
                    $minTime = if ($lines -match "Min Time:\s+([\d.]+)") { $matches[1] } else { "" }
                    $maxTime = if ($lines -match "Max Time:\s+([\d.]+)") { $matches[1] } else { "" }
                    $p95Time = if ($lines -match "P95 Time:\s+([\d.]+)") { $matches[1] } else { "" }
                    $p99Time = if ($lines -match "P99 Time:\s+([\d.]+)") { $matches[1] } else { "" }
                    $throughput = if ($lines -match "Throughput:\s+([\d.]+)") { $matches[1] } else { "" }
                    $speedup = if ($lines -match "Speedup:\s+([\d.]+)") { $matches[1] } else { "" }
                    $efficiency = if ($lines -match "Efficiency:\s+([\d.%]+)") { $matches[1] } else { "" }
                    $correctness = if ($lines -match "Correctness:\s+(\w+)") { $matches[1] } else { "" }
                    $maxError = if ($lines -match "max error:\s+([\d.E+-]+)") { $matches[1] } else { "" }
                    
                    # Write to CSV
                    $csvLine = "$size,$threads,$algo,$avgTime,$minTime,$maxTime,$p95Time,$p99Time,$throughput,$speedup,$efficiency,$correctness,$maxError"
                    $csvLine | Out-File -FilePath $OutputFile -Append -Encoding UTF8
                }
            }
        }
        catch {
            Write-Host "Error running benchmark: $_" -ForegroundColor Red
        }
    }
}

Write-Host "`n✅ Benchmark complete! Results saved to: $OutputFile" -ForegroundColor Green
Write-Host "`nTo view results:" -ForegroundColor Cyan
Write-Host "  Import-Csv $OutputFile | Format-Table -AutoSize" -ForegroundColor Yellow


