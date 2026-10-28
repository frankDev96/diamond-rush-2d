"""Stabilize biped walk cycles for a 2D top-down oblique hybrid game.

Blender 4.x / 5.x (bpy). Fixes sprite flicker from:
  - frame-to-frame center-of-mass drift
  - uneven root motion / stride translation
  - inconsistent feet pivots across facings (especially up/down)
  - harsh self-shadows under rotating lights

Diamond Rush camera contract (oblique-hybrid-view):
  Orthographic, pitch 45°, yaw 0°, location (0, -5, 5), film transparent.
  Walk frames: 160×256, feet pivot (0.5, 0.04), four poses per facing.

Run inside Blender (Text Editor → Run Script) or CLI:

  /Applications/Blender.app/Contents/MacOS/Blender \\
    BlenderStudio/DiamondRush_Oblique.blend --background \\
    --python .agents/skills/diamond-rush-blender/scripts/stabilize_walk_cycle.py -- \\
    --armature Armature --mesh Explorer --facing down \\
    --frames 24 --out BlenderStudio/renders/Walk

Or from BlenderStudio/scripts/stabilize_walk_cycle.py (launcher).
"""

from __future__ import annotations

import argparse
import math
import os
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Optional, Sequence, Tuple

import bpy
from mathutils import Matrix, Vector


# ---------------------------------------------------------------------------
# Tunables (CLI overrides these)
# ---------------------------------------------------------------------------

DEFAULTS = {
    "frame_count": 24,
    "fps": 24,
    "elevation_deg": 45.0,  # Diamond Rush lock; 30–45 is valid for other projects
    "ortho_scale": 2.4,  # room for full body + padding
    "frame_w": 160,
    "frame_h": 256,
    "padding_px": 8,
    "feet_pivot_y": 0.04,  # Unity Sprite pivot: (0.5, 0.04)
    "pelvis_bob_amp": 0.012,  # metres; keep tiny on up/down facings
    "pelvis_bone_candidates": (
        "hips",
        "pelvis",
        "hip",
        "root",
        "spine",
        "mixamorig:Hips",
        "Hips",
        "Pelvis",
    ),
    "foot_bone_candidates": (
        "foot.L",
        "foot.R",
        "Foot.L",
        "Foot.R",
        "mixamorig:LeftFoot",
        "mixamorig:RightFoot",
        "LeftFoot",
        "RightFoot",
    ),
}


FACING_YAW = {
    "down": 0.0,  # toward camera (south)
    "up": 180.0,  # away
    "side": -90.0,  # facing screen-right; engine mirrors for left
    "right": -90.0,
    "left": 90.0,
}


# ---------------------------------------------------------------------------
# Repo / output helpers
# ---------------------------------------------------------------------------

def repo_root() -> Path:
    here = Path(__file__).resolve()
    for parent in here.parents:
        if (parent / "Assets").is_dir() and (parent / "ProjectSettings").is_dir():
            return parent
    # Fallback: blend file directory parents
    blend = Path(bpy.data.filepath) if bpy.data.filepath else Path.cwd()
    for parent in [blend, *blend.parents]:
        if (parent / "Assets").is_dir():
            return parent
    return Path.cwd()


def ensure_dir(path: Path) -> Path:
    path.mkdir(parents=True, exist_ok=True)
    return path


# ---------------------------------------------------------------------------
# Object / armature discovery
# ---------------------------------------------------------------------------

def find_object(name: Optional[str], types: Sequence[str]) -> Optional[bpy.types.Object]:
    if name:
        obj = bpy.data.objects.get(name)
        if obj is None:
            raise RuntimeError(f"Object '{name}' not found")
        if obj.type not in types:
            raise RuntimeError(f"Object '{name}' type is {obj.type}, expected {types}")
        return obj
    for obj in bpy.data.objects:
        if obj.type in types and not obj.hide_render:
            return obj
    return None


def find_bone(armature: bpy.types.Object, candidates: Iterable[str]) -> Optional[bpy.types.PoseBone]:
    pose = armature.pose
    lower = {b.name.lower(): b for b in pose.bones}
    for cand in candidates:
        if cand in pose.bones:
            return pose.bones[cand]
        if cand.lower() in lower:
            return lower[cand.lower()]
    # Heuristic: shortest bone name containing "hip" or "pelvis"
    for bone in pose.bones:
        n = bone.name.lower()
        if "hip" in n or "pelvis" in n:
            return bone
    return pose.bones[0] if pose.bones else None


