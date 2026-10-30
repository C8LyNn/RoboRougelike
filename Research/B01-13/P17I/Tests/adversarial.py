#!/usr/bin/env python3
"""Independent black-box negative controls for the frozen original C# trace replayer."""
import hashlib
import pathlib
import subprocess
import sys
import tempfile

HEADER = "trace,t_ms,x_mm,z_mm,held,state,event,stalls,recoveries,discontinuities"
ORIGINAL = "blocked100,2000,0,0,1,SuspectedStall,1,1,0,0"
INCORRECT = "blocked100,2000,0,0,1,Idle,1,1,0,0"
TIME = "blocked250,250,0,0,1,Attempting,0,0,0,0"
REWOUND_TIME = "blocked250,0,0,0,1,Attempting,0,0,0,0"

def main(dll, fixture, receipt):
    original = pathlib.Path(fixture).read_text(encoding="utf-8")
    assert original.startswith(HEADER + "\n")
    assert original.count(ORIGINAL) == 1, "FROZEN_GOLDEN_TRACE_CHANGED"
    assert original.count(TIME) == 1, "FROZEN_SECOND_TRACE_CHANGED"
    patterns = [
        ("false_expected_state", original.replace(ORIGINAL, INCORRECT, 1), 1, "MISMATCH_TRACE"),
        ("invalid_header", original.replace(HEADER, HEADER + ",extra", 1), 2, "BAD_HEADER_OR_NO_SAMPLES"),
        ("non_monotonic_time", original.replace(TIME, REWOUND_TIME, 1), 2, "NON_MONOTONIC_TIME"),
    ]
    receipt_lines = ["EXACT_ORIGINAL_TRACE_SHA256 " +
                     hashlib.sha256(original.encode("utf-8")).hexdigest()]
    with tempfile.TemporaryDirectory(prefix="b01-13-w11-negative-") as tmp:
        for name, content, code, expected in patterns:
            path = pathlib.Path(tmp, name + ".csv")
            path.write_text(content, encoding="utf-8", newline="\n")
            result = subprocess.run(["dotnet", str(pathlib.Path(dll).resolve()), str(path)],
                                    text=True, capture_output=True, timeout=30, check=False)
            output = result.stdout + result.stderr
            if result.returncode != code or expected not in output:
                raise AssertionError("NEGATIVE_FAILED " + name + " rc=" +
                                     str(result.returncode) + " output=" + output[:800])
            receipt_lines.append("NEGATIVE_PASS " + name + " rc=" + str(result.returncode)
                                 + " evidence=" + expected)
            print(receipt_lines[-1])
    receipt_lines.append("NEGATIVE_TOTAL 3 PASS 0 FAIL")
    pathlib.Path(receipt).parent.mkdir(parents=True, exist_ok=True)
    pathlib.Path(receipt).write_text("\n".join(receipt_lines) + "\n", encoding="utf-8")
    print(receipt_lines[-1])

if __name__ == "__main__":
    if len(sys.argv) != 4:
        sys.exit("USAGE adversarial.py <compiled-DLL> <golden-csv> <negative-receipt>")
    try:
        main(*sys.argv[1:])
    except (AssertionError, OSError, ValueError, subprocess.SubprocessError) as exc:
        print("NEGATIVE_FAIL " + str(exc), file=sys.stderr)
        sys.exit(1)
