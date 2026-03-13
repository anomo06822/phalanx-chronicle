# Art Source Pipeline

Authoring sources live here and are not read directly at runtime.

- `Characters/`: Aseprite source files for battle sprites and portraits.
- `Terrain/`: Aseprite source files for stage terrain tiles and props.
- `Stages/`: Tiled `.tmx` / `.tsx` files used for stage layout authoring and validation.

Runtime output still flows through:

- `Assets/Art/Characters/Battle`
- `Assets/Art/Characters/Portraits`
- `Assets/Resources/Terrain`

Export rules:

- Battle sprite: `128x128`, `Point`, `128 PPU`, `Center Pivot`
- Terrain tile: `64x64`, `Point`, `64 PPU`
- Portrait: high-resolution illustration, `Bilinear`
