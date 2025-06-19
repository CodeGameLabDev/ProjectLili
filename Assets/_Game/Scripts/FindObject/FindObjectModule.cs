using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sirenix.OdinInspector;

// Ana oyun mantığını yöneten class
public class FindObjectModule : MonoBehaviour
{
    [Header("Level System")]
    [SerializeField] private string[] levelPrefabNames = new string[] { 
        "Level_1", "Level_2", "Level_3", "Level_4", "Level_5", "Level_6", "Level_7", "Level_8", "Level_9", "Level_10"
    };
    private int currentLevelIndex = 0;
    private bool hasStarted = false;

    [Header("Level Components")]
    [SerializeField] private GameObject LevelParent;
    
    [Header("Progress Bar")]
    public Image progressBarImage;
    
    [Header("Animation Settings")]
    public float moveToUITime = 0.5f;
    public float shakeIntensity = 0.1f;
    public float shakeSpeed = 5f;
    public float idleTimeBeforeShake = 3f;
    
    [Header("Debug Settings")]
    public bool showDebugMessages = true;
    
    [Header("Debug Info")]
    [ReadOnly] public string currentLevelName;
    [ReadOnly] public int debugCurrentLevelIndex;
    
    private FindObjectAssetHolder assetHolder;
    private Dictionary<GameObject, FindObject> objectMap;
    private Dictionary<GameObject, Vector3> originalPositions;
    private Dictionary<GameObject, Transform> originalParents;
    private Dictionary<GameObject, Coroutine> shakeCoroutines;
    private float lastInteractionTime;
    private int foundObjectsCount = 0;

    private const string LEVEL_PROGRESS_KEY = "CurrentLevelIndex";
    
    void Start()
    {
        Debug.Log($"[FindObjectModule] Start - currentLevelIndex (before LoadProgress): {currentLevelIndex}");
        LoadProgress();
        Debug.Log($"[FindObjectModule] Start - currentLevelIndex (after LoadProgress): {currentLevelIndex}");
        UpdateCurrentLevelDisplay();
        hasStarted = true;
    }
    
    void Update()
    {
        // Belirli süre hiçbir şey bulunmazsa objeleri sallat
        if (Time.time - lastInteractionTime > idleTimeBeforeShake)
        {
            StartShakingUnfoundObjects();
            lastInteractionTime = Time.time; // Sürekli sallanmasını önlemek için
        }
    }
    
    [Button("Oyunu Başlat", ButtonSizes.Large)]
    [GUIColor(0.4f, 0.8f, 1f)]
    public void OyunuBaslat()
    {
        Debug.Log($"[FindObjectModule] OyunuBaslat called - hasStarted: {hasStarted}, currentLevelIndex: {currentLevelIndex}");
        
        // Eğer Start() henüz çalışmamışsa
        if (!hasStarted)
        {
            Debug.Log("[FindObjectModule] Start() henüz çalışmamış, LoadProgress() manuel olarak çağrılıyor...");
            LoadProgress();
            UpdateCurrentLevelDisplay();
            hasStarted = true;
        }

        // Mevcut level'ı yükle
        LoadCurrentLevel();
    }

    private void LoadCurrentLevel()
    {
        if (currentLevelIndex >= levelPrefabNames.Length)
        {
            Debug.Log("Tüm level'lar tamamlandı! Başa dönüyor...");
            currentLevelIndex = 0;
            SaveProgress();
        }

        string levelName = levelPrefabNames[currentLevelIndex];
        string resourcePath = $"FindObject/{levelName}";

        Debug.Log($"[FindObjectModule] Yüklenecek level: {levelName}, Resource Path: {resourcePath}");

        GameObject levelPrefab = Resources.Load<GameObject>(resourcePath);
        
        if (levelPrefab == null)
        {
            Debug.LogError($"Level prefab'ı bulunamadı: {resourcePath}. Resources/FindObject/ klasöründe {levelName}.prefab dosyası olduğundan emin olun.");
            return;
        }

        // Önceki level'ı temizle
        if (assetHolder != null)
        {
            DestroyImmediate(assetHolder.gameObject);
        }
        
        // Yeni level'ı instantiate et
        GameObject instantiatedHolder = Instantiate(levelPrefab, LevelParent.transform);
        assetHolder = instantiatedHolder.GetComponent<FindObjectAssetHolder>();
        
        if (assetHolder == null)
        {
            Debug.LogError($"Level prefab'ında FindObjectAssetHolder component'i bulunamadı! {levelName}");
            return;
        }
        
        // Level'a tamamlanma callback'ini ekle
        assetHolder.OnGameWon.AddListener(OnLevelCompleted);
        
        InitializeObjects();
        foundObjectsCount = 0;
        UpdateProgressBar();
        lastInteractionTime = Time.time;
        
        Debug.Log($"Level yüklendi: {levelName} (İndeks: {currentLevelIndex})");
        
        if (showDebugMessages)
        {
            Debug.Log("✅ Level başarıyla initialize edildi!");
        }
    }

