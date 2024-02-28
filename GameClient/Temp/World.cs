/*
using System;

namespace GameClient.Engine
{
    public delegate void CollisionHandler(int collisionMask, Span<RigidBody> rigidBodies);

    public class World
    {
        private Memory<RigidBody> m_RigidBodies;

        public World()
        {
        }

        public void Initialize(IRigidBodyGroup[] rigidBodyGroups)
        {
            m_RigidBodies = AllocateRigidBodies(rigidBodyGroups);

            int offset = 0;
            for (int i = 0; i < rigidBodyGroups.Length; i++)
            {
                for (int j = 0; j < rigidBodyGroups[i].Count; j++)
                {
                    int layer = rigidBodyGroups[i].Layer;
                    int type = rigidBodyGroups[i].Type;

                    // TODO: Check layers right order
                    // TODO: Check no duplicate layers

                    m_RigidBodies.Span[offset] = new RigidBody(layer, j, type);
                    offset++;
                }

                // TODO: Slice!
                // TODO: Bind storage
            }
        }

        private Memory<RigidBody> AllocateRigidBodies(IRigidBodyGroup[] rigidBodyGroups)
        {
            int totalCount = 0;
            for (int i = 0; i < rigidBodyGroups.Length; i++)
                totalCount += rigidBodyGroups[i].Count;

            return new RigidBody[totalCount];
        }

        public void Update(int tickRate, CollisionHandler collisionHandler)
        {
            Console.Write("!");
        }
    }
}
*/

