using System;
using SharedCode.Data;

namespace GameClient
{
    public class SkillCooldown
    {
        private readonly float[] m_SkillTimers;

        public SkillCooldown()
        {
            int skillCount = Enum.GetValues<SkillGroup>().Length;
            m_SkillTimers = new float[skillCount];
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < m_SkillTimers.Length; i++)
            {
                m_SkillTimers[i] -= deltaTime;
                if (m_SkillTimers[i] <= 0f)
                    m_SkillTimers[i] = 0f;
            }
        }

        public bool Ready(SkillGroup skillGroup)
        {
            return m_SkillTimers[(int)skillGroup] == 0;
        }

        public void UseSkill(SkillGroup skillGroup)
        {
            m_SkillTimers[(int)skillGroup] = 1000f;
        }
    }
}