# Local C# compilation without Unity Editor

Source: `f62ce4f9352238bb09fc142dd5ad64697c80cbf3`, including the stacked garage changes through inventory dismantling. The working tree was clean during compilation.

- `Assembly-CSharp.csproj`: **PASS**, Microsoft Roslyn C# compiler 4.14.0, exit 0, 199 listed source files, 12 warnings, 0 errors. All six changed runtime C# files since `5b21ad80` were in the project list.
- `Assembly-CSharp-Editor.csproj`: **PASS**, same compiler, exit 0, 201 listed source files, 12 warnings, 0 errors. It references the freshly compiled `Assembly-CSharp.dll`; the modified `UI943PortraitCatalogActionsTests.cs` is included.
- The compiler consumed the generated project files' C# 9 defines and local Unity/project references, including previously compiled dependency assemblies from `Library/ScriptAssemblies`. Its outputs and logs are isolated under `Artifacts/Local/CSharpCompile-f62ce4f9-retry/` and `Artifacts/Local/CSharpEditorCompile-f62ce4f9/`; no existing game assembly was overwritten.
- The first runtime attempt `Artifacts/Local/CSharpCompile-f62ce4f9/` failed because the temporary response file split `/define:` and `/out:` arguments. That was a command-generation error, not a product compile result; the corrected retry above passed.

This is a real C# semantic compilation with local references, **not** a Unity Editor compile, executed test run, Player build, manual GUI check or performance measurement. Dependency assemblies were reused rather than rebuilt. Unity and Windows Player checks in the test packet remain outstanding; do not treat the resulting editor-target DLLs as a playable build.
