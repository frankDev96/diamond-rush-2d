---
name: oblique-hybrid-view
description: >-
  Locks game art to a top-down oblique hybrid view: orthographic camera,
  45° pitch, 0° yaw, square grid, front faces visible, characters upright.
  Use for Diamond Rush and any other project when floors, walls, avatars,
  props, or levels must be shown only in that view.
compatibility: >-
  Portable Agent Skill. Cursor, Codex, OpenCode, and VS Code Copilot read
  .agents/skills. Claude Code reads the .claude/skills symlink. Blender 5
  is required only when a mesh is actually built.
---

# Top-down oblique hybrid view

Every mesh, sprite, and level piece uses this view and no other. Grid math stays orthogonal. The picture is tilted just enough to show front faces.

## Camera

| Control | Value |
| --- | --- |
| Location | X 0, Y −5, Z 5 |
| Rotation | X 45°, Y 0°, Z 0° |
| Projection | Orthographic |
| Orthographic scale | 2.0 for one cell |
| Film | Transparent PNG |

Z stays 0°. One world unit is one grid cell. North is up the screen. The camera looks from the south.

## What the frame must show

- Floor cells stay squares. A wall shows its top and a short front lip. A boulder shows its front face. An avatar stands upright with feet on the cell.
- Facing art is four-way only: toward the camera, away, and to the right. Left is the right-facing sprite mirrored.
- Props sit on the cell floor. Their ground contact (feet, base, wall foot) is readable at the export size.

## Frames that fail the view

- Isometric diamond grid (yaw 45°, or elevation about 54.7°).
- Perspective camera.
- Side view or a straight-on front (pitch 0°).
- Pure top-down (pitch 90°) that hides the front face.

## Depth

Author the contact point on the ground. Diamond Rush draws order with `sortingOrder` on `GridBoard` and leaves Graphics transparency sort at its default. A different project that uses a 2D renderer can put the sprite pivot on that contact point and sort on axis `(0, 1, 0)`. Do not change Diamond Rush graphics settings to match that other setup.

## Other projects

Reuse this camera. Keep the blend outside the game `Assets` folder. Render transparent PNGs. Bind filenames and the output folder in that project's own skill. Diamond Rush binds them in `diamond-rush-blender`.