def mesh_objects_for_armature(armature: bpy.types.Object) -> list[bpy.types.Object]:
    meshes = []
    for obj in bpy.data.objects:
        if obj.type != "MESH" or obj.hide_render:
            continue
        if obj.parent == armature:
            meshes.append(obj)
            continue
        for mod in obj.modifiers:
            if mod.type == "ARMATURE" and mod.object == armature:
                meshes.append(obj)
                break
    return meshes


# ---------------------------------------------------------------------------
# Scene: camera, lights, timeline
# ---------------------------------------------------------------------------

def prefer_eevee(scene: bpy.types.Scene) -> None:
    engine = scene.render.bl_rna.properties["engine"]
    identifiers = [item.identifier for item in engine.enum_items]
    for name in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
        if name in identifiers:
            scene.render.engine = name
            return


def ensure_cell_origin(scene: bpy.types.Scene) -> bpy.types.Object:
    empty = bpy.data.objects.get("WalkRoot")
    if empty is None:
        empty = bpy.data.objects.new("WalkRoot", None)
        scene.collection.objects.link(empty)
    empty.empty_display_type = "PLAIN_AXES"
    empty.empty_display_size = 0.35
    empty.location = (0.0, 0.0, 0.0)
    empty.rotation_euler = (0.0, 0.0, 0.0)
    empty.hide_render = True
    return empty


def lock_oblique_camera(
    scene: bpy.types.Scene,
    root: bpy.types.Object,
    elevation_deg: float,
    ortho_scale: float,
) -> bpy.types.Object:
    """Orthographic camera parented to WalkRoot so root registration stays locked."""
    cam = bpy.data.objects.get("WalkCamera")
    if cam is None or cam.type != "CAMERA":
        data = bpy.data.cameras.new("WalkCamera")
        cam = bpy.data.objects.new("WalkCamera", data)
        scene.collection.objects.link(cam)

    data = cam.data
    data.type = "ORTHO"
    data.ortho_scale = ortho_scale
    data.clip_start = 0.05
    data.clip_end = 200.0

    # Parent to WalkRoot: facing yaw turns the character; camera stays "south" of root.
    # Diamond Rush: world camera at (0,-5,5) with X=45°, Z=0° — same local offset.
    elev = math.radians(elevation_deg)
    dist = 5.0 * math.sqrt(2.0)
    cam.parent = None
    cam.location = (0.0, 0.0, 0.0)
    cam.rotation_euler = (0.0, 0.0, 0.0)
    cam.parent = root
    cam.matrix_parent_inverse = Matrix.Identity(4)
    cam.location = (0.0, -dist * math.cos(elev), dist * math.sin(elev))
    cam.rotation_euler = (elev, 0.0, 0.0)
    cam.scale = (1.0, 1.0, 1.0)

    scene.camera = cam
    return cam


def setup_flat_lighting(scene: bpy.types.Scene, root: bpy.types.Object) -> None:
    """Soft, locked lights parented to WalkRoot — no rotating self-shadow flicker."""

    def ensure_light(name: str, light_type: str, energy: float, loc, rot) -> bpy.types.Object:
        obj = bpy.data.objects.get(name)
        if obj is None or obj.type != "LIGHT":
            data = bpy.data.lights.new(name, light_type)
            obj = bpy.data.objects.new(name, data)
            scene.collection.objects.link(obj)
        obj.data.type = light_type
        obj.data.energy = energy
        if hasattr(obj.data, "angle"):
            obj.data.angle = math.radians(2.0)  # soft sun disc if supported
        if hasattr(obj.data, "use_shadow"):
            obj.data.use_shadow = False  # sprites: avoid frame-to-frame shadow crawl
        obj.parent = root
        obj.location = loc
        obj.rotation_euler = rot
        return obj

    ensure_light(
        "WalkKey",
        "SUN",
        2.2,
        (0.0, 0.0, 0.0),
        (math.radians(50.0), math.radians(-15.0), 0.0),
    )
    ensure_light(
        "WalkFill",
        "SUN",
        0.7,
        (0.0, 0.0, 0.0),
        (math.radians(70.0), math.radians(40.0), math.radians(180.0)),
    )
    # Optional world ambient so silhouettes stay readable when sun shadows are off
    world = scene.world or bpy.data.worlds.new("WalkWorld")
    scene.world = world
    world.use_nodes = True
    nodes = world.node_tree.nodes
    links = world.node_tree.links
    nodes.clear()
    bg = nodes.new("ShaderNodeBackground")
    bg.inputs[0].default_value = (0.08, 0.09, 0.10, 1.0)
    bg.inputs[1].default_value = 0.35
    out = nodes.new("ShaderNodeOutputWorld")
    links.new(bg.outputs[0], out.inputs[0])


