using System;

namespace SharedCode.AI
{
    public class CharacterTarget
    {
        public readonly int ClientIndex;
        public float AggressionReceivedPoints;
        public float AggressionReceived;
        public float Tagged;

        public CharacterTarget(int clientIndex)
        {
            ClientIndex = clientIndex;
            AggressionReceivedPoints = 0;
            AggressionReceived = 0;
            Tagged = 0;
        }

        public static int Comparison(CharacterTarget a, CharacterTarget b)
        {
            return (int)a.AggressionReceivedPoints - (int)b.AggressionReceivedPoints;
        }
    }

    // TODO: Refactor
    public class AggressionPool
    {
        private const bool PRINT = false;

        private const float TAG_DURATION = 500f;

        public readonly int MaxCharacterCount;
        public readonly CharacterTarget[] HuntTargets;
        private readonly CharacterTarget[] m_CharacterTargets;
        private float m_PrintDelay = 0;

        public AggressionPool(int maxCharacterCount)
        {
            MaxCharacterCount = maxCharacterCount;
            HuntTargets = new CharacterTarget[maxCharacterCount];
            m_CharacterTargets = new CharacterTarget[maxCharacterCount];

            for (int i = 0; i < m_CharacterTargets.Length; i++)
            {
                var characterTarget = new CharacterTarget(i);
                m_CharacterTargets[i] = characterTarget;
                HuntTargets[i] = characterTarget;
            }

            m_PrintDelay = 0;
        }

        public void Reset()
        {
            for (int i = 0; i < m_CharacterTargets.Length; i++)
            {
                m_CharacterTargets[i].AggressionReceivedPoints = 0;
                m_CharacterTargets[i].AggressionReceived = 0;
                m_CharacterTargets[i].Tagged = 0;
            }
        }

        public void Update(float deltaTime)
        {
            float totalAggressionReceivedPoints = 0;
            for (int i = 0; i < m_CharacterTargets.Length; i++)
            {
                m_CharacterTargets[i].Tagged -= deltaTime;
                if (m_CharacterTargets[i].Tagged <= 0)
                    m_CharacterTargets[i].Tagged = 0;

                if (m_CharacterTargets[i].AggressionReceivedPoints > deltaTime)
                    m_CharacterTargets[i].AggressionReceivedPoints -= deltaTime;

                totalAggressionReceivedPoints += m_CharacterTargets[i].AggressionReceivedPoints;
            }

            for (int i = 0; i < m_CharacterTargets.Length; i++)
            {
                if (m_CharacterTargets[i].AggressionReceivedPoints > 0)
                {
                    m_CharacterTargets[i].AggressionReceived =
                        m_CharacterTargets[i].AggressionReceivedPoints / totalAggressionReceivedPoints;
                }
            }

            Array.Sort(HuntTargets, CharacterTarget.Comparison);

            if (PRINT)
            {
                m_PrintDelay -= deltaTime;
                if (m_PrintDelay <= 0)
                {
                    Console.WriteLine($"=== AGRO ===");
                    for (int i = 0; i < HuntTargets.Length; i++)
                    {
                        Console.WriteLine(
                            $"Character[{i}]: {HuntTargets[i].AggressionReceived}, Tagged: {HuntTargets[i].Tagged}");
                    }

                    m_PrintDelay = 1000f;
                }
            }
        }

        public void AddAggression(int from, int to)
        {
            // This need to be fine tuned based on target skill cooldown
            const float aggressionPointSize = 10 * 1000f;
            m_CharacterTargets[to].AggressionReceivedPoints += aggressionPointSize;
        }

        public float GetCharacterReceivedAggression(int index)
        {
            return m_CharacterTargets[index].AggressionReceived;
        }

        public int FindHuntTarget(int clientIndex)
        {
            float lowPriorityHunt = (1f / MaxCharacterCount) * 0.8f;
            float highPriorityHunt = (1f / MaxCharacterCount) * 0.4f;

            for (int i = 0; i < HuntTargets.Length; i++)
            {
                if (clientIndex == HuntTargets[i].ClientIndex)
                    continue;

                if (HuntTargets[i].AggressionReceived <= highPriorityHunt)
                {
                    // Allow double hunt
                    if (HuntTargets[i].Tagged > TAG_DURATION)
                        continue;

                    return HuntTargets[i].ClientIndex;
                }

                if (HuntTargets[i].AggressionReceived <= lowPriorityHunt)
                {
                    if (HuntTargets[i].Tagged > 0)
                        continue;

                    return HuntTargets[i].ClientIndex;
                }
            }

            return -1;
        }

        public void TagHuntTarget(int targetIndex)
        {
            m_CharacterTargets[targetIndex].Tagged += TAG_DURATION;
        }
    }
}