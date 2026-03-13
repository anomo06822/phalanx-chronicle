#!/usr/bin/env python3

from __future__ import annotations

import re
from dataclasses import dataclass
from pathlib import Path

from generate_character_art import RSVG_CONVERT, convert_svg_to_png, darken, ensure_dir, lighten, mix, path


ROOT = Path(__file__).resolve().parents[1]
ITEM_CATALOG_PATH = ROOT / "Assets" / "Scripts" / "Core" / "CampaignContentCatalog.cs"
ITEM_SOURCE_ROOT = ROOT / "Assets" / "ArtSource" / "Items" / "WarReportBadges"
ITEM_OUTPUT_ROOT = ROOT / "Assets" / "Art" / "UI" / "Items"
ITEM_RESOURCES_ROOT = ROOT / "Assets" / "Resources" / "ItemIcons"
ICON_SIZE = 256

ITEM_PATTERN = re.compile(
    r'\["(?P<item_id>[^"]+)"\]\s*=\s*new ItemDefinition\("(?P=item_id)",\s*ItemCategory\.(?P<category>\w+).*?(?P<treasure>isTreasure:\s*true)?',
)


@dataclass(frozen=True)
class ItemSpec:
    item_id: str
    category: str
    motif: str
    treasure: bool


MOTIFS = {
    "vermilion-jian": "jian",
    "iron-crescent-glaive": "glaive",
    "featherback-war-bow": "bow",
    "wind-feather-fan": "fan",
    "white-dragon-spear": "spear",
    "western-lance": "lance",
    "commander-travel-cloak": "cloak",
    "guardian-scale-vest": "vest",
    "ranger-hunt-coat": "coat",
    "strategist-robe": "robe",
    "scout-travel-mail": "mail",
    "raider-scale-vest": "harness",
    "tempered-jian": "jian",
    "commander-lamellar": "lamellar",
    "crescent-glaive": "glaive",
    "guardian-plate": "plate",
    "composite-bow": "bow",
    "ranger-coat": "coat",
    "dragon-rider-spear": "spear",
    "scout-war-cloak": "cloak",
    "storm-lance": "lance",
    "raider-war-harness": "harness",
    "field-horse": "horse",
    "swift-warhorse": "horse",
    "yellow-turban-signet": "signet",
    "bowang-fire-token": "token",
    "changban-scout-map": "map",
    "jiangxia-river-reins": "reins",
    "jiameng-oath-banner": "banner",
    "baishui-signal-spear": "spear",
    "mianzhu-feather-sigil": "sigil",
    "luocheng-breach-hammer": "hammer",
    "yangping-stone-route": "route",
    "tiandang-falcon-badge": "badge",
    "hanshui-command-seal": "seal",
    "dingjun-war-banner": "banner",
}

CATEGORY_COLORS = {
    "Weapon": ("#A84B3A", "#C59A49", "#EDE8DE"),
    "Armor": ("#6D7A87", "#C59A49", "#E6E1D6"),
    "Mount": ("#4E6B52", "#C59A49", "#E9E4D8"),
    "SpecialGood": ("#334A62", "#C59A49", "#E6E0D5"),
}


def load_items() -> list[ItemSpec]:
    specs: list[ItemSpec] = []
    for line in ITEM_CATALOG_PATH.read_text(encoding="utf-8").splitlines():
        match = ITEM_PATTERN.search(line)
        if not match:
            continue

        item_id = match.group("item_id")
        specs.append(
            ItemSpec(
                item_id=item_id,
                category=match.group("category"),
                motif=MOTIFS[item_id],
                treasure=bool(match.group("treasure")),
            )
        )

    missing = sorted(set(MOTIFS) - {spec.item_id for spec in specs})
    if missing:
        raise SystemExit(f"Missing item definitions for motifs: {', '.join(missing)}")

    return specs


def svg_path_for(item_id: str) -> Path:
    return ITEM_SOURCE_ROOT / f"{item_id}.svg"