    private void OnLevelCompleted()
    {
        Debug.Log($"Level tamamlandı: {levelPrefabNames[currentLevelIndex]}");
        
        // Bir sonraki level'a geç
        currentLevelIndex++;
        SaveProgress();
        UpdateCurrentLevelDisplay();
        
        if (currentLevelIndex < levelPrefabNames.Length)
        {
            Debug.Log($"Bir sonraki level hazır: {levelPrefabNames[currentLevelIndex]}");
        }
        else
        {
            Debug.Log("Tüm level'lar tamamlandı! Tebrikler!");
        }
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(LEVEL_PROGRESS_KEY, currentLevelIndex);
        PlayerPrefs.Save();
        Debug.Log($"Level ilerlemesi kaydedildi: {currentLevelIndex}");
    }

    private void LoadProgress()
    {
        currentLevelIndex = PlayerPrefs.GetInt(LEVEL_PROGRESS_KEY, 0);
        Debug.Log($"Level ilerlemesi yüklendi: {currentLevelIndex}");
    }

    private void UpdateCurrentLevelDisplay()
    {
        if (currentLevelIndex < levelPrefabNames.Length)
        {
            currentLevelName = levelPrefabNames[currentLevelIndex];
        }
        else
        {
            currentLevelName = "Tamamlandı";
        }
        debugCurrentLevelIndex = currentLevelIndex;
        Debug.Log($"[FindObjectModule] UpdateCurrentLevelDisplay - currentLevelName: {currentLevelName}, currentLevelIndex: {currentLevelIndex}");
    }

    [Button("Reset Progress")]
    public void ResetProgress()
    {
        currentLevelIndex = 0;
        SaveProgress();
        UpdateCurrentLevelDisplay();
        Debug.Log("Level ilerlemesi sıfırlandı!");
    }

    [Button("Skip Current Level")]
    public void SkipCurrentLevel()
    {
        if (assetHolder != null)
        {
            OnLevelCompleted();
        }
    }

    public int GetCurrentLevelIndex()
    {
        return currentLevelIndex;
    }

    public string GetCurrentLevelName()
    {
        return currentLevelIndex < levelPrefabNames.Length ? levelPrefabNames[currentLevelIndex] : "Tamamlandı";
    }
    
    void InitializeObjects()
    {
        if (assetHolder == null || assetHolder.findObjects == null) return;
        
        objectMap = new Dictionary<GameObject, FindObject>();
        originalPositions = new Dictionary<GameObject, Vector3>();
        originalParents = new Dictionary<GameObject, Transform>();
        shakeCoroutines = new Dictionary<GameObject, Coroutine>();
        
        foreach (FindObject findObj in assetHolder.findObjects)
        {
            if (findObj.obj != null)
            {
                objectMap[findObj.obj] = findObj;
                originalPositions[findObj.obj] = findObj.obj.transform.position;
                originalParents[findObj.obj] = findObj.obj.transform.parent;
                
                // UI objesine tıklama özelliği ekle
                AddUIClickListener(findObj.obj);
                
                // UI objesinin child'ını başlangıçta kapat
                if (findObj.uiobj != null && findObj.uiobj.transform.childCount > 0)
                {
                    findObj.uiobj.transform.GetChild(0).gameObject.SetActive(false);
                }
            }
        }
    }
    
