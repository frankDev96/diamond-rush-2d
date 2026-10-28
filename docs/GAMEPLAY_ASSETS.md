# Gameplay assets — Angkor Wat (World 1)

Spec sheet for every sprite the Level 1 gameplay scene uses. Product rules live in [`GAME_PRD.md`](GAME_PRD.md); layout metrics live in the mobile-ui skill.

## Pipeline

| Step | Rule |
| --- | --- |
| Style | Painterly semi-realistic, soft lighting, material texture (stone grain, fabric, scales, gem refraction). Warm jungle-temple palette. Oblique top-down (Zelda-style): walls show a front face, characters stand upright. No thick cartoon outlines, no baked text. Level 2 map mockup is style reference only — do not ship potions, energy cans, blue gems, grappling hook, or focus/vitality bars from that mockup (PRD). |
| Source | Generated JPG in `ArtSource/Gameplay/Angkor/` (outside `Assets`). Flat **#FF00FF** background; **#00FF00** when the subject is purple/magenta. Full-bleed tiles have no background. Modeled world sprites (explorer, boulder, wall caps, diamonds, snakes) are rendered from `BlenderStudio/DiamondRush_Oblique.blend` by the diamond-rush-blender skill, already transparent, straight into the output folder. |
| Processing | **Diamond Rush > Process Gameplay Art** (`Assets/Editor/GameplayArtImporter.cs`) or `node ArtSource/Gameplay/process_art.mjs`. Corner-pixel chroma key, un-blend, despill, trim (alpha > 0.05, 2px pad), supersampled resize. |
| Output | `Assets/Art/Gameplay/Angkor/*.png` |
| Import | Sprite (Single), **128 px per unit** (1 tile = 1 world unit), no mipmaps, bilinear, uncompressed, clamp wrap. Tiled art uses repeat wrap + Full Rect mesh. |
| Pivot | Centre unless listed. Walk frames pivot at the feet (0.5, 0.04). |

Sorting orders (world sprites): floor 0, wall shadow 1, dirt 2, walls 3, door glow 3, door 4, door slots 5, gem glow 6, gem 7, player shadow 9, FX 9, player 10. Backdrop −50.

## World tiles

| File | Pixels | Source | Mode | Use |
| --- | --- | --- | --- | --- |
| `floor_stone.png` | 128×128 | `angkor_floor_tile.png` | Full, inset + seamless | Sandy floor slab (center crop, tiles on open cells). Random X flip by cell hash. |
| `floor_stone_alt.png` | 128×128 | `angkor_floor_tile.png` | Full, rotated 90° | Floor variant (~42%). |
| `floor_moss.png` | 128×128 | `angkor_floor_tile.png` | Full, lighter inset | Same art with more corner moss (~18%). |
| `floor_cracked.png` | 128×128 | `angkor_floor_tile.png` | Full, inset + seamless | Cracked stone variant (~10%); same temple floor family (not procedural grey). |
| `boulder_face.png` | 128×128 | `boulder_face.jpg` (Angkor L2 map idols) | Key | Pushable face boulder (~0.92 cells). |
| `snake_green.png` | 128×128 | `snake_green.jpg` (Angkor L2 map) | Key | Patrolling snake (`GridWorldController`). Bright green per PRD. |
| `chest_wood.png` | 128×128 | `chest_wood.jpg` | Key | +4 purple when stepped on. |
| `chest_secret.png` | 128×128 | `chest_secret.jpg` | Key | Darker secret chest; +8 purple / extra life chance. |
| `trap_fire.png` / `trap_spear.png` | 128×128 | procedural | Full | Fire damage; spear toggles every ~0.7 s. Levels 7+. |
| `checkpoint_pad.png` | 128×128 | `checkpoint_pad.jpg` (glowing rune circle) | Key | Sets respawn; backpack **RETURN** from level 7+. |
| `torch_wall.png` | 128×128 | `torch_wall.jpg` | Key | Decorative wall torch (warm flame). Optional décor. |
| `dirt_soft.png` | ~128×128 | `dirt_soft.jpg` (opaque grass clump) | Key | Soft-clear irregular grass on floor; live `BushSway`; rustles then clears on bump. Must read as green vegetation at follow distance (not a dark canopy). |
| `wall_block.png` | 128×128 | `wall_block.jpg` | Key | Oblique square **top + south lip** wall cap (floor south / E–W corridors). Never isometric diamonds; no baked magenta. |
| `wall_top.png` | 128×128 | `wall_top.jpg` | Key | Flatter north-rim / inner-mass cap (same square oblique rules). |
| `jungle_fill.png` | 256×256, seamless | `jungle_fill.jpg` | Full, tiled | Dense jungle foliage for deep solid cells + exterior backdrop. |
| `bush_edge.png` | ~128–512 | `bush_edge.jpg` (top-down clump) | Key | Exterior bush clumps along the board border (`SpawnBushClumpRow`). Top-down irregular foliage, gentle sway. |

## Exit and pickups

