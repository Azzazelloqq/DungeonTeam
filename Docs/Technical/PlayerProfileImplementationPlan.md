# DungeonTeam — Player Profile Implementation Plan

**Статус:** PP-0/PP-1 COMPLETE; PP-2 NEXT

**Версия:** 0.1

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
| PP-2 | Editable leader/team/loadout and run integration | Next |
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

1. Agree exact selection interaction within the existing Profile screen.
2. Add profile operations to choose leader, add/remove ordered companions and choose an allowed loadout.
3. Validate membership/uniqueness in Profile Domain and current run constraints through `DungeonRunTeamSetup` at application boundary.
4. Save only after a valid operation; show an explicit reason for rejected/incomplete selection.
5. Build `DungeonRunTeamSelection` from the latest profile snapshot in `WorldMapDestinationResolver` instead of `.DefaultSelection`.
6. Test variable roster/team sizes and at least two fixture compositions without asserting production count.

PP-2 does not add recruitment, hero purchase, equipment or skill-tree progression.

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
