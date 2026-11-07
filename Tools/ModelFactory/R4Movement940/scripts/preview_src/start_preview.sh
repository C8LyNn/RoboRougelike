#!/bin/sh
cd "$(dirname "$0")"
echo "Podglad: http://localhost:8940/"
python3 -m http.server 8940
