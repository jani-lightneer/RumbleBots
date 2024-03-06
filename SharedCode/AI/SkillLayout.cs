using System;
using SharedCode.Core;

namespace SharedCode.AI
{
    // TODO: Refactor
    public class SkillLayout
    {
        public const int TIER_ONE_LAYOUT_COUNT = 14;
        public const int TIER_TWO_LAYOUT_COUNT = 20;
        public const int TIER_THREE_LAYOUT_COUNT = 16;

        // TODO: Rename to RangedSkill
        public readonly Skill ProjectileSkill;
        public readonly Skill DefenceSkill;
        public readonly Skill UtilitySkill;

        public SkillLayout(Skill projectileSkill, Skill defenceSkill, Skill utilitySkill)
        {
            ProjectileSkill = projectileSkill;
            DefenceSkill = defenceSkill;
            UtilitySkill = utilitySkill;
        }

        public static int[] GetLayoutIds(int tier)
        {
            int layoutCount = 0;
            switch (tier)
            {
                case 1:
                    layoutCount = TIER_ONE_LAYOUT_COUNT;
                    break;
                case 2:
                    layoutCount = TIER_TWO_LAYOUT_COUNT;
                    break;
                case 3:
                    layoutCount = TIER_THREE_LAYOUT_COUNT;
                    break;
                default:
                    throw new NotImplementedException();
            }

            int[] layoutIds = new int[layoutCount];
            for (int i = 0; i < layoutIds.Length; i++)
                layoutIds[i] = i;

            return layoutIds;
        }

        public static SkillLayout GetLayout(int tier, int id)
        {
            switch (tier)
            {
                case 1:
                    return GetTierOneLayout(id);
                case 2:
                    return GetTierTwoLayout(id);
                case 3:
                    return GetTierThreeLayout(id);
                default:
                    throw new NotImplementedException();
            }
        }

        private static SkillLayout GetTierOneLayout(int id)
        {
            switch (id % TIER_ONE_LAYOUT_COUNT)
            {
                case 0:
                    return new SkillLayout(Skill.EnergyProjectile_1, Skill.CounterShield, Skill.Dash);
                case 1:
                    return new SkillLayout(Skill.EnergyProjectile_1, Skill.CounterShield, Skill.Stomp);
                case 2:
                    return new SkillLayout(Skill.EnergyProjectile_1, Skill.CounterShield, Skill.None);
                case 3:
                    return new SkillLayout(Skill.EnergyProjectile_1, Skill.Teleport, Skill.Dash);
                case 4:
                    return new SkillLayout(Skill.EnergyProjectile_1, Skill.Teleport, Skill.Stomp);
                case 5:
                    return new SkillLayout(Skill.EnergyProjectile_1, Skill.Teleport, Skill.None);
                case 6:
                    return new SkillLayout(Skill.RapidShot, Skill.CounterShield, Skill.Dash);
                case 7:
                    return new SkillLayout(Skill.RapidShot, Skill.CounterShield, Skill.Stomp);
                case 8:
                    return new SkillLayout(Skill.RapidShot, Skill.CounterShield, Skill.None);
                case 9:
                    return new SkillLayout(Skill.RapidShot, Skill.Teleport, Skill.Dash);
                case 10:
                    return new SkillLayout(Skill.RapidShot, Skill.Teleport, Skill.Stomp);
                case 11:
                    return new SkillLayout(Skill.RapidShot, Skill.Teleport, Skill.None);
                case 12:
                    return new SkillLayout(Skill.EnergyProjectile_2, Skill.CounterShield, Skill.None);
                case 13:
                    return new SkillLayout(Skill.EnergyProjectile_2, Skill.Teleport, Skill.None);
                default:
                    throw new Exception("Unexpected error");
            }
        }

