using System;
using SharedCode.Core;

namespace SharedCode.Data
{
    public struct SensoryDataConfig
    {
        public int MaxCharacterCount;
        public int MaxProjectileCount;
    }

    public class SensoryData
    {
        public Vector2 GameAreaCenter;
        public float GameAreaRadius;

        private readonly Memory<CharacterDataEntry> m_CharacterDataEntries;
        private int m_CharacterCount;

        private readonly Memory<ProjectileDataEntry> m_ProjectileDataEntries;
        private int m_ProjectileCount;

        public SensoryData(SensoryDataConfig config)
        {
            GameAreaRadius = 0;

            m_CharacterDataEntries = new CharacterDataEntry[config.MaxCharacterCount];
            m_CharacterCount = 0;

            m_ProjectileDataEntries = new ProjectileDataEntry[config.MaxProjectileCount];
            m_ProjectileCount = 0;
        }

        public void Reset()
        {
            GameAreaCenter = Vector2.Zero;
            GameAreaRadius = 0;

            // No need to fill CharacterDataEntry memory with zero
            m_CharacterCount = 0;

            // No need to fill ProjectileDataEntry memory with zero
            m_ProjectileCount = 0;
        }

        public ReadOnlySpan<CharacterDataEntry> ReadCharacters()
        {
            return m_CharacterDataEntries.Span.Slice(0, m_CharacterCount);
        }

        public Span<CharacterDataEntry> WriteCharacters(int count)
        {
            m_CharacterCount = count;
            return m_CharacterDataEntries.Span.Slice(0, count);
        }

        public ReadOnlySpan<ProjectileDataEntry> ReadProjectiles()
        {
            return m_ProjectileDataEntries.Span.Slice(0, m_ProjectileCount);
        }

        public Span<ProjectileDataEntry> WriteProjectiles(int count)
        {
            m_ProjectileCount = count;
            return m_ProjectileDataEntries.Span.Slice(0, count);
        }
    }
}