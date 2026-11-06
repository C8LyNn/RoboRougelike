"""In-place, length-preserving removal of local user paths from .blend files.
Each C string containing the user folder is overwritten with a relative '//' path padded
with NUL bytes to its original length, so no DNA block size or offset changes."""
import hashlib, re, sys
def patch(path, user):
    b = bytearray(open(path, "rb").read())
    n = 0
    for m in list(re.finditer(re.escape(user), b)):
        s = b.rfind(b"\0", 0, m.start()) + 1
        e = b.find(b"\0", m.start())
        old = bytes(b[s:e])
        new = b"//renders/" if b"\\renders\\" in old else b"//"
        b[s:e] = new + b"\0" * (len(old) - len(new))
        n += 1
    open(path, "wb").write(b)
    return n, hashlib.sha256(b).hexdigest()
if __name__ == "__main__":
    # usage: sanitize_blend_paths.py <windows user folder name> <file.blend>...
    user = sys.argv[1].encode()
    for p in sys.argv[2:]:
        print(p, *patch(p, user))
