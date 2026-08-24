# DungeonTeam — Player Profile Technical Design

**Статус:** PP-1/PP-2 IMPLEMENTED; PP-3 DESIGNED, NOT IMPLEMENTED

**Версия:** 0.5

**Дата:** 24 августа 2026

**Product source:** [Player Profile GDD](../Product/PlayerProfileGDD.md)

**Implementation order:** [Player Profile Implementation Plan](./PlayerProfileImplementationPlan.md)

---

## 1. Responsibility and public boundary

`PlayerProfile` владеет чистым постоянным состоянием игрока, его инвариантами, загрузкой/сохранением через persistence port и выдачей immutable snapshot.

Он не владеет:

- Guild Hall world/UI lifecycle;
- Actor/Skill config и Unity presentation;
- Dungeon Run state и reward collection;
- предметами и equipment до PP-3;
- rank definitions/promotions до PP-5;
- quest definitions/state.

Минимальный Application contract PP-1:

```text
IPlayerProfileRepository.LoadOrCreate(seed) -> PlayerProfileState
IPlayerProfileRepository.Save(state)
PlayerProfileSession.Snapshot -> immutable PlayerProfileSnapshot
```

Реальные имена могут быть уменьшены при реализации, если одна concrete application-lifetime модель и repository дают тот же ясный контракт. Интерфейс вводится только для реального SaveStore adapter и детерминированных Application tests.

## 2. Modules and assemblies

```text
Assets/Code/Gameplay/PlayerProfile/
├─ Domain/
│  └─ DungeonTeam.PlayerProfile.Domain.asmdef
├─ Application/
│  └─ DungeonTeam.PlayerProfile.Application.asmdef
├─ Infrastructure/
│  └─ DungeonTeam.PlayerProfile.Infrastructure.asmdef
└─ Tests/EditMode/
   └─ DungeonTeam.PlayerProfile.Tests.EditMode.asmdef
```

| Assembly | Responsibility | Allowed references |
| --- | --- | --- |
| `DungeonTeam.PlayerProfile.Domain` | profile aggregate/value state and invariants | BCL only; `noEngineReferences: true` |
| `DungeonTeam.PlayerProfile.Application` | seed/snapshot, load/save orchestration and persistence port | `PlayerProfile.Domain`; `noEngineReferences: true` |
| `DungeonTeam.PlayerProfile.Infrastructure` | SaveStore V2 DTO/key mapping | `PlayerProfile.Application`, `PlayerProfile.Domain`, `LocalSaveSystem` |

Profile UI не создаёт четвёртую assembly. Единственный текущий consumer — Guild Hall; его local MVVM family живёт в `GuildHall.Runtime` и получает flat `GuildProfileSnapshot` из `GuildHall.Application`. Это не связывает Guild Hall с profile implementation и не создаёт UI module ради одного consumer.

Allowed direction:

```text
Bootstrap/Composition
├─ PlayerProfile.Infrastructure → PlayerProfile.Application → PlayerProfile.Domain
├─ Actors.Runtime / Skills.Runtime / DungeonRun.Application
└─ GuildHall.Runtime → GuildHall.Application
```

`GuildHall.Application` не ссылается на `PlayerProfile.Application`: Bootstrap переводит profile + definitions в локальный immutable hub snapshot. `PlayerProfile` не ссылается на Guild Hall, Actors, Skills, DungeonRun или Bootstrap.

## 3. State model PP-1

```text
PlayerProfileState
├─ gold: long >= 0
├─ rankId: optional stable string
├─ heroes: HeroProfileState[]
│  ├─ actorId
│  ├─ level > 0
│  └─ loadoutId
├─ leaderActorId
└─ companionActorIds: ordered unique IDs
```

Invariants:

- hero IDs unique and non-empty;
- roster non-empty;
- leader belongs to roster;
- every companion belongs to roster;
- leader does not occur in companions;
- companions are unique;
- level positive; loadout ID non-empty;
- Gold nonnegative;
- optional rank ID is either absent or non-empty.

Domain does not know whether an actor/level/loadout exists in current content. Application initialization validates the loaded snapshot against the seed/current definitions prepared by composition. Unknown or incompatible IDs are an explicit load error in PP-1; silent replacement with defaults is forbidden.

PP-1 does not reserve inventory/equipment fields. PP-3 changes the profile record to V2 and supplies a migration.

## 4. First-profile seed and content resolution

`ApplicationRoot` already owns:

- `ActorConfigCatalog`;
- `SkillCatalog`;
- `DungeonRunTeamSetup`.

Composition prepares a pure `PlayerProfileSeed` from `DungeonRunTeamSetup.DefaultSelection` and available team definitions. For current production content the roster is the union of the default leader and companions; no enemy is admitted merely because it exists in `ActorConfigCatalog`.

