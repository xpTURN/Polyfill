#!/usr/bin/env bash
#
# run-compat.sh — xpTURN.Polyfill version matrix check
#
# For each Unity editor, builds a throwaway project under Logs/compat/<version>/Project from
# tests/Compat/Assets, the sample scripts (samples/PolyfillSample/Assets/Scripts, copied to
# Assets/Samples) and the package tests (tests/com.xpturn.polyfill.tests, copied to
# Assets/Tests), with a copy of this repository's package embedded in it, and checks the
# release gate:
#
#   1. apply  runs the package menu command that sets -langversion:preview and CSHARP_PREVIEW
#   2. tests  runs the package tests in the editor (PlayMode platform)
#   3. gate   Library/ScriptAssemblies/Case.C1_Polyfill.dll exists (the probe compiled), no test
#             failed or was inconclusive, and the test run exited with 0
#
# The gate looks for the probe DLL rather than for error lines: Bee stops after the first failing
# assembly, so a log without errors does not mean that the probe compiled.
#
# Usage:
#   tools/run-compat.sh [options] [<unity-version>...]
#
#   Without a version, runs 2022.3.62f3, 6000.3.9f1 and 6000.6.0f1 in turn.
#
# Options:
#   --smoke          also build Mono and IL2CPP players (StandaloneOSX, arm64) and run the smoke
#                    in each (an IL2CPP build takes about 1.5 to 4 minutes)
#   --player-tests   also run the package tests in an IL2CPP player (StandaloneOSX). The display
#                    has to stay awake, so the script holds caffeinate until it ends. Run one at a
#                    time: a test player reports to whichever editor connects to it first
#   --net-framework  also run the package tests with the .NET Framework API level
#   --alternatives   also compile the probe against other polyfill libraries, with the editor's
#                    compiler and the response file that Unity generated for the probe. The
#                    libraries are downloaded at run time into Logs/compat/_downloads
#   --no-gate        skip steps 1 and 2 and reuse the work project of an earlier run
#   --clean          delete the work project first
#   --update-docs    write the tables of docs/Compatibility.md from the latest result of each step,
#                    whether from this run or an earlier one
#   -h, --help       show this help
#
# Each step keeps its latest result in Logs/compat/<version>/status.txt until it runs again.
#
# Environment variables:
#   UNITY_EDITORS    folder that holds the editors (default: /Applications/Unity/Hub/Editor)
#
# macOS only. The work project must not be open in another editor (batch mode needs the lock).
# Exit code: non-zero if a gate or a selected step fails.

set -uo pipefail

# ── Paths ────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

TEMPLATE="${REPO_ROOT}/tests/Compat"
PACKAGE="${REPO_ROOT}/src/Polyfill/Assets/Polyfill"
SAMPLES="${REPO_ROOT}/samples/PolyfillSample/Assets/Scripts"
TESTS="${REPO_ROOT}/tests/com.xpturn.polyfill.tests"
WORK_ROOT="${REPO_ROOT}/Logs/compat"
DOWNLOADS="${WORK_ROOT}/_downloads"
DOCS_FILE="${REPO_ROOT}/docs/Compatibility.md"
EDITORS_DIR="${UNITY_EDITORS:-/Applications/Unity/Hub/Editor}"

# ── Options ──────────────────────────────────────────────────────────────────
RUN_GATE=1
RUN_SMOKE=0
RUN_PLAYER_TESTS=0
RUN_NETFX=0
RUN_ALTERNATIVES=0
CLEAN=0
UPDATE_DOCS=0
VERSIONS=()

usage() { awk 'NR>1 && /^#/ {sub(/^# ?/, ""); print; next} NR>1 {exit}' "${BASH_SOURCE[0]}"; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --smoke)         RUN_SMOKE=1 ;;
    --player-tests)  RUN_PLAYER_TESTS=1 ;;
    --net-framework) RUN_NETFX=1 ;;
    --alternatives)  RUN_ALTERNATIVES=1 ;;
    --no-gate)       RUN_GATE=0 ;;
    --clean)         CLEAN=1 ;;
    --update-docs)   UPDATE_DOCS=1 ;;
    -h|--help)       usage; exit 0 ;;
    -*) echo "Unknown option: $1" >&2; usage; exit 2 ;;
    *)  VERSIONS+=("$1") ;;
  esac
  shift
