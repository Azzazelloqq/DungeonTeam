# Правила модуля feature

Новая feature создаётся в `Assets/Game/Features/<FeatureName>`.

```text
<FeatureName>/
  Domain/           # сущности, value objects, правила; без Unity
  Application/      # use cases, интерфейсы портов
  Infrastructure/   # реализации портов feature
  Presentation/
    Gameplay/       # MVP
    UI/             # MVVM
  Composition/      # FeatureRoot, factory, assembly scope при необходимости
  <FeatureName>.Domain.asmdef
  <FeatureName>.Application.asmdef
  <FeatureName>.Presentation.asmdef
```

Не создавайте папку или assembly заранее: они появляются, когда в feature есть реальный код.

## Перед началом feature

1. Сформулировать ответственность feature и её публичные контракты.
2. Выбрать owner/lifecycle: application, scene или feature root.
3. Определить, какие ресурсы и сохранения feature владеет.
4. Добавить asmdef зависимости только в направлении слоёв.
5. Добавить тесты поведения: Domain/Application — EditMode, Unity-сценарии — PlayMode.

## Нейминг

- Интерфейс: `I<Responsibility>`.
- Use case: глагол + предмет (`StartDungeonRun`, `ApplyDamage`).
- Root: `<Feature>Root`.
- MVP: `<Name>Presenter`, `<Name>Model`, `<Name>View`.
- MVVM: `<Name>ViewModel`, `<Name>View`.
