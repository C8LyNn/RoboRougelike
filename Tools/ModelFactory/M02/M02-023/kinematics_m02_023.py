#!/usr/bin/env python3
"""M02-023 chain-drive analytical geometry, metres, no Blender dependency."""
import math

ID = "M02-023"
PITCH = 0.046
LINKS = 48
BIG_TEETH = 24
SMALL_TEETH = 16
BIG_R = PITCH / (2.0 * math.sin(math.pi / BIG_TEETH))
SMALL_R = PITCH / (2.0 * math.sin(math.pi / SMALL_TEETH))
Z_CENTER = -0.205
Y_PLANE = -0.240
FRAMES = 121
ROLLER_R = 0.008
TOOTH_TIP_GAP = 0.013
PLATE_LENGTH = 0.031
PLATE_HEIGHT = 0.010

def loop_length(distance):
    delta = BIG_R - SMALL_R
    theta = math.asin(delta / distance)
    tangent = math.sqrt(distance * distance - delta * delta)
    return 2.0 * tangent + math.pi * (BIG_R + SMALL_R) + 2.0 * theta * delta

def solve_centres():
    target = LINKS * PITCH
    lo, hi = BIG_R + SMALL_R + 0.12, 1.4
    assert loop_length(lo) < target < loop_length(hi)
    for _ in range(70):
        mid = (lo + hi) * 0.5
        if loop_length(mid) < target:
            lo = mid
        else:
            hi = mid
    return (lo + hi) * 0.5

CENTRES = solve_centres()
LENGTH = LINKS * PITCH
THETA = math.asin((BIG_R - SMALL_R) / CENTRES)
TANGENT = math.sqrt(CENTRES**2 - (BIG_R - SMALL_R)**2)
SMALL_ARC = SMALL_R * (math.pi - 2.0 * THETA)
BIG_ARC = BIG_R * (math.pi + 2.0 * THETA)

def sample(distance):
    """Return X,Z,tangent angle for clockwise continuous path, length in metres."""
    s = distance % LENGTH
    sn, cs = math.sin(THETA), math.cos(THETA)
    if s < TANGENT:
        return (-CENTRES/2 + BIG_R*sn + s*cs,
                Z_CENTER + BIG_R*cs - s*sn, -THETA)
    s -= TANGENT
    if s < SMALL_ARC:
        a = math.pi/2 - THETA - s/SMALL_R
        return (CENTRES/2 + SMALL_R*math.cos(a),
                Z_CENTER + SMALL_R*math.sin(a), a - math.pi/2)
    s -= SMALL_ARC
    if s < TANGENT:
        return (CENTRES/2 + SMALL_R*sn - s*cs,
                Z_CENTER - SMALL_R*cs - s*sn, -math.pi + THETA)
    s -= TANGENT
    a = -math.pi/2 + THETA - s/BIG_R
    return (-CENTRES/2 + BIG_R*math.cos(a),
            Z_CENTER + BIG_R*math.sin(a), a - math.pi/2)

def link_pose(index, frame):
    travel = LENGTH * (frame - 1) / (FRAMES - 1)
    total = index * PITCH + travel
    x,z,angle = sample(total)
    # Unwrap rotation at the seam so no single-frame 360-degree jump.
    angle -= 2.0 * math.pi * int(total // LENGTH)
    return x,z,angle

def sprocket_angles(frame):
    travel = LENGTH * (frame - 1) / (FRAMES - 1)
    return (2.0 * math.pi * travel / (BIG_TEETH * PITCH),
            2.0 * math.pi * travel / (SMALL_TEETH * PITCH))

def _obb_overlap(a,b):
    """2D oriented rectangle SAT: test plate boxes in X/Z plane."""
    x1,z1,t1=a
    x2,z2,t2=b
    dx,dz=x2-x1,z2-z1
    axes=((math.cos(t1),math.sin(t1)),(-math.sin(t1),math.cos(t1)),
          (math.cos(t2),math.sin(t2)),(-math.sin(t2),math.cos(t2)))
    for ax,az in axes:
        distance=abs(dx*ax+dz*az)
        projection=0.0
        for angle in (t1,t2):
            ux,uz=math.cos(angle),math.sin(angle)
            vx,vz=-uz,ux
            projection+=0.5*PLATE_LENGTH*abs(ax*ux+az*uz)
            projection+=0.5*PLATE_HEIGHT*abs(ax*vx+az*vz)
        if distance > projection + 1e-9:
            return False
    return True

def analytical_qa():
    assert abs(loop_length(CENTRES)-LENGTH) < 1e-10
    assert BIG_R > SMALL_R
    assert abs((BIG_TEETH / SMALL_TEETH) - 1.5) < 1e-12
    assert (TOOTH_TIP_GAP - ROLLER_R) > 0.004
    minimum_neighbour = 10.0
    minimum_plate_gap = 10.0
    checks=0
    for frame in range(1, FRAMES+1):
        poses=[link_pose(i,frame) for i in range(LINKS)]
        for i in range(LINKS):
            j=(i+1)%LINKS
            a,b=poses[i],poses[j]
            d=math.hypot(a[0]-b[0],a[1]-b[1])
            minimum_neighbour=min(minimum_neighbour,d)
            minimum_plate_gap=min(minimum_plate_gap,d-PLATE_LENGTH)
            assert d > PLATE_LENGTH + 0.009, (frame,i,d)
            assert not _obb_overlap(a,b), (frame,i,"adjacent plate collision")
            checks+=1
        for i in range(LINKS):
            for j in range(i+2,LINKS):
                if i==0 and j==LINKS-1:
                    continue
                assert not _obb_overlap(poses[i],poses[j]),(frame,i,j,"plate collision")
                checks+=1
        big,small=sprocket_angles(frame)
        assert abs(big - small*SMALL_TEETH/BIG_TEETH)<1e-9
    return {
        "frames":FRAMES,"links":LINKS,"pitch_m":PITCH,
        "centre_distance_m":CENTRES,"chain_length_m":LENGTH,
        "big_pitch_radius_m":BIG_R,"small_pitch_radius_m":SMALL_R,
        "minimum_adjacent_pin_distance_m":minimum_neighbour,
        "minimum_adjacent_plate_center_gap_m":minimum_plate_gap,
        "minimum_roller_to_tooth_tip_radial_gap_m":TOOTH_TIP_GAP-ROLLER_R,
        "plate_sat_checks":checks,"sprocket_ratio":"24:16 (3:2)",
        "limitations":"2D swept plate SAT and analytic roller/tooth radial envelopes; no triangle-triangle contact, tooth load or Unity physics"
    }

if __name__ == "__main__":
    import json
    print(json.dumps(analytical_qa(),indent=2))
