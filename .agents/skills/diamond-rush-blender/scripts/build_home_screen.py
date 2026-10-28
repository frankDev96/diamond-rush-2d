"""High-detail Diamond Rush Home hero scene (Blender).

Builds refined meshes for temple, explorer, gems, and logo. Renders stay in
BlenderStudio/renders/Home/ until approved. Pass --install only after the
preview resembles the painted Home mockup.

  /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \\
    --python .agents/skills/diamond-rush-blender/scripts/build_home_screen.py
"""

from __future__ import annotations

import math
import random
import sys
from pathlib import Path

import bpy
from mathutils import Euler, Vector, noise


def repo_root() -> Path:
    here = Path(__file__).resolve()
    for parent in here.parents:
        if (parent / "Assets").is_dir() and (parent / "ProjectSettings").is_dir():
            return parent
    raise SystemExit("DiamondRush repo root not found from " + str(here))


ROOT = repo_root()
BLEND = ROOT / "BlenderStudio" / "HomeHero.blend"
OUT_DIR = ROOT / "Assets" / "Art" / "Home"
PREVIEW_DIR = ROOT / "BlenderStudio" / "renders" / "Home"
RNG = random.Random(42)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def prefer_engine(scene, prefer_cycles=True):
    engines = [i.identifier for i in scene.render.bl_rna.properties["engine"].enum_items]
    if prefer_cycles and "CYCLES" in engines:
        scene.render.engine = "CYCLES"
        scene.cycles.samples = 64
        scene.cycles.use_denoising = True
        try:
            scene.cycles.device = "GPU"
        except Exception:
            scene.cycles.device = "CPU"
        return "CYCLES"
    for name in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
        if name in engines:
            scene.render.engine = name
            return name
    return scene.render.engine


def collection(name):
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    return col


def link(obj, col):
    col.objects.link(obj)
    return obj


def set_hide(col, hide):
    if col is None:
        return
    for obj in col.objects:
        obj.hide_render = hide
        obj.hide_viewport = hide


