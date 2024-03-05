using System;
using SharedCode.Core;
using SharedCode.Data;

namespace SharedCode.AI
{
    public delegate void BotUpdateCallback(float deltaTime);

    public struct ActionTarget
    {
        public int Weight;
        public Vector2 Target;
    }

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
        private readonly ActionTarget[] m_MoveTargets;
        private readonly ActionTarget[] m_SkillTargets;
        private int m_MoveTargetIndex;
        private float m_MoveCooldown;

        // TODO: Refactor
        private readonly float m_ProjectileMaxRange;
        private readonly SkillMonitor m_SkillMonitor;
        private readonly AggressionPool m_AggressionPool;

        private readonly DistanceStack m_DistanceStack;
        private ActionTarget m_LastUpdateMoveTarget;

        public Bot(
            CachedRandom random,
            int clientIndex,
            BotInput input,
            AggressionPool aggressionPool,
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
            m_MoveTargets = new ActionTarget[36];
            m_SkillTargets = new ActionTarget[aggressionPool.MaxCharacterCount];
            m_MoveTargetIndex = 0;
            m_MoveCooldown = 0;

            m_ProjectileMaxRange = skillConfigGroup.Skills[(int)Skill.Projectile].Range;
            m_SkillMonitor = new SkillMonitor(skillConfigGroup);
            m_AggressionPool = aggressionPool;

            m_DistanceStack = new DistanceStack(3);
            m_LastUpdateMoveTarget = new ActionTarget();
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

            for (int i = 0; i < 4; i++)
            {
                float actionDistance = m_ActionDistance - (m_ActionDistance * i * 0.25f);
                if (FindPotentialMoveTargets(characters, actionDistance, m_MoveCooldown <= 0f))
                    break;
            }

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
                // m_MoveTargets[0].Target.X += (1f - m_Random.NextFloat() * 2f) * m_SensoryData.GameAreaRadius * 0.4f;
                // m_MoveTargets[0].Target.Y += (1f - m_Random.NextFloat() * 2f) * m_SensoryData.GameAreaRadius * 0.4f;
                m_MoveTargetIndex = 0;
            }

            if (!m_Input.Move(m_ClientIndex, m_MoveTargets[m_MoveTargetIndex].Target))
                m_MoveCooldown = 0;

            m_ProjectileDelay -= deltaTime;
            if (m_ProjectileDelay <= 0f && FindPotentialSkillTargets(characters))
            {
                int targetIndex = RandomSkillTarget();
                if (targetIndex != -1)
                {
                    float targetDistance = Distance(characters, m_ClientIndex, targetIndex);

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

                    if (m_Input.UseSkill(m_ClientIndex, Skill.Projectile, direction))
                        m_AggressionPool.AddAggression(m_ClientIndex, targetIndex);
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

        private bool FindPotentialMoveTargets(
            ReadOnlySpan<CharacterDataEntry> characters,
            float actionDistance,
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

            bool foundMoveTarget = false;
            m_DistanceStack.Reset(actionDistance * 1000);

            int huntTarget = (allowTurning) ? m_AggressionPool.FindHuntTarget(m_ClientIndex) : -1;

            for (int i = 0; i < m_MoveTargets.Length; i++)
            {
                float radian = i / (float)m_MoveTargets.Length * (float)Math.PI * 2;
                m_MoveTargets[i].Target.X = actionDistance * (float)Math.Sin(radian)
                                            + characters[m_ClientIndex].Position.X;

                m_MoveTargets[i].Target.Y = actionDistance * (float)Math.Cos(radian)
                                            + characters[m_ClientIndex].Position.Y;

                if (!GameAreaContains(m_MoveTargets[i].Target))
                {
                    m_MoveTargets[i].Weight = 0;
                    continue;
                }

                foundMoveTarget = true;

                int moveWeight = 0;
                if (huntTarget != -1)
                {
                    // Console.WriteLine("HUNT!");
                    float distance = Distance(characters, m_ClientIndex, huntTarget);
                    moveWeight = (int)((1f - distance) * 200);
                }
                else
                {
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

                    moveWeight = (int)((1f - bestSkillRange) * 200);
                }

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
                }
            }

            if (huntTarget != -1)
                m_AggressionPool.TagHuntTarget(huntTarget);

            if (m_DistanceStack.Used)
            {
                for (int i = 0; i < m_MoveTargets.Length; i++)
                {
                    if (m_DistanceStack.Contain(i))
                        continue;

                    m_MoveTargets[i].Weight = 0;
                }
            }

            Array.Sort(m_MoveTargets, (a, b) => b.Weight - a.Weight);
            return foundMoveTarget;
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

        private bool FindPotentialSkillTargets(ReadOnlySpan<CharacterDataEntry> characters)
        {
            bool foundSkillTarget = false;

            for (int i = 0; i < characters.Length; i++)
            {
                if (i == m_ClientIndex || characters[i].Health <= 0)
                {
                    m_SkillTargets[i].Weight = 0;
                    continue;
                }


                float distance = Distance(characters, m_ClientIndex, i);
                if (distance >= m_ProjectileMaxRange)
                {
                    m_SkillTargets[i].Weight = 0;
                    continue;
                }

                foundSkillTarget = true;

                float invertedDistance = Math.Clamp(m_ProjectileMaxRange - distance, 1, m_ProjectileMaxRange);
                float receivedAggression = m_AggressionPool.GetCharacterReceivedAggression(i);
                float invertedReceivedAggression = Math.Clamp(1f - receivedAggression, 0.01f, 1f);
                float weight = invertedDistance * 10 * invertedReceivedAggression * invertedReceivedAggression;

                m_SkillTargets[i].Weight = (int)weight;
                if (m_SkillTargets[i].Weight < 0)
                    m_SkillTargets[i].Weight = 1;
            }

            return foundSkillTarget;
        }

        // TODO: Combine with random target (move + skill)
        private int RandomSkillTarget()
        {
            int totalWeight = 0;
            for (int i = 0; i < m_SkillTargets.Length; i++)
                totalWeight += m_SkillTargets[i].Weight;

            if (totalWeight == 0)
                return -1;

            int selection = m_Random.Next(0, totalWeight);
            for (int i = 0; i < m_SkillTargets.Length; i++)
            {
                selection -= m_SkillTargets[i].Weight;
                if (selection <= 0)
                    return i;
            }

            return -1;
        }

        /*
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
                if (distance <= m_ProjectileMaxRange)
                    continue;

                if (distance < targetDistance)
                {
                    closestIndex = i;
                    targetDistance = distance;
                }
            }

            // TODO: Multiply based on health?

            return closestIndex;
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
            AggressionPool aggressionPool,
            float actionDistance,
            SkillConfigGroup skillConfigGroup,
            SensoryData sensoryData)
        {
            var bot = new Bot(
                random,
                clientIndex,
                input,
                aggressionPool,
                actionDistance,
                skillConfigGroup,
                sensoryData);

            return (bot, bot.Update);
        }
    }
}