The seed contains stable IDs and allowed actor/level/loadout facts needed for validation, not concrete catalogs or Unity objects. It is used only when no saved `player.profile` record exists. Existing saved state is never overwritten because the production config changed.

The current roster contains exactly the configured default team, but code and tests do not assume four members or two skill slots.

## 5. Persistence

### 5.1. SaveStore ownership

```text
ApplicationRoot
├─ SaveStore V2
├─ SaveStorePlayerProfileRepository (borrows store)
└─ application-lifetime PlayerProfile state/session
```

`ApplicationRoot` creates one `SaveStore` with a dedicated directory under `Application.persistentDataPath`, registers the profile key, loads the profile before `CreateGuildHallAsync`, and disposes the store after all profile consumers.

The repository does not own/dispose the injected store. No feature root, ViewModel or View receives `ISaveStore`.

### 5.2. Stable record

- key: `player.profile`;
- value: `[SaveVersion(1)]` DTO with explicit `[SaveFieldId]` identifiers;
- tagged format and atomic write remain enabled;
- arrays/simple DTO values are used instead of Unity serialization types;
- first creation is persisted immediately;
- PP-1 has no autosave loop because it has no mutable UI operation after initialization.

DTO and Domain state are separate. Infrastructure maps both directions and validates null/default collections before constructing Domain state.

### 5.3. Proven package limitation

Installed `LocalSaveSystem` is `2.0.1`. Its `SaveStore.ForceSave()` catches I/O exceptions and only logs them, so the caller cannot currently distinguish a successful durable write from a failed one. PP-1 may use this API for initial persistence and must report the limitation; PP-4 cannot claim atomic/idempotent reward commit until the package exposes a failure result/exception or an approved verified adapter closes that gap.

This is not solved with a second custom save format or legacy `UnityBinaryLocalSaveSystem`.

## 6. Guild Hall snapshot

Bootstrap builds `GuildProfileSnapshot` from `PlayerProfileSnapshot`, `ActorConfigCatalog`, `SkillCatalog` and `DungeonRunTeamSetup`.

```text
GuildProfileSnapshot
├─ goldText/value
├─ rank display text
├─ leader: GuildHeroSnapshot
├─ companions: GuildHeroSnapshot[]
└─ roster: GuildHeroSnapshot[]

GuildHeroSnapshot
├─ actorId / display name
├─ role: Leader | Companion | Available
├─ level
├─ maximumHealth / movementSpeed
└─ skills: GuildHeroSkillSnapshot[]
```

Each skill snapshot is resolved from the saved `loadoutId` and contains stable slot identity, display name, skill level and the small set of values PP-1 actually renders. The ViewModel never branches on concrete `SkillDefinition` subtype; the builder prepares presentation text/value rows.

Invalid actor, level, loadout or skill references fail before Guild Hall creation with a contextual error. Guild Hall does not query config.

## 7. Guild Profile MVVM and UX

The family is owned by `GuildHallRoot` and serialized inside `GuildHallGraybox.prefab` like Notice Board/Run Summary:

```text
Presentation/UI/GuildProfile/
├─ Base/GuildProfileModelBase.cs
├─ Base/GuildProfileViewModelBase.cs
├─ Base/GuildProfileViewBase.cs
├─ GuildProfileModel.cs
├─ GuildProfileViewModel.cs
└─ GuildProfileView.cs
```

Simple hero rows remain passive bindings inside the family unless they gain independent commands/subscriptions. PP-1 needs one selected-hero detail state; selection is transient UI state owned by `GuildProfileModel`, keyed by actor ID, and is not saved.

The leader distinction uses all of:

- dedicated fixed region;
- `Главный герой` label;
- larger card/shape hierarchy;
- `Вы управляете этим героем` text;
- optional color accent only as redundant signal.

Companions use a separate `Команда` region. Roster/detail collections are populated from the snapshot length; no fixed child count or hero-specific serialized field is allowed.

## 8. Reception behavior

`GuildHallStartContext` gains optional/non-null profile snapshot as required by PP-1 production startup.

Reception policy:

1. If the current hall has an unviewed run summary, open it.
2. Closing that summary marks only the current hall presentation as viewed.
3. A later Reception interaction opens Guild Profile.
4. Profile open/close uses the existing modal/input-blocking policy.
5. Leaving the hall disposes Profile MVVM/View with the root; persistent profile state remains owned by ApplicationRoot.

No generic reception action framework is introduced.

## 9. Ownership and lifecycle

