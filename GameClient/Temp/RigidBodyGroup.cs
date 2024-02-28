/*
using System;

namespace GameClient.Engine
{
    // TODO: Could be enum
    public interface IRigidBodyGroup
    {
        public int Layer { get; }
        public int Count { get; }
        public int Type { get; }
    }

    public class RigidBodyGroup<T> : IRigidBodyGroup where T : ICollider
    {
        public int Layer { get; private set; }
        public int Count { get; private set; }
        public int Type => m_Collider.Type;

        private readonly ICollider m_Collider;

        private Memory<RigidBody> m_Storage;
        private bool m_StorageReady;

        public RigidBodyGroup(int layer, int count)
        {
            Layer = layer;
            Count = count;

            m_Collider = Activator.CreateInstance<T>();

            m_Storage = null;
            m_StorageReady = false;
        }

        public void BindStorage(Memory<RigidBody> rigidBodies)
        {
            if (m_StorageReady)
                throw new Exception("Storage is already bound");

            m_Storage = rigidBodies;
            m_StorageReady = true;
        }

        public void AllowedCollision(int id)
        {
            // TODO
        }

        public Span<RigidBody> Write()
        {
            return Span<RigidBody>.Empty;
        }
    }
}
*/

