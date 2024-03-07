using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharedCode.AI;
using SharedCode.Data;
using Vector2 = SharedCode.Core.Vector2;

namespace GameClient.Core
{
    public class ExternalForce
    {
        public float Force;
        public Vector2 Direction;
    }

    public class GameWindow : Game
    {
        private const int SPEED_MULTIPLIER = 1;

        private const int SCREEN_WIDTH = 1000;
        private const int SCREEN_HEIGHT = 1000;

        private const int TICK_RATE = 100;
        private const int GAME_AREA_SIZE = 800;

        private const float AREA_DISSOLVE_SPEED = 5f;

        private const int BOT_AREA_RADIUS = 16;
        private const float BOT_MOVE_SPEED = 80f;

        private const float PROJECTILE_SPEED = 300f;
        private const float PROJECTILE_MAX_RANGE = 200f;
        private const int PROJECTILE_AREA_RADIUS = 4;

        private const bool DISABLE_SKILL_USE = false;
        private const bool RENDER_MOVE_TARGET = false;

        private readonly AssetLoader m_AssetLoader;
        private readonly RenderContext m_RenderContext;

        private readonly Vector2 m_ScreenCenter;
        private Font m_Font;

        private SkillConfigGroup m_SkillConfigGroup;
        private BotManager m_BotManager;

        private float m_GameAreaRadius;
        private Texture2D m_GameArea;

        private ObjectPool<CharacterDataEntry> m_Characters;
        private ExternalForce[] m_CharacterExternalForces;
        private float[] m_CharacterShieldBuffs;
        private float[] m_CharacterHasteBuffs;
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

            const float ACTION_DISTANCE = 80f;

            m_SkillConfigGroup = new SkillConfigGroup(new[]
            {
                new SkillConfig(Skill.EnergyProjectile_1, PROJECTILE_MAX_RANGE, 0),
                new SkillConfig(Skill.EnergyProjectile_2, PROJECTILE_MAX_RANGE, 0),
                new SkillConfig(Skill.EnergyProjectile_3, PROJECTILE_MAX_RANGE, 0),
                new SkillConfig(Skill.RapidShot, PROJECTILE_MAX_RANGE * 0.8f, 0),
                new SkillConfig(Skill.HomingMissile, PROJECTILE_MAX_RANGE * 2, 0), // Not implemented
                new SkillConfig(Skill.CounterShield, 0, 1700),
                new SkillConfig(Skill.Teleport, PROJECTILE_MAX_RANGE, 0), // Probably not right range
                new SkillConfig(Skill.Dash, 0, 1800),
                new SkillConfig(Skill.Stomp, 8, 0), // Range => Radius
            });

            var sensoryDataConfig = new SensoryDataConfig()
            {
                MaxCharacterCount = maxCharacterCount,
                MaxProjectileCount = maxProjectileCount
            };

            m_BotManager = new BotManager(ACTION_DISTANCE, m_SkillConfigGroup, sensoryDataConfig);
            m_BotManager.Input.Move = BotMove;
            m_BotManager.Input.UseSkill = BotUseSkill;

            // This should be used on round start
            m_BotManager.ResetData();
            m_BotManager.ShuffleSkillLayout(3);

            for (int i = 0; i < m_BotManager.Bots.Length; i++)
            {
                var bot = m_BotManager.Bots[i];
                bot.Difficulty = 0.5f;
            }

            m_GameAreaRadius = GAME_AREA_SIZE / 2f;
            m_GameArea = m_RenderContext.CreateTexture(GAME_AREA_SIZE, GAME_AREA_SIZE);
            Circle.Generate(m_GameArea, Color.White);

            m_Characters = new ObjectPool<CharacterDataEntry>(maxCharacterCount);
            m_CharacterExternalForces = new ExternalForce[maxCharacterCount];
            m_CharacterShieldBuffs = new float[maxCharacterCount];
            m_CharacterHasteBuffs = new float[maxCharacterCount];
            m_CharacterMoveTargets = new Vector2[maxCharacterCount];
            m_SkillCooldownTimers = new SkillCooldownTimer[maxCharacterCount];
            m_CharacterTextures = new Texture2D[maxCharacterCount];

            for (int i = 0; i < maxCharacterCount; i++)
            {
                m_CharacterExternalForces[i] = new ExternalForce();
                m_CharacterMoveTargets[i] = Vector2.Zero;
                m_SkillCooldownTimers[i] = new SkillCooldownTimer();

                int textureSize = BOT_AREA_RADIUS * 2;
                m_CharacterTextures[i] = m_RenderContext.CreateTexture(textureSize, textureSize);
                Circle.Generate(m_CharacterTextures[i], Color.White);
            }

            SpawnCharacters(maxCharacterCount);

