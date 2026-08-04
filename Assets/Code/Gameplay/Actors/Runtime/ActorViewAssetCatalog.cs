using System;
using Code.Addressables.Generated;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    internal static class ActorViewAssetCatalog
    {
        public static string ResolveAddress(string actorId)
        {
            return actorId switch
            {
                "actor.king" =>
                    AddressableIds.Characters.CharactersCharacterMaleKing,
                "actor.druid" =>
                    AddressableIds.Characters.CharactersCharacterFemaleDruid,
                "actor.rogue" =>
                    AddressableIds.Characters.CharactersCharacterMaleRouge,
                "actor.wizard" =>
                    AddressableIds.Characters.CharactersCharacterMaleWizard,
                "actor.skeleton.mage" =>
                    AddressableIds.Characters.CharactersCharacterSkeletonMage,
                "actor.skeleton.warrior" =>
                    AddressableIds.Characters.CharactersCharacterSkeletonWarrior,
                _ => throw new InvalidOperationException(
                    $"Actor view for ID '{actorId}' is not registered.")
            };
        }
    }
}
