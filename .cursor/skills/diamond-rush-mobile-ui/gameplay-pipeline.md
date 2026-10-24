# Gameplay pipeline: art, levels, builders, verification

Procedures used to build Angkor Level 1 (tunnel maze, follow camera, walk cycle). Use them for every new level, gameplay asset, or gameplay screen change.

## Order of work

```
- [ ] 1. Read docs/GAME_PRD.md (mechanics, world order, hints) and docs/GAMEPLAY_ASSETS.md (asset specs)
- [ ] 2. Write or update the asset spec in docs/GAMEPLAY_ASSETS.md before generating anything
- [ ] 3. Generate art sources, process them, inspect every output PNG
- [ ] 4. Design the layout in a Node script and run it until it prints OK
- [ ] 5. Change runtime scripts, then the builder (bump LayoutVersion)
- [ ] 6. Compile check
- [ ] 7. Browser preview screenshots at 1920x1080 and 2400x1080
- [ ] 8. Tell the user how to rebuild in Unity and what to test in the Game view
```

## 1. Art generation

- Generate with the `GenerateImage` tool on flat **#FF00FF magenta** (use #00FF00 green when the asset contains purple). Ask for thick dark outlines, cartoon style, oblique top-down view (walls show a front face, characters stand upright).
- Save sources as `ArtSource/Gameplay/Angkor/<name>.jpg`. Processed PNGs go to `Assets/Art/Gameplay/Angkor/`.
- World 1 tiles: `angkor_floor_tile.png` → floors only; `angkor_wall_front.jpg` → `wall_block`, `wall_top`, `jungle_fill`. South-adjacent floor gets `wall_block` (front face, bottom-aligned); north-only adjacency gets `wall_top` cap.
- **Animation sheets**: one 1024x1024 image, 2x2 grid, one direction per sheet (down, up, side facing right; left = side mirrored). Frame order TL, TR, BL, BR = contact, passing, contact, passing. Name the poses explicitly in the prompt ("wide stride", "upright passing pose") or every frame comes out mid-stride; regenerate if so.
- Tileable textures: ask for "seamless, tileable, no border". If a dark border appears, use `inset`; if it doesn't tile, use `seamless`.
- Screen frames (vignettes) have content in the corners, so the corner pixel is not the background. Use `KeyCenter`.
- After processing, **read every output PNG** to check the keying, the trim, and that the poses differ.

## 2. Processing

Two mirrors that must stay in sync: `Assets/Editor/GameplayArtImporter.cs` (Unity menu **Diamond Rush → Process Gameplay Art**) and `ArtSource/Gameplay/process_art.mjs` (works without the Editor).

```bash
/opt/homebrew/opt/node@22/bin/node ArtSource/Gameplay/process_art.mjs              # all entries
/opt/homebrew/opt/node@22/bin/node ArtSource/Gameplay/process_art.mjs explorer_walk # prefix filter
```

| Mode | Use |
|------|-----|
| `Full` | No keying (opaque tiles) |
| `Key` | Corner-pixel chroma key plus despill, then trim |
| `KeyLeftHalf` / `KeyRightHalf` | Two assets on one sheet |
| `KeyCenter` | Key from the centre pixel, no trim (vignettes) |
| `Grid2x2` | Walk sheets: key, one shared bounding box for all 4 quadrants (no jitter), each frame 160x256, bottom-aligned 8px, written as `name_0..3.png` |

Entry options: `size`, `inset` (crop fraction for dark borders), `seamless` (cross-fade band fraction), `tiled` (Repeat wrap, FullRect mesh), `feetPivot` (0.5, 0.04), `rotate90`, `clearCenter`.

Import settings for all gameplay art: 128 PPU, no mipmaps, bilinear, uncompressed, Clamp (Repeat if tiled), FullRect mesh. Runtime loads go through `GameplayArtImporter.Load(file)` and `LoadFrames(stem)`. Never assign raw texture paths in builders.

When adding an asset: add the entry in both files, add it to `docs/GAMEPLAY_ASSETS.md` (size, pivot, sort order, usage), then wire it in the builder.

## 3. World rendering conventions

