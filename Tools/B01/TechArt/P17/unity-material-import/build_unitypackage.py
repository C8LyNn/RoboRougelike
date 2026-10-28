#!/usr/bin/env python3
"""MAT-IMPORT-001: a real, deterministic and self-contained Unity .unitypackage.

Uses ONLY original MAT-A1-001 generated RGBA PNGs from exact producer SHA.
Packages 8 source PNG assets, 1 optional Unity Editor installer and 1 README.
It never touches Assets/ or a Codex worktree. Unity/URP runtime is NOT_RUN.
"""
from __future__ import annotations
import argparse
import gzip
import hashlib
import io
import json
from pathlib import Path
import tarfile

ROOT = "Assets/B0130MATPack01"
PROFILES = ("tire_rubber", "brushed_arm_alloy")
MAPS = ("basecolor", "normal", "metallic_smoothness", "occlusion")
PACKAGE_ID = "B01-30-MAT-IMPORT-001"
SOURCE_COMMIT = "171983f85ff41df6ef33b95939cc877ba68e03ca"
SOURCE_GENERATOR_BLOB = "171d53bc7d5a4840a067af5c934a8fd1c7ad774e"


def stable_guid(path: str) -> str:
    # 128-bit unique deterministic namespace for this throwaway asset pack.
    return hashlib.sha256((PACKAGE_ID + "/v1/" + path).encode()).hexdigest()[:32]


def texture_meta(path: str, normal: bool, base: bool) -> bytes:
    # Importable Unity meta with explicitly named channel/color data.
    # Editor receiver corrects import options again in the target Unity version.
    s = f"""fileFormatVersion: 2
guid: {stable_guid(path)}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
  isReadable: 0
  sRGBTexture: {1 if base else 0}
  textureType: {1 if normal else 0}
  textureShape: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""
    return s.encode("utf-8")


def editor_meta(path: str) -> bytes:
    return f"""fileFormatVersion: 2
guid: {stable_guid(path)}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData:
  assetBundleName:
  assetBundleVariant:
""".encode("utf-8")


def text_meta(path: str) -> bytes:
    return f"""fileFormatVersion: 2
guid: {stable_guid(path)}
TextScriptImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
""".encode("utf-8")


def package_assets(generated: Path, installer: Path) -> list[dict]:
    original = json.loads((generated / "material-manifest.json").read_text(encoding="utf-8"))
    assert original["package_id"] == "B01-30-MAT-A1-001"
    assert original["version"] == "R1"
    assert original["size"] == 512
    file_manifest = {entry["path"]: entry for entry in original["textures"]}
    assert len(file_manifest) == 9, "Eight textures plus one contact sheet expected"
    assets = []
    for family in PROFILES:
        for kind in MAPS:
            rel = f"{family}/{family}_{kind}.png"
            source = generated / rel
            data = source.read_bytes()
            assert data.startswith(bytes.fromhex("89504e470d0a1a0a"))
            assert hashlib.sha256(data).hexdigest() == file_manifest[rel]["sha256"]
            assert len(data) == file_manifest[rel]["bytes"]
            dest = f"{ROOT}/Textures/{rel}"
            assets.append({"path": dest, "bytes": data,
                           "meta": texture_meta(dest, kind == "normal", kind == "basecolor"),
                           "input_sha256": hashlib.sha256(data).hexdigest()})
    editor_path = f"{ROOT}/Editor/MatA1Installer.cs"
    editor_bytes = installer.read_bytes()
    assert b"namespace B0130.MaterialImport" in editor_bytes
    assert b"Shader.Find(ShaderName)" in editor_bytes
    assert b"[MenuItem(" in editor_bytes
    assets.append({"path":editor_path,"bytes":editor_bytes,
                   "meta":editor_meta(editor_path),
                   "input_sha256":hashlib.sha256(editor_bytes).hexdigest()})
    readme_path = f"{ROOT}/README.txt"
    text = ("""MAT-IMPORT-001 / B01-30 PASS17
This package contains eight real RGBA PNGs from original MAT-A1-001.
Unity 6000.3.14f1, Universal Render Pipeline is required for the importer.
Open: Tools > B01 > MAT-A1-001 > Create candidate URP materials.
Two standalone Materials appear in Assets/B0130MATPack01/Materials/.
No gameplay or old Assets files are overwritten by the package installer.
All asset pixels, materials and palette are CANDIDATE and NOT HQ approved.
Delete only Assets/B0130MATPack01 to rollback (after restoring references).
Native Unity visual acceptance and G1 were NOT RUN by B01-30.
""").encode("utf-8")
    assets.append({"path":readme_path,"bytes":text,"meta":text_meta(readme_path),
                   "input_sha256":hashlib.sha256(text).hexdigest()})
    return sorted(assets,key=lambda e:e["path"])


def tar_entry(tf: tarfile.TarFile, path: str, data: bytes) -> None:
    info = tarfile.TarInfo(path)
    info.size = len(data)
    info.mtime = 0
    info.uid = info.gid = 0
    info.uname = info.gname = ""
    info.mode = 0o644
    tf.addfile(info, io.BytesIO(data))


def build_unitypackage(assets: list[dict]) -> bytes:
    buffer = io.BytesIO()
    with gzip.GzipFile(fileobj=buffer, mode="wb", compresslevel=9,
                       mtime=0, filename="") as compressed:
        with tarfile.open(fileobj=compressed, mode="w", format=tarfile.USTAR_FORMAT) as tar:
            for item in assets:
                path = item["path"]
                guid = stable_guid(path)
                for suffix, content in (
                    ("asset", item["bytes"]),
                    ("asset.meta", item["meta"]),
                    ("pathname", (path + "\n").encode("utf-8")),
                ):
                    tar_entry(tar, guid + "/" + suffix, content)
    return buffer.getvalue()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--original", required=True, help="exact original generator output folder")
    parser.add_argument("--out", required=True, help="disposable output directory")
    args = parser.parse_args()
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    installer = Path(__file__).with_name("MatA1Installer.cs")
    assets = package_assets(Path(args.original), installer)
    package = out / "B01-30-MAT-IMPORT-001-R1.unitypackage"
    binary = build_unitypackage(assets)
    package.write_bytes(binary)
    assert len(assets) == 10
    assert len(binary) > 100000
    receipt = {
        "package_id":PACKAGE_ID,
        "version":"R1",
        "producer_original_exact_commit":SOURCE_COMMIT,
        "producer_original_generator_git_blob":SOURCE_GENERATOR_BLOB,
        "status":"UNITYPACKAGE_BINARY_CREATED_NOT_UNITY_IMPORTED",
        "file":package.name,
        "package_sha256":hashlib.sha256(binary).hexdigest(),
        "assets_count":len(assets),
        "original_generated_texture_count":8,
        "shader_target":"Universal Render Pipeline/Lit",
        "installer_menu":"Tools/B01/MAT-A1-001/Create candidate URP materials",
        "assets":[{"path":item["path"],"guid":stable_guid(item["path"]),
                   "byte_size":len(item["bytes"]),
                   "sha256":item["input_sha256"]} for item in assets],
        "not_run":["Unity 6000.3.14f1 importer","Unity material display",
                   "Unity shader compilation","G1 independent QA","G2 integration"]
    }
    (out / "MAT-IMPORT-001-receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
    print("B01_30_MAT_IMPORT_001_REAL_UNITYPACKAGE_GENERATED",
          json.dumps({"assets":len(assets),"textures":8,
                      "file_bytes":len(binary),"sha256":receipt["package_sha256"]},sort_keys=True))


if __name__ == "__main__":
    main()
