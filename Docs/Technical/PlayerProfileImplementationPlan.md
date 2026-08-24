# DungeonTeam — Player Profile Implementation Plan

**Статус:** PP-0/PP-1/PP-2 COMPLETE; PP-3/PP-4 IMPLEMENTED, AUTOMATED VALIDATION PASSED; manual Unity smoke outstanding

**Версия:** 0.5

**Дата:** 24 августа 2026

**Design:** [Player Profile Technical Design](./PlayerProfileTechnicalDesign.md)

**Product scope:** [Player Profile GDD](../Product/PlayerProfileGDD.md)

---

## 1. Delivery rule

Каждый milestone даёт законченный наблюдаемый результат и не создаёт пустых будущих систем. Квесты не входят ни в один PP milestone. Тесты проверяют переданные snapshots и инварианты, а не production count героев, спутников, предметов, навыков или рангов.

## 2. Status

| Milestone | Result | Status |
| --- | --- | --- |
| PP-0 | Product/technical design, boundaries and order | Complete |
| PP-1 | Read-only persistent profile vertical slice | Complete |
| PP-2 | Editable leader/team/loadout and run integration | Complete |
| PP-3 | Unique equipment, stackable resources and three hero slots | Implemented; targeted EditMode passed, manual Guild-to-Run smoke outstanding |
| PP-4 | Verified result commit, Gold banking and selling | Implemented; targeted EditMode passed, manual failure/selling smoke outstanding |
| PP-5 | Guild ranks and board gating | Requires rank rules/content decision |
| PP-6 | Integrated regression and documentation closure | Planned |

## 3. PP-1 — persistent read-only profile

**Goal:** after restart, Reception shows the same valid profile with a clearly distinguished main hero and actual team/stats/skills.

### Work

1. Add `PlayerProfile.Domain`, `.Application`, `.Infrastructure` asmdefs with the directed graph from the design.
2. TDD the pure profile invariants and defensive immutable snapshots.
3. Add the real SaveStore V2 adapter:
   - key `player.profile`;
   - V1 tagged DTO with stable field IDs;
   - first-run seed mapping;
   - load validation;
   - no legacy save API and no second file format.
4. Compose one application-lifetime SaveStore/repository/profile owner before Guild Hall startup.
5. Build a Guild-local immutable profile snapshot from the profile plus current Actor/Skill definitions.
6. Add the `GuildProfile` MVVM family and serialized bindings to `GuildHallGraybox.prefab`.
7. Implement Reception policy `unviewed summary → profile` and modal input lifecycle.
8. Add content-driven Russian fallback labels; do not encode hero names/skills in View code.
9. Add focused EditMode and PlayMode tests from the technical design.
10. Run compile, focused tests, full relevant regression and mechanical Unity validation.

### Done

- first start creates and durably attempts to write V1 profile;
- next application start loads it instead of recreating defaults;
- Gold, rank placeholder, leader, companions, stats and skills match the persisted IDs/current definitions;
- leader/team distinction does not rely only on color;
- no Dungeon Run/reward/equipment/rank-rule behavior changed;
- SaveStore `ForceSave` error-reporting limitation remains explicitly documented.

## 4. PP-2 — composition editing

**Goal:** the saved profile becomes the user-owned source of the next team selection.

### Agreed interaction

- Keep editing inside the current Reception Profile screen.
- Roster selection drives details and current action buttons.
- `Сделать главным` is available for a selected non-leader and preserves team size.
- `Добавить в команду` / `Убрать из команды` reflect the selected hero's current role.
- Allowed loadouts render as a variable list; selecting the current loadout is a no-op.
- Valid actions persist immediately. Invalid actions leave the previous state intact and show a configured reason.

### Work

1. TDD immutable Profile Domain transformations: change leader, add/remove ordered companion and replace hero loadout.
2. Make `PlayerProfileSession` commit an already validated candidate by saving before replacing its current state.
3. Add a pure profile-to-`DungeonRunTeamSelection` mapper and an explicit validation result on the current `DungeonRunTeamSetup` boundary.
4. Add the narrow Guild Hall edit request/result contract and loadout/action presentation snapshots.
5. Implement the Bootstrap bridge that builds a candidate, validates it against the current setup, commits it and rebuilds the Guild snapshot.
6. Extend Profile MVVM with reactive snapshot replacement, stable selected actor ID, action commands and visible rejection state.
7. Extend Guild Hall config/prefab with localization-ready action/error text and dynamic loadout/action bindings.
8. Pass the edit callback through `GuildHallRoot`; do not expose repository, session or catalogs to Guild Hall.
9. Resolve World Map dungeon requests from the latest persisted profile selection instead of `.DefaultSelection`.
10. Add behavior tests for variable sizes, both leader-change paths, size-limit rejection, loadout rejection, save count/failure and two valid fixture compositions.
11. Validate C# compile, focused EditMode tests, production prefab bindings, relevant PlayMode flow and mechanical Unity assets.

