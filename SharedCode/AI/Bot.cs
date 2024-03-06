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

    // TODO: Missing defence triggers for homing projectiles
    // TODO: Missing perfect defence triggers for rapid shot
    // TODO: Missing defence triggers for none projectile blasts

    public class Bot
    {
        public bool Active;

        private readonly CachedRandom m_Random;
        private readonly int m_ClientIndex;
        private readonly BotInput m_Input;
        private readonly float m_ActionDistance;
        private readonly SensoryData m_SensoryData;

        private readonly ActionTarget[] m_MoveTargets;
        private readonly ActionTarget[] m_SkillTargets;
        private int m_MoveTargetIndex;
        private float m_MoveCooldown;

        public SkillLayout SkillLayout;
        private readonly SkillManager m_SkillManager;
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

            m_MoveTargets = new ActionTarget[36];
            m_SkillTargets = new ActionTarget[aggressionPool.MaxCharacterCount];
            m_MoveTargetIndex = 0;
            m_MoveCooldown = 0;

            SkillLayout = null;
            m_SkillManager = new SkillManager(skillConfigGroup);
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

            m_SkillManager.Update(deltaTime);

            if (CheckProjectileTrajectories(m_ActionDistance * 4, characters, out float closestDistance))
            {
                if (!UseDefenceSkill(characters, closestDistance)
                    && m_SkillManager.GetActiveTime(Skill.CounterShield) <= 200)
                {
                    UseReactiveUtilitySkill(closestDistance);
                }
            }

            m_MoveCooldown -= deltaTime;

            for (int i = 0; i < 4; i++)
            {
                float actionDistance = m_ActionDistance - (m_ActionDistance * i * 0.2f);
                if (FindPotentialMoveTargets(characters, actionDistance, m_MoveCooldown <= 0f))
                    break;
            }

            if (m_MoveCooldown <= 0f)
                m_MoveCooldown = m_Random.Next(1000, 1500);

            m_MoveTargetIndex = RandomMoveTarget();
            if (m_MoveTargetIndex == -1)
            {
                m_MoveTargets[0].Target = m_SensoryData.GameAreaCenter;
                m_MoveTargetIndex = 0;
            }

            if (!m_Input.Move(m_ClientIndex, m_MoveTargets[m_MoveTargetIndex].Target))
                m_MoveCooldown = 0;

            if (m_Random.NextFloat() > 0.95f && FindPotentialSkillTargets(characters))
            {
                int targetIndex = RandomSkillTarget();
                if (targetIndex != -1)
                {
                    float projectileMaxRange = m_SkillManager.GetRange(SkillLayout.ProjectileSkill);
                    float targetDistance = Distance(characters, m_ClientIndex, targetIndex);

                    var target = characters[targetIndex].Position;
                    float range = targetDistance / projectileMaxRange;
                    float spread = Math.Clamp((range - 0.5f) * 2f, 0, 1f);

                    float randomX = 1f - m_Random.NextFloat() * 2f;
                    float randomY = 1f - m_Random.NextFloat() * 2f;

                    target.X += randomX * spread * projectileMaxRange * 0.03f;
                    target.Y += randomY * spread * projectileMaxRange * 0.03f;

                    var direction = Geometry.Direction(
                        characters[m_ClientIndex].Position,
                        target);

                    if (m_Input.UseSkill(m_ClientIndex, SkillLayout.ProjectileSkill, direction))
                        m_AggressionPool.AddAggression(m_ClientIndex, targetIndex);
                }
            }

            if (SkillLayout.UtilitySkill == Skill.Stomp)
            {
                float stompRange = m_SkillManager.GetRange(Skill.Stomp);
                for (int i = 0; i < characters.Length; i++)
                {
                    if (i == m_ClientIndex)
                        continue;

                    float distance = Distance(characters, m_ClientIndex, i);
                    if (distance <= stompRange)
                        m_Input.UseSkill(m_ClientIndex, Skill.Stomp, Vector2.Zero);
                }
            }
        }

        private bool UseDefenceSkill(ReadOnlySpan<CharacterDataEntry> characters, float closestDistance)
        {
            switch (SkillLayout.DefenceSkill)
            {
                case Skill.CounterShield:
                    if (closestDistance >= m_ActionDistance * 3)
                        return false;

                    if (m_Input.UseSkill(m_ClientIndex, Skill.CounterShield, Vector2.Zero))
                    {
                        m_SkillManager.ActiveSkill(Skill.CounterShield);
                        return true;
                    }

                    break;
                // TODO: Refactor
                case Skill.Teleport:
                    if (closestDistance >= m_ActionDistance * 3)
                        return false;

                    Vector2 teleportTarget = Vector2.Zero;
                    bool foundTeleportTarget = false;

                    for (int i = 0; i < m_AggressionPool.HuntTargets.Length; i++)
                    {
                        if (FindTeleportTarget(
                                m_AggressionPool.HuntTargets[i].ClientIndex,
                                characters,
                                out teleportTarget))
                        {
                            foundTeleportTarget = true;
                            break;
                        }
                    }

                    if (foundTeleportTarget && m_Input.UseSkill(m_ClientIndex, Skill.Teleport, teleportTarget))
                    {
                        m_SkillManager.ActiveSkill(Skill.Teleport);
                        return true;
                    }

                    break;
            }

            return false;
        }

        private bool FindTeleportTarget(
            int targetIndex,
            ReadOnlySpan<CharacterDataEntry> characters,
            out Vector2 teleportTarget)
        {
            if (targetIndex == m_ClientIndex || characters[targetIndex].Health <= 0)
            {
                teleportTarget = Vector2.Zero;
                return false;
            }

            const int TARGET_COUNT = 10;
            float maxDistance = m_SkillManager.GetRange(Skill.Teleport);
            float bestTeleportRange = m_ActionDistance * 2f;

            if (SkillLayout.UtilitySkill == Skill.Stomp)
                bestTeleportRange = m_ActionDistance * 0.5f;

            for (int i = 0; i < TARGET_COUNT; i++)
            {
                // Not best way to solve this ...
                int randomIndex = m_Random.Next(0, TARGET_COUNT);

                float radian = i / (float)randomIndex * (float)Math.PI * 2;
                teleportTarget = new Vector2();

                teleportTarget.X = bestTeleportRange * (float)Math.Sin(radian)
                                   + characters[targetIndex].Position.X;

                teleportTarget.Y = bestTeleportRange * (float)Math.Cos(radian)
                                   + characters[targetIndex].Position.Y;

                float distance = Geometry.Distance(characters[m_ClientIndex].Position, teleportTarget);
                if (distance > maxDistance)
                    return false;

                if (GameAreaContains(teleportTarget))
                    return true;
            }

            teleportTarget = Vector2.Zero;
            return false;
        }

        private void UseReactiveUtilitySkill(float closestDistance)
        {
            switch (SkillLayout.UtilitySkill)
            {
                case Skill.Dash:
                    if (closestDistance <= m_ActionDistance * 4
                        && m_Input.UseSkill(m_ClientIndex, Skill.Dash, Vector2.Zero))
                    {
                        m_SkillManager.ActiveSkill(Skill.Dash);
                        m_MoveCooldown = 0;
                    }

                    break;
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
            float projectileMaxRange = m_SkillManager.GetRange(SkillLayout.ProjectileSkill);

            bool foundMoveTarget = false;
            m_DistanceStack.Reset(actionDistance * 1000);

            int huntTarget = -1;
            float maxHuntDistance = projectileMaxRange * 1.8f;

            if (allowTurning)
            {
                huntTarget = m_AggressionPool.FindHuntTarget(m_ClientIndex);
                if (huntTarget != -1)
                {
                    if (Distance(characters, m_ClientIndex, huntTarget) > maxHuntDistance)
                        huntTarget = -1;
                }
            }

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

                int moveWeight;
                if (huntTarget != -1)
                {
                    float distance = Distance(characters, m_ClientIndex, huntTarget);
                    float skillRange = Math.Abs(distance - maxHuntDistance);

                    moveWeight = (int)((1f - skillRange) * 200);
                }
                else
                {
                    float bestSkillRange = 1f;
                    for (int j = 0; j < characters.Length; j++)
                    {
                        if (j == m_ClientIndex)
                            continue;

                        float distance = Distance(characters, m_ClientIndex, j);
                        float skillRange = Math.Abs(distance - projectileMaxRange);
                        if (skillRange < bestSkillRange)
                            bestSkillRange = skillRange;
                    }

                    moveWeight = (int)((1f - bestSkillRange) * 200);
                }

                m_MoveTargets[i].Weight = Math.Clamp(moveWeight, 1, 200);

                // TODO: Fine tune
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
            float projectileMaxRange = m_SkillManager.GetRange(SkillLayout.ProjectileSkill);

            for (int i = 0; i < characters.Length; i++)
            {
                if (i == m_ClientIndex || characters[i].Health <= 0)
                {
                    m_SkillTargets[i].Weight = 0;
                    continue;
                }

                float distance = Distance(characters, m_ClientIndex, i);
                if (distance >= projectileMaxRange)
                {
                    m_SkillTargets[i].Weight = 0;
                    continue;
                }

                foundSkillTarget = true;

                float invertedDistance = Math.Clamp(projectileMaxRange - distance, 1, projectileMaxRange);
                float receivedAggression = m_AggressionPool.GetCharacterReceivedAggression(i);
                float invertedReceivedAggression = Math.Clamp(1f - receivedAggression, 0.70f, 1f);

                float weight = invertedDistance * 10;
                weight *= invertedReceivedAggression * invertedReceivedAggression * invertedReceivedAggression;

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