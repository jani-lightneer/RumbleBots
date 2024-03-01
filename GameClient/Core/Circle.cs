using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameClient.Core
{
    public static class Circle
    {
        public static void Generate(Texture2D texture, Color color)
        {
            int textureWidth = texture.Width;
            int textureHeight = texture.Height;

            // Generate new color data everytime, not optimal
            var colors = new Color[textureWidth * textureHeight];

            double centerX = textureWidth / 2f;
            double centerY = textureHeight / 2f;
            double radius = Math.Min(textureWidth, textureHeight) / 2f;

            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    double dx = centerX - x;
                    double dy = centerY - y;

                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    if (distance > radius)
                        continue;

                    int index = y * textureWidth + x;
                    colors[index] = color;
                }
            }

            texture.SetData(colors);
        }
    }
}