### Done when

- the open Profile immediately reflects accepted edits and shows explicit configured feedback for rejection;
- rejected edits perform zero saves and accepted edits perform one save;
- reopening the hall uses the committed state;
- a normal World Map launch carries the latest profile leader, ordered companions, levels and loadouts;
- no Profile/Guild Hall/Dungeon Run reverse dependency or service locator was added;
- tests derive expectations from fixtures/configuration and never assert a fixed hero, team, loadout or skill count.

PP-2 does not add recruitment, hero purchase, equipment or skill-tree progression.

### Implemented and validated

- Domain transformations, setup validation, save-before-publish session commit and the narrow Guild Hall edit contract are implemented.
- Accepted edits refresh the open Profile and normal World Map launch uses the latest saved composition; rejection leaves state unchanged and exposes configured feedback.
- Focused pure EditMode behavior regression passed: 27/27 tests. C# solution compile passed with 0 errors.
- Scoped diff validation passed. Project-wide mechanical validation is clean for PP-2 files and reports only the unrelated pre-existing TMP fallback asset whitespace.
- Full Unity EditMode/PlayMode automation and runtime visual interaction remain unverified in this stage because the open Editor owned the project and Unity MCP was unavailable.

## 5. PP-3 — inventory and equipment

**Goal:** a player owns three real starter equipment instances, equips them on heroes and sees their documented effects in the next run.

**Decisions:** gear is unique by `instanceId`; monster crystals are stackable by definition; each hero has `Weapon`, `Armor`, `Relic`; there is no capacity, rarity, durability, crafting or generic modifier framework. The starter blade, coat and charm respectively affect Primary power, maximum health and movement speed. Details are fixed in technical design section 14.

### Work

1. Add Inventory Domain/Application/Runtime and test asmdefs exactly as section 14.7.A specifies; do not add an Inventory root or DI scope.
2. TDD unique ownership, slot replacement/transfer, unequip and stack invariants before config/UI code.
3. Add typed `ItemConfigPage`, register it in the existing configuration asset, and author only blade, coat, charm and monster-crystal definitions.
4. Extend PlayerProfile Domain with inventory state; move the single stored DTO to V2 without changing its CLR type identity.
5. Add the V1→V2 migrator, deterministic starter instances and migration rewrite proof; V1 data may not lose Gold, rank, roster, level or loadout.
6. Replace raw SaveStore lifetime with application-owned `PlayerProfilePersistence` and the documented fresh-reader verified write/reset path.
7. Map resolved item effects to `DungeonRunActorBonus`; prove zero-bonus config/dev/enemy paths remain unchanged.
8. Apply health/speed in actor creation and Primary damage through the current skill execution path without mutating shared definitions.
9. Extend the existing Guild Profile snapshot/request/MVVM/View family with equip/unequip/transfer rows and configured feedback; no separate inventory UI.
10. Run focused EditMode/PlayMode/lifecycle regression, compile, Unity mechanical validation and manual Profile → Run smoke.

### Done when

- an item is never duplicated or equipped twice;
- a player can move each starter item between eligible heroes, replace a slot and unequip it;
- all three effects are visible in the next run and disappear on unequip;
- V1 profiles migrate without losing roster, Gold, rank or loadout data;
- a failed or unverifiable write keeps the previously persisted profile active;
- no item UI reads SaveStore/config directly and no generic inventory framework is introduced.

### Implemented and validated

- Added isolated `Inventory.Domain`/`.Application`/`.Runtime` assemblies, typed `ItemConfigPage`, one monster-crystal resource and three starter equipment instances with the agreed three slots/effects.
- Profile V2 retains the historical `PlayerProfileSaveV1` CLR identity, migrates V1 values to deterministic unequipped starter instances, and uses an application-owned `PlayerProfilePersistence` with a fresh-reader verification/recovery path.
- Guild Profile exposes prepared equipment/resource rows and equip/unequip/transfer actions without receiving a repository, inventory state or config object; Bootstrap remains the cross-feature bridge.
- The next player run maps effective stats through `DungeonRunActorBonus`; health, speed and damaging Primary direct/area/projectile values change without mutating shared definitions. Default/config/enemy selections remain zero-bonus.
- Unity Editor compilation is clean after correcting explicit asmdef references. Targeted Unity EditMode: 38/38 passed (`DungeonTeam.Inventory.Tests.EditMode`, `DungeonTeam.PlayerProfile.Tests.EditMode`, `Bootstrap.Tests.EditMode`).
- `validate-unity-change.ps1` reports only the pre-existing unrelated TMP fallback whitespace; scoped PP-3 diff check is clean.

Manual Guild Profile → equip/transfer/unequip → Map → Run proof remains intentionally unrun. No player build or playtest was run.

