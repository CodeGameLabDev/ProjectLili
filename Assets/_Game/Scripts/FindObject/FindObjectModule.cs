using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sirenix.OdinInspector;

// Ana oyun mantığını yöneten class
public class FindObjectModule : MonoBehaviour
{
    [Header("Prefab Settings")]
    public GameObject assetHolderPrefab;
    
    [Header("Progress Bar")]
    public Image progressBarImage;
    
    [Header("Animation Settings")]
    public float moveToUITime = 0.5f;
    public float shakeIntensity = 0.1f;
    public float shakeSpeed = 5f;
    public float idleTimeBeforeShake = 3f;
    
    [Header("Debug Settings")]
    public bool showDebugMessages = true;
    
    private FindObjectAssetHolder assetHolder;
    private Dictionary<GameObject, FindObject> objectMap;
    private Dictionary<GameObject, Vector3> originalPositions;
    private Dictionary<GameObject, Transform> originalParents;
    private Dictionary<GameObject, Coroutine> shakeCoroutines;
    private float lastInteractionTime;
    private int foundObjectsCount = 0;

    [SerializeField] private GameObject LevelParent;
    
    void Update()
    {
        // Belirli süre hiçbir şey bulunmazsa objeleri sallat
        if (Time.time - lastInteractionTime > idleTimeBeforeShake)
        {
            StartShakingUnfoundObjects();
            lastInteractionTime = Time.time; // Sürekli sallanmasını önlemek için
        }
    }
    
    [Button("Initialize Asset Holder", ButtonSizes.Large)]
    [GUIColor(0.4f, 0.8f, 1f)]
    void InitializeAssetHolder()
    {
        if (assetHolderPrefab != null)
        {
            // Önceki instance'ı temizle
            if (assetHolder != null)
            {
                DestroyImmediate(assetHolder.gameObject);
            }
            
            GameObject instantiatedHolder = Instantiate(assetHolderPrefab,LevelParent.transform);
            assetHolder = instantiatedHolder.GetComponent<FindObjectAssetHolder>();
            
            if (assetHolder == null)
            {
                Debug.LogError("AssetHolder prefab'ında FindObjectAssetHolder component'i bulunamadı!");
                return;
            }
            
            InitializeObjects();
            foundObjectsCount = 0;
            UpdateProgressBar();
            lastInteractionTime = Time.time;
            
            if (showDebugMessages)
            {
                Debug.Log("✅ AssetHolder başarıyla initialize edildi!");
            }
        }
        else
        {
            Debug.LogError("AssetHolder prefab'ı atanmamış!");
        }
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
        
        foreach (FindObject findObj in assetHolder.findObjects)
        {
            if (!findObj.isFound && findObj.obj != null && findObj.obj.activeInHierarchy)
            {
                if (shakeCoroutines.ContainsKey(findObj.obj) && shakeCoroutines[findObj.obj] == null)
                {
                    shakeCoroutines[findObj.obj] = StartCoroutine(ShakeObject(findObj.obj));
                }
            }
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