- 1 cell = 1 world unit. Sort orders: backdrop −50, floor 0, shadows 1, dirt 2, walls 3, door 4, door slots 5, gem glow 6, gem 7, player shadow and FX 9, player 10.
- Wall autotiling in `GridBoard` (oblique hybrid): floor **south** of stone (`y-1`) → bottom-aligned `wall_block` front face + shadow on that floor; floor **north** only → `wall_top` cap; E/W corridors → front face with `flipX`; inner mass with no cardinal floor → `wall_top` if any 8-neighbour open else tiled `jungle_fill` backdrop (board + 12 cells).
- Floor variants are picked by a stable hash (`(x*73856093 ^ y*19349663) & 0x7fffffff`): about 18% `floor_moss`, 10% `floor_cracked`, then `floor_stone`/`floor_stone_alt`. The preview uses `Math.imul` so the hashes match.
- **Intro goal pan:** after the intro card fades, `TutorialOverlay` calls `GameplayCameraFit.PanTo(board.DoorCameraAnchor, hold)` so the locked door is visible once; input stays blocked while `IsPanning`.
- **Interaction hints:** `InteractionHintOverlay` shows one contextual arrow (dig-adjacent dirt, or pushable boulder) until the player digs; raycasts off.
- Reference mockups for Level 2 may show every mechanic at once; shipping levels follow PRD hints one at a time.
- Characters use feet pivots. The player visual sits at local y −0.42 with scale 0.52, so the boots are near the bottom of the cell.
- **PlayerGridMotor:** root transform = grid cell; child **Visual** gets bob, lean, squash, and idle breathe. Step timing 0.18 s/tile (0.36 s when pushing boulders); dig 0.12 s lunge (halt at tile edge, lean, shovel arc); push 0.12 s brace. Code-driven 4-frame walk arrays per direction plus 0.05 s down-facing pivot on horizontal reversal. Optional `Animator` on Visual exposes `MoveX`, `MoveY`, `IsMoving`, `IsDigging`, `IsPushing` (no controller required).
- **GameplayFeedback / GameplayFxParticles:** foot dust and dig debris use runtime `ParticleSystem` bursts (warm-gray specs 0.04–0.08, sort order 9). Do not fall back to `GameplaySprites.SoftCircle` for footsteps — it reads as a giant white puff. Gem pickup animates the **Gem** child only (hide glow); fly to `GameplayHudController.RequiredChipScreenPoint` then `PunchRequired`.

## 4. Level layouts

- Symbols: `#` stone, `.` floor, `d` soft dirt, `r` red gem, `g` purple gem, `B` boulder, `T`/`t` chest/secret chest, `F` fire, `!` spear, `K` checkpoint, `S` snake marker (floor; patrol from `SnakePatrolSpec` in layout), `E` exit (`EE`), `P` spawn. Rows are listed top row first; `GridBoard` flips them so y=0 is the bottom.
- Levels 1–9: `GameplayLevelCatalog`, scenes `GameplayLevelN.unity`, builder **Build Angkor Gameplay Levels 2-9** (`GameplayLevelsBuilder`, layout version 6). World Select PLAY loads level 1–9.
- Design in a Node script (template: `ArtSource/Gameplay/level1_maze.mjs`) that carves rectangles and validates. Iterate until it prints `OK`, then paste the rows into `LevelNLayout.cs` with `RequiredReds` / `TotalReds` / `ExtraReds` constants.
- Validation rules (mirrored in `Assets/Editor/LevelLayoutValidator.cs`, which the builder runs and refuses to save on failure):
  - Exact width and height; stone border except the `EE` door in the top row.
  - Exactly one `P`; red count equals `TotalReds`.
  - Every red and the door are reachable with dirt treated as passable.
  - No red is reachable **without** digging (Level 1 teaches "Move to Dig!").
- Maze feel: tunnels 1–2 cells wide, walls at least 1 thick, zones in the PRD teaching order, extra reds in dead-end pockets, the door at the stage edge.
- Only mechanics the PRD allows for that level. Level 1 has no boulders, snakes or traps.

## 5. Gameplay screen layout

- HUD safe margins in 1920x1080 reference px: top 150, bottom 40, left 250, right 260. Scale them by the CanvasScaler factor `2^lerp(log2(w/1920), log2(h/1080), 0.5)`.
- Follow camera (`GameplayCameraFit`): `ppu = min(availW/13, availH/7)` (about 13x7 cells visible); the focus point sits at the safe-area centre; SmoothDamp at 0.18 s; clamp to the board plus 1 cell (1.6 on top for the door); `ShakeOffset` is added after the clamp; `PanTo(pos, hold)` for reveals blocks input while panning.
- Canvas child order: exit indicator → joystick touch zone, base and hint → HUD → tutorial → Modals. Jungle leaves/vines are world sprites on the board exterior (`jungleEdgeSprite` on `GridBoard`), not a full-screen UI vignette.
- An off-screen target gets an edge arrow clamped to the safe rect, inset 80 ref px, rotated with `atan2`, pulsing.
- Every UI image that is not a control has `raycastTarget = false`.

