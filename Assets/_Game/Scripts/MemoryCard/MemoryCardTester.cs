using UnityEngine;
using UnityEngine.UI;

public class MemoryCardTester : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private MemoryCardManager memoryCardManager;
    [SerializeField] private Button testNumbersButton;
    [SerializeField] private Button testAnimalsButton;
    [SerializeField] private Button testObjectsButton;
    [SerializeField] private Button resetButton;
    
    [Header("Test Results")]
    [SerializeField] private Text resultText;
    
    private void Start()
    {
        if (testNumbersButton != null)
            testNumbersButton.onClick.AddListener(TestNumbersMode);
            
        if (testAnimalsButton != null)
            testAnimalsButton.onClick.AddListener(TestAnimalsMode);
            
        if (testObjectsButton != null)
            testObjectsButton.onClick.AddListener(TestObjectsMode);
            

            
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetTest);
            
        if (memoryCardManager != null)
        {
            memoryCardManager.OnGameStart += OnGameStart;
            memoryCardManager.OnGameComplete += OnGameComplete;
        }
    }
    
    private void TestNumbersMode()
    {
        if (memoryCardManager != null)
        {
            memoryCardManager.gameMode = MemoryCardManager.GameMode.Numbers;
            memoryCardManager.numberOfPairs = 4;
            memoryCardManager.StartGame();
            LogResult("Numbers Mode Test Started - 4 pairs");
        }
    }
    
    private void TestAnimalsMode()
    {
        if (memoryCardManager != null)
        {
            memoryCardManager.gameMode = MemoryCardManager.GameMode.Animals;
            memoryCardManager.numberOfPairs = 3;
            memoryCardManager.StartGame();
            LogResult("Animals Mode Test Started - 3 pairs");
        }
    }
    
    private void TestObjectsMode()
    {
        if (memoryCardManager != null)
        {
            memoryCardManager.gameMode = MemoryCardManager.GameMode.Objects;
            memoryCardManager.numberOfPairs = 3;
            memoryCardManager.StartGame();
            LogResult("Objects Mode Test Started - 3 pairs");
        }
    }
    

    
    private void ResetTest()
    {
        if (memoryCardManager != null)
        {
            memoryCardManager.CompleteGame();
            LogResult("Test Reset");
        }
    }
    
    private void OnGameStart()
    {
        LogResult("Game Started Successfully!");
    }
    
    private void OnGameComplete()
    {
        LogResult("Game Completed Successfully!");
    }
    
    private void LogResult(string message)
    {
        Debug.Log($"[MemoryCardTester] {message}");
        if (resultText != null)
        {
            resultText.text = $"[{System.DateTime.Now:HH:mm:ss}] {message}\n{resultText.text}";
        }
    }
    
    private void OnDestroy()
    {
        if (memoryCardManager != null)
        {
            memoryCardManager.OnGameStart -= OnGameStart;
            memoryCardManager.OnGameComplete -= OnGameComplete;
        }
    }
} 