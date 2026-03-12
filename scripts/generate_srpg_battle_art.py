#!/usr/bin/env python3

from __future__ import annotations

import csv
from pathlib import Path
from typing import Iterable

from generate_character_art import (
    ART_SOURCE_ROOT,
    BATTLE_SIZE,
    CHARACTER_SPECS,
    MANIFEST_PATH,
    MA_CHAO_PALETTE,
    Palette,
    PLAYER_PALETTE,
    PURSUIT_PALETTE,
    RSVG_CONVERT,
    WEI_PALETTE,
    XIAHOU_DUN_PALETTE,
    XIAHOU_YUAN_PALETTE,
    YELLOW_TURBAN_PALETTE,
    ZHAO_YUN_PALETTE,
    battle_paths_for,
    convert_svg_to_png,
    darken,
    ensure_dir,
    lighten,
    load_asset_keys,
    mix,
    CharacterSpec,
)

FIGURE_CENTER_X = 64
FIGURE_CENTER_Y = 66
FIGURE_SCALE_X = 1.14
FIGURE_SCALE_Y = 1.18
FIGURE_OFFSET_Y = 2


def transform_point(x: int, y: int) -> tuple[int, int]:
    transformed_x = FIGURE_CENTER_X + ((x - FIGURE_CENTER_X) * FIGURE_SCALE_X)
    transformed_y = FIGURE_CENTER_Y + ((y - FIGURE_CENTER_Y) * FIGURE_SCALE_Y) + FIGURE_OFFSET_Y
    return int(round(transformed_x)), int(round(transformed_y))


def poly(points: Iterable[tuple[int, int]], fill: str, opacity: float = 1.0) -> str:
    transformed_points = [transform_point(x, y) for x, y in points]
    point_string = " ".join(f"{x},{y}" for x, y in transformed_points)
    return f'<polygon points="{point_string}" fill="{fill}" fill-opacity="{opacity:.3f}" />'


def rect(x: int, y: int, width: int, height: int, fill: str, opacity: float = 1.0) -> str:
    x1, y1 = transform_point(x, y)
    x2, y2 = transform_point(x + width, y + height)
    transformed_width = max(1, x2 - x1)
    transformed_height = max(1, y2 - y1)
    return f'<rect x="{x1}" y="{y1}" width="{transformed_width}" height="{transformed_height}" fill="{fill}" fill-opacity="{opacity:.3f}" />'


def line(points: Iterable[tuple[int, int]], stroke: str, width: int, opacity: float = 1.0) -> str:
    transformed_points = [transform_point(x, y) for x, y in points]
    point_string = " ".join(f"{x},{y}" for x, y in transformed_points)
    transformed_width = max(1, int(round(width * ((FIGURE_SCALE_X + FIGURE_SCALE_Y) * 0.5))))
    return f'<polyline points="{point_string}" fill="none" stroke="{stroke}" stroke-width="{transformed_width}" stroke-linecap="square" stroke-linejoin="miter" stroke-opacity="{opacity:.3f}" />'


def translate(points: Iterable[tuple[int, int]], dx: int = 0, dy: int = 0, flip_x: bool = False) -> list[tuple[int, int]]:
    points = list(points)
    if not flip_x:
        return [(x + dx, y + dy) for x, y in points]

    return [((128 - x) + dx, y + dy) for x, y in points]


def render_battle_svg(spec: CharacterSpec) -> str:
    p = spec.palette
    outline = darken(p.line, 0.14)
    cloth_dark = darken(p.primary, 0.24)
    cloth_mid = darken(p.primary, 0.08)
    cloth_light = lighten(p.primary, 0.2)
    metal_dark = darken(p.metal, 0.22)
    metal_light = lighten(p.metal, 0.22)
    accent_dark = darken(p.accent, 0.18)
    accent_light = lighten(p.accent, 0.16)
    hair = darken(p.hair, 0.14)
    skin_dark = darken(p.skin, 0.12)
    skin_light = lighten(p.skin, 0.1)

    parts = [
        render_weapon(spec, outline, metal_light, metal_dark, accent_light),
        render_back_cloak(spec, outline, cloth_dark),
        render_body(spec, outline, cloth_mid, cloth_dark, cloth_light, accent_dark, metal_dark),
        render_arms(spec, outline, cloth_dark, metal_dark),
        render_head(spec, outline, skin_light, skin_dark, hair, metal_light, accent_light),
        render_role_signature(spec, outline, accent_dark, metal_light),
    ]

    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{BATTLE_SIZE}" height="{BATTLE_SIZE}" viewBox="0 0 {BATTLE_SIZE} {BATTLE_SIZE}" shape-rendering="crispEdges">
  <rect width="128" height="128" fill="#000000" fill-opacity="0" />
  {''.join(parts)}
