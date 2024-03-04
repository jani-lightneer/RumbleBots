using SharedCode.Core;

namespace SharedCode.Data
{
    public struct ProjectileDataEntry
    {
        public int Owner;
        public Vector2 Position;
        public Vector2 Direction;
        public float MaxRange;
        public float AreaRadius;
    }
}