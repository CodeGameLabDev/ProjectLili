using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

public class TracingModule : MonoBehaviour
{
    [Header("Letter System")]
    [SerializeField] private string[] uppercaseLetterNames = new string[] { 
        "A", "B", "C", "Ç", "D", "E", "Ə", "F", "G", "Ğ", "H", "X", "I", "İ", "J", "K", "Q", "L", "M", "N", "O", "Ö", "P", "R", "S", "Ş", "T", "U", "Ü", "V", "Y", "Z","W"
    };
    [SerializeField] private string[] lowercaseLetterNames = new string[] { 
        "a", "b", "c", "ç", "d", "e", "ə", "f", "g", "ğ", "h", "x", "ı", "i", "j", "k", "q", "l", "m", "n", "o", "ö", "p", "r", "s", "ş", "t", "u", "ü", "v", "y", "z","w"
    };
    private int currentLetterIndex = 0;
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
        Debug.Log($"[TracingModule] Start - currentLetterIndex (before LoadProgress): {currentLetterIndex}");
        LoadProgress();
        Debug.Log($"[TracingModule] Start - currentLetterIndex (after LoadProgress): {currentLetterIndex}");
        UpdateCurrentLetterDisplay();
        hasStarted = true;
    }

    [Button("Start Tracing")]
    public void StartTracing()
    {
        if (isLevelCompleted)
        {
            // Level tamamlandıysa bir sonraki harfe geç
            currentLetterIndex++;
            
            // Eğer son harfe ulaştıysak başa dön
            if (currentLetterIndex >= uppercaseLetterNames.Length)
            {
                currentLetterIndex = 0;
                Debug.Log("Alfabe tamamlandı! Başa dönülüyor...");
            }
            
            isUppercase = true;
            isLevelCompleted = false;
            debugIsLevelCompleted = false;
            SaveProgress();
            UpdateCurrentLetterDisplay();
            Debug.Log($"Yeni harf yükleniyor: {uppercaseLetterNames[currentLetterIndex]}");
        }

        Debug.Log($"[TracingModule] StartTracing called - hasStarted: {hasStarted}, currentLetterIndex: {currentLetterIndex}");
        
        if (!hasStarted)
        {
            Debug.Log("[TracingModule] Start() henüz çalışmamış, LoadProgress() manuel olarak çağrılıyor...");
            LoadProgress();
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
        Debug.Log($"[TracingModule] LoadCurrentLetter - currentLetterIndex: {currentLetterIndex}, isUppercase: {isUppercase}");
        
        if (currentLetterIndex >= uppercaseLetterNames.Length)
        {
            Debug.Log("Tüm harfler tamamlandı! Başa dönüyor...");
            currentLetterIndex = 0;
            isUppercase = true;
            SaveProgress();
        }

        string letterName = isUppercase ? uppercaseLetterNames[currentLetterIndex] : lowercaseLetterNames[currentLetterIndex];
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
            currentLetterObject.transform.position = centerLetterPosition.position;
        }
        else
        {
            currentLetterObject.transform.position = rightLetterPosition.position;
        }
        
        UpdateCurrentLetterDisplay();
        Debug.Log($"Harf yüklendi: {letterName} (İndeks: {currentLetterIndex}, Büyük/Küçük: {isUppercase})");
    }

    private void OnLetterCompleted()
    {
        Debug.Log($"Harf tamamlandı: {(isUppercase ? uppercaseLetterNames[currentLetterIndex] : lowercaseLetterNames[currentLetterIndex])}");
        
        if (isUppercase)
        {
            // Animate uppercase letter to the left
            currentLetterObject.transform.DOMove(leftLetterPosition.position, letterTransitionDuration)
                .SetEase(letterTransitionEase)
                .OnComplete(() => {
                    isUppercase = false;
                    LoadCurrentLetter();
                });
        }
        else
        {
            isLevelCompleted = true;
            debugIsLevelCompleted = true;
            Debug.Log("Level tamamlandı! Her iki harf de boyandı.");
        }
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(LETTER_PROGRESS_KEY, currentLetterIndex);
        PlayerPrefs.SetInt(LETTER_CASE_KEY, isUppercase ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"İlerleme kaydedildi: {currentLetterIndex}, Büyük/Küçük: {isUppercase}");
    }

    private void LoadProgress()
    {
        currentLetterIndex = PlayerPrefs.GetInt(LETTER_PROGRESS_KEY, 0);
        isUppercase = PlayerPrefs.GetInt(LETTER_CASE_KEY, 1) == 1;
        isLevelCompleted = false;
        debugIsLevelCompleted = false;
        Debug.Log($"İlerleme yüklendi: {currentLetterIndex}, Büyük/Küçük: {isUppercase}");
    }

    private void UpdateCurrentLetterDisplay()
    {
        if (currentLetterIndex < uppercaseLetterNames.Length)
        {
            currentLetter = isUppercase ? uppercaseLetterNames[currentLetterIndex] : lowercaseLetterNames[currentLetterIndex];
        }
        else
        {
            currentLetter = "Tamamlandı";
        }
        debugCurrentLetterIndex = currentLetterIndex;
        debugIsUppercase = isUppercase;
        Debug.Log($"[TracingModule] UpdateCurrentLetterDisplay - currentLetter: {currentLetter}, currentLetterIndex: {currentLetterIndex}, isUppercase: {isUppercase}");
    }

    [Button("Reset Progress")]
    public void ResetProgress()
    {
        currentLetterIndex = 0;
        isUppercase = true;
        isLevelCompleted = false;
        debugIsLevelCompleted = false;
        SaveProgress();
        UpdateCurrentLetterDisplay();
        Debug.Log("İlerleme sıfırlandı!");
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

    public int GetCurrentLetterIndex()
    {
        return currentLetterIndex;
    }

    public string GetCurrentLetterName()
    {
        if (currentLetterIndex >= uppercaseLetterNames.Length)
            return "Tamamlandı";
            
        return isUppercase ? uppercaseLetterNames[currentLetterIndex] : lowercaseLetterNames[currentLetterIndex];
    }
}
