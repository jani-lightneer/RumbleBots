using System;

namespace GameClient.Core
{
    public delegate void ObjectPoolCallback<T>(int index, ref T entity);

    public class ObjectPool<T> where T : struct
    {
        public readonly int MaxCapacity;
        public int Count;

        private readonly T[] m_Entities;
        private bool[] m_Allocated;

        public ObjectPool(int maxCapacity)
        {
            MaxCapacity = maxCapacity;
            Count = 0;

            m_Entities = new T[maxCapacity];
            m_Allocated = new bool[maxCapacity];
        }

        public ref T Allocate(out int index)
        {
            for (int i = 0; i < MaxCapacity; i++)
            {
                if (m_Allocated[i])
                    continue;

                m_Allocated[i] = true;
                Count++;

                index = i;
                return ref m_Entities[i];
            }

            throw new Exception("Buffer is full");
        }

        public ref T Get(int index)
        {
            if (!m_Allocated[index])
                throw new Exception($"Invalid index {index}");

            return ref m_Entities[index];
        }

        public void Free(int index)
        {
            if (!m_Allocated[index])
                throw new Exception($"Invalid index {index}");

            m_Allocated[index] = false;
            Count--;
        }

        public void ForEach(ObjectPoolCallback<T> cb)
        {
            for (int i = 0; i < MaxCapacity; i++)
            {
                if (!m_Allocated[i])
                    continue;

                cb(i, ref m_Entities[i]);
            }
        }

        public void CopyTo(Span<T> entities)
        {
            int writeIndex = 0;
            for (int i = 0; i < MaxCapacity; i++)
            {
                if (!m_Allocated[i])
                    continue;

                entities[writeIndex] = m_Entities[i];
                writeIndex++;
            }
        }
    }
}