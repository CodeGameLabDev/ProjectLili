using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "GameLevelData", menuName = "Game/Game Level Data")]
public class GameLevelData : ScriptableObject
{
    [TabGroup("Game Settings")]
    [SerializeField] private string gameName;
    
    [TabGroup("Game Settings")]
    [SerializeField] private string gameDescription;
    
    [TabGroup("Game Settings")]
    [SerializeField] private Sprite gameIcon;
    
    [TabGroup("Levels")]
    [SerializeField] private List<GameObject> gameLevels = new List<GameObject>();
    
    [TabGroup("Game Data")]
    [SerializeField] private ScriptableObject gameData;
    
    [TabGroup("Debug")]
    [ReadOnly] public int currentLevelIndex = 0;
    
    [TabGroup("Debug")]
    [ReadOnly] public int totalLevels;
    
    // Properties
    public string GameName => gameName;
    public string GameDescription => gameDescription;
    public Sprite GameIcon => gameIcon;
    public List<GameObject> GameLevels => gameLevels;
    public int TotalLevels => gameLevels.Count;
    public int CurrentLevelIndex => currentLevelIndex;
    
    // Generic method to get game data with casting
    public T GetGameData<T>() where T : class, IGameData
    {
        if (gameData is T data)
        {
            return data;
        }
        
        Debug.LogError($"Game data cannot be cast to {typeof(T).Name}. Current type: {gameData?.GetType().Name}");
        return null;
    }
    
    // Get current level
    public GameObject GetCurrentLevel()
    {
        if (currentLevelIndex >= 0 && currentLevelIndex < gameLevels.Count)
        {
            return gameLevels[currentLevelIndex];
        }
        return null;
    }
    
    // Get level by index
    public GameObject GetLevel(int index)
    {
        if (index >= 0 && index < gameLevels.Count)
        {
            return gameLevels[index];
        }
        return null;
    }
    
    // Navigation methods
    public bool MoveToNextLevel()
    {
        if (currentLevelIndex < gameLevels.Count - 1)
        {
            currentLevelIndex++;
            return true;
        }
        return false;
    }
    
    public bool MoveToPreviousLevel()
    {
        if (currentLevelIndex > 0)
        {
            currentLevelIndex--;
            return true;
        }
        return false;
    }
    
    public void ResetToFirstLevel()
    {
        currentLevelIndex = 0;
    }
    
    public void SetCurrentLevel(int index)
    {
        if (index >= 0 && index < gameLevels.Count)
        {
            currentLevelIndex = index;
        }
    }
    
    // Validation
    private void OnValidate()
    {
        totalLevels = gameLevels.Count;
        
        // Validate that all game objects have IGameLevel component
        for (int i = 0; i < gameLevels.Count; i++)
        {
            if (gameLevels[i] != null)
            {
                var gameLevel = gameLevels[i].GetComponent<IGameLevel>();
                if (gameLevel == null)
                {
                    Debug.LogWarning($"Level {i} ({gameLevels[i].name}) does not have IGameLevel component!");
                }
            }
        }
        
        // Validate game data
        if (gameData != null && !(gameData is IGameData))
        {
            Debug.LogWarning($"Game data ({gameData.name}) does not implement IGameData interface!");
        }
    }
    
    [Button("Initialize Game Data")]
    public void InitializeGameData()
    {
        var data = gameData as IGameData;
        data?.InitializeGameData();
    }
} 