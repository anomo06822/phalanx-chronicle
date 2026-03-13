#!/usr/bin/env python3

from __future__ import annotations

import csv
import math
import random
import subprocess
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


ROOT = Path(__file__).resolve().parents[1]
MANIFEST_PATH = ROOT / "Docs" / "character-asset-manifest.csv"
ART_SOURCE_ROOT = ROOT / "Assets" / "ArtSource" / "Characters" / "Generated"
PORTRAIT_OUTPUT_ROOT = ROOT / "Assets" / "Art" / "Characters" / "Portraits"
BATTLE_OUTPUT_ROOT = ROOT / "Assets" / "Art" / "Characters" / "Battle"
WEAPON_OUTPUT_ROOT = ROOT / "Assets" / "Art" / "UI" / "Weapons"

RSVG_CONVERT = "/opt/homebrew/bin/rsvg-convert"
PORTRAIT_SIZE = 768
BATTLE_SIZE = 128
ICON_SIZE = 128


@dataclass(frozen=True)
class Palette:
    line: str
    bg_top: str
    bg_bottom: str
    haze: str
    paper: str
    primary: str
    secondary: str
    accent: str
    metal: str
    skin: str
    hair: str


@dataclass(frozen=True)
class CharacterSpec:
    base_key: str
    category: str
    faction: str
    role: str
    palette: Palette
    weapon: str
    headwear: str
    beard: str
    shoulder_style: str
    hair_shape: str = "tapered"
    face_mark: str = "none"
    weapon_side: str = "right"


@dataclass(frozen=True)
class WeaponSpec:
    key: str
    palette: Palette
    weapon: str
    ornament: str = "ring"


PLAYER_PALETTE = Palette(
    line="#1B1511",
    bg_top="#12171D",
    bg_bottom="#283329",
    haze="#EDE8DE",
    paper="#F0E8DA",
    primary="#4E6B52",
    secondary="#334A62",
    accent="#C59A49",
    metal="#D8C49A",
    skin="#E0BE9A",
    hair="#1A1818",
)

LIU_BEI_PALETTE = Palette(
    line="#181411",
    bg_top="#151A1D",
    bg_bottom="#354532",
    haze="#EFE8D9",
    paper="#F1E8D5",
    primary="#5A7146",
    secondary="#2E4654",
    accent="#D1A357",
    metal="#DAB88B",
    skin="#E5C4A2",
    hair="#1E1B18",
)

GUAN_YU_PALETTE = Palette(
    line="#181311",
    bg_top="#131A1A",
    bg_bottom="#1F3C30",
    haze="#ECE6D6",
    paper="#EDE5D7",
    primary="#1E5E43",
    secondary="#143026",
    accent="#D5AF57",
    metal="#CDBA90",
    skin="#DEB691",
    hair="#251614",
)

ZHANG_FEI_PALETTE = Palette(
    line="#190F10",
    bg_top="#17151F",
    bg_bottom="#3D2630",
    haze="#E7DED6",
    paper="#EFE6D8",
    primary="#3C3650",
    secondary="#271E25",
    accent="#B86148",
    metal="#C9A17C",
    skin="#D9A67D",
    hair="#1A1414",
)

HUANG_ZHONG_PALETTE = Palette(
    line="#1A1410",
    bg_top="#191C1B",
    bg_bottom="#4E5535",
    haze="#EDE6D6",
    paper="#EFE4D3",
    primary="#776338",
    secondary="#304D3F",
    accent="#D8BC73",
    metal="#D6C39B",
    skin="#DDBA94",
    hair="#C9C2B8",
)

ZHUGE_PALETTE = Palette(
    line="#171514",
    bg_top="#15191D",
    bg_bottom="#2E423B",
    haze="#F0ECE2",
    paper="#F5EFE5",
    primary="#D9E0D3",
    secondary="#52675D",
    accent="#BFA66E",
    metal="#D7C9A9",
    skin="#E3C3A1",
    hair="#232321",
)

ZHAO_YUN_PALETTE = Palette(
    line="#171516",
    bg_top="#141A20",
    bg_bottom="#36556F",
    haze="#F4EFE6",
    paper="#F7F2E7",
    primary="#D6DBDC",
    secondary="#54728A",
    accent="#CBB47A",
    metal="#E1D9C6",
    skin="#E4C3A0",
    hair="#22201E",
)

MA_CHAO_PALETTE = Palette(
    line="#151417",
    bg_top="#151A20",
    bg_bottom="#304D63",
    haze="#F3ECDD",
    paper="#F4E8DA",
    primary="#E3DDCF",
    secondary="#3C5F76",
    accent="#D8B365",
    metal="#E1D0A8",
    skin="#E1BC93",
    hair="#1F1A18",
)

YELLOW_TURBAN_PALETTE = Palette(
    line="#1A130F",
    bg_top="#1E1714",
    bg_bottom="#54331E",
    haze="#F0E1C7",
    paper="#F3E3CD",
    primary="#A47D31",
    secondary="#5B3A1F",
    accent="#E1B85C",
    metal="#C39A63",
    skin="#D6A97F",
    hair="#2B1E18",
)

ZHANG_BAO_PALETTE = Palette(
    line="#19120F",
    bg_top="#231916",
    bg_bottom="#6A3C1D",
    haze="#F3E2C6",
    paper="#F5E3C7",
    primary="#B1872F",
    secondary="#6A3819",
    accent="#F1C55E",
    metal="#D9A566",
    skin="#D6A67E",
    hair="#2A1914",
)

ZHANG_LIANG_PALETTE = Palette(
    line="#190F0D",
    bg_top="#271815",
    bg_bottom="#6B2E20",
    haze="#F0DAC5",
    paper="#F4E0CF",
    primary="#A86E2B",
    secondary="#58271B",
    accent="#E6A35A",
    metal="#C38F5C",
    skin="#D2A077",
    hair="#211411",
)

WEI_PALETTE = Palette(
    line="#14161A",
    bg_top="#12151A",
    bg_bottom="#293746",
    haze="#ECE8E0",
    paper="#F0ECE5",
    primary="#50647A",
    secondary="#273241",
    accent="#CBA46A",
    metal="#C8D0D7",
    skin="#DDB691",
    hair="#1E1D1D",
)

XIAHOU_DUN_PALETTE = Palette(
    line="#141519",
    bg_top="#15161A",
    bg_bottom="#3A3648",
    haze="#E8E3DC",
    paper="#F0E9E0",
    primary="#5B6289",
    secondary="#252734",
    accent="#C77B47",
    metal="#D4D4D8",
    skin="#D8AE89",
    hair="#201818",
)

XIAHOU_YUAN_PALETTE = Palette(
    line="#14171A",
    bg_top="#12171D",
    bg_bottom="#335067",
    haze="#ECE8E0",
    paper="#F0ECE5",
    primary="#496781",
    secondary="#263444",
    accent="#C78D4E",
    metal="#D0D5DB",
    skin="#DBB18C",
    hair="#1F1D1B",
)

PURSUIT_PALETTE = Palette(
    line="#14161A",
    bg_top="#12161A",
    bg_bottom="#33465A",
    haze="#ECE7DE",
    paper="#F1EBE3",
    primary="#4A5D71",
    secondary="#202B38",
    accent="#D09658",
    metal="#D2D8DE",
    skin="#DDB48D",
    hair="#1F1A19",
)

JIANGXIA_GUARD_PALETTE = Palette(
    line="#151718",
    bg_top="#11161B",
    bg_bottom="#35505B",
    haze="#E9ECE4",
    paper="#F0ECE3",
    primary="#60756A",
    secondary="#355767",
    accent="#C8A66A",
    metal="#CBD3D9",
    skin="#DCB38D",
    hair="#1E1B19",
)

JIANGXIA_BOW_PALETTE = Palette(
    line="#14171A",
    bg_top="#111820",
    bg_bottom="#33576C",
    haze="#E9ECE7",
    paper="#F2EEE5",
    primary="#4F6B6A",
    secondary="#2E4557",
    accent="#D6BB7D",
    metal="#D2D9DD",
    skin="#DEB48B",
    hair="#1F1C1A",
)

