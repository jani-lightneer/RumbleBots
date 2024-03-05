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

            var random = new CachedRandom();
            for (int i = 0; i < Bots.Length; i++)
            {
                var (bot, botUpdate) = Bot.Create(
                    random,
                    i,
                    Input,
                    m_AggressionPool,
                    actionDistance,
                    skillConfigGroup,
                    SensoryData);

                Bots[i] = bot;
                m_BotUpdates[i] = botUpdate;
            }
        }

        public void ResetData()
        {
            m_AggressionPool.Reset();
            SensoryData.Reset();
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < m_BotUpdates.Length; i++)
                m_BotUpdates[i](deltaTime);

            m_AggressionPool.Update(deltaTime);
        }
    }
}