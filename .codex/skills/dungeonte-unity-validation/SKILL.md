---
name: dungeonte-unity-validation
description: Choose and report the correct DungeonTeam Unity validation level for C# logic, asmdefs, roots, UI, gameplay, prefabs, scenes, input, async lifecycle, Addressables, and performance-sensitive changes. Use when planning, implementing, testing, or reviewing Unity work.
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