JIANGXIA_COMMAND_PALETTE = Palette(
    line="#131619",
    bg_top="#121821",
    bg_bottom="#2C5164",
    haze="#ECEDE7",
    paper="#F2EEE5",
    primary="#415F69",
    secondary="#22384C",
    accent="#D1A15E",
    metal="#D7DBDE",
    skin="#DDB189",
    hair="#1D1A18",
)

JIANGXIA_RIDER_PALETTE = Palette(
    line="#14171A",
    bg_top="#10161B",
    bg_bottom="#2B4C5E",
    haze="#ECECE3",
    paper="#F1EBDD",
    primary="#6A7E70",
    secondary="#395A6C",
    accent="#D5B06B",
    metal="#D5D9DC",
    skin="#DDB28A",
    hair="#1D1A19",
)

LUOCHENG_GUARD_PALETTE = Palette(
    line="#171311",
    bg_top="#191512",
    bg_bottom="#5A4234",
    haze="#EDE3D7",
    paper="#F3E8DA",
    primary="#6C5A48",
    secondary="#3C4A52",
    accent="#D2A367",
    metal="#C8C0B5",
    skin="#D9AE86",
    hair="#211917",
)

LUOCHENG_WALL_PALETTE = Palette(
    line="#171414",
    bg_top="#181516",
    bg_bottom="#54494A",
    haze="#ECE3D8",
    paper="#F2E9DD",
    primary="#5A665F",
    secondary="#485868",
    accent="#D6B177",
    metal="#CED1D1",
    skin="#D9AE86",
    hair="#211B19",
)

LUOCHENG_COMMAND_PALETTE = Palette(
    line="#171210",
    bg_top="#191412",
    bg_bottom="#603D2F",
    haze="#ECE0D5",
    paper="#F1E5D9",
    primary="#6B5043",
    secondary="#2D3946",
    accent="#D08C55",
    metal="#D4CAC1",
    skin="#DAAE86",
    hair="#201816",
)


CHARACTER_SPECS = {
    "player-liu-bei": CharacterSpec("player-liu-bei", "Heroes", "player", "Commander", LIU_BEI_PALETTE, "sword", "crown", "short", "robe", hair_shape="tapered", weapon_side="left"),
    "player-guan-yu": CharacterSpec("player-guan-yu", "Heroes", "player", "Guardian", GUAN_YU_PALETTE, "glaive", "highcap", "long", "armor", hair_shape="heavy", weapon_side="right"),
    "player-zhang-fei": CharacterSpec("player-zhang-fei", "Heroes", "player", "Guardian", ZHANG_FEI_PALETTE, "spear", "warhelm", "full", "armor", hair_shape="wild", weapon_side="left"),
    "player-huang-zhong": CharacterSpec("player-huang-zhong", "Heroes", "player", "Ranger", HUANG_ZHONG_PALETTE, "bow", "eldercap", "elder", "cloak", hair_shape="trimmed", weapon_side="right"),
    "player-zhuge-liang": CharacterSpec("player-zhuge-liang", "Heroes", "player", "Commander", ZHUGE_PALETTE, "fan", "scholarcap", "none", "robe", hair_shape="tapered", weapon_side="right"),
    "player-zhao-yun": CharacterSpec("player-zhao-yun", "Heroes", "player", "Scout", ZHAO_YUN_PALETTE, "spear", "plumehelm", "none", "light", hair_shape="trimmed", weapon_side="right"),
    "player-ma-chao": CharacterSpec("player-ma-chao", "Heroes", "player", "Raider", MA_CHAO_PALETTE, "lance", "westernhelm", "none", "raider", hair_shape="wind", weapon_side="left"),
    "enemy-zhang-bao": CharacterSpec("enemy-zhang-bao", "Bosses", "enemy", "Commander", ZHANG_BAO_PALETTE, "seal", "talisman", "goatee", "robe", hair_shape="wild", weapon_side="right"),
    "enemy-zhang-liang": CharacterSpec("enemy-zhang-liang", "Bosses", "enemy", "Commander", ZHANG_LIANG_PALETTE, "blade", "rebelcrown", "mustache", "armor", hair_shape="heavy", weapon_side="left"),
    "enemy-jiangxia-bridge-captain": CharacterSpec("enemy-jiangxia-bridge-captain", "Enemies", "enemy", "Guardian", JIANGXIA_GUARD_PALETTE, "spear", "weihelm", "trim", "armor", hair_shape="trimmed", weapon_side="left"),
    "enemy-jiangxia-bow-chief": CharacterSpec("enemy-jiangxia-bow-chief", "Enemies", "enemy", "Ranger", JIANGXIA_BOW_PALETTE, "bow", "hawkhelm", "none", "cloak", hair_shape="trimmed", weapon_side="right"),
    "enemy-jiangxia-outer-warden-a": CharacterSpec("enemy-jiangxia-outer-warden-a", "Enemies", "enemy", "Guardian", JIANGXIA_GUARD_PALETTE, "spear", "lighthelm", "mustache", "armor", hair_shape="trimmed", weapon_side="left"),
    "enemy-jiangxia-ferry-captain": CharacterSpec("enemy-jiangxia-ferry-captain", "Bosses", "enemy", "Commander", JIANGXIA_COMMAND_PALETTE, "blade", "scarhelm", "trim", "armor", hair_shape="trimmed", weapon_side="right"),
    "enemy-jiangxia-river-rider": CharacterSpec("enemy-jiangxia-river-rider", "Enemies", "enemy", "Raider", JIANGXIA_RIDER_PALETTE, "lance", "westernhelm", "none", "raider", hair_shape="wind", weapon_side="left"),
    "enemy-luocheng-gate-captain": CharacterSpec("enemy-luocheng-gate-captain", "Enemies", "enemy", "Guardian", LUOCHENG_GUARD_PALETTE, "glaive", "weihelm", "trim", "armor", hair_shape="trimmed", weapon_side="left"),
    "enemy-luocheng-wall-bow": CharacterSpec("enemy-luocheng-wall-bow", "Enemies", "enemy", "Ranger", LUOCHENG_WALL_PALETTE, "bow", "hawkhelm", "none", "cloak", hair_shape="trimmed", weapon_side="right"),
    "enemy-luocheng-outer-guard": CharacterSpec("enemy-luocheng-outer-guard", "Enemies", "enemy", "Guardian", LUOCHENG_GUARD_PALETTE, "spear", "lighthelm", "short", "armor", hair_shape="trimmed", weapon_side="left"),
    "enemy-luocheng-commandant": CharacterSpec("enemy-luocheng-commandant", "Bosses", "enemy", "Commander", LUOCHENG_COMMAND_PALETTE, "blade", "weihelm", "mustache", "armor", hair_shape="heavy", weapon_side="right"),
    "enemy-luocheng-street-guard-a": CharacterSpec("enemy-luocheng-street-guard-a", "Enemies", "enemy", "Guardian", LUOCHENG_GUARD_PALETTE, "spear", "lighthelm", "none", "armor", hair_shape="trimmed", weapon_side="left"),
    "enemy-pursuit_commander": CharacterSpec("enemy-pursuit_commander", "Bosses", "enemy", "Commander", PURSUIT_PALETTE, "blade", "weihelm", "trim", "armor", hair_shape="trimmed", weapon_side="right"),
    "enemy-xiahou-dun": CharacterSpec("enemy-xiahou-dun", "Bosses", "enemy", "Commander", XIAHOU_DUN_PALETTE, "lance", "scarhelm", "trim", "armor", hair_shape="trimmed", face_mark="eyepatch", weapon_side="left"),
    "enemy-xiahou-yuan": CharacterSpec("enemy-xiahou-yuan", "Bosses", "enemy", "Commander", XIAHOU_YUAN_PALETTE, "bow", "hawkhelm", "none", "cloak", hair_shape="trimmed", weapon_side="right"),
    "archetype-yellow-turban-raider": CharacterSpec("archetype-yellow-turban-raider", "Enemies", "enemy", "Raider", YELLOW_TURBAN_PALETTE, "spear", "bandana", "mustache", "raider", hair_shape="wild", weapon_side="left"),
    "archetype-yellow-turban-ranger": CharacterSpec("archetype-yellow-turban-ranger", "Enemies", "enemy", "Ranger", YELLOW_TURBAN_PALETTE, "bow", "bandana", "none", "cloak", hair_shape="trimmed", weapon_side="right"),
    "archetype-yellow-turban-guardian": CharacterSpec("archetype-yellow-turban-guardian", "Enemies", "enemy", "Guardian", YELLOW_TURBAN_PALETTE, "blade", "bandana", "full", "armor", hair_shape="heavy", weapon_side="right"),
    "archetype-wei-guardian": CharacterSpec("archetype-wei-guardian", "Enemies", "enemy", "Guardian", WEI_PALETTE, "spear", "weihelm", "none", "armor", hair_shape="trimmed", weapon_side="left"),
    "archetype-wei-ranger": CharacterSpec("archetype-wei-ranger", "Enemies", "enemy", "Ranger", WEI_PALETTE, "bow", "weihelm", "none", "cloak", hair_shape="trimmed", weapon_side="right"),
    "archetype-wei-raider": CharacterSpec("archetype-wei-raider", "Enemies", "enemy", "Raider", WEI_PALETTE, "lance", "lighthelm", "none", "raider", hair_shape="wind", weapon_side="left"),
    "archetype-wei-commandant": CharacterSpec("archetype-wei-commandant", "Enemies", "enemy", "Commander", WEI_PALETTE, "blade", "weihelm", "trim", "robe", hair_shape="trimmed", weapon_side="right"),
}