def select_only(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def apply_all(obj):
    select_only(obj)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.select_set(False)


def shade_smooth(obj, auto_smooth=True, angle=40):
    # Prefer mesh API so objects linked only to a child collection still work.
    mesh = getattr(obj, "data", None)
    if mesh is not None and hasattr(mesh, "polygons"):
        for poly in mesh.polygons:
            poly.use_smooth = True
        if hasattr(mesh, "use_auto_smooth"):
            mesh.use_auto_smooth = auto_smooth
            mesh.auto_smooth_angle = math.radians(angle)
        return
    try:
        select_only(obj)
        bpy.ops.object.shade_smooth()
        obj.select_set(False)
    except RuntimeError:
        pass


def subdivide(obj, levels=2, render_levels=None):
    mod = obj.modifiers.new("Subsurf", "SUBSURF")
    mod.levels = levels
    mod.render_levels = render_levels if render_levels is not None else max(levels, 2)
    return mod


def bevel(obj, width=0.02, segments=3):
    mod = obj.modifiers.new("Bevel", "BEVEL")
    mod.width = width
    mod.segments = segments
    mod.limit_method = "ANGLE"
    mod.angle_limit = math.radians(30)
    return mod


def displace_noise(obj, strength=0.04, scale=2.5, name="Disp"):
    tex = bpy.data.textures.new(name + "Tex", type="CLOUDS")
    tex.noise_scale = scale
    mod = obj.modifiers.new(name, "DISPLACE")
    mod.texture = tex
    mod.strength = strength
    mod.mid_level = 0.5
    return mod


def assign(obj, material):
    if obj.data.materials:
        obj.data.materials[0] = material
    else:
        obj.data.materials.append(material)


def principled(
    name,
    color,
    roughness=0.5,
    metallic=0.0,
    specular=0.5,
    transmission=0.0,
    ior=1.45,
    emission=None,
    emission_strength=0.0,
    subsurface=0.0,
):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nodes = nt.nodes
    links = nt.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.location = (0, 0)
    out.location = (300, 0)

    def set_in(key, value):
        if key in bsdf.inputs:
            bsdf.inputs[key].default_value = value

    set_in("Base Color", (*color, 1.0))
    set_in("Roughness", roughness)
    set_in("Metallic", metallic)
    if "Specular IOR Level" in bsdf.inputs:
        set_in("Specular IOR Level", specular)
    elif "Specular" in bsdf.inputs:
        set_in("Specular", specular)
    if transmission > 0:
        if "Transmission Weight" in bsdf.inputs:
            set_in("Transmission Weight", transmission)
        elif "Transmission" in bsdf.inputs:
            set_in("Transmission", transmission)
        set_in("IOR", ior)
    if subsurface > 0:
        if "Subsurface Weight" in bsdf.inputs:
            set_in("Subsurface Weight", subsurface)
        elif "Subsurface" in bsdf.inputs:
            set_in("Subsurface", subsurface)
        if "Subsurface Radius" in bsdf.inputs:
            set_in("Subsurface Radius", (1.0, 0.4, 0.2))
    if emission is not None:
        if "Emission Color" in bsdf.inputs:
            set_in("Emission Color", (*emission, 1.0))
        elif "Emission" in bsdf.inputs:
            set_in("Emission", (*emission, 1.0))
        set_in("Emission Strength", emission_strength)

    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat, bsdf, nodes, links


def stone_mat(name, base=(0.42, 0.40, 0.38), variation=0.12):
    mat, bsdf, nodes, links = principled(name, base, roughness=0.78)
    # Procedural color variation
    tex = nodes.new("ShaderNodeTexNoise")
    tex.inputs["Scale"].default_value = 8.0
    tex.inputs["Detail"].default_value = 8.0
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.35
    ramp.color_ramp.elements[0].color = (
        max(0, base[0] - variation),
        max(0, base[1] - variation),
        max(0, base[2] - variation),
        1,
    )
    ramp.color_ramp.elements[1].position = 0.7
    ramp.color_ramp.elements[1].color = (
        min(1, base[0] + variation),
        min(1, base[1] + variation * 0.8),
        min(1, base[2] + variation * 0.6),
        1,
    )
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.25
    bump.inputs["Distance"].default_value = 0.08
    coord = nodes.new("ShaderNodeTexCoord")
    links.new(coord.outputs["Object"], tex.inputs["Vector"])
    links.new(tex.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(tex.outputs["Fac"], bump.inputs["Height"])
    if "Normal" in bsdf.inputs:
        links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def leaf_mat(name, color=(0.18, 0.48, 0.12)):
    mat, bsdf, nodes, links = principled(name, color, roughness=0.55)
    tex = nodes.new("ShaderNodeTexNoise")
    tex.inputs["Scale"].default_value = 18.0
    tex.inputs["Detail"].default_value = 4.0
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (color[0] * 0.55, color[1] * 0.55, color[2] * 0.4, 1)
    ramp.color_ramp.elements[1].color = (min(1, color[0] * 1.3), min(1, color[1] * 1.2), min(1, color[2] * 1.1), 1)
    links.new(tex.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


def gem_mat(name, color, emission_strength=0.8):
    mat, bsdf, *_ = principled(
        name,
        color,
        roughness=0.06,
        metallic=0.05,
        specular=1.0,
        transmission=0.85,
        ior=2.4,
        emission=color,
        emission_strength=emission_strength,
    )
    return mat


def make_materials():
    return {
        "stone": stone_mat("HP_Stone", (0.46, 0.44, 0.40), 0.1),
        "stone_dark": stone_mat("HP_StoneDark", (0.28, 0.27, 0.25), 0.08),
        "stone_warm": stone_mat("HP_StoneWarm", (0.52, 0.48, 0.40), 0.09),
        "gold": principled("HP_Gold", (0.82, 0.55, 0.14), roughness=0.22, metallic=0.95)[0],
        "path": stone_mat("HP_Path", (0.58, 0.54, 0.46), 0.12),
        "moss": leaf_mat("HP_Moss", (0.14, 0.36, 0.10)),
        "grass": leaf_mat("HP_Grass", (0.22, 0.50, 0.14)),
        "leaf_a": leaf_mat("HP_LeafA", (0.20, 0.55, 0.14)),
        "leaf_b": leaf_mat("HP_LeafB", (0.12, 0.38, 0.09)),
        "leaf_c": leaf_mat("HP_LeafC", (0.30, 0.62, 0.18)),
        "torch_wood": principled("HP_TorchWood", (0.28, 0.14, 0.06), roughness=0.85)[0],
        "flame": principled(
            "HP_Flame",
            (1.0, 0.55, 0.12),
            roughness=0.4,
            emission=(1.0, 0.55, 0.08),
            emission_strength=25.0,
        )[0],
        "skin": principled("HP_Skin", (0.84, 0.62, 0.46), roughness=0.45, subsurface=0.25)[0],
        "hair": principled("HP_Hair", (0.20, 0.10, 0.05), roughness=0.7)[0],
        "hat": principled("HP_Hat", (0.36, 0.22, 0.11), roughness=0.6)[0],
        "shirt": principled("HP_Shirt", (0.78, 0.68, 0.46), roughness=0.62)[0],
        "pants": principled("HP_Pants", (0.58, 0.48, 0.30), roughness=0.65)[0],
        "boot": principled("HP_Boot", (0.20, 0.11, 0.06), roughness=0.72)[0],
        "pack": principled("HP_Pack", (0.28, 0.15, 0.08), roughness=0.68)[0],
        "bedroll": principled("HP_Bedroll", (0.66, 0.34, 0.12), roughness=0.55)[0],
        "glove": principled("HP_Glove", (0.16, 0.09, 0.05), roughness=0.8)[0],
        "belt": principled("HP_Belt", (0.14, 0.08, 0.04), roughness=0.7)[0],
        "eye_white": principled("HP_EyeW", (0.95, 0.95, 0.95), roughness=0.3)[0],
        "eye_iris": principled("HP_EyeI", (0.28, 0.16, 0.08), roughness=0.25)[0],
        "red": gem_mat("HP_Red", (0.85, 0.05, 0.08), 1.2),
        "purple": gem_mat("HP_Purple", (0.48, 0.12, 0.78), 1.0),
        "pink": gem_mat("HP_Pink", (0.92, 0.22, 0.58), 1.1),
        "logo_stone": stone_mat("HP_LogoStone", (0.82, 0.74, 0.55), 0.08),
        "logo_gold": principled("HP_LogoGold", (0.98, 0.68, 0.14), roughness=0.18, metallic=0.95)[0],
        "mist": principled(
            "HP_Mist",
            (0.62, 0.78, 0.48),
            roughness=1.0,
            emission=(0.55, 0.72, 0.4),
            emission_strength=0.4,
        )[0],
    }


def new_cube(name, scale, loc, mat, col, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.rotation_euler = Euler(tuple(math.radians(a) for a in rot))
    apply_all(obj)
    assign(obj, mat)
    bevel(obj, 0.018, 3)
    subdivide(obj, 1, 2)
    shade_smooth(obj)
    bpy.context.scene.collection.objects.unlink(obj)
    col.objects.link(obj)
    return obj


def new_cylinder(name, radius, depth, loc, mat, col, verts=64, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_euler = Euler(tuple(math.radians(a) for a in rot))
    apply_all(obj)
    assign(obj, mat)
    bevel(obj, 0.015, 3)
    shade_smooth(obj)
    bpy.context.scene.collection.objects.unlink(obj)
    col.objects.link(obj)
    return obj


def new_torus(name, major, minor, loc, mat, col, major_seg=48, minor_seg=16):
    bpy.ops.mesh.primitive_torus_add(
        major_segments=major_seg,
        minor_segments=minor_seg,
        major_radius=major,
        minor_radius=minor,
        location=loc,
    )
    obj = bpy.context.active_object
    obj.name = name
    assign(obj, mat)
    shade_smooth(obj)
    bpy.context.scene.collection.objects.unlink(obj)
    col.objects.link(obj)
    return obj


def new_uvsphere(name, radius, loc, mat, col, segments=48, rings=24, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=radius, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    apply_all(obj)
    assign(obj, mat)
    shade_smooth(obj)
    bpy.context.scene.collection.objects.unlink(obj)
    col.objects.link(obj)
    return obj


def new_ico(name, radius, loc, mat, col, subdivisions=3):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=radius, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    assign(obj, mat)
    shade_smooth(obj)
    bpy.context.scene.collection.objects.unlink(obj)
    col.objects.link(obj)
    return obj


def leaf_card(name, loc, mat, col, size=0.55, rot=(0, 0, 0)):
    """Broad tropical leaf as subdivided plane with solidify."""
    bpy.ops.mesh.primitive_plane_add(size=1, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (size * 0.55, size, 1)
    obj.rotation_euler = Euler(tuple(math.radians(a) for a in rot))
    apply_all(obj)
    select_only(obj)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.subdivide(number_cuts=4)
    bpy.ops.object.mode_set(mode="OBJECT")
    # Pinch tip by scaling top verts in local Y
    mesh = obj.data
    for v in mesh.vertices:
        # taper toward +Y
        t = (v.co.y + 0.5)
        v.co.x *= max(0.15, 1.0 - t * 0.85)
        v.co.z += math.sin(t * math.pi) * 0.08
    mesh.update()
    solid = obj.modifiers.new("LeafThick", "SOLIDIFY")
    solid.thickness = 0.02
    subdivide(obj, 1, 2)
    assign(obj, mat)
    shade_smooth(obj)
    bpy.context.scene.collection.objects.unlink(obj)
    col.objects.link(obj)
    return obj


def vine_curve(name, points, mat, col, radius=0.035):
    curve = bpy.data.curves.new(name + "Data", type="CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 12
    curve.bevel_depth = radius
    curve.bevel_resolution = 4
    spline = curve.splines.new("NURBS")
    spline.points.add(len(points) - 1)
    for i, p in enumerate(points):
        spline.points[i].co = (p[0], p[1], p[2], 1)
    spline.use_endpoint_u = True
    obj = bpy.data.objects.new(name, curve)
    assign(obj, mat)
    link(obj, col)
    return obj


def diamond_mesh(name, scale, loc, mat, col, rot=(0, 0, 0)):
    s = scale
    # More facets for a cut gem look
    verts = [(0, 0, 0.55 * s)]  # table center elevated slightly as apex for crown look
    # table ring
    for i in range(8):
        a = i * math.pi / 4
        verts.append((math.cos(a) * 0.22 * s, math.sin(a) * 0.22 * s, 0.28 * s))
    # girdle
    for i in range(8):
        a = i * math.pi / 4 + math.pi / 8
        verts.append((math.cos(a) * 0.38 * s, math.sin(a) * 0.38 * s, 0.0))
    verts.append((0, 0, -0.55 * s))  # culet
    faces = []
    # table
    faces.append(tuple(range(1, 9)))
    # crown
    for i in range(8):
        faces.append((0, 1 + i, 1 + ((i + 1) % 8)))
    # upper girdle
    for i in range(8):
        a = 1 + i
        b = 1 + ((i + 1) % 8)
        c = 9 + ((i + 1) % 8)
        d = 9 + i
        faces.append((a, b, c, d))
    # pavilion
    tip = 17
    for i in range(8):
        faces.append((9 + i, 9 + ((i + 1) % 8), tip))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = loc
    obj.rotation_euler = Euler(tuple(math.radians(a) for a in rot))
    link(obj, col)
    assign(obj, mat)
    shade_smooth(obj, angle=20)
    bevel(obj, 0.004, 2)
    return obj


def crystal(name, height, radius, loc, mat, col, rot=(0, 0, 0), sides=8):
    bpy.ops.mesh.primitive_cone_add(
        vertices=sides, radius1=radius, radius2=0.01, depth=height, location=loc
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_euler = Euler(tuple(math.radians(a) for a in rot))
    apply_all(obj)
    assign(obj, mat)
    bevel(obj, 0.008, 2)
    shade_smooth(obj, angle=15)
    bpy.context.scene.collection.objects.unlink(obj)
    col.objects.link(obj)
    return obj


# ---------------------------------------------------------------------------
# Temple
# ---------------------------------------------------------------------------


def build_temple(mats, col):
    # Ground
    ground = new_cube("Ground", (18, 18, 0.2), (0, 6, -0.12), mats["grass"], col)
    displace_noise(ground, 0.03, 4.0, "GroundDisp")

    # Irregular path slabs
    for i in range(5):
        for j in range(5):
            x = -2.0 + i * 1.05 + RNG.uniform(-0.08, 0.08)
            y = 0.6 + j * 1.15 + RNG.uniform(-0.06, 0.06)
            slab = new_cube(
                f"Path_{i}_{j}",
                (0.95 + RNG.uniform(-0.08, 0.1), 0.95 + RNG.uniform(-0.08, 0.1), 0.12),
                (x, y, 0.04),
                mats["path"],
                col,
                rot=(0, 0, RNG.uniform(-6, 6)),
            )
            displace_noise(slab, 0.015, 3.5, f"PathDisp_{i}_{j}")

    # Side ruined walls (block masonry)
    for side, sx in (("L", -3.85), ("R", 3.85)):
        for row in range(5):
            for col_i in range(3):
                new_cube(
                    f"Masonry_{side}_{row}_{col_i}",
                    (1.05, 1.15, 0.95),
                    (
                        sx + RNG.uniform(-0.05, 0.05),
                        1.2 + col_i * 1.15,
                        0.45 + row * 0.95,
                    ),
                    mats["stone"] if (row + col_i) % 2 == 0 else mats["stone_dark"],
                    col,
                )

    # Ornate pillars
    for side, sx in (("L", -2.45), ("R", 2.45)):
        shaft = new_cylinder(f"Pillar_{side}", 0.62, 5.0, (sx, 0.7, 2.55), mats["stone"], col, verts=64)
        # Fluting via displace along normals with wood texture alternate
        displace_noise(shaft, 0.012, 1.2, f"Flute_{side}")
        new_cylinder(f"Capital_{side}", 0.78, 0.35, (sx, 0.7, 5.0), mats["stone_warm"], col, verts=48)
        new_cylinder(f"Base_{side}", 0.82, 0.4, (sx, 0.7, 0.15), mats["stone_warm"], col, verts=48)
        new_torus(f"GoldTop_{side}", 0.7, 0.07, (sx, 0.7, 4.55), mats["gold"], col)
        new_torus(f"GoldBot_{side}", 0.72, 0.08, (sx, 0.7, 0.55), mats["gold"], col)
        # Mid ornamental band
        new_torus(f"GoldMid_{side}", 0.66, 0.045, (sx, 0.7, 2.6), mats["gold"], col)

        # Torch assembly
        stick = new_cylinder(
            f"TorchStick_{side}",
            0.07,
            0.85,
            (sx + (0.85 if side == "L" else -0.85), 0.25, 2.7),
            mats["torch_wood"],
            col,
            verts=24,
            rot=(22, 0, 0),
        )
        flame = new_uvsphere(
            f"Flame_{side}",
            0.22,
            (sx + (0.85 if side == "L" else -0.85), -0.05, 3.25),
            mats["flame"],
            col,
            segments=32,
            rings=16,
            scale=(0.75, 0.75, 1.45),
        )
        # Point light for torch glow
        light_data = bpy.data.lights.new(f"TorchLight_{side}", "POINT")
        light_data.energy = 120
        light_data.color = (1.0, 0.55, 0.2)
        light_data.shadow_soft_size = 0.4
        light = bpy.data.objects.new(f"TorchLight_{side}", light_data)
        light.location = (sx + (0.85 if side == "L" else -0.85), -0.05, 3.25)
        link(light, col)

    # Pointed Angkor arch — layered stones along a pointed curve
    def arch_z(t):
        # t in [-1,1], pointed gothic-ish peak
        return 4.2 + (1.0 - abs(t)) * 1.55 + (1.0 - t * t) * 0.35

    for i in range(13):
        t = -1.0 + i * (2.0 / 12.0)
        x = t * 2.05
        z = arch_z(t)
        w = 0.55 + (1.0 - abs(t)) * 0.25
        new_cube(
            f"ArchStone_{i}",
            (w, 1.05, 0.55),
            (x, 0.55, z),
            mats["stone_dark"] if i % 2 else mats["stone"],
            col,
            rot=(0, t * -12, 0),
        )
    # Carved peak ornament
    new_cube("PeakOrnament", (0.7, 0.7, 0.55), (0, 0.35, 6.05), mats["gold"], col)
    new_ico("PeakJewel", 0.18, (0, 0.15, 6.35), mats["red"], col, 3)

    # Inner dark passage / mist depth
    new_cube("GateRecess", (2.6, 0.5, 3.6), (0, 1.35, 2.2), mats["stone_dark"], col)
    mist = new_cube("MistVolume", (8, 0.3, 5), (0, 8.5, 3.0), mats["mist"], col)
    mist.display_type = "WIRE"

    # Hanging vines on pillars and arch
    vine_specs = [
        [(-2.9, 0.9, 5.0), (-2.85, 0.6, 3.8), (-2.7, 0.3, 2.6), (-2.55, 0.1, 1.5)],
        [(-2.2, 0.4, 5.1), (-2.1, 0.1, 3.9), (-1.95, -0.1, 2.8)],
        [(2.9, 0.9, 5.0), (2.85, 0.6, 3.8), (2.7, 0.3, 2.6), (2.55, 0.1, 1.5)],
        [(2.2, 0.4, 5.1), (2.1, 0.1, 3.9), (1.95, -0.1, 2.8)],
        [(-1.2, 0.7, 5.8), (-1.0, 0.4, 5.0), (-0.7, 0.2, 4.4)],
        [(1.2, 0.7, 5.8), (1.0, 0.4, 5.0), (0.7, 0.2, 4.4)],
    ]
    for i, pts in enumerate(vine_specs):
        vine_curve(f"Vine_{i}", pts, mats["leaf_b"], col, radius=0.04)
        # Leaves along vine
        for j, p in enumerate(pts[1:]):
            leaf_card(
                f"VineLeaf_{i}_{j}",
                (p[0] + RNG.uniform(-0.1, 0.1), p[1] - 0.1, p[2]),
                mats["leaf_a"] if j % 2 == 0 else mats["leaf_c"],
                col,
                size=RNG.uniform(0.35, 0.55),
                rot=(RNG.uniform(40, 80), RNG.uniform(-30, 30), RNG.uniform(-40, 40)),
            )

    # Dense jungle framing left/right
    bush_spots = []
    for x_base in (-5.2, -4.2, -3.5, 3.5, 4.2, 5.2):
        for y in (-1.2, -0.2, 1.0, 2.5, 4.0):
            bush_spots.append((x_base + RNG.uniform(-0.3, 0.3), y + RNG.uniform(-0.2, 0.2), RNG.uniform(0.7, 1.4)))
    for i, (x, y, r) in enumerate(bush_spots):
        bush = new_uvsphere(
            f"Bush_{i}",
            r,
            (x, y, r * 0.7),
            mats["leaf_a"] if i % 3 == 0 else (mats["leaf_b"] if i % 3 == 1 else mats["leaf_c"]),
            col,
            segments=32,
            rings=16,
            scale=(1.25, 1.0, 1.05),
        )
        displace_noise(bush, 0.08, 1.8, f"BushDisp_{i}")
        for k in range(3):
            leaf_card(
                f"BushLeaf_{i}_{k}",
                (x + RNG.uniform(-0.5, 0.5), y - 0.3, r * 0.5 + RNG.uniform(0, 0.6)),
                mats["leaf_c"],
                col,
                size=RNG.uniform(0.45, 0.8),
                rot=(RNG.uniform(20, 70), RNG.uniform(-50, 50), RNG.uniform(-60, 60)),
            )

    # Foreground broad leaves framing
    for i, (x, rot_z) in enumerate(((-3.8, 25), (-4.5, 40), (3.8, -25), (4.5, -40))):
        leaf_card(f"FrameLeaf_{i}", (x, -1.6, 1.2), mats["leaf_a"], col, size=1.4, rot=(55, 0, rot_z))
        leaf_card(f"FrameLeafB_{i}", (x * 0.9, -1.8, 0.6), mats["leaf_b"], col, size=1.1, rot=(65, 10, rot_z))

    # Rocks at pillar bases
    for i, (x, y) in enumerate(((-2.8, 0.2), (-1.9, -0.3), (2.8, 0.2), (1.9, -0.35), (-0.4, 0.9), (0.5, 1.1))):
        rock = new_ico(f"Rock_{i}", RNG.uniform(0.2, 0.4), (x, y, 0.15), mats["stone_dark"], col, 3)
        rock.scale = (1.2, 0.9, 0.7)
        apply_all(rock)
        displace_noise(rock, 0.05, 2.0, f"RockDisp_{i}")


# ---------------------------------------------------------------------------
# Explorer
# ---------------------------------------------------------------------------


def build_explorer(mats, col):
    # Smooth cartoon proportions facing camera, gesturing.
    head = new_uvsphere("Head", 0.28, (0, 0.05, 2.12), mats["skin"], col, 48, 24)
    subdivide(head, 1, 2)

    # Face features
    new_uvsphere("EyeL", 0.055, (-0.09, -0.22, 2.14), mats["eye_white"], col, 24, 12)
    new_uvsphere("EyeR", 0.055, (0.09, -0.22, 2.14), mats["eye_white"], col, 24, 12)
    new_uvsphere("IrisL", 0.03, (-0.09, -0.26, 2.14), mats["eye_iris"], col, 16, 8)
    new_uvsphere("IrisR", 0.03, (0.09, -0.26, 2.14), mats["eye_iris"], col, 16, 8)
    # Smile (small torus segment approx via stretched sphere)
    smile = new_uvsphere("Smile", 0.08, (0, -0.24, 2.0), mats["hair"], col, 16, 8, scale=(1.3, 0.35, 0.35))
    smile.rotation_euler.x = math.radians(15)

    # Hair under hat
    hair = new_uvsphere("Hair", 0.26, (0, 0.08, 2.18), mats["hair"], col, 32, 16, scale=(1.05, 0.85, 0.55))
    displace_noise(hair, 0.02, 2.5, "HairDisp")

    # Fedora
    crown = new_cylinder("HatCrown", 0.3, 0.28, (0, 0.02, 2.38), mats["hat"], col, 48)
    brim = new_cylinder("HatBrim", 0.52, 0.04, (0, 0.0, 2.26), mats["hat"], col, 64)
    # Pinch crown top slightly
    dent = new_cube("HatDent", (0.22, 0.35, 0.06), (0, 0.02, 2.52), mats["hat"], col)

    # Torso with slight chest shape
    torso = new_cube("Torso", (0.72, 0.4, 0.88), (0, 0.0, 1.48), mats["shirt"], col)
    subdivide(torso, 2, 3)
    displace_noise(torso, 0.01, 3.0, "ShirtDisp")
    new_cube("PocketL", (0.16, 0.05, 0.16), (-0.18, -0.22, 1.52), mats["pants"], col)
    new_cube("PocketR", (0.16, 0.05, 0.16), (0.18, -0.22, 1.52), mats["pants"], col)
    new_cube("Belt", (0.78, 0.42, 0.1), (0, 0.0, 1.05), mats["belt"], col)
    new_cube("Buckle", (0.12, 0.06, 0.08), (0, -0.23, 1.05), mats["gold"], col)

    # Legs / boots
    for side, x in (("L", -0.18), ("R", 0.2)):
        leg = new_cylinder(f"Leg_{side}", 0.13, 0.78, (x, 0.0, 0.6), mats["pants"], col, 32)
        subdivide(leg, 1, 2)
        boot = new_cube(f"Boot_{side}", (0.28, 0.42, 0.26), (x, 0.08, 0.14), mats["boot"], col)
        bevel(boot, 0.03, 3)

    # Arms — raised thumbs-up (viewer left) and presenting (viewer right)
    arm_l = new_cylinder("Arm_L", 0.09, 0.58, (-0.52, 0.05, 1.7), mats["shirt"], col, 28)
    arm_l.rotation_euler = Euler((math.radians(-45), 0, math.radians(35)))
    hand_l = new_uvsphere("Hand_L", 0.1, (-0.72, -0.12, 2.0), mats["glove"], col, 24, 12)
    # thumb bump
    new_uvsphere("Thumb_L", 0.045, (-0.78, -0.2, 2.08), mats["glove"], col, 12, 8)

    arm_r = new_cylinder("Arm_R", 0.09, 0.62, (0.55, 0.15, 1.35), mats["shirt"], col, 28)
    arm_r.rotation_euler = Euler((math.radians(25), 0, math.radians(-48)))
    hand_r = new_uvsphere("Hand_R", 0.1, (0.88, 0.35, 1.1), mats["glove"], col, 24, 12)

    # Backpack + bedroll
    pack = new_cube("Pack", (0.55, 0.38, 0.78), (0, 0.42, 1.55), mats["pack"], col)
    subdivide(pack, 1, 2)
    new_cylinder("Bedroll", 0.14, 0.7, (0, 0.48, 2.05), mats["bedroll"], col, 32, rot=(0, 90, 0))
    new_cylinder("StrapL", 0.03, 0.55, (-0.22, 0.15, 1.75), mats["belt"], col, 12, rot=(25, 0, 15))
    new_cylinder("StrapR", 0.03, 0.55, (0.22, 0.15, 1.75), mats["belt"], col, 12, rot=(25, 0, -15))


# ---------------------------------------------------------------------------
# Gems / Logo
# ---------------------------------------------------------------------------


def build_gems(mats, col):
    crystal("CrystalMain", 1.7, 0.32, (0, 0, 0.85), mats["pink"], col, sides=8)
    crystal("CrystalL", 1.25, 0.24, (-0.42, 0.15, 0.62), mats["purple"], col, rot=(10, 0, -22), sides=8)
    crystal("CrystalR", 1.2, 0.22, (0.45, -0.1, 0.58), mats["purple"], col, rot=(-8, 0, 26), sides=8)
    crystal("CrystalS1", 0.65, 0.14, (-0.15, -0.25, 0.32), mats["purple"], col, rot=(20, 0, 40), sides=7)
    crystal("CrystalS2", 0.55, 0.12, (0.25, 0.28, 0.28), mats["pink"], col, rot=(-14, 0, -32), sides=7)
    crystal("CrystalS3", 0.4, 0.09, (0.05, -0.05, 0.2), mats["purple"], col, rot=(8, 0, 10), sides=6)
    diamond_mesh("RedSpark", 0.48, (-0.7, -0.15, 0.7), mats["red"], col, rot=(5, 25, 20))
    diamond_mesh("PurpleSpark", 0.32, (0.65, 0.2, 0.45), mats["purple"], col, rot=(-10, -18, 30))


def build_logo(mats, col):
    def add_text(name, body, size, loc, mat, extrude=0.18):
        curve = bpy.data.curves.new(name + "Data", type="FONT")
        curve.body = body
        curve.size = size
        curve.extrude = extrude
        curve.bevel_depth = 0.02
        curve.bevel_resolution = 3
        curve.align_x = "CENTER"
        curve.align_y = "CENTER"
        obj = bpy.data.objects.new(name, curve)
        obj.rotation_euler = Euler((math.radians(90), 0, 0))
        obj.location = loc
        link(obj, col)
        select_only(obj)
        bpy.ops.object.convert(target="MESH")
        obj = bpy.context.active_object
        obj.name = name
        assign(obj, mat)
        bevel(obj, 0.012, 2)
        shade_smooth(obj)
        displace_noise(obj, 0.008, 4.0, name + "Disp")
        obj.select_set(False)
        return obj

    add_text("TitleDiamond", "DIAMOND", 1.0, (0, 0, 0.65), mats["logo_stone"], 0.2)
    add_text("TitleRush", "RUSH", 1.0, (0, 0, -0.2), mats["logo_stone"], 0.2)
    add_text("Title2D", "2D", 0.58, (1.95, -0.08, -0.85), mats["logo_gold"], 0.14)

    diamond_mesh("LogoRed", 0.85, (-2.95, 0.25, 0.25), mats["red"], col, rot=(15, 30, -22))
    diamond_mesh("LogoPurple", 0.65, (2.95, 0.2, 0.05), mats["purple"], col, rot=(-10, -25, 28))
    diamond_mesh("LogoOGem", 0.28, (-0.55, 0.3, 0.65), mats["red"], col, rot=(0, 18, 8))

    # Wrapping vines / leaves around logo
    vine_curve(
        "LogoVine1",
        [(-2.2, -0.2, 1.1), (-1.0, -0.4, 1.3), (0.2, -0.35, 1.25), (1.4, -0.25, 1.05)],
        mats["leaf_b"],
        col,
        0.045,
    )
    vine_curve(
        "LogoVine2",
        [(2.0, -0.2, 0.9), (1.0, -0.35, -0.9), (0.0, -0.3, -1.05), (-1.2, -0.25, -0.85)],
        mats["leaf_b"],
        col,
        0.04,
    )
    for i, (x, z, s, key) in enumerate(
        (
            (-1.8, 1.15, 0.4, "leaf_a"),
            (-0.3, 1.35, 0.35, "leaf_c"),
            (1.5, 1.1, 0.38, "leaf_a"),
            (0.8, -1.15, 0.32, "leaf_b"),
            (-1.0, -1.05, 0.3, "leaf_c"),
            (2.2, 0.2, 0.28, "leaf_a"),
        )
    ):
        leaf_card(f"LogoLeaf_{i}", (x, -0.45, z), mats[key], col, size=s, rot=(70, RNG.uniform(-20, 20), RNG.uniform(-40, 40)))


# ---------------------------------------------------------------------------
# World / lights / cameras / render
# ---------------------------------------------------------------------------


def setup_world(scene):
    world = bpy.data.worlds.new("HomeWorld")
    scene.world = world
    world.use_nodes = True
    nodes = world.node_tree.nodes
    links = world.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputWorld")
    bg = nodes.new("ShaderNodeBackground")
    # Soft jungle canopy gradient via color ramp on incoming
    path = nodes.new("ShaderNodeLightPath")
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.0
    ramp.color_ramp.elements[0].color = (0.25, 0.42, 0.22, 1)
    ramp.color_ramp.elements[1].position = 1.0
    ramp.color_ramp.elements[1].color = (0.62, 0.78, 0.48, 1)
    # Use a fixed mid color — LightPath isometry is limited; keep simple
    bg.inputs["Color"].default_value = (0.48, 0.68, 0.38, 1)
    bg.inputs["Strength"].default_value = 0.55
    links.new(bg.outputs["Background"], out.inputs["Surface"])


def setup_lights(col):
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("SunData", "SUN"))
    sun.data.energy = 4.5
    sun.data.color = (1.0, 0.95, 0.82)
    sun.data.angle = math.radians(5)
    sun.rotation_euler = Euler((math.radians(48), math.radians(-22), math.radians(12)))
    link(sun, col)

    fill = bpy.data.objects.new("Fill", bpy.data.lights.new("FillData", "AREA"))
    fill.data.energy = 180
    fill.data.size = 10
    fill.data.color = (0.65, 0.9, 0.55)
    fill.location = (-5, -6, 4)
    fill.rotation_euler = Euler((math.radians(60), 0, math.radians(-30)))
    link(fill, col)

    rim = bpy.data.objects.new("Rim", bpy.data.lights.new("RimData", "AREA"))
    rim.data.energy = 90
    rim.data.size = 5
    rim.data.color = (1.0, 0.7, 0.35)
    rim.location = (3, 2, 5)
    rim.rotation_euler = Euler((math.radians(50), math.radians(25), 0))
    link(rim, col)

    # Soft canopy bounce from above
    top = bpy.data.objects.new("Canopy", bpy.data.lights.new("CanopyData", "AREA"))
    top.data.energy = 60
    top.data.size = 14
    top.data.color = (0.55, 0.85, 0.4)
    top.location = (0, 2, 9)
    top.rotation_euler = Euler((0, 0, 0))
    link(top, col)


def make_camera(name, location, rotation_deg, ortho=False, ortho_scale=8.0, lens=32):
    data = bpy.data.cameras.new(name)
    if ortho:
        data.type = "ORTHO"
        data.ortho_scale = ortho_scale
    else:
        data.type = "PERSP"
        data.lens = lens
    data.clip_end = 200
    cam = bpy.data.objects.new(name, data)
    cam.location = location
    cam.rotation_euler = Euler(tuple(math.radians(a) for a in rotation_deg))
    bpy.context.scene.collection.objects.link(cam)
    return cam


def configure_render(scene, width, height, transparent, engine_name):
    prefer_engine(scene, prefer_cycles=(engine_name == "CYCLES"))
    scene.render.resolution_x = width
    scene.render.resolution_y = height
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = transparent
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.render.use_file_extension = True
    try:
        scene.view_settings.view_transform = "Standard"
    except (TypeError, ValueError):
        pass
    if scene.render.engine.startswith("BLENDER_EEVEE") and hasattr(scene, "eevee"):
        eevee = scene.eevee
        for attr, value in (("taa_render_samples", 128), ("use_soft_shadows", True)):
            if hasattr(eevee, attr):
                setattr(eevee, attr, value)


def render_pass(scene, camera, path, width, height, transparent, show, hide, engine_name):
    scene.camera = camera
    configure_render(scene, width, height, transparent, engine_name)
    for c in show:
        set_hide(c, False)
    for c in hide:
        set_hide(c, True)
    path.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    print("Rendered", path)


def parse_args(argv):
    return {"install": "--install" in argv, "eevee": "--eevee" in argv}


def main():
    raw = sys.argv
    extra = raw[raw.index("--") + 1 :] if "--" in raw else []
    args = parse_args(extra)

    clear_scene()
    scene = bpy.context.scene
    engine_name = prefer_engine(scene, prefer_cycles=not args["eevee"])
    print("Render engine:", engine_name)
    setup_world(scene)
    mats = make_materials()

    temple = collection("HomeTemple")
    explorer = collection("HomeExplorer")
    gems = collection("HomeGems")
    logo = collection("HomeLogo")
    lights = collection("HomeLights")

    print("Building temple...")
    build_temple(mats, temple)
    print("Building explorer...")
    build_explorer(mats, explorer)
    print("Building gems...")
    build_gems(mats, gems)
    print("Building logo...")
    build_logo(mats, logo)
    setup_lights(lights)

    # Layout for combined hero preview (mockup composition)
    for obj in explorer.objects:
        obj.location.x += -2.7
        obj.location.y += -1.5
        obj.scale *= 1.25
    for obj in gems.objects:
        obj.location.x += 3.55
        obj.location.y += -0.55
        obj.scale *= 0.9

    cam_bg = make_camera("HomeBgCam", (0.0, -8.2, 2.7), (80, 0, 0), lens=28)
    cam_explorer = make_camera("HomeExplorerCam", (-2.7, -6.5, 1.55), (88, 0, 0), ortho=True, ortho_scale=3.8)
    cam_gems = make_camera("HomeGemCam", (3.55, -3.8, 1.05), (68, 0, 0), ortho=True, ortho_scale=2.1)
    cam_logo = make_camera("HomeLogoCam", (0.0, -9.0, 0.05), (90, 0, 0), ortho=True, ortho_scale=7.4)

    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    print("Rendering passes...")
    render_pass(scene, cam_bg, PREVIEW_DIR / "bg_temple.png", 1920, 1080, False, [temple, lights], [explorer, gems, logo], engine_name)
    render_pass(scene, cam_explorer, PREVIEW_DIR / "hero_explorer.png", 620, 1028, True, [explorer, lights], [temple, gems, logo], engine_name)
    render_pass(scene, cam_gems, PREVIEW_DIR / "gem_cluster.png", 540, 632, True, [gems, lights], [temple, explorer, logo], engine_name)
    render_pass(scene, cam_logo, PREVIEW_DIR / "logo.png", 800, 360, True, [logo, lights], [temple, explorer, gems], engine_name)
    render_pass(scene, cam_bg, PREVIEW_DIR / "home_preview.png", 1920, 1080, False, [temple, explorer, gems, lights], [logo], engine_name)

    if args["install"]:
        OUT_DIR.mkdir(parents=True, exist_ok=True)
        for name in ("bg_temple.png", "hero_explorer.png", "gem_cluster.png", "logo.png"):
            dst = OUT_DIR / name
            dst.write_bytes((PREVIEW_DIR / name).read_bytes())
            print("Installed", dst)
    else:
        print("Preview only (not installed to Unity). Re-run with -- --install after approval.")

    BLEND.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
    print("Saved", BLEND)


if __name__ == "__main__":
    main()
    sys.exit(0)
