using System;
using System.Collections.Generic;

namespace Lili.SaveSystem.Handlers
{
    [Serializable]
    public class AlphabetLevelState
    {
        public int level;
    }

    public class AlphabetDataHandler : BaseDataHandler
    {
        private const string StateKey = "alphabet_level_state";

        public AlphabetDataHandler(string fileName) : base(fileName)
        {
        }

        public override Dictionary<string, object> ConvertData(Dictionary<string, object> data)
        {
            var converted = new Dictionary<string, object>();

            if (data.TryGetValue(StateKey, out var state))
            {
                converted.Add(StateKey, SaveDataUtils.ConvertObject<AlphabetLevelState>(state));
            }

            return converted;
        }

        protected override void SetupHandlerKeys()
        {
            HandlerKeys.Add(StateKey);
        }

        public int GetCurrentLevel()
        {
            if (!LoadedData.TryGetValue(StateKey, out var value)) return 0;
            var data = (AlphabetLevelState)value;
            return data.level;
        }

        public void SetNewLevel(int level)
        {
            LoadedData.TryAdd(StateKey, new AlphabetLevelState());
            var data = (AlphabetLevelState)LoadedData[StateKey];
            data.level = level;
            SaveData();
        }
    }
}