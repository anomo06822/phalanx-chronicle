#!/usr/bin/env python3

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from generate_character_art import convert_svg_to_png, darken, ensure_dir, lighten, mix


ROOT = Path(__file__).resolve().parents[1]
TERRAIN_SOURCE_ROOT = ROOT / "Assets" / "ArtSource" / "Terrain" / "Generated"
TERRAIN_OUTPUT_ROOT = ROOT / "Assets" / "Resources" / "Terrain"
TERRAIN_SIZE = 64
VARIANTS = ("a", "b", "c")
LAYERS = ("base", "overlay", "prop")
TERRAIN_KEYS = ("plain", "forest", "fort", "hazard", "blocked")


@dataclass(frozen=True)
class TerrainPaletteSpec:
    key: str
    style: str
    ground: str
    ground_dark: str
    ground_light: str
    foliage: str
    wood: str
    stone: str
    hazard: str
    accent: str


PALETTES = (
    TerrainPaletteSpec("frontier-plain", "frontier", "#8C724F", "#645139", "#D2BC8D", "#798661", "#9F7448", "#8F8677", "#D68E56", "#E1C17B"),
    TerrainPaletteSpec("guangzong-smoke", "smoke", "#786247", "#584634", "#D8C08D", "#72684B", "#915D39", "#7E776E", "#E69455", "#E1BE74"),
    TerrainPaletteSpec("jiangxia-bridges", "river", "#7C806F", "#5E6255", "#D0CBC0", "#7B968C", "#A27A52", "#868D87", "#7DA9C9", "#DDE7EE"),
    TerrainPaletteSpec("luocheng-gate", "gate", "#7A6550", "#5A4939", "#D1BA9D", "#8A967A", "#A86D42", "#8B7A68", "#D59057", "#DFBC86"),
    TerrainPaletteSpec("changban-river", "river", "#6F7A74", "#4E5955", "#CBD2CC", "#83A097", "#98724E", "#808681", "#87B0CA", "#DDE8EF"),
    TerrainPaletteSpec("dingjun-stone", "mountain", "#817A62", "#625C4B", "#D1C7AA", "#73806A", "#947653", "#919086", "#C9A36D", "#D8C79A"),
)


def rect(x: int, y: int, width: int, height: int, fill: str, opacity: float = 1.0) -> str:
    return f'<rect x="{x}" y="{y}" width="{width}" height="{height}" fill="{fill}" fill-opacity="{opacity:.3f}" />'


def poly(points: list[tuple[int, int]], fill: str, opacity: float = 1.0) -> str:
    point_string = " ".join(f"{x},{y}" for x, y in points)
    return f'<polygon points="{point_string}" fill="{fill}" fill-opacity="{opacity:.3f}" />'


def line(points: list[tuple[int, int]], stroke: str, width: int, opacity: float = 1.0) -> str:
    point_string = " ".join(f"{x},{y}" for x, y in points)
    return f'<polyline points="{point_string}" fill="none" stroke="{stroke}" stroke-width="{width}" stroke-linecap="square" stroke-linejoin="miter" stroke-opacity="{opacity:.3f}" />'


def terrain_paths_for(palette_key: str, terrain_key: str, variant: str, layer: str) -> tuple[Path, Path]:
    file_name = f"{terrain_key}_{variant}_{layer}"
    svg_path = TERRAIN_SOURCE_ROOT / palette_key / f"{file_name}.svg"
    png_path = TERRAIN_OUTPUT_ROOT / palette_key / f"{file_name}.png"
    return svg_path, png_path


def render_tile_svg(palette: TerrainPaletteSpec, terrain_key: str, variant: str, layer: str) -> str:
    if layer == "base":
        shapes = render_base_layer(palette, terrain_key, variant)
    elif layer == "overlay":
        shapes = render_overlay_layer(palette, terrain_key, variant)
    else:
        shapes = render_prop_layer(palette, terrain_key, variant)

    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{TERRAIN_SIZE}" height="{TERRAIN_SIZE}" viewBox="0 0 {TERRAIN_SIZE} {TERRAIN_SIZE}" shape-rendering="crispEdges">
  <rect width="64" height="64" fill="#000000" fill-opacity="0" />
  {''.join(shapes)}
