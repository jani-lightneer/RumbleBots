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
            if (skill == Skill.None)
                return false;

            return m_Timers[(int)skill] == 0;
        }

        public void UseSkill(Skill skill)
        {
            switch (skill)
            {
                case Skill.None:
                    // Skip
                    break;
                case Skill.EnergyProjectile_1:
                case Skill.EnergyProjectile_2:
                case Skill.EnergyProjectile_3:
                    m_Timers[(int)skill] = 4000f;
                    break;
                case Skill.RapidShot:
                    m_Timers[(int)skill] = 3000f;
                    break;
                case Skill.HomingMissile:
                    m_Timers[(int)skill] = 3000f;
                    break;
                case Skill.CounterShield:
                    m_Timers[(int)skill] = 8000f;
                    break;
                case Skill.Teleport:
                    m_Timers[(int)skill] = 12000f;
                    break;
                case Skill.Dash:
                    m_Timers[(int)skill] = 5000f;
                    break;
                case Skill.Stomp:
                    m_Timers[(int)skill] = 3000f;
                    break;
            }
        }
    }
}