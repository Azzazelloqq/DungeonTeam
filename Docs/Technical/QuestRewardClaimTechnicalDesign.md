# DungeonTeam — Quest Q-1 Reward Claim Technical Design

**Status:** implemented and automation-validated; manual player-flow smoke/build not run

**Scope:** a completed configured quest may be actively claimed at the reception or a configured Guild Hall NPC. Q-1 supports Gold and stacked resource grants only.

## 1. Boundary and ownership

`Quests` owns quest completion, reward definition and claim eligibility. `PlayerProfile` owns Gold, resource inventory and durable idempotency for banked rewards. `GuildHall` renders immutable snapshots and returns semantic actions. `Bootstrap` is the only cross-feature coordinator.

```text
Bootstrap
 ├─ QuestSession (quest state / guild.quests)
 ├─ PlayerProfileSession (Gold/resources / player.profile)
 └─ GuildHallRoot
     ├─ GuildProfile MVVM → Reception reward action
     ├─ RewardCollection MVVM → explicit receive rows
     └─ AmbientNpc dialogue close → NPC reward collection
```

No new root, DI scope, event bus, registry or general claim-point module. Existing Quests and PlayerProfile assemblies are extended; Guild Hall stays in its current assemblies. `GuildHall.Runtime` never sees sessions, SaveStore or config; AmbientNpc has no dependency on quests/profile/rewards; Quests and PlayerProfile do not reference each other.

## 2. Config/domain

`QuestDefinition` receives optional `QuestRewardDefinition`: `GoldAmount >= 0`, an ordered defensive unique-by-definition resource list (each amount > 0), `QuestRewardClaimPoint`, and `QuestText claimHint`. The bundle must contain Gold or a resource. The claim point is a small Quests value object with only `Reception` (no target) and `Npc` (required `npcId`). `QuestConfigPage` serializes it. `QuestCatalog` validates structural rules; Bootstrap validates resources against `ItemCatalog` and NPC targets against `GuildHallCatalog`.

Production content demonstrates both routes: `quest.crystal-supply` pays reception Gold/resources; `quest.speak-debater` pays from `npc.debater`; `quest.clear-crypt` remains rewardless.

## 3. State, persistence and exact-once banking

`QuestState` gains ordered `claimedRewardQuestIds` and pure `IsRewardClaimed`, `GetClaimableAt(point, catalog)`, `TryMarkRewardClaimed(questId, catalog)`. Only a completed reward-bearing quest can be marked. `QuestSaveV2` adds that list under existing `guild.quests`; V1 migrates with an empty list.

Player Profile adds narrow `ClaimReward(ProfileRewardClaimRequest)`: stable `claimId`, Gold and resources, with no Quest/GuildHall references. Profile state stores applied claim IDs; `player.profile` migrates V4→V5. The use case checks the ID first, then commits Gold/resources plus the ID atomically in the single profile save and returns `Applied` or `AlreadyApplied`. This is a compact idempotency boundary for two durable owners, not a general reward framework. Q-1 derives `claimId` as `quest.reward:<questId>` because quests are non-repeatable.

Coordinator sequence:

1. Bootstrap revalidates completed/unclaimed quest and exact configured point.
2. It sends the bundle to `PlayerProfileSession.ClaimReward`.
3. On `Applied` or `AlreadyApplied`, it calls `QuestSession.MarkRewardClaimed`, which saves before publishing.
4. A Profile save failure leaves Quest unchanged. A Quest save failure after Profile success is recoverable: retry sees `AlreadyApplied`, then marks Quest without a duplicate payout.

There is no distributed transaction, rollback or optimistic UI update.

## 4. Guild Hall flow and lifecycle

Bootstrap prepares immutable `QuestRewardClaimSnapshot` entries (quest ID, title, reward lines, source hint, receive text). `GuildHallStartContext` receives only callbacks for current entries at a point and a claim request. `GuildProfileSnapshot` adds prepared reception reward action/count; it still cannot mutate profile state.

CQ-1 supplies a second real source (Contracts), so the Guild Hall-local MVVM family is now neutral `RewardCollection`. Its entry carries a typed source identity (`Quest` or `Contract`) and stable source ID; Bootstrap routes it explicitly. The family creates/registers/initializes under its parent, owns item ViewModels, replaces rows only after a successful response and never reads config or persistence.

Reception opens the filtered collection from Guild Profile after the existing summary/profile policy. NPC flow is unchanged until normal dialogue close: GuildHallRoot first forwards the Q-0 dialogue-completed callback, then asks Bootstrap for entries at that NPC and opens collection only when non-empty. Dialogue close never grants rewards. No new tick, async operation, Addressables owner or AmbientNpc contract is added.

## 5. Assets and validation

Author through Unity Editor only: a reception reward action/count in Guild Profile; inactive `QuestRewardCollection` panel and row template (title, reward lines, receive, close); and a Board claim hint only if needed for legibility. Do not hand-edit prefab YAML.

Focused EditMode tests:

- config/state validation, point matching, completed-only and once-only claims with variable fixtures;
- Quest V1→V2 and Profile V4→V5 migration;
- correct Gold/resource grant, duplicate `claimId` no-op, profile-save failure, and Profile-applied/Quest-unmarked retry;
- Bootstrap forged/wrong-point rejection and snapshot separation;
- reception filtering, NPC post-dialogue collection and explicit-receive-only UI command.

Run focused Quests, PlayerProfile, Bootstrap and GuildHall EditMode tests, compilation for affected assemblies and the Unity mechanical validator. Manual Unity smoke is required for both reception/NPC routes and restart after claim. Build/external playtest are outside Q-1.

## 6. Delivery order and non-goals

1. TDD Quests reward data/state/V2 save and Profile reward-claim/V5 save.
2. Compose Bootstrap validation and retry-safe coordinator.
3. Extend snapshots and callbacks; implement local collection MVVM.
4. Author config/prefab bindings and run validation.

No rewards for contracts/chests/mail/shops/achievements; no automatic claims, other point kinds, unique equipment grants, choices/history/refunds, repeatables, generic event bus or localization system.

## 7. Implementation evidence

Implemented the optional Quest reward config, Quest V1→V2 claimed-marker persistence, Player Profile V4→V5 idempotent claim banking and Bootstrap's Profile-first coordinator. The production config contains rewardless `quest.clear-crypt`, Reception reward `quest.crystal-supply`, and `npc.debater` reward `quest.speak-debater`. Guild Hall now has an Editor-authored inactive reward collection panel, row template and binding.

Validation in Unity `6000.7.0a3`:

- focused Quests, PlayerProfile, GuildHall and Bootstrap EditMode tests: `94/94 passed`;
- `dotnet build Bootstrap.csproj --no-restore -v:minimal`: `0` warnings, `0` errors;
- production `QuestConfigPage.CreateCatalog()` returned exactly rewardless/reception/NPC variants; prefab reward view binding validated in Editor and starts inactive;
- Editor console after refresh: `0` errors.

The general mechanical validator reports serialization whitespace in the Unity-authored Quest config/Guild Hall prefab and the pre-existing TMP fallback asset. No prefab YAML was hand-edited. Manual reception/NPC/restart smoke, player build and external playtest remain unrun.