## 6. PP-4 — rewards, Gold and selling

**Prerequisite met:** PP-3 verified-write repository is implemented and targeted automated validation passed. Full design is section 14.8 of the technical design.

1. TDD V2→V3 migration, pending recovery, duplicate submission, failed verification and atomic sell candidates.
2. Add one stable run ID per `DungeonRunRoot` and keep it in `DungeonRunResult`.
3. Add the narrow Bootstrap mapping for the current three reward IDs only: Gold/silver to wallet, crystal to `resource.monster-crystal`; reject unknown IDs before saving.
4. Extend the existing profile DTO/state/session to V3 with `pendingTerminalResult` and `lastAppliedRunId`; persist pending before apply, then apply once via the verified repository path.
5. Recover an interrupted pending result during profile initialization before Guild Hall consumes the profile.
6. Make terminal return build a Guild summary only from a committed receipt; failure returns to Guild Hall without a reward-success summary.
7. Extend existing Profile snapshots/MVVM/View and Bootstrap handler with sell-one-unique (only unequipped) and sell-whole-resource-stack actions at configured item prices.
8. Run targeted EditMode suites, compilation, asmdef/GUID validation and preserve a clear manual-smoke boundary.

No shop, exchange rates, second currency, quantity picker, new reward IDs or unique-equipment dungeon drops are added.

### Implemented and validated

- `DungeonRunResult` carries one stable run ID; Bootstrap maps only the current Gold/silver/crystal reward IDs before profile persistence.
- Profile V3 persists a pending terminal result and last applied run ID, recovers interrupted pending work at startup and returns a receipt only after the applied record is verified.
- Dungeon return now banks before it stops the run or creates a summary. A failed/rejected bank returns to Guild Hall with no reward-success summary.
- Reception sells an unequipped unique item or a whole resource stack at `ItemCatalog` prices through the existing Profile bridge.
- Targeted Unity EditMode: 119/119 passed. `Bootstrap.csproj` compiles with 0 errors and 0 warnings. The project mechanical validator reports only the unrelated pre-existing TMP fallback whitespace.

Manual live verification of the return failure/retry branch and selling UI remains unrun; no player build or playtest was run.

## 7. PP-5 — guild ranks

Before code, agree the rank ladder, promotion requirements, cost and at least one actual gated behavior.

1. Add rank definitions/config and validate their order/IDs.
2. Add promotion eligibility and mutation in Profile Domain/Application.
3. Expose promotion at Reception with explicit requirement/result snapshot.
4. Let Application prepare Notice Board availability from the current rank; Board remains presentation-only.
5. Extend save through a versioned migration only if V1 optional rank representation is insufficient.
6. Test boundaries from supplied rank definitions, not a fixed number or hard-coded names.

## 8. PP-6 — closure

1. Run full EditMode and PlayMode project suites.
2. Re-run Addressables/prefab/asmdef/GUID mechanical validation.
3. Audit that Guild Hall UI has no SaveStore/config access and Dungeon Run has no profile persistence access.
4. Audit legacy save API and raw string Addressable keys: zero new consumers.
5. Update actual implementation/validation statuses in all profile and Guild Hall docs.
6. Report compile, automated tests, Unity proof and unverified manual/build paths separately.

## 9. Implemented PP-1 baseline

- `PlayerProfile.Domain`, `.Application` and `.Infrastructure` own profile invariants, application lifetime and SaveStore V2 mapping respectively.
- `ApplicationRoot` creates the application-lifetime profile before Guild Hall and converts it to a flat Guild Hall snapshot.
- Reception preserves the `unviewed summary → profile` order and the existing modal input lifecycle.
- Guild Profile renders Gold, optional rank, a distinct leader region, companions, variable roster rows and current stats/skills without fixed content counts.
- Production Guild Hall prefab and text config contain the required localization-ready bindings.
- Equipment, editable composition, reward banking/selling, rank progression and quests remain outside PP-1 as planned.
- SaveStore `ForceSave()` still cannot report a durable-write failure to Application; this remains the PP-4 reliability gate.

Validation recorded for PP-1:

- focused EditMode regression: 42/42 passed;
- C# solution compile: 0 errors;
- production source prefab: `GuildHallView.ValidateBindings()` and `GuildProfileView.ValidateBindings()` passed in Unity Editor;
- a final filtered PlayMode rerun was not accepted as evidence because the current MCP runner returned an empty `0/0` suite; the earlier real run was 14/17 and its three failures were the now-fixed missing source-prefab Profile bindings;
- manual playtest and player build were not run.

## 10. Explicitly separate quest track

Quest system planning starts only from its own product behavior. It receives its own module owner, config, state, persistence key and implementation plan. PP milestones may consume a future read-only quest fact only after that contract exists; they do not pre-create quest fields or services.
