using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Sirenix.OdinInspector;

namespace HiddenLetterGame
{
    /// <summary>
    /// FindObject mantığının harf versiyonu. Sahnede gizlenmiş harfleri bulup yukarıdaki slotlara yerleştirir.
    /// </summary>
    public class HiddenLetterModule : MonoBehaviour
    {
        [Header("Kelime Ayarları")]
        [Tooltip("Hedef kelime. Harfler sıralı olarak slotlara yerleşir.")]
        public string targetLetters;

        [Header("Letter Holder Settings")]
        [Tooltip("Slotlarda kullanılacak LetterHolder prefabı (Shadow/Sprite child'ları içerebilir)")]
        public GameObject letterHolderPrefab;
        // Çalışma sırasında instantiate edilen holder'ları takip etmek için
        private readonly List<GameObject> spawnedLetterHolders = new List<GameObject>();

        [Header("Progress Bar")]
        public Image progressBar;

        [Header("Animasyon Ayarları")]
        public float moveToSlotDuration = 0.6f;
        public AnimationCurve moveEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Shake Ayarları (Bulunmayan harfler için isteğe bağlı)")]
        public bool enableShake = true;
        public float shakeIntensity = 0.1f;
        public float shakeSpeed = 5f;
        public float idleTimeBeforeShake = 3f;

        [Header("Debug")]
        public bool showDebugMessages = true;
        [ReadOnly] public int placedCount = 0;

        [Header("Level System")]
        [Tooltip("Resources/FindLetters klasöründeki prefab isimleri")] public string[] levelPrefabNames =
            {"Level_1", "Level_2", "Level_3", "Level_4", "Level_5"};
        [SerializeField] private Transform levelParent;

        // Internal state
        private HiddenLetterGame.HiddenLetterAssetHolder assetHolder;
        private Dictionary<GameObject, HiddenLetterGame.HiddenLetter> letterMap;
        private Dictionary<GameObject, Vector3> originalPositions;
        private Dictionary<GameObject, Transform> originalParents;
        private Dictionary<GameObject, Coroutine> shakeCoroutines;
        private float lastInteractionTime;
        private int currentLevelIndex = 0;
        private const string LEVEL_PROGRESS_KEY = "HiddenLetterLevelIndex";

        [Header("Auto Slot Generation")]
        public bool autoGenerateSlots = true;
        public Transform autoSlotParent;
        public float autoSlotSpacing = 120f;
        public float autoSlotSize = 100f;

        [Header("Letter Database")]
        public LetterPathDatabase letterDatabase;

        // Runtime slot list (either from assetHolder or generated)
        private List<RectTransform> currentSlots = new List<RectTransform>();

        // ------- Helper methods copied/adapted from WordBaloon --------
        private float GetLetterHolderSize()
        {
            // If prefab has RectTransform use its max size, else fallback to autoSlotSize
            if (letterHolderPrefab == null) return autoSlotSize;
            var rect = letterHolderPrefab.GetComponent<RectTransform>();
            if (rect != null)
            {
                return Mathf.Max(rect.sizeDelta.x, rect.sizeDelta.y);
            }
            return autoSlotSize;
        }

        private void SetupLetterComponent(Transform letterTransform, Sprite sprite, string type)
        {
            if (letterTransform == null || sprite == null) return;

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

            var image = letterTransform.GetComponent<Image>();
            if (image == null)
            {
                image = letterTransform.gameObject.AddComponent<Image>();
            }
            image.sprite = sprite;
            image.SetNativeSize();

            float holderSize = GetLetterHolderSize();
            if (holderSize > 0)
            {
                Vector2 spriteSize = image.sprite.rect.size;
                float scale = Mathf.Min(holderSize / spriteSize.x, holderSize / spriteSize.y);
                rect.sizeDelta = spriteSize * scale;
            }

            if (type == "Shadow")
            {
                image.color = new Color(1,1,1,0.3f);
                letterTransform.gameObject.SetActive(true);
            }
            else
            {
                letterTransform.gameObject.SetActive(false);
            }
        }

