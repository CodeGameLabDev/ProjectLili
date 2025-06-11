using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TracingModule : MonoBehaviour
{
    [Header("Letter System")]
    [SerializeField] private string[] letterNames = new string[] { 
        "A", "B", "C", "Ç", "D", "E", "Ə", "F", "G", "Ğ", "H", "X", "I", "İ", "J", "K", "Q", "L", "M", "N", "O", "Ö", "P", "R", "S", "Ş", "T", "U", "Ü", "V", "Y", "Z"
    };
    private int currentLetterIndex = 0;
    private bool hasStarted = false;
    
    [Header("Components")]
    public FollowerPen followerPen;
    
    [Header("Debug Info")]
    [ReadOnly] public string currentLetter;
    [ReadOnly] public int debugCurrentLetterIndex;
    [ReadOnly] public GameObject letterObject;

    private const string LETTER_PROGRESS_KEY = "CurrentLetterIndex";

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
        Debug.Log($"[TracingModule] StartTracing called - hasStarted: {hasStarted}, currentLetterIndex: {currentLetterIndex}");
        
        if (!hasStarted)
        {
            Debug.Log("[TracingModule] Start() henüz çalışmamış, LoadProgress() manuel olarak çağrılıyor...");
            LoadProgress();
            UpdateCurrentLetterDisplay();
            hasStarted = true;
        }
        
        if (letterObject != null)
        {
            DestroyImmediate(letterObject);
        }

        LoadCurrentLetter();
    }

    private void LoadCurrentLetter()
    {
        Debug.Log($"[TracingModule] LoadCurrentLetter - currentLetterIndex: {currentLetterIndex}, letterNames.Length: {letterNames.Length}");
        
        if (currentLetterIndex >= letterNames.Length)
        {
            Debug.Log("Tüm harfler tamamlandı! Başa dönüyor...");
            currentLetterIndex = 0;
            SaveProgress();
        }

        string letterName = letterNames[currentLetterIndex];
        string resourcePath = $"Letters/Letter_{letterName}";

        Debug.Log($"[TracingModule] Yüklenecek harf: {letterName}, Resource Path: {resourcePath}");

        GameObject letterPrefab = Resources.Load<GameObject>(resourcePath);
        
        if (letterPrefab == null)
        {
            Debug.LogError($"Harf prefab'ı bulunamadı: {resourcePath}. Resources/Letters/ klasöründe Letter_{letterName}.prefab dosyası olduğundan emin olun.");
            return;
        }

        letterObject = Instantiate(letterPrefab, transform);
        TracingController tracingController = letterObject.GetComponent<TracingController>();
        
        if (tracingController != null)
        {
            tracingController.followerPen = followerPen;
            tracingController.onAllSegmentsCompleted.AddListener(OnLetterCompleted);
        }
        else
        {
            Debug.LogError($"TracingController bulunamadı! Letter_{letterName} prefab'ının TracingController component'ine sahip olduğundan emin olun.");
        }

        UpdateCurrentLetterDisplay();
        Debug.Log($"Harf yüklendi: {letterName} (İndeks: {currentLetterIndex})");
    }

    private void OnLetterCompleted()
    {
        Debug.Log($"Harf tamamlandı: {letterNames[currentLetterIndex]}");
        
        currentLetterIndex++;
        SaveProgress();
        UpdateCurrentLetterDisplay();
        
        if (currentLetterIndex < letterNames.Length)
        {
            Debug.Log($"Bir sonraki harf hazır: {letterNames[currentLetterIndex]}");
        }
        else
        {
            Debug.Log("Tüm harfler tamamlandı! Tebrikler!");
        }
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(LETTER_PROGRESS_KEY, currentLetterIndex);
        PlayerPrefs.Save();
        Debug.Log($"İlerleme kaydedildi: {currentLetterIndex}");
    }

    private void LoadProgress()
    {
        currentLetterIndex = PlayerPrefs.GetInt(LETTER_PROGRESS_KEY, 0);
        Debug.Log($"İlerleme yüklendi: {currentLetterIndex}");
    }

    private void UpdateCurrentLetterDisplay()
    {
        if (currentLetterIndex < letterNames.Length)
        {
            currentLetter = letterNames[currentLetterIndex];
        }
        else
        {
            currentLetter = "Tamamlandı";
        }
        debugCurrentLetterIndex = currentLetterIndex;
        Debug.Log($"[TracingModule] UpdateCurrentLetterDisplay - currentLetter: {currentLetter}, currentLetterIndex: {currentLetterIndex}");
    }

    [Button("Reset Progress")]
    public void ResetProgress()
    {
        currentLetterIndex = 0;
        SaveProgress();
        UpdateCurrentLetterDisplay();
        Debug.Log("İlerleme sıfırlandı!");
    }

    [Button("Skip Current Letter")]
    public void SkipCurrentLetter()
    {
        OnLetterCompleted();
    }

    public int GetCurrentLetterIndex()
    {
        return currentLetterIndex;
    }

    public string GetCurrentLetterName()
    {
        return currentLetterIndex < letterNames.Length ? letterNames[currentLetterIndex] : "Tamamlandı";
    }
}
