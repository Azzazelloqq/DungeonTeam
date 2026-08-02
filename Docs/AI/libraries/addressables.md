# Addressables и сгенерированные ключи

Источник истины — Addressables Settings. Команда `Tools/Addressables/Generate Resource IDs` генерирует `Assets/Code/Addressables/Generated/AddressableIds.g.cs`; runtime-код получает ключи только через `AddressableIds`.

Сгенерированные `.cs` файлы не редактируются вручную. После изменения Addressables Settings ключи нужно перегенерировать и включить обновлённый файл в ту же задачу.

## Runtime-граница

- Прямые вызовы `Addressables` разрешены только внутри infrastructure/composition.
- Domain, gameplay и UI не получают raw keys или `AsyncOperationHandle`; потребитель зависит от узкого проектного контракта.
- `ResourceLoader` и `SceneSwitcher` не являются service locator и не внедряются глобально. Их создаёт и освобождает владеющий root.

## SceneSwitcher 1.0.4

`com.azzazello.sceneswitcher` использует Addressables 3.1.0 и UniTask, когда пакет UniTask установлен. `AddressablesSceneSwitcher` выполняет загрузку/выгрузку, а `SceneNavigator` предоставляет `ISceneNavigator` и владеет переданным `ISceneSwitcher`.

- В `sceneId` передаётся только константа из `AddressableIds`, не строковый литерал.
- В проектном runtime предпочтительны `NavigateToAsync`/`UnloadAsync` с token владельца. Callback API оставлен для Unity-boundary, где callback явно наблюдается.
- Один navigator обслуживает один последовательный navigation flow; конкурентные переходы без отдельной координации запрещены.
- `SceneNavigator.Dispose()` освобождает switcher; `AddressablesSceneSwitcher` выгружает отслеживаемые сцены и очищает события.
- Отмена прекращает ожидание вызывающего кода, но Addressables-операция завершается и очищается асинхронно. Поэтому token нужно отменить до dispose владельца, а ошибки перехода — наблюдать на orchestration boundary.
- `LoadSceneMode.Single` и `Additive` выбираются только реальным multi-scene use case. Scene handle остаётся у switcher до unload/dispose.

`WaitForCompletion` в runtime scene flow запрещён.
