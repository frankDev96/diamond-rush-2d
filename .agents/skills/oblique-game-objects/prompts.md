# Object prompts

Use the oblique camera from `oblique-hybrid-view`. One object, one PNG, transparent film. No text. No baked floor shadow. No isometric yaw.

## Red diamond

Build a single red diamond pickup on `CellOrigin`.

Camera, unchanged: orthographic, location `(0, -5, 5)`, rotation `(45°, 0°, 0°)`, orthographic scale `2`, transparent film.

Geometry:

- Cartoon brilliant cut with a few large facets: flat table, crown, girdle, pavilion ending in a point.
- About `0.55` units wide and `0.42` units tall. The point sits `0.02` above `Z = 0`, inside one cell.
- Turn the gem about `8°` on Z so one crown facet faces the camera. Leave the camera Z at `0°` so the cell stays square.

Material:

- Ruby body near `(0.75, 0.05, 0.08)`, darker pavilion, one hard white highlight on the table.
- Thick dark outline that still reads at 128 pixels.
- No floor plane, no shadow catcher, no caption.

Export:

- `128×128` RGBA PNG.
- Diamond Rush stem `red_diamond`. In game it is about `0.7` cells wide, centre pivot, sort order `7`. It also fills the exit-door sockets at a smaller size.

## Purple diamond

Duplicate the red diamond mesh and camera. Recolor the body to violet near `(0.45, 0.15, 0.75)` with a white table highlight. Use it for the purple currency gem only. Do not create a blue diamond.

## Face boulder

Build one pushable block on `CellOrigin`, about `0.92` cells wide and tall.

- Beige crumbling stone, thick dark outline.
- A carved face on the south front (eyes and mouth) so the 45° camera shows that face, plus the top of the block.
- Flat enough to occupy one cell. It must not read as a tall statue.
- Stem `boulder_face`, `128×128`, centre pivot.

The face may look like a stone mask or skull. It stays a grid boulder: it pushes, falls, and crushes. It is not a separate enemy type.

## Green snake

Build one bright green snake coiled on the cell, head raised toward the camera, thick dark outline, no pattern that becomes noise at 128 pixels. Stem `snake_green`, `128×128`, centre pivot. It reads as a patrolling ground hazard, not a side-view sprite.

## Chest

Build a small wooden chest, lid closed, front latch visible from the south, about `0.8` cells. Thick outline. Stem `chest_wood`. A secret chest uses the same form with a darker band: stem `chest_secret`. Both are `128×128`, centre pivot. Opening the lid is a second render only when a level needs an open pose.

## Soft dirt

Build a `1×1` mound of soft dug earth sitting on the stone floor color, slightly rounded, darker than the sandy slab, thick outline, seamless enough to sit on any floor cell. Stem `dirt_soft`, `128×128`.

## Checkpoint pad

Build a flat golden rune circle inset in the floor, glowing warm yellow, no letters, about `0.9` cells wide. The 45° camera shows the ring as a squat oval on the square cell, not as a vertical coin. Stem `checkpoint_pad`, `128×128`.

## Fire and spear

- Fire: a short flame column on a brazier base, warm orange, contained in one cell. Stem `trap_fire`.
- Spear: a stone trap housing with a metal spear pointing across the cell, side visible from the south. Stem `trap_spear`.

Both are `128×128`, centre pivot, thick outlines.
