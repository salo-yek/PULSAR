#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="${1:-}"
OUTPUT_DIR="${2:-}"

if [[ -z "$PROJECT_DIR" ]]; then
    read -rp "Source directory: " PROJECT_DIR
fi
if [[ -z "$OUTPUT_DIR" ]]; then
    read -rp "Output directory: " OUTPUT_DIR
fi

PROJECT_DIR="$(realpath "$PROJECT_DIR")"
OUTPUT_DIR="$(realpath -m "$OUTPUT_DIR")"

[[ -d "$PROJECT_DIR" ]] || { echo "Error: '$PROJECT_DIR' is not a directory" >&2; exit 1; }
mkdir -p "$OUTPUT_DIR"

PROJECT_NAME="$(basename "$PROJECT_DIR")"
RUNTIMES=("win-x64" "linux-x64" "linux-arm64")

FAILED=()
for RUNTIME in "${RUNTIMES[@]}"; do
    printf 'Publishing %s ...\n' "$RUNTIME"
    if dotnet publish -c Release -r "$RUNTIME" --self-contained true \
        /p:PublishSingleFile=true /p:PublishTrimmed=true \
        -o "$OUTPUT_DIR/$RUNTIME" "$PROJECT_DIR"; then
        printf '  Done: %s/%s\n' "$OUTPUT_DIR/$RUNTIME" "$PROJECT_NAME"
    else
        FAILED+=("$RUNTIME")
    fi
done

if [[ ${#FAILED[@]} -gt 0 ]]; then
    printf 'Failed targets: %s\n' "${FAILED[*]}" >&2
    exit 1
fi

printf 'All %d targets published successfully.\n' "${#RUNTIMES[@]}"
