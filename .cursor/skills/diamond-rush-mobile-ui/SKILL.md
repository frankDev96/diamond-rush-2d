---
name: diamond-rush-mobile-ui
description: Apply Diamond Rush mobile-first game UI/UX on every screen, layout, HUD, modal, and menu change. Use when editing Home, World Select, canvases, buttons, settings, art placement, or any player-facing UI in this Unity project.
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

## World Select layout

- Same 16:9 canvas and music.
- Back returns to Home.
- One row of world cards. Angkor Wat unlocked. Others locked.
- Tapping a locked world or an unfinished world shows a toast, not a new desktop panel.

## Rebuild

After UI hierarchy changes, rebuild with **Diamond Rush → Build Home Screen** and **Diamond Rush → Build World Select Screen**. Do not hand-edit huge `.unity` YAML when a builder exists.
