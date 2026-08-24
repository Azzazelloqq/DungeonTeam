# DungeonTeam — Guild Hall GDD

**Статус:** текущий feature contract; GH-0…GH-7 и PP-1…PP-5 реализованы; PP-6 full EditMode `429/429` и PlayMode `102/102` passed, manual flow smoke/build/external playtest not run

**Версия:** 0.5

**Дата:** 16 августа 2026

**Связанные документы:** [Product MVP GDD](./ProductMvpGDD.md), [Dungeon Expedition Vertical Slice GDD](./DungeonExpeditionVerticalSliceGDD.md), [Guild Hall Technical Design](../Technical/GuildHallTechnicalDesign.md), [World Map and Application Flow Technical Design](../Technical/WorldMapApplicationFlowTechnicalDesign.md), [Dungeon Return and Guild Summary Technical Design](../Technical/DungeonReturnGuildSummaryTechnicalDesign.md), [Ambient NPC Technical Design](../Technical/AmbientNpcTechnicalDesign.md), [Notice Board Technical Design](../Technical/NoticeBoardTechnicalDesign.md), [Guild Hall Implementation Plan](../Technical/GuildHallImplementationPlan.md)

---

## 1. Назначение

Guild Hall — доступная для перемещения 3D-локация гильдии авантюристов. Она должна ощущаться живым местом между экспедициями, а не набором меню, при этом оставаться компактной и дешёвой в производстве.

Текущий feature slice проверяет основу хаба и принимает подготовленный Player Profile snapshot через Bootstrap:

- игрок ходит по вручную собранному graybox-залу;
- видит иллюзию деятельности гильдии;
- разговаривает с любым NPC;
- выбирает доступное задание на доске;
- выходит на карту мира;
- запускает существующую экспедицию и возвращается в гильдию.
- профиль открывается на стойке регистрации;
- разрешённые изменения состава/loadout, equipment, продажа и повышение ранга проходят через Bootstrap bridge;
- terminal rewards банковуются в profile до показа summary.

Guild Hall runtime/UI по-прежнему не владеет Player Profile session, SaveStore, catalogs или persistence; полноценные квесты остаются вне scope.

## 2. Player flow текущего slice

```text
Guild Hall
→ осмотреть зал / поговорить с NPC
→ открыть доску объявлений
→ выбрать доступный контракт
→ выйти из гильдии
→ открыть карту мира
→ выбрать гильдию или доступную экспедиционную точку
→ пройти существующий Dungeon Run
→ применить terminal rewards к profile и получить summary только после подтверждённого commit
→ вернуться в Guild Hall
```

Одновременно активна только одна player-facing feature: Guild Hall, World Map или Dungeon Run.

## 3. Пространство Guild Hall

Первая версия собирается вручную из Unity primitives. Генерация планировки кодом и автоматическая сборка уровня не используются.

Обязательные функциональные точки:

| Точка | Текущее поведение | Будущее расширение |
| --- | --- | --- |
| Доска объявлений | Показать подготовленные предложения, включая rank-gating, выбрать один контракт | Квесты, ротация, истечение времени |
| Стойка регистрации | Профиль, composition/loadout/equipment actions, продажа, promotion и committed expedition summary | Квестовые требования и дополнительные экономические системы |
| Выход | Открыть карту мира | Другие городские места и маршруты |
| Общий зал | NPC стоят, ходят и выполняют короткие постановочные активности | Новые NPC, связанные сценки, квестовые состояния |

Планировка должна быть компактной: движение создаёт ощущение места, но не превращает повторный визит в долгий обязательный обход.

## 4. NPC и иллюзия активности

В зале допускаются только authored-поведения из небольшого набора:

- стоять и иногда менять направление взгляда;
- смотреть на доску;
- пройти по заданному маршруту и остановиться;
- сидеть;
- выпивать у стойки или стола;
- участвовать в короткой сценке спора с другим NPC.

Маршруты, точки стояния, места у доски, стулья и участники парных сцен задаются вручную в prefab. Config выбирает смысловой профиль и параметры поведения, но не хранит Unity `Transform` или другие ссылки на сцену.

NPC не симулируют полноценное расписание и социальную жизнь. Их задача — правдоподобная фоновая активность в пределах видимой части зала.

Ambient-поведение и однофразовый разговор не являются уникальными системами Guild Hall: тот же runtime должен применяться в будущих городских интерьерах и других небоевых локациях. Локация владеет конкретным списком NPC, расстановкой, маршрутами и semantic interactions; reusable-модуль владеет только authored routine, dialogue presentation и lifecycle набора NPC. Это переиспользование не распространяется на квесты, торговлю, ранги и другие роли NPC.

## 5. Разговоры

Игрок может начать разговор с каждым настроенным NPC.

Для текущего slice:

- регистратор и другие функциональные NPC могут иметь свой пул реплик;
- обычный NPC при взаимодействии выдаёт одну реплику из назначенного пула;
- повторный разговор может выбрать другую реплику;
- диалог не ветвится и не меняет gameplay state;
- отсутствие настроенного пула является ошибкой данных, а не поводом молча показывать технический текст.

Текст не хранится в коде. Каждая реплика имеет стабильный `lineId` и русский fallback-текст. UI получает уже подготовленный текстовый snapshot. Это позволяет позже подключить localization tables без изменения NPC, доски и ViewModel.

