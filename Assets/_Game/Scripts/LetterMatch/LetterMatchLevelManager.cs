using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LetterMatch
{
    public class LetterMatchLevelManager : MonoBehaviour
    {
        [TabGroup("Level Configuration")]
        public LetterMatchLevelConfig currentLevelConfig;
    
        [TabGroup("Level Library")]
        public List<LetterMatchLevelConfig> availableLevels = new List<LetterMatchLevelConfig>();
    
        [TabGroup("Debug"), ReadOnly]
        public string currentLevelId = "";
        public bool isLevelLoaded = false;
        public int currentLevelIndex = 0;
    
        private LetterMatchGameManager gameManager;
        private LetterMatchSpawner spawner;
        private KoreographerAudioManager audioManager;
    
        void Start()
        {
            InitializeLevelManager();
        }
    
        void InitializeLevelManager()
        {
            gameManager = LetterMatchGameManager.Instance;
            spawner = gameManager?.letterSpawner;
            audioManager = gameManager?.audioManager;
        
            if (currentLevelConfig != null)
            {
                LoadLevel(currentLevelConfig);
            }
        }
    
        [Button("Load Level", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        public void LoadLevel(LetterMatchLevelConfig levelConfig)
        {
            if (levelConfig == null)
            {
                Debug.LogError("LetterMatchLevelManager: No level config provided!");
                return;
            }
        
            currentLevelConfig = levelConfig;
            currentLevelId = levelConfig.levelId;
            isLevelLoaded = true;
        
            if (gameManager != null)
            {
                gameManager.currentLevel = levelConfig.levelId;
            }
        
            if (spawner != null)
            {
                spawner.currentLevelConfig = levelConfig;
            }
        
            if (audioManager != null)
            {
               // audioManager.LoadLevelMusic(levelConfig.levelId);
            }
        
            Debug.Log($"Loaded level: {levelConfig.levelName} ({levelConfig.levelId})");
        }
    
        [Button("Load Level by ID", ButtonSizes.Medium), GUIColor(0.8f, 1f, 0.4f)]
        public void LoadLevelById(string levelId)
        {
            var levelConfig = availableLevels.Find(l => l.levelId == levelId);
            if (levelConfig != null)
            {
                LoadLevel(levelConfig);
            }
            else
            {
                Debug.LogError($"LetterMatchLevelManager: Level with ID '{levelId}' not found!");
            }
        }
    
        [Button("Load Next Level", ButtonSizes.Medium), GUIColor(1f, 0.8f, 0.4f)]
        public void LoadNextLevel()
        {
            if (availableLevels.Count == 0) return;
        
            currentLevelIndex = (currentLevelIndex + 1) % availableLevels.Count;
            LoadLevel(availableLevels[currentLevelIndex]);
        }
    
        [Button("Load Previous Level", ButtonSizes.Medium), GUIColor(1f, 0.8f, 0.4f)]
        public void LoadPreviousLevel()
        {
            if (availableLevels.Count == 0) return;
        
            currentLevelIndex = (currentLevelIndex - 1 + availableLevels.Count) % availableLevels.Count;
            LoadLevel(availableLevels[currentLevelIndex]);
        }
    
        [Button("Reload Current Level", ButtonSizes.Medium), GUIColor(0.6f, 0.6f, 1f)]
        public void ReloadCurrentLevel()
        {
            if (currentLevelConfig != null)
            {
                LoadLevel(currentLevelConfig);
            }
        }
    
        public LetterMatchLevelConfig GetCurrentLevelConfig()
        {
            return currentLevelConfig;
        }
    
        public LetterConfig GetLetterConfig(string letterId)
        {
            return currentLevelConfig?.GetLetterConfig(letterId);
        }

    
        public bool IsLevelLoaded()
        {
            return isLevelLoaded && currentLevelConfig != null;
        }
    
        public string GetCurrentLevelId()
        {
            return currentLevelId;
        }
    
        public string GetCurrentLevelName()
        {
            return currentLevelConfig?.levelName ?? "No Level";
        }
    
        public int GetTotalLevels()
        {
            return availableLevels.Count;
        }
    
        public int GetCurrentLevelIndex()
        {
            return currentLevelIndex;
        }
    
        [Button("Validate Level Config", ButtonSizes.Small), GUIColor(1f, 1f, 0.6f)]
        public void ValidateCurrentLevelConfig()
        {
            if (currentLevelConfig == null)
            {
                Debug.LogError("No level config to validate!");
                return;
            }
        
            var errors = new List<string>();
        
            if (currentLevelConfig.letters.Count == 0)
            {
                errors.Add("No letters configured");
            }
        
            foreach (var letter in currentLevelConfig.letters)
            {
                if (string.IsNullOrEmpty(letter.capitalLetter) || string.IsNullOrEmpty(letter.smallLetter))
                {
                    errors.Add($"Letter {letter.letterId} has missing capital or small letter");
                }
            
                if (string.IsNullOrEmpty(letter.channelId))
                {
                    errors.Add($"Letter {letter.letterId} has no channel ID");
                }
            }
        
            if (errors.Count > 0)
            {
                Debug.LogError($"Level config validation failed:\n{string.Join("\n", errors)}");
            }
            else
            {
                Debug.Log("Level config validation passed!");
            }
        }
    }
} 