// TODO: Wrong namespace

using Microsoft.Xna.Framework;

namespace GameClient.AI
{
    public struct Projectile
    {
        public const int MAX_COUNT = 100;

        public int Owner;
        public Vector2 Position;
        public Vector2 Velocity;
        public float AreaRadius;
    }
}