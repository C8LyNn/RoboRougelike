#!/usr/bin/env python3
"""M02-024 independent mecanum roller geometry, in metres (stdlib only)."""
import math

ID = "M02-024"
ROLLER_COUNT = 12
ROLLER_CENTRE_R = 0.355
ROLLER_RADIUS = 0.049
ROLLER_LENGTH = 0.165
ROLLER_TILT_DEG = 45.0
HUB_OUTER_R = 0.122
RIM_MAJOR_R = 0.280
RIM_MINOR_R = 0.013
FRAMES = 121
TAU = 2.0 * math.pi

def add(a, b):
    return tuple(x + y for x, y in zip(a, b))

def sub(a, b):
    return tuple(x - y for x, y in zip(a, b))

def mul(a, k):
    return tuple(x * k for x in a)

def dot(a, b):
    return sum(x * y for x, y in zip(a, b))

def clamp(v, lo, hi):
    return max(lo, min(hi, v))

def roller_pose(index, wheel_angle=0.0):
    """Return centre and unit spindle axis; wheel spins around Blender +Y."""
    theta = TAU * index / ROLLER_COUNT + wheel_angle
    radial = (math.cos(theta), 0.0, math.sin(theta))
    tangent = (-math.sin(theta), 0.0, math.cos(theta))
    tilt = math.radians(ROLLER_TILT_DEG)
    axis = add(mul(tangent, math.cos(tilt)), (0.0, math.sin(tilt), 0.0))
    return mul(radial, ROLLER_CENTRE_R), axis

def segment_distance(a, u, b, v, length=ROLLER_LENGTH):
    """Exact closest distance between finite roller spindle segments."""
    # Segment parameters are 0..1, rather than centred at zero.
    p = sub(a, mul(u, length / 2.0))
    q = sub(b, mul(v, length / 2.0))
    d1, d2, r = mul(u, length), mul(v, length), sub(p, q)
    aa, ee, ff = dot(d1, d1), dot(d2, d2), dot(d2, r)
    bb, cc = dot(d1, d2), dot(d1, r)
    denom = aa * ee - bb * bb
    if denom > 1e-14:
        s = clamp((bb * ff - cc * ee) / denom, 0.0, 1.0)
    else:
        s = 0.0
    t = (bb * s + ff) / ee
    if t < 0.0:
        t = 0.0
        s = clamp(-cc / aa, 0.0, 1.0)
    elif t > 1.0:
        t = 1.0
        s = clamp((bb - cc) / aa, 0.0, 1.0)
    delta = sub(add(p, mul(d1, s)), add(q, mul(d2, t)))
    return math.sqrt(dot(delta, delta))

def wheel_angle(frame):
    return TAU * (frame - 1) / (FRAMES - 1)

def roller_angle(frame):
    # Passive rollers shown spinning for visual inspection, not PhysX torque.
    return -2.0 * TAU * (frame - 1) / (FRAMES - 1)

def analytical_qa():
    assert ROLLER_COUNT == 12
    assert ROLLER_TILT_DEG == 45.0
    assert ROLLER_CENTRE_R - ROLLER_RADIUS > RIM_MAJOR_R + RIM_MINOR_R
    minimum = 1e9
    tests = 0
    for frame in range(1, FRAMES + 1):
        poses = [roller_pose(i, wheel_angle(frame)) for i in range(ROLLER_COUNT)]
        for i in range(ROLLER_COUNT):
            a, u = poses[i]
            assert abs(dot(u, u) - 1.0) < 1e-12
            for j in range(i + 1, ROLLER_COUNT):
                b, v = poses[j]
                gap = segment_distance(a, u, b, v) - 2.0 * ROLLER_RADIUS
                minimum = min(minimum, gap)
                tests += 1
                assert gap > 0.015, (frame, i, j, gap)
    return {
        "poses": FRAMES * ROLLER_COUNT,
        "pairwise_roller_checks": tests,
        "minimum_roller_capsule_clearance_m": round(minimum, 7),
        "radial_rim_clearance_m": round(ROLLER_CENTRE_R - ROLLER_RADIUS - RIM_MAJOR_R - RIM_MINOR_R, 7),
        "roller_tilt_deg": ROLLER_TILT_DEG,
        "collision_method": "analytical capsule/capsule clearance, not triangle BVH or PhysX",
    }

if __name__ == "__main__":
    import json
    print(json.dumps(analytical_qa(), indent=2))
