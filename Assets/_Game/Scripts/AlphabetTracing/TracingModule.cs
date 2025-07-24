using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using System;
using Unity.VisualScripting;

public class TracingModule : MonoBehaviour, IGameLevel
{
    // IGameLevel implementation
    public event Action OnGameStart;
    public event Action OnGameComplete;
    public bool IsCompleted => isLevelCompleted;
    public string LevelName => "Tracing Game";
    AlfabeModuleData alfabeModuleData;
    NumberModuleData numberModuleData;
    private bool isNumberMode = false; // true when tracing a number rather than a letter
    public void StartGame()
    {
        OnGameStart?.Invoke();
        alfabeModuleData = GameManager.Instance.GetAlfabeModuleData();
        if(alfabeModuleData == null){
            numberModuleData = GameManager.Instance.GetNumberModuleData();
            isNumberMode = numberModuleData != null;
        }
        else
        {
            isNumberMode = false;
        }

        StartTracing();
    }

    public string GetWord(){
        string letterName = "";
        if(alfabeModuleData != null){
            letterName = isUppercase ? alfabeModuleData.UpperCaseLetter.letter.ToString() : alfabeModuleData.LowerCaseLetter.letter.ToString();
        }
        else if(numberModuleData != null){
            letterName = numberModuleData.NumberData.letter.ToString();
        }

        return letterName;


    }
    
    public void CompleteGame()
    {
        if (!isLevelCompleted)
        {
            isLevelCompleted = true;
            debugIsLevelCompleted = true;
            Debug.Log($"[TracingModule] CompleteGame() - OnGameComplete subscribers: {OnGameComplete?.GetInvocationList()?.Length ?? 0}");
            OnGameComplete?.Invoke();
        }
    }
    
    [Header("Letter System")]
    
    private bool hasStarted = false;
    private bool isUppercase = true;
    private bool isLevelCompleted = false;
    
    [Header("Components")]
    public FollowerPen followerPen;
    
    [Header("Letter Positions")]
    [SerializeField] private Transform centerLetterPosition;
    [SerializeField] private Transform leftLetterPosition;
    [SerializeField] private Transform rightLetterPosition;
    
    [Header("Animation Settings")]
    [SerializeField] private float letterTransitionDuration = 0.5f;
    [SerializeField] private Ease letterTransitionEase = Ease.InOutQuad;
    
    [Header("Debug Info")]
    [ReadOnly] public string currentLetter;
    [ReadOnly] public int debugCurrentLetterIndex;
    [ReadOnly] public GameObject currentLetterObject;
    [ReadOnly] public bool debugIsUppercase;
    [ReadOnly] public bool debugIsLevelCompleted;

    private const string LETTER_PROGRESS_KEY = "CurrentLetterIndex";
    private const string LETTER_CASE_KEY = "IsUppercase";

    void Start()
    {
        UpdateCurrentLetterDisplay();
        hasStarted = true;
    }

    [Button("Start Tracing")]
    public void StartTracing()
    {
        if (isLevelCompleted)
        {
            isUppercase = true;
            isLevelCompleted = false;
            UpdateCurrentLetterDisplay();     
        }

        if (!hasStarted)
        {
            Debug.Log("[TracingModule] Start() henüz çalışmamış, LoadProgress() manuel olarak çağrılıyor...");
            UpdateCurrentLetterDisplay();
            hasStarted = true;
        }
        
        // Tüm mevcut harfleri yok et
        if (currentLetterObject != null)
        {
            Destroy(currentLetterObject);
            currentLetterObject = null;
        }

        // Sahnedeki tüm TracingController'ları bul ve yok et
        TracingController[] existingLetters = FindObjectsOfType<TracingController>();
        foreach (TracingController letter in existingLetters)
        {
            if (letter.gameObject != currentLetterObject)
            {
                Destroy(letter.gameObject);
            }
        }

        LoadCurrentLetter();
    }

    private void LoadCurrentLetter()
    {
        
        string letterName = GetWord();
        string resourcePath = $"Letters/{(isUppercase ? "Letter_" : "small/Letter_")}{letterName}";

        Debug.Log($"[TracingModule] Yüklenecek harf: {letterName}, Resource Path: {resourcePath}");

        GameObject letterPrefab = Resources.Load<GameObject>(resourcePath);
        
        if (letterPrefab == null)
        {
            Debug.LogError($"Harf prefab'ı bulunamadı: {resourcePath}. Resources/Letters/ klasöründe Letter_{letterName}.prefab dosyası olduğundan emin olun.");
            return; 
        }

        currentLetterObject = Instantiate(letterPrefab, transform);
        TracingController tracingController = currentLetterObject.GetComponent<TracingController>();
        
        if (tracingController != null)
        {
            tracingController.followerPen = followerPen;
            tracingController.onAllSegmentsCompleted.AddListener(OnLetterCompleted);
        }
        else
        {
            Debug.LogError($"TracingController bulunamadı! Letter_{letterName} prefab'ının TracingController component'ine sahip olduğundan emin olun.");
        }

        // Position the letter
        if (isUppercase)
        {
            if (centerLetterPosition != null)
                currentLetterObject.transform.position = centerLetterPosition.position;
        }
        else
        {
            if (rightLetterPosition != null)
                currentLetterObject.transform.position = rightLetterPosition.position;
        }
        
        UpdateCurrentLetterDisplay();
    }

    private void OnLetterCompleted()
    {
        // If this level is a number, there is no lowercase/uppercase distinction.
        if (isNumberMode)
        {
            Debug.Log("Sayı tamamlandı! Level bitiyor.");
            CompleteGame();
            return;
        }
        
        if (isUppercase)
        {
            // Animate uppercase letter to the left
            if (leftLetterPosition != null)
            {
                currentLetterObject.transform.DOMove(leftLetterPosition.position, letterTransitionDuration)
                    .SetEase(letterTransitionEase)
                    .OnComplete(() => {
                        isUppercase = false;
                        LoadCurrentLetter();
                    });
            }
            else
            {
                isUppercase = false;
                LoadCurrentLetter();
            }
        }
        else
        {
            Debug.Log("Level tamamlandı! Her iki harf de boyandı.");
            CompleteGame();
        }
    }

    private void UpdateCurrentLetterDisplay()
    {
        currentLetter = GetWord();

        debugIsUppercase = isUppercase;
    }


    [Button("Skip Current Letter")]
    public void SkipCurrentLetter()
    {
        if (isLevelCompleted)
        {
            Debug.Log("Level tamamlandı! Yeni level başlatmak için Reset Progress'e basın.");
            return;
        }
        OnLetterCompleted();
    }


    public string GetCurrentLetterName()
    {
        return GetWord();
    }
}
