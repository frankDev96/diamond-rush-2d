"""Render individual high-poly Home hero elements for refinement.

Each mesh is saved into BlenderStudio/HomeHero.blend and rendered alone into
BlenderStudio/renders/Home/elements/. Approve pieces here before --install.

  /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \\
    --python .agents/skills/diamond-rush-blender/scripts/build_home_elements.py -- --eevee
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler

# Reuse builders from the home screen script.
SCRIPTS = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPTS))
import build_home_screen as home  # noqa: E402


def isolate_and_render(scene, cam, col, path, engine_name, size=512):
    for other in bpy.data.collections:
        if other == col:
            home.set_hide(other, False)
        elif other.name.startswith("Elem"):
            home.set_hide(other, True)
    lights = bpy.data.collections.get("HomeLights")
    if lights is not None:
        home.set_hide(lights, False)
    scene.camera = cam
    home.configure_render(scene, size, size, True, engine_name)
    path.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    print("Element", path.name)


def frame_collection(col, cam, pad=1.35):
    coords = []
    for obj in col.objects:
        for corner in obj.bound_box:
            coords.append(obj.matrix_world @ __import__("mathutils").Vector(corner))
    if not coords:
        return
    min_v = coords[0].copy()
    max_v = coords[0].copy()
    for v in coords[1:]:
        min_v.x = min(min_v.x, v.x)
        min_v.y = min(min_v.y, v.y)
        min_v.z = min(min_v.z, v.z)
        max_v.x = max(max_v.x, v.x)
        max_v.y = max(max_v.y, v.y)
        max_v.z = max(max_v.z, v.z)
    center = (min_v + max_v) * 0.5
    size = max(max_v.x - min_v.x, max_v.y - min_v.y, max_v.z - min_v.z, 0.5)
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = size * pad
    cam.location = (center.x, center.y - size * 2.2, center.z + size * 0.35)
    cam.rotation_euler = Euler((math.radians(78), 0, 0))


def main():
    raw = sys.argv
    extra = raw[raw.index("--") + 1 :] if "--" in raw else []
    args = home.parse_args(extra)

    home.clear_scene()
    scene = bpy.context.scene
    engine_name = home.prefer_engine(scene, prefer_cycles=not args["eevee"])
    home.setup_world(scene)
    mats = home.make_materials()
    lights = home.collection("HomeLights")
    home.setup_lights(lights)

    out = home.PREVIEW_DIR / "elements"
    out.mkdir(parents=True, exist_ok=True)
    cam = home.make_camera("ElemCam", (0, -5, 2), (78, 0, 0), ortho=True, ortho_scale=4)

    # --- Pillar ---
    pillar = home.collection("ElemPillar")
    shaft = home.new_cylinder("Pillar", 0.55, 4.2, (0, 0, 2.1), mats["stone"], pillar, 64)
    home.displace_noise(shaft, 0.01, 1.4, "PillarDisp")
    home.new_cylinder("Capital", 0.7, 0.3, (0, 0, 4.15), mats["stone_warm"], pillar, 48)
    home.new_cylinder("Base", 0.72, 0.35, (0, 0, 0.1), mats["stone_warm"], pillar, 48)
    home.new_torus("GoldTop", 0.62, 0.06, (0, 0, 3.75), mats["gold"], pillar)
    home.new_torus("GoldBot", 0.64, 0.07, (0, 0, 0.5), mats["gold"], pillar)
    frame_collection(pillar, cam)
    isolate_and_render(scene, cam, pillar, out / "pillar.png", engine_name)

    # --- Torch ---
    torch = home.collection("ElemTorch")
    home.new_cylinder("Stick", 0.07, 0.9, (0, 0, 0.7), mats["torch_wood"], torch, 24)
    home.new_uvsphere("Flame", 0.22, (0, 0, 1.35), mats["flame"], torch, 32, 16, scale=(0.7, 0.7, 1.4))
    frame_collection(torch, cam, 1.6)
    isolate_and_render(scene, cam, torch, out / "torch.png", engine_name)

    # --- Floor slab ---
    floor = home.collection("ElemFloor")
    slab = home.new_cube("FloorSlab", (1.1, 1.1, 0.14), (0, 0, 0.07), mats["path"], floor)
    home.displace_noise(slab, 0.02, 3.0, "FloorDisp")
    frame_collection(floor, cam, 1.8)
    isolate_and_render(scene, cam, floor, out / "floor_slab.png", engine_name)

    # --- Leaf ---
    leaf = home.collection("ElemLeaf")
    home.leaf_card("Leaf", (0, 0, 0.2), mats["leaf_a"], leaf, size=1.2, rot=(55, 0, 20))
    frame_collection(leaf, cam, 1.7)
    isolate_and_render(scene, cam, leaf, out / "leaf.png", engine_name)

    # --- Arch stone ---
    arch = home.collection("ElemArch")
    home.new_cube("ArchStone", (1.2, 0.9, 0.55), (0, 0, 0.3), mats["stone"], arch)
    home.new_cube("Ornament", (0.4, 0.4, 0.35), (0, -0.2, 0.7), mats["gold"], arch)
    frame_collection(arch, cam, 1.7)
    isolate_and_render(scene, cam, arch, out / "arch_stone.png", engine_name)

    # --- Crystal cluster piece ---
    crystal = home.collection("ElemCrystal")
    home.crystal("Crystal", 1.6, 0.3, (0, 0, 0.8), mats["pink"], crystal, sides=8)
    frame_collection(crystal, cam, 1.7)
    isolate_and_render(scene, cam, crystal, out / "crystal_pink.png", engine_name)

    home.BLEND.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(home.BLEND))
    print("Saved", home.BLEND)
    print("Element previews in", out)


if __name__ == "__main__":
    main()
    sys.exit(0)
