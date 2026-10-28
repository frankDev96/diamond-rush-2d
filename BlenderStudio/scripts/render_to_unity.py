"""Blender text-editor entry. The maintained script is the skill copy."""

import runpy
import sys
from pathlib import Path

_skill = (
    Path(__file__).resolve().parents[2]
    / ".agents"
    / "skills"
    / "diamond-rush-blender"
    / "scripts"
    / "render_to_unity.py"
)
_ns = runpy.run_path(str(_skill), run_name="diamond_rush_render")
render_oblique_asset_to_unity = _ns["render_oblique_asset_to_unity"]


if __name__ == "__main__" and "--" in sys.argv:
    _ns["main"]()