| File | Pixels | Source | Mode | Use |
| --- | --- | --- | --- | --- |
| `exit_door_closed.png` | ~375×384 | `exit_door_closed.jpg` | Key | Two-cell door in the top wall, drawn 2.1 cells wide, bottom aligned to the `EE` row bottom edge. |
| `exit_door_open.png` | ~374×384 | `exit_door_open.jpg` | Key | Swapped in on unlock + gold glow pulse. |
| `red_diamond.png` | 128×119 | `red_diamond.jpg` | Key | Pickup, 0.7 cells wide, bob + red glow. Also the door slot sockets (0.29 cells, dark tint until lit). |
| `purple_diamond.png` | 128×128 | `purple_diamond.jpg` (green-keyed) | Key | Currency gem pickup / chest contents. No blue gem in v1. |

## Explorer

| File | Pixels | Source | Mode | Use |
| --- | --- | --- | --- | --- |
| `explorer_walk_down_0..3.png` | 160×256 each | `explorer_walk_down.jpg` (1024×1024, 2×2) | Grid2x2 | Walk cycle facing camera. Frames: right contact, passing, left contact, passing. Shared bbox across frames; pivot (0.5, 0.04). |
| `explorer_walk_up_0..3.png` | 160×256 each | `explorer_walk_up.jpg` | Grid2x2 | Walk cycle facing away. |
| `explorer_walk_side_0..3.png` | 160×256 each | `explorer_walk_side.jpg` | Grid2x2 | Walk cycle facing right; mirrored for left. |
| `explorer_down.png` / `_up` / `_side` | ≤150×256 | `explorer_*.jpg` | Key | Fallback static poses when walk frames are missing. |

Runtime: visual scale 0.52 (≈1.04 cells tall), 10 fps cycle, frame 0 when idle, step squash 4%.

## Controls and HUD (UI, preserve aspect)

| File | Pixels | Use |
| --- | --- | --- |
| `joystick_base.png` | 313×320 (centre cleared) | Floating joystick ring, 220 ref-px, rests bottom-left at 45% alpha. |
| `joystick_knob.png` | 158×160 | Knob, 104 ref-px, max travel 96 ref-px. |
| `btn_pause.png` | ~190×190 | Top-right, 104 ref-px. Opens pause modal. |
| `btn_restart.png` | ~190×190 | Right cluster, 112 ref-px. Restart confirm. |
| `btn_inventory.png` | ~190×190 | Right cluster, 150 ref-px. Backpack modal. |
| `icon_red.png` | 96×89 | Required / extra red chips, backpack, win panel. |
| `icon_purple.png` | 96×89 | Purple wallet chip (green-keyed source). |
| `icon_lives_head.png` | 77×96 | Lives chip. |
| `exit_arrow.png` | 96×96 | Gold arrow pointing right. Off-screen exit indicator, rotated toward the door, 88 ref-px, pulses. |
Shared menu art reused by gameplay: `title_banner.png` for the Level title plate (THE GATES); `hud_plaque_gold.png` (9-sliced) for counter chips when present, else `hud_chip.png`; intro card may use `hud_chip.png`. Never put a World Select screenshot into `hud_chip`. Also: `Assets/Art/Home/stone_board.png`, `button_wood.png`, `button_continue.png`, `pill_on.png`, `pill_off.png` for modals.

## FX

| File | Pixels | Source | Use |
| --- | --- | --- | --- |
| `dust_puff.png` | 64×64 | `fx_dust_dirt.jpg` left half | Step dust, 0.3 cells, grows 2× and fades over 0.35 s. |
| `dirt_chunk.png` | 64×51 | `fx_dust_dirt.jpg` right half | Six chunks per dig, gravity + spin, 0.45 s. |

Runtime-generated: soft radial circle (glows, shadows, bursts) and a white square (wall shadow strips).

## Retired

| File | Reason |
| --- | --- |
| `jungle_border_16x9.png` | Opaque centre; does not suit the scrolling follow camera. Kept on disk, unused. |
| `jungle_vignette_16x9.png` | Replaced by `bush_edge.png` for exterior strips (stretching the full 16:9 frame crushed leaves). Kept on disk, unused. |
| `angkor_wall_top_view.jpg` → wall/fill | Isometric magenta cube baked pink diamonds; replaced by keyed `wall_block.jpg` / `wall_top.jpg` + foliage `jungle_fill.jpg`. |
| `Assets/Art/Gameplay/Generated/*` | Procedural placeholders; fallback only. |

## Generation prompts (summary)

| Source | Prompt essentials |
| --- | --- |
| `explorer_walk_*.jpg` | Same explorer as `explorer_*.jpg` (brown fedora, tan shirt, backpack, brown boots). 2×2 grid of four walk frames, identical scale and baseline, full body, centred in each quadrant, flat magenta background, no shadows, no text. |
| `angkor_floor_tile.png` | 1024×1024 rounded sandy floor slab with cracks and corner moss; inset + seamless → `floor_stone` / `floor_moss`. |
| `angkor_wall_top_view.jpg` | 1024×1024 oblique **top-of-wall** sheet (slab tops, vines, moss). Walls/deep fill only; one-cell caps in `GridBoard.SpawnWall`. |
| `jungle_vignette_16x9.jpg` | 16:9 frame of jungle leaves and vines hugging all four edges, flat magenta centre filling ~80% of the image. |
| `exit_arrow.jpg` | Chunky gold arrow pointing right with dark outline, flat magenta background. |
