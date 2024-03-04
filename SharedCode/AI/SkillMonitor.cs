namespace SharedCode.AI
{
    public class SkillMonitor
    {
        private readonly float[] m_SkillActiveTimes;
        private readonly float[] m_SkillDurations;

        public SkillMonitor(SkillConfigGroup skillConfigGroup)
        {
            m_SkillActiveTimes = new float[skillConfigGroup.Skills.Length];
            m_SkillDurations = new float[skillConfigGroup.Skills.Length];

            for (int i = 0; i < skillConfigGroup.Skills.Length; i++)
                m_SkillDurations[i] = skillConfigGroup.Skills[i].Duration;
        }

        public void Start(Skill skill)
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
    }
}