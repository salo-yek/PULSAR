#!/usr/bin/env bash
# Build script for PULSAR on Linux
set -e

echo "=== PULSAR Build Script ==="
echo ""

# Restore NuGet packages
echo "[1/3] Restoring packages..."
dotnet restore src/PULSAR/PULSAR.csproj

# Build
echo "[2/3] Building (Release)..."
dotnet build src/PULSAR/PULSAR.csproj -c Release --no-restore

# Optional publish
if [ "$1" = "--publish" ]; then
    echo "[3/3] Publishing..."
    dotnet publish src/PULSAR/PULSAR.csproj -c Release --no-build -o ./publish
    echo "Published to ./publish"
fi

echo ""
echo "=== Build Complete ==="