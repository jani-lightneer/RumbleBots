using System;

namespace GameClient
{
    public struct ItemCounter
    {
        public bool Registered;
        public float Lifetime;
    }

    public class ItemDisposer
    {
        private ItemCounter[] m_ItemCounters;

        public ItemDisposer(int itemCount)
        {
            m_ItemCounters = new ItemCounter[itemCount];
        }

        public void Update(float deltaTime, Action<int> cb)
        {
            for (int i = 0; i < m_ItemCounters.Length; i++)
            {
                if (!m_ItemCounters[i].Registered)
                    continue;

                m_ItemCounters[i].Lifetime -= deltaTime;
                if (m_ItemCounters[i].Lifetime <= 0)
                {
                    cb(i);

                    m_ItemCounters[i].Registered = false;
                    m_ItemCounters[i].Lifetime = 0;
                }
            }
        }

        public void Add(int index, float lifetime)
        {
            if (m_ItemCounters[index].Registered)
                throw new Exception("Item is already on dispose timer");

            m_ItemCounters[index].Registered = true;
            m_ItemCounters[index].Lifetime = lifetime;
        }

        public void Dispose(int index)
        {
            m_ItemCounters[index].Lifetime = 0;
        }
    }
}