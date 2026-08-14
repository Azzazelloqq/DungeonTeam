# DungeonTeam — Dungeon Expedition Vertical Slice GDD

**Статус:** нормативный scope текущей реализации

**Версия:** 0.4

**Дата:** 14 августа 2026

**Заменяет для текущего playable:** `CoreCombatPrototypeGDD.md`

## 1. Цель

Собрать один законченный playable-забег по authored пещерному коридору. Игрок ведёт лидера, три автономных спутника держат строй и автоматически применяют loadout-driven навыки, камера кинематографично сопровождает группу, а маршрут содержит один сундук и один составной encounter.

Slice должен доказать не только боевую читаемость, но и качество перехода `движение по данжу → активность → бой → продолжение маршрута → финиш`.

Неизведанные участки не должны заранее раскрывать содержимое: authored visibility-зоны остаются затемнёнными до продвижения группы, а закрытая дверь является жёсткой границей раскрытия.

## 2. Player experience

1. Группа входит в пещерный коридор; камера находится сзади и немного сверху.
2. На прямых камера мягко смотрит вперёд по маршруту.
3. Перед поворотом камера раскрывает направление движения, но не показывает содержимое ещё не открытой visibility-зоны.
4. Спутники следуют за лидером в различимом строю и занимают authored tactical anchors во время encounter.
5. Закрытая authored-дверь скрывает комнату, врагов, награды и HUD-маркеры за ней; interaction в допустимой дистанции открывает дверь и раскрывает следующую зону.
6. Когда сундук доступен, HUD показывает interaction prompt, а открытие не требует пиксельного наведения.
7. В encounter враги читаются по механике и телеграфам; лидер использует `Primary` вручную, а спутники автономно выбирают допустимые действия при валидной цели и готовом cooldown.
8. После победы группа продолжает маршрут и достигает выхода; после `Completed` или `Defeated` показывается terminal summary с единственным переходом в главное меню.

## 3. In scope

- один линейный authored cave-corridor с прямыми участками и минимум двумя поворотами;
- один лидер и три автономных спутника с различимыми proxy-функциями; рабочие labels `close pressure/durability`, `ranged damage`, `support/healer` не являются жёсткими классами или окончательной taxonomy;
- три функционально разных enemy archetype: pressure melee, area threat, disruptor/ranged; текущие actor-модели являются допустимыми proxy и не обязаны визуально различаться;
- следование отряда, восстановление строя и authored tactical anchors;
- corridor camera с look-ahead и turn blends; отдельный cinematic activity focus является polish и отложен;
- простая authored exploration visibility: ordered visibility-зоны, минимум одна закрытая door-boundary, затемнение нераскрытых зон и однонаправленное раскрытие после открытия/продвижения;
- один сундук: доступность, prompt, opening, visual result;
- один authored encounter с переходами exploration/combat;
- ручной видимый `Primary` и `Active1` лидера; автономный выбор действий спутниками;
- target priority и отдельный одноразовый жёсткий `FOLLOW`; `Dodge` и будущие тактические команды не входят в текущий scope;
- HUD для health/cooldowns/target/`Primary`/`Active1`/`FOLLOW`/interaction и terminal summary; скрытые за visibility-boundary объекты не публикуют target/interaction markers;
- terminal flow `Completed/Defeated → summary → main menu`; `Run Again` в текущий scope не входит;
- project-owned prefabs, models, materials, textures, VFX slots и definitions;
- Editor/PC playable как текущий путь проверки slice; Android build и device profiling отложены и не являются gate текущего slice;
- automated validation Unity dependencies: production content не ссылается на `Assets/ImportedAssets`.

## 4. Content mapping

| Рабочая proxy-функция | Visual source |
| --- | --- |
| Leader | Polygon Fantasy Characters — King |
| Close pressure / durability proxy | Polygon Fantasy Characters — Rogue |
| Ranged damage | Polygon Fantasy Characters — Wizard |
| Support / healer proxy | Polygon Fantasy Characters — Druid |
| Pressure melee | Текущий project-owned enemy proxy; отдельная Goblin-модель отложена |
| Area threat | Текущий project-owned enemy proxy; отдельная Minotaur-модель отложена |
| Disruptor/ranged | Текущий project-owned enemy proxy; отдельная Skeleton-модель отложена |
| Level/chest | POLYSTYLE Medieval Dungeons |

Импортированные пакеты являются только источником. Любой используемый asset и вся его необходимая dependency closure должны находиться в project-owned `Assets/Content`. Production scene, prefab, material, definition и controller не могут иметь зависимость на `Assets/ImportedAssets`.

Разные enemy-модели, финальная визуальная taxonomy и production-quality animation не являются gate текущего slice. Различимость проверяется по поведению, дистанции, telegraph и результату действия.

## 5. Animation proxy policy

Отсутствие humanoid-клипа не блокирует текущий slice. До получения финальных клипов locomotion, hit и cast подтверждаются движением модели, facing, timing, VFX и HUD feedback.

Нужный humanoid set:

- `Idle`, `Walk`, `Run`;
- `MeleeAttack`, `Cast`;
- `Hit`, `Death` или `Downed`;
- `InteractChest`;
- желательно `TurnLeft90`, `TurnRight90`.

## 6. Authoring contract

Level designer редактирует маршрут без изменения кода:

- ordered route checkpoints;
- camera shot anchors и blend distances;
- encounter start/end anchors;
- chest interaction anchor;
- formation offsets по конкретным спутникам;
- authored tactical anchors без жёсткой role-taxonomy;
- actor/enemy presentation profiles и prefab/VFX slots.

Runtime не ищет эти точки по строковым именам и не строит production level из primitives.

## 7. Non-goals

- procedural generation, room grammar и route choice;
- inventory, loot tables, equipment, economy и progression;
- hub, narrative, contracts и extraction;
- универсальные ability/interaction/AI frameworks;
- Addressables до появления generated-key API;
- production animation set и final audio;
- отдельные Goblin/Minotaur/Skeleton production-модели и визуальное разнообразие врагов;
- `Run Again`; следующий запуск пока начинается из главного меню, а в будущем terminal flow может вести в хаб;
- универсальная fog-of-war, grid visibility, minimap reveal, stealth/perception и симуляция освещения;
- VFX Graph как обязательный delivery: он добавляется только для конкретного эффекта после визуального и performance smoke.

## 8. Acceptance

Slice считается готовым, когда:

- запуск из application flow приводит в corridor run;
- отряд проходит маршрут и оба поворота без заметного распада строя;
- камера не перескакивает и не смотрит в стену на маршруте и в encounter;
- нераскрытая зона визуально затемнена и не выдаёт врагов, награды, target или interaction markers;
- закрытая дверь не раскрывает следующую зону; после её открытия раскрывается только связанная authored-зона и остаётся раскрытой до конца run;
- сундук открывается один раз и не ломает progression;
- лидер выполняет различимую ручную атаку через `Primary`, а спутники автоматически выполняют различимые полезные действия;
- в encounter присутствуют три механически различимых enemy archetype; разные production-модели не требуются;
- победа открывает путь к выходу; `Completed` и `Defeated` показывают summary и по одной команде возвращают в главное меню без повторного применения результата;
- production dependency audit возвращает ноль путей из `Assets/ImportedAssets`;
- EditMode/PlayMode проверки зелёные, а corridor smoke пройден в Editor/PC без blocking console errors; Android build и device profiler не входят в текущий acceptance.
