"""#1239 procedural clip sets of the kit families (style-neutral timeline + events, style-specific curves). Pure python (no numpy):
the same file is used by the generator and inside Blender."""
import math
import astyle as ST
CLIPS = {
 "static":   [("Idle", 1, 25, True), ("Hit", 27, 39, False), ("Damaged", 41, 65, True), ("Recover", 67, 91, False)],
 "active":   [("Idle", 1, 25, True), ("Activate", 27, 75, True), ("Hit", 77, 89, False)],
 "deployer": [("Idle", 1, 25, True), ("Deploy", 27, 99, False), ("Hit", 101, 113, False)],
 "trap":     [("Idle", 1, 25, True), ("Arm", 27, 51, False), ("Triggered", 53, 65, False), ("Recovery", 67, 103, False)],
}
EVENTS = {"static": {"Hit": 27}, "active": {"Activate_Start": 27, "Hit": 77}, "deployer": {"Deploy_Open": 27, "Deploy_Out": 63, "Deploy_Reset": 85, "Hit": 101},
          "trap": {"Arm": 27, "Armed": 51, "Trigger": 53, "Recovered": 103}}
# clips whose TIMING may be reshaped per style when the animation is transferred from an approved rig (transitions without a gameplay event
# inside); every other source clip (Fire, EMP, Impulse, loops, aim sweeps ...) is transferred frame-exact in all five styles
WARPABLE = {"Return", "Reload", "Cooldown", "Cool", "Recovery", "Recover", "SpinUp", "SpinDown", "WarmUp", "Charge", "Heat", "Stop", "Brake", "Retract", "Rebound", "Ignition", "Jam", "Pump"}


def clip_set(family, has_nodes):
    if family == "world_trap": return "trap"
    if family == "deployer": return "deployer"
    return "active" if has_nodes else "static"


def qaa(axis, deg):
    a = math.radians(deg)/2; n = math.sqrt(sum(c*c for c in axis)) or 1.0; s = math.sin(a)/n; return [math.cos(a), axis[0]*s, axis[1]*s, axis[2]*s]
def qmul(a, b):
    return [a[0]*b[0] - a[1]*b[1] - a[2]*b[2] - a[3]*b[3], a[0]*b[1] + a[1]*b[0] + a[2]*b[3] - a[3]*b[2], a[0]*b[2] - a[1]*b[3] + a[2]*b[0] + a[3]*b[1], a[0]*b[3] + a[1]*b[2] - a[2]*b[1] + a[3]*b[0]]


def _pulse(style, u, seed):
    return ST.ease(style, 2*u, seed) if u < 0.5 else ST.ease(style, 2 - 2*u, seed + 1)


def _spin(style, u, seed):
    """fraction of the loop travelled (0 -> 1, exactly periodic); the character differs, the period does not"""
    if style == "A": return u
    if style == "B": return u + 0.035*math.sin(2*math.pi*u)                  # heavy flywheel: lags, then catches up
    if style == "C": return math.floor(u*16 + 1e-9)/16.0                       # sixteen snaps per loop
    if style == "D": return u + 0.02*math.sin(2*math.pi*3*u) + 0.012*math.sin(2*math.pi*7*u + seed)   # rattle
    return u + 0.06*math.sin(2*math.pi*2*u)                                    # E: surging


