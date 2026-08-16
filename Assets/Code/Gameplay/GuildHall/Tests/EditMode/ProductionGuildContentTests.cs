using System;
using Azzazelloqq.Config;
using Code.Configuration;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Config;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Config;
using DungeonTeam.UI.WorldMap;
using NUnit.Framework;
using UnityEditor;

namespace DungeonTeam.Gameplay.GuildHall.Tests.EditMode
{
    public sealed class ProductionGuildContentTests
    {
        [Test]
        public void ConfigCatalog_RegisteredGuildPages_CreateConsistentCatalogs()
        {
            var catalogAsset = AssetDatabase.LoadAssetAtPath<ConfigCatalog>(
                "Assets/Content/Configuration/ConfigCatalog.asset");
            Assert.That(catalogAsset, Is.Not.Null);
            var pages = catalogAsset.GetPages();

            var guildHall = RequirePage<GuildHallConfigPage>(pages).CreateCatalog();
            var dialogues = RequirePage<DialogueConfigPage>(pages).CreateCatalog();
            var profiles = RequirePage<AmbientNpcConfigPage>(pages).CreateCatalog();
            var contracts = RequirePage<ContractConfigPage>(pages).CreateCatalog();
            var worldMap = RequirePage<WorldMapConfigPage>(pages).CreateCatalog();
            var launchPresets = RequirePage<DungeonRunLaunchConfigPage>(pages).CreateCatalog();

            Assert.DoesNotThrow(() => GuildContentValidator.Validate(
                guildHall,
                dialogues,
                profiles,
                contracts,
                worldMap.ContractDestinationLocationIds));
            for (var index = 0; index < worldMap.Locations.Count; index++)
            {
                var location = worldMap.Locations[index];
                if (location.DestinationKind == WorldLocationDestinationKind.DungeonRun)
                {
                    Assert.DoesNotThrow(() => launchPresets.Require(location.DestinationId));
                }
            }

            Assert.That(guildHall.Npcs, Is.Not.Empty);
            Assert.That(contracts.Offers, Is.Not.Empty);
            Assert.That(worldMap.Locations, Is.Not.Empty);
            Assert.That(guildHall.NoticeBoardText.Header.DisplayText, Is.Not.Empty);
        }

        private static TPage RequirePage<TPage>(IConfigPage[] pages)
            where TPage : class, IConfigPage
        {
            for (var index = 0; index < pages.Length; index++)
            {
                if (pages[index] is TPage page)
                {
                    return page;
                }
            }

            throw new InvalidOperationException(
                $"Production ConfigCatalog has no {typeof(TPage).Name}.");
        }
    }
}
