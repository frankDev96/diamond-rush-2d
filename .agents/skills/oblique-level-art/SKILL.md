---
name: oblique-level-art
description: >-
  Builds floor tiles, wall caps, avatars, and level kits for a top-down
  oblique hybrid grid. Use when creating floors, walls, characters, walk
  cycles, or the pieces a level is assembled from in Diamond Rush or
  another orthogonal grid game.
compatibility: >-
  Read oblique-hybrid-view first. Portable Agent Skill in .agents/skills,
  with a .claude/skills symlink. Requires Blender 5 to build meshes.
---

# Floors, walls, avatars, levels

Read [oblique-hybrid-view](../oblique-hybrid-view/SKILL.md). Full prompts are in [prompts.md](prompts.md). Render each piece with [oblique-game-assets](../oblique-game-assets/SKILL.md).

A level is a square grid of these pieces plus the objects in `oblique-game-objects`. Blender renders the pieces. It does not write the game scene.

## Pieces

| Piece | What the oblique frame shows | Diamond Rush stem |
| --- | --- | --- |
| Floor | Flat square slab, thin front edge, seamless | `floor_stone`, `floor_stone_alt`, `floor_moss`, `floor_cracked` |
| Wall | One-cell top cap and a short south lip, seamless | `wall_block`, `wall_top`, deep fill `jungle_fill` |
| Avatar | Upright body, four facings, feet on the cell | `explorer_walk_down`, `_up`, `_side` frames `_0`..`_3` |
| Level | Separate PNGs laid on the grid | Layout files, not a baked map |

World 1 walls stay one cell tall. A tall arch billboard is the wrong sprite. The front lip of the cap is what the player passes under in sort order.

## Diamond Rush levels

World 1 teaching order in `docs/GAME_PRD.md` decides which pieces a stage may use. Levels 1–3 teach move, dig, reds, boulders, and snakes. Later bands add crushing, chests, purple gems, fire, spears, checkpoints, then the mallet.

A reference map of skull chambers, a garden, a crypt, and a laboratory can guide Angkor stone, carved faces, arch lips, and a golden checkpoint at an entrance. It does not add a stage, a Unity Tilemap, water traversal, jars, or mummies. Layouts stay in the existing level scripts and `GridBoard`.

## Other projects

Keep the same four piece types and the same camera. Swap the style words in the prompts (stone, snow, ice) and bind stems in that project's skill. Bavaria and Tibet in this repo are named and not ready for stage art.
