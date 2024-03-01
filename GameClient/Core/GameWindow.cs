using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharedCode.AI;
using SharedCode.Data;
using Vector2 = SharedCode.Core.Vector2;

namespace GameClient.Core
{
    public class GameWindow : Game
    {
        private const int SCREEN_WIDTH = 1000;
        private const int SCREEN_HEIGHT = 1000;

        private const int TICK_RATE = 100;
        private const int GAME_AREA_SIZE = 800;

        private const int BOT_AREA_RADIUS = 16;
        private const int PROJECTILE_AREA_RADIUS = 4;

        private const bool DISABLE_SKILL_USE = true;

        private readonly AssetLoader m_AssetLoader;
        private readonly RenderContext m_RenderContext;

        private readonly Vector2 m_ScreenCenter;
        private Font m_Font;

        private BotManager m_BotManager;

        private float m_GameAreaRadius;
        private Texture2D m_GameArea;

        private ObjectPool<CharacterDataEntry> m_Characters;
        private Vector2[] m_CharacterMoveTargets;
        private SkillCooldownTimer[] m_SkillCooldownTimers;
        private Texture2D[] m_CharacterTextures;

        private ObjectPool<ProjectileDataEntry> m_Projectiles;
        private Texture2D[] m_ProjectileTextures;
        private ItemDisposer m_ProjectileDisposer;

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
            var monospaceFont = m_AssetLoader.LoadTextureFromFile("MonospaceFont.png");
            m_Font = new Font(monospaceFont, 16, 16);

            const int maxCharacterCount = 8;
            const int maxProjectileCount = 50;

            var sensoryDataConfig = new SensoryDataConfig()
            {
                MaxCharacterCount = maxCharacterCount,
                MaxProjectileCount = maxProjectileCount
            };

            m_BotManager = new BotManager(sensoryDataConfig);
            m_BotManager.Input.Move = BotMove;
            m_BotManager.Input.UseSkill = BotUseSkill;

            m_GameAreaRadius = GAME_AREA_SIZE / 2f;
            m_GameArea = m_RenderContext.CreateTexture(GAME_AREA_SIZE, GAME_AREA_SIZE);
            Geometry.FillCircle(m_GameArea, Color.White);

            m_Characters = new ObjectPool<CharacterDataEntry>(maxCharacterCount);
            m_CharacterMoveTargets = new Vector2[m_Characters.MaxCapacity];
            m_SkillCooldownTimers = new SkillCooldownTimer[m_Characters.MaxCapacity];
            for (int i = 0; i < m_SkillCooldownTimers.Length; i++)
                m_SkillCooldownTimers[i] = new SkillCooldownTimer();

            m_CharacterTextures = new Texture2D[m_Characters.MaxCapacity];
            for (int i = 0; i < m_CharacterTextures.Length; i++)
            {
                int textureSize = BOT_AREA_RADIUS * 2;
                m_CharacterTextures[i] = m_RenderContext.CreateTexture(textureSize, textureSize);
                Geometry.FillCircle(m_CharacterTextures[i], Color.White);
            }

            SpawnCharacters(maxCharacterCount);

            m_Projectiles = new ObjectPool<ProjectileDataEntry>(maxProjectileCount);
            m_ProjectileTextures = new Texture2D[m_Projectiles.MaxCapacity];
            for (int i = 0; i < m_ProjectileTextures.Length; i++)
            {
                int textureSize = PROJECTILE_AREA_RADIUS * 2;
                m_ProjectileTextures[i] = m_RenderContext.CreateTexture(textureSize, textureSize);
                Geometry.FillCircle(m_ProjectileTextures[i], Color.White);
            }

            m_ProjectileDisposer = new ItemDisposer(m_Projectiles.MaxCapacity);
        }

        private void BotMove(int clientIndex, Vector2 target)
        {
            ref var character = ref m_Characters.Get(clientIndex);
            m_CharacterMoveTargets[clientIndex] = target;

            float distance = Distance(character.Position, target);
            if (distance <= 0)
                return;

            const float MAX_SPEED = 30f / TICK_RATE;
            float move = Math.Min(distance, MAX_SPEED) / distance;
            character.Position = Lerp(character.Position, target, move);
        }

        private bool BotUseSkill(int clientIndex, SkillGroup skillGroup, Vector2 direction)
        {
            if (DISABLE_SKILL_USE)
                return false;

            if (!m_SkillCooldownTimers[clientIndex].Ready(skillGroup))
                return false;

            ref var character = ref m_Characters.Get(clientIndex);
            ref var projectile = ref m_Projectiles.Allocate(out int index);
            projectile.Owner = clientIndex;
            projectile.Position = character.Position;
            projectile.Direction = direction;
            projectile.AreaRadius = PROJECTILE_AREA_RADIUS;

            m_SkillCooldownTimers[clientIndex].UseSkill(skillGroup);

            const float projectileLifetime = 4000;
            m_ProjectileDisposer.Add(index, projectileLifetime);

            return true;
        }

