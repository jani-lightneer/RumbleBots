using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameClient.Core
{
    public class RenderContext
    {
        private readonly GraphicsDeviceManager m_GraphicsDeviceManager;
        private GraphicsDevice m_GraphicsDevice;
        private SpriteBatch m_SpriteBatch;
        private Texture2D m_EmptyColor;

        public RenderContext(GraphicsDeviceManager graphicsDeviceManager)
        {
            m_GraphicsDeviceManager = graphicsDeviceManager;
        }

        public void Initialize()
        {
            m_GraphicsDevice = m_GraphicsDeviceManager.GraphicsDevice;
            m_SpriteBatch = new SpriteBatch(m_GraphicsDevice);
            m_EmptyColor = CreateTexture(1, 1);
            m_EmptyColor.SetData(new byte[] { 255, 255, 255, 255 });
        }

        public Texture2D CreateTexture(int width, int height)
        {
            return new Texture2D(m_GraphicsDevice, width, height);
        }

        public void Clear(Color color)
        {
            m_GraphicsDevice.Clear(color);
        }

        public void Begin()
        {
            m_SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp);
        }

        public void DrawRectangle(
            Color color,
            Vector2 position,
            Vector2 size,
            Vector2? origin = null)
        {
            if (origin == null)
                origin = new Vector2(0.5f, 0.5f);

            m_SpriteBatch.Draw(
                m_EmptyColor,
                position,
                null,
                color,
                0,
                (Vector2)origin,
                size,
                SpriteEffects.None,
                0f);
        }

        public void DrawSprite(
            Texture2D texture,
            float scale,
            Vector2 position,
            Vector2? origin = null,
            Color? color = null)
        {
            var frame = texture.Bounds;

            var spriteOrigin = (origin == null)
                ? new Vector2(frame.Width * 0.5f, frame.Height * 0.5f)
                : new Vector2(frame.Width * ((Vector2)origin).X, frame.Height * ((Vector2)origin).Y);

            var spriteColor = color ?? Color.White;

            m_SpriteBatch.Draw(
                texture,
                position,
                frame,
                spriteColor,
                0,
                spriteOrigin,
                new Vector2(scale, scale),
                SpriteEffects.None,
                0f);
        }

        public void DrawGlyph(
            Font font,
            float scale,
            int glyph,
            float x,
            float y,
            Vector2? origin = null,
            Color? color = null)
        {
            var frame = font.Glyphs[glyph];

            var spriteOrigin = (origin == null)
                ? new Vector2(frame.Width * 0.5f, frame.Height * 0.5f)
                : new Vector2(frame.Width * ((Vector2)origin).X, frame.Height * ((Vector2)origin).Y);

            var spriteColor = color ?? Color.White;

            m_SpriteBatch.Draw(
                font.TextureAtlas,
                new Vector2(x, y),
                frame,
                spriteColor,
                0,
                spriteOrigin,
                new Vector2(scale, scale),
                SpriteEffects.None,
                0f);
        }

        public void End()
        {
            m_SpriteBatch.End();
        }
    }
}