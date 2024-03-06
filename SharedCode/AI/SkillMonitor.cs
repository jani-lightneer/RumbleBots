namespace SharedCode.AI
{
    public class SkillManager
    {
        private readonly float[] m_SkillActiveTimes;
        private readonly float[] m_SkillRanges;
        private readonly float[] m_SkillDurations;

        public SkillManager(SkillConfigGroup skillConfigGroup)
        {
            m_SkillActiveTimes = new float[skillConfigGroup.Skills.Length];
            m_SkillDurations = new float[skillConfigGroup.Skills.Length];
            m_SkillRanges = new float[skillConfigGroup.Skills.Length];

            for (int i = 0; i < skillConfigGroup.Skills.Length; i++)
            {
                m_SkillRanges[i] = skillConfigGroup.Skills[i].Range;
                m_SkillDurations[i] = skillConfigGroup.Skills[i].Duration;
            }
        }

        public void ActiveSkill(Skill skill)
        {
            m_SkillActiveTimes[(int)skill] = m_SkillDurations[(int)skill];
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < m_SkillActiveTimes.Length; i++)
            {
                m_SkillActiveTimes[i] -= deltaTime;
                if (m_SkillActiveTimes[i] <= 0)
                    m_SkillActiveTimes[i] = 0;
            }
        }

        public float GetActiveTime(Skill skill)
        {
            return m_SkillActiveTimes[(int)skill];
        }

        public float GetRange(Skill skill)
        {
            return m_SkillRanges[(int)skill];
        }
    }
}