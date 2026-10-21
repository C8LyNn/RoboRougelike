"""Validate the Git index, not untracked local Editor state. Standard library only."""
import json, re, subprocess, sys
from pathlib import Path, PurePosixPath
REQUIRED_FILES=['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt','ProjectSettings/EditorSettings.asset','ProjectSettings/EditorBuildSettings.asset','ProjectSettings/ProjectSettings.asset','ProjectSettings/GraphicsSettings.asset','.gitignore','.gitattributes','README.md','Docs/DEVELOPMENT_WORKFLOW.md','Docs/GITHUB_MIGRATION_AND_CI_REPORT.md','.github/workflows/ci-01-repository.yml','.github/workflows/ci-02-editmode.yml','.github/workflows/ci-03-playmode.yml','.github/workflows/ci-04-build.yml']
GENERATED={'library','temp','obj','logs','usersettings','build','builds','memorycaptures','artifacts','testresults','coverage','.ci-tmp','.superpowers','.vs','.idea','__pycache__'}

def validate(root,files):
    root=Path(root); files=set(files); errors=[]; guids={}
    def read(rel): return (root/rel).read_text(encoding='utf-8-sig')
    for rel in REQUIRED_FILES:
        if rel not in files or not (root/rel).is_file(): errors.append(f'Missing required file: {rel}')
    for rel in sorted(files):
        parts=PurePosixPath(rel).parts
        if any(p.lower() in GENERATED for p in parts): errors.append(f'Generated file tracked: {rel}')
        if not (root/rel).is_file():
            errors.append(f'Tracked file absent from checkout: {rel}'); continue
        if (root/rel).is_symlink(): errors.append(f'Symlink unsupported in Unity source: {rel}')
        if (root/rel).stat().st_size>50*1024*1024: errors.append(f'Large file needs explicit LFS review: {rel}')
        if not rel.startswith('Assets/'): continue
        if not rel.endswith('.meta'):
            if rel+'.meta' not in files: errors.append(f'Missing tracked meta: {rel}.meta')
        else:
            target=rel[:-5]
            if target not in files and not any(f.startswith(target+'/') for f in files):
                errors.append(f'Orphan meta: {rel}')
            match=re.search(r'(?m)^guid: ([0-9a-f]{32})\s*$',read(rel))
            if not match: errors.append(f'Invalid meta GUID: {rel}')
            elif match[1] in guids: errors.append(f'Duplicate GUID: {rel}, {guids[match[1]]}')
            else: guids[match[1]]=rel
        for folder in PurePosixPath(rel).parents:
            if str(folder) in {'.','Assets'}: break
            if str(folder)+'.meta' not in files: errors.append(f'Missing tracked folder meta: {folder}.meta')
    try:
        if not re.search(r'(?m)^m_EditorVersion: 6000\.3\.14f1\s*$',read('ProjectSettings/ProjectVersion.txt')): errors.append('Unity version must remain 6000.3.14f1')
        if not re.search(r'(?m)^  m_SerializationMode: 2\s*$',read('ProjectSettings/EditorSettings.asset')): errors.append('Unity must use Force Text serialization')
        manifest=json.loads(read('Packages/manifest.json'))['dependencies']
        lock=json.loads(read('Packages/packages-lock.json'))['dependencies']
        for name,version in manifest.items():
            if name not in lock or lock[name].get('version')!=version: errors.append(f'Package lock mismatch: {name}')
            if version.startswith(('file:','http:','https:','git:')): errors.append(f'Non-registry dependency needs explicit audit: {name}')
        scenes=re.findall(r'- enabled: 1\s+path: (.+)\s+guid: ([0-9a-f]{32})',read('ProjectSettings/EditorBuildSettings.asset'))
        if not scenes: errors.append('No enabled build scene')
        for scene,guid in scenes:
            scene=scene.strip()
            if scene not in files or guids.get(guid)!=scene+'.meta': errors.append(f'Invalid build scene reference: {scene}')
    except (OSError,ValueError,KeyError,TypeError) as exc:
        errors.append(f'Invalid project configuration: {exc}')
    return sorted(set(errors))

if __name__=='__main__':
    root=Path(__file__).resolve().parents[2]
    paths=subprocess.check_output(['git','ls-files','-z'],cwd=root).decode('utf8').split('\0')
    files=[f for f in paths if f]
    errors=validate(root,files)
    for error in errors: print('ERROR:',error)
    print(f'Validated {len(files)} tracked files; {len(errors)} error(s)')
    sys.exit(bool(errors))
