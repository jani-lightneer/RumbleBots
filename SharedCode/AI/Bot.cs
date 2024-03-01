using SharedCode.Core;
using SharedCode.Data;

namespace SharedCode.AI
{
    public delegate void BotUpdateCallback(float deltaTime);

    public class Bot
    {
        public bool Active;
        public float RerouteCooldown;

        private readonly CachedRandom m_Random;
        private readonly int m_ClientIndex;
        private readonly BotInput m_Input;
        private readonly SensoryData m_SensoryData;

        private Vector2 m_MoveTarget;

        public Bot(
            CachedRandom random,
            int clientIndex,
            BotInput input,
            SensoryData sensoryData)
        {
            Active = false;

            m_Random = random;
            m_ClientIndex = clientIndex;
            m_Input = input;
            m_SensoryData = sensoryData;

            m_MoveTarget = new Vector2(m_Random.Next(100, 900), m_Random.Next(100, 900));
        }

        private void Update(float deltaTime)
        {
            if (!Active)
                return;

            var characters = m_SensoryData.ReadCharacters();
            if (characters[m_ClientIndex].Health <= 0)
                return;

            // TODO
            m_Input.Move(m_ClientIndex, m_MoveTarget);

            // TODO
            if (m_Random.NextFloat() > 0.95f)
            {
                int targetIndex = m_Random.Next(0, characters.Length);

                if (targetIndex != m_ClientIndex)
                {
                    var direction = NormalizeDirection(characters, m_ClientIndex, targetIndex);
                    bool success = m_Input.UseSkill(m_ClientIndex, SkillGroup.Projectile, direction);
                    // TODO: Result
                }
            }
        }

        private Vector2 NormalizeDirection(ReadOnlySpan<CharacterDataEntry> characters, int from, int to)
        {
            float x = characters[to].Position.X - characters[from].Position.X;
            float y = characters[to].Position.Y - characters[from].Position.Y;

            float normalize = 1f / MathF.Sqrt(x * x + y * y);

            return new Vector2
            {
                X = x * normalize,
                Y = y * normalize
            };
        }

        public static (Bot, BotUpdateCallback) Create(
            CachedRandom random,
            int clientIndex,
            BotInput input,
            SensoryData sensoryData)
        {
            var bot = new Bot(random, clientIndex, input, sensoryData);
            return (bot, bot.Update);
        }
    }
}