        private static SkillLayout GetTierTwoLayout(int id)
        {
            switch (id % TIER_TWO_LAYOUT_COUNT)
            {
                case 0:
                    return new SkillLayout(Skill.EnergyProjectile_2, Skill.CounterShield, Skill.Dash);
                case 1:
                    return new SkillLayout(Skill.EnergyProjectile_2, Skill.CounterShield, Skill.Stomp);
                case 2:
                    return new SkillLayout(Skill.EnergyProjectile_2, Skill.CounterShield, Skill.None);
                case 3:
                    return new SkillLayout(Skill.EnergyProjectile_2, Skill.Teleport, Skill.Dash);
                case 4:
                    return new SkillLayout(Skill.EnergyProjectile_2, Skill.Teleport, Skill.Stomp);
                case 5:
                    return new SkillLayout(Skill.EnergyProjectile_2, Skill.Teleport, Skill.None);
                case 6:
                    return new SkillLayout(Skill.RapidShot, Skill.CounterShield, Skill.Dash);
                case 7:
                    return new SkillLayout(Skill.RapidShot, Skill.CounterShield, Skill.Stomp);
                case 8:
                    return new SkillLayout(Skill.RapidShot, Skill.CounterShield, Skill.None);
                case 9:
                    return new SkillLayout(Skill.RapidShot, Skill.Teleport, Skill.Dash);
                case 10:
                    return new SkillLayout(Skill.RapidShot, Skill.Teleport, Skill.Stomp);
                case 11:
                    return new SkillLayout(Skill.RapidShot, Skill.Teleport, Skill.None);
                case 12:
                    return new SkillLayout(Skill.EnergyProjectile_3, Skill.CounterShield, Skill.None);
                case 13:
                    return new SkillLayout(Skill.EnergyProjectile_3, Skill.Teleport, Skill.None);
                case 14:
                    return new SkillLayout(Skill.HomingMissile, Skill.CounterShield, Skill.Dash);
                case 15:
                    return new SkillLayout(Skill.HomingMissile, Skill.CounterShield, Skill.Stomp);
                case 16:
                    return new SkillLayout(Skill.HomingMissile, Skill.CounterShield, Skill.None);
                case 17:
                    return new SkillLayout(Skill.HomingMissile, Skill.Teleport, Skill.Dash);
                case 18:
                    return new SkillLayout(Skill.HomingMissile, Skill.Teleport, Skill.Stomp);
                case 19:
                    return new SkillLayout(Skill.HomingMissile, Skill.Teleport, Skill.None);
                default:
                    throw new Exception("Unexpected error");
            }
        }

        private static SkillLayout GetTierThreeLayout(int id)
        {
            switch (id % TIER_THREE_LAYOUT_COUNT)
            {
                case 0:
                    return new SkillLayout(Skill.EnergyProjectile_3, Skill.CounterShield, Skill.Dash);
                case 1:
                    return new SkillLayout(Skill.EnergyProjectile_3, Skill.CounterShield, Skill.Stomp);
                case 2:
                    return new SkillLayout(Skill.EnergyProjectile_3, Skill.Teleport, Skill.Dash);
                case 3:
                    return new SkillLayout(Skill.EnergyProjectile_3, Skill.Teleport, Skill.Stomp);
                case 4:
                    return new SkillLayout(Skill.RapidShot, Skill.CounterShield, Skill.Dash);
                case 5:
                    return new SkillLayout(Skill.RapidShot, Skill.CounterShield, Skill.Stomp);
                case 6:
                    return new SkillLayout(Skill.RapidShot, Skill.Teleport, Skill.Dash);
                case 7:
                    return new SkillLayout(Skill.RapidShot, Skill.Teleport, Skill.Stomp);
                case 8:
                    return new SkillLayout(Skill.HomingMissile, Skill.CounterShield, Skill.Dash);
                case 9:
                    return new SkillLayout(Skill.HomingMissile, Skill.CounterShield, Skill.Stomp);
                case 10:
                    return new SkillLayout(Skill.HomingMissile, Skill.Teleport, Skill.Dash);
                case 11:
                    return new SkillLayout(Skill.HomingMissile, Skill.Teleport, Skill.Stomp);
                case 12:
                    return new SkillLayout(Skill.EnergyProjectile_2, Skill.CounterShield, Skill.Dash);
                case 13:
                    return new SkillLayout(Skill.EnergyProjectile_2, Skill.CounterShield, Skill.Stomp);
                case 14:
                    return new SkillLayout(Skill.EnergyProjectile_2, Skill.Teleport, Skill.Dash);
                case 15:
                    return new SkillLayout(Skill.EnergyProjectile_2, Skill.Teleport, Skill.Stomp);
                default:
                    throw new Exception("Unexpected error");
            }
        }
    }
}