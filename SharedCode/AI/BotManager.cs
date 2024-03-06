using SharedCode.Core;
using SharedCode.Data;

namespace SharedCode.AI
{
    public delegate bool MoveHandler(
        int clientIndex,
        Vector2 target);

    public delegate bool UseSkillHandler(
        int clientIndex,
        Skill skill,
        Vector2 direction);

    public class BotInput
    {
        public MoveHandler Move;
        public UseSkillHandler UseSkill;
    }

    public class BotManager
    {
        public readonly BotInput Input;
        public readonly SensoryData SensoryData;

        public readonly Bot[] Bots;
        private readonly AggressionPool m_AggressionPool;
        private readonly BotUpdateCallback[] m_BotUpdates;
        private readonly CachedRandom m_Random;

        public BotManager(
            float actionDistance,
            SkillConfigGroup skillConfigGroup,
            SensoryDataConfig sensoryDataConfig)
        {
            Input = new BotInput();
            SensoryData = new SensoryData(sensoryDataConfig);

            Bots = new Bot[sensoryDataConfig.MaxCharacterCount];
            m_AggressionPool = new AggressionPool(sensoryDataConfig.MaxCharacterCount);
            m_BotUpdates = new BotUpdateCallback[sensoryDataConfig.MaxCharacterCount];

            m_Random = new CachedRandom();
            for (int i = 0; i < Bots.Length; i++)
            {
                var (bot, botUpdate) = Bot.Create(
                    m_Random,
                    i,
                    Input,
                    m_AggressionPool,
                    actionDistance,
                    skillConfigGroup,
                    SensoryData);

                Bots[i] = bot;
                m_BotUpdates[i] = botUpdate;
            }

            ShuffleSkillLayout(1);
        }

        public void ResetData()
        {
            m_AggressionPool.Reset();
            SensoryData.Reset();

            // No skill layout reset
        }

        public void ShuffleSkillLayout(int tier)
        {
            int[] layoutIds = SkillLayout.GetLayoutIds(tier);
            Shuffle(layoutIds);

            // TODO: Get random can be improved with knowing which bots are in the game
            switch (tier)
            {
                case 1:
                    for (int i = 0; i < Bots.Length; i++)
                    {
                        int id = layoutIds[i];
                        Bots[i].SkillLayout = SkillLayout.GetTierOneLayout(id);
                    }

                    break;
                case 2:
                    for (int i = 0; i < Bots.Length; i++)
                    {
                        int id = layoutIds[i];
                        Bots[i].SkillLayout = SkillLayout.GetTierTwoLayout(id);
                    }

                    break;
                case 3:
                    for (int i = 0; i < Bots.Length; i++)
                    {
                        int id = layoutIds[i];
                        Bots[i].SkillLayout = SkillLayout.GetTierThreeLayout(id);
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }

            /*
            Console.WriteLine("=== SHUFFLE ===");
            for (int i = 0; i < Bots.Length; i++)
            {
                Console.WriteLine($"Picked: {Bots[i].SkillLayout.Id}");
            }
            */
        }

        private void Shuffle(int[] array)
        {
            int length = array.Length;
            for (int i = 0; i < (length - 1); i++)
            {
                int randomIndex = i + m_Random.Next(0, length - i);

                // ReSharper disable once SwapViaDeconstruction
                int value = array[randomIndex];
                array[randomIndex] = array[i];
                array[i] = value;
            }
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < m_BotUpdates.Length; i++)
                m_BotUpdates[i](deltaTime);

            m_AggressionPool.Update(deltaTime);
        }
    }
}