using SharedCode.Core;
using SharedCode.Data;

namespace SharedCode.AI
{
    public delegate void BotUpdateCallback(float deltaTime);

    public struct MoveTarget
    {
        public int Weight;
        public Vector2 Target;
    }

    public class Bot
    {
        // TODO: Refactor
        public bool Active;
        public bool Reroute;

        private readonly CachedRandom m_Random;
        private readonly int m_ClientIndex;
        private readonly BotInput m_Input;
        private readonly SensoryData m_SensoryData;

        private MoveTarget[] m_MoveTargets;
        private int m_MoveTargetIndex;
        private float m_MoveCooldown;

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

            m_MoveTargets = new MoveTarget[36];
            m_MoveTargetIndex = 0;
            m_MoveCooldown = 0;
        }

        private void Update(float deltaTime)
        {
            if (!Active)
                return;

            var characters = m_SensoryData.ReadCharacters();
            if (characters[m_ClientIndex].Health <= 0)
                return;

            m_MoveCooldown -= deltaTime;
            if (m_MoveCooldown <= 0f)
            {
                FindPotentialMoveTargets(characters);
                m_MoveTargetIndex = RandomMoveTarget();
                m_MoveCooldown = m_Random.Next(1000, 1500);
            }

            m_Input.Move(m_ClientIndex, m_MoveTargets[m_MoveTargetIndex].Target);

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

        private void FindPotentialMoveTargets(ReadOnlySpan<CharacterDataEntry> characters)
        {
            const float MAX_DISTANCE_RADIUS = 100;
            for (int i = 0; i < m_MoveTargets.Length; i++)
            {
                float radian = i / (float)m_MoveTargets.Length * (float)Math.PI * 2;
                m_MoveTargets[i].Target.X = MAX_DISTANCE_RADIUS * (float)Math.Sin(radian)
                                            + characters[m_ClientIndex].Position.X;

                m_MoveTargets[i].Target.Y = MAX_DISTANCE_RADIUS * (float)Math.Cos(radian)
                                            + characters[m_ClientIndex].Position.Y;

                m_MoveTargets[i].Weight = 1;
            }

            // TODO: Filter danger zone
            // TODO: Order based other distances
        }

        private int RandomMoveTarget()
        {
            int totalWeight = 0;
            for (int i = 0; i < m_MoveTargets.Length; i++)
                totalWeight += m_MoveTargets[i].Weight;

            int selection = totalWeight > 0 ? m_Random.Next(0, totalWeight) : 0;
            for (int i = 0; i < m_MoveTargets.Length; i++)
            {
                selection -= m_MoveTargets[i].Weight;
                if (selection <= 0)
                    return i;
            }

            return 0;
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