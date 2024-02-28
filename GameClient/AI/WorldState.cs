using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GameClient.AI
{
    public class WorldState
    {
        public float GameAreaRadius;

        public readonly List<ICharacter> Characters;

        private readonly Memory<Projectile> m_Projectiles;
        private int m_ProjectileCount;

        // TODO: Obstacles

        public WorldState()
        {
            GameAreaRadius = 0;

            Characters = new List<ICharacter>();

            m_Projectiles = new Projectile[Projectile.MAX_COUNT];
            m_ProjectileCount = 0;
        }

        public void Spawn(int gameAreaRadius, Vector2 offset)
        {
            GameAreaRadius = gameAreaRadius;

            for (int i = 0; i < Characters.Count; i++)
            {
                float radius = gameAreaRadius * 0.8f;
                float radian = i / (float)Characters.Count * (float)Math.PI * 2;

                float x = radius * (float)Math.Sin(radian);
                float y = radius * (float)Math.Cos(radian);

                var position = new Vector2(x, y) + offset;
                Characters[i].Position = position;
            }
        }

        public ReadOnlySpan<Projectile> ReadProjectiles()
        {
            return m_Projectiles.Span.Slice(0, m_ProjectileCount);
        }

        public Span<Projectile> WriteProjectiles(int count)
        {
            m_ProjectileCount = count;
            return m_Projectiles.Span.Slice(0, count);
        }
    }
}