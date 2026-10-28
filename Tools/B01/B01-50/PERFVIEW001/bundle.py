#!/usr/bin/env python3
"""Build a reproducible zero-network single-file HTML UI from authored source files."""
from pathlib import Path
from hashlib import sha256
import re

ROOT=Path(__file__).resolve().parent
OUT=ROOT/'perfview-standalone.html'

def render():
    html=(ROOT/'index.html').read_text(encoding='utf8')
    css=(ROOT/'style.css').read_text(encoding='utf8')
    core=(ROOT/'core.mjs').read_text(encoding='utf8')
    app=(ROOT/'app.mjs').read_text(encoding='utf8')
    assert '</script>' not in (core+app).lower(), 'inline script breakout'
    assert '<link rel="stylesheet" href="style.css">' in html
    assert '<script type="module" src="app.mjs"></script>' in html
    assert "import {analyzeCapture,compare,makeSyntheticCapture} from './core.mjs';" in app
    core=re.sub(r'(?m)^export (const|function) ', r'\1 ',core)
    app=app.replace("import {analyzeCapture,compare,makeSyntheticCapture} from './core.mjs';",'')
    html=html.replace('<link rel="stylesheet" href="style.css">','<style>\n'+css+'\n</style>')
    html=html.replace('<script type="module" src="app.mjs"></script>', '<script>\n'+core+'\n'+app+'\n</script>')
    return html

if __name__=='__main__':
    out=render()
    OUT.write_text(out,encoding='utf8')
    print(f'GENERATED_OFFLINE_STANDALONE {OUT.name} {len(out.encode())} B SHA256={sha256(out.encode()).hexdigest()}')
