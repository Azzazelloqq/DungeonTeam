# Skill VFX: карта и настройка

## Где лежат файлы

- Presentation-ассеты: `Assets/Content/Gameplay/Skills/Presentation`.
- VFX и projectile prefabs: `Assets/Content/Gameplay/Skills/Visuals/Particles/Prefabs`.
- Материалы и текстуры: соседние папки `Materials` и `Textures`.
- Связь `skill id -> presentation/projectile`: `Assets/Code/Gameplay/Skills/Runtime/SkillViewAssetCatalog.cs`.
- Addressables: группа `Assets/AddressableAssetsData/AssetGroups/Skills.asset`.

Fireball следует той же структуре, что остальные навыки. Отдельной папки `Skills/Fireball` больше нет.

## Карта навыков

| Skill ID | Presentation | Cast/Commit VFX | Projectile | Impact VFX |
| --- | --- | --- | --- | --- |
| `skill.fireball` | `FireballSkillPresentation` | `FireballCastingCircleVfx` | `FireballProjectile` | `FireballImpactVfx` |
| `skill.bolt.arcane` | `ArcaneBoltPresentation` | `ArcaneCastingCircleVfx` | `ArcaneBoltProjectile` | `ArcaneImpactVfx` |
| `skill.bolt.druid` | `DruidBoltPresentation` | `DruidCastingCircleVfx` | `DruidBoltProjectile` | `MagicImpactVfx` |
| `skill.heal.druid` | `DruidHealPresentation` | `DruidCastingCircleVfx` | — | `MagicImpactVfx` |
| `skill.lance.druid` | `NatureLancePresentation` | `DruidCastingCircleVfx` | `NatureLanceProjectile` | `MagicImpactVfx` |
| `skill.strike.king` | `KingStrikePresentation` | `KingStrikeVfx` | — | `KingStrikeImpactVfx` |
| `skill.smite.king` | `KingSmitePresentation` | `KingSmiteVfx` | — | `KingSmiteImpactVfx` |
| `skill.strike.rogue` | `RogueStrikePresentation` | `RogueStrikeVfx` | — | `RogueStrikeImpactVfx` |
| `skill.knife.rogue` | `ShadowKnifePresentation` | `RogueStrikeVfx` | `ShadowKnifeProjectile` | `RogueStrikeImpactVfx` |
| `skill.strike.skeleton` | `MeleeSkillPresentation` | — | — | — |

Некоторые VFX намеренно переиспользуются. Изменение `MagicImpactVfx`, `DruidCastingCircleVfx`, `RogueStrikeVfx` или `RogueStrikeImpactVfx` затронет сразу несколько навыков.

## Как быстро поправить конкретный навык

1. Открыть `DungeonTeam > Skills > VFX Lab > Open` — лаборатория сама откроет
   `Assets/Scenes/Development/SkillVfxPreview.unity`.
2. Выбрать `Skill`, его `Level`, модели `Source Actor` и `Target Actor`.
3. Настроить расстояние и положение цели через `Distance`, `Side Offset` и
   `Height Offset`; для быстрого выравнивания использовать `Face Each Other` и
   `Frame Actors`.
4. Нажать `Play Full Sequence`, отдельную фазу либо двигать `Scrub`.
5. В `Sequence Timeline` перетаскивать полосу для изменения `Delay`, а её правый
   край — для изменения `Lifetime`. Полосы могут пересекаться.
6. Нажать на дорожку VFX или Animation. В `Selected Cue` появятся только её
   настройки и фактический source asset выбранного героя.
7. В `Selected Cue` настроить:
   - `Position Offset` — смещение только этого эффекта относительно выбранного anchor.
   - `Scale Multiplier` — общий размер эффекта только для этого cue. Например, `0.5` уменьшает его вдвое.
   - `Rotation Offset Euler` — локальная поправка ориентации без изменения общего prefab.
   - `Lifetime` — сколько живёт созданный экземпляр.
   - `Delay` — задержка относительно начала выбранной фазы.
   - `Anchor` — точка появления: источник, цель или фактическая позиция попадания.
   - `Follow Anchor` — должен ли эффект продолжать двигаться вместе с anchor.
8. Для быстрого цикла использовать:
   - `Replay Selected` — проверить текущий черновик без сохранения;
   - `Apply & Replay` — сохранить cue/timing в production и сразу повторить;
   - `Edit Source` — открыть VFX prefab или реальный AnimationClip героя;
   - `Save Source & Replay` — сохранить source asset, вернуться в Lab и повторить cue.
   После обычного сохранения открытого source asset Lab также автоматически
   возвращается к preview и повторяет выбранный cue.