        private void SetupSpineComponent(Transform spineTransform, GameObject spinePrefab)
        {
            if (spineTransform == null || spinePrefab == null) return;

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

            GameObject spineInstance = Instantiate(spinePrefab, spineTransform);
            spineInstance.transform.localPosition = Vector3.zero;
            spineInstance.transform.localRotation = Quaternion.identity;
            spineInstance.transform.localScale = Vector3.one;

            float holderSize = GetLetterHolderSize();
            var spineRect = spineInstance.GetComponent<RectTransform>();
            if (spineRect != null && holderSize > 0)
            {
                Vector2 spineSize = spineRect.sizeDelta;
                float scale = Mathf.Min(holderSize / spineSize.x, holderSize / spineSize.y);
                spineRect.sizeDelta = spineSize * scale;
            }

            spineTransform.gameObject.SetActive(false);
        }

        void Awake()
        {
            LoadProgress();
        }

        void Start()
        {
            LoadCurrentLevel();
        }

        // ---------------- Level Loading -------------
        void LoadCurrentLevel()
        {
            if (currentLevelIndex >= levelPrefabNames.Length)
            {
                Debug.Log("Tüm level'lar tamamlandı, başa dönülüyor.");
                currentLevelIndex = 0;
            }

            string levelName = levelPrefabNames[currentLevelIndex];
            string resourcePath = $"FindLetters/{levelName}";

            GameObject levelPrefab = Resources.Load<GameObject>(resourcePath);
            if (levelPrefab == null)
            {
                Debug.LogError($"Level prefab'ı bulunamadı: {resourcePath}");
                return;
            }

            // clear previous
            if (levelParent == null)
            {
                Debug.LogWarning("levelParent atanmamış, yeni empty GameObject oluşturulacak");
                levelParent = new GameObject("HiddenLetterLevelParent").transform;
            }
            foreach (Transform child in levelParent) Destroy(child.gameObject);

            GameObject holderInstance = Instantiate(levelPrefab, levelParent);
            assetHolder = holderInstance.GetComponent<HiddenLetterGame.HiddenLetterAssetHolder>();
            if (assetHolder == null)
            {
                Debug.LogError("Level prefab'ında HiddenLetterAssetHolder bulunamadı!");
                return;
            }

            placedCount = 0;
            assetHolder.ResetProgress();

            // Generate hidden letters inside the level based on target word
            if (assetHolder != null)
            {
                assetHolder.GenerateHiddenLetters(targetLetters, letterHolderPrefab, letterDatabase);
            }

            SpawnLetterHolders();
            InitializeLetters();
            UpdateProgressBar();
            lastInteractionTime = Time.time;
            Debug.Log($"HiddenLetter level yüklendi: {levelName} (index {currentLevelIndex})");
        }

        [Button("Sonraki Level'e Geç")]
        public void NextLevel()
        {
            currentLevelIndex++;
            SaveProgress();
            LoadCurrentLevel();
        }

        void SaveProgress() { PlayerPrefs.SetInt(LEVEL_PROGRESS_KEY, currentLevelIndex); }
        void LoadProgress() { currentLevelIndex = PlayerPrefs.GetInt(LEVEL_PROGRESS_KEY, 0); }

        void Update()
        {
            if (!enableShake) return;

            if (Time.time - lastInteractionTime > idleTimeBeforeShake)
            {
                StartShakingUnfoundLetters();
                lastInteractionTime = Time.time; // sürekli tetiklenmesin
            }
        }

        [Button("Oyunu Başlat", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        public void OyunuBaslat()
        {
            placedCount = 0;
            assetHolder.ResetProgress();
            InitializeLetters();
            SpawnLetterHolders();
            UpdateProgressBar();
        }

        void InitializeLetters()
        {
            letterMap = new Dictionary<GameObject, HiddenLetterGame.HiddenLetter>();
            originalPositions = new Dictionary<GameObject, Vector3>();
            originalParents = new Dictionary<GameObject, Transform>();
            shakeCoroutines = new Dictionary<GameObject, Coroutine>();

            foreach (var hidden in assetHolder.hiddenLetters)
            {
                if (hidden.obj == null) continue;

                letterMap[hidden.obj] = hidden;
                originalPositions[hidden.obj] = hidden.obj.transform.position;
                originalParents[hidden.obj] = hidden.obj.transform.parent;
                hidden.isFound = false;

                AddClickListener(hidden.obj);
            }
        }

        void AddClickListener(GameObject obj)
        {
            // Önce Button varsa kullan
            var btn = obj.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnLetterClicked(obj));
            }
            else
            {
                // Yoksa ClickableUIObject kullan
                var clickable = obj.GetComponent<ClickableUIObject>();
                if (clickable == null)
                {
                    clickable = obj.AddComponent<ClickableUIObject>();
                }
                clickable.OnClick = () => OnLetterClicked(obj);
            }
        }

