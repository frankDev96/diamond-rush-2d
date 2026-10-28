---
name: diamond-rush-blender
description: >-
  Models and batch-renders Diamond Rush oblique gameplay sprites in
  BlenderStudio/DiamondRush_Oblique.blend (45° orthographic camera, Z
  rotation 0°, transparent PNG) into Assets/Art/Gameplay/Angkor. Use when
  the user mentions Blender, a .blend file, oblique or top-down hybrid
  sprites, rendering the explorer, face boulder, Angkor walls, diamonds,
  or snakes, or exporting those renders into the Unity project. Pair with
  oblique-hybrid-view, oblique-game-objects, oblique-game-assets, and
  oblique-level-art.
compatibility: >-
  Blender 5 at /Applications/Blender.app on this Mac. System Python cannot
  import bpy. Cursor, Codex, OpenCode, and VS Code Copilot read this folder
  at .agents/skills. Claude Code reads the .claude/skills symlink to this
  same folder. Do not fork a second copy of the skill.
---

# Diamond Rush Blender sprites

Unity plays the game. Blender only models, lights, and renders transparent PNG sprites. Keep the Unity project open in Unity. Keep `BlenderStudio/DiamondRush_Oblique.blend` open in Blender while iterating.

Product and mechanics stay in `docs/GAME_PRD.md`. Filenames, pixel sizes, and pivots stay in `docs/GAMEPLAY_ASSETS.md`. Canvas and touch rules stay in the mobile-ui skill.

Companion skills, same view for this project and later games:

- [oblique-hybrid-view](../oblique-hybrid-view/SKILL.md) — the only camera. Square grid, front faces, upright characters.
- [oblique-game-objects](../oblique-game-objects/SKILL.md) — diamonds, boulders, snakes, chests, dirt, traps. Prompts in [prompts.md](../oblique-game-objects/prompts.md).
- [oblique-game-assets](../oblique-game-assets/SKILL.md) — PNG size, pivot, and export.
- [oblique-level-art](../oblique-level-art/SKILL.md) — floor, wall, avatar, and how a level is assembled. Prompts in [prompts.md](../oblique-level-art/prompts.md).

## Red diamond

Follow the Red diamond prompt in [oblique-game-objects/prompts.md](../oblique-game-objects/prompts.md), then render:

```bash
/Applications/Blender.app/Contents/MacOS/Blender \
  BlenderStudio/DiamondRush_Oblique.blend --background \
  --python .agents/skills/diamond-rush-blender/scripts/render_to_unity.py -- \
  --name red_diamond --resolution 128
```

## Layout

```
DiamondRush/
├── .agents/skills/diamond-rush-blender/   # this skill (canonical)
├── .claude/skills/diamond-rush-blender    # symlink for Claude Code
├── BlenderStudio/
│   ├── DiamondRush_Oblique.blend          # master scene, beside Assets
│   └── scripts/render_to_unity.py         # launcher into the skill script
└── Assets/Art/Gameplay/Angkor/            # rendered PNGs land here
```

`BlenderStudio/` stays next to `Assets/`, `Packages/`, and `ProjectSettings`. Unity does not import `.blend` files from there. One Blender unit is one grid cell.

## Camera

Lock this once in `DiamondRush_Oblique.blend`. Re-run the setup script if the file is missing or still a zero-byte placeholder.

| Control | Value |
| --- | --- |
| Location | X 0 m, Y −5 m, Z 5 m |
| Rotation | X 45°, Y 0°, Z 0° |
| Type | Orthographic |
| Orthographic scale | 2.0 (one cell, with a little margin) |
| Film | Transparent |
| Output | PNG, RGBA, 8-bit |

Z stays 0° so a cell stays a square in screen space. A 45° Z rotation would turn the grid into diamonds. The Unity gameplay camera is a separate orthographic follow camera in `GameplayCameraFit`. Do not copy the Blender camera into a scene YAML file.

```bash
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
  --python .agents/skills/diamond-rush-blender/scripts/setup_oblique_scene.py
```

`blender` is not on PATH. The setup script keeps existing meshes when the `.blend` is already a real file, and it rewrites the camera, sun, cell-origin empty, and film settings.

## Render

Model at the `CellOrigin` empty (1×1×1 display cube, not rendered). Then:

```bash
/Applications/Blender.app/Contents/MacOS/Blender \
  BlenderStudio/DiamondRush_Oblique.blend --background \
  --python .agents/skills/diamond-rush-blender/scripts/render_to_unity.py -- \
  --name boulder_face --resolution 128
```

`--name` is the stem in `docs/GAMEPLAY_ASSETS.md` (`boulder_face`, `wall_block`, `red_diamond`, `snake_green`). The script writes `Assets/Art/Gameplay/Angkor/<stem>.png`. Orthographic scale 2.0 frames one cell. Explorer frames pass `--width 160 --height 256` and keep that scale so the body is about two units tall; place the boots on the cell floor and read the PNG before using it.

| Kind | Stem | Frame | Pivot |
| --- | --- | --- | --- |
| Tiles, boulder, snake, diamond, dirt | matching `GAMEPLAY_ASSETS.md` | 128×128 | centre (0.5, 0.5) |
| Floor and wall caps that tile | `floor_stone`, `wall_block`, `wall_top` | 128×128 seamless | centre |
| Explorer walk | `explorer_walk_down`, `_up`, `_side` as four frames `_0`..`_3` | 160×256 each | feet (0.5, 0.04) |

World 1 walls are one-cell top caps. The oblique camera should show the front lip of that cap inside the 128×128 frame. Deep fill stays `jungle_fill`.

From Blender's Text Editor, run `BlenderStudio/scripts/render_to_unity.py` and call `render_oblique_asset_to_unity("boulder_face", resolution=128)`.

## After the PNG exists

1. Add or update the row in `docs/GAMEPLAY_ASSETS.md` (pixels, pivot, sort order, use) before wiring it into a level.
2. Unity import matches `GameplayArtImporter`: Sprite (Single), 128 pixels per unit, no mipmaps, bilinear, uncompressed, Full Rect mesh, alpha is transparency. Clamp wrap, except Repeat for tiled floors and fills. Centre pivot, or feet `(0.5, 0.04)` for the explorer.
3. Sort with the `sortingOrder` values in `GridBoard` (floor 0, dirt 2, walls 3, player 10). Leave `ProjectSettings/GraphicsSettings.asset` transparency sort at its default.
4. Wire the sprite through the existing builder. Bump `LayoutVersion`. Do not hand-edit `.unity` YAML.
5. Read the PNG before reporting done. Corners are transparent. The subject sits in the cell the way the asset spec describes.

HUD buttons, joystick art, and vignettes stay on the GenerateImage path in `gameplay-pipeline.md`. Blender is for modeled world sprites.

## Existing mesh already in Unity

Reveal the `.fbx`, `.obj`, or `.gltf` in Finder, import that file into `DiamondRush_Oblique.blend`, then render with the command above. Leave the source mesh outside `Assets/` when you can.

## Traps

- A zero-byte `DiamondRush_Oblique.blend` is not a Blender file. Run the setup script.
- Render into `Assets/Art/Gameplay/Angkor/`. The game loads that folder through `GameplayArtImporter`.
- The board is `GridBoard`. One cell is one world unit. There is no tilemap asset and no `ObliqueGridManager` to add.
- This repo has no Blender MCP server. Use the Blender binary.
- System `python3` cannot `import bpy` here. Always launch Blender.
- Ignore `BlenderStudio/*.blend1` backups. The master file is `DiamondRush_Oblique.blend`.
