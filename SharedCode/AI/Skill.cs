using System;

namespace SharedCode.AI
{
    public enum Skill
    {
        None,
        EnergyProjectile_1,
        EnergyProjectile_2,
        EnergyProjectile_3,
        RapidShot,
        HomingMissile,
        CounterShield,
        Teleport,
        Dash,
        Stomp
    }

    public class SkillConfig
    {
        public readonly Skill Skill;
        public readonly float Range;
        public readonly float Duration;

        public SkillConfig(Skill skill, float range, float duration)
        {
            Skill = skill;
            Range = range;
            Duration = duration;
        }
    }

    public class SkillConfigGroup
    {
        public readonly SkillConfig[] Skills;

        public SkillConfigGroup(SkillConfig[] skillConfigs)
        {
            int skillCount = Enum.GetValues(typeof(Skill)).Length;
            Skills = new SkillConfig[skillCount];

            int index = 0;
            foreach (Skill skill in Enum.GetValues(typeof(Skill)))
            {
                if (skill == Skill.None)
                {
                    Skills[index] = new SkillConfig(Skill.None, 0, 0);
                    index++;
                    continue;
                }

                Skills[index] = FindSkillConfig(skill, skillConfigs);
                index++;
            }
        }

        private SkillConfig FindSkillConfig(Skill skill, SkillConfig[] skillConfigs)
        {
            for (int i = 0; i < skillConfigs.Length; i++)
            {
                if (skillConfigs[i].Skill == skill)
                    return skillConfigs[i];
            }

            throw new Exception($"Could not find skill {skill}");
        }
    }
}