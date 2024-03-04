#if !UNITY_STANDALONE && !UNITY_EDITOR && !UNITY_ANDROID && !UNITY_IOS
using System;

namespace SharedCode.Core
{
    public class CachedRandom
    {
        // TODO: Cache
        private readonly Random m_RandomGenerator;

        public CachedRandom()
        {
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
#else
using UnityEngine;

namespace SharedCode.Core
{
    public class CachedRandom
    {
        public int Next(int minValue, int maxValue)
        {
            return Random.Range(minValue, maxValue);
        }

        public float NextFloat()
        {
            return Random.Range(0f, 1f);
        }
    }
}
#endif