```text
GameBootstrapper
└─ ApplicationRoot
   ├─ SaveStore
   ├─ PlayerProfile repository/state
   └─ active GuildHallRoot
      ├─ Guild Hall world lease
      ├─ existing gameplay/UI children
      └─ Guild Profile MVVM family
```

- Save load is synchronous package I/O during application cold initialization, before the hall is shown.
- Guild Profile creates no tick, timer, Addressables load or separate root.
- Root creates/registers family before initialization and disposes it before the world lease.
- View holds Unity bindings and subscriptions only; ViewModel receives snapshot/model and close callback.
- No LightDI registrations are needed for the feature graph. Explicit construction is sufficient.

## 10. PP-1 implementation impact

- add three PlayerProfile assemblies and focused EditMode tests;
- add `LocalSaveSystem`, profile assemblies and only required references to `Bootstrap.asmdef`;
- extend `GuildHallStartContext` with the prepared profile snapshot;
- add Guild Profile MVVM family and serialized prefab bindings;
- add reception summary-then-profile policy;
- add localization-ready Russian fallback texts in existing Guild Hall text config where appropriate;
- update current technical/product docs and router.

PP-1 does not change `DungeonRunTeamSelection`, run launch, reward result or skill/actor definitions.

## 11. Validation

### EditMode

- valid state preserves every supplied hero and companion in input order;
- duplicate/unknown/missing hero IDs, invalid level/loadout and negative Gold fail explicitly;
- repository maps V1 DTO ↔ Domain state without losing IDs/order;
- missing record creates the supplied seed, saves it and returns the same snapshot;
- existing record loads instead of being replaced by a changed seed;
- snapshot builder excludes non-roster actors and resolves actual stats/skills;
- ViewModel distinguishes leader/companions, selects details by stable actor ID and restores nothing from list index;
- tests derive expected rows from supplied fixtures and do not assert a production content count.

### PlayMode/Unity automation

- production Guild Hall Addressable prefab contains valid Guild Profile bindings;
- Reception opens summary first when present, then profile after summary close;
- Profile open blocks world input; close restores it;
- variable roster/skill rows render and dispose without leaked children/subscriptions;
- repeated Hall creation/disposal does not retain profile View state or duplicate input ownership.

### Mechanical

- affected asmdefs compile directionally;
- metas/GUIDs/prefab references are valid;
- `validate-unity-change.ps1` and `git diff --check` pass except documented unrelated pre-existing whitespace;
- full relevant EditMode/PlayMode regressions remain green.

Manual full-flow smoke and build are reported separately and are not PP-1 automation gates.

## 12. Non-goals and later decisions

- PP-3: implemented inventory/equipment work after the approved contract in section 14;
- PP-4: durable terminal-result commit, Gold banking and selling;
- PP-5: configured guild rank ladder, promotion and board availability;
- quests remain a separate feature and save key;
- multiple profiles, cloud account sync and save-slot UI are not planned.

## 13. PP-2 — composition editing contract

### 13.1. UX decision

PP-2 extends the existing Reception Profile screen; it does not add a separate team-builder screen.

- selecting a roster hero still opens that hero's details;
- a non-leader can be made leader with one explicit action;
- a companion can be removed and an available roster hero can be added;
- the selected hero exposes only loadouts allowed for that actor by the current `DungeonRunTeamSetup`;
- every accepted action is applied immediately, persisted once and reflected in the open Profile screen;
- there is no invalid or unsaved draft: an action that would violate current team limits or content compatibility is rejected without changing the session;
- a rejected action shows a concrete localization-ready reason supplied in the Guild Profile text snapshot.

Changing leader preserves team size. If the selected hero is a companion, the previous leader takes that companion's ordered slot. If the selected hero was available, the previous leader becomes available and the companion order is unchanged.

### 13.2. Module boundary

```text
GuildProfileView command
  -> GuildProfileViewModel / GuildProfileModel
  -> GuildHallRoot callback: GuildProfileEditRequest -> GuildProfileEditResult
  -> Bootstrap composition bridge
     -> build candidate PlayerProfileState
     -> build and validate DungeonRunTeamSelection through DungeonRunTeamSetup
     -> PlayerProfileSession.Commit(candidate)
     -> rebuild flat GuildProfileSnapshot
  -> model replaces its snapshot and keeps selection by actorId
```

`GuildHall.Application` owns only the semantic edit request/result and flat presentation snapshots. It has no reference to PlayerProfile, DungeonRun, Actor or Skill assemblies. `PlayerProfile` owns immutable state changes and persistence, but does not reference Guild Hall or run configuration. Bootstrap remains the only cross-feature composition point.

No new DI scope, event bus, generic command framework or profile UI assembly is introduced.

### 13.3. Public data

`GuildHeroSnapshot` additionally carries:

