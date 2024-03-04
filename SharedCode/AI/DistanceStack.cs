namespace SharedCode.AI
{
    public struct DistanceStackItem
    {
        public int Index;
        public float Distance;
    }

    // TODO: Rethink names
    public class DistanceStack
    {
        public bool Used { get; private set; }

        private readonly int m_Count;
        private readonly DistanceStackItem[] m_Items;

        public DistanceStack(int count)
        {
            Used = false;

            m_Count = count;
            m_Items = new DistanceStackItem[count];
        }

        public void Reset(float maxDistance)
        {
            Used = false;

            for (int i = 0; i < m_Count; i++)
            {
                m_Items[i].Index = -1;
                m_Items[i].Distance = maxDistance;
            }
        }

        public void TryAdd(int target, float distance)
        {
            for (int i = 0; i < m_Count; i++)
            {
                if (distance < m_Items[i].Distance)
                {
                    Used = true;
                    m_Items[i].Index = target;
                    m_Items[i].Distance = distance;

                    // TODO: Test!
                    Array.Sort(m_Items, (a, b) => (int)b.Distance - (int)a.Distance);
                    return;
                }
            }
        }

        public bool Contain(int index)
        {
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Items[i].Index == index)
                    return true;
            }

            return false;
        }
    }
}