#!/usr/bin/env python3
"""B01-40: read-only exact-Git-object receiver scan; no Unity or package import."""
import argparse
import collections
import hashlib
import json
import pathlib
import re
import subprocess
import unittest

PINS = {
    "CODEX": "231005c6d0351945a8ee6684f3856af118f0e94f",
    "CHL": "2355bff6026e22443b164aa9de816c27d3a289e9",
    "PARTSIZE": "be2be0e4802bc3f450179901f9ae91c6d84bcb5b",
}
EXPECTED_ROOTS = {
    "CHL": ("Assets/RoboRougelikeChallenges/", "Assets/RoboRougelikeChallenges.meta"),
    "PARTSIZE": ("Assets/PARTSIZE001/", "Assets/PARTSIZE001.meta"),
}
GUID_RE = re.compile(rb"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$")
NS_RE = re.compile(r"(?m)^\s*namespace\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\s*(?:[;{]|\r?$)")
TYPE_RE = re.compile(r"(?m)^\s*(?:(?:public|private|internal|protected|sealed|static|abstract|partial|readonly|unsafe|new)\s+)*(?:class|struct|interface|enum|record)\s+([A-Za-z_]\w*)\b")

def cmd(repo, *args):
    return subprocess.check_output(["git", "-C", repo, *args], stderr=subprocess.PIPE)

def tree(repo, commit):
    raw = cmd(repo, "ls-tree", "-rz", commit)
    out = {}
    for record in raw.split(b"\x00"):
        if not record:
            continue
        h, path = record.split(b"\t", 1)
        mode, kind, blob_sha = h.split(b" ")
        if kind == b"blob":
            name = path.decode("utf-8")
            if name in out:
                raise ValueError("DUPLICATE_GIT_TREE_PATH:" + name)
            out[name] = blob_sha.decode("ascii")
    return out

def read_blobs(repo, sha_list):
    keys = sorted(set(sha_list))
    request = ("\n".join(keys) + "\n").encode("ascii")
    raw = subprocess.run(["git", "-C", repo, "cat-file", "--batch"],
                         input=request, capture_output=True, check=True).stdout
    blobs = {}
    idx = 0
    for expected in keys:
        line_end = raw.find(b"\n", idx)
        if line_end < 0:
            raise ValueError("TRUNCATED_GIT_BATCH")
        actual_sha, kind, size = raw[idx:line_end].split(b" ")
        if actual_sha.decode("ascii") != expected or kind != b"blob":
            raise ValueError("GIT_BLOB_IDENTITY_MISMATCH:" + expected)
        length = int(size)
        contents = raw[line_end + 1:line_end + 1 + length]
        if len(contents) != length or raw[line_end + 1 + length:line_end + 2 + length] != b"\n":
            raise ValueError("TRUNCATED_GIT_BLOB:" + expected)
        blobs[expected] = contents
        idx = line_end + 2 + length
    if idx != len(raw):
        raise ValueError("EXTRA_GIT_BATCH_DATA")
    return blobs

def guid_index(tree_map, contents):
    found, defects = {}, []
    for path, oid in tree_map.items():
        if not path.startswith("Assets/") or not path.endswith(".meta"):
            continue
        matches = GUID_RE.findall(contents[oid])
        if len(matches) != 1:
            defects.append({"path": path, "reason": "GUID_COUNT_" + str(len(matches))})
            continue
        found[path] = matches[0].decode("ascii").lower()
    return found, defects

def duplicates(index):
    reverse = collections.defaultdict(list)
    for path, value in index.items():
        reverse[value].append(path)
    return {k: sorted(v) for k, v in reverse.items() if len(v) > 1}

def asmdefs(tree_map, contents):
    names, problems = {}, []
    for path, oid in tree_map.items():
        if not path.startswith("Assets/") or not path.endswith(".asmdef"):
            continue
        try:
            data = json.loads(contents[oid])
            name = data["name"]
            if not isinstance(name, str) or not name.strip():
                raise ValueError("EMPTY_NAME")
            names[path] = name
        except (ValueError, KeyError, UnicodeDecodeError) as ex:
            problems.append({"path": path, "reason": str(ex)})
    return names, problems

def namespaces_and_types(tree_map, contents):
    found_ns, found_types = set(), collections.defaultdict(list)
    for path, oid in tree_map.items():
        if not path.startswith("Assets/") or not path.endswith(".cs"):
            continue
        source = contents[oid].decode("utf-8-sig", errors="replace")
        ns_match = NS_RE.search(source)
        if not ns_match:
            continue
        ns = ns_match.group(1)
        found_ns.add(ns)
        for match in TYPE_RE.finditer(source):
            found_types[ns + "." + match.group(1)].append(path)
    return found_ns, dict(found_types)

