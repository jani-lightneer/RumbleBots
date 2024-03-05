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
        public readonly int MaxCharacterCount;

        private readonly CharacterTarget[] m_CharacterTargets;
        private readonly CharacterTarget[] m_HuntTargets;

        public AggressionPool(int maxCharacterCount)
        {
            MaxCharacterCount = maxCharacterCount;

            m_CharacterTargets = new CharacterTarget[maxCharacterCount];
            m_HuntTargets = new CharacterTarget[maxCharacterCount];

            for (int i = 0; i < m_CharacterTargets.Length; i++)
            {
                var characterTarget = new CharacterTarget(i);
                m_CharacterTargets[i] = characterTarget;
                m_HuntTargets[i] = characterTarget;
            }
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

            Array.Sort(m_HuntTargets, CharacterTarget.Comparison);

            /*
            Console.WriteLine($"=== AGRO ===");
            for (int i = 0; i < m_HuntTargets.Length; i++)
            {
                Console.WriteLine(
                    $"Character[{i}]: {m_HuntTargets[i].AggressionReceived}, Tagged: {m_HuntTargets[i].Tagged}");
            }
            */
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
            float huntThreshold = (1f / MaxCharacterCount) * 0.7f;
            for (int i = 0; i < m_HuntTargets.Length; i++)
            {
                if (clientIndex == m_HuntTargets[i].ClientIndex)
                    continue;

                if (m_HuntTargets[i].Tagged > 0)
                    continue;

                if (m_HuntTargets[i].AggressionReceived > huntThreshold)
                    continue;

                return m_HuntTargets[i].ClientIndex;
            }

            return -1;
        }

        public void TagHuntTarget(int targetIndex)
        {
            for (int i = 0; i < m_HuntTargets.Length; i++)
            {
                if (targetIndex != m_HuntTargets[i].ClientIndex)
                    continue;

                m_HuntTargets[i].Tagged = 500f;
            }
        }
    }
}