9. Для projectile отдельно проверить `Projectile Speed`, `Projectile Root Scale`
   и `Projectile Root Rotation`.
10. Нажать `Apply to Production Assets`. До этого все изменения остаются в черновике;
   `Revert Draft` их отменяет.

VFX Lab работает в Edit Mode: кнопку Play редактора нажимать не нужно. Модели и
настройки берутся из production-каталогов, поэтому отдельного ручного списка навыков
в лаборатории нет. Сцена не добавлена в build.

Кнопки `Select/Open Prefab` открывают сам VFX/projectile prefab для глубокой правки
`Particle System` (`Start Size`, `Start Speed`, `Shape`, `Emission` и т. п.). Такие
изменения общие для всех навыков, которые используют этот prefab.

Для изменения только одного навыка сначала использовать `Position Offset`, `Scale Multiplier` и `Rotation Offset Euler` в presentation. Сам prefab менять, только если поправка должна затронуть всех его потребителей.

## Перекрывающиеся эффекты и timeline

`Vfx Cues` не обязаны идти последовательно. Внутри одной фазы каждый cue имеет
собственное время:

- `Delay` — когда cue начинается относительно начала фазы;
- `Lifetime` — сколько времени он остаётся активным.

Например, первый cue с `Delay = 0` и `Lifetime = 0.6`, а второй с `Delay = 0.25`
и `Lifetime = 0.4` будут одновременно видны с `0.25` до `0.6` секунды.

Если это две дочерние `Particle System` внутри одного цельного prefab и они всегда
используются вместе на одном anchor, задержку второй системы лучше задавать через
её `Main > Start Delay`. Если им нужны разные anchor, scale, rotation, lifetime
или независимое переиспользование — разделить их на два VFX prefab/cue и настроить
перекрытие через `Delay` в presentation.

Внизу инспектора `SkillPresentationAsset` есть `Sequence Timeline`. Каждый cue
показан на отдельной дорожке, поэтому пересечения полос видны сразу. Фазы имеют
разные точки отсчёта: `Start`, `Commit` и `Impact` запускаются игровыми событиями,
а `Delay` применяется уже внутри соответствующей фазы.

Unity Timeline для этого не используется: он дублировал бы lifecycle навыка и
создал второй источник таймингов. Его стоит вводить только для постановочных
сцен с camera/audio/animation tracks, а не для обычных боевых VFX.

## Если projectile летит не туда

- Направление полёта теперь вычисляется от `SkillOriginAnchor` к текущему `HitVfxAnchor` цели на каждом tick.
- Позиция корня projectile prefab должна быть `(0, 0, 0)`. Положение, сохранённое в prefab preview, не используется как стартовая точка.
- Поворот корня prefab считается авторской поправкой оси модели/частиц. Если визуал летит боком или хвостом вперёд, повернуть корень projectile prefab на кратное `90°`; runtime сохранит эту поправку и продолжит наводить projectile на цель.
- Не добавлять projectile prefab в `Vfx Cues`: gameplay создаёт его отдельно на `Commit`. Иначе появится визуальный дубль.

## Если VFX слишком большой

- Для локальной балансировки конкретного навыка уменьшить `Scale Multiplier` в его presentation.
- Для общей правки открыть prefab и менять размеры его `Particle System`: `Start Size`, shape radius/scale и размеры дочерних систем.
- Не масштабировать импортированный исходник и не менять texture/material ради размера.
- После правки общего prefab проверить все навыки из таблицы, которые его переиспользуют.

## Если VFX появляется не там

- Подправить расположение конкретного cue через `Position Offset`, не двигая общий prefab.
- Cast/Commit на персонаже: `Anchor = SourceOrigin`, обычно `Follow Anchor = true`.
- Эффект на цели до попадания: `Anchor = TargetHit`.
- Взрыв после фактического попадания: фаза `Impact`, `Anchor = ImpactPosition`, `Follow Anchor = false`.
- Сначала проверить `SkillOriginAnchor` и `HitVfxAnchor` на prefab персонажа. Presentation offset не должен компенсировать неправильно выставленный anchor.

## Правила для новых VFX

- Корень prefab: position `(0, 0, 0)`, scale `(1, 1, 1)`; rotation допускается как поправка визуальной оси.
- Projectile prefab обязан иметь `SkillProjectileView` на корне. Cast/impact prefab не должен иметь этот компонент.
- Разделять cast, projectile и impact на разные prefabs, даже если на первом проходе они используют одинаковые материалы или дочерние системы.
- Сначала настраивать тайминг и масштаб в presentation, затем внутренние параметры частиц.
- После изменения запустить Skills EditMode и PlayMode tests и сделать визуальный smoke в боевой сцене.