done
if [[ ${#VERSIONS[@]} -eq 0 ]]; then
  VERSIONS=(2022.3.62f3 6000.3.9f1 6000.6.0f1)
fi

# ── Colors (only when writing to a tty) ──────────────────────────────────────
if [[ -t 1 ]]; then
  C_RED=$'\033[31m'; C_YEL=$'\033[33m'; C_CYN=$'\033[36m'; C_BLD=$'\033[1m'; C_RST=$'\033[0m'
else
  C_RED=""; C_YEL=""; C_CYN=""; C_BLD=""; C_RST=""
fi

section() { echo; echo "${C_BLD}${C_CYN}=== $* ===${C_RST}"; }
step()    { echo "${C_BLD}▶ $1${C_RST}${2:+  ($2)}"; }

# ── Helpers ──────────────────────────────────────────────────────────────────
record() { echo "$1=$2" >> "${WORK}/status.txt"; }

# begin <step> <key...> — drops the step's earlier results and stamps today's date
begin() {
  local name="$1"; shift
  touch "${WORK}/status.txt"
  grep -v -E "^(${name}_date|$(IFS='|'; echo "$*"))=" "${WORK}/status.txt" > "${WORK}/status.tmp"
  mv "${WORK}/status.tmp" "${WORK}/status.txt"
  record "${name}_date" "$(date +%Y-%m-%d)"
}

# run_limited <seconds> <command...>
# Kills the command, and every process started with the work project's path, when it outlives the
# limit. Returns the command's exit code, or 124 on timeout. The token file is removed before the
# watcher is stopped, so a watcher that wakes up late does nothing.
run_limited() {
  local limit="$1"; shift
  local token="${WORK}/.run.$$.${RANDOM}"
  touch "${token}"
  "$@" &
  local pid=$!
  ( sleep "${limit}"
    if [[ -f "${token}" ]]; then touch "${token}.timeout"; kill -9 "${pid}"; pkill -9 -f -- "${PROJECT}"; fi ) 2>/dev/null &
  local watch=$!
  wait "${pid}"
  local rc=$?
  rm -f "${token}"
  pkill -P "${watch}" 2>/dev/null; wait "${watch}" 2>/dev/null
  if [[ -f "${token}.timeout" ]]; then
    rm -f "${token}.timeout"
    return 124
  fi
  return ${rc}
}

# unity <seconds> <log> <arguments...>
unity() {
  local limit="$1" log="$2"; shift 2
  run_limited "${limit}" "${UNITY}" -batchmode -nographics -projectPath "${PROJECT}" -logFile "${log}" "$@"
}

# run_tests <slug> <platform> <seconds>
run_tests() {
  local slug="$1" platform="$2" limit="$3"
  local xml="${WORK}/results-${slug}.xml" log="${WORK}/tests-${slug}.log"
  rm -f "${xml}"
  step "tests (${slug}, ${platform})" "log: ${log}"
  unity "${limit}" "${log}" -runTests -testPlatform "${platform}" -testResults "${xml}"
  record "tests_${slug}_exit" $?
}

# ── Work project ─────────────────────────────────────────────────────────────
# The template, the samples and the tests are copied without .meta files; Unity writes them into
# the work project, and the sync keeps them.
# ProjectVersion.txt makes Unity open the folder as an existing project, which keeps the manifest
# as written (a new project gets the editor's default packages added). The package is copied in,
# not referenced through "file:": Unity rewrites the .meta files of a local package it can write to.
prepare_project() {
  if [[ ${CLEAN} -eq 1 ]]; then rm -rf "${WORK}"; fi
  mkdir -p "${PROJECT}/Packages" "${PROJECT}/ProjectSettings"
  if [[ ! -f "${PROJECT}/ProjectSettings/ProjectVersion.txt" ]]; then
    echo "m_EditorVersion: ${VERSION}" > "${PROJECT}/ProjectSettings/ProjectVersion.txt"
  fi
  rsync -a --delete --exclude '*.meta' --exclude 'Compat.unity' --exclude '/Samples/' --exclude '/Tests/' "${TEMPLATE}/Assets/" "${PROJECT}/Assets/" || return 1
  rsync -a --delete --exclude '*.meta' "${SAMPLES}/" "${PROJECT}/Assets/Samples/" || return 1
  rsync -a --delete --exclude '*.meta' --exclude 'package.json' "${TESTS}/" "${PROJECT}/Assets/Tests/" || return 1
  rsync -a --delete --exclude '.DS_Store' "${PACKAGE}/" "${PROJECT}/Packages/com.xpturn.polyfill/" || return 1
  local framework
  framework="$(python3 -c 'import json, sys; print(json.load(open(sys.argv[1]))["packages"]["com.unity.test-framework"]["version"])' \
    "${CONTENTS}/Resources/PackageManager/Editor/manifest.json")" || return 1
  cat > "${PROJECT}/Packages/manifest.json" <<EOF
{
  "dependencies": {
    "com.unity.test-framework": "${framework}"
  }
}
EOF
}

# ── Steps ────────────────────────────────────────────────────────────────────
step_gate() {
  begin gate apply_exit tests_editor_exit
  step "apply" "log: ${WORK}/apply.log"
  unity 1800 "${WORK}/apply.log" -executeMethod xpTURN.Polyfill.Editor.PlayerSettingsAdditionalCompilerArguments.ApplyArgumentsForAllTargets -quit
  record apply_exit $?
  run_tests editor PlayMode 1800
}

step_net_framework() {
  begin netfx netfx_set_exit tests_netfx_exit netfx_restore_exit
  step ".NET Framework API level" "log: ${WORK}/netfx-set.log"
  unity 1800 "${WORK}/netfx-set.log" -executeMethod CompatBuild.UseNetFramework -quit
  record netfx_set_exit $?
  run_tests netfx PlayMode 1800
  unity 1800 "${WORK}/netfx-restore.log" -executeMethod CompatBuild.UseNetStandard -quit
  local rc=$?
  record netfx_restore_exit ${rc}
  if [[ ${rc} -ne 0 ]]; then echo "${C_YEL}  The API level was not restored to .NET Standard; rerun with --clean.${C_RST}"; fi
}

step_player_tests() {
  begin player il2cpp_set_exit tests_player_exit
  step "IL2CPP backend" "log: ${WORK}/il2cpp-set.log"
  unity 1800 "${WORK}/il2cpp-set.log" -executeMethod CompatBuild.UseIl2cpp -quit
  record il2cpp_set_exit $?
  run_tests player StandaloneOSX 3600
}

step_smoke() {
  local backend method app out
  begin smoke build_mono_exit smoke_mono_exit build_il2cpp_exit smoke_il2cpp_exit
  for backend in mono il2cpp; do
    if [[ "${backend}" == mono ]]; then method=BuildMono; else method=BuildIl2cpp; fi
    rm -rf "${PROJECT}/Build/${backend}"
    step "smoke (${backend}) — build" "log: ${WORK}/build-${backend}.log"
    unity 2400 "${WORK}/build-${backend}.log" -executeMethod "CompatBuild.${method}" -quit
    record "build_${backend}_exit" $?
    out="${WORK}/smoke-${backend}.txt"
    rm -f "${out}"
    app="${PROJECT}/Build/${backend}/Compat.app/Contents/MacOS/Compat"
    if [[ ! -x "${app}" ]]; then
      record "smoke_${backend}_exit" missing
      continue
    fi
    step "smoke (${backend}) — run" "log: ${WORK}/smoke-${backend}-player.log"
    run_limited 300 "${app}" -batchmode -nographics -logFile "${WORK}/smoke-${backend}-player.log" -compatSmoke "${out}" > /dev/null 2>&1
    record "smoke_${backend}_exit" $?
  done
}

# fetch <name> <url> — downloads and unpacks once into ${DOWNLOADS}/<name>
fetch() {
  local name="$1" url="$2" archive="${DOWNLOADS}/$1.download"
  if [[ -d "${DOWNLOADS}/${name}" ]]; then return 0; fi
  mkdir -p "${DOWNLOADS}/${name}.tmp"
  if ! curl -fsSL --retry 2 -o "${archive}" "${url}"; then
    echo "${C_RED}Download failed:${C_RST} ${url}" >&2
    return 1
  fi
  case "${url}" in
    *.nupkg) unzip -q -o "${archive}" -d "${DOWNLOADS}/${name}.tmp" ;;
    *)       tar -xzf "${archive}" -C "${DOWNLOADS}/${name}.tmp" ;;
  esac || return 1
  mv "${DOWNLOADS}/${name}.tmp" "${DOWNLOADS}/${name}"
  rm -f "${archive}"
}

fetch_alternatives() {
  local nuget=https://api.nuget.org/v3-flatcontainer
  fetch polysharp-1.15.0 "${nuget}/polysharp/1.15.0/polysharp.1.15.0.nupkg" &&
  fetch polysharp-1.16.0 "${nuget}/polysharp/1.16.0/polysharp.1.16.0.nupkg" &&
  fetch polyfilllib-11.4.1 "${nuget}/polyfilllib/11.4.1/polyfilllib.11.4.1.nupkg" &&
  fetch polyfill-11.4.1 "${nuget}/polyfill/11.4.1/polyfill.11.4.1.nupkg" &&
  fetch meziantou.polyfill-1.0.165 "${nuget}/meziantou.polyfill/1.0.165/meziantou.polyfill.1.0.165.nupkg" &&
  fetch moderncsharpforunity-0.1.0 https://github.com/iAcolyte/ModernCSharpForUnity/archive/refs/tags/v0.1.0.tar.gz &&
  fetch isexternalinit-1.0.0 https://package.openupm.com/games.corundum.isexternalinit/-/games.corundum.isexternalinit-1.0.0.tgz
}

# Compiles Assets/Cases/C1_Polyfill/Probe.cs once per case with the csc command line that Bee ran
# for Case.C1_Polyfill in the editor, and counts the features that compile. A case whose assembly
# fails outside the probe (a generator's own error, for one) counts 0.
step_alternatives() {
  begin alternatives alternatives_exit
  step "alternatives" "outputs: ${WORK}/alternatives"
  fetch_alternatives || { record alternatives_exit download; return; }
  python3 - "${PROJECT}" "${WORK}" "${DOWNLOADS}" "${TEMPLATE}" "${VERSION}" <<'PY'
import collections, glob, json, os, re, shlex, subprocess, sys

project, work, downloads, template, version = sys.argv[1:6]
out_dir = os.path.join(work, "alternatives")
os.makedirs(out_dir, exist_ok=True)
SOURCE = '"Assets/Cases/C1_Polyfill/Probe.cs"'

def one(pattern):
    hits = sorted(glob.glob(os.path.join(downloads, pattern)))
    if not hits:
        sys.exit(f"missing download: {pattern}")
    return hits[0]

def unity_requirement(package_json):
    required = json.load(open(package_json, encoding="utf-8")).get("unity")
    as_tuple = lambda s: tuple(int(x) for x in re.findall(r"\d+", s)[:2])
    return f"package requires Unity {required}" if required and as_tuple(version) < as_tuple(required) else None

polysharp_115 = f'-analyzer:"{one("polysharp-1.15.0/analyzers/dotnet/cs/PolySharp.SourceGenerators.dll")}"'
polysharp_116 = f'-analyzer:"{one("polysharp-1.16.0/analyzers/dotnet/cs/PolySharp.SourceGenerators.dll")}"'
polysharp_runtime = f'-analyzerconfig:"{os.path.join(template, "Alternatives", "polysharp-runtime.globalconfig")}"'
polysharp_config = f'-analyzerconfig:"{os.path.join(template, "Alternatives", "polysharp.globalconfig")}"'
corundum_dll = f'-r:"{one("isexternalinit-1.0.0/package/IsExternalInit.System.Runtime.CompilerServices.dll")}"'

# (label, drop the package reference, extra arguments, note)
CASES = [
    ("No polyfill", True, [], None),
    ("xpTURN.Polyfill (this repository)", False, [], None),
    ("PolySharp 1.15.0", True, [polysharp_115], None),
    ("PolySharp 1.16.0", True, [polysharp_116], None),
    ("PolySharp 1.16.0, runtime-supported attributes on", True, [polysharp_116, polysharp_runtime], None),
    ("PolySharp 1.16.0, runtime-supported attributes on, embedded attribute off", True, [polysharp_116, polysharp_config], None),
    ("PolyfillLib 11.4.1", True, [f'-r:"{one("polyfilllib-11.4.1/lib/netstandard2.1/PolyfillLib.dll")}"'], None),
    ("Polyfill 11.4.1 (source-only)", True, [f'-recurse:"{one("polyfill-11.4.1/contentFiles/cs/netstandard2.1")}/*.cs"'], None),
    ("Meziantou.Polyfill 1.0.165", True, [f'-analyzer:"{one("meziantou.polyfill-1.0.165/analyzers/dotnet/cs/Meziantou.Polyfill.dll")}"'], None),
    ("ModernCSharpForUnity 0.1.0", True, [f'-analyzer:"{one("moderncsharpforunity-0.1.0/*/Generators~/ModernCSharp.Generators.dll")}"'],
     unity_requirement(one("moderncsharpforunity-0.1.0/*/package.json"))),
    ("IsExternalInit (Corundum) 1.0.0", True, [corundum_dll], unity_requirement(one("isexternalinit-1.0.0/package/package.json"))),
    ("xpTURN.Polyfill + IsExternalInit DLL", False, [corundum_dll], None),
    ("xpTURN.Polyfill + PolySharp 1.16.0, both settings", False, [polysharp_116, polysharp_config], None),
]

# The csc command line Bee ran for the probe in the editor (not a player build).
action = None
dags = [p for p in glob.glob(os.path.join(project, "Library/Bee/*.dag.json")) if re.match(r"^[0-9a-f]+E(Dbg)?\.dag\.json$", os.path.basename(p))]
for dag in sorted(dags, key=os.path.getmtime, reverse=True):
    for node in json.load(open(dag, encoding="utf-8")).get("Nodes", []):
        if str(node.get("Annotation", "")).startswith("Csc ") and any(o.endswith("/Case.C1_Polyfill.dll") for o in node.get("Outputs") or []):
            action = node.get("Action")
            break
    if action:
        break
if not action:
    sys.exit("no csc command for Case.C1_Polyfill in Library/Bee — run the gate first")

args = shlex.split(action)
responses = [a for a in args if a.startswith("@")]
command = [a for a in args if not a.startswith("@") and a != "/shared"]
base = open(os.path.join(project, responses[0][1:]), encoding="utf-8").read().splitlines()

probe_lines = open(os.path.join(project, "Assets/Cases/C1_Polyfill/Probe.cs"), encoding="utf-8").read().splitlines()
feature_of = {}
for number, text in enumerate(probe_lines, 1):
    m = re.search(r"\bP(\d\d)_", text)
    if m:
        feature_of[number] = int(m.group(1))
features = set(range(5, 16)) | ({16} if "-define:UNITY_6000_5_OR_NEWER" in base else set())

located = re.compile(r"^(.*?)\((\d+),(\d+)\): (error|warning) (\w+): ")
unlocated = re.compile(r"^(error|warning) (\w+): ")
analyzer_warnings = {"CS8032", "CS8033", "CS9057"}

rows = []
for index, (label, drop, extra, note) in enumerate(CASES):
    lines = [l for l in base if not l.startswith(("-out:", "-refout:")) and l != SOURCE
             and not (drop and l.startswith("-r:") and "xpTURN.Polyfill.Runtime" in l)]
    lines += [f'-out:"{os.path.join(out_dir, f"case{index}.dll")}"', SOURCE] + extra
    rsp = os.path.join(out_dir, f"case{index}.rsp")
    open(rsp, "w", encoding="utf-8").write("\n".join(lines) + "\n")
    run = subprocess.run(command + ["@" + rsp] + responses[1:], cwd=project, capture_output=True, text=True)
    output = run.stdout + run.stderr
    open(os.path.join(out_dir, f"case{index}.txt"), "w", encoding="utf-8").write(f"# {label}\n{output}\nexit={run.returncode}\n")

    failed, probe_codes, other_codes, warnings, lang = set(), collections.Counter(), collections.Counter(), set(), False
    for line in output.splitlines():
        m = located.match(line)
        if m:
            path, number, kind, code = m.group(1), int(m.group(2)), m.group(4), m.group(5)
            if kind == "warning":
                if code in analyzer_warnings: warnings.add(code)
            elif path.replace("\\", "/").endswith("Assets/Cases/C1_Polyfill/Probe.cs"):
                feature = feature_of.get(number)
                if feature in features: failed.add(feature); probe_codes[code] += 1
                elif feature == 17: lang = True
                else: other_codes[code] += 1
            else:
                other_codes[code] += 1
            continue
        m = unlocated.match(line)
        if m:
            if m.group(1) == "warning":
                if m.group(2) in analyzer_warnings: warnings.add(m.group(2))
            else:
                other_codes[m.group(2)] += 1
    count = 0 if other_codes else len(features - failed)
    notes = [c for c, _ in (other_codes or probe_codes).most_common(2)] + sorted(warnings)
    if lang: notes.append("language features fail")
    if note: notes.append(note)
    rows.append((label, count, len(features), ", ".join(notes)))

with open(os.path.join(work, "alternatives.tsv"), "w", encoding="utf-8") as f:
    for row in rows:
        f.write("\t".join(str(x) for x in row) + "\n")
for label, count, total, note in rows:
    print(f"  {label:<74} {count:>2} / {total:<2} {note}")
PY
  record alternatives_exit $?
}

# ── Final report ─────────────────────────────────────────────────────────────
# Prints the steps that this run asked for. With --update-docs, also writes each editor's latest
# results into the tables between the compat: comments of docs/Compatibility.md (one column per editor;
# the other columns stay as they are).
print_summary() {
  section "Final report"
  WORK_ROOT="${WORK_ROOT}" DOCS_FILE="${DOCS_FILE}" UPDATE_DOCS=${UPDATE_DOCS} VERSIONS="${VERSIONS[*]}" \
  RUN_GATE=${RUN_GATE} RUN_SMOKE=${RUN_SMOKE} RUN_PLAYER_TESTS=${RUN_PLAYER_TESTS} RUN_NETFX=${RUN_NETFX} \
  RUN_ALTERNATIVES=${RUN_ALTERNATIVES} python3 - <<'PY'
import os, re, sys
import xml.etree.ElementTree as ET

tty = sys.stdout.isatty()
RED = "\033[31m" if tty else ""
GRN = "\033[32m" if tty else ""
BLD = "\033[1m" if tty else ""
RST = "\033[0m" if tty else ""

root = os.environ["WORK_ROOT"]
versions = os.environ["VERSIONS"].split()
want = lambda name: os.environ.get(name) == "1"

def newer_than_6000_5(version):
    major, minor = (int(x) for x in version.split(".")[:2])
    return (major, minor) >= (6000, 5)

def status_of(work):
    status = {}
    try:
        for line in open(os.path.join(work, "status.txt"), encoding="utf-8"):
            key, _, value = line.strip().partition("=")
            status[key] = value
    except OSError:
        pass
    return status

# Each result is (detail, ok, cell); the cell is what docs/Compatibility.md shows.
def tests_result(work, status, slug):
    exit_code = status.get(f"tests_{slug}_exit", "not run")
    try:
        run = ET.parse(os.path.join(work, f"results-{slug}.xml")).getroot()
    except Exception:
        return (f"no results, exit {exit_code}", False, f"FAIL: no results, exit {exit_code}")
    total, passed, failed = int(run.get("total", 0)), int(run.get("passed", 0)), int(run.get("failed", 0))
    inconclusive, skipped = int(run.get("inconclusive", 0)), int(run.get("skipped", 0))
    ok = exit_code == "0" and total > 0 and failed == 0 and inconclusive == 0
    detail = f"{total} total, {passed} passed, {failed} failed, {inconclusive} inconclusive, {skipped} skipped, exit {exit_code}"
    return (detail, ok, f"{passed} / {total}" if ok else f"FAIL: {passed} / {total}, exit {exit_code}")

def probe_result(work, status, version):
    dll = os.path.isfile(os.path.join(work, "Project/Library/ScriptAssemblies/Case.C1_Polyfill.dll"))
    total = 11 + (1 if newer_than_6000_5(version) else 0)
    failing = set()
    try:
        log = open(os.path.join(work, "tests-editor.log"), encoding="utf-8", errors="replace").read()
        failing = {int(n) for n in re.findall(r"Assets/Cases/C1_Polyfill/Probe\.cs\((\d+),\d+\): error", log)}
    except OSError:
        pass
    passed = total if dll else max(0, total - len(failing))
    ok = dll and status.get("apply_exit") == "0"
    detail = f"{passed}/{total}, Case.C1_Polyfill.dll {'present' if dll else 'missing'}, apply exit {status.get('apply_exit', 'not run')}"
    return (detail, ok, f"{passed} / {total}" if ok else f"FAIL: {passed} / {total}")

def smoke_result(work, status, backend, version):
    expected = 12 + (6 if newer_than_6000_5(version) else 0)
    try:
        lines = [l.strip().split("|") for l in open(os.path.join(work, f"smoke-{backend}.txt"), encoding="utf-8") if l.startswith("SMOKE|")]
    except OSError:
        detail = f"no output, build exit {status.get(f'build_{backend}_exit', 'not run')}, run exit {status.get(f'smoke_{backend}_exit', 'not run')}"
        return (detail, False, "FAIL: no output")
    checks = [l for l in lines if len(l) >= 4 and l[2] not in ("end", "fatal")]
    bad = [f"{l[2]}={l[3]}" for l in checks if l[3] != "PASS"] + ["fatal" for l in lines if len(l) >= 3 and l[2] == "fatal"]
    done = any(len(l) >= 4 and l[2] == "end" for l in lines)
    passed = sum(1 for l in checks if l[3] == "PASS")
    ok = done and not bad and len(checks) == expected
    detail = f"{passed}/{expected} passed" + (f", failing: {'; '.join(bad[:4])}" if bad else "") + ("" if done else ", no end line")
    return (detail, ok, f"{passed} / {expected}" if ok else f"FAIL: {passed} / {expected}")

# (option that runs the step, status key written by the step, label, docs label, result)
STEPS = [
    ("RUN_GATE", "apply_exit", "probe", "Probe: type-dependent features", lambda w, s, v: probe_result(w, s, v)),
    ("RUN_GATE", "tests_editor_exit", "tests (editor)", "Package tests: editor, .NET Standard 2.1", lambda w, s, v: tests_result(w, s, "editor")),
    ("RUN_NETFX", "tests_netfx_exit", "tests (.NET Framework)", "Package tests: editor, .NET Framework", lambda w, s, v: tests_result(w, s, "netfx")),
    ("RUN_PLAYER_TESTS", "tests_player_exit", "tests (IL2CPP player)", "Package tests: IL2CPP player", lambda w, s, v: tests_result(w, s, "player")),
    ("RUN_SMOKE", "build_mono_exit", "smoke mono", "Smoke: Mono player", lambda w, s, v: smoke_result(w, s, "mono", v)),
    ("RUN_SMOKE", "build_il2cpp_exit", "smoke il2cpp", "Smoke: IL2CPP player", lambda w, s, v: smoke_result(w, s, "il2cpp", v)),
]
MATRIX_ORDER = [step[3] for step in STEPS] + ["Measured"]

failed_any = False
matrix, alternatives, alternative_order = {}, {}, []
for version in versions:
    work = os.path.join(root, version)
    status = status_of(work)
    print(f"{BLD}Unity {version}{RST}")
    if status.get("editor") in ("missing", "unprepared"):
        reason = "editor not found" if status["editor"] == "missing" else "work project could not be prepared"
        print(f"  {RED}{reason}{RST}"); failed_any = True; continue
    rows, cells = [], {}
    for option, key, label, docs_label, result in STEPS:
        if not want(option) and key not in status:
            continue
        detail, ok, cell = result(work, status, version)
        if want(option):
            rows.append((label, detail, ok))
        if key in status:
            cells[docs_label] = cell
    dates = [value for key, value in status.items() if key.endswith("_date") and key not in ("editor_date", "alternatives_date")]
    if cells and dates:
        cells["Measured"] = max(dates)
        matrix[version] = cells
    tsv = os.path.join(work, "alternatives.tsv")
    if status.get("alternatives_exit") == "0" and os.path.isfile(tsv):
        found = [l.rstrip("\n").split("\t") for l in open(tsv, encoding="utf-8")]
        alternatives[version] = {r[0]: f"{r[1]} / {r[2]}" + (f", {r[3]}" if len(r) > 3 and r[3] else "") for r in found}
        alternatives[version]["Measured"] = status.get("alternatives_date", "")
        alternative_order += [r[0] for r in found if r[0] not in alternative_order]
    if want("RUN_ALTERNATIVES"):
        ok = version in alternatives
        rows.append(("alternatives", f"{len(alternatives[version]) - 1} cases compiled" if ok else f"no results, exit {status.get('alternatives_exit', 'not run')}", ok))
    for label, detail, ok in rows:
        print(f"  {GRN if ok else RED}{'PASS' if ok else 'FAIL'}{RST}  {label:<24} {detail}")
        failed_any |= not ok

def table(first, columns, order, old=None):
    old_columns, old_rows = old or ([], {})
    names = sorted(set(old_columns) | set(columns), key=lambda v: [int(x) for x in re.findall(r"\d+", v)])
    labels = [l for l in order if l in old_rows or any(l in c for c in columns.values())]
    labels += [l for l in old_rows if l not in labels]
    lines = [f"| {first} | " + " | ".join(names) + " |", "|" + "---|" * (len(names) + 1)]
    for label in labels:
        row = [columns[n].get(label, "") if n in columns else old_rows.get(label, {}).get(n, "") for n in names]
        lines.append(f"| {label} | " + " | ".join(row) + " |")
    return "\n".join(lines)

def parse_table(body):
    lines = [l.strip() for l in body.splitlines() if l.strip().startswith("|")]
    if len(lines) < 2:
        return [], {}
    split = lambda l: [c.strip() for c in l.strip("|").split("|")]
    columns = split(lines[0])[1:]
    return columns, {cells[0]: dict(zip(columns, cells[1:])) for cells in map(split, lines[2:])}

if want("RUN_ALTERNATIVES") and alternatives:
    measured = {v: alternatives[v] for v in versions if v in alternatives}
    print(f"\n{BLD}Alternatives{RST}\n" + table("Option", measured, alternative_order + ["Measured"]))

if want("UPDATE_DOCS"):
    path = os.environ["DOCS_FILE"]
    try:
        text = open(path, encoding="utf-8").read()
    except OSError:
        print(f"{RED}Missing {path}{RST}"); sys.exit(1)
    for name, first, columns, order in (("matrix", "Check", matrix, MATRIX_ORDER), ("alternatives", "Option", alternatives, alternative_order + ["Measured"])):
        if not columns:
            continue
        begin, end = f"<!-- compat:{name} -->", f"<!-- /compat:{name} -->"
        i, j = text.find(begin), text.find(end)
        if i < 0 or j < i:
            print(f"{RED}{path} has no {begin} block{RST}"); failed_any = True; continue
        body = table(first, columns, order, parse_table(text[i + len(begin):j]))
        text = text[:i + len(begin)] + "\n" + body + "\n" + text[j:]
    open(path, "w", encoding="utf-8").write(text)
    print(f"\nUpdated {path}")

sys.exit(1 if failed_any else 0)
PY
}

# ── Main ─────────────────────────────────────────────────────────────────────
if [[ ${RUN_PLAYER_TESTS} -eq 1 ]]; then
  caffeinate -d -i -u -w $$ &
fi

for VERSION in "${VERSIONS[@]}"; do
  section "Unity ${VERSION}"
  CONTENTS="${EDITORS_DIR}/${VERSION}/Unity.app/Contents"
  UNITY="${CONTENTS}/MacOS/Unity"
  WORK="${WORK_ROOT}/${VERSION}"
  PROJECT="${WORK}/Project"
  if [[ ! -x "${UNITY}" ]]; then
    echo "${C_RED}Unity ${VERSION} not found:${C_RST} ${UNITY}" >&2
    mkdir -p "${WORK}"; begin editor editor; record editor missing
    continue
  fi
  if ! prepare_project; then
    echo "${C_RED}Could not prepare the work project:${C_RST} ${PROJECT}" >&2
    mkdir -p "${WORK}"; begin editor editor; record editor unprepared
    continue
  fi
  begin editor editor
  if [[ ${RUN_GATE} -eq 1 ]]; then step_gate; fi
  if [[ ${RUN_NETFX} -eq 1 ]]; then step_net_framework; fi
  if [[ ${RUN_PLAYER_TESTS} -eq 1 ]]; then step_player_tests; fi
  if [[ ${RUN_SMOKE} -eq 1 ]]; then step_smoke; fi
  if [[ ${RUN_ALTERNATIVES} -eq 1 ]]; then step_alternatives; fi
done

print_summary
