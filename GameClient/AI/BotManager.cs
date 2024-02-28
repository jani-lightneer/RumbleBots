using Microsoft.Xna.Framework;

namespace GameClient.AI
{
    public delegate void MoveHandler(
        int clientIndex,
        Vector2 target);

    public delegate void CastSpellHandler(
        int clientIndex,
        SpellType spellType,
        Vector2 direction);

    public class BotManager
    {
        private readonly Bot[] m_Bots;

        public BotManager(int botCount)
        {
            var sharedRandom = new CachedRandom();
            m_Bots = new Bot[botCount];

            for (int i = 0; i < m_Bots.Length; i++)
                m_Bots[i] = new Bot(sharedRandom, i);
        }

        public void RegisterMove(MoveHandler move)
        {
            for (int i = 0; i < m_Bots.Length; i++)
                m_Bots[i].Move = move;
        }

        public void RegisterCastSpell(CastSpellHandler castSpell)
        {
            for (int i = 0; i < m_Bots.Length; i++)
                m_Bots[i].CastSpell = castSpell;
        }

        public ICharacter GetBot(int index)
        {
            return m_Bots[index];
        }

        public void Update(WorldState worldState, float deltaTime)
        {
            foreach (var character in worldState.Characters)
            {
                if (character is Bot bot)
                    bot.Update(worldState, deltaTime);
            }
        }
    }
}