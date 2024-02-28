/*
using System;

namespace GameClient.Engine
{
    // TODO: Could be enum
    public interface ICollider
    {
        public int Type { get; }
    }

    public class CircleCollider : ICollider
    {
        public const int TYPE = 1;
        public int Type => TYPE;
    }

    public class BoxCollider : ICollider
    {
        public const int TYPE = 2;
        public int Type => TYPE;
    }

    public static class ColliderExtension
    {
        public static void SetCircleRadius(this RigidBody rigidBody, int radius)
        {
            if (rigidBody.Type != CircleCollider.TYPE)
                throw new Exception("RigidBody missing CircleCollider");

            rigidBody.Shape = radius;
        }

        public static void SetBoxSize(this RigidBody rigidBody, int width, int height)
        {
            if (rigidBody.Type != BoxCollider.TYPE)
                throw new Exception("RigidBody missing BoxCollider");

            throw new NotImplementedException();
        }
    }

    // (x2-x1)^2 + (y2-y1)^2 <= (r1+r2)^2
}
*/

