---
name: diamond-rush-mobile-ui
description: Apply Diamond Rush mobile-first game UI/UX and the gameplay production pipeline (generated art, sprite sheets, level layouts, builders, compile check, browser preview screenshots). Use when editing Home, World Select, gameplay levels, HUD, canvases, buttons, modals, settings, camera, art assets, or any player-facing screen in this Unity project.
---

# Diamond Rush mobile UI

Always focus changes made is accepatable to mobile UI screen

## When to use

Read and follow this skill before changing any player-facing UI, scene layout, menu flow, HUD, modal, or touch control.

## Target device

- Primary device is a **landscape 16:9 phone**.
- Canvas scaler: **1920×1080**, Scale With Screen Size, match 0.5.
- Portrait is off. Desktop two-column boards, tiny labels, and hover-only states are not allowed.

## Game UI principles

- One job per screen. Home is play (Continue / New Game) plus Settings. Extra options belong in a **modal**.
- One primary action. Continue is the strongest control; New Game and Settings are secondary.
- Thumb reach: main buttons sit in the **right half** of landscape so a right thumb can tap them without covering the character.
- Hit targets at least **64px** tall (prefer 96–140px for menu buttons). ON/OFF pills at least 112×52.
- Short labels. Prefer CONTINUE, NEW GAME, SETTINGS, ON, OFF.
- Immediate feedback: pressed color, toast, or modal open. Settings apply on tap.
- Persist sound and vibrate in `PlayerPrefs`. Sound off mutes menu music the same frame.
- Menu music loops and survives Home ↔ World Select. Do not start a second copy.
- Safe padding: keep 24px from screen edges; keep buttons off the explorer silhouette.
- Do not overlap the left-side avatar with interactive controls.
- Locked worlds stay visible but not playable. Tell the player with a short toast.
- No desktop dual panels, no dense how-to boards on Home, no hover-only help.

## Home layout

- Full-bleed temple background.
- Explorer on the **left**, full height, unchanged pose.
- Logo top-center, small enough to miss the hat and buttons — **DIAMOND RUSH 2D must stay clearly readable** against the misty temple (opaque plate, not ghosted chroma-key).
- Right stack only: Continue (with last-save subtitle), New Game, Settings.
- Menu button plates and the logo are **opaque** (solid dark-stone backing). Do not ship holey / semi-transparent UI plates that show white ghosts or blend into the background.
- Settings opens a dim + stone-board **modal** with Sound ON/OFF and Vibrate ON/OFF. Close on dim tap or Close.

## World Select / level map layout

- Same 16:9 canvas. Back top-left, HUD top-right, world list left, parchment map right.
- Worlds: Angkor Wat unlocked; Bavaria, Tibet, Giza, Thiruvithamkoor locked. Locked taps toast "World locked."
- Angkor map: 12 stage nodes. Cleared / current / locked from the active profile level. PLAY is the only primary action.
- PLAY and locked nodes toast until a digging level exists.
- Play `ankor-wat-bgm.wav` while this screen is open. Restore `The_Gilded_Labyrinth.wav` on Home. One `MenuMusicPlayer` only.

## Gameplay screen layout

- Same 16:9 canvas. Banner top-left, counter chips top-right, Pause under the chips, Restart and Inventory bottom-right, floating joystick bottom-left (touch zone covers the left 40%).
- The world stays inside the HUD safe margins (ref px: top 150, bottom 40, left 250, right 260). The follow camera shows about 13x7 cells with the player at the safe-area centre.
- Screen overlays (vignette, arrows, hints) never take touches (`raycastTarget = false`) and never cover the joystick or buttons.
- One hint per level from the PRD (Level 1: "Move to Dig!"). Required and extra reds are shown separately (`2/3`, `1/2`).

## Gameplay pipeline

For new levels, gameplay art, sprite sheets, the camera, builders, or verification, read [gameplay-pipeline.md](gameplay-pipeline.md) and follow its checklist. Modeled world sprites rendered from Blender follow [the Blender skill](../../../.agents/skills/diamond-rush-blender/SKILL.md). Scripts:

- `scripts/compile_check.sh`: Roslyn compile of runtime and editor scripts while Unity stays open.
- `scripts/serve_preview.mjs`: serves the repo for `ArtSource/Gameplay/preview.html`.
- `scripts/decode_cdp_screenshot.mjs`: turns a saved CDP screenshot into a PNG.

## Rebuild

