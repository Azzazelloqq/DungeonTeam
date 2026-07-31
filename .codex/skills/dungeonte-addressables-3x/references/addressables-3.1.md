# Addressables 3.1.0 reference

Verified against `Library/PackageCache/com.unity.addressables@*/package.json`: package `3.1.0`.

## Lifecycle rules

- `Addressables.InitializeAsync` returns a handle (`Addressables.cs`, lines 1082-1092). The initializer owner decides whether and when to release it.
- `LoadAssetAsync<T>` returns `AsyncOperationHandle<T>` (lines 1166-1185). Release the load handle through `Addressables.Release` when its owner no longer needs the asset.
- `InstantiateAsync` has `trackHandle` overloads (lines 1886-1951). Release the created instance via `Addressables.ReleaseInstance`; retain the handle if `trackHandle` is false.
- `LoadSceneAsync` returns `AsyncOperationHandle<SceneInstance>` and accepts `SceneReleaseMode` (lines 1972-2074). Keep the handle until `UnloadSceneAsync` (lines 2095-2140).
- `AssetReference` uses `ReleaseAsset` and `ReleaseInstance` (`AssetReference.cs`, lines 622-640). Do not treat it as an unrelated raw handle owner.

## Prohibitions

- Do not call `WaitForCompletion` in production loading/scene flow. It is unsupported on some platforms (`AsyncOperationBase.cs`, lines 171-178); scene unload explicitly requires async (`SceneProvider.cs`, lines 314-319).
- Do not release a handle from a different feature/root than the one that owns it.
- Do not expose raw keys and handles through Domain, gameplay, or UI.

## Before changing code

1. Re-read `package.json` and confirm the installed version.
2. Inspect the exact overload in the current PackageCache.
3. Identify owner, success result, error handling, cancellation policy, and release path.
