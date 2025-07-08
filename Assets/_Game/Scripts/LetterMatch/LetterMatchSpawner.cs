using System.Collections.Generic;
using LetterMatch;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace LetterMatch
{
    public class LetterMatchSpawner : MonoBehaviour
    {
        [TabGroup("Database")]
        public LetterPathDatabase letterDatabase;
        public ColorPalette colorPalette;
    
        [TabGroup("Settings")]
        public CanvasScaler canvasScaler;
        public Transform capitalParent;
        public Transform smallParent;
    
        [TabGroup("Level Configuration")]
        public LetterMatchLevelConfig currentLevelConfig;
    
        [TabGroup("Debug"), ReadOnly]
        public List<Vector2> capitalPositions = new List<Vector2>();
        public List<Vector2> smallPositions = new List<Vector2>();
        public Vector3 letterScale = Vector3.one;
    
        [TabGroup("Generated Lists"), ReadOnly]
        public List<LetterMatchController> capitalLetters = new List<LetterMatchController>();
        public List<LetterMatchController> smallLetters = new List<LetterMatchController>();
        public List<GameObject> capitalSpines = new List<GameObject>();
        public List<GameObject> smallSpines = new List<GameObject>();
    
        [TabGroup("Actions")]
        public string currentLevel = "ABC";
    
        private List<int> smallOrder = new List<int>();
    
        [TabGroup("Actions")]
        [Button("Spawn Letters", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        public void SpawnLetters(string levelId)
        {
            currentLevel = levelId;
            if (!ValidateReferences()) return;
            ClearAll();
            smallOrder.Clear(); // Reset order for new level
            CreateLetterObjects();
        }
    
        [Button("Spawn from Config", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        public void SpawnFromConfig()
        {
            if (currentLevelConfig == null)
            {
                Debug.LogError("LetterMatchSpawner: No level config assigned!");
                return;
            }
            currentLevel = currentLevelConfig.levelId;
            if (!ValidateReferences()) return;
            ClearAll();
            smallOrder.Clear(); // Reset order for new level
            CreateLetterObjects();
        }
    
        [TabGroup("Actions")]
        [Button("Clear All", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.6f)]
        public void ClearAll()
        {
            // Clear capital letters
            for (int i = 0; i < capitalLetters.Count; i++)
            {
                if (capitalLetters[i] != null)
                {
#if UNITY_EDITOR
                    DestroyImmediate(capitalLetters[i].gameObject);
#else
                Destroy(capitalLetters[i].gameObject);
#endif
                }
            }
        
            // Clear small letters
            for (int i = 0; i < smallLetters.Count; i++)
            {
                if (smallLetters[i] != null)
                {
#if UNITY_EDITOR
                    DestroyImmediate(smallLetters[i].gameObject);
#else
                Destroy(smallLetters[i].gameObject);
#endif
                }
            }
        
            capitalLetters.Clear();
            smallLetters.Clear();
            capitalSpines.Clear();
            smallSpines.Clear();
        }
    
        bool ValidateReferences()
        {
            if (letterDatabase == null || canvasScaler == null || capitalParent == null || smallParent == null)
            {
                Debug.LogError("LetterMatchSpawner: Missing required references!");
                return false;
            }
        
            if (currentLevelConfig == null)
            {
                Debug.LogError($"LetterMatchSpawner: No level config found for {currentLevel}");
                return false;
            }
        
            return true;
        }
    
        void CreateLetterObjects()
        {
            if (currentLevelConfig == null) return;

            // Create capital letters (in order)
            int capitalCount = Mathf.Min(currentLevelConfig.letters.Count, 3);
            for (int i = 0; i < capitalCount; i++)
            {
                var letterConfig = currentLevelConfig.letters[i];
                CreateCapitalLetter(letterConfig, i);
            }

            // Create small letters (randomized order, but persistent)
            int smallCount = Mathf.Min(currentLevelConfig.letters.Count, 3);
            if (smallOrder.Count != smallCount)
            {
                smallOrder = new List<int> { 0, 1, 2 };
                for (int i = 0; i < smallOrder.Count; i++)
                {
                    int swap = Random.Range(i, smallOrder.Count);
                    int tmp = smallOrder[i];
                    smallOrder[i] = smallOrder[swap];
                    smallOrder[swap] = tmp;
                }
            }
            for (int i = 0; i < smallCount; i++)
            {
                var letterConfig = currentLevelConfig.letters[smallOrder[i]];
                CreateSmallLetter(letterConfig, i);
            }
        }
    
        void CreateCapitalLetter(LetterConfig letterConfig, int index)
        {
            var capitalLetter = CreateLetterObject(letterConfig.capitalLetter, letterConfig, capitalParent, $"Capital_{letterConfig.capitalLetter}_{index}");
            if (capitalLetter != null)
            {
                capitalLetters.Add(capitalLetter);
                capitalLetter.SetLetterType(LetterType.Capital);
                capitalLetter.SetMatchData(new LetterMatchData(letterConfig.capitalLetter, letterConfig.capitalLetter, letterConfig.smallLetter, letterConfig.channelId, letterConfig.channelType));
            }
        }
        
        void CreateSmallLetter(LetterConfig letterConfig, int index)
        {
            var smallLetter = CreateLetterObject(letterConfig.smallLetter, letterConfig, smallParent, $"Small_{letterConfig.smallLetter}_{index}");
            if (smallLetter != null)
            {
                smallLetters.Add(smallLetter);
                smallLetter.SetLetterType(LetterType.Small);
                smallLetter.SetMatchData(new LetterMatchData(letterConfig.smallLetter, letterConfig.capitalLetter, letterConfig.smallLetter, letterConfig.channelId, letterConfig.channelType));
            }
        }
    
        LetterMatchController CreateLetterObject(string letterId, LetterConfig letterConfig, Transform parent, string objectName)
        {
            var letterData = letterDatabase.LoadLetterData(letterId);
            if (letterData == null) return null;
        
            var obj = new GameObject(objectName);
            obj.transform.SetParent(parent);
        
            var image = obj.AddComponent<Image>();
        
            // Use custom sprite if available, otherwise use database sprite
            if (letterConfig.useCustomSprite)
            {
                if (letterConfig.IsCapitalLetter(letterId) && letterConfig.customCapitalSprite != null)
                    image.sprite = letterConfig.customCapitalSprite;
                else if (letterConfig.IsSmallLetter(letterId) && letterConfig.customSmallSprite != null)
                    image.sprite = letterConfig.customSmallSprite;
                else
                    image.sprite = letterData.letterSprite;
            }
            else
            {
                image.sprite = letterData.letterSprite;
            }
        
            image.SetNativeSize();
        
            var rect = obj.GetComponent<RectTransform>();
            rect.localScale = letterScale * letterConfig.scale;
        
            var letterController = obj.AddComponent<LetterMatchController>();
            letterController.SetId(letterId);
            letterController.SetChannelId(letterConfig.channelId);
            letterController.SetChannelType(letterConfig.channelType);
            letterController.SetLetterConfig(letterConfig);
        
            // Create spine child
            var spineObj = CreateSpineObject(letterData, Vector2.zero, letterId, obj.transform);
            if (spineObj != null)
            {
                letterController.SetSpineChild(spineObj);
                if (parent == capitalParent)
                    capitalSpines.Add(spineObj);
                else
                    smallSpines.Add(spineObj);
            }
        
            return letterController;
        }
    
        GameObject CreateSpineObject(LetterData letterData, Vector2 position, string letterId, Transform parent)
        {
            if (letterData.letterSpinePrefab == null) return null;
        
            var spineObj = Instantiate(letterData.letterSpinePrefab, parent);
            spineObj.name = $"Spine_{letterId}";
        
            var rect = spineObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = position;
                rect.localScale = letterScale;
            }
        
            return spineObj;
        }
    
        public LetterMatchController GetLetterByChannel(string channelId)
        {
            // Search in both capital and small letters
            var capitalLetter = capitalLetters.Find(l => l.GetChannelId() == channelId);
            if (capitalLetter != null) return capitalLetter;
        
            var smallLetter = smallLetters.Find(l => l.GetChannelId() == channelId);
            return smallLetter;
        }
    
        public List<LetterMatchController> GetAllLetters()
        {
            var allLetters = new List<LetterMatchController>();
            allLetters.AddRange(capitalLetters);
            allLetters.AddRange(smallLetters);
            return allLetters;
        }
    
        public int GetTotalLetters()
        {
            // Return the number of letter pairs, not individual letters
            return capitalLetters.Count; // Since we have equal numbers of capitals and smalls
        }

        public int GetTotalLetterPairs()
        {
            return capitalLetters.Count; // This is the number of letter pairs
        }
    
        public LetterConfig GetLetterConfig(string letterId)
        {
            return currentLevelConfig?.GetLetterConfig(letterId);
        }
    }

    [System.Serializable]
    public class LevelLetterData
    {
        public string levelId;
        public List<LetterInfo> letters = new List<LetterInfo>();
    }

    [System.Serializable]
    public class LetterInfo
    {
        public string capitalLetter;
        public string smallLetter;
        public string channelId;
        public MusicChannelType channelType;
    }

    public enum LetterType
    {
        Capital,
        Small
    }
}