            m_Projectiles = new ObjectPool<ProjectileDataEntry>(maxProjectileCount);
            m_ProjectileTextures = new Texture2D[maxProjectileCount];
            for (int i = 0; i < m_ProjectileTextures.Length; i++)
            {
                int textureSize = PROJECTILE_AREA_RADIUS * 2;
                m_ProjectileTextures[i] = m_RenderContext.CreateTexture(textureSize, textureSize);
                Circle.Generate(m_ProjectileTextures[i], Color.White);
            }

            m_ProjectileDisposer = new ItemDisposer(maxProjectileCount);
        }

        private bool BotMove(int clientIndex, Vector2 target)
        {
            if (m_CharacterExternalForces[clientIndex].Force > 0)
                return false;

            ref var character = ref m_Characters.Get(clientIndex);
            m_CharacterMoveTargets[clientIndex] = target;

            float distance = Distance(character.Position, target);
            if (distance <= 0)
                return false;

            const float TICK_MOVE = BOT_MOVE_SPEED / TICK_RATE;
            float move = Math.Min(distance, TICK_MOVE) / distance;

            if (m_CharacterHasteBuffs[clientIndex] > 0)
                move *= 2.25f;

            character.Position = Lerp(character.Position, target, move);

            return true;
        }

        private bool BotUseSkill(int clientIndex, Skill skill, Vector2 direction)
        {
            if (DISABLE_SKILL_USE)
                return false;

            if (!m_SkillCooldownTimers[clientIndex].Ready(skill))
                return false;

            m_SkillCooldownTimers[clientIndex].UseSkill(skill);

            switch (skill)
            {
                case Skill.EnergyProjectile_1:
                case Skill.EnergyProjectile_2:
                case Skill.EnergyProjectile_3:
                case Skill.RapidShot:
                case Skill.HomingMissile:
                    ref var character = ref m_Characters.Get(clientIndex);
                    ref var projectile = ref m_Projectiles.Allocate(out int index);
                    projectile.Owner = clientIndex;
                    projectile.Position = character.Position;
                    projectile.Direction = direction;
                    projectile.MaxRange = PROJECTILE_MAX_RANGE;
                    projectile.AreaRadius = PROJECTILE_AREA_RADIUS;

                    const float TIME_TO_REACH_MAX_RANGE = PROJECTILE_MAX_RANGE / PROJECTILE_SPEED;
                    const float projectileLifetime = TIME_TO_REACH_MAX_RANGE * 1000f;
                    m_ProjectileDisposer.Add(index, projectileLifetime);
                    return true;
                case Skill.CounterShield:
                    m_CharacterShieldBuffs[clientIndex] = m_SkillConfigGroup.Skills[(int)skill].Duration;
                    return true;
                case Skill.Teleport:
                    ref var teleportCharacter = ref m_Characters.Get(clientIndex);
                    teleportCharacter.Position = direction; // TODO: Refactor name
                    return true;
                case Skill.Dash:
                    m_CharacterHasteBuffs[clientIndex] = m_SkillConfigGroup.Skills[(int)skill].Duration;
                    return true;
                case Skill.Stomp:
                    Console.WriteLine("Stomp!");
                    // Not simulated
                    return true;
                default:
                    throw new NotImplementedException();
            }
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
                character.Health = 4;
                character.Position = position;
                character.Direction = Vector2.Zero;

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

            for (int i = 0; i < SPEED_MULTIPLIER; i++)
                UpdateTick(deltaTime);

            base.Update(gameTime);
        }