def find_version(data):
    match = re.search(rb"(?m)^m_EditorVersion:\s*([^\r\n]+)", data)
    return match.group(1).decode("utf8") if match else "MISSING"

def evaluate(records, contents):
    violations, groups = [], {}
    for group, files in records.items():
        guids, invalid = guid_index(files, contents)
        ass, malformed = asmdefs(files, contents)
        ns, fqtypes = namespaces_and_types(files, contents)
        groups[group] = {"paths": set(files), "guids": guids, "asmdefs": ass,
                         "namespaces": ns, "types": fqtypes,
                         "guid_duplicates": duplicates(guids),
                         "asmdef_duplicates": duplicates(ass)}
        for defect in invalid:
            violations.append(group + ":INVALID_META:" + defect["path"])
        for defect in malformed:
            violations.append(group + ":INVALID_ASMDEF:" + defect["path"])
        for guid in groups[group]["guid_duplicates"]:
            violations.append(group + ":DUPLICATE_INTERNAL_GUID:" + guid)
        for name in groups[group]["asmdef_duplicates"]:
            violations.append(group + ":DUPLICATE_INTERNAL_ASMDEF:" + name)

    for producer in ["CHL", "PARTSIZE"]:
        root, root_meta = EXPECTED_ROOTS[producer]
        escaped = sorted(p for p in records[producer]
                         if p.startswith("Assets/") and not (p.startswith(root) or p == root_meta))
        if escaped:
            violations.append(producer + ":SOURCE_SCOPE_ESCAPE:" + repr(escaped[:5]))
        path_overlap = {p for p in (groups["CODEX"]["paths"] & groups[producer]["paths"])
                        if p.startswith("Assets/")}
        if path_overlap:
            violations.append(producer + ":CODEX_PATH_OVERLAP:" + repr(sorted(path_overlap)[:5]))
        guid_overlap = set(groups["CODEX"]["guids"].values()) & set(groups[producer]["guids"].values())
        if guid_overlap:
            violations.append(producer + ":CODEX_GUID_COLLISION:" + repr(sorted(guid_overlap)))
        names_overlap = set(groups["CODEX"]["asmdefs"].values()) & set(groups[producer]["asmdefs"].values())
        if names_overlap:
            violations.append(producer + ":CODEX_ASMDEF_COLLISION:" + repr(sorted(names_overlap)))
        for f in ["ProjectSettings/ProjectVersion.txt", "Packages/manifest.json"]:
            if f not in records[producer] or f not in records["CODEX"]:
                violations.append(producer + ":MISSING_REQUIRED_HOST_CONFIG:" + f)
            elif contents[records[producer][f]] != contents[records["CODEX"][f]]:
                violations.append(producer + ":HOST_CONFIG_MISMATCH:" + f)
    path_cross = groups["CHL"]["paths"] & groups["PARTSIZE"]["paths"]
    path_cross = sorted(p for p in path_cross if p.startswith("Assets/"))
    if path_cross:
        violations.append("CHL_PART_SOURCE_PATH_OVERLAP:" + repr(path_cross[:5]))
    guid_cross = set(groups["CHL"]["guids"].values()) & set(groups["PARTSIZE"]["guids"].values())
    if guid_cross:
        violations.append("CHL_PART_GUID_COLLISION:" + repr(sorted(guid_cross)))
    asm_cross = set(groups["CHL"]["asmdefs"].values()) & set(groups["PARTSIZE"]["asmdefs"].values())
    if asm_cross:
        violations.append("CHL_PART_ASMDEF_COLLISION:" + repr(sorted(asm_cross)))

    # Same C# namespace is not a C# collision; report only exact probable name overlaps.
    target_types = set(groups["CODEX"]["types"])
    fqn_hints = {}
    for producer in ["CHL", "PARTSIZE"]:
        common = sorted(target_types & set(groups[producer]["types"]))
        fqn_hints[producer] = common  # advisory, regex is not a compiler/parser
    return {
        "status": "STATIC_PUBLIC_GIT_RECEIVER_PASS" if not violations else "STATIC_PUBLIC_GIT_RECEIVER_FAIL",
        "violations": violations,
        "inputs": PINS,
        "groups": {n: {"file_blobs": len(records[n]), "unity_meta_guids": len(g["guids"]),
                       "asmdef_names": len(g["asmdefs"]), "namespaces": len(g["namespaces"]),
                       "files_cs": sum(x.startswith("Assets/") and x.endswith(".cs") for x in records[n]),
                       "unity": find_version(contents[records[n]["ProjectSettings/ProjectVersion.txt"]])
                           if "ProjectSettings/ProjectVersion.txt" in records[n] else "MISSING"}
                   for n,g in groups.items()},
        "possible_type_name_overlap_advisory": fqn_hints,
        "common_namespace_names_advisory": {
            n: sorted(groups["CODEX"]["namespaces"] & groups[n]["namespaces"])
            for n in ["CHL", "PARTSIZE"]},
        "requirements": {"target_private_head": "NOT_VERIFIED", "unity_import": "NOT_RUN",
                         "G1_CHL": "NOT_RUN", "G1_PARTSIZE": "NOT_RUN",
                         "G2": "HOLD", "OFFERED": False, "CODEX_ACK": False},
    }

