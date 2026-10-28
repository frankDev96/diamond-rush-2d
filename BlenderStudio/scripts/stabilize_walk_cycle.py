"""Blender text-editor / CLI entry. Maintained copy lives in the skill folder."""

import runpy
import sys
from pathlib import Path

_skill = (
    Path(__file__).resolve().parents[2]
    / ".agents"
    / "skills"
    / "diamond-rush-blender"
    / "scripts"
    / "stabilize_walk_cycle.py"
)
_ns = runpy.run_path(str(_skill), run_name="diamond_rush_stabilize_walk")
run = _ns["run"]


if __name__ == "__main__":
    run()
