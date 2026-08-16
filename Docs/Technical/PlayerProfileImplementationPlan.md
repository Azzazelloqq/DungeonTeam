# DungeonTeam — Player Profile Implementation Plan

**Статус:** PP-0/PP-1/PP-2 COMPLETE; PP-3 REQUIRES PRODUCT/TECHNICAL DESIGN

**Версия:** 0.3

**Дата:** 16 августа 2026

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
| PP-3 | Inventory/equipment after separate item design | Requires product/design decision |
| PP-4 | Result commit, Gold banking and selling | Planned after persistence reliability gate |
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

Before code, a separate product/technical decision must define:

- whether gear is unique instances or stackable definitions;
- current equipment slots;
- ownership and sale semantics;
- stat/effect application;
- what happens to incompatible/removed content;
- the minimum actual item set that changes gameplay.

After that decision:

1. Add the smallest real Item/Equipment Domain/Application boundary; do not put item rules in PlayerProfile UI.
2. Add typed item definitions/config only for implemented content.
3. Extend profile save through V1→V2 migration with inventory/equipped IDs.
4. Add equip/unequip use cases and derived actor/run snapshot application.
5. Add equipment details/editing to Profile UI.
6. Prove that equipped effects reach the actual run and are removed/replaced correctly.

Empty slots, fake items and a generic modifier framework are not PP-3 substitutes.

## 6. PP-4 — rewards, Gold and selling

**Prerequisite:** persistence write failures must be observable to Application.

1. Give terminal results a stable id and explicit bankable payload; keep run-local collection separate.
2. Add idempotent Application commit: the same result cannot change the profile twice.
3. Persist pending/applied state before showing rewards as banked.
4. Map Gold rewards to the single wallet; item/crystal rewards to real inventory definitions.
5. Replace session-only debrief data where needed with an already-applied persistent snapshot.
6. Add concrete Reception sell use case with prepared prices and an atomic inventory→Gold mutation.
7. Test retry after failure, reload and double-submit.

No shop, exchange rates or second spendable currency are added.

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
