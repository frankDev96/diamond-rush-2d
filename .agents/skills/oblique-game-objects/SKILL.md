---
name: oblique-game-objects
description: >-
  Generates gameplay objects as Blender meshes for a top-down oblique hybrid
  view: diamonds, boulders, snakes, chests, dirt, traps, and checkpoints.
  Use when creating or prompting a game prop, pickup, or hazard for Diamond
  Rush or another orthogonal grid game.
compatibility: >-
  Read oblique-hybrid-view first. Portable Agent Skill in .agents/skills,
  with a .claude/skills symlink. Requires Blender 5 to build the mesh.
---

# Game object generation

Read [oblique-hybrid-view](../oblique-hybrid-view/SKILL.md) and build one object at a time on `CellOrigin`. Copy the matching prompt from [prompts.md](prompts.md) and follow it. Do not bake a whole level into one picture.

## Shape of every prompt

1. Name the object and the gameplay job (pickup, blocker, hazard, save point).
2. Lock the oblique camera. Do not retarget it per object.
3. State size in cells, facet or silhouette, and the ground contact.
4. State material, outline, and what must stay out of the pixels (text, cast shadow, backdrop).
5. State the PNG size and the filename stem.

## Diamond Rush objects

Product rules stay in `docs/GAME_PRD.md`. Stems stay in `docs/GAMEPLAY_ASSETS.md`.

| Object | Stem | Frame |
| --- | --- | --- |
| Red diamond pickup | `red_diamond` | 128×128, about 0.7 cells wide |
| Purple currency gem | recolor of the same mesh, only when a purple sprite is requested | 128×128 or 96×96 HUD icon |
| Face boulder | `boulder_face` | 128×128, about 0.92 cells |
| Green snake | `snake_green` | 128×128 |
| Chest / secret chest | `chest_wood`, `chest_secret` | 128×128 |
| Soft dirt | `dirt_soft` | 128×128 |
| Checkpoint pad | `checkpoint_pad` | 128×128 |
| Fire, spear | `trap_fire`, `trap_spear` | 128×128 |

Red diamonds unlock the exit. Purple diamonds are currency. There is no blue diamond in v1.

Boulders push on the grid, crush snakes, block fire, and fall when the dirt under them is gone. Levels 4–6 also route boulders vertically. A boulder prompt is not limited to left and right.

The grappling hook is named and its acquisition is still TBD. Do not generate a hook, a water-crossing mechanic, potion jars, mummies, or a focus bar from a reference map.

Render with the command in `diamond-rush-blender`.