        private void UpdateTick(float deltaTime)
        {
            for (int i = 0; i < m_SkillCooldownTimers.Length; i++)
                m_SkillCooldownTimers[i].Update(deltaTime);

            m_Projectiles.ForEach((int index, ref ProjectileDataEntry projectile) =>
            {
                const float TICK_MOVE = PROJECTILE_SPEED / TICK_RATE;
                projectile.Position.X += projectile.Direction.X * TICK_MOVE;
                projectile.Position.Y += projectile.Direction.Y * TICK_MOVE;

                if (ProjectileCollision(ref projectile, out int characterIndex))
                {
                    if (m_CharacterShieldBuffs[characterIndex] > 0)
                    {
                        projectile.Owner = characterIndex;
                        projectile.Direction.X *= -1;
                        projectile.Direction.Y *= -1;

                        // Move extra step just in case
                        projectile.Position.X += projectile.Direction.X * TICK_MOVE;
                        projectile.Position.Y += projectile.Direction.Y * TICK_MOVE;
                        return;
                    }

                    ref var character = ref m_Characters.Get(characterIndex);
                    character.Health--;

                    m_CharacterExternalForces[characterIndex].Force = 50f;
                    m_CharacterExternalForces[characterIndex].Direction = projectile.Direction;

                    m_ProjectileDisposer.Dispose(index);
                }
            });

            m_ProjectileDisposer.Update(deltaTime, m_Projectiles.Free);

            m_Characters.ForEach((int index, ref CharacterDataEntry character) =>
            {
                m_CharacterShieldBuffs[index] -= deltaTime;
                if (m_CharacterShieldBuffs[index] < 0)
                    m_CharacterShieldBuffs[index] = 0;

                m_CharacterHasteBuffs[index] -= deltaTime;
                if (m_CharacterHasteBuffs[index] < 0)
                    m_CharacterHasteBuffs[index] = 0;

                if (m_CharacterExternalForces[index].Force > 0)
                {
                    float force = 300f / TICK_RATE;
                    m_CharacterExternalForces[index].Force -= force;

                    if (m_CharacterExternalForces[index].Force < 0)
                    {
                        force += m_CharacterExternalForces[index].Force;
                        m_CharacterExternalForces[index].Force = 0;
                    }

                    character.Position.X += force * m_CharacterExternalForces[index].Direction.X;
                    character.Position.Y += force * m_CharacterExternalForces[index].Direction.Y;
                }
            });

            m_GameAreaRadius -= AREA_DISSOLVE_SPEED / TICK_RATE;

            m_BotManager.SensoryData.GameAreaCenter = m_ScreenCenter;
            m_BotManager.SensoryData.GameAreaRadius = m_GameAreaRadius;
            var characters = m_BotManager.SensoryData.WriteCharacters(m_Characters.Count);
            m_Characters.CopyTo(characters);

            var projectiles = m_BotManager.SensoryData.WriteProjectiles(m_Projectiles.Count);
            m_Projectiles.CopyTo(projectiles);

            // m_DebugTimer.Start();
            m_BotManager.Update(deltaTime);

            /*
            m_DebugTimer.Stop();
            m_DebugTimes.Add(m_DebugTimer.Elapsed.TotalMilliseconds);
            m_DebugTimer.Reset();

            if (m_DebugTimes.Count == 100)
            {
                Console.WriteLine(m_DebugTimes.Average());
                m_DebugTimes.Clear();
            }
            */
        }

        // private Stopwatch m_DebugTimer = new Stopwatch();
        // private List<double> m_DebugTimes = new List<double>();

        private bool ProjectileCollision(ref ProjectileDataEntry projectile, out int characterIndex)
        {
            int collisionIndex = -1;

            int ownerIndex = projectile.Owner;
            var position = projectile.Position;

            // Not fastest way to do this
            m_Characters.ForEach((int index, ref CharacterDataEntry character) =>
            {
                if (ownerIndex == index || character.Health <= 0)
                    return;

                if (CircleContains(character.Position, BOT_AREA_RADIUS, position))
                    collisionIndex = index;
            });

            characterIndex = collisionIndex;
            return collisionIndex != -1;
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
                if (character.Health <= 0)
                    return;

                var texture = m_CharacterTextures[index];
                float x = character.Position.X;
                float y = character.Position.Y;

                var characterColor = CharacterColor(255);
                characterColor.R = m_CharacterShieldBuffs[index] > 0 ? (byte)100 : (byte)240;
                m_RenderContext.DrawSprite(texture, 1, x, y, color: characterColor);

                var glyphColor = new Color(255, 255, 255);
                glyphColor.R = m_CharacterHasteBuffs[index] > 0 ? (byte)100 : (byte)255;
                glyphColor.B = m_CharacterHasteBuffs[index] > 0 ? (byte)100 : (byte)255;
                m_RenderContext.DrawGlyph(m_Font, 1, index, x, y, color: glyphColor);
            });

            m_Projectiles.ForEach((int index, ref ProjectileDataEntry projectile) =>
            {
                var texture = m_ProjectileTextures[index];
                float x = projectile.Position.X;
                float y = projectile.Position.Y;

                var projectileColor = new Color(200, 200, 200);
                m_RenderContext.DrawSprite(texture, 1, x, y, color: projectileColor);
            });

            for (int i = 0; i < m_CharacterMoveTargets.Length; i++)
            {
                if (!RENDER_MOVE_TARGET)
                    continue;

                ref var character = ref m_Characters.Get(i);
                if (character.Health <= 0)
                    continue;

                var moveTarget = m_CharacterMoveTargets[i];
                const float size = 22;

                const byte alpha = 60;
                var glyphColor = new Color((byte)255, (byte)255, (byte)255, alpha);

                m_RenderContext.DrawRectangle(CharacterColor(alpha), moveTarget.X, moveTarget.Y, size, size);
                m_RenderContext.DrawGlyph(m_Font, 1, i, moveTarget.X, moveTarget.Y, color: glyphColor);
            }

            m_RenderContext.End();

            base.Draw(gameTime);
        }

        private bool CircleContains(Vector2 circlePoint, float circleRadius, Vector2 point)
        {
            // (x - center_x)² + (y - center_y)² < radius²
            return (point.X - circlePoint.X) * (point.X - circlePoint.X)
                   + (point.Y - circlePoint.Y) * (point.Y - circlePoint.Y)
                   < circleRadius * circleRadius;
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

        private Color CharacterColor(byte alpha)
        {
            return new Color((byte)240, (byte)170, (byte)170, alpha);
        }
    }
}