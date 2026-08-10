using System;
using Code.Addressables.Generated;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    internal static class SkillViewAssetCatalog
    {
        public static string ResolveProjectileAddress(string skillId)
        {
            return skillId switch
            {
                "skill.bolt.druid" => AddressableIds.Skills.SkillsDruidBoltProjectile,
                "skill.lance.druid" => AddressableIds.Skills.SkillsDruidNatureLanceProjectile,
                "skill.knife.rogue" => AddressableIds.Skills.SkillsRogueShadowKnifeProjectile,
                "skill.bolt.arcane" => AddressableIds.Skills.SkillsWizardArcaneBoltProjectile,
                "skill.fireball" => AddressableIds.Skills.SkillsFireballProjectile,
                _ => throw new InvalidOperationException(
                    $"Projectile view for skill ID '{skillId}' is not registered.")
            };
        }

        public static string ResolvePresentationAddress(string skillId)
        {
            return skillId switch
            {
                "skill.strike.king" => AddressableIds.Skills.SkillsKingStrikePresentation,
                "skill.smite.king" => AddressableIds.Skills.SkillsKingSmitePresentation,
                "skill.bolt.druid" => AddressableIds.Skills.SkillsDruidBoltPresentation,
                "skill.lance.druid" => AddressableIds.Skills.SkillsDruidNatureLancePresentation,
                "skill.strike.rogue" => AddressableIds.Skills.SkillsRogueStrikePresentation,
                "skill.knife.rogue" => AddressableIds.Skills.SkillsRogueShadowKnifePresentation,
                "skill.bolt.arcane" => AddressableIds.Skills.SkillsWizardArcaneBoltPresentation,
                "skill.strike.skeleton" => AddressableIds.Skills.SkillsMeleePresentation,
                "skill.fireball" => AddressableIds.Skills.SkillsFireballPresentation,
                _ => throw new InvalidOperationException(
                    $"Presentation for skill ID '{skillId}' is not registered.")
            };
        }
    }
}
