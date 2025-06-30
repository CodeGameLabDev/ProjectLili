using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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
    public Transform baloonParent;
    public Transform letterShadowParent;

    [Header("LetterShadow Ayarları")]
    public float letterShadowSpacing = 120f;
    public float letterShadowSize = 100f;

    [Header("Balon Animasyon Ayarları")]
    public float baloonRiseDuration = 2.5f;
    public Ease baloonRiseEase = Ease.Linear;

    [Header("Balon Spawn Ayarları")]
    public float baloonSpawnIntervalMin = 1.0f;
    public float baloonSpawnIntervalMax = 2.0f;
    public float firstSpawnDelay = 0.5f;
    public int baloonSpawnCountPerTick = 1;
    public bool useRandomInterval = true;

    // [Header("Progres Bar")]
    // public Image progressBar; // şimdilik kullanılmıyor

    private List<GameObject> letterShadows = new List<GameObject>();
    private List<Baloon> activeBaloons = new List<Baloon>();
    private List<char> targetLetterList = new List<char>();
    private List<char> wrongLetterPool = new List<char>();
    private int placedCount = 0;
    private bool isGameActive = false;

    // Object Pooling
    private Queue<Baloon> baloonPool = new Queue<Baloon>();
    public int poolSize = 10;

    void Start()
    {
        if (letterDatabase == null || baloonParent == null || letterShadowParent == null || string.IsNullOrEmpty(targetLetters))
        {
            Debug.LogError("Eksik referanslar veya targetLetters boş!");
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
        foreach (char c in targetLetters)
        {
            if (!char.IsWhiteSpace(c))
                targetLetterList.Add(c);
        }
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
        float spacing = letterShadowSpacing;
        float shadowSize = letterShadowSize;
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
            GameObject shadowObj = new GameObject($"Shadow_{letterId}_{i}", typeof(RectTransform));
            shadowObj.transform.SetParent(letterShadowParent, false);
            var image = shadowObj.AddComponent<Image>();
            image.sprite = letterData.letterShadowSprite;
            // image.SetNativeSize(); // Kaldırıldı
            var rect = shadowObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(shadowSize, shadowSize);
            rect.localScale = Vector3.one;
            rect.anchoredPosition = new Vector2(startX + i * spacing, 0);
            letterShadows.Add(shadowObj);
        }
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
        baloon.SetLetter(letter, IsTargetLetter(letter), letterData);
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
                    activeBaloons.Remove(baloon);
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
                    activeBaloons.Remove(baloon);
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
        if (!isGameActive) return;
        if (baloon.isTarget)
        {
            // (YORUM: Doğru balon spine patlama animasyonu burada oynatılacak)
            // (YORUM: Harf gölgesine doğru hareket animasyonu burada başlatılacak)
            StartCoroutine(MoveBaloonToShadow(baloon));
        }
        else
        {
            // (YORUM: Yanlış balon, bulp sesi burada çalınacak)
            // (YORUM: Yanlış balon hafifçe zıplatılabilir)
        }
    }

    IEnumerator MoveBaloonToShadow(Baloon baloon)
    {
        int shadowIndex = FindFirstAvailableShadowIndex(baloon.letter);
        if (shadowIndex == -1)
        {
            RecycleBaloon(baloon);
            yield break;
        }
        GameObject shadow = letterShadows[shadowIndex];
        Vector3 start = baloon.transform.position;
        Vector3 end = shadow.transform.position;
        float t = 0;
        float duration = 0.5f;
        // (YORUM: Kudurma animasyonu burada başlatılabilir)
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            baloon.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        // (YORUM: Harf iz düşümüne yerleşti, normal formuna dönebilir)
        var cg = shadow.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f; // (isteğe bağlı: gölgeyi vurgula)
        RecycleBaloon(baloon);
        placedCount++;
        if (placedCount >= targetLetterList.Count)
        {
            OnAllLettersPlaced();
        }
        // (YORUM: Progress bar güncellemesi burada yapılabilir)
    }

    int FindFirstAvailableShadowIndex(char c)
    {
        for (int i = 0; i < targetLetterList.Count; i++)
        {
            if (targetLetterList[i] == c)
                return i;
        }
        return -1;
    }

    void RecycleBaloon(Baloon baloon)
    {
        var rect = baloon.GetComponent<RectTransform>();
        if (rect != null) rect.DOKill();
        else baloon.transform.DOKill();
        baloon.gameObject.SetActive(false);
        baloonPool.Enqueue(baloon);
    }

    void OnAllLettersPlaced()
    {
        isGameActive = false;
        // (YORUM: Tüm harflerin sevinç animasyonu burada oynatılacak)
        // (YORUM: Progress bar güncellemesi burada yapılabilir)
        // (YORUM: Görev tamamlandı, diğer işlemler yapılabilir)
    }

    // --- Balon Component ---
    // Baloon class'ı ayrı dosyaya taşındı.
}