def configure_timeline(scene: bpy.types.Scene, frame_count: int, fps: int) -> None:
    scene.render.fps = fps
    scene.frame_start = 1
    scene.frame_end = frame_count
    scene.frame_current = 1


def configure_render(
    scene: bpy.types.Scene,
    width: int,
    height: int,
    out_dir: Path,
    stem: str,
) -> None:
    prefer_eevee(scene)
    scene.render.resolution_x = width
    scene.render.resolution_y = height
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.render.image_settings.compression = 15
    # Disable motion blur / DOF flicker sources
    scene.render.use_motion_blur = False
    if hasattr(scene.eevee, "use_gtao"):
        scene.eevee.use_gtao = True
    if hasattr(scene.eevee, "use_shadows"):
        scene.eevee.use_shadows = False
    scene.render.filepath = str(out_dir / f"{stem}_")


# ---------------------------------------------------------------------------
# Animation: in-place root + symmetric pelvis bob
# ---------------------------------------------------------------------------

@dataclass
class PelvisSample:
    frame: int
    z: float


def action_or_nla_range(armature: bpy.types.Object) -> Tuple[int, int]:
    """Return (start, end) from active action or scene."""
    anim = armature.animation_data
    if anim and anim.action:
        action = anim.action
        try:
            # Blender 4.4+ Action.frame_range; older: fcurves
            fr = action.frame_range
            return int(fr[0]), int(fr[1])
        except Exception:
            pass
    scene = bpy.context.scene
    return scene.frame_start, scene.frame_end


def sample_pelvis_z(
    armature: bpy.types.Object,
    pelvis: bpy.types.PoseBone,
    frames: Sequence[int],
) -> list[PelvisSample]:
    depsgraph = bpy.context.evaluated_depsgraph_get()
    samples: list[PelvisSample] = []
    for f in frames:
        bpy.context.scene.frame_set(f)
        depsgraph.update()
        # World Z of pelvis head
        mw = armature.matrix_world @ pelvis.matrix
        samples.append(PelvisSample(f, mw.translation.z))
    return samples


def make_symmetric_bob(samples: list[PelvisSample], amp: float) -> dict[int, float]:
    """Map frame -> target pelvis world Z.

    Uses a single-cycle cosine bob around the mean, amplitude clamped to `amp`.
    Removes asymmetric hitch that reads as vertical jitter when the sprite
    translates in screen space (worst on up/down facings).
    """
    if not samples:
        return {}
    zs = [s.z for s in samples]
    mean_z = sum(zs) / len(zs)
    n = len(samples)
    out: dict[int, float] = {}
    for i, s in enumerate(samples):
        # Two peaks per cycle (classic walk); phase so contact ≈ trough
        phase = (i / n) * math.tau
        bob = -amp * abs(math.cos(phase))  # always downward from mean, symmetric
        out[s.frame] = mean_z + bob
    return out


def zero_root_xz_motion(armature: bpy.types.Object, frames: Sequence[int]) -> None:
    """Bake armature object XY (and keep Z) so walk is in-place at WalkRoot."""
    scene = bpy.context.scene
    # Clear object-level location keys on X/Y; keep authored Z if any
    if armature.animation_data and armature.animation_data.action:
        action = armature.animation_data.action
        for fc in list(action.fcurves):
            if fc.data_path == "location" and fc.array_index in (0, 1):
                action.fcurves.remove(fc)

    for f in frames:
        scene.frame_set(f)
        armature.location.x = 0.0
        armature.location.y = 0.0
        armature.keyframe_insert(data_path="location", index=0, frame=f)
        armature.keyframe_insert(data_path="location", index=1, frame=f)

    # Constant interpolation on those channels to avoid bezier overshoot
    if armature.animation_data and armature.animation_data.action:
        for fc in armature.animation_data.action.fcurves:
            if fc.data_path == "location" and fc.array_index in (0, 1):
                for kp in fc.keyframe_points:
                    kp.interpolation = "LINEAR"


