using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class WordBaloon : MonoBehaviour
{
    [Header("Oyun Ayarları")]
    public string targetLetters;
    [Range(0.1f, 5f)] public float baloonSpawnInterval = 1.2f;
    [Range(0f, 1f)] public float wrongBaloonChance = 0.3f;
    public int maxBaloonCount = 5;

    [Header("Database ve Parentlar")]
    public LetterPathDatabase letterDatabase;
    public GameObject baloonPrefab;
    public GameObject letterHolderPrefab;
    public Transform baloonParent;
    public Transform letterShadowParent;

    [Header("LetterShadow Ayarları")]
    public float letterShadowSpacing = 120f;
    public float letterShadowSize = 100f;
    
    [Header("LetterHolder Ayarları")]
    [Tooltip("LetterHolder boyutunu otomatik hesapla (true) veya manuel ayarla (false)")]
    public bool autoSizeLetterHolder = true;
    [Tooltip("LetterHolder manuel boyutu (autoSizeLetterHolder false ise kullanılır)")]
    public float manualLetterHolderSize = 100f;
    [Tooltip("letterShadowSize'ı minimum boyut olarak kullan")]
    public bool useLetterShadowSizeAsMinimum = true;
    [Tooltip("LetterHolder'lar arası minimum boşluk çarpanı (1.2 = boyutun %20'si)")]
    [Range(1.0f, 2.0f)] public float spacingMultiplier = 1.2f;

    [Header("Balon Animasyon Ayarları")]
    public float baloonRiseDuration = 2.5f;
    public Ease baloonRiseEase = Ease.Linear;

    [Header("Spine Hareket Animasyonu")]
    public float spineMoveDuration = 1.2f;
    public Ease spineMoveEase = Ease.OutQuart;
    public float spineRotationSpeed = 2f;
    public float shadowHighlightScale = 1.3f;
    public float shadowHighlightDuration = 0.3f;

    [Header("Harf Hareket Animasyonu")]
    public float letterMoveDuration = 0.8f;
    public Ease letterMoveEase = Ease.OutQuart;
    [Range(0.1f,1f)] public float letterEndScale = 0.4f;
    public float letterRotationSpeed = 3f;
    public float letterScaleEffect = 1.2f;
    public float letterTrailEffect = 0.1f;
    public AnimationCurve letterScaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1.2f);

    [Header("Balon Spawn Ayarları")]
    public float baloonSpawnIntervalMin = 1.0f;
    public float baloonSpawnIntervalMax = 2.0f;
    public float firstSpawnDelay = 0.5f;
    public int baloonSpawnCountPerTick = 1;
    public bool useRandomInterval = true;

    [Header("Progress Bar")]
    public Image progressBar;

    private List<GameObject> letterShadows = new List<GameObject>();
    private List<Baloon> activeBaloons = new List<Baloon>();
    private List<char> targetLetterList = new List<char>();
    private List<char> wrongLetterPool = new List<char>();
    private int placedCount = 0;
    private bool isGameActive = false;
    
    // Aynı harflerin sırayla yerleştirilmesi için takip sistemi
    private List<bool> letterPlacedStatus = new List<bool>();
    private Dictionary<char, int> letterPlacementIndex = new Dictionary<char, int>();

    // Object Pooling
    private Queue<Baloon> baloonPool = new Queue<Baloon>();
    public int poolSize = 10;

    // Overlay canvas for floating letters
    [HideInInspector] public Canvas overlayCanvas;

    [Header("Renk Ayarları")]
    public ColorPalette colorPalette;
    private int colorIndex = 0;

    // ADDED: Event that will be invoked when level is completed
    [Header("Events")]
    public UnityEvent onLevelCompleted;

    void Awake()
    {
        // Ensure overlay canvas exists
        overlayCanvas = FindObjectsOfType<Canvas>().FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceOverlay);
        if (overlayCanvas == null)
        {
            GameObject canvasGO = new GameObject("FloatingLetterCanvas");
            overlayCanvas = canvasGO.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        // Her durumda yüksek sorting order ver
        overlayCanvas.sortingOrder = 1000;
    }

    public Canvas GetOverlayCanvas()
    {
        return overlayCanvas;
    }

    void Start()
    {
        // EventSystem kontrolü ve otomatik ekleme
        if (FindObjectOfType<EventSystem>() == null)
        {
            Debug.LogWarning("EventSystem bulunamadı! Otomatik olarak ekleniyor...");
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
        
        if (letterDatabase == null || baloonParent == null || letterShadowParent == null || string.IsNullOrEmpty(targetLetters))
        {
            Debug.LogError("Eksik referanslar veya targetLetters boş!");
            return;
        }
        
        if (letterHolderPrefab == null)
        {
            Debug.LogError("LetterHolder prefabı atanmamış!");
            return;
        }
        SetupTargetLetters();
        SetupWrongLetterPool();
        SpawnLetterShadows();
        InitBaloonPool();
        StartCoroutine(BaloonSpawner());
        isGameActive = true;
        // (YORUM: Sesli yönerge burada oynatılabilir)
    }

    void SetupTargetLetters()
    {
        targetLetterList.Clear();
        letterPlacedStatus.Clear();
        letterPlacementIndex.Clear();
        
        foreach (char c in targetLetters)
        {
            if (!char.IsWhiteSpace(c))
            {
                targetLetterList.Add(c);
                letterPlacedStatus.Add(false); // Başlangıçta hiçbiri yerleştirilmemiş
            }
        }
        
        // Her harf için kaçıncı kez geldiğini takip etmek için index oluştur
        Dictionary<char, int> letterCount = new Dictionary<char, int>();
        for (int i = 0; i < targetLetterList.Count; i++)
        {
            char letter = targetLetterList[i];
            if (!letterCount.ContainsKey(letter))
            {
                letterCount[letter] = 0;
            }
            letterCount[letter]++;
            letterPlacementIndex[letter] = letterCount[letter] - 1; // 0-based index
        }
        
        Debug.Log($"Target harfler ayarlandı: {string.Join("", targetLetterList)}");
        Debug.Log($"Harf sayıları: {string.Join(", ", letterCount.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}");
    }

    void SetupWrongLetterPool()
    {
        string alphabet = "ABCÇDEƏFGĞHIİJKLMNOÖPRSŞTUÜVYZabcdeəfghijklmnopqrstuvwxyzçğıöşü";
        wrongLetterPool.Clear();
        foreach (char c in alphabet)
        {
            if (!targetLetterList.Contains(c))
                wrongLetterPool.Add(c);
        }
    }

    void SpawnLetterShadows()
    {
        foreach (Transform child in letterShadowParent) Destroy(child.gameObject);
        letterShadows.Clear();
        
        // LetterHolder prefabının boyutlarını al
        float holderSize = GetLetterHolderSize();
        float spacing = CalculateOptimalSpacing(holderSize);
        
        Debug.Log($"LetterHolder boyutu: {holderSize} (letterShadowSize: {letterShadowSize}), Spacing: {spacing}");
        
        float startX = -((targetLetterList.Count - 1) * spacing) / 2f;
        
        for (int i = 0; i < targetLetterList.Count; i++)
        {
            string letterId = targetLetterList[i].ToString();
            var letterData = letterDatabase.LoadLetterData(letterId);
            if (letterData == null)
            {
                Debug.LogWarning($"LetterData bulunamadı: {letterId}");
                continue;
            }
            
            // LetterHolder prefabını instantiate et
            GameObject letterHolder = Instantiate(letterHolderPrefab, letterShadowParent);
            letterHolder.name = $"LetterHolder_{letterId}_{i}";
            
            // LetterHolder'ın RectTransform'unu ayarla
            var holderRect = letterHolder.GetComponent<RectTransform>();
            if (holderRect == null)
            {
                holderRect = letterHolder.AddComponent<RectTransform>();
            }
            holderRect.anchorMin = new Vector2(0.5f, 0.5f);
            holderRect.anchorMax = new Vector2(0.5f, 0.5f);
            holderRect.pivot = new Vector2(0.5f, 0.5f);
            holderRect.sizeDelta = new Vector2(holderSize, holderSize);
            holderRect.localScale = Vector3.one;
            holderRect.anchoredPosition = new Vector2(startX + i * spacing, 0);
            
            // Child objeleri bul ve ayarla
            Transform spriteLetter = letterHolder.transform.Find("SpriteLetter");
            Transform spineLetter = letterHolder.transform.Find("SpineLetter");
            Transform shadowLetter = letterHolder.transform.Find("ShadowLetter");
            
            if (spriteLetter != null)
            {
                SetupLetterComponent(spriteLetter, letterData.letterSprite, "Sprite");
            }
            
            if (spineLetter != null && letterData.prefab != null)
            {
                SetupSpineComponent(spineLetter, letterData.prefab);
            }
            
            if (shadowLetter != null)
            {
                SetupLetterComponent(shadowLetter, letterData.letterShadowSprite, "Shadow");
            }
            
            letterShadows.Add(letterHolder);
            Debug.Log($"LetterHolder oluşturuldu: {letterId} - Sprite: {spriteLetter != null}, Spine: {spineLetter != null}, Shadow: {shadowLetter != null}");
        }
    }
    
    private void SetupLetterComponent(Transform letterTransform, Sprite sprite, string type)
    {
        // RectTransform ayarla
        var rect = letterTransform.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = letterTransform.gameObject.AddComponent<RectTransform>();
        }
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        
        // Image component'i ayarla
        var image = letterTransform.GetComponent<Image>();
        if (image == null)
        {
            image = letterTransform.gameObject.AddComponent<Image>();
        }
        image.sprite = sprite;
        image.SetNativeSize();
        
        // LetterHolder boyutuna göre child boyutunu ayarla
        float holderSize = GetLetterHolderSize();
        if (holderSize > 0)
        {
            // Sprite'ın orijinal boyutunu koru ama LetterHolder'a sığdır
            Vector2 spriteSize = image.sprite.rect.size;
            float scale = Mathf.Min(holderSize / spriteSize.x, holderSize / spriteSize.y);
            rect.sizeDelta = spriteSize * scale;
        }
        
        // Shadow için özel ayarlar
        if (type == "Shadow")
        {
            image.color = new Color(1, 1, 1, 0.3f); // Şeffaf gölge
            letterTransform.gameObject.SetActive(true); // Shadow başlangıçta görünür
        }
        else
        {
            letterTransform.gameObject.SetActive(false); // Sprite ve Spine başlangıçta gizli
        }
    }
    
    private void SetupSpineComponent(Transform spineTransform, GameObject spinePrefab)
    {
        // RectTransform ayarla
        var rect = spineTransform.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = spineTransform.gameObject.AddComponent<RectTransform>();
        }
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        
        // Spine prefabını instantiate et
        GameObject spineInstance = Instantiate(spinePrefab, spineTransform);
        spineInstance.transform.localPosition = Vector3.zero;
        spineInstance.transform.localRotation = Quaternion.identity;
        spineInstance.transform.localScale = Vector3.one;
        
        // LetterHolder boyutuna göre spine boyutunu ayarla
        float holderSize = GetLetterHolderSize();
        if (holderSize > 0)
        {
            // Spine'in RectTransform'unu kontrol et
            var spineRect = spineInstance.GetComponent<RectTransform>();
            if (spineRect != null)
            {
                // Spine'i LetterHolder boyutuna sığdır
                Vector2 spineSize = spineRect.sizeDelta;
                float scale = Mathf.Min(holderSize / spineSize.x, holderSize / spineSize.y);
                spineRect.sizeDelta = spineSize * scale;
            }
        }
        
        // Spine başlangıçta gizli
        spineTransform.gameObject.SetActive(false);
    }

    private float GetLetterHolderSize()
    {
        // Manuel boyut kullanılıyorsa onu döndür
        if (!autoSizeLetterHolder)
        {
            return manualLetterHolderSize;
        }
        
        if (letterHolderPrefab == null) return letterShadowSize; // Fallback
        
        // LetterHolder prefabının RectTransform'unu kontrol et
        var prefabRect = letterHolderPrefab.GetComponent<RectTransform>();
        if (prefabRect != null)
        {
            float prefabSize = Mathf.Max(prefabRect.sizeDelta.x, prefabRect.sizeDelta.y);
            // letterShadowSize'ı minimum boyut olarak kullan (eğer ayarlandıysa)
            if (useLetterShadowSizeAsMinimum)
            {
                return Mathf.Max(prefabSize, letterShadowSize);
            }
            return prefabSize;
        }
        
        // Eğer RectTransform yoksa, child'lardan en büyük boyutu al
        float maxChildSize = 0f;
        Transform[] children = { 
            letterHolderPrefab.transform.Find("SpriteLetter"),
            letterHolderPrefab.transform.Find("SpineLetter"),
            letterHolderPrefab.transform.Find("ShadowLetter")
        };
        
        foreach (var child in children)
        {
            if (child != null)
            {
                var childRect = child.GetComponent<RectTransform>();
                if (childRect != null)
                {
                    float childSize = Mathf.Max(childRect.sizeDelta.x, childRect.sizeDelta.y);
                    maxChildSize = Mathf.Max(maxChildSize, childSize);
                }
            }
        }
        
        // letterShadowSize'ı minimum boyut olarak kullan (eğer ayarlandıysa)
        if (useLetterShadowSizeAsMinimum)
        {
            float finalSize = Mathf.Max(maxChildSize, letterShadowSize);
            return finalSize > 0 ? finalSize : letterShadowSize; // Fallback
        }
        
        return maxChildSize > 0 ? maxChildSize : letterShadowSize; // Fallback
    }
    
    private float CalculateOptimalSpacing(float holderSize)
    {
        // LetterHolder boyutuna göre optimal spacing hesapla
        // Spacing multiplier kullan
        float minSpacing = holderSize * spacingMultiplier;
        
        // Inspector'daki letterShadowSpacing ile karşılaştır
        // Daha büyük olanı kullan
        return Mathf.Max(minSpacing, letterShadowSpacing);
    }

    void InitBaloonPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(baloonPrefab, baloonParent);
            obj.SetActive(false);
            Baloon baloon = obj.GetComponent<Baloon>();
            if (baloon == null)
            {
                Debug.LogError("Baloon prefabında Baloon scripti yok!");
                continue;
            }
            baloon.manager = this;
            baloonPool.Enqueue(baloon);
        }
    }

    IEnumerator BaloonSpawner()
    {
        yield return new WaitForSeconds(firstSpawnDelay);
        while (isGameActive)
        {
            if (activeBaloons.Count < maxBaloonCount)
            {
                int spawnCount = Mathf.Min(baloonSpawnCountPerTick, maxBaloonCount - activeBaloons.Count);
                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnBaloon();
                }
            }
            float wait = useRandomInterval ? Random.Range(baloonSpawnIntervalMin, baloonSpawnIntervalMax) : baloonSpawnIntervalMin;
            yield return new WaitForSeconds(wait);
        }
    }

    void SpawnBaloon()
    {
        if (baloonPool.Count == 0) return;
        Baloon baloon = baloonPool.Dequeue();
        char letter = GetRandomBaloonLetter();
        string letterId = letter.ToString();
        var letterData = letterDatabase.LoadLetterData(letterId);
        if (letterData == null)
        {
            Debug.LogWarning($"LetterData bulunamadı: {letterId}");
            baloonPool.Enqueue(baloon);
            return;
        }
        // ColorPalette üzerinden renk seçimi (palet yoksa rastgele renk kullan)
        Color currentColor;
        if (colorPalette != null && colorPalette.ColorCount > 0)
        {
            currentColor = colorPalette.GetColor(colorIndex++);
        }
        else
        {
            // Fallback – rastgele canlı renk
            currentColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f);
        }

        baloon.SetLetter(letter, IsTargetLetter(letter), letterData, currentColor);
        var rect = baloon.GetComponent<RectTransform>();
        float x = Random.Range(-300, 300);
        float startY = -Screen.height * 0.5f;
        float endY = Screen.height * 0.6f;
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(x, startY);
            rect.DOKill();
            rect.DOAnchorPosY(endY, baloonRiseDuration)
                .SetEase(baloonRiseEase)
                .OnComplete(() => {
                    RecycleBaloon(baloon);
                });
        }
        else
        {
            baloon.transform.localPosition = new Vector3(x, startY, 0);
            baloon.transform.DOKill();
            baloon.transform.DOLocalMoveY(endY, baloonRiseDuration)
                .SetEase(baloonRiseEase)
                .OnComplete(() => {
                    RecycleBaloon(baloon);
                });
        }
        baloon.gameObject.SetActive(true);
        activeBaloons.Add(baloon);
    }

    char GetRandomBaloonLetter()
    {
        if (Random.value < wrongBaloonChance && wrongLetterPool.Count > 0)
        {
            return wrongLetterPool[Random.Range(0, wrongLetterPool.Count)];
        }
        else
        {
            return targetLetterList[Random.Range(0, targetLetterList.Count)];
        }
    }

    bool IsTargetLetter(char c)
    {
        return targetLetterList.Contains(c);
    }

    public void OnBaloonTapped(Baloon baloon)
    {
        Debug.Log($"OnBaloonTapped çağrıldı! isGameActive: {isGameActive}, isTarget: {baloon.isTarget}, letter: {baloon.letter}");
        
        if (!isGameActive) return;
        if (baloon.isTarget && HasAvailableShadow(baloon.letter))
        {
            Debug.Log("Doğru balon tıklandı, patlama animasyonu başlatılıyor...");
            // Doğru balon patlama animasyonu başlat
            baloon.PopBaloon();
        }
        else
        {
            Debug.Log("Yanlış balon tıklandı veya slot kalmadı, zıplama animasyonu başlatılıyor...");
            // (YORUM: Yanlış balon, bulp sesi burada çalınacak)
            // (YORUM: Yanlış balon hafifçe zıplatılabilir)
            // Yanlış balon için hafif zıplama animasyonu
            var rect = baloon.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.DOShakePosition(0.3f, 10f, 10, 90f, false, true);
            }
        }
    }

    public void OnBaloonPopped(Baloon baloon, GameObject letterInstance)
    {
        // Harf sprite'ı patlama animasyonu oynat ve hedef gölgeye hareket ettir
        StartCoroutine(MoveLetterToShadow(baloon, letterInstance));
    }

    IEnumerator MoveLetterToShadow(Baloon baloon, GameObject letterPrefab)
    {
        int shadowIndex = FindAndReserveShadowIndex(baloon.letter);
        if (shadowIndex == -1)
        {
            Destroy(letterPrefab);
            RecycleBaloon(baloon);
            yield break;
        }

        GameObject letterHolder = letterShadows[shadowIndex];

        // Overlay canvas referansı
        Canvas overlay = GetOverlayCanvas();
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();

        // LetterPrefab'in RectTransform'u
        RectTransform letterRect = letterPrefab.GetComponent<RectTransform>();

        // Hedef LetterHolder'ın ekran pozisyonunu al
        Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(null, letterHolder.transform.position);

        // Animasyon: position -> targetScreen
        letterRect.DOKill();

        // Hareket ve küçülme animasyonunu aynı anda yap
        Sequence seq = DOTween.Sequence();
        seq.Join(letterRect.DOMove(targetScreen, letterMoveDuration)
                    .SetEase(letterMoveEase));

        Vector3 endScale = Vector3.one * Mathf.Clamp01(letterEndScale);
        seq.Join(letterRect.DOScale(endScale, letterMoveDuration)
                    .SetEase(Ease.InOutQuad));

        seq.OnComplete(() => {
            // LetterPrefab'i balona geri taşı ve pasifleştir
            letterPrefab.transform.SetParent(baloon.transform, false);
            letterPrefab.SetActive(false);

            // Yerleştirme sayısını güncelle
            placedCount++;

            UpdateProgressBar();

            // LetterHolder içindeki SpriteLetter'ın rengini, balondaki harf renginden al
            Transform spriteLetterTf = letterHolder.transform.Find("SpriteLetter");
            if (spriteLetterTf != null)
            {
                var img = spriteLetterTf.GetComponent<Image>();
                if (img != null)
                {
                    img.color = baloon.letterColor;
                }
            }

            HighlightShadow(letterHolder);

            if (placedCount >= targetLetterList.Count)
                OnAllLettersPlaced();

        });

        RecycleBaloon(baloon);

        yield return null;
    }

    private void HighlightShadow(GameObject shadow)
    {
        // LetterHolder'dan child objeleri bul
        Transform spriteLetter = shadow.transform.Find("SpriteLetter");
        Transform shadowLetter = shadow.transform.Find("ShadowLetter");
        
        // Shadow'u gizle, Sprite'ı göster
        if (shadowLetter != null) shadowLetter.gameObject.SetActive(false);
        if (spriteLetter != null) spriteLetter.gameObject.SetActive(true);
        
        // Basit başarı animasyonu
        shadow.transform.DOScale(shadowHighlightScale, shadowHighlightDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => {
                shadow.transform.DOScale(1f, 0.2f)
                    .SetEase(Ease.OutBounce);
            });
        
        // Renk değişimi efekti
        if (spriteLetter != null)
        {
            var spriteImage = spriteLetter.GetComponent<Image>();
            if (spriteImage != null)
            {
                Color originalColor = spriteImage.color;
                spriteImage.DOColor(Color.yellow, shadowHighlightDuration)
                    .OnComplete(() => {
                        spriteImage.DOColor(originalColor, shadowHighlightDuration);
                    });
            }
        }
        
        Debug.Log("Harf başarıyla yerleştirildi!");
    }

    int FindAndReserveShadowIndex(char c)
    {
        // Bu harfin kaçıncı kez geldiğini bul
        int currentPlacementIndex = 0;
        
        // Şu ana kadar bu harften kaç tane yerleştirildiğini say
        for (int i = 0; i < targetLetterList.Count; i++)
        {
            if (targetLetterList[i] == c && letterPlacedStatus[i])
            {
                currentPlacementIndex++;
            }
        }
        
        // Bu harfin sıradaki pozisyonunu bul
        for (int i = 0; i < targetLetterList.Count; i++)
        {
            if (targetLetterList[i] == c && !letterPlacedStatus[i])
            {
                // Bu harfin kaçıncı kez geldiğini kontrol et
                int letterOccurrence = 0;
                for (int j = 0; j <= i; j++)
                {
                    if (targetLetterList[j] == c)
                    {
                        letterOccurrence++;
                    }
                }
                
                // Eğer bu harfin sıradaki pozisyonu ise, bu index'i döndür
                if (letterOccurrence == currentPlacementIndex + 1)
                {
                    Debug.Log($"Harf '{c}' için uygun pozisyon bulundu: {i} (sıra: {currentPlacementIndex + 1})");
                    letterPlacedStatus[i] = true;
                    return i;
                }
            }
        }
        
        Debug.LogWarning($"Harf '{c}' için uygun pozisyon bulunamadı!");
        return -1;
    }

    void RecycleBaloon(Baloon baloon)
    {
        var rect = baloon.GetComponent<RectTransform>();
        if (rect != null) rect.DOKill();
        else baloon.transform.DOKill();
        baloon.gameObject.SetActive(false);
        baloonPool.Enqueue(baloon);
        // Ensure the balloon is removed from the active list
        activeBaloons.Remove(baloon);
    }

    void OnAllLettersPlaced()
    {
        isGameActive = false;
        Debug.Log("Level bitti");
        // Invoke level complete event if any listeners are attached
        onLevelCompleted?.Invoke();
        // (YORUM: Tüm harflerin sevinç animasyonu burada oynatılacak)
        // (YORUM: Progress bar güncellemesi burada yapılabilir)
        // (YORUM: Görev tamamlandı, diğer işlemler yapılabilir)
    }

    // Debug için: Mevcut yerleştirme durumunu göster
    [ContextMenu("Yerleştirme Durumunu Göster")]
    private void ShowPlacementStatus()
    {
        string status = "";
        for (int i = 0; i < targetLetterList.Count; i++)
        {
            status += $"{targetLetterList[i]}:{(letterPlacedStatus[i] ? "✓" : "✗")} ";
        }
        Debug.Log($"Yerleştirme Durumu: {status}");
    }
    
    // Debug için: Animasyon ayarlarını test et
    [ContextMenu("Animasyon Ayarlarını Test Et")]
    private void TestAnimationSettings()
    {
        Debug.Log($"Harf Hareket Animasyonu Ayarları:");
        Debug.Log($"- Süre: {letterMoveDuration}s");
        Debug.Log($"- Ease: {letterMoveEase}");
        Debug.Log($"- Dönme Hızı: {letterRotationSpeed}");
        Debug.Log($"- Highlight Scale: {shadowHighlightScale}");
        Debug.Log($"- Highlight Duration: {shadowHighlightDuration}");
    }

    // Seçilen harf için henüz yerleştirilmemiş bir gölge var mı?
    private bool HasAvailableShadow(char c)
    {
        for (int i = 0; i < targetLetterList.Count; i++)
        {
            if (targetLetterList[i] == c && !letterPlacedStatus[i])
                return true;
        }
        return false;
    }

    // --- Progress Bar ---
    private void UpdateProgressBar()
    {
        if (progressBar == null) return;
        float ratio = targetLetterList.Count == 0 ? 0f : (float)placedCount / targetLetterList.Count;
        ratio = Mathf.Clamp01(ratio);
        progressBar.fillAmount = ratio;
    }

    // --- Balon Component ---
    // Baloon class'ı ayrı dosyaya taşındı.
}
