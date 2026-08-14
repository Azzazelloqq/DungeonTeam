using NUnit.Framework;
using UnityEditor;

namespace DungeonTeam.Gameplay.Dungeon.Tests.EditMode
{
    public sealed class ProductionContentDependencyTests
    {
        [Test]
        public void ProductionContent_DoesNotDependOnImportedAssets()
        {
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { "Assets/Content" });
            for (var index = 0; index < guids.Length; index++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[index]);
                var dependencies = AssetDatabase.GetDependencies(assetPath, recursive: true);
                for (var dependencyIndex = 0;
                     dependencyIndex < dependencies.Length;
                     dependencyIndex++)
                {
                    StringAssert.DoesNotStartWith(
                        "Assets/ImportedAssets/",
                        dependencies[dependencyIndex],
                        $"Production asset '{assetPath}' depends on imported content.");
                }
            }
        }
    }
}
