"""Create or refresh BlenderStudio/DiamondRush_Oblique.blend.

Run with Blender, not system Python:

  /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \\
    --python .agents/skills/diamond-rush-blender/scripts/setup_oblique_scene.py
"""

import math
import sys
from pathlib import Path

import bpy


def repo_root():
    here = Path(__file__).resolve()
    for parent in here.parents:
        if (parent / "Assets").is_dir() and (parent / "ProjectSettings").is_dir():
            return parent
    raise SystemExit("DiamondRush repo root not found from " + str(here))


def blend_path():
    return repo_root() / "BlenderStudio" / "DiamondRush_Oblique.blend"


def load_or_start_empty(path):
    if path.is_file() and path.stat().st_size > 16:
        bpy.ops.wm.open_mainfile(filepath=str(path))
        return
    bpy.ops.wm.read_factory_settings(use_empty=True)


def prefer_eevee(scene):
    engine = scene.render.bl_rna.properties["engine"]
    identifiers = [item.identifier for item in engine.enum_items]
    for name in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
        if name in identifiers:
            scene.render.engine = name
            return


def ensure_camera(scene):
    camera = bpy.data.objects.get("Camera")
    if camera is None or camera.type != "CAMERA":
        data = bpy.data.cameras.new("ObliqueCamera")
        camera = bpy.data.objects.new("Camera", data)
        scene.collection.objects.link(camera)
    data = camera.data
    data.type = "ORTHO"
    data.ortho_scale = 2.0
    data.clip_start = 0.1
    data.clip_end = 100.0
    camera.location = (0.0, -5.0, 5.0)
    camera.rotation_euler = (math.radians(45.0), 0.0, 0.0)
    scene.camera = camera
    return camera


def ensure_sun(scene):
    light = bpy.data.objects.get("Key")
    if light is None or light.type != "LIGHT":
        data = bpy.data.lights.new("Key", "SUN")
        light = bpy.data.objects.new("Key", data)
        scene.collection.objects.link(light)
    light.data.energy = 3.0
    light.rotation_euler = (math.radians(50.0), math.radians(-20.0), 0.0)


def ensure_cell_origin(scene):
    empty = bpy.data.objects.get("CellOrigin")
    if empty is None:
        empty = bpy.data.objects.new("CellOrigin", None)
        scene.collection.objects.link(empty)
    empty.empty_display_type = "CUBE"
    empty.empty_display_size = 0.5
    empty.location = (0.0, 0.0, 0.0)
    empty.hide_render = True


def lock_render(scene):
    prefer_eevee(scene)
    scene.render.resolution_x = 128
    scene.render.resolution_y = 128
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.render.use_file_extension = True
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    try:
        scene.view_settings.view_transform = "Standard"
    except (TypeError, ValueError):
        pass


def main():
    path = blend_path()
    path.parent.mkdir(parents=True, exist_ok=True)
    load_or_start_empty(path)
    scene = bpy.context.scene
    ensure_camera(scene)
    ensure_sun(scene)
    ensure_cell_origin(scene)
    lock_render(scene)
    bpy.ops.wm.save_as_mainfile(filepath=str(path))
    camera = scene.camera
    print(
        "Saved oblique blend:",
        path,
        "camera",
        tuple(round(v, 3) for v in camera.location),
        "rotation_deg",
        tuple(round(math.degrees(v), 3) for v in camera.rotation_euler),
        "ortho_scale",
        camera.data.ortho_scale,
        "transparent",
        scene.render.film_transparent,
    )


if __name__ == "__main__":
    main()
    sys.exit(0)
