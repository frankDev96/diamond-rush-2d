---
name: oblique-game-assets
description: >-
  Turns oblique Blender objects into game-ready transparent PNG sprites and
  walk sheets with stable names, pivots, and pixel sizes. Use when exporting,
  naming, or batch-rendering game assets from Blender for Diamond Rush or
  another top-down oblique project.
compatibility: >-
  Read oblique-hybrid-view and oblique-game-objects first. Portable Agent
  Skill in .agents/skills, with a .claude/skills symlink.
---

# Game asset creation

Read [oblique-hybrid-view](../oblique-hybrid-view/SKILL.md). Generate the mesh from [oblique-game-objects](../oblique-game-objects/SKILL.md) or [oblique-level-art](../oblique-level-art/SKILL.md), then export one sprite per object.

## Export rules

- PNG, RGBA, transparent film, 8-bit.
- One cell of floor or a prop: `128×128`. A taller avatar frame: `160×256`. A two-cell door may be taller. Do not pick a new pixels-per-unit per asset. Diamond Rush uses 128 pixels per unit.
- Centre pivot for tiles and props. Feet pivot `(0.5, 0.04)` for the avatar.
- Filename stems are lowercase with underscores. Diamond Rush stems are listed in `docs/GAMEPLAY_ASSETS.md`.
- No text, HUD numbers, or ground shadow baked into the texture.
- Corners of a prop PNG are fully transparent. A tiling floor or wall fills the frame and is seamless on the edges that repeat.

## Sheets

A walk cycle is three sheets, not a turntable. Each sheet is one facing (toward camera, away, right). Four frames in reading order: contact, passing, contact, passing. Left facing is the right sheet mirrored. Name the stride in the prompt ("wide stride", "upright passing pose").

## Checklist

- [ ] Camera still matches oblique-hybrid-view (Z rotation 0°).
- [ ] Subject sits on the cell and the front face is visible.
- [ ] PNG size and stem match the project asset list.
- [ ] Open the PNG and check the outline, the alpha corners, and that a walk's frames are different poses.

## Diamond Rush

```bash
/Applications/Blender.app/Contents/MacOS/Blender \
  BlenderStudio/DiamondRush_Oblique.blend --background \
  --python .agents/skills/diamond-rush-blender/scripts/render_to_unity.py -- \
  --name red_diamond --resolution 128
```

That writes `Assets/Art/Gameplay/Angkor/<stem>.png`. Then update `docs/GAMEPLAY_ASSETS.md` and let `GameplayArtImporter` set Sprite, 128 pixels per unit, no mipmaps, bilinear, uncompressed. Another project writes into its own sprite folder and records the path in its project skill.
