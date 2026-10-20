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
- Logo top-center, small enough to miss the hat and buttons.
- Right stack only: Continue (with last-save subtitle), New Game, Settings.
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

For new levels, gameplay art, sprite sheets, the camera, builders, or verification, read [gameplay-pipeline.md](gameplay-pipeline.md) and follow its checklist. Scripts:

- `scripts/compile_check.sh`: Roslyn compile of runtime and editor scripts while Unity stays open.
- `scripts/serve_preview.mjs`: serves the repo for `ArtSource/Gameplay/preview.html`.
- `scripts/decode_cdp_screenshot.mjs`: turns a saved CDP screenshot into a PNG.

## Rebuild

After UI hierarchy changes, rebuild with **Diamond Rush → Build Home Screen**, **Diamond Rush → Build World Select Screen** or **Diamond Rush → Build Angkor Level 1 Gameplay** (run **Process Gameplay Art** first when art changed). Bump the builder's `LayoutVersion` so the auto-build picks it up. Do not hand-edit huge `.unity` YAML when a builder exists.
