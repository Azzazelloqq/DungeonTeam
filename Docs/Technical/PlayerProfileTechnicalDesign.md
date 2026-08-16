# DungeonTeam — Player Profile Technical Design

**Статус:** PP-1/PP-2 IMPLEMENTED; PP-3 REQUIRES ITEM/EQUIPMENT DESIGN

**Версия:** 0.3

**Дата:** 16 августа 2026

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

PP-1 does not reserve inventory/equipment fields. PP-3 changes save version and supplies a migration.

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

- PP-3: separately designed inventory/equipment with real items and stat application;
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