## 6. Доска объявлений

Доска отображает список подготовленных предложений. Для каждого предложения текущему UI достаточно:

- стабильного `contractId`;
- заголовка и краткого описания;
- целевой `locationId`;
- признака доступности;
- причины недоступности, если она есть.

Доска не проверяет ранг, деньги, прогресс квеста или другие условия. Bootstrap подготавливает immutable offer snapshot, включая rank-gating; доска только показывает его и отправляет наружу команду принятия. Bootstrap повторно валидирует подготовленную доступность на boundary, затем CQ-0 сохраняет принятие контракта.

Сейчас список строится из config, текущего profile rank и session-state. Будущие квестовые условия могут использовать тот же snapshot без переделки UI доски.

Для CQ-0 принятый контракт сохраняется отдельно от Player Profile: до успешного matching Dungeon Run он active, после него — completed. Visual selection не является отдельным источником прогресса.

## 7. Стойка регистрации

Стойка регистрации открывает подготовленный Profile snapshot. Composition/loadout/equipment actions, продажа и повышение ранга выполняются через узкий Bootstrap callback; Hall UI не читает persistence/config и не меняет business state напрямую. После Dungeon Run summary показывается только для подтверждённого profile settlement.

## 8. Карта мира

Выход из Guild Hall полностью завершает её runtime-lifecycle и открывает World Map.

Карта показывает конфигурируемые точки:

- город и гильдию;
- лес;
- данж;
- будущие городские и внешние места.

Каждая точка имеет стабильный `locationId`, текстовый snapshot, состояние доступности и optional причину блокировки. Карта не запускает gameplay сама: она отправляет `SelectLocation(locationId)` в Application flow.

В первом сквозном milestone обязательно доступны:

- Guild Hall — возврат в гильдию;
- существующий Dungeon Run — запуск выбранного контракта.

Лес становится доступным только вместе с реальным destination/сценарием. До этого он может присутствовать как явно недоступная конфигурируемая точка либо отсутствовать в production config; пустая локация не имитируется.

## 9. Данные и конфигурирование

Статический контент разделяется по ответственности:

| Config page | Содержимое |
| --- | --- |
| `GuildHallConfigPage` | NPC definitions, ambient profiles, параметры движения и взаимодействия |
| `DialogueConfigPage` | Пулы реплик и `lineId` с fallback-текстом |
| `ContractConfigPage` | Определения текущих контрактов и их destination IDs |
| `WorldMapConfigPage` | Точки карты, порядок отображения и доступность текущего контента |

Не создаётся один универсальный `GuildConfig` со всеми будущими системами. Rank и Item definitions уже принадлежат своим config pages/owners; Quest получает собственные definitions/config только вместе с реальным поведением.

Минимальный набор стабильных ID:

- `npcId`;
- `ambientProfileId`;
- `dialoguePoolId`;
- `lineId`;
- `contractId`;
- `locationId`.

Config и runtime snapshots не содержат `GameObject`, `MonoBehaviour`, Addressables handle или mutable `ScriptableObject`.

## 10. Runtime state

Guild Hall Application владеет только immutable snapshots и session-state:

- выбранный `contractId`;
- optional последний подготовленный summary;
- текущее player-facing состояние flow.

Guild Hall владеет только временным состоянием своего активного экземпляра:

- позиция игрока;
- состояния фоновых NPC;
- ближайшая доступная interaction;
- открытое окно диалога или доски.

При закрытии приложения session-state теряется. Persistent profile state и SaveStore принадлежат ApplicationRoot/PlayerProfile; в save записываются только бизнес-значения и stable IDs, но не Unity objects, config assets или presentation-state.

## 11. Non-goals текущего slice

- новые profile fields и persistence owners сверх PP-5;
- новые валюты, shop/exchange и rank requirements от квестов;
- inventory/equipment systems сверх текущих PP-3 actions;
- generic quest, dialogue, schedule или interaction engine;
- ветвящиеся диалоги и память отношений;
- полноценная симуляция жизни NPC;
- процедурная генерация или runtime-сборка Guild Hall;
- свободно исследуемый 3D-город;
- отдельные интерьеры города;
- новый forest gameplay без отдельного feature scope;
- замена или перепроектирование Dungeon Run.

## 12. Acceptance текущего slice

- Guild Hall загружается как вручную собранный graybox и корректно освобождается при выходе.
- Игрок может перемещаться по залу и не использовать боевые действия.
- Каждая authored interaction определяется по смысловому ID, а не по имени GameObject.
- С каждым настроенным NPC можно поговорить и получить реплику из его config-пула.
- В зале одновременно видны несколько разных authored-активностей; точное количество NPC не является контрактом теста.
- Доска показывает все переданные ей offer snapshots и возвращает выбранный `contractId`.
- Стойка регистрации открывает Profile и подготовленный terminal summary; продажа/rank actions идут через Bootstrap bridge.
- Выход открывает World Map; карта возвращает выбранный `locationId`.
- Выбор существующего данжа запускает текущий Dungeon Run; завершение или выход возвращает в новую чистую Guild Hall session.
- Повторные переходы не оставляют второй input owner, дублированные NPC, tick-подписки или удержанные Addressables resources.
- Тесты проверяют поведение и содержимое переданного snapshot, а не фиксированное число NPC, реплик, контрактов или точек карты.
