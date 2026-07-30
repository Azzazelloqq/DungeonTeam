# Config и сохранения

## Config

`IConfig` хранит неизменяемые design-time данные: баланс, параметры врагов, таблицы лута. Страница конфигурации имеет тип, а не строковый ключ.

Не использовать Config для прогресса игрока или временного состояния боя.

## SaveStore

Использовать только V2 `SaveStore` и `SaveKey<T>`.

```csharp
private static readonly SaveKey<int> Gold = new("player.gold", () => 0);

saveStore.RegisterKey(Gold);
saveStore.Set(Gold, 100);
saveStore.Save();
```

Идентификаторы save-key являются обратным контрактом: не менять их без миграции. Для изменяемых моделей указывать версию и мигратор. `UnityBinaryLocalSaveSystem` устарел и запрещён.