</svg>
"""


def render_back_cloak(spec: CharacterSpec, outline: str, cloth_dark: str) -> str:
    if spec.shoulder_style not in {"robe", "cloak", "raider"}:
        return ""

    flip_left = spec.weapon_side == "left"
    cloak = translate([(48, 56), (42, 76), (48, 98), (60, 90), (64, 70), (70, 90), (82, 98), (88, 74), (80, 54), (66, 48)], flip_x=flip_left)
    inner = translate([(50, 58), (46, 76), (50, 94), (60, 88), (64, 70), (68, 88), (78, 94), (82, 76), (78, 56), (66, 52)], flip_x=flip_left)
    return poly(cloak, outline) + poly(inner, cloth_dark)


def render_body(spec: CharacterSpec, outline: str, cloth_mid: str, cloth_dark: str, cloth_light: str, accent_dark: str, metal_dark: str) -> str:
    body_shapes = {
        "Commander": [(48, 56), (52, 74), (52, 92), (60, 96), (64, 82), (68, 96), (76, 92), (76, 74), (80, 56), (72, 48), (56, 48)],
        "Guardian": [(44, 56), (48, 76), (48, 92), (58, 98), (64, 84), (70, 98), (80, 92), (80, 76), (84, 56), (74, 46), (54, 46)],
        "Ranger": [(50, 58), (52, 76), (54, 92), (60, 96), (64, 84), (68, 96), (74, 92), (76, 76), (78, 58), (70, 50), (58, 50)],
        "Scout": [(50, 60), (52, 78), (54, 92), (60, 96), (64, 84), (68, 96), (74, 92), (76, 78), (78, 60), (70, 50), (58, 50)],
        "Raider": [(46, 58), (50, 76), (50, 90), (58, 96), (64, 82), (70, 96), (82, 90), (82, 74), (86, 58), (74, 48), (54, 48)],
    }
    inner_shapes = {
        "Commander": [(50, 58), (54, 74), (54, 90), (60, 94), (64, 80), (68, 94), (74, 90), (74, 74), (78, 58), (70, 50), (58, 50)],
        "Guardian": [(46, 58), (50, 76), (50, 90), (58, 96), (64, 82), (70, 96), (78, 90), (78, 76), (82, 58), (72, 48), (56, 48)],
        "Ranger": [(52, 60), (54, 76), (56, 90), (60, 94), (64, 82), (68, 94), (72, 90), (74, 76), (76, 60), (68, 52), (60, 52)],
        "Scout": [(52, 62), (54, 78), (56, 90), (60, 94), (64, 82), (68, 94), (72, 90), (74, 78), (76, 62), (68, 52), (60, 52)],
        "Raider": [(48, 60), (52, 76), (52, 88), (58, 94), (64, 80), (70, 94), (80, 88), (80, 74), (84, 60), (72, 50), (56, 50)],
    }

    sash = {
        "Commander": [(56, 66), (64, 62), (72, 66), (68, 72), (60, 72)],
        "Guardian": [(54, 68), (64, 64), (74, 68), (70, 74), (58, 74)],
        "Ranger": [(56, 68), (64, 64), (72, 68), (68, 72), (60, 72)],
        "Scout": [(56, 68), (64, 64), (72, 68), (68, 72), (60, 72)],
        "Raider": [(54, 66), (64, 62), (76, 68), (72, 74), (58, 72)],
    }

    chest = [(58, 60), (64, 56), (70, 60), (68, 72), (60, 72)]
    center_shadow = [(62, 56), (64, 56), (68, 92), (64, 96), (60, 92)]
    layers = [
        poly(body_shapes.get(spec.role, body_shapes["Commander"]), outline),
        poly(inner_shapes.get(spec.role, inner_shapes["Commander"]), cloth_mid),
        poly(center_shadow, cloth_dark, 0.9),
        poly(chest, cloth_light, 0.84),
        poly(sash.get(spec.role, sash["Commander"]), accent_dark, 0.78),
    ]

    if spec.role in {"Guardian", "Raider"}:
        layers.append(poly([(46, 64), (50, 56), (58, 58), (58, 76), (48, 82), (44, 74)], metal_dark, 0.88))
        layers.append(poly([(82, 64), (78, 56), (70, 58), (70, 76), (80, 82), (84, 74)], metal_dark, 0.88))
    elif spec.role in {"Ranger", "Scout"}:
        layers.append(poly([(46, 58), (52, 54), (56, 72), (50, 84), (44, 78)], darken(cloth_dark, 0.08), 0.92))

    return "".join(layers)


def render_arms(spec: CharacterSpec, outline: str, cloth_dark: str, metal_dark: str) -> str:
    if spec.role == "Guardian":
        left = [(42, 66), (38, 76), (42, 86), (48, 82), (48, 68)]
        right = [(86, 66), (80, 68), (80, 82), (86, 86), (90, 76)]
    elif spec.role in {"Ranger", "Scout"}:
        left = [(48, 66), (42, 72), (44, 84), (50, 80), (54, 70)]
        right = [(80, 66), (74, 70), (78, 80), (84, 84), (86, 72)]
    else:
        left = [(46, 64), (40, 74), (44, 86), (50, 82), (54, 66)]
        right = [(82, 64), (74, 66), (78, 82), (84, 86), (90, 74)]

    brace = [(40, 74), (44, 72), (46, 80), (42, 82)]
    brace_right = [(82, 72), (86, 74), (84, 82), (80, 80)]
    return "".join(
        [
            poly(left, outline),
            poly([(x + 1, y + 1) for x, y in left], cloth_dark),
            poly(right, outline),
            poly([(x - 1, y + 1) for x, y in right], cloth_dark),
            poly(brace, metal_dark, 0.86),
            poly(brace_right, metal_dark, 0.86),
        ]
    )


def render_head(spec: CharacterSpec, outline: str, skin_light: str, skin_dark: str, hair: str, metal_light: str, accent_light: str) -> str:
    face = [
        rect(56, 36, 16, 18, outline),
        rect(57, 37, 14, 16, skin_light),
        rect(57, 45, 14, 8, skin_dark, 0.28),
        rect(59, 43, 2, 2, outline, 0.7),
        rect(67, 43, 2, 2, outline, 0.7),
        rect(63, 47, 2, 3, darken(skin_dark, 0.18), 0.46),
    ]

    hair_shapes = {
        "tapered": [(56, 40), (58, 34), (64, 30), (70, 34), (72, 42), (68, 40), (64, 38), (58, 40)],
        "heavy": [(54, 42), (56, 34), (64, 28), (72, 32), (74, 42), (70, 40), (64, 38), (58, 40)],
        "wild": [(54, 42), (56, 32), (62, 28), (66, 30), (70, 26), (74, 34), (74, 42), (70, 40), (64, 38), (58, 40)],
        "trimmed": [(56, 40), (58, 34), (64, 30), (70, 34), (72, 40), (68, 38), (60, 38)],
        "wind": [(56, 40), (58, 32), (64, 30), (72, 34), (76, 38), (74, 44), (68, 40), (60, 38)],
    }
    face.append(poly(hair_shapes.get(spec.hair_shape, hair_shapes["tapered"]), hair))
    face.append(render_headwear(spec, outline, metal_light, accent_light))
    face.append(render_beard(spec, hair))

    if spec.face_mark == "eyepatch":
        face.append(rect(66, 41, 4, 4, outline, 0.92))
        face.append(line([(58, 40), (70, 44)], outline, 1, 0.88))

    return "".join(face)


def render_headwear(spec: CharacterSpec, outline: str, metal_light: str, accent_light: str) -> str:
    mapping = {
        "crown": poly([(56, 34), (58, 24), (64, 30), (70, 22), (74, 34), (70, 36), (58, 36)], accent_light, 0.92),
        "highcap": poly([(58, 36), (60, 24), (64, 20), (70, 24), (72, 36), (66, 38), (58, 38)], darken(spec.palette.secondary, 0.02), 0.92),
        "warhelm": poly([(54, 36), (58, 28), (64, 24), (72, 28), (74, 36), (68, 36), (64, 34), (58, 36)], metal_light, 0.96),
        "eldercap": poly([(56, 36), (58, 28), (64, 24), (72, 28), (74, 36), (68, 38), (58, 38)], darken(spec.palette.secondary, 0.02), 0.88),
        "scholarcap": poly([(56, 36), (58, 28), (64, 24), (72, 26), (74, 34), (70, 36), (58, 36)], darken(spec.palette.secondary, 0.08), 0.94),
        "plumehelm": poly([(54, 36), (58, 28), (64, 24), (72, 28), (76, 36), (68, 36), (64, 34), (58, 36)], metal_light, 0.96) + line([(66, 24), (74, 18), (82, 26)], lighten(spec.palette.paper, 0.04), 2, 0.54),
        "westernhelm": poly([(54, 36), (58, 28), (64, 24), (72, 26), (76, 36), (68, 36), (64, 34), (58, 36)], metal_light, 0.96) + line([(66, 24), (74, 18), (82, 24)], accent_light, 2, 0.56),
        "talisman": poly([(56, 36), (58, 26), (64, 22), (70, 26), (72, 36), (68, 38), (58, 38)], accent_light, 0.9) + rect(62, 18, 4, 10, lighten(spec.palette.paper, 0.02), 0.7),
        "rebelcrown": poly([(54, 34), (58, 28), (62, 32), (64, 24), (68, 32), (72, 28), (76, 34), (70, 38), (58, 38)], accent_light, 0.86),
        "weihelm": poly([(54, 36), (58, 28), (64, 24), (72, 28), (76, 36), (68, 36), (64, 34), (58, 36)], metal_light, 0.94),
        "scarhelm": poly([(54, 36), (58, 28), (64, 24), (72, 28), (76, 36), (68, 36), (64, 34), (58, 36)], metal_light, 0.94) + line([(70, 24), (78, 20), (84, 28)], accent_light, 2, 0.42),
        "hawkhelm": poly([(54, 36), (58, 28), (64, 24), (72, 28), (76, 36), (68, 36), (64, 34), (58, 36)], metal_light, 0.94) + line([(66, 24), (76, 20), (84, 30)], lighten(spec.palette.paper, 0.04), 2, 0.46),
        "bandana": poly([(54, 36), (58, 32), (64, 30), (72, 32), (76, 36), (72, 40), (56, 40)], accent_light, 0.82),
        "lighthelm": poly([(56, 36), (58, 30), (64, 26), (70, 30), (72, 36), (68, 36), (58, 36)], metal_light, 0.86),
    }
    return mapping.get(spec.headwear, rect(58, 30, 12, 6, outline, 0.0))


def render_beard(spec: CharacterSpec, hair: str) -> str:
    if spec.beard == "none":
        return ""
    mapping = {
        "short": poly([(60, 50), (64, 54), (68, 50), (66, 58), (64, 60), (62, 58)], hair, 0.82),
        "trim": poly([(60, 50), (64, 54), (68, 50), (66, 56), (64, 58), (62, 56)], hair, 0.78),
        "mustache": poly([(58, 50), (64, 52), (70, 50), (64, 54)], hair, 0.86),
        "goatee": poly([(60, 50), (64, 52), (68, 50), (66, 58), (64, 60), (62, 58)], hair, 0.88),
        "full": poly([(58, 48), (60, 58), (64, 70), (68, 58), (70, 48), (68, 72), (64, 80), (60, 72)], hair, 0.92),
        "long": poly([(58, 48), (60, 60), (64, 82), (68, 60), (70, 48), (68, 84), (64, 94), (60, 84)], hair, 0.94),
        "elder": poly([(58, 48), (60, 58), (64, 76), (68, 58), (70, 48), (68, 78), (64, 88), (60, 78)], lighten(hair, 0.12), 0.84),
    }
    return mapping.get(spec.beard, "")


def render_weapon(spec: CharacterSpec, outline: str, metal_light: str, metal_dark: str, accent_light: str) -> str:
    flip_left = spec.weapon_side == "left"
    offset = -4 if flip_left else 4

    if spec.weapon in {"spear", "lance"}:
        shaft = translate([(92, 28), (94, 28), (86, 102), (84, 102)], dx=offset, flip_x=flip_left)
        blade = translate([(92, 18), (98, 28), (90, 34)], dx=offset, flip_x=flip_left)
        tassel = translate([(90, 38), (96, 34), (100, 42), (94, 46)], dx=offset, flip_x=flip_left)
        return poly(shaft, metal_dark, 0.88) + poly(blade, metal_light, 0.96) + poly(tassel, accent_light, 0.62)

    if spec.weapon == "glaive":
        shaft = translate([(92, 30), (94, 30), (86, 102), (84, 102)], dx=offset, flip_x=flip_left)
        blade = translate([(92, 18), (104, 24), (96, 38), (88, 34)], dx=offset, flip_x=flip_left)
        return poly(shaft, metal_dark, 0.88) + poly(blade, metal_light, 0.96)

    if spec.weapon == "bow":
        bow = translate([(88, 26), (94, 34), (96, 48), (94, 72), (88, 82), (86, 72), (88, 48), (86, 34)], dx=offset, flip_x=flip_left)
        string = translate([(90, 30), (92, 48), (90, 78)], dx=offset, flip_x=flip_left)
        return poly(bow, accent_light, 0.76) + line(string, lighten(spec.palette.paper, 0.02), 1, 0.78)

    if spec.weapon == "fan":
        fan = translate([(84, 64), (94, 56), (102, 64), (94, 72)], dx=offset, flip_x=flip_left)
        ribs = translate([(90, 60), (94, 56), (98, 60)], dx=offset, flip_x=flip_left)
        handle = translate([(90, 70), (92, 70), (94, 82), (92, 84)], dx=offset, flip_x=flip_left)
        return poly(fan, lighten(spec.palette.paper, 0.02), 0.92) + poly(ribs, accent_light, 0.46) + poly(handle, metal_dark, 0.78)

    if spec.weapon == "seal":
        block = translate([(88, 56), (96, 56), (96, 68), (88, 68)], dx=offset, flip_x=flip_left)
        top = translate([(90, 48), (94, 48), (94, 56), (90, 56)], dx=offset, flip_x=flip_left)
        return poly(block, accent_light, 0.88) + poly(top, metal_dark, 0.82)

    blade = translate([(92, 20), (98, 32), (92, 46), (86, 32)], dx=offset, flip_x=flip_left)
    grip = translate([(90, 44), (94, 44), (88, 102), (84, 102)], dx=offset, flip_x=flip_left)
    cross = translate([(86, 46), (98, 46), (98, 50), (86, 50)], dx=offset, flip_x=flip_left)
    return poly(blade, metal_light, 0.96) + poly(grip, metal_dark, 0.86) + poly(cross, accent_light, 0.68)


def render_role_signature(spec: CharacterSpec, outline: str, accent_dark: str, metal_light: str) -> str:
    if spec.role == "Commander":
        return rect(62, 58, 4, 16, metal_light, 0.52) + rect(60, 70, 8, 3, accent_dark, 0.56)
    if spec.role == "Guardian":
        return rect(42, 70, 6, 10, metal_light, 0.48) + rect(80, 70, 6, 10, metal_light, 0.48)
    if spec.role == "Ranger":
        return poly([(46, 60), (50, 58), (54, 70), (50, 80), (44, 76)], darken(spec.palette.secondary, 0.16), 0.84)
    if spec.role == "Scout":
        return line([(50, 60), (46, 74), (50, 88)], metal_light, 2, 0.56) + line([(78, 60), (82, 74), (78, 88)], metal_light, 2, 0.56)
    return line([(48, 62), (82, 84)], metal_light, 2, 0.42)


def generate_battle_assets() -> None:
    character_specs = load_manifest_specs()
    ensure_dir(ART_SOURCE_ROOT / "Battle")

    count = 0
    for key in sorted(character_specs):
        spec = character_specs[key]
        battle_svg, battle_png = battle_paths_for(spec)
        ensure_dir(battle_svg.parent)
        ensure_dir(battle_png.parent)
        battle_svg.write_text(render_battle_svg(spec), encoding="utf-8")
        convert_svg_to_png(battle_svg, battle_png, BATTLE_SIZE)
        count += 1

    print(f"Generated SRPG battle sprites: {count}")


def load_manifest_specs() -> dict[str, CharacterSpec]:
    specs: dict[str, CharacterSpec] = {}
    with MANIFEST_PATH.open(newline="") as manifest_file:
        rows = list(csv.DictReader(manifest_file))

    for row in rows:
        unit_id = row["unit_id"]
        if unit_id in CHARACTER_SPECS:
            specs[unit_id] = CHARACTER_SPECS[unit_id]
            continue

        specs[unit_id] = derive_manifest_spec(row)

    return specs


def derive_manifest_spec(row: dict[str, str]) -> CharacterSpec:
    unit_id = row["unit_id"]
    role = row["role"].strip().title()
    faction = row["faction"].strip().lower()
    category = "Heroes" if faction == "player" else "Enemies"
    palette = derive_palette(unit_id)
    weapon = derive_weapon(row.get("weapon_icon_key", ""), role)
    headwear = derive_headwear(unit_id, role)
    beard = derive_beard(unit_id, role)
    shoulder_style = derive_shoulders(unit_id, role)
    hair_shape = derive_hair(unit_id, role)
    weapon_side = "left" if "raider" in unit_id or "tiger_leopard" in unit_id or role == "Raider" else "right"

    return CharacterSpec(
        base_key=unit_id,
        category=category,
        faction=faction,
        role=role,
        palette=palette,
        weapon=weapon,
        headwear=headwear,
        beard=beard,
        shoulder_style=shoulder_style,
        hair_shape=hair_shape,
        weapon_side=weapon_side,
    )


def derive_palette(unit_id: str) -> Palette:
    lower = unit_id.lower()
    if "yellow_turban" in lower or "fervent" in lower or "zealot" in lower:
        if "archer" in lower:
            return tint_palette(YELLOW_TURBAN_PALETTE, primary="#957934", secondary="#5A3B22", accent="#E3C06A")
        if "zealot" in lower:
            return tint_palette(YELLOW_TURBAN_PALETTE, primary="#8A6C3C", secondary="#4A3125", accent="#D9B86A")
        return tint_palette(YELLOW_TURBAN_PALETTE, primary="#A07D34", secondary="#5A381F", accent="#E1B760")

    if "bowang" in lower:
        if "archer" in lower or "hunter" in lower:
            return tint_palette(WEI_PALETTE, primary="#6D7155", secondary="#384150", accent="#C9A265")
        if "rider" in lower or "rearguard" in lower:
            return tint_palette(WEI_PALETTE, primary="#5C6558", secondary="#33424C", accent="#C49860")
        return tint_palette(WEI_PALETTE, primary="#6F634F", secondary="#3C4044", accent="#C79F68")

    if "hanshui" in lower or "river" in lower:
        if "bow" in lower or "deadeye" in lower:
            return tint_palette(PURSUIT_PALETTE, primary="#587184", secondary="#273847", accent="#D0A46B")
        if "rider" in lower or "raider" in lower:
            return tint_palette(PURSUIT_PALETTE, primary="#4F697E", secondary="#283848", accent="#D08E5D")
        return tint_palette(PURSUIT_PALETTE, primary="#627B87", secondary="#2F414A", accent="#C7A36D")

    if "jiameng" in lower or "gate" in lower or "stonewall" in lower:
        if "bow" in lower:
            return tint_palette(WEI_PALETTE, primary="#796A5A", secondary="#453A36", accent="#D0A06A")
        return tint_palette(WEI_PALETTE, primary="#716355", secondary="#4B433E", accent="#C88B5C")

    if "tiger_guard" in lower:
        return tint_palette(XIAHOU_DUN_PALETTE, primary="#4C5372", secondary="#232632", accent="#D28E53")

    if "tiger_leopard" in lower or "cavalry_scout" in lower:
        return tint_palette(ZHAO_YUN_PALETTE if "scout" in lower else MA_CHAO_PALETTE, primary="#D4D4CF", secondary="#496B83", accent="#C69D62")

    if "pursuit" in lower:
        return tint_palette(PURSUIT_PALETTE, primary="#56697A", secondary="#2A3440", accent="#C98755")

    if "xiahou-dun" in lower:
        return XIAHOU_DUN_PALETTE

    if "xiahou-yuan" in lower:
        return XIAHOU_YUAN_PALETTE

    if "wei" in lower:
        return tint_palette(WEI_PALETTE, primary="#5B6878", secondary="#2B3441", accent="#C19A68")

    return tint_palette(PLAYER_PALETTE, primary="#5A6E58", secondary="#36424A", accent="#C5A05B")


def tint_palette(base: Palette, primary: str, secondary: str, accent: str) -> Palette:
    return Palette(
        line=base.line,
        bg_top=base.bg_top,
        bg_bottom=base.bg_bottom,
        haze=base.haze,
        paper=base.paper,
        primary=primary,
        secondary=secondary,
        accent=accent,
        metal=base.metal,
        skin=base.skin,
        hair=base.hair,
    )


def derive_weapon(icon_key: str, role: str) -> str:
    lower = icon_key.lower()
    if "fan" in lower:
        return "fan"
    if "seal" in lower:
        return "seal"
    if "glaive" in lower or "halberd" in lower or "polearm" in lower:
        return "glaive"
    if "bow" in lower:
        return "bow"
    if "lance" in lower:
        return "lance"
    if "spear" in lower:
        return "spear"
    if role == "Ranger":
        return "bow"
    if role in {"Scout", "Raider"}:
        return "lance"
    return "blade"


def derive_headwear(unit_id: str, role: str) -> str:
    lower = unit_id.lower()
    if "yellow_turban" in lower or "fervent" in lower or "zealot" in lower:
        return "bandana" if role != "Commander" else "rebelcrown"
    if "tiger_guard" in lower:
        return "scarhelm"
    if "tiger_leopard" in lower:
        return "westernhelm"
    if "jiameng" in lower or "gate" in lower:
        return "warhelm" if role == "Guardian" else "weihelm"
    if "hanshui" in lower or "river" in lower:
        return "hawkhelm" if role == "Ranger" else "lighthelm"
    if "bowang" in lower:
        return "weihelm" if role != "Raider" else "lighthelm"
    if "pursuit" in lower:
        return "weihelm"
    if "wei" in lower:
        return "weihelm"
    return "crown" if role == "Commander" else "weihelm"


def derive_beard(unit_id: str, role: str) -> str:
    lower = unit_id.lower()
    if role == "Ranger":
        return "none"
    if "captain" in lower or "command" in lower or "warden" in lower:
        return "trim"
    if "yellow_turban" in lower or "fervent" in lower:
        return "mustache" if role != "Guardian" else "full"
    if role == "Guardian":
        return "trim"
    return "none"


def derive_shoulders(unit_id: str, role: str) -> str:
    lower = unit_id.lower()
    if role == "Commander":
        return "robe"
    if role == "Guardian":
        return "armor"
    if role == "Ranger":
        return "cloak"
    if "tiger_leopard" in lower or role == "Raider":
        return "raider"
    return "light"


def derive_hair(unit_id: str, role: str) -> str:
    lower = unit_id.lower()
    if "yellow_turban" in lower or "fervent" in lower:
        return "wild"
    if "tiger_leopard" in lower or role == "Raider":
        return "wind"
    if role == "Guardian":
        return "heavy"
    if role == "Ranger":
        return "trimmed"
    return "tapered"


if __name__ == "__main__":
    generate_battle_assets()
