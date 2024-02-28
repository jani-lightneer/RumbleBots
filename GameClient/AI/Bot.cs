using System;
using Microsoft.Xna.Framework;

namespace GameClient.AI
{
    public class Bot : ICharacter
    {
        public const int CIRCLE_COLLIDER_RADIUS = 16;

        public MoveHandler Move;
        public CastSpellHandler CastSpell;

        public int ClientIndex { get; private set; }
        public Vector2 Position { get; set; }

        private readonly CachedRandom m_Random;
        private Vector2 m_MoveTarget;
        private SpellBook m_SpellBook;

        public Bot(CachedRandom random, int clientIndex)
        {
            ClientIndex = clientIndex;
            Position = Vector2.Zero;

            m_Random = random;
            m_MoveTarget = new Vector2(500, 500);
            m_SpellBook = new SpellBook();
            m_SpellBook.Initialize();
        }

        public void Update(WorldState worldState, float deltaTime)
        {
            Move(ClientIndex, m_MoveTarget);

            if (m_Random.NextFloat() > 0.95f
                && m_SpellBook.GetSpellCooldown() == 0f)
            {
                var targetCharacter = RandomCharacter(worldState);
                if (targetCharacter != this)
                {
                    var direction = targetCharacter.Position - Position;
                    direction.Normalize();

                    CastSpell(ClientIndex, SpellType.EnergyBolt, direction);
                    m_SpellBook.StartSpellCooldown();
                }
            }

            m_SpellBook.Update(deltaTime);
        }

        private ICharacter RandomCharacter(WorldState worldState)
        {
            int characterIndex = m_Random.Next(0, worldState.Characters.Count);
            return worldState.Characters[characterIndex];
        }
    }
}