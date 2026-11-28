#!/usr/bin/env bash
# Records the README visuals (Material/Wiki) from the Avalonia visualization running on a virtual screen.
# Requires: .NET 10 SDK, xvfb-run (Xvfb) and ffmpeg. Uses Mesa's software OpenGL if no GPU is available.
# Usage: Material/Scripts/ReadmeVisuals/record.sh [shot ...]   (shots: 2d 3d large multi paths; default: all)
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
repo="$(cd "$here/../../.." && pwd)"
wiki="$repo/Material/Wiki"
out="$(mktemp -d)"
trap 'rm -rf "$out"' EXIT
shots=("${@:-2d 3d large multi paths}")
shots=(${shots[@]})

dotnet build "$here/ReadmeVisuals.csproj" -c Release -o "$out/bin" > /dev/null

# The simulation looks for its resource files relative to the working directory
cd "$repo"
for shot in "${shots[@]}"; do
    echo "Recording $shot ..."
    xvfb-run -a -s "-screen 0 1600x900x24" dotnet "$out/bin/ReadmeVisuals.dll" "$out" "$shot" | grep -E "Recorded|Exception" || true
done

# Convert: videos to GIFs (no dithering, it flickers), stills to 256-color PNGs
gif() { ffmpeg -loglevel error -y -i "$1" -vf "fps=$3,scale=$4:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=$5:stats_mode=full[p];[b][p]paletteuse=dither=none:diff_mode=rectangle" "$2"; }
[[ -f "$out/rawsim-o-2d.mp4" ]] && gif "$out/rawsim-o-2d.mp4" "$wiki/rawsim-o-2d.gif" 12 1000 128
[[ -f "$out/rawsim-o-3d.mp4" ]] && gif "$out/rawsim-o-3d.mp4" "$wiki/rawsim-o-3d.gif" 10 860 96
for png in "$out"/*.png; do
    [[ -f "$png" ]] || continue
    ffmpeg -loglevel error -y -i "$png" -vf "split[a][b];[a]palettegen=max_colors=256[p];[b][p]paletteuse=dither=none" "$wiki/$(basename "$png")"
done
rmdir "$repo"/readme-default-default-* 2>/dev/null || true # empty simulation output folders
echo "Updated visuals in $wiki"