After UI hierarchy changes, rebuild with **Diamond Rush → Build Home Screen**, **Diamond Rush → Build World Select Screen** or **Diamond Rush → Build Angkor Level 1 Gameplay** (run **Process Gameplay Art** first when art changed). Bump the builder's `LayoutVersion` so the auto-build picks it up. Do not hand-edit huge `.unity` YAML when a builder exists.

## Traps

- White blank menu buttons usually mean `Image.sprite` is null. After replacing a Home/Map PNG, stale `.meta` sprite sheets (`spriteMode: 2` with old rects larger than the new texture) drop the Sprite. `MenuUiFactory.EnsureSingleSprite` must force Single, clear `m_Sprites` **and** leftover `nameFileIdTable` `_0` entries (those recreate out-of-bounds slices), then rebuild.
- Soft chroma-key alpha on logo/buttons makes labels invisible on the temple mist. Re-run `process_home_refined.mjs --install` so logo/buttons are opaque plates; bump `HomeScreenBuilder.LayoutVersion` and rebuild Home.
- Fixing only the `.meta` is not enough if the scene still has `m_Sprite: {fileID: 0}` — bump layout version and rebuild Home / World Select so Images rebind. Single sprites use `fileID: 21300000`.

## Learned (from user feedback)

Capture new valid criticism here as short bullets (see `.cursor/rules/capture-feedback.mdc`).

- Home: logo **DIAMOND RUSH 2D** must be visible; enlarge/center if needed; never leave it semi-transparent over the bright temple.
- Home: remove holey UI transparency on logo and menu buttons; use opaque plates with a proper dark stone backing color.
- Home: after installing new PNGs, verify scene `m_Sprite` is not `{fileID: 0}` — white empty buttons mean import/meta failed, not missing art files.
- World Select: `title_banner` / `icon_diamond` (and other Map/Home icons) must stay `spriteMode: 1`; Multiple sheets with rects larger than the PNG make TitleBanner and gem icons disappear.
- Empty Multiple sheets (`spriteMode: 2` + `sprites: []`) also yield zero sprites — same white UI. After PNG swaps, wipe `internalIDToNameTable` / `nameFileIdTable` and force Single before rebuilding World Select.
- Explorer walk: `explorer_walk_down` / `_up` / `_side` must be **three distinct sheets** (front / back / right profile). Identical copies make left/right flips look wrong. Left = side sheet + `flipX`. Never route L↔R turns through a fake down pose.
- Up walk must match down’s leg rhythm: clear wide contact → close passing → opposite contact → passing. If up frames look like a static V-stance, regenerate the sheet (don’t only tweak the motor).
- Soft-clear tiles are bush/grass (not mud mounds); bumping/walking in clears them immediately while the walk cycle continues. Legs must stay locked to cell travel (`strideCyclesPerStep`), not a free-running Hz timer.
- Soft-clear grass must read as **warm green vegetation** at follow-camera distance (opaque irregular clump on the floor). A dark translucent canopy looks like grey noise — reject it. Live `BushSway`; on bump `Rustle(push)` then parting clear.
- Wall caps must be **axis-aligned square oblique tops + short south lip**, processed with **Key** (never Mode.Full on magenta-backed isometric cubes). Baked magenta = pink voids in Game view. Fail process if opaque magenta >1%.
- Exterior jungle uses **top-down bush clumps** with gentle tilt-only sway (`BushSway.ConfigureGentle`). Never rotate side-view hedge strips 90° into a fence, and never stretch the 16:9 vignette.
- Walk feel: `stepDuration` ~0.22, `walkSpeedScale` ~0.85, stronger abs(sin) bob (~0.07) + contact plant squash. Stone walls hard-stop with input cooldown — no bounce/squash spam.
- Player `sortingOrder` must stay fixed above floors (10). Do not use `40 + (-y*10)` — north cells go negative and the explorer vanishes under FloorOrder 0.
- Gameplay title banner must use `title_banner.png` (wide stone plate), never stretch `hud_chip`. If `hud_chip` source is a World Select screenshot, reprocess from a real chip plaque — it shows as map nodes behind THE GATES.
- Soft grass: hand-reach lean mid-step + `grass_rustle` SFX, then part. Freeze `BushSway` before clear; no camera shake on grass (shake reads as flicker).
- Up walk sheets must be generated from the down frames’ leg poses (contact/passing pairs). A weak up sheet makes “down looks fine, up looks stuck.”