- current stable `loadoutId`;
- ordered `GuildHeroLoadoutSnapshot[]` allowed for that actor;
- each loadout option contains a stable ID and prepared display text, not a runtime config object.

`GuildProfileTextSnapshot` carries action labels, loadout label and rejection texts. The View contains no player-facing hard-coded fallback strings.

The edit boundary is one current feature-specific contract:

```text
GuildProfileEditRequest
├─ kind: SetLeader | AddCompanion | RemoveCompanion | SetLoadout
├─ actorId
└─ loadoutId: required only for SetLoadout

GuildProfileEditResult
├─ accepted
├─ updated GuildProfileSnapshot when accepted
└─ prepared error display text when rejected
```

### 13.4. PlayerProfile state operations

Domain exposes immutable transformations for the current aggregate only:

- change leader with the team-size/order behavior above;
- add an existing roster hero as the last companion;
- remove an existing companion;
- replace one existing roster hero's loadout ID.

These methods enforce profile membership and uniqueness. They do not decide team-size limits or whether a loadout exists in current content. The Bootstrap bridge validates every candidate through `DungeonRunTeamSetup` before calling `PlayerProfileSession.Commit`.

`PlayerProfileSession.Commit` saves the candidate before replacing `State`. A thrown repository/mapping error leaves the previous state active. The known SaveStore `ForceSave()` inability to report swallowed I/O failure remains unchanged and explicitly outside the PP-2 claim.

### 13.5. Run integration

A pure Bootstrap mapper builds `DungeonRunTeamSelection` from the latest `PlayerProfileState` by stable actor IDs, levels and loadout IDs. `ApplicationRoot` supplies that selection when resolving a World Map dungeon destination. `WorldMapDestinationResolver` no longer receives `DungeonRunTeamSetup.DefaultSelection` for normal player flow.

The final run boundary still calls `DungeonRunTeamSetup.RequireValid`; Profile editing does not weaken launch validation. Developer console presets remain independent developer tooling.

### 13.6. PP-2 acceptance and tests

- leader change preserves member count and deterministic companion order;
- add/remove respects supplied minimum/maximum sizes, including variable fixtures;
- loadout change accepts only an actor-supported loadout;
- a rejected action neither saves nor replaces the session state and returns an explicit reason;
- an accepted action saves exactly once and refreshes leader/team/roster/loadout presentation;
- closing/reopening Reception and recreating Guild Hall use the latest session state;
- at least two different fixture compositions map to valid run selections without fixed production counts;
- World Map launch uses the latest profile selection, not the configured default;
- prefab bindings and dynamic action/loadout rows validate and dispose without duplicate listeners.

### 13.7. Implemented and validated state

- Profile Domain provides immutable leader, companion and loadout transformations; every candidate is validated through the current `DungeonRunTeamSetup` before commit.
- The Bootstrap edit handler commits before publishing the refreshed Guild snapshot. Rejection and repository exceptions keep the previous session state and return configured presentation feedback.
- The open Profile refreshes from the accepted snapshot while preserving selection by stable actor ID; normal World Map launch maps the latest session state.
- Focused pure EditMode behavior regression passed: 27/27 tests across Profile Domain, application flow, Guild snapshot builder and Guild Profile ViewModel.
- C# solution compile passed with 0 errors. Scoped `git diff --check` passed; the project-wide mechanical script reports only pre-existing whitespace in the unrelated modified TMP fallback asset.
- Full Unity EditMode/PlayMode automation and runtime visual interaction were not run because the project was owned by the open Editor and Unity MCP was unavailable. The implementation reuses the existing validated dynamic roster-row bindings for action/loadout rows and does not add serialized prefab fields; this is not a runtime visual proof.
- The documented SaveStore `ForceSave()` swallowed-I/O limitation remains unchanged and outside the PP-2 durability claim.

## 14. PP-3 — inventory, equipment and save contract

### 14.1. Responsibility and boundary

`Inventory` is a reusable player-owned feature: it owns unique item instances, stackable resources and equipment assignment. `PlayerProfile` remains the application-lifetime aggregate and the only persistent record owner. Inventory does not know Guild Hall, Dungeon Run, Unity, Addressables or SaveStore; Guild Hall and Dungeon Run receive prepared flat snapshots from Bootstrap.

The real implementation creates only the required pure assemblies:

```text
Inventory.Domain                 (BCL-only ownership/equip invariants)
Inventory.Application            (catalog and pure effect resolver)
Inventory.Runtime                (typed ItemConfigPage)
PlayerProfile.Domain -> Inventory.Domain
PlayerProfile.Application -> PlayerProfile.Domain
PlayerProfile.Infrastructure -> PlayerProfile.Application/Domain, Inventory.Domain, LocalSaveSystem
Bootstrap -> Inventory.Application/Runtime and composition consumers
```

