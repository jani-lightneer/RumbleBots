using System;
using SharedCode.AI;

namespace GameClient
{
    public class SkillCooldownTimer
    {
        private readonly float[] m_Timers;

        public SkillCooldownTimer()
        {
            int skillCount = Enum.GetValues<Skill>().Length;
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

        public bool Ready(Skill skill)
        {
            return m_Timers[(int)skill] == 0;
        }

        public void UseSkill(Skill skill)
        {
            switch (skill)
            {
                case Skill.Projectile:
                    m_Timers[(int)skill] = 4000f;
                    break;
                case Skill.Shield:
                    m_Timers[(int)skill] = 8000f;
                    break;
                case Skill.Dash:
                    m_Timers[(int)skill] = 5000f;
                    break;
            }
        }
    }
}