## 6. Builders

- Scenes are generated by `Assets/Editor/*Builder.cs` (menu **Diamond Rush → Build …**). Never hand-edit `.unity` YAML.
- Builders auto-run on domain reload when `EditorPrefs` `DiamondRush.<Scene>LayoutVersion` differs from `LayoutVersion`. **Bump `LayoutVersion`** whenever builder output changes, or the user's scene stays stale.
- Wire references with `MenuUiFactory.SetRef(so, field, obj)`; for `Sprite[]` fields set `arraySize` and each element.
- The Unity Editor normally has the project open, so batch mode fails on the project lock. The user rebuilds by focusing Unity (auto-build) or using the menu.

## 7. Compile check (Editor stays open)

```bash
zsh .cursor/skills/diamond-rush-mobile-ui/scripts/compile_check.sh
```

Compiles `Assets/Scripts`, then `Assets/Editor` against it, with Unity's bundled Roslyn. It prints `OK` or the compiler errors. Fix all errors before reporting done. Set `UNITY_VERSION` if the Editor version changes.

## 8. Browser preview and screenshots

`ArtSource/Gameplay/preview.html` re-implements the board, camera and HUD maths in DOM. Keep it in sync with runtime changes (layout rows, margins, ppu, autotile rules, sort orders).

Query params: `view=follow|overview`, `w`, `h` (try 1920x1080 and 2400x1080), `px`/`py` (player cell, top-origin), `frame=0..3`, `unlocked=1` (open door plus exit arrow).

1. Start the server in the background: `node .cursor/skills/diamond-rush-mobile-ui/scripts/serve_preview.mjs` (port 8765). Check first with `curl -s -o /dev/null -w "%{http_code}" http://localhost:8765/ArtSource/Gameplay/preview.html`.
2. `browser_navigate` to `http://localhost:8765/ArtSource/Gameplay/preview.html?view=follow&w=1920&h=1080&n=<unique>`. Change `n` on every load to avoid a stale page.
3. The page script does not auto-run in the IDE browser. Run it with CDP `Runtime.evaluate` (`awaitPromise: true`):
   ```js
   (async () => { await new Promise(r => setTimeout(r, 500));
     if (!document.querySelector('#stage img')) eval(document.scripts[0].textContent);
     await Promise.all([...document.images].map(i => i.complete ? 1 : new Promise(r => { i.onload = i.onerror = r; })));
     await new Promise(r => setTimeout(r, 1500)); // CSS background tiles are not in document.images
     return location.search; })()
   ```
   Run it as its own call after navigation. Calling it in parallel fails with "Execution context was destroyed".
4. Capture with CDP `Page.captureScreenshot` (`format: png`). The response is saved to a JSON log file. Decode it:
   `node .cursor/skills/diamond-rush-mobile-ui/scripts/decode_cdp_screenshot.mjs <log.json> ArtSource/Gameplay/preview_<name>.png`
5. Read the PNG and check: the HUD clears the player, controls are not covered, the maze reads as tunnels, and the arrow and door are placed correctly.

Known traps:
- `browser_take_screenshot` can return stale or blank frames, especially after `Emulation.setDeviceMetricsOverride`. Prefer `Page.captureScreenshot` and avoid emulation.
- `Page.captureScreenshot` with `clip` returns tiled garbage. Capture the full viewport.
- `sips -c` always crops from the centre on this machine, which cuts the HUD off. Do not crop; the black band below the stage is harmless.
- When culling off-screen cells in the preview, record exit cells **before** the cull or the door disappears.

## 9. Environment

- Use Node at `/opt/homebrew/opt/node@22/bin/node`. Python fails here (Xcode license). Use `sips` for image sizes.
- zsh does not word-split `$VARS`. Build argument lists in files or loops.
- Computer Use has no Accessibility or Screen Recording permission, so the Unity Game view cannot be screenshotted. Verify with the compile check and the browser preview, and ask the user to playtest.
- Ignore the "Unable to locate a Java Runtime" line printed at shell start.

## Finish

Report to the user: what changed, the preview screenshots (embed them), the compile result, and the exact Unity steps (focus Unity, or **Process Gameplay Art** then **Build …**). Also list what to test in the Game view and any deviations from the plan.