`Inventory.Runtime -> Inventory.Application -> Inventory.Domain`; `Inventory.Domain` never references PlayerProfile. `DungeonRun.Application` receives its own immutable equipment bonus values in the already prepared team selection; it does not reference Inventory. `GuildHall.Application` receives only profile/detail snapshots and edit request/result values. No Inventory root, DI scope, event bus, generic stat-modifier engine or standalone inventory screen is introduced.

### 14.2. Domain state and rules

```text
InventoryState
├─ uniqueItems: ItemInstanceState[]
│  ├─ instanceId (stable, unique)
│  └─ definitionId
├─ resources: ResourceStackState[]
│  ├─ definitionId (unique)
│  └─ quantity > 0
└─ equipmentByHero: HeroEquipmentState[]
   ├─ actorId (unique roster actor)
   ├─ weaponInstanceId?
   ├─ armorInstanceId?
   └─ relicInstanceId?
```

- `Weapon`, `Armor` and `Relic` are the only PP-3 slots.
- Every equipped ID belongs to `uniqueItems`; an item can occupy at most one hero slot.
- `Equip` validates ownership, configured slot and configured eligible actor. It replaces only the target slot and returns the old instance to the same inventory state.
- `Unequip` removes a currently equipped instance. Resources cannot be equipped, and equipment cannot stack.
- Profile/Inventory Domain validates ownership and uniqueness; `Inventory.Application` validates current definition compatibility. Unknown or removed definition/instance references are an explicit load error, never silently removed.

No capacity, sorting, durability, repair, rarity, random affix, crafting, set bonus, consumable use or generic modifier pipeline belongs to PP-3.

### 14.3. Definitions and first content

Typed config owns static display data, sale value, slot, eligible actor IDs and one of exactly three current effects:

| Definition | Slot | Effect |
| --- | --- | --- |
| `equipment.training-blade` | Weapon | `PrimaryPower + value` |
| `equipment.warden-coat` | Armor | `MaximumHealth + value` |
| `equipment.pathfinder-charm` | Relic | `MovementSpeed + value` |
| `resource.monster-crystal` | Resource | stackable, no PP-3 use |

The initial values are authored config, not code constants. A profile starts, or migrates from V1, with one deterministic unique instance of each equipment definition and no resources. The three effects are mapped explicitly to the current actor/run stats; extending them requires a new product rule and a concrete mapping, not a catch-all modifier abstraction.

### 14.4. Save V2 and observable write result

The key remains `player.profile`; Gold, roster, inventory and equipment are one SaveStore entry. The existing CLR DTO type must retain its stored type identity while its `[SaveVersion]` becomes `2`; renaming `PlayerProfileSaveV1` during this migration would make SaveStore reject the old entry by type name before a migrator can run. A V1→V2 migrator initializes empty resources, one deterministic starter instance per first equipment definition and no equipped IDs.

SaveStore uses atomic replacement for an individual key file, but `ForceSave()` catches write exceptions and returns no result. PP-3 replaces the repository write path with the following verified write; its existing immediate profile edits keep their immediate UX only after this verification. PP-4 reuses the same path:

1. write the full candidate record through SaveStore V2 and `ForceSave()`;
2. create a fresh SaveStore reader with the same options, registry, migrators and key;
3. read and semantically compare the persisted record with the candidate;
4. only then replace application session state and report success;
5. on mismatch/read failure, discard and recreate the live store from disk, keep the old session state and return a configured persistence rejection.

This uses no second save format or raw file parser. It proves that a fresh SaveStore reader observes the candidate after the synchronous write; it does not claim a stronger hardware/fsync guarantee than the installed package exposes.

### 14.5. PP-4 terminal result transaction

PP-4 upgrades the same record to V3 with `pendingTerminalResult` and `lastAppliedRunId`. `DungeonRunResult` receives a stable run ID. The application executes only one active result at a time:

1. verified-save pending result before showing it as banked;
2. on load/retry, if pending run ID differs from `lastAppliedRunId`, build the complete profile candidate — Gold, resources and unique instances — and verified-save it with `lastAppliedRunId` set and pending cleared;
3. if it already matches, clear only the stale pending value by the same verified path;
4. Guild Hall receives debrief only from the committed profile snapshot.

The result cannot be applied twice across restart, and failed persistence cannot be displayed as a successful bank operation. Selling is a later PP-4 use case: atomically remove a stack/resource or unique instance and increase Gold in the same V3 candidate record.

### 14.6. PP-3 tests and proof