    void AddUIClickListener(GameObject obj)
    {
        // UI için Button component'i varsa kullan
        Button button = obj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnObjectClicked(obj));
        }
        else
        {
            // Button yoksa ClickableUIObject ekle
            ClickableUIObject clickable = obj.GetComponent<ClickableUIObject>();
            if (clickable == null)
            {
                clickable = obj.AddComponent<ClickableUIObject>();
            }
            clickable.OnClick = () => OnObjectClicked(obj);
        }
    }
    
    void OnObjectClicked(GameObject clickedObj)
    {
        if (objectMap.ContainsKey(clickedObj) && !objectMap[clickedObj].isFound)
        {
            lastInteractionTime = Time.time;
            StartCoroutine(HandleObjectFound(clickedObj));
        }
    }
    
    IEnumerator HandleObjectFound(GameObject obj)
    {
        FindObject findObj = objectMap[obj];
        findObj.isFound = true;
        foundObjectsCount++;
        
        // Sallanma animasyonunu durdur
        if (shakeCoroutines.ContainsKey(obj) && shakeCoroutines[obj] != null)
        {
            StopCoroutine(shakeCoroutines[obj]);
            shakeCoroutines[obj] = null;
            obj.transform.position = originalPositions[obj];
        }
        
        // Objeyi UI objesinin child'ı yap (en öne gelsin)
        Transform originalParent = originalParents[obj];
        obj.transform.SetParent(findObj.uiobj.transform, true);
        
        // UI objesinin pozisyonuna git
        Vector3 startPos = obj.transform.position;
        Vector3 targetPos = findObj.uiobj.transform.position;
        
        float elapsedTime = 0;
        while (elapsedTime < moveToUITime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveToUITime;
            obj.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        // Objeyi kapat
        obj.SetActive(false);
        
        // Parent'ı geri eski haline getir
        obj.transform.SetParent(originalParent, true);
        
        // UI objesinin child'ını aç
        if (findObj.uiobj.transform.childCount > 0)
        {
            findObj.uiobj.transform.GetChild(0).gameObject.SetActive(true);
        }
        
        // Progress bar'ı güncelle ve debug
        UpdateProgressBar();
        
        if (showDebugMessages)
        {
            Debug.Log($"Progress: {foundObjectsCount}/{assetHolder.findObjects.Count} - %{GetProgressPercentage():F1}");
        }
        
        // AssetHolder'a obje bulundu bilgisini gönder
        if (assetHolder != null)
        {
            assetHolder.OnObjectFoundCallback();
        }
        
        // Tüm objeler bulundu mu kontrol et
        if (foundObjectsCount >= assetHolder.findObjects.Count)
        {
            GameWon();
        }
    }
    
    void UpdateProgressBar()
    {
        if (progressBarImage != null && assetHolder != null && assetHolder.findObjects.Count > 0)
        {
            float progress = (float)foundObjectsCount / assetHolder.findObjects.Count;
            progressBarImage.fillAmount = progress;
        }
    }
    
    void GameWon()
    {
        if (showDebugMessages)
        {
            Debug.Log("🎉 Oyun Kazanıldı! Tüm objeler bulundu! 🎉");
        }
    }
    
    public float GetProgressPercentage()
    {
        if (assetHolder == null || assetHolder.findObjects.Count == 0) return 0f;
        return ((float)foundObjectsCount / assetHolder.findObjects.Count) * 100f;
    }
    
    void StartShakingUnfoundObjects()
    {
        if (assetHolder == null) return;
        
        // Bulunamayan objeleri topla
        List<FindObject> unfoundObjects = new List<FindObject>();
        foreach (FindObject findObj in assetHolder.findObjects)
        {
            if (!findObj.isFound && findObj.obj != null && findObj.obj.activeInHierarchy)
            {
                unfoundObjects.Add(findObj);
            }
        }
        
        if (unfoundObjects.Count == 0) return;
        
        // Rastgele bir obje seç ve sadece onu shake et
        FindObject selectedObj = unfoundObjects[Random.Range(0, unfoundObjects.Count)];
        
        if (!shakeCoroutines.ContainsKey(selectedObj.obj) || shakeCoroutines[selectedObj.obj] == null)
        {
            shakeCoroutines[selectedObj.obj] = StartCoroutine(ShakeObject(selectedObj.obj));
        }
    }
    
    IEnumerator ShakeObject(GameObject obj)
    {
        Vector3 originalPos = originalPositions[obj];
        
        while (true)
        {
            float shakeX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
            float shakeY = Mathf.Cos(Time.time * shakeSpeed * 1.1f) * shakeIntensity;
            
            obj.transform.position = originalPos + new Vector3(shakeX, shakeY, 0);
            
            yield return null;
        }
    }
}