def png_paths_for(item_id: str) -> tuple[Path, Path]:
    file_name = f"{item_id}__icon.png"
    return ITEM_OUTPUT_ROOT / file_name, ITEM_RESOURCES_ROOT / file_name


def render_icon_svg(spec: ItemSpec) -> str:
    accent, bronze, paper = CATEGORY_COLORS.get(spec.category, CATEGORY_COLORS["SpecialGood"])
    base_dark = "#111318"
    panel_dark = mix(base_dark, accent, 0.12)
    inner_ring = mix(bronze, paper, 0.26)
    motif_fill = paper if not spec.treasure else lighten(paper, 0.04)
    motif_shadow = darken(accent, 0.24)

    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{ICON_SIZE}" height="{ICON_SIZE}" viewBox="0 0 {ICON_SIZE} {ICON_SIZE}">
  <defs>
    <linearGradient id="panel" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="{lighten(panel_dark, 0.05)}" />
      <stop offset="100%" stop-color="{darken(base_dark, 0.04)}" />
    </linearGradient>
    <radialGradient id="glow" cx="50%" cy="38%" r="58%">
      <stop offset="0%" stop-color="{lighten(accent, 0.18)}" stop-opacity="0.45" />
      <stop offset="70%" stop-color="{accent}" stop-opacity="0.08" />
      <stop offset="100%" stop-color="{accent}" stop-opacity="0" />
    </radialGradient>
    <linearGradient id="bronze" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="{lighten(bronze, 0.18)}" />
      <stop offset="52%" stop-color="{bronze}" />
      <stop offset="100%" stop-color="{darken(bronze, 0.18)}" />
    </linearGradient>
  </defs>
  <path d="{path([(58, 22), (198, 22), (234, 58), (234, 198), (198, 234), (58, 234), (22, 198), (22, 58)])}" fill="{base_dark}" />
  <path d="{path([(62, 30), (194, 30), (226, 62), (226, 194), (194, 226), (62, 226), (30, 194), (30, 62)])}" fill="url(#bronze)" opacity="0.98" />
  <path d="{path([(72, 42), (184, 42), (214, 72), (214, 184), (184, 214), (72, 214), (42, 184), (42, 72)])}" fill="url(#panel)" />
  <circle cx="128" cy="128" r="76" fill="url(#glow)" />
  <circle cx="128" cy="128" r="66" fill="none" stroke="{inner_ring}" stroke-width="4" stroke-opacity="0.32" />
  <rect x="42" y="42" width="24" height="172" rx="10" fill="{mix(accent, base_dark, 0.16)}" opacity="0.92" />
  <rect x="47" y="62" width="14" height="84" rx="7" fill="{lighten(accent, 0.08)}" opacity="0.88" />
  <path d="{path([(54, 154), (60, 164), (54, 174), (48, 164)])}" fill="{paper}" opacity="0.66" />
  {render_motif(spec, motif_fill, motif_shadow, accent, bronze)}
  {render_corner_stamp(spec, bronze, accent)}
  <path d="M 70 206 C 94 222 162 222 186 206" fill="none" stroke="{paper}" stroke-opacity="0.12" stroke-width="4" stroke-linecap="round" />
