# Floor, wall, avatar, and level prompts

Camera for every prompt: orthographic, location `(0, -5, 5)`, rotation `(45°, 0°, 0°)`, scale `2` for one cell, transparent film. Z stays `0°`.

## Floor

Build one `1×1` floor slab on `CellOrigin`, about `0.08` units thick.

- Sandy gray temple stone, cracks, a little corner moss. Thick dark outline, no border frame.
- The 45° camera shows the top face and a thin south edge. The cell stays square.
- Edges tile: the north edge matches the south edge, and the east edge matches the west edge.
- `128×128`, centre pivot. Diamond Rush stems: `floor_stone`, with mossier and cracked variants `floor_moss` and `floor_cracked`.

## Wall

Build one wall cap that occupies a single cell.

- Crumbling gray stone, vines, moss. Thick outline.
- Show the top surface and a short south front lip. Crop or model it so the sprite is one cell, not a two-storey wall.
- Left and right edges tile for corridors.
- `128×128`, centre pivot. Stems: `wall_block` where the open floor is south, `wall_top` for the north rim and inner mass. Deep solid fill is `jungle_fill` at `256×256`.

## Avatar

Build the explorer standing upright on `CellOrigin`: brown fedora, tan shirt, backpack, brown boots, thick dark outline.

- Feet on `Z = 0`. Body about two units tall before the game scales it.
- Four facings only. Toward the camera, away from the camera, and to the right. Do not model a diagonal stance.
- Walk sheets are separate: contact, upright passing, contact, passing. Say "wide stride" and "upright passing pose" so the frames do not all look mid-step.
- Diamond Rush frames are `160×256`, feet pivot `(0.5, 0.04)`, stems `explorer_walk_down_0..3`, `explorer_walk_up_0..3`, `explorer_walk_side_0..3`. Left play mirrors the side sheet.
- Render with `--width 160 --height 256`. Keep orthographic scale `2`.

## Level kit

Do not render a finished stage as one image.

1. Render floor, wall, avatar, and the objects the stage is allowed to use, each as its own PNG.
2. Lay them on a square grid in the game's level format. One cell is one unit. Movement is up, down, left, and right.
3. For Diamond Rush, follow the World 1 band in `docs/GAME_PRD.md` and the symbols in the gameplay pipeline (`#` stone, `.` floor, `d` dirt, `r` red, `B` boulder, `S` snake, `P` spawn, `E` exit).
4. Check the oblique read: wall lips face south, the avatar stands, diamonds sit on the floor, and no piece has turned the grid into diamonds.