- EditMode: unique instance ownership, no duplicate equip, slot replacement, transfer, unequip, invalid actor/slot/definition, resource positive quantities and variable roster/item counts.
- EditMode: V1→V2 migration preserves all prior profile fields and produces exactly the three required starter instances once; a second load never creates duplicates.
- EditMode: mapping equipped values into two different team fixtures changes only the documented run stats; no fixed production roster or item count assertions.
- EditMode: verified-write repository keeps session state unchanged when a fresh reader cannot observe the candidate.
- PlayMode/manual proof: profile shows inventory/equipment, commands update the detail state, and the next run visibly receives each of the three concrete effects.

### 14.7. Implementation blueprint

#### A. New code and assembly graph

Create only these real files/assemblies, with tests adjacent to the pure layers:

```text
Assets/Code/Gameplay/Inventory/
├─ Domain/
│  ├─ EquipmentSlot.cs
│  ├─ ItemInstanceState.cs
│  ├─ ResourceStackState.cs
│  ├─ HeroEquipmentState.cs
│  ├─ InventoryState.cs
│  └─ DungeonTeam.Inventory.Domain.asmdef
├─ Application/
│  ├─ EquipmentItemDefinition.cs
│  ├─ ResourceItemDefinition.cs
│  ├─ ItemCatalog.cs
│  ├─ EquipmentEffectSnapshot.cs
│  ├─ EquipmentEffectResolver.cs
│  └─ DungeonTeam.Inventory.Application.asmdef
├─ Runtime/Config/
│  ├─ ItemConfigPage.cs
│  └─ DungeonTeam.Inventory.Runtime.asmdef
└─ Tests/EditMode/
   ├─ InventoryDomainTests.cs
   ├─ InventoryApplicationTests.cs
   └─ DungeonTeam.Inventory.Tests.EditMode.asmdef
```

`Inventory.Domain` has BCL only and `noEngineReferences`; `Inventory.Application` references only it; `Inventory.Runtime` references `Inventory.Application`, `Inventory.Domain` and `DungeonTeam.Configuration` because its typed config serializes `EquipmentSlot`. Test asmdefs reference their target and NUnit only. Bootstrap, PlayerProfile Domain/Infrastructure and their EditMode tests receive the smallest required directed references. Neither GuildHall assembly nor DungeonRun Runtime/Application receives an Inventory reference.

#### B. Pure contracts

`EquipmentSlot` has exactly `Weapon`, `Armor`, `Relic`. `InventoryState` is immutable and defensive. Its public operations are `Equip(actorId, instanceId, slot)` and `Unequip(actorId, slot)`; they enforce instance ownership, one equip occurrence and deterministic slot replacement, but never query Unity/config. `PlayerProfileState` gains one `InventoryState` and returns a new aggregate from an inventory replacement; it remains the owner that rejects an equipment entry for a non-roster actor.

`ItemCatalog` resolves static definitions. `EquipmentEffectResolver.Resolve(inventory, actorId)` validates definition compatibility and returns one BCL-only `EquipmentEffectSnapshot` with only `PrimaryDamageBonus`, `MaximumHealthBonus` and `MovementSpeedBonus`. It does not inspect loadout, combat types or Unity objects. Bootstrap owns the one conversion of this snapshot into the consumer-specific Dungeon Run and Guild Hall values.

#### C. Config and seed

Add `ItemConfigPage` to the existing config asset. It exposes separate serialized arrays for equipment definitions and resource definitions, so a resource cannot accidentally carry a slot/effect. Catalog validation rejects duplicate IDs, empty text IDs/fallbacks, invalid effect values, duplicate eligible actor IDs, missing current roster actors and an effect incompatible with its declared slot.

Author exactly four initial definitions: the three equipment definitions from section 14.3 and `resource.monster-crystal`. Give the three equipment items non-zero, authored values; crystals have no PP-3 grant or UI action. `PlayerProfileComposition` receives `ItemCatalog` and creates the same three deterministic starter instance IDs for a new profile. The V1→V2 migrator creates those IDs only when the V1 record is read; its first verified rewrite makes later loads ordinary V2 loads.

#### D. Persistence ownership and migration

Replace the `out SaveStore` composition contract with one application-lifetime `PlayerProfilePersistence` disposable owned by `ApplicationRoot`. It owns the live SaveStore, the shared store-options/registry/migrator factory and `SaveStorePlayerProfileRepository`; the session borrows the repository. `ApplicationRoot` disposes `PlayerProfilePersistence` after Guild Hall/Dungeon consumers and never holds or exposes a raw SaveStore.

The stored CLR type stays `PlayerProfileSaveV1` despite its historical name; it receives `[SaveVersion(2)]` and V2 fields. Do not rename it. Add V2 nested save DTOs with stable `SaveFieldId`s for item instances, resource stacks and hero equipment. The migration tracker is created with every store so the repository can immediately verified-rewrite a migrated value. Fresh verifier stores use the identical options, registry, migrators and key, and are disposed in the same synchronous method.

