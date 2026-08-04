using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Rewards.Runtime;
using DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup.Base;
using NUnit.Framework;
using UnityEngine;

namespace DungeonTeam.Gameplay.Rewards.Tests
{
    public sealed class RewardPickupFactoryTests
    {
        [Test]
        public void Create_CollectAndDispose_OwnsPickupGraph()
        {
            var prefabObject = new GameObject("RewardPickupTestPrefab");
            var prefab = prefabObject.AddComponent<TestRewardPickupView>();
            var parentObject = new GameObject("RewardPickupTestParent");
            var factory = new RewardPickupFactory();
            var position = new Vector3(1f, 2f, 3f);
            var definition = new RewardDefinition(
                "reward.gold",
                "Gold");
            RewardPickupInstance instance = null;

            try
            {
                instance = factory.Create(
                    prefab,
                    new RewardPickupSpawnRequest(position, definition, amount: 2),
                    parentObject.transform);
                var instanceView = parentObject.transform
                    .GetChild(0)
                    .GetComponent<TestRewardPickupView>();

                Assert.That(instance.Position, Is.EqualTo(position));
                Assert.That(instance.IsCollected, Is.False);
                Assert.That(instanceView.IsCollectedVisual, Is.False);

                Assert.That(instance.TryCollect(out var firstReward), Is.True);
                Assert.That(firstReward.RewardId, Is.EqualTo("reward.gold"));
                Assert.That(firstReward.Amount, Is.EqualTo(2));
                Assert.That(instance.IsCollected, Is.True);
                Assert.That(instanceView.IsCollectedVisual, Is.True);

                Assert.That(instance.TryCollect(out var secondReward), Is.False);
                Assert.That(secondReward, Is.EqualTo(default(RewardGrant)));

                instance.Dispose();
                Assert.That(parentObject.transform.childCount, Is.Zero);
                Assert.DoesNotThrow(instance.Dispose);
            }
            finally
            {
                instance?.Dispose();
                Object.DestroyImmediate(prefabObject);
                Object.DestroyImmediate(parentObject);
            }
        }

        public sealed class TestRewardPickupView : RewardPickupViewBase
        {
            public bool IsCollectedVisual { get; private set; }

            public override Vector3 Position => transform.position;

            public override void SetCollected(bool isCollected)
            {
                IsCollectedVisual = isCollected;
            }

            protected override void OnInitialize()
            {
            }

            protected override ValueTask OnInitializeAsync(CancellationToken token)
            {
                return default;
            }

            protected override void OnDispose()
            {
            }

            protected override ValueTask OnDisposeAsync(CancellationToken token)
            {
                return default;
            }
        }
    }
}
