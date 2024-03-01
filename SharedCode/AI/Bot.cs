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
        public bool Active;

        private readonly CachedRandom m_Random;
        private readonly int m_ClientIndex;
        private readonly BotInput m_Input;
        private readonly SensoryData m_SensoryData;

        private readonly MoveTarget[] m_MoveTargets;
        private int m_MoveTargetIndex;
        private float m_MoveCooldown;

        // TODO: Refactor this
        private float m_ShieldActiveTime;
        private float m_HasteActiveTime;

        private MoveTarget m_LastUpdateMoveTarget;

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

            m_ShieldActiveTime = 0;

            m_LastUpdateMoveTarget = new MoveTarget();
        }

        private void Update(float deltaTime)
        {
            if (!Active)
                return;

            var characters = m_SensoryData.ReadCharacters();
            if (characters[m_ClientIndex].Health <= 0)
                return;


            // Cast projectile spell
            if (m_Random.NextFloat() > 0.95f)
            {
                int targetIndex = FindPotentialSkillTarget(characters);
                if (targetIndex != -1)
                {
                    var direction = Direction(characters, m_ClientIndex, targetIndex);

                    // Skill might be on cooldown
                    m_Input.UseSkill(m_ClientIndex, SkillGroup.Projectile, direction);
                }
            }

            bool potentialParticleCollision = false;

            var projectiles = m_SensoryData.ReadProjectiles();
            for (int i = 0; i < projectiles.Length; i++)
            {
                if (projectiles[i].Owner == m_ClientIndex)
                    continue;

                const float MAX_DISTANCE = 200f;
                var endLocation = new Vector2()
                {
                    X = projectiles[i].Position.X + projectiles[i].Direction.X * MAX_DISTANCE,
                    Y = projectiles[i].Position.Y + projectiles[i].Direction.Y * MAX_DISTANCE,
                };

                // TODO: Need also character size
                potentialParticleCollision = Geometry.LineToCircleCollision(
                    projectiles[i].Position,
                    endLocation,
                    characters[m_ClientIndex].Position,
                    projectiles[i].AreaRadius);
            }

            m_ShieldActiveTime -= deltaTime;
            if (m_ShieldActiveTime <= 0)
                m_ShieldActiveTime = 0;

            m_HasteActiveTime -= deltaTime;
            if (m_HasteActiveTime <= 0)
                m_HasteActiveTime = 0;

            if (potentialParticleCollision)
            {
                if (m_Input.UseSkill(m_ClientIndex, SkillGroup.Block, Vector2.Zero))
                {
                    m_ShieldActiveTime = 2000f; // TODO: This need right active time
                }
                else if (m_ShieldActiveTime <= 200
                         && m_Input.UseSkill(m_ClientIndex, SkillGroup.Movement, Vector2.Zero))
                {
                    m_HasteActiveTime = 2000f; // TODO: This need right active time
                    m_MoveCooldown = 0;
                }
            }

            m_MoveCooldown -= deltaTime;
            if (m_MoveCooldown <= 0f)
            {
                FindPotentialMoveTargets(characters);
                m_MoveTargetIndex = RandomMoveTarget();

                if (m_HasteActiveTime > 0)
                    m_MoveCooldown = 250;
                else
                    m_MoveCooldown = m_Random.Next(500, 1000);
            }

            Vector2 moveTarget = m_MoveTargetIndex == -1
                ? m_SensoryData.GameAreaCenter
                : m_MoveTargets[m_MoveTargetIndex].Target;

            // Reroute if move is not allowed current tick
            if (!m_Input.Move(m_ClientIndex, moveTarget))
                m_MoveCooldown = 0;
        }

        private void FindPotentialMoveTargets(ReadOnlySpan<CharacterDataEntry> characters)
        {
            const float MAX_DISTANCE_RADIUS = 50;
            for (int i = 0; i < m_MoveTargets.Length; i++)
            {
                float radian = i / (float)m_MoveTargets.Length * (float)Math.PI * 2;
                m_MoveTargets[i].Target.X = MAX_DISTANCE_RADIUS * (float)Math.Sin(radian)
                                            + characters[m_ClientIndex].Position.X;

                m_MoveTargets[i].Target.Y = MAX_DISTANCE_RADIUS * (float)Math.Cos(radian)
                                            + characters[m_ClientIndex].Position.Y;

                if (!GameAreaContains(m_MoveTargets[i].Target))
                {
                    m_MoveTargets[i].Weight = 0;
                    continue;
                }

                float closestDistance = float.MaxValue;
                for (int j = 0; j < characters.Length; j++)
                {
                    if (j == m_ClientIndex)
                        continue;

                    float distance = Distance(characters, m_ClientIndex, j);
                    if (distance < closestDistance)
                        closestDistance = distance;
                }

                // TODO: Fine tune!
                m_MoveTargets[i].Weight = Math.Clamp((int)closestDistance, 1, 200);

                if (m_LastUpdateMoveTarget.Weight != 0)
                {
                    float distance = Geometry.Distance(m_LastUpdateMoveTarget.Target, m_MoveTargets[i].Target);
                    int multiplier = (int)(Math.Clamp(100 - distance, 0, 100) / 10);
                    m_MoveTargets[i].Weight *= multiplier;

                    // Console.WriteLine(multiplier);
                }
            }

            // TODO: Test
            Array.Sort(m_MoveTargets, (a, b) => b.Weight - a.Weight);
        }

        private int RandomMoveTarget()
        {
            int totalWeight = 0;
            for (int i = 0; i < m_MoveTargets.Length; i++)
                totalWeight += m_MoveTargets[i].Weight;

            if (totalWeight == 0)
            {
                m_LastUpdateMoveTarget.Weight = 0;
                return -1;
            }

            int selection = m_Random.Next(0, totalWeight);
            for (int i = 0; i < m_MoveTargets.Length; i++)
            {
                selection -= m_MoveTargets[i].Weight;
                if (selection <= 0)
                {
                    m_LastUpdateMoveTarget = m_MoveTargets[i];
                    return i;
                }
            }

            m_LastUpdateMoveTarget.Weight = 0;
            return -1;
        }

        private int FindPotentialSkillTarget(ReadOnlySpan<CharacterDataEntry> characters)
        {
            int closestIndex = -1;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < characters.Length; i++)
            {
                if (i == m_ClientIndex || characters[i].Health <= 0)
                    continue;

                float distance = Distance(characters, m_ClientIndex, i);
                if (distance < closestDistance)
                {
                    closestIndex = i;
                    closestDistance = distance;
                }
            }

            // TODO: Multiply based on health?

            return closestIndex;
        }

        // TODO: Generic helper
        private Vector2 Direction(ReadOnlySpan<CharacterDataEntry> characters, int from, int to)
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

        // TODO: Generic helper
        private float Distance(ReadOnlySpan<CharacterDataEntry> characters, int from, int to)
        {
            float distanceX = characters[to].Position.X - characters[from].Position.X;
            float distanceY = characters[to].Position.Y - characters[from].Position.Y;
            return MathF.Sqrt(distanceX * distanceX + distanceY * distanceY);
        }

        /*
        private float Distance(Vector2 a, Vector2 b)
        {
            float tempA = a.X - b.X;
            float tempB = a.Y - b.Y;
            return MathF.Sqrt(tempA * tempA + tempB * tempB);
        }
        */

        // TODO: Generic helper
        private bool GameAreaContains(Vector2 point)
        {
            const float OPTIMAL_ZONE = 0.8f;

            Vector2 circleCenter = m_SensoryData.GameAreaCenter;
            float circleRadius = m_SensoryData.GameAreaRadius * OPTIMAL_ZONE;

            // (x - center_x)² + (y - center_y)² < radius²
            return (point.X - circleCenter.X) * (point.X - circleCenter.X)
                   + (point.Y - circleCenter.Y) * (point.Y - circleCenter.Y)
                   < circleRadius * circleRadius;
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