`Save` writes the full candidate, creates a fresh reader and compares canonical Domain state rather than DTO reference/order artifacts. On verification failure it disposes the tainted live store, recreates it from disk, throws a typed persistence exception and leaves `PlayerProfileSession.State` unchanged. This is cold/warm user-action code, not a tick: no coroutine, Update, caching layer or LightDI registration is needed.

#### E. Edit bridge and profile UI

Extend only the existing Guild Profile request/result boundary:

```text
GuildProfileEditKind += EquipItem | UnequipItem
GuildProfileEditRequest
├─ actorId
├─ itemInstanceId (required only for EquipItem)
└─ equipmentSlot (required only for UnequipItem)
```

The constructor rejects irrelevant/missing fields. `GuildProfileEditHandler` stays the Bootstrap cross-feature bridge: it resolves the selected item through `ItemCatalog`, validates actor/slot eligibility, builds the candidate `PlayerProfileState`, commits it, then publishes a rebuilt flat `GuildProfileSnapshot`. It never passes Session, catalog or Inventory objects into GuildHall.

`GuildHeroSnapshot` receives prepared effective health/speed, three prepared equipment-slot rows and the selected hero's applicable inventory-item rows. `GuildProfileSnapshot` receives prepared resource rows. Existing Guild Profile Model/ViewModel owns selection and commands; the existing dynamic row template is reused for equip, transfer and unequip actions, with configured text/rejection snapshots. No new screen, prefab family, serialized binding or inventory grid is introduced unless the existing row template proves insufficient during Unity authoring.

#### F. Dungeon Run integration

Add BCL-only `DungeonRunActorBonus` to `DungeonTeam.DungeonRun.Application` and append it as an optional, default-zero value to `DungeonRunActorSelection`; existing config/dev/enemy selections therefore retain current behavior. The type contains only the three PP-3 additive values and validates non-negative results.

`PlayerProfileComposition.MapToTeamSelection(profile, itemCatalog)` uses `EquipmentEffectResolver`, then creates a selection with the mapped bonus. `GuildProfileSnapshotBuilder` uses the same resolver for effective stats, so the profile and next run cannot diverge.

`DungeonRunRoot` creates a derived `ActorRuntimeDefinition` with the health/speed bonus before `ActorFactory.Create`. It passes `PrimaryDamageBonus` to `ActorCombatController`. The controller carries that value only for `SkillSlot.Primary`; `SkillUseExecution` carries it through the already active execution until commit. Direct, area and projectile damage apply the additive bonus; direct heal is unchanged. Projectile construction receives an explicit resolved damage value rather than mutating static `SkillLevelDefinition`. Enemy selections always use zero.

This keeps the existing combat/skill lifecycle, tick ownership and presentation intact. There are no item lookups, LINQ allocations or config queries in `Tick`, attack execution or hot targeting paths.

#### G. Delivery order and acceptance

1. Add asmdefs and failing pure Inventory Domain tests.
2. Implement Domain, then ItemCatalog/effect resolver and config validation tests.
3. Extend Profile Domain/DTO/migration and implement `PlayerProfilePersistence` verified writes with focused filesystem tests.
4. Update Bootstrap composition and the pure profile-to-run/snapshot mappers.
5. Extend DungeonRun selection/runtime/skill execution and add focused EditMode plus PlayMode damage/health/speed proof.
6. Extend Guild snapshots and existing Profile MVVM/View/config; author only required prefab/config bindings.
7. Run compile, affected EditMode suites, lifecycle/Addressable regression and the Unity mechanical validator. Run manual Guild Profile → equip → Map → Run smoke because prefab/UI/input/runtime effects are not proven by compile.

PP-3 is complete only when a migrated V1 profile remains valid, the three item effects are observable in the next run, an unverifiable write leaves the previous profile active, and no new reverse reference or service locator appears.

### 14.8. PP-4 implementation blueprint — banked terminal rewards and selling

#### A. Scope and boundary

PP-4 turns only existing `RewardGrant` content into persistent player data: `reward.gold` and `reward.silver` add Gold; `reward.crystal` adds the existing stackable `resource.monster-crystal`. No new reward IDs, equipment drops, shop, second currency, price negotiation, quantity picker or quest reward pipeline is introduced. Unique equipment is sellable because PP-3 already owns unique instances, but current dungeon content does not create new unique gear.

