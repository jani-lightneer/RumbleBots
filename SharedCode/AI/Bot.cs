using System;
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

    // TODO: Improve casting cone
    // TODO: Improve casting random time
    // TODO: Improve movement sin & cos

    public class Bot
    {
        private const float MAX_PROJECTILE_DELAY = 400f;

        public bool Active;

        private readonly CachedRandom m_Random;
        private readonly int m_ClientIndex;
        private readonly BotInput m_Input;
        private readonly float m_ActionDistance;
        private readonly SensoryData m_SensoryData;

        private float m_ProjectileDelay;
        private readonly MoveTarget[] m_MoveTargets;
        private int m_MoveTargetIndex;
        private float m_MoveCooldown;

        // TODO: Refactor
        private readonly float m_ProjectileMaxRange;
        private readonly SkillMonitor m_SkillMonitor;

        private readonly DistanceStack m_DistanceStack;
        private MoveTarget m_LastUpdateMoveTarget;

        public Bot(
            CachedRandom random,
            int clientIndex,
            BotInput input,
            float actionDistance,
            SkillConfigGroup skillConfigGroup,
            SensoryData sensoryData)
        {
            Active = false;

            m_Random = random;
            m_ClientIndex = clientIndex;
            m_Input = input;
            m_ActionDistance = actionDistance;
            m_SensoryData = sensoryData;

            m_ProjectileDelay = m_Random.NextFloat() * MAX_PROJECTILE_DELAY;
            m_MoveTargets = new MoveTarget[36];
            m_MoveTargetIndex = 0;
            m_MoveCooldown = 0;

            m_ProjectileMaxRange = skillConfigGroup.Skills[(int)Skill.Projectile].Range;
            m_SkillMonitor = new SkillMonitor(skillConfigGroup);

            m_DistanceStack = new DistanceStack(3);
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

            if (CheckProjectileTrajectories(m_ActionDistance * 4, characters, out float closestDistance))
            {
                // Can cause Dash & Shield usage same time
                bool shieldCooldown = true;

                // TODO: Fine tune based world size
                if (closestDistance <= m_ActionDistance * 3
                    && m_Input.UseSkill(m_ClientIndex, Skill.Shield, Vector2.Zero))
                {
                    m_SkillMonitor.Start(Skill.Shield);
                    shieldCooldown = false;
                }

                // TODO: Fine tune based world size
                if (shieldCooldown
                    && closestDistance <= m_ActionDistance * 4
                    && m_SkillMonitor.GetActiveTime(Skill.Shield) <= 200
                    && m_Input.UseSkill(m_ClientIndex, Skill.Dash, Vector2.Zero))
                {
                    m_SkillMonitor.Start(Skill.Dash);
                    m_MoveCooldown = 0;
                }
            }

            m_MoveCooldown -= deltaTime;

            FindPotentialMoveTargets(characters, m_MoveCooldown <= 0f);
            if (m_MoveCooldown <= 0f)
            {
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

            m_MoveTargetIndex = RandomMoveTarget();
            if (m_MoveTargetIndex == -1)
            {
                m_MoveTargets[0].Target = m_SensoryData.GameAreaCenter;
                m_MoveTargets[0].Target.X += (1f - m_Random.NextFloat() * 2f) * m_SensoryData.GameAreaRadius * 0.4f;
                m_MoveTargets[0].Target.Y += (1f - m_Random.NextFloat() * 2f) * m_SensoryData.GameAreaRadius * 0.4f;
                m_MoveTargetIndex = 0;
            }

            if (!m_Input.Move(m_ClientIndex, m_MoveTargets[m_MoveTargetIndex].Target))
                m_MoveCooldown = 0;

            m_ProjectileDelay -= deltaTime;
            if (m_ProjectileDelay <= 0f)
            {
                int targetIndex = FindPotentialSkillTarget(characters, out float targetDistance);
                if (targetIndex != -1)
                {
                    var target = characters[targetIndex].Position;
                    float range = targetDistance / m_ProjectileMaxRange;
                    float spread = Math.Clamp((range - 0.5f) * 2f, 0, 1f);

                    float randomX = 1f - m_Random.NextFloat() * 2f;
                    float randomY = 1f - m_Random.NextFloat() * 2f;

                    target.X += randomX * spread * m_ProjectileMaxRange * 0.04f;
                    target.Y += randomY * spread * m_ProjectileMaxRange * 0.04f;

                    var direction = Geometry.Direction(
                        characters[m_ClientIndex].Position,
                        target);

                    m_Input.UseSkill(m_ClientIndex, Skill.Projectile, direction);
                }

                m_ProjectileDelay = m_Random.NextFloat() * MAX_PROJECTILE_DELAY;
            }
        }

        private bool CheckProjectileTrajectories(
            float maxDistance,
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

                float distance = Geometry.Distance(
                    characters[m_ClientIndex].Position,
                    projectiles[i].Position);

                if (distance > projectiles[i].MaxRange)
                    continue;

                var endLocation = new Vector2()
                {
                    X = projectiles[i].Position.X + projectiles[i].Direction.X * maxDistance,
                    Y = projectiles[i].Position.Y + projectiles[i].Direction.Y * maxDistance,
                };

                // TODO: Need also character size
                potentialProjectileCollision = Geometry.LineToCircleCollision(
                    projectiles[i].Position,
                    endLocation,
                    characters[m_ClientIndex].Position,
                    projectiles[i].AreaRadius);

                if (potentialProjectileCollision)
                    closestDistance = Math.Min(distance, closestDistance);
            }

            return potentialProjectileCollision;
        }

        private void FindPotentialMoveTargets(
            ReadOnlySpan<CharacterDataEntry> characters,
            bool allowTurning)
        {
            // const int FORCE_TARGET_COUNT = 3;

            // DistanceStack

            /*
            int[] bestForceMoveTargets = new int[FORCE_TARGET_COUNT];
            float[] bestForceMoveTargetDistances = new float[FORCE_TARGET_COUNT];
            for (int i = 0; i < FORCE_TARGET_COUNT; i++)
            {
                bestForceMoveTargets[i] = -1;
                bestForceMoveTargetDistances[i] = float.MaxValue;
            }
            */

            m_DistanceStack.Reset(m_ActionDistance * 1000);

            for (int i = 0; i < m_MoveTargets.Length; i++)
            {
                float radian = i / (float)m_MoveTargets.Length * (float)Math.PI * 2;
                m_MoveTargets[i].Target.X = m_ActionDistance * (float)Math.Sin(radian)
                                            + characters[m_ClientIndex].Position.X;

                m_MoveTargets[i].Target.Y = m_ActionDistance * (float)Math.Cos(radian)
                                            + characters[m_ClientIndex].Position.Y;

                if (!GameAreaContains(m_MoveTargets[i].Target))
                {
                    m_MoveTargets[i].Weight = 0;
                    continue;
                }

                float bestSkillRange = 1f;
                for (int j = 0; j < characters.Length; j++)
                {
                    if (j == m_ClientIndex)
                        continue;

                    float distance = Distance(characters, m_ClientIndex, j);
                    float skillRange = Math.Abs(distance - m_ProjectileMaxRange);
                    if (skillRange < bestSkillRange)
                        bestSkillRange = skillRange;
                }

                // TODO: Fine tune
                int moveWeight = (int)((1f - bestSkillRange) * 200);
                m_MoveTargets[i].Weight = Math.Clamp(moveWeight, 1, 200);

                // Decrease probability for U-turn
                if (m_LastUpdateMoveTarget.Weight != 0)
                {
                    float distance = Geometry.Distance(m_LastUpdateMoveTarget.Target, m_MoveTargets[i].Target);
                    int invertedDistance = 100 - (int)Math.Clamp(distance, 0, 100);
                    m_MoveTargets[i].Weight *= invertedDistance / 10;
                }

                if (m_LastUpdateMoveTarget.Weight != 0 && !allowTurning)
                {
                    float distance = Geometry.Distance(m_LastUpdateMoveTarget.Target, m_MoveTargets[i].Target);
                    m_DistanceStack.TryAdd(i, distance);

                    /*
                    if (distance < bestForceMoveTargetDistance)
                    {
                        bestForceMoveTarget = i;
                        bestForceMoveTargetDistance = distance;
                    }
                    */
                }
            }

            if (m_DistanceStack.Used)
            {
                for (int i = 0; i < m_MoveTargets.Length; i++)
                {
                    if (m_DistanceStack.Contain(i))
                        continue;

                    m_MoveTargets[i].Weight = 0;
                }

                /*
                for (int i = 0; i < m_DistanceStack.Items.Length; i++)
                {
                    int index = m_DistanceStack.Items[i].Index;
                    if (index == -1)
                        continue;

                    m_MoveTargets[index].Weight = 0;
                }
                */
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

        private int FindPotentialSkillTarget(
            ReadOnlySpan<CharacterDataEntry> characters,
            out float targetDistance)
        {
            int closestIndex = -1;
            targetDistance = float.MaxValue;

            for (int i = 0; i < characters.Length; i++)
            {
                if (i == m_ClientIndex || characters[i].Health <= 0)
                    continue;

                float distance = Distance(characters, m_ClientIndex, i);
                if (distance <= m_ProjectileMaxRange && distance < targetDistance)
                {
                    closestIndex = i;
                    targetDistance = distance;
                }
            }

            // TODO: Multiply based on health?

            return closestIndex;
        }

        /*
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
        */

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
            const float OPTIMAL_GAME_AREA_ZONE = 0.9f;

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
            float actionDistance,
            SkillConfigGroup skillConfigGroup,
            SensoryData sensoryData)
        {
            var bot = new Bot(
                random,
                clientIndex,
                input,
                actionDistance,
                skillConfigGroup,
                sensoryData);

            return (bot, bot.Update);
        }
    }
}