        void OnLetterClicked(GameObject clickedObj)
        {
            if (!letterMap.ContainsKey(clickedObj)) return;

            HiddenLetterGame.HiddenLetter hidden = letterMap[clickedObj];
            if (hidden.isFound) return;

            // Kelime uzunluğunu aştıysa ya da slot kalmadıysa ignor
            if (currentSlots == null || placedCount >= currentSlots.Count)
            {
                if (showDebugMessages) Debug.Log("Tüm slotlar dolu, ek harf yoksayılıyor.");
                return;
            }

            // Harfin kelimedeki beklenen harf olup olmadığı kontrolü yapılabilir
            // (isteğe bağlı) Şimdilik her harfi sırayla kabul ediyoruz.

            lastInteractionTime = Time.time;
            StartCoroutine(HandleLetterFound(clickedObj));
        }

        IEnumerator HandleLetterFound(GameObject obj)
        {
            HiddenLetterGame.HiddenLetter hidden = letterMap[obj];
            hidden.isFound = true;
            placedCount++;

            // Sallanmayı durdur
            if (shakeCoroutines.ContainsKey(obj) && shakeCoroutines[obj] != null)
            {
                StopCoroutine(shakeCoroutines[obj]);
                shakeCoroutines[obj] = null;
                obj.transform.position = originalPositions[obj];
            }

            // Hedef slot
            int slotIndex = placedCount - 1;
            if (currentSlots == null || currentSlots.Count == 0)
            {
                Debug.LogError("Slot list is empty! Slots oluşturulmadı.");
                yield break;
            }
            if (slotIndex >= currentSlots.Count)
            {
                slotIndex = currentSlots.Count - 1;
            }
            RectTransform targetSlot = currentSlots[slotIndex];

            // Animasyon için parent değişimi (overlay canvas gibi)
            Transform originalParent = originalParents[obj];
            obj.transform.SetParent(targetSlot, true);

            Vector3 startPos = obj.transform.position;
            Vector3 targetPos = targetSlot.position;

            float elapsed = 0f;
            while (elapsed < moveToSlotDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / moveToSlotDuration;
                float eased = moveEase.Evaluate(t);
                obj.transform.position = Vector3.Lerp(startPos, targetPos, eased);
                yield return null;
            }

            // Objeyi slotta sabitle
            obj.transform.localPosition = Vector3.zero;
            // Shadow/Sprite toggle
            if (targetSlot != null)
            {
                Transform spriteLetter = targetSlot.Find("SpriteLetter");
                Transform shadowLetter = targetSlot.Find("ShadowLetter");
                if (shadowLetter != null) shadowLetter.gameObject.SetActive(false);
                if (spriteLetter != null) spriteLetter.gameObject.SetActive(true);
            }

            // İstersek objeyi disable edip slot içinde sprite enjekte edebiliriz; şimdilik aktif bırakıyoruz.

            UpdateProgressBar();
            assetHolder.OnLetterFoundCallback();

            if (assetHolder.GetProgressPercentage() >= 100f)
            {
                if (showDebugMessages) Debug.Log("🎉 Tüm harfler bulundu!");
            }
        }

        void UpdateProgressBar()
        {
            if (progressBar == null) return;
            if (assetHolder != null)
                progressBar.fillAmount = placedCount / (float)Mathf.Max(1, assetHolder.hiddenLetters.Count);
        }