        private void SpawnCharacters(int characterCount)
        {
            for (int i = 0; i < characterCount; i++)
            {
                float radius = GAME_AREA_SIZE * 0.4f;
                float radian = i / (float)characterCount * (float)Math.PI * 2;

                float x = radius * (float)Math.Sin(radian);
                float y = radius * (float)Math.Cos(radian);

                var position = new Vector2(x, y);
                position.X += m_ScreenCenter.X;
                position.Y += m_ScreenCenter.Y;

                ref var character = ref m_Characters.Allocate(out _);
                character.ClientIndex = i;
                character.Health = 100;
                character.Position = position;
                character.Direction = Vector2.Zero;
                character.AreaRadius = BOT_AREA_RADIUS;

                // Every character is bot
                m_BotManager.Bots[i].Active = true;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Escape))
                Exit();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            for (int i = 0; i < m_SkillCooldownTimers.Length; i++)
                m_SkillCooldownTimers[i].Update(deltaTime);

            m_Projectiles.ForEach((int _, ref ProjectileDataEntry projectile) =>
            {
                const float PROJECTILE_SPEED = 200f / TICK_RATE;
                projectile.Position.X += projectile.Direction.X * PROJECTILE_SPEED;
                projectile.Position.Y += projectile.Direction.Y * PROJECTILE_SPEED;
            });

            m_ProjectileDisposer.Update(deltaTime, m_Projectiles.Free);

            const float DISSOLVE_SPEED = 15f / TICK_RATE;
            m_GameAreaRadius -= DISSOLVE_SPEED;

            m_BotManager.SensoryData.GameAreaRadius = m_GameAreaRadius;
            var characters = m_BotManager.SensoryData.WriteCharacters(m_Characters.Count);
            m_Characters.CopyTo(characters);

            var projectiles = m_BotManager.SensoryData.WriteProjectiles(m_Projectiles.Count);
            m_Projectiles.CopyTo(projectiles);
            // Console.WriteLine(m_Projectiles.Count);

            m_BotManager.Update(deltaTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            m_RenderContext.Clear(new Color(200, 95, 95));
            m_RenderContext.Begin();

            float gameAreaScale = m_GameAreaRadius / (GAME_AREA_SIZE * 0.5f);
            var gameAreaColor = new Color(0, 0, 0, 60);
            m_RenderContext.DrawSprite(
                m_GameArea,
                gameAreaScale,
                m_ScreenCenter.X,
                m_ScreenCenter.Y,
                color: gameAreaColor);

            m_Characters.ForEach((int index, ref CharacterDataEntry character) =>
            {
                var texture = m_CharacterTextures[index];
                float x = character.Position.X;
                float y = character.Position.Y;

                m_RenderContext.DrawSprite(texture, 1, x, y, color: CharacterColor(index));
                m_RenderContext.DrawGlyph(m_Font, 1, index, x, y);
            });

            m_Projectiles.ForEach((int index, ref ProjectileDataEntry projectile) =>
            {
                var texture = m_ProjectileTextures[index];
                float x = projectile.Position.X;
                float y = projectile.Position.Y;

                m_RenderContext.DrawSprite(texture, 1, x, y, color: Color.Black);
            });

            for (int i = 0; i < m_CharacterMoveTargets.Length; i++)
            {
                var moveTarget = m_CharacterMoveTargets[i];
                const float size = 22;

                m_RenderContext.DrawRectangle(CharacterColor(i), moveTarget.X, moveTarget.Y, size, size);
                m_RenderContext.DrawGlyph(m_Font, 1, i, moveTarget.X, moveTarget.Y);
            }

            m_RenderContext.End();

            base.Draw(gameTime);
        }

        private float Distance(Vector2 a, Vector2 b)
        {
            float tempA = a.X - b.X;
            float tempB = a.Y - b.Y;
            return MathF.Sqrt(tempA * tempA + tempB * tempB);
        }

        private Vector2 Lerp(Vector2 a, Vector2 b, float amount)
        {
            return new Vector2
            {
                X = a.X + (b.X - a.X) * amount,
                Y = a.Y + (b.Y - a.Y) * amount
            };
        }

        private Color CharacterColor(int index)
        {
            // TODO
            return new Color(240, 170, 170);
        }
    }
}