def apply_pelvis_world_z(
    armature: bpy.types.Object,
    pelvis: bpy.types.PoseBone,
    targets: dict[int, float],
) -> None:
    """Drive pelvis bone so world Z matches target; leaves XY free for stride pose."""
    scene = bpy.context.scene
    for frame, target_z in targets.items():
        scene.frame_set(frame)
        # Current world matrix of pelvis
        mw = armature.matrix_world @ pelvis.matrix
        delta = target_z - mw.translation.z
        # Apply as pose location in bone local +Z approximately via armature space
        # Convert world delta Z into pose bone location offset
        parent_mw = armature.matrix_world
        if pelvis.parent:
            parent_mw = armature.matrix_world @ pelvis.parent.matrix
        # World +Z offset in parent space
        offset_world = Vector((0.0, 0.0, delta))
        local = parent_mw.inverted() @ (parent_mw.translation + offset_world) - (
            parent_mw.inverted() @ parent_mw.translation
        )
        # Simpler: add delta along armature world Z into pose bone location.z
        # after converting with bone rest matrix
        rest = armature.data.bones[pelvis.name].matrix_local
        # pose bone location is in bone rest local space
        inv_rest = rest.inverted()
        # World-space translation delta → bone local
        bone_delta = inv_rest.to_3x3().inverted() @ (
            armature.matrix_world.to_3x3().inverted() @ offset_world
        )
        pelvis.location = pelvis.location + bone_delta
        pelvis.keyframe_insert(data_path="location", frame=frame)


def ensure_loop_match(armature: bpy.types.Object, start: int, end: int) -> None:
    """Copy pose of start onto end so the cycle loops without a hitch frame."""
    scene = bpy.context.scene
    scene.frame_set(start)
    bpy.ops.pose.select_all(action="SELECT")
    bpy.ops.pose.copy()
    scene.frame_set(end)
    bpy.ops.pose.paste(flipped=False)
    bpy.ops.anim.keyframe_insert_menu(type="Location")
    # Prefer LocRot if available
    try:
        for pb in armature.pose.bones:
            pb.keyframe_insert(data_path="location", frame=end)
            pb.keyframe_insert(data_path="rotation_quaternion", frame=end)
            pb.keyframe_insert(data_path="rotation_euler", frame=end)
            pb.keyframe_insert(data_path="scale", frame=end)
    except RuntimeError:
        pass


# ---------------------------------------------------------------------------
# Bounds / pivot registration (the anti-flicker core)
# ---------------------------------------------------------------------------

@dataclass
class FrameBounds:
    frame: int
    # Camera-space AABB of the rendered mesh(es)
    min_c: Vector
    max_c: Vector
    # World-space feet contact (lowest world Z among meshes)
    feet_world: Vector
    com_world: Vector

    @property
    def center_c(self) -> Vector:
        return (self.min_c + self.max_c) * 0.5

    @property
    def size_c(self) -> Vector:
        return self.max_c - self.min_c


def camera_matrix_world(cam: bpy.types.Object) -> Matrix:
    return cam.matrix_world.copy()


def world_to_camera(cam: bpy.types.Object, p: Vector) -> Vector:
    return camera_matrix_world(cam).inverted() @ p


def iter_evaluated_mesh_verts(
    obj: bpy.types.Object,
    depsgraph: bpy.types.Depsgraph,
) -> Iterable[Vector]:
    eval_obj = obj.evaluated_get(depsgraph)
    me = eval_obj.to_mesh()
    try:
        mw = eval_obj.matrix_world
        for v in me.vertices:
            yield mw @ v.co
    finally:
        eval_obj.to_mesh_clear()


