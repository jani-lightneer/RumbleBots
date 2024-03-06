using System;
using SharedCode.Core;

namespace SharedCode.AI
{
    public class SkillLayout
    {
        public const int TIER_ONE_LAYOUT_COUNT = 8;
        public const int TIER_TWO_LAYOUT_COUNT = 6;
        public const int TIER_THREE_LAYOUT_COUNT = 5;

        public readonly int Id;
        public readonly Skill ProjectileSkill;
        public readonly Skill DefenceSkill;
        public readonly Skill UtilitySkill;

        public SkillLayout(int id, Skill projectileSkill, Skill defenceSkill, Skill utilitySkill)
        {
            Id = id;
            ProjectileSkill = projectileSkill;
            DefenceSkill = defenceSkill;
            UtilitySkill = utilitySkill;
        }

        // TODO: Refactor
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

        public static SkillLayout GetTierOneLayout(int id)
        {
            switch (id % TIER_ONE_LAYOUT_COUNT)
            {
                case 0:
                    return new SkillLayout(GetId(id, 1), Skill.EnergyProjectile_1, Skill.CounterShield, Skill.Dash);
                case 1:
                    return new SkillLayout(GetId(id, 1), Skill.EnergyProjectile_1, Skill.Teleport, Skill.Dash);
                case 2:
                    return new SkillLayout(GetId(id, 1), Skill.EnergyProjectile_1, Skill.CounterShield, Skill.Stomp);
                case 3:
                    return new SkillLayout(GetId(id, 1), Skill.RapidShot, Skill.CounterShield, Skill.Dash);
                case 4:
                    return new SkillLayout(GetId(id, 1), Skill.RapidShot, Skill.Teleport, Skill.Dash);
                case 5:
                    return new SkillLayout(GetId(id, 1), Skill.RapidShot, Skill.CounterShield, Skill.Stomp);
                case 6:
                    return new SkillLayout(GetId(id, 1), Skill.EnergyProjectile_2, Skill.CounterShield, Skill.None);
                case 7:
                    return new SkillLayout(GetId(id, 1), Skill.EnergyProjectile_2, Skill.Teleport, Skill.None);
                case 8: // Bad ass
                    return new SkillLayout(GetId(id, 1), Skill.EnergyProjectile_1, Skill.Teleport, Skill.Stomp);
                default:
                    throw new Exception("Unexpected error");
            }
        }

        public static SkillLayout GetTierTwoLayout(int id)
        {
            switch (id % TIER_TWO_LAYOUT_COUNT)
            {
                case 0:
                    return new SkillLayout(GetId(id, 2), Skill.EnergyProjectile_2, Skill.CounterShield, Skill.Dash);
                case 1:
                    return new SkillLayout(GetId(id, 2), Skill.EnergyProjectile_2, Skill.Teleport, Skill.Dash);
                case 2:
                    return new SkillLayout(GetId(id, 2), Skill.EnergyProjectile_2, Skill.CounterShield, Skill.Stomp);
                case 3:
                    return new SkillLayout(GetId(id, 2), Skill.HomingMissile, Skill.CounterShield, Skill.Dash);
                case 4:
                    return new SkillLayout(GetId(id, 2), Skill.RapidShot, Skill.Teleport, Skill.Dash);
                case 5:
                    return new SkillLayout(GetId(id, 2), Skill.HomingMissile, Skill.CounterShield, Skill.Stomp);
                case 6: // Bad ass
                    return new SkillLayout(GetId(id, 2), Skill.EnergyProjectile_2, Skill.Teleport, Skill.Stomp);
                default:
                    throw new Exception("Unexpected error");
            }
        }

        public static SkillLayout GetTierThreeLayout(int id)
        {
            switch (id % TIER_THREE_LAYOUT_COUNT)
            {
                case 0:
                    return new SkillLayout(GetId(id, 3), Skill.EnergyProjectile_3, Skill.CounterShield, Skill.Dash);
                case 1:
                    return new SkillLayout(GetId(id, 3), Skill.EnergyProjectile_3, Skill.Teleport, Skill.Dash);
                case 2:
                    return new SkillLayout(GetId(id, 3), Skill.EnergyProjectile_3, Skill.CounterShield, Skill.Stomp);
                case 3:
                    return new SkillLayout(GetId(id, 3), Skill.HomingMissile, Skill.CounterShield, Skill.Dash);
                case 4:
                    return new SkillLayout(GetId(id, 3), Skill.HomingMissile, Skill.CounterShield, Skill.Stomp);
                case 5: // Bad ass
                    return new SkillLayout(GetId(id, 2), Skill.EnergyProjectile_3, Skill.Teleport, Skill.Stomp);
                default:
                    throw new Exception("Unexpected error");
            }
        }

        private static int GetId(int id, int tier)
        {
            switch (tier)
            {
                case 1:
                    return id;
                case 2:
                    return id + TIER_ONE_LAYOUT_COUNT;
                case 3:
                    return id + TIER_ONE_LAYOUT_COUNT + TIER_TWO_LAYOUT_COUNT;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}