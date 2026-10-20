#!/bin/zsh
# Compile Assets/Scripts and Assets/Editor with Unity's bundled Roslyn while the Editor holds the project lock.
# Usage (from the repo root): zsh .cursor/skills/diamond-rush-mobile-ui/scripts/compile_check.sh
# Prints compiler errors and exits non-zero on failure. Temp output goes to .compilecheck/ and is removed.

UNITY_VERSION=${UNITY_VERSION:-6000.6.6f1}
S=/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/Resources/Scripting
M=$S/Managed
DN=$S/DotNetSdk
CSC=$(ls -d $DN/sdk/*/Roslyn/bincore/csc.dll 2>/dev/null | tail -1)
OUT=.compilecheck

if [[ ! -f "$CSC" ]]; then
  echo "csc.dll not found under $DN/sdk" >&2
  exit 2
fi

mkdir -p $OUT
FLAGS="-nologo -target:library -langversion:9 -nostdlib -nowarn:1701,1702,0649,0414,0169"

# zsh does not word-split variables, so references go through response files.
{
  echo "$FLAGS -out:$OUT/Runtime.dll"
  for f in $M/UnityEngine/UnityEngine*.dll; do echo "-r:\"$f\""; done
  echo "-r:\"$S/NetStandard/ref/2.1.0/netstandard.dll\""
  for a in Unity.TextMeshPro Unity.InputSystem UnityEngine.UI; do echo "-r:Library/ScriptAssemblies/$a.dll"; done
  find Assets/Scripts -name '*.cs' | while read f; do echo "\"$f\""; done
} > $OUT/runtime.rsp

# Editor modules only: adding Managed/UnityEditor.dll as well causes CS0433 duplicate types.
{
  echo "$FLAGS -out:$OUT/Editor.dll"
  for f in $M/UnityEngine/UnityEngine*.dll $M/UnityEngine/UnityEditor*.dll; do echo "-r:\"$f\""; done
  echo "-r:\"$S/NetStandard/ref/2.1.0/netstandard.dll\""
  for a in Unity.TextMeshPro Unity.InputSystem UnityEngine.UI Unity.TextMeshPro.Editor; do echo "-r:Library/ScriptAssemblies/$a.dll"; done
  echo "-r:$OUT/Runtime.dll"
  find Assets/Editor -name '*.cs' | while read f; do echo "\"$f\""; done
} > $OUT/editor.rsp

echo "--- runtime (Assets/Scripts)"
$DN/dotnet $CSC @$OUT/runtime.rsp
runtime=$?
editor=1
if [[ $runtime -eq 0 ]]; then
  echo "--- editor (Assets/Editor)"
  $DN/dotnet $CSC @$OUT/editor.rsp
  editor=$?
fi

rm -rf $OUT
if [[ $runtime -eq 0 && $editor -eq 0 ]]; then
  echo "OK: runtime and editor compile"
  exit 0
fi
echo "FAILED (runtime=$runtime editor=$editor)" >&2
exit 1
