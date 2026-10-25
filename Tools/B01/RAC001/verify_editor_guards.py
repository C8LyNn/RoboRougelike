#!/usr/bin/env python3
"""RAC001 editor guard static regression; does not compile or launch Unity."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "Assets/RoboRougelikeMechanics/RAC001CheckpointRace/Editor/RAC001DemoSceneBuilder.cs"

def verify(src: str):
    stack = []
    directives = []
    for line_no, raw in enumerate(src.splitlines(), 1):
        s = raw.strip()
        if s.startswith("#if "):
            stack.append(line_no)
            directives.append(s.split()[0])
        elif s.startswith("#elif ") or s == "#else":
            assert stack, (line_no, "orphan #elif or #else")
        elif s.startswith("#endif"):
            assert stack, (line_no, "orphan #endif")
            stack.pop()
            directives.append("#endif")
    assert not stack, ("unclosed #if lines", stack)
    assert src.lstrip().startswith("#if UNITY_EDITOR"), "must guard UnityEditor imports"
    assert src.rstrip().endswith("#endif // UNITY_EDITOR"), "missing terminal guard"
    assert directives == ["#if", "#endif"], directives

if __name__ == "__main__":
    # A deliberately broken guard must fail the same test: real RED regression.
    assert_source = "#if UNITY_EDITOR\nusing UnityEditor;\n"
    try:
        verify(assert_source)
        raise AssertionError("negative missing-endif fixture incorrectly accepted")
    except AssertionError as e:
        assert "unclosed" in str(e), e
    verify(SOURCE.read_text(encoding="utf-8"))
    print("STATIC_PASS: RAC001 UNITY_EDITOR #if/#endif balanced, negative fixture rejected")
    print("UNITY_COMPILE=NOT_RUN; EDITMODE=NOT_RUN; PLAYMODE=NOT_RUN")
