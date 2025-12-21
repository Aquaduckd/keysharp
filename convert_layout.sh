#!/bin/bash
# Simple script to convert a layout using the LayoutConverter
# Usage: ./convert_layout.sh <source> <template> <output> [startKey]

dotnet run --project Keysharp.csproj -- convert "$@"

