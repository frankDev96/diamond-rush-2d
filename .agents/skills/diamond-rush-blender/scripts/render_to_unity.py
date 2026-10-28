"""Render the open oblique blend into Assets/Art/Gameplay/Angkor.

The .blend must already be open (Blender loads it before --python):

  /Applications/Blender.app/Contents/MacOS/Blender \\
    BlenderStudio/DiamondRush_Oblique.blend --background \\
    --python .agents/skills/diamond-rush-blender/scripts/render_to_unity.py -- \\
    --name boulder_face --resolution 128

`name` is the gameplay filename stem from docs/GAMEPLAY_ASSETS.md.
"""

import argparse
import os
import sys

import bpy

OUTPUT_DIR = "//../Assets/Art/Gameplay/Angkor/"


def render_oblique_asset_to_unity(asset_name, resolution=128, width=None, height=None):
    if not asset_name or not str(asset_name).strip():
        raise SystemExit("asset_name is required")
    stem = os.path.splitext(os.path.basename(str(asset_name).strip()))[0]
    if stem != stem.lower() or " " in stem:
        raise SystemExit(
            "Use the gameplay stem from docs/GAMEPLAY_ASSETS.md, for example boulder_face"
        )

    scene = bpy.context.scene
    if scene.camera is None:
        raise SystemExit("No camera. Run setup_oblique_scene.py first.")

    scene.render.resolution_x = int(width or resolution)
    scene.render.resolution_y = int(height or resolution)
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.render.use_file_extension = True

    unity_sprite_dir = bpy.path.abspath(OUTPUT_DIR)
    os.makedirs(unity_sprite_dir, exist_ok=True)
    output_path = os.path.join(unity_sprite_dir, stem + ".png")
    scene.render.filepath = output_path
    bpy.ops.render.render(write_still=True)
    print("Rendered 2D oblique sprite to Unity:", output_path)
    return output_path


def main(argv=None):
    parser = argparse.ArgumentParser(description="Render a Diamond Rush oblique sprite")
    parser.add_argument("--name", required=True, help="File stem, such as boulder_face")
    parser.add_argument("--resolution", type=int, default=128, help="Square frame, in pixels")
    parser.add_argument("--width", type=int, help="Overrides --resolution width")
    parser.add_argument("--height", type=int, help="Overrides --resolution height")
    args = parser.parse_args(argv)
    render_oblique_asset_to_unity(args.name, args.resolution, args.width, args.height)


if __name__ == "__main__":
    raw = sys.argv
    extra = raw[raw.index("--") + 1 :] if "--" in raw else []
    main(extra)