        void StartShakingUnfoundLetters()
        {
            List<HiddenLetterGame.HiddenLetter> unfound = new List<HiddenLetterGame.HiddenLetter>();
            if (assetHolder == null) return;
            foreach (var h in assetHolder.hiddenLetters)
            {
                if (!h.isFound && h.obj != null && h.obj.activeInHierarchy)
                {
                    unfound.Add(h);
                }
            }
            if (unfound.Count == 0) return;

            HiddenLetterGame.HiddenLetter selected = unfound[Random.Range(0, unfound.Count)];

            if (!shakeCoroutines.ContainsKey(selected.obj) || shakeCoroutines[selected.obj] == null)
            {
                shakeCoroutines[selected.obj] = StartCoroutine(ShakeObject(selected.obj));
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

        // --- Letter Holder Spawn ---
        void SpawnLetterHolders()
        {
            // Öncekileri temizle
            foreach (var go in spawnedLetterHolders)
            {
                if (go != null) Destroy(go);
            }
            spawnedLetterHolders.Clear();

            currentSlots.Clear();

            if (letterHolderPrefab == null)
            {
                Debug.LogWarning("letterHolderPrefab atanmamış!");
                return;
            }

            List<RectTransform> slotSource = null;

            if (assetHolder != null && assetHolder.hiddenLetterPositions != null && assetHolder.hiddenLetterPositions.Count > 0)
            {
                slotSource = assetHolder.hiddenLetterPositions;
            }
            else if (autoGenerateSlots)
            {
                // generate slots dynamically based on targetLetters
                if (autoSlotParent == null)
                {
                    autoSlotParent = new GameObject("AutoSlotParent").AddComponent<RectTransform>();
                    autoSlotParent.SetParent(levelParent ?? transform, false);
                    var rect = autoSlotParent.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(0, -50f);
                }

                string trimmedWord = string.IsNullOrEmpty(targetLetters) ? "" : targetLetters.Replace(" ", "");
                float startX = -((trimmedWord.Length - 1) * autoSlotSpacing) / 2f;

                for (int i = 0; i < trimmedWord.Length; i++)
                {
                    GameObject slotGO = new GameObject($"AutoSlot_{i}", typeof(RectTransform));
                    RectTransform slotRect = slotGO.GetComponent<RectTransform>();
                    slotRect.SetParent(autoSlotParent, false);
                    slotRect.sizeDelta = new Vector2(autoSlotSize, autoSlotSize);
                    slotRect.anchoredPosition = new Vector2(startX + i * autoSlotSpacing, 0);
                    slotSource ??= new List<RectTransform>();
                    slotSource.Add(slotRect);
                }
            }

            if (slotSource == null)
            {
                Debug.LogError("Slot kaynağı bulunamadı. hiddenLetterPositions boş ve autoGenerateSlots kapalı.");
                return;
            }

            for (int idx = 0; idx < slotSource.Count; idx++)
            {
                RectTransform slot = slotSource[idx];
                if (slot == null) continue;

                GameObject holder = Instantiate(letterHolderPrefab, slot);
                holder.transform.localPosition = Vector3.zero;
                spawnedLetterHolders.Add(holder);

                // Child isimleri hem *Letter hem *Holder varyasyonlarını desteklesin
                string[] spriteNames = {"SpriteLetter", "SpriteHolder"};
                string[] shadowNames = {"ShadowLetter", "ShadowHolder"};
                string[] spineNames  = {"SpineLetter",  "SpineHolder"};

                Transform spriteTf = spriteNames.Select(n => holder.transform.Find(n)).FirstOrDefault(t => t != null);
                Transform shadowTf = shadowNames.Select(n => holder.transform.Find(n)).FirstOrDefault(t => t != null);
                Transform spineTf  = spineNames.Select(n => holder.transform.Find(n)).FirstOrDefault(t => t != null);

                // Load letter data if possible
                char letterChar = '\0';
                if (!string.IsNullOrEmpty(targetLetters))
                {
                    string trimmedWord = targetLetters.Replace(" ", "");
                    if (idx < trimmedWord.Length) letterChar = trimmedWord[idx];
                }

                if (letterDatabase != null && letterChar != '\0')
                {
                    var letterData = letterDatabase.LoadLetterData(letterChar.ToString());
                    if (letterData != null)
                    {
                        // setup components
                        SetupLetterComponent(spriteTf, letterData.letterSprite, "Sprite");
                        SetupLetterComponent(shadowTf, letterData.letterShadowSprite, "Shadow");
                        if (letterData.prefab != null)
                            SetupSpineComponent(spineTf, letterData.prefab);
                    }
                }

                // Varsayılan görünürlük: shadow açık, sprite kapalı
                if (shadowTf != null) shadowTf.gameObject.SetActive(true);
                if (spriteTf != null) spriteTf.gameObject.SetActive(false);

                currentSlots.Add(slot);
            }

            // if we used assetHolder list directly, also populate currentSlots
            if (currentSlots.Count == 0 && slotSource != null)
            {
                currentSlots.AddRange(slotSource);
            }
        }
    }
}
