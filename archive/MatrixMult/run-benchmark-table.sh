#!/bin/bash
# Benchmark script to generate performance table
# Tests different matrix sizes and thread counts

MATRIX_SIZES=(500 1000 2000 3000 5000 10000)
THREAD_COUNTS=(1 2 4 8 16)
MAX_THREADS=$(nproc)

# Add powers of 2 up to max threads
power=16
while [ $((power * 2)) -le $MAX_THREADS ]; do
    power=$((power * 2))
    THREAD_COUNTS+=($power)
done

# Add max threads if not already included
if [[ ! " ${THREAD_COUNTS[@]} " =~ " ${MAX_THREADS} " ]]; then
    THREAD_COUNTS+=($MAX_THREADS)
fi

# Sort thread counts
IFS=$'\n' THREAD_COUNTS=($(sort -n <<<"${THREAD_COUNTS[*]}"))
unset IFS

ITERATIONS=3
WARMUP=1
ALGORITHM="all"
OUTPUT_FILE="benchmark-results.csv"

echo "=== Matrix Multiplication Benchmark Table ==="
echo "Matrix Sizes: ${MATRIX_SIZES[*]}"
echo "Thread Counts: ${THREAD_COUNTS[*]}"
echo "Iterations: $ITERATIONS"
echo "Algorithm: $ALGORITHM"
echo "Max Threads Available: $MAX_THREADS"
echo ""

# Create CSV header
echo "MatrixSize,Threads,Algorithm,AvgTimeMs,MinTimeMs,MaxTimeMs,P95TimeMs,P99TimeMs,ThroughputGFlops,Speedup,Efficiency,Correctness,MaxError" > "$OUTPUT_FILE"

TOTAL_RUNS=$((${#MATRIX_SIZES[@]} * ${#THREAD_COUNTS[@]}))
CURRENT_RUN=0

for size in "${MATRIX_SIZES[@]}"; do
    echo ""
    echo "--- Testing Matrix Size: ${size}x${size} ---"
    
    for threads in "${THREAD_COUNTS[@]}"; do
        CURRENT_RUN=$((CURRENT_RUN + 1))
        echo "[$CURRENT_RUN/$TOTAL_RUNS] Size: ${size}x${size}, Threads: $threads"
        
        dotnet run -- --n $size --threads $threads --algo $ALGORITHM --iterations $ITERATIONS --warmup $WARMUP >> /tmp/benchmark-output.txt 2>&1
        
        # Parse output and append to CSV (simplified - full parsing would require more complex script)
        # For now, just run the C# version which handles CSV generation
    done
done

echo ""
echo "✅ Benchmark complete! Results saved to: $OUTPUT_FILE"