class NegativeFixtureTests(unittest.TestCase):
    def test_guid_duplicates_caught(self):
        self.assertEqual(duplicates({"a": "123", "b": "123"}), {"123": ["a","b"]})
    def test_distinct_guids_ok(self):
        self.assertFalse(duplicates({"a": "123", "b": "456"}))
    def test_asmdef_duplicate_name_caught(self):
        self.assertIn("Pkg.Runtime", duplicates({"a": "Pkg.Runtime", "b": "Pkg.Runtime"}))
    def test_broken_guids_rejected(self):
        g, defects = guid_index({"Assets/A.meta": "a"}, {"a": b"fileFormatVersion: 2\nguid: xyz\n"})
        self.assertFalse(g)
        self.assertEqual(len(defects), 1)
    def test_meta_valid(self):
        g, defects = guid_index({"Assets/A.meta": "a"}, {"a": b"fileFormatVersion: 2\nguid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\n"})
        self.assertEqual(g["Assets/A.meta"], "a"*32)
        self.assertFalse(defects)
    def test_assembly_json_malformed(self):
        _, errors = asmdefs({"Assets/X.asmdef": "x"}, {"x": b"{broken"})
        self.assertEqual(len(errors), 1)
    def test_namespace_not_equivalent_to_type_collision(self):
        ns, typ = namespaces_and_types({"Assets/X.cs": "x"},
                {"x": b"namespace Example.Clash { public sealed class Hello {} }"})
        self.assertIn("Example.Clash", ns)
        self.assertFalse(typ)  # conservative line-start-only advisory
    def test_guid_case_normalized(self):
        g, _ = guid_index({"Assets/A.meta": "a"}, {"a": b"guid: AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\n"})
        self.assertEqual(g["Assets/A.meta"], "a"*32)
    def test_version_parser(self):
        self.assertEqual(find_version(b"m_EditorVersion: 6000.3.14f1\r\n"), "6000.3.14f1")

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", default=".")
    parser.add_argument("--out")
    parser.add_argument("--selftest", action="store_true")
    a = parser.parse_args()
    if a.selftest:
        suite = unittest.defaultTestLoader.loadTestsFromTestCase(NegativeFixtureTests)
        test = unittest.TextTestRunner(verbosity=2).run(suite)
        raise SystemExit(0 if test.wasSuccessful() else 2)
    repo = str(pathlib.Path(a.repo).resolve())
    raw_records = {name: tree(repo, sha) for name, sha in PINS.items()}
    # Producer commits inherit the entire base game repository. The package
    # being received is ONLY its exclusive source root + root .meta; comparing
    # inherited Assets/ and repo files would invent massive false collisions.
    # Retain project config in the input selection for host compatibility.
    records = {"CODEX": raw_records["CODEX"]}
    host_config = {"ProjectSettings/ProjectVersion.txt", "Packages/manifest.json"}
    for producer, (root, root_meta) in EXPECTED_ROOTS.items():
        records[producer] = {p: oid for p, oid in raw_records[producer].items()
                             if p.startswith(root) or p == root_meta or p in host_config}
        if not any(p.startswith(root) for p in records[producer]) or root_meta not in records[producer]:
            raise ValueError(producer + ":MISSING_EXCLUSIVE_PACKAGE_ROOT")
    relevant = set()
    for files in records.values():
        for p, oid in files.items():
            if (p.startswith("Assets/") and p.endswith((".meta", ".asmdef", ".cs"))) or p in (
                "ProjectSettings/ProjectVersion.txt", "Packages/manifest.json"):
                relevant.add(oid)
    contents = read_blobs(repo, relevant)
    result = evaluate(records, contents)
    result["git_blob_count_read"] = len(contents)
    result["input_tree_sha256"] = hashlib.sha256("\n".join(
        f"{n}\t{p}\t{oid}" for n,files in sorted(records.items()) for p,oid in sorted(files.items())
    ).encode()).hexdigest()
    output = json.dumps(result, indent=2, ensure_ascii=False, sort_keys=True) + "\n"
    print(output)
    if a.out:
        pathlib.Path(a.out).write_text(output, encoding="utf8")
    if result["violations"]:
        raise SystemExit(2)

if __name__ == "__main__":
    main()