</svg>
"""


def render_base_layer(palette: TerrainPaletteSpec, terrain_key: str, variant: str) -> list[str]:
    shapes = [rect(0, 0, 64, 64, palette.ground_dark), rect(2, 2, 60, 60, palette.ground)]
    accent = palette.accent
    offset_x, offset_y = variant_offsets(variant)

    if terrain_key == "plain":
        shapes.extend(
            [
                rect(4 + offset_x, 6, 20, 14, mix(palette.ground, palette.ground_light, 0.42), 0.92),
                rect(28, 10 + offset_y, 18, 12, darken(palette.ground, 0.08), 0.68),
                rect(42 + offset_x, 28, 14, 10, mix(palette.ground, palette.ground_light, 0.24), 0.78),
                rect(8, 34 + offset_y, 18, 14, darken(palette.ground, 0.04), 0.72),
                rect(28 + offset_x, 42, 22, 12, mix(palette.ground, palette.ground_light, 0.36), 0.82),
            ]
        )
    elif terrain_key == "forest":
        shapes.extend(
            [
                rect(4, 4, 24, 16, darken(palette.foliage, 0.28), 0.9),
                rect(24 + offset_x, 10, 20, 16, darken(palette.foliage, 0.18), 0.88),
                rect(40, 6 + offset_y, 18, 18, darken(palette.foliage, 0.12), 0.9),
                rect(8 + offset_x, 30, 22, 18, darken(palette.ground, 0.12), 0.72),
                rect(34, 36 + offset_y, 18, 16, darken(palette.foliage, 0.2), 0.82),
            ]
        )
    elif terrain_key == "fort":
        block = mix(palette.stone, palette.ground_light, 0.16)
        seam = darken(palette.stone, 0.14)
        for y in range(4, 60, 12):
            shapes.append(rect(4, y, 56, 10, block, 0.94))
        for x in range(4 + offset_x, 60, 16):
            shapes.append(rect(x, 4, 2, 56, seam, 0.88))
        shapes.append(rect(4, 28, 56, 2, seam, 0.88))
    elif terrain_key == "hazard":
        if palette.style == "river":
            water = palette.hazard
            shapes.extend(
                [
                    rect(2, 2, 60, 60, darken(water, 0.24), 0.92),
                    rect(4, 8 + offset_y, 52, 12, lighten(water, 0.06), 0.72),
                    rect(8 + offset_x, 26, 48, 10, mix(water, palette.accent, 0.16), 0.66),
                    rect(4, 42, 54, 12, darken(water, 0.08), 0.72),
                ]
            )
        else:
            ash = darken(palette.ground_dark, 0.1)
            flame = palette.hazard
            shapes.extend(
                [
                    rect(4, 4, 56, 56, ash, 0.94),
                    rect(6 + offset_x, 10, 18, 10, darken(flame, 0.22), 0.7),
                    rect(26, 22 + offset_y, 18, 10, darken(flame, 0.14), 0.7),
                    rect(40 + offset_x, 38, 16, 10, darken(flame, 0.06), 0.72),
                    rect(16, 44 + offset_y, 18, 8, mix(flame, palette.accent, 0.2), 0.7),
                ]
            )
    else:
        rock = mix(palette.stone, darken(palette.ground_dark, 0.18), 0.6)
        shapes.extend(
            [
                rect(4, 4, 56, 56, darken(rock, 0.12), 0.94),
                rect(10 + offset_x, 8, 20, 16, rock, 0.82),
                rect(30, 18 + offset_y, 18, 14, lighten(rock, 0.06), 0.82),
                rect(12, 34, 22, 16, darken(rock, 0.1), 0.82),
                rect(36 + offset_x, 40, 16, 12, lighten(rock, 0.04), 0.82),
            ]
        )

    shapes.extend(
        [
            rect(2, 2, 60, 2, accent, 0.18),
            rect(2, 60, 60, 2, darken(palette.ground_dark, 0.08), 0.34),
            rect(2, 2, 2, 60, accent, 0.12),
            rect(60, 2, 2, 60, darken(palette.ground_dark, 0.08), 0.24),
        ]
    )
    return shapes


def render_overlay_layer(palette: TerrainPaletteSpec, terrain_key: str, variant: str) -> list[str]:
    offset_x, offset_y = variant_offsets(variant)
    shapes: list[str] = []

    if terrain_key == "plain":
        if palette.style == "river":
            ripple = lighten(palette.hazard, 0.26)
            shapes.extend(
                [
                    line([(10, 18 + offset_y), (22, 16 + offset_y), (34, 18 + offset_y)], ripple, 2, 0.52),
                    line([(20 + offset_x, 34), (34 + offset_x, 32), (46 + offset_x, 34)], ripple, 2, 0.5),
                    line([(8, 48), (18, 46), (28, 48)], ripple, 2, 0.46),
                ]
            )
        elif palette.style in {"smoke", "gate"}:
            ash = lighten(palette.accent, 0.08)
            shapes.extend(
                [
                    rect(10 + offset_x, 16, 6, 2, ash, 0.44),
                    rect(18 + offset_x, 22, 4, 2, ash, 0.34),
                    rect(40, 38 + offset_y, 6, 2, ash, 0.4),
                    rect(28, 48 + offset_y, 4, 2, ash, 0.34),
                ]
            )
        else:
            grass = lighten(palette.foliage, 0.12)
            shapes.extend(
                [
                    line([(12, 18), (14, 12), (18, 18)], grass, 2, 0.58),
                    line([(34 + offset_x, 26), (36 + offset_x, 20), (40 + offset_x, 26)], grass, 2, 0.56),
                    line([(22, 50 + offset_y), (24, 44 + offset_y), (28, 50 + offset_y)], grass, 2, 0.52),
                ]
            )
    elif terrain_key == "forest":
        leaf = lighten(palette.foliage, 0.16)
        shapes.extend(
            [
                rect(8 + offset_x, 10, 8, 4, leaf, 0.48),
                rect(28, 18 + offset_y, 10, 4, leaf, 0.5),
                rect(44, 12, 8, 4, leaf, 0.44),
                rect(16, 40 + offset_y, 10, 4, leaf, 0.46),
                rect(36 + offset_x, 46, 10, 4, leaf, 0.44),
            ]
        )
    elif terrain_key == "fort":
        trim = lighten(palette.accent, 0.06)
        seam = darken(palette.stone, 0.18)
        shapes.extend(
            [
                rect(6, 12 + offset_y, 52, 2, trim, 0.42),
                rect(6, 30, 52, 2, trim, 0.34),
                rect(6, 48 + offset_y, 52, 2, trim, 0.38),
                rect(18 + offset_x, 4, 2, 56, seam, 0.42),
                rect(38, 4, 2, 56, seam, 0.42),
            ]
        )
    elif terrain_key == "hazard":
        if palette.style == "river":
            foam = lighten(palette.accent, 0.24)
            shapes.extend(
                [
                    line([(12, 14 + offset_y), (20, 12 + offset_y), (30, 14 + offset_y)], foam, 2, 0.56),
                    line([(34 + offset_x, 28), (44 + offset_x, 26), (54 + offset_x, 28)], foam, 2, 0.56),
                    line([(14, 46), (24, 44), (36, 46)], foam, 2, 0.52),
                ]
            )
        else:
            flame = lighten(palette.hazard, 0.08)
            core = lighten(palette.accent, 0.18)
            shapes.extend(
                [
                    poly([(14, 48), (18, 32), (24, 40), (20, 54)], flame, 0.72),
                    poly([(32 + offset_x, 40), (36 + offset_x, 26), (42 + offset_x, 34), (38 + offset_x, 46)], flame, 0.72),
                    poly([(18, 46), (20, 36), (22, 42), (20, 50)], core, 0.78),
                ]
            )
    else:
        crack = lighten(palette.stone, 0.1)
        shapes.extend(
            [
                line([(12, 16), (22, 26), (32, 20)], crack, 2, 0.46),
                line([(34 + offset_x, 18), (44 + offset_x, 28), (52 + offset_x, 22)], crack, 2, 0.46),
                line([(18, 40), (28, 48), (38, 42)], crack, 2, 0.42),
            ]
        )

    return shapes


def render_prop_layer(palette: TerrainPaletteSpec, terrain_key: str, variant: str) -> list[str]:
    offset_x, offset_y = variant_offsets(variant)
    shapes: list[str] = []

    if terrain_key == "plain":
        if palette.style == "smoke":
            shapes.extend(
                [
                    rect(16 + offset_x, 40, 8, 6, lighten(palette.accent, 0.06), 0.34),
                    rect(18 + offset_x, 36, 4, 4, lighten(palette.hazard, 0.08), 0.44),
                ]
            )
        elif palette.style == "river":
            shapes.extend(
                [
                    line([(44 + offset_x, 18), (46 + offset_x, 10), (50 + offset_x, 18)], lighten(palette.foliage, 0.18), 2, 0.66),
                    line([(50 + offset_x, 18), (52 + offset_x, 8), (56 + offset_x, 18)], lighten(palette.foliage, 0.18), 2, 0.62),
                ]
            )
        else:
            shapes.append(line([(18, 42), (20, 34), (24, 42)], lighten(palette.foliage, 0.18), 2, 0.66))
    elif terrain_key == "forest":
        trunk = darken(palette.wood, 0.18)
        canopy = darken(palette.foliage, 0.04)
        shapes.extend(
            [
                rect(28 + offset_x, 26, 6, 18, trunk, 0.82),
                rect(22 + offset_x, 16, 18, 12, canopy, 0.86),
                rect(18 + offset_x, 24, 10, 8, canopy, 0.82),
                rect(34 + offset_x, 24, 10, 8, canopy, 0.82),
            ]
        )
    elif terrain_key == "fort":
        if palette.style in {"smoke", "gate"}:
            banner = palette.accent
            post = darken(palette.wood, 0.18)
            shapes.extend(
                [
                    rect(18 + offset_x, 18, 4, 24, post, 0.82),
                    rect(22 + offset_x, 20, 12, 10, banner, 0.74),
                    rect(40, 20 + offset_y, 4, 22, post, 0.8),
                ]
            )
        else:
            shapes.extend(
                [
                    rect(20 + offset_x, 34, 22, 8, darken(palette.wood, 0.12), 0.82),
                    rect(24 + offset_x, 28, 4, 14, darken(palette.wood, 0.18), 0.82),
                    rect(36 + offset_x, 28, 4, 14, darken(palette.wood, 0.18), 0.82),
                ]
            )
    elif terrain_key == "hazard":
        if palette.style == "river":
            shapes.extend(
                [
                    poly([(18 + offset_x, 42), (24 + offset_x, 34), (30 + offset_x, 42), (24 + offset_x, 48)], lighten(palette.accent, 0.1), 0.42),
                    poly([(40, 28 + offset_y), (46, 22 + offset_y), (52, 28 + offset_y), (46, 34 + offset_y)], lighten(palette.accent, 0.08), 0.38),
                ]
            )
        else:
            brazier = darken(palette.wood, 0.18)
            flame = lighten(palette.hazard, 0.08)
            shapes.extend(
                [
                    rect(18 + offset_x, 40, 10, 6, brazier, 0.82),
                    poly([(20 + offset_x, 40), (23 + offset_x, 26), (28 + offset_x, 38)], flame, 0.82),
                    rect(40, 44 + offset_y, 10, 6, brazier, 0.72),
                    poly([(42, 44 + offset_y), (45, 32 + offset_y), (50, 42 + offset_y)], flame, 0.72),
                ]
            )
    else:
        rock = lighten(palette.stone, 0.02)
        shapes.extend(
            [
                rect(18 + offset_x, 26, 18, 12, rock, 0.7),
                rect(14 + offset_x, 36, 24, 12, darken(rock, 0.08), 0.78),
                rect(38, 18 + offset_y, 12, 10, lighten(rock, 0.06), 0.68),
            ]
        )

    return shapes


def variant_offsets(variant: str) -> tuple[int, int]:
    if variant == "b":
        return 4, -2
    if variant == "c":
        return -4, 3
    return 0, 0


def generate_terrain_assets() -> None:
    ensure_dir(TERRAIN_SOURCE_ROOT)
    ensure_dir(TERRAIN_OUTPUT_ROOT)
    count = 0

    for palette in PALETTES:
        for terrain_key in TERRAIN_KEYS:
            for variant in VARIANTS:
                for layer in LAYERS:
                    svg_path, png_path = terrain_paths_for(palette.key, terrain_key, variant, layer)
                    ensure_dir(svg_path.parent)
                    ensure_dir(png_path.parent)
                    svg_path.write_text(render_tile_svg(palette, terrain_key, variant, layer), encoding="utf-8")
                    convert_svg_to_png(svg_path, png_path, TERRAIN_SIZE)
                    count += 1

    print(f"Generated terrain sprites: {count}")


if __name__ == "__main__":
    generate_terrain_assets()
