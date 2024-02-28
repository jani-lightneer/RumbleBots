using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace GameClient.Core
{
    public class Font
    {
        public readonly Texture2D TextureAtlas;
        public readonly Rectangle[] Glyphs;
        public readonly int GlyphSize;

        public Font(Texture2D textureAtlas, int spacing, int glyphSize)
        {
            TextureAtlas = textureAtlas;
            Glyphs = GenerateGlyphs(TextureAtlas.Width, TextureAtlas.Height, spacing, glyphSize);
            GlyphSize = glyphSize;
        }

        private Rectangle[] GenerateGlyphs(int textureWidth, int textureHeight, int spacing, int glyphSize)
        {
            int width = textureWidth / (spacing + glyphSize);
            int height = textureHeight / (spacing + glyphSize);

            var glyphs = new Rectangle[width * height];

            int index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    glyphs[index] = new Rectangle
                    (
                        x * (spacing + glyphSize) + spacing,
                        y * (spacing + glyphSize) + spacing,
                        glyphSize,
                        glyphSize
                    );

                    index++;
                }
            }

            return glyphs;
        }
    }
}