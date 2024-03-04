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
        // Temp variable
        public const float WORLD_SIZE = 1;

        public bool Active;

        private readonly CachedRandom m_Random;
        private readonly int m_ClientIndex;
        private readonly BotInput m_Input;
        private readonly SensoryData m_SensoryData;

        private readonly MoveTarget[] m_MoveTargets;
        private int m_MoveTargetIndex;
        private float m_MoveCooldown;

        private readonly SkillMonitor m_SkillMonitor;

        private MoveTarget m_LastUpdateMoveTarget;

        public Bot(
            CachedRandom random,
            int clientIndex,
            BotInput input,
            SkillConfigGroup skillConfigGroup,
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

            m_SkillMonitor = new SkillMonitor(skillConfigGroup);

            m_LastUpdateMoveTarget = new MoveTarget();
        }

        private void Update(float deltaTime)
        {
            if (!Active)
                return;

            var characters = m_SensoryData.ReadCharacters();
            if (characters[m_ClientIndex].Health <= 0)
                return;

            m_SkillMonitor.Update(deltaTime);

            if (CheckProjectileTrajectories(characters, out float closestDistance))
            {
                bool shieldCooldown = true;

                // TODO: Fine tune based world size
                if (closestDistance <= WORLD_SIZE * 100f
                    && m_Input.UseSkill(m_ClientIndex, Skill.Shield, Vector2.Zero))
                {
                    m_SkillMonitor.Start(Skill.Shield);
                    shieldCooldown = false;
                }

                // TODO: Fine tune based world size
                if (shieldCooldown
                    && closestDistance <= WORLD_SIZE * 200f
                    && m_SkillMonitor.GetActiveTime(Skill.Shield) <= 200
                    && m_Input.UseSkill(m_ClientIndex, Skill.Dash, Vector2.Zero))
                {
                    m_SkillMonitor.Start(Skill.Dash);
                    m_MoveCooldown = 0;
                }
            }

            m_MoveCooldown -= deltaTime;
            if (m_MoveCooldown <= 0f)
            {
                FindPotentialMoveTargets(characters);
                m_MoveTargetIndex = RandomMoveTarget();

                if (m_SkillMonitor.GetActiveTime(Skill.Dash) > 0)
                {
                    // TODO: Fine tune based dash speed buff
                    m_MoveCooldown = 250;
                }
                else
                {
                    // TODO: Fine tune based movement speed
                    m_MoveCooldown = m_Random.Next(500, 800);
                }
            }

            Vector2 moveTarget = m_MoveTargetIndex == -1
                ? m_SensoryData.GameAreaCenter
                : m_MoveTargets[m_MoveTargetIndex].Target;

            if (!m_Input.Move(m_ClientIndex, moveTarget))
                m_MoveCooldown = 0;

            // TODO: Improve
            if (m_Random.NextFloat() > 0.95f)
            {
                int targetIndex = FindPotentialSkillTarget(characters);
                if (targetIndex != -1)
                {
                    var direction = Direction(characters, m_ClientIndex, targetIndex);
                    m_Input.UseSkill(m_ClientIndex, Skill.Projectile, direction);
                }
            }
        }

        private bool CheckProjectileTrajectories(
            ReadOnlySpan<CharacterDataEntry> characters,
            out float closestDistance)
        {
            bool potentialProjectileCollision = false;
            closestDistance = float.MaxValue;

            var projectiles = m_SensoryData.ReadProjectiles();
            for (int i = 0; i < projectiles.Length; i++)
            {
                if (projectiles[i].Owner == m_ClientIndex)
                    continue;

                const float MAX_DISTANCE = WORLD_SIZE * 200f;
                var endLocation = new Vector2()
                {
                    X = projectiles[i].Position.X + projectiles[i].Direction.X * MAX_DISTANCE,
                    Y = projectiles[i].Position.Y + projectiles[i].Direction.Y * MAX_DISTANCE,
                };

                // TODO: Need also character size
                potentialProjectileCollision = Geometry.LineToCircleCollision(
                    projectiles[i].Position,
                    endLocation,
                    characters[m_ClientIndex].Position,
                    projectiles[i].AreaRadius);

                if (potentialProjectileCollision)
                {
                    float distance = Geometry.Distance(
                        characters[m_ClientIndex].Position,
                        projectiles[i].Position);

                    closestDistance = Math.Min(distance, closestDistance);
                }
            }

            return potentialProjectileCollision;
        }

        private void FindPotentialMoveTargets(ReadOnlySpan<CharacterDataEntry> characters)
        {
            const float MAX_MOVE_TARGET_DISTANCE = WORLD_SIZE * 50f;

            for (int i = 0; i < m_MoveTargets.Length; i++)
            {
                float radian = i / (float)m_MoveTargets.Length * (float)Math.PI * 2;
                m_MoveTargets[i].Target.X = MAX_MOVE_TARGET_DISTANCE * (float)Math.Sin(radian)
                                            + characters[m_ClientIndex].Position.X;

                m_MoveTargets[i].Target.Y = MAX_MOVE_TARGET_DISTANCE * (float)Math.Cos(radian)
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
                }
            }

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

        // TODO: Generic helper
        private bool GameAreaContains(Vector2 point)
        {
            // TODO: Increment this when GameAreaRadius is smaller
            const float OPTIMAL_GAME_AREA_ZONE = 0.8f;

            Vector2 circleCenter = m_SensoryData.GameAreaCenter;
            float circleRadius = m_SensoryData.GameAreaRadius * OPTIMAL_GAME_AREA_ZONE;

            // (x - center_x)² + (y - center_y)² < radius²
            return (point.X - circleCenter.X) * (point.X - circleCenter.X)
                   + (point.Y - circleCenter.Y) * (point.Y - circleCenter.Y)
                   < circleRadius * circleRadius;
        }

        public static (Bot, BotUpdateCallback) Create(
            CachedRandom random,
            int clientIndex,
            BotInput input,
            SkillConfigGroup skillConfigGroup,
            SensoryData sensoryData)
        {
            var bot = new Bot(random, clientIndex, input, skillConfigGroup, sensoryData);
            return (bot, bot.Update);
        }
    }
}