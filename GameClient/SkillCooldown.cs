using System;
using SharedCode.Data;

namespace GameClient
{
    public class SkillCooldownTimer
    {
        private readonly float[] m_Timers;

        public SkillCooldownTimer()
        {
            int skillCount = Enum.GetValues<SkillGroup>().Length;
            m_Timers = new float[skillCount];
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < m_Timers.Length; i++)
            {
                m_Timers[i] -= deltaTime;
                if (m_Timers[i] <= 0f)
                    m_Timers[i] = 0f;
            }
        }

        public bool Ready(SkillGroup skillGroup)
        {
            return m_Timers[(int)skillGroup] == 0;
        }

        public void UseSkill(SkillGroup skillGroup)
        {
            switch (skillGroup)
            {
                case SkillGroup.Projectile:
                    m_Timers[(int)skillGroup] = 4000f;
                    break;
                case SkillGroup.Block:
                    m_Timers[(int)skillGroup] = 8000f;
                    break;
                case SkillGroup.Movement:
                    m_Timers[(int)skillGroup] = 5000f;
                    break;
            }
        }
    }
}