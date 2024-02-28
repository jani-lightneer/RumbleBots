namespace GameClient
{
    // Debug version
    public class SpellBook
    {
        private float m_DebugCooldown;

        public SpellBook()
        {
            m_DebugCooldown = 0f;
        }

        public void Initialize()
        {
        }

        public void Update(float deltaTime)
        {
            m_DebugCooldown -= deltaTime;
            if (m_DebugCooldown <= 0f)
                m_DebugCooldown = 0f;
        }

        public float GetSpellCooldown()
        {
            return m_DebugCooldown;
        }

        public void StartSpellCooldown()
        {
            m_DebugCooldown = 1000f;
        }
    }
}