def hex_to_rgb(value: str) -> tuple[int, int, int]:
    value = value.lstrip("#")
    return int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16)


def rgb_to_hex(rgb: tuple[int, int, int]) -> str:
    return "#" + "".join(f"{max(0, min(255, component)):02X}" for component in rgb)


def mix(color_a: str, color_b: str, amount: float) -> str:
    a = hex_to_rgb(color_a)
    b = hex_to_rgb(color_b)
    return rgb_to_hex(tuple(round(a[index] * (1 - amount) + b[index] * amount) for index in range(3)))


def darken(color: str, amount: float) -> str:
    return mix(color, "#000000", amount)


def lighten(color: str, amount: float) -> str:
    return mix(color, "#FFFFFF", amount)


def alpha(fill: str, opacity: float) -> str:
    return f'fill="{fill}" fill-opacity="{opacity:.3f}"'


def path(points: Iterable[tuple[float, float]], close: bool = True) -> str:
    points = list(points)
    if not points:
        return ""

    commands = [f"M {points[0][0]:.1f} {points[0][1]:.1f}"]
    commands.extend(f"L {x:.1f} {y:.1f}" for x, y in points[1:])
    if close:
        commands.append("Z")
    return " ".join(commands)


def ensure_dir(path: Path) -> None:
    path.mkdir(parents=True, exist_ok=True)


def portrait_paths_for(spec: CharacterSpec) -> tuple[Path, Path]:
    svg_path = ART_SOURCE_ROOT / "Portraits" / spec.category / f"{spec.base_key}__portrait.svg"
    png_path = PORTRAIT_OUTPUT_ROOT / spec.category / f"{spec.base_key}__portrait.png"
    return svg_path, png_path


def battle_paths_for(spec: CharacterSpec) -> tuple[Path, Path]:
    svg_path = ART_SOURCE_ROOT / "Battle" / spec.category / f"{spec.base_key}__battle.svg"
    png_path = BATTLE_OUTPUT_ROOT / spec.category / f"{spec.base_key}__battle.png"
    return svg_path, png_path


def icon_paths_for(icon_key: str) -> tuple[Path, Path]:
    svg_path = ART_SOURCE_ROOT / "Weapons" / f"{icon_key}.svg"
    png_path = WEAPON_OUTPUT_ROOT / f"{icon_key}.png"
    return svg_path, png_path


