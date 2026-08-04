---
name: dungeonte-unity-validation
description: Choose, run, and report the correct DungeonTeam Unity validation level for C# logic, asmdefs, roots, UI, gameplay, prefabs, scenes, input, async lifecycle, Addressables, and performance-sensitive changes. Use when planning, implementing, testing, or reviewing Unity work, including mechanical validation of changed Unity assets and assembly boundaries.
---

# DungeonTeam Unity Validation

| Change | Minimum proof |
| --- | --- |
| Pure C# behavior | Focused EditMode test and compile |
| asmdef/public contract | Compile all affected assemblies |
| Root, DI, disposal, async | Focused lifecycle test plus compile |
| MVP/MVVM without serialized wiring | Focused behavior test plus compile |
| Prefab, scene, input, animation, material, serialized field | Compile plus Unity manual smoke |
| Addressables or scene loading | Handle/lifecycle test plus Unity manual smoke |
| Runtime hot path | Code review; profiler when a hotspot is claimed |

Always report compile, automated tests, Unity/manual proof, and unverified paths separately. Green compilation is not proof of scene, prefab, input, or lifecycle behavior.

After changing Unity assets, scenes, prefabs, meta files, or asmdefs, run the mechanical project check:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\.codex\skills\dungeonte-unity-validation\scripts\validate-unity-change.ps1
```

Use `-AllAssets` only for an explicit repository-wide audit. The script checks changed asset/folder metas, duplicate and unresolved GUIDs, asmdef JSON and prohibited project-layer references, plus `git diff --check`. It does not replace compilation, behavior tests, or Unity manual smoke.
