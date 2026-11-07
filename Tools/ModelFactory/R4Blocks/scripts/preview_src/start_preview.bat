@echo off
cd /d "%~dp0"
echo Podglad: http://localhost:8940/
start "" http://localhost:8940/
python -m http.server 8940 || py -m http.server 8940
