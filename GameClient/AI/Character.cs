using System;
using Microsoft.Xna.Framework;

// TODO: Wrong namespace
namespace GameClient.AI
{
    public interface ICharacter
    {
        public int ClientIndex { get; }
        public Vector2 Position { get; set; }
    }

    public static class Character
    {
        public const int MAX_COUNT = 8;
    }
}