def tracks(style, set_name, nodes, deploy_dir=(0, 0, 1), seed=0.0, amp_scale=1.0):
    """nodes: {name: anim spec or None}; returns (F, {node: dict(loc=[[x,y,z]], quat=[[w,x,y,z]], scl=[s])}) for frames 1..F"""
    clips = CLIPS[set_name]; F = clips[-1][2]; out = {}
    def blank(): return dict(loc=[[0.0, 0.0, 0.0] for _ in range(F)], quat=[[1.0, 0.0, 0.0, 0.0] for _ in range(F)], scl=[1.0]*F)
    body = blank(); out["Body"] = body
    amp = {"A": 0.45, "B": 0.8, "C": 0.7, "D": 0.9, "E": 1.0}[style]*amp_scale      # degrees of the cosmetic body shake (scaled down for long parts)
    for (name, s, e, loop) in clips:
        for f in range(s, e + 1):
            u = (f - s)/float(e - s); i = f - 1
            if name == "Hit":
                k = ST.shake(style, u, seed); body["quat"][i] = qaa((1, 0.3, 0), amp*k); body["loc"][i] = [0.0, 0.0, -0.0012*abs(k)]
            elif name == "Damaged":
                w = 0.25*math.sin(2*math.pi*(2 if style != "B" else 1)*u); w = round(w*3)/3.0 if style == "C" else w
                body["quat"][i] = qaa((0.2, 1, 0), amp*(0.8 + w))
            elif name == "Recover":
                body["quat"][i] = qaa((0.2, 1, 0), amp*0.8*(1 - ST.ease(style, u, seed)))
            for n, an in nodes.items():
                if not an: continue
                t = out.setdefault(n, blank()); typ = an["type"]; ax = an.get("axis", (0, 0, 1)); amt = an.get("amount", 0.0); sd = seed + len(n)
                if name == "Activate":
                    if typ == "spin": t["quat"][i] = qaa(ax, 360.0*max(1, 48//int(an.get("frames", 24)))*_spin(style, u, sd))
                    elif typ == "swing":
                        k = math.sin(2*math.pi*u); k = (round(k*2)/2.0 if style == "C" else k)*(1.25 if style == "E" else 1.0)*(1.0 + (0.15*math.sin(2*math.pi*5*u) if style == "D" else 0.0))
                        t["quat"][i] = qaa(ax, amt*k if style != "B" else amt*math.sin(2*math.pi*u)**3)
                    elif typ == "slide": t["loc"][i] = [c*amt*_pulse(style, u, sd) for c in ax]
                elif name == "Deploy":
                    # 0 .. 0.33 prepare, hold, event Deploy_Out at u = 0.5, 0.8 .. 1 reset
                    k = ST.ease(style, u/0.33, sd) if u < 0.33 else (1.0 if u < 0.8 else 1.0 - ST.ease(style, (u - 0.8)/0.2, sd + 2))
                    if typ == "spin": t["quat"][i] = qaa(ax, 120.0*k)
                    elif typ == "swing": t["quat"][i] = qaa(ax, amt*k)
                    elif typ == "slide": t["loc"][i] = [c*amt*k for c in ax]
                    elif typ == "payload":
                        lift = 0.010*(ST.ease(style, (u - 0.08)/0.3, sd) if u < 0.5 else 1.0); go = min(1.0, max(0.0, (u - 0.5)/0.08))
                        sc = 1.0 - 0.96*go if u < 0.86 else 0.04 + 0.96*ST.ease(style, (u - 0.86)/0.14, sd + 3); sc = min(1.0, max(0.04, sc))
                        if u >= 0.86: lift, go = 0.0, 0.0
                        t["loc"][i] = [deploy_dir[0]*0.03*go, deploy_dir[1]*0.03*go, lift + deploy_dir[2]*0.03*go]; t["scl"][i] = sc
                elif name in ("Arm", "Triggered", "Recovery"):
                    if name == "Arm": k = ST.ease(style, u, sd)
                    elif name == "Triggered": k = 1.0 + 0.6*math.sin(math.pi*min(1.0, u*1.5))*(1.3 if style == "E" else 1.0)
                    else: k = 1.0 - ST.ease(style, u, sd)
                    if typ == "spin": t["quat"][i] = qaa(ax, {"Arm": 180.0*u*u, "Triggered": 180.0 + 720.0*u, "Recovery": 180.0*(1 - (1 - u)**2) + 180.0}[name])
                    elif typ == "swing": t["quat"][i] = qaa(ax, amt*k)
                    elif typ == "slide": t["loc"][i] = [c*amt*k for c in ax]
    return F, out


def warp(style, clips, f, seed=0.0):
    """source time for output frame f when a rig animation is transferred: identity except inside WARPABLE transition clips"""
    for c in clips:
        s, e = c["start"], c["end"]
        if s <= f <= e and c["name"] in WARPABLE and not c.get("loop") and e > s and style != "A":
            u = (f - s)/float(e - s); k = ST.ease(style, u, seed)
            if style == "A": k = u
            return s + (e - s)*min(1.0, max(0.0, k))
    return float(f)
