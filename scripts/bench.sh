#!/usr/bin/env bash
# bench.sh – Laufzeit von GET /api/events über N Aufrufe
set -euo pipefail

URL="${1:-http://localhost:5000/api/events}"
RUNS="${2:-100}"

# Erster Aufruf separat: füllt den Cache und zeigt den Kaltstart
first=$(curl -s -o /dev/null -w '%{time_total}' "$URL")
echo "Erster Aufruf: ${first}s"

start=$(date +%s.%N)
times=()
for _ in $(seq "$RUNS"); do
    times+=("$(curl -s -o /dev/null -w '%{time_total}' "$URL")")
done
end=$(date +%s.%N)

printf '%s\n' "${times[@]}" | sort -n | awk -v total="$(echo "$end - $start" | bc)" -v runs="$RUNS" '
    { v[NR] = $1; sum += $1 }
    END {
        printf "Aufrufe:  %d in %.2fs\n", NR, total
        printf "Schnitt:  %.2f ms\n", sum / NR * 1000
        printf "Median:   %.2f ms\n", v[int(NR/2)] * 1000
        printf "p95:      %.2f ms\n", v[int(NR*0.95)] * 1000
        printf "Max:      %.2f ms\n", v[NR] * 1000
    }'