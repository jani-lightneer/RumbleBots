using System;
using GameClient.AI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameClient.Core
{
    public class GameWindow : Game
    {
        private const int SCREEN_WIDTH = 1000;
        private const int SCREEN_HEIGHT = 1000;

        private const int TICK_RATE = 60;
        private const int GAME_AREA_SIZE = 800;

        private readonly AssetLoader m_AssetLoader;
        private readonly RenderContext m_RenderContext;

        private readonly Vector2 m_ScreenCenter;

        private BotManager m_BotManager;
        private WorldState m_WorldState;

        private Texture2D m_GameArea;
        private Texture2D[] m_CharacterTextures;
        private Texture2D[] m_ProjectileTextures;

        private int m_ProjectileCount;

        public GameWindow()
        {
            var graphicsDeviceManager = new GraphicsDeviceManager(this);
            graphicsDeviceManager.PreferredBackBufferWidth = SCREEN_WIDTH;
            graphicsDeviceManager.PreferredBackBufferHeight = SCREEN_HEIGHT;
            graphicsDeviceManager.SynchronizeWithVerticalRetrace = false;

            m_AssetLoader = new AssetLoader(graphicsDeviceManager);
            m_RenderContext = new RenderContext(graphicsDeviceManager);

            m_ScreenCenter = new Vector2(SCREEN_WIDTH / 2f, SCREEN_HEIGHT / 2f);

            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0f / TICK_RATE);
            IsFixedTimeStep = true;
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            Window.Title = "Word Game";

            m_AssetLoader.Initialize();
            m_RenderContext.Initialize();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            m_BotManager = new BotManager(Character.MAX_COUNT);
            m_BotManager.RegisterMove(BotMove);
            m_BotManager.RegisterCastSpell(BotCastSpell);

            m_WorldState = new WorldState();

            for (int i = 0; i < Character.MAX_COUNT; i++)
            {
                var bot = m_BotManager.GetBot(i);
                m_WorldState.Characters.Add(bot);
            }

            // TODO: Add rest

            int gameAreaRadius = GAME_AREA_SIZE / 2;
            m_WorldState.Spawn(gameAreaRadius, m_ScreenCenter);

            // var monospaceFont = m_AssetLoader.LoadTextureFromFile("MonospaceFont.png");
            // m_Font = new Font(monospaceFont, 16, 16);

            m_GameArea = m_RenderContext.CreateTexture(GAME_AREA_SIZE, GAME_AREA_SIZE);
            Geometry.FillCircle(m_GameArea, Color.White);

            m_CharacterTextures = new Texture2D[Character.MAX_COUNT];
            for (int i = 0; i < m_CharacterTextures.Length; i++)
            {
                int textureSize = Bot.CIRCLE_COLLIDER_RADIUS * 2;
                m_CharacterTextures[i] = m_RenderContext.CreateTexture(textureSize, textureSize);
                Geometry.FillCircle(m_CharacterTextures[i], Color.White);
            }

            m_ProjectileTextures = new Texture2D[Projectile.MAX_COUNT];
            for (int i = 0; i < m_ProjectileTextures.Length; i++)
            {
                int textureSize = 8;
                m_ProjectileTextures[i] = m_RenderContext.CreateTexture(textureSize, textureSize);
                Geometry.FillCircle(m_ProjectileTextures[i], Color.White);
            }

            m_ProjectileCount = 0;
        }

        // This would be use Unity GameObject
        private void BotMove(int clientIndex, Vector2 target)
        {
            var character = m_BotManager.GetBot(clientIndex);

            const float MAX_SPEED = 20f / TICK_RATE;

            float distance = Vector2.Distance(character.Position, target);
            if (distance <= 0)
                return;

            float move = Math.Min(distance, MAX_SPEED) / distance;
            character.Position = Vector2.Lerp(character.Position, target, move);
        }

        // This would be use Unity GameObject
        private void BotCastSpell(int clientIndex, SpellType spellType, Vector2 direction)
        {
            if (m_ProjectileCount >= Projectile.MAX_COUNT)
            {
                Console.WriteLine("Projectile buffer is full");
                return;
            }

            m_ProjectileCount++;
            Console.WriteLine(m_ProjectileCount);

            const float PROJECTILE_SPEED = 200f / TICK_RATE;

            var character = m_BotManager.GetBot(clientIndex);
            Span<Projectile> projectiles = m_WorldState.WriteProjectiles(m_ProjectileCount);
            projectiles[^1].Position.X = character.Position.X;
            projectiles[^1].Position.Y = character.Position.Y;
            projectiles[^1].Velocity.X = direction.X * PROJECTILE_SPEED;
            projectiles[^1].Velocity.Y = direction.Y * PROJECTILE_SPEED;

            Console.WriteLine($"CastSpell[{clientIndex}]: {spellType} {direction}");
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Escape))
                Exit();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            Span<Projectile> projectiles = m_WorldState.WriteProjectiles(m_ProjectileCount);
            for (int i = 0; i < projectiles.Length; i++)
                projectiles[i].Position += projectiles[i].Velocity;

            m_BotManager.Update(m_WorldState, deltaTime);

            const float DISSOLVE_SPEED = 15f / TICK_RATE;
            m_WorldState.GameAreaRadius -= DISSOLVE_SPEED;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            m_RenderContext.Clear(new Color(200, 95, 95));
            m_RenderContext.Begin();

            float gameAreaScale = m_WorldState.GameAreaRadius / (GAME_AREA_SIZE * 0.5f);
            var gameAreaColor = new Color(0, 0, 0, 60);
            m_RenderContext.DrawSprite(m_GameArea, gameAreaScale, m_ScreenCenter, color: gameAreaColor);

            foreach (var character in m_WorldState.Characters)
            {
                var texture = m_CharacterTextures[character.ClientIndex];
                m_RenderContext.DrawSprite(texture, 1, character.Position, color: Color.Pink);
            }

            ReadOnlySpan<Projectile> projectiles = m_WorldState.ReadProjectiles();
            for (int i = 0; i < projectiles.Length; i++)
            {
                var texture = m_ProjectileTextures[i];
                m_RenderContext.DrawSprite(texture, 1, projectiles[i].Position, color: Color.Black);
            }

            m_RenderContext.End();

            base.Draw(gameTime);
        }
    }
}