`DungeonRun.Runtime` keeps collecting `RewardGrant` and does not depend on profile, inventory or save code. `Rewards.Runtime` keeps definitions/presentation names. Bootstrap is the only mapper from a completed `DungeonRunResult` plus `RewardCatalog` to a PlayerProfile application request, and is the only consumer that can then create a Guild summary. GuildHall receives only prepared snapshots/commands; it never sees pending records, SaveStore, `DungeonRunResult`, `RewardCatalog` or item config.

No new root, DI scope, event bus, generic transaction framework or cross-feature assembly is added.

#### B. Stable terminal identity and BCL request

`DungeonRunRoot` creates one `Guid.NewGuid().ToString("N")` run ID when it is created and appends it to immutable `DungeonRunResult`. The ID is copied unchanged through the one existing `Finished` event; it is not calculated from dungeon/seed and all config/dev/enemy paths keep their current result behavior.

`PlayerProfile.Application` owns small BCL request/receipt values:

```text
ProfileTerminalResultRequest { RunId, GoldAmount, ResourceGrants[] }
ProfileSettlementReceipt       { RunId, GoldAmount, ResourceGrants[] }
```

They validate stable IDs, positive values and duplicate resource IDs. Bootstrap's concrete `RewardSettlementMapper` accepts exactly the three configured reward IDs above, aggregates by target and rejects an unknown reward before any profile save. The mapper is not a new general reward service.

#### C. Profile V3 and exactly-once algorithm

The stored CLR type remains `PlayerProfileSaveV1`; it becomes `[SaveVersion(3)]` and gains stable fields `pending_terminal_result` and `last_applied_run_id`. A V2→V3 migrator sets both absent. Domain state contains the equivalent optional `PendingTerminalResultState` and optional last-applied ID; both are immutable and defensive.

`PlayerProfileSession.BankTerminalResult(request)` is synchronous cold-path application logic:

1. If `lastAppliedRunId == request.RunId`, return `AlreadyApplied` with no save and no receipt.
2. If another pending ID exists, reject rather than overwrite it. If the same pending ID exists, reuse its canonical payload.
3. Otherwise verified-save a candidate holding that pending payload. Only after success may the session publish it.
4. Derive one candidate with Gold/resources incremented, pending cleared and `lastAppliedRunId` set; verified-save it, then publish and return its receipt.

On application creation, `PlayerProfileSession` calls `RecoverPendingTerminalResult()` before Guild consumers exist. It applies the stored pending payload using step 4. A crash after step 3 therefore finishes once on the next start; a crash after step 4 reads `lastAppliedRunId` and cannot apply that run again. A `PlayerProfilePersistenceException` never produces a receipt/summary and leaves the in-memory session state unchanged. The existing fresh-reader verification remains the only write proof; PP-4 does not claim an fsync guarantee beyond it.

#### D. Return flow and summary

`ApplicationRoot.ReturnFromFinishedDungeonRunAsync` banks the mapped request before it stops the run or stores a Guild summary. It builds `GuildRunSummarySnapshot` only from the returned `ProfileSettlementReceipt` plus configured texts/catalog display values; it never shows a raw unbanked `DungeonRunResult` as success. Duplicate terminal callbacks are ignored by the existing named subscription/gate and receive no second summary.

If banking rejects or persistence is unverifiable, the run is stopped and the current recovery-to-GuildHall path runs with no reward summary; the exception is reported, but Gold/inventory success is not displayed. This prevents a terminal root from becoming stuck and never lies about a banked payout.

#### E. Reception selling

Extend the existing Guild Profile request/result bridge only with `SellUniqueItem(instanceId)` and `SellResource(definitionId)`. The prepared snapshot exposes sell actions/prices, not catalog objects. A unique instance may be sold only while not equipped by any hero; a resource sale sells its whole current stack. `ItemCatalog` remains the price authority (`saleValue`); the candidate removes the instance/stack and adds `saleValue` or `saleValue × quantity` Gold in one `PlayerProfileState` replacement, then uses the same save-before-publish session commit. Invalid/equipped/missing targets leave state untouched and use existing configured rejection feedback.

#### F. Delivery and proof

1. Add failing pure tests for V2→V3 migration, pending recovery, duplicate same-run submission, persistence failure and whole-stack/unequipped-item sale.
2. Add run ID, Bootstrap mapper and result-to-profile request tests with variable grants; unknown IDs must save zero data.
3. Implement V3 DTO/migrator/domain/session state and verified two-step banking.
4. Change terminal return ordering and summary builder input; test bank → stop → summary ordering and bank-failure recovery.
5. Add prepared sell rows/commands to the existing Profile MVVM/View and tests that it carries no persistence/config objects.
6. Run affected EditMode suites, Unity compile and mechanical validation. Manual smoke, if later desired, is Guild → Map → Dungeon → return summary → sell → restart; it is not silently claimed by a build.