def measure_frame(
    frame: int,
    meshes: Sequence[bpy.types.Object],
    cam: bpy.types.Object,
) -> FrameBounds:
    depsgraph = bpy.context.evaluated_depsgraph_get()
    bpy.context.scene.frame_set(frame)
    depsgraph.update()

    pts_world: list[Vector] = []
    for mesh in meshes:
        pts_world.extend(iter_evaluated_mesh_verts(mesh, depsgraph))
    if not pts_world:
        z = Vector((0, 0, 0))
        return FrameBounds(frame, z, z, z, z)

    min_w = Vector((min(p.x for p in pts_world), min(p.y for p in pts_world), min(p.z for p in pts_world)))
    max_w = Vector((max(p.x for p in pts_world), max(p.y for p in pts_world), max(p.z for p in pts_world)))
    com = sum(pts_world, Vector((0, 0, 0))) / len(pts_world)
    # Feet: average of lowest 5% vertices (stable vs single outlier)
    sorted_z = sorted(pts_world, key=lambda p: p.z)
    n_feet = max(1, len(sorted_z) // 20)
    feet = sum(sorted_z[:n_feet], Vector((0, 0, 0))) / n_feet

    pts_c = [world_to_camera(cam, p) for p in pts_world]
    min_c = Vector((min(p.x for p in pts_c), min(p.y for p in pts_c), min(p.z for p in pts_c)))
    max_c = Vector((max(p.x for p in pts_c), max(p.y for p in pts_c), max(p.z for p in pts_c)))
    return FrameBounds(frame, min_c, max_c, feet, com)


def compute_master_registration(
    bounds: Sequence[FrameBounds],
) -> Tuple[Vector, Vector, float, float]:
    """Union AABB in camera space + mean feet XY in camera space.

    Returns (union_min, union_max, feet_x_c, feet_y_c) used as the locked pivot.
    """
    u_min = Vector(bounds[0].min_c)
    u_max = Vector(bounds[0].max_c)
    for b in bounds[1:]:
        u_min.x = min(u_min.x, b.min_c.x)
        u_min.y = min(u_min.y, b.min_c.y)
        u_min.z = min(u_min.z, b.min_c.z)
        u_max.x = max(u_max.x, b.max_c.x)
        u_max.y = max(u_max.y, b.max_c.y)
        u_max.z = max(u_max.z, b.max_c.z)

    # Mean feet projected to camera X/Y — locks ground contact across frames
    # (we re-measure feet in camera space)
    return u_min, u_max, 0.0, 0.0


def offset_object_in_camera_plane(
    obj: bpy.types.Object,
    cam: bpy.types.Object,
    delta_camera_xy: Vector,
) -> None:
    """Translate `obj` so its projection shifts by delta_camera_xy (z ignored)."""
    cam_mw = camera_matrix_world(cam)
    delta_world = cam_mw.to_3x3() @ Vector((delta_camera_xy.x, delta_camera_xy.y, 0.0))
    obj.location = obj.location + delta_world


def register_all_frames(
    armature: bpy.types.Object,
    cam: bpy.types.Object,
    meshes: Sequence[bpy.types.Object],
    frames: Sequence[int],
    feet_pivot_y: float,
) -> list[FrameBounds]:
    """Measure all frames, then keyframe armature XY offsets for locked feet pivot."""
    measured = [measure_frame(f, meshes, cam) for f in frames]
    u_min, u_max, _, _ = compute_master_registration(measured)

    # Mean feet camera X across cycle — kill lateral sway in the sprite
    feet_c_list = [world_to_camera(cam, b.feet_world) for b in measured]
    mean_feet_x = sum(p.x for p in feet_c_list) / len(feet_c_list)
    ground_y = min(p.y for p in feet_c_list)  # lowest camera-Y among feet (= "down" on screen for this cam)
    # For a 45° ortho looking from -Y+Z, camera +Y is typically "up the image".
    # Lock every frame's feet to (mean_feet_x, ground_y + tiny bob allowance).
    # Using the *minimum* ground_y keeps contact from floating; bob stays in mesh.

    target_x = mean_feet_x
    target_y = min(p.y for p in feet_c_list)

    # Also kill COM lateral drift relative to feet (backpack sway) by a light
    # blend toward feet — optional; we register on feet only for Diamond Rush.

    for b, feet_c in zip(measured, feet_c_list):
        bpy.context.scene.frame_set(b.frame)
        delta = Vector((target_x - feet_c.x, target_y - feet_c.y, 0.0))
        offset_object_in_camera_plane(armature, cam, delta)
        armature.keyframe_insert(data_path="location", frame=b.frame)

    # Linear keys — no bezier overshoot that reintroduces sub-pixel drift
    if armature.animation_data and armature.animation_data.action:
        for fc in armature.animation_data.action.fcurves:
            if fc.data_path == "location":
                for kp in fc.keyframe_points:
                    kp.interpolation = "LINEAR"

    # Re-measure after registration for report / sheet packing
    rem = [measure_frame(f, meshes, cam) for f in frames]
    # Store union on first element via attribute for exporters
    rem[0].min_c = rem[0].min_c  # noqa — keep mypy calm
    setattr(rem[0], "_union_min", u_min)
    setattr(rem[0], "_union_max", u_max)
    setattr(rem[0], "_feet_pivot_y", feet_pivot_y)
    return rem


# ---------------------------------------------------------------------------
# Export: individual PNGs + optional 2×2 sheet
# ---------------------------------------------------------------------------

def render_frame_sequence(
    scene: bpy.types.Scene,
    frames: Sequence[int],
    out_dir: Path,
    stem: str,
) -> list[Path]:
    paths: list[Path] = []
    for i, f in enumerate(frames):
        scene.frame_set(f)
        path = out_dir / f"{stem}_{i:02d}.png"
        scene.render.filepath = str(path.with_suffix(""))  # Blender adds .png
        # Blender appends frame numbers if # in path; use exact path:
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        # Some builds write path.png.png — normalize
        written = Path(scene.render.filepath)
        if not written.exists() and written.with_suffix(".png").exists():
            written = written.with_suffix(".png")
        if written.exists() and written.suffix != ".png":
            # Force rename if extension missing
            target = written if written.suffix == ".png" else Path(str(written) + ".png")
            if written.exists() and written != target:
                written.replace(target)
                written = target
        paths.append(written if written.exists() else path)
        print(f"[stabilize_walk] rendered {paths[-1].name}")
    return paths


def pack_2x2_sheet(
    frame_paths: Sequence[Path],
    sheet_path: Path,
    cell_w: int,
    cell_h: int,
) -> Path:
    """Pack first 4 frames into a 2×2 sheet (TL, TR, BL, BR) using Blender images."""
    if len(frame_paths) < 4:
        raise RuntimeError("Need at least 4 frames to pack a 2×2 walk sheet")

    sheet_w, sheet_h = cell_w * 2, cell_h * 2
    sheet = bpy.data.images.new("WalkSheet", width=sheet_w, height=sheet_h, alpha=True)
    pixels = [0.0] * (sheet_w * sheet_h * 4)

    order = [0, 1, 2, 3]  # contact, passing, contact, passing
    positions = [(0, 1), (1, 1), (0, 0), (1, 0)]  # TL TR BL BR in image space (Y up)

    for idx, (col, row) in zip(order, positions):
        img = bpy.data.images.load(str(frame_paths[idx]), check_existing=False)
        img.scale(cell_w, cell_h)
        src = list(img.pixels)
        for y in range(cell_h):
            for x in range(cell_w):
                si = (y * cell_w + x) * 4
                dx = col * cell_w + x
                dy = row * cell_h + y
                di = (dy * sheet_w + dx) * 4
                pixels[di : di + 4] = src[si : si + 4]
        bpy.data.images.remove(img)

    sheet.pixels = pixels
    sheet.filepath_raw = str(sheet_path)
    sheet.file_format = "PNG"
    sheet.save()
    print(f"[stabilize_walk] wrote sheet {sheet_path}")
    return sheet_path


def subsample_frames(total: int, count: int = 4) -> list[int]:
    """Pick `count` evenly spaced frames from 1..total (classic contact/pass)."""
    if total <= count:
        return list(range(1, total + 1))
    # contact, passing, opposite contact, passing
    idxs = [0, total // 4, total // 2, (3 * total) // 4]
    return [i + 1 for i in idxs]


# ---------------------------------------------------------------------------
# Facing
# ---------------------------------------------------------------------------

def set_facing(root: bpy.types.Object, facing: str) -> None:
    yaw = FACING_YAW.get(facing.lower())
    if yaw is None:
        raise RuntimeError(f"Unknown facing '{facing}'. Use: {list(FACING_YAW)}")
    root.rotation_euler = (0.0, 0.0, math.radians(yaw))


# ---------------------------------------------------------------------------
# Engine notes (printed + written beside PNGs)
# ---------------------------------------------------------------------------

ENGINE_NOTES = """
# Engine-side anti-flicker notes (Unity / Godot)

Flicker after a clean Blender export is almost always **sub-pixel sampling**
or **per-frame pivot drift**. Stabilize both.

## Unity (Diamond Rush)

1. Sprite import: 128 PPU, **no mipmaps**, Filter Mode Bilinear (or Point if
   you want crunchy pixels), Compression None, Mesh Type Full Rect.
2. Walk frames share pivot **(0.5, 0.04)** — feet. Never rebuild pivots per frame.
3. Movement: snap the **visual** to the pixel grid while physics stays smooth:

```csharp
// LateUpdate on the sprite child (not the Rigidbody/transform root)
var ppu = 128f;
var sp = transform.position;
transform.position = new Vector3(
    Mathf.Round(sp.x * ppu) / ppu,
    Mathf.Round(sp.y * ppu) / ppu,
    sp.z);
```

4. Camera: orthographic follow; optionally round camera position the same way
   so the character and world share one lattice.
5. Animator: avoid root-motion curves on the sprite; translate the capsule /
   grid agent only. Cycle offset 0; loop time on; transition duration 0 for
   direction swaps.
6. Sorting: keep `sortingOrder` stable (GridBoard). Do not animate Z to fake
   bob — bob is already in the sprite.

## Godot 4

1. `Sprite2D.texture_filter = TEXTURE_FILTER_NEAREST` (or Linear for soft).
2. Project Settings → Rendering → 2D → **Snap 2D Transforms to Pixel**
   and **Snap 2D Vertices to Pixel**.
3. Movement:

```gdscript
func _process(_dt):
    # visual child
    $Sprite.global_position = global_position.round()
```

4. Use `AnimationPlayer` or `AnimatedSprite2D` with identical offset on every
   frame; never per-frame `offset` keys.
5. Camera2D: enable **Position Smoothing** carefully; if jitter returns,
   disable smoothing and round `global_position` instead.

## Checklist

- [ ] All facings share the same canvas size and feet pivot.
- [ ] No baked ground shadow (shadows shift COM).
- [ ] Up/down sheets use the same ortho scale as side.
- [ ] Engine never re-centres the sprite by opaque bounds each frame.
"""


def write_engine_notes(out_dir: Path) -> None:
    path = out_dir / "ENGINE_SMOOTHING.md"
    path.write_text(ENGINE_NOTES.lstrip(), encoding="utf-8")
    print(f"[stabilize_walk] wrote {path}")


# ---------------------------------------------------------------------------
# Main pipeline
# ---------------------------------------------------------------------------

def parse_args(argv: Sequence[str]) -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Stabilize oblique walk cycle renders")
    p.add_argument("--armature", default=None, help="Armature object name")
    p.add_argument("--mesh", default=None, help="Optional primary mesh name")
    p.add_argument("--facing", default="down", choices=sorted(set(FACING_YAW)))
    p.add_argument("--frames", type=int, default=DEFAULTS["frame_count"])
    p.add_argument("--fps", type=int, default=DEFAULTS["fps"])
    p.add_argument("--elevation", type=float, default=DEFAULTS["elevation_deg"])
    p.add_argument("--ortho-scale", type=float, default=DEFAULTS["ortho_scale"])
    p.add_argument("--width", type=int, default=DEFAULTS["frame_w"])
    p.add_argument("--height", type=int, default=DEFAULTS["frame_h"])
    p.add_argument("--padding", type=int, default=DEFAULTS["padding_px"])
    p.add_argument("--pelvis-bob", type=float, default=DEFAULTS["pelvis_bob_amp"])
    p.add_argument("--sheet", action="store_true", help="Also pack first 4 frames to 2x2 sheet")
    p.add_argument("--skip-anim-fix", action="store_true", help="Only register+render existing action")
    p.add_argument(
        "--out",
        default=None,
        help="Output directory (default: BlenderStudio/renders/Walk/<facing>)",
    )
    p.add_argument("--stem", default=None, help="Filename stem (default: explorer_walk_<facing>)")
    # Blender passes args after `--`
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = []
    return p.parse_args(argv)


def run(argv: Optional[Sequence[str]] = None) -> None:
    args = parse_args(list(argv if argv is not None else sys.argv))
    scene = bpy.context.scene

    armature = find_object(args.armature, ("ARMATURE",))
    if armature is None:
        raise RuntimeError(
            "No Armature found. Pass --armature Name, or import a biped first."
        )

    meshes = mesh_objects_for_armature(armature)
    if args.mesh:
        primary = find_object(args.mesh, ("MESH",))
        if primary and primary not in meshes:
            meshes.insert(0, primary)
    if not meshes:
        raise RuntimeError("No mesh bound to the armature (parent or Armature modifier).")

    pelvis = find_bone(armature, DEFAULTS["pelvis_bone_candidates"])
    if pelvis is None:
        raise RuntimeError("Could not find a pelvis/hips bone on the armature.")

    root = ensure_cell_origin(scene)
    # Parent armature under WalkRoot for facing rotates
    if armature.parent != root:
        mw = armature.matrix_world.copy()
        armature.parent = root
        armature.matrix_world = mw

    set_facing(root, args.facing)
    cam = lock_oblique_camera(scene, root, args.elevation, args.ortho_scale)
    setup_flat_lighting(scene, root)
    configure_timeline(scene, args.frames, args.fps)

    # Ensure pose mode ops have context
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)

    frames = list(range(1, args.frames + 1))

    if not args.skip_anim_fix:
        # Stretch / clamp scene to requested length if action exists
        a0, a1 = action_or_nla_range(armature)
        print(f"[stabilize_walk] source action range {a0}..{a1}, using {frames[0]}..{frames[-1]}")

        zero_root_xz_motion(armature, frames)

        samples = sample_pelvis_z(armature, pelvis, frames)
        # Stronger bob clamp on vertical facings (up/down) — screen Y = world depth
        bob_amp = args.pelvis_bob
        if args.facing in ("up", "down"):
            bob_amp *= 0.55
        targets = make_symmetric_bob(samples, bob_amp)
        apply_pelvis_world_z(armature, pelvis, targets)

        # Loop hitch: frame_end matches frame_start
        try:
            bpy.ops.object.mode_set(mode="POSE")
            ensure_loop_match(armature, frames[0], frames[-1])
            bpy.ops.object.mode_set(mode="OBJECT")
        except Exception as exc:  # noqa: BLE001 — pose copy may fail without keys
            print(f"[stabilize_walk] loop match skipped: {exc}")
            try:
                bpy.ops.object.mode_set(mode="OBJECT")
            except Exception:
                pass

    print("[stabilize_walk] registering pivots across frames…")
    register_all_frames(
        armature,
        cam,
        meshes,
        frames,
        DEFAULTS["feet_pivot_y"],
    )

    out_dir = Path(args.out) if args.out else (
        repo_root() / "BlenderStudio" / "renders" / "Walk" / args.facing
    )
    ensure_dir(out_dir)
    stem = args.stem or f"explorer_walk_{args.facing}"

    configure_render(scene, args.width, args.height, out_dir, stem)

    # Render every frame, then also the 4-pose subsample sheet for Diamond Rush
    all_paths = render_frame_sequence(scene, frames, out_dir, stem)

    sample_idx = subsample_frames(args.frames, 4)
    # Map frame numbers to paths (frame f → index f-1)
    sample_paths = [all_paths[f - 1] for f in sample_idx if 0 <= f - 1 < len(all_paths)]
    for i, src in enumerate(sample_paths):
        dest = out_dir / f"{stem}_pose_{i}.png"
        if src.exists():
            dest.write_bytes(src.read_bytes())

    if args.sheet and len(sample_paths) >= 4:
        pack_2x2_sheet(
            sample_paths,
            out_dir / f"{stem}_sheet.png",
            args.width,
            args.height,
        )

    write_engine_notes(out_dir)

    # Drift report
    rem = [measure_frame(f, meshes, cam) for f in frames]
    feet_xs = [world_to_camera(cam, b.feet_world).x for b in rem]
    feet_ys = [world_to_camera(cam, b.feet_world).y for b in rem]
    print(
        "[stabilize_walk] feet camera-space drift "
        f"X={max(feet_xs) - min(feet_xs):.6f} "
        f"Y={max(feet_ys) - min(feet_ys):.6f} "
        "(should be ~0 after registration)"
    )
    print(f"[stabilize_walk] done → {out_dir}")


if __name__ == "__main__":
    run()