</svg>
"""


def render_corner_stamp(spec: ItemSpec, bronze: str, accent: str) -> str:
    if not spec.treasure:
        return f'<circle cx="192" cy="62" r="10" fill="{mix(accent, bronze, 0.2)}" opacity="0.7" />'

    star = path([(192, 44), (197, 56), (210, 58), (200, 67), (202, 80), (192, 73), (182, 80), (184, 67), (174, 58), (187, 56)])
    return f'<path d="{star}" fill="{lighten(bronze, 0.2)}" stroke="{accent}" stroke-width="3" stroke-linejoin="round" />'


def render_motif(spec: ItemSpec, fill: str, shadow: str, accent: str, bronze: str) -> str:
    motif = spec.motif
    if motif == "jian":
        return f'<g><rect x="123" y="68" width="10" height="94" rx="4" fill="{shadow}" /><path d="{path([(128, 42), (142, 70), (128, 92), (114, 70)])}" fill="{fill}" /><rect x="104" y="154" width="48" height="10" rx="4" fill="{accent}" /><rect x="121" y="164" width="14" height="36" rx="6" fill="{bronze}" /></g>'
    if motif == "glaive":
        return f'<g transform="translate(132 58) rotate(18)"><rect x="-5" y="6" width="10" height="128" rx="4" fill="{shadow}" /><path d="M 0 -6 C 36 -2 48 26 28 54 C 16 70 -6 76 -16 62 C -4 52 6 36 0 20 Z" fill="{fill}" /><path d="{path([(-18, 40), (2, 22), (20, 44), (2, 52)])}" fill="{accent}" opacity="0.62" /></g>'
    if motif == "bow":
        return f'<g transform="translate(128 132) rotate(-18)"><path d="M -20 -60 C 30 -42 38 8 -18 72" fill="none" stroke="{mix(fill, accent, 0.2)}" stroke-width="18" stroke-linecap="round" /><line x1="2" y1="-52" x2="8" y2="66" stroke="{fill}" stroke-width="4" stroke-linecap="round" /></g>'
    if motif == "fan":
        return f'<g transform="translate(82 154) rotate(-12)"><path d="M 0 0 C 32 -50 84 -54 118 0 C 80 30 34 30 0 0 Z" fill="{fill}" /><path d="M 16 6 C 42 -22 76 -24 100 6" fill="none" stroke="{accent}" stroke-width="7" stroke-opacity="0.54" /><rect x="-6" y="-2" width="12" height="38" rx="5" fill="{shadow}" /></g>'
    if motif == "spear":
        return f'<g transform="translate(132 52) rotate(12)"><rect x="-5" y="18" width="10" height="132" rx="4" fill="{shadow}" /><path d="{path([(0, -12), (16, 18), (0, 46), (-16, 18)])}" fill="{fill}" /><path d="{path([(-18, 40), (0, 28), (18, 40), (0, 48)])}" fill="{accent}" opacity="0.52" /></g>'
    if motif == "lance":
        pennant = path([(18, 76), (52, 88), (18, 108)])
        return f'<g transform="translate(130 46) rotate(26)"><rect x="-5" y="18" width="10" height="132" rx="4" fill="{shadow}" /><path d="{path([(0, -14), (18, 18), (0, 50), (-18, 18)])}" fill="{fill}" /><path d="{pennant}" fill="{accent}" opacity="0.82" /></g>'
    if motif == "cloak":
        return f'<g><path d="{path([(92, 72), (164, 72), (188, 120), (172, 190), (84, 190), (68, 120)])}" fill="{fill}" /><path d="{path([(122, 72), (136, 72), (148, 124), (128, 192), (108, 124)])}" fill="{shadow}" opacity="0.32" /><path d="M 96 102 C 116 120 140 120 160 102" fill="none" stroke="{accent}" stroke-width="6" stroke-opacity="0.44" /></g>'
    if motif == "vest":
        return f'<g><path d="{path([(90, 66), (166, 66), (184, 108), (166, 188), (90, 188), (72, 108)])}" fill="{fill}" /><path d="{path([(102, 86), (154, 86), (164, 122), (154, 176), (102, 176), (92, 122)])}" fill="{accent}" opacity="0.34" /><path d="M 128 76 L 128 182" stroke="{shadow}" stroke-width="8" stroke-opacity="0.42" /></g>'
    if motif == "coat":
        return f'<g><path d="{path([(88, 68), (168, 68), (184, 104), (176, 192), (80, 192), (72, 104)])}" fill="{fill}" /><path d="M 108 82 L 98 190" stroke="{shadow}" stroke-width="8" stroke-linecap="round" opacity="0.34" /><path d="M 148 82 L 158 190" stroke="{shadow}" stroke-width="8" stroke-linecap="round" opacity="0.34" /></g>'
    if motif == "robe":
        return f'<g><path d="{path([(96, 58), (160, 58), (176, 102), (190, 190), (66, 190), (80, 102)])}" fill="{fill}" /><path d="{path([(118, 58), (138, 58), (152, 122), (128, 192), (104, 122)])}" fill="{accent}" opacity="0.3" /><circle cx="128" cy="92" r="8" fill="{bronze}" /></g>'
    if motif == "mail":
        return f'<g><path d="{path([(92, 64), (164, 64), (184, 102), (172, 188), (84, 188), (72, 102)])}" fill="{fill}" /><path d="M 90 110 H 166 M 90 132 H 166 M 90 154 H 166" stroke="{shadow}" stroke-width="7" stroke-opacity="0.34" /><path d="M 106 86 H 150" stroke="{accent}" stroke-width="6" stroke-opacity="0.44" /></g>'
    if motif == "lamellar":
        return f'<g><path d="{path([(90, 62), (166, 62), (188, 106), (174, 190), (82, 190), (68, 106)])}" fill="{fill}" /><path d="M 92 98 H 164 M 88 126 H 168 M 84 154 H 172" stroke="{accent}" stroke-width="8" stroke-opacity="0.38" /><path d="M 110 74 H 146" stroke="{shadow}" stroke-width="7" stroke-opacity="0.4" /></g>'
    if motif == "plate":
        return f'<g><path d="{path([(92, 56), (164, 56), (188, 100), (178, 188), (78, 188), (68, 100)])}" fill="{fill}" /><path d="{path([(108, 76), (148, 76), (160, 120), (148, 170), (108, 170), (96, 120)])}" fill="{accent}" opacity="0.32" /><path d="M 128 72 V 176" stroke="{shadow}" stroke-width="8" stroke-opacity="0.42" /></g>'
    if motif == "harness":
        return f'<g><path d="{path([(88, 70), (168, 70), (182, 102), (172, 188), (84, 188), (74, 102)])}" fill="{fill}" /><path d="M 96 82 L 160 188" stroke="{accent}" stroke-width="10" stroke-linecap="round" stroke-opacity="0.34" /><path d="M 160 82 L 96 188" stroke="{accent}" stroke-width="10" stroke-linecap="round" stroke-opacity="0.34" /></g>'
    if motif == "horse":
        return f'<g><path d="{path([(90, 126), (110, 90), (156, 88), (178, 112), (186, 148), (166, 176), (100, 176), (82, 150)])}" fill="{fill}" /><path d="{path([(122, 72), (154, 56), (176, 72), (164, 100), (134, 104)])}" fill="{accent}" opacity="0.62" /><rect x="98" y="170" width="10" height="34" rx="4" fill="{shadow}" /><rect x="150" y="170" width="10" height="34" rx="4" fill="{shadow}" /></g>'
    if motif == "signet":
        return f'<g><rect x="92" y="104" width="72" height="72" rx="18" fill="{fill}" /><rect x="112" y="70" width="32" height="42" rx="10" fill="{shadow}" /><path d="{path([(106, 118), (128, 104), (150, 118), (150, 152), (128, 166), (106, 152)])}" fill="{accent}" opacity="0.56" /></g>'
    if motif == "token":
        return f'<g><circle cx="128" cy="132" r="42" fill="{fill}" /><circle cx="128" cy="132" r="22" fill="{accent}" opacity="0.42" /><path d="M 100 78 L 156 78" stroke="{shadow}" stroke-width="10" stroke-linecap="round" /><path d="M 114 62 L 142 62" stroke="{bronze}" stroke-width="8" stroke-linecap="round" /></g>'
    if motif == "map":
        return f'<g><path d="{path([(84, 76), (122, 66), (150, 80), (176, 70), (176, 182), (146, 192), (118, 178), (84, 188)])}" fill="{fill}" /><path d="M 122 66 V 178 M 150 80 V 192" stroke="{shadow}" stroke-width="6" stroke-opacity="0.34" /><path d="M 98 154 C 112 134 134 130 150 116" stroke="{accent}" stroke-width="7" fill="none" stroke-linecap="round" /></g>'
    if motif == "reins":
        return f'<g><path d="M 88 118 C 96 84 126 74 148 88 C 164 98 168 122 154 138 C 142 152 116 150 108 134" fill="none" stroke="{fill}" stroke-width="14" stroke-linecap="round" /><path d="M 110 138 C 124 166 120 186 108 202" fill="none" stroke="{accent}" stroke-width="10" stroke-linecap="round" /><circle cx="160" cy="154" r="16" fill="none" stroke="{bronze}" stroke-width="8" /></g>'
    if motif == "banner":
        return f'<g><rect x="118" y="56" width="10" height="146" rx="4" fill="{shadow}" /><path d="{path([(128, 64), (186, 78), (164, 112), (186, 144), (128, 158)])}" fill="{fill}" /><path d="M 142 88 H 174 M 142 116 H 168" stroke="{accent}" stroke-width="8" stroke-linecap="round" stroke-opacity="0.42" /></g>'
    if motif == "sigil":
        return f'<g><path d="{path([(128, 74), (166, 102), (152, 154), (104, 154), (90, 102)])}" fill="{fill}" /><circle cx="128" cy="120" r="18" fill="{accent}" opacity="0.4" /><path d="M 128 44 V 70 M 128 158 V 188 M 92 120 H 66 M 190 120 H 164" stroke="{bronze}" stroke-width="8" stroke-linecap="round" /></g>'
    if motif == "hammer":
        return f'<g transform="translate(132 70) rotate(20)"><rect x="-7" y="46" width="14" height="106" rx="6" fill="{shadow}" /><rect x="-42" y="0" width="84" height="42" rx="10" fill="{fill}" /><rect x="18" y="8" width="20" height="26" rx="6" fill="{accent}" opacity="0.54" /></g>'
    if motif == "route":
        return f'<g><path d="{path([(86, 80), (140, 68), (176, 92), (170, 182), (118, 196), (82, 168)])}" fill="{fill}" /><path d="M 98 160 C 114 142 124 120 150 104" fill="none" stroke="{accent}" stroke-width="8" stroke-linecap="round" /><path d="M 150 104 L 138 100 L 144 116 Z" fill="{accent}" /></g>'
    if motif == "badge":
        return f'<g><path d="{path([(128, 64), (154, 74), (166, 102), (152, 134), (128, 146), (104, 134), (90, 102), (102, 74)])}" fill="{fill}" /><path d="{path([(110, 146), (126, 184), (128, 204), (130, 184), (146, 146)])}" fill="{accent}" opacity="0.62" /></g>'
    if motif == "seal":
        return f'<g><rect x="94" y="98" width="68" height="82" rx="16" fill="{fill}" /><rect x="112" y="62" width="32" height="40" rx="10" fill="{shadow}" /><path d="{path([(108, 122), (128, 110), (148, 122), (148, 152), (128, 164), (108, 152)])}" fill="{accent}" opacity="0.56" /><path d="M 116 136 H 140" stroke="{bronze}" stroke-width="6" stroke-linecap="round" /></g>'
    raise ValueError(f"Unknown motif: {motif}")


def main() -> None:
    if not Path(RSVG_CONVERT).exists():
        raise SystemExit(f"rsvg-convert not found at {RSVG_CONVERT}")

    specs = load_items()
    for spec in specs:
        svg_path = svg_path_for(spec.item_id)
        ui_png_path, resource_png_path = png_paths_for(spec.item_id)
        svg = render_icon_svg(spec)
        ensure_dir(svg_path.parent)
        svg_path.write_text(svg, encoding="utf-8")
        convert_svg_to_png(svg_path, ui_png_path, ICON_SIZE)
        convert_svg_to_png(svg_path, resource_png_path, ICON_SIZE)

    print(f"Generated {len(specs)} item icons.")


if __name__ == "__main__":
    main()
