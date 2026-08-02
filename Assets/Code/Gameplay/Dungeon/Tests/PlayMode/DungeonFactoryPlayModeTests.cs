using System.Collections;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.Dungeon.Runtime.Config;
using DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonTeam.Gameplay.Dungeon.Tests.PlayMode
{
    public sealed class DungeonFactoryPlayModeTests
    {
        private const string ConfigJson =
            "{\"_authoredDungeons\":[{\"_dungeonId\":\"dungeon.demo.authored\"," +
            "\"_mapAssetId\":\"map.authored.demo\"}]," +
            "\"_chunkedDungeons\":[{\"_dungeonId\":\"dungeon.demo.chunked\"," +
            "\"_entryChunkId\":\"chunk.demo.entry\"," +
            "\"_exitChunkId\":\"chunk.demo.exit\"," +
            "\"_mandatoryChunkIds\":[\"chunk.demo.mandatory\"]," +
            "\"_chunkPool\":[\"chunk.demo.room\"]," +
            "\"_targetChunkCount\":5,\"_maxGenerationAttempts\":8}]," +
            "\"_scenarios\":[{\"_scenarioId\":\"scenario.demo\"," +
            "\"_baseThreatBudget\":1," +
            "\"_enemyCandidates\":[{\"_enemyId\":\"enemy.grunt\",\"_cost\":1," +
            "\"_weight\":1,\"_allowedSlotTags\":[\"enemy.common\"]}]," +
            "\"_interestPointRules\":[{\"_slotTag\":\"interest.common\"," +
            "\"_minCount\":1,\"_maxCount\":1," +
            "\"_candidates\":[{\"_interestPointId\":\"interest.chest.basic\"," +
            "\"_weight\":1,\"_rewardProfileId\":\"reward.common\"}]}]," +
            "\"_enabledOptionalPlacementIds\":[\"interest.optional.chest\"]," +
            "\"_requiredObjectives\":[{\"_objectiveId\":\"objective.exit\"," +
            "\"_requiredSlotTag\":\"objective.exit\"}]}]," +
            "\"_difficulties\":[{\"_difficultyId\":\"normal\"," +
            "\"_threatBudgetMultiplier\":1," +
            "\"_interestPointCountMultiplier\":1," +
            "\"_rewardBudgetMultiplier\":1}]}";

        [UnityTest]
        public IEnumerator CreateAndDispose_DemoAuthoredDungeon_BuildsAndReleasesMap()
        {
            var config = ScriptableObject.CreateInstance<DungeonConfigPage>();
            JsonUtility.FromJsonOverwrite(ConfigJson, config);

            var factory = new DungeonFactory(config);
            var request = new DungeonBuildRequest(
                "dungeon.demo.authored",
                "scenario.demo",
                "normal",
                seed: 42);
            IDungeonInstance instance = null;

            yield return factory.CreateAsync(request, default)
                .ToCoroutine(result => instance = result);

            var mapRoot = GameObject.Find("AuthoredDungeonDemo(Clone)");
            try
            {
                Assert.That(instance, Is.Not.Null);
                Assert.That(mapRoot, Is.Not.Null);
                Assert.That(instance.MapSnapshot.DungeonId, Is.EqualTo("dungeon.demo.authored"));
                Assert.That(instance.ContentPlan.EnemySpawns, Has.Count.EqualTo(2));
                Assert.That(instance.ContentPlan.InterestPointSpawns, Has.Count.EqualTo(2));
                Assert.That(instance.ContentPlan.ObjectiveSpawns, Has.Count.EqualTo(1));
            }
            finally
            {
                instance?.Dispose();
                Object.Destroy(config);
            }

            yield return null;

            Assert.That(mapRoot == null, Is.True);
        }

        [UnityTest]
        public IEnumerator CreateAndDispose_DemoChunkedDungeon_BuildsAndReleasesAllChunks()
        {
            var config = ScriptableObject.CreateInstance<DungeonConfigPage>();
            JsonUtility.FromJsonOverwrite(ConfigJson, config);

            var factory = new DungeonFactory(config);
            var request = new DungeonBuildRequest(
                "dungeon.demo.chunked",
                "scenario.demo",
                "normal",
                seed: 42);
            IDungeonInstance instance = null;

            yield return factory.CreateAsync(request, default)
                .ToCoroutine(result => instance = result);

            var mapRoot = GameObject.Find("ChunkedDungeon_dungeon.demo.chunked");
            GameObject[] chunkRoots = null;
            try
            {
                Assert.That(instance, Is.Not.Null);
                Assert.That(mapRoot, Is.Not.Null);
                Assert.That(mapRoot.transform.childCount, Is.EqualTo(5));
                Assert.That(instance.MapSnapshot.DungeonId, Is.EqualTo("dungeon.demo.chunked"));
                Assert.That(instance.ContentPlan.EnemySpawns, Has.Count.EqualTo(2));
                Assert.That(instance.ContentPlan.InterestPointSpawns, Has.Count.EqualTo(1));
                Assert.That(instance.ContentPlan.ObjectiveSpawns, Has.Count.EqualTo(1));

                chunkRoots = new GameObject[mapRoot.transform.childCount];
                for (var index = 0; index < chunkRoots.Length; index++)
                {
                    chunkRoots[index] = mapRoot.transform.GetChild(index).gameObject;
                }
            }
            finally
            {
                instance?.Dispose();
                Object.Destroy(config);
            }

            yield return null;

            Assert.That(mapRoot == null, Is.True);
            for (var index = 0; index < chunkRoots.Length; index++)
            {
                Assert.That(chunkRoots[index] == null, Is.True);
            }
        }
    }
}
