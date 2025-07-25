using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private IGameData gameData;

    public IGameData GameData => gameData;
    
    [SerializeField] private Transform widthParent;
    [SerializeField] private Transform heightParent;
    
    [Header("Current State")]
    [ReadOnly] public int currentIndex = 0;
    [ReadOnly] public GameObject currentGameObject;
    [ReadOnly] public IGameLevel currentGameLevel;
    
    [Header("UI References")]
    public GameObject levelSelectCanvas;

    [Header("Transition Settings")]
    [Tooltip("Seconds to wait after a level completes before loading the next level.")]
    [MinValue(0)]
    public float levelTransitionDelay = 1.5f;
    


    
    
    private void CreateCurrentLevel()
    {
        if (gameData.GameLevels == null || gameData.GameLevels.Count == 0)
        {
            Debug.LogError("No game levels found!");
            return;
        }
        
        if (currentIndex >= gameData.GameLevels.Count)
        {
            Debug.Log("All levels completed!");
            ShowLevelSelectCanvas();
            return;
        }
        
        // Mevcut level'i temizle
        DestroyCurrentLevel();
        
        // Yeni level'i yarat
        var gameClass = gameData.GameLevels[currentIndex];
        Transform parent = gameClass.canvasType == CanvasType.Width ? widthParent : heightParent;
        
        if (parent == null)
        {
            Debug.LogError($"No parent object assigned for {gameClass.canvasType}!");
            return;
        }
        
        currentGameObject = Instantiate(gameClass.gameObject, parent);
        currentGameLevel = currentGameObject.GetComponent<IGameLevel>();
        if (currentGameLevel == null)
        {
            // Fallback: search in children (inactive included)
            currentGameLevel = currentGameObject.GetComponentInChildren<IGameLevel>(true);
        }
        
        if (currentGameLevel != null)
        {
            currentGameLevel.OnGameComplete += OnGameComplete;
            currentGameLevel.StartGame();
            
            Debug.Log($"Level {currentIndex} created: {currentGameLevel.LevelName}");
            Debug.Log($"Event subscribed for: {currentGameLevel.GetType().Name}");
        }
        else
        {
            Debug.LogError($"Level prefab doesn't have IGameLevel component!");
        }
    }
    
    private void OnGameComplete()
    {
        StartCoroutine(HandleLevelCompleteAfterDelay());
    }

    private IEnumerator HandleLevelCompleteAfterDelay()
    {
        Debug.Log($"Game completed! Waiting {levelTransitionDelay:F1}s before next level...");

        // Unsubscribe immediately to avoid duplicate calls during delay
        if (currentGameLevel != null)
        {
            currentGameLevel.OnGameComplete -= OnGameComplete;
        }

        yield return new WaitForSeconds(levelTransitionDelay);

        // Destroy current level and advance
        DestroyCurrentLevel();
        currentIndex++;
        CreateCurrentLevel();
    }
    
    private void DestroyCurrentLevel()
    {
        if (currentGameObject != null)
        {
            // Eğer WordGameManager (Singleton) ise reset et
            if (currentGameObject.GetComponent<WordGameManager>() != null)
            {
                WordGameManager.ResetInstance();
                return; // ResetInstance zaten destroy ediyor
            }
            
            Destroy(currentGameObject);
            currentGameObject = null;
            currentGameLevel = null;
        }
    }
    
    [Button("Set Game Data")]
    public void SetGameData(IGameData newGameData)
    {
        gameData = newGameData;
        CreateCurrentLevel();
    }
    
    public void StartLevel(IGameData data, GameObject canvas)
    {
        gameData = data;
        levelSelectCanvas = canvas;
        currentIndex = 0;
        CreateCurrentLevel();
    }
    
    public void StartLevel(AlfabeModuleData data, GameObject canvas)
    {
        gameData = data;
        levelSelectCanvas = canvas;
        currentIndex = 0;
        CreateCurrentLevel();
    }
    
    public void StartLevel(NumberModuleData data, GameObject canvas)
    {
        gameData = data;
        levelSelectCanvas = canvas;
        currentIndex = 0;
        CreateCurrentLevel();
    }
    
    private void ShowLevelSelectCanvas()
    {
        MainMenu.Instance.ModuleMenuSetActive(true);
    }
    
    
    private void OnDestroy()
    {
        if (currentGameLevel != null)
        {
            currentGameLevel.OnGameComplete -= OnGameComplete;
        }
    }

    public AlfabeModuleData GetAlfabeModuleData(){
        if(GameData is AlfabeModuleData alfabeModuleData){
            return alfabeModuleData;
        }
        return null;
    }

    public NumberModuleData GetNumberModuleData(){
        if(GameData is NumberModuleData numberModuleData){
            return numberModuleData;
        }
        return null;
    }


} 