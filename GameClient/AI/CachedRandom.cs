using System;

namespace GameClient.AI
{
    public class CachedRandom
    {
        private readonly Random m_RandomGenerator;

        public CachedRandom()
        {
            // TODO: Cache
            m_RandomGenerator = new Random();
        }

        public int Next(int minValue, int maxValue)
        {
            return m_RandomGenerator.Next(minValue, maxValue);
        }

        public float NextFloat()
        {
            return m_RandomGenerator.NextSingle();
        }
    }
}