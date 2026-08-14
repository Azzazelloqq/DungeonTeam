using System;
using Code.Addressables.Generated;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    internal readonly struct SkillViewAssetDefinition
    {
        public SkillViewAssetDefinition(
            string presentationAddress,
            string iconAddress,
            string projectileAddress = null)
        {
            PresentationAddress = RequireAddress(
                presentationAddress,
                nameof(presentationAddress));
            IconAddress = RequireAddress(iconAddress, nameof(iconAddress));
            ProjectileAddress = string.IsNullOrWhiteSpace(projectileAddress)
                ? null
                : projectileAddress;
        }

        public string PresentationAddress { get; }
        public string IconAddress { get; }
        public string ProjectileAddress { get; }

        public string RequireProjectileAddress(string skillId)
        {
            return ProjectileAddress ?? throw new InvalidOperationException(
                $"Projectile view for skill ID '{skillId}' is not registered.");
        }

        private static string RequireAddress(string value, string parameterName)
        {
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException(
                    "Skill view asset address cannot be empty.",
                    parameterName);
        }
    }

    internal static class SkillViewAssetCatalog
    {
        public static SkillViewAssetDefinition Resolve(string skillId)
        {
            return skillId switch
            {
                "skill.strike.king" => new SkillViewAssetDefinition(
                    AddressableIds.Skills.SkillsKingStrikePresentation,
                    AddressableIds.Skills.SkillsKingStrikeIcon),
                "skill.smite.king" => new SkillViewAssetDefinition(
                    AddressableIds.Skills.SkillsKingSmitePresentation,
                    AddressableIds.Skills.SkillsKingSmiteIcon),
                "skill.bolt.druid" => new SkillViewAssetDefinition(
                    AddressableIds.Skills.SkillsDruidBoltPresentation,
                    AddressableIds.Skills.SkillsDruidBoltIcon,
                    AddressableIds.Skills.SkillsDruidBoltProjectile),
                "skill.heal.druid" => new SkillViewAssetDefinition(
                    AddressableIds.Skills.SkillsDruidHealPresentation,
                    AddressableIds.Skills.SkillsDruidHealIcon),
                "skill.lance.druid" => new SkillViewAssetDefinition(
                    AddressableIds.Skills.SkillsDruidNatureLancePresentation,
                    AddressableIds.Skills.SkillsDruidNatureLanceIcon,
                    AddressableIds.Skills.SkillsDruidNatureLanceProjectile),
                "skill.strike.rogue" => new SkillViewAssetDefinition(
                    AddressableIds.Skills.SkillsRogueStrikePresentation,
                    AddressableIds.Skills.SkillsRogueStrikeIcon),
                "skill.knife.rogue" => new SkillViewAssetDefinition(
                    AddressableIds.Skills.SkillsRogueShadowKnifePresentation,
                    AddressableIds.Skills.SkillsRogueShadowKnifeIcon,
                    AddressableIds.Skills.SkillsRogueShadowKnifeProjectile),
                "skill.bolt.arcane" => new SkillViewAssetDefinition(
                    AddressableIds.Skills.SkillsWizardArcaneBoltPresentation,
                    AddressableIds.Skills.SkillsWizardArcaneBoltIcon,
                    AddressableIds.Skills.SkillsWizardArcaneBoltProjectile),
                "skill.strike.skeleton" => new SkillViewAssetDefinition(
                    AddressableIds.Skills.SkillsMeleePresentation,
                    AddressableIds.Skills.SkillsSkeletonStrikeIcon),
                "skill.blast.skeleton" => new SkillViewAssetDefinition(
                    AddressableIds.Skills.SkillsFireballPresentation,
                    AddressableIds.Skills.SkillsFireballIcon),
                "skill.fireball" => new SkillViewAssetDefinition(
                    AddressableIds.Skills.SkillsFireballPresentation,
                    AddressableIds.Skills.SkillsFireballIcon,
                    AddressableIds.Skills.SkillsFireballProjectile),
                _ => throw new InvalidOperationException(
                    $"Skill view assets for skill ID '{skillId}' are not registered.")
            };
        }
    }
}