def render_portrait_svg(spec: CharacterSpec) -> str:
    rng = random.Random(spec.base_key)
    p = spec.palette
    paper_shadow = darken(p.paper, 0.12)
    robe_deep = darken(p.primary, 0.18)
    robe_light = lighten(p.primary, 0.18)
    metal_shadow = darken(p.metal, 0.2)
    accent_glow = lighten(p.accent, 0.15)
    bg_low = darken(p.bg_bottom, 0.05)
    sun_color = lighten(p.accent, 0.28)
    stroke = darken(p.line, 0.02)

    background_mountains = [
        f'<path d="{path([(0, 560), (140, 470), (280, 545), (420, 430), (550, 512), (768, 410), (768, 768), (0, 768)])}" {alpha(darken(bg_low, 0.05), 0.92)} />',
        f'<path d="{path([(0, 620), (120, 540), (235, 610), (380, 500), (540, 620), (690, 565), (768, 600), (768, 768), (0, 768)])}" {alpha(mix(bg_low, p.primary, 0.22), 0.58)} />',
    ]

    mist_layers = []
    for index in range(7):
        cx = 80 + index * 102 + rng.randint(-18, 18)
        cy = 430 + rng.randint(-28, 32)
        rx = 120 + rng.randint(-16, 34)
        ry = 44 + rng.randint(-8, 10)
        mist_layers.append(
            f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" {alpha(p.haze, 0.06 + index * 0.01)} />')

    splashes = []
    for _ in range(10):
        cx = rng.randint(55, 710)
        cy = rng.randint(40, 680)
        rx = rng.randint(26, 95)
        ry = rng.randint(12, 40)
        rotation = rng.randint(-48, 48)
        fill = mix(p.accent, p.primary, rng.random() * 0.55)
        splashes.append(
            f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" transform="rotate({rotation} {cx} {cy})" {alpha(fill, 0.045)} />')

    weapon = render_portrait_weapon(spec)
    shoulders = render_portrait_shoulders(spec, robe_light, robe_deep, metal_shadow, stroke)
    head = render_portrait_head(spec, stroke)
    ornaments = render_portrait_ornaments(spec, stroke, accent_glow)
    face_mark = render_face_mark(spec, stroke)
    corner_marks = render_corner_marks(p)

    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{PORTRAIT_SIZE}" height="{PORTRAIT_SIZE}" viewBox="0 0 {PORTRAIT_SIZE} {PORTRAIT_SIZE}">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="{p.bg_top}" />
      <stop offset="100%" stop-color="{bg_low}" />
    </linearGradient>
    <radialGradient id="sun" cx="50%" cy="45%" r="42%">
      <stop offset="0%" stop-color="{sun_color}" stop-opacity="0.52" />
      <stop offset="60%" stop-color="{p.accent}" stop-opacity="0.12" />
      <stop offset="100%" stop-color="{p.accent}" stop-opacity="0" />
    </radialGradient>
    <linearGradient id="robe" x1="0" y1="0" x2="0.9" y2="1">
      <stop offset="0%" stop-color="{robe_light}" />
      <stop offset="100%" stop-color="{robe_deep}" />
    </linearGradient>
    <linearGradient id="metal" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="{lighten(p.metal, 0.18)}" />
      <stop offset="100%" stop-color="{metal_shadow}" />
    </linearGradient>
    <linearGradient id="accent" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="{accent_glow}" />
      <stop offset="100%" stop-color="{darken(p.accent, 0.18)}" />
    </linearGradient>
  </defs>
  <rect width="768" height="768" fill="url(#bg)" />
  <rect width="768" height="768" {alpha(darken(p.bg_top, 0.28), 0.18)} />
  <circle cx="520" cy="178" r="194" fill="url(#sun)" />
  {''.join(splashes)}
  {''.join(background_mountains)}
  {''.join(mist_layers)}
  <path d="{path([(94, 602), (174, 556), (252, 590), (332, 540), (412, 584), (496, 534), (628, 578), (710, 522), (736, 548), (620, 640), (516, 616), (378, 662), (246, 628), (116, 654)])}" {alpha(p.paper, 0.07)} />
  {weapon}
  <ellipse cx="382" cy="366" rx="182" ry="230" {alpha(p.haze, 0.1)} />
  {shoulders}
  {head}
  {face_mark}
  {ornaments}
  {corner_marks}
  <rect x="22" y="22" width="724" height="724" rx="22" ry="22" fill="none" stroke="{mix(p.accent, p.paper, 0.45)}" stroke-opacity="0.34" stroke-width="2" />
  <rect x="36" y="36" width="696" height="696" rx="16" ry="16" fill="none" stroke="{paper_shadow}" stroke-opacity="0.14" stroke-width="1.2" />
</svg>
"""


def render_portrait_weapon(spec: CharacterSpec) -> str:
    x_flip = -1 if spec.weapon_side == "left" else 1
    tx = 530 if spec.weapon_side == "right" else 238
    p = spec.palette
    shaft = darken(p.secondary, 0.18)
    blade = lighten(p.metal, 0.08)
    accent = p.accent

    if spec.weapon in {"spear", "lance"}:
        return f"""
  <g transform="translate({tx} 118) rotate({18 * x_flip}) scale({x_flip} 1)">
    <rect x="-8" y="0" width="16" height="380" rx="8" fill="{shaft}" opacity="0.78" />
    <path d="{path([(-12, 8), (0, -70), (12, 8), (0, 46)])}" fill="{blade}" opacity="0.86" />
    <path d="{path([(-20, 80), (0, 46), (20, 80), (0, 98)])}" fill="{accent}" opacity="0.6" />
  </g>"""

    if spec.weapon == "glaive":
        return f"""
  <g transform="translate({tx} 102) rotate({14 * x_flip}) scale({x_flip} 1)">
    <rect x="-7" y="0" width="14" height="402" rx="7" fill="{shaft}" opacity="0.82" />
    <path d="M 2 -56 C 78 -42 92 24 50 76 C 22 110 -26 122 -40 96 C -18 86 4 56 2 16 Z" fill="{blade}" opacity="0.92" />
    <path d="{path([(-18, 72), (10, 54), (34, 94), (6, 112)])}" fill="{accent}" opacity="0.54" />
  </g>"""

    if spec.weapon == "bow":
        return f"""
  <g transform="translate({tx} 150) rotate({12 * x_flip}) scale({x_flip} 1)">
    <path d="M -8 8 C 84 40 92 260 -4 336" fill="none" stroke="{mix(p.metal, p.accent, 0.18)}" stroke-width="16" stroke-linecap="round" stroke-opacity="0.82" />
    <line x1="12" y1="12" x2="30" y2="320" stroke="{p.paper}" stroke-width="3" stroke-opacity="0.72" />
    <path d="{path([(26, 104), (88, 122), (30, 142)])}" fill="{p.accent}" opacity="0.65" />
  </g>"""

    if spec.weapon == "fan":
        return f"""
  <g transform="translate({tx} 296) rotate({-8 * x_flip}) scale({x_flip} 1)">
    <path d="M 0 0 C 82 -122 194 -122 256 8 C 172 66 88 92 0 0 Z" fill="{p.paper}" opacity="0.94" />
    <path d="M 14 2 C 84 -92 166 -92 220 8" fill="none" stroke="{p.accent}" stroke-width="10" stroke-opacity="0.52" />
    <path d="M 0 0 C 82 -122 194 -122 256 8" fill="none" stroke="{darken(p.secondary, 0.12)}" stroke-width="7" stroke-opacity="0.32" />
    <rect x="-10" y="-6" width="22" height="120" rx="8" fill="{darken(p.secondary, 0.14)}" opacity="0.72" />
  </g>"""

    if spec.weapon == "seal":
        return f"""
  <g transform="translate({tx} 210) rotate({10 * x_flip}) scale({x_flip} 1)">
    <rect x="-48" y="-20" width="94" height="112" rx="16" fill="{mix(p.accent, p.metal, 0.2)}" opacity="0.82" />
    <rect x="-16" y="-74" width="30" height="64" rx="12" fill="{darken(p.secondary, 0.14)}" opacity="0.8" />
    <path d="{path([(-18, 18), (18, 18), (18, 58), (-18, 58)])}" fill="{darken(p.secondary, 0.08)}" opacity="0.72" />
  </g>"""

    sword_blade = "M 0 -82 L 20 32 L 0 86 L -20 32 Z"
    if spec.weapon in {"blade", "sword"}:
        return f"""
  <g transform="translate({tx} 136) rotate({18 * x_flip}) scale({x_flip} 1)">
    <path d="{sword_blade}" fill="{blade}" opacity="0.92" />
    <rect x="-8" y="82" width="16" height="222" rx="8" fill="{shaft}" opacity="0.78" />
    <rect x="-42" y="86" width="84" height="16" rx="8" fill="{accent}" opacity="0.7" />
    <rect x="-12" y="102" width="24" height="30" rx="8" fill="{darken(accent, 0.18)}" opacity="0.7" />
  </g>"""

    return ""


def render_portrait_shoulders(spec: CharacterSpec, robe_light: str, robe_deep: str, metal_shadow: str, stroke: str) -> str:
    p = spec.palette
    shoulder_points = {
        "robe": [(178, 690), (236, 518), (304, 432), (462, 432), (534, 520), (590, 690)],
        "armor": [(146, 696), (212, 510), (300, 432), (462, 432), (550, 516), (622, 696)],
        "cloak": [(188, 696), (254, 540), (314, 430), (448, 430), (516, 536), (582, 696)],
        "light": [(196, 696), (264, 558), (322, 448), (438, 448), (500, 548), (568, 696)],
        "raider": [(164, 696), (226, 520), (290, 428), (468, 438), (558, 506), (614, 696)],
    }
    chest = shoulder_points.get(spec.shoulder_style, shoulder_points["robe"])
    chest_svg = [
        f'<path d="{path(chest)}" fill="url(#robe)" stroke="{stroke}" stroke-opacity="0.22" stroke-width="5" />',
        f'<path d="{path([(284, 704), (322, 464), (382, 430), (438, 464), (482, 704)])}" fill="{mix(p.secondary, robe_deep, 0.5)}" fill-opacity="0.66" />',
        f'<path d="{path([(344, 430), (378, 502), (416, 430), (438, 520), (378, 612), (320, 522)])}" fill="{lighten(p.secondary, 0.08)}" fill-opacity="0.72" />',
        f'<path d="{path([(326, 518), (378, 472), (430, 518), (410, 564), (348, 564)])}" fill="{p.paper}" fill-opacity="0.18" />',
    ]

    if spec.shoulder_style in {"armor", "raider"}:
        chest_svg.append(
            f'<path d="{path([(164, 600), (212, 504), (286, 522), (294, 604), (244, 648), (176, 640)])}" fill="{lighten(metal_shadow, 0.2)}" fill-opacity="0.64" />')
        chest_svg.append(
            f'<path d="{path([(594, 600), (544, 516), (472, 522), (462, 604), (516, 648), (584, 638)])}" fill="{lighten(metal_shadow, 0.2)}" fill-opacity="0.64" />')

    if spec.shoulder_style in {"cloak", "light"}:
        chest_svg.append(
            f'<path d="{path([(204, 696), (154, 560), (202, 444), (264, 466), (252, 624)])}" fill="{darken(p.secondary, 0.02)}" fill-opacity="0.48" />')

    if spec.shoulder_style == "raider":
        chest_svg.append(
            f'<path d="{path([(508, 432), (586, 512), (638, 680), (574, 696), (510, 566)])}" fill="{darken(p.primary, 0.16)}" fill-opacity="0.74" />')

    chest_svg.append(
        f'<path d="{path([(228, 682), (286, 602), (380, 584), (470, 604), (540, 682), (502, 698), (382, 650), (266, 700)])}" fill="{p.accent}" fill-opacity="0.12" />')
    return "\n  ".join(chest_svg)


def render_portrait_head(spec: CharacterSpec, stroke: str) -> str:
    p = spec.palette
    skin_shadow = darken(p.skin, 0.12)
    hair_dark = darken(p.hair, 0.12)
    beard_fill = mix(p.hair, p.secondary, 0.1)
    headwear = render_headwear(spec, stroke)
    hair = render_hair(spec, hair_dark)
    beard = render_beard(spec, beard_fill)

    face = [
        f'<ellipse cx="384" cy="272" rx="84" ry="100" fill="{p.skin}" />',
        f'<path d="{path([(300, 306), (330, 374), (382, 404), (444, 370), (470, 304), (430, 304), (422, 332), (390, 350), (354, 330), (344, 304)])}" fill="{skin_shadow}" fill-opacity="0.46" />',
        f'<ellipse cx="352" cy="274" rx="15" ry="10" fill="{stroke}" fill-opacity="0.76" />',
        f'<ellipse cx="416" cy="274" rx="15" ry="10" fill="{stroke}" fill-opacity="0.76" />',
        f'<path d="M 336 246 Q 354 232 372 244" fill="none" stroke="{stroke}" stroke-width="6" stroke-linecap="round" stroke-opacity="0.84" />',
        f'<path d="M 396 244 Q 414 232 432 246" fill="none" stroke="{stroke}" stroke-width="6" stroke-linecap="round" stroke-opacity="0.84" />',
        f'<path d="M 382 286 Q 372 318 386 330" fill="none" stroke="{darken(p.skin, 0.22)}" stroke-width="4" stroke-linecap="round" stroke-opacity="0.56" />',
        f'<path d="M 352 350 Q 384 364 416 350" fill="none" stroke="{darken(p.skin, 0.26)}" stroke-width="5" stroke-linecap="round" stroke-opacity="0.74" />',
        f'<ellipse cx="354" cy="286" rx="6" ry="4" fill="{lighten(p.paper, 0.1)}" fill-opacity="0.32" />',
        f'<ellipse cx="418" cy="286" rx="6" ry="4" fill="{lighten(p.paper, 0.1)}" fill-opacity="0.32" />',
    ]

    return "\n  ".join(face + [hair, beard, headwear])


def render_hair(spec: CharacterSpec, hair_dark: str) -> str:
    hair_shapes = {
        "tapered": f'<path d="{path([(300, 272), (306, 202), (338, 150), (388, 130), (440, 152), (464, 204), (470, 280), (444, 234), (392, 222), (330, 240)])}" fill="{hair_dark}" />',
        "heavy": f'<path d="{path([(292, 288), (304, 196), (346, 138), (398, 134), (448, 150), (478, 220), (474, 296), (440, 246), (396, 230), (334, 250)])}" fill="{hair_dark}" />',
        "wild": f'<path d="{path([(292, 294), (292, 204), (322, 134), (388, 122), (454, 150), (488, 222), (474, 302), (432, 248), (392, 230), (344, 248), (322, 292)])}" fill="{hair_dark}" />',
        "trimmed": f'<path d="{path([(306, 274), (312, 212), (350, 160), (392, 150), (438, 170), (460, 220), (458, 280), (436, 238), (392, 230), (344, 240)])}" fill="{hair_dark}" />',
        "wind": f'<path d="{path([(292, 286), (304, 206), (344, 150), (402, 142), (446, 162), (470, 216), (456, 270), (498, 260), (470, 324), (426, 292), (384, 232), (324, 252)])}" fill="{hair_dark}" />',
    }
    return hair_shapes.get(spec.hair_shape, hair_shapes["tapered"])


def render_beard(spec: CharacterSpec, beard_fill: str) -> str:
    if spec.beard == "none":
        return ""
    if spec.beard == "short":
        return f'<path d="{path([(354, 350), (384, 384), (414, 350), (404, 402), (382, 420), (362, 404)])}" fill="{beard_fill}" fill-opacity="0.84" />'
    if spec.beard == "trim":
        return f'<path d="{path([(360, 348), (384, 374), (406, 350), (404, 388), (382, 398), (360, 388)])}" fill="{beard_fill}" fill-opacity="0.78" />'
    if spec.beard == "mustache":
        return f'<path d="M 346 340 C 364 330 378 332 384 342 C 388 334 406 330 424 342 C 408 348 394 352 384 348 C 374 352 360 348 346 340 Z" fill="{beard_fill}" fill-opacity="0.84" />'
    if spec.beard == "goatee":
        return f'<path d="M 356 340 C 374 332 396 332 410 342 C 396 352 388 364 384 392 C 376 366 370 354 356 340 Z" fill="{beard_fill}" fill-opacity="0.88" />'
    if spec.beard == "full":
        return f'<path d="{path([(334, 340), (354, 414), (382, 474), (412, 412), (432, 340), (418, 486), (382, 548), (346, 486)])}" fill="{beard_fill}" fill-opacity="0.9" />'
    if spec.beard == "long":
        return f'<path d="{path([(342, 342), (360, 430), (382, 574), (406, 430), (424, 342), (418, 538), (382, 638), (346, 534)])}" fill="{beard_fill}" fill-opacity="0.9" />'
    if spec.beard == "elder":
        return f'<path d="{path([(344, 340), (356, 408), (374, 518), (394, 578), (412, 508), (422, 390), (410, 530), (384, 640), (356, 532)])}" fill="{beard_fill}" fill-opacity="0.82" />'
    return ""


def render_headwear(spec: CharacterSpec, stroke: str) -> str:
    p = spec.palette
    metal = lighten(p.metal, 0.06)
    secondary = lighten(p.secondary, 0.06)
    accent = lighten(p.accent, 0.05)

    if spec.headwear == "crown":
        return f'<path d="{path([(322, 184), (344, 120), (376, 156), (410, 104), (446, 182), (432, 214), (336, 214)])}" fill="{accent}" fill-opacity="0.86" stroke="{stroke}" stroke-width="4" stroke-opacity="0.24" />'
    if spec.headwear == "highcap":
        return f'<path d="{path([(338, 196), (354, 104), (392, 84), (426, 102), (440, 196), (422, 214), (354, 214)])}" fill="{secondary}" fill-opacity="0.88" />'
    if spec.headwear == "warhelm":
        return f'<path d="{path([(312, 224), (328, 164), (372, 126), (420, 128), (450, 166), (458, 226), (430, 212), (388, 204), (344, 210)])}" fill="{metal}" fill-opacity="0.9" />'
    if spec.headwear == "eldercap":
        return f'<path d="{path([(322, 214), (346, 144), (388, 124), (430, 142), (450, 214), (426, 232), (346, 232)])}" fill="{secondary}" fill-opacity="0.86" />'
    if spec.headwear == "scholarcap":
        return f'<path d="{path([(330, 208), (334, 148), (374, 126), (424, 130), (438, 184), (428, 212), (340, 212)])}" fill="{darken(p.secondary, 0.02)}" fill-opacity="0.92" /><path d="M 332 162 C 292 192 288 248 330 278" fill="none" stroke="{accent}" stroke-width="10" stroke-opacity="0.38" />'
    if spec.headwear == "plumehelm":
        return f'<path d="{path([(316, 222), (332, 162), (376, 130), (424, 132), (452, 170), (458, 226), (428, 214), (386, 206), (340, 214)])}" fill="{metal}" fill-opacity="0.9" /><path d="M 388 124 C 438 74 480 70 514 106" fill="none" stroke="{lighten(p.paper, 0.08)}" stroke-width="18" stroke-opacity="0.48" />'
    if spec.headwear == "westernhelm":
        return f'<path d="{path([(316, 226), (334, 166), (376, 124), (424, 126), (458, 168), (464, 226), (430, 214), (388, 208), (340, 216)])}" fill="{metal}" fill-opacity="0.9" /><path d="M 390 126 C 426 82 470 78 504 114" fill="none" stroke="{accent}" stroke-width="12" stroke-opacity="0.48" />'
    if spec.headwear == "talisman":
        return f'<path d="{path([(326, 214), (344, 138), (388, 108), (434, 138), (452, 214), (426, 226), (348, 226)])}" fill="{accent}" fill-opacity="0.82" /><rect x="374" y="90" width="24" height="62" rx="8" fill="{p.paper}" fill-opacity="0.74" />'
    if spec.headwear == "rebelcrown":
        return f'<path d="{path([(320, 208), (340, 142), (370, 124), (386, 162), (402, 118), (422, 162), (444, 132), (460, 208), (428, 220), (350, 220)])}" fill="{accent}" fill-opacity="0.76" />'
    if spec.headwear == "weihelm":
        return f'<path d="{path([(316, 218), (332, 160), (374, 126), (424, 126), (454, 164), (458, 220), (430, 214), (388, 206), (340, 210)])}" fill="{metal}" fill-opacity="0.9" />'
    if spec.headwear == "scarhelm":
        return f'<path d="{path([(314, 220), (332, 160), (372, 124), (426, 126), (456, 170), (458, 222), (430, 214), (388, 206), (338, 214)])}" fill="{metal}" fill-opacity="0.92" /><path d="M 420 132 C 454 94 488 92 514 122" fill="none" stroke="{accent}" stroke-width="10" stroke-opacity="0.42" />'
    if spec.headwear == "hawkhelm":
        return f'<path d="{path([(316, 220), (332, 162), (374, 128), (424, 128), (454, 166), (458, 224), (430, 214), (388, 208), (340, 214)])}" fill="{metal}" fill-opacity="0.9" /><path d="M 392 128 C 438 84 486 88 520 130" fill="none" stroke="{lighten(p.paper, 0.1)}" stroke-width="10" stroke-opacity="0.38" />'
    if spec.headwear == "bandana":
        return f'<path d="{path([(316, 224), (332, 172), (382, 150), (438, 174), (454, 224), (430, 242), (340, 242)])}" fill="{accent}" fill-opacity="0.78" />'
    if spec.headwear == "lighthelm":
        return f'<path d="{path([(322, 220), (340, 166), (380, 136), (422, 138), (448, 170), (450, 220), (424, 214), (382, 206), (344, 214)])}" fill="{metal}" fill-opacity="0.82" />'
    return ""


def render_face_mark(spec: CharacterSpec, stroke: str) -> str:
    if spec.face_mark != "eyepatch":
        return ""
    return f'<path d="{path([(394, 250), (442, 250), (442, 286), (392, 286)])}" fill="{stroke}" fill-opacity="0.78" /><line x1="374" y1="250" x2="462" y2="286" stroke="{stroke}" stroke-width="7" stroke-opacity="0.76" />'


def render_portrait_ornaments(spec: CharacterSpec, stroke: str, accent_glow: str) -> str:
    p = spec.palette
    return "\n  ".join(
        [
            f'<path d="{path([(202, 520), (254, 498), (286, 538), (232, 560)])}" fill="{p.accent}" fill-opacity="0.22" />',
            f'<path d="{path([(514, 512), (574, 488), (604, 532), (544, 556)])}" fill="{p.accent}" fill-opacity="0.22" />',
            f'<path d="M 220 628 C 300 650 468 650 550 628" fill="none" stroke="{accent_glow}" stroke-width="10" stroke-opacity="0.16" stroke-linecap="round" />',
            f'<path d="M 184 202 C 246 154 276 150 330 166" fill="none" stroke="{lighten(p.accent, 0.15)}" stroke-width="7" stroke-opacity="0.18" stroke-linecap="round" />',
            f'<path d="M 582 194 C 528 148 500 150 442 166" fill="none" stroke="{lighten(p.accent, 0.15)}" stroke-width="7" stroke-opacity="0.18" stroke-linecap="round" />',
            f'<circle cx="172" cy="612" r="7" fill="{p.accent}" fill-opacity="0.45" />',
            f'<circle cx="596" cy="612" r="7" fill="{p.accent}" fill-opacity="0.45" />',
        ]
    )


def render_corner_marks(p: Palette) -> str:
    mark = lighten(p.accent, 0.14)
    shadow = darken(p.secondary, 0.1)
    corners = []
    for x, y, flip_x, flip_y in ((82, 82, 1, 1), (686, 82, -1, 1), (82, 686, 1, -1), (686, 686, -1, -1)):
        corners.append(
            f'<g transform="translate({x} {y}) scale({flip_x} {flip_y})">'
            f'<path d="M 0 18 C 16 6 34 0 62 0 L 62 12 C 42 12 26 18 12 30 L 0 18 Z" fill="{mark}" fill-opacity="0.52" />'
            f'<path d="M 18 0 C 6 16 0 34 0 62 L 12 62 C 12 42 18 26 30 12 L 18 0 Z" fill="{shadow}" fill-opacity="0.24" />'
            f'</g>'
        )
    return "\n  ".join(corners)


def render_battle_svg(spec: CharacterSpec) -> str:
    p = spec.palette
    body = darken(p.primary, 0.06)
    body_light = lighten(p.primary, 0.18)
    metal = p.metal
    shadow = darken(p.line, 0.02)
    accent = p.accent
    hair = darken(p.hair, 0.08)
    skin = p.skin

    body_shapes = {
        "Commander": [(34, 122), (40, 76), (48, 48), (64, 32), (80, 46), (90, 78), (96, 122), (72, 116), (64, 90), (56, 116)],
        "Guardian": [(28, 122), (34, 70), (44, 42), (64, 24), (86, 40), (96, 70), (102, 122), (76, 120), (64, 84), (52, 120)],
        "Ranger": [(38, 122), (42, 76), (52, 42), (64, 28), (78, 42), (88, 80), (92, 122), (72, 118), (64, 90), (56, 118)],
        "Scout": [(38, 122), (40, 80), (50, 46), (64, 28), (78, 44), (88, 76), (92, 122), (72, 118), (64, 86), (56, 118)],
        "Raider": [(34, 122), (40, 72), (52, 42), (64, 26), (84, 40), (96, 68), (104, 122), (76, 116), (66, 82), (50, 116)],
    }
    body_path = path(body_shapes.get(spec.role, body_shapes["Commander"]))
    sash = path([(48, 70), (62, 66), (82, 72), (74, 84), (54, 82)])
    glow = mix(accent, p.paper, 0.38)

    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{BATTLE_SIZE}" height="{BATTLE_SIZE}" viewBox="0 0 {BATTLE_SIZE} {BATTLE_SIZE}">
  <defs>
    <linearGradient id="body" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="{body_light}" />
      <stop offset="100%" stop-color="{body}" />
    </linearGradient>
    <linearGradient id="metal" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="{lighten(metal, 0.15)}" />
      <stop offset="100%" stop-color="{darken(metal, 0.22)}" />
    </linearGradient>
  </defs>
  <circle cx="64" cy="60" r="40" fill="{glow}" fill-opacity="0.08" />
  {render_battle_weapon(spec)}
  <path d="{body_path}" fill="url(#body)" stroke="{shadow}" stroke-width="3" stroke-opacity="0.14" />
  <path d="{sash}" fill="{accent}" fill-opacity="0.42" />
  {render_battle_armor(spec)}
  <circle cx="64" cy="34" r="15" fill="{skin}" />
  <path d="{path([(49, 39), (52, 24), (60, 16), (68, 15), (76, 18), (80, 28), (79, 40), (72, 33), (62, 30), (54, 34)])}" fill="{hair}" />
  {render_battle_headwear(spec)}
  {render_battle_beard(spec)}
  <path d="M 58 35 Q 64 38 70 35" fill="none" stroke="{shadow}" stroke-width="2.3" stroke-linecap="round" stroke-opacity="0.55" />
  <path d="M 56 29 Q 60 26 64 28" fill="none" stroke="{shadow}" stroke-width="2" stroke-linecap="round" stroke-opacity="0.65" />
  <path d="M 64 28 Q 68 26 72 29" fill="none" stroke="{shadow}" stroke-width="2" stroke-linecap="round" stroke-opacity="0.65" />
  {render_battle_face_mark(spec)}
</svg>
"""


def render_battle_weapon(spec: CharacterSpec) -> str:
    p = spec.palette
    shaft = darken(p.secondary, 0.18)
    blade = lighten(p.metal, 0.08)
    accent = p.accent
    flip = -1 if spec.weapon_side == "left" else 1
    tx = 92 if spec.weapon_side == "right" else 36
    if spec.weapon in {"spear", "lance"}:
        return f'<g transform="translate({tx} 6) rotate({16 * flip}) scale({flip} 1)"><rect x="-3" y="12" width="6" height="106" rx="3" fill="{shaft}" opacity="0.84" /><path d="{path([(-5, 10), (0, -10), (5, 10), (0, 18)])}" fill="{blade}" opacity="0.92" /><path d="{path([(-9, 26), (0, 18), (9, 26), (0, 32)])}" fill="{accent}" opacity="0.55" /></g>'
    if spec.weapon == "glaive":
        return f'<g transform="translate({tx} 6) rotate({14 * flip}) scale({flip} 1)"><rect x="-3" y="16" width="6" height="102" rx="3" fill="{shaft}" opacity="0.86" /><path d="M 0 0 C 18 2 28 18 20 34 C 14 44 0 52 -8 44 C 0 38 4 24 0 14 Z" fill="{blade}" opacity="0.92" /></g>'
    if spec.weapon == "bow":
        return f'<g transform="translate({tx} 12) rotate({12 * flip}) scale({flip} 1)"><path d="M -4 6 C 20 18 24 74 -4 102" fill="none" stroke="{mix(blade, accent, 0.22)}" stroke-width="6" stroke-linecap="round" stroke-opacity="0.82" /><line x1="2" y1="10" x2="8" y2="96" stroke="{p.paper}" stroke-width="1.8" stroke-opacity="0.78" /></g>'
    if spec.weapon == "fan":
        return f'<g transform="translate({tx} 58) rotate({-10 * flip}) scale({flip} 1)"><path d="M 0 0 C 18 -18 40 -18 58 0 C 40 12 18 14 0 0 Z" fill="{p.paper}" opacity="0.92" /><rect x="-3" y="-3" width="6" height="30" rx="3" fill="{shaft}" opacity="0.72" /></g>'
    if spec.weapon == "seal":
        return f'<g transform="translate({tx} 48) rotate({10 * flip}) scale({flip} 1)"><rect x="-10" y="-8" width="20" height="24" rx="4" fill="{lighten(accent, 0.08)}" opacity="0.82" /><rect x="-4" y="-20" width="8" height="14" rx="3" fill="{shaft}" opacity="0.8" /></g>'
    return f'<g transform="translate({tx} 8) rotate({16 * flip}) scale({flip} 1)"><path d="{path([(0, -8), (5, 12), (0, 28), (-5, 12)])}" fill="{blade}" opacity="0.92" /><rect x="-3" y="24" width="6" height="94" rx="3" fill="{shaft}" opacity="0.82" /><rect x="-12" y="26" width="24" height="5" rx="2" fill="{accent}" opacity="0.62" /></g>'


def render_battle_armor(spec: CharacterSpec) -> str:
    p = spec.palette
    accent = lighten(p.accent, 0.08)
    metal = lighten(p.metal, 0.06)
    if spec.role == "Guardian":
        return f'<path d="{path([(36, 60), (44, 48), (62, 50), (62, 72), (48, 82), (34, 74)])}" fill="{metal}" fill-opacity="0.64" /><path d="{path([(92, 60), (84, 48), (66, 50), (66, 72), (80, 82), (94, 74)])}" fill="{metal}" fill-opacity="0.64" />'
    if spec.role in {"Commander", "Raider"}:
        return f'<path d="{path([(50, 52), (64, 48), (78, 52), (74, 66), (54, 66)])}" fill="{accent}" fill-opacity="0.36" />'
    if spec.role in {"Ranger", "Scout"}:
        return f'<path d="{path([(42, 76), (34, 56), (40, 38), (52, 44), (50, 72)])}" fill="{darken(p.secondary, 0.02)}" fill-opacity="0.34" />'
    return ""


def render_battle_headwear(spec: CharacterSpec) -> str:
    p = spec.palette
    accent = lighten(p.accent, 0.05)
    metal = lighten(p.metal, 0.08)
    secondary = lighten(p.secondary, 0.04)
    mapping = {
        "crown": f'<path d="{path([(52, 22), (58, 8), (64, 18), (70, 6), (76, 22), (72, 26), (56, 26)])}" fill="{accent}" fill-opacity="0.86" />',
        "highcap": f'<path d="{path([(56, 24), (58, 8), (64, 4), (72, 10), (74, 24), (68, 26), (58, 26)])}" fill="{secondary}" fill-opacity="0.88" />',
        "warhelm": f'<path d="{path([(49, 24), (54, 14), (64, 8), (76, 14), (79, 24), (72, 24), (64, 22), (54, 24)])}" fill="{metal}" fill-opacity="0.92" />',
        "eldercap": f'<path d="{path([(52, 24), (56, 14), (64, 10), (74, 14), (76, 24), (70, 26), (56, 26)])}" fill="{secondary}" fill-opacity="0.84" />',
        "scholarcap": f'<path d="{path([(54, 24), (56, 14), (64, 10), (72, 12), (75, 22), (72, 24), (56, 24)])}" fill="{secondary}" fill-opacity="0.94" />',
        "plumehelm": f'<path d="{path([(50, 24), (54, 14), (64, 8), (74, 12), (78, 24), (70, 24), (64, 22), (54, 24)])}" fill="{metal}" fill-opacity="0.9" /><path d="M 64 8 C 74 0 84 0 92 10" fill="none" stroke="{lighten(p.paper, 0.08)}" stroke-width="4" stroke-opacity="0.48" />',
        "westernhelm": f'<path d="{path([(50, 24), (54, 14), (64, 8), (74, 10), (80, 22), (72, 24), (64, 22), (54, 24)])}" fill="{metal}" fill-opacity="0.92" /><path d="M 66 8 C 76 0 86 2 94 12" fill="none" stroke="{accent}" stroke-width="3.6" stroke-opacity="0.48" />',
        "talisman": f'<path d="{path([(52, 24), (56, 12), (64, 6), (72, 12), (76, 24), (70, 26), (56, 26)])}" fill="{accent}" fill-opacity="0.84" />',
        "rebelcrown": f'<path d="{path([(50, 22), (56, 14), (60, 18), (64, 10), (68, 18), (74, 14), (80, 22), (72, 26), (56, 26)])}" fill="{accent}" fill-opacity="0.78" />',
        "weihelm": f'<path d="{path([(50, 24), (54, 14), (64, 8), (74, 12), (78, 24), (70, 24), (64, 22), (54, 24)])}" fill="{metal}" fill-opacity="0.9" />',
        "scarhelm": f'<path d="{path([(50, 24), (54, 14), (64, 8), (74, 12), (80, 24), (70, 24), (64, 22), (54, 24)])}" fill="{metal}" fill-opacity="0.92" />',
        "hawkhelm": f'<path d="{path([(50, 24), (54, 14), (64, 8), (74, 12), (80, 24), (72, 24), (64, 22), (54, 24)])}" fill="{metal}" fill-opacity="0.9" /><path d="M 66 8 C 78 2 88 4 96 16" fill="none" stroke="{lighten(p.paper, 0.08)}" stroke-width="2.8" stroke-opacity="0.44" />',
        "bandana": f'<path d="{path([(50, 24), (56, 18), (64, 16), (74, 18), (80, 24), (74, 28), (54, 28)])}" fill="{accent}" fill-opacity="0.78" />',
        "lighthelm": f'<path d="{path([(52, 24), (56, 16), (64, 10), (72, 14), (76, 24), (70, 24), (64, 22), (56, 24)])}" fill="{metal}" fill-opacity="0.82" />',
    }
    return mapping.get(spec.headwear, "")


def render_battle_beard(spec: CharacterSpec) -> str:
    fill = darken(spec.palette.hair, 0.08)
    if spec.beard == "none":
        return ""
    if spec.beard in {"short", "trim"}:
        return f'<path d="{path([(58, 42), (64, 48), (70, 42), (68, 54), (64, 58), (60, 54)])}" fill="{fill}" fill-opacity="0.78" />'
    if spec.beard in {"mustache", "goatee"}:
        return f'<path d="{path([(56, 40), (64, 44), (72, 40), (64, 48)])}" fill="{fill}" fill-opacity="0.82" />'
    if spec.beard == "full":
        return f'<path d="{path([(54, 40), (58, 56), (64, 70), (70, 56), (74, 40), (70, 74), (64, 88), (58, 74)])}" fill="{fill}" fill-opacity="0.88" />'
    if spec.beard == "long":
        return f'<path d="{path([(56, 40), (60, 60), (64, 86), (68, 60), (72, 40), (70, 84), (64, 102), (58, 84)])}" fill="{fill}" fill-opacity="0.9" />'
    if spec.beard == "elder":
        return f'<path d="{path([(56, 40), (60, 58), (64, 80), (68, 58), (72, 40), (70, 76), (64, 96), (58, 76)])}" fill="{fill}" fill-opacity="0.8" />'
    return ""


def render_battle_face_mark(spec: CharacterSpec) -> str:
    if spec.face_mark != "eyepatch":
        return ""
    stroke = darken(spec.palette.line, 0.02)
    return f'<rect x="66" y="26" width="10" height="8" rx="2" fill="{stroke}" fill-opacity="0.82" /><line x1="56" y1="26" x2="78" y2="34" stroke="{stroke}" stroke-width="2" stroke-opacity="0.78" />'


def render_icon_svg(spec: WeaponSpec) -> str:
    p = spec.palette
    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{ICON_SIZE}" height="{ICON_SIZE}" viewBox="0 0 {ICON_SIZE} {ICON_SIZE}">
  <defs>
    <radialGradient id="glow" cx="50%" cy="40%" r="56%">
      <stop offset="0%" stop-color="{lighten(p.accent, 0.16)}" stop-opacity="0.62" />
      <stop offset="72%" stop-color="{p.accent}" stop-opacity="0.14" />
      <stop offset="100%" stop-color="{p.accent}" stop-opacity="0" />
    </radialGradient>
    <linearGradient id="disk" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="{darken(p.bg_top, 0.02)}" />
      <stop offset="100%" stop-color="{darken(p.bg_bottom, 0.08)}" />
    </linearGradient>
  </defs>
  <circle cx="64" cy="64" r="54" fill="url(#disk)" />
  <circle cx="64" cy="64" r="52" fill="url(#glow)" />
  <circle cx="64" cy="64" r="49" fill="none" stroke="{mix(p.accent, p.paper, 0.28)}" stroke-opacity="0.44" stroke-width="2" />
  <circle cx="64" cy="64" r="38" fill="none" stroke="{mix(p.secondary, p.paper, 0.16)}" stroke-opacity="0.18" stroke-width="8" />
  {render_icon_weapon(spec)}
  <path d="M 26 96 C 40 106 88 106 102 96" fill="none" stroke="{p.paper}" stroke-width="3" stroke-opacity="0.14" stroke-linecap="round" />
</svg>
"""


def render_icon_weapon(spec: WeaponSpec) -> str:
    p = spec.palette
    shaft = darken(p.secondary, 0.16)
    blade = lighten(p.metal, 0.1)
    accent = lighten(p.accent, 0.06)
    weapon = spec.weapon
    if weapon in {"spear", "lance"}:
        return f'<g transform="translate(66 18) rotate(28)"><rect x="-4" y="8" width="8" height="86" rx="4" fill="{shaft}" opacity="0.86" /><path d="{path([(-8, 8), (0, -16), (8, 8), (0, 18)])}" fill="{blade}" opacity="0.96" /><path d="{path([(-12, 30), (0, 18), (12, 30), (0, 36)])}" fill="{accent}" opacity="0.58" /></g>'
    if weapon == "glaive":
        return f'<g transform="translate(66 18) rotate(24)"><rect x="-4" y="12" width="8" height="82" rx="4" fill="{shaft}" opacity="0.86" /><path d="M 0 -8 C 20 -6 28 12 20 28 C 14 38 0 44 -8 36 C 0 30 4 18 0 8 Z" fill="{blade}" opacity="0.96" /></g>'
    if weapon == "bow":
        return f'<g transform="translate(64 64) rotate(-18)"><path d="M -8 -30 C 28 -18 34 18 -8 54" fill="none" stroke="{mix(blade, accent, 0.22)}" stroke-width="10" stroke-linecap="round" stroke-opacity="0.88" /><line x1="4" y1="-26" x2="10" y2="48" stroke="{p.paper}" stroke-width="2" stroke-opacity="0.82" /></g>'
    if weapon == "fan":
        return f'<g transform="translate(48 72) rotate(-14)"><path d="M 0 0 C 18 -26 46 -28 68 0 C 46 18 18 18 0 0 Z" fill="{p.paper}" opacity="0.94" /><path d="M 8 2 C 22 -14 44 -16 58 2" fill="none" stroke="{accent}" stroke-width="4" stroke-opacity="0.5" /><rect x="-4" y="-2" width="8" height="26" rx="4" fill="{shaft}" opacity="0.8" /></g>'
    if weapon == "seal":
        return f'<g transform="translate(64 58)"><rect x="-16" y="-10" width="32" height="36" rx="8" fill="{mix(accent, blade, 0.18)}" opacity="0.9" /><rect x="-6" y="-28" width="12" height="18" rx="4" fill="{shaft}" opacity="0.82" /></g>'
    return f'<g transform="translate(66 18) rotate(22)"><path d="{path([(0, -12), (8, 12), (0, 34), (-8, 12)])}" fill="{blade}" opacity="0.96" /><rect x="-4" y="30" width="8" height="64" rx="4" fill="{shaft}" opacity="0.86" /><rect x="-14" y="32" width="28" height="6" rx="3" fill="{accent}" opacity="0.66" /></g>'


def convert_svg_to_png(svg_path: Path, png_path: Path, size: int) -> None:
    ensure_dir(svg_path.parent)
    ensure_dir(png_path.parent)
    subprocess.run(
        [RSVG_CONVERT, "-w", str(size), "-h", str(size), "-o", str(png_path), str(svg_path)],
        check=True,
    )


def derive_weapon_spec(key: str) -> WeaponSpec:
    lower = key.lower()
    if "fan" in lower:
        weapon = "fan"
    elif "seal" in lower:
        weapon = "seal"
    elif "glaive" in lower or "halberd" in lower or "polearm" in lower:
        weapon = "glaive"
    elif "bow" in lower:
        weapon = "bow"
    elif "spear" in lower or "lance" in lower:
        weapon = "spear"
    else:
        weapon = "blade"

    if "yellow-turban" in lower:
        palette = YELLOW_TURBAN_PALETTE
    elif "white-dragon" in lower:
        palette = ZHAO_YUN_PALETTE
    elif "western-lance" in lower:
        palette = MA_CHAO_PALETTE
    elif "wind-feather-fan" in lower:
        palette = ZHUGE_PALETTE
    elif "vermilion-jian" in lower:
        palette = LIU_BEI_PALETTE
    elif "iron-crescent-glaive" in lower:
        palette = GUAN_YU_PALETTE
    elif "featherback-war-bow" in lower:
        palette = HUANG_ZHONG_PALETTE
    elif "tiger" in lower or "wei" in lower or "pursuit" in lower or "command" in lower or "river" in lower or "fort" in lower or "gate" in lower:
        palette = WEI_PALETTE
    else:
        palette = PLAYER_PALETTE
    return WeaponSpec(key=key, palette=palette, weapon=weapon)


def load_asset_keys() -> tuple[dict[str, CharacterSpec], list[WeaponSpec]]:
    with MANIFEST_PATH.open(newline="") as manifest_file:
        rows = list(csv.DictReader(manifest_file))

    character_keys = sorted(
        {
            row["portrait_asset_key"].replace("__portrait", "")
            for row in rows
            if row.get("portrait_asset_key")
        }
        |
        {
            row["battle_asset_key"].replace("__battle", "")
            for row in rows
            if row.get("battle_asset_key")
        }
    )
    missing_character_keys = [key for key in character_keys if key not in CHARACTER_SPECS]
    if missing_character_keys:
        raise KeyError(f"Missing character specs for: {', '.join(missing_character_keys)}")

    character_specs = {key: CHARACTER_SPECS[key] for key in character_keys}

    weapon_keys = sorted({row["weapon_icon_key"] for row in rows if row.get("weapon_icon_key")})
    weapon_specs = [derive_weapon_spec(key) for key in weapon_keys]
    return character_specs, weapon_specs


def generate_character_assets() -> None:
    character_specs, weapon_specs = load_asset_keys()
    ensure_dir(ART_SOURCE_ROOT / "Portraits")
    ensure_dir(ART_SOURCE_ROOT / "Weapons")

    portrait_count = 0
    for spec in character_specs.values():
        portrait_svg, portrait_png = portrait_paths_for(spec)
        ensure_dir(portrait_svg.parent)
        ensure_dir(portrait_png.parent)
        portrait_svg.write_text(render_portrait_svg(spec), encoding="utf-8")
        convert_svg_to_png(portrait_svg, portrait_png, PORTRAIT_SIZE)
        portrait_count += 1

    icon_count = 0
    for spec in weapon_specs:
        icon_svg, icon_png = icon_paths_for(spec.key)
        ensure_dir(icon_svg.parent)
        ensure_dir(icon_png.parent)
        icon_svg.write_text(render_icon_svg(spec), encoding="utf-8")
        convert_svg_to_png(icon_svg, icon_png, ICON_SIZE)
        icon_count += 1

    print(f"Generated portraits: {portrait_count}")
    print("Skipped battle sprites: use scripts/generate_srpg_battle_art.py for the formal SRPG battlefield style.")
    print(f"Generated weapon icons: {icon_count}")


if __name__ == "__main__":
    generate_character_assets()
